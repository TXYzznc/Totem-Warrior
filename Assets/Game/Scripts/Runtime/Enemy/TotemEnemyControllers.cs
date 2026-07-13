using System;
using UnityEngine;

public sealed class TotemEnemyPathBudget
{
    private int _remaining;

    public TotemEnemyPathBudget(int requestsPerFrame)
    {
        RequestsPerFrame = Mathf.Max(0, requestsPerFrame);
        _remaining = RequestsPerFrame;
    }

    public int RequestsPerFrame { get; private set; }

    public int Remaining => _remaining;

    public void Configure(int requestsPerFrame)
    {
        RequestsPerFrame = Mathf.Max(0, requestsPerFrame);
        _remaining = Mathf.Min(_remaining, RequestsPerFrame);
    }

    public void BeginFrame()
    {
        _remaining = RequestsPerFrame;
    }

    public bool TryConsume()
    {
        if (_remaining <= 0)
        {
            return false;
        }

        _remaining--;
        return true;
    }
}

public abstract class TotemEnemyControllerBase
{
    private const float ReturnArrivalSqr = 0.25f;
    private const float ProgressEpsilonSqr = 0.0004f;
    private readonly ITotemEnemyAbility[] _abilities;
    private readonly Vector3[] _pathNodes;
    private readonly ITotemEnemyObserver _observer;
    private ITotemEnemyAbility _activeAbility;
    private TotemActorModel _target;
    private TotemEnemyState _staggerReturnState;
    private float _decisionRemaining;
    private float _staggerRemaining;
    private float _noProgressElapsed;
    private float _pathExpiresAt;
    private int _pathCount;
    private int _pathIndex;
    private int _cachedTargetCellX = int.MinValue;
    private int _cachedTargetCellZ = int.MinValue;
    private Vector3 _lastProgressPosition;
    private bool _deathHandled;

    protected TotemEnemyControllerBase(
        TotemEnemyModel enemy,
        TotemEnemyRuntimeDefinition definition,
        ITotemEnemyObserver observer)
    {
        Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _observer = observer;
        Threat = new TotemEnemyThreatTable();

        TotemEnemyAbilityRuntimeDefinition[] definitions = definition.abilities ?? Array.Empty<TotemEnemyAbilityRuntimeDefinition>();
        _abilities = new ITotemEnemyAbility[definitions.Length];
        for (int i = 0; i < definitions.Length; i++)
        {
            _abilities[i] = TotemEnemyAbilityFactory.Create(definitions[i]);
        }

        int pathCapacity = definition.behavior == null ? 32 : definition.behavior.pathNodeCapacity;
        _pathNodes = new Vector3[Mathf.Max(4, pathCapacity)];
        State = TotemEnemyState.Dormant;
        Lod = TotemEnemyLod.Cold;
        BossPhase = 1;
        DamageMultiplier = 1f;
        _lastProgressPosition = enemy.Position;
    }

    public TotemEnemyModel Enemy { get; }

    public TotemEnemyRuntimeDefinition Definition { get; }

    public TotemEnemyThreatTable Threat { get; }

    public TotemEnemyState State { get; private set; }

    public TotemEnemyLod Lod { get; private set; }

    public TotemActorModel Target => _target;

    public ITotemEnemyAbility ActiveAbility => _activeAbility;

    public float StateElapsed { get; private set; }

    public float Shield { get; private set; }

    public int BossPhase { get; protected set; }

    public float DamageMultiplier { get; protected set; }

    public int DecisionCount { get; private set; }

    public bool DeathHandled => _deathHandled;

    protected TotemEnemyBehaviorDefinition Behavior => Definition.behavior ?? DefaultBehavior;

    private static TotemEnemyBehaviorDefinition DefaultBehavior { get; } = new TotemEnemyBehaviorDefinition();

    public void Activate(float worldTime)
    {
        if (State != TotemEnemyState.Dormant)
        {
            return;
        }

        Transition(TotemEnemyState.Spawn, "SpawnRegistered", worldTime);
    }

