using NUnit.Framework;

public sealed class TotemFirstPlayableElementStateTests
{
    [Test]
    public void SameElement_RisesWeakStandardStrongAndStrongOnlyRefreshes()
    {
        var state = new TotemFirstPlayableElementState();

        Assert.That(state.Apply(TotemFirstPlayableElement.Fire, new TotemParticipantId(1), 1, 10f).Applied, Is.True);
        Assert.That(state.Tier, Is.EqualTo(TotemElementTier.Weak));
        state.AdvanceDecay(2f, gameplaySuspended: false);
        Assert.That(state.Apply(TotemFirstPlayableElement.Fire, new TotemParticipantId(2), 2, 10f).Applied, Is.True);
        Assert.That(state.Tier, Is.EqualTo(TotemElementTier.Standard));
        Assert.That(state.DecayRemaining, Is.EqualTo(3f));
        state.Apply(TotemFirstPlayableElement.Fire, new TotemParticipantId(3), 3, 10f);
        TotemElementApplyResult refreshed = state.Apply(TotemFirstPlayableElement.Fire, new TotemParticipantId(4), 4, 10f);

        Assert.That(state.Tier, Is.EqualTo(TotemElementTier.Strong));
        Assert.That(refreshed.RefreshedStrong, Is.True);
        Assert.That(state.DecayRemaining, Is.EqualTo(3f));
        Assert.That(state.TryGetLayerSource(2, out var newestStored), Is.True);
        Assert.That(newestStored.SourceParticipantId, Is.EqualTo(new TotemParticipantId(3)));
    }

    [Test]
    public void Decay_ConsumesOneFifoLayerEveryThreeSecondsAndPauses()
    {
        var state = ThreeLayerState(TotemFirstPlayableElement.Ice);

        Assert.That(state.AdvanceDecay(20f, gameplaySuspended: true), Is.Zero);
        Assert.That(state.Tier, Is.EqualTo(TotemElementTier.Strong));
        Assert.That(state.AdvanceDecay(6.1f, gameplaySuspended: false), Is.EqualTo(2));
        Assert.That(state.Tier, Is.EqualTo(TotemElementTier.Weak));
        Assert.That(state.TryGetLayerSource(0, out var remaining), Is.True);
        Assert.That(remaining.SourceParticipantId, Is.EqualTo(new TotemParticipantId(3)));
        Assert.That(state.AdvanceDecay(2.9f, gameplaySuspended: false), Is.EqualTo(1));
        Assert.That(state.Element, Is.EqualTo(TotemFirstPlayableElement.None));
    }

    [TestCase(TotemFirstPlayableElement.Fire, TotemFirstPlayableElement.Ice, TotemReactionKind.HeatShock, 6f)]
    [TestCase(TotemFirstPlayableElement.Ice, TotemFirstPlayableElement.Fire, TotemReactionKind.HeatShock, 6f)]
    [TestCase(TotemFirstPlayableElement.Fire, TotemFirstPlayableElement.Lightning, TotemReactionKind.Overload, 3.5f)]
    [TestCase(TotemFirstPlayableElement.Lightning, TotemFirstPlayableElement.Fire, TotemReactionKind.Overload, 3.5f)]
    [TestCase(TotemFirstPlayableElement.Ice, TotemFirstPlayableElement.Lightning, TotemReactionKind.Stasis, 0f)]
    [TestCase(TotemFirstPlayableElement.Lightning, TotemFirstPlayableElement.Ice, TotemReactionKind.Stasis, 0f)]
    public void Reaction_IsOrderIndependentAndConsumesIncomingAndOneExistingLayer(
        TotemFirstPlayableElement existing,
        TotemFirstPlayableElement incoming,
        TotemReactionKind expected,
        float expectedDamage)
    {
        var state = new TotemFirstPlayableElementState();
        state.Apply(existing, new TotemParticipantId(1), 10, 10f);
        TotemElementApplyResult result = state.Apply(incoming, new TotemParticipantId(2), 11, 10f);

        Assert.That(result.Reaction, Is.EqualTo(expected));
        Assert.That(result.Attribution.TriggerParticipantId, Is.EqualTo(new TotemParticipantId(2)));
        Assert.That(result.Attribution.AssistingParticipantId, Is.EqualTo(new TotemParticipantId(1)));
        Assert.That(result.Attribution.KillOwner, Is.EqualTo(new TotemParticipantId(2)));
        Assert.That(result.Attribution.IndirectElementDamage, Is.EqualTo(expectedDamage).Within(0.001f));
        Assert.That(state.HasElement, Is.False);
    }

