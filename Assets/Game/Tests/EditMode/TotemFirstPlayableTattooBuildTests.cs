using NUnit.Framework;

public sealed class TotemFirstPlayableTattooBuildTests
{
    [Test]
    public void SerializedContract_RejectsMissingPublicTextAndLegacyPatternExpansion()
    {
        var config = new TotemFirstPlayableContractConfig();
        var errors = new System.Collections.Generic.List<string>();

        Assert.That(TotemFirstPlayableContractValidator.Validate(config, errors), Is.True, string.Join("\n", errors));

        config.tattooBuild.patterns[0].publicEffectText = string.Empty;
        config.tattooBuild.patterns = new[]
        {
            config.tattooBuild.patterns[0],
            config.tattooBuild.patterns[1],
            new TotemFirstPlayableTattooPatternConfig
            {
                pattern = (TotemFirstPlayablePatternId)3,
                behavior = TotemFirstPlayablePatternBehavior.NeighborSpread,
                publicEffectText = "legacy",
            },
        };
        errors.Clear();

        Assert.That(TotemFirstPlayableContractValidator.Validate(config, errors), Is.False);
        Assert.That(errors, Has.Some.Contains("exactly P01 and P02"));
    }

    [Test]
    public void Contract_UsesSixSlotsTwoUnnamedPatternsAndThreePigments()
    {
        var state = new TotemFirstPlayableTattooBuildState();

        Assert.That(state.CaptureLoadout(), Has.Length.EqualTo(6));
        Assert.That(TotemFirstPlayableTattooBuildState.IsAvailablePattern(TotemFirstPlayablePatternId.P01), Is.True);
        Assert.That(TotemFirstPlayableTattooBuildState.IsAvailablePattern(TotemFirstPlayablePatternId.P02), Is.True);
        Assert.That(TotemFirstPlayableTattooBuildState.IsAvailablePattern((TotemFirstPlayablePatternId)3), Is.False);
        Assert.That(TotemFirstPlayableTattooBuildState.GetPublicEffectText(TotemFirstPlayablePatternId.P01), Is.Not.Empty);
        Assert.That(TotemFirstPlayableTattooBuildState.GetPublicEffectText(TotemFirstPlayablePatternId.P02), Is.Not.Empty);
        Assert.That(TotemFirstPlayableTattooBuildState.GetPublicEffectText((TotemFirstPlayablePatternId)3), Is.Empty);
        Assert.That((int)TotemPigmentKind.Fire, Is.Not.EqualTo((int)TotemPigmentKind.Ice));
        Assert.That((int)TotemPigmentKind.Ice, Is.Not.EqualTo((int)TotemPigmentKind.Lightning));
    }

    [Test]
    public void EquipAndRemove_SpendTenAndRefundSix()
    {
        var state = new TotemFirstPlayableTattooBuildState();
        state.SetPigment(TotemPigmentKind.Fire, 10);

        Assert.That(state.TryEquip(
            TotemMatchPhase.OpeningBuild,
            TotemTattooSlotId.Head,
            TotemFirstPlayablePatternId.P01,
            TotemFirstPlayableElement.Fire,
            out var equipped), Is.True);
        Assert.That(equipped.SpentAmount, Is.EqualTo(10));
        Assert.That(state.GetPigment(TotemPigmentKind.Fire), Is.Zero);

        Assert.That(state.TryRemove(TotemMatchPhase.Build2, TotemTattooSlotId.Head, out var removed), Is.True);
        Assert.That(removed.RefundedAmount, Is.EqualTo(6));
        Assert.That(state.GetPigment(TotemPigmentKind.Fire), Is.EqualTo(6));
        Assert.That(state.GetSlot(TotemTattooSlotId.Head).IsEquipped, Is.False);
    }

    [Test]
    public void SameElementReplacement_CanAtomicallyUseItsRefund()
    {
        var state = EquippedState(TotemFirstPlayableElement.Ice);
        state.SetPigment(TotemPigmentKind.Ice, 4);

        Assert.That(state.TryEquip(
            TotemMatchPhase.Build3,
            TotemTattooSlotId.RightArm,
            TotemFirstPlayablePatternId.P02,
            TotemFirstPlayableElement.Ice,
            out var result), Is.True);
        Assert.That(result.RefundedAmount, Is.EqualTo(6));
        Assert.That(result.SpentAmount, Is.EqualTo(10));
        Assert.That(state.GetPigment(TotemPigmentKind.Ice), Is.Zero);
        Assert.That(state.GetSlot(TotemTattooSlotId.RightArm).Pattern, Is.EqualTo(TotemFirstPlayablePatternId.P02));
    }

