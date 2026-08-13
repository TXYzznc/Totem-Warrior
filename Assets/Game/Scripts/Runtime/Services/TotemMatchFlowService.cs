using System;
using UnityEngine;

public sealed class TotemMatchFlowService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    private TotemGameFlowService gameFlow;
    private TotemMatchTimingConfig timing = new TotemMatchTimingConfig();
    private bool running;
    private bool fastMode;
    private float phaseElapsed;
    private float activityElapsed;

    public override string ServiceName => "MatchFlow";

    public TotemMatchPhase CurrentPhase { get; private set; } = TotemMatchPhase.FrontEnd;
    public TotemMatchActivity CurrentActivity { get; private set; } = TotemMatchActivity.FrontEnd;
    public bool IsRunning => running;
    public bool FastMode => fastMode;
    public bool IsGameplaySuspended => running && TotemMatchPhaseContract.IsGameplaySuspended(CurrentPhase);
    public bool IsZoneShrinking => running && CurrentActivity == TotemMatchActivity.ZoneShrink;
    public float PhaseElapsed => phaseElapsed;
    public float ActivityElapsed => activityElapsed;
    public float ActivityDuration => ResolveActivityDuration(CurrentPhase, CurrentActivity);
    public float ActivityRemaining => Mathf.Max(0f, ActivityDuration - activityElapsed);

    public event Action<TotemMatchPhase, TotemMatchPhase> PhaseChanged;
    public event Action<TotemMatchActivity, TotemMatchActivity> ActivityChanged;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        gameFlow = runtime.GetService<TotemGameFlowService>();
        if (gameFlow != null)
        {
            gameFlow.StateChanged += OnGameFlowStateChanged;
        }
    }

    protected override void OnShutdown()
    {
        if (gameFlow != null)
        {
            gameFlow.StateChanged -= OnGameFlowStateChanged;
            gameFlow = null;
        }

        ResetToFrontEnd();
        PhaseChanged = null;
        ActivityChanged = null;
    }

    public void Tick(float deltaTime)
    {
        if (!running)
        {
            return;
        }

        Advance(Time.unscaledDeltaTime);
    }

    public void BeginMatch(bool useFastMode = false)
    {
        fastMode = useFastMode;
        running = true;
        SetPhase(TotemMatchPhase.OpeningBuild, TotemMatchActivity.Build, "Match.Begin");
    }

    public void Advance(float unscaledDeltaTime)
    {
        float remaining = Mathf.Max(0f, unscaledDeltaTime);
        int transitionGuard = 0;
        while (running && remaining > 0f && transitionGuard++ < 16)
        {
            float duration = ActivityDuration;
            if (duration <= 0f)
            {
                AdvanceBoundary();
                continue;
            }

            float step = Mathf.Min(remaining, Mathf.Max(0f, duration - activityElapsed));
            phaseElapsed += step;
            activityElapsed += step;
            remaining -= step;
            if (activityElapsed + 0.0001f >= duration)
            {
                AdvanceBoundary();
            }
        }
    }

    public void Configure(TotemMatchTimingConfig config, bool useFastMode)
    {
        timing = config ?? new TotemMatchTimingConfig();
        fastMode = useFastMode;
    }

#if UNITY_EDITOR
    public void CompleteCurrentActivityForDiagnostics()
    {
        if (running)
        {
            Advance(Mathf.Max(0.001f, ActivityRemaining));
        }
    }
