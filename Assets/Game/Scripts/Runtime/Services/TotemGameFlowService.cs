using System;

public enum TotemGameFlowState
{
    None = 0,
    MainMenu = 1,
    CombatHud = 4,
}

public sealed class TotemGameFlowService : TotemRuntimeServiceBase
{
    public override string ServiceName => "GameFlow";

    public TotemGameFlowState CurrentState { get; private set; } = TotemGameFlowState.None;

    public event Action<TotemGameFlowState, TotemGameFlowState> StateChanged;

    public void EnterMainMenu()
    {
        TotemGameplaySceneLoader.CancelPending("EnterMainMenu");
        ChangeState(TotemGameFlowState.MainMenu);
        if (UnityEngine.Application.isPlaying)
        {
            TotemGameplaySceneLoader.UnloadGameplayScene();
        }
    }

    public void EnterCombatHud()
    {
        ChangeState(TotemGameFlowState.CombatHud);
    }

    public void ConfirmLocalFirstPlayable()
    {
        GFTrace.Success("TotemFlow", "Startup.Confirmed", null, GFTrace.Data(
            "characterId", "1",
            "weaponId", TotemWeaponService.DefaultWeaponId,
            "patterns", "1,2"));

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
