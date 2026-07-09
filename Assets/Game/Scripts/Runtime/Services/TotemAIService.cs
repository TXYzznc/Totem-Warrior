using System.Collections.Generic;
using System;
using UnityEngine;

public sealed class TotemAIService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    public const float LodRadius = 20f;
    public const float LodScanInterval = 0.2f;
    public const float SmartColdInterval = 0.5f;
    public const float LightHotInterval = 0.2f;
    public const float LightColdInterval = 2f;
    public const float SmartAttackRange = 4f;
    public const float LightAttackRange = 3f;
    public const float BossAttackRange = 5f;
    public const float DeathChestLootRadius = 2.5f;
    public const float MinChaseLootGreedFactor = 0.45f;
    public const float MinMapResourceChaseWeight = 1f;
    public const float MinSmartShopPreference = 0.5f;

    private readonly List<TotemAIActorState> aiStates = new List<TotemAIActorState>(64);
    private readonly TotemAIActorState bossState = new TotemAIActorState();
    private TotemGameFlowService flowService;
    private TotemActorService actorService;
    private TotemBossService bossService;
    private TotemStatusService statusService;
    private TotemTattooService tattooService;
    private TotemWeaponService weaponService;
    private TotemSkillService skillService;
    private TotemVfxService vfxService;
    private TotemEconomyService economyService;
    private TotemNpcService npcService;
    private TotemAITuningDefinition tuning = TotemAITuningDefinition.Default;
    private TotemBotProfileDefinition[] botProfiles = Array.Empty<TotemBotProfileDefinition>();
    private TotemBotBuildPresetDefinition[] botBuildPresets = Array.Empty<TotemBotBuildPresetDefinition>();
    private bool active;
    private float elapsedSec;
    private float lodScanRemaining;
    private int totalDecisions;
    private int totalAttacks;
    private int totalSkillUses;
    private int decisionSequence;
    private TotemAIDecisionRecord lastDecision;

    public override string ServiceName => "AI";

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        actorService = runtime.GetService<TotemActorService>();
        bossService = runtime.GetService<TotemBossService>();
        statusService = runtime.GetService<TotemStatusService>();
        tattooService = runtime.GetService<TotemTattooService>();
        weaponService = runtime.GetService<TotemWeaponService>();
        skillService = runtime.GetService<TotemSkillService>();
        vfxService = runtime.GetService<TotemVfxService>();
        economyService = runtime.GetService<TotemEconomyService>();
        npcService = runtime.GetService<TotemNpcService>();
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

        bossService = null;
        statusService = null;
        tattooService = null;
        weaponService = null;
        skillService = null;
        vfxService = null;
        economyService = null;
        npcService = null;
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
        TickBoss(deltaTime);
    }

    public IReadOnlyList<TotemAIActorState> States => aiStates;

    public TotemAISnapshot CaptureSnapshot()
    {
        var snapshot = new TotemAISnapshot
        {
            active = active,
            totalDecisions = totalDecisions,
            totalAttacks = totalAttacks,
            totalSkillUses = totalSkillUses,
        };

        for (int i = 0; i < aiStates.Count; i++)
        {
            var state = aiStates[i];
            if (state.Actor == null)
            {
                continue;
            }

            if (state.Actor.Kind == TotemActorKind.SmartAi)
            {
                snapshot.smartCount++;
                if (state.Profile != null)
                {
                    snapshot.smartProfileCount++;
                }
            }
            else if (state.Actor.Kind == TotemActorKind.LightAi)
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

            if (state.SelfTattooAwaitingCompletion || state.SelfTattooReadRemaining > 0f)
            {
                snapshot.smartReadingCount++;
            }

            snapshot.totalPlannedTattooCount += state.PlannedTattooCount;

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

            snapshot.totalDeathChestLoots += state.DeathChestLoots;
            snapshot.totalResourcePickupClaims += state.ResourcePickupClaims;
            snapshot.totalShopPurchases += state.ShopPurchases;
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

        if (state.Profile != null && state.Actor.Kind == TotemActorKind.SmartAi)
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
            if (actor == null || (actor.Kind != TotemActorKind.SmartAi && actor.Kind != TotemActorKind.LightAi))
            {
                continue;
            }

            int profileIndex = actor.Kind == TotemActorKind.SmartAi ? smartIndex++ : lightIndex++;
            var profile = SelectProfile(actor.Kind, profileIndex, profiles);
            var preset = ResolvePreset(profile?.PreferredPreset ?? 0, presets);
            result.Add(new TotemAIActorState
            {
                Actor = actor,
                State = actor.Kind == TotemActorKind.SmartAi ? TotemAIState.Chase : TotemAIState.Wander,
                Bucket = ResolveBucket(actor.Position, playerPosition),
                Profile = profile,
                BuildPreset = preset,
                WanderDirection = BuildWanderDirection(actor.ActorId, 0),
                NextDecisionTime = 0f,
                NextBuildRethinkTime = profile == null ? 20f : Mathf.Max(0.1f, profile.RethinkInterval),
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

    public static bool ShouldStartSelfTattoo(float safetyScore, float boldness)
    {
        float threshold = 1f - Mathf.Clamp01(boldness) * 0.8f;
        return Mathf.Clamp01(safetyScore) >= threshold;
    }

    public static bool TryPlanNextBuild(TotemBotBuildPresetDefinition preset, int plannedPartMask, out TotemBotBuildSlot next)
    {
        next = null;
        if (preset == null)
        {
            return false;
        }

        var recommendedSeq = preset.RecommendedSeq;
        if (recommendedSeq != null)
        {
            for (int i = 0; i < recommendedSeq.Length; i++)
            {
                var slot = recommendedSeq[i];
                if (slot != null && !HasPlannedPart(plannedPartMask, slot.partId))
                {
                    next = slot;
                    return true;
                }
            }
        }

        var preferredParts = preset.PreferredParts;
        if (preferredParts != null && preferredParts.Length > 0)
        {
            int colorId = TendencyArgmaxColor(preset.Tendency);
            for (int i = 0; i < preferredParts.Length; i++)
            {
                int partId = preferredParts[i];
                if (!HasPlannedPart(plannedPartMask, partId))
                {
                    next = new TotemBotBuildSlot { partId = partId, colorId = colorId, patternId = 1 };
                    return true;
                }
            }
        }

        return false;
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

        weaponService?.EquipWeapon(actor, "pistol_basic");
        skillService?.EquipDefaultLoadout(actor);
    }

    private void ClearRuntimeState()
    {
        aiStates.Clear();
        active = false;
        elapsedSec = 0f;
        lodScanRemaining = 0f;
        totalDecisions = 0;
        totalAttacks = 0;
        totalSkillUses = 0;
        decisionSequence = 0;
        lastDecision = null;
        bossState.Actor = null;
        bossState.State = TotemAIState.Idle;
        bossState.AttackCooldownRemaining = 0f;
        bossState.SkillCooldownRemaining = 0f;
        bossState.Decisions = 0;
        bossState.Attacks = 0;
        bossState.SkillUses = 0;
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
        snapshot.lastDecisionDistance = record.Distance;
        snapshot.lastDecisionSafetyScore = record.SafetyScore;
        snapshot.lastDecisionProfileBotId = record.ProfileBotId;
        snapshot.lastDecisionBuildPresetId = record.BuildPresetId;
        snapshot.lastDecisionWeaponId = record.WeaponId;
        snapshot.lastDecisionSkillId = record.SkillId;
        snapshot.lastDecisionPickupInstanceId = record.PickupInstanceId;
        snapshot.lastDecisionPickupWeaponId = record.PickupWeaponId;
        snapshot.lastDecisionPickupSource = record.PickupSource;
        snapshot.lastDecisionNpcId = record.NpcId;
        snapshot.lastDecisionShopItemId = record.ShopItemId;
        snapshot.lastDecisionShopPrice = record.ShopPrice;
        snapshot.lastDecisionShopStockLeft = record.ShopStockLeft;
        snapshot.lastDecisionShopRewardType = record.ShopRewardType;
        snapshot.lastDecisionShopRewardSummary = record.ShopRewardSummary;
        snapshot.lastDecisionPersonality = record.Personality;
    }

    private void RecordDecision(
        TotemAIActorState state,
        string action,
        string reason,
        TotemActorModel target = null,
        float distance = -1f,
        string weaponId = null,
        string skillId = null,
        TotemWeaponPickupModel pickup = null,
        TotemNpcModel npc = null,
        TotemShopOffer shopOffer = null,
        TotemShopPurchaseResult shopPurchase = null)
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
        record.TargetActorId = target?.ActorId ?? 0;
        record.TargetName = target?.Name ?? string.Empty;
        record.TargetKind = target?.Kind ?? TotemActorKind.Player;
        record.Distance = distance;
        record.ActorHealth = state.Actor.Health;
        record.TargetHealth = target?.Health ?? 0f;
        record.SafetyScore = state.SafetyScore;
        record.ProfileBotId = state.Profile?.BotId ?? 0;
        record.BuildPresetId = state.BuildPreset?.PresetId ?? 0;
        record.WeaponId = weaponId ?? string.Empty;
        record.SkillId = skillId ?? string.Empty;
        record.PickupInstanceId = pickup?.InstanceId ?? 0;
        record.PickupWeaponId = pickup?.WeaponId ?? string.Empty;
        record.PickupSource = pickup?.Source ?? string.Empty;
        record.NpcId = npc?.NpcId ?? string.Empty;
        record.ShopItemId = shopPurchase?.itemId ?? shopOffer?.ItemId ?? 0;
        record.ShopPrice = shopPurchase?.actualPrice ?? (shopOffer == null || npc == null ? 0 : Mathf.RoundToInt(shopOffer.Price * npc.ThemePriceMultiplier));
        record.ShopStockLeft = shopPurchase?.stockLeft ?? shopOffer?.Stock ?? 0;
        record.ShopRewardType = shopPurchase?.rewardType ?? (shopOffer == null ? TotemShopRewardType.Unknown : TotemNpcService.InferRewardType(shopOffer));
        record.ShopRewardSummary = shopPurchase?.rewardSummary ?? string.Empty;
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
            if (state.Actor.Kind == TotemActorKind.SmartAi)
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
            state.SkillCooldownRemaining = Mathf.Max(0f, state.SkillCooldownRemaining - deltaTime);
            state.DodgeCooldownRemaining = Mathf.Max(0f, state.DodgeCooldownRemaining - deltaTime);
            if (IsStatusBlocked(state.Actor))
            {
                state.State = TotemAIState.Idle;
                RecordDecision(state, "Idle", "Status:Stun");
                continue;
            }

            bool tattooStillInService = tattooService != null && tattooService.IsSelfTattooInProgress(state.Actor);
            if (state.SelfTattooAwaitingCompletion || state.SelfTattooReadRemaining > 0f || tattooStillInService)
            {
                if (state.SelfTattooReadRemaining > 0f)
                {
                    state.SelfTattooReadRemaining = Mathf.Max(0f, state.SelfTattooReadRemaining - deltaTime);
                }

                state.State = TotemAIState.Idle;
                tattooStillInService = tattooService != null && tattooService.IsSelfTattooInProgress(state.Actor);
                if (state.SelfTattooAwaitingCompletion && state.SelfTattooReadRemaining <= 0f && !tattooStillInService)
                {
                    state.PlannedTattooCount++;
                    state.SelfTattooAwaitingCompletion = false;
                    GFTrace.Success("TotemAI", "Smart.SelfTattooPlanFinished", null, GFTrace.Data(
                        "actor", state.Actor.Name,
                        "profile", state.Profile?.DisplayName ?? string.Empty,
                        "tattoo", state.LastPlannedTattoo ?? string.Empty));
                }

                continue;
            }

            float interval = GetRuntimeDecisionInterval(state);
            if (elapsedSec < state.NextDecisionTime)
            {
                continue;
            }

            state.NextDecisionTime = elapsedSec + interval;
            if (state.Actor.Kind == TotemActorKind.SmartAi)
            {
                TickSmart(state, deltaTime);
            }
            else
            {
                TickLight(state, deltaTime);
            }
        }
    }

    private void TickSmart(TotemAIActorState state, float deltaTime)
    {
        state.Decisions++;
        totalDecisions++;
        state.ResourcePickupTarget = null;
        state.ShopTargetNpc = null;

        if (TryStartSmartSelfTattooPlan(state))
        {
            state.State = TotemAIState.Idle;
            RecordDecision(state, "SelfTattoo", "BuildPlan");
            return;
        }

        float smartVisionRadius = GetProfileVisionRadius(state, GetActorDetectRange(state.Actor, tuning.smartVisionRadius));
        bool bossOverridesResources = IsBossTargetOverrideActive(state);
        if (!bossOverridesResources && TryPursueDeathChest(state, deltaTime, smartVisionRadius))
        {
            return;
        }

        if (!bossOverridesResources && TryPursueMapResourcePickup(state, deltaTime, smartVisionRadius))
        {
            return;
        }

        if (!bossOverridesResources && TryPursueShopPurchase(state, deltaTime, smartVisionRadius))
        {
            return;
        }

        var target = FindBestSmartTarget(state, smartVisionRadius, bossOverridesResources);
        if (target == null)
        {
            state.State = TotemAIState.Idle;
            RecordDecision(state, "Idle", bossOverridesResources ? "BossActiveNoReachableTarget" : "NoTarget");
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
        bool targetReading = IsReadingTarget(target);
        float attackRange = GetProfileAttackRange(state, GetActorAttackRange(state.Actor, tuning.smartAttackRange));
        if (targetReading)
        {
            attackRange = Mathf.Max(attackRange, GetProfileAggroRadius(state, attackRange));
        }

        if (distance <= attackRange)
        {
            state.State = TotemAIState.Attack;
            TryAiAttack(state, target, GetProfileDamage(state, GetActorDamage(state.Actor, tuning.smartDamage)), GetProfileAttackCooldown(state, tuning.smartAttackCooldown));
            TrySmartSkill(state, target);
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
        RecordDecision(state, "Chase", ResolveSmartChaseReason(state, target, targetReading), target, distance);
        if (distance <= tuning.smartSkillRadius)
        {
            TrySmartSkill(state, target);
        }
    }

    private void TickLight(TotemAIActorState state, float deltaTime)
    {
        state.Decisions++;
        totalDecisions++;

        float lightVisionRadius = GetProfileVisionRadius(state, GetActorDetectRange(state.Actor, tuning.lightVisionRadius));
        var target = FindClosestTarget(state.Actor, lightVisionRadius, includePeerAi: true, preferReadingTarget: false);
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

        if (TryPursueDeathChest(state, deltaTime, lightVisionRadius))
        {
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
            actorService.MoveActor(state.Actor, state.WanderDirection * (wanderSpeed * deltaTime));
        }
    }

    private bool TryPursueDeathChest(TotemAIActorState state, float deltaTime, float visionRadius)
    {
        state.LootTargetActor = null;
        if (economyService == null || actorService == null || state?.Actor == null)
        {
            return false;
        }

        float searchRadius = GetDeathChestSearchRadius(state, visionRadius);
        if (searchRadius <= 0f)
        {
            return false;
        }

        var target = FindBestDeathChest(state.Actor, searchRadius);
        if (target == null)
        {
            return false;
        }

        state.LootTargetActor = target;
        state.State = TotemAIState.Loot;
        float distance = FlatDistance(state.Actor.Position, target.Position);
        if (distance <= DeathChestLootRadius)
        {
            if (!economyService.TryLootDeathChest(state.Actor, target, out var snapshot))
            {
                state.LootTargetActor = null;
                return false;
            }

            state.DeathChestLoots++;
            RecordDecision(state, "Loot", "ClaimDeathChest", target, distance);
            GFTrace.Success("TotemAI", "DeathChest.Looted", null, GFTrace.Data(
                "actor", state.Actor.Name,
                "profile", state.Profile?.DisplayName ?? string.Empty,
                "deadActor", target.Name,
                "coins", snapshot.coins.ToString(),
                "ink", snapshot.inkBottleCount.ToString(),
                "recipes", snapshot.recipeCopyCount.ToString(),
                "equipment", snapshot.equipmentCount.ToString()));
            return true;
        }

        float speedFallback = state.Actor.Kind == TotemActorKind.LightAi ? tuning.lightMoveSpeed : tuning.smartMoveSpeed;
        MoveToward(state.Actor, target, deltaTime, GetProfileMoveSpeed(state, speedFallback) * 1.1f);
        RecordDecision(state, "Loot", "ChaseDeathChest", target, distance);
        return true;
    }

    public static float GetDeathChestSearchRadius(TotemAIActorState state, float visionRadius)
    {
        float greed = GetProfileLootGreedFactor(state);
        if (greed <= 0f)
        {
            return 0f;
        }

        if (greed < MinChaseLootGreedFactor)
        {
            return DeathChestLootRadius;
        }

        float clampedVision = Mathf.Max(DeathChestLootRadius, visionRadius);
        return Mathf.Min(clampedVision, DeathChestLootRadius + Mathf.Clamp(greed, 0f, 2f) * 12f);
    }

    private static float GetProfileLootGreedFactor(TotemAIActorState state)
    {
        if (state?.Profile != null)
        {
            return Mathf.Max(0f, state.Profile.LootGreedFactor);
        }

        if (state?.Actor == null)
        {
            return 0f;
        }

        return state.Actor.Kind == TotemActorKind.SmartAi ? 0.8f : 0.25f;
    }

    private bool TryPursueMapResourcePickup(TotemAIActorState state, float deltaTime, float visionRadius)
    {
        state.ResourcePickupTarget = null;
        if (weaponService == null || state?.Actor == null || !ShouldPursueMapResourcePickup(state))
        {
            return false;
        }

        float searchRadius = GetMapResourcePickupSearchRadius(state, visionRadius);
        if (searchRadius <= 0f)
        {
            return false;
        }

        var pickup = FindBestMapResourcePickup(state, searchRadius);
        if (pickup == null)
        {
            return false;
        }

        state.ResourcePickupTarget = pickup;
        state.State = TotemAIState.Loot;
        float distance = FlatDistance(state.Actor.Position, pickup.Position);
        if (distance <= TotemWeaponService.PickupInteractRadius)
        {
            if (!weaponService.TryPickupWeapon(state.Actor, pickup, out var result))
            {
                RecordDecision(state, "Loot", $"MapResourcePickupRejected:{result?.reason ?? "Unknown"}", null, distance, null, null, pickup);
                return true;
            }

            state.ResourcePickupClaims++;
            RecordDecision(state, "Loot", "ClaimMapResourcePickup", null, distance, result.weaponId, null, pickup);
            GFTrace.Success("TotemAI", "MapResource.Picked", null, GFTrace.Data(
                "actor", state.Actor.Name,
                "profile", state.Profile?.DisplayName ?? string.Empty,
                "pickup", pickup.InstanceId.ToString(),
                "weaponId", result.weaponId,
                "level", result.weaponLevel.ToString(),
                "reason", result.reason ?? string.Empty));
            return true;
        }

        MoveTowardPosition(state.Actor, pickup.Position, deltaTime, GetProfileMoveSpeed(state, GetActorMoveSpeed(state.Actor, tuning.smartMoveSpeed)) * 1.05f);
        RecordDecision(state, "Loot", "ChaseMapResourcePickup", null, distance, null, null, pickup);
        return true;
    }

    private static bool ShouldPursueMapResourcePickup(TotemAIActorState state)
    {
        return state?.Actor != null &&
               state.Actor.Kind == TotemActorKind.SmartAi &&
               GetProfileResourceWeight(state) >= MinMapResourceChaseWeight;
    }

    private static float GetMapResourcePickupSearchRadius(TotemAIActorState state, float visionRadius)
    {
        float weight = GetProfileResourceWeight(state);
        if (weight <= 0f)
        {
            return 0f;
        }

        float clampedVision = Mathf.Max(TotemWeaponService.PickupInteractRadius, visionRadius);
        float weightedRadius = TotemWeaponService.PickupInteractRadius + Mathf.Clamp(weight, 0f, 3f) * 12f;
        return Mathf.Min(clampedVision, weightedRadius);
    }

    private static float GetProfileResourceWeight(TotemAIActorState state)
    {
        if (state?.Profile != null)
        {
            return Mathf.Max(0f, state.Profile.TargetResourceWeight);
        }

        return state?.Actor != null && state.Actor.Kind == TotemActorKind.SmartAi ? 0.4f : 0f;
    }

    private bool TryPursueShopPurchase(TotemAIActorState state, float deltaTime, float visionRadius)
    {
        state.ShopTargetNpc = null;
        if (npcService == null || economyService == null || state?.Actor == null || !ShouldPursueShopPurchase(state))
        {
            return false;
        }

        float searchRadius = GetShopSearchRadius(state, visionRadius);
        if (searchRadius <= 0f)
        {
            return false;
        }

        if (!TryFindBestShopPurchase(state, searchRadius, out var merchant, out var offer))
        {
            return false;
        }

        state.ShopTargetNpc = merchant;
        state.State = TotemAIState.Loot;
        float distance = FlatDistance(state.Actor.Position, merchant.Position);
        if (distance <= Mathf.Max(0.1f, merchant.InteractRadius))
        {
            if (!npcService.TryPurchase(state.Actor, merchant, offer.ItemId, out var result))
            {
                RecordDecision(state, "Shop", $"ShopPurchaseRejected:{result?.reason ?? "Unknown"}", null, distance, null, null, null, merchant, offer, result);
                return true;
            }

            state.ShopPurchases++;
            RecordDecision(state, "Shop", "PurchaseShopOffer", null, distance, null, null, null, merchant, offer, result);
            GFTrace.Success("TotemAI", "Shop.Purchased", null, GFTrace.Data(
                "actor", state.Actor.Name,
                "profile", state.Profile?.DisplayName ?? string.Empty,
                "npcId", merchant.NpcId,
                "itemId", result.itemId.ToString(),
                "price", result.actualPrice.ToString(),
                "stockLeft", result.stockLeft.ToString(),
                "reward", result.rewardSummary ?? string.Empty));
            return true;
        }

        MoveTowardPosition(state.Actor, merchant.Position, deltaTime, GetProfileMoveSpeed(state, GetActorMoveSpeed(state.Actor, tuning.smartMoveSpeed)));
        RecordDecision(state, "Shop", "ChaseMerchant", null, distance, null, null, null, merchant, offer);
        return true;
    }

    private static bool ShouldPursueShopPurchase(TotemAIActorState state)
    {
        return state?.Actor != null &&
               state.Actor.Kind == TotemActorKind.SmartAi &&
               GetProfileShopPreference(state) >= MinSmartShopPreference;
    }

    private static float GetShopSearchRadius(TotemAIActorState state, float visionRadius)
    {
        float preference = GetProfileShopPreference(state);
        if (preference <= 0f)
        {
            return 0f;
        }

        float clampedVision = Mathf.Max(1f, visionRadius);
        float weightedRadius = 2.5f + Mathf.Clamp(preference, 0f, 2f) * 14f;
        return Mathf.Min(clampedVision, weightedRadius);
    }

    private static float GetProfileShopPreference(TotemAIActorState state)
    {
        if (state?.Profile != null)
        {
            return Mathf.Max(0f, state.Profile.ShopPreference);
        }

        return 0f;
    }

    private bool TryFindBestShopPurchase(TotemAIActorState state, float searchRadius, out TotemNpcModel bestMerchant, out TotemShopOffer bestOffer)
    {
        bestMerchant = null;
        bestOffer = null;
        var self = state?.Actor;
        if (self == null || npcService == null || economyService == null || searchRadius <= 0f)
        {
            return false;
        }

        int coins = economyService.CaptureInventory(self).coins;
        if (coins <= 0)
        {
            return false;
        }

        float maxSqr = searchRadius * searchRadius;
        float bestScore = float.MinValue;
        var npcs = npcService.Npcs;
        for (int npcIndex = 0; npcIndex < npcs.Count; npcIndex++)
        {
            var merchant = npcs[npcIndex];
            if (merchant == null || merchant.Type != TotemNpcType.Merchant)
            {
                continue;
            }

            float sqr = FlatSqrDistance(self.Position, merchant.Position);
            if (sqr > maxSqr)
            {
                continue;
            }

            var offers = merchant.Offers ?? Array.Empty<TotemShopOffer>();
            for (int offerIndex = 0; offerIndex < offers.Length; offerIndex++)
            {
                var offer = offers[offerIndex];
                if (offer == null || offer.Stock <= 0)
                {
                    continue;
                }

                int price = Mathf.RoundToInt(offer.Price * merchant.ThemePriceMultiplier);
                if (price > coins || TotemNpcService.InferRewardType(offer) == TotemShopRewardType.Unknown)
                {
                    continue;
                }

                float distance = Mathf.Sqrt(sqr);
                float score = CalculateShopOfferScore(state, offer, distance, searchRadius, price);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMerchant = merchant;
                    bestOffer = offer;
                }
            }
        }

        return bestMerchant != null && bestOffer != null;
    }

    private static float CalculateShopOfferScore(TotemAIActorState state, TotemShopOffer offer, float distance, float searchRadius, int price)
    {
        float normalizedDistance = searchRadius <= 0f ? 1f : Mathf.Clamp01(distance / searchRadius);
        float score = GetProfileShopPreference(state) * 100f - normalizedDistance * 35f;
        score += Mathf.Max(0, offer.Weight);
        score -= Mathf.Max(0, price) * 0.02f;

        switch (TotemNpcService.InferRewardType(offer))
        {
            case TotemShopRewardType.WeaponUpgrade:
                score += 35f;
                break;
            case TotemShopRewardType.SkillCore:
                score += 30f;
                break;
            case TotemShopRewardType.StatusCleanse:
                score += 20f;
                break;
            case TotemShopRewardType.Ink:
                score += 15f;
                break;
        }

        if (state?.Profile != null && state.Profile.Personality == TotemAIPersonality.ResourceAcquisition)
        {
            score += 25f;
        }

        return score;
    }

    private TotemWeaponPickupModel FindBestMapResourcePickup(TotemAIActorState state, float searchRadius)
    {
        var self = state?.Actor;
        if (self == null || weaponService == null || searchRadius <= 0f)
        {
            return null;
        }

        float maxSqr = searchRadius * searchRadius;
        float bestScore = float.MinValue;
        TotemWeaponPickupModel best = null;
        string equippedWeaponId = weaponService.GetEquippedWeaponId(self);
        float resourceWeight = GetProfileResourceWeight(state);
        var pickups = weaponService.ActivePickups;
        for (int i = 0; i < pickups.Count; i++)
        {
            var pickup = pickups[i];
            if (pickup == null || !string.Equals(pickup.Source, "MapResource", StringComparison.Ordinal))
            {
                continue;
            }

            float sqr = FlatSqrDistance(self.Position, pickup.Position);
            if (sqr > maxSqr)
            {
                continue;
            }

            float distance = Mathf.Sqrt(sqr);
            float normalizedDistance = searchRadius <= 0f ? 1f : Mathf.Clamp01(distance / searchRadius);
            float score = resourceWeight * 100f - normalizedDistance * 45f;
            score += string.Equals(equippedWeaponId, pickup.WeaponId, StringComparison.Ordinal) ? 10f : 25f;
            if (state.Profile != null && state.Profile.Personality == TotemAIPersonality.ResourceAcquisition)
            {
                score += 30f;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = pickup;
            }
        }

        return best;
    }

    private TotemActorModel FindBestDeathChest(TotemActorModel seeker, float searchRadius)
    {
        if (seeker == null || searchRadius <= 0f)
        {
            return null;
        }

        float maxSqr = searchRadius * searchRadius;
        float bestScore = float.MinValue;
        TotemActorModel best = null;
        var actors = actorService.Actors;
        for (int i = 0; i < actors.Count; i++)
        {
            var candidate = actors[i];
            if (candidate == null || candidate == seeker || candidate.IsAlive)
            {
                continue;
            }

            if (candidate.Kind == TotemActorKind.Player || candidate.Kind == TotemActorKind.Boss)
            {
                continue;
            }

            if (!economyService.HasPendingDeathChest(candidate))
            {
                continue;
            }

            float sqr = FlatSqrDistance(seeker.Position, candidate.Position);
            if (sqr > maxSqr)
            {
                continue;
            }

            int value = economyService.GetPendingDeathChestValue(candidate);
            if (value <= 0)
            {
                continue;
            }

            float score = value - sqr * 0.1f;
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private bool TryStartSmartSelfTattooPlan(TotemAIActorState state)
    {
        if (tattooService == null || state.Profile == null || state.BuildPreset == null || elapsedSec < state.NextBuildRethinkTime)
        {
            return false;
        }

        state.NextBuildRethinkTime = elapsedSec + Mathf.Max(0.1f, state.Profile.RethinkInterval);
        if (!ShouldStartSelfTattoo(state.SafetyScore, state.Profile.SelfTattooBoldness))
        {
            return false;
        }

        if (!TryPlanNextBuild(state.BuildPreset, state.PlannedTattooPartMask, out var next))
        {
            return false;
        }

        if (!tattooService.StartSelfTattoo(state.Actor, next.partId, next.colorId, next.patternId))
        {
            return false;
        }

        state.PlannedTattooPartMask |= 1 << next.partId;
        state.LastPlannedTattoo = next.Format();
        state.SelfTattooReadRemaining = TotemTattooService.GetSelfTattooDuration(next.partId);
        state.SelfTattooAwaitingCompletion = true;
        GFTrace.Success("TotemAI", "Smart.SelfTattooPlanStarted", null, GFTrace.Data(
            "actor", state.Actor.Name,
            "profile", state.Profile.DisplayName ?? string.Empty,
            "preset", state.BuildPreset.Name ?? string.Empty,
            "tattoo", state.LastPlannedTattoo,
            "duration", state.SelfTattooReadRemaining.ToString("F1")));
        return true;
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
        return actor == null || actor.DetectRange <= 0f ? fallback : actor.DetectRange;
    }

    private static float GetActorAttackRange(TotemActorModel actor, float fallback)
    {
        return actor == null || actor.AttackRange <= 0f ? fallback : actor.AttackRange;
    }

    private static float GetActorMoveSpeed(TotemActorModel actor, float fallback)
    {
        return actor == null || actor.MoveSpeed <= 0f ? fallback : actor.MoveSpeed;
    }

    private static float GetActorDamage(TotemActorModel actor, float fallback)
    {
        return actor == null || actor.BaseDamage <= 0f ? fallback : actor.BaseDamage;
    }

    private static float GetProfileDamageMultiplier(TotemAIActorState state)
    {
        if (state?.Profile == null)
        {
            return 1f;
        }

        return Mathf.Lerp(0.75f, 1.15f, Mathf.Clamp01(state.Profile.Confidence));
    }

    private void TickBoss(float deltaTime)
    {
        var boss = actorService?.Boss;
        var player = actorService?.Player;
        if (boss == null || player == null || !boss.IsAlive || !player.IsAlive)
        {
            return;
        }

        bossState.AttackCooldownRemaining = Mathf.Max(0f, bossState.AttackCooldownRemaining - deltaTime);
        bossState.SkillCooldownRemaining = Mathf.Max(0f, bossState.SkillCooldownRemaining - deltaTime);
        if (IsStatusBlocked(boss))
        {
            RecordDecision(GetBossPseudoState(), "Idle", "Status:Stun", player, FlatDistance(boss.Position, player.Position));
            return;
        }

        if (bossService != null && bossService.CanUseSkill(out string skillId))
        {
            float bossDamage = GetActorDamage(boss, tuning.bossDamage);
            float bossAttackRange = GetActorAttackRange(boss, tuning.bossAttackRange);
            float damage = bossDamage * bossService.EnrageMultiplier;
            if (skillService != null)
            {
                if (!TryCastBossSkill(boss, skillId, out var skill))
                {
                    return;
                }

                var bossWeapon = weaponService?.GetOrCreateState(boss)?.Weapon;
                damage = TotemSkillService.ResolveSkillDamage(skill, bossWeapon, bossDamage) * bossService.EnrageMultiplier;
                vfxService?.SpawnSkillBurst(player.Position, skill.SkillId, skill.Radius > 0f ? skill.Radius : bossAttackRange);
            }

            actorService.NotifyActorAttack(boss, string.IsNullOrWhiteSpace(skillId) ? "BossSkill" : $"BossSkill:{skillId}");
            actorService.ApplyDamage(player, damage, boss, string.IsNullOrWhiteSpace(skillId) ? "BossSkill" : $"BossSkill:{skillId}");
            totalSkillUses++;
            RecordDecision(GetBossPseudoState(), "Skill", "BossPhase", player, FlatDistance(boss.Position, player.Position), null, skillId);
            GFTrace.Info("TotemAI", "Boss.Skill", null, GFTrace.Data(
                "skillId", skillId,
                "phase", bossService.CurrentPhase.ToString(),
                "damage", damage.ToString("F1")));
            return;
        }

        float distance = FlatDistance(boss.Position, player.Position);
        if (distance <= GetActorAttackRange(boss, tuning.bossAttackRange))
        {
            var bossState = GetBossPseudoState();
            TryAiAttack(bossState, player, GetActorDamage(boss, tuning.bossDamage) * (bossService?.EnrageMultiplier ?? 1f), tuning.bossAttackCooldown);
            return;
        }

        MoveToward(boss, player, deltaTime, GetActorMoveSpeed(boss, tuning.bossMoveSpeed));
        RecordDecision(GetBossPseudoState(), "Chase", "BossTargetOutOfRange", player, distance);
    }

    private bool TryCastBossSkill(TotemActorModel boss, string skillId, out TotemSkillDefinition skill)
    {
        skill = null;
        if (skillService == null || boss == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(skillId) || !skillService.TryGetRuntimeDefinition(skillId, out _))
        {
            GFTrace.Warning("TotemAI", "Boss.SkillMissing", null, GFTrace.Data("skillId", skillId ?? string.Empty));
            return false;
        }

        skillService.EquipSkill(boss, 0, skillId);
        if (skillService.TryCastSlot(boss, 0, out skill))
        {
            return true;
        }

        GFTrace.Warning("TotemAI", "Boss.SkillCastRejected", null, GFTrace.Data("skillId", skillId));
        return false;
    }

    private TotemAIActorState GetBossPseudoState()
    {
        bossState.Actor = actorService.Boss;
        bossState.State = TotemAIState.Chase;
        bossState.Bucket = TotemAILodBucket.Hot;
        return bossState;
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

        var fireResult = weaponService?.FireWeapon(state.Actor, target, false, 0f);
        if (fireResult != null && !fireResult.Fired)
        {
            return false;
        }

        float baseDamage = fireResult == null ? damage : fireResult.Damage * GetProfileDamageMultiplier(state);
        float finalDamage = tattooService == null
            ? baseDamage
            : tattooService.ResolveAttackDamage(state.Actor, target, baseDamage, out _);
        actorService.NotifyActorAttack(state.Actor, state.Actor.Kind == TotemActorKind.Boss ? "BossAttack" : "AIAttack");
        bool killed = actorService.ApplyDamage(target, finalDamage, state.Actor, state.Actor.Kind == TotemActorKind.Boss ? "BossAttack" : "AIAttack");
        weaponService?.ApplyTraitEffect(fireResult, state.Actor, target, killed);
        vfxService?.SpawnProjectileTrail(state.Actor.Position, target.Position, fireResult?.Projectile, false, false);
        vfxService?.SpawnAttackHit(target.Position, fireResult?.Weapon?.WeaponId, false);
        var tattooResults = tattooService?.Trigger("AttackHitEvent", state.Actor, target, baseDamage);
        state.AttackCooldownRemaining = cooldown;
        state.Attacks++;
        totalAttacks++;
        RecordDecision(
            state,
            "Attack",
            state.Actor.Kind == TotemActorKind.Boss ? "BossAttack" : "WeaponAttack",
            target,
            FlatDistance(state.Actor.Position, target.Position),
            fireResult?.Weapon?.WeaponId);
        GFTrace.Info("TotemAI", "Attack", null, GFTrace.Data(
            "actor", state.Actor.Name,
            "target", target.Name,
            "weapon", fireResult?.Weapon?.WeaponId ?? string.Empty,
            "damage", finalDamage.ToString("F1"),
            "tattooTriggers", (tattooResults?.Length ?? 0).ToString()));
        return true;
    }

    private bool TrySmartSkill(TotemAIActorState state, TotemActorModel target)
    {
        if (state.SkillCooldownRemaining > 0f || target == null || !target.IsAlive)
        {
            return false;
        }

        if (!CanActorAct(state.Actor))
        {
            RecordDecision(state, "Idle", "Status:Stun", target, FlatDistance(state.Actor.Position, target.Position));
            return false;
        }

        if (!ShouldUseSmartSkill(state))
        {
            return false;
        }

        TotemSkillDefinition skill = null;
        if (skillService != null && !skillService.TryCastSlot(state.Actor, 0, out skill))
        {
            return false;
        }

        var weapon = weaponService?.GetOrCreateState(state.Actor)?.Weapon;
        float damage = skill == null
            ? GetProfileDamage(state, GetActorDamage(state.Actor, tuning.smartDamage)) * 1.6f
            : TotemSkillService.ResolveSkillDamage(skill, weapon, GetActorDamage(state.Actor, tuning.smartDamage)) * GetProfileDamageMultiplier(state);
        actorService.NotifyActorAttack(state.Actor, skill == null ? "AISkill" : $"AISkill:{skill.SkillId}");
        actorService.ApplyDamage(target, damage, state.Actor, skill == null ? "AISkill" : $"AISkill:{skill.SkillId}");
        vfxService?.SpawnSkillBurst(target.Position, skill?.SkillId, skill == null || skill.Radius <= 0f ? tuning.smartSkillRadius : skill.Radius);
        var tattooResults = tattooService?.Trigger("SkillCastEvent", state.Actor, target, damage);
        state.SkillCooldownRemaining = tuning.smartSkillCooldown;
        state.SkillUses++;
        totalSkillUses++;
        RecordDecision(state, "Skill", "SmartSkill", target, FlatDistance(state.Actor.Position, target.Position), null, skill?.SkillId);
        GFTrace.Info("TotemAI", "Smart.Skill", null, GFTrace.Data(
            "actor", state.Actor.Name,
            "target", target.Name,
            "skillId", skill?.SkillId ?? string.Empty,
            "damage", damage.ToString("F1"),
            "tattooTriggers", (tattooResults?.Length ?? 0).ToString()));
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

    private static bool ShouldUseSmartSkill(TotemAIActorState state)
    {
        int roll = Mathf.Abs((state.Actor.ActorId * 53 + state.Decisions * 11) % 100);
        float confidence = state.Profile == null ? 1f : state.Profile.Confidence;
        float macroMul = state.BuildPreset != null && state.BuildPreset.BehaviorMacro == TotemAIBehaviorMacro.Pivot ? 1.35f : 1f;
        float threshold = 20f * Mathf.Clamp01(state.SafetyScore) * Mathf.Clamp01(confidence) * macroMul;
        return roll < threshold;
    }

    private TotemActorModel FindClosestTarget(TotemActorModel self, float visionRadius, bool includePeerAi, bool preferReadingTarget)
    {
        if (self == null)
        {
            return null;
        }

        float maxSqr = visionRadius * visionRadius;
        float bestSqr = float.MaxValue;
        TotemActorModel best = null;
        bool bestReading = false;
        var actors = actorService.Actors;
        for (int i = 0; i < actors.Count; i++)
        {
            var candidate = actors[i];
            if (candidate == null || candidate == self || !candidate.IsAlive)
            {
                continue;
            }

            if (candidate.Kind == TotemActorKind.Boss)
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

            bool candidateReading = preferReadingTarget && IsReadingTarget(candidate);
            if (preferReadingTarget && candidateReading != bestReading)
            {
                if (!candidateReading)
                {
                    continue;
                }

                bestReading = true;
                bestSqr = sqr;
                best = candidate;
                continue;
            }

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = candidate;
                bestReading = candidateReading;
            }
        }

        return best;
    }

    private TotemActorModel FindBestSmartTarget(TotemAIActorState state, float visionRadius, bool bossOverridesResources)
    {
        var self = state?.Actor;
        if (self == null || actorService == null)
        {
            return null;
        }

        float searchRadius = ResolveSmartTargetSearchRadius(state, visionRadius, bossOverridesResources);
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

            if (candidate.Kind == TotemActorKind.Boss && !IsBossTargetAllowed(state, candidate))
            {
                continue;
            }

            float sqr = FlatSqrDistance(self.Position, candidate.Position);
            if (sqr > maxSqr)
            {
                continue;
            }

            float score = CalculateSmartTargetScore(state, candidate, Mathf.Sqrt(sqr), searchRadius, bossOverridesResources);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private float ResolveSmartTargetSearchRadius(TotemAIActorState state, float visionRadius, bool bossOverridesResources)
    {
        float result = Mathf.Max(0.1f, visionRadius);
        if (bossOverridesResources)
        {
            result = Mathf.Max(result, tuning.lodRadius * 3f);
        }

        return result;
    }

    private float CalculateSmartTargetScore(TotemAIActorState state, TotemActorModel candidate, float distance, float searchRadius, bool bossOverridesResources)
    {
        float targetWeight = GetTargetWeight(state, candidate);
        if (targetWeight <= 0f)
        {
            return float.MinValue;
        }

        float normalizedDistance = searchRadius <= 0f ? 1f : Mathf.Clamp01(distance / searchRadius);
        float score = targetWeight * 100f - normalizedDistance * 35f;
        if (candidate.Kind == TotemActorKind.Boss && bossOverridesResources)
        {
            score += 150f;
        }

        if (IsReadingTarget(candidate))
        {
            score += Mathf.Max(0f, state.Profile?.ReadingTargetWeight ?? 0.8f) * 45f;
        }

        float healthRatio = candidate.MaxHealth <= 0f ? 1f : Mathf.Clamp01(candidate.Health / candidate.MaxHealth);
        score += (1f - healthRatio) * 25f * Mathf.Max(0.25f, state.Profile?.RiskTolerance ?? 0.6f);
        return score;
    }

    private static float GetTargetWeight(TotemAIActorState state, TotemActorModel candidate)
    {
        var profile = state?.Profile;
        if (profile == null || candidate == null)
        {
            return candidate?.Kind == TotemActorKind.Boss ? 0f : 1f;
        }

        switch (candidate.Kind)
        {
            case TotemActorKind.Player:
                return profile.TargetPlayerWeight;
            case TotemActorKind.SmartAi:
            case TotemActorKind.LightAi:
                return profile.TargetHumanoidAiWeight;
            case TotemActorKind.Boss:
                return profile.TargetBossWeight;
            default:
                return 0f;
        }
    }

    private bool IsBossTargetAllowed(TotemAIActorState state, TotemActorModel boss)
    {
        if (boss == null || !boss.IsAlive)
        {
            return false;
        }

        return bossService != null && bossService.IsActive && GetTargetWeight(state, boss) > 0f;
    }

    private bool IsBossTargetOverrideActive(TotemAIActorState state)
    {
        var boss = actorService?.Boss;
        return state?.Profile != null &&
               state.Profile.Personality == TotemAIPersonality.BossPriority &&
               bossService != null &&
               bossService.IsActive &&
               boss != null &&
               boss.IsAlive;
    }

    private static string ResolveSmartChaseReason(TotemAIActorState state, TotemActorModel target, bool targetReading)
    {
        if (target?.Kind == TotemActorKind.Boss && state?.Profile?.Personality == TotemAIPersonality.BossPriority)
        {
            return "BossPriority";
        }

        if (targetReading)
        {
            return "ReadingTargetOutOfRange";
        }

        return state?.Profile?.Personality == TotemAIPersonality.PlayerPriority ? "PlayerPriorityTarget" : "TargetVisible";
    }

    private bool IsReadingTarget(TotemActorModel actor)
    {
        return tattooService != null && actor != null && tattooService.IsSelfTattooInProgress(actor);
    }

    private float CalculateSafety(TotemActorModel actor)
    {
        int hostiles = 0;
        var actors = actorService.Actors;
        for (int i = 0; i < actors.Count; i++)
        {
            var other = actors[i];
            if (other == null || other == actor || !other.IsAlive || other.Kind == TotemActorKind.Boss)
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
        actorService.MoveActor(actor, direction * (effectiveSpeed * deltaTime));
    }

    private void MoveTowardPosition(TotemActorModel actor, Vector3 targetPosition, float deltaTime, float speed)
    {
        float effectiveSpeed = ResolveMoveSpeed(actor, speed);
        if (effectiveSpeed <= 0f)
        {
            return;
        }

        Vector3 direction = FlatDirection(actor.Position, targetPosition);
        actorService.MoveActor(actor, direction * (effectiveSpeed * deltaTime));
    }

    private void MoveAwayFrom(TotemActorModel actor, TotemActorModel target, float deltaTime, float speed)
    {
        float effectiveSpeed = ResolveMoveSpeed(actor, speed);
        if (effectiveSpeed <= 0f)
        {
            return;
        }

        Vector3 direction = -FlatDirection(actor.Position, target.Position);
        actorService.MoveActor(actor, direction * (effectiveSpeed * deltaTime));
    }

    private bool IsStatusBlocked(TotemActorModel actor)
    {
        return statusService != null && statusService.IsStunned(actor);
    }

    private bool CanActorAct(TotemActorModel actor)
    {
        return statusService == null || statusService.CanAct(actor);
    }

    private float ResolveMoveSpeed(TotemActorModel actor, float speed)
    {
        if (statusService == null)
        {
            return speed;
        }

        return speed * statusService.GetMoveSpeedMultiplier(actor);
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

    private static bool HasPlannedPart(int plannedPartMask, int partId)
    {
        if (partId <= 0 || partId > 30)
        {
            return true;
        }

        int bit = 1 << partId;
        return (plannedPartMask & bit) != 0;
    }

    private static int TendencyArgmaxColor(float[] tendency)
    {
        if (tendency == null || tendency.Length <= 0)
        {
            return 1;
        }

        int bestIndex = 0;
        float bestValue = float.MinValue;
        for (int i = 0; i < tendency.Length; i++)
        {
            if (tendency[i] > bestValue)
            {
                bestValue = tendency[i];
                bestIndex = i;
            }
        }

        return Mathf.Clamp(bestIndex + 1, 1, TotemTattooService.ColorCount);
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
