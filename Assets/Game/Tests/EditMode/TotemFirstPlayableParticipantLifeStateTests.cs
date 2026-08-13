using NUnit.Framework;

public sealed class TotemFirstPlayableParticipantLifeStateTests
{
    [Test]
    public void LethalHit_WithLivingTeammate_EntersFortyPercentDownedPool()
    {
        var state = new TotemFirstPlayableParticipantLifeState(100f);

        Assert.That(state.TryEnterDowned(true, new TotemParticipantId(4), out var transition), Is.True);
        Assert.That(transition.Reason, Is.EqualTo(TotemDownedTransitionReason.LethalDamage));
        Assert.That(state.LifeState, Is.EqualTo(TotemFirstPlayableLifeState.Downed));
        Assert.That(state.DownedHealth, Is.EqualTo(40f));
        Assert.That(state.BleedoutRemaining, Is.EqualTo(20f));
        Assert.That(state.MoveSpeedMultiplier, Is.EqualTo(0.35f));
        Assert.That(state.CanAttack, Is.False);
        Assert.That(state.CanBuild, Is.False);
    }

    [Test]
    public void LethalHit_WithoutLivingTeammate_EliminatesInsteadOfDowning()
    {
        var state = new TotemFirstPlayableParticipantLifeState(100f);

        Assert.That(state.TryEnterDowned(false, new TotemParticipantId(4), out _), Is.False);
        Assert.That(state.EliminateWithoutLivingTeammate(new TotemParticipantId(4), out var transition), Is.True);
        Assert.That(transition.Reason, Is.EqualTo(TotemDownedTransitionReason.TeamEliminated));
        Assert.That(state.LifeState, Is.EqualTo(TotemFirstPlayableLifeState.Eliminated));
    }

    [TestCase(TotemReviveContinuationStatus.OutOfRange, TotemDownedTransitionReason.ReviveCancelledOutOfRange)]
    [TestCase(TotemReviveContinuationStatus.ReviverControlled, TotemDownedTransitionReason.ReviveCancelledControlled)]
    [TestCase(TotemReviveContinuationStatus.ReviverDowned, TotemDownedTransitionReason.ReviveCancelledReviverDowned)]
    [TestCase(TotemReviveContinuationStatus.InteractionReleased, TotemDownedTransitionReason.ReviveCancelledInteraction)]
    public void ReviveInvalidation_ResetsProgress(
        TotemReviveContinuationStatus status,
        TotemDownedTransitionReason expectedReason)
    {
        var state = DownedState();
        state.TryBeginRevive(new TotemParticipantId(2), out _);
        state.ContinueRevive(2f, TotemReviveContinuationStatus.Valid, out _);

        Assert.That(state.ContinueRevive(0f, status, out var transition), Is.False);
        Assert.That(transition.Reason, Is.EqualTo(expectedReason));
        Assert.That(state.ReviveProgress, Is.Zero);
        Assert.That(state.ReviverParticipantId.IsValid, Is.False);
    }

    [Test]
    public void ThreeSecondRevive_RestoresThirtyPercentAndOneSecondProtection()
    {
        var state = DownedState();
        state.TryBeginRevive(new TotemParticipantId(2), out _);

        Assert.That(state.ContinueRevive(2.99f, TotemReviveContinuationStatus.Valid, out _), Is.True);
        Assert.That(state.IsDowned, Is.True);
        Assert.That(state.ContinueRevive(0.01f, TotemReviveContinuationStatus.Valid, out var transition), Is.True);
        Assert.That(transition.Reason, Is.EqualTo(TotemDownedTransitionReason.ReviveCompleted));
        Assert.That(state.LifeState, Is.EqualTo(TotemFirstPlayableLifeState.Alive));
        Assert.That(state.DownedHealth, Is.EqualTo(30f).Within(0.0001f));
        Assert.That(state.ProtectionRemaining, Is.EqualTo(1f));
        state.Advance(1f, gameplaySuspended: false, out _);
        Assert.That(state.IsProtected, Is.False);
    }

    [Test]
    public void Bleedout_PausesDuringSuspensionAndEliminatesAtTwentySeconds()
    {
        var state = DownedState();

        state.Advance(100f, gameplaySuspended: true, out _);
        Assert.That(state.BleedoutRemaining, Is.EqualTo(20f));
        Assert.That(state.Advance(19.99f, gameplaySuspended: false, out _), Is.False);
        Assert.That(state.Advance(0.01f, gameplaySuspended: false, out var transition), Is.True);
        Assert.That(transition.Reason, Is.EqualTo(TotemDownedTransitionReason.BledOut));
        Assert.That(transition.InstigatorParticipantId, Is.EqualTo(new TotemParticipantId(4)));
    }

    [Test]
    public void OpponentExecution_UsesLastEffectiveDamageSourceAndOnlyConsumesDownedPool()
    {
        var state = DownedState();

        Assert.That(state.ApplyDownedDamage(10f, new TotemParticipantId(5), out float first, out _), Is.True);
        Assert.That(first, Is.EqualTo(10f));
        Assert.That(state.ApplyDownedDamage(100f, new TotemParticipantId(6), out float second, out var transition), Is.True);
        Assert.That(second, Is.EqualTo(30f));
        Assert.That(transition.Reason, Is.EqualTo(TotemDownedTransitionReason.Executed));
        Assert.That(transition.InstigatorParticipantId, Is.EqualTo(new TotemParticipantId(6)));
    }