    public void Tick(
        float deltaTime,
        float worldTime,
        ITotemEnemyParticipantSource participants,
        ITotemEnemyAbilityHost host,
        ITotemEnemyPathProvider pathProvider,
        TotemEnemyPathBudget pathBudget)
    {
        if (deltaTime <= 0f || State == TotemEnemyState.Dead || !Enemy.IsAlive)
        {
            return;
        }

        StateElapsed += deltaTime;
        UpdateLod(participants);
        var abilityContext = new TotemEnemyAbilityContext(this, host, _target, worldTime);
        TickAbilities(abilityContext, deltaTime, worldTime);
        TickPolicy(deltaTime, worldTime, host);

        if (State == TotemEnemyState.Spawn)
        {
            Transition(TotemEnemyState.Patrol, "SpawnCompleted", worldTime);
        }

        TickMovement(deltaTime, worldTime, host, pathProvider, pathBudget);
        if (State == TotemEnemyState.Stagger)
        {
            _staggerRemaining -= deltaTime;
            if (_staggerRemaining <= 0f)
            {
                TotemEnemyState next = IsTargetValid(_target, worldTime, participants)
                    ? _staggerReturnState
                    : TotemEnemyState.Patrol;
                if (next == TotemEnemyState.AttackWindup || next == TotemEnemyState.Cast || next == TotemEnemyState.AttackActive)
                {
                    next = TotemEnemyState.Recover;
                }

                Transition(next, "StaggerCompleted", worldTime);
            }
        }

        _decisionRemaining -= deltaTime;
        if (_decisionRemaining > 0f || State == TotemEnemyState.Stagger)
        {
            return;
        }

        _decisionRemaining = ResolveDecisionInterval();
        DecisionCount++;
        Decide(worldTime, participants, host);
    }

    public void AddThreat(TotemActorModel source, float amount, float worldTime)
    {
        Threat.AddDamage(source, amount, worldTime);
    }

    public void ReceiveGroupAlert(TotemActorModel source, float amount, float worldTime)
    {
        if (source == null || State == TotemEnemyState.Dead)
        {
            return;
        }

        Threat.AddAlert(source, amount, worldTime);
        if (_target == null)
        {
            SetTarget(source, "GroupAlert", worldTime);
        }

        if (State == TotemEnemyState.Patrol || State == TotemEnemyState.Return)
        {
            Transition(TotemEnemyState.Alert, "GroupAlert", worldTime);
        }
    }

    public float AbsorbDamage(float amount)
    {
        if (amount <= 0f || Shield <= 0f)
        {
            return amount;
        }

        float absorbed = Mathf.Min(Shield, amount);
        Shield -= absorbed;
        return amount - absorbed;
    }

    public void AddShield(float amount)
    {
        if (amount > 0f)
        {
            Shield = Mathf.Min(Enemy.MaxHealth, Shield + amount);
        }
    }

    public void Interrupt(TotemActorModel source, float threat, float worldTime, ITotemEnemyAbilityHost host)
    {
        AddThreat(source, threat, worldTime);
        if (_activeAbility == null || !_activeAbility.Definition.interruptible ||
            (_activeAbility.Phase != TotemEnemyAbilityPhase.Windup && _activeAbility.Phase != TotemEnemyAbilityPhase.Active))
        {
            return;
        }

        var context = new TotemEnemyAbilityContext(this, host, _target, worldTime);
        _activeAbility.Cancel(context, "InterruptedByDamage");
        _staggerReturnState = TotemEnemyState.Chase;
        _staggerRemaining = 0.25f;
        Transition(TotemEnemyState.Stagger, "AbilityInterrupted", worldTime);
    }

    public bool MarkDead(float worldTime, string reason, ITotemEnemyAbilityHost host)
    {
        if (_deathHandled)
        {
            return false;
        }

        _deathHandled = true;
        var context = new TotemEnemyAbilityContext(this, host, _target, worldTime);
        for (int i = 0; i < _abilities.Length; i++)
        {
            _abilities[i]?.OnOwnerDeath(context);
        }

        Transition(TotemEnemyState.Dead, string.IsNullOrEmpty(reason) ? "HealthDepleted" : reason, worldTime);
        SetTarget(null, "OwnerDead", worldTime);
        return true;
    }

