using UnityEngine;

public interface ITotemEnemyAbilityHost
{
    bool TryDamageTarget(TotemEnemyControllerBase controller, TotemActorModel target, TotemEnemyAbilityRuntimeDefinition definition, float multiplier);

    int DamageParticipantsInRadius(TotemEnemyControllerBase controller, Vector3 center, float radius, TotemEnemyAbilityRuntimeDefinition definition);

    int DamageParticipantsInCone(TotemEnemyControllerBase controller, Vector3 origin, Vector3 forward, float radius, float halfAngle, TotemEnemyAbilityRuntimeDefinition definition);

    bool TryMove(TotemEnemyControllerBase controller, Vector3 delta);

    bool TrySummon(TotemEnemyControllerBase controller, TotemEnemyAbilityRuntimeDefinition definition, int count);

    void SpawnProjectile(TotemEnemyControllerBase controller, TotemActorModel target, TotemEnemyAbilityRuntimeDefinition definition);

    void CreateHazard(TotemEnemyControllerBase controller, Vector3 position, TotemEnemyAbilityRuntimeDefinition definition);

    void PlayCue(TotemEnemyControllerBase controller, TotemEnemyAbilityRuntimeDefinition definition);

    void NotifyAbility(TotemEnemyControllerBase controller, ITotemEnemyAbility ability, string reason);

    void NotifyPathRequest(TotemEnemyControllerBase controller, bool accepted);
}

public readonly struct TotemEnemyAbilityContext
{
    public readonly TotemEnemyControllerBase Controller;
    public readonly ITotemEnemyAbilityHost Host;
    public readonly TotemActorModel Target;
    public readonly float WorldTime;

    public TotemEnemyAbilityContext(
        TotemEnemyControllerBase controller,
        ITotemEnemyAbilityHost host,
        TotemActorModel target,
        float worldTime)
    {
        Controller = controller;
        Host = host;
        Target = target;
        WorldTime = worldTime;
    }

    public TotemEnemyModel Owner => Controller?.Enemy;

    public bool DamageTarget(TotemEnemyAbilityRuntimeDefinition definition, float multiplier = 1f)
    {
        return Host != null && Target != null && Host.TryDamageTarget(Controller, Target, definition, multiplier);
    }

    public int DamageRadius(Vector3 center, TotemEnemyAbilityRuntimeDefinition definition)
    {
        return Host == null ? 0 : Host.DamageParticipantsInRadius(Controller, center, definition.radius, definition);
    }

    public int DamageCone(Vector3 origin, Vector3 forward, TotemEnemyAbilityRuntimeDefinition definition)
    {
        return Host == null ? 0 : Host.DamageParticipantsInCone(Controller, origin, forward, definition.radius, definition.coneHalfAngle, definition);
    }
}

public interface ITotemEnemyAbility
{
    TotemEnemyAbilityRuntimeDefinition Definition { get; }

    TotemEnemyAbilityPhase Phase { get; }

    float CooldownRemaining { get; }

    bool CanStart(in TotemEnemyAbilityContext context);

    float Score(in TotemEnemyAbilityContext context);

    void Begin(in TotemEnemyAbilityContext context);

    void Tick(in TotemEnemyAbilityContext context, float deltaTime);

    void Cancel(in TotemEnemyAbilityContext context, string reason);

    void OnOwnerDeath(in TotemEnemyAbilityContext context);
}

public abstract class TotemEnemyAbilityBase : ITotemEnemyAbility
{
    private float _phaseRemaining;
    private bool _activeResolved;

    protected TotemEnemyAbilityBase(TotemEnemyAbilityRuntimeDefinition definition)
    {
        Definition = definition;
    }

    public TotemEnemyAbilityRuntimeDefinition Definition { get; }

    public TotemEnemyAbilityPhase Phase { get; private set; }

    public float CooldownRemaining { get; private set; }

    public virtual bool CanStart(in TotemEnemyAbilityContext context)
    {
        if (context.Owner == null || !context.Owner.IsAlive || Definition == null || CooldownRemaining > 0f)
        {
            return false;
        }

        if (Phase == TotemEnemyAbilityPhase.Windup || Phase == TotemEnemyAbilityPhase.Active || Phase == TotemEnemyAbilityPhase.Recovery)
        {
            return false;
        }

        if (Definition.abilityType == TotemEnemyAbilityType.DeathBurst || Definition.abilityType == TotemEnemyAbilityType.PhaseTransition)
        {
            return false;
        }

        if (context.Controller != null && context.Controller.BossPhase < Mathf.Max(1, Definition.minimumBossPhase))
        {
            return false;
        }

        return DoesRangeAllow(context);
    }

