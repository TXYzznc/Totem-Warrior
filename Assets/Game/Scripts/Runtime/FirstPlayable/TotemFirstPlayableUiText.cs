using System.Collections.Generic;

public static class TotemFirstPlayableUiText
{
    public const string MainMenuSummary = "6名参与者 · 3支双人队伍 · 1名玩家 + 5名人机\n五轮完整流程 · 四次缩圈 · 单一枪械 · 构筑阶段暂停战斗";

    public static string FormatResultEvidence(
        TotemUIService ui,
        TotemMatchFlowService match,
        TotemFirstPlayableTattooBuildState build,
        TotemActorModel player)
    {
        string loadout = "无";
        if (build != null)
        {
            TotemTattooLoadoutEntry[] entries = build.CaptureLoadout();
            var parts = new List<string>(entries.Length);
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].IsEquipped)
                {
                    parts.Add($"{entries[i].Slot}:{entries[i].Pattern}/{entries[i].Element}");
                }
            }

            if (parts.Count > 0)
            {
                loadout = string.Join(", ", parts);
            }
        }

        bool fast = ui?.LastLocalMatchFastMode ?? match?.FastMode ?? false;
        string timing = fast
            ? "构筑60/45/45/45/45秒 · 战斗60秒 · 缩圈10秒"
            : "构筑60/45/45/45/45秒 · 战斗180秒 · 缩圈30秒";
        return $"Seed {ui?.LastLocalMatchSeed ?? 1} · 快速模式 {(fast ? "开" : "关")} · 队伍 {player?.TeamId.ToString() ?? "未知"}\n{timing}\n最终构筑：{loadout}";
    }
}
