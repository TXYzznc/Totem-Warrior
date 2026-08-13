using UnityEngine;

public sealed class TotemPauseMenuForm : TotemOverlayFormBase
{
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        Time.timeScale = 0f;
        BuildView();
        GFTrace.Success("TotemUI", "PauseMenu.Open");
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        Time.timeScale = 1f;
        base.OnClose(isShutdown, userData);
    }

    private void BuildView()
    {
        var panel = RebuildPanel("Paused", new Vector2(440f, 330f));
        AddText(panel, "Status", FormatStatus(CombatService?.CaptureCombatSnapshot()), 16, TextAnchor.MiddleCenter, 46f);
        AddButton(panel, "ResumeButton", "Resume", OnClickClose);
        AddButton(panel, "SettingsButton", "Settings", OnSettingsClicked);
        AddButton(panel, "MainMenuButton", "Return To Main Menu", OnReturnToMainMenuClicked);
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
            return "Combat paused";
        }

        return $"HP {snapshot.playerHealth:F0}  Alive {snapshot.aliveParticipantCount}  Eliminations {snapshot.killCount}";
    }
}
