using System;
using System.Collections.Generic;
using UnityEngine;

public enum TotemCombatRosterMode
{
    FullMatch = 0,
    PlayerOnlyPreview = 1,
}

public sealed class TotemActorService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    public const int SmartAiCount = 20;
    public const int LightAiCount = 29;
    public const int ParticipantCount = 50;
    public const float CoverIncomingDamageMultiplier = 0.6f;
    public const float CoverMeleeBypassDistance = 4f;
    public const float ParticipantSpawnMinDistance = 18f;
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

    public bool CanEnemyTarget(TotemActorModel target)
    {
        var readiness = Runtime?.GetService<TotemParticipantReadinessService>();
        return target != null
            && target.IsAlive
            && (readiness == null || !IsParticipantActor(target) || readiness.CanBeTargeted(target))
            && (!playerStartupInvulnerable || target != Player);
    }

    public event Action<TotemActorModel, float, bool> DamageApplied;

    public event Action<TotemDamageRecord> DamageResolved;

    public TotemDamageRecord LastDamage { get; private set; }

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        mapService = runtime.GetService<TotemMapService>();
        assetService = runtime.GetService<TotemAssetService>();
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
            flowService = null;
        }

        DespawnActors();
        assetService = null;
        playerStartupInvulnerable = false;
        playerStartupDamageBlockedCount = 0;
        playerStartupProtectionReason = string.Empty;
        nextCombatRosterMode = TotemCombatRosterMode.FullMatch;
        DamageApplied = null;
        DamageResolved = null;
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

    public void MoveActor(TotemActorModel actor, Vector3 delta)
    {
        if (actor == null || !actor.IsAlive || delta.sqrMagnitude <= 0f)
        {
            return;
        }

        float mapSize = mapService?.CurrentMap?.MapSize ?? TotemMapService.DefaultMapSize;
        Vector3 previous = actor.Position;
        if (mapService != null)
        {
            delta *= mapService.GetMoveSpeedMultiplier(previous);
        }

        var next = actor.Position + delta;
        next.x = Mathf.Clamp(next.x, 0f, mapSize);
        next.z = Mathf.Clamp(next.z, 0f, mapSize);
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

        target.ApplyDamage(amount);
        bool killed = !target.IsAlive;
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
            Amount = amount,
            Killed = killed,
            Reason = string.IsNullOrWhiteSpace(reason) ? "Damage" : reason,
            TargetHealthAfter = target.Health,
        };
        DamageApplied?.Invoke(target, amount, killed);
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

        if (FlatDistance(source.Position, target.Position) <= CoverMeleeBypassDistance)
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
        TotemStartupSelection selection,
        bool createObjects,
        TotemCombatRosterMode rosterMode = TotemCombatRosterMode.FullMatch)
    {
        DespawnActors();
        actorRoot = createObjects ? new GameObject("[TotemActors]") : null;
        if (actorRoot != null)
        {
            spawnedObjects.Add(actorRoot);
        }

        var spawnInfos = BuildActorRoster(map, selection, rosterMode);
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
    }

    public static TotemActorSpawnInfo[] BuildActorRoster(
        TotemMapSnapshot map,
        TotemStartupSelection selection,
        TotemCombatRosterMode rosterMode = TotemCombatRosterMode.FullMatch)
    {
        if (map == null)
        {
            map = TotemMapService.BuildLayout(seed: 1, themeId: 1);
        }

        int spawnSelectionSeed = unchecked((int)DateTime.UtcNow.Ticks ^ map.Seed ^
                                            ((selection?.CharacterId ?? 1) * 486187739));
        Vector3 playerPosition = TotemMapService.ResolveRandomAnchorPosition(
            map,
            TotemMapAnchorKind.PlayerSpawn,
            FindRoom(map, TotemRoomType.SpawnRoom)?.CenterWorld ?? new Vector3(82f, 0f, 82f),
            new System.Random(spawnSelectionSeed));
        playerPosition.y = 0.5f;
        var participantPositions = new List<Vector3>(ParticipantCount);
        playerPosition = ResolveParticipantSpawnPosition(map, playerPosition, playerPosition, participantPositions, 0);
        participantPositions.Add(playerPosition);
        var playerSpawnInfo = new TotemActorSpawnInfo
        {
            ActorId = selection != null && selection.CharacterId > 0 ? selection.CharacterId : 1,
            Name = "Player",
            Kind = TotemActorKind.Player,
            ControllerKind = TotemParticipantControllerKind.Human,
            Position = playerPosition,
            MaxHealth = 100f,
        };
        if (rosterMode == TotemCombatRosterMode.PlayerOnlyPreview)
        {
            return new[] { playerSpawnInfo };
        }

        var result = new TotemActorSpawnInfo[ParticipantCount];
        int cursor = 0;
        result[cursor++] = playerSpawnInfo;

        int participantIndex = 0;
        int[] ringCounts = { 14, 17, 18 };
        float[] fallbackRingRadii = { 8f, 13f, 18f };
        float[] anchoredGroupRadii = { 0.75f, 0.9f, 1.05f };
        var participantAnchors = TotemMapService.FindAnchors(map, TotemMapAnchorKind.EnemySpawn);
        for (int ring = 0; ring < ringCounts.Length && participantIndex < SmartAiCount + LightAiCount; ring++)
        {
            int count = ringCounts[ring];
            var participantAnchor = FindParticipantSpawnAnchor(participantAnchors, ring);
            Vector3 groupCenter = participantAnchor == null ? playerPosition : participantAnchor.Position;
            groupCenter.y = 0.5f;
            float radius = participantAnchor == null ? fallbackRingRadii[ring] : anchoredGroupRadii[ring];
            for (int slot = 0; slot < count && participantIndex < SmartAiCount + LightAiCount; slot++)
            {
                bool smart = participantIndex < SmartAiCount;
                float angle = (slot + ring * 0.3f) * Mathf.PI * 2f / count;
                var desiredPosition = new Vector3(
                    groupCenter.x + Mathf.Cos(angle) * radius,
                    0.5f,
                    groupCenter.z + Mathf.Sin(angle) * radius);
                var position = ResolveWalkableParticipantSpawnPosition(map, desiredPosition, groupCenter, playerPosition);
                position = ResolveParticipantSpawnPosition(map, position, groupCenter, participantPositions, participantIndex + 1);

                var spawnInfo = new TotemActorSpawnInfo
                {
                    ActorId = participantIndex + 2,
                    Name = smart ? $"SmartAI{participantIndex + 1:00}" : $"LightAI{participantIndex - SmartAiCount + 1:00}",
                    Kind = smart ? TotemActorKind.SmartAi : TotemActorKind.LightAi,
                    ControllerKind = smart ? TotemParticipantControllerKind.SmartBot : TotemParticipantControllerKind.LightBot,
                    Position = position,
                    MaxHealth = 100f,
                };
                result[cursor++] = spawnInfo;
                participantPositions.Add(position);
                participantIndex++;
            }
        }

        return result;
    }

    private static TotemMapAnchor FindParticipantSpawnAnchor(IReadOnlyList<TotemMapAnchor> anchors, int groupIndex)
    {
        if (anchors == null || anchors.Count <= 0)
        {
            return null;
        }

        string payloadId = groupIndex == 0 ? "inner" : groupIndex == 1 ? "mid" : "outer";
        for (int i = 0; i < anchors.Count; i++)
        {
            var anchor = anchors[i];
            if (anchor != null && string.Equals(anchor.PayloadId, payloadId, StringComparison.Ordinal))
            {
                return anchor;
            }
        }

        return groupIndex >= 0 && groupIndex < anchors.Count ? anchors[groupIndex] : null;
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
        int salt)
    {
        desiredPosition = ClampSpawnPosition(map, desiredPosition);
        desiredPosition.y = 0.5f;
        if (IsValidParticipantSpawnPosition(map, desiredPosition, occupiedPositions, ParticipantSpawnMinDistance))
        {
            return desiredPosition;
        }

        Vector3 bestPosition = desiredPosition;
        float bestDistance = GetNearestParticipantDistance(desiredPosition, occupiedPositions);
        bool hasWalkableCandidate = IsParticipantSpawnArea(map, desiredPosition);
        for (int attempt = 0; attempt < ParticipantSpawnRadialAttempts; attempt++)
        {
            int ring = attempt / 24 + 1;
            float radius = ParticipantSpawnMinDistance + ring * ParticipantSpawnSearchStep;
            float angle = (attempt * 137.50777f + salt * 23.711f) * Mathf.Deg2Rad;
            var candidate = new Vector3(
                groupCenter.x + Mathf.Cos(angle) * radius,
                0.5f,
                groupCenter.z + Mathf.Sin(angle) * radius);
            if (TryEvaluateParticipantSpawnCandidate(map, candidate, occupiedPositions, ref bestPosition, ref bestDistance, ref hasWalkableCandidate))
            {
                return bestPosition;
            }
        }

        float mapSize = map?.MapSize ?? TotemMapService.DefaultMapSize;
        for (int attempt = 0; attempt < ParticipantSpawnGlobalAttempts; attempt++)
        {
            float x = DeterministicUnit(salt, attempt, 0) * mapSize;
            float z = DeterministicUnit(salt, attempt, 1) * mapSize;
            var candidate = new Vector3(x, 0.5f, z);
            if (TryEvaluateParticipantSpawnCandidate(map, candidate, occupiedPositions, ref bestPosition, ref bestDistance, ref hasWalkableCandidate))
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
        ref Vector3 bestPosition,
        ref float bestDistance,
        ref bool hasWalkableCandidate)
    {
        candidate = ClampSpawnPosition(map, candidate);
        candidate.y = 0.5f;
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

        return nearestDistance >= ParticipantSpawnMinDistance;
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

        float initialRadius = Mathf.Max(0f, map.MapSize * 0.5f);
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
        float mapSize = map?.MapSize ?? TotemMapService.DefaultMapSize;
        position.x = Mathf.Clamp(position.x, 0f, mapSize);
        position.z = Mathf.Clamp(position.z, 0f, mapSize);
        return position;
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
            SpawnActors(map, flowService?.StartupSelection, createObjects: true, rosterMode: rosterMode);
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

    private GameObject CreateActorObject(TotemActorModel actor)
    {
        string assetKey = GetActorAssetKey(actor);
        string usedAssetKey = assetKey;
        bool instantiated = TryInstantiateActorObject(usedAssetKey, actor, out var instance);
        if (!instantiated && actor.Kind == TotemActorKind.Player && !string.Equals(assetKey, "actor.player", StringComparison.Ordinal))
        {
            usedAssetKey = "actor.player";
            instantiated = TryInstantiateActorObject(usedAssetKey, actor, out instance);
        }

        if (instantiated)
        {
            instance.name = $"Totem_{actor.Name}";
            actor.VisualAssetKey = usedAssetKey;
            TotemActorVisualHelper.AttachActorVisuals(instance, actor.Kind);
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
        switch (characterId)
        {
            case 1:
                return "actor.player.1";
            case 2:
                return "actor.player.2";
            case 3:
                return "actor.player.3";
            default:
                return "actor.player";
        }
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
                return "actor.player";
            case TotemActorKind.SmartAi:
                return "actor.smartAi";
            case TotemActorKind.LightAi:
                return "actor.lightAi";
            default:
                return string.Empty;
        }
    }

    private void DespawnActors()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
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