#endif

    private void AdvanceBoundary()
    {
        switch (CurrentPhase)
        {
            case TotemMatchPhase.OpeningBuild:
                SetPhase(TotemMatchPhase.Round1Combat, TotemMatchActivity.Combat, "OpeningBuild.Complete");
                break;
            case TotemMatchPhase.Round1Combat:
                SetPhase(TotemMatchPhase.Build2, TotemMatchActivity.Build, "Round1.Complete");
                break;
            case TotemMatchPhase.Build2:
                SetPhase(TotemMatchPhase.Round2Combat, TotemMatchActivity.ZoneShrink, "Build2.Complete");
                break;
            case TotemMatchPhase.Round2Combat:
                if (CurrentActivity == TotemMatchActivity.ZoneShrink)
                {
                    SetActivity(TotemMatchActivity.Combat, "Shrink1.Complete");
                }
                else
                {
                    SetPhase(TotemMatchPhase.Build3, TotemMatchActivity.Build, "Round2.Complete");
                }
                break;
            case TotemMatchPhase.Build3:
                SetPhase(TotemMatchPhase.Round3Combat, TotemMatchActivity.ZoneShrink, "Build3.Complete");
                break;
            case TotemMatchPhase.Round3Combat:
                if (CurrentActivity == TotemMatchActivity.ZoneShrink)
                {
                    SetActivity(TotemMatchActivity.Combat, "Shrink2.Complete");
                }
                else
                {
                    SetPhase(TotemMatchPhase.Build4, TotemMatchActivity.Build, "Round3.Complete");
                }
                break;
            case TotemMatchPhase.Build4:
                SetPhase(TotemMatchPhase.Round4Combat, TotemMatchActivity.ZoneShrink, "Build4.Complete");
                break;
            case TotemMatchPhase.Round4Combat:
                if (CurrentActivity == TotemMatchActivity.ZoneShrink)
                {
                    SetActivity(TotemMatchActivity.Combat, "Shrink3.Complete");
                }
                else
                {
                    SetPhase(TotemMatchPhase.Build5, TotemMatchActivity.Build, "Round4.Complete");
                }
                break;
            case TotemMatchPhase.Build5:
                SetPhase(TotemMatchPhase.Round5Combat, TotemMatchActivity.ZoneShrink, "Build5.Complete");
                break;
            case TotemMatchPhase.Round5Combat:
                if (CurrentActivity == TotemMatchActivity.ZoneShrink)
                {
                    SetActivity(TotemMatchActivity.Combat, "Shrink4.Complete");
                }
                else
                {
                    TotemCombatService combat = Runtime?.GetService<TotemCombatService>();
                    if (combat != null)
                    {
                        combat.FinishFiveRoundFlow();
                    }
                    else
                    {
                        CompleteMatchToResult("Round5.Complete");
                    }
                }
                break;
        }
    }

    public void CompleteMatchToResult(string reason)
    {
        if (CurrentPhase == TotemMatchPhase.Result)
        {
            running = false;
            return;
        }

        running = false;
        SetPhase(TotemMatchPhase.Result, TotemMatchActivity.Result, reason);
    }

    private float ResolveActivityDuration(TotemMatchPhase phase, TotemMatchActivity activity)
    {
        switch (activity)
        {
            case TotemMatchActivity.Build:
                return timing.ResolveBuildSeconds(phase);
            case TotemMatchActivity.ZoneShrink:
                return timing.ResolveShrinkSeconds(fastMode);
            case TotemMatchActivity.Combat:
                return timing.ResolveCombatSeconds(fastMode);
            default:
                return float.PositiveInfinity;
        }
    }

    private void SetPhase(TotemMatchPhase nextPhase, TotemMatchActivity nextActivity, string reason)
    {
        TotemMatchPhase previousPhase = CurrentPhase;
        TotemMatchActivity previousActivity = CurrentActivity;
        CurrentPhase = nextPhase;
        CurrentActivity = nextActivity;
        phaseElapsed = 0f;
        activityElapsed = 0f;
        GFTrace.Success("TotemMatch", "Phase.Changed", null, GFTrace.Data(
            "from", previousPhase.ToString(),
            "to", nextPhase.ToString(),
            "activity", nextActivity.ToString(),
            "reason", reason ?? string.Empty));
        if (previousPhase != nextPhase)
        {
            PhaseChanged?.Invoke(previousPhase, nextPhase);
        }

        if (previousActivity != nextActivity)
        {
            ActivityChanged?.Invoke(previousActivity, nextActivity);
        }
    }

    private void SetActivity(TotemMatchActivity nextActivity, string reason)
    {
        TotemMatchActivity previous = CurrentActivity;
        CurrentActivity = nextActivity;
        activityElapsed = 0f;
        GFTrace.Success("TotemMatch", "Activity.Changed", null, GFTrace.Data(
            "phase", CurrentPhase.ToString(),
            "from", previous.ToString(),
            "to", nextActivity.ToString(),
            "reason", reason ?? string.Empty));
        ActivityChanged?.Invoke(previous, nextActivity);
    }

    private void OnGameFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            BeginMatch(fastMode);
            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            ResetToFrontEnd();
        }
    }

    private void ResetToFrontEnd()
    {
        TotemMatchPhase previousPhase = CurrentPhase;
        TotemMatchActivity previousActivity = CurrentActivity;
        running = false;
        fastMode = false;
        CurrentPhase = TotemMatchPhase.FrontEnd;
        CurrentActivity = TotemMatchActivity.FrontEnd;
        phaseElapsed = 0f;
        activityElapsed = 0f;
        if (previousPhase != CurrentPhase)
        {
            PhaseChanged?.Invoke(previousPhase, CurrentPhase);
        }

        if (previousActivity != CurrentActivity)
        {
            ActivityChanged?.Invoke(previousActivity, CurrentActivity);
        }
    }
}
