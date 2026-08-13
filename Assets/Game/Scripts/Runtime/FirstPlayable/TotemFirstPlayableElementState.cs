using UnityEngine;

public static class TotemFirstPlayableElementRules
{
    public const float LayerDecaySeconds = 3f;
    public const float FireTickSeconds = 0.5f;
    public const float FireBaseTickDamage = 1f;
    public const float LightningDischargeIntervalSeconds = 0.5f;
    public const float OverloadRadius = 3f;
    public const float HeatShockDamageMultiplier = 0.6f;
    public const float OverloadCenterDamageMultiplier = 0.35f;
    public const float OverloadNeighborDamageMultiplier = 0.25f;
    public const float LightningSelfReturnMultiplier = 0.5f;
    public const float StasisDirectDamageMultiplier = 0.8f;
    public const float StasisDurationSeconds = 2f;
    public const float PatternNeighborSpreadRadius = 3f;
    public const float OverloadKnockbackDistance = 0.5f;

    public static float ResolveFireTierMultiplier(TotemElementTier tier)
    {
        switch (tier)
        {
            case TotemElementTier.Weak: return 1f;
            case TotemElementTier.Standard: return 1.25f;
            case TotemElementTier.Strong: return 1.5f;
            default: return 0f;
        }
    }

    public static float ResolveIceSlowRatio(TotemElementTier tier)
    {
        switch (tier)
        {
            case TotemElementTier.Weak: return 0.12f;
            case TotemElementTier.Standard: return 0.20f;
            case TotemElementTier.Strong: return 0.28f;
            default: return 0f;
        }
    }

    public static TotemReactionKind ResolveReaction(
        TotemFirstPlayableElement first,
        TotemFirstPlayableElement second)
    {
        if (first == second || first == TotemFirstPlayableElement.None || second == TotemFirstPlayableElement.None)
        {
            return TotemReactionKind.None;
        }

        bool fireIce = IsPair(first, second, TotemFirstPlayableElement.Fire, TotemFirstPlayableElement.Ice);
        if (fireIce)
        {
            return TotemReactionKind.HeatShock;
        }

        bool fireLightning = IsPair(first, second, TotemFirstPlayableElement.Fire, TotemFirstPlayableElement.Lightning);
        if (fireLightning)
        {
            return TotemReactionKind.Overload;
        }

        return IsPair(first, second, TotemFirstPlayableElement.Ice, TotemFirstPlayableElement.Lightning)
            ? TotemReactionKind.Stasis
            : TotemReactionKind.None;
    }

    public static float ResolveReactionCenterDamage(TotemReactionKind reaction, float bodyHitDamage)
    {
        float damage = Mathf.Max(0f, bodyHitDamage);
        switch (reaction)
        {
            case TotemReactionKind.HeatShock:
                return damage * HeatShockDamageMultiplier;
            case TotemReactionKind.Overload:
                return damage * OverloadCenterDamageMultiplier;
            default:
                return 0f;
        }
    }

    private static bool IsPair(
        TotemFirstPlayableElement first,
        TotemFirstPlayableElement second,
        TotemFirstPlayableElement left,
        TotemFirstPlayableElement right)
    {
        return (first == left && second == right) || (first == right && second == left);
    }
}

public static class TotemTattooPatternTargetResolver
{
    public static int ResolveSecondaryTarget(
        TotemFirstPlayablePatternId pattern,
        int primaryCombatantId,
        int primaryFactionId,
        Vector3 primaryPosition,
        TotemElementTargetCandidate[] candidates,
        int candidateCount,
        float maxDistance = TotemFirstPlayableElementRules.PatternNeighborSpreadRadius)
    {
        if (pattern != TotemFirstPlayablePatternId.P02 || primaryCombatantId <= 0 || maxDistance <= 0f)
        {
            return 0;
        }

        int bestTarget = 0;
        float bestSqrDistance = maxDistance * maxDistance;
        int count = candidates == null ? 0 : Mathf.Min(Mathf.Max(0, candidateCount), candidates.Length);
        for (int i = 0; i < count; i++)
        {
            TotemElementTargetCandidate candidate = candidates[i];
            if (!candidate.Eligible
                || candidate.CombatantId <= 0
                || candidate.CombatantId == primaryCombatantId
                || candidate.FactionId != primaryFactionId)
            {
                continue;
            }

            float sqrDistance = (candidate.Position - primaryPosition).sqrMagnitude;
            if (sqrDistance > bestSqrDistance)
            {
                continue;
            }

            if (bestTarget == 0
                || sqrDistance < bestSqrDistance
                || (Mathf.Approximately(sqrDistance, bestSqrDistance) && candidate.CombatantId < bestTarget))
            {
                bestTarget = candidate.CombatantId;
                bestSqrDistance = sqrDistance;
            }
        }

        return bestTarget;
    }
}

