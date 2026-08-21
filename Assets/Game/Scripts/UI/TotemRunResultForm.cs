using UnityEngine;

using System.Text;

public sealed class TotemRunResultForm : TotemOverlayFormBase
{
    [SerializeField] private TMPro.TMP_Text titleText;
    [SerializeField] private TMPro.TMP_Text summaryText;
    [SerializeField] private UnityEngine.UI.Image victoryEmblem;
    [SerializeField] private UnityEngine.UI.Image defeatEmblem;
    [SerializeField] private UnityEngine.UI.Button restartButton;
    [SerializeField] private UnityEngine.UI.Button returnButton;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        titleText ??= FindChildComponent<TMPro.TMP_Text>("Txt_ResultTitle");
        summaryText ??= FindChildComponent<TMPro.TMP_Text>("Txt_ResultSummary");
        victoryEmblem ??= FindChildComponent<UnityEngine.UI.Image>("Img_ResultVictoryEmblem");
        defeatEmblem ??= FindChildComponent<UnityEngine.UI.Image>("Img_ResultDefeatEmblem");
        restartButton = BindButton(restartButton, "Btn_ResultRestart", OnRestartClicked);
        returnButton = BindButton(returnButton, "Btn_ResultReturn", OnReturnToMainMenuClicked);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        Time.timeScale = 1f;
        BuildAuthoredView();
        GFTrace.Success("TotemUI", "RunResult.Open", null, GFTrace.Data("win", (UIService?.ActiveRunResult?.win ?? false).ToString()));
    }

    private void BuildAuthoredView()
    {
        var result = UIService?.ActiveRunResult;
        if (result != null && result.cumulativeStats == null)
        {
            result.cumulativeStats = Runtime?.GetService<TotemRunStatsService>()?.CaptureSnapshot();
        }

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
        summary += "\n" + FormatFinalRoster();

        titleText?.SetText(FormatTitle(result));
        summaryText?.SetText(summary);
        bool draw = result != null && result.draw;
        bool positive = result != null && (result.extracted || result.win);
        if (victoryEmblem != null)
        {
            victoryEmblem.gameObject.SetActive(positive || draw);
            victoryEmblem.color = draw ? new Color(0.67f, 0.78f, 0.80f, 1f) : Color.white;
        }
        if (defeatEmblem != null)
        {
            defeatEmblem.gameObject.SetActive(!positive && !draw);
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
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
        if (result != null && result.extracted) return "成功撤离";
        if (result != null && result.draw) return "本局平局";
        return result != null && result.win ? "胜利" : "本局结束";
    }

    public static string FormatSummary(TotemRunResultSnapshot result)
    {
        if (result == null)
        {
            return "暂无本局结果。";
        }

        string runSummary = $"结束原因：{result.reason}\n淘汰：{result.killCount}  存活参与者：{result.aliveParticipantCount}\n生命：{result.playerHealth:F0}  时长：{result.elapsedSec:F1}s";
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

    private string FormatFinalRoster()
    {
        var actors = ActorService?.Actors;
        var buildService = Runtime?.GetService<TotemFirstPlayableTattooBuildService>();
        if (actors == null || actors.Count == 0) return "最终队伍状态：暂无参与者数据。";

        var text = new StringBuilder(512);
        text.AppendLine("最终队伍状态与构筑");
        for (int team = 0; team < TotemFirstPlayableRules.TeamCount; team++)
        {
            text.Append("队伍 ").Append((char)('A' + team)).Append("：");
            bool wroteActor = false;
            for (int i = 0; i < actors.Count; i++)
            {
                TotemActorModel actor = actors[i];
                if (actor == null || actor.TeamId.Value != team) continue;
                if (wroteActor) text.Append("；");
                wroteActor = true;
                text.Append("P").Append(actor.ParticipantId)
                    .Append(actor.IsAlive ? " 存活" : " 淘汰")
                    .Append(" · ").Append(FormatLoadout(buildService?.GetOrCreateState(actor)));
            }

            if (!wroteActor) text.Append("无参与者");
            text.AppendLine();
        }

        return text.ToString().TrimEnd();
    }

    private static string FormatLoadout(TotemFirstPlayableTattooBuildState build)
    {
        if (build == null) return "无纹身";
        TotemTattooLoadoutEntry[] entries = build.CaptureLoadout();
        var text = new StringBuilder(128);
        for (int i = 0; i < entries.Length; i++)
        {
            if (!entries[i].IsEquipped) continue;
            if (text.Length > 0) text.Append(", ");
            text.Append(entries[i].Slot).Append(':').Append(entries[i].Pattern).Append('/').Append(entries[i].Element);
        }

        return text.Length == 0 ? "无纹身" : text.ToString();
    }

}