    public void FillSnapshot(TotemEnemyInstanceSnapshot snapshot, float worldTime)
    {
        if (snapshot == null)
        {
            return;
        }

        snapshot.combatantId = Enemy.CombatantId;
        snapshot.enemyId = Enemy.EnemyId;
        snapshot.tier = Enemy.Tier.ToString();
        snapshot.state = State.ToString();
        snapshot.lod = Lod.ToString();
        snapshot.targetId = _target?.ActorId ?? 0;
        snapshot.health = Enemy.Health;
        snapshot.maxHealth = Enemy.MaxHealth;
        snapshot.shield = Shield;
        snapshot.bossPhase = BossPhase;
        snapshot.activeAbilityId = _activeAbility?.Definition?.abilityId ?? string.Empty;
        snapshot.stateElapsed = StateElapsed;
        snapshot.worldTime = worldTime;
    }

    protected virtual void TickPolicy(float deltaTime, float worldTime, ITotemEnemyAbilityHost host)
    {
    }

    protected virtual float ModifyAbilityScore(ITotemEnemyAbility ability, float score)
    {
        return score;
    }

    protected virtual void OnTargetAcquired(TotemActorModel target, float worldTime)
    {
    }

    protected void NotifyBossPhase(in TotemBossPhaseChangedEvent evt)
    {
        _observer?.OnBossPhaseChanged(evt);
    }

    protected void SetTarget(TotemActorModel target, string reason, float worldTime)
    {
        if (ReferenceEquals(_target, target))
        {
            return;
        }

        int previousId = _target?.ActorId ?? 0;
        _target = target;
        int currentId = _target?.ActorId ?? 0;
        var evt = new TotemEnemyTargetChangedEvent(Enemy, previousId, currentId, reason, worldTime);
        _observer?.OnTargetChanged(evt);
        GFTrace.Info(
            "TotemEnemy",
            "Enemy.TargetChanged",
            null,
            GFTrace.Data(
                "enemyId", Enemy.EnemyId,
                "entityId", Enemy.CombatantId.ToString(),
                "fromTarget", previousId.ToString(),
                "toTarget", currentId.ToString(),
                "reason", reason ?? string.Empty,
                "worldTime", worldTime.ToString("F3")));

        if (target != null)
        {
            OnTargetAcquired(target, worldTime);
        }
    }

    protected bool Transition(TotemEnemyState next, string reason, float worldTime)
    {
        if (State == next || State == TotemEnemyState.Dead || string.IsNullOrEmpty(reason) || !IsLegalTransition(State, next))
        {
            return false;
        }

        TotemEnemyState previous = State;
        State = next;
        StateElapsed = 0f;
        var evt = new TotemEnemyStateChangedEvent(Enemy, previous, next, reason, _target?.ActorId ?? 0, worldTime);
        _observer?.OnStateChanged(evt);
        GFTrace.Info(
            "TotemEnemy",
            "Enemy.StateChanged",
            null,
            GFTrace.Data(
                "enemyId", Enemy.EnemyId,
                "entityId", Enemy.CombatantId.ToString(),
                "from", previous.ToString(),
                "to", next.ToString(),
                "reason", reason,
                "targetId", (_target?.ActorId ?? 0).ToString(),
                "worldTime", worldTime.ToString("F3")));
        return true;
    }

    protected static float FlatSqrDistance(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float z = a.z - b.z;
        return x * x + z * z;
    }

    private void TickAbilities(in TotemEnemyAbilityContext context, float deltaTime, float worldTime)
    {
        for (int i = 0; i < _abilities.Length; i++)
        {
            ITotemEnemyAbility ability = _abilities[i];
            if (ability == null)
            {
                continue;
            }

            ability.Tick(context, deltaTime);
        }

        if (_activeAbility == null)
        {
            return;
        }

        switch (_activeAbility.Phase)
        {
            case TotemEnemyAbilityPhase.Active:
                Transition(TotemEnemyState.AttackActive, "AbilityActive", worldTime);
                break;
            case TotemEnemyAbilityPhase.Recovery:
                Transition(TotemEnemyState.Recover, "AbilityRecovery", worldTime);
                break;
            case TotemEnemyAbilityPhase.Complete:
                _activeAbility = null;
                if (State == TotemEnemyState.AttackWindup || State == TotemEnemyState.Cast || State == TotemEnemyState.AttackActive)
                {
                    Transition(TotemEnemyState.Recover, "AbilityRecoverySkipped", worldTime);
                }
                Transition(_target == null ? TotemEnemyState.Return : TotemEnemyState.Chase, "AbilityCompleted", worldTime);
                break;
            case TotemEnemyAbilityPhase.Cancelled:
                _activeAbility = null;
                if (State != TotemEnemyState.Stagger)
                {
                    Transition(TotemEnemyState.Recover, "AbilityCancelled", worldTime);
                }
                break;
        }
    }