public readonly struct TotemElementApplyResult
{
    public TotemElementApplyResult(
        bool applied,
        bool refreshedStrong,
        TotemReactionKind reaction,
        TotemFirstPlayableElement retainedElement,
        TotemElementTier retainedTier,
        TotemReactionAttribution attribution)
    {
        Applied = applied;
        RefreshedStrong = refreshedStrong;
        Reaction = reaction;
        RetainedElement = retainedElement;
        RetainedTier = retainedTier;
        Attribution = attribution;
    }

    public bool Applied { get; }
    public bool RefreshedStrong { get; }
    public TotemReactionKind Reaction { get; }
    public TotemFirstPlayableElement RetainedElement { get; }
    public TotemElementTier RetainedTier { get; }
    public TotemReactionAttribution Attribution { get; }
    public bool TriggeredReaction => Reaction != TotemReactionKind.None;
}

public readonly struct TotemElementAdvanceResult
{
    public TotemElementAdvanceResult(int fireTickCount, float fireTierMultiplier, int decayedLayerCount)
    {
        FireTickCount = fireTickCount;
        FireTierMultiplier = fireTierMultiplier;
        DecayedLayerCount = decayedLayerCount;
    }

    public int FireTickCount { get; }
    public float FireTierMultiplier { get; }
    public int DecayedLayerCount { get; }
}

public readonly struct TotemElementTargetCandidate
{
    public TotemElementTargetCandidate(int combatantId, int factionId, Vector3 position, bool eligible = true)
    {
        CombatantId = combatantId;
        FactionId = factionId;
        Position = position;
        Eligible = eligible;
    }

    public int CombatantId { get; }
    public int FactionId { get; }
    public Vector3 Position { get; }
    public bool Eligible { get; }
}

public readonly struct TotemLightningDischargeResult
{
    public TotemLightningDischargeResult(int targetCombatantId, bool returnedToSelf, float damageMultiplier)
    {
        TargetCombatantId = targetCombatantId;
        ReturnedToSelf = returnedToSelf;
        DamageMultiplier = damageMultiplier;
    }

    public int TargetCombatantId { get; }
    public bool ReturnedToSelf { get; }
    public float DamageMultiplier { get; }
}

public static class TotemLightningDischargeResolver
{
    public static TotemLightningDischargeResult Resolve(
        int primaryCombatantId,
        int primaryFactionId,
        Vector3 primaryPosition,
        TotemElementTargetCandidate[] candidates,
        int candidateCount)
    {
        int bestTarget = 0;
        float bestSqrDistance = float.PositiveInfinity;
        int count = candidates == null ? 0 : Mathf.Min(Mathf.Max(0, candidateCount), candidates.Length);
        for (int i = 0; i < count; i++)
        {
            TotemElementTargetCandidate candidate = candidates[i];
            if (!candidate.Eligible
                || candidate.CombatantId <= 0
                || candidate.CombatantId == primaryCombatantId
                || candidate.FactionId != primaryFactionId)
            {
                continue;
            }

            float sqrDistance = (candidate.Position - primaryPosition).sqrMagnitude;
            if (sqrDistance < bestSqrDistance
                || (Mathf.Approximately(sqrDistance, bestSqrDistance) && candidate.CombatantId < bestTarget))
            {
                bestTarget = candidate.CombatantId;
                bestSqrDistance = sqrDistance;
            }
        }

        return bestTarget > 0
            ? new TotemLightningDischargeResult(bestTarget, false, 1f)
            : new TotemLightningDischargeResult(
                primaryCombatantId,
                true,
                TotemFirstPlayableElementRules.LightningSelfReturnMultiplier);
    }
}

