using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TotemEnemyWorldService : TotemRuntimeServiceBase, ITotemRuntimeTickService,
    ITotemEnemyRuntimeBridge, ITotemEnemyPathProvider, ITotemEnemySpawnGate,
    ITotemEnemyReachabilityProvider, ITotemEnemyObserver
{
    private const int FirstEnemyCombatantId = 100000;
    private const int RuntimeActiveEnemyCap = 80;
    private const int EnemyVisualPoolCapacity = 128;

    private readonly TotemEncounterService encounterService = new TotemEncounterService();
    private readonly List<TotemSpawnPlanEntry> dueSpawns = new List<TotemSpawnPlanEntry>(16);
    private readonly List<Vector3> participantPositions = new List<Vector3>(TotemActorService.ParticipantCount);
    private readonly List<Vector3> sameWavePositions = new List<Vector3>(16);
    private readonly Dictionary<int, EnemyVisualInstance> enemyObjects = new Dictionary<int, EnemyVisualInstance>(128);
    private readonly List<EnemyVisualInstance> inactiveEnemyVisuals = new List<EnemyVisualInstance>(EnemyVisualPoolCapacity);
    private readonly TotemEnemyInstanceSnapshot[] despawnBuffer = new TotemEnemyInstanceSnapshot[TotemEnemyService.DefaultEnemyCapacity];
    private readonly TotemEnemyModel[] activeEnemyBuffer = new TotemEnemyModel[TotemEnemyService.DefaultEnemyCapacity];

    private TotemGameFlowService flowService;
    private TotemMatchClockService matchClock;
    private TotemDataService dataService;
    private TotemAssetService assetService;
    private TotemMapService mapService;
    private TotemActorService actorService;
    private TotemParticipantReadinessService readinessService;
    private TotemCombatRelationshipService relationshipService;
    private TotemEnemyService enemyService;
    private TotemVfxService vfxService;
    private TotemAudioService audioService;
    private TotemSpawnPlan spawnPlan;
    private GameObject enemyRoot;
    private int nextEnemyCombatantId = FirstEnemyCombatantId;
    private int spawnRejectedCount;
    private int spawnedFromPlanCount;
    private int visualCreatedCount;
    private int visualReusedCount;
    private string lastSpawnRejection = string.Empty;

    public override string ServiceName => "EnemyWorld";

    public TotemSpawnPlan SpawnPlan => spawnPlan;

    public int ActiveVisualCount => enemyObjects.Count;

    public int PooledVisualCount => inactiveEnemyVisuals.Count;

    public int VisualCreatedCount => visualCreatedCount;

    public int VisualReusedCount => visualReusedCount;

    public bool HasVisualRoot => enemyRoot != null;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        matchClock = runtime.GetService<TotemMatchClockService>();
        dataService = runtime.GetService<TotemDataService>();
        assetService = runtime.GetService<TotemAssetService>();
        mapService = runtime.GetService<TotemMapService>();
        actorService = runtime.GetService<TotemActorService>();
        readinessService = runtime.GetService<TotemParticipantReadinessService>();
        relationshipService = runtime.GetService<TotemCombatRelationshipService>();
        enemyService = runtime.GetService<TotemEnemyService>();
        vfxService = runtime.GetService<TotemVfxService>();
        audioService = runtime.GetService<TotemAudioService>();

        var catalog = dataService?.GameplayCatalog ?? TotemDataService.LoadGameplayCatalogOrDefault();
        catalog.Normalize();
        enemyService?.RegisterCatalogDefinitions(
            catalog.CreateEnemyDefinitions(),
            catalog.CreateEnemyAbilityDefinitions(),
            catalog.CreateBossPhases());
        enemyService?.Configure(
            new TotemActorEnemyParticipantSource(actorService, readinessService, this),
            this,
            this,
            this,
            relationshipService,
            this);
        if (enemyService != null)
        {
            enemyService.EnemyDied += OnEnemyDied;
        }

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

        if (enemyService != null)
        {
            enemyService.EnemyDied -= OnEnemyDied;
        }

        ResetRun("Shutdown");
        DestroyVisualPool();
        flowService = null;
        matchClock = null;
        dataService = null;
        assetService = null;
        mapService = null;
        actorService = null;
        readinessService = null;
        relationshipService = null;
        enemyService = null;
        vfxService = null;
        audioService = null;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f
            || flowService?.CurrentState != TotemGameFlowState.CombatHud
            || matchClock == null
            || !matchClock.IsWorldActive
            || enemyService == null)
        {
            return;
        }

        if (spawnPlan == null)
        {
            BuildSpawnPlan();
        }

        TotemEnemyRuntimeSnapshot enemies = enemyService.CaptureSnapshot();
        dueSpawns.Clear();
        encounterService.Clock.CollectDueSpawns(
            new TotemEncounterClockContext(
                true,
                matchClock.WorldTime,
                enemies.lightCount,
                enemies.eliteCount,
                enemies.bossCount),
            dueSpawns);
        if (dueSpawns.Count <= 0)
        {
            return;
        }

        CaptureActiveParticipantPositions();
        sameWavePositions.Clear();
        for (int i = 0; i < dueSpawns.Count; i++)
        {
            TotemSpawnPlanEntry entry = dueSpawns[i];
            if (!encounterService.TryResolveSpawnPosition(
                    mapService?.CurrentMap,
                    entry,
                    participantPositions,
                    sameWavePositions,
                    out Vector3 position,
                    out TotemSpawnPlanRejectionReason rejection))
            {
                RecordSpawnRejected(entry, rejection);
                continue;
            }

            var request = new TotemEnemySpawnRequest(
                nextEnemyCombatantId++,
                entry.EnemyId,
                position,
                StableHash(entry.EncounterId),
                entry.AnchorId,
                matchClock.WorldTime);
            if (!enemyService.TrySpawn(request, out _, out TotemEnemySpawnBlockReason blockReason))
            {
                RecordSpawnRejected(entry, blockReason.ToString());
                continue;
            }

            sameWavePositions.Add(position);
            spawnedFromPlanCount++;
        }
    }

    public TotemEnemyWorldSnapshot CaptureSnapshot()
    {
        TotemEncounterClockSnapshot clock = encounterService.Clock.CaptureSnapshot();
        return new TotemEnemyWorldSnapshot
        {
            hasPlan = spawnPlan != null,
            planEntryCount = spawnPlan?.Entries?.Length ?? 0,
            planRejectionCount = spawnPlan?.Rejections?.Length ?? 0,
            spawnedFromPlanCount = spawnedFromPlanCount,
            spawnRejectedCount = spawnRejectedCount,
            visualObjectCount = enemyObjects.Count,
            lastSpawnRejection = lastSpawnRejection,
            processedPlanEntryCount = clock.processedEntryCount,
            worldTime = matchClock?.WorldTime ?? 0f,
        };
    }

    void ITotemEnemyRuntimeBridge.OnEnemySpawned(TotemEnemyModel enemy, string runtimeAssetKey)
    {
        if (enemy == null)
        {
            return;
        }

        EnsureEnemyRoot();
        string assetKey = runtimeAssetKey ?? string.Empty;
        EnemyVisualInstance visual;
        bool hasVisual = TryTakePooledVisual(assetKey, enemy.ThemeId, enemy.Tier, false, out visual);
        if (!hasVisual && assetService != null && !string.IsNullOrWhiteSpace(assetKey))
        {
            assetService.TryInstantiateGameObject(
                assetKey,
                enemyRoot.transform,
                enemy.Position,
                GetFallbackScale(enemy.Tier),
                out GameObject instance);
            if (instance != null)
            {
                visual = CreateVisualEntry(instance, assetKey, enemy.ThemeId, enemy.Tier, false);
                visualCreatedCount++;
                hasVisual = true;
            }
        }

        if (!hasVisual)
        {
            hasVisual = TryTakePooledVisual(string.Empty, enemy.ThemeId, enemy.Tier, true, out visual);
            if (!hasVisual)
            {
                visual = CreateVisualEntry(
                    CreateFallbackVisual(enemy),
                    string.Empty,
                    enemy.ThemeId,
                    enemy.Tier,
                    true);
                visualCreatedCount++;
            }

            enemy.VisualAssetKey = "primitive.enemy." + enemy.ThemeId + "." + enemy.Tier;
        }
        else
        {
            enemy.VisualAssetKey = assetKey;
        }

        ActivateVisual(ref visual, enemy);
        enemy.GameObject = visual.GameObject;
        enemyObjects[enemy.CombatantId] = visual;
    }

    void ITotemEnemyRuntimeBridge.OnEnemyDespawned(TotemEnemyModel enemy)
    {
        if (enemy == null || !enemyObjects.TryGetValue(enemy.CombatantId, out EnemyVisualInstance visual))
        {
            return;
        }

        enemyObjects.Remove(enemy.CombatantId);
        ReturnVisualToPool(visual);
        enemy.GameObject = null;
    }

    bool ITotemEnemyRuntimeBridge.TryMove(TotemEnemyModel enemy, Vector3 delta)
    {
        if (enemy == null || !enemy.IsAlive || delta.sqrMagnitude <= 0f)
        {
            return false;
        }

        float mapSize = mapService?.CurrentMap?.MapSize ?? TotemMapService.DefaultMapSize;
        Vector3 next = enemy.Position + delta;
        next.x = Mathf.Clamp(next.x, 0f, mapSize);
        next.z = Mathf.Clamp(next.z, 0f, mapSize);
        if (mapService != null && !mapService.IsWalkable(next))
        {
            return false;
        }

        enemy.Position = next;
        if (enemy.GameObject != null)
        {
            enemy.GameObject.transform.position = next;
        }

        return true;
    }

    bool ITotemEnemyRuntimeBridge.ResolveDamage(in TotemEnemyDamageCommand command)
    {
        return actorService != null
            && command.Target != null
            && actorService.TryApplyDamage(
                command.Target,
                command.Amount,
                command.Source,
                "EnemyAbility:" + command.AbilityId);
    }

    void ITotemEnemyRuntimeBridge.ApplyStatus(
        TotemEnemyModel source,
        TotemActorModel target,
        string statusId,
        float statusChance,
        string abilityId)
    {
        TotemStatusService statusService = Runtime?.GetService<TotemStatusService>();
        if (statusService == null || target == null || string.IsNullOrWhiteSpace(statusId))
        {
            return;
        }

        string normalized = statusId.Trim();
        float dps = string.Equals(normalized, TotemStatusService.BurnStatus, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, TotemStatusService.PoisonStatus, StringComparison.OrdinalIgnoreCase)
            ? 4f
            : string.Equals(normalized, TotemStatusService.ShockStatus, StringComparison.OrdinalIgnoreCase) ? 2f : 0f;
        float duration = string.Equals(normalized, TotemStatusService.StunStatus, StringComparison.OrdinalIgnoreCase) ? 1.25f : 3f;
        statusService.ApplyStatus(target, normalized, dps, duration, source, "EnemyAbility:" + abilityId);
    }

    void ITotemEnemyRuntimeBridge.SpawnProjectile(in TotemEnemyProjectileCommand command)
    {
        if (command.Target == null || !command.Target.IsAlive)
        {
            return;
        }

        vfxService?.SpawnSkillBurst(command.Target.Position, command.AbilityId, 0.8f);
    }

    void ITotemEnemyRuntimeBridge.CreateHazard(in TotemEnemyHazardCommand command)
    {
        vfxService?.SpawnSkillBurst(command.Position, command.AbilityId, command.Radius);
    }

    void ITotemEnemyRuntimeBridge.PlayCue(TotemEnemyModel enemy, string vfxId, string audioCueId)
    {
        if (enemy == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(vfxId))
        {
            vfxService?.SpawnSkillBurst(enemy.Position, vfxId, enemy.Tier == TotemEnemyTier.Boss ? 4f : 2f);
        }

        if (!string.IsNullOrWhiteSpace(audioCueId))
        {
            audioService?.PlaySfxCue(audioCueId, enemy.Position, "EnemyAbility");
        }
    }

    public bool TryBuildPath(Vector3 start, Vector3 destination, Vector3[] nodeBuffer, out int nodeCount)
    {
        nodeCount = 0;
        if (nodeBuffer == null || nodeBuffer.Length <= 0 || mapService == null)
        {
            return false;
        }

        if (mapService.IsWalkable(destination))
        {
            nodeBuffer[0] = destination;
            nodeCount = 1;
            return true;
        }

        for (int ring = 1; ring <= 4; ring++)
        {
            float radius = ring * 2f;
            for (int spoke = 0; spoke < 16; spoke++)
            {
                float angle = spoke * Mathf.PI * 2f / 16f;
                Vector3 candidate = destination + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                if (!mapService.IsWalkable(candidate))
                {
                    continue;
                }

                nodeBuffer[0] = candidate;
                nodeCount = 1;
                return true;
            }
        }

        return false;
    }

    public bool CanSpawn(int encounterInstanceId, string enemyId, int requestedCount, out TotemEnemySpawnBlockReason reason)
    {
        int active = enemyService?.CaptureSnapshot().aliveEnemyCount ?? 0;
        if (requestedCount <= 0 || active + requestedCount > RuntimeActiveEnemyCap)
        {
            reason = TotemEnemySpawnBlockReason.EncounterActiveCap;
            return false;
        }

        int encounterActiveCap = ResolveEncounterActiveCap(encounterInstanceId);
        if (encounterActiveCap > 0)
        {
            int activeInEncounter = 0;
            int count = enemyService?.CopyAliveEnemies(activeEnemyBuffer) ?? 0;
            for (int i = 0; i < count; i++)
            {
                if (activeEnemyBuffer[i]?.EncounterInstanceId == encounterInstanceId)
                {
                    activeInEncounter++;
                }
            }

            if (activeInEncounter + requestedCount > encounterActiveCap)
            {
                reason = TotemEnemySpawnBlockReason.EncounterActiveCap;
                return false;
            }
        }

        reason = TotemEnemySpawnBlockReason.None;
        return true;
    }

    private int ResolveEncounterActiveCap(int encounterInstanceId)
    {
        TotemSpawnPlanEntry[] entries = spawnPlan?.Entries;
        if (encounterInstanceId == 0 || entries == null)
        {
            return 0;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            TotemSpawnPlanEntry entry = entries[i];
            if (entry != null && StableHash(entry.EncounterId) == encounterInstanceId)
            {
                return Mathf.Max(0, entry.ActiveCap);
            }
        }

        return 0;
    }

    public bool IsReachable(TotemEnemyModel enemy, TotemActorModel participant)
    {
        return enemy != null
            && participant != null
            && (mapService == null
                || (mapService.IsWalkable(enemy.Position) && mapService.IsWalkable(participant.Position)));
    }

    public void OnStateChanged(in TotemEnemyStateChangedEvent evt)
    {
    }

    public void OnTargetChanged(in TotemEnemyTargetChangedEvent evt)
    {
    }

    public void OnAbilityChanged(in TotemEnemyAbilityEvent evt)
    {
    }

    public void OnBossPhaseChanged(in TotemBossPhaseChangedEvent evt)
    {
        if (evt.Enemy == null)
        {
            return;
        }

        vfxService?.SpawnSkillBurst(evt.Enemy.Position, evt.VfxId, 5f);
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            ResetRun("CombatHud.Enter");
            EnsureEnemyRoot();
            BuildSpawnPlan();
            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            ResetRun("CombatHud.Exit");
        }
    }

    private void BuildSpawnPlan()
    {
        TotemMapSnapshot map = mapService?.CurrentMap;
        var catalog = dataService?.GameplayCatalog ?? TotemDataService.LoadGameplayCatalogOrDefault();
        catalog.Normalize();
        CaptureActiveParticipantPositions();
        int seed = map?.Seed ?? 1;
        spawnPlan = encounterService.BuildSpawnPlan(
            map,
            map?.ThemeName,
            catalog.CreateEncounterSpawnDefinitions(),
            catalog.CreateEnemyDefinitions(),
            seed,
            participantPositions);
        GFTrace.Success("TotemEncounter", "SpawnPlan.Built", null, GFTrace.Data(
            "seed", seed.ToString(),
            "theme", spawnPlan?.ThemeId ?? string.Empty,
            "entryCount", (spawnPlan?.Entries?.Length ?? 0).ToString(),
            "rejectionCount", (spawnPlan?.Rejections?.Length ?? 0).ToString()));
    }

    private void CaptureActiveParticipantPositions()
    {
        participantPositions.Clear();
        var actors = actorService?.Actors;
        if (actors == null)
        {
            return;
        }

        for (int i = 0; i < actors.Count; i++)
        {
            TotemActorModel actor = actors[i];
            if (actor != null
                && actor.IsAlive
                && (readinessService == null || readinessService.CountsAsAlive(actor)))
            {
                participantPositions.Add(actor.Position);
            }
        }
    }

    private void OnEnemyDied(TotemEnemyDiedEvent evt)
    {
        if (evt.Enemy == null)
        {
            return;
        }

        vfxService?.SpawnSkillBurst(evt.Enemy.Position, "enemy_death_" + evt.Enemy.ThemeId, evt.Enemy.Tier == TotemEnemyTier.Boss ? 5f : 1.5f);
        enemyService?.Despawn(evt.Enemy.CombatantId, "DeathResolved");
    }

    private void ResetRun(string reason)
    {
        if (enemyService != null)
        {
            int count = enemyService.CopyInstanceSnapshots(despawnBuffer);
            for (int i = 0; i < count; i++)
            {
                if (despawnBuffer[i] != null)
                {
                    enemyService.Despawn(despawnBuffer[i].combatantId, reason);
                }
            }
        }

        foreach (EnemyVisualInstance visual in enemyObjects.Values)
        {
            ReturnVisualToPool(visual);
        }
        enemyObjects.Clear();
        spawnPlan = null;
        nextEnemyCombatantId = FirstEnemyCombatantId;
        spawnRejectedCount = 0;
        spawnedFromPlanCount = 0;
        lastSpawnRejection = string.Empty;
        dueSpawns.Clear();
        participantPositions.Clear();
        sameWavePositions.Clear();
    }

    private void EnsureEnemyRoot()
    {
        if (enemyRoot == null)
        {
            enemyRoot = new GameObject("[TotemEnemies]");
        }
    }

    private GameObject CreateFallbackVisual(TotemEnemyModel enemy)
    {
        PrimitiveType primitive = enemy.Tier == TotemEnemyTier.Boss
            ? PrimitiveType.Cube
            : enemy.Tier == TotemEnemyTier.Elite ? PrimitiveType.Cylinder : PrimitiveType.Capsule;
        GameObject instance = GameObject.CreatePrimitive(primitive);
        instance.transform.SetParent(enemyRoot.transform, false);
        instance.transform.position = enemy.Position;
        instance.transform.localScale = GetFallbackScale(enemy.Tier);
        Renderer renderer = instance.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                var material = new Material(shader);
                Color color = GetThemeColor(enemy.ThemeId, enemy.Tier);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                renderer.material = material;
            }
        }
        return instance;
    }

    private static EnemyVisualInstance CreateVisualEntry(
        GameObject instance,
        string assetKey,
        string themeId,
        TotemEnemyTier tier,
        bool isFallback)
    {
        return new EnemyVisualInstance(
            instance,
            assetKey,
            themeId,
            tier,
            isFallback,
            instance.transform.localRotation,
            instance.transform.localScale);
    }

    private bool TryTakePooledVisual(
        string assetKey,
        string themeId,
        TotemEnemyTier tier,
        bool isFallback,
        out EnemyVisualInstance visual)
    {
        for (int i = inactiveEnemyVisuals.Count - 1; i >= 0; i--)
        {
            EnemyVisualInstance candidate = inactiveEnemyVisuals[i];
            if (candidate.GameObject == null)
            {
                RemovePooledVisualAt(i);
                continue;
            }

            bool matches = candidate.IsFallback == isFallback
                && candidate.Tier == tier
                && (isFallback
                    ? string.Equals(candidate.ThemeId, themeId, StringComparison.Ordinal)
                    : string.Equals(candidate.AssetKey, assetKey, StringComparison.Ordinal));
            if (!matches)
            {
                continue;
            }

            visual = candidate;
            RemovePooledVisualAt(i);
            visualReusedCount++;
            return true;
        }

        visual = default;
        return false;
    }

    private void RemovePooledVisualAt(int index)
    {
        int lastIndex = inactiveEnemyVisuals.Count - 1;
        inactiveEnemyVisuals[index] = inactiveEnemyVisuals[lastIndex];
        inactiveEnemyVisuals.RemoveAt(lastIndex);
    }

    private void ActivateVisual(ref EnemyVisualInstance visual, TotemEnemyModel enemy)
    {
        GameObject instance = visual.GameObject;
        Transform visualTransform = instance.transform;
        visualTransform.SetParent(enemyRoot.transform, false);
        visualTransform.localRotation = visual.LocalRotation;
        visualTransform.localScale = visual.LocalScale;
        visualTransform.position = enemy.Position;
        instance.name = $"TotemEnemy_{enemy.CombatantId}_{enemy.EnemyId}";
        if (!instance.activeSelf)
        {
            instance.SetActive(true);
        }
    }

    private void ReturnVisualToPool(EnemyVisualInstance visual)
    {
        if (visual.GameObject == null)
        {
            return;
        }

        visual.GameObject.SetActive(false);
        if (inactiveEnemyVisuals.Count < EnemyVisualPoolCapacity)
        {
            inactiveEnemyVisuals.Add(visual);
            return;
        }

        DestroyObject(visual.GameObject);
    }

    private void DestroyVisualPool()
    {
        foreach (EnemyVisualInstance visual in enemyObjects.Values)
        {
            DestroyObject(visual.GameObject);
        }
        enemyObjects.Clear();

        for (int i = 0; i < inactiveEnemyVisuals.Count; i++)
        {
            DestroyObject(inactiveEnemyVisuals[i].GameObject);
        }
        inactiveEnemyVisuals.Clear();
        DestroyObject(enemyRoot);
        enemyRoot = null;
    }

    private static Vector3 GetFallbackScale(TotemEnemyTier tier)
    {
        switch (tier)
        {
            case TotemEnemyTier.Boss: return new Vector3(3.2f, 3.2f, 3.2f);
            case TotemEnemyTier.Elite: return new Vector3(1.5f, 1.8f, 1.5f);
            default: return new Vector3(0.85f, 0.85f, 0.85f);
        }
    }

    private static Color GetThemeColor(string themeId, TotemEnemyTier tier)
    {
        float tierBoost = tier == TotemEnemyTier.Boss ? 0.2f : tier == TotemEnemyTier.Elite ? 0.1f : 0f;
        if (!string.IsNullOrWhiteSpace(themeId) && themeId.IndexOf("virus", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return new Color(0.35f + tierBoost, 0.85f, 0.25f);
        }
        if (!string.IsNullOrWhiteSpace(themeId) && themeId.IndexOf("alien", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return new Color(0.65f + tierBoost, 0.25f, 0.85f);
        }
        if (!string.IsNullOrWhiteSpace(themeId) && themeId.IndexOf("ai", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return new Color(0.2f, 0.65f + tierBoost, 1f);
        }
        return new Color(0.85f, 0.3f + tierBoost, 0.2f);
    }

    private void RecordSpawnRejected(TotemSpawnPlanEntry entry, TotemSpawnPlanRejectionReason reason)
    {
        RecordSpawnRejected(entry, reason.ToString());
    }

    private void RecordSpawnRejected(TotemSpawnPlanEntry entry, string reason)
    {
        spawnRejectedCount++;
        lastSpawnRejection = reason ?? string.Empty;
        GFTrace.Warning("TotemEncounter", "Spawn.Rejected", null, GFTrace.Data(
            "entryId", entry?.PlanEntryId ?? string.Empty,
            "enemyId", entry?.EnemyId ?? string.Empty,
            "reason", lastSpawnRejection,
            "worldTime", (matchClock?.WorldTime ?? 0f).ToString("F3")));
    }

    private static int StableHash(string value)
    {
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < (value?.Length ?? 0); i++) hash = hash * 31 + value[i];
            return hash;
        }
    }

    private static void DestroyObject(UnityEngine.Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) UnityEngine.Object.Destroy(value);
        else UnityEngine.Object.DestroyImmediate(value);
    }

    private readonly struct EnemyVisualInstance
    {
        public readonly GameObject GameObject;
        public readonly string AssetKey;
        public readonly string ThemeId;
        public readonly TotemEnemyTier Tier;
        public readonly bool IsFallback;
        public readonly Quaternion LocalRotation;
        public readonly Vector3 LocalScale;

        public EnemyVisualInstance(
            GameObject gameObject,
            string assetKey,
            string themeId,
            TotemEnemyTier tier,
            bool isFallback,
            Quaternion localRotation,
            Vector3 localScale)
        {
            GameObject = gameObject;
            AssetKey = assetKey ?? string.Empty;
            ThemeId = themeId ?? string.Empty;
            Tier = tier;
            IsFallback = isFallback;
            LocalRotation = localRotation;
            LocalScale = localScale;
        }
    }
}

[Serializable]
public sealed class TotemEnemyWorldSnapshot
{
    public bool hasPlan;
    public int planEntryCount;
    public int planRejectionCount;
    public int processedPlanEntryCount;
    public int spawnedFromPlanCount;
    public int spawnRejectedCount;
    public int visualObjectCount;
    public string lastSpawnRejection;
    public float worldTime;
}
