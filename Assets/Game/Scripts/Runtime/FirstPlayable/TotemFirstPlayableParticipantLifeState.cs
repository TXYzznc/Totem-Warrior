using UnityEngine;

public enum TotemReviveContinuationStatus : byte
{
    Valid = 0,
    OutOfRange = 1,
    ReviverControlled = 2,
    ReviverDowned = 3,
    InteractionReleased = 4,
}

public static class TotemReviveCommandCodec
{
    public static bool TryDecodeTarget(
        in TotemGameplayCommand command,
        TotemGameplayCommandType expectedType,
        out TotemParticipantId targetParticipantId)
    {
        targetParticipantId = new TotemParticipantId(command.IntValue);
        return command.IsValid
            && command.Type == expectedType
            && targetParticipantId.IsValid
            && targetParticipantId != command.ParticipantId;
    }
}

/// <summary>
/// Pure first-playable downed/revive/elimination rules for one participant.
/// Scene distance, input and control checks are supplied by the runtime layer;
/// this type owns only deterministic state and timers.
/// </summary>
public sealed class TotemFirstPlayableParticipantLifeState
{
    private const float TimerCompletionEpsilon = 0.0001f;

    private readonly float maxHealth;
    private TotemFirstPlayableLifeState lifeState;
    private float downedHealth;
    private float bleedoutRemaining;
    private float reviveProgress;
    private float protectionRemaining;
    private TotemParticipantId reviverParticipantId;
    private TotemCombatantReference lastEffectiveDamageSource;

    public TotemFirstPlayableParticipantLifeState(float participantMaxHealth)
    {
        maxHealth = Mathf.Max(1f, participantMaxHealth);
        lifeState = TotemFirstPlayableLifeState.Alive;
    }

    public float MaxHealth => maxHealth;
    public TotemFirstPlayableLifeState LifeState => lifeState;
    public float DownedHealth => downedHealth;
    public float BleedoutRemaining => bleedoutRemaining;
    public float ReviveProgress => reviveProgress;
    public float ProtectionRemaining => protectionRemaining;
    public TotemParticipantId ReviverParticipantId => reviverParticipantId;
    public TotemCombatantReference LastEffectiveDamageSourceCombatant => lastEffectiveDamageSource;
    public TotemParticipantId LastEffectiveDamageSource => lastEffectiveDamageSource.ParticipantId;
    public bool IsDowned => lifeState == TotemFirstPlayableLifeState.Downed;
    public bool IsEliminated => lifeState == TotemFirstPlayableLifeState.Eliminated;
    public bool IsProtected => lifeState == TotemFirstPlayableLifeState.Alive && protectionRemaining > 0f;
    public bool CanAttack => lifeState == TotemFirstPlayableLifeState.Alive;
    public bool CanBuild => lifeState == TotemFirstPlayableLifeState.Alive;
    public float MoveSpeedMultiplier => IsDowned ? TotemDownedStateContract.MoveSpeedRatio : lifeState == TotemFirstPlayableLifeState.Alive ? 1f : 0f;

    public bool TryEnterDowned(
        bool hasLivingTeammate,
        TotemParticipantId instigatorParticipantId,
        out TotemDownedStateContract transition)
    {
        return TryEnterDowned(
            hasLivingTeammate,
            TotemCombatantReference.FromParticipant(instigatorParticipantId),
            out transition);
    }

    public bool TryEnterDowned(
        bool hasLivingTeammate,
        TotemCombatantReference instigator,
        out TotemDownedStateContract transition)
    {
        if (lifeState != TotemFirstPlayableLifeState.Alive || !hasLivingTeammate)
        {
            transition = default;
            return false;
        }

        TotemFirstPlayableLifeState previous = lifeState;
        lifeState = TotemFirstPlayableLifeState.Downed;
        downedHealth = maxHealth * TotemDownedStateContract.DownedHealthRatio;
        bleedoutRemaining = TotemDownedStateContract.BleedoutSeconds;
        reviveProgress = 0f;
        protectionRemaining = 0f;
        reviverParticipantId = default;
        if (instigator.IsValid)
        {
            lastEffectiveDamageSource = instigator;
        }

        transition = new TotemDownedStateContract(
            previous,
            lifeState,
            TotemDownedTransitionReason.LethalDamage,
            instigator);
        return true;
    }

