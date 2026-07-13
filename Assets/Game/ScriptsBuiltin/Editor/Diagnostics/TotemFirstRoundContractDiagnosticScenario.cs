#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemFirstRoundContractDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem First Round Contract";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            context.Assert(
                TotemDataService.TryLoadGameplayCatalogFromFile(TotemDataService.GetGameplayCatalogPath(), out var catalog, out string loadError),
                $"First round contract must load the AI-editable gameplay catalog: {loadError}");

            var validationErrors = new List<string>();
            context.Assert(
                TotemGameplayCatalogValidator.Validate(catalog, validationErrors),
                "First round gameplay catalog must validate: " + string.Join("; ", validationErrors));

            CheckCatalogCounts(context, catalog);
            CheckActorAndAiCounts(context, catalog);
            CheckNpcChoiceZoneAndBossCounts(context, catalog);
            context.Pass("First-round Totem contract is covered by GF_X runtime data.");
        }

        private static void CheckCatalogCounts(GFDiagnosticScenarioContext context, TotemGameplayCatalog catalog)
        {
            context.AssertEqual(31, catalog.items.Length, "firstRound.items");
            context.AssertEqual(14, catalog.resources.Length, "firstRound.resources");
            context.AssertEqual(5, catalog.weapons.Length, "firstRound.weapons");
            context.AssertEqual(10, catalog.weaponTraits.Length, "firstRound.weaponTraits");
            context.AssertEqual(2, catalog.projectiles.Length, "firstRound.projectiles");
            context.AssertEqual(15, catalog.weaponDrops.Length, "firstRound.weaponDrops");
            context.AssertEqual(6, catalog.chestRewards.Length, "firstRound.chestRewards");
            context.AssertEqual(14, catalog.skills.Length, "firstRound.skills");
            context.AssertEqual(15, catalog.enemies.Length, "firstRound.enemies");
            context.AssertEqual(25, catalog.enemyAbilities.Length, "firstRound.enemyAbilities");
            context.AssertEqual(9, catalog.encounterSpawns.Length, "firstRound.encounterSpawns");
            context.AssertEqual(37, catalog.enemyLoot.Length, "firstRound.enemyLoot");
            context.AssertEqual(9, catalog.bossPhases.Length, "firstRound.bossPhases");

            var tattoos = catalog.CreateTattooDefinitions();
            context.AssertEqual(336, tattoos.Length, "firstRound.tattooCombinations");
            context.AssertEqual(6, catalog.tattooParts.Length, "firstRound.tattooParts");
            context.AssertEqual(7, catalog.tattooColors.Length, "firstRound.tattooColors");
            context.AssertEqual(8, catalog.tattooPatterns.Length, "firstRound.tattooPatterns");
            context.AssertEqual(8, catalog.tattooShapes.Length, "firstRound.tattooShapes");
            context.AssertEqual(6, catalog.tattooReadingTimes.Length, "firstRound.tattooReadingTimes");
            context.AssertEqual(24, catalog.tattooEnchantAffixes.Length, "firstRound.tattooEnchantAffixes");
            context.AssertEqual(3, catalog.tattooEnchantRecipes.Length, "firstRound.tattooEnchantRecipes");
            context.Assert(tattoos.Any(item => item.PartName == "RightArm" && item.ColorName == "Red" && item.PatternName == "Line"), "First-round tattoo catalog must include RightArm/Red/Line.");
        }

        private static void CheckActorAndAiCounts(GFDiagnosticScenarioContext context, TotemGameplayCatalog catalog)
        {
            var enemies = catalog.CreateEnemyDefinitions();
            context.AssertEqual(8, enemies.Count(enemy => enemy.Tier == TotemEnemyTier.Light), "firstRound.enemyRows.light");
            context.AssertEqual(4, enemies.Count(enemy => enemy.Tier == TotemEnemyTier.Elite), "firstRound.enemyRows.elite");
            context.AssertEqual(3, enemies.Count(enemy => enemy.Tier == TotemEnemyTier.Boss), "firstRound.enemyRows.boss");
            context.AssertEqual(23, catalog.botProfiles.Length, "firstRound.botProfiles");
            context.AssertEqual(20, catalog.CreateBotProfiles().Count(profile => profile.ActorKind == TotemActorKind.SmartAi), "firstRound.smartProfiles");
            context.AssertEqual(3, catalog.CreateBotProfiles().Count(profile => profile.ActorKind == TotemActorKind.LightAi), "firstRound.lightProfiles");
            context.AssertEqual(7, catalog.botBuildPresets.Length, "firstRound.botBuildPresets");

            var map = TotemMapService.BuildLayout(seed: 407, themeId: 1);
            var roster = TotemActorService.BuildActorRoster(map, new TotemStartupSelection
            {
                CharacterId = 1,
                ColorId = 1,
                WeaponId = "knife_basic",
                PatternIds = new[] { 1 },
            });
            int humanParticipants = roster.Count(actor => actor.ControllerKind == TotemParticipantControllerKind.Human);
            int smartBotParticipants = roster.Count(actor => actor.ControllerKind == TotemParticipantControllerKind.SmartBot);
            int lightBotParticipants = roster.Count(actor => actor.ControllerKind == TotemParticipantControllerKind.LightBot);
            context.Detail("firstRound.contentSummary.participantCount", roster.Length);
            context.Detail("firstRound.contentSummary.humanParticipants", humanParticipants);
            context.Detail("firstRound.contentSummary.smartBotParticipants", smartBotParticipants);
            context.Detail("firstRound.contentSummary.lightBotParticipants", lightBotParticipants);
            context.AssertEqual(50, roster.Length, "firstRound.actorRoster.participantCount");
            context.AssertEqual(1, humanParticipants, "firstRound.actorRoster.human");
            context.AssertEqual(20, smartBotParticipants, "firstRound.actorRoster.smartBot");
            context.AssertEqual(29, lightBotParticipants, "firstRound.actorRoster.lightBot");
            context.Assert(roster.All(actor => TotemActorService.IsParticipantKind(actor.Kind)),
                "First-round Actor roster must contain Participant kinds only.");
            var models = roster.Select(info => new TotemActorModel(info)).ToArray();
            var player = models.First(actor => actor.Kind == TotemActorKind.Player);
            var aiStates = TotemAIService.BuildInitialStates(models, player.Position, catalog.CreateBotProfiles(), catalog.CreateBotBuildPresets());
            int smartAiStates = aiStates.Count(state => state.Actor.Kind == TotemActorKind.SmartAi);
            int lightAiRuntimeStates = aiStates.Count(state => state.Actor.Kind == TotemActorKind.LightAi);
            context.Detail("firstRound.contentSummary.aiStates.totalRuntimeControllers", aiStates.Length);
            context.Detail("firstRound.contentSummary.aiStates.smartRuntimeControllers", smartAiStates);
            context.Detail("firstRound.contentSummary.aiStates.lightRuntimeControllersFromReusedProfiles", lightAiRuntimeStates);
            context.AssertEqual(49, aiStates.Length, "firstRound.aiStates.total");
            context.AssertEqual(20, smartAiStates, "firstRound.aiStates.smart");
            context.AssertEqual(29, lightAiRuntimeStates, "firstRound.aiStates.lightRuntimeActorsFromReusedProfiles");
            context.AssertEqual(20, aiStates.Count(state => state.Actor.Kind == TotemActorKind.SmartAi && state.Profile != null && state.BuildPreset != null), "firstRound.aiStates.profiledSmart");
            context.AssertEqual(29, aiStates.Count(state => state.Actor.Kind == TotemActorKind.LightAi && state.Profile != null), "firstRound.aiStates.profiledLightRuntimeActorsFromReusedProfiles");
        }

        private static void CheckNpcChoiceZoneAndBossCounts(GFDiagnosticScenarioContext context, TotemGameplayCatalog catalog)
        {
            context.AssertEqual(3, catalog.mapTemplates.Length, "firstRound.mapTemplates");
            context.AssertEqual(5, catalog.npcs.Length, "firstRound.npcs");
            context.AssertEqual(15, catalog.shopStocks.Length, "firstRound.shopStocks");
            context.AssertEqual(9, catalog.merchantSlots.Length, "firstRound.merchantSlots");
            context.AssertEqual(6, catalog.events.Length, "firstRound.events");
            context.AssertEqual(11, catalog.choiceOptions.Length, "firstRound.choiceOptions");
            context.AssertEqual(3, catalog.zonePhases.Length, "firstRound.zonePhases");
            context.AssertEqual(14, catalog.audioCues.Length, "firstRound.audioCues");
            context.Detail("firstRound.contentSummary.shopNpcThreeChoiceZoneBoss", "shop=15 stocks/9 merchant slots; npc=5; threeChoice=11 options; zone=3 phases; boss=3 groups/9 phases");

            var map = TotemMapService.BuildLayout(seed: 408, themeId: 1);
            var npcs = catalog.CreateNpcModels(map);
            context.Assert(npcs.Any(npc => npc.NpcId == "tattooist_default"), "First-round NPC catalog must include tattooist_default.");
            context.Assert(npcs.Any(npc => npc.NpcId == "merchant_general"), "First-round NPC catalog must include merchant_general.");
            context.Assert(npcs.Any(npc => npc.NpcId == "merchant_alien"), "First-round NPC catalog must include merchant_alien.");
            context.Assert(catalog.CreateChoiceOptions().Any(option => option.EffectType == TotemChoiceEffectType.SkillAcquire), "First-round choices must include SkillAcquire.");
            var bossPhases = catalog.CreateBossPhases();
            var bossIds = catalog.CreateEnemyDefinitions()
                .Where(enemy => enemy.Tier == TotemEnemyTier.Boss)
                .Select(enemy => enemy.EnemyId)
                .ToHashSet();
            var phaseGroups = bossPhases.GroupBy(phase => phase.BossId).ToArray();
            context.AssertEqual(3, phaseGroups.Length, "firstRound.bossPhaseGroups");
            context.Assert(phaseGroups.All(group => bossIds.Contains(group.Key)), "Every first-round Boss phase group must resolve to a Boss EnemyConfig row.");
            context.Assert(phaseGroups.All(group => group.Select(phase => phase.PhaseIndex).OrderBy(index => index).SequenceEqual(new[] { 1, 2, 3 })), "Every first-round Boss must define phases 1, 2, and 3.");
            context.Assert(phaseGroups.All(group => group.Single(phase => phase.PhaseIndex == 3).EnrageMultiplier > 1f), "Every first-round Boss phase 3 must increase pressure.");
            context.Assert(phaseGroups.All(group => !string.IsNullOrWhiteSpace(group.Single(phase => phase.PhaseIndex == 3).DeathPatternRecipeId)), "Every first-round Boss phase 3 must bind a death recipe.");
        }
    }
}
#endif
