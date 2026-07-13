using System;
using UnityEngine;

public sealed class TotemEnemyService : TotemRuntimeServiceBase, ITotemRuntimeTickService, ITotemEnemyAbilityHost, ITotemEnemyObserver, ITotemEnemyDeathEventSource, ITotemEnemyStatusTickSink
{
    public const int DefaultEnemyCapacity = 128;
    public const int DefaultDefinitionCapacity = 48;
    public const int DefaultPathRequestsPerFrame = 4;

    private readonly RuntimeEntry[] _entries;
    private readonly TotemEnemyRuntimeDefinition[] _definitions;
    private readonly TotemEnemyPathBudget _pathBudget;
    private readonly TotemEnemyStandaloneBridge _standaloneBridge = new TotemEnemyStandaloneBridge();
    private int _definitionCount;
    private int _enemyCount;
    private int _nextSummonedCombatantId = 2000000000;
    private float _worldTime;
    private ITotemEnemyParticipantSource _participants;
    private ITotemEnemyRuntimeBridge _bridge;
    private ITotemEnemyPathProvider _pathProvider;
    private ITotemEnemySpawnGate _spawnGate;
    private ITotemEnemyObserver _observer;
    private TotemCombatRelationshipService _relationshipService;
    private int _totalSpawns;
    private int _totalDespawns;
    private int _totalDeaths;
    private int _totalDecisions;
    private int _totalAbilityStarts;
    private int _totalPathRequests;
    private int _blockedPathRequests;
    private int _blockedSummons;
    private int _totalStatusApplications;
    private int _totalStatusTicks;
    private int _rejectedStatusApplications;
    private int _lastEnemyCombatantId;
    private int _lastTargetId;
    private string _lastEnemyId = string.Empty;
    private string _lastSpawnedEnemyId = string.Empty;
    private string _lastDiedEnemyId = string.Empty;
    private string _lastState = string.Empty;
    private string _lastReason = string.Empty;
    private string _lastAbilityId = string.Empty;
    private string _lastSpawnBlockReason = string.Empty;

    public TotemEnemyService()
        : this(DefaultEnemyCapacity, DefaultDefinitionCapacity, DefaultPathRequestsPerFrame)
    {
    }

    public TotemEnemyService(int enemyCapacity, int definitionCapacity, int pathRequestsPerFrame)
    {
        _entries = new RuntimeEntry[Mathf.Max(1, enemyCapacity)];
        _definitions = new TotemEnemyRuntimeDefinition[Mathf.Max(TotemEnemyBuiltInCatalog.DefinitionCount, definitionCapacity)];
        _pathBudget = new TotemEnemyPathBudget(pathRequestsPerFrame);
        _bridge = _standaloneBridge;
        RegisterDefinitions(TotemEnemyBuiltInCatalog.CreateDefinitions());
    }

    public override string ServiceName => "Enemy";

    public int EnemyCount => _enemyCount;

    public int DefinitionCount => _definitionCount;

    public int Capacity => _entries.Length;

    public float WorldTime => _worldTime;

    public event Action<TotemEnemySpawnedEvent> EnemySpawned;

    public event Action<TotemEnemyDiedEvent> EnemyDied;

    public event Action<TotemEnemyStateChangedEvent> StateChanged;

    public event Action<TotemEnemyTargetChangedEvent> TargetChanged;

    public event Action<TotemEnemyAbilityEvent> AbilityChanged;

    public event Action<TotemBossPhaseChangedEvent> BossPhaseChanged;

    public void Configure(
        ITotemEnemyParticipantSource participants,
        ITotemEnemyRuntimeBridge bridge,
        ITotemEnemyPathProvider pathProvider,
        ITotemEnemySpawnGate spawnGate,
        TotemCombatRelationshipService relationshipService,
        ITotemEnemyObserver observer = null)
    {
        _participants = participants;
        _bridge = bridge ?? _standaloneBridge;
        _pathProvider = pathProvider;
        _spawnGate = spawnGate;
        _relationshipService = relationshipService;
        _observer = observer;
    }

    public void ConfigurePathBudget(int requestsPerFrame)
    {
        _pathBudget.Configure(requestsPerFrame);
    }

    public void ConfigureSummonedCombatantIdStart(int firstCombatantId)
    {
        _nextSummonedCombatantId = firstCombatantId;
    }

    public bool RegisterDefinition(TotemEnemyRuntimeDefinition definition)
    {
        if (definition == null || string.IsNullOrEmpty(definition.enemyId))
        {
            return false;
        }

        for (int i = 0; i < _definitionCount; i++)
        {
            if (string.Equals(_definitions[i].enemyId, definition.enemyId, StringComparison.Ordinal))
            {
                _definitions[i] = definition;
                return true;
            }
        }

        if (_definitionCount >= _definitions.Length)
        {
            return false;
        }

        _definitions[_definitionCount++] = definition;
        return true;
    }

    public void RegisterDefinitions(TotemEnemyRuntimeDefinition[] definitions)
    {
        if (definitions == null)
        {
            return;
        }

        for (int i = 0; i < definitions.Length; i++)
        {
            RegisterDefinition(definitions[i]);
        }
    }

    public void RegisterCatalogDefinitions(
        TotemEnemyDefinition[] enemyDefinitions,
        TotemEnemyAbilityDefinition[] abilityDefinitions,
        TotemBossPhase[] bossPhases)
    {
        if (enemyDefinitions == null)
        {
            return;
        }

        for (int i = 0; i < enemyDefinitions.Length; i++)
        {
            RegisterDefinition(TotemEnemyCatalogAdapter.CreateRuntimeDefinition(
                enemyDefinitions[i],
                abilityDefinitions,
                bossPhases));
        }
    }