    public virtual float Score(in TotemEnemyAbilityContext context)
    {
        if (!CanStart(context))
        {
            return float.MinValue;
        }

        float range = Mathf.Max(0.1f, Definition.range);
        float distance = context.Target == null || context.Owner == null
            ? range
            : FlatDistance(context.Owner.Position, context.Target.Position);
        float rangeFit = 1f - Mathf.Clamp01(Mathf.Abs(range - distance) / range);
        return Definition.score + rangeFit;
    }

    public void Begin(in TotemEnemyAbilityContext context)
    {
        if (!CanStart(context))
        {
            return;
        }

        _activeResolved = false;
        SetPhase(context, TotemEnemyAbilityPhase.Windup, Mathf.Max(0f, Definition.windup), "Begin");
        OnBegin(context);
    }

    public void Tick(in TotemEnemyAbilityContext context, float deltaTime)
    {
        CooldownRemaining = Mathf.Max(0f, CooldownRemaining - Mathf.Max(0f, deltaTime));
        if (deltaTime <= 0f || Phase == TotemEnemyAbilityPhase.Inactive || Phase == TotemEnemyAbilityPhase.Complete || Phase == TotemEnemyAbilityPhase.Cancelled)
        {
            return;
        }

        float remainingDelta = deltaTime;
        for (int step = 0; step < 4 && remainingDelta >= 0f; step++)
        {
            if (_phaseRemaining > remainingDelta)
            {
                _phaseRemaining -= remainingDelta;
                return;
            }

            remainingDelta -= _phaseRemaining;
            _phaseRemaining = 0f;
            if (Phase == TotemEnemyAbilityPhase.Windup)
            {
                SetPhase(context, TotemEnemyAbilityPhase.Active, Mathf.Max(0f, Definition.active), "WindupComplete");
                if (!_activeResolved)
                {
                    _activeResolved = true;
                    ResolveActive(context);
                }
                continue;
            }

            if (Phase == TotemEnemyAbilityPhase.Active)
            {
                SetPhase(context, TotemEnemyAbilityPhase.Recovery, Mathf.Max(0f, Definition.recovery), "ActiveComplete");
                continue;
            }

            if (Phase == TotemEnemyAbilityPhase.Recovery)
            {
                Phase = TotemEnemyAbilityPhase.Complete;
                CooldownRemaining = Mathf.Max(0f, Definition.cooldown);
                context.Host?.NotifyAbility(context.Controller, this, "RecoveryComplete");
                OnComplete(context);
                return;
            }

            return;
        }
    }

    public virtual void Cancel(in TotemEnemyAbilityContext context, string reason)
    {
        if (Phase != TotemEnemyAbilityPhase.Windup && Phase != TotemEnemyAbilityPhase.Active && Phase != TotemEnemyAbilityPhase.Recovery)
        {
            return;
        }

        Phase = TotemEnemyAbilityPhase.Cancelled;
        _phaseRemaining = 0f;
        CooldownRemaining = Mathf.Max(CooldownRemaining, Definition.cooldown * 0.5f);
        context.Host?.NotifyAbility(context.Controller, this, string.IsNullOrEmpty(reason) ? "Cancelled" : reason);
    }

    public virtual void OnOwnerDeath(in TotemEnemyAbilityContext context)
    {
        Cancel(context, "OwnerDead");
    }

    protected virtual bool DoesRangeAllow(in TotemEnemyAbilityContext context)
    {
        if (context.Target == null || context.Owner == null)
        {
            return false;
        }

        return FlatSqrDistance(context.Owner.Position, context.Target.Position) <= Definition.range * Definition.range;
    }

    protected virtual void OnBegin(in TotemEnemyAbilityContext context)
    {
        context.Host?.PlayCue(context.Controller, Definition);
    }

    protected virtual void OnComplete(in TotemEnemyAbilityContext context)
    {
    }

    protected abstract void ResolveActive(in TotemEnemyAbilityContext context);

    protected static Vector3 FlatDirection(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        direction.y = 0f;
        return direction.sqrMagnitude <= 0.0001f ? Vector3.forward : direction.normalized;
    }

    protected static float FlatDistance(Vector3 a, Vector3 b)
    {
        return Mathf.Sqrt(FlatSqrDistance(a, b));
    }

    protected static float FlatSqrDistance(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float z = a.z - b.z;
        return x * x + z * z;
    }

