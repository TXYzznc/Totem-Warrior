using System.Collections.Generic;
using GameDesinger.FirstPlayable.Domain;
using NUnit.Framework;

public sealed class FirstPlayableCoreDomainContractTests
{
    [Test]
    public void Roster_RequiresOneHumanAndThreeDuoTeams()
    {
        ParticipantRoster roster;
        string error;

        Assert.That(ParticipantRoster.TryCreate(CreateDefinitions(), out roster, out error), Is.True, error);
        Assert.That(roster.Count, Is.EqualTo(6));
        Assert.That(roster.AreTeammates(new ParticipantId(1), new ParticipantId(2)), Is.True);
        Assert.That(roster.AreTeammates(new ParticipantId(1), new ParticipantId(3)), Is.False);
    }

    [Test]
    public void Roster_RejectsTeamsThatAreNotDuo()
    {
        var definitions = CreateDefinitions();
        definitions[5] = new ParticipantDefinition(new ParticipantId(6), new TeamId(2), ParticipantControllerKind.Bot);
        ParticipantRoster roster;
        string error;

        Assert.That(ParticipantRoster.TryCreate(definitions, out roster, out error), Is.False);
        Assert.That(error, Is.Not.Empty);
    }

    [Test]
    public void Commands_RespectBuildAndCombatBoundaries()
    {
        ParticipantRoster roster = CreateRoster();
        var build = new GameplayCommand(new ParticipantId(1), GameplayCommandSource.HumanInput, GameplayCommandKind.BuildMutation);
        var fire = new GameplayCommand(new ParticipantId(1), GameplayCommandSource.HumanInput, GameplayCommandKind.Fire);
        var botSpoofingHuman = new GameplayCommand(new ParticipantId(2), GameplayCommandSource.HumanInput, GameplayCommandKind.Fire);

        Assert.That(GameplayCommandRules.IsAllowed(roster, build, MatchPhase.Build2, ParticipantLifeState.Alive), Is.True);
        Assert.That(GameplayCommandRules.IsAllowed(roster, fire, MatchPhase.Build2, ParticipantLifeState.Alive), Is.False);
        Assert.That(GameplayCommandRules.IsAllowed(roster, fire, MatchPhase.Round1Combat, ParticipantLifeState.Alive), Is.True);
        Assert.That(GameplayCommandRules.IsAllowed(roster, fire, MatchPhase.Round1Combat, ParticipantLifeState.Downed), Is.False);
        Assert.That(GameplayCommandRules.IsAllowed(roster, botSpoofingHuman, MatchPhase.Round1Combat, ParticipantLifeState.Alive), Is.False);
    }

    [Test]
    public void Damage_RejectsFriendlyFireWithoutCreatingEffectiveDirectDamage()
    {
        ParticipantRoster roster = CreateRoster();
        var vitals = new CombatantVitals(150f, 150f, 0f, ParticipantLifeState.Alive);
        var result = DamageResolver.Resolve(roster, new DamageIntent(new ParticipantId(1), new ParticipantId(2), DamageKind.Direct, HitRegion.Body, 16f), vitals);

        Assert.That(result.RejectionReason, Is.EqualTo(DamageRejectionReason.SameTeam));
        Assert.That(result.EffectiveAmount, Is.EqualTo(0f));
        Assert.That(result.IsEffectiveDirectDamage, Is.False);
    }

    [Test]
    public void Damage_AppliesShieldBeforeHealthAndMarksEffectiveDirectDamage()
    {
        ParticipantRoster roster = CreateRoster();
        var vitals = new CombatantVitals(150f, 150f, 10f, ParticipantLifeState.Alive);
        var result = DamageResolver.Resolve(roster, new DamageIntent(new ParticipantId(1), new ParticipantId(3), DamageKind.Direct, HitRegion.Weakpoint, 24f), vitals);

        Assert.That(result.ShieldDamage, Is.EqualTo(10f));
        Assert.That(result.HealthDamage, Is.EqualTo(14f));
        Assert.That(result.TargetAfter.CurrentHealth, Is.EqualTo(136f));
        Assert.That(result.IsEffectiveDirectDamage, Is.True);
    }

