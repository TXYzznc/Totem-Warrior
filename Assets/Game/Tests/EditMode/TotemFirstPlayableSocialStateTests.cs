using NUnit.Framework;
using UnityEngine;

public sealed class TotemFirstPlayableSocialStateTests
{
    [Test]
    public void AchievementCounter_AccumulatesEveryPublishedCategoryExactly()
    {
        var counter = new TotemMatchAchievementCounter();
        counter.AddPlayerDamage(10.5f);
        counter.AddPlayerDown();
        counter.AddPlayerElimination();
        counter.AddAllyHealing(3f);
        counter.AddAllyShieldOrMitigation(4f);
        counter.AddSuccessfulRevive();
        counter.AddCleanseOrControlRemoval();
        counter.AddEffectiveControl(1.5f);
        counter.AddAllyDamageGainCreated(6f);
        counter.AddResourcesAcquired(7);
        counter.AddResourcesShared(8);
        counter.AddSelfDown();
        counter.AddIndirectElementDamage(9f);

        TotemMatchAchievementSnapshot value = counter.Capture();
        Assert.That(value.playerDamage, Is.EqualTo(10.5f));
        Assert.That(value.playerDowns, Is.EqualTo(1));
        Assert.That(value.playerEliminations, Is.EqualTo(1));
        Assert.That(value.allyHealing, Is.EqualTo(3f));
        Assert.That(value.allyShieldOrMitigation, Is.EqualTo(4f));
        Assert.That(value.successfulRevives, Is.EqualTo(1));
        Assert.That(value.cleansesOrControlRemovals, Is.EqualTo(1));
        Assert.That(value.effectiveControlSeconds, Is.EqualTo(1.5f));
        Assert.That(value.effectiveControlCount, Is.EqualTo(1));
        Assert.That(value.allyDamageGainCreated, Is.EqualTo(6f));
        Assert.That(value.resourcesAcquired, Is.EqualTo(7));
        Assert.That(value.resourcesShared, Is.EqualTo(8));
        Assert.That(value.selfDowns, Is.EqualTo(1));
        Assert.That(value.indirectElementDamage, Is.EqualTo(9f));
    }

    [Test]
    public void BoundarySnapshot_IsFrozenAndUsesPublicTextWithoutInternalNumbers()
    {
        TotemActorModel actor = Actor(1, 0);
        var build = new TotemFirstPlayableTattooBuildState();
        build.SetPigment(TotemPigmentKind.Fire, 20);
        Assert.That(
            build.TryEquip(
                TotemMatchPhase.OpeningBuild,
                TotemTattooSlotId.RightArm,
                TotemFirstPlayablePatternId.P01,
                TotemFirstPlayableElement.Fire,
                out _),
            Is.True);

        var achievement = new TotemMatchAchievementSnapshot { playerDamage = 12.5f };
        TotemConstructionIntelligenceSnapshot frozen =
            TotemFirstPlayableSocialService.CreateBoundarySnapshot(actor, build, achievement, TotemMatchPhase.Build2);

        build.TryRemove(TotemMatchPhase.Build2, TotemTattooSlotId.RightArm, out _);
        achievement.playerDamage = 99f;

        Assert.That(frozen.tattoos.Length, Is.EqualTo(1));
        Assert.That(frozen.tattoos[0].pattern, Is.EqualTo(TotemFirstPlayablePatternId.P01));
        Assert.That(frozen.tattoos[0].publicEffectText, Is.EqualTo(TotemFirstPlayableTattooBuildConfig.P01PublicEffectText));
        Assert.That(frozen.tattoos[0].publicEffectText, Does.Not.Match(@"\d"));
        Assert.That(frozen.achievements.playerDamage, Is.EqualTo(12.5f));
        Assert.That(frozen.attributes.Length, Is.EqualTo(3));
        Assert.That(frozen.attributes[0].baseValue, Is.EqualTo(100f));
        Assert.That(frozen.attributes[0].inMatchBonus, Is.Zero);
    }

    [Test]
    public void ApprovedPigmentRequest_AtomicallyMovesBothInventories()
    {
        var ledger = new TotemPigmentTradeLedger();
        var donor = Inventory(TotemPigmentKind.Ice, 8);
        var receiver = Inventory(TotemPigmentKind.Ice, 1);

        Assert.That(ledger.TryCreate(P(1), P(2), TotemPigmentKind.Ice, 5, 1, 2, donor, out var request), Is.True);
        Assert.That(
            ledger.TryResolve(request.RequestId, P(2), true, donor, receiver, out var resolved, out var transfer),
            Is.True);

        Assert.That(resolved.State, Is.EqualTo(TotemPigmentRequestState.Approved));
        Assert.That(transfer.RequiresAtomicCommit, Is.True);
        Assert.That(donor.GetPigment(TotemPigmentKind.Ice), Is.EqualTo(3));
        Assert.That(receiver.GetPigment(TotemPigmentKind.Ice), Is.EqualTo(6));
        Assert.That(transfer.InventoryVersion, Is.EqualTo(donor.InventoryVersion));
    }