/// <summary>
/// Fixed-capacity, allocation-free elemental state for one combat target.
/// It intentionally terminates reactions: applying a different element consumes
/// the new application and one FIFO layer of the retained element.
/// </summary>
public sealed class TotemFirstPlayableElementState
{
    private readonly TotemParticipantId[] layerSources = new TotemParticipantId[3];
    private readonly int[] applicationSequences = new int[3];
    private TotemFirstPlayableElement element;
    private int layerCount;
    private float decayRemaining;
    private float fireTickRemaining;
    private float lightningDischargeRemaining;
    private float stasisRemaining;

    public TotemFirstPlayableElement Element => element;
    public TotemElementTier Tier => (TotemElementTier)layerCount;
    public int LayerCount => layerCount;
    public float DecayRemaining => decayRemaining;
    public bool HasElement => layerCount > 0 && element != TotemFirstPlayableElement.None;
    public float IceSlowRatio => element == TotemFirstPlayableElement.Ice
        ? TotemFirstPlayableElementRules.ResolveIceSlowRatio(Tier)
        : 0f;
    public float StasisRemaining => stasisRemaining;
    public bool HasStasis => stasisRemaining > 0f;

    public TotemElementApplyResult Apply(
        TotemFirstPlayableElement incomingElement,
        TotemParticipantId sourceParticipantId,
        int applicationSequence,
        float bodyHitDamage)
    {
        if (incomingElement == TotemFirstPlayableElement.None
            || !sourceParticipantId.IsValid
            || applicationSequence < 0)
        {
            return new TotemElementApplyResult(false, false, TotemReactionKind.None, element, Tier, default);
        }

        if (!HasElement || incomingElement == element)
        {
            bool refreshedStrong = layerCount == 3;
            if (!refreshedStrong)
            {
                layerSources[layerCount] = sourceParticipantId;
                applicationSequences[layerCount] = applicationSequence;
                layerCount++;
            }

            element = incomingElement;
            decayRemaining = TotemFirstPlayableElementRules.LayerDecaySeconds;
            if (element == TotemFirstPlayableElement.Fire && fireTickRemaining <= 0f)
            {
                fireTickRemaining = TotemFirstPlayableElementRules.FireTickSeconds;
            }
            return new TotemElementApplyResult(true, refreshedStrong, TotemReactionKind.None, element, Tier, default);
        }

        TotemReactionKind reaction = TotemFirstPlayableElementRules.ResolveReaction(element, incomingElement);
        if (reaction == TotemReactionKind.None)
        {
            return new TotemElementApplyResult(false, false, TotemReactionKind.None, element, Tier, default);
        }

        TotemParticipantId assistingSource = layerSources[0];
        float reactionDamage = TotemFirstPlayableElementRules.ResolveReactionCenterDamage(reaction, bodyHitDamage);
        var attribution = new TotemReactionAttribution(
            reaction,
            sourceParticipantId,
            assistingSource,
            reactionDamage);
        if (reaction == TotemReactionKind.Stasis)
        {
            stasisRemaining = TotemFirstPlayableElementRules.StasisDurationSeconds;
        }
        ConsumeOldestLayer();
        return new TotemElementApplyResult(true, false, reaction, element, Tier, attribution);
    }

