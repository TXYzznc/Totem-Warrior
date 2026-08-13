using System;
using UnityEngine;

public enum TotemHitRegion : byte
{
    Body = 0,
    Weakpoint = 1,
}

public readonly struct TotemGunHitContext
{
    public TotemGunHitContext(
        TotemParticipantId sourceParticipantId,
        TotemTeamId sourceTeamId,
        int targetCombatantId,
        TotemTeamId targetTeamId,
        TotemHitRegion hitRegion,
        Vector3 hitPoint,
        Vector3 hitNormal,
        float requestedDamage)
    {
        SourceParticipantId = sourceParticipantId;
        SourceTeamId = sourceTeamId;
        TargetCombatantId = targetCombatantId;
        TargetTeamId = targetTeamId;
        HitRegion = hitRegion;
        HitPoint = hitPoint;
        HitNormal = hitNormal;
        RequestedDamage = Mathf.Max(0f, requestedDamage);
    }

    public TotemParticipantId SourceParticipantId { get; }
    public TotemTeamId SourceTeamId { get; }
    public int TargetCombatantId { get; }
    public TotemTeamId TargetTeamId { get; }
    public TotemHitRegion HitRegion { get; }
    public Vector3 HitPoint { get; }
    public Vector3 HitNormal { get; }
    public float RequestedDamage { get; }
    public bool IsWeakpoint => HitRegion == TotemHitRegion.Weakpoint;
}

public readonly struct TotemDirectDamageResult
{
    public TotemDirectDamageResult(TotemGunHitContext hit, float shieldDamage, float healthDamage, bool relationshipAllowed)
    {
        Hit = hit;
        ShieldDamage = Mathf.Max(0f, shieldDamage);
        HealthDamage = Mathf.Max(0f, healthDamage);
        RelationshipAllowed = relationshipAllowed;
    }

    public TotemGunHitContext Hit { get; }
    public float ShieldDamage { get; }
    public float HealthDamage { get; }
    public bool RelationshipAllowed { get; }
    public float EffectiveDamage => RelationshipAllowed ? ShieldDamage + HealthDamage : 0f;
    public bool IsEffectiveDirectDamage => EffectiveDamage > 0f;
    public bool CanSubmitRifleArmEvent => IsEffectiveDirectDamage;
}

public readonly struct TotemGunAttackResult
{
    public TotemGunAttackResult(
        bool fired,
        string reason,
        TotemWeaponDefinition weapon,
        TotemHitRegion hitRegion,
        TotemDirectDamageResult directDamage,
        bool killed,
        uint fireSequence)
    {
        Fired = fired;
        Reason = reason ?? string.Empty;
        Weapon = weapon;
        HitRegion = hitRegion;
        DirectDamage = directDamage;
        Killed = killed;
        FireSequence = fireSequence;
    }

    public bool Fired { get; }
    public string Reason { get; }
    public TotemWeaponDefinition Weapon { get; }
    public TotemHitRegion HitRegion { get; }
    public TotemDirectDamageResult DirectDamage { get; }
    public bool Killed { get; }
    public uint FireSequence { get; }
}

public enum TotemFirstPlayableElement : byte
{
    None = 0,
    Fire = 1,
    Ice = 2,
    Lightning = 3,
}

public enum TotemElementTier : byte
{
    None = 0,
    Weak = 1,
    Standard = 2,
    Strong = 3,
}

public readonly struct TotemElementLayerSource
{
    public TotemElementLayerSource(
        TotemFirstPlayableElement element,
        TotemParticipantId sourceParticipantId,
        int applicationSequence,
        float remainingSeconds)
    {
        Element = element;
        SourceParticipantId = sourceParticipantId;
        ApplicationSequence = applicationSequence;
        RemainingSeconds = Mathf.Max(0f, remainingSeconds);
    }

    public TotemFirstPlayableElement Element { get; }
    public TotemParticipantId SourceParticipantId { get; }
    public int ApplicationSequence { get; }
    public float RemainingSeconds { get; }
    public bool IsValid => Element != TotemFirstPlayableElement.None && SourceParticipantId.IsValid && ApplicationSequence >= 0;
}

