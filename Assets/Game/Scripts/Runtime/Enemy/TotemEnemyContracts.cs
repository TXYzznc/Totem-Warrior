using System;
using UnityEngine;

public enum TotemEnemyState
{
    Dormant = 0,
    Spawn = 1,
    Patrol = 2,
    Alert = 3,
    Chase = 4,
    AttackWindup = 5,
    Cast = 6,
    AttackActive = 7,
    Recover = 8,
    Return = 9,
    Stagger = 10,
    Dead = 11,
}

public enum TotemEnemyLod
{
    Hot = 0,
    Warm = 1,
    Cold = 2,
}

public enum TotemEnemyAbilityPhase
{
    Inactive = 0,
    Windup = 1,
    Active = 2,
    Recovery = 3,
    Complete = 4,
    Cancelled = 5,
}

public enum TotemEnemySpawnBlockReason
{
    None = 0,
    ServiceCapacity = 1,
    EncounterActiveCap = 2,
    DefinitionMissing = 3,
    DuplicateCombatantId = 4,
}

public enum TotemEnemyStatusKind
{
    Invalid = 0,
    DamageOverTime = 1,
    Slow = 2,
    Stun = 3,
}

public enum TotemEnemyStatusApplyResult
{
    Applied = 0,
    Refreshed = 1,
    InvalidTarget = 2,
    InvalidDefinition = 3,
    RelationshipBlocked = 4,
    CapacityReached = 5,
}