    private void SetPhase(in TotemEnemyAbilityContext context, TotemEnemyAbilityPhase phase, float duration, string reason)
    {
        Phase = phase;
        _phaseRemaining = duration;
        context.Host?.NotifyAbility(context.Controller, this, reason);
    }
}

public sealed class TotemEnemyMeleeAbility : TotemEnemyAbilityBase
{
    public TotemEnemyMeleeAbility(TotemEnemyAbilityRuntimeDefinition definition) : base(definition) { }

    protected override void ResolveActive(in TotemEnemyAbilityContext context)
    {
        context.DamageTarget(Definition);
    }
}

public sealed class TotemEnemyProjectileAbility : TotemEnemyAbilityBase
{
    public TotemEnemyProjectileAbility(TotemEnemyAbilityRuntimeDefinition definition) : base(definition) { }

    protected override void ResolveActive(in TotemEnemyAbilityContext context)
    {
        context.Host?.SpawnProjectile(context.Controller, context.Target, Definition);
        context.DamageTarget(Definition);
    }
}

public sealed class TotemEnemyChargeAbility : TotemEnemyAbilityBase
{
    public TotemEnemyChargeAbility(TotemEnemyAbilityRuntimeDefinition definition) : base(definition) { }

    protected override void ResolveActive(in TotemEnemyAbilityContext context)
    {
        if (context.Owner != null && context.Target != null)
        {
            Vector3 direction = FlatDirection(context.Owner.Position, context.Target.Position);
            float distance = Definition.moveDistance > 0f ? Definition.moveDistance : Definition.range;
            context.Host?.TryMove(context.Controller, direction * distance);
        }

        context.DamageTarget(Definition);
    }
}

public sealed class TotemEnemyLeapAbility : TotemEnemyAbilityBase
{
    public TotemEnemyLeapAbility(TotemEnemyAbilityRuntimeDefinition definition) : base(definition) { }

    protected override void ResolveActive(in TotemEnemyAbilityContext context)
    {
        if (context.Owner != null && context.Target != null)
        {
            Vector3 destination = context.Target.Position - FlatDirection(context.Owner.Position, context.Target.Position) * 0.75f;
            context.Host?.TryMove(context.Controller, destination - context.Owner.Position);
            context.DamageRadius(destination, Definition);
        }
    }
}

public sealed class TotemEnemyBeamAbility : TotemEnemyAbilityBase
{
    public TotemEnemyBeamAbility(TotemEnemyAbilityRuntimeDefinition definition) : base(definition) { }

    protected override void ResolveActive(in TotemEnemyAbilityContext context)
    {
        context.DamageTarget(Definition);
    }
}

public sealed class TotemEnemyConeSweepAbility : TotemEnemyAbilityBase
{
    public TotemEnemyConeSweepAbility(TotemEnemyAbilityRuntimeDefinition definition) : base(definition) { }

    protected override void ResolveActive(in TotemEnemyAbilityContext context)
    {
        if (context.Owner == null)
        {
            return;
        }

        Vector3 forward = context.Target == null
            ? Vector3.forward
            : FlatDirection(context.Owner.Position, context.Target.Position);
        context.DamageCone(context.Owner.Position, forward, Definition);
    }
}

public sealed class TotemEnemyAreaPulseAbility : TotemEnemyAbilityBase
{
    public TotemEnemyAreaPulseAbility(TotemEnemyAbilityRuntimeDefinition definition) : base(definition) { }

    protected override bool DoesRangeAllow(in TotemEnemyAbilityContext context)
    {
        return context.Owner != null && context.Target != null &&
               FlatSqrDistance(context.Owner.Position, context.Target.Position) <= Definition.radius * Definition.radius;
    }

    protected override void ResolveActive(in TotemEnemyAbilityContext context)
    {
        if (context.Owner != null)
        {
            context.DamageRadius(context.Owner.Position, Definition);
        }
    }
}

public sealed class TotemEnemyHazardZoneAbility : TotemEnemyAbilityBase
{
    public TotemEnemyHazardZoneAbility(TotemEnemyAbilityRuntimeDefinition definition) : base(definition) { }

    protected override void ResolveActive(in TotemEnemyAbilityContext context)
    {
        Vector3 position = context.Target == null ? context.Owner.Position : context.Target.Position;
        context.Host?.CreateHazard(context.Controller, position, Definition);
        context.DamageRadius(position, Definition);
    }
}

public sealed class TotemEnemyShieldAbility : TotemEnemyAbilityBase
{
    public TotemEnemyShieldAbility(TotemEnemyAbilityRuntimeDefinition definition) : base(definition) { }

    protected override bool DoesRangeAllow(in TotemEnemyAbilityContext context)
    {
        return context.Owner != null;
    }

