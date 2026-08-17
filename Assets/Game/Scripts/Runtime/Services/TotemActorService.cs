using System;
using System.Collections.Generic;
using UnityEngine;

public enum TotemCombatRosterMode
{
    FullMatch = 0,
    PlayerOnlyPreview = 1,
}

public sealed class TotemActorService : TotemRuntimeServiceBase, ITotemRuntimeTickService, ITotemGameplaySimulationService
{
    public const int SmartAiCount = 3;
    public const int LightAiCount = TotemFirstPlayableRules.BotCount - SmartAiCount;
    public const int ParticipantCount = TotemFirstPlayableRules.ParticipantCount;
    public const float CoverIncomingDamageMultiplier = 0.6f;
    public const float CoverCloseRangeBypassDistance = 4f;
    public const float ParticipantSpawnMinDistance = 18f;
    public const float TeamSpawnMinDistance = 28f;
    public const float TeammateSpawnRadius = 3.5f;
    public const float TeammateSpawnMinDistance = 2f;
    private const float DeathHideDelay = 0.75f;
    private const float TerrainEffectTickInterval = 0.2f;
    private const int ParticipantSpawnRadialAttempts = 256;
    private const int ParticipantSpawnGlobalAttempts = 2048;
    private const float ParticipantSpawnSearchStep = 6f;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int DirectionHash = Animator.StringToHash("Direction");
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int DodgeTriggerHash = Animator.StringToHash("DodgeTrigger");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    private readonly List<TotemActorModel> actors = new List<TotemActorModel>(64);
    private readonly List<GameObject> spawnedObjects = new List<GameObject>(64);
    private readonly List<TotemPendingActorHide> pendingActorHides = new List<TotemPendingActorHide>(32);
    private readonly HashSet<TotemActorModel> movedActors = new HashSet<TotemActorModel>();
    private readonly Dictionary<Animator, TotemAnimatorParameterMask> animatorParameterMasks = new Dictionary<Animator, TotemAnimatorParameterMask>();
    private TotemGameFlowService flowService;
    private TotemMapService mapService;
    private TotemAssetService assetService;
    private TotemMatchFlowService matchFlowService;
    private GameObject actorRoot;
    private int damageSequence;
    private float terrainEffectAccumulator;
    private int terrainHazardHitCount;
    private float lastTerrainHazardDamageTick;
    private int terrainCoverReducedHitCount;
    private float lastTerrainCoverDamageBefore;
    private float lastTerrainCoverDamageAfter;
    private float combatElapsedSec;
    private bool playerStartupInvulnerable;
    private int playerStartupDamageBlockedCount;
    private string playerStartupProtectionReason = string.Empty;
    private TotemCombatRosterMode nextCombatRosterMode = TotemCombatRosterMode.FullMatch;

    public override string ServiceName => "Actor";

    public IReadOnlyList<TotemActorModel> Actors => actors;

    public TotemActorModel Player { get; private set; }

    public float CombatElapsedSeconds => combatElapsedSec;

    public bool PlayerStartupInvulnerable => playerStartupInvulnerable;

    public int PlayerStartupDamageBlockedCount => playerStartupDamageBlockedCount;

    /// <summary>
    /// 指定下一次进入 CombatHud 时生成的参与者名单。
    /// 请求只消费一次，正式战斗仍默认生成完整人机名单。
    /// </summary>
    public void RequestNextCombatRoster(TotemCombatRosterMode rosterMode)
    {
        nextCombatRosterMode = rosterMode;
    }

    public void BeginPlayerStartupProtection(string reason)
    {
        if (playerStartupInvulnerable)
        {
            return;
        }

        playerStartupInvulnerable = true;
        playerStartupDamageBlockedCount = 0;
        playerStartupProtectionReason = string.IsNullOrWhiteSpace(reason) ? "Startup" : reason;
        GFTrace.Success("TotemActor", "PlayerStartupProtection.Enabled", null, GFTrace.Data(
            "reason", playerStartupProtectionReason,
            "flowState", flowService?.CurrentState.ToString() ?? string.Empty));
    }

    public bool TryReleasePlayerStartupProtection(TotemActorModel expectedPlayer, string reason)
    {
        if (!playerStartupInvulnerable
            || expectedPlayer == null
            || Player != expectedPlayer
            || !expectedPlayer.IsAlive
            || flowService?.CurrentState != TotemGameFlowState.CombatHud)
        {
            return false;
        }

        playerStartupInvulnerable = false;
        playerStartupProtectionReason = string.IsNullOrWhiteSpace(reason) ? "Ready" : reason;
        terrainEffectAccumulator = 0f;
        GFTrace.Success("TotemActor", "PlayerStartupProtection.Released", null, GFTrace.Data(
            "reason", playerStartupProtectionReason,
            "blockedDamageCount", playerStartupDamageBlockedCount.ToString()));
        return true;
    }

    public bool CanOpponentTarget(TotemActorModel target)
    {
        var readiness = Runtime?.GetService<TotemParticipantReadinessService>();
        return target != null
            && target.IsAlive
            && (readiness == null || !IsParticipantActor(target) || readiness.CanBeTargeted(target))
            && (!playerStartupInvulnerable || target != Player);
    }

    public event Action<TotemActorModel, float, bool> DamageApplied;

    public event Action<TotemDamageRecord> DamageResolved;

    public event Action ActorsSpawned;

    public TotemDamageRecord LastDamage { get; private set; }

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        mapService = runtime.GetService<TotemMapService>();
        assetService = runtime.GetService<TotemAssetService>();
        matchFlowService = runtime.GetService<TotemMatchFlowService>();
        if (flowService != null)
        {
            flowService.StateChanged += OnFlowStateChanged;
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

        if (matchFlowService != null)
        {
            matchFlowService.PhaseChanged -= OnMatchPhaseChanged;
            matchFlowService = null;
        }

        DespawnActors();
        assetService = null;
        playerStartupInvulnerable = false;
        playerStartupDamageBlockedCount = 0;
        playerStartupProtectionReason = string.Empty;
        nextCombatRosterMode = TotemCombatRosterMode.FullMatch;
        DamageApplied = null;
        DamageResolved = null;
        ActorsSpawned = null;
        LastDamage = default;
        damageSequence = 0;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime > 0f && flowService?.CurrentState == TotemGameFlowState.CombatHud)
        {
            combatElapsedSec += deltaTime;
        }