public readonly struct TotemEnemyStatusDefinition
{
    public readonly string StatusId;
    public readonly TotemEnemyStatusKind Kind;
    public readonly float Duration;
    public readonly float Power;
    public readonly float TickInterval;
    public readonly float MoveSpeedMultiplier;
    public readonly bool CanHitEnemies;
    public readonly bool WorldDamageAffectsEnemies;

    public TotemEnemyStatusDefinition(
        string statusId,
        TotemEnemyStatusKind kind,
        float duration,
        float power = 0f,
        float tickInterval = TotemStatusService.TickInterval,
        float moveSpeedMultiplier = 1f,
        bool canHitEnemies = false,
        bool worldDamageAffectsEnemies = false)
    {
        StatusId = statusId ?? string.Empty;
        Kind = kind;
        Duration = Mathf.Max(0f, duration);
        Power = Mathf.Max(0f, power);
        TickInterval = Mathf.Max(0.01f, tickInterval);
        MoveSpeedMultiplier = Mathf.Clamp01(moveSpeedMultiplier);
        CanHitEnemies = canHitEnemies;
        WorldDamageAffectsEnemies = worldDamageAffectsEnemies;
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(StatusId)
        && Kind != TotemEnemyStatusKind.Invalid
        && Duration > 0f;

    public static bool TryCreateBuiltIn(
        string statusId,
        float power,
        float duration,
        out TotemEnemyStatusDefinition definition)
    {
        definition = default;
        if (string.IsNullOrWhiteSpace(statusId) || duration <= 0f)
        {
            return false;
        }

        if (string.Equals(statusId, TotemStatusService.StunStatus, StringComparison.OrdinalIgnoreCase)
            || string.Equals(statusId, TotemStatusService.ShockStatus, StringComparison.OrdinalIgnoreCase))
        {
            definition = new TotemEnemyStatusDefinition(
                statusId,
                TotemEnemyStatusKind.Stun,
                duration,
                power,
                moveSpeedMultiplier: 0f);
            return true;
        }

        if (string.Equals(statusId, TotemStatusService.SlowStatus, StringComparison.OrdinalIgnoreCase)
            || string.Equals(statusId, "Frost", StringComparison.OrdinalIgnoreCase))
        {
            float slowAmount = power > 0f ? power : 0.3f;
            definition = new TotemEnemyStatusDefinition(
                statusId,
                TotemEnemyStatusKind.Slow,
                duration,
                slowAmount,
                moveSpeedMultiplier: Mathf.Clamp(1f - slowAmount, 0.15f, 1f));
            return true;
        }

        if (string.Equals(statusId, TotemStatusService.BurnStatus, StringComparison.OrdinalIgnoreCase)
            || string.Equals(statusId, TotemStatusService.PoisonStatus, StringComparison.OrdinalIgnoreCase)
            || string.Equals(statusId, "Bleed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(statusId, "Dot", StringComparison.OrdinalIgnoreCase)
            || string.Equals(statusId, "DamageOverTime", StringComparison.OrdinalIgnoreCase)
            || string.Equals(statusId, "Mutation", StringComparison.OrdinalIgnoreCase)
            || string.Equals(statusId, "HealMark", StringComparison.OrdinalIgnoreCase))
        {
            definition = new TotemEnemyStatusDefinition(
                statusId,
                TotemEnemyStatusKind.DamageOverTime,
                duration,
                power,
                TotemStatusService.TickInterval);
            return true;
        }

        return false;
    }
}

public readonly struct TotemEnemyStatusTick
{
    public readonly TotemCombatantModel Source;
    public readonly float Damage;
    public readonly string Reason;
    public readonly float WorldTime;
    public readonly bool CanHitEnemies;
    public readonly bool WorldDamageAffectsEnemies;

    public TotemEnemyStatusTick(
        TotemCombatantModel source,
        float damage,
        string reason,
        float worldTime,
        bool canHitEnemies,
        bool worldDamageAffectsEnemies)
    {
        Source = source;
        Damage = Mathf.Max(0f, damage);
        Reason = reason ?? string.Empty;
        WorldTime = Mathf.Max(0f, worldTime);
        CanHitEnemies = canHitEnemies;
        WorldDamageAffectsEnemies = worldDamageAffectsEnemies;
    }
}

internal interface ITotemEnemyStatusTickSink
{
    bool ApplyStatusTick(int enemyCombatantId, in TotemEnemyStatusTick tick);
}

[Serializable]
public sealed class TotemEnemyBehaviorDefinition
{
    public string behaviorProfileId = string.Empty;
    public float detectRange = 14f;
    public float attackRange = 2f;
    public float leashRange = 24f;
    public float moveSpeed = 3f;
    public float groupAlertRadius = 10f;
    public float hotRadius = 20f;
    public float warmRadius = 60f;
    public float lightHotHz = 5f;
    public float lightWarmHz = 2f;
    public float lightColdHz = 0.5f;
    public float eliteHotHz = 10f;
    public float eliteWarmHz = 4f;
    public float eliteColdHz = 1f;
    public float bossHotHz = 20f;
    public float bossWarmHz = 10f;
    public float bossColdHz = 5f;
    public float noProgressSeconds = 0.75f;
    public float pathCacheSeconds = 1f;
    public int pathNodeCapacity = 32;
    public int maxActiveAbilities = 4;
}

[Serializable]
public sealed class TotemEnemyAbilityRuntimeDefinition
{
    public string abilityId = string.Empty;
    public TotemEnemyAbilityType abilityType;
    public float range = 2f;
    public float radius = 2f;
    public float cooldown = 1f;
    public float windup = 0.2f;
    public float active = 0.1f;
    public float recovery = 0.3f;
    public float damageMultiplier = 1f;
    public float score = 1f;
    public float shieldAmount;
    public float healAmount;
    public float moveDistance;
    public float coneHalfAngle = 45f;
    public string statusId = string.Empty;
    public float statusChance;
    public string summonEnemyId = string.Empty;
    public int summonCount;
    public string vfxId = string.Empty;
    public string audioCueId = string.Empty;
    public bool canHitEnemies;
    public bool interruptible = true;
    public int minimumBossPhase = 1;
}

[Serializable]
public sealed class TotemBossPhaseDefinition
{
    public int phase = 1;
    public float enterHealthRatio = 1f;
    public float damageMultiplier = 1f;
    public float transitionSeconds = 0.5f;
    public string vfxId = string.Empty;
    public string audioCueId = string.Empty;
}

[Serializable]
public sealed class TotemEnemyRuntimeDefinition
{
    public string enemyId = string.Empty;
    public string displayName = string.Empty;
    public string themeId = string.Empty;
    public TotemEnemyTier tier = TotemEnemyTier.Light;
    public string runtimeAssetKey = string.Empty;
    public string lootTableId = string.Empty;
    public string guaranteedLootIds = string.Empty;
    public string abilityIds = string.Empty;
    public float maxHealth = 50f;
    public float baseDamage = 8f;
    public TotemEnemyBehaviorDefinition behavior = new TotemEnemyBehaviorDefinition();
    public TotemEnemyAbilityRuntimeDefinition[] abilities = Array.Empty<TotemEnemyAbilityRuntimeDefinition>();
    public TotemBossPhaseDefinition[] bossPhases = Array.Empty<TotemBossPhaseDefinition>();
}

public readonly struct TotemEnemySpawnRequest
{
    public readonly int CombatantId;
    public readonly string EnemyId;
    public readonly Vector3 Position;
    public readonly int EncounterInstanceId;
    public readonly string AnchorId;
    public readonly float WorldTime;

    public TotemEnemySpawnRequest(
        int combatantId,
        string enemyId,
        Vector3 position,
        int encounterInstanceId,
        string anchorId,
        float worldTime)
    {
        CombatantId = combatantId;
        EnemyId = enemyId ?? string.Empty;
        Position = position;
        EncounterInstanceId = encounterInstanceId;
        AnchorId = anchorId ?? string.Empty;
        WorldTime = Mathf.Max(0f, worldTime);
    }
}

public readonly struct TotemEnemyDamageCommand
{
    public readonly TotemEnemyModel Source;
    public readonly TotemActorModel Target;
    public readonly string AbilityId;
    public readonly float Amount;
    public readonly bool CanHitEnemies;
    public readonly float WorldTime;

    public TotemEnemyDamageCommand(
        TotemEnemyModel source,
        TotemActorModel target,
        string abilityId,
        float amount,
        bool canHitEnemies,
        float worldTime)
    {
        Source = source;
        Target = target;
        AbilityId = abilityId ?? string.Empty;
        Amount = Mathf.Max(0f, amount);
        CanHitEnemies = canHitEnemies;
        WorldTime = Mathf.Max(0f, worldTime);
    }
}

public readonly struct TotemEnemyProjectileCommand
{
    public readonly TotemEnemyModel Source;
    public readonly TotemActorModel Target;
    public readonly string AbilityId;
    public readonly Vector3 Origin;
    public readonly Vector3 Direction;
    public readonly float Damage;
    public readonly float WorldTime;

    public TotemEnemyProjectileCommand(
        TotemEnemyModel source,
        TotemActorModel target,
        string abilityId,
        Vector3 origin,
        Vector3 direction,
        float damage,
        float worldTime)
    {
        Source = source;
        Target = target;
        AbilityId = abilityId ?? string.Empty;
        Origin = origin;
        Direction = direction;
        Damage = Mathf.Max(0f, damage);
        WorldTime = Mathf.Max(0f, worldTime);
    }
}

public readonly struct TotemEnemyHazardCommand
{
    public readonly TotemEnemyModel Source;
    public readonly string AbilityId;
    public readonly Vector3 Position;
    public readonly float Radius;
    public readonly float Damage;
    public readonly float WorldTime;

    public TotemEnemyHazardCommand(
        TotemEnemyModel source,
        string abilityId,
        Vector3 position,
        float radius,
        float damage,
        float worldTime)
    {
        Source = source;
        AbilityId = abilityId ?? string.Empty;
        Position = position;
        Radius = Mathf.Max(0f, radius);
        Damage = Mathf.Max(0f, damage);
        WorldTime = Mathf.Max(0f, worldTime);
    }
}

public readonly struct TotemEnemyStateChangedEvent
{
    public readonly TotemEnemyModel Enemy;
    public readonly TotemEnemyState Previous;
    public readonly TotemEnemyState Current;
    public readonly string Reason;
    public readonly int TargetId;
    public readonly float WorldTime;

    public TotemEnemyStateChangedEvent(
        TotemEnemyModel enemy,
        TotemEnemyState previous,
        TotemEnemyState current,
        string reason,
        int targetId,
        float worldTime)
    {
        Enemy = enemy;
        Previous = previous;
        Current = current;
        Reason = reason ?? string.Empty;
        TargetId = targetId;
        WorldTime = Mathf.Max(0f, worldTime);
    }
}

public readonly struct TotemEnemyTargetChangedEvent
{
    public readonly TotemEnemyModel Enemy;
    public readonly int PreviousTargetId;
    public readonly int CurrentTargetId;
    public readonly string Reason;
    public readonly float WorldTime;

    public TotemEnemyTargetChangedEvent(
        TotemEnemyModel enemy,
        int previousTargetId,
        int currentTargetId,
        string reason,
        float worldTime)
    {
        Enemy = enemy;
        PreviousTargetId = previousTargetId;
        CurrentTargetId = currentTargetId;
        Reason = reason ?? string.Empty;
        WorldTime = Mathf.Max(0f, worldTime);
    }
}

public readonly struct TotemEnemyAbilityEvent
{
    public readonly TotemEnemyModel Enemy;
    public readonly string AbilityId;
    public readonly TotemEnemyAbilityType AbilityType;
    public readonly TotemEnemyAbilityPhase Phase;
    public readonly int TargetId;
    public readonly string Reason;
    public readonly float WorldTime;

    public TotemEnemyAbilityEvent(
        TotemEnemyModel enemy,
        string abilityId,
        TotemEnemyAbilityType abilityType,
        TotemEnemyAbilityPhase phase,
        int targetId,
        string reason,
        float worldTime)
    {
        Enemy = enemy;
        AbilityId = abilityId ?? string.Empty;
        AbilityType = abilityType;
        Phase = phase;
        TargetId = targetId;
        Reason = reason ?? string.Empty;
        WorldTime = Mathf.Max(0f, worldTime);
    }
}

public readonly struct TotemBossPhaseChangedEvent
{
    public readonly TotemEnemyModel Enemy;
    public readonly int PreviousPhase;
    public readonly int CurrentPhase;
    public readonly string VfxId;
    public readonly string AudioCueId;
    public readonly float DamageMultiplier;
    public readonly float WorldTime;

    public TotemBossPhaseChangedEvent(
        TotemEnemyModel enemy,
        int previousPhase,
        int currentPhase,
        string vfxId,
        string audioCueId,
        float damageMultiplier,
        float worldTime)
    {
        Enemy = enemy;
        PreviousPhase = previousPhase;
        CurrentPhase = currentPhase;
        VfxId = vfxId ?? string.Empty;
        AudioCueId = audioCueId ?? string.Empty;
        DamageMultiplier = Mathf.Max(0f, damageMultiplier);
        WorldTime = Mathf.Max(0f, worldTime);
    }
}

public interface ITotemEnemyParticipantSource
{
    int ParticipantCount { get; }

    TotemActorModel GetParticipantAt(int index);

    bool CanBeTargeted(TotemActorModel participant);

    bool IsReachable(TotemEnemyModel enemy, TotemActorModel participant);
}

public interface ITotemEnemyReachabilityProvider
{
    bool IsReachable(TotemEnemyModel enemy, TotemActorModel participant);
}

public sealed class TotemActorEnemyParticipantSource : ITotemEnemyParticipantSource
{
    private readonly TotemActorService _actorService;
    private readonly TotemParticipantReadinessService _readinessService;
    private readonly ITotemEnemyReachabilityProvider _reachabilityProvider;

    public TotemActorEnemyParticipantSource(
        TotemActorService actorService,
        TotemParticipantReadinessService readinessService,
        ITotemEnemyReachabilityProvider reachabilityProvider = null)
    {
        _actorService = actorService;
        _readinessService = readinessService;
        _reachabilityProvider = reachabilityProvider;
    }

    public int ParticipantCount => _actorService?.Actors?.Count ?? 0;

    public TotemActorModel GetParticipantAt(int index)
    {
        var actors = _actorService?.Actors;
        return actors != null && index >= 0 && index < actors.Count ? actors[index] : null;
    }

    public bool CanBeTargeted(TotemActorModel participant)
    {
        return participant != null
            && TotemActorService.IsParticipantActor(participant)
            && (_readinessService == null || _readinessService.CanBeTargeted(participant))
            && (_actorService == null || _actorService.CanEnemyTarget(participant));
    }

    public bool IsReachable(TotemEnemyModel enemy, TotemActorModel participant)
    {
        return _reachabilityProvider == null || _reachabilityProvider.IsReachable(enemy, participant);
    }
}

public interface ITotemEnemyPathProvider
{
    bool TryBuildPath(Vector3 start, Vector3 destination, Vector3[] nodeBuffer, out int nodeCount);
}

public interface ITotemEnemySpawnGate
{
    bool CanSpawn(int encounterInstanceId, string enemyId, int requestedCount, out TotemEnemySpawnBlockReason reason);
}

public interface ITotemEnemyRuntimeBridge
{
    void OnEnemySpawned(TotemEnemyModel enemy, string runtimeAssetKey);

    void OnEnemyDespawned(TotemEnemyModel enemy);

    bool TryMove(TotemEnemyModel enemy, Vector3 delta);

    bool ResolveDamage(in TotemEnemyDamageCommand command);

    void ApplyStatus(TotemEnemyModel source, TotemActorModel target, string statusId, float statusChance, string abilityId);

    void SpawnProjectile(in TotemEnemyProjectileCommand command);

    void CreateHazard(in TotemEnemyHazardCommand command);

    void PlayCue(TotemEnemyModel enemy, string vfxId, string audioCueId);
}

public interface ITotemEnemyObserver
{
    void OnStateChanged(in TotemEnemyStateChangedEvent evt);

    void OnTargetChanged(in TotemEnemyTargetChangedEvent evt);

    void OnAbilityChanged(in TotemEnemyAbilityEvent evt);

    void OnBossPhaseChanged(in TotemBossPhaseChangedEvent evt);
}

[Serializable]
public sealed class TotemEnemyRuntimeSnapshot
{
    public int capacity;
    public int definitionCount;
    public int enemyCount;
    public int aliveEnemyCount;
    public int lightCount;
    public int eliteCount;
    public int bossCount;
    public int hotCount;
    public int warmCount;
    public int coldCount;
    public int totalSpawns;
    public int totalDespawns;
    public int totalDeaths;
    public int totalDecisions;
    public int totalAbilityStarts;
    public int totalPathRequests;
    public int blockedPathRequests;
    public int blockedSummons;
    public int activeStatusCount;
    public int totalStatusApplications;
    public int totalStatusTicks;
    public int rejectedStatusApplications;
    public int lastEnemyCombatantId;
    public int lastTargetId;
    public string lastEnemyId;
    public string lastState;
    public string lastReason;
    public string lastAbilityId;
    public string lastSpawnBlockReason;
    public float worldTime;
}

[Serializable]
public sealed class TotemEnemyInstanceSnapshot
{
    public int combatantId;
    public string enemyId;
    public string tier;
    public string state;
    public string lod;
    public int targetId;
    public float health;
    public float maxHealth;
    public float shield;
    public int bossPhase;
    public string activeAbilityId;
    public int activeStatusCount;
    public bool stunned;
    public float moveSpeedMultiplier;
    public float stateElapsed;
    public float worldTime;
}

public sealed class TotemEnemyStandaloneBridge : ITotemEnemyRuntimeBridge
{
    public void OnEnemySpawned(TotemEnemyModel enemy, string runtimeAssetKey)
    {
    }

    public void OnEnemyDespawned(TotemEnemyModel enemy)
    {
    }

    public bool TryMove(TotemEnemyModel enemy, Vector3 delta)
    {
        if (enemy == null || !enemy.IsAlive)
        {
            return false;
        }

        enemy.Position += delta;
        if (enemy.GameObject != null)
        {
            enemy.GameObject.transform.position = enemy.Position;
        }

        return true;
    }

    public bool ResolveDamage(in TotemEnemyDamageCommand command)
    {
        return command.Target != null && command.Target.ApplyDamage(command.Amount) > 0f;
    }

    public void ApplyStatus(TotemEnemyModel source, TotemActorModel target, string statusId, float statusChance, string abilityId)
    {
    }

    public void SpawnProjectile(in TotemEnemyProjectileCommand command)
    {
    }

    public void CreateHazard(in TotemEnemyHazardCommand command)
    {
    }

    public void PlayCue(TotemEnemyModel enemy, string vfxId, string audioCueId)
    {
    }
}
