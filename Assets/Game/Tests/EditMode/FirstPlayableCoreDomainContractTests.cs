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

    [Test]
    public void TattooCatalog_ContainsOnlyConfirmedPatternsAndIndependentCooldowns()
    {
        TattooEffectDefinition p01RightArm;
        TattooEffectDefinition p02RightArm;
        TattooEffectDefinition ignored;

        Assert.That(TattooEffectCatalog.TryGet(TattooPatternId.P01, TattooBodyPart.RightArm, out p01RightArm), Is.True);
        Assert.That(TattooEffectCatalog.TryGet(TattooPatternId.P02, TattooBodyPart.RightArm, out p02RightArm), Is.True);
        Assert.That(p01RightArm.CooldownSeconds, Is.EqualTo(2.5f));
        Assert.That(p02RightArm.CooldownSeconds, Is.EqualTo(6f));
        Assert.That(TattooEffectCatalog.TryGet((TattooPatternId)3, TattooBodyPart.Head, out ignored), Is.False);
    }

    [Test]
    public void TattooBuild_ReplacementIsAtomicAndUsesTenSixEconomy()
    {
        TattooBuildState build = TattooBuildState.CreateEmpty(10, 10, 0);
        TattooBuildMutationResult equipped = build.TryEquip(MatchPhase.OpeningBuild, TattooBodyPart.Head, TattooPatternId.P01, ElementType.Fire);
        TattooBuildMutationResult replaced = equipped.State.TryEquip(MatchPhase.OpeningBuild, TattooBodyPart.Head, TattooPatternId.P02, ElementType.Ice);
        TattooBuildMutationResult rejected = replaced.State.TryEquip(MatchPhase.Round1Combat, TattooBodyPart.Head, TattooPatternId.P01, ElementType.Fire);

        Assert.That(equipped.IsSuccess, Is.True);
        Assert.That(replaced.IsSuccess, Is.True);
        Assert.That(replaced.State.GetPigment(ElementType.Fire), Is.EqualTo(6));
        Assert.That(replaced.State.GetPigment(ElementType.Ice), Is.EqualTo(0));
        Assert.That(rejected.IsSuccess, Is.False);
        Assert.That(rejected.State.GetSlot(TattooBodyPart.Head).Pattern, Is.EqualTo(TattooPatternId.P02));
    }

    [Test]
    public void TattooCooldown_IsPerBodyPartAndRefreshesAtCombatStart()
    {
        TattooBuildState build = TattooBuildState.CreateEmpty(20, 0, 0);
        build = build.TryEquip(MatchPhase.OpeningBuild, TattooBodyPart.LeftArm, TattooPatternId.P01, ElementType.Fire).State;
        build = build.TryEquip(MatchPhase.OpeningBuild, TattooBodyPart.RightArm, TattooPatternId.P01, ElementType.Fire).State;
        var cooldowns = new TattooCooldownState();

        Assert.That(cooldowns.TryStart(build, TattooBodyPart.LeftArm), Is.True);
        Assert.That(cooldowns.TryStart(build, TattooBodyPart.RightArm), Is.True);
        Assert.That(cooldowns.GetRemaining(TattooBodyPart.LeftArm), Is.EqualTo(10f));
        Assert.That(cooldowns.GetRemaining(TattooBodyPart.RightArm), Is.EqualTo(2.5f));
        cooldowns.RefreshForCombatStart(build);
        Assert.That(cooldowns.GetRemaining(TattooBodyPart.LeftArm), Is.EqualTo(0f));
    }

    [Test]
    public void Elements_SameTypeStacksFifoAndRefreshesDecay()
    {
        var first = new ParticipantId(1);
        var second = new ParticipantId(3);
        ElementAttachment attachment = default(ElementAttachment);
        attachment = attachment.Apply(ElementType.Fire, first, 10).Attachment;
        attachment = attachment.Apply(ElementType.Fire, second, 11).Attachment;
        attachment = attachment.Apply(ElementType.Fire, first, 12).Attachment;
        ElementAttachment refreshed = attachment.Apply(ElementType.Fire, second, 13).Attachment;

        Assert.That(refreshed.Strength, Is.EqualTo(ElementStrength.Strong));
        Assert.That(refreshed.GetLayerSource(0).ParticipantId, Is.EqualTo(first));
        Assert.That(refreshed.RemainingToDecaySeconds, Is.EqualTo(ElementAttachment.DecayIntervalSeconds));
        Assert.That(refreshed.AdvanceTime(3f).Strength, Is.EqualTo(ElementStrength.Standard));
    }

    [Test]
    public void Elements_DifferentTypeConsumesOldestLayerAndCreatesTerminalReaction()
    {
        var source = new ParticipantId(1);
        var trigger = new ParticipantId(3);
        ElementAttachment attachment = default(ElementAttachment);
        attachment = attachment.Apply(ElementType.Fire, source, 1).Attachment;
        ElementApplicationResult result = attachment.Apply(ElementType.Ice, trigger, 2);

        Assert.That(result.HasReaction, Is.True);
        Assert.That(result.Reaction.Definition.Type, Is.EqualTo(ElementReactionType.ThermalShock));
        Assert.That(result.Reaction.TriggerParticipantId, Is.EqualTo(trigger));
        Assert.That(result.Reaction.AssistingLayer.ParticipantId, Is.EqualTo(source));
        Assert.That(result.Attachment.HasElement, Is.False);
    }

    [Test]
    public void Elements_ExposeConfirmedTraitBaselines()
    {
        Assert.That(ElementReactionRules.GetTrait(ElementType.Lightning), Is.EqualTo(ElementTraitKind.LightningDischarge));
        Assert.That(ElementReactionRules.GetTraitStrength(ElementType.Fire, ElementStrength.Strong), Is.EqualTo(1.5f));
        Assert.That(ElementReactionRules.GetTraitStrength(ElementType.Ice, ElementStrength.Standard), Is.EqualTo(0.20f));
        Assert.That(ElementReactionRules.Get(ElementType.Fire, ElementType.Lightning).RadiusMeters, Is.EqualTo(3f));
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
