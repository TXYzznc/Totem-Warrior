using System;
using System.Collections.Generic;

public sealed class TotemFirstPlayableSocialService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    private readonly Dictionary<int, TotemMatchAchievementCounter> achievements =
        new Dictionary<int, TotemMatchAchievementCounter>(TotemFirstPlayableRules.ParticipantCount);
    private readonly TotemPigmentTradeLedger tradeLedger = new TotemPigmentTradeLedger();
    private TotemConstructionIntelligenceSnapshot[] frozenSnapshots = Array.Empty<TotemConstructionIntelligenceSnapshot>();
    private TotemMatchFlowService matchFlowService;
    private TotemActorService actorService;
    private TotemFirstPlayableLifecycleService lifecycleService;
    private TotemFirstPlayableTattooBuildService tattooBuildService;
    private TotemMatchPhase pendingCapturePhase = TotemMatchPhase.FrontEnd;
    private int requestSequence;

    public override string ServiceName => "FirstPlayableSocial";

    public event Action<TotemConstructionIntelligenceSnapshot[]> IntelligenceCaptured;
    public event Action<TotemPigmentRequest> PigmentRequestChanged;
    public event Action<TotemPigmentTransfer> PigmentTransferCommitted;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        matchFlowService = runtime.GetService<TotemMatchFlowService>();
        actorService = runtime.GetService<TotemActorService>();
        lifecycleService = runtime.GetService<TotemFirstPlayableLifecycleService>();
        tattooBuildService = runtime.GetService<TotemFirstPlayableTattooBuildService>();

        if (matchFlowService != null)
        {
            matchFlowService.PhaseChanged += OnPhaseChanged;
        }
        if (actorService != null)
        {
            actorService.DamageResolved += OnParticipantDamageResolved;
            actorService.ActorsSpawned += OnActorsSpawned;
        }
        if (lifecycleService != null)
        {
            lifecycleService.LifeStateChanged += OnLifeStateChanged;
        }
    }

    protected override void OnShutdown()
    {
        if (matchFlowService != null)
        {
            matchFlowService.PhaseChanged -= OnPhaseChanged;
        }
        if (actorService != null)
        {
            actorService.DamageResolved -= OnParticipantDamageResolved;
            actorService.ActorsSpawned -= OnActorsSpawned;
        }
        if (lifecycleService != null)
        {
            lifecycleService.LifeStateChanged -= OnLifeStateChanged;
        }

        matchFlowService = null;
        actorService = null;
        lifecycleService = null;
        tattooBuildService = null;
        ResetMatchState();
        IntelligenceCaptured = null;
        PigmentRequestChanged = null;
        PigmentTransferCommitted = null;
    }

    public void Tick(float deltaTime)
    {
        TryCapturePendingBoundary();
    }

    public TotemConstructionIntelligenceSnapshot[] CaptureFrozenSnapshots() => CloneSnapshots(frozenSnapshots);

    public TotemMatchAchievementSnapshot CaptureAchievement(TotemParticipantId participantId) =>
        achievements.TryGetValue(participantId.Value, out TotemMatchAchievementCounter counter)
            ? counter.Capture()
            : default;

    public TotemPigmentRequest[] CapturePigmentRequests() => tradeLedger.CaptureAll();

    public bool TryApplyCommand(
        in TotemGameplayCommand command,
        out TotemPigmentRequest request,
        out TotemPigmentTransfer transfer)
    {
        transfer = default;
        if (TotemPigmentCommandCodec.TryDecodeRequest(command, out TotemPigmentKind pigment, out int amount))
        {
            return TryRequestPigment(command.ParticipantId, pigment, amount, out request, out transfer);
        }

        if (TotemPigmentCommandCodec.TryDecodeResolution(command, out int requestId, out bool approve))
        {
            return TryResolvePigmentRequest(command.ParticipantId, requestId, approve, out request, out transfer);
        }

        request = default;
        return false;
    }

    public bool TryRequestPigment(
        TotemParticipantId requesterId,
        TotemPigmentKind pigment,
        int amount,
        out TotemPigmentRequest request,
        out TotemPigmentTransfer immediateTransfer)
    {
        immediateTransfer = default;
        if (!TotemMatchPhaseContract.IsBuild(matchFlowService?.CurrentPhase ?? TotemMatchPhase.FrontEnd)
            || !TryFindActor(requesterId, out TotemActorModel requester)
            || !TryFindUniqueTeammate(requester, out TotemActorModel teammate))
        {
            request = default;
            return false;
        }

        TotemFirstPlayableTattooBuildState teammateInventory = tattooBuildService?.GetOrCreateState(teammate);
        if (!tradeLedger.TryCreate(
                requesterId,
                new TotemParticipantId(teammate.ParticipantId),
                pigment,
                amount,
                ++requestSequence,
                (int)matchFlowService.CurrentPhase,
                teammateInventory,
                out request))
        {
            return false;
        }

        PigmentRequestChanged?.Invoke(request);
        if (teammate.ControllerKind == TotemParticipantControllerKind.Human)
        {
            return true;
        }

        bool resolved = TryResolvePigmentRequest(
            new TotemParticipantId(teammate.ParticipantId),
            request.RequestId,
            approve: true,
            out request,
            out immediateTransfer);
        return resolved;
    }

    public bool TryResolvePigmentRequest(
        TotemParticipantId responderId,
        int requestId,
        bool approve,
        out TotemPigmentRequest request,
        out TotemPigmentTransfer transfer)
    {
        transfer = default;
        if (!tradeLedger.TryGet(requestId, out TotemPigmentRequest pending)
            || !TryFindActor(pending.TeammateId, out TotemActorModel donor)
            || !TryFindActor(pending.RequesterId, out TotemActorModel receiver))
        {
            request = default;
            return false;
        }

        bool resolved = tradeLedger.TryResolve(
            requestId,
            responderId,
            approve,
            tattooBuildService?.GetOrCreateState(donor),
            tattooBuildService?.GetOrCreateState(receiver),
            out request,
            out transfer);
        if (!request.IsValid && tradeLedger.TryGet(requestId, out TotemPigmentRequest latest))
        {
            request = latest;
        }

        if (request.IsValid)
        {
            PigmentRequestChanged?.Invoke(request);
        }

        if (transfer.RequiresAtomicCommit)
        {
            Counter(transfer.FromParticipantId).AddResourcesShared(transfer.Amount);
            Counter(transfer.ToParticipantId).AddResourcesAcquired(transfer.Amount);
            PigmentTransferCommitted?.Invoke(transfer);
        }

        return resolved;
    }

    public void RecordAllyHealing(TotemParticipantId source, float amount) => Counter(source).AddAllyHealing(amount);
    public void RecordAllyShieldOrMitigation(TotemParticipantId source, float amount) => Counter(source).AddAllyShieldOrMitigation(amount);
    public void RecordCleanseOrControlRemoval(TotemParticipantId source) => Counter(source).AddCleanseOrControlRemoval();
    public void RecordEffectiveControl(TotemParticipantId source, float seconds) => Counter(source).AddEffectiveControl(seconds);
    public void RecordAllyDamageGainCreated(TotemParticipantId source, float amount) => Counter(source).AddAllyDamageGainCreated(amount);
    public void RecordIndirectElementDamage(TotemParticipantId source, float amount) => Counter(source).AddIndirectElementDamage(amount);
    public void RecordResourcesAcquired(TotemParticipantId source, int amount) => Counter(source).AddResourcesAcquired(amount);

    public void RecordReactionAttribution(in TotemReactionAttribution attribution)
    {
        if (attribution.IndirectElementDamage <= 0f)
        {
            return;
        }

        Counter(attribution.TriggerParticipantId).AddIndirectElementDamage(attribution.IndirectElementDamage);
        if (attribution.AssistingParticipantId.IsValid
            && attribution.AssistingParticipantId != attribution.TriggerParticipantId)
        {
            Counter(attribution.AssistingParticipantId).AddIndirectElementDamage(attribution.IndirectElementDamage);
        }
    }

    public void ResetMatchState()
    {
        achievements.Clear();
        tradeLedger.Reset();
        frozenSnapshots = Array.Empty<TotemConstructionIntelligenceSnapshot>();
        pendingCapturePhase = TotemMatchPhase.FrontEnd;
        requestSequence = 0;
    }

    private void OnPhaseChanged(TotemMatchPhase previous, TotemMatchPhase current)
    {
        tradeLedger.ExpirePendingExceptPhase((int)current);
        if (current == TotemMatchPhase.FrontEnd)
        {
            ResetMatchState();
            return;
        }

        if (TotemMatchPhaseContract.IsBuild(current))
        {
            pendingCapturePhase = current;
            TryCapturePendingBoundary();
        }
    }

    private void OnActorsSpawned()
    {
        TryCapturePendingBoundary();
    }

    private void TryCapturePendingBoundary()
    {
        if (!TotemMatchPhaseContract.IsBuild(pendingCapturePhase)
            || actorService?.Actors == null
            || actorService.Actors.Count != TotemFirstPlayableRules.ParticipantCount)
        {
            return;
        }

        CaptureBoundarySnapshots(pendingCapturePhase);
        pendingCapturePhase = TotemMatchPhase.FrontEnd;
    }

    private void CaptureBoundarySnapshots(TotemMatchPhase phase)
    {
        var result = new TotemConstructionIntelligenceSnapshot[TotemFirstPlayableRules.ParticipantCount];
        for (int i = 0; i < actorService.Actors.Count; i++)
        {
            TotemActorModel actor = actorService.Actors[i];
            if (actor == null || actor.ParticipantId < 1 || actor.ParticipantId > result.Length)
            {
                continue;
            }

            TotemFirstPlayableTattooBuildState build = tattooBuildService?.GetOrCreateState(actor);
            result[actor.ParticipantId - 1] = CreateBoundarySnapshot(
                actor,
                build,
                CaptureAchievement(new TotemParticipantId(actor.ParticipantId)),
                phase);
        }

        frozenSnapshots = result;
        IntelligenceCaptured?.Invoke(CloneSnapshots(frozenSnapshots));
    }

    public static TotemConstructionIntelligenceSnapshot CreateBoundarySnapshot(
        TotemActorModel actor,
        TotemFirstPlayableTattooBuildState build,
        TotemMatchAchievementSnapshot achievement,
        TotemMatchPhase phase)
    {
        if (actor == null)
        {
            return null;
        }

        return new TotemConstructionIntelligenceSnapshot
        {
            participantId = actor.ParticipantId,
            teamId = actor.TeamId.Value,
            capturedAtPhase = (int)phase,
            tattoos = CapturePublicTattoos(build),
            attributes = CaptureAttributes(actor),
            achievements = achievement,
        };
    }

    private static TotemPublicTattooSnapshotEntry[] CapturePublicTattoos(TotemFirstPlayableTattooBuildState build)
    {
        if (build == null)
        {
            return Array.Empty<TotemPublicTattooSnapshotEntry>();
        }

        TotemTattooLoadoutEntry[] loadout = build.CaptureLoadout();
        int count = 0;
        for (int i = 0; i < loadout.Length; i++)
        {
            if (loadout[i].IsEquipped)
            {
                count++;
            }
        }

        var result = new TotemPublicTattooSnapshotEntry[count];
        int index = 0;
        for (int i = 0; i < loadout.Length; i++)
        {
            if (!loadout[i].IsEquipped)
            {
                continue;
            }

            result[index++] = new TotemPublicTattooSnapshotEntry
            {
                slot = loadout[i].Slot,
                pattern = loadout[i].Pattern,
                element = loadout[i].Element,
                publicEffectText = TotemFirstPlayableTattooBuildState.GetPublicEffectText(loadout[i].Pattern),
            };
        }

        return result;
    }

    private static TotemAttributeSnapshotEntry[] CaptureAttributes(TotemActorModel actor)
    {
        return new[]
        {
            new TotemAttributeSnapshotEntry { attributeId = "max_health", baseValue = actor.MaxHealth, inMatchBonus = 0f },
            new TotemAttributeSnapshotEntry { attributeId = "weapon_damage_multiplier", baseValue = 1f, inMatchBonus = 0f },
            new TotemAttributeSnapshotEntry { attributeId = "move_speed_multiplier", baseValue = 1f, inMatchBonus = 0f },
        };
    }

    private void OnParticipantDamageResolved(TotemDamageRecord record)
    {
        if (record.Amount <= 0f
            || !(record.Source is TotemActorModel source)
            || record.Target == null
            || source.TeamId == record.Target.TeamId)
        {
            return;
        }

        Counter(new TotemParticipantId(source.ParticipantId)).AddPlayerDamage(record.Amount);
    }

    private void OnLifeStateChanged(TotemActorModel target, TotemDownedStateContract transition)
    {
        if (target == null || transition.Reason == TotemDownedTransitionReason.None)
        {
            return;
        }

        TotemParticipantId targetId = new TotemParticipantId(target.ParticipantId);
        if (transition.Reason == TotemDownedTransitionReason.LethalDamage)
        {
            Counter(targetId).AddSelfDown();
            if (transition.InstigatorParticipantId.IsValid && transition.InstigatorParticipantId != targetId)
            {
                Counter(transition.InstigatorParticipantId).AddPlayerDown();
            }
        }
        else if (transition.Reason == TotemDownedTransitionReason.ReviveCompleted)
        {
            Counter(transition.InstigatorParticipantId).AddSuccessfulRevive();
        }

        if (transition.Current == TotemFirstPlayableLifeState.Eliminated
            && transition.InstigatorParticipantId.IsValid
            && transition.InstigatorParticipantId != targetId)
        {
            Counter(transition.InstigatorParticipantId).AddPlayerElimination();
        }
    }

    private TotemMatchAchievementCounter Counter(TotemParticipantId participantId)
    {
        if (!participantId.IsValid)
        {
            return NullCounter.Instance;
        }

        if (!achievements.TryGetValue(participantId.Value, out TotemMatchAchievementCounter counter))
        {
            counter = new TotemMatchAchievementCounter();
            achievements.Add(participantId.Value, counter);
        }

        return counter;
    }

    private bool TryFindActor(TotemParticipantId participantId, out TotemActorModel actor)
    {
        IReadOnlyList<TotemActorModel> actors = actorService?.Actors;
        if (actors != null)
        {
            for (int i = 0; i < actors.Count; i++)
            {
                if (actors[i]?.ParticipantId == participantId.Value)
                {
                    actor = actors[i];
                    return true;
                }
            }
        }

        actor = null;
        return false;
    }

    private bool TryFindUniqueTeammate(TotemActorModel participant, out TotemActorModel teammate)
    {
        teammate = null;
        IReadOnlyList<TotemActorModel> actors = actorService?.Actors;
        if (participant == null || actors == null || !participant.TeamId.IsValid)
        {
            return false;
        }

        for (int i = 0; i < actors.Count; i++)
        {
            TotemActorModel candidate = actors[i];
            if (candidate == null || candidate == participant || candidate.TeamId != participant.TeamId)
            {
                continue;
            }

            if (teammate != null)
            {
                return false;
            }

            teammate = candidate;
        }

        return teammate != null;
    }

    private static TotemConstructionIntelligenceSnapshot[] CloneSnapshots(
        TotemConstructionIntelligenceSnapshot[] source)
    {
        if (source == null || source.Length == 0)
        {
            return Array.Empty<TotemConstructionIntelligenceSnapshot>();
        }

        var result = new TotemConstructionIntelligenceSnapshot[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            TotemConstructionIntelligenceSnapshot item = source[i];
            if (item == null)
            {
                continue;
            }

            result[i] = new TotemConstructionIntelligenceSnapshot
            {
                participantId = item.participantId,
                teamId = item.teamId,
                capturedAtPhase = item.capturedAtPhase,
                achievements = item.achievements,
                tattoos = item.tattoos == null ? Array.Empty<TotemPublicTattooSnapshotEntry>() : CloneTattoos(item.tattoos),
                attributes = item.attributes == null ? Array.Empty<TotemAttributeSnapshotEntry>() : CloneAttributes(item.attributes),
            };
        }

        return result;
    }

    private static TotemPublicTattooSnapshotEntry[] CloneTattoos(TotemPublicTattooSnapshotEntry[] source)
    {
        var result = new TotemPublicTattooSnapshotEntry[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            TotemPublicTattooSnapshotEntry item = source[i];
            result[i] = item == null ? null : new TotemPublicTattooSnapshotEntry
            {
                slot = item.slot,
                pattern = item.pattern,
                element = item.element,
                publicEffectText = item.publicEffectText ?? string.Empty,
            };
        }

        return result;
    }

    private static TotemAttributeSnapshotEntry[] CloneAttributes(TotemAttributeSnapshotEntry[] source)
    {
        var result = new TotemAttributeSnapshotEntry[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            TotemAttributeSnapshotEntry item = source[i];
            result[i] = item == null ? null : new TotemAttributeSnapshotEntry
            {
                attributeId = item.attributeId ?? string.Empty,
                baseValue = item.baseValue,
                inMatchBonus = item.inMatchBonus,
            };
        }

        return result;
    }

    private sealed class NullCounter : TotemMatchAchievementCounter
    {
        public static readonly NullCounter Instance = new NullCounter();
    }
}
