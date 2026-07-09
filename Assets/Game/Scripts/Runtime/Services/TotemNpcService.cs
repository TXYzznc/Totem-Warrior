using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TotemNpcService : TotemRuntimeServiceBase
{
    private readonly List<TotemNpcModel> npcs = new List<TotemNpcModel>(5);
    private readonly List<GameObject> spawnedObjects = new List<GameObject>(5);
    private TotemGameFlowService flowService;
    private TotemMapService mapService;
    private TotemEconomyService economyService;
    private TotemWeaponService weaponService;
    private TotemSkillService skillService;
    private TotemStatusService statusService;
    private TotemTattooService tattooService;
    private TotemDataService dataService;
    private TotemAssetService assetService;

    public override string ServiceName => "Npc";

    public IReadOnlyList<TotemNpcModel> Npcs => npcs;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        mapService = runtime.GetService<TotemMapService>();
        economyService = runtime.GetService<TotemEconomyService>();
        weaponService = runtime.GetService<TotemWeaponService>();
        skillService = runtime.GetService<TotemSkillService>();
        statusService = runtime.GetService<TotemStatusService>();
        tattooService = runtime.GetService<TotemTattooService>();
        dataService = runtime.GetService<TotemDataService>();
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

        mapService = null;
        economyService = null;
        weaponService = null;
        skillService = null;
        statusService = null;
        tattooService = null;
        dataService = null;
        assetService = null;
        DespawnNpcs();
    }

    public void SpawnNpcs(TotemMapSnapshot map, bool createObjects)
    {
        DespawnNpcs();
        var built = BuildRuntimeNpcs(map);
        for (int i = 0; i < built.Length; i++)
        {
            npcs.Add(built[i]);
            if (createObjects)
            {
                spawnedObjects.Add(CreateNpcObject(built[i]));
            }
        }

        GFTrace.Success("TotemNpc", "Spawned", null, GFTrace.Data("count", npcs.Count.ToString()));
    }

    public TotemNpcModel[] BuildRuntimeNpcs(TotemMapSnapshot map)
    {
        var catalogNpcs = dataService?.GameplayCatalog?.CreateNpcModels(map);
        return catalogNpcs == null || catalogNpcs.Length <= 0 ? BuildDefaultNpcs(map) : catalogNpcs;
    }

    public static TotemNpcModel[] BuildDefaultNpcs(TotemMapSnapshot map)
    {
        return TotemDataService.LoadGameplayCatalogOrDefault().CreateNpcModels(map);
    }

    public TotemNpcModel FindNearestInteractable(Vector3 position)
    {
        TotemNpcModel best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < npcs.Count; i++)
        {
            var npc = npcs[i];
            float radiusSqr = npc.InteractRadius * npc.InteractRadius;
            float sqr = (npc.Position - position).sqrMagnitude;
            if (sqr <= radiusSqr && sqr < bestDistance)
            {
                best = npc;
                bestDistance = sqr;
            }
        }

        return best;
    }

    public bool TryPurchase(TotemActorModel buyer, TotemNpcModel merchant, int itemId)
    {
        return TryPurchase(buyer, merchant, itemId, out _);
    }

    public bool TryPurchase(TotemActorModel buyer, TotemNpcModel merchant, int itemId, out TotemShopPurchaseResult result)
    {
        if (buyer == null || merchant == null || merchant.Type != TotemNpcType.Merchant || economyService == null)
        {
            result = BuildPurchaseResult(false, "InvalidContext", itemId, 0, 0, TotemShopRewardType.Unknown, string.Empty);
            return false;
        }

        var offers = merchant.Offers ?? Array.Empty<TotemShopOffer>();
        for (int i = 0; i < offers.Length; i++)
        {
            var offer = offers[i];
            if (offer.ItemId != itemId)
            {
                continue;
            }

            int actualPrice = Mathf.RoundToInt(offer.Price * merchant.ThemePriceMultiplier);
            if (offer.Stock <= 0)
            {
                result = BuildPurchaseResult(false, "StockExhausted", itemId, actualPrice, offer.Stock, InferRewardType(offer), string.Empty);
                return false;
            }

            if (!economyService.SpendCoins(buyer, actualPrice))
            {
                result = BuildPurchaseResult(false, "NotEnoughCoins", itemId, actualPrice, offer.Stock, InferRewardType(offer), string.Empty);
                return false;
            }

            bool rewardApplied = ApplyPurchasedOfferEffect(offer, buyer, economyService, weaponService, skillService, statusService, tattooService, out string rewardSummary);
            var rewardType = InferRewardType(offer);
            if (!rewardApplied)
            {
                economyService.AddCoins(buyer, actualPrice);
                result = BuildPurchaseResult(false, "RewardUnavailable", itemId, actualPrice, offer.Stock, rewardType, rewardSummary);
                GFTrace.Warning("TotemNpc", "ShopPurchaseRejected", null, GFTrace.Data(
                    "npcId", merchant.NpcId,
                    "itemId", itemId.ToString(),
                    "price", actualPrice.ToString(),
                    "stockLeft", offer.Stock.ToString(),
                    "reason", result.reason,
                    "reward", rewardSummary));
                return false;
            }

            offer.Stock--;
            result = BuildPurchaseResult(true, "Purchased", itemId, actualPrice, offer.Stock, rewardType, rewardSummary);
            GFTrace.Success("TotemNpc", "ShopPurchase", null, GFTrace.Data(
                "npcId", merchant.NpcId,
                "itemId", itemId.ToString(),
                "price", actualPrice.ToString(),
                "stockLeft", offer.Stock.ToString(),
                "reward", rewardSummary));
            return true;
        }

        result = BuildPurchaseResult(false, "OfferNotFound", itemId, 0, 0, TotemShopRewardType.Unknown, string.Empty);
        return false;
    }

    public bool TryApplyTattooEnchant(TotemActorModel buyer, string colorTier, out TotemTattooEnchantPurchaseResult result)
    {
        return TryApplyTattooEnchant(buyer, colorTier, economyService, tattooService, out result);
    }

    public static bool TryApplyTattooEnchant(
        TotemActorModel buyer,
        string colorTier,
        TotemEconomyService economy,
        TotemTattooService tattoo,
        out TotemTattooEnchantPurchaseResult result)
    {
        string resolvedTier = ResolveRequestedEnchantTier(tattoo, colorTier);
        if (buyer == null)
        {
            result = BuildTattooEnchantResult(false, "InvalidContext", resolvedTier, null, null, null);
            return false;
        }

        if (economy == null || tattoo == null)
        {
            result = BuildTattooEnchantResult(false, "MissingService", resolvedTier, null, economy?.CaptureInventory(buyer), null);
            return false;
        }

        if (tattoo.Equipped == null || tattoo.Equipped.Count <= 0)
        {
            result = BuildTattooEnchantResult(false, "NoEquippedTattoo", resolvedTier, null, economy.CaptureInventory(buyer), null);
            return false;
        }

        if (!tattoo.TryGetRuntimeEnchantRecipe(resolvedTier, out var recipe))
        {
            result = BuildTattooEnchantResult(false, "RecipeMissing", resolvedTier, null, economy.CaptureInventory(buyer), null);
            return false;
        }

        var before = economy.CaptureInventory(buyer);
        if (before.coins < recipe.CoinCost)
        {
            result = BuildTattooEnchantResult(false, "NotEnoughCoins", resolvedTier, recipe, before, null);
            return false;
        }

        if (before.inkBottleCount < recipe.RarePigmentCost)
        {
            result = BuildTattooEnchantResult(false, "NotEnoughInk", resolvedTier, recipe, before, null);
            return false;
        }

        if (!economy.SpendCoins(buyer, recipe.CoinCost))
        {
            result = BuildTattooEnchantResult(false, "NotEnoughCoins", resolvedTier, recipe, economy.CaptureInventory(buyer), null);
            return false;
        }

        if (!economy.SpendInk(buyer, recipe.RarePigmentCost))
        {
            economy.AddCoins(buyer, recipe.CoinCost);
            result = BuildTattooEnchantResult(false, "NotEnoughInk", resolvedTier, recipe, economy.CaptureInventory(buyer), null);
            return false;
        }

        if (!tattoo.ApplyEnchant(resolvedTier))
        {
            economy.AddCoins(buyer, recipe.CoinCost);
            economy.AddInk(buyer, recipe.RarePigmentCost);
            result = BuildTattooEnchantResult(false, "ApplyRejected", resolvedTier, recipe, economy.CaptureInventory(buyer), null);
            GFTrace.Warning("TotemNpc", "TattooEnchantRejected", null, GFTrace.Data(
                "actorId", buyer.ActorId.ToString(),
                "tier", resolvedTier,
                "reason", result.reason));
            return false;
        }

        var enchantSnapshot = tattoo.CaptureSnapshot();
        result = BuildTattooEnchantResult(true, "Applied", resolvedTier, recipe, economy.CaptureInventory(buyer), enchantSnapshot);
        GFTrace.Success("TotemNpc", "TattooEnchantPurchased", null, GFTrace.Data(
            "actorId", buyer.ActorId.ToString(),
            "tier", resolvedTier,
            "coinCost", recipe.CoinCost.ToString(),
            "inkCost", recipe.RarePigmentCost.ToString(),
            "affixId", enchantSnapshot.lastEnchantAffixId.ToString()));
        return true;
    }

    public static TotemShopRewardType InferRewardType(TotemShopOffer offer)
    {
        if (offer == null)
        {
            return TotemShopRewardType.Unknown;
        }

        if (offer.RewardType != TotemShopRewardType.Unknown)
        {
            return offer.RewardType;
        }

        return offer.RewardType;
    }

    public static bool ApplyPurchasedOfferEffect(
        TotemShopOffer offer,
        TotemActorModel buyer,
        TotemEconomyService economy,
        TotemWeaponService weapon,
        TotemSkillService skill,
        TotemTattooService tattoo,
        out string rewardSummary)
    {
        return ApplyPurchasedOfferEffect(offer, buyer, economy, weapon, skill, null, tattoo, out rewardSummary);
    }

    public static bool ApplyPurchasedOfferEffect(
        TotemShopOffer offer,
        TotemActorModel buyer,
        TotemEconomyService economy,
        TotemWeaponService weapon,
        TotemSkillService skill,
        TotemStatusService status,
        TotemTattooService tattoo,
        out string rewardSummary)
    {
        rewardSummary = string.Empty;
        if (offer == null || buyer == null)
        {
            return false;
        }

        switch (InferRewardType(offer))
        {
            case TotemShopRewardType.Ink:
            {
                int count = offer.RewardAmount > 0 ? offer.RewardAmount : 1;
                economy?.AddInk(buyer, count);
                rewardSummary = $"Ink +{count}";
                return economy != null;
            }
            case TotemShopRewardType.WeaponUpgrade:
            {
                if (weapon == null)
                {
                    rewardSummary = "Weapon service unavailable";
                    return false;
                }

                string weaponId = !string.IsNullOrWhiteSpace(offer.RewardId) ? offer.RewardId : TotemWeaponService.DefaultWeaponId;
                bool upgraded = weapon.TryUpgrade(buyer, weaponId, offer.Price, out int convertedGold);
                if (!upgraded && convertedGold > 0)
                {
                    economy?.AddCoins(buyer, convertedGold);
                }

                rewardSummary = upgraded ? $"{weaponId} upgraded" : $"{weaponId} converted {convertedGold}";
                return upgraded || convertedGold > 0;
            }
            case TotemShopRewardType.SkillCore:
            {
                if (skill == null)
                {
                    rewardSummary = "Skill service unavailable";
                    return false;
                }

                int slot = offer.RewardSlot >= 0 ? offer.RewardSlot : 0;
                bool refreshed = skill.RefreshSkillSlot(buyer, slot);
                rewardSummary = refreshed ? $"Skill slot {slot} refreshed" : $"Skill slot {slot} unavailable";
                return refreshed;
            }
            case TotemShopRewardType.StatusCleanse:
                status?.ClearAllStatuses(buyer);
                rewardSummary = "Statuses cleansed";
                return status != null;
            default:
                rewardSummary = "Unknown reward";
                return false;
        }
    }

    public TotemNpcSnapshot CaptureSnapshot()
    {
        var snapshot = new TotemNpcSnapshot();
        for (int i = 0; i < npcs.Count; i++)
        {
            var npc = npcs[i];
            snapshot.npcCount++;
            if (npc.Type == TotemNpcType.Merchant)
            {
                snapshot.merchantCount++;
                snapshot.shopOfferCount += npc.Offers?.Length ?? 0;
            }
            else
            {
                snapshot.tattooistCount++;
            }
        }

        return snapshot;
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            SpawnNpcs(mapService?.CurrentMap ?? TotemMapService.BuildLayout(1, 1), createObjects: true);
            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            DespawnNpcs();
            GFTrace.Info("TotemNpc", "Despawned", null, GFTrace.Data("nextState", nextState.ToString()));
        }
    }

    private static TotemShopPurchaseResult BuildPurchaseResult(bool purchased, string reason, int itemId, int actualPrice, int stockLeft, TotemShopRewardType rewardType, string rewardSummary)
    {
        return new TotemShopPurchaseResult
        {
            purchased = purchased,
            reason = reason,
            itemId = itemId,
            actualPrice = actualPrice,
            stockLeft = stockLeft,
            rewardType = rewardType,
            rewardSummary = rewardSummary ?? string.Empty,
        };
    }

    private static string ResolveRequestedEnchantTier(TotemTattooService tattoo, string colorTier)
    {
        if (!string.IsNullOrWhiteSpace(colorTier))
        {
            return colorTier;
        }

        var equipped = tattoo?.Equipped;
        if (equipped != null && equipped.Count > 0 && equipped[0] != null)
        {
            return TotemTattooService.ResolveColorTier(equipped[0].ColorId);
        }

        return "Common";
    }

    private static TotemTattooEnchantPurchaseResult BuildTattooEnchantResult(
        bool succeeded,
        string reason,
        string colorTier,
        TotemTattooEnchantRecipeDefinition recipe,
        TotemInventorySnapshot inventory,
        TotemTattooSnapshot enchantSnapshot)
    {
        return new TotemTattooEnchantPurchaseResult
        {
            succeeded = succeeded,
            reason = reason ?? string.Empty,
            colorTier = string.IsNullOrWhiteSpace(colorTier) ? "Common" : colorTier,
            coinCost = recipe?.CoinCost ?? 0,
            rarePigmentCost = recipe?.RarePigmentCost ?? 0,
            coinsAfter = inventory?.coins ?? 0,
            inkAfter = inventory?.inkBottleCount ?? 0,
            affixId = enchantSnapshot?.lastEnchantAffixId ?? 0,
            affixSummary = enchantSnapshot?.lastEnchantDisplayText ?? string.Empty,
        };
    }

    private GameObject CreateNpcObject(TotemNpcModel npc)
    {
        string assetKey = npc.Type == TotemNpcType.Merchant ? "npc.merchant" : "npc.tattooist";
        if (assetService != null && assetService.TryInstantiateGameObject(assetKey, null, npc.Position + Vector3.up * 0.5f, new Vector3(0.75f, 0.75f, 0.75f), out var instance))
        {
            instance.name = $"TotemNpc_{npc.NpcId}";
            return instance;
        }

        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = $"TotemNpc_{npc.NpcId}";
        go.transform.position = npc.Position + Vector3.up * 0.5f;
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            Color color = npc.Type == TotemNpcType.Merchant ? new Color(1f, 0.82f, 0.18f) : new Color(0.65f, 0.30f, 0.90f);
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

        return go;
    }

    private void DespawnNpcs()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            var go = spawnedObjects[i];
            if (go == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(go);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        spawnedObjects.Clear();
        npcs.Clear();
    }

    private static Vector3 FindRoomCenter(TotemMapSnapshot map, TotemRoomType roomType, Vector3 fallback)
    {
        var rooms = map?.Rooms;
        if (rooms == null)
        {
            return fallback;
        }

        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i].RoomType == roomType)
            {
                return rooms[i].CenterWorld;
            }
        }

        return fallback;
    }
}
