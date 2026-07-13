using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TotemParticipantReadinessService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    public const float DefaultProtectionSeconds = 5f;
    public const float DefaultLoadingTimeoutSeconds = 90f;

    private readonly Dictionary<TotemActorModel, ReadinessEntry> entries = new Dictionary<TotemActorModel, ReadinessEntry>(64);
    private TotemGameFlowService flowService;
    private TotemInputService inputService;
    private TotemActorService actorService;
    private TotemMatchClockService matchClock;
    private TotemActorModel localPlayer;
    private int transitionCount;
    private int protectionReleaseCount;
    private int timeoutCount;
    private string lastReason = string.Empty;

    public override string ServiceName => "ParticipantReadiness";

    public float ProtectionSeconds { get; set; } = DefaultProtectionSeconds;

    public float LoadingTimeoutSeconds { get; set; } = DefaultLoadingTimeoutSeconds;

    public event Action<TotemActorModel, TotemParticipantLifecycle, TotemParticipantLifecycle, string> LifecycleChanged;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        inputService = runtime.GetService<TotemInputService>();
        actorService = runtime.GetService<TotemActorService>();
        matchClock = runtime.GetService<TotemMatchClockService>();
        if (flowService != null)
        {
            flowService.StateChanged += OnFlowStateChanged;
        }
    }

    protected override void OnShutdown()
    {
        if (flowService != null)
        {
            flowService.StateChanged -= OnFlowStateChanged;
        }

        flowService = null;
        inputService = null;
        actorService = null;
        matchClock = null;
        localPlayer = null;
        entries.Clear();
        LifecycleChanged = null;
        transitionCount = 0;
        protectionReleaseCount = 0;
        timeoutCount = 0;
        lastReason = string.Empty;
    }

    public void Tick(float deltaTime)
    {
        if (flowService?.CurrentState != TotemGameFlowState.CombatHud || entries.Count <= 0)
        {
            return;
        }

        float elapsed = Mathf.Max(0f, deltaTime);
        foreach (var pair in entries)
        {
            pair.Value.Elapsed += elapsed;
        }

        if (localPlayer == null || !entries.TryGetValue(localPlayer, out var entry))
        {
            return;
        }

        if (entry.Lifecycle == TotemParticipantLifecycle.Loading
            && entry.Elapsed >= Mathf.Max(1f, LoadingTimeoutSeconds))
        {
            timeoutCount++;
            Transition(localPlayer, entry, TotemParticipantLifecycle.Disconnected, "ReadyTimeout");
            SetRuntimeObjectActive(localPlayer, false);
            GFTrace.Warning("TotemParticipant", "ReadyTimeout", null, GFTrace.Data(
                "participantId", localPlayer.ActorId.ToString(),
                "elapsed", entry.Elapsed.ToString("F2"),
                "worldTime", (matchClock?.WorldTime ?? 0f).ToString("F2")));
            return;
        }

        if (entry.Lifecycle != TotemParticipantLifecycle.Protected)
        {
            return;
        }

        if (HasActionableIntent(inputService?.Current ?? TotemInputSnapshot.Empty))
        {
            ReleaseProtection(localPlayer, entry, "InputIntent");
            return;
        }

        if (entry.Elapsed >= Mathf.Max(0f, ProtectionSeconds))
        {
            ReleaseProtection(localPlayer, entry, "ProtectionTimeout");
        }
    }

    public bool NotifyLocalClientReady(TotemActorModel expectedPlayer, string reason)
    {
        if (flowService?.CurrentState != TotemGameFlowState.CombatHud
            || expectedPlayer == null
            || expectedPlayer != localPlayer
            || !expectedPlayer.IsAlive
            || !entries.TryGetValue(expectedPlayer, out var entry)
            || entry.Lifecycle != TotemParticipantLifecycle.Loading)
        {
            return false;
        }

        SetRuntimeObjectActive(expectedPlayer, true);
        Transition(expectedPlayer, entry, TotemParticipantLifecycle.Protected, string.IsNullOrWhiteSpace(reason) ? "ClientReady" : reason);
        GFTrace.Success("TotemParticipant", "Ready", null, GFTrace.Data(
            "participantId", expectedPlayer.ActorId.ToString(),
            "worldTime", (matchClock?.WorldTime ?? 0f).ToString("F2"),
            "protectionSeconds", Mathf.Max(0f, ProtectionSeconds).ToString("F2")));
        return true;
    }

    public TotemParticipantLifecycle GetLifecycle(TotemActorModel participant)
    {
        if (participant != null && entries.TryGetValue(participant, out var entry))
        {
            return entry.Lifecycle;
        }

        return TotemActorService.IsParticipantActor(participant)
            ? TotemParticipantLifecycle.Active
            : TotemParticipantLifecycle.Eliminated;
    }

    public bool CanAct(TotemActorModel participant)
    {
        return participant != null
            && participant.IsAlive
            && GetLifecycle(participant) == TotemParticipantLifecycle.Active;
    }

    public bool CanBeTargeted(TotemActorModel participant)
    {
        return CanAct(participant);
    }

    public bool CountsAsAlive(TotemActorModel participant)
    {
        if (participant == null || !participant.IsAlive || !TotemActorService.IsParticipantActor(participant))
        {
            return false;
        }

        var lifecycle = GetLifecycle(participant);
        return lifecycle != TotemParticipantLifecycle.Eliminated
            && lifecycle != TotemParticipantLifecycle.Disconnected;
    }

    public TotemParticipantReadinessSnapshot CaptureSnapshot()
    {
        var snapshot = new TotemParticipantReadinessSnapshot
        {
            participantCount = entries.Count,
            transitionCount = transitionCount,
            protectionReleaseCount = protectionReleaseCount,
            timeoutCount = timeoutCount,
            lastReason = lastReason,
            worldTime = matchClock?.WorldTime ?? 0f,
            localLifecycle = localPlayer == null ? string.Empty : GetLifecycle(localPlayer).ToString(),
        };

        foreach (var pair in entries)
        {
            switch (pair.Value.Lifecycle)
            {
                case TotemParticipantLifecycle.Loading:
                    snapshot.loadingCount++;
                    break;
                case TotemParticipantLifecycle.Protected:
                    snapshot.protectedCount++;
                    break;
                case TotemParticipantLifecycle.Active:
                    snapshot.activeCount++;
                    break;
                case TotemParticipantLifecycle.Disconnected:
                    snapshot.disconnectedCount++;
                    break;
            }
        }

        return snapshot;
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            BeginRun();
            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            entries.Clear();
            localPlayer = null;
        }
    }

    private void BeginRun()
    {
        entries.Clear();
        localPlayer = actorService?.Player;
        var participants = actorService?.Actors;
        if (participants == null)
        {
            return;
        }

        for (int i = 0; i < participants.Count; i++)
        {
            var participant = participants[i];
            if (!TotemActorService.IsParticipantActor(participant))
            {
                continue;
            }

            entries[participant] = new ReadinessEntry
            {
                Lifecycle = participant == localPlayer
                    ? TotemParticipantLifecycle.Loading
                    : TotemParticipantLifecycle.Active,
            };
            participant.SetLifecycle(entries[participant].Lifecycle, participant == localPlayer ? "RunLoading" : "AuthorityReady");
        }

        SetRuntimeObjectActive(localPlayer, false);

        GFTrace.Success("TotemParticipant", "Readiness.RunStarted", null, GFTrace.Data(
            "participantCount", entries.Count.ToString(),
            "localPlayerId", (localPlayer?.ActorId ?? 0).ToString()));
    }

    private void ReleaseProtection(TotemActorModel participant, ReadinessEntry entry, string reason)
    {
        protectionReleaseCount++;
        Transition(participant, entry, TotemParticipantLifecycle.Active, reason);
        actorService?.TryReleasePlayerStartupProtection(participant, reason);
        GFTrace.Success("TotemParticipant", "ProtectionReleased", null, GFTrace.Data(
            "participantId", participant.ActorId.ToString(),
            "reason", reason,
            "worldTime", (matchClock?.WorldTime ?? 0f).ToString("F2")));
    }

    private void Transition(
        TotemActorModel participant,
        ReadinessEntry entry,
        TotemParticipantLifecycle next,
        string reason)
    {
        var previous = entry.Lifecycle;
        entry.Lifecycle = next;
        entry.Elapsed = 0f;
        participant?.SetLifecycle(next, reason);
        transitionCount++;
        lastReason = reason ?? string.Empty;
        LifecycleChanged?.Invoke(participant, previous, next, lastReason);
        GFTrace.Info("TotemParticipant", "LifecycleChanged", null, GFTrace.Data(
            "participantId", participant?.ActorId.ToString() ?? "0",
            "from", previous.ToString(),
            "to", next.ToString(),
            "reason", lastReason,
            "worldTime", (matchClock?.WorldTime ?? 0f).ToString("F2")));
    }

    private static bool HasActionableIntent(TotemInputSnapshot input)
    {
        return input.move.sqrMagnitude > 0.0001f
            || input.attackPressed
            || input.attackHeld
            || input.skillSlotEPressed
            || input.skillSlotQPressed
            || input.dodgePressed
            || input.interactPressed
            || input.selfTattooTogglePressed;
    }

    private static void SetRuntimeObjectActive(TotemActorModel participant, bool active)
    {
        if (participant?.GameObject != null && participant.GameObject.activeSelf != active)
        {
            participant.GameObject.SetActive(active);
        }
    }

    private sealed class ReadinessEntry
    {
        public TotemParticipantLifecycle Lifecycle;
        public float Elapsed;
    }
}

[Serializable]
public sealed class TotemParticipantReadinessSnapshot
{
    public int participantCount;
    public int loadingCount;
    public int protectedCount;
    public int activeCount;
    public int disconnectedCount;
    public int transitionCount;
    public int protectionReleaseCount;
    public int timeoutCount;
    public string localLifecycle;
    public string lastReason;
    public float worldTime;
}
