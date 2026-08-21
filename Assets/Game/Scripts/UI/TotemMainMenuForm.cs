using UnityEngine;
using UnityEngine.UI;

public sealed class TotemMainMenuForm : TotemOverlayFormBase
{
    public const string FirstPlayableSummary = TotemFirstPlayableUiText.MainMenuSummary;

    [SerializeField] private Button startButton;
    [SerializeField] private Button archiveButton;
    [SerializeField] private Button helpButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button exitButton;
    private GameObject activeMenuOverlay;
    private int developmentSeed = 1;
    private bool developmentFastMode;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        startButton = BindButton(startButton, "Btn_StartLocal", OnStartClicked);
        archiveButton = BindButton(archiveButton, "Btn_Archive", BuildArchiveView);
        helpButton = BindButton(helpButton, "Btn_Help", BuildHelpView);
        settingsButton = BindButton(settingsButton, "Btn_Settings", OnSettingsClicked);
        creditsButton = BindButton(creditsButton, "Btn_Credits", BuildCreditsView);
        exitButton = BindButton(exitButton, "Btn_Exit", BuildExitConfirmation);
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
        ClearRuntimeOverlay();
        if (activeMenuOverlay != null)
        {
            activeMenuOverlay.SetActive(false);
            activeMenuOverlay = null;
        }
    }

    private void BuildLocalMatchConfirmation()
    {
        if (ShowMenuOverlay("Overlay_LocalConfirm"))
        {
            BindButton(null, "Btn_ConfirmLocalMatch", StartFirstPlayable);
            BindButton(null, "Btn_ConfirmBack", BuildMainView);
            BindButton(null, "Btn_SeedDecrease", DecreaseDevelopmentSeed);
            BindButton(null, "Btn_SeedIncrease", IncreaseDevelopmentSeed);
            BindButton(null, "Btn_FastMode", ToggleDevelopmentFastMode);
            RefreshDevelopmentMatchSettings();
            return;
        }

        GFTrace.Warning("TotemUI", "MainMenu.LocalConfirmMissing");
    }

    private void StartFirstPlayable()
    {
        UIService?.StartLocalFirstPlayable(developmentSeed, developmentFastMode);
    }

    private void DecreaseDevelopmentSeed()
    {
        developmentSeed = Mathf.Max(1, developmentSeed - 1);
        RefreshDevelopmentMatchSettings();
    }

    private void IncreaseDevelopmentSeed()
    {
        developmentSeed = developmentSeed == int.MaxValue ? 1 : developmentSeed + 1;
        RefreshDevelopmentMatchSettings();
    }

    private void ToggleDevelopmentFastMode()
    {
        developmentFastMode = !developmentFastMode;
        RefreshDevelopmentMatchSettings();
    }

    private void RefreshDevelopmentMatchSettings()
    {
        TMPro.TMP_Text settings = FindChildComponent<TMPro.TMP_Text>("Txt_ConfirmDevSettings");
        if (settings != null)
        {
            settings.SetText($"开发设置 · 种子 {developmentSeed} · 快速模式：{(developmentFastMode ? "开启" : "关闭")}");
        }

        Button fastModeButton = FindChildComponent<Button>("Btn_FastMode");
        TMPro.TMP_Text fastModeLabel = fastModeButton?.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (fastModeLabel != null)
        {
            fastModeLabel.SetText($"快速模式：{(developmentFastMode ? "开启" : "关闭")}");
        }
    }

    private void BuildArchiveView()
    {
        if (ShowMenuOverlay("Overlay_Archive"))
        {
            BindButton(null, "Btn_ArchiveBack", BuildMainView);
            return;
        }

        GFTrace.Warning("TotemUI", "MainMenu.ArchiveMissing");
    }

    private void BuildHelpView()
    {
        if (ShowMenuOverlay("Overlay_Help"))
        {
            BindButton(null, "Btn_HelpBack", BuildMainView);
            return;
        }

        GFTrace.Warning("TotemUI", "MainMenu.HelpMissing");
    }

    private void BuildCreditsView()
    {
        if (ShowMenuOverlay("Overlay_Credits"))
        {
            BindButton(null, "Btn_CreditsBack", BuildMainView);
            return;
        }

        GFTrace.Warning("TotemUI", "MainMenu.CreditsMissing");
    }

    private void BuildExitConfirmation()
    {
        if (ShowMenuOverlay("Overlay_ExitConfirm"))
        {
            BindButton(null, "Btn_ExitConfirm", ExitApplication);
            BindButton(null, "Btn_ExitCancel", BuildMainView);
            return;
        }

        GFTrace.Warning("TotemUI", "MainMenu.ExitConfirmMissing");
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

    private bool ShowMenuOverlay(string overlayName)
    {
        if (activeMenuOverlay != null)
        {
            activeMenuOverlay.SetActive(false);
            activeMenuOverlay = null;
        }

        Transform overlay = transform.Find(overlayName);
        if (overlay == null)
        {
            return false;
        }

        activeMenuOverlay = overlay.gameObject;
        activeMenuOverlay.SetActive(true);
        return true;
    }
}
