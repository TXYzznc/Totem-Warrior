using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TotemWeaponService : TotemRuntimeServiceBase, ITotemRuntimeTickService
{
    public const string DefaultWeaponId = "knife_basic";
    public const int MaxWeaponLevel = 3;
    public const float PickupInteractRadius = 2.5f;
    public const int PickupDuplicateConvertBaseGold = 50;

    private const int TraitHitBufferCapacity = TotemEnemyService.DefaultEnemyCapacity;

    private readonly Dictionary<int, TotemWeaponState> states = new Dictionary<int, TotemWeaponState>(64);
    private readonly List<TotemWeaponPickupModel> activePickups = new List<TotemWeaponPickupModel>(32);
    private TotemGameFlowService flowService;
    private TotemActorService actorService;
    private TotemEconomyService economyService;
    private TotemAssetService assetService;
    private TotemMapService mapService;
    private TotemStatusService statusService;
    private TotemTattooService tattooService;
    private TotemCombatRelationshipService relationshipService;
    private TotemMatchClockService matchClock;
    private TotemEnemyService enemyService;
    private readonly TotemEnemyModel[] enemyTraitBuffer = new TotemEnemyModel[TotemEnemyService.DefaultEnemyCapacity];
    private readonly int[] traitHitActorIdBuffer = new int[TraitHitBufferCapacity];
    private TotemWeaponDefinition[] runtimeCatalog = Array.Empty<TotemWeaponDefinition>();
    private TotemProjectileDefinition[] runtimeProjectileCatalog = Array.Empty<TotemProjectileDefinition>();
    private TotemWeaponTraitDefinition[] runtimeTraitCatalog = Array.Empty<TotemWeaponTraitDefinition>();
    private TotemWeaponDropDefinition[] runtimeDropCatalog = Array.Empty<TotemWeaponDropDefinition>();
    private int nextPickupInstanceId = 1;
    private int spawnedPickupCount;
    private int pickedPickupCount;
    private string lastPickupWeaponId = string.Empty;
    private int lastPickupActorId;
    private int mapResourcePickupCount;
    private string lastMapResourceAnchorId = string.Empty;
    private int traitEffectAppliedCount;
    private TotemWeaponTraitEffectResult lastTraitEffect = BuildTraitSkipped(null, null, null, "None");

    public override string ServiceName => "Weapon";

    public int TraitEffectAppliedCount => traitEffectAppliedCount;

    public TotemWeaponTraitEffectResult LastTraitEffect => lastTraitEffect;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        actorService = runtime.GetService<TotemActorService>();
        economyService = runtime.GetService<TotemEconomyService>();
        assetService = runtime.GetService<TotemAssetService>();
        mapService = runtime.GetService<TotemMapService>();
        statusService = runtime.GetService<TotemStatusService>();
        tattooService = runtime.GetService<TotemTattooService>();
        relationshipService = runtime.GetService<TotemCombatRelationshipService>();
        matchClock = runtime.GetService<TotemMatchClockService>();
        enemyService = runtime.GetService<TotemEnemyService>();
        var catalog = runtime.GetService<TotemDataService>()?.GameplayCatalog ?? TotemDataService.LoadGameplayCatalogOrDefault();
        runtimeCatalog = NonEmpty(catalog.CreateWeaponDefinitions(), LoadWeaponCatalog());
        runtimeProjectileCatalog = NonEmpty(catalog.CreateProjectileDefinitions(), LoadProjectileCatalog());
        runtimeTraitCatalog = NonEmpty(catalog.CreateWeaponTraitDefinitions(), LoadTraitCatalog());
        runtimeDropCatalog = NonEmpty(catalog.CreateWeaponDropDefinitions(), Array.Empty<TotemWeaponDropDefinition>());
        if (flowService != null)
        {
            flowService.StateChanged += OnFlowStateChanged;
        }

    }

    protected override void OnShutdown()
    {
        if (flowService != null)
        {
            flowService.StateChanged -= OnFlowStateChanged;
            flowService = null;
        }

        DestroyAllPickups();
        actorService = null;
        economyService = null;
        assetService = null;
        mapService = null;
        statusService = null;
        tattooService = null;
        relationshipService = null;
        matchClock = null;
        enemyService = null;
        runtimeCatalog = Array.Empty<TotemWeaponDefinition>();
        runtimeProjectileCatalog = Array.Empty<TotemProjectileDefinition>();
        runtimeTraitCatalog = Array.Empty<TotemWeaponTraitDefinition>();
        runtimeDropCatalog = Array.Empty<TotemWeaponDropDefinition>();
        states.Clear();
        spawnedPickupCount = 0;
        pickedPickupCount = 0;
        lastPickupWeaponId = string.Empty;
        lastPickupActorId = 0;
        mapResourcePickupCount = 0;
        lastMapResourceAnchorId = string.Empty;
        nextPickupInstanceId = 1;
        traitEffectAppliedCount = 0;
        lastTraitEffect = BuildTraitSkipped(null, null, null, "None");
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        foreach (var pair in states)
        {
            var state = pair.Value;
            if (state.CooldownRemaining > 0f)
            {
                state.CooldownRemaining = Mathf.Max(0f, state.CooldownRemaining - deltaTime);
            }
        }
    }

    public static IReadOnlyList<TotemWeaponDefinition> GetCatalog()
    {
        return LoadWeaponCatalog();
    }

    public static bool TryGetDefinition(string weaponId, out TotemWeaponDefinition definition)
    {
        var catalog = LoadWeaponCatalog();
        for (int i = 0; i < catalog.Length; i++)
        {
            if (catalog[i] != null && string.Equals(catalog[i].WeaponId, weaponId, StringComparison.Ordinal))
            {
                definition = catalog[i];
                return true;
            }
        }

        definition = null;
        return false;
    }

    public static bool TryGetProjectileDefinition(string projectileId, out TotemProjectileDefinition definition)
    {
        var catalog = LoadProjectileCatalog();
        for (int i = 0; i < catalog.Length; i++)
        {
            if (catalog[i] != null && string.Equals(catalog[i].ProjectileId, projectileId, StringComparison.Ordinal))
            {
                definition = catalog[i];
                return true;
            }
        }

        definition = null;
        return false;
    }

    public static bool TryGetTraitDefinition(string traitId, out TotemWeaponTraitDefinition definition)
    {
        var catalog = LoadTraitCatalog();
        for (int i = 0; i < catalog.Length; i++)
        {
            if (catalog[i] != null && string.Equals(catalog[i].TraitId, traitId, StringComparison.Ordinal))
            {
                definition = catalog[i];
                return true;
            }
        }

        definition = null;
        return false;
    }

    public IReadOnlyList<TotemWeaponDefinition> GetRuntimeCatalog()
    {
        return runtimeCatalog;
    }

    public IReadOnlyList<TotemProjectileDefinition> GetRuntimeProjectileCatalog()
    {
        return runtimeProjectileCatalog;
    }

    public IReadOnlyList<TotemWeaponTraitDefinition> GetRuntimeTraitCatalog()
    {
        return runtimeTraitCatalog;
    }

    public IReadOnlyList<TotemWeaponDropDefinition> GetRuntimeDropCatalog()
    {
        return runtimeDropCatalog;
    }

    public IReadOnlyList<TotemWeaponPickupModel> ActivePickups => activePickups;

    public TotemWeaponPickupSnapshot CapturePickupSnapshot()
    {
        var snapshot = new TotemWeaponPickupSnapshot
        {
            activePickupCount = activePickups.Count,
            spawnedPickupCount = spawnedPickupCount,
            pickedPickupCount = pickedPickupCount,
            lastPickupWeaponId = lastPickupWeaponId,
            lastPickupActorId = lastPickupActorId,
            mapResourcePickupCount = mapResourcePickupCount,
            lastMapResourceAnchorId = lastMapResourceAnchorId,
        };

        for (int i = 0; i < activePickups.Count; i++)
        {
            var pickup = activePickups[i];
            string visualAssetKey = pickup?.VisualAssetKey ?? string.Empty;
            if (string.IsNullOrWhiteSpace(visualAssetKey))
            {
                continue;
            }

            if (visualAssetKey.StartsWith("primitive.", StringComparison.Ordinal))
            {
                snapshot.visualFallbackPickupCount++;
                snapshot.lastVisualFallbackKey = visualAssetKey;
            }
            else
            {
                snapshot.visualAssetPickupCount++;
                snapshot.lastVisualAssetKey = visualAssetKey;
            }
        }

        return snapshot;
    }

    public bool TryGetRuntimeDefinition(string weaponId, out TotemWeaponDefinition definition)
    {
        var catalog = runtimeCatalog == null || runtimeCatalog.Length <= 0 ? LoadWeaponCatalog() : runtimeCatalog;
        for (int i = 0; i < catalog.Length; i++)
        {
            if (catalog[i] != null && string.Equals(catalog[i].WeaponId, weaponId, StringComparison.Ordinal))
            {
                definition = catalog[i];
                return true;
            }
        }

        definition = null;
        return false;
    }

    public bool TryGetRuntimeProjectileDefinition(string projectileId, out TotemProjectileDefinition definition)
    {
        var catalog = runtimeProjectileCatalog == null || runtimeProjectileCatalog.Length <= 0 ? LoadProjectileCatalog() : runtimeProjectileCatalog;
        for (int i = 0; i < catalog.Length; i++)
        {
            if (catalog[i] != null && string.Equals(catalog[i].ProjectileId, projectileId, StringComparison.Ordinal))
            {
                definition = catalog[i];
                return true;
            }
        }

        definition = null;
        return false;
    }

    public bool TryGetRuntimeTraitDefinition(string traitId, out TotemWeaponTraitDefinition definition)
    {
        var catalog = runtimeTraitCatalog == null || runtimeTraitCatalog.Length <= 0 ? LoadTraitCatalog() : runtimeTraitCatalog;
        for (int i = 0; i < catalog.Length; i++)
        {
            if (catalog[i] != null && string.Equals(catalog[i].TraitId, traitId, StringComparison.Ordinal))
            {
                definition = catalog[i];
                return true;
            }
        }

        definition = null;
        return false;
    }

    public void EquipWeapon(TotemActorModel actor, string weaponId)
    {
        if (actor == null)
        {
            return;
        }

        if (!TryGetRuntimeDefinition(weaponId, out var definition))
        {
            TryGetRuntimeDefinition(DefaultWeaponId, out definition);
        }

        states[actor.ActorId] = new TotemWeaponState
        {
            Weapon = definition,
            CurrentAmmo = definition.MaxAmmo,
            Level = 1,
            CooldownRemaining = 0f,
        };

        GFTrace.Info("TotemWeapon", "Equip", null, GFTrace.Data(
            "actor", actor.Name,
            "weaponId", definition.WeaponId));
    }

    public TotemWeaponState GetOrCreateState(TotemActorModel actor)
    {
        if (actor == null)
        {
            return null;
        }

        if (!states.TryGetValue(actor.ActorId, out var state))
        {
            EquipWeapon(actor, DefaultWeaponId);
            state = states[actor.ActorId];
        }

        return state;
    }

    public string GetEquippedWeaponId(TotemActorModel actor)
    {
        return GetOrCreateState(actor)?.Weapon?.WeaponId ?? string.Empty;
    }

    public int GetWeaponLevel(TotemActorModel actor)
    {
        return GetOrCreateState(actor)?.Level ?? 0;
    }

    public TotemWeaponFireResult FireWeapon(TotemActorModel actor, TotemCombatantModel target, bool isCharged, float chargeRatio)
    {
        var state = GetOrCreateState(actor);
        if (state == null || state.Weapon == null)
        {
            return Skipped("NoWeapon");
        }

        if (state.CooldownRemaining > 0f)
        {
            return Skipped("Cooldown");
        }

        var weapon = state.Weapon;
        if (weapon.RequiresCharge && !isCharged)
        {
            return Skipped("RequiresCharge");
        }

        if (weapon.Class == TotemWeaponClass.Ranged && target == null)
        {
            return Skipped("NoTarget");
        }

        bool ammoExhausted = weapon.MaxAmmo > 0 && state.CurrentAmmo <= 0;
        if (weapon.MaxAmmo > 0 && state.CurrentAmmo > 0)
        {
            state.CurrentAmmo--;
        }

        var multipliers = GetMultipliers(state.Level);
        float damage = weapon.BaseDamage * multipliers.DamageMul;
        if (isCharged)
        {
            damage *= weapon.ChargedMultiplier <= 0f ? 1.5f : weapon.ChargedMultiplier;
        }

        if (ammoExhausted)
        {
            damage *= 0.4f;
        }

        TotemProjectileDefinition projectile = null;
        if (!string.IsNullOrWhiteSpace(weapon.ProjectileId))
        {
            TryGetRuntimeProjectileDefinition(weapon.ProjectileId, out projectile);
        }

        TotemWeaponTraitDefinition activeTrait = null;
        string activeTraitId = isCharged ? weapon.ChargedTraitId : weapon.NormalTraitId;
        if (!string.IsNullOrWhiteSpace(activeTraitId))
        {
            TryGetRuntimeTraitDefinition(activeTraitId, out activeTrait);
        }

        float enchantCooldownMul = tattooService == null ? 1f : tattooService.ResolveWeaponCooldownMultiplier(actor);
        float enchantRangeMul = tattooService == null ? 1f : tattooService.ResolveRangeMultiplier(actor);
        state.CooldownRemaining = weapon.Cooldown * multipliers.CooldownMul * enchantCooldownMul;
        unchecked
        {
            state.FireSequence++;
        }

        return new TotemWeaponFireResult
        {
            Fired = true,
            Reason = ammoExhausted ? "AmmoExhaustedDegraded" : "Fired",
            Weapon = weapon,
            Projectile = projectile,
            ActiveTrait = activeTrait,
            Damage = damage,
            Range = (weapon.Range + multipliers.RangeAdd) * enchantRangeMul,
            IsCharged = isCharged,
            FireSequence = state.FireSequence,
        };
    }

    public TotemWeaponTraitEffectResult ApplyTraitEffect(TotemWeaponFireResult fireResult, TotemActorModel source, TotemActorModel target, bool targetKilled)
    {
        var trait = fireResult?.ActiveTrait;
        if (fireResult == null || !fireResult.Fired)
        {
            return RecordTraitEffect(BuildTraitSkipped(trait, source, target, "NoFiredWeapon"));
        }

        if (trait == null || trait.EffectType == TotemWeaponTraitEffectType.Unknown)
        {
            return RecordTraitEffect(BuildTraitSkipped(trait, source, target, "NoTrait"));
        }

        if (target == null || targetKilled || !target.IsAlive)
        {
            return RecordTraitEffect(BuildTraitSkipped(trait, source, target, "TargetUnavailable"));
        }

        switch (trait.EffectType)
        {
            case TotemWeaponTraitEffectType.Status:
                return ApplyStatusTrait(trait, fireResult, source, target, DeriveStatusName(trait), trait.EffectParam1, trait.EffectParam2);
            case TotemWeaponTraitEffectType.Stun:
                return ApplyStatusTrait(trait, fireResult, source, target, "Stun", 0f, trait.EffectParam1);
            case TotemWeaponTraitEffectType.Quick:
                return ApplyQuickTrait(trait, fireResult, source, target);
            case TotemWeaponTraitEffectType.Pierce:
                return ApplyPierceTrait(trait, fireResult, source, target);
            case TotemWeaponTraitEffectType.Chain:
                return ApplyChainTrait(trait, fireResult, source, target);
            case TotemWeaponTraitEffectType.Explosive:
                return ApplyExplosiveTrait(trait, fireResult, source, target);
            case TotemWeaponTraitEffectType.MultiShot:
                return ApplyMultiShotTrait(trait, fireResult, source, target);
            case TotemWeaponTraitEffectType.Pull:
                return ApplyPullTrait(trait, fireResult, source, target);
            default:
                return RecordTraitEffect(BuildTraitSkipped(trait, source, target, $"Unsupported:{trait.EffectType}"));
        }
    }

    public TotemWeaponTraitEffectResult ApplyTraitEffect(
        TotemWeaponFireResult fireResult,
        TotemActorModel source,
        TotemEnemyModel target,
        bool targetKilled)
    {
        TotemWeaponTraitDefinition trait = fireResult?.ActiveTrait;
        var result = new TotemWeaponTraitEffectResult
        {
            traitId = trait?.TraitId ?? string.Empty,
            effectType = trait?.EffectType.ToString() ?? string.Empty,
            sourceActorId = source?.ActorId ?? 0,
            targetActorId = target?.CombatantId ?? 0,
        };
        if (fireResult == null || !fireResult.Fired || trait == null || target == null || targetKilled || !target.IsAlive || enemyService == null)
        {
            result.reason = "TargetUnavailable";
            return RecordTraitEffect(result);
        }

        float worldTime = matchClock?.WorldTime ?? enemyService.WorldTime;
        switch (trait.EffectType)
        {
            case TotemWeaponTraitEffectType.Quick:
                if (string.Equals(trait.TraitId, "trait_lifesteal", StringComparison.Ordinal))
                {
                    float heal = source?.Heal(Mathf.Min(
                        trait.EffectParam2 <= 0f ? float.MaxValue : trait.EffectParam2,
                        Mathf.Max(0f, fireResult.Damage) * Mathf.Clamp01(trait.EffectParam1))) ?? 0f;
                    result.applied = heal > 0f;
                    result.reason = result.applied ? "Applied" : "NoMissingHealth";
                    result.sourceHeal = heal;
                    return RecordTraitEffect(result);
                }

                if (source != null && states.TryGetValue(source.ActorId, out TotemWeaponState state))
                {
                    float before = state.CooldownRemaining;
                    state.CooldownRemaining *= 1f - Mathf.Clamp(trait.EffectParam1, 0f, 0.95f);
                    result.applied = state.CooldownRemaining < before;
                    result.reason = result.applied ? "Applied" : "NoCooldown";
                    result.cooldownRemaining = state.CooldownRemaining;
                }
                return RecordTraitEffect(result);

            case TotemWeaponTraitEffectType.Status:
            case TotemWeaponTraitEffectType.Stun:
                float chanceBonus = tattooService?.ResolveStatusChanceBonus(source) ?? 0f;
                float chance = TotemTattooService.ComputeStatusApplyChance(TotemTattooService.DefaultStatusApplyChance, chanceBonus);
                float roll = ResolveDeterministicStatusRoll(
                    trait.TraitId,
                    source?.ActorId ?? 0,
                    target.CombatantId,
                    fireResult.FireSequence,
                    chance);
                string statusName = trait.EffectType == TotemWeaponTraitEffectType.Stun ? TotemStatusService.StunStatus : DeriveStatusName(trait);
                float statusPower = trait.EffectType == TotemWeaponTraitEffectType.Stun ? 0f : Mathf.Max(0f, trait.EffectParam1);
                float statusDuration = trait.EffectType == TotemWeaponTraitEffectType.Stun
                    ? (trait.EffectParam1 > 0f ? trait.EffectParam1 : 2f)
                    : (trait.EffectParam2 > 0f ? trait.EffectParam2 : 2f);
                bool chancePassed = TotemTattooService.ShouldApplyStatus(chance, roll);
                TotemEnemyStatusApplyResult statusResult = TotemEnemyStatusApplyResult.InvalidDefinition;
                bool applies = chancePassed && enemyService.TryApplyStatus(
                    target.CombatantId,
                    source,
                    statusName,
                    statusPower,
                    statusDuration,
                    "WeaponTrait:" + trait.TraitId,
                    worldTime,
                    out statusResult);
                result.applied = applies;
                result.reason = applies ? statusResult.ToString() : chancePassed ? statusResult.ToString() : "StatusChanceMiss";
                result.statusName = statusName;
                result.statusDps = statusPower;
                result.statusDuration = statusDuration;
                result.statusApplied = applies;
                result.statusChance = chance;
                result.statusChanceBonus = chanceBonus;
                result.statusRoll = roll;
                return RecordTraitEffect(result);

            case TotemWeaponTraitEffectType.Pull:
                if (source == null)
                {
                    result.reason = "InvalidPullContext";
                    return RecordTraitEffect(result);
                }
                Vector3 pullDelta = source.Position - target.Position;
                pullDelta.y = 0f;
                float distance = pullDelta.magnitude;
                float maxDistance = trait.EffectParam2 <= 0f ? float.MaxValue : trait.EffectParam2;
                float pullDistance = distance <= maxDistance
                    ? Mathf.Min(Mathf.Max(0f, trait.EffectParam1), Mathf.Max(0f, distance - 0.75f))
                    : 0f;
                result.applied = pullDistance > 0f
                    && enemyService.TryDisplaceEnemy(target.CombatantId, pullDelta.normalized * pullDistance);
                result.reason = result.applied ? "Applied" : "PullBlocked";
                result.displacement = result.applied ? pullDistance : 0f;
                return RecordTraitEffect(result);

            case TotemWeaponTraitEffectType.Pierce:
            case TotemWeaponTraitEffectType.Chain:
            case TotemWeaponTraitEffectType.Explosive:
            case TotemWeaponTraitEffectType.MultiShot:
                int maxHits = trait.EffectType == TotemWeaponTraitEffectType.Explosive
                    ? int.MaxValue
                    : Mathf.Max(0, Mathf.RoundToInt(trait.EffectParam1));
                float radius = trait.EffectType == TotemWeaponTraitEffectType.Explosive
                    ? Mathf.Max(0.1f, trait.EffectParam1)
                    : Mathf.Max(3f, fireResult.Range * (trait.EffectType == TotemWeaponTraitEffectType.Chain ? 0.5f : 1f));
                float radiusSqr = radius * radius;
                int count = enemyService.CopyAliveEnemies(enemyTraitBuffer);
                int hitCount = 0;
                float totalDamage = 0f;
                for (int i = 0; i < count && hitCount < maxHits; i++)
                {
                    TotemEnemyModel candidate = enemyTraitBuffer[i];
                    if (candidate == null
                        || candidate == target
                        || (candidate.Position - target.Position).sqrMagnitude > radiusSqr)
                    {
                        continue;
                    }

                    float multiplier = trait.EffectType == TotemWeaponTraitEffectType.Explosive
                        ? (trait.EffectParam2 <= 0f ? 0.5f : trait.EffectParam2)
                        : Mathf.Clamp01(1f - Mathf.Max(0f, trait.EffectParam2) * (hitCount + 1));
                    float damage = Mathf.Max(1f, fireResult.Damage * multiplier);
                    if (enemyService.TryApplyDamage(
                            candidate.CombatantId,
                            source,
                            damage,
                            "WeaponTrait:" + trait.TraitId,
                            worldTime,
                            out float appliedDamage))
                    {
                        hitCount++;
                        totalDamage += appliedDamage;
                    }
                }

                result.applied = hitCount > 0;
                result.reason = result.applied ? "Applied" : "NoSecondaryEnemy";
                result.secondaryHitCount = hitCount;
                result.secondaryDamage = totalDamage;
                result.extraProjectileCount = trait.EffectType == TotemWeaponTraitEffectType.MultiShot ? maxHits : 0;
                result.effectRadius = radius;
                return RecordTraitEffect(result);

            default:
                result.reason = "Unsupported:" + trait.EffectType;
                return RecordTraitEffect(result);
        }
    }

    private TotemWeaponTraitEffectResult ApplyPierceTrait(
        TotemWeaponTraitDefinition trait,
        TotemWeaponFireResult fireResult,
        TotemActorModel source,
        TotemActorModel target)
    {
        int maxExtraHits = Mathf.Min(traitHitActorIdBuffer.Length, Mathf.Max(0, Mathf.RoundToInt(trait.EffectParam1)));
        if (maxExtraHits <= 0)
        {
            return RecordTraitEffect(BuildTraitSkipped(trait, source, target, "NoPierceCount"));
        }

        if (actorService?.Actors == null)
        {
            return RecordTraitEffect(BuildTraitSkipped(trait, source, target, "ActorServiceMissing"));
        }

        float maxRadius = ResolveTraitSearchRadius(fireResult, source, target);
        float falloff = Mathf.Clamp01(trait.EffectParam2);
        int hitCount = 0;
        float totalDamage = 0f;
        for (int i = 0; i < maxExtraHits; i++)
        {
            var candidate = FindNearestTraitTarget(target.Position, source, target, traitHitActorIdBuffer, hitCount, maxRadius);
            if (candidate == null)
            {
                break;
            }

            traitHitActorIdBuffer[hitCount++] = candidate.ActorId;
            float damage = Mathf.Max(1f, fireResult.Damage * (1f - falloff * hitCount));
            totalDamage += damage;
            actorService.ApplyDamage(candidate, damage, source, $"WeaponTrait:{trait.TraitId}");
        }

        var result = BuildTraitBase(trait, source, target, hitCount > 0 ? "Applied" : "NoSecondaryTarget", hitCount > 0);
        result.secondaryHitCount = hitCount;
        result.secondaryDamage = totalDamage;
        result.effectRadius = maxRadius;
        return RecordTraitEffect(result);
    }

    private TotemWeaponTraitEffectResult ApplyChainTrait(
        TotemWeaponTraitDefinition trait,
        TotemWeaponFireResult fireResult,
        TotemActorModel source,
        TotemActorModel target)
    {
        int maxJumps = Mathf.Min(traitHitActorIdBuffer.Length, Mathf.Max(0, Mathf.RoundToInt(trait.EffectParam1)));
        if (maxJumps <= 0)
        {
            return RecordTraitEffect(BuildTraitSkipped(trait, source, target, "NoChainCount"));
        }

        if (actorService?.Actors == null)
        {
            return RecordTraitEffect(BuildTraitSkipped(trait, source, target, "ActorServiceMissing"));
        }

        float jumpRadius = Mathf.Max(3f, ResolveTraitSearchRadius(fireResult, source, target) * 0.5f);
        float falloff = Mathf.Clamp01(trait.EffectParam2);
        int hitCount = 0;
        float totalDamage = 0f;
        var jumpOrigin = target;
        for (int i = 0; i < maxJumps; i++)
        {
            var candidate = FindNearestTraitTarget(jumpOrigin.Position, source, target, traitHitActorIdBuffer, hitCount, jumpRadius);
            if (candidate == null)
            {
                break;
            }

            traitHitActorIdBuffer[hitCount++] = candidate.ActorId;
            float damage = Mathf.Max(1f, fireResult.Damage * (1f - falloff * hitCount));
            totalDamage += damage;
            actorService.ApplyDamage(candidate, damage, source, $"WeaponTrait:{trait.TraitId}");
            jumpOrigin = candidate;
        }

        var result = BuildTraitBase(trait, source, target, hitCount > 0 ? "Applied" : "NoChainTarget", hitCount > 0);
        result.secondaryHitCount = hitCount;
        result.secondaryDamage = totalDamage;
        result.effectRadius = jumpRadius;
        return RecordTraitEffect(result);
    }

    private TotemWeaponTraitEffectResult ApplyExplosiveTrait(
        TotemWeaponTraitDefinition trait,
        TotemWeaponFireResult fireResult,
        TotemActorModel source,
        TotemActorModel target)
    {
        if (actorService?.Actors == null)
        {
            return RecordTraitEffect(BuildTraitSkipped(trait, source, target, "ActorServiceMissing"));
        }

        float radius = Mathf.Max(0.1f, trait.EffectParam1);
        float damageMul = trait.EffectParam2 <= 0f ? 0.5f : trait.EffectParam2;
        float radiusSqr = radius * radius;
        int hitCount = 0;
        float totalDamage = 0f;
        var actors = actorService.Actors;
        for (int i = 0; i < actors.Count; i++)
        {
            var candidate = actors[i];
            if (!IsValidTraitTarget(candidate, source, target))
            {
                continue;
            }

            if ((candidate.Position - target.Position).sqrMagnitude > radiusSqr)
            {
                continue;
            }

            float damage = Mathf.Max(1f, fireResult.Damage * damageMul);
            totalDamage += damage;
            hitCount++;
            actorService.ApplyDamage(candidate, damage, source, $"WeaponTrait:{trait.TraitId}");
        }

        var result = BuildTraitBase(trait, source, target, hitCount > 0 ? "Applied" : "NoExplosionTarget", hitCount > 0);
        result.secondaryHitCount = hitCount;
        result.secondaryDamage = totalDamage;
        result.effectRadius = radius;
        return RecordTraitEffect(result);
    }

    private TotemWeaponTraitEffectResult ApplyMultiShotTrait(
        TotemWeaponTraitDefinition trait,
        TotemWeaponFireResult fireResult,
        TotemActorModel source,
        TotemActorModel target)
    {
        int projectileCount = Mathf.Max(1, Mathf.RoundToInt(trait.EffectParam1));
        int extraProjectiles = Mathf.Max(0, projectileCount - 1);
        if (extraProjectiles <= 0)
        {
            return RecordTraitEffect(BuildTraitSkipped(trait, source, target, "NoExtraProjectile"));
        }

        if (actorService?.Actors == null)
        {
            return RecordTraitEffect(BuildTraitSkipped(trait, source, target, "ActorServiceMissing"));
        }

        float maxRadius = ResolveTraitSearchRadius(fireResult, source, target);
        int maxSecondaryHits = Mathf.Min(extraProjectiles, traitHitActorIdBuffer.Length);
        int hitCount = 0;
        float totalDamage = 0f;
        for (int i = 0; i < maxSecondaryHits; i++)
        {
            var candidate = FindNearestTraitTarget(target.Position, source, target, traitHitActorIdBuffer, hitCount, maxRadius);
            if (candidate == null)
            {
                break;
            }

            traitHitActorIdBuffer[hitCount++] = candidate.ActorId;
            float damage = Mathf.Max(1f, fireResult.Damage);
            totalDamage += damage;
            actorService.ApplyDamage(candidate, damage, source, $"WeaponTrait:{trait.TraitId}");
        }

        var result = BuildTraitBase(trait, source, target, hitCount > 0 ? "Applied" : "NoFanTarget", hitCount > 0);
        result.secondaryHitCount = hitCount;
        result.secondaryDamage = totalDamage;
        result.extraProjectileCount = extraProjectiles;
        result.effectRadius = maxRadius;
        return RecordTraitEffect(result);
    }

    private TotemWeaponTraitEffectResult ApplyPullTrait(
        TotemWeaponTraitDefinition trait,
        TotemWeaponFireResult fireResult,
        TotemActorModel source,
        TotemActorModel target)
    {
        if (source == null || target == null)
        {
            return RecordTraitEffect(BuildTraitSkipped(trait, source, target, "InvalidPullContext"));
        }

        Vector3 delta = source.Position - target.Position;
        delta.y = 0f;
        float distance = delta.magnitude;
        if (distance <= 0.001f)
        {
            return RecordTraitEffect(BuildTraitSkipped(trait, source, target, "AlreadyAtSource"));
        }

        float maxDistance = trait.EffectParam2 <= 0f ? float.MaxValue : trait.EffectParam2;
        if (distance > maxDistance)
        {
            return RecordTraitEffect(BuildTraitSkipped(trait, source, target, "OutOfPullRange"));
        }

        float pullDistance = Mathf.Min(Mathf.Max(0f, trait.EffectParam1), Mathf.Max(0f, distance - 0.75f));
        if (pullDistance <= 0f)
        {
            return RecordTraitEffect(BuildTraitSkipped(trait, source, target, "NoPullDistance"));
        }

        Vector3 move = delta.normalized * pullDistance;
        if (actorService != null)
        {
            actorService.MoveActor(target, move);
        }
        else
        {
            target.Position += move;
            if (target.GameObject != null)
            {
                target.GameObject.transform.position = target.Position;
            }
        }

        var result = BuildTraitBase(trait, source, target, "Applied", true);
        result.displacement = pullDistance;
        result.effectRadius = maxDistance < float.MaxValue ? maxDistance : 0f;
        return RecordTraitEffect(result);
    }

    private TotemWeaponTraitEffectResult ApplyQuickTrait(
        TotemWeaponTraitDefinition trait,
        TotemWeaponFireResult fireResult,
        TotemActorModel source,
        TotemActorModel target)
    {
        if (trait == null || source == null)
        {
            return RecordTraitEffect(BuildTraitSkipped(trait, source, target, "InvalidQuickContext"));
        }

        if (string.Equals(trait.TraitId, "trait_lifesteal", StringComparison.Ordinal))
        {
            float ratio = Mathf.Clamp01(trait.EffectParam1);
            float cap = trait.EffectParam2 <= 0f ? float.MaxValue : trait.EffectParam2;
            float healAmount = Mathf.Min(cap, Mathf.Max(0f, fireResult?.Damage ?? 0f) * ratio);
            float healed = source.Heal(healAmount);
            var result = BuildTraitBase(trait, source, target, healed > 0f ? "Applied" : "NoMissingHealth", healed > 0f);
            result.sourceHeal = healed;
            return RecordTraitEffect(result);
        }

        if (!states.TryGetValue(source.ActorId, out var state) || state == null)
        {
            return RecordTraitEffect(BuildTraitSkipped(trait, source, target, "WeaponStateMissing"));
        }

        float reduction = Mathf.Clamp(trait.EffectParam1, 0f, 0.95f);
        float before = state.CooldownRemaining;
        state.CooldownRemaining = Mathf.Max(0f, before * (1f - reduction));
        var cooldownResult = BuildTraitBase(trait, source, target, before > state.CooldownRemaining ? "Applied" : "NoCooldown", before > state.CooldownRemaining);
        cooldownResult.cooldownRemaining = state.CooldownRemaining;
        return RecordTraitEffect(cooldownResult);
    }

    private TotemWeaponTraitEffectResult ApplyStatusTrait(
        TotemWeaponTraitDefinition trait,
        TotemWeaponFireResult fireResult,
        TotemActorModel source,
        TotemActorModel target,
        string statusName,
        float dps,
        float duration)
    {
        if (statusService == null)
        {
            return RecordTraitEffect(BuildTraitSkipped(trait, source, target, "StatusServiceMissing"));
        }

        string resolvedStatus = string.IsNullOrWhiteSpace(statusName) ? "TraitStatus" : statusName;
        float resolvedDuration = duration > 0f ? duration : 2f;
        float resolvedDps = Mathf.Max(0f, dps);
        float statusChanceBonus = tattooService?.ResolveStatusChanceBonus(source) ?? 0f;
        float statusChance = TotemTattooService.ComputeStatusApplyChance(TotemTattooService.DefaultStatusApplyChance, statusChanceBonus);
        float statusRoll = ResolveDeterministicStatusRoll(
            trait?.TraitId,
            source?.ActorId ?? 0,
            target?.ActorId ?? 0,
            fireResult?.FireSequence ?? 0u,
            statusChance);
        if (!TotemTattooService.ShouldApplyStatus(statusChance, statusRoll))
        {
            return RecordTraitEffect(new TotemWeaponTraitEffectResult
            {
                applied = false,
                reason = "StatusChanceMiss",
                traitId = trait?.TraitId ?? string.Empty,
                effectType = trait?.EffectType.ToString() ?? string.Empty,
                statusName = resolvedStatus,
                statusDps = resolvedDps,
                statusDuration = resolvedDuration,
                statusApplied = false,
                statusChance = statusChance,
                statusChanceBonus = statusChanceBonus,
                statusRoll = statusRoll,
                sourceActorId = source?.ActorId ?? 0,
                targetActorId = target?.ActorId ?? 0,
            });
        }

        statusService.ApplyStatus(target, resolvedStatus, resolvedDps, resolvedDuration, source, $"WeaponTrait:{trait.TraitId}");
        return RecordTraitEffect(new TotemWeaponTraitEffectResult
        {
            applied = true,
            reason = "Applied",
            traitId = trait.TraitId,
            effectType = trait.EffectType.ToString(),
            statusName = resolvedStatus,
            statusDps = resolvedDps,
            statusDuration = resolvedDuration,
            statusApplied = true,
            statusChance = statusChance,
            statusChanceBonus = statusChanceBonus,
            statusRoll = statusRoll,
            sourceActorId = source?.ActorId ?? 0,
            targetActorId = target?.ActorId ?? 0,
        });
    }

    public static float ResolveDeterministicStatusRoll(
        string traitId,
        int sourceCombatantId,
        int targetCombatantId,
        uint fireSequence,
        float statusChance)
    {
        if (statusChance >= 1f)
        {
            return 0f;
        }

        unchecked
        {
            uint hash = 2166136261u;
            hash = (hash ^ (uint)StableHash(traitId)) * 16777619u;
            hash = (hash ^ (uint)sourceCombatantId) * 16777619u;
            hash = (hash ^ (uint)targetCombatantId) * 16777619u;
            hash = (hash ^ fireSequence) * 16777619u;
            hash ^= hash >> 16;
            hash *= 0x7feb352du;
            hash ^= hash >> 15;
            hash *= 0x846ca68bu;
            hash ^= hash >> 16;
            return (hash & 0x00ffffffu) * (1f / 16777216f);
        }
    }

    private static int StableHash(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        unchecked
        {
            int hash = 23;
            for (int i = 0; i < value.Length; i++)
            {
                hash = hash * 31 + value[i];
            }

            return hash & 0x7fffffff;
        }
    }

    private static TotemWeaponTraitEffectResult BuildTraitBase(
        TotemWeaponTraitDefinition trait,
        TotemActorModel source,
        TotemActorModel target,
        string reason,
        bool applied)
    {
        return new TotemWeaponTraitEffectResult
        {
            applied = applied,
            reason = reason ?? string.Empty,
            traitId = trait?.TraitId ?? string.Empty,
            effectType = trait?.EffectType.ToString() ?? string.Empty,
            sourceActorId = source?.ActorId ?? 0,
            targetActorId = target?.ActorId ?? 0,
        };
    }

    private TotemWeaponTraitEffectResult RecordTraitEffect(TotemWeaponTraitEffectResult result)
    {
        lastTraitEffect = result ?? BuildTraitSkipped(null, null, null, "NullResult");
        if (lastTraitEffect.applied)
        {
            traitEffectAppliedCount++;
            GFTrace.Info("TotemWeapon", "TraitEffect.Applied", null, GFTrace.Data(
                "traitId", lastTraitEffect.traitId ?? string.Empty,
                "effectType", lastTraitEffect.effectType ?? string.Empty,
                "status", lastTraitEffect.statusName ?? string.Empty,
                "secondaryHits", lastTraitEffect.secondaryHitCount.ToString(),
                "secondaryDamage", lastTraitEffect.secondaryDamage.ToString("F1"),
                "displacement", lastTraitEffect.displacement.ToString("F2"),
                "targetActorId", lastTraitEffect.targetActorId.ToString()));
        }
        else
        {
            GFTrace.Info("TotemWeapon", "TraitEffect.Skipped", null, GFTrace.Data(
                "traitId", lastTraitEffect.traitId ?? string.Empty,
                "reason", lastTraitEffect.reason ?? string.Empty));
        }

        return lastTraitEffect;
    }

    private static TotemWeaponTraitEffectResult BuildTraitSkipped(
        TotemWeaponTraitDefinition trait,
        TotemActorModel source,
        TotemActorModel target,
        string reason)
    {
        return new TotemWeaponTraitEffectResult
        {
            applied = false,
            reason = string.IsNullOrWhiteSpace(reason) ? "Skipped" : reason,
            traitId = trait?.TraitId ?? string.Empty,
            effectType = trait?.EffectType.ToString() ?? string.Empty,
            statusName = string.Empty,
            statusDps = 0f,
            statusDuration = 0f,
            sourceActorId = source?.ActorId ?? 0,
            targetActorId = target?.ActorId ?? 0,
        };
    }

    private TotemActorModel FindNearestTraitTarget(
        Vector3 origin,
        TotemActorModel source,
        TotemActorModel primaryTarget,
        int[] excludedActorIds,
        int excludedCount,
        float maxRadius)
    {
        var actors = actorService?.Actors;
        if (actors == null)
        {
            return null;
        }

        float maxSqr = maxRadius <= 0f ? float.MaxValue : maxRadius * maxRadius;
        float bestSqr = float.MaxValue;
        TotemActorModel best = null;
        for (int i = 0; i < actors.Count; i++)
        {
            var candidate = actors[i];
            if (!IsValidTraitTarget(candidate, source, primaryTarget))
            {
                continue;
            }

            if (IsExcluded(candidate.ActorId, excludedActorIds, excludedCount))
            {
                continue;
            }

            float sqr = (candidate.Position - origin).sqrMagnitude;
            if (sqr > maxSqr || sqr >= bestSqr)
            {
                continue;
            }

            bestSqr = sqr;
            best = candidate;
        }

        return best;
    }

    private bool IsValidTraitTarget(TotemActorModel candidate, TotemActorModel source, TotemActorModel primaryTarget)
    {
        if (candidate == null || !candidate.IsAlive || ReferenceEquals(candidate, source) || ReferenceEquals(candidate, primaryTarget))
        {
            return false;
        }

        if (source == null || relationshipService == null)
        {
            return true;
        }

        return relationshipService.EvaluateDamage(
            source,
            candidate,
            new TotemCombatRelationshipContext(matchClock?.WorldTime ?? 0f)).Allowed;
    }

    private static bool IsExcluded(int actorId, int[] excludedActorIds, int excludedCount)
    {
        if (excludedActorIds == null || excludedCount <= 0)
        {
            return false;
        }

        for (int i = 0; i < excludedCount && i < excludedActorIds.Length; i++)
        {
            if (excludedActorIds[i] == actorId)
            {
                return true;
            }
        }

        return false;
    }

    private static float ResolveTraitSearchRadius(TotemWeaponFireResult fireResult, TotemActorModel source, TotemActorModel target)
    {
        float radius = fireResult?.Range ?? 0f;
        if (radius <= 0f && source != null && target != null)
        {
            radius = Vector3.Distance(source.Position, target.Position) + 6f;
        }

        return Mathf.Max(3f, radius);
    }

    private static string DeriveStatusName(TotemWeaponTraitDefinition trait)
    {
        string traitId = trait?.TraitId ?? string.Empty;
        if (traitId.IndexOf("burn", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Burn";
        }

        if (traitId.IndexOf("poison", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Poison";
        }

        string displayName = trait?.DisplayName ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName.Replace(" ", string.Empty);
        }

        return "TraitStatus";
    }

    public bool TryUpgrade(TotemActorModel actor, string weaponId, int baseGoldCost, out int convertedGold)
    {
        convertedGold = 0;
        var state = GetOrCreateState(actor);
        if (state == null)
        {
            return false;
        }

        if (!string.Equals(state.Weapon?.WeaponId, weaponId, StringComparison.Ordinal))
        {
            EquipWeapon(actor, weaponId);
            state = GetOrCreateState(actor);
        }

        if (state.Level >= MaxWeaponLevel)
        {
            convertedGold = ComputeConvertGold(baseGoldCost);
            return false;
        }

        state.Level++;
        return true;
    }

    public bool TryUpgradeEquipped(TotemActorModel actor, int baseGoldCost, out int convertedGold)
    {
        convertedGold = 0;
        var state = GetOrCreateState(actor);
        if (state?.Weapon == null)
        {
            return false;
        }

        return TryUpgrade(actor, state.Weapon.WeaponId, baseGoldCost, out convertedGold);
    }

    public bool TrySelectWeaponDrop(string source, int roomIndex, int seed, out string weaponId)
    {
        weaponId = string.Empty;
        int totalWeight = 0;
        for (int i = 0; i < runtimeDropCatalog.Length; i++)
        {
            var drop = runtimeDropCatalog[i];
            if (!IsDropCandidate(drop, source, roomIndex))
            {
                continue;
            }

            totalWeight += Mathf.Max(0, drop.Weight);
        }

        if (totalWeight <= 0)
        {
            return false;
        }

        int roll = (int)(Math.Abs((long)seed) % totalWeight);
        int cursor = 0;
        for (int i = 0; i < runtimeDropCatalog.Length; i++)
        {
            var drop = runtimeDropCatalog[i];
            if (!IsDropCandidate(drop, source, roomIndex))
            {
                continue;
            }

            cursor += Mathf.Max(0, drop.Weight);
            if (roll < cursor)
            {
                weaponId = drop.WeaponId;
                return true;
            }
        }

        return false;
    }

    public bool SpawnWeightedWeaponPickup(string source, int roomIndex, Vector3 position, int seed, out TotemWeaponPickupModel pickup)
    {
        pickup = null;
        if (!TrySelectWeaponDrop(source, roomIndex, seed, out string weaponId))
        {
            return false;
        }

        pickup = SpawnWeaponPickup(weaponId, source, position);
        return pickup != null;
    }

    public int SpawnMapResourcePickups(TotemMapSnapshot map, int seed)
    {
        var anchors = TotemMapService.FindAnchors(map, TotemMapAnchorKind.Resource);
        int spawned = 0;
        for (int i = 0; i < anchors.Length; i++)
        {
            var anchor = anchors[i];
            if (anchor == null)
            {
                continue;
            }

            string weaponId = string.IsNullOrWhiteSpace(anchor.PayloadId)
                ? ResolveFallbackResourceWeaponId(seed + i)
                : anchor.PayloadId;
            var pickup = SpawnWeaponPickup(weaponId, "MapResource", anchor.Position);
            if (pickup == null)
            {
                continue;
            }

            spawned++;
            mapResourcePickupCount++;
            lastMapResourceAnchorId = anchor.AnchorId ?? string.Empty;
        }

        if (spawned > 0)
        {
            GFTrace.Success("TotemWeapon", "Pickup.MapResourcesSpawned", null, GFTrace.Data(
                "count", spawned.ToString(),
                "lastAnchor", lastMapResourceAnchorId));
        }

        return spawned;
    }

    public TotemWeaponPickupModel SpawnWeaponPickup(string weaponId, string source, Vector3 position)
    {
        if (!TryGetRuntimeDefinition(weaponId, out _))
        {
            return null;
        }

        var pickup = new TotemWeaponPickupModel
        {
            InstanceId = nextPickupInstanceId++,
            WeaponId = weaponId,
            Source = string.IsNullOrWhiteSpace(source) ? "Manual" : source,
            Position = position,
        };
        pickup.GameObject = CreatePickupObject(pickup);
        activePickups.Add(pickup);
        spawnedPickupCount++;
        GFTrace.Success("TotemWeapon", "Pickup.Spawned", null, GFTrace.Data(
            "instanceId", pickup.InstanceId.ToString(),
            "weaponId", pickup.WeaponId,
            "source", pickup.Source,
            "position", $"{position.x:F1},{position.y:F1},{position.z:F1}"));
        return pickup;
    }

    private static string ResolveFallbackResourceWeaponId(int seed)
    {
        int index = (int)((uint)seed % 3u);
        switch (index)
        {
            case 1:
                return "hammer_heavy";
            case 2:
                return "bow_charge";
            default:
                return "pistol_basic";
        }
    }

    public TotemWeaponPickupModel FindNearestPickup(Vector3 position, float radius)
    {
        float maxSqr = radius * radius;
        float bestSqr = float.MaxValue;
        TotemWeaponPickupModel best = null;
        for (int i = 0; i < activePickups.Count; i++)
        {
            var pickup = activePickups[i];
            if (pickup == null)
            {
                continue;
            }

            float sqr = (pickup.Position - position).sqrMagnitude;
            if (sqr > maxSqr || sqr >= bestSqr)
            {
                continue;
            }

            bestSqr = sqr;
            best = pickup;
        }

        return best;
    }

    public bool TryPickupNearestWeapon(TotemActorModel actor, float radius, out TotemWeaponPickupResult result)
    {
        result = null;
        if (actor == null)
        {
            result = BuildPickupResult(false, "NoActor", null, false, 0);
            return false;
        }

        var pickup = FindNearestPickup(actor.Position, radius);
        if (pickup == null)
        {
            result = BuildPickupResult(false, "NoPickupInRange", null, false, 0);
            return false;
        }

        return TryPickupWeapon(actor, pickup, out result);
    }

    public bool TryPickupWeapon(TotemActorModel actor, TotemWeaponPickupModel pickup, out TotemWeaponPickupResult result)
    {
        result = null;
        if (actor == null || pickup == null || !activePickups.Contains(pickup))
        {
            result = BuildPickupResult(false, "InvalidPickup", pickup, false, 0);
            return false;
        }

        bool upgraded = TryUpgrade(actor, pickup.WeaponId, PickupDuplicateConvertBaseGold, out int convertedGold);
        if (!upgraded && convertedGold <= 0)
        {
            result = BuildPickupResult(false, "UpgradeRejected", pickup, false, 0);
            return false;
        }

        if (convertedGold > 0)
        {
            economyService?.AddCoins(actor, convertedGold);
        }

        RemovePickup(pickup);
        pickedPickupCount++;
        lastPickupWeaponId = pickup.WeaponId;
        lastPickupActorId = actor.ActorId;
        int level = GetWeaponLevel(actor);
        result = BuildPickupResult(true, convertedGold > 0 ? "ConvertedGold" : "Picked", pickup, upgraded, convertedGold);
        result.weaponLevel = level;
        GFTrace.Success("TotemWeapon", "Pickup.Picked", null, GFTrace.Data(
            "actor", actor.Name,
            "weaponId", pickup.WeaponId,
            "level", level.ToString(),
            "upgraded", upgraded.ToString(),
            "convertedGold", convertedGold.ToString()));
        return true;
    }

    public static TotemWeaponMultipliers GetMultipliers(int level)
    {
        int steps = Mathf.Max(0, level - 1);
        return new TotemWeaponMultipliers
        {
            DamageMul = Mathf.Pow(1.2f, steps),
            RangeAdd = 0.5f * steps,
            CooldownMul = Mathf.Pow(0.9f, steps),
        };
    }

    public static int ComputeConvertGold(int baseGoldCost)
    {
        return Mathf.RoundToInt(Mathf.Max(0, baseGoldCost) * 0.5f);
    }

    private static TotemWeaponDefinition[] LoadWeaponCatalog()
    {
        return NonEmpty(
            TotemDataService.LoadGameplayCatalogOrDefault().CreateWeaponDefinitions(),
            Array.Empty<TotemWeaponDefinition>());
    }

    private static TotemProjectileDefinition[] LoadProjectileCatalog()
    {
        return NonEmpty(
            TotemDataService.LoadGameplayCatalogOrDefault().CreateProjectileDefinitions(),
            Array.Empty<TotemProjectileDefinition>());
    }

    private static TotemWeaponTraitDefinition[] LoadTraitCatalog()
    {
        return NonEmpty(
            TotemDataService.LoadGameplayCatalogOrDefault().CreateWeaponTraitDefinitions(),
            Array.Empty<TotemWeaponTraitDefinition>());
    }

    private static T[] NonEmpty<T>(T[] primary, T[] fallback)
    {
        return primary == null || primary.Length <= 0 ? fallback : primary;
    }

    private bool IsDropCandidate(TotemWeaponDropDefinition drop, string source, int roomIndex)
    {
        if (drop == null || drop.Weight <= 0 || string.IsNullOrWhiteSpace(drop.WeaponId))
        {
            return false;
        }

        if (!string.Equals(drop.DropSource, source, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (roomIndex < drop.MinRoomIndex || roomIndex > drop.MaxRoomIndex)
        {
            return false;
        }

        return TryGetRuntimeDefinition(drop.WeaponId, out _);
    }

    private int ResolveRoomIndex(Vector3 position)
    {
        var rooms = mapService?.CurrentMap?.Rooms;
        if (rooms == null || rooms.Length <= 0)
        {
            return 1;
        }

        int nearestRoomId = rooms[0].RoomId;
        float bestSqr = float.MaxValue;
        var point = new Vector2(position.x, position.z);
        for (int i = 0; i < rooms.Length; i++)
        {
            var room = rooms[i];
            if (room == null)
            {
                continue;
            }

            if (room.Bounds.Contains(point))
            {
                return Mathf.Max(1, room.RoomId + 1);
            }

            Vector3 delta = room.CenterWorld - position;
            delta.y = 0f;
            float sqr = delta.sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                nearestRoomId = room.RoomId;
            }
        }

        return Mathf.Max(1, nearestRoomId + 1);
    }

    private GameObject CreatePickupObject(TotemWeaponPickupModel pickup)
    {
        var go = new GameObject($"TotemWeaponPickup_{pickup.InstanceId}_{pickup.WeaponId}");
        go.transform.position = pickup.Position;
        string assetKey = $"weapon.{pickup.WeaponId}";
        Sprite sprite = null;
        if (assetService != null)
        {
            assetService.TryLoadSprite(assetKey, out sprite);
        }

        if (sprite != null)
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            var sorter = go.AddComponent<TotemActorDepthSorter>();
            sorter.BaseOffset = TotemActorDepthSorter.DefaultWorldBaseOffset;
            sorter.SortingLayerName = TotemActorDepthSorter.WorldSortingLayer;
            sorter.RefreshRenderers();
            sorter.ForceRecalculate();
            go.transform.localScale = Vector3.one * 0.75f;
            pickup.VisualAssetKey = assetKey;
            return go;
        }

        var fallback = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fallback.name = go.name;
        fallback.transform.position = pickup.Position;
        fallback.transform.localScale = new Vector3(0.6f, 0.15f, 0.6f);
        pickup.VisualAssetKey = "primitive.weaponPickup";
        DestroyObject(go);
        return fallback;
    }

    private void RemovePickup(TotemWeaponPickupModel pickup)
    {
        activePickups.Remove(pickup);
        DestroyObject(pickup.GameObject);
        pickup.GameObject = null;
    }

    private void DestroyAllPickups()
    {
        for (int i = activePickups.Count - 1; i >= 0; i--)
        {
            var pickup = activePickups[i];
            if (pickup != null)
            {
                DestroyObject(pickup.GameObject);
                pickup.GameObject = null;
            }
        }

        activePickups.Clear();
    }

    private static TotemWeaponPickupResult BuildPickupResult(bool picked, string reason, TotemWeaponPickupModel pickup, bool upgraded, int convertedGold)
    {
        return new TotemWeaponPickupResult
        {
            picked = picked,
            reason = reason,
            pickupInstanceId = pickup?.InstanceId ?? 0,
            weaponId = pickup?.WeaponId ?? string.Empty,
            upgraded = upgraded,
            convertedGold = convertedGold,
        };
    }

    private static void DestroyObject(UnityEngine.Object obj)
    {
        if (obj == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(obj);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            ResetRunState();
            if (actorService?.Player != null)
            {
                EquipWeapon(actorService.Player, flowService?.StartupSelection?.WeaponId);
            }

            SpawnMapResourcePickups(mapService?.CurrentMap, mapService?.CurrentMap?.Seed ?? 1);
            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            ResetRunState();
            GFTrace.Info("TotemWeapon", "RunState.Cleared", null, GFTrace.Data("nextState", nextState.ToString()));
        }
    }

    private void ResetRunState()
    {
        DestroyAllPickups();
        states.Clear();
        spawnedPickupCount = 0;
        pickedPickupCount = 0;
        lastPickupWeaponId = string.Empty;
        lastPickupActorId = 0;
        mapResourcePickupCount = 0;
        lastMapResourceAnchorId = string.Empty;
        nextPickupInstanceId = 1;
        traitEffectAppliedCount = 0;
        lastTraitEffect = BuildTraitSkipped(null, null, null, "None");
    }

    private static TotemWeaponFireResult Skipped(string reason)
    {
        return new TotemWeaponFireResult
        {
            Fired = false,
            Reason = reason,
        };
    }
}
