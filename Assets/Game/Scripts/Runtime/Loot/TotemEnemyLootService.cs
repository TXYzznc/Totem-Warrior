using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TotemEnemyLootService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    public const float PickupRadius = 2.5f;
    private const int PickupVisualPoolCapacity = 128;

    private readonly TotemEnemyLootGenerator generator = new TotemEnemyLootGenerator();
    private readonly List<TotemLootPickupModel> activePickups = new List<TotemLootPickupModel>(64);
    private readonly List<TotemLootPickupModel> generatedBuffer = new List<TotemLootPickupModel>(8);
    private readonly Dictionary<int, TotemLootPickupModel> pickupsById = new Dictionary<int, TotemLootPickupModel>(64);
    private readonly HashSet<int> processedEnemyDeaths = new HashSet<int>();
    private readonly Dictionary<int, HashSet<string>> botRecipeProfiles = new Dictionary<int, HashSet<string>>(49);
    private readonly Dictionary<int, PickupVisualInstance> pickupObjects = new Dictionary<int, PickupVisualInstance>(64);
    private readonly List<PickupVisualInstance> inactivePickupVisuals = new List<PickupVisualInstance>(PickupVisualPoolCapacity);

    private TotemEconomyService economyService;
    private TotemMetaProgressService metaProgressService;
    private TotemGameFlowService flowService;
    private TotemActorService actorService;
    private TotemParticipantReadinessService readinessService;
    private ITotemEnemyDeathEventSource deathEventSource;
    private GameObject pickupRoot;
    private int nextPickupId = 1;
    private int totalSpawnedPickupCount;
    private int totalClaimedPickupCount;
    private int lastSourceEnemyCombatantId;
    private int lastPickupId;
    private int lastClaimParticipantId;
    private string lastLootEntryId = string.Empty;
    private string lastPickupReason = string.Empty;
    private int visualCreatedCount;
    private int visualReusedCount;

    public override string ServiceName => "EnemyLoot";

    public int RunSeed { get; set; }

    public IReadOnlyList<TotemLootPickupModel> ActivePickups => activePickups;

    public int ActiveVisualCount => pickupObjects.Count;

    public int PooledVisualCount => inactivePickupVisuals.Count;

    public int VisualCreatedCount => visualCreatedCount;

    public int VisualReusedCount => visualReusedCount;

    public bool HasVisualRoot => pickupRoot != null;

    public event Action<TotemLootPickupModel> LootSpawned;

    public event Action<TotemLootPickupModel, TotemParticipantModel, TotemLootPickupResult> LootPickedUp;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        if (economyService == null)
        {
            economyService = runtime.GetService<TotemEconomyService>();
        }

        if (metaProgressService == null)
        {
            metaProgressService = runtime.GetService<TotemMetaProgressService>();
        }

        flowService = runtime.GetService<TotemGameFlowService>();
        actorService = runtime.GetService<TotemActorService>();
        readinessService = runtime.GetService<TotemParticipantReadinessService>();
        if (flowService != null)
        {
            flowService.StateChanged += OnFlowStateChanged;
        }

        if (generator.DefinitionCount <= 0)
        {
            var dataService = runtime.GetService<TotemDataService>();
            var catalog = dataService?.GameplayCatalog ?? TotemDataService.LoadGameplayCatalogOrDefault();
            catalog.Normalize();
            ReloadDefinitions(new TotemEnemyLootDefinitionArraySource(catalog.CreateEnemyLootDefinitions()));
        }

        BindDeathSource(runtime.GetService<TotemEnemyService>());
    }

    protected override void OnShutdown()
    {
        BindDeathSource(null);
        if (flowService != null)
        {
            flowService.StateChanged -= OnFlowStateChanged;
            flowService = null;
        }

        economyService = null;
        metaProgressService = null;
        actorService = null;
        readinessService = null;
        ResetRun();
        DestroyVisualPool();
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f || flowService?.CurrentState != TotemGameFlowState.CombatHud || activePickups.Count <= 0)
        {
            return;
        }

        var actors = actorService?.Actors;
        if (actors == null)
        {
            return;
        }

        float radiusSqr = PickupRadius * PickupRadius;
        for (int pickupIndex = activePickups.Count - 1; pickupIndex >= 0; pickupIndex--)
        {
            TotemLootPickupModel pickup = activePickups[pickupIndex];
            TotemActorModel claimant = null;
            float bestSqr = float.MaxValue;
            for (int actorIndex = 0; actorIndex < actors.Count; actorIndex++)
            {
                TotemActorModel candidate = actors[actorIndex];
                if (candidate == null
                    || !candidate.IsAlive
                    || (readinessService != null && !readinessService.CanAct(candidate)))
                {
                    continue;
                }

                float sqr = (candidate.Position - pickup.Position).sqrMagnitude;
                if (sqr > radiusSqr
                    || sqr > bestSqr
                    || (Mathf.Approximately(sqr, bestSqr) && claimant != null && candidate.ActorId > claimant.ActorId))
                {
                    continue;
                }

                bestSqr = sqr;
                claimant = candidate;
            }

            if (claimant != null)
            {
                TryPickup(pickup.PickupId, claimant, out _);
            }
        }
    }

    public void ConfigureRuntimeDependencies(
        TotemEconomyService economy,
        TotemMetaProgressService metaProgress)
    {
        economyService = economy;
        metaProgressService = metaProgress;
    }

    public void ReloadDefinitions(ITotemEnemyLootDefinitionSource source)
    {
        generator.ReloadDefinitions(source);
    }

    public void BindDeathSource(ITotemEnemyDeathEventSource source)
    {
        if (ReferenceEquals(deathEventSource, source))
        {
            return;
        }

        if (deathEventSource != null)
        {
            deathEventSource.EnemyDied -= OnEnemyDied;
        }

        deathEventSource = source;
        if (deathEventSource != null)
        {
            deathEventSource.EnemyDied += OnEnemyDied;
        }
    }

    public int HandleEnemyDied(in TotemEnemyDiedEvent evt)
    {
        var enemy = evt.Enemy;
        if (enemy == null || generator.DefinitionCount <= 0 || processedEnemyDeaths.Contains(enemy.CombatantId))
        {
            return 0;
        }

        processedEnemyDeaths.Add(enemy.CombatantId);
        lastSourceEnemyCombatantId = enemy.CombatantId;
        if (!generator.ValidateTierRules(enemy, out string validationReason))
        {
            GFTrace.Warning("TotemEnemyLoot", "LootTable.InvalidTierRules", null, GFTrace.Data(
                "enemyId", enemy.EnemyId,
                "lootTableId", enemy.LootTableId,
                "tier", enemy.Tier.ToString(),
                "reason", validationReason));
        }

        generatedBuffer.Clear();
        int generatedCount = generator.Generate(evt, RunSeed, generatedBuffer, ref nextPickupId);
        for (int i = 0; i < generatedBuffer.Count; i++)
        {
            var pickup = generatedBuffer[i];
            activePickups.Add(pickup);
            pickupsById.Add(pickup.PickupId, pickup);
            CreatePickupVisual(pickup);
            totalSpawnedPickupCount++;
            lastPickupId = pickup.PickupId;
            lastLootEntryId = pickup.LootEntryId;
            LootSpawned?.Invoke(pickup);
            GFTrace.Success("TotemEnemyLoot", "Enemy.LootSpawned", null, GFTrace.Data(
                "enemyId", enemy.EnemyId,
                "enemyCombatantId", enemy.CombatantId.ToString(),
                "pickupId", pickup.PickupId.ToString(),
                "lootEntryId", pickup.LootEntryId,
                "rewardType", pickup.RewardType.ToString(),
                "itemId", pickup.ItemId,
                "count", pickup.Count.ToString(),
                "worldTime", evt.WorldTime.ToString("F3")));
        }

        return generatedCount;
    }

    public bool TryGetPickup(int pickupId, out TotemLootPickupModel pickup)
    {
        return pickupsById.TryGetValue(pickupId, out pickup);
    }

    public bool TryPickup(
        int pickupId,
        TotemParticipantModel participant,
        out TotemLootPickupResult result)
    {
        if (participant == null)
        {
            result = Failed(pickupId, 0, "ParticipantMissing");
            return false;
        }

        if (!participant.IsAlive || participant.Lifecycle != TotemParticipantLifecycle.Active)
        {
            result = Failed(pickupId, participant.ParticipantId, "ParticipantNotActive");
            return false;
        }

        if (!pickupsById.TryGetValue(pickupId, out var pickup) || pickup.IsClaimed)
        {
            result = Failed(pickupId, participant.ParticipantId, "PickupUnavailable");
            return false;
        }

        bool recipeUnlocked = false;
        bool duplicateConverted = false;
        int grantedCount = pickup.Count;
        string conversionItemId = string.Empty;
        int conversionCount = 0;

        if (pickup.RewardType == TotemEnemyLootRewardType.Recipe)
        {
            if (!TryApplyRecipePickup(
                    pickup,
                    participant,
                    out recipeUnlocked,
                    out duplicateConverted,
                    out conversionItemId,
                    out conversionCount,
                    out string recipeFailureReason))
            {
                result = Failed(pickupId, participant.ParticipantId, recipeFailureReason);
                return false;
            }

            grantedCount = recipeUnlocked ? 1 : conversionCount;
        }
        else if (economyService == null
            || !economyService.TryAddEnemyLoot(participant, pickup.RewardType, pickup.ItemId, pickup.Count))
        {
            result = Failed(pickupId, participant.ParticipantId, "InventoryWriteFailed");
            return false;
        }

        pickup.MarkClaimed(participant.ParticipantId);
        pickupsById.Remove(pickupId);
        activePickups.Remove(pickup);
        DestroyPickupVisual(pickupId);
        totalClaimedPickupCount++;
        lastClaimParticipantId = participant.ParticipantId;
        lastPickupId = pickupId;
        lastLootEntryId = pickup.LootEntryId;
        lastPickupReason = duplicateConverted ? "DuplicateRecipeConverted" : "Granted";
        result = new TotemLootPickupResult(
            true,
            lastPickupReason,
            pickupId,
            participant.ParticipantId,
            pickup.RewardType,
            pickup.ItemId,
            grantedCount,
            recipeUnlocked,
            duplicateConverted,
            conversionItemId,
            conversionCount);
        LootPickedUp?.Invoke(pickup, participant, result);
        GFTrace.Success("TotemEnemyLoot", "Enemy.LootPickedUp", null, GFTrace.Data(
            "pickupId", pickupId.ToString(),
            "participantId", participant.ParticipantId.ToString(),
            "controller", participant.ControllerKind.ToString(),
            "lootEntryId", pickup.LootEntryId,
            "rewardType", pickup.RewardType.ToString(),
            "itemId", pickup.ItemId,
            "count", grantedCount.ToString(),
            "reason", lastPickupReason));
        return true;
    }

    public bool HasBotRecipe(int participantId, string recipeId)
    {
        return !string.IsNullOrWhiteSpace(recipeId)
            && botRecipeProfiles.TryGetValue(participantId, out var recipes)
            && recipes.Contains(recipeId);
    }

    public TotemEnemyLootSnapshot CaptureSnapshot()
    {
        return new TotemEnemyLootSnapshot
        {
            definitionCount = generator.DefinitionCount,
            activePickupCount = activePickups.Count,
            processedEnemyDeathCount = processedEnemyDeaths.Count,
            totalSpawnedPickupCount = totalSpawnedPickupCount,
            totalClaimedPickupCount = totalClaimedPickupCount,
            lastSourceEnemyCombatantId = lastSourceEnemyCombatantId,
            lastPickupId = lastPickupId,
            lastClaimParticipantId = lastClaimParticipantId,
            lastLootEntryId = lastLootEntryId,
            lastPickupReason = lastPickupReason,
        };
    }

    public void ResetRun()
    {
        foreach (PickupVisualInstance visual in pickupObjects.Values)
        {
            ReturnVisualToPool(visual);
        }
        pickupObjects.Clear();
        activePickups.Clear();
        generatedBuffer.Clear();
        pickupsById.Clear();
        processedEnemyDeaths.Clear();
        botRecipeProfiles.Clear();
        nextPickupId = 1;
        totalSpawnedPickupCount = 0;
        totalClaimedPickupCount = 0;
        lastSourceEnemyCombatantId = 0;
        lastPickupId = 0;
        lastClaimParticipantId = 0;
        lastLootEntryId = string.Empty;
        lastPickupReason = string.Empty;
    }

    private bool TryApplyRecipePickup(
        TotemLootPickupModel pickup,
        TotemParticipantModel participant,
        out bool recipeUnlocked,
        out bool duplicateConverted,
        out string conversionItemId,
        out int conversionCount,
        out string failureReason)
    {
        recipeUnlocked = false;
        duplicateConverted = false;
        conversionItemId = string.Empty;
        conversionCount = 0;
        failureReason = string.Empty;

        bool newlyUnlocked;
        if (participant.ControllerKind == TotemParticipantControllerKind.Human)
        {
            if (metaProgressService == null
                || !metaProgressService.TryUnlockBossRecipe(pickup.ItemId, out newlyUnlocked))
            {
                failureReason = "RecipePersistenceFailed";
                return false;
            }
        }
        else
        {
            if (!botRecipeProfiles.TryGetValue(participant.ParticipantId, out var recipes))
            {
                recipes = new HashSet<string>(StringComparer.Ordinal);
                botRecipeProfiles.Add(participant.ParticipantId, recipes);
            }

            newlyUnlocked = recipes.Add(pickup.ItemId);
        }

        if (newlyUnlocked)
        {
            recipeUnlocked = true;
            return true;
        }

        if (economyService == null
            || string.IsNullOrWhiteSpace(pickup.DuplicateRecipePaintItemId)
            || pickup.DuplicateRecipePaintCount <= 0
            || !economyService.TryAddEnemyLoot(
                participant,
                TotemEnemyLootRewardType.Paint,
                pickup.DuplicateRecipePaintItemId,
                pickup.DuplicateRecipePaintCount))
        {
            failureReason = "DuplicateRecipeConversionFailed";
            return false;
        }

        duplicateConverted = true;
        conversionItemId = pickup.DuplicateRecipePaintItemId;
        conversionCount = pickup.DuplicateRecipePaintCount;
        return true;
    }

    private void OnEnemyDied(TotemEnemyDiedEvent evt)
    {
        HandleEnemyDied(evt);
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud || previousState == TotemGameFlowState.CombatHud)
        {
            ResetRun();
        }
    }

    private TotemLootPickupResult Failed(int pickupId, int participantId, string reason)
    {
        lastPickupId = pickupId;
        lastClaimParticipantId = participantId;
        lastPickupReason = reason ?? string.Empty;
        return new TotemLootPickupResult(
            false,
            lastPickupReason,
            pickupId,
            participantId,
            TotemEnemyLootRewardType.Unknown,
            string.Empty,
            0,
            false,
            false,
            string.Empty,
            0);
    }

    private void CreatePickupVisual(TotemLootPickupModel pickup)
    {
        if (pickup == null || pickupObjects.ContainsKey(pickup.PickupId))
        {
            return;
        }

        if (pickupRoot == null)
        {
            pickupRoot = new GameObject("[TotemEnemyLoot]");
        }

        PickupVisualInstance visual;
        if (!TryTakePooledVisual(pickup.RewardType, out visual))
        {
            GameObject created = GameObject.CreatePrimitive(
                pickup.RewardType == TotemEnemyLootRewardType.Recipe ? PrimitiveType.Cube : PrimitiveType.Sphere);
            Material material = null;
            Renderer createdRenderer = created.GetComponent<Renderer>();
            if (createdRenderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader != null)
                {
                    material = new Material(shader);
                    Color color = GetPickupColor(pickup.RewardType);
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                    if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                    createdRenderer.sharedMaterial = material;
                }
            }

            visual = new PickupVisualInstance(created, material, pickup.RewardType);
            visualCreatedCount++;
        }

        GameObject instance = visual.GameObject;
        instance.name = $"TotemEnemyLoot_{pickup.PickupId}_{pickup.RewardType}_{pickup.ItemId}";
        instance.transform.SetParent(pickupRoot.transform, false);
        instance.transform.position = pickup.Position + Vector3.up * 0.35f;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = pickup.RewardType == TotemEnemyLootRewardType.Recipe
            ? Vector3.one * 0.6f
            : Vector3.one * 0.4f;
        if (!instance.activeSelf)
        {
            instance.SetActive(true);
        }

        pickupObjects.Add(pickup.PickupId, visual);
    }

    private void DestroyPickupVisual(int pickupId)
    {
        if (!pickupObjects.TryGetValue(pickupId, out PickupVisualInstance visual))
        {
            return;
        }

        pickupObjects.Remove(pickupId);
        ReturnVisualToPool(visual);
    }

    private bool TryTakePooledVisual(TotemEnemyLootRewardType rewardType, out PickupVisualInstance visual)
    {
        for (int i = inactivePickupVisuals.Count - 1; i >= 0; i--)
        {
            PickupVisualInstance candidate = inactivePickupVisuals[i];
            if (candidate.GameObject == null)
            {
                DestroyObject(candidate.Material);
                RemovePooledVisualAt(i);
                continue;
            }

            if (candidate.RewardType != rewardType)
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
        int lastIndex = inactivePickupVisuals.Count - 1;
        inactivePickupVisuals[index] = inactivePickupVisuals[lastIndex];
        inactivePickupVisuals.RemoveAt(lastIndex);
    }

    private void ReturnVisualToPool(PickupVisualInstance visual)
    {
        if (visual.GameObject == null)
        {
            DestroyObject(visual.Material);
            return;
        }

        visual.GameObject.SetActive(false);
        if (inactivePickupVisuals.Count < PickupVisualPoolCapacity)
        {
            inactivePickupVisuals.Add(visual);
            return;
        }

        DestroyObject(visual.Material);
        DestroyObject(visual.GameObject);
    }

    private void DestroyVisualPool()
    {
        foreach (PickupVisualInstance visual in pickupObjects.Values)
        {
            DestroyObject(visual.Material);
            DestroyObject(visual.GameObject);
        }
        pickupObjects.Clear();

        for (int i = 0; i < inactivePickupVisuals.Count; i++)
        {
            DestroyObject(inactivePickupVisuals[i].Material);
            DestroyObject(inactivePickupVisuals[i].GameObject);
        }
        inactivePickupVisuals.Clear();
        DestroyObject(pickupRoot);
        pickupRoot = null;
    }

    private static Color GetPickupColor(TotemEnemyLootRewardType rewardType)
    {
        switch (rewardType)
        {
            case TotemEnemyLootRewardType.Coin: return new Color(1f, 0.78f, 0.15f);
            case TotemEnemyLootRewardType.Paint: return new Color(0.85f, 0.25f, 0.8f);
            case TotemEnemyLootRewardType.Recipe: return new Color(0.25f, 0.85f, 1f);
            case TotemEnemyLootRewardType.Weapon: return new Color(1f, 0.35f, 0.2f);
            case TotemEnemyLootRewardType.Equipment: return new Color(0.45f, 0.65f, 1f);
            default: return Color.white;
        }
    }

    private static void DestroyObject(UnityEngine.Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) UnityEngine.Object.Destroy(value);
        else UnityEngine.Object.DestroyImmediate(value);
    }

    private readonly struct PickupVisualInstance
    {
        public readonly GameObject GameObject;
        public readonly Material Material;
        public readonly TotemEnemyLootRewardType RewardType;

        public PickupVisualInstance(
            GameObject gameObject,
            Material material,
            TotemEnemyLootRewardType rewardType)
        {
            GameObject = gameObject;
            Material = material;
            RewardType = rewardType;
        }
    }
}