    [Test]
    public void BotPlanner_IsDeterministicAndUsesOnlyAffordablePigment()
    {
        var state = new TotemFirstPlayableTattooBuildState();
        state.SetPigment(TotemPigmentKind.Ice, 10);

        Assert.That(TotemFirstPlayableBotBuildPlanner.TryPlan(1, 0, state, out var first), Is.True);
        Assert.That(TotemFirstPlayableBotBuildPlanner.TryPlan(1, 0, state, out var repeated), Is.True);
        Assert.That(repeated.Slot, Is.EqualTo(first.Slot));
        Assert.That(repeated.Pattern, Is.EqualTo(first.Pattern));
        Assert.That(repeated.Element, Is.EqualTo(first.Element));
        Assert.That(first.Slot, Is.EqualTo(TotemTattooSlotId.RightArm));
        Assert.That(first.Pattern, Is.EqualTo(TotemFirstPlayablePatternId.P02));
        Assert.That(first.Element, Is.EqualTo(TotemFirstPlayableElement.Ice));
    }

    [Test]
    public void BotPlanner_RejectsNoPigmentAndSkipsIdenticalLoadout()
    {
        var empty = new TotemFirstPlayableTattooBuildState();
        Assert.That(TotemFirstPlayableBotBuildPlanner.TryPlan(2, 0, empty, out _), Is.False);

        empty.SetPigment(TotemPigmentKind.Lightning, 10);
        Assert.That(empty.TryEquip(
            TotemMatchPhase.OpeningBuild,
            TotemTattooSlotId.RightArm,
            TotemFirstPlayablePatternId.P01,
            TotemFirstPlayableElement.Lightning,
            out _), Is.True);
        Assert.That(TotemFirstPlayableBotBuildPlanner.TryPlan(2, 0, empty, out _), Is.False);
    }

    [Test]
    public void BotPlanner_CanUseSameElementRefundForPatternChange()
    {
        var state = EquippedState(TotemFirstPlayableElement.Ice);
        state.SetPigment(TotemPigmentKind.Ice, 4);

        Assert.That(TotemFirstPlayableBotBuildPlanner.TryPlan(2, 1, state, out var plan), Is.True);
        Assert.That(plan.Pattern, Is.EqualTo(TotemFirstPlayablePatternId.P02));
        Assert.That(plan.Element, Is.EqualTo(TotemFirstPlayableElement.Ice));
    }

    [Test]
    public void CrossElementReplacement_WhenTargetPigmentIsInsufficient_IsAtomic()
    {
        var state = EquippedState(TotemFirstPlayableElement.Fire);
        state.SetPigment(TotemPigmentKind.Lightning, 9);
        int version = state.InventoryVersion;

        Assert.That(state.TryEquip(
            TotemMatchPhase.Build2,
            TotemTattooSlotId.RightArm,
            TotemFirstPlayablePatternId.P02,
            TotemFirstPlayableElement.Lightning,
            out var result), Is.False);
        Assert.That(result.Code, Is.EqualTo(TotemTattooMutationCode.InsufficientPigment));
        Assert.That(state.InventoryVersion, Is.EqualTo(version));
        Assert.That(state.GetPigment(TotemPigmentKind.Fire), Is.Zero);
        Assert.That(state.GetPigment(TotemPigmentKind.Lightning), Is.EqualTo(9));
        Assert.That(state.GetSlot(TotemTattooSlotId.RightArm).Element, Is.EqualTo(TotemFirstPlayableElement.Fire));
    }

    [TestCase(TotemMatchPhase.FrontEnd)]
    [TestCase(TotemMatchPhase.Round1Combat)]
    [TestCase(TotemMatchPhase.Round2Combat)]
    [TestCase(TotemMatchPhase.Round3Combat)]
    [TestCase(TotemMatchPhase.Result)]
    public void MutationOutsideBuildPhases_IsRejectedWithoutChangingState(TotemMatchPhase phase)
    {
        var state = new TotemFirstPlayableTattooBuildState();
        state.SetPigment(TotemPigmentKind.Fire, 10);
        int version = state.InventoryVersion;

        Assert.That(state.TryEquip(
            phase,
            TotemTattooSlotId.Torso,
            TotemFirstPlayablePatternId.P01,
            TotemFirstPlayableElement.Fire,
            out var result), Is.False);
        Assert.That(result.Code, Is.EqualTo(TotemTattooMutationCode.NotBuildPhase));
        Assert.That(state.InventoryVersion, Is.EqualTo(version));
        Assert.That(state.GetPigment(TotemPigmentKind.Fire), Is.EqualTo(10));
        Assert.That(state.GetSlot(TotemTattooSlotId.Torso).IsEquipped, Is.False);
    }

    [Test]
    public void InvalidInputsAndNegativeWalletValues_NeverMutateLoadoutOrCreateDebt()
    {
        var state = new TotemFirstPlayableTattooBuildState();
        state.SetPigment(TotemPigmentKind.Fire, -20);
        int version = state.InventoryVersion;

        Assert.That(state.GetPigment(TotemPigmentKind.Fire), Is.Zero);
        Assert.That(state.TryEquip(
            TotemMatchPhase.OpeningBuild,
            (TotemTattooSlotId)99,
            TotemFirstPlayablePatternId.P01,
            TotemFirstPlayableElement.Fire,
            out var invalidSlot), Is.False);
        Assert.That(invalidSlot.Code, Is.EqualTo(TotemTattooMutationCode.InvalidSlot));
        Assert.That(state.TryEquip(
            TotemMatchPhase.OpeningBuild,
            TotemTattooSlotId.LeftLeg,
            (TotemFirstPlayablePatternId)3,
            TotemFirstPlayableElement.Fire,
            out var invalidPattern), Is.False);
        Assert.That(invalidPattern.Code, Is.EqualTo(TotemTattooMutationCode.InvalidPattern));
        Assert.That(state.TryEquip(
            TotemMatchPhase.OpeningBuild,
            TotemTattooSlotId.LeftLeg,
            TotemFirstPlayablePatternId.P01,
            TotemFirstPlayableElement.None,
            out var invalidElement), Is.False);
        Assert.That(invalidElement.Code, Is.EqualTo(TotemTattooMutationCode.InvalidElement));
        Assert.That(state.InventoryVersion, Is.EqualTo(version));
    }

