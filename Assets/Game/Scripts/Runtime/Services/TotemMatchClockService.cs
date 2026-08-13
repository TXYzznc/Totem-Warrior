using UnityEngine;

public sealed class TotemMatchClockService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    private TotemGameFlowService flowService;
    private TotemMatchFlowService matchFlowService;
    private readonly TotemMatchClockAccumulator clock = new TotemMatchClockAccumulator();

    public override string ServiceName => "MatchClock";

    public float WorldTime => clock.WorldTime;

    public bool IsWorldActive => clock.IsWorldActive;

    public float UiTime => clock.UiTime;

    public bool IsGameplaySuspended => matchFlowService?.IsGameplaySuspended ?? false;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        matchFlowService = runtime.GetService<TotemMatchFlowService>();
        if (flowService != null)
        {
            flowService.StateChanged += OnFlowStateChanged;
        }
    }

    protected override void OnShutdown()
    {
        if (flowService != null)
        {
            flowService.StateChanged -= OnFlowStateChanged;
            flowService = null;
        }

        clock.Reset();
        matchFlowService = null;
    }

    public void Tick(float deltaTime)
    {
        Advance(deltaTime, Time.unscaledDeltaTime);
    }

    public void Advance(float gameplayDeltaTime, float unscaledUiDeltaTime)
    {
        clock.Advance(gameplayDeltaTime, unscaledUiDeltaTime, IsGameplaySuspended);
    }

#if UNITY_EDITOR
    public void SetWorldTimeForDiagnostics(float worldTime)
    {
        clock.SetWorldTimeForDiagnostics(worldTime);
    }
#endif

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            clock.Activate();
            GFTrace.Success("TotemMatch", "World.Active", null, GFTrace.Data("worldTime", "0"));
            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            clock.Deactivate();
            GFTrace.Info("TotemMatch", "World.Inactive", null, GFTrace.Data(
                "worldTime", WorldTime.ToString("F2"),
                "nextState", nextState.ToString()));
        }
    }
}
