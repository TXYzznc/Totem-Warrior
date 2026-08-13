using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TotemFirstPlayableLifecycleService : TotemRuntimeServiceBase, ITotemRuntimeTickService, ITotemGameplaySimulationService
{
    public const float ReviveInteractRadius = 3f;

    private readonly Dictionary<int, TotemFirstPlayableParticipantLifeState> states =
        new Dictionary<int, TotemFirstPlayableParticipantLifeState>(TotemFirstPlayableRules.ParticipantCount);
    private TotemActorService actorService;
    private TotemMatchFlowService matchFlowService;
    private TotemInputService inputService;
    private TotemStatusService statusService;
    private TotemParticipantReadinessService readinessService;
    private TotemExtractionService extractionService;
    private int commandSequence;

    public override string ServiceName => "FirstPlayableLifecycle";

    public event Action<TotemActorModel, TotemDownedStateContract> LifeStateChanged;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        actorService = runtime.GetService<TotemActorService>();
        matchFlowService = runtime.GetService<TotemMatchFlowService>();
        inputService = runtime.GetService<TotemInputService>();
        statusService = runtime.GetService<TotemStatusService>();
        readinessService = runtime.GetService<TotemParticipantReadinessService>();
        extractionService = runtime.GetService<TotemExtractionService>();
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

        actorService = null;
        matchFlowService = null;
        inputService = null;
        statusService = null;
        readinessService = null;
        extractionService = null;
        commandSequence = 0;
        states.Clear();
        LifeStateChanged = null;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f || states.Count == 0)
        {
            return;
        }

        bool gameplaySuspended = matchFlowService?.IsGameplaySuspended ?? false;
        if (!gameplaySuspended)
        {
            TryConsumeHumanReviveInput();
        }

        foreach (var pair in states)
        {
            TotemFirstPlayableParticipantLifeState state = pair.Value;
            TotemActorModel participant = FindParticipant(pair.Key);
            if (state.Advance(deltaTime, gameplaySuspended, out TotemDownedStateContract transition))
            {
                ApplyTransitionToActor(participant, state, transition);
            }

            if (!gameplaySuspended && state.IsDowned && state.ReviverParticipantId.IsValid)
            {
                ContinueRevive(
                    participant,
                    deltaTime,
                    ResolveReviveContinuationStatus(state, participant),
                    out _);
            }
        }
    }

    public TotemFirstPlayableParticipantLifeState GetOrCreateState(TotemActorModel participant)
    {
        if (participant == null || !TotemActorService.IsParticipantActor(participant))
        {
            return null;
        }

        if (!states.TryGetValue(participant.ParticipantId, out TotemFirstPlayableParticipantLifeState state))
        {
            state = new TotemFirstPlayableParticipantLifeState(participant.MaxHealth);
            states.Add(participant.ParticipantId, state);
        }

        return state;
    }

    public bool IsDowned(TotemActorModel participant)
    {
        return participant != null
            && states.TryGetValue(participant.ParticipantId, out TotemFirstPlayableParticipantLifeState state)
            && state.IsDowned;
    }

    public bool IsReviveProtected(TotemActorModel participant)
    {
        return participant != null
            && states.TryGetValue(participant.ParticipantId, out TotemFirstPlayableParticipantLifeState state)
            && state.IsProtected;
    }

    public float GetMoveSpeedMultiplier(TotemActorModel participant)
    {
        return participant != null
            && states.TryGetValue(participant.ParticipantId, out TotemFirstPlayableParticipantLifeState state)
            ? state.MoveSpeedMultiplier
            : 1f;
    }

    public bool TryResolveLethalDamage(
        TotemActorModel target,
        TotemCombatantModel source,
        out TotemDownedStateContract transition)
    {
        TotemFirstPlayableParticipantLifeState state = GetOrCreateState(target);
        if (state == null || state.LifeState != TotemFirstPlayableLifeState.Alive)
        {
            transition = default;
            return false;
        }

        TotemCombatantReference instigator = TotemCombatantReference.FromCombatant(source);
        bool handled = HasLivingTeammate(target)
            ? state.TryEnterDowned(true, instigator, out transition)
            : state.EliminateWithoutLivingTeammate(instigator, out transition);
        if (handled)
        {
            ApplyTransitionToActor(target, state, transition);
        }

        return handled;
    }

    public bool TryApplyDownedDamage(
        TotemActorModel target,
        float amount,
        TotemCombatantModel source,
        out float appliedDamage,
        out TotemDownedStateContract transition)
    {
        if (target == null
            || !states.TryGetValue(target.ParticipantId, out TotemFirstPlayableParticipantLifeState state)
            || !state.IsDowned
            || !state.ApplyDownedDamage(
                amount,
                TotemCombatantReference.FromCombatant(source),
                out appliedDamage,
                out transition))
        {
            appliedDamage = 0f;
            transition = default;
            return false;
        }

        target.SetHealthForLifecycle(state.DownedHealth);
        if (transition.Current == TotemFirstPlayableLifeState.Eliminated)
        {
            ApplyTransitionToActor(target, state, transition);
        }

        return true;
    }

    public bool TryBeginRevive(
        TotemActorModel reviver,
        TotemActorModel target,
        out TotemDownedStateContract transition)
    {
        if (reviver == null
            || target == null
            || reviver == target
            || !reviver.TeamId.IsValid
            || reviver.TeamId != target.TeamId
            || reviver.Lifecycle != TotemParticipantLifecycle.Active
            || !CanReviverAct(reviver)
            || !IsWithinReviveRange(reviver, target)
            || !states.TryGetValue(target.ParticipantId, out TotemFirstPlayableParticipantLifeState state)
            || !state.IsDowned)
        {
            transition = default;
            return false;
        }

        bool started = state.TryBeginRevive(new TotemParticipantId(reviver.ParticipantId), out transition);
        if (started)
        {
            LifeStateChanged?.Invoke(target, transition);
        }

        return started;
    }

    public bool TryIssueBeginReviveCommand(
        TotemActorModel reviver,
        TotemActorModel target,
        TotemGameplayCommandSource source,
        out TotemDownedStateContract transition)
    {
        if (reviver == null || target == null)
        {
            transition = default;
            return false;
        }

        var command = new TotemGameplayCommand(
            new TotemParticipantId(reviver.ParticipantId),
            source,
            TotemGameplayCommandType.BeginRevive,
            ++commandSequence,
            target.Position,
            target.ParticipantId);
        return TryApplyCommand(command, out transition);
    }

    public bool TryGetNearestDownedTeammate(
        TotemActorModel reviver,
        out TotemActorModel target,
        out float distance)
    {
        target = null;
        distance = float.PositiveInfinity;
        if (reviver == null || !reviver.TeamId.IsValid)
        {
            return false;
        }

        IReadOnlyList<TotemActorModel> actors = actorService?.Actors;
        if (actors == null)
        {
            return false;
        }

        float bestSqrDistance = float.PositiveInfinity;
        for (int i = 0; i < actors.Count; i++)
        {
            TotemActorModel candidate = actors[i];
            if (candidate == null
                || candidate == reviver
                || candidate.TeamId != reviver.TeamId
                || !IsDowned(candidate))
            {
                continue;
            }

            float sqrDistance = (candidate.Position - reviver.Position).sqrMagnitude;
            if (sqrDistance >= bestSqrDistance)
            {
                continue;
            }

            bestSqrDistance = sqrDistance;
            target = candidate;
        }

        if (target == null)
        {
            return false;
        }

        distance = Mathf.Sqrt(bestSqrDistance);
        return true;
    }

    public bool IsReviving(TotemActorModel reviver)
    {
        if (reviver == null)
        {
            return false;
        }

        foreach (var pair in states)
        {
            if (pair.Value.IsDowned
                && pair.Value.ReviverParticipantId.Value == reviver.ParticipantId)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryApplyCommand(
        in TotemGameplayCommand command,
        out TotemDownedStateContract transition)
    {
        if (command.Type == TotemGameplayCommandType.BeginRevive
            && TotemReviveCommandCodec.TryDecodeTarget(
                command,
                TotemGameplayCommandType.BeginRevive,
                out TotemParticipantId beginTarget))
        {
            return TryBeginRevive(
                FindParticipant(command.ParticipantId.Value),
                FindParticipant(beginTarget.Value),
                out transition);
        }

        if (command.Type == TotemGameplayCommandType.CancelRevive
            && TotemReviveCommandCodec.TryDecodeTarget(
                command,
                TotemGameplayCommandType.CancelRevive,
                out TotemParticipantId cancelTarget)
            && states.TryGetValue(
                cancelTarget.Value,
                out TotemFirstPlayableParticipantLifeState state)
            && state.CancelRevive(
                command.ParticipantId,
                TotemDownedTransitionReason.ReviveCancelledInteraction,
                out transition))
        {
            LifeStateChanged?.Invoke(FindParticipant(cancelTarget.Value), transition);
            return true;
        }

        transition = default;
        return false;
    }

    public bool ContinueRevive(
        TotemActorModel target,
        float deltaTime,
        TotemReviveContinuationStatus status,
        out TotemDownedStateContract transition)
    {
        if (target == null
            || !states.TryGetValue(target.ParticipantId, out TotemFirstPlayableParticipantLifeState state))
        {
            transition = default;
            return false;
        }

        bool continued = state.ContinueRevive(deltaTime, status, out transition);
        if (transition.Reason == TotemDownedTransitionReason.ReviveCompleted)
        {
            ApplyTransitionToActor(target, state, transition);
        }
        else if (transition.Reason != TotemDownedTransitionReason.None)
        {
            LifeStateChanged?.Invoke(target, transition);
        }

        return continued;
    }

    public TotemSpectatorState ResolveSpectatorState(TotemActorModel participant)
    {
        if (participant == null
            || !states.TryGetValue(participant.ParticipantId, out TotemFirstPlayableParticipantLifeState state))
        {
            return TotemSpectatorState.None;
        }

        return state.ResolveSpectatorState(HasLivingTeammate(participant));
    }

    public TotemActorModel ResolveSpectatorTarget(TotemActorModel participant)
    {
        if (ResolveSpectatorState(participant) != TotemSpectatorState.SpectatingTeammate)
        {
            return null;
        }

        IReadOnlyList<TotemActorModel> actors = actorService?.Actors;
        if (actors == null)
        {
            return null;
        }

        for (int i = 0; i < actors.Count; i++)
        {
            TotemActorModel candidate = actors[i];
            if (candidate != null
                && candidate != participant
                && candidate.TeamId == participant.TeamId
                && candidate.IsAlive
                && candidate.Lifecycle == TotemParticipantLifecycle.Active)
            {
                return candidate;
            }
        }

        return null;
    }

    public void ResetMatchState()
    {
        states.Clear();
        commandSequence = 0;
    }

    private void TryConsumeHumanReviveInput()
    {
        if (extractionService?.ShouldReserveLocalInteraction() == true)
        {
            return;
        }

        TotemActorModel player = actorService?.Player;
        TotemInputSnapshot input = inputService?.Current ?? TotemInputSnapshot.Empty;
        if (player == null
            || player.ControllerKind != TotemParticipantControllerKind.Human
            || !input.interactPressed
            || IsReviving(player)
            || !TryGetNearestDownedTeammate(player, out TotemActorModel target, out float distance)
            || distance > ReviveInteractRadius)
        {
            return;
        }

        TryIssueBeginReviveCommand(
            player,
            target,
            TotemGameplayCommandSource.HumanInput,
            out _);
    }

    private TotemReviveContinuationStatus ResolveReviveContinuationStatus(
        TotemFirstPlayableParticipantLifeState state,
        TotemActorModel target)
    {
        TotemActorModel reviver = FindParticipant(state.ReviverParticipantId.Value);
        if (reviver == null
            || !reviver.IsAlive
            || reviver.Lifecycle == TotemParticipantLifecycle.Downed
            || reviver.Lifecycle == TotemParticipantLifecycle.Eliminated
            || reviver.Lifecycle == TotemParticipantLifecycle.Disconnected)
        {
            return TotemReviveContinuationStatus.ReviverDowned;
        }

        if (statusService != null && !statusService.CanAct(reviver))
        {
            return TotemReviveContinuationStatus.ReviverControlled;
        }

        if (readinessService != null && !readinessService.CanAct(reviver))
        {
            return TotemReviveContinuationStatus.ReviverDowned;
        }

        if (target == null || !IsWithinReviveRange(reviver, target))
        {
            return TotemReviveContinuationStatus.OutOfRange;
        }

        if (reviver.ControllerKind == TotemParticipantControllerKind.Human
            && (inputService == null || !inputService.Current.interactHeld))
        {
            return TotemReviveContinuationStatus.InteractionReleased;
        }

        return TotemReviveContinuationStatus.Valid;
    }

    private bool CanReviverAct(TotemActorModel reviver)
    {
        return reviver != null
            && reviver.IsAlive
            && (statusService == null || statusService.CanAct(reviver))
            && (readinessService == null || readinessService.CanAct(reviver));
    }

    public static bool IsWithinReviveRange(TotemActorModel reviver, TotemActorModel target)
    {
        return reviver != null
            && target != null
            && (target.Position - reviver.Position).sqrMagnitude
                <= ReviveInteractRadius * ReviveInteractRadius;
    }

    private void OnPhaseChanged(TotemMatchPhase previous, TotemMatchPhase current)
    {
        if (current == TotemMatchPhase.FrontEnd)
        {
            ResetMatchState();
            return;
        }

        if (!TotemMatchPhaseContract.IsBuild(current) || states.Count == 0)
        {
            return;
        }

        foreach (var pair in states)
        {
            TotemFirstPlayableParticipantLifeState state = pair.Value;
            if (!state.EliminateAtBuildBoundary(out TotemDownedStateContract transition))
            {
                continue;
            }

            ApplyTransitionToActor(FindParticipant(pair.Key), state, transition);
        }
    }

    private bool HasLivingTeammate(TotemActorModel participant)
    {
        if (participant == null || !participant.TeamId.IsValid)
        {
            return false;
        }

        IReadOnlyList<TotemActorModel> actors = actorService?.Actors;
        if (actors == null)
        {
            return false;
        }

        for (int i = 0; i < actors.Count; i++)
        {
            TotemActorModel candidate = actors[i];
            if (candidate != null
                && candidate != participant
                && candidate.TeamId == participant.TeamId
                && candidate.IsAlive
                && candidate.Lifecycle != TotemParticipantLifecycle.Downed
                && candidate.Lifecycle != TotemParticipantLifecycle.Eliminated
                && candidate.Lifecycle != TotemParticipantLifecycle.Disconnected)
            {
                return true;
            }
        }

        return false;
    }

    private TotemActorModel FindParticipant(int participantId)
    {
        IReadOnlyList<TotemActorModel> actors = actorService?.Actors;
        if (actors == null)
        {
            return null;
        }

        for (int i = 0; i < actors.Count; i++)
        {
            if (actors[i]?.ParticipantId == participantId)
            {
                return actors[i];
            }
        }

        return null;
    }

    private void ApplyTransitionToActor(
        TotemActorModel participant,
        TotemFirstPlayableParticipantLifeState state,
        TotemDownedStateContract transition)
    {
        if (participant != null)
        {
            switch (transition.Current)
            {
                case TotemFirstPlayableLifeState.Downed:
                    participant.SetHealthForLifecycle(state.DownedHealth);
                    participant.SetLifecycle(TotemParticipantLifecycle.Downed, transition.Reason.ToString());
                    break;
                case TotemFirstPlayableLifeState.Alive:
                    participant.SetHealthForLifecycle(state.DownedHealth);
                    participant.SetLifecycle(TotemParticipantLifecycle.Active, transition.Reason.ToString());
                    break;
                case TotemFirstPlayableLifeState.Eliminated:
                    participant.SetHealthForLifecycle(0f);
                    participant.SetLifecycle(TotemParticipantLifecycle.Eliminated, transition.Reason.ToString());
                    actorService?.NotifyParticipantEliminated(participant, transition.Reason.ToString());
                    EliminateOrphanedDownedTeammates(participant, transition.Instigator);
                    break;
            }
        }

        LifeStateChanged?.Invoke(participant, transition);
    }

    private void EliminateOrphanedDownedTeammates(
        TotemActorModel eliminatedParticipant,
        TotemCombatantReference instigator)
    {
        if (eliminatedParticipant == null || !eliminatedParticipant.TeamId.IsValid)
        {
            return;
        }

        IReadOnlyList<TotemActorModel> actors = actorService?.Actors;
        if (actors == null)
        {
            return;
        }

        for (int i = 0; i < actors.Count; i++)
        {
            TotemActorModel teammate = actors[i];
            if (teammate == null
                || teammate == eliminatedParticipant
                || teammate.TeamId != eliminatedParticipant.TeamId
                || !states.TryGetValue(teammate.ParticipantId, out TotemFirstPlayableParticipantLifeState teammateState)
                || !teammateState.IsDowned
                || HasLivingTeammate(teammate))
            {
                continue;
            }

            TotemCombatantReference resolvedInstigator = instigator.IsValid
                ? instigator
                : teammateState.LastEffectiveDamageSourceCombatant;
            if (teammateState.EliminateForTeamWipe(resolvedInstigator, out TotemDownedStateContract transition))
            {
                ApplyTransitionToActor(teammate, teammateState, transition);
            }
        }
    }

}