        TickMovementAnimations();
        TickPendingActorHides(deltaTime);
        TickTerrainEffects(deltaTime);
    }

    public TotemActorSnapshot CaptureActorSnapshot()
    {
        var snapshot = new TotemActorSnapshot();
        var readiness = Runtime?.GetService<TotemParticipantReadinessService>();
        for (int i = 0; i < actors.Count; i++)
        {
            var actor = actors[i];
            string visualAssetKey = actor.VisualAssetKey ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(visualAssetKey))
            {
                if (visualAssetKey.StartsWith("primitive.", StringComparison.Ordinal))
                {
                    snapshot.visualFallbackActorCount++;
                    snapshot.lastVisualFallbackKey = visualAssetKey;
                }
                else
                {
                    snapshot.visualAssetActorCount++;
                    snapshot.lastVisualAssetKey = visualAssetKey;
                }
            }

            switch (actor.Kind)
            {
                case TotemActorKind.Player:
                    snapshot.playerCount++;
                    if (readiness == null ? actor.IsAlive : readiness.CountsAsAlive(actor))
                    {
                        snapshot.aliveParticipantCount++;
                    }
                    break;
                case TotemActorKind.SmartAi:
                    snapshot.smartAiCount++;
                    if (readiness == null ? actor.IsAlive : readiness.CountsAsAlive(actor))
                    {
                        snapshot.aliveParticipantCount++;
                    }
                    break;
                case TotemActorKind.LightAi:
                    snapshot.lightAiCount++;
                    if (readiness == null ? actor.IsAlive : readiness.CountsAsAlive(actor))
                    {
                        snapshot.aliveParticipantCount++;
                    }
                    break;
            }
        }

        snapshot.actorCount = snapshot.playerCount + snapshot.smartAiCount + snapshot.lightAiCount;
        snapshot.participantCount = snapshot.actorCount;
        snapshot.terrainHazardHitCount = terrainHazardHitCount;
        snapshot.lastTerrainHazardDamageTick = lastTerrainHazardDamageTick;
        snapshot.terrainCoverReducedHitCount = terrainCoverReducedHitCount;
        snapshot.lastTerrainCoverDamageBefore = lastTerrainCoverDamageBefore;
        snapshot.lastTerrainCoverDamageAfter = lastTerrainCoverDamageAfter;
        snapshot.playerStartupInvulnerable = playerStartupInvulnerable;
        snapshot.playerStartupDamageBlockedCount = playerStartupDamageBlockedCount;
        snapshot.playerStartupProtectionReason = playerStartupProtectionReason;
        return snapshot;
    }

    public TotemActorModel FindUniqueAliveParticipant()
    {
        TotemActorModel uniqueParticipant = null;
        var readiness = Runtime?.GetService<TotemParticipantReadinessService>();
        for (int i = 0; i < actors.Count; i++)
        {
            var actor = actors[i];
            if (!IsParticipantActor(actor)
                || !(readiness == null ? actor.IsAlive : readiness.CountsAsAlive(actor)))
            {
                continue;
            }

            if (uniqueParticipant != null)
            {
                return null;
            }

            uniqueParticipant = actor;
        }

        return uniqueParticipant;
    }

    public TotemActorModel FindUniqueAliveTeamRepresentative(out int aliveTeamCount)
    {
        aliveTeamCount = 0;
        TotemTeamId firstTeam = default;
        TotemActorModel representative = null;
        var readiness = Runtime?.GetService<TotemParticipantReadinessService>();
        for (int i = 0; i < actors.Count; i++)
        {
            TotemActorModel actor = actors[i];
            if (!IsParticipantActor(actor)
                || !actor.TeamId.IsValid
                || !(readiness == null ? actor.IsAlive : readiness.CountsAsAlive(actor)))
            {
                continue;
            }

            if (aliveTeamCount == 0)
            {
                firstTeam = actor.TeamId;
                representative = actor;
                aliveTeamCount = 1;
                continue;
            }

            if (actor.TeamId != firstTeam)
            {
                aliveTeamCount = 2;
                return null;
            }

            if (representative == null || actor.ActorId < representative.ActorId)
            {
                representative = actor;
            }
        }

        return representative;
    }

    public bool HasAliveParticipantOnTeam(TotemTeamId teamId)
    {
        if (!teamId.IsValid)
        {
            return false;
        }

        var readiness = Runtime?.GetService<TotemParticipantReadinessService>();
        for (int i = 0; i < actors.Count; i++)
        {
            TotemActorModel actor = actors[i];
            if (IsParticipantActor(actor)
                && actor.TeamId == teamId
                && (readiness == null ? actor.IsAlive : readiness.CountsAsAlive(actor)))
            {
                return true;
            }
        }

        return false;
    }

    public void MoveActor(TotemActorModel actor, Vector3 delta)
    {
        if (actor == null || !actor.IsAlive || delta.sqrMagnitude <= 0f)
        {
            return;
        }

        TotemMapSnapshot map = mapService?.CurrentMap;
        Vector2 worldMin = TotemMapService.GetWorldMin(map);
        Vector2 worldMax = TotemMapService.GetWorldMax(map);
        Vector3 previous = actor.Position;
        if (mapService != null)
        {
            delta *= mapService.GetMoveSpeedMultiplier(previous);
        }

        var next = actor.Position + delta;
        next.x = Mathf.Clamp(next.x, worldMin.x, worldMax.x);
        next.z = Mathf.Clamp(next.z, worldMin.y, worldMax.y);
        if (mapService != null && !mapService.IsWalkable(next))
        {
            var xOnly = new Vector3(next.x, previous.y, previous.z);
            if (mapService.IsWalkable(xOnly))
            {
                next = xOnly;
            }
            else
            {
                var zOnly = new Vector3(previous.x, previous.y, next.z);
                next = mapService.IsWalkable(zOnly) ? zOnly : previous;
            }
        }

        actor.Position = next;
        if (actor.GameObject != null)
        {
            actor.GameObject.transform.position = next;
        }

        UpdateActorMovementAnimation(actor, next - previous);
    }

    public bool TryApplyFirstPlayableMoveCommand(
        in TotemGameplayCommand command,
        float deltaTime,
        float moveSpeed,
        out float movedDistance)
    {
        movedDistance = 0f;
        if (!command.IsValid
            || command.Type != TotemGameplayCommandType.Move
            || deltaTime <= 0f
            || moveSpeed <= 0f
            || !TotemMatchPhaseContract.IsCombat(matchFlowService?.CurrentPhase ?? TotemMatchPhase.FrontEnd))
        {
            return false;
        }

        TotemActorModel actor = null;
        for (int i = 0; i < actors.Count; i++)
        {
            if (actors[i]?.ParticipantId == command.ParticipantId.Value)
            {
                actor = actors[i];
                break;
            }
        }

        Vector3 input = command.WorldValue;
        input.y = 0f;
        float magnitude = Mathf.Clamp01(input.magnitude);
        if (actor == null || !actor.IsAlive || magnitude <= 0.001f)
        {
            return false;
        }

        movedDistance = moveSpeed * deltaTime * magnitude;
        MoveActor(actor, input.normalized * movedDistance);
        return true;
    }

    public bool ApplyDamage(TotemActorModel target, float amount, TotemCombatantModel source = null, string reason = null)
    {
        if (target == null || amount <= 0f || !target.IsAlive)
        {
            return false;
        }

        var readiness = Runtime?.GetService<TotemParticipantReadinessService>();
        if (readiness != null
            && IsParticipantActor(target)
            && !readiness.CanBeTargeted(target))
        {
            if (target == Player)
            {
                playerStartupDamageBlockedCount++;
            }

            GFTrace.Info("TotemActor", "Damage.Blocked.ParticipantReadiness", null, GFTrace.Data(
                "source", source?.Name ?? string.Empty,
                "target", target.Name,
                "lifecycle", readiness.GetLifecycle(target).ToString(),
                "reason", string.IsNullOrWhiteSpace(reason) ? "Damage" : reason));
            return false;
        }

        var sourceParticipant = source as TotemActorModel;
        if (readiness != null
            && IsParticipantActor(sourceParticipant)
            && !readiness.CanAct(sourceParticipant))
        {
            GFTrace.Info("TotemActor", "Damage.Blocked.ParticipantCannotAct", null, GFTrace.Data(
                "source", source.Name,
                "target", target.Name,
                "lifecycle", readiness.GetLifecycle(sourceParticipant).ToString(),
                "reason", string.IsNullOrWhiteSpace(reason) ? "Damage" : reason));
            return false;
        }

        var relationship = Runtime?.GetService<TotemCombatRelationshipService>();
        if (relationship != null)
        {
            float worldTime = Runtime?.GetService<TotemMatchClockService>()?.WorldTime ?? combatElapsedSec;
            var decision = relationship.EvaluateDamage(
                source,
                target,
                new TotemCombatRelationshipContext(worldTime));
            if (!decision.Allowed)
            {
                GFTrace.Info("TotemActor", "Damage.Blocked.Relationship", null, GFTrace.Data(
                    "source", source?.Name ?? string.Empty,
                    "target", target.Name,
                    "relationship", decision.Reason.ToString(),
                    "reason", string.IsNullOrWhiteSpace(reason) ? "Damage" : reason));
                return false;
            }
        }

        if (playerStartupInvulnerable && target == Player)
        {
            playerStartupDamageBlockedCount++;
            GFTrace.Info("TotemActor", "Damage.Blocked.PlayerStartupProtection", null, GFTrace.Data(
                "source", source?.Name ?? string.Empty,
                "target", target.Name,
                "amount", amount.ToString("F1"),
                "reason", string.IsNullOrWhiteSpace(reason) ? "Damage" : reason));
            return false;
        }

        var lifecycle = Runtime?.GetService<TotemFirstPlayableLifecycleService>();
        if (lifecycle != null && lifecycle.IsReviveProtected(target))
        {
            GFTrace.Info("TotemActor", "Damage.Blocked.ReviveProtection", null, GFTrace.Data(
                "source", source?.Name ?? string.Empty,
                "target", target.Name,
                "amount", amount.ToString("F1"),
                "reason", string.IsNullOrWhiteSpace(reason) ? "Damage" : reason));
            return false;
        }

        float originalAmount = amount;
        amount = ResolveTerrainAdjustedDamage(source, target, amount, reason);
        if (amount <= 0f)
        {
            return false;
        }

        if (amount < originalAmount)
        {
            terrainCoverReducedHitCount++;
            lastTerrainCoverDamageBefore = originalAmount;
            lastTerrainCoverDamageAfter = amount;
        }

        float healthBeforeLifecycleResolution = target.Health;
        float appliedDamage;
        bool killed;
        if (lifecycle != null && lifecycle.IsDowned(target))
        {
            if (!lifecycle.TryApplyDownedDamage(target, amount, source, out appliedDamage, out _))
            {
                return false;
            }

            killed = !target.IsAlive;
        }
        else if (lifecycle != null
            && amount >= target.Health
            && lifecycle.TryResolveLethalDamage(target, source, out TotemDownedStateContract lethalTransition))
        {
            appliedDamage = Mathf.Min(amount, healthBeforeLifecycleResolution);
            killed = lethalTransition.Current == TotemFirstPlayableLifeState.Eliminated;
        }
        else
        {
            appliedDamage = target.ApplyDamage(amount);
            killed = !target.IsAlive;
        }

        if (appliedDamage <= 0f)
        {
            return false;
        }

        if (killed)
        {
            ApplyActorDeathAnimation(target, reason);
            ScheduleActorHide(target);
        }

        LastDamage = new TotemDamageRecord
        {
            Sequence = ++damageSequence,
            Source = source,
            Target = target,
            Amount = appliedDamage,
            Killed = killed,
            Reason = string.IsNullOrWhiteSpace(reason) ? "Damage" : reason,
            TargetHealthAfter = target.Health,
        };
        DamageApplied?.Invoke(target, appliedDamage, killed);
        DamageResolved?.Invoke(LastDamage);
        return killed;
    }

    /// <summary>
    /// Resolves damage through the same policy as ApplyDamage and reports whether
    /// a damage record was committed, independent of whether the hit was lethal.
    /// </summary>
    public bool TryApplyDamage(
        TotemActorModel target,
        float amount,
        TotemCombatantModel source = null,
        string reason = null)
    {
        int sequenceBefore = damageSequence;
        ApplyDamage(target, amount, source, reason);
        return damageSequence > sequenceBefore;
    }

    public float ResolveTerrainAdjustedDamage(TotemCombatantModel source, TotemActorModel target, float amount, string reason = null)
    {
        if (amount <= 0f || source == null || target == null || source == target || mapService?.CurrentMap == null)
        {
            return Mathf.Max(0f, amount);
        }

        if (IsEnvironmentOrStatusDamage(reason))
        {
            return Mathf.Max(0f, amount);
        }

        if (mapService.QueryTerrain(target.Position) != TotemTerrainType.Cover)
        {
            return Mathf.Max(0f, amount);
        }

        if (FlatDistance(source.Position, target.Position) <= CoverCloseRangeBypassDistance)
        {
            return Mathf.Max(0f, amount);
        }

        return Mathf.Max(0f, amount * CoverIncomingDamageMultiplier);
    }

    private void TickTerrainEffects(float deltaTime)
    {
        if (deltaTime <= 0f || mapService?.CurrentMap == null || actors.Count <= 0)
        {
            return;
        }

        terrainEffectAccumulator += deltaTime;
        if (terrainEffectAccumulator < TerrainEffectTickInterval)
        {
            return;
        }

        float tickDuration = terrainEffectAccumulator;
        terrainEffectAccumulator = 0f;
        lastTerrainHazardDamageTick = 0f;
        int hitCount = 0;
        for (int i = 0; i < actors.Count; i++)
        {
            var actor = actors[i];
            if (actor == null || !actor.IsAlive)
            {
                continue;
            }

            float hazardDps = TotemMapService.GetTerrainHazardDps(mapService.QueryTerrain(actor.Position));
            if (hazardDps <= 0f)
            {
                continue;
            }

            float damage = hazardDps * tickDuration;
            ApplyDamage(actor, damage, null, "TerrainHazard");
            lastTerrainHazardDamageTick += damage;
            hitCount++;
        }

        if (hitCount > 0)
        {
            terrainHazardHitCount += hitCount;
        }
    }

    public void NotifyActorAttack(TotemActorModel actor, string reason = null)
    {
        if (actor == null || !actor.IsAlive)
        {
            return;
        }

        actor.AnimationAttackTriggerCount++;
        actor.AnimationLastReason = string.IsNullOrWhiteSpace(reason) ? "Attack" : reason;
        var animator = FindAnimator(actor);
        if (animator == null)
        {
            return;
        }

        var mask = GetAnimatorParameterMask(animator);
        if (mask.hasAttackTrigger)
        {
            animator.SetTrigger(AttackTriggerHash);
        }
    }

    public void NotifyActorDodge(TotemActorModel actor, string reason = null)
    {
        if (actor == null || !actor.IsAlive)
        {
            return;
        }

        actor.AnimationLastReason = string.IsNullOrWhiteSpace(reason) ? "Dodge" : reason;
        var animator = FindAnimator(actor);
        if (animator == null)
        {
            return;
        }

        var mask = GetAnimatorParameterMask(animator);
        if (mask.hasDodgeTrigger)
        {
            animator.SetTrigger(DodgeTriggerHash);
        }
    }

    public void NotifyParticipantEliminated(TotemActorModel actor, string reason)
    {
        if (actor == null)
        {
            return;
        }

        ApplyActorDeathAnimation(actor, reason);
        ScheduleActorHide(actor);
    }

    public TotemActorAnimationSnapshot CaptureAnimationSnapshot(TotemActorModel actor)
    {
        var snapshot = new TotemActorAnimationSnapshot
        {
            actorId = actor?.ActorId ?? 0,
            actorName = actor?.Name ?? string.Empty,
            actorKind = actor?.Kind ?? TotemActorKind.Player,
            hasGameObject = actor?.GameObject != null,
            animationMoving = actor?.AnimationMoving ?? false,
            animationDirection = actor?.AnimationDirection ?? 0,
            animationDead = actor?.AnimationDead ?? false,
            attackTriggerCount = actor?.AnimationAttackTriggerCount ?? 0,
            deathTriggerCount = actor?.AnimationDeathTriggerCount ?? 0,
            lastReason = actor?.AnimationLastReason ?? string.Empty,
        };

        var animator = FindAnimator(actor);
        snapshot.hasAnimator = animator != null;
        if (animator == null)
        {
            return snapshot;
        }

        var mask = GetAnimatorParameterMask(animator);
        snapshot.animatorHasIsMoving = mask.hasIsMoving;
        snapshot.animatorHasDirection = mask.hasDirection;
        snapshot.animatorHasAttackTrigger = mask.hasAttackTrigger;
        snapshot.animatorHasDie = mask.hasDie;
        snapshot.animatorHasDead = mask.hasDead;
        if (mask.hasIsMoving)
        {
            snapshot.animatorIsMoving = animator.GetBool(IsMovingHash);
        }

        if (mask.hasDirection)
        {
            snapshot.animatorDirection = animator.GetInteger(DirectionHash);
        }

        if (mask.hasDead)
        {
            snapshot.animatorDead = animator.GetBool(DeadHash);
        }

        return snapshot;
    }

    public void SpawnActors(
        TotemMapSnapshot map,
        bool createObjects,
        TotemCombatRosterMode rosterMode = TotemCombatRosterMode.FullMatch)
    {
        DespawnActors();
        actorRoot = createObjects ? new GameObject("[TotemActors]") : null;
        if (actorRoot != null)
        {
            spawnedObjects.Add(actorRoot);
        }

        var spawnInfos = BuildActorRoster(map, rosterMode);
        for (int i = 0; i < spawnInfos.Length; i++)
        {
            var actor = new TotemActorModel(spawnInfos[i]);
            if (createObjects)
            {
                actor.GameObject = CreateActorObject(actor);
                ResetActorAnimation(actor);
            }

            actors.Add(actor);
            if (actor.Kind == TotemActorKind.Player)
            {
                Player = actor;
            }
        }

        var snapshot = CaptureActorSnapshot();
        GFTrace.Success("TotemActor", "Actors.Spawned", null, GFTrace.Data(
            "actorCount", snapshot.actorCount.ToString(),
            "smartAi", snapshot.smartAiCount.ToString(),
            "lightAi", snapshot.lightAiCount.ToString(),
            "participantCount", snapshot.actorCount.ToString()));
        ActorsSpawned?.Invoke();
    }

    public static TotemActorSpawnInfo[] BuildActorRoster(
        TotemMapSnapshot map,
        TotemCombatRosterMode rosterMode = TotemCombatRosterMode.FullMatch)
    {
        if (map == null)
        {
            map = TotemMapService.BuildLayout(seed: 1, themeId: 1);
        }

        int spawnSelectionSeed = unchecked(map.Seed ^ 486187739);
        var random = new System.Random(spawnSelectionSeed);
        var playerAnchors = TotemMapService.FindAnchors(map, TotemMapAnchorKind.PlayerSpawn);
        if (playerAnchors.Length == 0)
        {
            GFTrace.Warning("TotemActor", "SpawnAnchors.MissingFallback", null, GFTrace.Data(
                "mapSeed", map.Seed.ToString(),
                "fallback", "SpawnRoomOrDefaultCenter"));
        }

        int teamAnchorStartIndex = playerAnchors.Length > 0 ? random.Next(playerAnchors.Length) : 0;
        Vector3 fallbackCenter = FindRoom(map, TotemRoomType.SouthWestArea)?.CenterWorld ?? new Vector3(82f, 0f, 82f);
        var participantPositions = new List<Vector3>(ParticipantCount);
        if (rosterMode == TotemCombatRosterMode.PlayerOnlyPreview)
        {
            Vector3 previewPosition = ResolveTeamCenter(map, playerAnchors, fallbackCenter, teamAnchorStartIndex, 0, participantPositions);
            return new[]
            {
                CreateParticipantSpawnInfo(1, 0, true, previewPosition),
            };
        }

        var result = new TotemActorSpawnInfo[ParticipantCount];
        int cursor = 0;
        var teamCenters = new List<Vector3>(TotemFirstPlayableRules.TeamCount);
        for (int teamIndex = 0; teamIndex < TotemFirstPlayableRules.TeamCount; teamIndex++)
        {
            Vector3 teamCenter = ResolveTeamCenter(map, playerAnchors, fallbackCenter, teamAnchorStartIndex, teamIndex, teamCenters);
            teamCenters.Add(teamCenter);
            float angle = (float)(random.NextDouble() * Mathf.PI * 2f);
            for (int teamSlot = 0; teamSlot < TotemFirstPlayableRules.TeamSize; teamSlot++)
            {
                float memberAngle = angle + teamSlot * Mathf.PI;
                var desiredPosition = new Vector3(
                    teamCenter.x + Mathf.Cos(memberAngle) * TeammateSpawnRadius,
                    GetParticipantSpawnY(map),
                    teamCenter.z + Mathf.Sin(memberAngle) * TeammateSpawnRadius);
                Vector3 position = ResolveAdjacentTeamMemberPosition(
                    map,
                    desiredPosition,
                    teamCenter,
                    participantPositions,
                    teamIndex * TotemFirstPlayableRules.TeamSize + teamSlot);
                bool human = teamIndex == 0 && teamSlot == 0;
                int participantId = cursor + 1;
                result[cursor++] = CreateParticipantSpawnInfo(participantId, teamIndex, human, position);
                participantPositions.Add(position);
            }
        }

        return result;
    }

    private static TotemActorSpawnInfo CreateParticipantSpawnInfo(int participantId, int teamId, bool human, Vector3 position)
    {
        int botIndex = participantId - 2;
        bool smartBot = !human && botIndex < SmartAiCount;
        return new TotemActorSpawnInfo
        {
            ActorId = participantId,
            TeamId = teamId,
            Name = human ? "Player" : $"Bot{participantId - 1:00}",
            Kind = human ? TotemActorKind.Player : smartBot ? TotemActorKind.SmartAi : TotemActorKind.LightAi,
            ControllerKind = human ? TotemParticipantControllerKind.Human : smartBot ? TotemParticipantControllerKind.SmartBot : TotemParticipantControllerKind.LightBot,
            Position = position,
            MaxHealth = 100f,
        };
    }

    private static Vector3 ResolveTeamCenter(
        TotemMapSnapshot map,
        IReadOnlyList<TotemMapAnchor> anchors,
        Vector3 fallbackCenter,
        int anchorStartIndex,
        int teamIndex,
        IReadOnlyList<Vector3> occupiedTeamCenters)
    {
        Vector3 desired = fallbackCenter;
        if (anchors != null && anchors.Count > 0)
        {
            int index = (anchorStartIndex + teamIndex) % anchors.Count;
            if (anchors[index] != null)
            {
                desired = anchors[index].Position;
            }
        }
        else
        {
            float angle = teamIndex * Mathf.PI * 2f / TotemFirstPlayableRules.TeamCount;
            desired += new Vector3(Mathf.Cos(angle) * TeamSpawnMinDistance, 0f, Mathf.Sin(angle) * TeamSpawnMinDistance);
        }

        desired.y = GetParticipantSpawnY(map);
        return ResolveParticipantSpawnPosition(
            map,
            desired,
            desired,
            occupiedTeamCenters,
            100 + teamIndex,
            TeamSpawnMinDistance);
    }

    private static Vector3 ResolveAdjacentTeamMemberPosition(
        TotemMapSnapshot map,
        Vector3 desired,
        Vector3 teamCenter,
        IReadOnlyList<Vector3> occupiedPositions,
        int salt)
    {
        for (int attempt = 0; attempt < 24; attempt++)
        {
            float angle = (salt * 47f + attempt * 137.50777f) * Mathf.Deg2Rad;
            var candidate = attempt == 0
                ? desired
                : new Vector3(
                    teamCenter.x + Mathf.Cos(angle) * TeammateSpawnRadius,
                    GetParticipantSpawnY(map),
                    teamCenter.z + Mathf.Sin(angle) * TeammateSpawnRadius);
            candidate = ClampSpawnPosition(map, candidate);
            if (IsParticipantSpawnArea(map, candidate)
                && GetNearestParticipantDistance(candidate, occupiedPositions) >= TeammateSpawnMinDistance)
            {
                return candidate;
            }
        }

        return ResolveParticipantSpawnPosition(
            map,
            desired,
            teamCenter,
            occupiedPositions,
            200 + salt,
            TeammateSpawnMinDistance);
    }

    private static Vector3 ResolveWalkableParticipantSpawnPosition(TotemMapSnapshot map, Vector3 desiredPosition, Vector3 groupCenter, Vector3 fallback)
    {
        if (IsWalkableSpawnPosition(map, desiredPosition))
        {
            return desiredPosition;
        }

        if (IsWalkableSpawnPosition(map, groupCenter))
        {
            return groupCenter;
        }

        return fallback;
    }

    private static bool IsWalkableSpawnPosition(TotemMapSnapshot map, Vector3 position)
    {
        return map == null || TotemMapService.IsTerrainWalkable(TotemMapService.QueryTerrain(map, position));
    }

    private static Vector3 ResolveParticipantSpawnPosition(
        TotemMapSnapshot map,
        Vector3 desiredPosition,
        Vector3 groupCenter,
        IReadOnlyList<Vector3> occupiedPositions,
        int salt,
        float minDistance = ParticipantSpawnMinDistance)
    {
        desiredPosition = ClampSpawnPosition(map, desiredPosition);
        desiredPosition.y = GetParticipantSpawnY(map);
        if (IsValidParticipantSpawnPosition(map, desiredPosition, occupiedPositions, minDistance))
        {
            return desiredPosition;
        }

        Vector3 bestPosition = desiredPosition;
        float bestDistance = GetNearestParticipantDistance(desiredPosition, occupiedPositions);
        bool hasWalkableCandidate = IsParticipantSpawnArea(map, desiredPosition);
        for (int attempt = 0; attempt < ParticipantSpawnRadialAttempts; attempt++)
        {
            int ring = attempt / 24 + 1;
            float radius = minDistance + ring * ParticipantSpawnSearchStep;
            float angle = (attempt * 137.50777f + salt * 23.711f) * Mathf.Deg2Rad;
            var candidate = new Vector3(
                groupCenter.x + Mathf.Cos(angle) * radius,
                GetParticipantSpawnY(map),
                groupCenter.z + Mathf.Sin(angle) * radius);
            if (TryEvaluateParticipantSpawnCandidate(map, candidate, occupiedPositions, minDistance, ref bestPosition, ref bestDistance, ref hasWalkableCandidate))
            {
                return bestPosition;
            }
        }

        Vector2 worldMin = TotemMapService.GetWorldMin(map);
        Vector2 worldSize = TotemMapService.GetWorldSize(map);
        for (int attempt = 0; attempt < ParticipantSpawnGlobalAttempts; attempt++)
        {
            float x = worldMin.x + DeterministicUnit(salt, attempt, 0) * worldSize.x;
            float z = worldMin.y + DeterministicUnit(salt, attempt, 1) * worldSize.y;
            var candidate = new Vector3(x, GetParticipantSpawnY(map), z);
            if (TryEvaluateParticipantSpawnCandidate(map, candidate, occupiedPositions, minDistance, ref bestPosition, ref bestDistance, ref hasWalkableCandidate))
            {
                return bestPosition;
            }
        }

        if (hasWalkableCandidate)
        {
            return bestPosition;
        }

        return ResolveWalkableParticipantSpawnPosition(map, desiredPosition, groupCenter, desiredPosition);
    }

    private static bool TryEvaluateParticipantSpawnCandidate(
        TotemMapSnapshot map,
        Vector3 candidate,
        IReadOnlyList<Vector3> occupiedPositions,
        float minDistance,
        ref Vector3 bestPosition,
        ref float bestDistance,
        ref bool hasWalkableCandidate)
    {
        candidate = ClampSpawnPosition(map, candidate);
        candidate.y = GetParticipantSpawnY(map);
        if (!IsParticipantSpawnArea(map, candidate))
        {
            return false;
        }

        float nearestDistance = GetNearestParticipantDistance(candidate, occupiedPositions);
        if (nearestDistance > bestDistance || !hasWalkableCandidate)
        {
            bestDistance = nearestDistance;
            bestPosition = candidate;
            hasWalkableCandidate = true;
        }

        return nearestDistance >= minDistance;
    }

    private static bool IsValidParticipantSpawnPosition(
        TotemMapSnapshot map,
        Vector3 candidate,
        IReadOnlyList<Vector3> occupiedPositions,
        float minDistance)
    {
        return IsParticipantSpawnArea(map, candidate)
            && GetNearestParticipantDistance(candidate, occupiedPositions) >= minDistance;
    }

    private static bool IsParticipantSpawnArea(TotemMapSnapshot map, Vector3 position)
    {
        if (!IsWalkableSpawnPosition(map, position))
        {
            return false;
        }

        if (map == null)
        {
            return true;
        }

        float initialRadius = TotemMapService.GetInitialZoneRadius(map);
        var center = new Vector3(map.InitialZoneCenter.x, 0f, map.InitialZoneCenter.y);
        return FlatDistance(position, center) <= initialRadius;
    }

    private static float GetNearestParticipantDistance(Vector3 candidate, IReadOnlyList<Vector3> occupiedPositions)
    {
        if (occupiedPositions == null || occupiedPositions.Count <= 0)
        {
            return float.MaxValue;
        }

        float best = float.MaxValue;
        for (int i = 0; i < occupiedPositions.Count; i++)
        {
            float distance = FlatDistance(candidate, occupiedPositions[i]);
            if (distance < best)
            {
                best = distance;
            }
        }

        return best;
    }

    private static Vector3 ClampSpawnPosition(TotemMapSnapshot map, Vector3 position)
    {
        Vector2 worldMin = TotemMapService.GetWorldMin(map);
        Vector2 worldMax = TotemMapService.GetWorldMax(map);
        position.x = Mathf.Clamp(position.x, worldMin.x, worldMax.x);
        position.z = Mathf.Clamp(position.z, worldMin.y, worldMax.y);
        return position;
    }

    private static float GetParticipantSpawnY(TotemMapSnapshot map)
    {
        return (map?.WorldGroundY ?? 0f) + 0.5f;
    }

    private static float DeterministicUnit(int salt, int attempt, int axis)
    {
        unchecked
        {
            uint value = (uint)(salt * 73856093) ^ (uint)(attempt * 19349663) ^ (uint)(axis * 83492791) ^ 0x9E3779B9u;
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return (value & 0x00FFFFFFu) / 16777215f;
        }
    }

    public static bool IsParticipantActor(TotemActorModel actor)
    {
        return actor != null && IsParticipantKind(actor.Kind);
    }

    public static bool IsParticipantKind(TotemActorKind kind)
    {
        return kind == TotemActorKind.Player
            || kind == TotemActorKind.SmartAi
            || kind == TotemActorKind.LightAi;
    }

    private static bool IsEnvironmentOrStatusDamage(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return false;
        }

        return string.Equals(reason, "TerrainHazard", StringComparison.Ordinal)
            || string.Equals(reason, "ShrinkZone", StringComparison.Ordinal)
            || reason.StartsWith("Status:", StringComparison.Ordinal)
            || reason.StartsWith("StatusTick:", StringComparison.Ordinal)
            || reason.IndexOf("Status", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            combatElapsedSec = 0f;
            var map = mapService?.CurrentMap ?? TotemMapService.BuildLayout(seed: 1, themeId: 1);
            TotemCombatRosterMode rosterMode = nextCombatRosterMode;
            nextCombatRosterMode = TotemCombatRosterMode.FullMatch;
            SpawnActors(map, createObjects: true, rosterMode: rosterMode);
            SetActorWorldVisible(matchFlowService?.CurrentPhase != TotemMatchPhase.OpeningBuild);
            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            playerStartupInvulnerable = false;
            playerStartupProtectionReason = "CombatHud.Exit";
            DespawnActors();
            LastDamage = default;
            damageSequence = 0;
            combatElapsedSec = 0f;
            GFTrace.Info("TotemActor", "Actors.Despawned", null, GFTrace.Data("nextState", nextState.ToString()));
        }
    }

    private void OnMatchPhaseChanged(TotemMatchPhase previousPhase, TotemMatchPhase nextPhase)
    {
        if (previousPhase == TotemMatchPhase.OpeningBuild && nextPhase == TotemMatchPhase.Round1Combat)
        {
            SetActorWorldVisible(true);
        }
    }

    private void SetActorWorldVisible(bool visible)
    {
        if (actorRoot != null && actorRoot.activeSelf != visible)
        {
            actorRoot.SetActive(visible);
        }
    }

    private GameObject CreateActorObject(TotemActorModel actor)
    {
        string assetKey = GetActorAssetKey(actor);
        string usedAssetKey = assetKey;
        bool instantiated = TryInstantiateActorObject(usedAssetKey, actor, out var instance);

        if (instantiated)
        {
            instance.name = $"Totem_{actor.Name}";
            actor.VisualAssetKey = usedAssetKey;
            TotemActorVisualHelper.AttachActorVisuals(instance, actor.Kind);
            TotemHitRegionMarker.AttachParticipantMarkers(instance, actor.CombatantId);
            spawnedObjects.Add(instance);
            return instance;
        }

        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = $"Totem_{actor.Name}";
        go.transform.SetParent(actorRoot.transform, false);
        go.transform.position = actor.Position;
        go.transform.localScale = GetActorScale(actor.Kind);
        SetColor(go, GetActorColor(actor.Kind));
        if (TotemActorVisualHelper.TryResolveFactionRingColor(actor.Kind, out var factionColor))
        {
            TotemActorVisualHelper.AttachFactionRing(go, factionColor);
        }
        actor.VisualAssetKey = $"primitive.{actor.Kind}";
        TotemActorVisualHelper.AttachActorVisuals(go, actor.Kind);
        TotemHitRegionMarker.AttachParticipantMarkers(go, actor.CombatantId);
        spawnedObjects.Add(go);
        return go;
    }

    private bool TryInstantiateActorObject(string assetKey, TotemActorModel actor, out GameObject instance)
    {
        instance = null;
        return assetService != null
            && !string.IsNullOrWhiteSpace(assetKey)
            && assetService.TryInstantiateGameObject(assetKey, actorRoot.transform, actor.Position, GetActorScale(actor.Kind), out instance);
    }

    public static string GetPlayerAssetKey(int characterId)
    {
        return "actor.player";
    }

    private static string GetActorAssetKey(TotemActorModel actor)
    {
        if (actor == null)
        {
            return string.Empty;
        }

        if (actor.Kind == TotemActorKind.Player)
        {
            return GetPlayerAssetKey(actor.ActorId);
        }

        return GetActorAssetKey(actor.Kind);
    }

    private static string GetActorAssetKey(TotemActorKind kind)
    {
        switch (kind)
        {
            case TotemActorKind.Player:
            case TotemActorKind.SmartAi:
            case TotemActorKind.LightAi:
                return "actor.player";
            default:
                return string.Empty;
        }
    }

    private void DespawnActors()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            TotemHitRegionMarker.Detach(spawnedObjects[i]);
            DestroyObject(spawnedObjects[i]);
        }

        pendingActorHides.Clear();
        movedActors.Clear();
        animatorParameterMasks.Clear();
        spawnedObjects.Clear();
        actors.Clear();
        Player = null;
        actorRoot = null;
        LastDamage = default;
        damageSequence = 0;
        terrainEffectAccumulator = 0f;
        terrainHazardHitCount = 0;
        lastTerrainHazardDamageTick = 0f;
        terrainCoverReducedHitCount = 0;
        lastTerrainCoverDamageBefore = 0f;
        lastTerrainCoverDamageAfter = 0f;
        combatElapsedSec = 0f;
    }

    private static TotemRoomInfo FindRoom(TotemMapSnapshot map, TotemRoomType roomType)
    {
        var rooms = map?.Rooms;
        if (rooms == null)
        {
            return null;
        }

        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i].RoomType == roomType)
            {
                return rooms[i];
            }
        }

        return null;
    }

    private static Vector3 GetActorScale(TotemActorKind kind)
    {
        switch (kind)
        {
            case TotemActorKind.Player:
                return new Vector3(0.8f, 0.8f, 0.8f);
            default:
                return new Vector3(0.7f, 0.7f, 0.7f);
        }
    }

    private static Color GetActorColor(TotemActorKind kind)
    {
        switch (kind)
        {
            case TotemActorKind.Player:
                return new Color(0.20f, 0.65f, 1f);
            case TotemActorKind.SmartAi:
                return new Color(1f, 0.35f, 0.25f);
            case TotemActorKind.LightAi:
                return new Color(1f, 0.75f, 0.25f);
            default:
                return Color.gray;
        }
    }

    private static void SetColor(GameObject go, Color color)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        renderer.material = material;
    }

    public static int ComputeAnimationDirection(Vector3 delta)
    {
        if (Mathf.Abs(delta.z) >= Mathf.Abs(delta.x))
        {
            return delta.z >= 0f ? 1 : 0;
        }

        return delta.x >= 0f ? 3 : 2;
    }

    private void TickMovementAnimations()
    {
        for (int i = 0; i < actors.Count; i++)
        {
            var actor = actors[i];
            if (actor == null || !actor.IsAlive)
            {
                continue;
            }

            if (!movedActors.Contains(actor))
            {
                SetActorMoving(actor, false, actor.AnimationDirection, "MoveIdle");
            }
        }

        movedActors.Clear();
    }

    private void TickPendingActorHides(float deltaTime)
    {
        if (pendingActorHides.Count <= 0)
        {
            return;
        }

        for (int i = pendingActorHides.Count - 1; i >= 0; i--)
        {
            var pending = pendingActorHides[i];
            if (pending == null || pending.actor == null)
            {
                pendingActorHides.RemoveAt(i);
                continue;
            }

            pending.remainingSec -= Mathf.Max(0f, deltaTime);
            if (pending.remainingSec > 0f)
            {
                continue;
            }

            if (pending.actor.GameObject != null)
            {
                pending.actor.GameObject.SetActive(false);
            }

            pendingActorHides.RemoveAt(i);
        }
    }

    private void UpdateActorMovementAnimation(TotemActorModel actor, Vector3 actualDelta)
    {
        if (actor == null || !actor.IsAlive || actualDelta.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        int direction = ComputeAnimationDirection(actualDelta);
        movedActors.Add(actor);
        SetActorMoving(actor, true, direction, "Move");
    }

    private void ResetActorAnimation(TotemActorModel actor)
    {
        if (actor == null)
        {
            return;
        }

        actor.AnimationMoving = false;
        actor.AnimationDirection = 0;
        actor.AnimationDead = false;
        actor.AnimationAttackTriggerCount = 0;
        actor.AnimationDeathTriggerCount = 0;
        actor.AnimationLastReason = "Spawn";

        var animator = FindAnimator(actor);
        if (animator == null)
        {
            return;
        }

        var mask = GetAnimatorParameterMask(animator);
        if (mask.hasIsMoving)
        {
            animator.SetBool(IsMovingHash, false);
        }

        if (mask.hasDirection)
        {
            animator.SetInteger(DirectionHash, 0);
        }

        if (mask.hasDead)
        {
            animator.SetBool(DeadHash, false);
        }

        if (mask.hasDie)
        {
            animator.ResetTrigger(DieHash);
        }

        if (mask.hasAttackTrigger)
        {
            animator.ResetTrigger(AttackTriggerHash);
        }
    }

    private void SetActorMoving(TotemActorModel actor, bool moving, int direction, string reason)
    {
        if (actor == null || actor.AnimationDead)
        {
            return;
        }

        actor.AnimationMoving = moving;
        actor.AnimationDirection = Mathf.Clamp(direction, 0, 3);
        actor.AnimationLastReason = reason ?? string.Empty;

        var animator = FindAnimator(actor);
        if (animator == null)
        {
            return;
        }

        var mask = GetAnimatorParameterMask(animator);
        if (mask.hasIsMoving)
        {
            animator.SetBool(IsMovingHash, moving);
        }

        if (mask.hasDirection)
        {
            animator.SetInteger(DirectionHash, actor.AnimationDirection);
        }
    }

    private void ApplyActorDeathAnimation(TotemActorModel actor, string reason)
    {
        if (actor == null || actor.AnimationDead)
        {
            return;
        }

        actor.AnimationMoving = false;
        actor.AnimationDead = true;
        actor.AnimationDeathTriggerCount++;
        actor.AnimationLastReason = string.IsNullOrWhiteSpace(reason) ? "Death" : reason;

        var animator = FindAnimator(actor);
        if (animator == null)
        {
            return;
        }

        var mask = GetAnimatorParameterMask(animator);
        if (mask.hasIsMoving)
        {
            animator.SetBool(IsMovingHash, false);
        }

        if (mask.hasDead)
        {
            animator.SetBool(DeadHash, true);
        }

        if (mask.hasDie)
        {
            animator.SetTrigger(DieHash);
        }
    }

    private void ScheduleActorHide(TotemActorModel actor)
    {
        if (actor == null || actor.Kind == TotemActorKind.Player)
        {
            return;
        }

        for (int i = 0; i < pendingActorHides.Count; i++)
        {
            if (pendingActorHides[i]?.actor == actor)
            {
                pendingActorHides[i].remainingSec = DeathHideDelay;
                return;
            }
        }

        pendingActorHides.Add(new TotemPendingActorHide
        {
            actor = actor,
            remainingSec = DeathHideDelay,
        });
    }

    private Animator FindAnimator(TotemActorModel actor)
    {
        if (actor?.GameObject == null)
        {
            return null;
        }

        return actor.GameObject.GetComponentInChildren<Animator>(true);
    }

    private TotemAnimatorParameterMask GetAnimatorParameterMask(Animator animator)
    {
        if (animator == null)
        {
            return default;
        }

        if (animatorParameterMasks.TryGetValue(animator, out var cached))
        {
            return cached;
        }

        var mask = new TotemAnimatorParameterMask();
        if (animator.runtimeAnimatorController != null)
        {
            var parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.nameHash == IsMovingHash && parameter.type == AnimatorControllerParameterType.Bool)
                {
                    mask.hasIsMoving = true;
                }
                else if (parameter.nameHash == DirectionHash && parameter.type == AnimatorControllerParameterType.Int)
                {
                    mask.hasDirection = true;
                }
                else if (parameter.nameHash == AttackTriggerHash && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    mask.hasAttackTrigger = true;
                }
                else if (parameter.nameHash == DodgeTriggerHash && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    mask.hasDodgeTrigger = true;
                }
                else if (parameter.nameHash == DieHash && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    mask.hasDie = true;
                }
                else if (parameter.nameHash == DeadHash && parameter.type == AnimatorControllerParameterType.Bool)
                {
                    mask.hasDead = true;
                }
            }
        }

        animatorParameterMasks[animator] = mask;
        return mask;
    }

    private sealed class TotemPendingActorHide
    {
        public TotemActorModel actor;
        public float remainingSec;
    }

    private struct TotemAnimatorParameterMask
    {
        public bool hasIsMoving;
        public bool hasDirection;
        public bool hasAttackTrigger;
        public bool hasDodgeTrigger;
        public bool hasDie;
        public bool hasDead;
    }

    private static void DestroyObject(UnityEngine.Object obj)
    {
        if (obj == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(obj);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
