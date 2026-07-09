using UnityEngine;

public sealed class TotemTattooEnchantForm : TotemOverlayFormBase
{
    private TotemTattooEnchantPurchaseResult lastResult;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        BuildView();
        GFTrace.Success("TotemUI", "TattooEnchant.Open");
    }

    private void BuildView()
    {
        var panel = RebuildPanel("Tattoo Enchant", new Vector2(620f, 430f));
        var tattoo = TattooService;
        var snapshot = tattoo?.CaptureSnapshot();
        var player = ActorService?.Player;
        var economy = Runtime?.GetService<TotemEconomyService>();
        var inventory = player != null ? economy?.CaptureInventory(player) : null;
        var npc = UIService?.ActiveTattooNpc;
        string colorTier = ResolveDisplayTier(tattoo);
        TotemTattooEnchantRecipeDefinition recipe = null;
        if (tattoo != null)
        {
            tattoo.TryGetRuntimeEnchantRecipe(colorTier, out recipe);
        }
        AddText(panel, "Status", FormatStatus(snapshot), 16, TextAnchor.MiddleCenter, 72f);
        AddText(panel, "Cost", FormatCost(npc, colorTier, recipe, inventory), 15, TextAnchor.MiddleLeft, 70f);
        AddText(panel, "Result", FormatResult(lastResult), 15, TextAnchor.MiddleCenter, 42f);
        AddButton(panel, "ApplyButton", "Apply Enchant", ApplyEnchant, CanApply(npc, snapshot, recipe, inventory));
        AddButton(panel, "CloseButton", "Close", OnClickClose);
    }

    private void ApplyEnchant()
    {
        var npc = UIService?.ActiveTattooNpc;
        if (npc == null || npc.Type != TotemNpcType.Tattooist)
        {
            lastResult = new TotemTattooEnchantPurchaseResult { reason = "NoTattooist" };
            BuildView();
            return;
        }

        var npcService = Runtime?.GetService<TotemNpcService>();
        string colorTier = ResolveDisplayTier(TattooService);
        if (npcService == null)
        {
            lastResult = new TotemTattooEnchantPurchaseResult { reason = "MissingNpcService", colorTier = colorTier };
            GFTrace.Warning("TotemUI", "TattooEnchant.Rejected", null, GFTrace.Data(
                "npcId", npc.NpcId ?? string.Empty,
                "tier", colorTier,
                "reason", lastResult.reason));
            BuildView();
            return;
        }

        if (!npcService.TryApplyTattooEnchant(ActorService?.Player, colorTier, out lastResult))
        {
            GFTrace.Warning("TotemUI", "TattooEnchant.Rejected", null, GFTrace.Data(
                "npcId", npc.NpcId ?? string.Empty,
                "tier", colorTier,
                "reason", lastResult?.reason ?? string.Empty));
        }

        BuildView();
    }

    public static string FormatStatus(TotemTattooSnapshot snapshot)
    {
        if (snapshot == null || snapshot.equippedCount <= 0)
        {
            return "No equipped tattoo.";
        }

        if (snapshot.lastEnchantAffixId > 0)
        {
            return $"Equipped: {snapshot.equippedCount}  Enchants: {snapshot.enchantedCount}\nLast: {snapshot.lastEnchantDisplayText}";
        }

        return $"Equipped: {snapshot.equippedCount}  Enchants: {snapshot.enchantedCount}";
    }

    private static bool CanApply(
        TotemNpcModel npc,
        TotemTattooSnapshot snapshot,
        TotemTattooEnchantRecipeDefinition recipe,
        TotemInventorySnapshot inventory)
    {
        return npc != null &&
            npc.Type == TotemNpcType.Tattooist &&
            snapshot != null &&
            snapshot.equippedCount > 0 &&
            recipe != null &&
            inventory != null &&
            inventory.coins >= recipe.CoinCost &&
            inventory.inkBottleCount >= recipe.RarePigmentCost;
    }

    private static string ResolveDisplayTier(TotemTattooService tattoo)
    {
        var equipped = tattoo?.Equipped;
        if (equipped != null && equipped.Count > 0 && equipped[0] != null)
        {
            return TotemTattooService.ResolveColorTier(equipped[0].ColorId);
        }

        return "Common";
    }

    private static string FormatCost(
        TotemNpcModel npc,
        string colorTier,
        TotemTattooEnchantRecipeDefinition recipe,
        TotemInventorySnapshot inventory)
    {
        string npcText = npc == null || npc.Type != TotemNpcType.Tattooist ? "Tattooist: none" : $"Tattooist: {npc.NpcId}";
        if (recipe == null)
        {
            return $"{npcText}\nTier: {colorTier}  Recipe missing";
        }

        int coins = inventory?.coins ?? 0;
        int ink = inventory?.inkBottleCount ?? 0;
        return $"{npcText}\nTier: {colorTier}  Cost: {recipe.CoinCost} coins + {recipe.RarePigmentCost} ink  Have: {coins}/{ink}";
    }

    private static string FormatResult(TotemTattooEnchantPurchaseResult result)
    {
        if (result == null || string.IsNullOrWhiteSpace(result.reason))
        {
            return "Result: idle";
        }

        return result.succeeded
            ? $"Result: applied #{result.affixId}  Coins {result.coinsAfter}  Ink {result.inkAfter}"
            : $"Result: {result.reason}";
    }
}