    private void Decide(float worldTime, ITotemEnemyParticipantSource participants, ITotemEnemyAbilityHost host)
    {
        Threat.PruneInvalid(participants, worldTime);
        SelectTarget(worldTime, participants);

        if (_target == null)
        {
            if (State == TotemEnemyState.Chase || State == TotemEnemyState.Alert || State == TotemEnemyState.Recover)
            {
                Transition(TotemEnemyState.Return, "TargetLost", worldTime);
            }
            return;
        }

        if (FlatSqrDistance(Enemy.Position, Enemy.SpawnPosition) > Behavior.leashRange * Behavior.leashRange)
        {
            SetTarget(null, "LeashExceeded", worldTime);
            Transition(TotemEnemyState.Return, "LeashExceeded", worldTime);
            return;
        }

        if (State == TotemEnemyState.Patrol || State == TotemEnemyState.Return)
        {
            Transition(TotemEnemyState.Alert, "TargetDetected", worldTime);
            return;
        }

        if (State == TotemEnemyState.Alert)
        {
            Transition(TotemEnemyState.Chase, "AlertConfirmed", worldTime);
            return;
        }

        if (State != TotemEnemyState.Chase || _activeAbility != null)
        {
            return;
        }

        ITotemEnemyAbility selected = SelectAbility(worldTime, host);
        if (selected == null)
        {
            return;
        }

        _activeAbility = selected;
        var context = new TotemEnemyAbilityContext(this, host, _target, worldTime);
        selected.Begin(context);
        TotemEnemyAbilityType type = selected.Definition.abilityType;
        bool cast = type == TotemEnemyAbilityType.Projectile || type == TotemEnemyAbilityType.Beam ||
                    type == TotemEnemyAbilityType.AreaPulse || type == TotemEnemyAbilityType.HazardZone ||
                    type == TotemEnemyAbilityType.Shield || type == TotemEnemyAbilityType.Summon ||
                    type == TotemEnemyAbilityType.Regenerate;
        Transition(cast ? TotemEnemyState.Cast : TotemEnemyState.AttackWindup, "AbilitySelected", worldTime);
    }

    private ITotemEnemyAbility SelectAbility(float worldTime, ITotemEnemyAbilityHost host)
    {
        var context = new TotemEnemyAbilityContext(this, host, _target, worldTime);
        ITotemEnemyAbility best = null;
        float bestScore = float.MinValue;
        for (int i = 0; i < _abilities.Length; i++)
        {
            ITotemEnemyAbility ability = _abilities[i];
            if (ability == null || !ability.CanStart(context))
            {
                continue;
            }

            float score = ModifyAbilityScore(ability, ability.Score(context));
            if (score > bestScore)
            {
                best = ability;
                bestScore = score;
            }
        }

        return best;
    }

