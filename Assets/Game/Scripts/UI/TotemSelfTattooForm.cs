using UnityEngine;

public sealed class TotemSelfTattooForm : TotemOverlayFormBase
{
    private int selectedPartId = 1;
    private int selectedColorId = 1;
    private int selectedPatternId = 1;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        BuildView();
        GFTrace.Success("TotemUI", "SelfTattoo.Open");
    }

    private void BuildView()
    {
        var panel = RebuildPanel("Self Tattoo", new Vector2(620f, 520f));
        var snapshot = Runtime?.GetService<TotemTattooService>()?.CaptureSnapshot();
        AddText(panel, "Equipped", FormatEquipped(snapshot), 15, TextAnchor.MiddleLeft, 56f);
        AddText(panel, "Selection", FormatSelection(selectedPartId, selectedColorId, selectedPatternId), 16, TextAnchor.MiddleCenter, 38f);
        AddText(panel, "Reading", FormatReading(snapshot), 15, TextAnchor.MiddleCenter, 34f);
        AddButton(panel, "PartButton", $"Part Next ({selectedPartId})", NextPart);
        AddButton(panel, "ColorButton", $"Color Next ({selectedColorId})", NextColor);
        AddButton(panel, "PatternButton", $"Pattern Next ({selectedPatternId})", NextPattern);
        bool inProgress = snapshot != null && snapshot.selfTattooInProgress;
        AddButton(panel, "StartButton", "Start Reading", StartSelfTattoo, !inProgress);
        AddButton(panel, "CancelButton", "Cancel Reading", CancelSelfTattoo, inProgress);
        AddButton(panel, "EnchantButton", "Open Enchant", () => UIService?.OpenTattooEnchant(), UIService?.ActiveTattooNpc != null);
        AddButton(panel, "CloseButton", "Close", OnClickClose);
    }

    private void NextPart()
    {
        selectedPartId = selectedPartId % TotemTattooService.PartCount + 1;
        BuildView();
    }

    private void NextColor()
    {
        selectedColorId = selectedColorId % TotemTattooService.ColorCount + 1;
        BuildView();
    }

    private void NextPattern()
    {
        selectedPatternId = selectedPatternId % TotemTattooService.PatternCount + 1;
        BuildView();
    }

    private void StartSelfTattoo()
    {
        Runtime?.GetService<TotemTattooService>()?.StartSelfTattoo(selectedPartId, selectedColorId, selectedPatternId);
        BuildView();
    }

    private void CancelSelfTattoo()
    {
        Runtime?.GetService<TotemTattooService>()?.CancelSelfTattoo();
        BuildView();
    }

    public static string FormatSelection(int partId, int colorId, int patternId)
    {
        return $"Part {partId}  Color {colorId}  Pattern {patternId}";
    }

    public static string FormatEquipped(TotemTattooSnapshot snapshot)
    {
        if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.equippedSummary))
        {
            return "Equipped: none";
        }

        return $"Equipped: {snapshot.equippedSummary}";
    }

    public static string FormatReading(TotemTattooSnapshot snapshot)
    {
        if (snapshot == null || !snapshot.selfTattooInProgress)
        {
            return "Reading: idle";
        }

        return $"Reading: {snapshot.pendingSelfTattooSummary}  {snapshot.selfTattooRemainingSec:F1}s";
    }
}
