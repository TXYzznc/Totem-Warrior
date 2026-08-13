using System.Collections.Generic;

public sealed class TotemFirstPlayableTattooBuildService : TotemRuntimeServiceBase
{
    private readonly Dictionary<int, TotemFirstPlayableTattooBuildState> states = new Dictionary<int, TotemFirstPlayableTattooBuildState>(TotemFirstPlayableRules.ParticipantCount);
    private TotemMatchFlowService matchFlowService;
    private TotemActorService actorService;

    public override string ServiceName => "TattooBuild";

    public event System.Action<TotemActorModel, TotemTattooMutationResult> BuildChanged;

    public event System.Action BuildReset;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        matchFlowService = runtime.GetService<TotemMatchFlowService>();
        actorService = runtime.GetService<TotemActorService>();
        if (matchFlowService != null)
        {
            matchFlowService.PhaseChanged += OnPhaseChanged;
        }
    }

    protected override void OnShutdown()
    {
        if (matchFlowService != null)
        {
            matchFlowService.PhaseChanged -= OnPhaseChanged;
        }

        states.Clear();
        matchFlowService = null;
        actorService = null;
        BuildChanged = null;
        BuildReset = null;
    }

    public TotemFirstPlayableTattooBuildState GetOrCreateState(TotemActorModel actor)
    {
        if (actor == null)
        {
            return null;
        }

        if (!states.TryGetValue(actor.ParticipantId, out var state))
        {
            state = new TotemFirstPlayableTattooBuildState();
            states.Add(actor.ParticipantId, state);
        }

        return state;
    }

    public void AddPigment(TotemActorModel actor, TotemPigmentKind pigment, int amount)
    {
        GetOrCreateState(actor)?.AddPigment(pigment, amount);
    }

    public bool TryEquip(
        TotemActorModel actor,
        TotemTattooSlotId slot,
        TotemFirstPlayablePatternId pattern,
        TotemFirstPlayableElement element,
        out TotemTattooMutationResult result)
    {
        TotemFirstPlayableTattooBuildState state = GetOrCreateState(actor);
        if (state == null)
        {
            result = default;
            return false;
        }

        if (!state.TryEquip(CurrentPhase, slot, pattern, element, out result))
        {
            return false;
        }

        BuildChanged?.Invoke(actor, result);
        return true;
    }

    public bool TryRemove(
        TotemActorModel actor,
        TotemTattooSlotId slot,
        out TotemTattooMutationResult result)
    {
        TotemFirstPlayableTattooBuildState state = GetOrCreateState(actor);
        if (state == null)
        {
            result = default;
            return false;
        }

        if (!state.TryRemove(CurrentPhase, slot, out result))
        {
            return false;
        }

        BuildChanged?.Invoke(actor, result);
        return true;
    }

    public bool TryApplyCommand(in TotemGameplayCommand command, out TotemTattooMutationResult result)
    {
        TotemActorModel actor = FindActor(command.ParticipantId);
        TotemFirstPlayableTattooBuildState state = GetOrCreateState(actor);
        if (state == null)
        {
            result = default;
            return false;
        }

        if (!state.TryApplyCommand(CurrentPhase, command, out result))
        {
            return false;
        }

        BuildChanged?.Invoke(actor, result);
        return true;
    }

    private TotemMatchPhase CurrentPhase => matchFlowService?.CurrentPhase ?? TotemMatchPhase.FrontEnd;

    private TotemActorModel FindActor(TotemParticipantId participantId)
    {
        if (!participantId.IsValid || actorService == null)
        {
            return null;
        }

        var actors = actorService.Actors;
        for (int i = 0; i < actors.Count; i++)
        {
            if (actors[i] != null && actors[i].ParticipantId == participantId.Value)
            {
                return actors[i];
            }
        }

        return null;
    }

    public void ResetMatchState()
    {
        if (states.Count == 0)
        {
            return;
        }

        states.Clear();
        BuildReset?.Invoke();
    }

    private void OnPhaseChanged(TotemMatchPhase previous, TotemMatchPhase current)
    {
        if (current == TotemMatchPhase.FrontEnd)
        {
            ResetMatchState();
        }
    }
}