    [Test]
    public void BuildBoundary_ImmediatelyEliminatesAndClearsReviveProgress()
    {
        var state = DownedState();
        state.TryBeginRevive(new TotemParticipantId(2), out _);
        state.ContinueRevive(2.9f, TotemReviveContinuationStatus.Valid, out _);

        Assert.That(state.EliminateAtBuildBoundary(out var transition), Is.True);
        Assert.That(transition.Reason, Is.EqualTo(TotemDownedTransitionReason.BuildBoundary));
        Assert.That(state.IsEliminated, Is.True);
        Assert.That(state.ReviveProgress, Is.Zero);
    }

    [Test]
    public void EliminatedParticipant_SpectatesLivingTeammateOtherwiseWaitsForResult()
    {
        var state = DownedState();
        state.EliminateAtBuildBoundary(out _);

        Assert.That(state.ResolveSpectatorState(true), Is.EqualTo(TotemSpectatorState.SpectatingTeammate));
        Assert.That(state.ResolveSpectatorState(false), Is.EqualTo(TotemSpectatorState.WaitingForResult));
    }

    [Test]
    public void LastActiveTeammateEliminated_DownedTeammateTransitionsToTeamEliminated()
    {
        var state = DownedState();
        var finalSource = new TotemCombatantReference(TotemCombatantDomain.Participant, 5);

        Assert.That(state.EliminateForTeamWipe(finalSource, out var transition), Is.True);
        Assert.That(transition.Reason, Is.EqualTo(TotemDownedTransitionReason.TeamEliminated));
        Assert.That(transition.Instigator, Is.EqualTo(finalSource));
        Assert.That(state.IsEliminated, Is.True);
        Assert.That(state.ResolveSpectatorState(false), Is.EqualTo(TotemSpectatorState.WaitingForResult));
    }

    [Test]
    public void CombatRelationship_AllowsTargetingDownedButBlocksDownedAsDamageSource()
    {
        TotemActorModel active = Actor(1, 0);
        TotemActorModel downed = Actor(3, 1);
        downed.SetLifecycle(TotemParticipantLifecycle.Downed, "Test");

        TotemCombatRelationshipDecision targetDecision = TotemCombatRelationshipService.Evaluate(
            active,
            downed,
            new TotemCombatRelationshipContext(0f));
        TotemCombatRelationshipDecision sourceDecision = TotemCombatRelationshipService.Evaluate(
            downed,
            active,
            new TotemCombatRelationshipContext(0f));

        Assert.That(targetDecision.Allowed, Is.True);
        Assert.That(sourceDecision.Allowed, Is.False);
        Assert.That(sourceDecision.Reason, Is.EqualTo(TotemCombatRelationshipReason.BlockedSourceInactive));

    }

    [TestCase(TotemGameplayCommandSource.HumanInput)]
    [TestCase(TotemGameplayCommandSource.BotDecision)]
    public void ReviveCommand_HumanAndBotShareTargetValidation(TotemGameplayCommandSource source)
    {
        var begin = new TotemGameplayCommand(
            new TotemParticipantId(1),
            source,
            TotemGameplayCommandType.BeginRevive,
            10,
            UnityEngine.Vector3.zero,
            intValue: 2);
        var selfTarget = new TotemGameplayCommand(
            new TotemParticipantId(1),
            source,
            TotemGameplayCommandType.BeginRevive,
            11,
            UnityEngine.Vector3.zero,
            intValue: 1);

        Assert.That(
            TotemReviveCommandCodec.TryDecodeTarget(
                begin,
                TotemGameplayCommandType.BeginRevive,
                out TotemParticipantId target),
            Is.True);
        Assert.That(target, Is.EqualTo(new TotemParticipantId(2)));
        Assert.That(
            TotemReviveCommandCodec.TryDecodeTarget(
                selfTarget,
                TotemGameplayCommandType.BeginRevive,
                out _),
            Is.False);
    }

    [Test]
    public void ReviveRange_UsesExistingThreeMeterInteractionBoundary()
    {
        TotemActorModel reviver = Actor(1, 0);
        TotemActorModel target = Actor(2, 0);

        target.Position = new UnityEngine.Vector3(3f, 0f, 0f);
        Assert.That(TotemFirstPlayableLifecycleService.ReviveInteractRadius, Is.EqualTo(3f));
        Assert.That(TotemFirstPlayableLifecycleService.IsWithinReviveRange(reviver, target), Is.True);

        target.Position = new UnityEngine.Vector3(3.001f, 0f, 0f);
        Assert.That(TotemFirstPlayableLifecycleService.IsWithinReviveRange(reviver, target), Is.False);
    }

    private static TotemFirstPlayableParticipantLifeState DownedState()
    {
        var state = new TotemFirstPlayableParticipantLifeState(100f);
        state.TryEnterDowned(true, new TotemParticipantId(4), out _);
        return state;
    }

    private static TotemActorModel Actor(int participantId, int teamId)
    {
        return new TotemActorModel(new TotemActorSpawnInfo
        {
            ActorId = participantId,
            TeamId = teamId,
            Name = "P" + participantId,
            Kind = TotemActorKind.Player,
            Position = UnityEngine.Vector3.zero,
            MaxHealth = 100f,
        });
    }
}