    public bool ApplyDownedDamage(
        float amount,
        TotemParticipantId instigatorParticipantId,
        out float appliedDamage,
        out TotemDownedStateContract transition)
    {
        return ApplyDownedDamage(
            amount,
            TotemCombatantReference.FromParticipant(instigatorParticipantId),
            out appliedDamage,
            out transition);
    }

    public bool ApplyDownedDamage(
        float amount,
        TotemCombatantReference instigator,
        out float appliedDamage,
        out TotemDownedStateContract transition)
    {
        if (!IsDowned || amount <= 0f)
        {
            appliedDamage = 0f;
            transition = default;
            return false;
        }

        float before = downedHealth;
        downedHealth = Mathf.Max(0f, downedHealth - amount);
        appliedDamage = before - downedHealth;
        if (appliedDamage > 0f && instigator.IsValid)
        {
            lastEffectiveDamageSource = instigator;
        }

        if (downedHealth > 0f)
        {
            transition = default;
            return true;
        }

        Eliminate(TotemDownedTransitionReason.Executed, lastEffectiveDamageSource, out transition);
        return true;
    }

    public bool TryBeginRevive(
        TotemParticipantId reviverId,
        out TotemDownedStateContract transition)
    {
        if (!IsDowned || !reviverId.IsValid)
        {
            transition = default;
            return false;
        }

        if (reviverParticipantId != reviverId)
        {
            reviveProgress = 0f;
        }

        reviverParticipantId = reviverId;
        transition = new TotemDownedStateContract(
            lifeState,
            lifeState,
            TotemDownedTransitionReason.ReviveStarted,
            reviverId);
        return true;
    }

    public bool ContinueRevive(
        float deltaTime,
        TotemReviveContinuationStatus status,
        out TotemDownedStateContract transition)
    {
        if (!IsDowned || !reviverParticipantId.IsValid)
        {
            transition = default;
            return false;
        }

        if (status != TotemReviveContinuationStatus.Valid)
        {
            TotemParticipantId reviver = reviverParticipantId;
            TotemDownedTransitionReason reason = ResolveCancellationReason(status);
            reviveProgress = 0f;
            reviverParticipantId = default;
            transition = new TotemDownedStateContract(lifeState, lifeState, reason, reviver);
            return false;
        }

        if (deltaTime <= 0f)
        {
            transition = default;
            return true;
        }

        reviveProgress = Mathf.Min(TotemDownedStateContract.ReviveSeconds, reviveProgress + deltaTime);
        if (TotemDownedStateContract.ReviveSeconds - reviveProgress > TimerCompletionEpsilon)
        {
            transition = default;
            return true;
        }

        TotemParticipantId completedBy = reviverParticipantId;
        TotemFirstPlayableLifeState previous = lifeState;
        lifeState = TotemFirstPlayableLifeState.Alive;
        downedHealth = maxHealth * TotemDownedStateContract.RevivedHealthRatio;
        bleedoutRemaining = 0f;
        reviveProgress = 0f;
        protectionRemaining = TotemDownedStateContract.ReviveProtectionSeconds;
        reviverParticipantId = default;
        transition = new TotemDownedStateContract(
            previous,
            lifeState,
            TotemDownedTransitionReason.ReviveCompleted,
            completedBy);
        return true;
    }

    public bool CancelRevive(
        TotemParticipantId reviverId,
        TotemDownedTransitionReason reason,
        out TotemDownedStateContract transition)
    {
        if (!IsDowned
            || !reviverId.IsValid
            || reviverParticipantId != reviverId
            || !IsReviveCancellation(reason))
        {
            transition = default;
            return false;
        }

        reviveProgress = 0f;
        reviverParticipantId = default;
        transition = new TotemDownedStateContract(lifeState, lifeState, reason, reviverId);
        return true;
    }