    public bool TryGetDefinition(string enemyId, out TotemEnemyRuntimeDefinition definition)
    {
        for (int i = 0; i < _definitionCount; i++)
        {
            TotemEnemyRuntimeDefinition candidate = _definitions[i];
            if (candidate != null && string.Equals(candidate.enemyId, enemyId, StringComparison.Ordinal))
            {
                definition = candidate;
                return true;
            }
        }

        definition = null;
        return false;
    }

    public bool TrySpawn(in TotemEnemySpawnRequest request, out TotemEnemyModel enemy, out TotemEnemySpawnBlockReason reason)
    {
        enemy = null;
        _worldTime = Mathf.Max(_worldTime, request.WorldTime);
        if (FindEntryIndex(request.CombatantId) >= 0)
        {
            reason = TotemEnemySpawnBlockReason.DuplicateCombatantId;
            RecordSpawnBlocked(reason, request.EnemyId);
            return false;
        }

        if (!TryGetDefinition(request.EnemyId, out TotemEnemyRuntimeDefinition definition))
        {
            reason = TotemEnemySpawnBlockReason.DefinitionMissing;
            RecordSpawnBlocked(reason, request.EnemyId);
            return false;
        }

        int slot = FindFreeEntryIndex();
        if (slot < 0)
        {
            reason = TotemEnemySpawnBlockReason.ServiceCapacity;
            RecordSpawnBlocked(reason, request.EnemyId);
            return false;
        }

        if (_spawnGate != null && !_spawnGate.CanSpawn(request.EncounterInstanceId, request.EnemyId, 1, out reason))
        {
            RecordSpawnBlocked(reason, request.EnemyId);
            return false;
        }

        enemy = new TotemEnemyModel(
            request.CombatantId,
            definition.enemyId,
            definition.displayName,
            definition.themeId,
            definition.tier,
            definition.maxHealth,
            request.Position)
        {
            BehaviorProfileId = definition.behavior?.behaviorProfileId ?? string.Empty,
            AbilityIds = definition.abilityIds ?? string.Empty,
            LootTableId = definition.lootTableId ?? string.Empty,
            GuaranteedLootIds = definition.guaranteedLootIds ?? string.Empty,
            EncounterInstanceId = request.EncounterInstanceId,
            SpawnPosition = request.Position,
            LeashRange = definition.behavior?.leashRange ?? 0f,
            VisualAssetKey = definition.runtimeAssetKey ?? string.Empty,
        };

        TotemEnemyControllerBase controller = CreateController(enemy, definition);
        _entries[slot] = new RuntimeEntry
        {
            Enemy = enemy,
            Controller = controller,
            Definition = definition,
            AnchorId = request.AnchorId,
            Statuses = new TotemEnemyStatusRuntime(),
            MovementMultiplier = 1f,
        };
        _enemyCount++;
        _totalSpawns++;
        _lastEnemyCombatantId = enemy.CombatantId;
        _lastEnemyId = enemy.EnemyId;
        _lastSpawnedEnemyId = enemy.EnemyId;
        _lastReason = "Spawned";
        _bridge.OnEnemySpawned(enemy, definition.runtimeAssetKey);
        controller.Activate(request.WorldTime);
        var evt = new TotemEnemySpawnedEvent(enemy, request.AnchorId, request.WorldTime);
        EnemySpawned?.Invoke(evt);
        GFTrace.Info(
            "TotemEnemy",
            "Enemy.Spawned",
            null,
            GFTrace.Data(
                "enemyId", enemy.EnemyId,
                "entityId", enemy.CombatantId.ToString(),
                "tier", enemy.Tier.ToString(),
                "anchorId", request.AnchorId,
                "worldTime", request.WorldTime.ToString("F3")));
        reason = TotemEnemySpawnBlockReason.None;
        return true;
    }

    public bool Despawn(int combatantId, string reason)
    {
        int index = FindEntryIndex(combatantId);
        if (index < 0)
        {
            return false;
        }

        TotemEnemyModel enemy = _entries[index].Enemy;
        _bridge.OnEnemyDespawned(enemy);
        _entries[index] = default;
        _enemyCount--;
        _totalDespawns++;
        _lastEnemyCombatantId = combatantId;
        _lastEnemyId = enemy?.EnemyId ?? string.Empty;
        _lastReason = string.IsNullOrEmpty(reason) ? "Despawned" : reason;
        return true;
    }

    public TotemEnemyModel FindEnemy(int combatantId)
    {
        int index = FindEntryIndex(combatantId);
        return index < 0 ? null : _entries[index].Enemy;
    }

    public TotemEnemyControllerBase FindController(int combatantId)
    {
        int index = FindEntryIndex(combatantId);
        return index < 0 ? null : _entries[index].Controller;
    }

    public TotemEnemyModel FindClosestAliveEnemy(Vector3 origin, float maxRange = 0f, TotemEnemyTier tier = TotemEnemyTier.Unknown)
    {
        float maxSqr = maxRange <= 0f ? float.MaxValue : maxRange * maxRange;
        float bestSqr = float.MaxValue;
        TotemEnemyModel best = null;
        for (int i = 0; i < _entries.Length; i++)
        {
            TotemEnemyModel candidate = _entries[i].Enemy;
            if (candidate == null || !candidate.IsAlive || (tier != TotemEnemyTier.Unknown && candidate.Tier != tier))
            {
                continue;
            }

            float sqr = FlatSqrDistance(origin, candidate.Position);
            if (sqr <= maxSqr && sqr < bestSqr)
            {
                bestSqr = sqr;
                best = candidate;
            }
        }

        return best;
    }