    [Test]
    public void Damage_IndirectDamageDoesNotBecomeAnEffectiveDirectDamage()
    {
        ParticipantRoster roster = CreateRoster();
        var result = DamageResolver.Resolve(roster, new DamageIntent(new ParticipantId(1), new ParticipantId(3), DamageKind.IndirectElement, HitRegion.Body, 8f), new CombatantVitals(150f, 8f, 0f, ParticipantLifeState.Alive));

        Assert.That(result.TargetAfter.LifeState, Is.EqualTo(ParticipantLifeState.Downed));
        Assert.That(result.IsEffectiveDirectDamage, Is.False);
    }

    [Test]
    public void PhaseCursor_UsesUniqueCombatEpochsAndInvalidatesOldWork()
    {
        var cursor = new MatchPhaseCursor();
        Assert.That(cursor.TryTransition(MatchPhase.OpeningBuild, false), Is.True);
        Assert.That(cursor.TryTransition(MatchPhase.Round1Combat, false), Is.True);
        PhaseEpoch firstEpoch = cursor.CurrentEpoch;
        Assert.That(cursor.CanApplyDelayedWork(firstEpoch), Is.True);
        Assert.That(cursor.TryTransition(MatchPhase.Build2, true), Is.False);
        Assert.That(cursor.TryTransition(MatchPhase.Build2, false), Is.True);
        Assert.That(cursor.CanApplyDelayedWork(firstEpoch), Is.False);
        Assert.That(cursor.TryTransition(MatchPhase.Round2Combat, false), Is.True);
        Assert.That(cursor.CurrentEpoch.Value, Is.GreaterThan(firstEpoch.Value));
        Assert.That(cursor.CanApplyDelayedWork(firstEpoch), Is.False);
    }

    [Test]
    public void PhaseCursor_RejectsSkippedStages()
    {
        var cursor = new MatchPhaseCursor();
        Assert.That(cursor.TryTransition(MatchPhase.Round3Combat, false), Is.False);
        Assert.That(cursor.CurrentPhase, Is.EqualTo(MatchPhase.FrontEnd));
    }

    [Test]
    public void AchievementSnapshot_ClampsNegativeReadModelValues()
    {
        var snapshot = new MatchAchievementSnapshot(-1f, -1, -1, -1f, -1, -1f, -1f, -1, -1, -1f, -1f, -1, -1, -1);

        Assert.That(snapshot.PlayerDamage, Is.EqualTo(0f));
        Assert.That(snapshot.PlayerEliminations, Is.EqualTo(0));
        Assert.That(snapshot.ResourcesShared, Is.EqualTo(0));
        Assert.That(snapshot.TimesDowned, Is.EqualTo(0));
    }

    private static ParticipantRoster CreateRoster()
    {
        ParticipantRoster roster;
        string error;
        Assert.That(ParticipantRoster.TryCreate(CreateDefinitions(), out roster, out error), Is.True, error);
        return roster;
    }

    private static List<ParticipantDefinition> CreateDefinitions()
    {
        return new List<ParticipantDefinition>
        {
            new ParticipantDefinition(new ParticipantId(1), new TeamId(1), ParticipantControllerKind.Human),
            new ParticipantDefinition(new ParticipantId(2), new TeamId(1), ParticipantControllerKind.Bot),
            new ParticipantDefinition(new ParticipantId(3), new TeamId(2), ParticipantControllerKind.Bot),
            new ParticipantDefinition(new ParticipantId(4), new TeamId(2), ParticipantControllerKind.Bot),
            new ParticipantDefinition(new ParticipantId(5), new TeamId(3), ParticipantControllerKind.Bot),
            new ParticipantDefinition(new ParticipantId(6), new TeamId(3), ParticipantControllerKind.Bot),
        };
    }
}
