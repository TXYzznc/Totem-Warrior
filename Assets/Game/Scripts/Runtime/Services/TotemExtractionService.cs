using System;
using UnityEngine;

public sealed class TotemExtractionService : TotemRuntimeServiceBase, ITotemRuntimeTickService, ITotemGameplaySimulationService
{
    private const string RuntimeRootName = "[TotemExtractionPoints]";

    private readonly TotemExtractionPoint[] activePoints =
        new TotemExtractionPoint[TotemExtractionPointGenerator.MaxPointCount];
    private readonly int[] extractedParticipantIds = new int[TotemFirstPlayableRules.TeamSize];
    private TotemExtractionConfig config = new TotemExtractionConfig();
    private TotemInputService inputService;
    private TotemMatchFlowService matchFlowService;
    private TotemMapService mapService;
    private TotemActorService actorService;
    private TotemFirstPlayableLifecycleService lifecycleService;
    private TotemGameFlowService gameFlowService;
    private GameObject runtimeRoot;
    private int activePointCount;
    private int focusedPointIndex = -1;
    private int extractedParticipantCount;
    private int extractedTeamId;
    private float interactionProgress;
    private bool interactionBlockedUntilRelease;
    private bool unlocked;
    private bool completed;
    private string lastReason = string.Empty;

    public override string ServiceName => "Extraction";
    public bool IsUnlocked => unlocked;
    public bool IsCompleted => completed;
    public int ActivePointCount => activePointCount;

    public event Action<TotemExtractionPoint[]> ExtractionUnlocked;
    public event Action<int> LocalTeamExtracted;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        inputService = runtime.GetService<TotemInputService>();
        matchFlowService = runtime.GetService<TotemMatchFlowService>();
        mapService = runtime.GetService<TotemMapService>();
        actorService = runtime.GetService<TotemActorService>();
        lifecycleService = runtime.GetService<TotemFirstPlayableLifecycleService>();
        gameFlowService = runtime.GetService<TotemGameFlowService>();
        if (actorService != null)
        {
            actorService.DamageResolved += OnDamageResolved;
        }

