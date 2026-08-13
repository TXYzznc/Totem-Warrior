using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class TotemFirstPlayableContractTests
{
    [Test]
    public void RosterContract_AcceptsOneHumanFiveBotsAcrossThreeDuoTeams()
    {
        var slots = new TotemRosterSlot[TotemFirstPlayableRules.ParticipantCount];
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = new TotemRosterSlot(
                new TotemParticipantId(i + 1),
                new TotemTeamId(i / TotemFirstPlayableRules.TeamSize),
                i == 0 ? TotemFirstPlayableParticipantKind.Human : TotemFirstPlayableParticipantKind.Bot,
                TotemFirstPlayableLifeState.Alive);
        }

        Assert.That(TotemRosterContract.Validate(slots, out string error), Is.True, error);
    }

    [Test]
    public void RosterContract_RejectsDuplicateParticipantId()
    {
        var slots = new[]
        {
            Slot(1, 0, TotemFirstPlayableParticipantKind.Human),
            Slot(1, 0),
            Slot(3, 1),
            Slot(4, 1),
            Slot(5, 2),
            Slot(6, 2),
        };

        Assert.That(TotemRosterContract.Validate(slots, out string error), Is.False);
        StringAssert.Contains("Duplicate participant ID", error);
    }

    [Test]
    public void MatchPhaseContract_AllowsOnlyFrozenFiveRoundSequence()
    {
        var sequence = new[]
        {
            TotemMatchPhase.FrontEnd,
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
            TotemMatchPhase.FrontEnd,
        };

        for (int i = 0; i < sequence.Length - 1; i++)
        {
            Assert.That(TotemMatchPhaseContract.CanTransition(sequence[i], sequence[i + 1]), Is.True);
        }

        Assert.That(TotemMatchPhaseContract.CanTransition(TotemMatchPhase.Round5Combat, TotemMatchPhase.Build2), Is.False);
        Assert.That(TotemMatchPhaseContract.IsGameplaySuspended(TotemMatchPhase.Build5), Is.True);
        Assert.That(TotemMatchPhaseContract.IsGameplaySuspended(TotemMatchPhase.Round5Combat), Is.False);
    }

    [Test]
    public void GameplayCommand_HumanAndBotShareTheSameContract()
    {
        var human = new TotemGameplayCommand(new TotemParticipantId(1), TotemGameplayCommandSource.HumanInput, TotemGameplayCommandType.Fire, 7, Vector3.forward);
        var bot = new TotemGameplayCommand(new TotemParticipantId(2), TotemGameplayCommandSource.BotDecision, TotemGameplayCommandType.Fire, 8, Vector3.forward);

        Assert.That(human.IsValid, Is.True);
        Assert.That(bot.IsValid, Is.True);
        Assert.That(human.Type, Is.EqualTo(bot.Type));
    }

    [Test]
    public void DirectDamage_RifleArmRequiresPositiveAllowedDamage()
    {
        var hit = new TotemGunHitContext(
            new TotemParticipantId(1),
            new TotemTeamId(0),
            20,
            new TotemTeamId(1),
            TotemHitRegion.Weakpoint,
            Vector3.one,
            Vector3.up,
            10f);

        Assert.That(new TotemDirectDamageResult(hit, 0f, 0f, true).CanSubmitRifleArmEvent, Is.False);
        Assert.That(new TotemDirectDamageResult(hit, 0f, 10f, false).CanSubmitRifleArmEvent, Is.False);
        Assert.That(new TotemDirectDamageResult(hit, 2f, 8f, true).CanSubmitRifleArmEvent, Is.True);
    }

    [Test]
    public void EffectContract_UsesFrozenPrioritiesAndStableResolutionOrder()
    {
        Assert.That(TotemEffectPriority.Resolve(TotemEffectEventKind.Weakpoint), Is.GreaterThan(TotemEffectPriority.Resolve(TotemEffectEventKind.RifleArm)));
        Assert.That(TotemEffectPriority.Resolve(TotemEffectEventKind.RifleArm), Is.GreaterThan(TotemEffectPriority.Resolve(TotemEffectEventKind.Torso)));

        var identity = new TotemResolutionIdentity(12345, 9);
        Assert.That(identity.DeriveStableOrder(4), Is.EqualTo(identity.DeriveStableOrder(4)));
        Assert.That(identity.DeriveStableOrder(4), Is.Not.EqualTo(identity.DeriveStableOrder(5)));
    }

    [Test]
    public void ReactionAttribution_TriggerOwnsKillAndBothSourcesKeepIndirectDamageValue()
    {
        var attribution = new TotemReactionAttribution(
            TotemReactionKind.HeatShock,
            new TotemParticipantId(2),
            new TotemParticipantId(1),
            12.5f);

        Assert.That(attribution.KillOwner, Is.EqualTo(new TotemParticipantId(2)));
        Assert.That(attribution.IndirectElementDamage, Is.EqualTo(12.5f));
        Assert.That(attribution.AssistingParticipantId, Is.EqualTo(new TotemParticipantId(1)));
    }

    [Test]
    public void ConstructionSnapshot_SerializesExactAchievementsAndPublicText()
    {
        var snapshot = new TotemConstructionIntelligenceSnapshot
        {
            participantId = 1,
            teamId = 0,
            capturedAtPhase = (int)TotemMatchPhase.Build2,
            tattoos = new[]
            {
                new TotemPublicTattooSnapshotEntry
                {
                    slot = TotemTattooSlotId.RightArm,
                    pattern = TotemFirstPlayablePatternId.P01,
                    element = TotemFirstPlayableElement.Fire,
                    publicEffectText = "造成有效直接伤害时触发。",
                },
            },
            achievements = new TotemMatchAchievementSnapshot
            {
                playerDamage = 42.5f,
                indirectElementDamage = 12.5f,
                successfulRevives = 1,
            },
        };

        string json = JsonUtility.ToJson(snapshot);
        var restored = JsonUtility.FromJson<TotemConstructionIntelligenceSnapshot>(json);

        Assert.That(restored.tattoos[0].publicEffectText, Is.EqualTo("造成有效直接伤害时触发。"));
        Assert.That(restored.achievements.playerDamage, Is.EqualTo(42.5f));
        Assert.That(restored.achievements.indirectElementDamage, Is.EqualTo(12.5f));
    }

    [Test]
    public void ContractConfig_JsonRoundTripPassesValidationAndReferencesArtChange()
    {
        var config = new TotemFirstPlayableContractConfig
        {
            assets = new[]
            {
                new TotemPresentationAssetContract
                {
                    stableId = "rifle.hit.body",
                    kind = TotemPresentationAssetKind.Vfx,
                    assetKey = TotemFirstPlayableArtHandoff.VfxKeys.RifleHitBody,
                    fallbackKey = TotemFirstPlayableArtHandoff.FallbackKeys.MissingVfx,
                    handoffId = TotemFirstPlayableArtHandoff.VfxDeliveryId,
                },
            },
        };

        string json = JsonUtility.ToJson(config);
        var restored = JsonUtility.FromJson<TotemFirstPlayableContractConfig>(json);
        var errors = new List<string>();

        Assert.That(TotemFirstPlayableContractValidator.Validate(restored, errors), Is.True, string.Join("\n", errors));
        Assert.That(restored.artChangeId, Is.EqualTo("rebaseline-pvpve-art-resources"));
        Assert.That(restored.assets[0].fallbackKey, Is.Not.Empty);
    }

    [Test]
    public void PigmentRequest_RequiresPositiveAmountAndAtomicTransferCommit()
    {
        var request = new TotemPigmentRequest(1, new TotemParticipantId(1), new TotemParticipantId(2), TotemPigmentKind.Ice, 3, 5);
        var transfer = new TotemPigmentTransfer(1, new TotemParticipantId(2), new TotemParticipantId(1), TotemPigmentKind.Ice, 3, 9);

        Assert.That(request.IsValid, Is.True);
        Assert.That(transfer.RequiresAtomicCommit, Is.True);
    }

    [Test]
    public void ActorRoster_UsesSixParticipantsThreeDuoTeamsAndStableSeededSpawns()
    {
        TotemMapSnapshot map = CreateSpawnTestMap(7781);
        TotemActorSpawnInfo[] first = TotemActorService.BuildActorRoster(map);
        TotemActorSpawnInfo[] second = TotemActorService.BuildActorRoster(map);

        Assert.That(first.Length, Is.EqualTo(6));
        int humanCount = 0;
        for (int i = 0; i < first.Length; i++)
        {
            Assert.That(first[i].ActorId, Is.EqualTo(i + 1));
            Assert.That(first[i].TeamId, Is.EqualTo(i / 2));
            Assert.That(first[i].Position, Is.EqualTo(second[i].Position));
            if (first[i].ControllerKind == TotemParticipantControllerKind.Human)
            {
                humanCount++;
            }

            for (int j = 0; j < i; j++)
            {
                Assert.That(Vector3.Distance(first[i].Position, first[j].Position), Is.GreaterThanOrEqualTo(TotemActorService.TeammateSpawnMinDistance - 0.01f));
            }
        }

        Assert.That(humanCount, Is.EqualTo(1));
        for (int team = 0; team < 3; team++)
        {
            float teammateDistance = Vector3.Distance(first[team * 2].Position, first[team * 2 + 1].Position);
            Assert.That(teammateDistance, Is.LessThanOrEqualTo(TotemActorService.TeammateSpawnRadius * 2f + 0.01f));
        }
    }

    [Test]
    public void CombatRelationship_BlocksTeammatesAndAllowsOpponentsAtMatchStart()
    {
        var player = new TotemParticipantModel(1, "Player", TotemParticipantControllerKind.Human, 100f, Vector3.zero, TotemParticipantLifecycle.Active, 0);
        var teammate = new TotemParticipantModel(2, "Teammate", TotemParticipantControllerKind.SmartBot, 100f, Vector3.right, TotemParticipantLifecycle.Active, 0);
        var opponent = new TotemParticipantModel(3, "Opponent", TotemParticipantControllerKind.SmartBot, 100f, Vector3.left, TotemParticipantLifecycle.Active, 1);
        var context = new TotemCombatRelationshipContext(0f);

        TotemCombatRelationshipDecision friendly = TotemCombatRelationshipService.Evaluate(player, teammate, context);
        TotemCombatRelationshipDecision hostile = TotemCombatRelationshipService.Evaluate(player, opponent, context);

        Assert.That(friendly.Allowed, Is.False);
        Assert.That(friendly.Reason, Is.EqualTo(TotemCombatRelationshipReason.BlockedParticipantFriendlyFire));
        Assert.That(hostile.Allowed, Is.True);
        Assert.That(hostile.Reason, Is.EqualTo(TotemCombatRelationshipReason.AllowedParticipantToParticipant));
    }

    [Test]
    public void LocalConfirmation_EntersCombatWithoutLegacySelectionStates()
    {
        var flow = new TotemGameFlowService();

        flow.ConfirmLocalFirstPlayable();

        Assert.That(flow.CurrentState, Is.EqualTo(TotemGameFlowState.CombatHud));
    }

    private static TotemMapSnapshot CreateSpawnTestMap(int seed)
    {
        var anchors = new TotemMapAnchor[6];
        var positions = new[]
        {
            new Vector3(100f, 0f, 100f),
            new Vector3(250f, 0f, 100f),
            new Vector3(400f, 0f, 100f),
            new Vector3(100f, 0f, 400f),
            new Vector3(250f, 0f, 400f),
            new Vector3(400f, 0f, 400f),
        };
        for (int i = 0; i < anchors.Length; i++)
        {
            anchors[i] = new TotemMapAnchor
            {
                AnchorId = $"player.spawn.{i:000}",
                Kind = TotemMapAnchorKind.PlayerSpawn,
                Position = positions[i],
                IsReachable = true,
            };
        }

        return new TotemMapSnapshot
        {
            Seed = seed,
            MapSize = 500f,
            InitialZoneCenter = new Vector2(250f, 250f),
            AnchorPlacements = anchors,
            Rooms = new[]
            {
                new TotemRoomInfo
                {
                    RoomType = TotemRoomType.SouthWestArea,
                    CenterWorld = new Vector3(250f, 0f, 250f),
                },
            },
        };
    }

    private static TotemRosterSlot Slot(int participantId, int teamId, TotemFirstPlayableParticipantKind kind = TotemFirstPlayableParticipantKind.Bot)
    {
        return new TotemRosterSlot(
            new TotemParticipantId(participantId),
            new TotemTeamId(teamId),
            kind,
            TotemFirstPlayableLifeState.Alive);
    }

}
