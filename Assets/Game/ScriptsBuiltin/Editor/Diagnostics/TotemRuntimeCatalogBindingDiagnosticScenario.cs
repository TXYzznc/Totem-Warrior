#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemRuntimeCatalogBindingDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Runtime Catalog Binding";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemCatalogBindingDiagnosticRuntime]");
            TotemGameRuntime runtime = null;

            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                runtime.StartRuntime();

                var runtimeSnapshot = runtime.CaptureSnapshot();
                context.Assert(runtimeSnapshot.servicesReady, "Catalog binding requires all default runtime services ready.");
                context.AssertEqual(runtimeSnapshot.serviceCount, runtimeSnapshot.readyServiceCount, "catalogBinding.runtime.readyServiceCount");

                var data = RequireService<TotemDataService>(context, runtime, "Data");
                var catalog = data.GameplayCatalog;
                context.Assert(data.GameplayCatalogLoadedFromFile && !data.GameplayCatalogUsingFallback, $"Catalog binding must use external Business catalog: {data.GameplayCatalogMessage}");
                context.Assert(catalog != null, "Catalog binding requires a loaded gameplay catalog.");
                context.AssertEqual("GameData/AIData/DataTables/Business", catalog.source, "catalogBinding.catalog.source");

                var map = RequireService<TotemMapService>(context, runtime, "Map");
                var weapon = RequireService<TotemWeaponService>(context, runtime, "Weapon");
                var skill = RequireService<TotemSkillService>(context, runtime, "Skill");
                var tattoo = RequireService<TotemTattooService>(context, runtime, "Tattoo");
                var zone = RequireService<TotemZoneService>(context, runtime, "Zone");
                var boss = RequireService<TotemBossService>(context, runtime, "Boss");
                var choice = RequireService<TotemChoiceService>(context, runtime, "Choice");
                var chest = RequireService<TotemChestService>(context, runtime, "Chest");
                var audio = RequireService<TotemAudioService>(context, runtime, "Audio");
                var npc = RequireService<TotemNpcService>(context, runtime, "Npc");
                RequireService<TotemActorService>(context, runtime, "Actor");
                RequireService<TotemAIService>(context, runtime, "AI");

                AssertCount(context, catalog.CreateMapTemplates().Length, map.GetRuntimeTemplates(), "catalogBinding.map.templates");
                AssertCount(context, catalog.CreateWeaponDefinitions().Length, weapon.GetRuntimeCatalog(), "catalogBinding.weapon.definitions");
                AssertCount(context, catalog.CreateProjectileDefinitions().Length, weapon.GetRuntimeProjectileCatalog(), "catalogBinding.weapon.projectiles");
                AssertCount(context, catalog.CreateWeaponTraitDefinitions().Length, weapon.GetRuntimeTraitCatalog(), "catalogBinding.weapon.traits");
                AssertCount(context, catalog.CreateWeaponDropDefinitions().Length, weapon.GetRuntimeDropCatalog(), "catalogBinding.weapon.drops");
                AssertCount(context, catalog.CreateSkillDefinitions().Length, skill.GetRuntimeCatalog(), "catalogBinding.skill.definitions");
                AssertCount(context, catalog.CreateTattooDefinitions().Length, tattoo.GetRuntimeCatalog(), "catalogBinding.tattoo.combinations");
                AssertCount(context, catalog.CreateTattooReadingTimeDefinitions().Length, tattoo.GetRuntimeReadingTimes(), "catalogBinding.tattoo.readingTimes");
                AssertCount(context, catalog.CreateTattooEnchantAffixDefinitions().Length, tattoo.GetRuntimeEnchantAffixes(), "catalogBinding.tattoo.enchantAffixes");
                AssertCount(context, catalog.CreateTattooEnchantRecipeDefinitions().Length, tattoo.GetRuntimeEnchantRecipes(), "catalogBinding.tattoo.enchantRecipes");
                AssertCount(context, catalog.CreateZonePhases().Length, zone.GetRuntimePhases(), "catalogBinding.zone.phases");
                AssertCount(context, catalog.CreateBossPhases().Length, boss.GetRuntimePhases(), "catalogBinding.boss.phases");
                AssertCount(context, catalog.CreateChoiceOptions().Length, choice.GetRuntimeCatalog(), "catalogBinding.choice.options");
                AssertCount(context, catalog.CreateEvents().Length, choice.GetRuntimeEvents(), "catalogBinding.choice.events");
                AssertCount(context, catalog.CreateChestRewardDefinitions().Length, chest.GetRuntimeRewardCatalog(), "catalogBinding.chest.rewards");

                var audioSnapshot = audio.CaptureSnapshot();
                context.Detail("catalogBinding.audio.cues.expected", catalog.CreateAudioCueDefinitions().Length);
                context.Detail("catalogBinding.audio.cues.actual", audioSnapshot.cueCount);
                context.AssertEqual(catalog.CreateAudioCueDefinitions().Length, audioSnapshot.cueCount, "catalogBinding.audio.cues");

                var mapSnapshot = TotemMapService.BuildLayout(1, 1, catalog.CreateMapTemplates());
                var expectedNpcs = catalog.CreateNpcModels(mapSnapshot);
                var actualNpcs = npc.BuildRuntimeNpcs(mapSnapshot);
                context.Detail("catalogBinding.npc.definitions.expected", expectedNpcs.Length);
                context.Detail("catalogBinding.npc.definitions.actual", actualNpcs.Length);
                context.AssertEqual(expectedNpcs.Length, actualNpcs.Length, "catalogBinding.npc.definitions");
                context.Assert(actualNpcs.Length > 0, "Runtime NPC service must expose Business catalog NPC definitions.");

                CheckFirstRoundRuntimeContent(context, catalog, map, tattoo, zone, boss, choice, npc);

                context.Pass("Totem runtime services bind to the external Business catalog.");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                Object.DestroyImmediate(runtimeObject);
            }
        }

        private static T RequireService<T>(GFDiagnosticScenarioContext context, TotemGameRuntime runtime, string name) where T : class, ITotemRuntimeService
        {
            var service = runtime.GetService<T>();
            context.Assert(service != null && service.State == TotemRuntimeServiceState.Ready, $"{name} service should be ready.");
            return service;
        }

        private static void AssertCount<T>(GFDiagnosticScenarioContext context, int expected, IReadOnlyCollection<T> actual, string name)
        {
            int actualCount = actual == null ? -1 : actual.Count;
            context.Detail($"{name}.expected", expected);
            context.Detail($"{name}.actual", actualCount);
            context.AssertEqual(expected, actualCount, name);
            context.Assert(actualCount > 0, $"{name} must not fall back to an empty runtime catalog.");
        }

        private static void CheckFirstRoundRuntimeContent(
            GFDiagnosticScenarioContext context,
            TotemGameplayCatalog catalog,
            TotemMapService mapService,
            TotemTattooService tattooService,
            TotemZoneService zoneService,
            TotemBossService bossService,
            TotemChoiceService choiceService,
            TotemNpcService npcService)
        {
            var mapSnapshot = TotemMapService.BuildLayout(407, 1, mapService.GetRuntimeTemplates());
            var roster = TotemActorService.BuildActorRoster(mapSnapshot, new TotemStartupSelection(), catalog.CreateEnemyDefinitions());
            var actorModels = roster.Select(info => new TotemActorModel(info)).ToArray();
            var player = actorModels.First(actor => actor.Kind == TotemActorKind.Player);
            var aiStates = TotemAIService.BuildInitialStates(actorModels, player.Position, catalog.CreateBotProfiles(), catalog.CreateBotBuildPresets());

            int nonBossActors = roster.Count(actor => actor.Kind != TotemActorKind.Boss);
            int smartAiActors = roster.Count(actor => actor.Kind == TotemActorKind.SmartAi);
            int lightAiRuntimeActors = roster.Count(actor => actor.Kind == TotemActorKind.LightAi);
            int bossActors = roster.Count(actor => actor.Kind == TotemActorKind.Boss);
            int smartAiStates = aiStates.Count(state => state.Actor.Kind == TotemActorKind.SmartAi);
            int lightAiRuntimeStates = aiStates.Count(state => state.Actor.Kind == TotemActorKind.LightAi);
            int runtimeNpcCount = npcService.BuildRuntimeNpcs(mapSnapshot).Length;

            context.Detail("catalogBinding.firstRoundContent.nonBossActorsIncludingPlayer", nonBossActors);
            context.Detail("catalogBinding.firstRoundContent.smartAiRuntimeActors", smartAiActors);
            context.Detail("catalogBinding.firstRoundContent.lightAiRuntimeActorsFromReusedProfiles", lightAiRuntimeActors);
            context.Detail("catalogBinding.firstRoundContent.bossActors", bossActors);
            context.Detail("catalogBinding.firstRoundContent.aiStates.smartRuntimeControllers", smartAiStates);
            context.Detail("catalogBinding.firstRoundContent.aiStates.lightRuntimeControllersFromReusedProfiles", lightAiRuntimeStates);
            context.Detail("catalogBinding.firstRoundContent.tattooCombinations.runtime", tattooService.GetRuntimeCatalog().Count);
            context.Detail("catalogBinding.firstRoundContent.shopStocks.catalog", catalog.shopStocks.Length);
            context.Detail("catalogBinding.firstRoundContent.npcs.runtime", runtimeNpcCount);
            context.Detail("catalogBinding.firstRoundContent.threeChoiceOptions.runtime", choiceService.GetRuntimeCatalog().Count);
            context.Detail("catalogBinding.firstRoundContent.zonePhases.runtime", zoneService.GetRuntimePhases().Count);
            context.Detail("catalogBinding.firstRoundContent.bossPhases.runtime", bossService.GetRuntimePhases().Count);

            context.AssertEqual(50, nonBossActors, "catalogBinding.firstRoundContent.nonBossActorsIncludingPlayer");
            context.AssertEqual(20, smartAiActors, "catalogBinding.firstRoundContent.smartAiRuntimeActors");
            context.AssertEqual(29, lightAiRuntimeActors, "catalogBinding.firstRoundContent.lightAiRuntimeActorsFromReusedProfiles");
            context.AssertEqual(1, bossActors, "catalogBinding.firstRoundContent.bossActors");
            context.AssertEqual(20, smartAiStates, "catalogBinding.firstRoundContent.aiStates.smartRuntimeControllers");
            context.AssertEqual(29, lightAiRuntimeStates, "catalogBinding.firstRoundContent.aiStates.lightRuntimeControllersFromReusedProfiles");
            context.AssertEqual(336, tattooService.GetRuntimeCatalog().Count, "catalogBinding.firstRoundContent.tattooCombinations.runtime");
            context.AssertEqual(15, catalog.shopStocks.Length, "catalogBinding.firstRoundContent.shopStocks.catalog");
            context.AssertEqual(5, runtimeNpcCount, "catalogBinding.firstRoundContent.npcs.runtime");
            context.AssertEqual(3, TotemChoiceService.BuildThreeChoices("catalog_binding_first_round", 7, choiceService.GetRuntimeCatalog()).Options.Length, "catalogBinding.firstRoundContent.threeChoiceRoll.runtime");
            context.AssertEqual(3, zoneService.GetRuntimePhases().Count, "catalogBinding.firstRoundContent.zonePhases.runtime");
            context.AssertEqual(3, bossService.GetRuntimePhases().Count, "catalogBinding.firstRoundContent.bossPhases.runtime");
        }
    }
}
#endif
