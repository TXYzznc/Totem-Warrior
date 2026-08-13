using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TotemWeaponService : TotemRuntimeServiceBase, ITotemRuntimeTickService, ITotemGameplaySimulationService
{
    public const string DefaultWeaponId = "rifle_patrol_v1";
    public const int MaxWeaponLevel = 3;

    private readonly Dictionary<int, TotemWeaponState> states = new Dictionary<int, TotemWeaponState>(64);
    private TotemGameFlowService flowService;
    private TotemActorService actorService;
    private TotemMatchClockService matchClock;
    private TotemMatchFlowService matchFlowService;
    private TotemFirstPlayableTattooBuildService tattooBuildService;
    private TotemFirstPlayableElementService elementService;
    private TotemEffectResolutionService effectResolutionService;
    private TotemFirstPlayableSocialService socialService;
    private TotemVfxService vfxService;
    private readonly TotemElementTargetCandidate[] elementTargetBuffer =
        new TotemElementTargetCandidate[TotemFirstPlayableRules.ParticipantCount];
    private TotemWeaponDefinition[] runtimeCatalog = Array.Empty<TotemWeaponDefinition>();
    private int elementApplicationSequence;

    public override string ServiceName => "Weapon";

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        actorService = runtime.GetService<TotemActorService>();
        matchClock = runtime.GetService<TotemMatchClockService>();
        matchFlowService = runtime.GetService<TotemMatchFlowService>();
        tattooBuildService = runtime.GetService<TotemFirstPlayableTattooBuildService>();
        elementService = runtime.GetService<TotemFirstPlayableElementService>();
        effectResolutionService = runtime.GetService<TotemEffectResolutionService>();
        socialService = runtime.GetService<TotemFirstPlayableSocialService>();
        vfxService = runtime.GetService<TotemVfxService>();
        runtimeCatalog = CreateFirstPlayableWeaponCatalog();
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

        actorService = null;
        matchClock = null;
        matchFlowService = null;
        tattooBuildService = null;
        elementService = null;
        effectResolutionService = null;
        socialService = null;
        vfxService = null;
        runtimeCatalog = Array.Empty<TotemWeaponDefinition>();
        states.Clear();
        elementApplicationSequence = 0;
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

    public IReadOnlyList<TotemWeaponDefinition> GetRuntimeCatalog()
    {
        return runtimeCatalog;
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

    public void EquipWeapon(TotemActorModel actor, string weaponId)
    {
        if (actor == null)
        {
            return;
        }

        TryGetRuntimeDefinition(DefaultWeaponId, out var definition);
        if (definition == null)
        {
            return;
        }

        states[actor.ActorId] = new TotemWeaponState
        {
            Weapon = definition,
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

    /// <summary>
    /// Shared first-playable aiming, firing, hit-region and damage entry point.
    /// Both the human combat controller and bot controller call this method.
    /// </summary>
    public bool TryResolveFirstPlayableAttack(
        TotemActorModel source,
        TotemCombatantModel target,
        Vector3 rayOrigin,
        Vector3 aimDirection,
        float damageMultiplier,
        out TotemGunAttackResult result)
    {
        var state = GetOrCreateState(source);
        if (state?.Weapon == null)
        {
            result = BuildSkippedGunAttack("NoWeapon");
            return false;
        }

        if (target == null)
        {
            result = BuildSkippedGunAttack("NoTarget");
            return false;
        }

        if (state.CooldownRemaining > 0f)
        {
            result = BuildSkippedGunAttack("Cooldown");
            return false;
        }

        TotemWeaponDefinition weapon = state.Weapon;
        TotemWeaponMultipliers multipliers = GetMultipliers(state.Level);
        float range = weapon.Range + multipliers.RangeAdd;
        float baseDamage = weapon.BaseDamage * multipliers.DamageMul * Mathf.Max(0f, damageMultiplier);
        float damage = baseDamage;

        state.CooldownRemaining = weapon.Cooldown * multipliers.CooldownMul;
        unchecked
        {
            state.FireSequence++;
        }

        TotemHitRegion hitRegion = TotemHitRegionResolver.ResolveForTarget(
            rayOrigin,
            aimDirection,
            range,
            target.CombatantId,
            target.Position,
            out Vector3 hitPoint,
            out Vector3 hitNormal);
        bool wasAlive = target.IsAlive;
        actorService?.NotifyActorAttack(source, "FirstPlayableGun");
        TotemDirectDamageResult directDamage = ResolveGunHit(
            source,
            target,
            hitRegion,
            hitPoint,
            hitNormal,
            damage);
        bool killed = wasAlive && !target.IsAlive;

        vfxService?.SpawnRifleTrail(source.Position, hitPoint, weapon.WeaponId, source.Kind == TotemActorKind.Player);
        vfxService?.SpawnAttackHit(hitPoint, weapon.WeaponId, false);
        result = new TotemGunAttackResult(
            fired: true,
            reason: directDamage.IsEffectiveDirectDamage ? "Applied" : "NoEffectiveDamage",
            weapon,
            hitRegion,
            directDamage,
            killed,
            state.FireSequence);
        return true;
    }

    public bool TryApplyFirstPlayableFireCommand(
        in TotemGameplayCommand command,
        float damageMultiplier,
        out TotemGunAttackResult result)
    {
        if (!command.IsValid
            || command.Type != TotemGameplayCommandType.Fire
            || !TotemMatchPhaseContract.IsCombat(matchFlowService?.CurrentPhase ?? TotemMatchPhase.FrontEnd))
        {
            result = BuildSkippedGunAttack("InvalidFireCommand");
            return false;
        }

        TotemActorModel source = FindParticipant(command.ParticipantId);
        TotemCombatantModel target = FindCombatant(command.IntValue);
        if (source == null || !source.IsAlive)
        {
            result = BuildSkippedGunAttack("InvalidFireSource");
            return false;
        }

        Vector3 rayOrigin = source.Position + Vector3.up * 1.2f;
        Vector3 aimDirection = command.WorldValue;
        if (aimDirection.sqrMagnitude <= 0.0001f && target != null)
        {
            aimDirection = target.Position - rayOrigin;
        }

        return TryResolveFirstPlayableAttack(
            source,
            target,
            rayOrigin,
            aimDirection,
            damageMultiplier,
            out result);
    }

    /// <summary>
    /// The only first-playable direct-damage entry point. Human and bot gunfire
    /// both pass through this method before effect events can be submitted.
    /// </summary>
    public TotemDirectDamageResult ResolveGunHit(
        TotemActorModel source,
        TotemCombatantModel target,
        TotemHitRegion hitRegion,
        Vector3 hitPoint,
        Vector3 hitNormal,
        float requestedDamage)
    {
        float resolvedRequestedDamage = elementService == null || target == null
            ? requestedDamage
            : elementService.ModifyDirectDamage(target.CombatantId, requestedDamage);
        var hit = new TotemGunHitContext(
            new TotemParticipantId(source?.ParticipantId ?? -1),
            source?.TeamId ?? new TotemTeamId(-1),
            target?.CombatantId ?? 0,
            target is TotemParticipantModel participantTarget ? participantTarget.TeamId : new TotemTeamId(-1),
            hitRegion,
            hitPoint,
            hitNormal,
            resolvedRequestedDamage);

        float appliedDamage = 0f;
        if (source != null && target != null && resolvedRequestedDamage > 0f)
        {
            if (target is TotemActorModel actorTarget)
            {
                if (actorService != null
                    && actorService.TryApplyDamage(actorTarget, resolvedRequestedDamage, source, "FirstPlayableGun"))
                {
                    appliedDamage = actorService.LastDamage.Target == actorTarget
                        ? Mathf.Max(0f, actorService.LastDamage.Amount)
                        : 0f;
                }
            }
        }

        var result = new TotemDirectDamageResult(
            hit,
            shieldDamage: 0f,
            healthDamage: appliedDamage,
            relationshipAllowed: appliedDamage > 0f);
        if (result.IsEffectiveDirectDamage && effectResolutionService != null)
        {
            effectResolutionService.BeginResolution();
            effectResolutionService.SubmitGunHit(result);
            ResolveLightningDischarge(target, result.EffectiveDamage);
            SubmitPrimaryTattooElement(source, target, result.EffectiveDamage);
            effectResolutionService.Resolve();
        }

        return result;
    }

    private void SubmitPrimaryTattooElement(
        TotemActorModel source,
        TotemCombatantModel target,
        float bodyHitDamage)
    {
        if (source == null
            || target == null
            || !target.IsAlive
            || tattooBuildService == null
            || elementService == null)
        {
            return;
        }

        TotemFirstPlayableTattooBuildState build = tattooBuildService.GetOrCreateState(source);
        TotemTattooLoadoutEntry rifleArm = build?.GetSlot(TotemTattooSlotId.RightArm) ?? default;
        if (!rifleArm.IsEquipped)
        {
            return;
        }

        ApplyTattooElement(source, target, rifleArm.Element, bodyHitDamage);
        TotemCombatantModel secondaryTarget = FindPatternSecondaryTarget(rifleArm.Pattern, target);
        if (secondaryTarget != null)
        {
            ApplyTattooElement(source, secondaryTarget, rifleArm.Element, bodyHitDamage);
        }
    }

    private void ApplyTattooElement(
        TotemActorModel source,
        TotemCombatantModel target,
        TotemFirstPlayableElement element,
        float bodyHitDamage)
    {
        if (source == null || target == null || !target.IsAlive || element == TotemFirstPlayableElement.None)
        {
            return;
        }

        unchecked
        {
            elementApplicationSequence++;
        }

        TotemElementApplyResult applied = elementService.ApplyElement(
            target.CombatantId,
            element,
            new TotemParticipantId(source.ParticipantId),
            elementApplicationSequence,
            bodyHitDamage);
        if (!applied.Applied)
        {
            return;
        }

        if (applied.TriggeredReaction)
        {
            float actualIndirectDamage = ResolveReactionDamage(
                source,
                target,
                applied.Reaction,
                bodyHitDamage);
            var actualAttribution = new TotemReactionAttribution(
                applied.Attribution.Reaction,
                applied.Attribution.TriggerParticipantId,
                applied.Attribution.AssistingParticipantId,
                actualIndirectDamage);
            socialService?.RecordReactionAttribution(actualAttribution);
            if (applied.Reaction == TotemReactionKind.Stasis)
            {
                socialService?.RecordEffectiveControl(
                    applied.Attribution.TriggerParticipantId,
                    TotemFirstPlayableElementRules.StasisDurationSeconds);
            }

            effectResolutionService.TrySubmit(
                TotemEffectEventKind.Reaction,
                new TotemParticipantId(source.ParticipantId),
                target.CombatantId,
                actualIndirectDamage);
        }
        else
        {
            effectResolutionService.TrySubmit(
                TotemEffectEventKind.ElementApply,
                new TotemParticipantId(source.ParticipantId),
                target.CombatantId,
                (float)element);
        }
    }

    private TotemCombatantModel FindPatternSecondaryTarget(
        TotemFirstPlayablePatternId pattern,
        TotemCombatantModel primaryTarget)
    {
        if (pattern != TotemFirstPlayablePatternId.P02 || primaryTarget == null)
        {
            return null;
        }

        if (!TryFillSameFactionCandidates(primaryTarget, out int factionId, out int candidateCount))
        {
            return null;
        }

        int combatantId = TotemTattooPatternTargetResolver.ResolveSecondaryTarget(
            pattern,
            primaryTarget.CombatantId,
            factionId,
            primaryTarget.Position,
            elementTargetBuffer,
            candidateCount);
        if (combatantId <= 0)
        {
            return null;
        }

        return FindCombatant(combatantId);
    }

    private void ResolveLightningDischarge(TotemCombatantModel chargedTarget, float effectiveDirectDamage)
    {
        if (chargedTarget == null
            || !chargedTarget.IsAlive
            || effectiveDirectDamage <= 0f
            || elementService == null
            || !elementService.TryBeginLightningDischarge(chargedTarget.CombatantId, effectiveDirectDamage > 0f)
            || !elementService.TryGetOldestLayerSource(chargedTarget.CombatantId, out TotemElementLayerSource layerSource)
            || !TryFillSameFactionCandidates(chargedTarget, out int factionId, out int candidateCount))
        {
            return;
        }

        TotemLightningDischargeResult discharge = TotemLightningDischargeResolver.Resolve(
            chargedTarget.CombatantId,
            factionId,
            chargedTarget.Position,
            elementTargetBuffer,
            candidateCount);
        TotemCombatantModel dischargeTarget = FindCombatant(discharge.TargetCombatantId);
        TotemActorModel elementSource = FindParticipant(layerSource.SourceParticipantId);
        if (dischargeTarget == null || elementSource == null)
        {
            return;
        }

        float appliedDamage = ApplyIndirectDamage(
            elementSource,
            dischargeTarget,
            effectiveDirectDamage * discharge.DamageMultiplier,
            TotemReactionKind.None,
            "Element:LightningDischarge");
        if (appliedDamage <= 0f)
        {
            return;
        }

        socialService?.RecordIndirectElementDamage(layerSource.SourceParticipantId, appliedDamage);
        effectResolutionService?.TrySubmit(
            TotemEffectEventKind.ElementApply,
            layerSource.SourceParticipantId,
            dischargeTarget.CombatantId,
            appliedDamage);
    }

    private bool TryFillSameFactionCandidates(
        TotemCombatantModel primaryTarget,
        out int factionId,
        out int candidateCount)
    {
        candidateCount = 0;
        if (primaryTarget is TotemActorModel participantTarget)
        {
            factionId = participantTarget.TeamId.Value;
            IReadOnlyList<TotemActorModel> actors = actorService?.Actors;
            for (int i = 0; actors != null && i < actors.Count && candidateCount < elementTargetBuffer.Length; i++)
            {
                TotemActorModel actor = actors[i];
                if (actor == null)
                {
                    continue;
                }

                elementTargetBuffer[candidateCount++] = new TotemElementTargetCandidate(
                    actor.CombatantId,
                    actor.TeamId.Value,
                    actor.Position,
                    actor.IsAlive);
            }

            return actors != null;
        }

        factionId = -1;
        return false;
    }

    private TotemCombatantModel FindCombatant(int combatantId)
    {
        IReadOnlyList<TotemActorModel> actors = actorService?.Actors;
        for (int i = 0; actors != null && i < actors.Count; i++)
        {
            if (actors[i]?.CombatantId == combatantId)
            {
                return actors[i];
            }
        }

        return null;
    }

    private TotemActorModel FindParticipant(TotemParticipantId participantId)
    {
        IReadOnlyList<TotemActorModel> actors = actorService?.Actors;
        for (int i = 0; actors != null && i < actors.Count; i++)
        {
            if (actors[i]?.ParticipantId == participantId.Value)
            {
                return actors[i];
            }
        }

        return null;
    }

    private float ResolveReactionDamage(
        TotemActorModel source,
        TotemCombatantModel target,
        TotemReactionKind reaction,
        float bodyHitDamage)
    {
        if (reaction == TotemReactionKind.Stasis || source == null || target == null || !target.IsAlive)
        {
            return 0f;
        }

        float totalApplied = ApplyIndirectDamage(
            source,
            target,
            TotemFirstPlayableElementRules.ResolveReactionCenterDamage(reaction, bodyHitDamage),
            reaction);
        if (reaction == TotemReactionKind.Overload && totalApplied > 0f)
        {
            ApplyOverloadKnockback(source, target);
        }
        if (reaction != TotemReactionKind.Overload)
        {
            return totalApplied;
        }

        float neighborDamage = Mathf.Max(0f, bodyHitDamage)
            * TotemFirstPlayableElementRules.OverloadNeighborDamageMultiplier;
        float radiusSqr = TotemFirstPlayableElementRules.OverloadRadius
            * TotemFirstPlayableElementRules.OverloadRadius;
        if (target is TotemActorModel participantTarget)
        {
            IReadOnlyList<TotemActorModel> actors = actorService?.Actors;
            if (actors == null)
            {
                return totalApplied;
            }

            for (int i = 0; i < actors.Count; i++)
            {
                TotemActorModel candidate = actors[i];
                if (candidate == null
                    || candidate == participantTarget
                    || !candidate.IsAlive
                    || candidate.TeamId != participantTarget.TeamId
                    || (candidate.Position - participantTarget.Position).sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                float neighborApplied = ApplyIndirectDamage(source, candidate, neighborDamage, reaction);
                totalApplied += neighborApplied;
                if (neighborApplied > 0f)
                {
                    ApplyOverloadKnockback(source, candidate);
                }
            }

            return totalApplied;
        }

        return totalApplied;
    }

    private float ApplyIndirectDamage(
        TotemActorModel source,
        TotemCombatantModel target,
        float amount,
        TotemReactionKind reaction,
        string reasonOverride = null)
    {
        if (source == null || target == null || !target.IsAlive || amount <= 0f)
        {
            return 0f;
        }

        string reason = string.IsNullOrWhiteSpace(reasonOverride) ? "Reaction:" + reaction : reasonOverride;
        if (target is TotemActorModel actorTarget)
        {
            return actorService != null
                && actorService.TryApplyDamage(actorTarget, amount, source, reason)
                && actorService.LastDamage.Target == actorTarget
                    ? Mathf.Max(0f, actorService.LastDamage.Amount)
                    : 0f;
        }

        return 0f;
    }

    private void ApplyOverloadKnockback(TotemActorModel source, TotemCombatantModel target)
    {
        if (source == null || target == null || !target.IsAlive)
        {
            return;
        }

        Vector3 direction = target.Position - source.Position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = (target.CombatantId & 1) == 0 ? Vector3.right : Vector3.left;
        }
        else
        {
            direction.Normalize();
        }

        Vector3 delta = direction * TotemFirstPlayableElementRules.OverloadKnockbackDistance;
        if (target is TotemActorModel actorTarget)
        {
            actorService?.MoveActor(actorTarget, delta);
        }
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

    private static TotemWeaponDefinition[] LoadWeaponCatalog()
    {
        return CreateFirstPlayableWeaponCatalog();
    }

    private static TotemWeaponDefinition[] CreateFirstPlayableWeaponCatalog()
    {
        return new[]
        {
            new TotemWeaponDefinition
            {
                WeaponId = DefaultWeaponId,
                DisplayName = "First Playable Rifle",
                BaseDamage = 16f,
                Cooldown = 0.5f,
                Range = 30f,
                AimSpreadHalfDegrees = 12f,
            },
        };
    }

    private static TotemGunAttackResult BuildSkippedGunAttack(string reason)
    {
        return new TotemGunAttackResult(
            fired: false,
            reason,
            weapon: null,
            TotemHitRegion.Body,
            default,
            killed: false,
            fireSequence: 0);
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            ResetRunState();
            if (actorService?.Player != null)
            {
                EquipWeapon(actorService.Player, DefaultWeaponId);
            }

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
        states.Clear();
        elementApplicationSequence = 0;
    }

}
