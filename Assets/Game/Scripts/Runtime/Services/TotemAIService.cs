using System.Collections.Generic;
using System;
using UnityEngine;

public sealed class TotemAIService : TotemRuntimeServiceBase, ITotemRuntimeTickService, ITotemGameplaySimulationService
{
    public const float LodRadius = 20f;
    public const float LodScanInterval = 0.2f;
    public const float SmartColdInterval = 0.5f;
    public const float LightHotInterval = 0.2f;
    public const float LightColdInterval = 2f;
    public const float SmartAttackRange = 4f;
    public const float LightAttackRange = 3f;
    public const float MinMapResourceChaseWeight = 1f;

    private readonly List<TotemAIActorState> aiStates = new List<TotemAIActorState>(64);
    private TotemGameFlowService flowService;
    private TotemMatchFlowService matchFlowService;
    private TotemActorService actorService;
    private TotemMatchClockService matchClock;
    private TotemStatusService statusService;
    private TotemFirstPlayableElementService elementService;
    private TotemFirstPlayableLifecycleService lifecycleService;
    private TotemFirstPlayableTattooBuildService tattooBuildService;
    private TotemWeaponService weaponService;
    private TotemMapResourceService mapResourceService;
    private TotemVfxService vfxService;
    private TotemParticipantReadinessService readinessService;
    private TotemAITuningDefinition tuning = TotemAITuningDefinition.Default;
    private TotemBotProfileDefinition[] botProfiles = Array.Empty<TotemBotProfileDefinition>();
    private TotemBotBuildPresetDefinition[] botBuildPresets = Array.Empty<TotemBotBuildPresetDefinition>();
    private bool active;
    private float elapsedSec;
    private float lodScanRemaining;
    private int totalDecisions;
    private int totalAttacks;
    private int decisionSequence;
    private int gameplayCommandSequence;
    private TotemAIDecisionRecord lastDecision;

    public override string ServiceName => "AI";

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        matchFlowService = runtime.GetService<TotemMatchFlowService>();
        actorService = runtime.GetService<TotemActorService>();
        matchClock = runtime.GetService<TotemMatchClockService>();
        statusService = runtime.GetService<TotemStatusService>();
        elementService = runtime.GetService<TotemFirstPlayableElementService>();
        lifecycleService = runtime.GetService<TotemFirstPlayableLifecycleService>();
        tattooBuildService = runtime.GetService<TotemFirstPlayableTattooBuildService>();
        weaponService = runtime.GetService<TotemWeaponService>();
        mapResourceService = runtime.GetService<TotemMapResourceService>();
        vfxService = runtime.GetService<TotemVfxService>();
        readinessService = runtime.GetService<TotemParticipantReadinessService>();
        var catalog = runtime.GetService<TotemDataService>()?.GameplayCatalog ?? TotemDataService.LoadGameplayCatalogOrDefault();
        tuning = catalog.aiTuning ?? TotemAITuningDefinition.Default;
        botProfiles = catalog.CreateBotProfiles();
        botBuildPresets = catalog.CreateBotBuildPresets();
        if (flowService != null)
        {
            flowService.StateChanged += OnFlowStateChanged;
        }

        if (actorService != null)
        {
            actorService.DamageApplied += OnDamageApplied;
        }

