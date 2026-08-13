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
        string summary = FormatSummary(result);
        TotemActorModel player = ActorService?.Player;
        if (player != null)
        {
            TotemMatchAchievementSnapshot achievement = Runtime?.GetService<TotemFirstPlayableSocialService>()
                ?.CaptureAchievement(new TotemParticipantId(player.ParticipantId)) ?? default;
            summary += "\n" + FormatAchievementSummary(achievement);
            summary += "\n" + TotemFirstPlayableUiText.FormatResultEvidence(UIService, MatchFlowService,
                Runtime?.GetService<TotemFirstPlayableTattooBuildService>()?.GetOrCreateState(player), player);
        }
        AddText(panel, "UI-FP-RESULT-001/Summary", summary, 16, TextAnchor.MiddleCenter, 190f);
        AddButton(panel, "UI-FP-RESULT-001/Restart", "重新开始本地对局", OnRestartClicked);
        AddButton(panel, "UI-FP-RESULT-001/Return", "返回主菜单", OnReturnToMainMenuClicked);
    }

    private void OnReturnToMainMenuClicked()
    {
        UIService?.OpenMainMenu();
    }

    private void OnRestartClicked()
    {
        UIService?.RestartLocalFirstPlayable();
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

        string runSummary = $"Reason: {result.reason}\nEliminations: {result.killCount}  Participants: {result.aliveParticipantCount}\nHP: {result.playerHealth:F0}  Time: {result.elapsedSec:F1}s";
        if (result.cumulativeStats == null)
        {
            return runSummary;
        }

        return $"{runSummary}\n{TotemRunStatsService.FormatSnapshot(result.cumulativeStats)}";
    }

    public static string FormatAchievementSummary(TotemMatchAchievementSnapshot achievement)
    {
        return $"玩家伤害 {achievement.playerDamage:0} · 击倒 {achievement.playerDowns} · 淘汰 {achievement.playerEliminations}\n"
            + $"成功救援 {achievement.successfulRevives}\n"
            + $"资源获取 {achievement.resourcesAcquired} · 分享 {achievement.resourcesShared} · 自身倒地 {achievement.selfDowns}";
    }

}