    [Test]
    public void MatchCleanup_ClearsAllSlotsAndPigmentsWithoutRefunding()
    {
        var state = EquippedState(TotemFirstPlayableElement.Lightning);
        state.AddPigment(TotemPigmentKind.Fire, 7);
        int version = state.InventoryVersion;

        Assert.That(state.ClearForMatchCleanup(), Is.True);
        Assert.That(state.InventoryVersion, Is.EqualTo(version + 1));
        Assert.That(state.GetPigment(TotemPigmentKind.Fire), Is.Zero);
        Assert.That(state.GetPigment(TotemPigmentKind.Ice), Is.Zero);
        Assert.That(state.GetPigment(TotemPigmentKind.Lightning), Is.Zero);
        Assert.That(state.CaptureLoadout(), Has.All.Matches<TotemTattooLoadoutEntry>(entry => !entry.IsEquipped));
        Assert.That(state.ClearForMatchCleanup(), Is.False);
        Assert.That(state.InventoryVersion, Is.EqualTo(version + 1));
    }

    [Test]
    public void HumanAndBotCommands_UseTheSameTattooTransactionPath()
    {
        var humanState = new TotemFirstPlayableTattooBuildState();
        var botState = new TotemFirstPlayableTattooBuildState();
        humanState.SetPigment(TotemPigmentKind.Lightning, 10);
        botState.SetPigment(TotemPigmentKind.Lightning, 10);
        int payload = TotemFirstPlayableTattooCommandCodec.EncodeEquip(
            TotemTattooSlotId.LeftArm,
            TotemFirstPlayablePatternId.P02,
            TotemFirstPlayableElement.Lightning);
        var human = new TotemGameplayCommand(
            new TotemParticipantId(1),
            TotemGameplayCommandSource.HumanInput,
            TotemGameplayCommandType.EquipTattoo,
            1,
            UnityEngine.Vector3.zero,
            payload);
        var bot = new TotemGameplayCommand(
            new TotemParticipantId(2),
            TotemGameplayCommandSource.BotDecision,
            TotemGameplayCommandType.EquipTattoo,
            1,
            UnityEngine.Vector3.zero,
            payload);

        Assert.That(humanState.TryApplyCommand(TotemMatchPhase.Build2, human, out var humanResult), Is.True);
        Assert.That(botState.TryApplyCommand(TotemMatchPhase.Build2, bot, out var botResult), Is.True);
        Assert.That(humanResult.SpentAmount, Is.EqualTo(botResult.SpentAmount));
        Assert.That(humanState.GetSlot(TotemTattooSlotId.LeftArm).Pattern, Is.EqualTo(TotemFirstPlayablePatternId.P02));
        Assert.That(botState.GetSlot(TotemTattooSlotId.LeftArm).Pattern, Is.EqualTo(TotemFirstPlayablePatternId.P02));
    }

    [Test]
    public void MalformedTattooCommand_IsRejectedWithoutMutation()
    {
        var state = new TotemFirstPlayableTattooBuildState();
        state.SetPigment(TotemPigmentKind.Fire, 10);
        var malformed = new TotemGameplayCommand(
            new TotemParticipantId(1),
            TotemGameplayCommandSource.HumanInput,
            TotemGameplayCommandType.EquipTattoo,
            2,
            UnityEngine.Vector3.zero,
            int.MaxValue);
        int version = state.InventoryVersion;

        Assert.That(state.TryApplyCommand(TotemMatchPhase.OpeningBuild, malformed, out var result), Is.False);
        Assert.That(result.Code, Is.EqualTo(TotemTattooMutationCode.InvalidCommand));
        Assert.That(state.InventoryVersion, Is.EqualTo(version));
    }

    private static TotemFirstPlayableTattooBuildState EquippedState(TotemFirstPlayableElement element)
    {
        var state = new TotemFirstPlayableTattooBuildState();
        TotemPigmentKind pigment = element == TotemFirstPlayableElement.Fire
            ? TotemPigmentKind.Fire
            : element == TotemFirstPlayableElement.Ice
                ? TotemPigmentKind.Ice
                : TotemPigmentKind.Lightning;
        state.SetPigment(pigment, 10);
        Assert.That(state.TryEquip(
            TotemMatchPhase.OpeningBuild,
            TotemTattooSlotId.RightArm,
            TotemFirstPlayablePatternId.P01,
            element,
            out _), Is.True);
        return state;
    }
}
