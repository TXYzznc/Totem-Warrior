#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using System.IO;
using UnityEngine;

public sealed class TotemFirstPlayableUiFormattingTests
{
    [Test]
    public void MainMenu_DeclaresExactFirstPlayableRosterAndFlow()
    {
        StringAssert.Contains("6名参与者", TotemFirstPlayableUiText.MainMenuSummary);
        StringAssert.Contains("3支双人队伍", TotemFirstPlayableUiText.MainMenuSummary);
        StringAssert.Contains("1名玩家 + 5名人机", TotemFirstPlayableUiText.MainMenuSummary);
        StringAssert.Contains("五轮完整流程", TotemFirstPlayableUiText.MainMenuSummary);
        StringAssert.Contains("单一枪械", TotemFirstPlayableUiText.MainMenuSummary);
    }

    [Test]
    public void BuildFormatting_UsesSixSlotsAndExactPigmentEconomy()
    {
        var state = new TotemFirstPlayableTattooBuildState();
        state.SetPigment(TotemPigmentKind.Fire, 10);
        state.SetPigment(TotemPigmentKind.Ice, 20);
        state.SetPigment(TotemPigmentKind.Lightning, 30);

        Assert.That(TotemFirstPlayableTattooBuildState.SlotCount, Is.EqualTo(6));
        Assert.That(TotemFirstPlayableTattooBuildState.EquipPigmentCost, Is.EqualTo(10));
        Assert.That(TotemFirstPlayableTattooBuildState.RemovePigmentRefund, Is.EqualTo(6));
        Assert.That(TotemFirstPlayableHudPresenter.FormatInventory(state), Is.EqualTo("颜料：火 10 · 冰 20 · 雷 30"));
        StringAssert.Contains("空", TotemFirstPlayableHudPresenter.FormatSlot(state.GetSlot(TotemTattooSlotId.Head), TotemTattooSlotId.Head));
    }

    [Test]
    public void IntelligenceFormatting_ContainsPublicBuildAndEveryRequestedAchievementField()
    {
        var snapshot = new TotemConstructionIntelligenceSnapshot
        {
            participantId = 2,
            teamId = 1,
            tattoos = new[]
            {
                new TotemPublicTattooSnapshotEntry
                {
                    slot = TotemTattooSlotId.RightArm,
                    pattern = TotemFirstPlayablePatternId.P01,
                    element = TotemFirstPlayableElement.Fire,
                    publicEffectText = "公开效果文本",
                },
            },
            attributes = new[]
            {
                new TotemAttributeSnapshotEntry { attributeId = "max_health", baseValue = 100f, inMatchBonus = 15f },
            },
            achievements = new TotemMatchAchievementSnapshot
            {
                playerDamage = 1f,
                playerDowns = 2,
                playerEliminations = 3,
                allyHealing = 6f,
                allyShieldOrMitigation = 7f,
                successfulRevives = 8,
                cleansesOrControlRemovals = 9,
                effectiveControlSeconds = 10f,
                effectiveControlCount = 11,
                allyDamageGainCreated = 12f,
                resourcesAcquired = 13,
                resourcesShared = 14,
                selfDowns = 15,
                indirectElementDamage = 16f,
            },
        };

        string text = TotemFirstPlayableHudPresenter.FormatIntelligence(snapshot);
        StringAssert.Contains("公开效果文本", text);
        StringAssert.Contains("基础 100 + 局内 15", text);
        StringAssert.Contains("玩家伤害", text);
        StringAssert.Contains("击倒", text);
        StringAssert.Contains("淘汰", text);
        StringAssert.Contains("治疗队友", text);
        StringAssert.Contains("护盾或减伤", text);
        StringAssert.Contains("成功救援", text);
        StringAssert.Contains("净化解除", text);
        StringAssert.Contains("有效控制", text);
        StringAssert.Contains("队友增伤收益", text);
        StringAssert.Contains("间接元素伤害", text);
        StringAssert.Contains("资源获取", text);
        StringAssert.Contains("分享", text);
        StringAssert.Contains("自身倒地", text);
    }

    [Test]
    public void ResultEvidence_ContainsSeedTimingTeamPlaceholderAndFinalBuild()
    {
        var build = new TotemFirstPlayableTattooBuildState();
        build.SetPigment(TotemPigmentKind.Fire, 10);
        Assert.That(build.TryEquip(
            TotemMatchPhase.OpeningBuild,
            TotemTattooSlotId.RightArm,
            TotemFirstPlayablePatternId.P01,
            TotemFirstPlayableElement.Fire,
            out _), Is.True);

        string evidence = TotemFirstPlayableUiText.FormatResultEvidence(null, null, build, null);
        StringAssert.Contains("Seed 1", evidence);
        StringAssert.Contains("战斗180秒", evidence);
        StringAssert.Contains("队伍 未知", evidence);
        StringAssert.Contains("RightArm:P01/Fire", evidence);
    }

    [Test]
    public void StructuredResultEvidence_WritesComparableLatestAndReplayFiles()
    {
        string directory = Path.Combine(Path.GetTempPath(), "TotemFirstPlayableEvidenceTests", TestContext.CurrentContext.Test.ID);
        var evidence = new TotemFirstPlayableResultEvidence
        {
            capturedUtc = "2026-08-11T00:00:00Z",
            seed = 77,
            fastMode = true,
            finalPhase = TotemMatchPhase.Result.ToString(),
            resultReason = "FiveRoundScoreResolved",
            participants = new[]
            {
                new TotemResultParticipantEvidence
                {
                    participantId = 1,
                    teamId = 0,
                    controller = TotemParticipantControllerKind.Human.ToString(),
                    lifecycle = TotemParticipantLifecycle.Active.ToString(),
                    alive = true,
                    health = 80f,
                    maxHealth = 100f,
                },
            },
            teams = new[] { new TotemResultTeamEvidence { teamId = 0, aliveCount = 1, remainingHealth = 80f } },
            keyConfigs = new[] { TotemFirstPlayableRules.ConfigVersion },
        };

        try
        {
            Assert.That(TotemFirstPlayableResultEvidenceWriter.TryWrite(directory, evidence, out string replayFile, out string error), Is.True, error);
            string latestFile = Path.Combine(directory, "latest.json");
            Assert.That(File.Exists(latestFile), Is.True);
            Assert.That(File.Exists(replayFile), Is.True);
            TotemFirstPlayableResultEvidence restored = JsonUtility.FromJson<TotemFirstPlayableResultEvidence>(File.ReadAllText(latestFile));
            Assert.That(restored.seed, Is.EqualTo(77));
            Assert.That(restored.participants, Has.Length.EqualTo(1));
            Assert.That(restored.keyConfigs, Does.Contain(TotemFirstPlayableRules.ConfigVersion));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
#endif
