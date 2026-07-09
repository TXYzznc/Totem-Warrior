using UnityEngine;
using UnityEngine.UI;

public sealed class TotemMainMenuForm : TotemUIFormBase
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;

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
        GFTrace.Success("TotemUI", "MainMenu.Open");
    }

    public void OnStartClicked()
    {
        GFTrace.Info("TotemUI", "MainMenu.StartClicked");
        UIService?.OpenCharacterSelect();
    }

    private void OnSettingsClicked()
    {
        GFTrace.Info("TotemUI", "MainMenu.SettingsClicked");
        UIService?.OpenSettings();
    }
}
