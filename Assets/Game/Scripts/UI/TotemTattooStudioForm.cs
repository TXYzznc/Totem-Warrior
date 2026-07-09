using UnityEngine;

public sealed class TotemTattooStudioForm : TotemOverlayFormBase
{
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        BuildView();
        GFTrace.Success("TotemUI", "TattooStudio.Open", null, GFTrace.Data("npcId", UIService?.ActiveTattooNpc?.NpcId ?? string.Empty));
    }

    private void BuildView()
    {
        var panel = RebuildPanel("Tattoo Studio", new Vector2(620f, 360f));
        var npc = UIService?.ActiveTattooNpc;
        AddText(panel, "NpcInfo", FormatNpcText(npc), 16, TextAnchor.MiddleLeft, 36f);
        AddText(panel, "ChoiceInfo", FormatChoiceSummary(UIService?.ActiveChoice), 16, TextAnchor.MiddleLeft, 36f);
        AddButton(panel, "ThreeChoiceButton", "Open Three Choices", OnOpenThreeChoicesClicked, npc != null);
        AddButton(panel, "EnchantButton", "Open Enchant", () => UIService?.OpenTattooEnchant(), npc != null);
        AddButton(panel, "CloseButton", "Close", OnClickClose);
    }

    private void OnOpenThreeChoicesClicked()
    {
        var npc = UIService?.ActiveTattooNpc;
        if (npc == null)
        {
            GFTrace.Warning("TotemUI", "TattooStudio.ChoiceRejected", null, GFTrace.Data("reason", "NoNpc"));
            return;
        }

        string eventId = TotemInteractionService.BuildChoiceEventId(npc);
        var choice = UIService?.ActiveChoice;
        if (!CanReuseChoice(choice, eventId))
        {
            int actorId = ActorService?.Player?.ActorId ?? 0;
            int seed = TotemInteractionService.ComputeStableSeed(eventId, actorId);
            choice = Runtime?.GetService<TotemChoiceService>()?.RollThreeChoices(eventId, seed);
        }

        if (choice == null)
        {
            GFTrace.Warning("TotemUI", "TattooStudio.ChoiceRejected", null, GFTrace.Data("reason", "NoChoiceService"));
            return;
        }

        UIService?.OpenThreeChoice(choice);
    }

    public static string FormatNpcText(TotemNpcModel npc)
    {
        if (npc == null)
        {
            return "Tattooist: none";
        }

        return $"Tattooist: {npc.NpcId}  Theme x{npc.ThemePriceMultiplier:F2}";
    }

    public static string FormatChoiceSummary(TotemChoiceSnapshot choice)
    {
        if (choice == null)
        {
            return "Choices: not rolled";
        }

        return $"Choices: {choice.EventId}  Count: {choice.Options.Length}";
    }

    public static bool CanReuseChoice(TotemChoiceSnapshot choice, string eventId)
    {
        return choice != null &&
            choice.State == TotemChoiceRuntimeState.Showing &&
            string.Equals(choice.EventId, eventId, System.StringComparison.Ordinal);
    }
}