public enum TotemReactionKind : byte
{
    None = 0,
    HeatShock = 1,
    Overload = 2,
    Stasis = 3,
}

public readonly struct TotemReactionAttribution
{
    public TotemReactionAttribution(
        TotemReactionKind reaction,
        TotemParticipantId triggerParticipantId,
        TotemParticipantId assistingParticipantId,
        float indirectElementDamage)
    {
        Reaction = reaction;
        TriggerParticipantId = triggerParticipantId;
        AssistingParticipantId = assistingParticipantId;
        IndirectElementDamage = Mathf.Max(0f, indirectElementDamage);
    }

    public TotemReactionKind Reaction { get; }
    public TotemParticipantId TriggerParticipantId { get; }
    public TotemParticipantId AssistingParticipantId { get; }
    public float IndirectElementDamage { get; }
    public TotemParticipantId KillOwner => TriggerParticipantId;
}

public enum TotemEffectEventKind : byte
{
    ReservedActiveSkillArm = 0,
    Dodge = 1,
    Move = 2,
    Weakpoint = 3,
    RifleArm = 4,
    Torso = 5,
    ElementApply = 6,
    Reaction = 7,
}

public static class TotemEffectPriority
{
    public const int ActiveSkillArm = 100;
    public const int Dodge = 90;
    public const int Move = 80;
    public const int Weakpoint = 70;
    public const int RifleArm = 60;
    public const int Torso = 50;

    public static int Resolve(TotemEffectEventKind kind)
    {
        switch (kind)
        {
            case TotemEffectEventKind.ReservedActiveSkillArm: return ActiveSkillArm;
            case TotemEffectEventKind.Dodge: return Dodge;
            case TotemEffectEventKind.Move: return Move;
            case TotemEffectEventKind.Weakpoint: return Weakpoint;
            case TotemEffectEventKind.RifleArm: return RifleArm;
            case TotemEffectEventKind.Torso: return Torso;
            default: return 0;
        }
    }
}

public readonly struct TotemEffectEvent
{
    public TotemEffectEvent(
        TotemEffectEventKind kind,
        TotemParticipantId sourceParticipantId,
        int targetCombatantId,
        int submissionSequence,
        float scalar = 0f)
    {
        Kind = kind;
        SourceParticipantId = sourceParticipantId;
        TargetCombatantId = targetCombatantId;
        SubmissionSequence = submissionSequence;
        Scalar = scalar;
    }

    public TotemEffectEventKind Kind { get; }
    public TotemParticipantId SourceParticipantId { get; }
    public int TargetCombatantId { get; }
    public int SubmissionSequence { get; }
    public float Scalar { get; }
    public int Priority => TotemEffectPriority.Resolve(Kind);
}

public readonly struct TotemResolutionIdentity
{
    public TotemResolutionIdentity(int matchSeed, int resolutionSequence)
    {
        MatchSeed = matchSeed;
        ResolutionSequence = resolutionSequence;
    }

    public int MatchSeed { get; }
    public int ResolutionSequence { get; }

    public uint DeriveStableOrder(int submissionSequence)
    {
        unchecked
        {
            uint value = (uint)MatchSeed;
            value ^= (uint)ResolutionSequence * 0x9E3779B9u;
            value ^= (uint)submissionSequence * 0x85EBCA6Bu;
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            return value;
        }
    }
}

public readonly struct TotemEffectPresentationInstruction
{
    public TotemEffectPresentationInstruction(string assetKey, int resolvedOrder, float delaySeconds)
    {
        AssetKey = assetKey ?? string.Empty;
        ResolvedOrder = resolvedOrder;
        DelaySeconds = Mathf.Max(0f, delaySeconds);
    }

    public string AssetKey { get; }
    public int ResolvedOrder { get; }
    public float DelaySeconds { get; }
}