    private void SelectTarget(float worldTime, ITotemEnemyParticipantSource participants)
    {
        if (participants == null)
        {
            SetTarget(null, "ParticipantSourceMissing", worldTime);
            return;
        }

        bool currentValid = IsTargetValid(_target, worldTime, participants);
        float currentScore = currentValid ? ScoreTarget(_target, worldTime) : float.MinValue;
        TotemActorModel best = null;
        float bestScore = float.MinValue;
        int count = participants.ParticipantCount;
        for (int i = 0; i < count; i++)
        {
            TotemActorModel candidate = participants.GetParticipantAt(i);
            if (!IsTargetValid(candidate, worldTime, participants))
            {
                continue;
            }

            float score = ScoreTarget(candidate, worldTime);
            if (score > bestScore || (Mathf.Approximately(score, bestScore) && candidate.ActorId < (best?.ActorId ?? int.MaxValue)))
            {
                best = candidate;
                bestScore = score;
            }
        }

        if (!currentValid)
        {
            if (_target != null)
            {
                Threat.Remove(_target.ActorId);
            }
            SetTarget(best, best == null ? "NoValidTarget" : "ThreatAcquire", worldTime);
            return;
        }

        if (best == null || ReferenceEquals(best, _target))
        {
            return;
        }

        float threshold = currentScore <= 0f ? 0f : currentScore * TotemEnemyThreatTable.TargetSwitchMultiplier;
        if (bestScore >= threshold)
        {
            SetTarget(best, "ThreatOverride", worldTime);
        }
    }

    private bool IsTargetValid(TotemActorModel candidate, float worldTime, ITotemEnemyParticipantSource participants)
    {
        if (candidate == null || participants == null || !participants.CanBeTargeted(candidate) || !participants.IsReachable(Enemy, candidate))
        {
            return false;
        }

        float range = Mathf.Max(0.1f, Behavior.detectRange);
        if (FlatSqrDistance(Enemy.Position, candidate.Position) > range * range)
        {
            return false;
        }

        var context = new TotemCombatRelationshipContext(worldTime);
        return TotemCombatRelationshipService.Evaluate(Enemy, candidate, context).Allowed;
    }

    private float ScoreTarget(TotemActorModel candidate, float worldTime)
    {
        float range = Mathf.Max(0.1f, Behavior.detectRange);
        float distance = Mathf.Sqrt(FlatSqrDistance(Enemy.Position, candidate.Position));
        float proximity = Mathf.Max(0f, 10f * (1f - distance / range));
        return Threat.GetScore(candidate, proximity, worldTime);
    }

    private void TickMovement(
        float deltaTime,
        float worldTime,
        ITotemEnemyAbilityHost host,
        ITotemEnemyPathProvider pathProvider,
        TotemEnemyPathBudget pathBudget)
    {
        if (State == TotemEnemyState.Chase && _target != null)
        {
            float stopRange = Mathf.Max(0.1f, Behavior.attackRange * 0.85f);
            if (FlatSqrDistance(Enemy.Position, _target.Position) > stopRange * stopRange)
            {
                MoveToward(_target.Position, deltaTime, worldTime, host, pathProvider, pathBudget, true);
            }
            return;
        }

        if (State != TotemEnemyState.Return)
        {
            _noProgressElapsed = 0f;
            return;
        }

        if (FlatSqrDistance(Enemy.Position, Enemy.SpawnPosition) <= ReturnArrivalSqr)
        {
            Transition(TotemEnemyState.Patrol, "ReturnedToLeash", worldTime);
            return;
        }

        MoveToward(Enemy.SpawnPosition, deltaTime, worldTime, host, null, null, false);
    }