    public TotemElementAdvanceResult Advance(float deltaTime, bool gameplaySuspended)
    {
        if (gameplaySuspended || deltaTime <= 0f)
        {
            return default;
        }

        int fireTicks = 0;
        float fireMultiplier = 0f;
        int decayCount = 0;
        float remaining = deltaTime;
        while (remaining > 0f && HasElement)
        {
            bool isFire = element == TotemFirstPlayableElement.Fire;
            float step = Mathf.Min(remaining, decayRemaining);
            if (isFire)
            {
                step = Mathf.Min(step, fireTickRemaining);
            }

            decayRemaining -= step;
            if (isFire)
            {
                fireTickRemaining -= step;
            }
            remaining -= step;

            // When a tick and a layer decay share the same timestamp, the tick
            // observes the pre-decay tier. This keeps hitch recovery identical
            // to advancing the same duration in normal frame-sized slices.
            if (isFire && fireTickRemaining <= 0f)
            {
                fireTicks++;
                fireMultiplier += TotemFirstPlayableElementRules.ResolveFireTierMultiplier(Tier);
                fireTickRemaining += TotemFirstPlayableElementRules.FireTickSeconds;
            }

            if (decayRemaining <= 0f && HasElement)
            {
                ConsumeOldestLayer();
                decayCount++;
                if (HasElement)
                {
                    decayRemaining = TotemFirstPlayableElementRules.LayerDecaySeconds;
                }
            }
        }

        lightningDischargeRemaining = Mathf.Max(0f, lightningDischargeRemaining - deltaTime);
        stasisRemaining = Mathf.Max(0f, stasisRemaining - deltaTime);
        return new TotemElementAdvanceResult(fireTicks, fireMultiplier, decayCount);
    }

    public bool TryBeginLightningDischarge(bool effectiveDirectDamage)
    {
        if (!effectiveDirectDamage
            || element != TotemFirstPlayableElement.Lightning
            || !HasElement
            || lightningDischargeRemaining > 0f)
        {
            return false;
        }

        lightningDischargeRemaining = TotemFirstPlayableElementRules.LightningDischargeIntervalSeconds;
        return true;
    }

    public float ApplyStasisDirectDamageModifier(float directDamage)
    {
        float damage = Mathf.Max(0f, directDamage);
        return HasStasis ? damage * TotemFirstPlayableElementRules.StasisDirectDamageMultiplier : damage;
    }

    public int AdvanceDecay(float deltaTime, bool gameplaySuspended)
    {
        if (gameplaySuspended || deltaTime <= 0f || !HasElement)
        {
            return 0;
        }

        int consumed = 0;
        float remainingDelta = deltaTime;
        while (HasElement && remainingDelta >= decayRemaining)
        {
            remainingDelta -= decayRemaining;
            ConsumeOldestLayer();
            consumed++;
            if (HasElement)
            {
                decayRemaining = TotemFirstPlayableElementRules.LayerDecaySeconds;
            }
        }

        if (HasElement)
        {
            decayRemaining = Mathf.Max(0f, decayRemaining - remainingDelta);
        }

        return consumed;
    }

    public bool TryGetLayerSource(int fifoIndex, out TotemElementLayerSource source)
    {
        if (fifoIndex < 0 || fifoIndex >= layerCount)
        {
            source = default;
            return false;
        }

        source = new TotemElementLayerSource(
            element,
            layerSources[fifoIndex],
            applicationSequences[fifoIndex],
            decayRemaining);
        return true;
    }

    public void Clear()
    {
        element = TotemFirstPlayableElement.None;
        layerCount = 0;
        decayRemaining = 0f;
        fireTickRemaining = 0f;
        lightningDischargeRemaining = 0f;
        stasisRemaining = 0f;
        for (int i = 0; i < layerSources.Length; i++)
        {
            layerSources[i] = default;
            applicationSequences[i] = 0;
        }
    }

    private void ConsumeOldestLayer()
    {
        if (layerCount <= 0)
        {
            Clear();
            return;
        }

        for (int i = 1; i < layerCount; i++)
        {
            layerSources[i - 1] = layerSources[i];
            applicationSequences[i - 1] = applicationSequences[i];
        }

        layerCount--;
        layerSources[layerCount] = default;
        applicationSequences[layerCount] = 0;
        if (layerCount == 0)
        {
            element = TotemFirstPlayableElement.None;
            decayRemaining = 0f;
            fireTickRemaining = 0f;
        }
    }
}
