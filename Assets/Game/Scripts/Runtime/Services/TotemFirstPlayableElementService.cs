using System;
using System.Collections.Generic;

public sealed class TotemFirstPlayableElementService : TotemRuntimeServiceBase, ITotemRuntimeTickService, ITotemGameplaySimulationService
{
    private readonly Dictionary<int, TotemFirstPlayableElementState> states =
        new Dictionary<int, TotemFirstPlayableElementState>(TotemFirstPlayableRules.ParticipantCount);
    private TotemMatchFlowService matchFlowService;
    private TotemActorService actorService;
    private TotemFirstPlayableSocialService socialService;

    public override string ServiceName => "Element";

    public event Action<int, TotemElementApplyResult> ElementApplied;

    public event Action<int, int, float> FireTicksReady;

    public event Action ElementStatesReset;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        matchFlowService = runtime.GetService<TotemMatchFlowService>();
        actorService = runtime.GetService<TotemActorService>();
        socialService = runtime.GetService<TotemFirstPlayableSocialService>();
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

        matchFlowService = null;
        actorService = null;
        socialService = null;
        states.Clear();
        ElementApplied = null;
        FireTicksReady = null;
        ElementStatesReset = null;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        foreach (KeyValuePair<int, TotemFirstPlayableElementState> pair in states)
        {
            float remaining = deltaTime;
            while (remaining > 0f)
            {
                float step = Math.Min(remaining, TotemFirstPlayableElementRules.FireTickSeconds);
                pair.Value.TryGetLayerSource(0, out TotemElementLayerSource oldestSource);
                TotemElementAdvanceResult result = pair.Value.Advance(step, gameplaySuspended: false);
                if (result.FireTickCount > 0)
                {
                    FireTicksReady?.Invoke(pair.Key, result.FireTickCount, result.FireTierMultiplier);
                    float requestedDamage = TotemFirstPlayableElementRules.FireBaseTickDamage * result.FireTierMultiplier;
                    float appliedDamage = ApplyFireTickDamage(pair.Key, oldestSource.SourceParticipantId, requestedDamage);
                    if (appliedDamage > 0f)
                    {
                        socialService?.RecordIndirectElementDamage(oldestSource.SourceParticipantId, appliedDamage);
                    }
                }

                remaining -= step;
            }
        }
    }

    public TotemFirstPlayableElementState GetOrCreateState(int combatantId)
    {
        if (combatantId <= 0)
        {
            return null;
        }

        if (!states.TryGetValue(combatantId, out TotemFirstPlayableElementState state))
        {
            state = new TotemFirstPlayableElementState();
            states.Add(combatantId, state);
        }

        return state;
    }

    public TotemElementApplyResult ApplyElement(
        int targetCombatantId,
        TotemFirstPlayableElement element,
        TotemParticipantId sourceParticipantId,
        int applicationSequence,
        float bodyHitDamage)
    {
        TotemFirstPlayableElementState state = GetOrCreateState(targetCombatantId);
        if (state == null)
        {
            return default;
        }

        TotemElementApplyResult result = state.Apply(element, sourceParticipantId, applicationSequence, bodyHitDamage);
        if (result.Applied)
        {
            ElementApplied?.Invoke(targetCombatantId, result);
        }

        return result;
    }

    public float GetMoveSpeedMultiplier(int combatantId)
    {
        return states.TryGetValue(combatantId, out TotemFirstPlayableElementState state)
            ? 1f - state.IceSlowRatio
            : 1f;
    }

    public float ModifyDirectDamage(int combatantId, float directDamage)
    {
        return states.TryGetValue(combatantId, out TotemFirstPlayableElementState state)
            ? state.ApplyStasisDirectDamageModifier(directDamage)
            : Math.Max(0f, directDamage);
    }

    public bool TryBeginLightningDischarge(int combatantId, bool effectiveDirectDamage)
    {
        return states.TryGetValue(combatantId, out TotemFirstPlayableElementState state)
            && state.TryBeginLightningDischarge(effectiveDirectDamage);
    }

    public bool TryGetOldestLayerSource(int combatantId, out TotemElementLayerSource source)
    {
        if (states.TryGetValue(combatantId, out TotemFirstPlayableElementState state))
        {
            return state.TryGetLayerSource(0, out source);
        }

        source = default;
        return false;
    }

    public void ResetMatchState()
    {
        if (states.Count == 0)
        {
            return;
        }

        states.Clear();
        ElementStatesReset?.Invoke();
    }

    private void OnPhaseChanged(TotemMatchPhase previous, TotemMatchPhase current)
    {
        if (current == TotemMatchPhase.FrontEnd)
        {
            ResetMatchState();
        }
    }

    private float ApplyFireTickDamage(int combatantId, TotemParticipantId sourceParticipantId, float amount)
    {
        if (!sourceParticipantId.IsValid || amount <= 0f)
        {
            return 0f;
        }

        TotemActorModel source = FindParticipant(sourceParticipantId);
        if (source == null)
        {
            return 0f;
        }

        IReadOnlyList<TotemActorModel> actors = actorService?.Actors;
        for (int i = 0; actors != null && i < actors.Count; i++)
        {
            TotemActorModel target = actors[i];
            if (target?.CombatantId != combatantId)
            {
                continue;
            }

            return actorService.TryApplyDamage(target, amount, source, "Element:FireTick")
                   && actorService.LastDamage.Target == target
                ? Math.Max(0f, actorService.LastDamage.Amount)
                : 0f;
        }

        return 0f;
    }

    private TotemActorModel FindParticipant(TotemParticipantId participantId)
    {
        IReadOnlyList<TotemActorModel> actors = actorService?.Actors;
        for (int i = 0; actors != null && i < actors.Count; i++)
        {
            if (actors[i]?.ParticipantId == participantId.Value)
            {
                return actors[i];
            }
        }

        return null;
    }
}
