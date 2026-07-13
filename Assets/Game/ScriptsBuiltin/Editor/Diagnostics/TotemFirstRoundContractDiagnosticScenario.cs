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
            context.AssertEqual(0, catalog.resources.Length, "firstRound.resources");
            context.AssertEqual(5, catalog.weapons.Length, "firstRound.weapons");
            context.AssertEqual(10, catalog.weaponTraits.Length, "firstRound.weaponTraits");
            context.AssertEqual(2, catalog.projectiles.Length, "firstRound.projectiles");
            context.AssertEqual(15, catalog.weaponDrops.Length, "firstRound.weaponDrops");
            context.AssertEqual(6, catalog.chestRewards.Length, "firstRound.chestRewards");
            context.AssertEqual(14, catalog.skills.Length, "firstRound.skills");

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
            context.AssertEqual(3, catalog.enemies.Length, "firstRound.enemyRows");
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
            }, catalog.CreateEnemyDefinitions());
            int nonBossActors = roster.Count(actor => actor.Kind != TotemActorKind.Boss);
            int smartAiActors = roster.Count(actor => actor.Kind == TotemActorKind.SmartAi);
            int lightAiRuntimeActors = roster.Count(actor => actor.Kind == TotemActorKind.LightAi);
            int bossActors = roster.Count(actor => actor.Kind == TotemActorKind.Boss);
            context.Detail("firstRound.contentSummary.nonBossActorsIncludingPlayer", nonBossActors);
            context.Detail("firstRound.contentSummary.smartAiRuntimeActors", smartAiActors);
            context.Detail("firstRound.contentSummary.lightAiRuntimeActorsFromReusedProfiles", lightAiRuntimeActors);
            context.Detail("firstRound.contentSummary.bossActors", bossActors);
            context.AssertEqual(51, roster.Length, "firstRound.actorRoster.totalIncludingBoss");
            context.AssertEqual(50, nonBossActors, "firstRound.actorRoster.nonBossIncludingPlayer");
            context.AssertEqual(1, roster.Count(actor => actor.Kind == TotemActorKind.Player), "firstRound.actorRoster.player");
            context.AssertEqual(20, smartAiActors, "firstRound.actorRoster.smartAi");
            context.AssertEqual(29, lightAiRuntimeActors, "firstRound.actorRoster.lightAiRuntimeActorsFromReusedProfiles");
            context.AssertEqual(1, bossActors, "firstRound.actorRoster.boss");

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
            context.AssertEqual(3, catalog.bossPhases.Length, "firstRound.bossPhases");
            context.AssertEqual(14, catalog.audioCues.Length, "firstRound.audioCues");
            context.Detail("firstRound.contentSummary.shopNpcThreeChoiceZoneBoss", "shop=15 stocks/9 merchant slots; npc=5; threeChoice=11 options; zone=3 phases; boss=3 phases");

            var map = TotemMapService.BuildLayout(seed: 408, themeId: 1);
            var npcs = catalog.CreateNpcModels(map);
            context.Assert(npcs.Any(npc => npc.NpcId == "tattooist_default"), "First-round NPC catalog must include tattooist_default.");
            context.Assert(npcs.Any(npc => npc.NpcId == "merchant_general"), "First-round NPC catalog must include merchant_general.");
            context.Assert(npcs.Any(npc => npc.NpcId == "merchant_alien"), "First-round NPC catalog must include merchant_alien.");
            context.Assert(catalog.CreateChoiceOptions().Any(option => option.EffectType == TotemChoiceEffectType.SkillAcquire), "First-round choices must include SkillAcquire.");
            context.Assert(catalog.CreateBossPhases().Any(phase => phase.PhaseIndex == 3 && phase.EnrageMultiplier > 1f), "First-round boss phases must include phase 3 enrage.");
        }
    }
}
#endif