        if (gameFlowService != null)
        {
            gameFlowService.StateChanged += OnGameFlowStateChanged;
        }
    }

    protected override void OnShutdown()
    {
        if (actorService != null)
        {
            actorService.DamageResolved -= OnDamageResolved;
        }

        if (gameFlowService != null)
        {
            gameFlowService.StateChanged -= OnGameFlowStateChanged;
        }

        ResetMatchState();
        inputService = null;
        matchFlowService = null;
        mapService = null;
        actorService = null;
        lifecycleService = null;
        gameFlowService = null;
        ExtractionUnlocked = null;
        LocalTeamExtracted = null;
    }

    public void Configure(TotemExtractionConfig value)
    {
        config = value ?? new TotemExtractionConfig();
        config.pointCount = Mathf.Clamp(config.pointCount, 1, TotemExtractionPointGenerator.MaxPointCount);
        config.interactSeconds = Mathf.Max(0.1f, config.interactSeconds);
        config.interactRadius = Mathf.Max(0.5f, config.interactRadius);
    }

    public void Tick(float deltaTime)
    {
        TotemInputSnapshot input = inputService?.Current ?? TotemInputSnapshot.Empty;
        if (input.extractionUnlockPressed)
        {
            TryUnlock("DebugInput.ShiftSpace");
        }

        if (!unlocked || completed || matchFlowService?.IsGameplaySuspended != false)
        {
            ResetInteraction(blockUntilRelease: false);
            return;
        }

        TotemActorModel player = actorService?.Player;
        if (!TryResolveEligiblePoint(player, out int pointIndex))
        {
            ResetInteraction(blockUntilRelease: input.interactHeld);
            return;
        }

        if (!input.interactHeld)
        {
            interactionBlockedUntilRelease = false;
            ResetInteraction(blockUntilRelease: false);
            return;
        }

        if (interactionBlockedUntilRelease)
        {
            return;
        }

        if (focusedPointIndex != pointIndex)
        {
            interactionProgress = 0f;
            focusedPointIndex = pointIndex;
        }

        interactionProgress = Mathf.Min(config.interactSeconds, interactionProgress + Mathf.Max(0f, deltaTime));
        if (interactionProgress + 0.0001f >= config.interactSeconds)
        {
            CommitLocalTeamExtraction(player);
        }
    }

    public bool TryUnlock(string reason)
    {
        if (unlocked || completed || !CanUnlockInPhase(matchFlowService?.CurrentPhase ?? TotemMatchPhase.FrontEnd))
        {
            lastReason = unlocked ? "AlreadyUnlocked" : "PhaseRejected";
            return false;
        }

        TotemMapSnapshot map = mapService?.CurrentMap;
        int seed = Runtime?.GetService<TotemUIService>()?.LastLocalMatchSeed ?? map?.Seed ?? 1;
        activePointCount = TotemExtractionPointGenerator.Generate(map, seed, config.pointCount, activePoints);
        if (activePointCount <= 0)
        {
            lastReason = "NoReachableExtractionAnchors";
            return false;
        }

        unlocked = true;
        lastReason = reason ?? "Unlocked";
        CreateRuntimeVisuals();
        ExtractionUnlocked?.Invoke(CaptureActivePoints());
        GFTrace.Success("TotemExtraction", "Extraction.Unlocked", null, GFTrace.Data(
            "reason", lastReason,
            "pointCount", activePointCount.ToString(),
            "phase", matchFlowService.CurrentPhase.ToString()));
        return true;
    }

    public bool ShouldReserveLocalInteraction()
    {
        return unlocked
            && !completed
            && matchFlowService?.IsGameplaySuspended == false
            && TryResolveEligiblePoint(actorService?.Player, out _);
    }

    public TotemExtractionPoint[] CaptureActivePoints()
    {
        var result = new TotemExtractionPoint[activePointCount];
        Array.Copy(activePoints, result, activePointCount);
        return result;
    }

    public TotemExtractionSnapshot CaptureSnapshot()
    {
        var ids = new int[extractedParticipantCount];
        Array.Copy(extractedParticipantIds, ids, extractedParticipantCount);
        return new TotemExtractionSnapshot
        {
            unlocked = unlocked,
            completed = completed,
            activePointCount = activePointCount,
            interactionProgress = interactionProgress,
            interactionDuration = config.interactSeconds,
            focusedPointInstanceId = focusedPointIndex >= 0 ? activePoints[focusedPointIndex].InstanceId : 0,
            extractedTeamId = extractedTeamId,
            extractedParticipantIds = ids,
            lastReason = lastReason,
        };
    }

    public static bool CanUnlockInPhase(TotemMatchPhase phase)
    {
        return phase == TotemMatchPhase.Round4Combat
            || phase == TotemMatchPhase.Build5
            || phase == TotemMatchPhase.Round5Combat;
    }

    private bool TryResolveEligiblePoint(TotemActorModel player, out int pointIndex)
    {
        pointIndex = -1;
        if (player == null || !player.TeamId.IsValid || !IsAliveAndStanding(player) || HasDownedTeammate(player))
        {
            return false;
        }

        float bestDistance = config.interactRadius * config.interactRadius;
        for (int i = 0; i < activePointCount; i++)
        {
            float distance = (player.Position - activePoints[i].Position).sqrMagnitude;
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                pointIndex = i;
            }
        }

        return pointIndex >= 0;
    }

    private bool IsAliveAndStanding(TotemActorModel actor)
    {
        TotemFirstPlayableParticipantLifeState state = lifecycleService?.GetOrCreateState(actor);
        return actor != null && actor.Health > 0f && (state == null || state.LifeState == TotemFirstPlayableLifeState.Alive);
    }

    private bool HasDownedTeammate(TotemActorModel player)
    {
        var actors = actorService?.Actors;
        if (actors == null)
        {
            return false;
        }

        for (int i = 0; i < actors.Count; i++)
        {
            TotemActorModel candidate = actors[i];
            if (candidate == null || candidate == player || candidate.TeamId != player.TeamId)
            {
                continue;
            }

            if (lifecycleService?.GetOrCreateState(candidate)?.IsDowned == true)
            {
                return true;
            }
        }

        return false;
    }

    private void CommitLocalTeamExtraction(TotemActorModel player)
    {
        if (completed || player == null)
        {
            return;
        }

        extractedParticipantCount = 0;
        extractedTeamId = player.TeamId.Value;
        var actors = actorService?.Actors;
        for (int i = 0; actors != null && i < actors.Count; i++)
        {
            TotemActorModel actor = actors[i];
            if (actor == null || actor.TeamId != player.TeamId)
            {
                continue;
            }

            TotemFirstPlayableParticipantLifeState state = lifecycleService?.GetOrCreateState(actor);
            if (state?.IsEliminated == true)
            {
                continue;
            }

            if (extractedParticipantCount < extractedParticipantIds.Length)
            {
                extractedParticipantIds[extractedParticipantCount++] = actor.ParticipantId;
            }

            if (actor.GameObject != null)
            {
                actor.GameObject.SetActive(false);
            }
        }

        completed = true;
        lastReason = "LocalTeamExtracted";
        ResetInteraction(blockUntilRelease: true);
        LocalTeamExtracted?.Invoke(extractedTeamId);
        GFTrace.Success("TotemExtraction", "Team.Extracted", null, GFTrace.Data(
            "teamId", extractedTeamId.ToString(),
            "participantCount", extractedParticipantCount.ToString()));
        Runtime?.GetService<TotemCombatService>()?.FinishLocalTeamExtraction(extractedTeamId, player.ParticipantId);
    }

    private void OnDamageResolved(TotemDamageRecord record)
    {
        if (!completed && record.Amount > 0f && record.Target == actorService?.Player && interactionProgress > 0f)
        {
            lastReason = "InterruptedByDamage";
            ResetInteraction(blockUntilRelease: true);
        }
    }

    private void OnGameFlowStateChanged(TotemGameFlowState previous, TotemGameFlowState current)
    {
        if (current == TotemGameFlowState.MainMenu)
        {
            ResetMatchState();
        }
    }

    private void ResetInteraction(bool blockUntilRelease)
    {
        interactionProgress = 0f;
        focusedPointIndex = -1;
        interactionBlockedUntilRelease |= blockUntilRelease;
    }

    private void ResetMatchState()
    {
        unlocked = false;
        completed = false;
        activePointCount = 0;
        extractedParticipantCount = 0;
        extractedTeamId = 0;
        lastReason = string.Empty;
        interactionBlockedUntilRelease = false;
        ResetInteraction(blockUntilRelease: false);
        Array.Clear(activePoints, 0, activePoints.Length);
        Array.Clear(extractedParticipantIds, 0, extractedParticipantIds.Length);
        if (runtimeRoot != null)
        {
            UnityEngine.Object.Destroy(runtimeRoot);
            runtimeRoot = null;
        }
    }

    private void CreateRuntimeVisuals()
    {
        if (runtimeRoot != null)
        {
            UnityEngine.Object.Destroy(runtimeRoot);
        }

        runtimeRoot = new GameObject(RuntimeRootName);
        var colorBlock = new MaterialPropertyBlock();
        Color extractionColor = new Color(0.15f, 0.95f, 0.75f, 0.65f);
        colorBlock.SetColor("_BaseColor", extractionColor);
        colorBlock.SetColor("_Color", extractionColor);
        for (int i = 0; i < activePointCount; i++)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "ExtractionPoint_" + activePoints[i].InstanceId;
            visual.transform.SetParent(runtimeRoot.transform, false);
            visual.transform.position = activePoints[i].Position;
            visual.transform.localScale = new Vector3(config.interactRadius * 2f, 0.08f, config.interactRadius * 2f);
            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.SetPropertyBlock(colorBlock);
            }
        }
    }
}
