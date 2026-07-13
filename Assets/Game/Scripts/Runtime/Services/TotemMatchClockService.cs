using UnityEngine;

public sealed class TotemMatchClockService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    private TotemGameFlowService flowService;

    public override string ServiceName => "MatchClock";

    public float WorldTime { get; private set; }

    public bool IsWorldActive { get; private set; }

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
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

        WorldTime = 0f;
        IsWorldActive = false;
    }

    public void Tick(float deltaTime)
    {
        if (IsWorldActive)
        {
            WorldTime += Mathf.Max(0f, deltaTime);
        }
    }

#if UNITY_EDITOR
    public void SetWorldTimeForDiagnostics(float worldTime)
    {
        WorldTime = Mathf.Max(0f, worldTime);
    }
#endif

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            WorldTime = 0f;
            IsWorldActive = true;
            GFTrace.Success("TotemMatch", "World.Active", null, GFTrace.Data("worldTime", "0"));
            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            IsWorldActive = false;
            GFTrace.Info("TotemMatch", "World.Inactive", null, GFTrace.Data(
                "worldTime", WorldTime.ToString("F2"),
                "nextState", nextState.ToString()));
        }
    }
}