    protected override void ResolveActive(in TotemEnemyAbilityContext context)
    {
        context.Controller?.AddShield(Definition.shieldAmount > 0f ? Definition.shieldAmount : context.Owner.MaxHealth * 0.15f);
    }
}

public sealed class TotemEnemySummonAbility : TotemEnemyAbilityBase
{
    public TotemEnemySummonAbility(TotemEnemyAbilityRuntimeDefinition definition) : base(definition) { }

    protected override bool DoesRangeAllow(in TotemEnemyAbilityContext context)
    {
        return context.Owner != null && !string.IsNullOrEmpty(Definition.summonEnemyId) && Definition.summonCount > 0;
    }

    protected override void ResolveActive(in TotemEnemyAbilityContext context)
    {
        context.Host?.TrySummon(context.Controller, Definition, Definition.summonCount);
    }
}

public sealed class TotemEnemyRegenerateAbility : TotemEnemyAbilityBase
{
    public TotemEnemyRegenerateAbility(TotemEnemyAbilityRuntimeDefinition definition) : base(definition) { }

    protected override bool DoesRangeAllow(in TotemEnemyAbilityContext context)
    {
        return context.Owner != null && context.Owner.Health < context.Owner.MaxHealth;
    }

    protected override void ResolveActive(in TotemEnemyAbilityContext context)
    {
        if (context.Owner != null)
        {
            float amount = Definition.healAmount > 0f ? Definition.healAmount : context.Owner.MaxHealth * 0.1f;
            context.Owner.Heal(amount);
        }
    }
}

public sealed class TotemEnemyDeathBurstAbility : TotemEnemyAbilityBase
{
    private bool _resolved;

    public TotemEnemyDeathBurstAbility(TotemEnemyAbilityRuntimeDefinition definition) : base(definition) { }

    public override bool CanStart(in TotemEnemyAbilityContext context)
    {
        return false;
    }

    public override void OnOwnerDeath(in TotemEnemyAbilityContext context)
    {
        if (_resolved || context.Owner == null)
        {
            return;
        }

        _resolved = true;
        context.Host?.PlayCue(context.Controller, Definition);
        context.DamageRadius(context.Owner.Position, Definition);
        context.Host?.CreateHazard(context.Controller, context.Owner.Position, Definition);
        context.Host?.NotifyAbility(context.Controller, this, "OwnerDeath");
    }

    protected override void ResolveActive(in TotemEnemyAbilityContext context)
    {
    }
}

public sealed class TotemEnemyPhaseTransitionAbility : TotemEnemyAbilityBase
{
    public TotemEnemyPhaseTransitionAbility(TotemEnemyAbilityRuntimeDefinition definition) : base(definition) { }

    public override bool CanStart(in TotemEnemyAbilityContext context)
    {
        return false;
    }

    protected override void ResolveActive(in TotemEnemyAbilityContext context)
    {
    }
}

public static class TotemEnemyAbilityFactory
{
    public static ITotemEnemyAbility Create(TotemEnemyAbilityRuntimeDefinition definition)
    {
        if (definition == null)
        {
            return null;
        }

        switch (definition.abilityType)
        {
            case TotemEnemyAbilityType.Melee: return new TotemEnemyMeleeAbility(definition);
            case TotemEnemyAbilityType.Projectile: return new TotemEnemyProjectileAbility(definition);
            case TotemEnemyAbilityType.Charge: return new TotemEnemyChargeAbility(definition);
            case TotemEnemyAbilityType.Leap: return new TotemEnemyLeapAbility(definition);
            case TotemEnemyAbilityType.Beam: return new TotemEnemyBeamAbility(definition);
            case TotemEnemyAbilityType.ConeSweep: return new TotemEnemyConeSweepAbility(definition);
            case TotemEnemyAbilityType.AreaPulse: return new TotemEnemyAreaPulseAbility(definition);
            case TotemEnemyAbilityType.HazardZone: return new TotemEnemyHazardZoneAbility(definition);
            case TotemEnemyAbilityType.Shield: return new TotemEnemyShieldAbility(definition);
            case TotemEnemyAbilityType.Summon: return new TotemEnemySummonAbility(definition);
            case TotemEnemyAbilityType.Regenerate: return new TotemEnemyRegenerateAbility(definition);
            case TotemEnemyAbilityType.DeathBurst: return new TotemEnemyDeathBurstAbility(definition);
            case TotemEnemyAbilityType.PhaseTransition: return new TotemEnemyPhaseTransitionAbility(definition);
            default: return null;
        }
    }
}