    private void MoveToward(
        Vector3 destination,
        float deltaTime,
        float worldTime,
        ITotemEnemyAbilityHost host,
        ITotemEnemyPathProvider pathProvider,
        TotemEnemyPathBudget pathBudget,
        bool allowPath)
    {
        Vector3 moveTarget = destination;
        int targetCellX = Mathf.FloorToInt(destination.x);
        int targetCellZ = Mathf.FloorToInt(destination.z);
        if (_pathCount > 0 && worldTime <= _pathExpiresAt && targetCellX == _cachedTargetCellX && targetCellZ == _cachedTargetCellZ)
        {
            moveTarget = _pathNodes[Mathf.Min(_pathIndex, _pathCount - 1)];
            if (FlatSqrDistance(Enemy.Position, moveTarget) <= 0.16f)
            {
                _pathIndex++;
                if (_pathIndex >= _pathCount)
                {
                    InvalidatePath();
                    moveTarget = destination;
                }
                else
                {
                    moveTarget = _pathNodes[_pathIndex];
                }
            }
        }
        else if (_pathCount > 0)
        {
            InvalidatePath();
        }

        Vector3 direction = moveTarget - Enemy.Position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float maxDistance = Mathf.Max(0f, Behavior.moveSpeed) * deltaTime;
        Vector3 delta = direction.normalized * Mathf.Min(maxDistance, direction.magnitude);
        host?.TryMove(this, delta);

        float progress = FlatSqrDistance(_lastProgressPosition, Enemy.Position);
        if (progress > ProgressEpsilonSqr)
        {
            _lastProgressPosition = Enemy.Position;
            _noProgressElapsed = 0f;
            return;
        }

        _noProgressElapsed += deltaTime;
        if (!allowPath || pathProvider == null || pathBudget == null || _noProgressElapsed < Mathf.Max(0.1f, Behavior.noProgressSeconds))
        {
            return;
        }

        _noProgressElapsed = 0f;
        if (!pathBudget.TryConsume())
        {
            host?.NotifyPathRequest(this, false);
            return;
        }

        bool found = pathProvider.TryBuildPath(Enemy.Position, destination, _pathNodes, out _pathCount);
        _pathCount = Mathf.Clamp(_pathCount, 0, _pathNodes.Length);
        _pathIndex = 0;
        _cachedTargetCellX = targetCellX;
        _cachedTargetCellZ = targetCellZ;
        _pathExpiresAt = worldTime + Mathf.Max(0.1f, Behavior.pathCacheSeconds);
        if (!found || _pathCount <= 0)
        {
            InvalidatePath();
        }
        host?.NotifyPathRequest(this, found && _pathCount > 0);
    }

    private void UpdateLod(ITotemEnemyParticipantSource participants)
    {
        float nearestSqr = float.MaxValue;
        if (participants != null)
        {
            int count = participants.ParticipantCount;
            for (int i = 0; i < count; i++)
            {
                TotemActorModel participant = participants.GetParticipantAt(i);
                if (participant == null || !participants.CanBeTargeted(participant))
                {
                    continue;
                }

                float sqr = FlatSqrDistance(Enemy.Position, participant.Position);
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                }
            }
        }

        float hot = Mathf.Max(0f, Behavior.hotRadius);
        float warm = Mathf.Max(hot, Behavior.warmRadius);
        Lod = nearestSqr <= hot * hot
            ? TotemEnemyLod.Hot
            : nearestSqr <= warm * warm ? TotemEnemyLod.Warm : TotemEnemyLod.Cold;
    }

    private float ResolveDecisionInterval()
    {
        float hz;
        switch (Enemy.Tier)
        {
            case TotemEnemyTier.Boss:
                hz = Lod == TotemEnemyLod.Hot ? Behavior.bossHotHz : Lod == TotemEnemyLod.Warm ? Behavior.bossWarmHz : Behavior.bossColdHz;
                break;
            case TotemEnemyTier.Elite:
                hz = Lod == TotemEnemyLod.Hot ? Behavior.eliteHotHz : Lod == TotemEnemyLod.Warm ? Behavior.eliteWarmHz : Behavior.eliteColdHz;
                break;
            default:
                hz = Lod == TotemEnemyLod.Hot ? Behavior.lightHotHz : Lod == TotemEnemyLod.Warm ? Behavior.lightWarmHz : Behavior.lightColdHz;
                break;
        }

        return 1f / Mathf.Max(0.01f, hz);
    }

    private void InvalidatePath()
    {
        _pathCount = 0;
        _pathIndex = 0;
        _cachedTargetCellX = int.MinValue;
        _cachedTargetCellZ = int.MinValue;
        _pathExpiresAt = 0f;
    }

    private static bool IsLegalTransition(TotemEnemyState from, TotemEnemyState to)
    {
        if (to == TotemEnemyState.Dead || (to == TotemEnemyState.Stagger && from != TotemEnemyState.Dormant))
        {
            return true;
        }

        switch (from)
        {
            case TotemEnemyState.Dormant: return to == TotemEnemyState.Spawn;
            case TotemEnemyState.Spawn: return to == TotemEnemyState.Patrol;
            case TotemEnemyState.Patrol: return to == TotemEnemyState.Alert;
            case TotemEnemyState.Alert: return to == TotemEnemyState.Chase || to == TotemEnemyState.Return;
            case TotemEnemyState.Chase: return to == TotemEnemyState.AttackWindup || to == TotemEnemyState.Cast || to == TotemEnemyState.Return;
            case TotemEnemyState.AttackWindup:
            case TotemEnemyState.Cast: return to == TotemEnemyState.AttackActive || to == TotemEnemyState.Recover;
            case TotemEnemyState.AttackActive: return to == TotemEnemyState.Recover;
            case TotemEnemyState.Recover: return to == TotemEnemyState.Chase || to == TotemEnemyState.Return;
            case TotemEnemyState.Return: return to == TotemEnemyState.Patrol || to == TotemEnemyState.Alert;
            case TotemEnemyState.Stagger: return to == TotemEnemyState.Patrol || to == TotemEnemyState.Chase || to == TotemEnemyState.Recover || to == TotemEnemyState.Return;
            default: return false;
        }
    }
}

