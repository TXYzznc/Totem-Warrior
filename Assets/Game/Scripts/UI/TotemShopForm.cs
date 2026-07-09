using UnityEngine;

public sealed class TotemShopForm : TotemOverlayFormBase
{
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        BuildView();
        GFTrace.Success("TotemUI", "Shop.Open", null, GFTrace.Data("npcId", UIService?.ActiveShopNpc?.NpcId ?? string.Empty));
    }

    private void BuildView()
    {
        var panel = RebuildPanel("Shop", new Vector2(640f, 460f));
        var npc = UIService?.ActiveShopNpc;
        AddText(panel, "NpcInfo", FormatNpcText(npc), 16, TextAnchor.MiddleLeft, 34f);

        var inventory = Runtime?.GetService<TotemEconomyService>()?.CaptureInventory(ActorService?.Player);
        AddText(panel, "Inventory", FormatInventoryText(inventory?.coins ?? 0), 16, TextAnchor.MiddleLeft, 30f);

        var offers = npc?.Offers;
        if (offers == null || offers.Length == 0)
        {
            AddText(panel, "Empty", "No offers available.", 16, TextAnchor.MiddleCenter, 42f);
        }
        else
        {
            for (int i = 0; i < offers.Length; i++)
            {
                var offer = offers[i];
                int capturedItemId = offer.ItemId;
                bool interactable = offer.Stock > 0;
                AddButton(panel, $"Offer_{offer.ItemId}", FormatOfferText(offer, npc.ThemePriceMultiplier), () => OnPurchaseClicked(capturedItemId), interactable);
            }
        }

        AddButton(panel, "CloseButton", "Close", OnClickClose);
    }

    private void OnPurchaseClicked(int itemId)
    {
        var npc = UIService?.ActiveShopNpc;
        var npcService = Runtime?.GetService<TotemNpcService>();
        TotemShopPurchaseResult result = null;
        bool purchased = npcService != null && npcService.TryPurchase(ActorService?.Player, npc, itemId, out result);
        if (purchased)
        {
            GFTrace.Success("TotemUI", "Shop.Purchase", null, GFTrace.Data(
                "npcId", npc?.NpcId ?? string.Empty,
                "itemId", itemId.ToString(),
                "price", result.actualPrice.ToString(),
                "stockLeft", result.stockLeft.ToString(),
                "reward", result.rewardSummary));
        }
        else
        {
            GFTrace.Warning("TotemUI", "Shop.PurchaseRejected", null, GFTrace.Data(
                "npcId", npc?.NpcId ?? string.Empty,
                "itemId", itemId.ToString(),
                "reason", result?.reason ?? "NoNpcService"));
        }

        BuildView();
    }

    public static string FormatNpcText(TotemNpcModel npc)
    {
        if (npc == null)
        {
            return "Merchant: none";
        }

        return $"Merchant: {npc.NpcId}  Price x{npc.ThemePriceMultiplier:F2}";
    }

    public static string FormatInventoryText(int coins)
    {
        return $"Coins: {coins}";
    }

    public static string FormatOfferText(TotemShopOffer offer, float priceMultiplier)
    {
        if (offer == null)
        {
            return "Empty offer";
        }

        int price = Mathf.RoundToInt(offer.Price * priceMultiplier);
        return $"{offer.DisplayName}  Price: {price}  Stock: {offer.Stock}";
    }
}