    public TotemEnemyModel FindBestAimTarget(
        Vector3 origin,
        Vector3 forward,
        float maxRange,
        float halfAngleDegrees,
        TotemEnemyTier tier = TotemEnemyTier.Unknown)
    {
        forward.y = 0f;
        forward = forward.sqrMagnitude <= 0.0001f ? Vector3.forward : forward.normalized;
        float maxSqr = maxRange <= 0f ? float.MaxValue : maxRange * maxRange;
        float cosHalfAngle = halfAngleDegrees >= 179.99f
            ? -1f
            : Mathf.Cos(Mathf.Clamp(halfAngleDegrees, 0f, 180f) * Mathf.Deg2Rad);
        float bestScore = float.MaxValue;
        TotemEnemyModel best = null;
        for (int i = 0; i < _entries.Length; i++)
        {
            TotemEnemyModel candidate = _entries[i].Enemy;
            if (candidate == null || !candidate.IsAlive || (tier != TotemEnemyTier.Unknown && candidate.Tier != tier))
            {
                continue;
            }

            Vector3 delta = candidate.Position - origin;
            delta.y = 0f;
            float sqr = delta.sqrMagnitude;
            if (sqr <= 0.0001f || sqr > maxSqr)
            {
                continue;
            }

            float distance = Mathf.Sqrt(sqr);
            float dot = Vector3.Dot(forward, delta / distance);
            if (dot < cosHalfAngle)
            {
                continue;
            }

            float score = (1f - dot) * 100f + distance;
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    public int CopyAliveEnemies(TotemEnemyModel[] destination)
    {
        if (destination == null || destination.Length <= 0)
        {
            return 0;
        }

        int written = 0;
        for (int i = 0; i < _entries.Length && written < destination.Length; i++)
        {
            TotemEnemyModel enemy = _entries[i].Enemy;
            if (enemy != null && enemy.IsAlive)
            {
                destination[written++] = enemy;
            }
        }

        return written;
    }

    public bool TryInterruptEnemy(int combatantId, TotemActorModel source, float threat, string reason, float worldTime)
    {
        int index = FindEntryIndex(combatantId);
        if (index < 0 || source == null || !_entries[index].Enemy.IsAlive)
        {
            return false;
        }

        _entries[index].Controller.AddThreat(source, Mathf.Max(0f, threat), worldTime);
        _entries[index].Controller.Interrupt(source, Mathf.Max(0f, threat), worldTime, this);
        _lastEnemyCombatantId = combatantId;
        _lastTargetId = source.ActorId;
        _lastReason = string.IsNullOrWhiteSpace(reason) ? "Interrupted" : reason;
        return true;
    }

    public bool TryDisplaceEnemy(int combatantId, Vector3 delta)
    {
        int index = FindEntryIndex(combatantId);
        return index >= 0 && _bridge.TryMove(_entries[index].Enemy, delta);
    }

    public bool TryApplyStatus(
        int enemyCombatantId,
        TotemCombatantModel source,
        string statusId,
        float power,
        float duration,
        string reason,
        float worldTime,
        out TotemEnemyStatusApplyResult result)
    {
        if (!TotemEnemyStatusDefinition.TryCreateBuiltIn(statusId, power, duration, out TotemEnemyStatusDefinition definition))
        {
            _rejectedStatusApplications++;
            result = TotemEnemyStatusApplyResult.InvalidDefinition;
            return false;
        }

        return TryApplyStatus(enemyCombatantId, source, definition, reason, worldTime, out result);
    }

    public bool TryApplyStatus(
        int enemyCombatantId,
        TotemCombatantModel source,
        in TotemEnemyStatusDefinition definition,
        string reason,
        float worldTime,
        out TotemEnemyStatusApplyResult result)
    {
        int index = FindEntryIndex(enemyCombatantId);
        if (index < 0 || _entries[index].Enemy == null || !_entries[index].Enemy.IsAlive)
        {
            _rejectedStatusApplications++;
            result = TotemEnemyStatusApplyResult.InvalidTarget;
            return false;
        }

        if (!definition.IsValid)
        {
            _rejectedStatusApplications++;
            result = TotemEnemyStatusApplyResult.InvalidDefinition;
            return false;
        }

        _worldTime = Mathf.Max(_worldTime, worldTime);
        var context = new TotemCombatRelationshipContext(
            worldTime,
            allowEnemyFriendlyFire: definition.CanHitEnemies,
            worldDamageAffectsEnemies: definition.WorldDamageAffectsEnemies);
        TotemCombatRelationshipDecision decision = _relationshipService == null
            ? TotemCombatRelationshipService.Evaluate(source, _entries[index].Enemy, context)
            : _relationshipService.EvaluateDamage(source, _entries[index].Enemy, context);
        if (!decision.Allowed)
        {
            _rejectedStatusApplications++;
            _lastReason = decision.Reason.ToString();
            result = TotemEnemyStatusApplyResult.RelationshipBlocked;
            return false;
        }

        TotemEnemyStatusRuntime statuses = _entries[index].Statuses;
        if (statuses == null)
        {
            _rejectedStatusApplications++;
            result = TotemEnemyStatusApplyResult.CapacityReached;
            return false;
        }

        if (!statuses.TryApply(definition, source, reason, worldTime, out result))
        {
            _rejectedStatusApplications++;
            return false;
        }

        _totalStatusApplications++;
        _lastEnemyCombatantId = enemyCombatantId;
        _lastEnemyId = _entries[index].Enemy.EnemyId;
        _lastReason = "Status:" + definition.StatusId + ":" + result;
        if (definition.Kind == TotemEnemyStatusKind.Stun && source is TotemActorModel participantSource)
        {
            _entries[index].Controller.Interrupt(
                participantSource,
                Mathf.Max(1f, definition.Power),
                worldTime,
                this);
        }

        return true;
    }

    public bool HasStatus(int enemyCombatantId, string statusId)
    {
        int index = FindEntryIndex(enemyCombatantId);
        return index >= 0 && _entries[index].Statuses != null && _entries[index].Statuses.HasStatus(statusId, _worldTime);
    }

    public bool IsStunned(int enemyCombatantId)
    {
        int index = FindEntryIndex(enemyCombatantId);
        return index >= 0 && _entries[index].Statuses != null && _entries[index].Statuses.IsStunned(_worldTime);
    }

    public float GetMoveSpeedMultiplier(int enemyCombatantId)
    {
        int index = FindEntryIndex(enemyCombatantId);
        if (index < 0 || _entries[index].Enemy == null || !_entries[index].Enemy.IsAlive)
        {
            return 0f;
        }

        return _entries[index].Statuses?.GetMoveSpeedMultiplier(_worldTime) ?? 1f;
    }

    public bool TryGetStatusRemaining(int enemyCombatantId, string statusId, out float remaining)
    {
        int index = FindEntryIndex(enemyCombatantId);
        if (index < 0 || _entries[index].Statuses == null)
        {
            remaining = 0f;
            return false;
        }

        return _entries[index].Statuses.TryGetRemaining(statusId, _worldTime, out remaining);
    }

    public bool TryApplyDamage(
        int enemyCombatantId,
        TotemCombatantModel source,
        float amount,
        string reason,
        float worldTime,
        out float appliedDamage,
        bool canHitEnemies = false,
        bool worldDamageAffectsEnemies = false,
        bool canInterrupt = true)
    {
        appliedDamage = 0f;
        int index = FindEntryIndex(enemyCombatantId);
        if (index < 0 || amount <= 0f)
        {
            return false;
        }

        RuntimeEntry entry = _entries[index];
        if (entry.Enemy == null || !entry.Enemy.IsAlive || entry.Controller.DeathHandled)
        {
            return false;
        }

        _worldTime = Mathf.Max(_worldTime, worldTime);
        var context = new TotemCombatRelationshipContext(worldTime, allowEnemyFriendlyFire: canHitEnemies, worldDamageAffectsEnemies: worldDamageAffectsEnemies);
        TotemCombatRelationshipDecision decision = _relationshipService == null
            ? TotemCombatRelationshipService.Evaluate(source, entry.Enemy, context)
            : _relationshipService.EvaluateDamage(source, entry.Enemy, context);
        if (!decision.Allowed)
        {
            _lastReason = decision.Reason.ToString();
            return false;
        }

        float healthDamage = entry.Controller.AbsorbDamage(amount);
        appliedDamage = entry.Enemy.ApplyDamage(healthDamage);
        TotemActorModel participantSource = source as TotemActorModel;
        if (participantSource != null)
        {
            entry.Controller.AddThreat(participantSource, amount, worldTime);
            AlertGroup(entry, participantSource, amount * 0.25f, worldTime);
            if (canInterrupt && appliedDamage > 0f)
            {
                entry.Controller.Interrupt(participantSource, amount, worldTime, this);
            }
        }

        _lastEnemyCombatantId = entry.Enemy.CombatantId;
        _lastEnemyId = entry.Enemy.EnemyId;
        _lastReason = string.IsNullOrEmpty(reason) ? "DamageApplied" : reason;
        if (!entry.Enemy.IsAlive)
        {
            HandleDeath(index, participantSource, reason, worldTime);
        }

        return appliedDamage > 0f || healthDamage <= 0f;
    }

    public bool TryHeal(int enemyCombatantId, float amount, out float healed)
    {
        healed = 0f;
        TotemEnemyModel enemy = FindEnemy(enemyCombatantId);
        if (enemy == null || !enemy.IsAlive || amount <= 0f)
        {
            return false;
        }

        healed = enemy.Heal(amount);
        return healed > 0f;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        float frameStartTime = _worldTime;
        _worldTime += deltaTime;
        _pathBudget.BeginFrame();
        for (int i = 0; i < _entries.Length; i++)
        {
            TotemEnemyControllerBase controller = _entries[i].Controller;
            if (controller == null)
            {
                continue;
            }

            if (!_entries[i].Enemy.IsAlive)
            {
                HandleDeath(i, null, "HealthDepleted", _worldTime);
                continue;
            }

            TotemEnemyStatusRuntime statuses = _entries[i].Statuses;
            TotemEnemyModel statusOwner = _entries[i].Enemy;
            bool controlBlocked = statuses != null && statuses.IsStunned(frameStartTime);
            _entries[i].MovementMultiplier = statuses?.GetMoveSpeedMultiplier(frameStartTime) ?? 1f;
            statuses?.Tick(statusOwner.CombatantId, _worldTime, this);
            if (_entries[i].Enemy == null
                || !ReferenceEquals(_entries[i].Enemy, statusOwner)
                || !statusOwner.IsAlive)
            {
                continue;
            }

            if (controlBlocked)
            {
                continue;
            }

            int decisionsBefore = controller.DecisionCount;
            controller.Tick(deltaTime, _worldTime, _participants, this, _pathProvider, _pathBudget);
            _totalDecisions += controller.DecisionCount - decisionsBefore;
        }
    }

    public TotemEnemyRuntimeSnapshot CaptureSnapshot()
    {
        var snapshot = new TotemEnemyRuntimeSnapshot
        {
            capacity = _entries.Length,
            definitionCount = _definitionCount,
            enemyCount = _enemyCount,
            totalSpawns = _totalSpawns,
            totalDespawns = _totalDespawns,
            totalDeaths = _totalDeaths,
            totalDecisions = _totalDecisions,
            totalAbilityStarts = _totalAbilityStarts,
            totalPathRequests = _totalPathRequests,
            blockedPathRequests = _blockedPathRequests,
            blockedSummons = _blockedSummons,
            totalStatusApplications = _totalStatusApplications,
            totalStatusTicks = _totalStatusTicks,
            rejectedStatusApplications = _rejectedStatusApplications,
            lastEnemyCombatantId = _lastEnemyCombatantId,
            lastTargetId = _lastTargetId,
            lastEnemyId = _lastEnemyId,
            lastState = _lastState,
            lastReason = _lastReason,
            lastAbilityId = _lastAbilityId,
            lastSpawnBlockReason = _lastSpawnBlockReason,
            worldTime = _worldTime,
        };

        for (int i = 0; i < _entries.Length; i++)
        {
            RuntimeEntry entry = _entries[i];
            if (entry.Enemy == null)
            {
                continue;
            }

            if (!entry.Enemy.IsAlive)
            {
                continue;
            }

            snapshot.aliveEnemyCount++;
            snapshot.activeStatusCount += entry.Statuses?.Count ?? 0;
            switch (entry.Enemy.Tier)
            {
                case TotemEnemyTier.Boss: snapshot.bossCount++; break;
                case TotemEnemyTier.Elite: snapshot.eliteCount++; break;
                case TotemEnemyTier.Light: snapshot.lightCount++; break;
            }

            switch (entry.Controller.Lod)
            {
                case TotemEnemyLod.Hot: snapshot.hotCount++; break;
                case TotemEnemyLod.Warm: snapshot.warmCount++; break;
                case TotemEnemyLod.Cold: snapshot.coldCount++; break;
            }
        }

        return snapshot;
    }

    public TotemEnemyDomainSnapshot CaptureDomainSnapshot()
    {
        TotemEnemyRuntimeSnapshot runtime = CaptureSnapshot();
        return new TotemEnemyDomainSnapshot
        {
            enemyCount = runtime.enemyCount,
            aliveEnemyCount = runtime.aliveEnemyCount,
            lightCount = runtime.lightCount,
            eliteCount = runtime.eliteCount,
            bossCount = runtime.bossCount,
            hotCount = runtime.hotCount,
            warmCount = runtime.warmCount,
            coldCount = runtime.coldCount,
            lastSpawnedEnemyId = _lastSpawnedEnemyId,
            lastDiedEnemyId = _lastDiedEnemyId,
        };
    }

    public int CopyInstanceSnapshots(TotemEnemyInstanceSnapshot[] destination)
    {
        if (destination == null || destination.Length == 0)
        {
            return 0;
        }

        int written = 0;
        for (int i = 0; i < _entries.Length && written < destination.Length; i++)
        {
            TotemEnemyControllerBase controller = _entries[i].Controller;
            if (controller == null)
            {
                continue;
            }

            TotemEnemyInstanceSnapshot snapshot = destination[written];
            if (snapshot == null)
            {
                snapshot = new TotemEnemyInstanceSnapshot();
                destination[written] = snapshot;
            }
            controller.FillSnapshot(snapshot, _worldTime);
            snapshot.activeStatusCount = _entries[i].Statuses?.Count ?? 0;
            snapshot.stunned = _entries[i].Statuses != null && _entries[i].Statuses.IsStunned(_worldTime);
            snapshot.moveSpeedMultiplier = _entries[i].Statuses?.GetMoveSpeedMultiplier(_worldTime) ?? 1f;
            written++;
        }

        return written;
    }

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        if (_relationshipService == null)
        {
            _relationshipService = runtime.GetService<TotemCombatRelationshipService>();
        }
    }

    protected override void OnShutdown()
    {
        for (int i = 0; i < _entries.Length; i++)
        {
            if (_entries[i].Enemy != null)
            {
                _bridge.OnEnemyDespawned(_entries[i].Enemy);
                _entries[i] = default;
            }
        }

        _enemyCount = 0;
        _worldTime = 0f;
        _totalSpawns = 0;
        _totalDespawns = 0;
        _totalDeaths = 0;
        _totalDecisions = 0;
        _totalAbilityStarts = 0;
        _totalPathRequests = 0;
        _blockedPathRequests = 0;
        _blockedSummons = 0;
        _totalStatusApplications = 0;
        _totalStatusTicks = 0;
        _rejectedStatusApplications = 0;
        _lastEnemyCombatantId = 0;
        _lastTargetId = 0;
        _lastEnemyId = string.Empty;
        _lastSpawnedEnemyId = string.Empty;
        _lastDiedEnemyId = string.Empty;
        _lastState = string.Empty;
        _lastReason = string.Empty;
        _lastAbilityId = string.Empty;
        _lastSpawnBlockReason = string.Empty;
    }

    bool ITotemEnemyAbilityHost.TryDamageTarget(
        TotemEnemyControllerBase controller,
        TotemActorModel target,
        TotemEnemyAbilityRuntimeDefinition definition,
        float multiplier)
    {
        if (controller == null || target == null || definition == null)
        {
            return false;
        }

        var relationshipContext = new TotemCombatRelationshipContext(
            _worldTime,
            allowEnemyFriendlyFire: definition.canHitEnemies);
        TotemCombatantModel relationshipSource = definition.abilityType == TotemEnemyAbilityType.DeathBurst && !controller.Enemy.IsAlive
            ? null
            : controller.Enemy;
        TotemCombatRelationshipDecision decision = _relationshipService == null
            ? TotemCombatRelationshipService.Evaluate(relationshipSource, target, relationshipContext)
            : _relationshipService.EvaluateDamage(relationshipSource, target, relationshipContext);
        if (!decision.Allowed)
        {
            _lastReason = decision.Reason.ToString();
            return false;
        }

        float damage = controller.Definition.baseDamage * definition.damageMultiplier * controller.DamageMultiplier * Mathf.Max(0f, multiplier);
        var command = new TotemEnemyDamageCommand(
            controller.Enemy,
            target,
            definition.abilityId,
            damage,
            definition.canHitEnemies,
            _worldTime);
        bool resolved = _bridge.ResolveDamage(command);
        if (resolved
            && !string.IsNullOrWhiteSpace(definition.statusId)
            && ShouldApplyStatus(controller.Enemy, target, definition))
        {
            _bridge.ApplyStatus(
                controller.Enemy,
                target,
                definition.statusId,
                definition.statusChance,
                definition.abilityId);
        }
        _lastEnemyCombatantId = controller.Enemy.CombatantId;
        _lastEnemyId = controller.Enemy.EnemyId;
        _lastTargetId = target.CombatantId;
        _lastReason = resolved ? "DamageResolved" : "DamageBridgeRejected";
        return resolved;
    }

    int ITotemEnemyAbilityHost.DamageParticipantsInRadius(
        TotemEnemyControllerBase controller,
        Vector3 center,
        float radius,
        TotemEnemyAbilityRuntimeDefinition definition)
    {
        if (_participants == null || controller == null || definition == null)
        {
            return 0;
        }

        float radiusSqr = radius * radius;
        int hits = 0;
        int count = _participants.ParticipantCount;
        for (int i = 0; i < count; i++)
        {
            TotemActorModel participant = _participants.GetParticipantAt(i);
            if (participant == null || FlatSqrDistance(center, participant.Position) > radiusSqr)
            {
                continue;
            }

            if (((ITotemEnemyAbilityHost)this).TryDamageTarget(controller, participant, definition, 1f))
            {
                hits++;
            }
        }

        return hits;
    }

    int ITotemEnemyAbilityHost.DamageParticipantsInCone(
        TotemEnemyControllerBase controller,
        Vector3 origin,
        Vector3 forward,
        float radius,
        float halfAngle,
        TotemEnemyAbilityRuntimeDefinition definition)
    {
        if (_participants == null || controller == null || definition == null)
        {
            return 0;
        }

        float radiusSqr = radius * radius;
        float minDot = Mathf.Cos(Mathf.Clamp(halfAngle, 0f, 180f) * Mathf.Deg2Rad);
        forward.y = 0f;
        forward = forward.sqrMagnitude <= 0.0001f ? Vector3.forward : forward.normalized;
        int hits = 0;
        int count = _participants.ParticipantCount;
        for (int i = 0; i < count; i++)
        {
            TotemActorModel participant = _participants.GetParticipantAt(i);
            if (participant == null)
            {
                continue;
            }

            Vector3 delta = participant.Position - origin;
            delta.y = 0f;
            if (delta.sqrMagnitude > radiusSqr || delta.sqrMagnitude <= 0.0001f || Vector3.Dot(forward, delta.normalized) < minDot)
            {
                continue;
            }

            if (((ITotemEnemyAbilityHost)this).TryDamageTarget(controller, participant, definition, 1f))
            {
                hits++;
            }
        }

        return hits;
    }

    bool ITotemEnemyAbilityHost.TryMove(TotemEnemyControllerBase controller, Vector3 delta)
    {
        if (controller == null)
        {
            return false;
        }

        int entryIndex = FindEntryIndex(controller.Enemy.CombatantId);
        float movementMultiplier = entryIndex < 0 ? 1f : Mathf.Clamp01(_entries[entryIndex].MovementMultiplier);
        delta *= movementMultiplier;
        if (delta.sqrMagnitude <= 0.000001f)
        {
            return false;
        }

        if (_bridge.TryMove(controller.Enemy, delta))
        {
            return true;
        }

        Vector3 axis = new Vector3(delta.x, 0f, 0f);
        if (axis.sqrMagnitude > 0.000001f && _bridge.TryMove(controller.Enemy, axis))
        {
            return true;
        }

        axis = new Vector3(0f, 0f, delta.z);
        return axis.sqrMagnitude > 0.000001f && _bridge.TryMove(controller.Enemy, axis);
    }

    bool ITotemEnemyAbilityHost.TrySummon(TotemEnemyControllerBase controller, TotemEnemyAbilityRuntimeDefinition definition, int count)
    {
        if (controller == null || definition == null || string.IsNullOrEmpty(definition.summonEnemyId) || count <= 0)
        {
            return false;
        }

        if (_enemyCount + count > _entries.Length)
        {
            RecordSummonBlocked(TotemEnemySpawnBlockReason.ServiceCapacity, controller.Enemy.EnemyId);
            return false;
        }

        if (_spawnGate != null && !_spawnGate.CanSpawn(
                controller.Enemy.EncounterInstanceId,
                definition.summonEnemyId,
                count,
                out TotemEnemySpawnBlockReason gateReason))
        {
            RecordSummonBlocked(gateReason, controller.Enemy.EnemyId);
            return false;
        }

        int spawned = 0;
        for (int i = 0; i < count; i++)
        {
            float angle = (controller.Enemy.CombatantId * 31 + i * 137) * Mathf.Deg2Rad;
            Vector3 position = controller.Enemy.Position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (1.5f + i * 0.25f);
            var request = new TotemEnemySpawnRequest(
                _nextSummonedCombatantId--,
                definition.summonEnemyId,
                position,
                controller.Enemy.EncounterInstanceId,
                "ability_summon",
                _worldTime);
            if (TrySpawn(request, out _, out _))
            {
                spawned++;
            }
        }

        return spawned == count;
    }

    void ITotemEnemyAbilityHost.SpawnProjectile(
        TotemEnemyControllerBase controller,
        TotemActorModel target,
        TotemEnemyAbilityRuntimeDefinition definition)
    {
        if (controller == null || target == null || definition == null)
        {
            return;
        }

        Vector3 direction = target.Position - controller.Enemy.Position;
        direction.y = 0f;
        direction = direction.sqrMagnitude <= 0.0001f ? Vector3.forward : direction.normalized;
        float damage = controller.Definition.baseDamage * definition.damageMultiplier * controller.DamageMultiplier;
        var command = new TotemEnemyProjectileCommand(
            controller.Enemy,
            target,
            definition.abilityId,
            controller.Enemy.Position,
            direction,
            damage,
            _worldTime);
        _bridge.SpawnProjectile(command);
    }

    void ITotemEnemyAbilityHost.CreateHazard(
        TotemEnemyControllerBase controller,
        Vector3 position,
        TotemEnemyAbilityRuntimeDefinition definition)
    {
        if (controller == null || definition == null)
        {
            return;
        }

        float damage = controller.Definition.baseDamage * definition.damageMultiplier * controller.DamageMultiplier;
        var command = new TotemEnemyHazardCommand(
            controller.Enemy,
            definition.abilityId,
            position,
            definition.radius,
            damage,
            _worldTime);
        _bridge.CreateHazard(command);
    }

    void ITotemEnemyAbilityHost.PlayCue(TotemEnemyControllerBase controller, TotemEnemyAbilityRuntimeDefinition definition)
    {
        if (controller != null && definition != null)
        {
            _bridge.PlayCue(controller.Enemy, definition.vfxId, definition.audioCueId);
        }
    }

    void ITotemEnemyAbilityHost.NotifyAbility(TotemEnemyControllerBase controller, ITotemEnemyAbility ability, string reason)
    {
        if (controller == null || ability == null)
        {
            return;
        }

        if (string.Equals(reason, "Begin", StringComparison.Ordinal))
        {
            _totalAbilityStarts++;
        }

        _lastEnemyCombatantId = controller.Enemy.CombatantId;
        _lastEnemyId = controller.Enemy.EnemyId;
        _lastTargetId = controller.Target?.ActorId ?? 0;
        _lastAbilityId = ability.Definition?.abilityId ?? string.Empty;
        _lastReason = reason ?? string.Empty;
        var evt = new TotemEnemyAbilityEvent(
            controller.Enemy,
            _lastAbilityId,
            ability.Definition.abilityType,
            ability.Phase,
            _lastTargetId,
            reason,
            _worldTime);
        OnAbilityChanged(evt);
        GFTrace.Info(
            "TotemEnemy",
            "Enemy.AbilityChanged",
            null,
            GFTrace.Data(
                "enemyId", controller.Enemy.EnemyId,
                "entityId", controller.Enemy.CombatantId.ToString(),
                "abilityId", _lastAbilityId,
                "phase", ability.Phase.ToString(),
                "targetId", _lastTargetId.ToString(),
                "reason", reason ?? string.Empty,
                "worldTime", _worldTime.ToString("F3")));
    }

    void ITotemEnemyAbilityHost.NotifyPathRequest(TotemEnemyControllerBase controller, bool accepted)
    {
        _totalPathRequests++;
        if (!accepted)
        {
            _blockedPathRequests++;
        }
    }

    void ITotemEnemyObserver.OnStateChanged(in TotemEnemyStateChangedEvent evt)
    {
        _lastEnemyCombatantId = evt.Enemy?.CombatantId ?? 0;
        _lastEnemyId = evt.Enemy?.EnemyId ?? string.Empty;
        _lastTargetId = evt.TargetId;
        _lastState = evt.Current.ToString();
        _lastReason = evt.Reason;
        StateChanged?.Invoke(evt);
        _observer?.OnStateChanged(evt);
    }

    void ITotemEnemyObserver.OnTargetChanged(in TotemEnemyTargetChangedEvent evt)
    {
        _lastEnemyCombatantId = evt.Enemy?.CombatantId ?? 0;
        _lastEnemyId = evt.Enemy?.EnemyId ?? string.Empty;
        _lastTargetId = evt.CurrentTargetId;
        _lastReason = evt.Reason;
        TargetChanged?.Invoke(evt);
        _observer?.OnTargetChanged(evt);
    }

    void ITotemEnemyObserver.OnAbilityChanged(in TotemEnemyAbilityEvent evt)
    {
        OnAbilityChanged(evt);
    }

    void ITotemEnemyObserver.OnBossPhaseChanged(in TotemBossPhaseChangedEvent evt)
    {
        _lastEnemyCombatantId = evt.Enemy?.CombatantId ?? 0;
        _lastEnemyId = evt.Enemy?.EnemyId ?? string.Empty;
        _lastReason = "BossPhaseChanged";
        BossPhaseChanged?.Invoke(evt);
        _observer?.OnBossPhaseChanged(evt);
    }

    private void OnAbilityChanged(in TotemEnemyAbilityEvent evt)
    {
        AbilityChanged?.Invoke(evt);
        _observer?.OnAbilityChanged(evt);
    }

    bool ITotemEnemyStatusTickSink.ApplyStatusTick(int enemyCombatantId, in TotemEnemyStatusTick tick)
    {
        TotemEnemyModel enemy = FindEnemy(enemyCombatantId);
        if (enemy == null || !enemy.IsAlive)
        {
            return false;
        }

        _totalStatusTicks++;
        TryApplyDamage(
            enemyCombatantId,
            tick.Source,
            tick.Damage,
            tick.Reason,
            tick.WorldTime,
            out _,
            tick.CanHitEnemies,
            tick.WorldDamageAffectsEnemies,
            canInterrupt: false);
        return enemy.IsAlive;
    }

    private TotemEnemyControllerBase CreateController(TotemEnemyModel enemy, TotemEnemyRuntimeDefinition definition)
    {
        if (definition.tier == TotemEnemyTier.Light)
        {
            return new TotemLightEnemyController(enemy, definition, this);
        }

        if (definition.tier == TotemEnemyTier.Elite)
        {
            return new TotemEliteEnemyController(enemy, definition, this);
        }

        switch (definition.enemyId)
        {
            case "boss_ai_core_zero": return new TotemCoreZeroBossController(enemy, definition, this);
            case "boss_alien_hive_mother": return new TotemHiveMotherBossController(enemy, definition, this);
            case "boss_virus_terminus": return new TotemVirusTerminusBossController(enemy, definition, this);
            default: return new TotemBossEnemyController(enemy, definition, this);
        }
    }

    private void HandleDeath(int entryIndex, TotemActorModel killer, string reason, float worldTime)
    {
        RuntimeEntry entry = _entries[entryIndex];
        if (entry.Controller == null || !entry.Controller.MarkDead(worldTime, reason, this))
        {
            return;
        }

        _totalDeaths++;
        _lastEnemyCombatantId = entry.Enemy.CombatantId;
        _lastEnemyId = entry.Enemy.EnemyId;
        _lastDiedEnemyId = entry.Enemy.EnemyId;
        _lastReason = string.IsNullOrEmpty(reason) ? "HealthDepleted" : reason;
        var evt = new TotemEnemyDiedEvent(entry.Enemy, killer, _lastReason, worldTime);
        EnemyDied?.Invoke(evt);
        GFTrace.Info(
            "TotemEnemy",
            "Enemy.Died",
            null,
            GFTrace.Data(
                "enemyId", entry.Enemy.EnemyId,
                "entityId", entry.Enemy.CombatantId.ToString(),
                "killerId", (killer?.ActorId ?? 0).ToString(),
                "reason", _lastReason,
                "worldTime", worldTime.ToString("F3")));
    }

    private void AlertGroup(RuntimeEntry damaged, TotemActorModel source, float amount, float worldTime)
    {
        float radius = damaged.Definition.behavior?.groupAlertRadius ?? 0f;
        float radiusSqr = radius * radius;
        for (int i = 0; i < _entries.Length; i++)
        {
            RuntimeEntry candidate = _entries[i];
            if (candidate.Controller == null || candidate.Enemy.EncounterInstanceId != damaged.Enemy.EncounterInstanceId ||
                FlatSqrDistance(candidate.Enemy.Position, damaged.Enemy.Position) > radiusSqr)
            {
                continue;
            }

            candidate.Controller.ReceiveGroupAlert(source, amount, worldTime);
        }
    }

    private int FindEntryIndex(int combatantId)
    {
        for (int i = 0; i < _entries.Length; i++)
        {
            if (_entries[i].Enemy != null && _entries[i].Enemy.CombatantId == combatantId)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindFreeEntryIndex()
    {
        for (int i = 0; i < _entries.Length; i++)
        {
            if (_entries[i].Enemy == null)
            {
                return i;
            }
        }

        return -1;
    }

    private void RecordSpawnBlocked(TotemEnemySpawnBlockReason reason, string enemyId)
    {
        _lastEnemyId = enemyId ?? string.Empty;
        _lastSpawnBlockReason = reason.ToString();
        _lastReason = _lastSpawnBlockReason;
    }

    private void RecordSummonBlocked(TotemEnemySpawnBlockReason reason, string enemyId)
    {
        _blockedSummons++;
        RecordSpawnBlocked(reason, enemyId);
        GFTrace.Warning(
            "TotemEnemy",
            "Enemy.SummonBlocked",
            null,
            GFTrace.Data(
                "enemyId", enemyId ?? string.Empty,
                "reason", reason == TotemEnemySpawnBlockReason.EncounterActiveCap ? "Blocked.EncounterActiveCap" : reason.ToString(),
                "worldTime", _worldTime.ToString("F3")));
    }

    private static float FlatSqrDistance(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float z = a.z - b.z;
        return x * x + z * z;
    }

    private static bool ShouldApplyStatus(
        TotemEnemyModel source,
        TotemActorModel target,
        TotemEnemyAbilityRuntimeDefinition definition)
    {
        float chance = Mathf.Clamp01(definition?.statusChance ?? 0f);
        if (chance <= 0f)
        {
            return false;
        }

        if (chance >= 1f)
        {
            return true;
        }

        unchecked
        {
            uint hash = (uint)((source?.CombatantId ?? 0) * 73856093)
                ^ (uint)((target?.ActorId ?? 0) * 19349663);
            string abilityId = definition?.abilityId ?? string.Empty;
            for (int i = 0; i < abilityId.Length; i++) hash = hash * 16777619u ^ abilityId[i];
            float roll = (hash & 0x00FFFFFFu) / 16777215f;
            return roll < chance;
        }
    }

    private struct RuntimeEntry
    {
        public TotemEnemyModel Enemy;
        public TotemEnemyControllerBase Controller;
        public TotemEnemyRuntimeDefinition Definition;
        public string AnchorId;
        public TotemEnemyStatusRuntime Statuses;
        public float MovementMultiplier;
    }
}