public sealed class TotemLightEnemyController : TotemEnemyControllerBase
{
    public TotemLightEnemyController(TotemEnemyModel enemy, TotemEnemyRuntimeDefinition definition, ITotemEnemyObserver observer)
        : base(enemy, definition, observer)
    {
    }
}

public sealed class TotemEliteEnemyController : TotemEnemyControllerBase
{
    public TotemEliteEnemyController(TotemEnemyModel enemy, TotemEnemyRuntimeDefinition definition, ITotemEnemyObserver observer)
        : base(enemy, definition, observer)
    {
    }

    protected override float ModifyAbilityScore(ITotemEnemyAbility ability, float score)
    {
        if (Enemy.Health / Enemy.MaxHealth <= 0.35f &&
            (ability.Definition.abilityType == TotemEnemyAbilityType.Shield || ability.Definition.abilityType == TotemEnemyAbilityType.Regenerate))
        {
            return score + 10f;
        }

        return score;
    }
}

public class TotemBossEnemyController : TotemEnemyControllerBase
{
    private readonly TotemBossPhaseDefinition[] _phases;
    private readonly TotemEnemyAbilityRuntimeDefinition[] _phaseCues;

    public TotemBossEnemyController(TotemEnemyModel enemy, TotemEnemyRuntimeDefinition definition, ITotemEnemyObserver observer)
        : base(enemy, definition, observer)
    {
        BossPhase = 0;
        _phases = definition.bossPhases ?? Array.Empty<TotemBossPhaseDefinition>();
        _phaseCues = new TotemEnemyAbilityRuntimeDefinition[_phases.Length];
        for (int i = 0; i < _phases.Length; i++)
        {
            TotemBossPhaseDefinition phase = _phases[i];
            _phaseCues[i] = phase == null
                ? null
                : new TotemEnemyAbilityRuntimeDefinition
                {
                    abilityId = "phase_transition",
                    abilityType = TotemEnemyAbilityType.PhaseTransition,
                    vfxId = phase.vfxId,
                    audioCueId = phase.audioCueId,
                };
        }
    }

    protected override void TickPolicy(float deltaTime, float worldTime, ITotemEnemyAbilityHost host)
    {
        if (!Enemy.IsAlive || Enemy.MaxHealth <= 0f)
        {
            return;
        }

        float ratio = Enemy.Health / Enemy.MaxHealth;
        int desiredPhase = BossPhase;
        for (int i = 0; i < _phases.Length; i++)
        {
            TotemBossPhaseDefinition phase = _phases[i];
            if (phase != null && phase.phase > desiredPhase && ratio <= phase.enterHealthRatio)
            {
                desiredPhase = phase.phase;
            }
        }

        while (BossPhase < desiredPhase)
        {
            EnterPhase(BossPhase + 1, worldTime, host);
        }
    }

    protected virtual void OnPhaseEntered(int phase, float worldTime, ITotemEnemyAbilityHost host)
    {
    }