        if (matchFlowService != null)
        {
            matchFlowService.PhaseChanged += OnMatchPhaseChanged;
        }
    }

    protected override void OnShutdown()
    {
        if (flowService != null)
        {
            flowService.StateChanged -= OnFlowStateChanged;
            flowService = null;
        }

        if (actorService != null)
        {
            actorService.DamageApplied -= OnDamageApplied;
            actorService = null;
        }

        if (matchFlowService != null)
        {
            matchFlowService.PhaseChanged -= OnMatchPhaseChanged;
            matchFlowService = null;
        }

        matchClock = null;
        statusService = null;
        elementService = null;
        lifecycleService = null;
        tattooBuildService = null;
        weaponService = null;
        mapResourceService = null;
        vfxService = null;
        readinessService = null;
        tuning = TotemAITuningDefinition.Default;
        botProfiles = Array.Empty<TotemBotProfileDefinition>();
        botBuildPresets = Array.Empty<TotemBotBuildPresetDefinition>();
        ClearRuntimeState();
    }

    public void Tick(float deltaTime)
    {
        if (!active || actorService?.Player == null || deltaTime <= 0f)
        {
            return;
        }

        elapsedSec += deltaTime;
        TickLod(deltaTime);
        TickAIs(deltaTime);
    }

    public IReadOnlyList<TotemAIActorState> States => aiStates;

    public TotemAISnapshot CaptureSnapshot()
    {
        var snapshot = new TotemAISnapshot
        {
            active = active,
            playerStartupTargetSuppressed = actorService?.PlayerStartupInvulnerable ?? false,
            totalDecisions = totalDecisions,
            totalAttacks = totalAttacks,
        };

        for (int i = 0; i < aiStates.Count; i++)
        {
            var state = aiStates[i];
            if (state.Actor == null)
            {
                continue;
            }

            if (state.Actor.ControllerKind == TotemParticipantControllerKind.SmartBot)
            {
                snapshot.smartCount++;
                if (state.Profile != null)
                {
                    snapshot.smartProfileCount++;
                }
            }
            else if (state.Actor.ControllerKind == TotemParticipantControllerKind.LightBot)
            {
                snapshot.lightCount++;
                if (state.Profile != null)
                {
                    snapshot.lightProfileCount++;
                }
            }

            if (state.Profile != null)
            {
                snapshot.profiledCount++;
            }

            if (state.Bucket == TotemAILodBucket.Hot)
            {
                snapshot.hotCount++;
            }
            else
            {
                snapshot.coldCount++;
            }

            switch (state.State)
            {
                case TotemAIState.Chase:
                    snapshot.chaseCount++;
                    break;
                case TotemAIState.Attack:
                    snapshot.attackCount++;
                    break;
                case TotemAIState.Wander:
                    snapshot.wanderCount++;
                    break;
                case TotemAIState.Loot:
                    snapshot.lootCount++;
                    break;
            }

            snapshot.totalResourcePickupClaims += state.ResourcePickupClaims;
        }

        CopyLastDecisionToSnapshot(snapshot);
        return snapshot;
    }

    public static float GetDecisionInterval(TotemActorKind kind, TotemAILodBucket bucket)
    {
        if (kind == TotemActorKind.SmartAi)
        {
            return bucket == TotemAILodBucket.Hot ? 0f : SmartColdInterval;
        }

        if (kind == TotemActorKind.LightAi)
        {
            return bucket == TotemAILodBucket.Hot ? LightHotInterval : LightColdInterval;
        }

        return 0f;
    }

    public static TotemAILodBucket ResolveBucket(Vector3 actorPosition, Vector3 playerPosition)
    {
        Vector3 delta = actorPosition - playerPosition;
        delta.y = 0f;
        return delta.sqrMagnitude <= LodRadius * LodRadius ? TotemAILodBucket.Hot : TotemAILodBucket.Cold;
    }

    public float GetRuntimeDecisionInterval(TotemActorKind kind, TotemAILodBucket bucket)
    {
        if (kind == TotemActorKind.SmartAi)
        {
            return bucket == TotemAILodBucket.Hot ? 0f : tuning.smartColdInterval;
        }

        if (kind == TotemActorKind.LightAi)
        {
            return bucket == TotemAILodBucket.Hot ? tuning.lightHotInterval : tuning.lightColdInterval;
        }

        return 0f;
    }

    public float GetRuntimeDecisionInterval(TotemAIActorState state)
    {
        if (state == null || state.Actor == null)
        {
            return 0f;
        }

        if (state.Profile != null && state.Actor.ControllerKind == TotemParticipantControllerKind.SmartBot)
        {
            return state.Bucket == TotemAILodBucket.Hot
                ? 0f
                : Mathf.Max(0.05f, state.Profile.RethinkInterval * 0.025f);
        }

        return GetRuntimeDecisionInterval(state.Actor.Kind, state.Bucket);
    }

    public TotemAILodBucket ResolveRuntimeBucket(Vector3 actorPosition, Vector3 playerPosition)
    {
        Vector3 delta = actorPosition - playerPosition;
        delta.y = 0f;
        return delta.sqrMagnitude <= tuning.lodRadius * tuning.lodRadius ? TotemAILodBucket.Hot : TotemAILodBucket.Cold;
    }

    public static TotemAIActorState[] BuildInitialStates(IReadOnlyList<TotemActorModel> actors, Vector3 playerPosition)
    {
        var catalog = TotemDataService.LoadGameplayCatalogOrDefault();
        return BuildInitialStates(actors, playerPosition, catalog.CreateBotProfiles(), catalog.CreateBotBuildPresets());
    }

    public static TotemAIActorState[] BuildInitialStates(
        IReadOnlyList<TotemActorModel> actors,
        Vector3 playerPosition,
        IReadOnlyList<TotemBotProfileDefinition> profiles,
        IReadOnlyList<TotemBotBuildPresetDefinition> presets)
    {
        if (actors == null)
        {
            return new TotemAIActorState[0];
        }

        var result = new List<TotemAIActorState>(actors.Count);
        int smartIndex = 0;
        int lightIndex = 0;
        for (int i = 0; i < actors.Count; i++)
        {
            var actor = actors[i];
            if (actor == null
                || (actor.ControllerKind != TotemParticipantControllerKind.SmartBot
                    && actor.ControllerKind != TotemParticipantControllerKind.LightBot))
            {
                continue;
            }

            bool smart = actor.ControllerKind == TotemParticipantControllerKind.SmartBot;
            int profileIndex = smart ? smartIndex++ : lightIndex++;
            var profile = SelectProfile(actor.Kind, profileIndex, profiles);
            var preset = ResolvePreset(profile?.PreferredPreset ?? 0, presets);
            result.Add(new TotemAIActorState
            {
                Actor = actor,
                State = smart ? TotemAIState.Chase : TotemAIState.Wander,
                Bucket = ResolveBucket(actor.Position, playerPosition),
                Profile = profile,
                BuildPreset = preset,
                WanderDirection = BuildWanderDirection(actor.ActorId, 0),
                NextDecisionTime = 0f,
            });
        }

        return result.ToArray();
    }

    public static TotemBotProfileDefinition SelectProfile(TotemActorKind kind, int kindIndex, IReadOnlyList<TotemBotProfileDefinition> profiles)
    {
        if (profiles == null || profiles.Count <= 0)
        {
            return null;
        }

        int matchingCount = 0;
        for (int i = 0; i < profiles.Count; i++)
        {
            if (profiles[i] != null && profiles[i].ActorKind == kind)
            {
                matchingCount++;
            }
        }

        if (matchingCount <= 0)
        {
            return null;
        }

        int target = Mathf.Abs(kindIndex) % matchingCount;
        int cursor = 0;
        for (int i = 0; i < profiles.Count; i++)
        {
            var profile = profiles[i];
            if (profile == null || profile.ActorKind != kind)
            {
                continue;
            }

            if (cursor == target)
            {
                return profile;
            }

            cursor++;
        }

        return null;
    }

    public static TotemBotBuildPresetDefinition ResolvePreset(int presetId, IReadOnlyList<TotemBotBuildPresetDefinition> presets)
    {
        if (presets == null || presets.Count <= 0)
        {
            return null;
        }

        for (int i = 0; i < presets.Count; i++)
        {
            if (presets[i] != null && presets[i].PresetId == presetId)
            {
                return presets[i];
            }
        }

        return presets[0];
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            BuildRuntimeState();
            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            ClearRuntimeState();
        }
    }

    private void BuildRuntimeState()
    {
        ClearRuntimeState();
        var actors = actorService?.Actors;
        var player = actorService?.Player;
        if (actors == null || player == null)
        {
            return;
        }

        var states = BuildInitialStates(actors, player.Position, botProfiles, botBuildPresets);
        for (int i = 0; i < states.Length; i++)
        {
            var state = states[i];
            EnsureAiLoadout(state);
            aiStates.Add(state);
        }

        TryApplyFirstPlayableBotBuilds(matchFlowService?.CurrentPhase ?? TotemMatchPhase.FrontEnd);

        active = true;
        lodScanRemaining = 0f;
        GFTrace.Success("TotemAI", "Activated", null, GFTrace.Data("aiCount", aiStates.Count.ToString()));
    }

    private void EnsureAiLoadout(TotemAIActorState state)
    {
        var actor = state?.Actor;
        if (actor == null)
        {
            return;
        }

        weaponService?.EquipWeapon(actor, TotemWeaponService.DefaultWeaponId);
    }

    private void ClearRuntimeState()
    {
        aiStates.Clear();
        active = false;
        elapsedSec = 0f;
        lodScanRemaining = 0f;
        totalDecisions = 0;
        totalAttacks = 0;
        decisionSequence = 0;
        gameplayCommandSequence = 0;
        lastDecision = null;
    }

    private void OnMatchPhaseChanged(TotemMatchPhase previousPhase, TotemMatchPhase nextPhase)
    {
        if (TotemMatchPhaseContract.IsBuild(nextPhase))
        {
            TryApplyFirstPlayableBotBuilds(nextPhase);
        }
    }

    private void TryApplyFirstPlayableBotBuilds(TotemMatchPhase phase)
    {
        if (!TotemMatchPhaseContract.IsBuild(phase) || tattooBuildService == null)
        {
            return;
        }

        int buildOrdinal;
        switch (phase)
        {
            case TotemMatchPhase.OpeningBuild: buildOrdinal = 0; break;
            case TotemMatchPhase.Build2: buildOrdinal = 1; break;
            case TotemMatchPhase.Build3: buildOrdinal = 2; break;
            case TotemMatchPhase.Build4: buildOrdinal = 3; break;
            case TotemMatchPhase.Build5: buildOrdinal = 4; break;
            default: return;
        }
        for (int i = 0; i < aiStates.Count; i++)
        {
            TotemActorModel actor = aiStates[i]?.Actor;
            if (actor == null || !actor.IsAlive)
            {
                continue;
            }

            TotemFirstPlayableTattooBuildState state = tattooBuildService.GetOrCreateState(actor);
            if (!TotemFirstPlayableBotBuildPlanner.TryPlan(actor.ParticipantId, buildOrdinal, state, out TotemBotTattooPlan plan))
            {
                GFTrace.Info("TotemAI", "Build.Skipped", null, GFTrace.Data(
                    "participant", actor.ParticipantId.ToString(),
                    "phase", phase.ToString(),
                    "reason", "NoAffordableChange"));
                continue;
            }

            var command = new TotemGameplayCommand(
                new TotemParticipantId(actor.ParticipantId),
                TotemGameplayCommandSource.BotDecision,
                TotemGameplayCommandType.EquipTattoo,
                gameplayCommandSequence++,
                Vector3.zero,
                TotemFirstPlayableTattooCommandCodec.EncodeEquip(plan.Slot, plan.Pattern, plan.Element));
            if (tattooBuildService.TryApplyCommand(command, out TotemTattooMutationResult result))
            {
                GFTrace.Success("TotemAI", "Build.Applied", null, GFTrace.Data(
                    "participant", actor.ParticipantId.ToString(),
                    "phase", phase.ToString(),
                    "slot", plan.Slot.ToString(),
                    "pattern", plan.Pattern.ToString(),
                    "element", plan.Element.ToString()));
            }
            else
            {
                GFTrace.Warning("TotemAI", "Build.Rejected", null, GFTrace.Data(
                    "participant", actor.ParticipantId.ToString(),
                    "phase", phase.ToString(),
                    "reason", result.Code.ToString()));
            }
        }
    }

    private void CopyLastDecisionToSnapshot(TotemAISnapshot snapshot)
    {
        var record = lastDecision;
        if (record == null)
        {
            return;
        }

        snapshot.lastDecisionSequence = record.Sequence;
        snapshot.lastDecisionActorId = record.ActorId;
        snapshot.lastDecisionActorName = record.ActorName;
        snapshot.lastDecisionActorKind = record.ActorKind;
        snapshot.lastDecisionState = record.State;
        snapshot.lastDecisionBucket = record.Bucket;
        snapshot.lastDecisionAction = record.Action;
        snapshot.lastDecisionReason = record.Reason;
        snapshot.lastDecisionTargetActorId = record.TargetActorId;
        snapshot.lastDecisionTargetName = record.TargetName;
        snapshot.lastDecisionTargetKind = record.TargetKind;
        snapshot.lastDecisionTargetDomain = record.TargetDomain;
        snapshot.lastDecisionDistance = record.Distance;
        snapshot.lastDecisionSafetyScore = record.SafetyScore;
        snapshot.lastDecisionProfileBotId = record.ProfileBotId;
        snapshot.lastDecisionBuildPresetId = record.BuildPresetId;
        snapshot.lastDecisionWeaponId = record.WeaponId;
        snapshot.lastDecisionPersonality = record.Personality;
    }

    private void RecordDecision(
        TotemAIActorState state,
        string action,
        string reason,
        TotemCombatantModel target = null,
        float distance = -1f,
        string weaponId = null)
    {
        if (state?.Actor == null)
        {
            return;
        }

        var record = state.LastDecision;
        if (record == null)
        {
            record = new TotemAIDecisionRecord();
            state.LastDecision = record;
        }

        record.Sequence = ++decisionSequence;
        record.ElapsedSec = elapsedSec;
        record.ActorId = state.Actor.ActorId;
        record.ActorName = state.Actor.Name ?? string.Empty;
        record.ActorKind = state.Actor.Kind;
        record.State = state.State;
        record.Bucket = state.Bucket;
        record.Action = action ?? string.Empty;
        record.Reason = reason ?? string.Empty;
        record.TargetActorId = target?.CombatantId ?? 0;
        record.TargetName = target?.Name ?? string.Empty;
        record.TargetKind = target is TotemActorModel participantTarget ? participantTarget.Kind : TotemActorKind.Player;
        record.TargetDomain = target?.Domain ?? TotemCombatantDomain.Participant;
        record.Distance = distance;
        record.ActorHealth = state.Actor.Health;
        record.TargetHealth = target?.Health ?? 0f;
        record.SafetyScore = state.SafetyScore;
        record.ProfileBotId = state.Profile?.BotId ?? 0;
        record.BuildPresetId = state.BuildPreset?.PresetId ?? 0;
        record.WeaponId = weaponId ?? string.Empty;
        record.Personality = state.Profile?.Personality ?? TotemAIPersonality.Hybrid;
        lastDecision = record;
    }

    private void TickLod(float deltaTime)
    {
        lodScanRemaining -= deltaTime;
        if (lodScanRemaining > 0f)
        {
            return;
        }

        lodScanRemaining = tuning.lodScanInterval;
        Vector3 playerPosition = actorService.Player.Position;
        for (int i = 0; i < aiStates.Count; i++)
        {
            var state = aiStates[i];
            if (state.Actor == null || !state.Actor.IsAlive)
            {
                state.State = TotemAIState.Dead;
                continue;
            }

            state.Bucket = ResolveRuntimeBucket(state.Actor.Position, playerPosition);
            if (state.Actor.ControllerKind == TotemParticipantControllerKind.SmartBot)
            {
                state.SafetyScore = CalculateSafety(state.Actor);
            }
        }
    }

    private void TickAIs(float deltaTime)
    {
        for (int i = 0; i < aiStates.Count; i++)
        {
            var state = aiStates[i];
            if (state.Actor == null || !state.Actor.IsAlive)
            {
                state.State = TotemAIState.Dead;
                continue;
            }

            state.LastDamagedElapsed += deltaTime;
            state.AttackCooldownRemaining = Mathf.Max(0f, state.AttackCooldownRemaining - deltaTime);
            state.DodgeCooldownRemaining = Mathf.Max(0f, state.DodgeCooldownRemaining - deltaTime);
            if (state.Actor.Lifecycle != TotemParticipantLifecycle.Active)
            {
                state.State = lifecycleService != null && lifecycleService.IsDowned(state.Actor)
                    ? TotemAIState.Idle
                    : TotemAIState.Dead;
                continue;
            }

            if (IsStatusBlocked(state.Actor))
            {
                state.State = TotemAIState.Idle;
                RecordDecision(state, "Idle", "Status:Stun");
                continue;
            }

            if (TryHandleDownedTeammate(state, deltaTime))
            {
                continue;
            }

            float interval = GetRuntimeDecisionInterval(state);
            if (elapsedSec < state.NextDecisionTime)
            {
                continue;
            }

            state.NextDecisionTime = elapsedSec + interval;
            if (state.Actor.ControllerKind == TotemParticipantControllerKind.SmartBot)
            {
                TickSmart(state, deltaTime);
            }
            else
            {
                TickLight(state, deltaTime);
            }
        }
    }

    private bool TryHandleDownedTeammate(TotemAIActorState state, float deltaTime)
    {
        if (state?.Actor == null || lifecycleService == null)
        {
            return false;
        }

        if (lifecycleService.IsReviving(state.Actor))
        {
            state.State = TotemAIState.Idle;
            return true;
        }

        if (!lifecycleService.TryGetNearestDownedTeammate(
                state.Actor,
                out TotemActorModel teammate,
                out float distance))
        {
            return false;
        }

        bool shouldRecordDecision = elapsedSec >= state.NextDecisionTime;
        if (shouldRecordDecision)
        {
            state.NextDecisionTime = elapsedSec + GetRuntimeDecisionInterval(state);
            state.Decisions++;
            totalDecisions++;
        }

        if (distance > TotemFirstPlayableLifecycleService.ReviveInteractRadius)
        {
            state.State = TotemAIState.Chase;
            float baseSpeed = state.Actor.ControllerKind == TotemParticipantControllerKind.SmartBot
                ? tuning.smartMoveSpeed
                : tuning.lightMoveSpeed;
            MoveTowardPosition(
                state.Actor,
                teammate.Position,
                deltaTime,
                GetProfileMoveSpeed(state, GetActorMoveSpeed(state.Actor, baseSpeed)));
            if (shouldRecordDecision)
            {
                RecordDecision(state, "Chase", "DownedTeammate", teammate, distance);
            }

            return true;
        }

        state.State = TotemAIState.Idle;
        bool began = lifecycleService.TryIssueBeginReviveCommand(
            state.Actor,
            teammate,
            TotemGameplayCommandSource.BotDecision,
            out _);
        if (began && shouldRecordDecision)
        {
            RecordDecision(state, "Revive", "DownedTeammateInRange", teammate, distance);
        }

        return began;
    }

    private void TickSmart(TotemAIActorState state, float deltaTime)
    {
        state.Decisions++;
        totalDecisions++;
        state.ResourcePickupTarget = default;

        float smartVisionRadius = GetProfileVisionRadius(state, GetActorDetectRange(state.Actor, tuning.smartVisionRadius));
        if (TryPursueMapResourcePickup(state, deltaTime, smartVisionRadius))
        {
            return;
        }

        var target = FindBestSmartTarget(state, smartVisionRadius);
        if (target == null)
        {
            state.State = TotemAIState.Idle;
            RecordDecision(state, "Idle", "NoTarget");
            return;
        }

        if (ShouldSmartDodge(state))
        {
            MoveAwayFrom(state.Actor, target, deltaTime, GetProfileMoveSpeed(state, GetActorMoveSpeed(state.Actor, tuning.smartMoveSpeed)) * 1.4f);
            state.State = TotemAIState.Retreat;
            state.DodgeCooldownRemaining = 2f;
            RecordDecision(state, "Dodge", "RecentDamage", target, FlatDistance(state.Actor.Position, target.Position));
            return;
        }

        float distance = FlatDistance(state.Actor.Position, target.Position);
        float attackRange = GetProfileAttackRange(state, GetActorAttackRange(state.Actor, tuning.smartAttackRange));

        if (distance <= attackRange)
        {
            state.State = TotemAIState.Attack;
            TryAiAttack(state, target, GetProfileDamage(state, GetActorDamage(state.Actor, tuning.smartDamage)), GetProfileAttackCooldown(state, tuning.smartAttackCooldown));
            return;
        }

        if (!ShouldChaseTarget(state, distance))
        {
            state.State = TotemAIState.Wander;
            RecordDecision(state, "Wander", "TargetOutsideChasePreference", target, distance);
            return;
        }

        state.State = TotemAIState.Chase;
        MoveToward(state.Actor, target, deltaTime, GetProfileMoveSpeed(state, GetActorMoveSpeed(state.Actor, tuning.smartMoveSpeed)));
        RecordDecision(state, "Chase", ResolveSmartChaseReason(state), target, distance);
    }

    private void TickLight(TotemAIActorState state, float deltaTime)
    {
        state.Decisions++;
        totalDecisions++;

        float lightVisionRadius = GetProfileVisionRadius(state, GetActorDetectRange(state.Actor, tuning.lightVisionRadius));
        var target = FindClosestTarget(state.Actor, lightVisionRadius, includePeerAi: true);
        bool counterWindow = state.LastDamagedElapsed <= 2f;
        if (target != null && counterWindow)
        {
            float distance = FlatDistance(state.Actor.Position, target.Position);
            if (distance <= GetProfileAttackRange(state, GetActorAttackRange(state.Actor, tuning.lightAttackRange)))
            {
                state.State = TotemAIState.Attack;
                TryAiAttack(state, target, GetProfileDamage(state, GetActorDamage(state.Actor, tuning.lightDamage)), GetProfileAttackCooldown(state, tuning.lightAttackCooldown));
                return;
            }

            state.State = TotemAIState.Chase;
            MoveToward(state.Actor, target, deltaTime, GetProfileMoveSpeed(state, GetActorMoveSpeed(state.Actor, tuning.lightMoveSpeed)));
            RecordDecision(state, "Chase", "CounterWindow", target, distance);
            return;
        }

        state.State = TotemAIState.Wander;
        RecordDecision(
            state,
            "Wander",
            target == null ? "NoTarget" : "NoCounterWindow",
            target,
            target == null ? -1f : FlatDistance(state.Actor.Position, target.Position));
        if (state.Decisions % 12 == 0)
        {
            state.WanderDirection = BuildWanderDirection(state.Actor.ActorId, state.Decisions);
        }

        float wanderSpeed = ResolveMoveSpeed(state.Actor, GetProfileMoveSpeed(state, GetActorMoveSpeed(state.Actor, tuning.lightMoveSpeed)) * 0.5f);
        if (wanderSpeed > 0f)
        {
            ApplyMoveCommand(state.Actor, state.WanderDirection, deltaTime, wanderSpeed);
        }
    }

    private bool TryPursueMapResourcePickup(TotemAIActorState state, float deltaTime, float visionRadius)
    {
        state.ResourcePickupTarget = default;
        if (mapResourceService == null || state?.Actor == null || !ShouldPursueMapResourcePickup(state))
        {
            return false;
        }

        float searchRadius = GetMapResourcePickupSearchRadius(state, visionRadius);
        if (searchRadius <= 0f)
        {
            return false;
        }

        if (!mapResourceService.TryFindNearest(state.Actor.Position, searchRadius, out TotemMapResourcePickup pickup))
        {
            return false;
        }

        state.ResourcePickupTarget = pickup;
        state.State = TotemAIState.Loot;
        float distance = FlatDistance(state.Actor.Position, pickup.Position);
        if (distance <= TotemMapResourceService.PickupRadius)
        {
            if (!mapResourceService.TryPickup(state.Actor, pickup.InstanceId, out TotemMapResourcePickupResult result))
            {
                RecordDecision(state, "Loot", $"MapResourcePickupRejected:{result.Reason}", null, distance);
                return true;
            }

            state.ResourcePickupClaims++;
            RecordDecision(state, "Loot", "ClaimMapResourcePickup", null, distance);
            GFTrace.Success("TotemAI", "MapResource.Picked", null, GFTrace.Data(
                "actor", state.Actor.Name,
                "profile", state.Profile?.DisplayName ?? string.Empty,
                "pickup", pickup.InstanceId.ToString(),
                "resourceId", result.Pickup.ResourceId,
                "amount", result.Pickup.Amount.ToString(),
                "reason", result.Reason));
            return true;
        }

        MoveTowardPosition(state.Actor, pickup.Position, deltaTime, GetProfileMoveSpeed(state, GetActorMoveSpeed(state.Actor, tuning.smartMoveSpeed)) * 1.05f);
        RecordDecision(state, "Loot", "ChaseMapResourcePickup", null, distance);
        return true;
    }

    private static bool ShouldPursueMapResourcePickup(TotemAIActorState state)
    {
        return state?.Actor != null &&
               state.Actor.ControllerKind == TotemParticipantControllerKind.SmartBot &&
               GetProfileResourceWeight(state) >= MinMapResourceChaseWeight;
    }

    private static float GetMapResourcePickupSearchRadius(TotemAIActorState state, float visionRadius)
    {
        float weight = GetProfileResourceWeight(state);
        if (weight <= 0f)
        {
            return 0f;
        }

        float clampedVision = Mathf.Max(TotemMapResourceService.PickupRadius, visionRadius);
        float weightedRadius = TotemMapResourceService.PickupRadius + Mathf.Clamp(weight, 0f, 3f) * 12f;
        return Mathf.Min(clampedVision, weightedRadius);
    }

    private static float GetProfileResourceWeight(TotemAIActorState state)
    {
        if (state?.Profile != null)
        {
            return Mathf.Max(0f, state.Profile.TargetResourceWeight);
        }

        return state?.Actor != null && state.Actor.ControllerKind == TotemParticipantControllerKind.SmartBot ? 0.4f : 0f;
    }

    private static bool ShouldChaseTarget(TotemAIActorState state, float distance)
    {
        if (state?.BuildPreset == null || state.Profile == null)
        {
            return true;
        }

        if (state.BuildPreset.BehaviorMacro != TotemAIBehaviorMacro.Camp)
        {
            return true;
        }

        return distance <= Mathf.Max(0.1f, state.Profile.AggroRadius);
    }

    private static float GetProfileVisionRadius(TotemAIActorState state, float fallback)
    {
        return state?.Profile == null || state.Profile.VisionRadius <= 0f ? fallback : state.Profile.VisionRadius;
    }

    private static float GetProfileAttackRange(TotemAIActorState state, float fallback)
    {
        if (state?.Profile == null || state.Profile.AggroRadius <= 0f)
        {
            return fallback;
        }

        return Mathf.Min(fallback, state.Profile.AggroRadius);
    }

    private static float GetProfileAggroRadius(TotemAIActorState state, float fallback)
    {
        return state?.Profile == null || state.Profile.AggroRadius <= 0f ? fallback : state.Profile.AggroRadius;
    }

    private static float GetProfileMoveSpeed(TotemAIActorState state, float fallback)
    {
        if (state?.Profile == null)
        {
            return fallback;
        }

        float macroMul = 1f;
        if (state.BuildPreset != null)
        {
            switch (state.BuildPreset.BehaviorMacro)
            {
                case TotemAIBehaviorMacro.Rush:
                    macroMul = 1.15f;
                    break;
                case TotemAIBehaviorMacro.Camp:
                    macroMul = 0.85f;
                    break;
                case TotemAIBehaviorMacro.Pivot:
                    macroMul = 1.05f;
                    break;
            }
        }

        float confidenceMul = Mathf.Lerp(0.85f, 1.1f, Mathf.Clamp01(state.Profile.Confidence));
        return fallback * macroMul * confidenceMul;
    }

    private static float GetProfileAttackCooldown(TotemAIActorState state, float fallback)
    {
        return state?.Profile == null || state.Profile.AttackCooldown <= 0f ? fallback : state.Profile.AttackCooldown;
    }

    private static float GetProfileDamage(TotemAIActorState state, float fallback)
    {
        if (state?.Profile == null)
        {
            return fallback;
        }

        return fallback * GetProfileDamageMultiplier(state);
    }

    private static float GetActorDetectRange(TotemActorModel actor, float fallback)
    {
        return fallback;
    }

    private static float GetActorAttackRange(TotemActorModel actor, float fallback)
    {
        return fallback;
    }

    private static float GetActorMoveSpeed(TotemActorModel actor, float fallback)
    {
        return fallback;
    }

    private static float GetActorDamage(TotemActorModel actor, float fallback)
    {
        return fallback;
    }

    private static float GetProfileDamageMultiplier(TotemAIActorState state)
    {
        if (state?.Profile == null)
        {
            return 1f;
        }

        return Mathf.Lerp(0.75f, 1.15f, Mathf.Clamp01(state.Profile.Confidence));
    }

    private bool TryAiAttack(TotemAIActorState state, TotemActorModel target, float damage, float cooldown)
    {
        if (state == null || state.Actor == null || state.AttackCooldownRemaining > 0f || target == null || !target.IsAlive)
        {
            return false;
        }

        if (!CanActorAct(state.Actor))
        {
            RecordDecision(state, "Idle", "Status:Stun", target, FlatDistance(state.Actor.Position, target.Position));
            return false;
        }

        if (weaponService == null)
        {
            return false;
        }

        Vector3 rayOrigin = state.Actor.Position + Vector3.up * 1.2f;
        var command = new TotemGameplayCommand(
            new TotemParticipantId(state.Actor.ParticipantId),
            TotemGameplayCommandSource.BotDecision,
            TotemGameplayCommandType.Fire,
            gameplayCommandSequence++,
            target.Position - rayOrigin,
            target.CombatantId);
        if (!weaponService.TryApplyFirstPlayableFireCommand(
                command,
                GetProfileDamageMultiplier(state),
                out TotemGunAttackResult attackResult))
        {
            return false;
        }

        state.AttackCooldownRemaining = cooldown;
        state.Attacks++;
        totalAttacks++;
        RecordDecision(
            state,
            "Attack",
            "WeaponAttack",
            target,
            FlatDistance(state.Actor.Position, target.Position),
            attackResult.Weapon.WeaponId);
        GFTrace.Info("TotemAI", "Attack", null, GFTrace.Data(
            "actor", state.Actor.Name,
            "target", target.Name,
            "weapon", attackResult.Weapon.WeaponId,
            "damage", attackResult.DirectDamage.EffectiveDamage.ToString("F1")));
        return true;
    }

    private bool ShouldSmartDodge(TotemAIActorState state)
    {
        float reactionSec = state.Profile == null ? tuning.dodgeReactionSec : state.Profile.DodgeReactionSec;
        if (state.DodgeCooldownRemaining > 0f || state.LastDamagedElapsed > 1f || state.LastDamagedElapsed < reactionSec)
        {
            return false;
        }

        int roll = Mathf.Abs((state.Actor.ActorId * 37 + state.Decisions * 17) % 100);
        float confidence = state.Profile == null ? 1f : state.Profile.Confidence;
        float riskTolerance = state.Profile == null ? 0.6f : Mathf.Clamp01(state.Profile.RiskTolerance);
        float riskMul = Mathf.Lerp(1.35f, 0.7f, riskTolerance);
        float threshold = 30f * Mathf.Clamp01(state.SafetyScore) * Mathf.Clamp01(confidence) * riskMul;
        return roll < threshold;
    }

    private TotemActorModel FindClosestTarget(TotemActorModel self, float visionRadius, bool includePeerAi)
    {
        if (self == null)
        {
            return null;
        }

        float maxSqr = visionRadius * visionRadius;
        float bestSqr = float.MaxValue;
        TotemActorModel best = null;
        var actors = actorService.Actors;
        for (int i = 0; i < actors.Count; i++)
        {
            var candidate = actors[i];
            if (candidate == null || candidate == self || !candidate.IsAlive)
            {
                continue;
            }

            if (!actorService.CanOpponentTarget(candidate))
            {
                continue;
            }

            if (!IsLegalParticipantOpponent(self, candidate))
            {
                continue;
            }

            if (!includePeerAi && candidate.Kind != TotemActorKind.Player)
            {
                continue;
            }

            float sqr = FlatSqrDistance(self.Position, candidate.Position);
            if (sqr > maxSqr)
            {
                continue;
            }

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = candidate;
            }
        }

        return best;
    }

    private TotemActorModel FindBestSmartTarget(TotemAIActorState state, float visionRadius)
    {
        var self = state?.Actor;
        if (self == null || actorService == null)
        {
            return null;
        }

        float searchRadius = Mathf.Max(0.1f, visionRadius);
        float maxSqr = searchRadius * searchRadius;
        float bestScore = float.MinValue;
        TotemActorModel best = null;
        var actors = actorService.Actors;
        for (int i = 0; i < actors.Count; i++)
        {
            var candidate = actors[i];
            if (candidate == null || candidate == self || !candidate.IsAlive)
            {
                continue;
            }

            if (!actorService.CanOpponentTarget(candidate))
            {
                continue;
            }

            if (!IsLegalParticipantOpponent(self, candidate))
            {
                continue;
            }

            float sqr = FlatSqrDistance(self.Position, candidate.Position);
            if (sqr > maxSqr)
            {
                continue;
            }

            float score = CalculateSmartTargetScore(state, candidate, Mathf.Sqrt(sqr), searchRadius);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private bool IsLegalParticipantOpponent(TotemActorModel source, TotemActorModel target)
    {
        if (source == null || target == null)
        {
            return false;
        }

        return TotemCombatRelationshipService.Evaluate(
            source,
            target,
            new TotemCombatRelationshipContext(matchClock?.WorldTime ?? elapsedSec)).Allowed;
    }

    private float CalculateSmartTargetScore(TotemAIActorState state, TotemActorModel candidate, float distance, float searchRadius)
    {
        float targetWeight = GetTargetWeight(state, candidate);
        if (targetWeight <= 0f)
        {
            return float.MinValue;
        }

        float normalizedDistance = searchRadius <= 0f ? 1f : Mathf.Clamp01(distance / searchRadius);
        float score = targetWeight * 100f - normalizedDistance * 35f;
        float healthRatio = candidate.MaxHealth <= 0f ? 1f : Mathf.Clamp01(candidate.Health / candidate.MaxHealth);
        score += (1f - healthRatio) * 25f * Mathf.Max(0.25f, state.Profile?.RiskTolerance ?? 0.6f);
        return score;
    }

    private static float GetTargetWeight(TotemAIActorState state, TotemActorModel candidate)
    {
        var profile = state?.Profile;
        if (profile == null || candidate == null)
        {
            return 1f;
        }

        switch (candidate.Kind)
        {
            case TotemActorKind.Player:
                return profile.TargetPlayerWeight;
            case TotemActorKind.SmartAi:
            case TotemActorKind.LightAi:
                return profile.TargetHumanoidAiWeight;
            default:
                return 0f;
        }
    }

    private static string ResolveSmartChaseReason(TotemAIActorState state)
    {
        return state?.Profile?.Personality == TotemAIPersonality.PlayerPriority ? "PlayerPriorityTarget" : "TargetVisible";
    }

    private float CalculateSafety(TotemActorModel actor)
    {
        int hostiles = 0;
        var actors = actorService.Actors;
        for (int i = 0; i < actors.Count; i++)
        {
            var other = actors[i];
            if (other == null || other == actor || !other.IsAlive)
            {
                continue;
            }

            if (FlatSqrDistance(actor.Position, other.Position) <= tuning.lodRadius * tuning.lodRadius)
            {
                hostiles++;
            }
        }

        return Mathf.Clamp01(1f - hostiles * 0.15f);
    }

    private void MoveToward(TotemActorModel actor, TotemActorModel target, float deltaTime, float speed)
    {
        float effectiveSpeed = ResolveMoveSpeed(actor, speed);
        if (effectiveSpeed <= 0f)
        {
            return;
        }

        Vector3 direction = FlatDirection(actor.Position, target.Position);
        ApplyMoveCommand(actor, direction, deltaTime, effectiveSpeed);
    }

    private void MoveTowardPosition(TotemActorModel actor, Vector3 targetPosition, float deltaTime, float speed)
    {
        float effectiveSpeed = ResolveMoveSpeed(actor, speed);
        if (effectiveSpeed <= 0f)
        {
            return;
        }

        Vector3 direction = FlatDirection(actor.Position, targetPosition);
        ApplyMoveCommand(actor, direction, deltaTime, effectiveSpeed);
    }

    private void MoveAwayFrom(TotemActorModel actor, TotemActorModel target, float deltaTime, float speed)
    {
        float effectiveSpeed = ResolveMoveSpeed(actor, speed);
        if (effectiveSpeed <= 0f)
        {
            return;
        }

        Vector3 direction = -FlatDirection(actor.Position, target.Position);
        ApplyMoveCommand(actor, direction, deltaTime, effectiveSpeed);
    }

    private void ApplyMoveCommand(TotemActorModel actor, Vector3 direction, float deltaTime, float moveSpeed)
    {
        if (actor == null)
        {
            return;
        }

        var command = new TotemGameplayCommand(
            new TotemParticipantId(actor.ParticipantId),
            TotemGameplayCommandSource.BotDecision,
            TotemGameplayCommandType.Move,
            gameplayCommandSequence++,
            direction);
        actorService?.TryApplyFirstPlayableMoveCommand(command, deltaTime, moveSpeed, out _);
    }

    private bool IsStatusBlocked(TotemActorModel actor)
    {
        return statusService != null && statusService.IsStunned(actor);
    }

    private bool CanActorAct(TotemActorModel actor)
    {
        return actor != null
            && actor.IsAlive
            && (readinessService == null || readinessService.CanAct(actor))
            && (statusService == null || statusService.CanAct(actor));
    }

    private float ResolveMoveSpeed(TotemActorModel actor, float speed)
    {
        if (actor == null)
        {
            return 0f;
        }

        float multiplier = statusService == null ? 1f : statusService.GetMoveSpeedMultiplier(actor);
        if (elementService != null)
        {
            multiplier *= elementService.GetMoveSpeedMultiplier(actor.CombatantId);
        }
        if (lifecycleService != null)
        {
            multiplier *= lifecycleService.GetMoveSpeedMultiplier(actor);
        }

        return speed * multiplier;
    }

    private void OnDamageApplied(TotemActorModel target, float amount, bool killed)
    {
        for (int i = 0; i < aiStates.Count; i++)
        {
            var state = aiStates[i];
            if (state.Actor != target)
            {
                continue;
            }

            state.LastDamagedElapsed = 0f;
            if (killed)
            {
                state.State = TotemAIState.Dead;
            }
            return;
        }
    }

    private static Vector3 BuildWanderDirection(int actorId, int decision)
    {
        float angle = ((actorId * 47 + decision * 13) % 360) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;
    }

    private static Vector3 FlatDirection(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Vector3.forward;
        }

        return direction.normalized;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        return Mathf.Sqrt(FlatSqrDistance(a, b));
    }

    private static float FlatSqrDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }
}