    [Test]
    public void ReactionFromMultipleLayers_ConsumesOldestSourceFirstAndDoesNotStoreIncoming()
    {
        var state = new TotemFirstPlayableElementState();
        state.Apply(TotemFirstPlayableElement.Fire, new TotemParticipantId(3), 1, 20f);
        state.Apply(TotemFirstPlayableElement.Fire, new TotemParticipantId(4), 2, 20f);

        TotemElementApplyResult reaction = state.Apply(
            TotemFirstPlayableElement.Ice,
            new TotemParticipantId(5),
            3,
            20f);

        Assert.That(reaction.Attribution.AssistingParticipantId, Is.EqualTo(new TotemParticipantId(3)));
        Assert.That(state.Element, Is.EqualTo(TotemFirstPlayableElement.Fire));
        Assert.That(state.Tier, Is.EqualTo(TotemElementTier.Weak));
        Assert.That(state.TryGetLayerSource(0, out var remaining), Is.True);
        Assert.That(remaining.SourceParticipantId, Is.EqualTo(new TotemParticipantId(4)));
    }

    [Test]
    public void FrozenElementCoefficients_MatchFirstPlayableSpec()
    {
        Assert.That(TotemFirstPlayableElementRules.ResolveFireTierMultiplier(TotemElementTier.Weak), Is.EqualTo(1f));
        Assert.That(TotemFirstPlayableElementRules.ResolveFireTierMultiplier(TotemElementTier.Standard), Is.EqualTo(1.25f));
        Assert.That(TotemFirstPlayableElementRules.ResolveFireTierMultiplier(TotemElementTier.Strong), Is.EqualTo(1.5f));
        Assert.That(TotemFirstPlayableElementRules.ResolveIceSlowRatio(TotemElementTier.Weak), Is.EqualTo(0.12f));
        Assert.That(TotemFirstPlayableElementRules.ResolveIceSlowRatio(TotemElementTier.Standard), Is.EqualTo(0.20f));
        Assert.That(TotemFirstPlayableElementRules.ResolveIceSlowRatio(TotemElementTier.Strong), Is.EqualTo(0.28f));
        Assert.That(TotemFirstPlayableElementRules.FireTickSeconds, Is.EqualTo(0.5f));
        Assert.That(TotemFirstPlayableElementRules.FireBaseTickDamage, Is.EqualTo(1f));
        Assert.That(TotemFirstPlayableElementRules.LightningDischargeIntervalSeconds, Is.EqualTo(0.5f));
        Assert.That(TotemFirstPlayableElementRules.OverloadKnockbackDistance, Is.GreaterThan(0f));
    }

    [Test]
    public void FireTicksEveryHalfSecondAndUsesCurrentTierMultiplier()
    {
        var state = ThreeLayerState(TotemFirstPlayableElement.Fire);

        TotemElementAdvanceResult beforeTick = state.Advance(0.49f, gameplaySuspended: false);
        TotemElementAdvanceResult tick = state.Advance(0.01f, gameplaySuspended: false);
        TotemElementAdvanceResult paused = state.Advance(5f, gameplaySuspended: true);

        Assert.That(beforeTick.FireTickCount, Is.Zero);
        Assert.That(tick.FireTickCount, Is.EqualTo(1));
        Assert.That(tick.FireTierMultiplier, Is.EqualTo(1.5f));
        Assert.That(paused.FireTickCount, Is.Zero);
    }

    [Test]
    public void LargeAdvance_MatchesFrameSlicesAcrossFireTicksAndLayerDecay()
    {
        var largeStep = ThreeLayerState(TotemFirstPlayableElement.Fire);
        var sliced = ThreeLayerState(TotemFirstPlayableElement.Fire);

        TotemElementAdvanceResult largeResult = largeStep.Advance(6.1f, gameplaySuspended: false);
        int slicedTicks = 0;
        float slicedMultiplier = 0f;
        int slicedDecay = 0;
        for (int i = 0; i < 61; i++)
        {
            TotemElementAdvanceResult frame = sliced.Advance(0.1f, gameplaySuspended: false);
            slicedTicks += frame.FireTickCount;
            slicedMultiplier += frame.FireTierMultiplier;
            slicedDecay += frame.DecayedLayerCount;
        }

        Assert.That(largeResult.FireTickCount, Is.EqualTo(slicedTicks));
        Assert.That(largeResult.FireTierMultiplier, Is.EqualTo(slicedMultiplier).Within(0.001f));
        Assert.That(largeResult.DecayedLayerCount, Is.EqualTo(slicedDecay));
        Assert.That(largeStep.Tier, Is.EqualTo(sliced.Tier));
        Assert.That(largeStep.DecayRemaining, Is.EqualTo(sliced.DecayRemaining).Within(0.001f));
    }

    [Test]
    public void IceSlowTracksTierAndDisappearsWithElement()
    {
        var state = ThreeLayerState(TotemFirstPlayableElement.Ice);
        Assert.That(state.IceSlowRatio, Is.EqualTo(0.28f));

        state.AdvanceDecay(3f, gameplaySuspended: false);
        Assert.That(state.IceSlowRatio, Is.EqualTo(0.20f));
        state.Clear();
        Assert.That(state.IceSlowRatio, Is.Zero);
    }

