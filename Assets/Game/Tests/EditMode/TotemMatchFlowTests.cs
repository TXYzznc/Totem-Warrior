using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class TotemMatchFlowTests
{
    [Test]
    public void FastMode_CompletesExactFiveRoundSequenceAndStopsAtResult()
    {
        var flow = new TotemMatchFlowService();
        var phases = new List<TotemMatchPhase>();
        var activities = new List<TotemMatchActivity>();
        flow.PhaseChanged += (_, next) => phases.Add(next);
        flow.ActivityChanged += (_, next) => activities.Add(next);

        flow.Configure(new TotemMatchTimingConfig(), useFastMode: true);
        flow.BeginMatch(useFastMode: true);
        flow.Advance(60f + 5f * 60f + 4f * 45f + 4f * 10f);

        CollectionAssert.AreEqual(new[]
        {
            TotemMatchPhase.OpeningBuild,
            TotemMatchPhase.Round1Combat,
            TotemMatchPhase.Build2,
            TotemMatchPhase.Round2Combat,
            TotemMatchPhase.Build3,
            TotemMatchPhase.Round3Combat,
            TotemMatchPhase.Build4,
            TotemMatchPhase.Round4Combat,
            TotemMatchPhase.Build5,
            TotemMatchPhase.Round5Combat,
            TotemMatchPhase.Result,
        }, phases);
        CollectionAssert.AreEqual(new[]
        {
            TotemMatchActivity.Build,
            TotemMatchActivity.Combat,
            TotemMatchActivity.Build,
            TotemMatchActivity.ZoneShrink,
            TotemMatchActivity.Combat,
            TotemMatchActivity.Build,
            TotemMatchActivity.ZoneShrink,
            TotemMatchActivity.Combat,
            TotemMatchActivity.Build,
            TotemMatchActivity.ZoneShrink,
            TotemMatchActivity.Combat,
            TotemMatchActivity.Build,
            TotemMatchActivity.ZoneShrink,
            TotemMatchActivity.Combat,
            TotemMatchActivity.Result,
        }, activities);
        Assert.That(flow.CurrentPhase, Is.EqualTo(TotemMatchPhase.Result));
        Assert.That(flow.IsGameplaySuspended, Is.False);
    }

    [Test]
    public void NormalMode_UsesSixtyFortyFiveAndOneEightyThirtyTimings()
    {
        var flow = new TotemMatchFlowService();
        flow.BeginMatch(useFastMode: false);

        Assert.That(flow.ActivityDuration, Is.EqualTo(60f));
        flow.Advance(60f);
        Assert.That(flow.CurrentPhase, Is.EqualTo(TotemMatchPhase.Round1Combat));
        Assert.That(flow.ActivityDuration, Is.EqualTo(180f));
        flow.Advance(180f);
        Assert.That(flow.CurrentPhase, Is.EqualTo(TotemMatchPhase.Build2));
        Assert.That(flow.ActivityDuration, Is.EqualTo(45f));
        flow.Advance(45f);
        Assert.That(flow.CurrentActivity, Is.EqualTo(TotemMatchActivity.ZoneShrink));
        Assert.That(flow.ActivityDuration, Is.EqualTo(30f));
    }

    [Test]
    public void MatchClock_KeepsUiTimeRunningWhileBuildSuspendsWorldTime()
    {
        var clock = new TotemMatchClockAccumulator();
        clock.Activate();
        clock.Advance(1f, 2f, gameplaySuspended: true);
        Assert.That(clock.WorldTime, Is.EqualTo(0f));
        Assert.That(clock.UiTime, Is.EqualTo(2f));

        clock.Advance(1f, 2f, gameplaySuspended: false);
        Assert.That(clock.WorldTime, Is.EqualTo(1f));
        Assert.That(clock.UiTime, Is.EqualTo(4f));
    }

    [Test]
    public void ZoneRadius_CompletesFourDynamicShrinksWithinTheirOwnActivities()
    {
        Assert.That(TotemZoneService.ComputeFirstPlayableRadius(
            400f, 110f, 75f, 50f, 35f, TotemMatchPhase.Round2Combat, TotemMatchActivity.ZoneShrink, 0.5f),
            Is.EqualTo(155f).Within(0.001f));
        Assert.That(TotemZoneService.ComputeFirstPlayableRadius(
            400f, 110f, 75f, 50f, 35f, TotemMatchPhase.Round2Combat, TotemMatchActivity.Combat, 0f),
            Is.EqualTo(110f));
        Assert.That(TotemZoneService.ComputeFirstPlayableRadius(
            400f, 110f, 75f, 50f, 35f, TotemMatchPhase.Round4Combat, TotemMatchActivity.ZoneShrink, 0.5f),
            Is.EqualTo(62.5f).Within(0.001f));
        Assert.That(TotemZoneService.ComputeFirstPlayableRadius(
            400f, 110f, 75f, 50f, 35f, TotemMatchPhase.Result, TotemMatchActivity.Result, 0f),
            Is.EqualTo(35f));
    }

    [Test]
    public void BuildPhase_SuspendsDamageAndAllDeclaredSimulationServices()
    {
        var flow = new TotemMatchFlowService();
        flow.BeginMatch(useFastMode: true);
        Assert.That(flow.IsGameplaySuspended, Is.True);

        var target = new TotemParticipantModel(
            2,
            "Target",
            TotemParticipantControllerKind.LightBot,
            100f,
            UnityEngine.Vector3.zero,
            TotemParticipantLifecycle.Active,
            teamId: 1);
        var decision = TotemCombatRelationshipService.Evaluate(
            null,
            target,
            new TotemCombatRelationshipContext(0f, gameplaySuspended: true));
        Assert.That(decision.Allowed, Is.False);
        Assert.That(decision.Reason, Is.EqualTo(TotemCombatRelationshipReason.BlockedGameplaySuspended));

        Type marker = typeof(ITotemGameplaySimulationService);
        Type[] services =
        {
            typeof(TotemActorService), typeof(TotemAIService), typeof(TotemMapResourceService),
            typeof(TotemStatusService), typeof(TotemZoneService),
            typeof(TotemCombatService), typeof(TotemInteractionService),
            typeof(TotemParticipantReadinessService),
            typeof(TotemWeaponService),
        };
        for (int i = 0; i < services.Length; i++)
        {
            Assert.That(marker.IsAssignableFrom(services[i]), Is.True, services[i].Name);
        }
    }

    [Test]
    public void TransitionContract_RejectsSkippedAndPostResultGameplayPhases()
    {
        Assert.That(TotemMatchPhaseContract.CanTransition(TotemMatchPhase.FrontEnd, TotemMatchPhase.Round1Combat), Is.False);
        Assert.That(TotemMatchPhaseContract.CanTransition(TotemMatchPhase.Round1Combat, TotemMatchPhase.Round3Combat), Is.False);
        Assert.That(TotemMatchPhaseContract.CanTransition(TotemMatchPhase.Result, TotemMatchPhase.Round3Combat), Is.False);
        Assert.That(TotemMatchPhaseContract.CanTransition(TotemMatchPhase.Result, TotemMatchPhase.FrontEnd), Is.True);
    }

    [Test]
    public void OversizedAdvance_RecoversAtResultAfterFifthRound()
    {
        var flow = new TotemMatchFlowService();
        flow.BeginMatch(useFastMode: true);
        flow.Advance(100000f);

        Assert.That(flow.CurrentPhase, Is.EqualTo(TotemMatchPhase.Result));
        Assert.That(flow.CurrentActivity, Is.EqualTo(TotemMatchActivity.Result));
    }
}
