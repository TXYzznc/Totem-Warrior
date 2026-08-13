using System;
using UnityEngine;

public enum TotemCombatantDomain
{
    Participant = 0,
}

public enum TotemParticipantControllerKind
{
    Human = 0,
    SmartBot = 1,
    LightBot = 2,
}

public enum TotemParticipantLifecycle
{
    Reserved = 0,
    Loading = 1,
    Protected = 2,
    Active = 3,
    Eliminated = 4,
    Disconnected = 5,
    Downed = 6,
}

public enum TotemCombatRelationshipReason
{
    Unknown = 0,
    AllowedParticipantToParticipant = 3,
    AllowedWorldToParticipant = 4,
    BlockedNullTarget = 100,
    BlockedSelf = 101,
    BlockedSourceDead = 102,
    BlockedTargetDead = 103,
    BlockedSourceLoading = 104,
    BlockedSourceProtected = 105,
    BlockedSourceInactive = 106,
    BlockedTargetLoading = 107,
    BlockedTargetProtected = 108,
    BlockedTargetInactive = 109,
    BlockedParticipantCombatGracePeriod = 112,
    BlockedParticipantFriendlyFire = 113,
    BlockedGameplaySuspended = 114,
}

public readonly struct TotemCombatRelationshipContext
{
    public readonly float WorldTime;
    public readonly bool GameplaySuspended;

    public TotemCombatRelationshipContext(
        float worldTime,
        bool gameplaySuspended = false)
    {
        WorldTime = Mathf.Max(0f, worldTime);
        GameplaySuspended = gameplaySuspended;
    }
}

public readonly struct TotemCombatRelationshipDecision
{
    public readonly bool Allowed;
    public readonly TotemCombatRelationshipReason Reason;

    public TotemCombatRelationshipDecision(bool allowed, TotemCombatRelationshipReason reason)
    {
        Allowed = allowed;
        Reason = reason;
    }
}

public abstract class TotemCombatantModel
{
    protected TotemCombatantModel(
        int combatantId,
        string name,
        TotemCombatantDomain domain,
        float maxHealth,
        Vector3 position)
    {
        CombatantId = combatantId;
        Name = string.IsNullOrWhiteSpace(name) ? $"Combatant{combatantId}" : name;
        Domain = domain;
        MaxHealth = Mathf.Max(1f, maxHealth);
        Health = MaxHealth;
        Position = position;
    }

    public int CombatantId { get; }

    public string Name { get; }

    public TotemCombatantDomain Domain { get; }

    public float MaxHealth { get; private set; }

    public float Health { get; private set; }

    public Vector3 Position { get; set; }

    public GameObject GameObject { get; set; }

    public string VisualAssetKey { get; set; } = string.Empty;

    public bool IsAlive => Health > 0f;

    public float ApplyDamage(float amount)
    {
        if (amount <= 0f || !IsAlive)
        {
            return 0f;
        }

        float before = Health;
        Health = Mathf.Max(0f, Health - amount);
        return before - Health;
    }

    public float Heal(float amount)
    {
        if (amount <= 0f || !IsAlive)
        {
            return 0f;
        }

        float before = Health;
        Health = Mathf.Min(MaxHealth, Health + amount);
        return Health - before;
    }

    public void ResetHealth(float maxHealth)
    {
        MaxHealth = Mathf.Max(1f, maxHealth);
        Health = MaxHealth;
    }

    internal void SetHealthForLifecycle(float health)
    {
        Health = Mathf.Clamp(health, 0f, MaxHealth);
    }
}

public class TotemParticipantModel : TotemCombatantModel
{
    public TotemParticipantModel(
        int participantId,
        string name,
        TotemParticipantControllerKind controllerKind,
        float maxHealth,
        Vector3 position,
        TotemParticipantLifecycle lifecycle = TotemParticipantLifecycle.Reserved,
        int teamId = -1)
        : base(participantId, name, TotemCombatantDomain.Participant, maxHealth, position)
    {
        ControllerKind = controllerKind;
        Lifecycle = lifecycle;
        TeamId = new TotemTeamId(teamId);
    }

    public int ParticipantId => CombatantId;

    public TotemParticipantControllerKind ControllerKind { get; }

    public TotemTeamId TeamId { get; }

    public TotemParticipantLifecycle Lifecycle { get; private set; }

    public float LifecycleElapsed { get; private set; }

    public string LifecycleReason { get; private set; } = string.Empty;

    public bool CountsAsAlive => IsAlive && Lifecycle != TotemParticipantLifecycle.Eliminated && Lifecycle != TotemParticipantLifecycle.Disconnected;

    public bool IsTargetable => IsAlive && Lifecycle == TotemParticipantLifecycle.Active;

    public bool CanAct => IsAlive && Lifecycle == TotemParticipantLifecycle.Active;

    public bool SetLifecycle(TotemParticipantLifecycle lifecycle, string reason)
    {
        if (Lifecycle == lifecycle)
        {
            return false;
        }

        Lifecycle = lifecycle;
        LifecycleElapsed = 0f;
        LifecycleReason = reason ?? string.Empty;
        return true;
    }

    public void TickLifecycle(float deltaTime)
    {
        LifecycleElapsed += Mathf.Max(0f, deltaTime);
    }
}

public readonly struct TotemParticipantLifecycleChangedEvent
{
    public readonly TotemParticipantModel Participant;
    public readonly TotemParticipantLifecycle Previous;
    public readonly TotemParticipantLifecycle Current;
    public readonly string Reason;
    public readonly float WorldTime;

    public TotemParticipantLifecycleChangedEvent(
        TotemParticipantModel participant,
        TotemParticipantLifecycle previous,
        TotemParticipantLifecycle current,
        string reason,
        float worldTime)
    {
        Participant = participant;
        Previous = previous;
        Current = current;
        Reason = reason ?? string.Empty;
        WorldTime = Mathf.Max(0f, worldTime);
    }
}

[Serializable]
public sealed class TotemParticipantDomainSnapshot
{
    public int participantCount;
    public int aliveParticipantCount;
    public int humanCount;
    public int smartBotCount;
    public int lightBotCount;
    public int loadingCount;
    public int protectedCount;
    public int activeCount;
    public int disconnectedCount;
    public int winnerParticipantId;
}

[Serializable]
public sealed class TotemCombatRelationshipSnapshot
{
    public int evaluationCount;
    public int allowedCount;
    public int blockedCount;
    public int lastSourceId;
    public int lastTargetId;
    public string lastReason;
    public float lastWorldTime;
}