    private void EnterPhase(int phaseNumber, float worldTime, ITotemEnemyAbilityHost host)
    {
        TotemBossPhaseDefinition definition = FindPhase(phaseNumber);
        int previous = BossPhase;
        BossPhase = phaseNumber;
        DamageMultiplier = definition == null ? DamageMultiplier : Mathf.Max(0f, definition.damageMultiplier);
        var evt = new TotemBossPhaseChangedEvent(
            Enemy,
            previous,
            BossPhase,
            definition?.vfxId,
            definition?.audioCueId,
            DamageMultiplier,
            worldTime);
        NotifyBossPhase(evt);
        GFTrace.Info(
            "TotemEnemy",
            "Enemy.BossPhaseChanged",
            null,
            GFTrace.Data(
                "enemyId", Enemy.EnemyId,
                "entityId", Enemy.CombatantId.ToString(),
                "from", previous.ToString(),
                "to", BossPhase.ToString(),
                "worldTime", worldTime.ToString("F3")));

        TotemEnemyAbilityRuntimeDefinition cue = FindPhaseCue(phaseNumber);
        if (cue != null)
        {
            host?.PlayCue(this, cue);
        }

        OnPhaseEntered(BossPhase, worldTime, host);
    }

    private TotemBossPhaseDefinition FindPhase(int phaseNumber)
    {
        for (int i = 0; i < _phases.Length; i++)
        {
            if (_phases[i] != null && _phases[i].phase == phaseNumber)
            {
                return _phases[i];
            }
        }

        return null;
    }

    private TotemEnemyAbilityRuntimeDefinition FindPhaseCue(int phaseNumber)
    {
        for (int i = 0; i < _phases.Length; i++)
        {
            if (_phases[i] != null && _phases[i].phase == phaseNumber)
            {
                return _phaseCues[i];
            }
        }

        return null;
    }
}

public sealed class TotemCoreZeroBossController : TotemBossEnemyController
{
    private static readonly TotemEnemyAbilityRuntimeDefinition PhaseSummon = new TotemEnemyAbilityRuntimeDefinition
    {
        abilityId = "core_zero_phase_summon",
        abilityType = TotemEnemyAbilityType.Summon,
        summonEnemyId = "enemy_ai_arc_drone",
    };

    public TotemCoreZeroBossController(TotemEnemyModel enemy, TotemEnemyRuntimeDefinition definition, ITotemEnemyObserver observer)
        : base(enemy, definition, observer) { }

    protected override void OnPhaseEntered(int phase, float worldTime, ITotemEnemyAbilityHost host)
    {
        TryPhaseSummon(host, phase == 2 ? 1 : 2, "enemy_ai_arc_drone", worldTime);
    }

    private void TryPhaseSummon(ITotemEnemyAbilityHost host, int count, string enemyId, float worldTime)
    {
        host?.TrySummon(this, PhaseSummon, count);
    }
}

public sealed class TotemHiveMotherBossController : TotemBossEnemyController
{
    private static readonly TotemEnemyAbilityRuntimeDefinition BroodSummon = new TotemEnemyAbilityRuntimeDefinition
    {
        abilityId = "hive_mother_brood",
        abilityType = TotemEnemyAbilityType.Summon,
        summonEnemyId = "enemy_alien_crawler",
    };

    public TotemHiveMotherBossController(TotemEnemyModel enemy, TotemEnemyRuntimeDefinition definition, ITotemEnemyObserver observer)
        : base(enemy, definition, observer) { }

    protected override void OnPhaseEntered(int phase, float worldTime, ITotemEnemyAbilityHost host)
    {
        int count = phase == 2 ? 2 : 3;
        host?.TrySummon(this, BroodSummon, count);
    }
}

public sealed class TotemVirusTerminusBossController : TotemBossEnemyController
{
    private static readonly TotemEnemyAbilityRuntimeDefinition SplitSummon = new TotemEnemyAbilityRuntimeDefinition
    {
        abilityId = "virus_terminus_split",
        abilityType = TotemEnemyAbilityType.Summon,
        summonEnemyId = "enemy_virus_spore_carrier",
    };

    public TotemVirusTerminusBossController(TotemEnemyModel enemy, TotemEnemyRuntimeDefinition definition, ITotemEnemyObserver observer)
        : base(enemy, definition, observer) { }

    protected override void OnPhaseEntered(int phase, float worldTime, ITotemEnemyAbilityHost host)
    {
        int count = phase == 2 ? 2 : 4;
        host?.TrySummon(this, SplitSummon, count);
    }
}
