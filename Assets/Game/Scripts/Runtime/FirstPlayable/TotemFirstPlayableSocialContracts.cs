using System;

public enum TotemDownedTransitionReason : byte
{
    None = 0,
    LethalDamage = 1,
    ReviveStarted = 2,
    ReviveCancelledOutOfRange = 3,
    ReviveCancelledControlled = 4,
    ReviveCancelledReviverDowned = 5,
    ReviveCancelledInteraction = 6,
    ReviveCompleted = 7,
    Executed = 8,
    BledOut = 9,
    BuildBoundary = 10,
    TeamEliminated = 11,
}

public enum TotemSpectatorState : byte
{
    None = 0,
    SpectatingTeammate = 1,
    WaitingForResult = 2,
}

public readonly struct TotemCombatantReference : IEquatable<TotemCombatantReference>
{
    public TotemCombatantReference(TotemCombatantDomain domain, int combatantId)
    {
        Domain = domain;
        CombatantId = combatantId;
    }

    public TotemCombatantDomain Domain { get; }
    public int CombatantId { get; }
    public bool IsValid => CombatantId > 0;
    public TotemParticipantId ParticipantId => Domain == TotemCombatantDomain.Participant
        ? new TotemParticipantId(CombatantId)
        : default;

    public bool Equals(TotemCombatantReference other) =>
        Domain == other.Domain && CombatantId == other.CombatantId;
    public override bool Equals(object obj) => obj is TotemCombatantReference other && Equals(other);
    public override int GetHashCode() => ((int)Domain * 397) ^ CombatantId;
    public override string ToString() => IsValid ? $"{Domain}:{CombatantId}" : "Combatant.Invalid";
    public static bool operator ==(TotemCombatantReference left, TotemCombatantReference right) => left.Equals(right);
    public static bool operator !=(TotemCombatantReference left, TotemCombatantReference right) => !left.Equals(right);

    public static TotemCombatantReference FromParticipant(TotemParticipantId participantId) =>
        participantId.IsValid
            ? new TotemCombatantReference(TotemCombatantDomain.Participant, participantId.Value)
            : default;

    public static TotemCombatantReference FromCombatant(TotemCombatantModel combatant) =>
        combatant == null
            ? default
            : new TotemCombatantReference(combatant.Domain, combatant.CombatantId);
}

public readonly struct TotemDownedStateContract
{
    public const float DownedHealthRatio = 0.4f;
    public const float BleedoutSeconds = 20f;
    public const float MoveSpeedRatio = 0.35f;
    public const float ReviveSeconds = 3f;
    public const float RevivedHealthRatio = 0.3f;
    public const float ReviveProtectionSeconds = 1f;

    public TotemDownedStateContract(
        TotemFirstPlayableLifeState previous,
        TotemFirstPlayableLifeState current,
        TotemDownedTransitionReason reason,
        TotemParticipantId instigatorParticipantId)
        : this(previous, current, reason, TotemCombatantReference.FromParticipant(instigatorParticipantId))
    {
    }

    public TotemDownedStateContract(
        TotemFirstPlayableLifeState previous,
        TotemFirstPlayableLifeState current,
        TotemDownedTransitionReason reason,
        TotemCombatantReference instigator)
    {
        Previous = previous;
        Current = current;
        Reason = reason;
        Instigator = instigator;
    }

    public TotemFirstPlayableLifeState Previous { get; }
    public TotemFirstPlayableLifeState Current { get; }
    public TotemDownedTransitionReason Reason { get; }
    public TotemCombatantReference Instigator { get; }
    public TotemParticipantId InstigatorParticipantId => Instigator.ParticipantId;
}

public enum TotemTattooSlotId : byte
{
    Head = 0,
    Torso = 1,
    LeftArm = 2,
    RightArm = 3,
    LeftLeg = 4,
    RightLeg = 5,
}

public enum TotemFirstPlayablePatternId : byte
{
    None = 0,
    P01 = 1,
    P02 = 2,
}

public enum TotemPigmentKind : byte
{
    Fire = 1,
    Ice = 2,
    Lightning = 3,
}

[Serializable]
public sealed class TotemPublicTattooSnapshotEntry
{
    public TotemTattooSlotId slot;
    public TotemFirstPlayablePatternId pattern;
    public TotemFirstPlayableElement element;
    public string publicEffectText = string.Empty;
}

[Serializable]
public sealed class TotemAttributeSnapshotEntry
{
    public string attributeId = string.Empty;
    public float baseValue;
    public float inMatchBonus;
}

[Serializable]
public struct TotemMatchAchievementSnapshot
{
    public float playerDamage;
    public int playerDowns;
    public int playerEliminations;
    public float allyHealing;
    public float allyShieldOrMitigation;
    public int successfulRevives;
    public int cleansesOrControlRemovals;
    public float effectiveControlSeconds;
    public int effectiveControlCount;
    public float allyDamageGainCreated;
    public int resourcesAcquired;
    public int resourcesShared;
    public int selfDowns;
    public float indirectElementDamage;
}

[Serializable]
public sealed class TotemConstructionIntelligenceSnapshot
{
    public int participantId;
    public int teamId;
    public int capturedAtPhase;
    public TotemPublicTattooSnapshotEntry[] tattoos = Array.Empty<TotemPublicTattooSnapshotEntry>();
    public TotemAttributeSnapshotEntry[] attributes = Array.Empty<TotemAttributeSnapshotEntry>();
    public TotemMatchAchievementSnapshot achievements;
}

public enum TotemPigmentRequestState : byte
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Expired = 3,
    Invalidated = 4,
}

public readonly struct TotemPigmentRequest
{
    public TotemPigmentRequest(
        int requestId,
        TotemParticipantId requesterId,
        TotemParticipantId teammateId,
        TotemPigmentKind pigment,
        int amount,
        int createdSequence,
        TotemPigmentRequestState state = TotemPigmentRequestState.Pending)
    {
        RequestId = requestId;
        RequesterId = requesterId;
        TeammateId = teammateId;
        Pigment = pigment;
        Amount = amount;
        CreatedSequence = createdSequence;
        State = state;
    }

    public int RequestId { get; }
    public TotemParticipantId RequesterId { get; }
    public TotemParticipantId TeammateId { get; }
    public TotemPigmentKind Pigment { get; }
    public int Amount { get; }
    public int CreatedSequence { get; }
    public TotemPigmentRequestState State { get; }
    public bool IsValid => RequestId > 0 && RequesterId.IsValid && TeammateId.IsValid && RequesterId != TeammateId && Amount > 0;
}

public readonly struct TotemPigmentTransfer
{
    public TotemPigmentTransfer(
        int requestId,
        TotemParticipantId fromParticipantId,
        TotemParticipantId toParticipantId,
        TotemPigmentKind pigment,
        int amount,
        int inventoryVersion)
    {
        RequestId = requestId;
        FromParticipantId = fromParticipantId;
        ToParticipantId = toParticipantId;
        Pigment = pigment;
        Amount = amount;
        InventoryVersion = inventoryVersion;
    }

    public int RequestId { get; }
    public TotemParticipantId FromParticipantId { get; }
    public TotemParticipantId ToParticipantId { get; }
    public TotemPigmentKind Pigment { get; }
    public int Amount { get; }
    public int InventoryVersion { get; }
    public bool RequiresAtomicCommit => RequestId > 0 && Amount > 0 && FromParticipantId.IsValid && ToParticipantId.IsValid;
}
