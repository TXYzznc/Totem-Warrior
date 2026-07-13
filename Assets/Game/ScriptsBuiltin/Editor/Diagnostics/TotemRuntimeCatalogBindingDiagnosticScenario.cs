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
                var choice = RequireService<TotemChoiceService>(context, runtime, "Choice");
                var chest = RequireService<TotemChestService>(context, runtime, "Chest");
                var audio = RequireService<TotemAudioService>(context, runtime, "Audio");
                var npc = RequireService<TotemNpcService>(context, runtime, "Npc");
                var enemy = RequireService<TotemEnemyService>(context, runtime, "Enemy");
                RequireService<TotemEnemyWorldService>(context, runtime, "EnemyWorld");
                var enemyLoot = RequireService<TotemEnemyLootService>(context, runtime, "EnemyLoot");
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
                context.AssertEqual(9, catalog.CreateBossPhases().Length, "catalogBinding.boss.phases");
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

                CheckNativeEnemyRuntimeBindings(context, catalog, map, enemy, enemyLoot);
                CheckFirstRoundRuntimeContent(context, catalog, map, tattoo, zone, choice, npc);

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

        private static void CheckNativeEnemyRuntimeBindings(
            GFDiagnosticScenarioContext context,
            TotemGameplayCatalog catalog,
            TotemMapService mapService,
            TotemEnemyService enemyService,
            TotemEnemyLootService enemyLootService)
        {
            var enemyDefinitions = catalog.CreateEnemyDefinitions();
            context.AssertEqual(15, enemyDefinitions.Length, "catalogBinding.enemy.catalogDefinitionCount");
            context.AssertEqual(enemyDefinitions.Length, enemyService.DefinitionCount, "catalogBinding.enemy.runtimeDefinitionCount");
            foreach (var source in enemyDefinitions)
            {
                bool found = enemyService.TryGetDefinition(source.EnemyId, out var runtimeDefinition);
                context.Assert(found, $"Runtime Enemy service must bind EnemyConfig: {source.EnemyId}");
                if (!found)
                {
                    continue;
                }

                context.AssertEqual(source.Tier, runtimeDefinition.tier, $"catalogBinding.enemy.{source.EnemyId}.tier");
                context.AssertEqual(source.RuntimeAssetKey, runtimeDefinition.runtimeAssetKey, $"catalogBinding.enemy.{source.EnemyId}.runtimeAssetKey");
                context.AssertEqual(source.LootTableId, runtimeDefinition.lootTableId, $"catalogBinding.enemy.{source.EnemyId}.lootTableId");
                context.AssertEqual(CountDelimitedIds(source.AbilityIds), runtimeDefinition.abilities.Length, $"catalogBinding.enemy.{source.EnemyId}.abilityCount");
            }

            var lootSnapshot = enemyLootService.CaptureSnapshot();
            context.Detail("catalogBinding.enemyLoot.catalogDefinitionCount", catalog.enemyLoot.Length);
            context.Detail("catalogBinding.enemyLoot.runtimeDefinitionCount", lootSnapshot.definitionCount);
            context.AssertEqual(37, lootSnapshot.definitionCount, "catalogBinding.enemyLoot.runtimeDefinitionCount");

            var encounterService = new TotemEncounterService();
            var encounterDefinitions = catalog.CreateEncounterSpawnDefinitions();
            context.AssertEqual(9, encounterDefinitions.Length, "catalogBinding.encounter.catalogDefinitionCount");
            for (int themeId = 1; themeId <= 3; themeId++)
            {
                var map = TotemMapService.BuildLayout(700 + themeId, themeId, mapService.GetRuntimeTemplates());
                int planSeed = 1700 + themeId;
                var enemyRootBefore = GameObject.Find("[TotemEnemies]");
                int enemyChildrenBefore = enemyRootBefore == null ? 0 : enemyRootBefore.transform.childCount;
                var plan = encounterService.BuildSpawnPlan(map, map.ThemeName, encounterDefinitions, enemyDefinitions, planSeed);
                var repeatedPlan = encounterService.BuildSpawnPlan(map, map.ThemeName, encounterDefinitions, enemyDefinitions, planSeed);
                var enemyRootAfter = GameObject.Find("[TotemEnemies]");
                int enemyChildrenAfter = enemyRootAfter == null ? 0 : enemyRootAfter.transform.childCount;
                context.Detail($"catalogBinding.encounter.theme{themeId}.planEntries", plan.Entries.Length);
                context.Detail($"catalogBinding.encounter.theme{themeId}.rejections", plan.Rejections.Length);
                context.Assert(plan.Entries.Length > 0, $"Encounter runtime must build spawn entries for theme {map.ThemeName}.");
                AssertSpawnPlansEqual(context, plan, repeatedPlan, themeId);
                context.AssertEqual(enemyChildrenBefore, enemyChildrenAfter, $"catalogBinding.encounter.theme{themeId}.purePlanBuildEnemyChildren");
                context.Assert(ReferenceEquals(enemyRootBefore, enemyRootAfter), $"SpawnPlan construction must not create or replace the enemy scene root for theme {map.ThemeName}.");
                context.Assert(!plan.Rejections.Any(rejection =>
                        rejection.Reason == TotemSpawnPlanRejectionReason.InvalidConfig
                        || rejection.Reason == TotemSpawnPlanRejectionReason.MissingEnemyPool),
                    $"Encounter runtime must resolve configuration and enemy pools for theme {map.ThemeName}.");
            }
        }

        private static void AssertSpawnPlansEqual(
            GFDiagnosticScenarioContext context,
            TotemSpawnPlan expected,
            TotemSpawnPlan actual,
            int themeId)
        {
            context.AssertEqual(expected.Entries.Length, actual.Entries.Length, $"catalogBinding.encounter.theme{themeId}.deterministicEntryCount");
            context.AssertEqual(expected.Rejections.Length, actual.Rejections.Length, $"catalogBinding.encounter.theme{themeId}.deterministicRejectionCount");

            int entryCount = Mathf.Min(expected.Entries.Length, actual.Entries.Length);
            for (int i = 0; i < entryCount; i++)
            {
                var left = expected.Entries[i];
                var right = actual.Entries[i];
                context.Assert(
                    left.PlanEntryId == right.PlanEntryId
                    && left.EncounterId == right.EncounterId
                    && left.EnemyId == right.EnemyId
                    && left.AnchorId == right.AnchorId
                    && left.Position == right.Position
                    && left.WaveIndex == right.WaveIndex
                    && left.WaveSlot == right.WaveSlot
                    && Mathf.Approximately(left.TriggerTime, right.TriggerTime)
                    && left.PlacementSeed == right.PlacementSeed,
                    $"Repeated SpawnPlan entry {i} must be identical for theme {themeId}.");
            }

            int rejectionCount = Mathf.Min(expected.Rejections.Length, actual.Rejections.Length);
            for (int i = 0; i < rejectionCount; i++)
            {
                var left = expected.Rejections[i];
                var right = actual.Rejections[i];
                context.Assert(
                    left.EncounterId == right.EncounterId
                    && left.WaveIndex == right.WaveIndex
                    && left.WaveSlot == right.WaveSlot
                    && Mathf.Approximately(left.TriggerTime, right.TriggerTime)
                    && left.AnchorId == right.AnchorId
                    && left.Reason == right.Reason,
                    $"Repeated SpawnPlan rejection {i} must be identical for theme {themeId}.");
            }
        }

        private static int CountDelimitedIds(string value)
        {
            return (value ?? string.Empty).Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private static void CheckFirstRoundRuntimeContent(
            GFDiagnosticScenarioContext context,
            TotemGameplayCatalog catalog,
            TotemMapService mapService,
            TotemTattooService tattooService,
            TotemZoneService zoneService,
            TotemChoiceService choiceService,
            TotemNpcService npcService)
        {
            var mapSnapshot = TotemMapService.BuildLayout(407, 1, mapService.GetRuntimeTemplates());
            var roster = TotemActorService.BuildActorRoster(mapSnapshot, new TotemStartupSelection());
            var actorModels = roster.Select(info => new TotemActorModel(info)).ToArray();
            var player = actorModels.First(actor => actor.Kind == TotemActorKind.Player);
            var aiStates = TotemAIService.BuildInitialStates(actorModels, player.Position, catalog.CreateBotProfiles(), catalog.CreateBotBuildPresets());

            int humanParticipants = roster.Count(actor => actor.ControllerKind == TotemParticipantControllerKind.Human);
            int smartBotParticipants = roster.Count(actor => actor.ControllerKind == TotemParticipantControllerKind.SmartBot);
            int lightBotParticipants = roster.Count(actor => actor.ControllerKind == TotemParticipantControllerKind.LightBot);
            int smartAiStates = aiStates.Count(state => state.Actor.ControllerKind == TotemParticipantControllerKind.SmartBot);
            int lightAiRuntimeStates = aiStates.Count(state => state.Actor.ControllerKind == TotemParticipantControllerKind.LightBot);
            int runtimeNpcCount = npcService.BuildRuntimeNpcs(mapSnapshot).Length;

            context.Detail("catalogBinding.firstRoundContent.participantCount", roster.Length);
            context.Detail("catalogBinding.firstRoundContent.humanParticipants", humanParticipants);
            context.Detail("catalogBinding.firstRoundContent.smartBotParticipants", smartBotParticipants);
            context.Detail("catalogBinding.firstRoundContent.lightBotParticipants", lightBotParticipants);
            context.Detail("catalogBinding.firstRoundContent.aiStates.smartRuntimeControllers", smartAiStates);
            context.Detail("catalogBinding.firstRoundContent.aiStates.lightRuntimeControllersFromReusedProfiles", lightAiRuntimeStates);
            context.Detail("catalogBinding.firstRoundContent.tattooCombinations.runtime", tattooService.GetRuntimeCatalog().Count);
            context.Detail("catalogBinding.firstRoundContent.shopStocks.catalog", catalog.shopStocks.Length);
            context.Detail("catalogBinding.firstRoundContent.npcs.runtime", runtimeNpcCount);
            context.Detail("catalogBinding.firstRoundContent.threeChoiceOptions.runtime", choiceService.GetRuntimeCatalog().Count);
            context.Detail("catalogBinding.firstRoundContent.zonePhases.runtime", zoneService.GetRuntimePhases().Count);
            context.Detail("catalogBinding.firstRoundContent.bossPhases.runtime", catalog.CreateBossPhases().Length);

            context.AssertEqual(50, roster.Length, "catalogBinding.firstRoundContent.participantCount");
            context.AssertEqual(1, humanParticipants, "catalogBinding.firstRoundContent.humanParticipants");
            context.AssertEqual(20, smartBotParticipants, "catalogBinding.firstRoundContent.smartBotParticipants");
            context.AssertEqual(29, lightBotParticipants, "catalogBinding.firstRoundContent.lightBotParticipants");
            context.Assert(roster.All(actor => TotemActorService.IsParticipantKind(actor.Kind)),
                "Catalog binding Actor roster must contain Participant kinds only.");
            context.AssertEqual(20, smartAiStates, "catalogBinding.firstRoundContent.aiStates.smartRuntimeControllers");
            context.AssertEqual(29, lightAiRuntimeStates, "catalogBinding.firstRoundContent.aiStates.lightRuntimeControllersFromReusedProfiles");
            context.AssertEqual(336, tattooService.GetRuntimeCatalog().Count, "catalogBinding.firstRoundContent.tattooCombinations.runtime");
            context.AssertEqual(15, catalog.shopStocks.Length, "catalogBinding.firstRoundContent.shopStocks.catalog");
            context.AssertEqual(5, runtimeNpcCount, "catalogBinding.firstRoundContent.npcs.runtime");
            context.AssertEqual(3, TotemChoiceService.BuildThreeChoices("catalog_binding_first_round", 7, choiceService.GetRuntimeCatalog()).Options.Length, "catalogBinding.firstRoundContent.threeChoiceRoll.runtime");
            context.AssertEqual(3, zoneService.GetRuntimePhases().Count, "catalogBinding.firstRoundContent.zonePhases.runtime");
            context.AssertEqual(9, catalog.CreateBossPhases().Length, "catalogBinding.firstRoundContent.bossPhases.runtime");
        }
    }
}
#endif
