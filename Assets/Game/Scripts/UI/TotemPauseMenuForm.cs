using UnityEngine;

public sealed class TotemPauseMenuForm : TotemOverlayFormBase
{
    [SerializeField] private TMPro.TMP_Text statusText;
    [SerializeField] private UnityEngine.UI.Button resumeButton;
    [SerializeField] private UnityEngine.UI.Button settingsButton;
    [SerializeField] private UnityEngine.UI.Button mainMenuButton;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        statusText ??= FindChildComponent<TMPro.TMP_Text>("Txt_PauseStatus");
        resumeButton = BindButton(resumeButton, "Btn_Resume", OnClickClose);
        settingsButton = BindButton(settingsButton, "Btn_PauseSettings", OnSettingsClicked);
        mainMenuButton = BindButton(mainMenuButton, "Btn_ReturnMainMenu", OnReturnToMainMenuClicked);
    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        Time.timeScale = 0f;
        statusText?.SetText(FormatStatus(CombatService?.CaptureCombatSnapshot()));
        GFTrace.Success("TotemUI", "PauseMenu.Open");
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        Time.timeScale = 1f;
        base.OnClose(isShutdown, userData);
    }

    private void OnSettingsClicked()
    {
        UIService?.OpenSettings();
    }

    private void OnReturnToMainMenuClicked()
    {
        Time.timeScale = 1f;
        UIService?.OpenMainMenu();
    }

    public static string FormatStatus(TotemCombatSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return "战斗已暂停";
        }

        return $"生命 {snapshot.playerHealth:F0} · 存活 {snapshot.aliveParticipantCount} · 淘汰 {snapshot.killCount}";
    }
}