    public bool Advance(
        float deltaTime,
        bool gameplaySuspended,
        out TotemDownedStateContract transition)
    {
        if (gameplaySuspended || deltaTime <= 0f)
        {
            transition = default;
            return false;
        }

        if (lifeState == TotemFirstPlayableLifeState.Alive)
        {
            protectionRemaining = Mathf.Max(0f, protectionRemaining - deltaTime);
            transition = default;
            return false;
        }

        if (!IsDowned)
        {
            transition = default;
            return false;
        }

        bleedoutRemaining = Mathf.Max(0f, bleedoutRemaining - deltaTime);
        if (bleedoutRemaining > TimerCompletionEpsilon)
        {
            transition = default;
            return false;
        }

        Eliminate(TotemDownedTransitionReason.BledOut, lastEffectiveDamageSource, out transition);
        return true;
    }

    public bool EliminateAtBuildBoundary(out TotemDownedStateContract transition)
    {
        if (!IsDowned)
        {
            transition = default;
            return false;
        }

        Eliminate(TotemDownedTransitionReason.BuildBoundary, lastEffectiveDamageSource, out transition);
        return true;
    }

    public bool EliminateWithoutLivingTeammate(
        TotemParticipantId instigatorParticipantId,
        out TotemDownedStateContract transition)
    {
        return EliminateWithoutLivingTeammate(
            TotemCombatantReference.FromParticipant(instigatorParticipantId),
            out transition);
    }

    public bool EliminateWithoutLivingTeammate(
        TotemCombatantReference instigator,
        out TotemDownedStateContract transition)
    {
        if (lifeState != TotemFirstPlayableLifeState.Alive)
        {
            transition = default;
            return false;
        }

        Eliminate(TotemDownedTransitionReason.TeamEliminated, instigator, out transition);
        return true;
    }

    public bool EliminateForTeamWipe(
        TotemCombatantReference instigator,
        out TotemDownedStateContract transition)
    {
        if (lifeState != TotemFirstPlayableLifeState.Alive && !IsDowned)
        {
            transition = default;
            return false;
        }

        Eliminate(TotemDownedTransitionReason.TeamEliminated, instigator, out transition);
        return true;
    }

    public TotemSpectatorState ResolveSpectatorState(bool teammateCountsAsAlive)
    {
        if (!IsEliminated)
        {
            return TotemSpectatorState.None;
        }

        return teammateCountsAsAlive
            ? TotemSpectatorState.SpectatingTeammate
            : TotemSpectatorState.WaitingForResult;
    }

    private void Eliminate(
        TotemDownedTransitionReason reason,
        TotemCombatantReference instigator,
        out TotemDownedStateContract transition)
    {
        TotemFirstPlayableLifeState previous = lifeState;
        lifeState = TotemFirstPlayableLifeState.Eliminated;
        downedHealth = 0f;
        bleedoutRemaining = 0f;
        reviveProgress = 0f;
        protectionRemaining = 0f;
        reviverParticipantId = default;
        transition = new TotemDownedStateContract(previous, lifeState, reason, instigator);
    }

    private static TotemDownedTransitionReason ResolveCancellationReason(TotemReviveContinuationStatus status)
    {
        switch (status)
        {
            case TotemReviveContinuationStatus.OutOfRange:
                return TotemDownedTransitionReason.ReviveCancelledOutOfRange;
            case TotemReviveContinuationStatus.ReviverControlled:
                return TotemDownedTransitionReason.ReviveCancelledControlled;
            case TotemReviveContinuationStatus.ReviverDowned:
                return TotemDownedTransitionReason.ReviveCancelledReviverDowned;
            default:
                return TotemDownedTransitionReason.ReviveCancelledInteraction;
        }
    }

    private static bool IsReviveCancellation(TotemDownedTransitionReason reason)
    {
        return reason == TotemDownedTransitionReason.ReviveCancelledOutOfRange
            || reason == TotemDownedTransitionReason.ReviveCancelledControlled
            || reason == TotemDownedTransitionReason.ReviveCancelledReviverDowned
            || reason == TotemDownedTransitionReason.ReviveCancelledInteraction;
    }
}
