using UnityEngine;

public sealed class TotemRunResultForm : TotemOverlayFormBase
{
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        Time.timeScale = 1f;
        BuildView();
        GFTrace.Success("TotemUI", "RunResult.Open", null, GFTrace.Data("win", (UIService?.ActiveRunResult?.win ?? false).ToString()));
    }

    private void BuildView()
    {
        var result = UIService?.ActiveRunResult;
        if (result != null && result.cumulativeStats == null)
        {
            result.cumulativeStats = Runtime?.GetService<TotemRunStatsService>()?.CaptureSnapshot();
        }

        var panel = RebuildPanel(FormatTitle(result), new Vector2(560f, 410f));
        AddText(panel, "Summary", FormatSummary(result), 17, TextAnchor.MiddleCenter, 132f);
        AddButton(panel, "ReturnButton", "Return To Main Menu", OnReturnToMainMenuClicked);
    }

    private void OnReturnToMainMenuClicked()
    {
        UIService?.OpenMainMenu();
    }

    public static string FormatTitle(TotemRunResultSnapshot result)
    {
        return result != null && result.win ? "Victory" : "Run Ended";
    }

    public static string FormatSummary(TotemRunResultSnapshot result)
    {
        if (result == null)
        {
            return "No run result.";
        }

        string runSummary = $"Reason: {result.reason}\nKills: {result.killCount}  Alive: {result.aliveEnemyCount}\nHP: {result.playerHealth:F0}  Time: {result.elapsedSec:F1}s";
        if (result.bossRewardClaimed && !string.IsNullOrWhiteSpace(result.bossDeathPatternRecipeId))
        {
            runSummary += $"\nBoss Recipe: {result.bossDeathPatternRecipeId}";
        }

        if (result.cumulativeStats == null)
        {
            return runSummary;
        }

        return $"{runSummary}\n{TotemRunStatsService.FormatSnapshot(result.cumulativeStats)}";
    }
}
