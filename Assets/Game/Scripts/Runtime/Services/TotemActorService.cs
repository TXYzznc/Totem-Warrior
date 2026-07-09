using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TotemActorService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    public const int SmartAiCount = 20;
    public const int LightAiCount = 29;
    public const int RuntimeActorCountWithoutBoss = 50;
    public const float CoverIncomingDamageMultiplier = 0.6f;
    public const float CoverMeleeBypassDistance = 4f;
    private const float DeathHideDelay = 0.75f;
    private const float TerrainEffectTickInterval = 0.2f;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int DirectionHash = Animator.StringToHash("Direction");
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
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
    private TotemEnemyDefinition[] enemyDefinitions = Array.Empty<TotemEnemyDefinition>();
    private GameObject actorRoot;
    private int damageSequence;
    private float terrainEffectAccumulator;
    private int terrainHazardHitCount;
    private float lastTerrainHazardDamageTick;
    private int terrainCoverReducedHitCount;
    private float lastTerrainCoverDamageBefore;
    private float lastTerrainCoverDamageAfter;

    public override string ServiceName => "Actor";

    public IReadOnlyList<TotemActorModel> Actors => actors;

    public TotemActorModel Player { get; private set; }

    public TotemActorModel Boss { get; private set; }

    public event Action<TotemActorModel, float, bool> DamageApplied;

    public event Action<TotemDamageRecord> DamageResolved;

    public TotemDamageRecord LastDamage { get; private set; }

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        mapService = runtime.GetService<TotemMapService>();
        assetService = runtime.GetService<TotemAssetService>();
        enemyDefinitions = NonEmpty(runtime.GetService<TotemDataService>()?.GameplayCatalog?.CreateEnemyDefinitions(), LoadEnemyDefinitions());
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
        enemyDefinitions = Array.Empty<TotemEnemyDefinition>();
        DamageApplied = null;
        DamageResolved = null;
        LastDamage = default;
        damageSequence = 0;
    }

    public void Tick(float deltaTime)
    {
        TickMovementAnimations();
        TickPendingActorHides(deltaTime);
        TickTerrainEffects(deltaTime);
    }

    public TotemActorSnapshot CaptureActorSnapshot()
    {
        var snapshot = new TotemActorSnapshot();
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
                    break;
                case TotemActorKind.SmartAi:
                    snapshot.smartAiCount++;
                    if (actor.IsAlive)
                    {
                        snapshot.aliveEnemyCount++;
                    }
                    break;
                case TotemActorKind.LightAi:
                    snapshot.lightAiCount++;
                    if (actor.IsAlive)
                    {
                        snapshot.aliveEnemyCount++;
                    }
                    break;
                case TotemActorKind.Boss:
                    snapshot.bossCount++;
                    if (actor.IsAlive)
                    {
                        snapshot.aliveEnemyCount++;
                    }
                    break;
            }
        }

        snapshot.actorCount = snapshot.playerCount + snapshot.smartAiCount + snapshot.lightAiCount;
        snapshot.terrainHazardHitCount = terrainHazardHitCount;
        snapshot.lastTerrainHazardDamageTick = lastTerrainHazardDamageTick;
        snapshot.terrainCoverReducedHitCount = terrainCoverReducedHitCount;
        snapshot.lastTerrainCoverDamageBefore = lastTerrainCoverDamageBefore;
        snapshot.lastTerrainCoverDamageAfter = lastTerrainCoverDamageAfter;
        return snapshot;
    }

    public TotemActorModel FindClosestAliveEnemy(Vector3 origin)
    {
        TotemActorModel best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < actors.Count; i++)
        {
            var actor = actors[i];
            if (!IsEnemy(actor) || !actor.IsAlive)
            {
                continue;
            }

            float sqrDistance = (actor.Position - origin).sqrMagnitude;
            if (sqrDistance < bestDistance)
            {
                bestDistance = sqrDistance;
                best = actor;
            }
        }

        return best;
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

    public bool ApplyDamage(TotemActorModel target, float amount, TotemActorModel source = null, string reason = null)
    {
        if (target == null || amount <= 0f || !target.IsAlive)
        {
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

    public float ResolveTerrainAdjustedDamage(TotemActorModel source, TotemActorModel target, float amount, string reason = null)
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

    public void SpawnActors(TotemMapSnapshot map, TotemStartupSelection selection, bool createObjects)
    {
        DespawnActors();
        actorRoot = createObjects ? new GameObject("[TotemActors]") : null;
        if (actorRoot != null)
        {
            spawnedObjects.Add(actorRoot);
        }

        var spawnInfos = BuildActorRoster(map, selection, enemyDefinitions);
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
            else if (actor.Kind == TotemActorKind.Boss)
            {
                Boss = actor;
            }
        }

        var snapshot = CaptureActorSnapshot();
        GFTrace.Success("TotemActor", "Actors.Spawned", null, GFTrace.Data(
            "actorCount", snapshot.actorCount.ToString(),
            "smartAi", snapshot.smartAiCount.ToString(),
            "lightAi", snapshot.lightAiCount.ToString(),
            "boss", snapshot.bossCount.ToString(),
            "enemyRows", (enemyDefinitions?.Length ?? 0).ToString()));
    }

    public static TotemActorSpawnInfo[] BuildActorRoster(TotemMapSnapshot map, TotemStartupSelection selection)
    {
        return BuildActorRoster(map, selection, LoadEnemyDefinitions());
    }

    public static TotemActorSpawnInfo[] BuildActorRoster(
        TotemMapSnapshot map,
        TotemStartupSelection selection,
        IReadOnlyList<TotemEnemyDefinition> enemyDefinitions)
    {
        if (map == null)
        {
            map = TotemMapService.BuildLayout(seed: 1, themeId: 1);
        }

        Vector3 playerPosition = TotemMapService.ResolveAnchorPosition(
            map,
            TotemMapAnchorKind.PlayerSpawn,
            FindRoom(map, TotemRoomType.SpawnRoom)?.CenterWorld ?? new Vector3(82f, 0f, 82f));
        playerPosition.y = 0.5f;
        Vector3 bossPosition = TotemMapService.ResolveAnchorPosition(
            map,
            TotemMapAnchorKind.BossSpawn,
            FindRoom(map, TotemRoomType.BossRoom)?.CenterWorld ?? new Vector3(324f, 0f, 82f));
        bossPosition.y = 0.5f;

        var result = new TotemActorSpawnInfo[RuntimeActorCountWithoutBoss + 1];
        int cursor = 0;
        result[cursor++] = new TotemActorSpawnInfo
        {
            ActorId = selection != null && selection.CharacterId > 0 ? selection.CharacterId : 1,
            Name = "Player",
            Kind = TotemActorKind.Player,
            Position = playerPosition,
            MaxHealth = 100f,
        };

        int enemyIndex = 0;
        int[] ringCounts = { 14, 17, 18 };
        float[] fallbackRingRadii = { 8f, 13f, 18f };
        float[] anchoredGroupRadii = { 0.75f, 0.9f, 1.05f };
        var enemyAnchors = TotemMapService.FindAnchors(map, TotemMapAnchorKind.EnemySpawn);
        for (int ring = 0; ring < ringCounts.Length && enemyIndex < SmartAiCount + LightAiCount; ring++)
        {
            int count = ringCounts[ring];
            var enemyAnchor = FindEnemySpawnAnchor(enemyAnchors, ring);
            Vector3 groupCenter = enemyAnchor == null ? playerPosition : enemyAnchor.Position;
            groupCenter.y = 0.5f;
            float radius = enemyAnchor == null ? fallbackRingRadii[ring] : anchoredGroupRadii[ring];
            for (int slot = 0; slot < count && enemyIndex < SmartAiCount + LightAiCount; slot++)
            {
                bool smart = enemyIndex < SmartAiCount;
                float angle = (slot + ring * 0.3f) * Mathf.PI * 2f / count;
                var desiredPosition = new Vector3(
                    groupCenter.x + Mathf.Cos(angle) * radius,
                    0.5f,
                    groupCenter.z + Mathf.Sin(angle) * radius);
                var position = ResolveWalkableEnemySpawnPosition(map, desiredPosition, groupCenter, playerPosition);

                var definition = ResolveEnemyDefinition(smart ? TotemActorKind.SmartAi : TotemActorKind.LightAi, map.ThemeName, enemyDefinitions);
                var spawnInfo = new TotemActorSpawnInfo
                {
                    ActorId = enemyIndex + 2,
                    Name = smart ? $"SmartAI{enemyIndex + 1:00}" : $"LightAI{enemyIndex - SmartAiCount + 1:00}",
                    Kind = smart ? TotemActorKind.SmartAi : TotemActorKind.LightAi,
                    Position = position,
                    MaxHealth = definition?.BaseHP ?? 50f,
                };
                ApplyEnemyDefinition(spawnInfo, definition);
                result[cursor++] = spawnInfo;
                enemyIndex++;
            }
        }

        var bossDefinition = ResolveEnemyDefinition(TotemActorKind.Boss, map.ThemeName, enemyDefinitions);
        var bossInfo = new TotemActorSpawnInfo
        {
            ActorId = 1000,
            Name = "Boss",
            Kind = TotemActorKind.Boss,
            Position = bossPosition,
            MaxHealth = bossDefinition?.BaseHP ?? 300f,
        };
        ApplyEnemyDefinition(bossInfo, bossDefinition);
        result[cursor] = bossInfo;

        return result;
    }

    private static TotemMapAnchor FindEnemySpawnAnchor(IReadOnlyList<TotemMapAnchor> anchors, int groupIndex)
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

    private static Vector3 ResolveWalkableEnemySpawnPosition(TotemMapSnapshot map, Vector3 desiredPosition, Vector3 groupCenter, Vector3 fallback)
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

    public static TotemEnemyDefinition ResolveEnemyDefinition(
        TotemActorKind kind,
        string themeName,
        IReadOnlyList<TotemEnemyDefinition> definitions)
    {
        var tier = ToEnemyTier(kind);
        if (definitions == null || definitions.Count <= 0 || tier == TotemEnemyTier.Unknown)
        {
            return null;
        }

        TotemEnemyDefinition commonMatch = null;
        TotemEnemyDefinition firstTierMatch = null;
        for (int i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            if (definition == null || definition.Tier != tier)
            {
                continue;
            }

            firstTierMatch ??= definition;
            if (IsThemeMatch(definition.ThemeId, themeName))
            {
                return definition;
            }

            if (string.Equals(definition.ThemeId, "common", StringComparison.OrdinalIgnoreCase))
            {
                commonMatch ??= definition;
            }
        }

        return commonMatch ?? firstTierMatch;
    }

    public static bool IsEnemy(TotemActorModel actor)
    {
        return actor != null && actor.Kind != TotemActorKind.Player;
    }

    private static TotemEnemyTier ToEnemyTier(TotemActorKind kind)
    {
        switch (kind)
        {
            case TotemActorKind.SmartAi:
                return TotemEnemyTier.Elite;
            case TotemActorKind.LightAi:
                return TotemEnemyTier.Light;
            case TotemActorKind.Boss:
                return TotemEnemyTier.Boss;
            default:
                return TotemEnemyTier.Unknown;
        }
    }

    private static bool IsThemeMatch(string definitionTheme, string mapTheme)
    {
        return !string.IsNullOrWhiteSpace(definitionTheme)
            && !string.IsNullOrWhiteSpace(mapTheme)
            && string.Equals(definitionTheme, mapTheme, StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyEnemyDefinition(TotemActorSpawnInfo spawnInfo, TotemEnemyDefinition definition)
    {
        if (spawnInfo == null || definition == null)
        {
            return;
        }

        spawnInfo.EnemyId = definition.EnemyId;
        spawnInfo.DisplayName = definition.DisplayName;
        spawnInfo.ThemeId = definition.ThemeId;
        spawnInfo.EnemyTier = definition.Tier;
        spawnInfo.MaxHealth = definition.BaseHP;
        spawnInfo.HpCurveK = definition.HPCurveK;
        spawnInfo.BaseDamage = definition.BaseDamage;
        spawnInfo.DamageCurveK = definition.DamageCurveK;
        spawnInfo.MoveSpeed = definition.MoveSpeed;
        spawnInfo.AttackRange = definition.AttackRange;
        spawnInfo.DetectRange = definition.DetectRange;
        spawnInfo.SkillIds = definition.SkillIds;
        spawnInfo.LootTableId = definition.LootTableId;
        spawnInfo.GuaranteedLootIds = definition.GuaranteedLootIds;
        spawnInfo.ElitePaintDropRare = definition.ElitePaintDropRare;
        spawnInfo.XPReward = definition.XPReward;
        spawnInfo.CoinRewardMin = definition.CoinRewardMin;
        spawnInfo.CoinRewardMax = definition.CoinRewardMax;
        spawnInfo.PoolIds = definition.PoolIds;
    }

    private static TotemEnemyDefinition[] LoadEnemyDefinitions()
    {
        return NonEmpty(
            TotemDataService.LoadGameplayCatalogOrDefault().CreateEnemyDefinitions(),
            Array.Empty<TotemEnemyDefinition>());
    }

    private static T[] NonEmpty<T>(T[] primary, T[] fallback)
    {
        return primary == null || primary.Length <= 0 ? fallback : primary;
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
            var map = mapService?.CurrentMap ?? TotemMapService.BuildLayout(seed: 1, themeId: 1);
            SpawnActors(map, flowService?.StartupSelection, createObjects: true);
            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            DespawnActors();
            LastDamage = default;
            damageSequence = 0;
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

        PrimitiveType primitive = actor.Kind == TotemActorKind.Boss ? PrimitiveType.Cube : PrimitiveType.Capsule;
        var go = GameObject.CreatePrimitive(primitive);
        go.name = $"Totem_{actor.Name}";
        go.transform.SetParent(actorRoot.transform, false);
        go.transform.position = actor.Position;
        go.transform.localScale = GetActorScale(actor.Kind);
        SetColor(go, GetActorColor(actor.Kind));
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
            case TotemActorKind.Boss:
                return "actor.boss";
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
        Boss = null;
        actorRoot = null;
        LastDamage = default;
        damageSequence = 0;
        terrainEffectAccumulator = 0f;
        terrainHazardHitCount = 0;
        lastTerrainHazardDamageTick = 0f;
        terrainCoverReducedHitCount = 0;
        lastTerrainCoverDamageBefore = 0f;
        lastTerrainCoverDamageAfter = 0f;
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
            case TotemActorKind.Boss:
                return new Vector3(2.0f, 2.0f, 2.0f);
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
            case TotemActorKind.Boss:
                return new Color(0.65f, 0.15f, 0.85f);
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
