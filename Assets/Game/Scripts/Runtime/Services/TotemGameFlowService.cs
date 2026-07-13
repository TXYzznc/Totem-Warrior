using System;

public enum TotemGameFlowState
{
    None = 0,
    MainMenu = 1,
    CharacterSelect = 2,
    StartupSelect = 3,
    CombatHud = 4,
}

public sealed class TotemStartupSelection
{
    public int CharacterId { get; set; }
    public int ColorId { get; set; }
    public string WeaponId { get; set; }
    public int[] PatternIds { get; set; } = Array.Empty<int>();
}

public sealed class TotemGameFlowService : TotemRuntimeServiceBase
{
    public override string ServiceName => "GameFlow";

    public TotemGameFlowState CurrentState { get; private set; } = TotemGameFlowState.None;

    public int SelectedCharacterId { get; private set; } = 1;

    public TotemStartupSelection StartupSelection { get; private set; } = new TotemStartupSelection
    {
        CharacterId = 1,
        ColorId = 1,
        WeaponId = "knife_basic",
        PatternIds = new[] { 1 },
    };

    public event Action<TotemGameFlowState, TotemGameFlowState> StateChanged;

    public void EnterMainMenu()
    {
        ChangeState(TotemGameFlowState.MainMenu);
    }

    public void EnterCharacterSelect()
    {
        ChangeState(TotemGameFlowState.CharacterSelect);
    }

    public void SelectCharacter(int characterId)
    {
        SelectedCharacterId = characterId <= 0 ? 1 : characterId;
        StartupSelection.CharacterId = SelectedCharacterId;
        GFTrace.Info("TotemFlow", "Character.Selected", null, GFTrace.Data("characterId", SelectedCharacterId.ToString()));
    }

    public void EnterStartupSelect()
    {
        ChangeState(TotemGameFlowState.StartupSelect);
    }

    public void EnterCombatHud()
    {
        ChangeState(TotemGameFlowState.CombatHud);
    }

    public void ConfirmStartup(int colorId, string weaponId, int[] patternIds)
    {
        StartupSelection = new TotemStartupSelection
        {
            CharacterId = SelectedCharacterId <= 0 ? 1 : SelectedCharacterId,
            ColorId = colorId <= 0 ? 1 : colorId,
            WeaponId = string.IsNullOrWhiteSpace(weaponId) ? "knife_basic" : weaponId,
            PatternIds = patternIds == null || patternIds.Length == 0 ? new[] { 1 } : patternIds,
        };

        GFTrace.Success("TotemFlow", "Startup.Confirmed", null, GFTrace.Data(
            "characterId", StartupSelection.CharacterId.ToString(),
            "colorId", StartupSelection.ColorId.ToString(),
            "weaponId", StartupSelection.WeaponId,
            "patterns", string.Join(",", StartupSelection.PatternIds)));

        if (UnityEngine.Application.isPlaying)
        {
            TotemGameplaySceneLoader.Begin(TotemGameRuntime.Instance);
        }
        else
        {
            EnterCombatHud();
        }
    }

    private void ChangeState(TotemGameFlowState nextState)
    {
        if (CurrentState == nextState)
        {
            return;
        }

        var previousState = CurrentState;
        CurrentState = nextState;
        GFTrace.Success("TotemFlow", "StateChanged", null, GFTrace.Data(
            "from", previousState.ToString(),
            "to", CurrentState.ToString()));
        StateChanged?.Invoke(previousState, CurrentState);
    }
}