    [Test]
    public void ApprovalAfterInventoryChanged_InvalidatesWithoutPartialTransfer()
    {
        var ledger = new TotemPigmentTradeLedger();
        var donor = Inventory(TotemPigmentKind.Fire, 8);
        var receiver = Inventory(TotemPigmentKind.Fire, 1);
        ledger.TryCreate(P(1), P(2), TotemPigmentKind.Fire, 5, 1, 2, donor, out var request);
        donor.SetPigment(TotemPigmentKind.Fire, 4);
        int donorVersion = donor.InventoryVersion;
        int receiverVersion = receiver.InventoryVersion;

        Assert.That(
            ledger.TryResolve(request.RequestId, P(2), true, donor, receiver, out var resolved, out var transfer),
            Is.False);
        Assert.That(resolved.State, Is.EqualTo(TotemPigmentRequestState.Invalidated));
        Assert.That(transfer.RequiresAtomicCommit, Is.False);
        Assert.That(donor.GetPigment(TotemPigmentKind.Fire), Is.EqualTo(4));
        Assert.That(receiver.GetPigment(TotemPigmentKind.Fire), Is.EqualTo(1));
        Assert.That(donor.InventoryVersion, Is.EqualTo(donorVersion));
        Assert.That(receiver.InventoryVersion, Is.EqualTo(receiverVersion));
    }

    [Test]
    public void RejectionAndPhaseExpiry_NeverChangeInventory()
    {
        var ledger = new TotemPigmentTradeLedger();
        var donor = Inventory(TotemPigmentKind.Lightning, 6);
        var receiver = Inventory(TotemPigmentKind.Lightning, 0);
        ledger.TryCreate(P(1), P(2), TotemPigmentKind.Lightning, 2, 1, 2, donor, out var rejected);
        Assert.That(ledger.TryResolve(rejected.RequestId, P(2), false, donor, receiver, out var resolution, out _), Is.True);
        Assert.That(resolution.State, Is.EqualTo(TotemPigmentRequestState.Rejected));

        ledger.TryCreate(P(1), P(2), TotemPigmentKind.Lightning, 3, 2, 2, donor, out var expiring);
        Assert.That(ledger.ExpirePendingExceptPhase(3), Is.EqualTo(1));
        Assert.That(ledger.TryGet(expiring.RequestId, out var expired), Is.True);
        Assert.That(expired.State, Is.EqualTo(TotemPigmentRequestState.Expired));
        Assert.That(donor.GetPigment(TotemPigmentKind.Lightning), Is.EqualTo(6));
        Assert.That(receiver.GetPigment(TotemPigmentKind.Lightning), Is.Zero);
    }

    [Test]
    public void PigmentCommands_RoundTripRequestAndApprovalIntent()
    {
        var request = new TotemGameplayCommand(
            P(1),
            TotemGameplayCommandSource.HumanInput,
            TotemGameplayCommandType.RequestPigment,
            1,
            Vector3.zero,
            TotemPigmentCommandCodec.EncodeRequest(TotemPigmentKind.Lightning, 12));
        Assert.That(TotemPigmentCommandCodec.TryDecodeRequest(request, out var pigment, out int amount), Is.True);
        Assert.That(pigment, Is.EqualTo(TotemPigmentKind.Lightning));
        Assert.That(amount, Is.EqualTo(12));

        var approval = new TotemGameplayCommand(
            P(2),
            TotemGameplayCommandSource.BotDecision,
            TotemGameplayCommandType.ResolvePigmentRequest,
            2,
            Vector3.zero,
            TotemPigmentCommandCodec.EncodeResolution(7, true));
        Assert.That(TotemPigmentCommandCodec.TryDecodeResolution(approval, out int requestId, out bool approve), Is.True);
        Assert.That(requestId, Is.EqualTo(7));
        Assert.That(approve, Is.True);
    }

    [Test]
    public void ReactionAttribution_GivesSameActualIndirectDamageToTriggerAndAssistant()
    {
        var service = new TotemFirstPlayableSocialService();
        service.RecordReactionAttribution(new TotemReactionAttribution(
            TotemReactionKind.HeatShock,
            P(2),
            P(1),
            7.5f));

        Assert.That(service.CaptureAchievement(P(1)).indirectElementDamage, Is.EqualTo(7.5f));
        Assert.That(service.CaptureAchievement(P(2)).indirectElementDamage, Is.EqualTo(7.5f));
    }

    private static TotemParticipantId P(int id) => new TotemParticipantId(id);

    private static TotemFirstPlayableTattooBuildState Inventory(TotemPigmentKind pigment, int amount)
    {
        var state = new TotemFirstPlayableTattooBuildState();
        state.SetPigment(pigment, amount);
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
            ControllerKind = TotemParticipantControllerKind.Human,
            Position = Vector3.zero,
            MaxHealth = 100f,
        });
    }
}