    [Test]
    public void LightningDischargeUsesPerTargetIntervalAndNearestSameFactionTarget()
    {
        var state = new TotemFirstPlayableElementState();
        state.Apply(TotemFirstPlayableElement.Lightning, new TotemParticipantId(1), 1, 10f);

        Assert.That(state.TryBeginLightningDischarge(effectiveDirectDamage: true), Is.True);
        Assert.That(state.TryBeginLightningDischarge(effectiveDirectDamage: true), Is.False);
        state.Advance(0.5f, gameplaySuspended: false);
        Assert.That(state.TryBeginLightningDischarge(effectiveDirectDamage: true), Is.True);

        var candidates = new[]
        {
            new TotemElementTargetCandidate(11, 2, new UnityEngine.Vector3(5f, 0f, 0f)),
            new TotemElementTargetCandidate(12, 2, new UnityEngine.Vector3(2f, 0f, 0f)),
            new TotemElementTargetCandidate(13, 3, new UnityEngine.Vector3(1f, 0f, 0f)),
        };
        TotemLightningDischargeResult nearest = TotemLightningDischargeResolver.Resolve(
            10,
            2,
            UnityEngine.Vector3.zero,
            candidates,
            candidates.Length);
        Assert.That(nearest.TargetCombatantId, Is.EqualTo(12));
        Assert.That(nearest.ReturnedToSelf, Is.False);

        TotemLightningDischargeResult self = TotemLightningDischargeResolver.Resolve(
            10,
            9,
            UnityEngine.Vector3.zero,
            candidates,
            candidates.Length);
        Assert.That(self.TargetCombatantId, Is.EqualTo(10));
        Assert.That(self.ReturnedToSelf, Is.True);
        Assert.That(self.DamageMultiplier, Is.EqualTo(0.5f));
    }

    [Test]
    public void StasisReducesDirectDamageForTwoSecondsAndPauses()
    {
        var state = new TotemFirstPlayableElementState();
        state.Apply(TotemFirstPlayableElement.Ice, new TotemParticipantId(1), 1, 10f);
        state.Apply(TotemFirstPlayableElement.Lightning, new TotemParticipantId(2), 2, 10f);

        Assert.That(state.ApplyStasisDirectDamageModifier(100f), Is.EqualTo(80f));
        state.Advance(10f, gameplaySuspended: true);
        Assert.That(state.ApplyStasisDirectDamageModifier(100f), Is.EqualTo(80f));
        state.Advance(2f, gameplaySuspended: false);
        Assert.That(state.ApplyStasisDirectDamageModifier(100f), Is.EqualTo(100f));
    }

    [Test]
    public void RuntimeService_ExposesIceMovementStasisDamageAndFireTickBatches()
    {
        var service = new TotemFirstPlayableElementService();
        service.ApplyElement(20, TotemFirstPlayableElement.Ice, new TotemParticipantId(1), 1, 10f);
        service.ApplyElement(20, TotemFirstPlayableElement.Ice, new TotemParticipantId(1), 2, 10f);
        service.ApplyElement(20, TotemFirstPlayableElement.Ice, new TotemParticipantId(1), 3, 10f);
        Assert.That(service.GetMoveSpeedMultiplier(20), Is.EqualTo(0.72f).Within(0.001f));

        service.ApplyElement(21, TotemFirstPlayableElement.Ice, new TotemParticipantId(1), 4, 10f);
        service.ApplyElement(21, TotemFirstPlayableElement.Lightning, new TotemParticipantId(2), 5, 10f);
        Assert.That(service.ModifyDirectDamage(21, 100f), Is.EqualTo(80f));

        int tickTarget = 0;
        int tickCount = 0;
        float tickMultiplier = 0f;
        service.FireTicksReady += (target, count, multiplier) =>
        {
            tickTarget = target;
            tickCount = count;
            tickMultiplier = multiplier;
        };
        service.ApplyElement(22, TotemFirstPlayableElement.Fire, new TotemParticipantId(3), 6, 10f);
        service.Tick(0.5f);
        Assert.That(tickTarget, Is.EqualTo(22));
        Assert.That(tickCount, Is.EqualTo(1));
        Assert.That(tickMultiplier, Is.EqualTo(1f));
    }

    private static TotemFirstPlayableElementState ThreeLayerState(TotemFirstPlayableElement element)
    {
        var state = new TotemFirstPlayableElementState();
        state.Apply(element, new TotemParticipantId(1), 1, 1f);
        state.Apply(element, new TotemParticipantId(2), 2, 1f);
        state.Apply(element, new TotemParticipantId(3), 3, 1f);
        return state;
    }
}
