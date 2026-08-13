using UnityEngine;
using UnityEngine.UI;

public sealed class TotemMainMenuForm : TotemOverlayFormBase
{
    public const string FirstPlayableSummary = TotemFirstPlayableUiText.MainMenuSummary;

    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    private int developmentSeed = 1;
    private bool developmentFastMode;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        startButton = BindButton(startButton, "StartButton", OnStartClicked);
        settingsButton = BindButton(settingsButton, "SettingsButton", OnSettingsClicked);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        FlowService?.EnterMainMenu();
        BuildMainView();
        GFTrace.Success("TotemUI", "MainMenu.Open");
    }

    public void OnStartClicked()
    {
        GFTrace.Info("TotemUI", "MainMenu.StartClicked");
        BuildLocalMatchConfirmation();
    }

    private void OnSettingsClicked()
    {
        GFTrace.Info("TotemUI", "MainMenu.SettingsClicked");
        UIService?.OpenSettings();
    }

    private void BuildMainView()
    {
        var panel = RebuildPanel("首个可玩版本", new Vector2(560f, 680f));
        AddText(panel, "BuildInfo", FormatBuildInfo(), 14, TextAnchor.MiddleCenter, 32f);
        AddButton(panel, "LocalMatchButton", "开始本地对局", OnStartClicked);
        AddButton(panel, "ArchiveButton", "纹身与元素图鉴", BuildArchiveView);
        AddButton(panel, "HelpButton", "玩法说明", BuildHelpView);
        AddButton(panel, "SettingsButton", "设置", OnSettingsClicked);
        AddButton(panel, "CreditsButton", "制作人员", BuildCreditsView);
        AddButton(panel, "ExitButton", "退出游戏", BuildExitConfirmation);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        AddText(panel, "DevelopmentNotice", $"开发参数：Seed {developmentSeed} · 快速模式 {(developmentFastMode ? "开" : "关")}", 13, TextAnchor.MiddleCenter, 36f);
        var developmentRow = AddHorizontalRow(panel, "DevelopmentControls");
        AddButton(developmentRow, "SeedDownButton", "Seed -", DecreaseDevelopmentSeed);
        AddButton(developmentRow, "SeedUpButton", "Seed +", IncreaseDevelopmentSeed);
        AddButton(developmentRow, "FastModeButton", "切换快速模式", ToggleDevelopmentFastMode);
#endif
    }

    private void BuildLocalMatchConfirmation()
    {
        var panel = RebuildPanel("确认本地对局", new Vector2(560f, 430f));
        AddText(panel, "MatchSummary", FirstPlayableSummary, 18, TextAnchor.MiddleCenter, 150f);
        AddButton(panel, "ConfirmLocalMatchButton", "确认并开始", StartFirstPlayable);
        AddButton(panel, "BackButton", "返回", BuildMainView);
    }

    private void StartFirstPlayable()
    {
        UIService?.StartLocalFirstPlayable(developmentSeed, developmentFastMode);
    }

    private void DecreaseDevelopmentSeed() { developmentSeed = Mathf.Max(1, developmentSeed - 1); BuildMainView(); }
    private void IncreaseDevelopmentSeed() { developmentSeed = developmentSeed == int.MaxValue ? 1 : developmentSeed + 1; BuildMainView(); }
    private void ToggleDevelopmentFastMode() { developmentFastMode = !developmentFastMode; BuildMainView(); }

    private static RectTransform AddHorizontalRow(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        var element = go.AddComponent<LayoutElement>();
        element.preferredHeight = 44f;
        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        return rect;
    }

    private void BuildArchiveView()
    {
        var panel = RebuildPanel("纹身与元素图鉴", new Vector2(620f, 540f));
        AddText(panel, "ArchiveContent",
            "首版图案\nP01：命中后聚焦单个目标的效果。\nP02：命中后向邻近目标扩散的效果。\n\n元素层级\n火 / 冰 / 雷均分为弱、标准、强三层；反应消耗一层。\n热震、超载、凝滞按事件优先级进入队列依次结算。\n反应没有全局冷却，首版反应不会继续产生新反应事件。",
            16, TextAnchor.MiddleLeft, 360f);
        AddButton(panel, "BackButton", "返回", BuildMainView);
    }

    private void BuildHelpView()
    {
        var panel = RebuildPanel("玩法说明", new Vector2(620f, 560f));
        AddText(panel, "HelpContent",
            "目标：在五轮纯PVP流程中探索地图资源，并与另外两支双人队伍对抗；四次缩圈会逐步压缩活动空间。\n\n构筑：仅在构筑阶段修改六个纹身槽位；装备消耗10份对应颜料，移除返还6份。\n情报：每次构筑开始时公开六名参与者上一阶段结束时的构筑快照和整局成果。\n倒地：队友可在3米内持续救援；进入下一次构筑阶段仍未获救会被淘汰。\n协作：队友之间没有友伤，可发起颜料请求并在对方同意后转移。",
            16, TextAnchor.MiddleLeft, 380f);
        AddButton(panel, "BackButton", "返回", BuildMainView);
    }

    private void BuildCreditsView()
    {
        var panel = RebuildPanel("制作人员", new Vector2(520f, 330f));
        AddText(panel, "CreditsContent", "游戏设计与开发：独立开发者\n协作实现与验证：AI 开发工具链\n\n当前版本为结构完整的首个可玩版本。", 17, TextAnchor.MiddleCenter, 170f);
        AddButton(panel, "BackButton", "返回", BuildMainView);
    }

    private void BuildExitConfirmation()
    {
        var panel = RebuildPanel("确认退出", new Vector2(480f, 300f));
        AddText(panel, "ExitPrompt", "确定要退出游戏吗？", 18, TextAnchor.MiddleCenter, 100f);
        AddButton(panel, "ConfirmExitButton", "退出", ExitApplication);
        AddButton(panel, "BackButton", "取消", BuildMainView);
    }

    private static void ExitApplication()
    {
        GFTrace.Info("TotemUI", "MainMenu.ExitConfirmed", null, GFTrace.Data("formId", "UI-FP-EXIT-001"));
#if UNITY_EDITOR
        GFTrace.Info("TotemUI", "MainMenu.ExitSkippedInEditor");
#else
        Application.Quit();
#endif
    }

    public static string FormatBuildInfo()
    {
        string version = string.IsNullOrWhiteSpace(Application.version) ? "0.0.0" : Application.version;
        return $"版本 {version} · First Playable · UI-FP-MAIN-001";
    }
}
