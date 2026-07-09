#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemGameplayRuntimeDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Gameplay Runtime";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            CheckRuntimeTypes(context);
            CheckMapLayout(context);
            CheckMapTerrainContracts(context);
            CheckMapAnchorContracts(context);
            CheckMapRuntimeObjects(context);
            CheckMapTerrainMovement(context);
            CheckActorRoster(context);
            CheckMapAnchorConsumers(context);
            CheckMapResourceAndEventAnchorConsumers(context);
            CheckStartupSelectionRuntime(context);
            CheckActorAnimationRuntime(context);
            CheckInputMath(context);
            CheckSelfTattooUIInputRouting(context);
            CheckCombatMath(context);
            CheckCombatStatusControl(context);
            CheckCameraRuntime(context);
            CheckCombatLifecycleCleanup(context);
            context.Pass("Totem gameplay runtime contract is ready.");
        }

        private static void CheckRuntimeTypes(GFDiagnosticScenarioContext context)
        {
            string[] required =
            {
                "ITotemRuntimeTickService",
                "ITotemRuntimeLateTickService",
                "TotemMapService",
                "TotemActorService",
                "TotemSettingsService",
                "TotemEconomyService",
                "TotemStatusService",
                "TotemTattooService",
                "TotemWeaponService",
                "TotemSkillService",
                "TotemZoneService",
                "TotemBossService",
                "TotemAIService",
                "TotemNpcService",
                "TotemChoiceService",
                "TotemInteractionService",
                "TotemCameraService",
                "TotemVfxService",
                "TotemCombatService",
                "TotemGameplayModels",
                "TotemExtendedGameplayModels",
                "TotemActorAnimationSnapshot",
                "TotemUISnapshot",
            };

            for (int i = 0; i < required.Length; i++)
            {
                if (required[i] == "TotemGameplayModels")
                {
                    context.Assert(ResolveType("TotemMapSnapshot") != null && ResolveType("TotemMapRuntimeSnapshot") != null && ResolveType("TotemActorModel") != null && ResolveType("TotemTerrainType") != null && ResolveType("TotemMapAnchor") != null, "Gameplay model types must be resolvable.");
                    continue;
                }

                if (required[i] == "TotemExtendedGameplayModels")
                {
                    context.Assert(ResolveType("TotemTattooDefinition") != null && ResolveType("TotemWeaponDefinition") != null, "Extended gameplay model types must be resolvable.");
                    continue;
                }

                if (required[i] == "TotemActorAnimationSnapshot")
                {
                    context.Assert(ResolveType("TotemActorAnimationSnapshot") != null, "Actor animation snapshot type must be resolvable.");
                    continue;
                }

                if (required[i] == "TotemUISnapshot")
                {
                    context.Assert(ResolveType("TotemUISnapshot") != null, "UI snapshot type must be resolvable.");
                    continue;
                }

                context.Assert(ResolveType(required[i]) != null, $"Type can not be resolved: {required[i]}");
            }
        }

        private static void CheckMapLayout(GFDiagnosticScenarioContext context)
        {
            var map = TotemMapService.BuildLayout(seed: 1, themeId: 1);
            context.AssertEqual(400f, map.MapSize, "map.size");
            context.AssertEqual("AI_RUINS", map.ThemeName, "map.themeName");
            context.AssertEqual(101, map.TerrainPoolId, "map.terrainPoolId");
            context.AssertEqual("#66CCFF", map.HudAccentColor, "map.hudAccentColor");
            context.AssertEqual("#3A4858", map.DominantColor, "map.dominantColor");
            context.AssertEqual(4, map.BspMaxDepth, "map.bspMaxDepth");
            context.AssertEqual(4, map.Rooms.Length, "map.roomCount");
            RequireRoom(context, map, TotemRoomType.SpawnRoom);
            RequireRoom(context, map, TotemRoomType.TattooStudio);
            RequireRoom(context, map, TotemRoomType.Merchant);
            RequireRoom(context, map, TotemRoomType.BossRoom);
            context.Assert(map.InitialZoneCenter.x >= 400f / 3f && map.InitialZoneCenter.x <= 400f * 2f / 3f, "Initial zone center X must be inside the middle third.");
            context.Assert(map.InitialZoneCenter.y >= 400f / 3f && map.InitialZoneCenter.y <= 400f * 2f / 3f, "Initial zone center Y must be inside the middle third.");

            var alienHive = TotemMapService.BuildLayout(seed: 1, themeId: 2);
            context.AssertEqual("ALIEN_HIVE", alienHive.ThemeName, "map.alienHive.themeName");
            context.AssertEqual(102, alienHive.TerrainPoolId, "map.alienHive.terrainPoolId");
            context.AssertEqual("#7DFF88", alienHive.HudAccentColor, "map.alienHive.hudAccentColor");

            var virusSwamp = TotemMapService.BuildLayout(seed: 1, themeId: 3);
            context.AssertEqual("VIRUS_SWAMP", virusSwamp.ThemeName, "map.virusSwamp.themeName");
            context.AssertEqual(103, virusSwamp.TerrainPoolId, "map.virusSwamp.terrainPoolId");
            context.AssertEqual("#233A35", virusSwamp.DominantColor, "map.virusSwamp.dominantColor");
        }

        private static void CheckMapTerrainContracts(GFDiagnosticScenarioContext context)
        {
            var ruins = TotemMapService.BuildLayout(seed: 11, themeId: 1);
            var alienHive = TotemMapService.BuildLayout(seed: 11, themeId: 2);
            var virusSwamp = TotemMapService.BuildLayout(seed: 11, themeId: 3);

            AssertTerrainGrid(context, ruins, "map.terrain.ruins");
            AssertTerrainGrid(context, alienHive, "map.terrain.alienHive");
            AssertTerrainGrid(context, virusSwamp, "map.terrain.virusSwamp");
            context.Assert(!TerrainGridsEqual(ruins, alienHive), "Alien Hive terrain grid must differ from AI Ruins.");
            context.Assert(!TerrainGridsEqual(ruins, virusSwamp), "Virus Swamp terrain grid must differ from AI Ruins.");
            context.AssertEqual(TotemTerrainType.Blocked, TotemMapService.QueryTerrain(ruins, new Vector3(-1f, 0f, 10f)), "map.terrain.outOfBounds");
            context.Assert(TotemMapService.IsTerrainWalkable(TotemTerrainType.Ground), "Ground terrain must be walkable.");
            context.Assert(TotemMapService.IsTerrainWalkable(TotemTerrainType.Slow), "Slow terrain must be walkable.");
            context.Assert(TotemMapService.IsTerrainWalkable(TotemTerrainType.Cover), "Cover terrain must be walkable.");
            context.Assert(TotemMapService.IsTerrainWalkable(TotemTerrainType.Hazard), "Hazard terrain must be walkable.");
            context.Assert(!TotemMapService.IsTerrainWalkable(TotemTerrainType.Blocked), "Blocked terrain must reject movement.");
            AssertNear(context, 0.65f, TotemMapService.GetTerrainMoveSpeedMultiplier(TotemTerrainType.Slow), "map.terrain.slowMultiplier");
            context.Assert(TotemMapService.GetTerrainHazardDps(TotemTerrainType.Hazard) > 0f, "Hazard terrain must expose non-zero DPS for the next Combat/Status integration.");
        }

        private static void CheckMapAnchorContracts(GFDiagnosticScenarioContext context)
        {
            var map = TotemMapService.BuildLayout(seed: 77, themeId: 1);
            var sameSeed = TotemMapService.BuildLayout(seed: 77, themeId: 1);
            var differentSeed = TotemMapService.BuildLayout(seed: 78, themeId: 1);

            context.AssertEqual(16, map.AnchorPlacements?.Length ?? 0, "map.anchor.count");
            RequireAnchor(context, map, TotemMapAnchorKind.PlayerSpawn, "player.spawn");
            RequireAnchor(context, map, TotemMapAnchorKind.BossSpawn, "boss.spawn");
            RequireAnchor(context, map, TotemMapAnchorKind.Tattooist, "npc.tattooist.base");
            RequireAnchor(context, map, TotemMapAnchorKind.Merchant, "npc.merchant.base");
            context.AssertEqual(4, TotemMapService.FindAnchors(map, TotemMapAnchorKind.Chest).Length, "map.anchor.chestCount");
            context.AssertEqual(3, TotemMapService.FindAnchors(map, TotemMapAnchorKind.EnemySpawn).Length, "map.anchor.enemySpawnCount");
            context.AssertEqual(3, TotemMapService.FindAnchors(map, TotemMapAnchorKind.Resource).Length, "map.anchor.resourceCount");
            context.AssertEqual(2, TotemMapService.FindAnchors(map, TotemMapAnchorKind.Event).Length, "map.anchor.eventCount");
            AssertAnchorWalkable(context, map);
            context.Assert(AnchorSetsEqual(map, sameSeed), "Map anchors must be deterministic for the same seed and theme.");
            context.Assert(!AnchorSetsEqual(map, differentSeed), "Map anchors should vary when seed changes.");
        }

        private static void CheckActorRoster(GFDiagnosticScenarioContext context)
        {
            var map = TotemMapService.BuildLayout(seed: 1, themeId: 1);
            var roster = TotemActorService.BuildActorRoster(map, new TotemStartupSelection());
            context.AssertEqual(51, roster.Length, "actorRoster.totalIncludingBoss");
            context.AssertEqual(1, roster.Count(actor => actor.Kind == TotemActorKind.Player), "actorRoster.player");
            context.AssertEqual(20, roster.Count(actor => actor.Kind == TotemActorKind.SmartAi), "actorRoster.smartAi");
            context.AssertEqual(29, roster.Count(actor => actor.Kind == TotemActorKind.LightAi), "actorRoster.lightAi");
            context.AssertEqual(1, roster.Count(actor => actor.Kind == TotemActorKind.Boss), "actorRoster.boss");
        }

        private static void CheckMapAnchorConsumers(GFDiagnosticScenarioContext context)
        {
            var map = TotemMapService.BuildLayout(seed: 77, themeId: 1);
            var roster = TotemActorService.BuildActorRoster(map, new TotemStartupSelection());
            var playerAnchor = TotemMapService.FindAnchor(map, TotemMapAnchorKind.PlayerSpawn);
            var bossAnchor = TotemMapService.FindAnchor(map, TotemMapAnchorKind.BossSpawn);
            var player = roster.FirstOrDefault(actor => actor.Kind == TotemActorKind.Player);
            var boss = roster.FirstOrDefault(actor => actor.Kind == TotemActorKind.Boss);
            context.Assert(player != null && playerAnchor != null, "Anchor consumer diagnostic requires player spawn data.");
            context.Assert(boss != null && bossAnchor != null, "Anchor consumer diagnostic requires boss spawn data.");
            AssertNear(context, 0f, FlatDistance(player.Position, playerAnchor.Position), "map.anchor.consumer.player");
            AssertNear(context, 0f, FlatDistance(boss.Position, bossAnchor.Position), "map.anchor.consumer.boss");
            AssertEnemySpawnAnchorConsumers(context, map, roster);

            var chestService = new TotemChestService();
            chestService.SpawnChests(map, createObjects: false);
            var chestAnchors = TotemMapService.FindAnchors(map, TotemMapAnchorKind.Chest);
            context.AssertEqual(chestAnchors.Length, chestService.ActiveChests.Count, "map.anchor.consumer.chestCount");
            for (int i = 0; i < chestAnchors.Length; i++)
            {
                var chest = chestService.ActiveChests[i];
                context.AssertEqual(chestAnchors[i].PayloadId, chest.ChestId, $"map.anchor.consumer.chestPayload.{i}");
                AssertNear(context, 0f, FlatDistance(chest.Position, chestAnchors[i].Position), $"map.anchor.consumer.chestPosition.{i}");
            }

            var npcs = TotemDataService.LoadGameplayCatalogOrDefault().CreateNpcModels(map);
            var merchantAnchor = TotemMapService.FindAnchor(map, TotemMapAnchorKind.Merchant);
            var tattooAnchor = TotemMapService.FindAnchor(map, TotemMapAnchorKind.Tattooist);
            var merchant = npcs.FirstOrDefault(npc => npc.Type == TotemNpcType.Merchant);
            var tattooist = npcs.FirstOrDefault(npc => npc.Type == TotemNpcType.Tattooist);
            context.Assert(merchant != null && merchantAnchor != null, "Anchor consumer diagnostic requires merchant data.");
            context.Assert(tattooist != null && tattooAnchor != null, "Anchor consumer diagnostic requires tattooist data.");
            context.Assert(FlatDistance(merchant.Position, merchantAnchor.Position) <= 3f, "Merchant should be placed from the map merchant anchor plus catalog offset.");
            context.Assert(FlatDistance(tattooist.Position, tattooAnchor.Position) <= 3f, "Tattooist should be placed from the map tattooist anchor plus catalog offset.");
        }

        private static void AssertEnemySpawnAnchorConsumers(GFDiagnosticScenarioContext context, TotemMapSnapshot map, TotemActorSpawnInfo[] roster)
        {
            var enemyAnchors = TotemMapService.FindAnchors(map, TotemMapAnchorKind.EnemySpawn);
            context.AssertEqual(3, enemyAnchors.Length, "map.anchor.consumer.enemySpawnAnchorCount");
            context.AssertEqual(49, roster.Count(actor => actor.Kind == TotemActorKind.SmartAi || actor.Kind == TotemActorKind.LightAi), "map.anchor.consumer.enemySpawnActorCount");

            var counts = CountEnemyActorsByNearestAnchor(roster, enemyAnchors, out float maxDistance);
            context.Detail("map.anchor.consumer.enemySpawnMaxDistance", maxDistance);
            context.Assert(maxDistance <= 1.25f, "Enemy AI spawn positions should stay close to their map enemy spawn anchors.");

            string[] payloads = { "inner", "mid", "outer" };
            int[] expectedCounts = { 14, 17, 18 };
            for (int i = 0; i < payloads.Length; i++)
            {
                int anchorIndex = FindAnchorIndexByPayload(enemyAnchors, payloads[i]);
                context.Assert(anchorIndex >= 0, $"Enemy spawn anchor payload must exist: {payloads[i]}");
                if (anchorIndex < 0)
                {
                    continue;
                }

                context.AssertEqual(expectedCounts[i], counts[anchorIndex], $"map.anchor.consumer.enemySpawnGroup.{payloads[i]}");
            }
        }

        private static void CheckMapResourceAndEventAnchorConsumers(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemMapAnchorConsumerDiagnostic]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterCombatLifecycleServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var mapService = runtime.GetService<TotemMapService>();
                var weapon = runtime.GetService<TotemWeaponService>();
                var choice = runtime.GetService<TotemChoiceService>();
                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });

                var map = mapService.CurrentMap;
                var resourceAnchors = TotemMapService.FindAnchors(map, TotemMapAnchorKind.Resource);
                var eventAnchors = TotemMapService.FindAnchors(map, TotemMapAnchorKind.Event);
                var pickupSnapshot = weapon.CapturePickupSnapshot();
                context.AssertEqual(3, resourceAnchors.Length, "map.anchor.consumer.resourceAnchorCount");
                context.AssertEqual(resourceAnchors.Length, pickupSnapshot.mapResourcePickupCount, "map.anchor.consumer.mapResourcePickupCount");
                context.AssertEqual(resourceAnchors.Length, pickupSnapshot.activePickupCount, "map.anchor.consumer.mapResourceActivePickupCount");
                for (int i = 0; i < resourceAnchors.Length; i++)
                {
                    var anchor = resourceAnchors[i];
                    var pickup = weapon.ActivePickups.FirstOrDefault(item => item != null && item.Source == "MapResource" && item.WeaponId == anchor.PayloadId);
                    context.Assert(pickup != null, $"Map resource anchor should spawn pickup: {anchor.AnchorId}");
                    if (pickup != null)
                    {
                        AssertNear(context, 0f, FlatDistance(anchor.Position, pickup.Position), $"map.anchor.consumer.resourcePosition.{i}");
                    }
                }

                context.AssertEqual(2, eventAnchors.Length, "map.anchor.consumer.eventAnchorCount");
                var anchorChoice = choice.RollAnchorChoice(eventAnchors[0], map.Seed);
                context.Assert(anchorChoice != null, "Event anchor should roll a choice snapshot.");
                context.AssertEqual(eventAnchors[0].PayloadId, anchorChoice?.EventId ?? string.Empty, "map.anchor.consumer.eventChoiceId");
                context.AssertEqual(TotemChoiceRuntimeState.Showing, anchorChoice?.State ?? TotemChoiceRuntimeState.Idle, "map.anchor.consumer.eventChoiceState");
                context.AssertEqual(3, anchorChoice?.Options?.Length ?? 0, "map.anchor.consumer.eventChoiceCount");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void CheckMapRuntimeObjects(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemMapRuntimeDiagnostic]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterMapRuntimeServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var map = runtime.GetService<TotemMapService>();
                var asset = runtime.GetService<TotemAssetService>();
                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });

                var snapshot = map.CaptureRuntimeSnapshot();
                context.Assert(snapshot.hasRoot, "Map runtime should create a root object on CombatHud.");
                context.AssertEqual("[TotemMap]", snapshot.rootName, "map.runtime.rootName");
                context.AssertEqual(10, snapshot.spawnedObjectCount, "map.runtime.spawnedObjectCount");
                context.AssertEqual(9, snapshot.rootChildCount, "map.runtime.rootChildCount");
                context.AssertEqual(1, snapshot.groundObjectCount, "map.runtime.groundObjectCount");
                context.AssertEqual(4, snapshot.wallObjectCount, "map.runtime.wallObjectCount");
                context.AssertEqual(4, snapshot.roomMarkerObjectCount, "map.runtime.roomMarkerObjectCount");
                context.AssertEqual(9, snapshot.materialRequestCount, "map.runtime.materialRequestCount");
                context.AssertEqual(0, snapshot.materialFallbackCount, "map.runtime.materialFallbackCount");
                context.AssertEqual(0, asset.MissingEntryCount, "map.runtime.assetMissingEntryCount");
                context.AssertEqual(0, asset.FallbackRequiredCount, "map.runtime.assetFallbackRequiredCount");
                AssertNear(context, 400f, snapshot.mapSize, "map.runtime.mapSize");
                context.AssertEqual("AI_RUINS", snapshot.themeName, "map.runtime.themeName");
                context.AssertEqual("map.floor.blood", snapshot.lastMaterialAssetKey, "map.runtime.lastMaterialAssetKey");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void CheckMapTerrainMovement(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemTerrainMovementDiagnostic]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterTerrainMovementRuntimeServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var mapService = runtime.GetService<TotemMapService>();
                var actorService = runtime.GetService<TotemActorService>();
                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });

                var map = mapService.CurrentMap;
                var player = actorService.Player;
                context.Assert(map != null, "Terrain movement diagnostic map should exist.");
                context.Assert(player != null, "Terrain movement diagnostic player should exist.");
                SetAllActorsPosition(actorService, FindRoomCenter(map, TotemRoomType.SpawnRoom));

                context.Assert(FindSlowMoveSample(map, out var slowStart, out var slowDelta), "Terrain movement diagnostic should find a slow movement sample.");
                SetActorPosition(player, slowStart);
                actorService.MoveActor(player, slowDelta);
                float slowDistance = FlatDistance(slowStart, player.Position);
                context.Detail("map.terrain.slowMoveDistance", slowDistance);
                context.Detail("map.terrain.slowMoveRequested", slowDelta.magnitude);
                context.Assert(slowDistance > 0f && slowDistance < slowDelta.magnitude * 0.8f, "Slow terrain must reduce actor movement.");

                context.Assert(FindBlockedMoveSample(map, out var blockedStart, out var blockedDelta), "Terrain movement diagnostic should find a blocked movement sample.");
                SetActorPosition(player, blockedStart);
                actorService.MoveActor(player, blockedDelta);
                context.Assert(TotemMapService.QueryTerrain(map, player.Position) != TotemTerrainType.Blocked, "Blocked terrain must not become the actor position.");
                AssertNear(context, 0f, FlatDistance(blockedStart, player.Position), "map.terrain.blockedMoveDistance");

                context.Assert(FindHazardSample(map, out var hazardPosition), "Terrain movement diagnostic should find a hazard sample.");
                SetActorPosition(player, hazardPosition);
                float healthBefore = player.Health;
                actorService.Tick(0.2f);
                context.Detail("map.terrain.hazardHealthBefore", healthBefore);
                context.Detail("map.terrain.hazardHealthAfter", player.Health);
                context.Assert(player.Health < healthBefore, "Hazard terrain must damage actors on terrain tick.");
                context.AssertEqual("TerrainHazard", actorService.LastDamage.Reason, "map.terrain.hazardDamageReason");
                var actorSnapshot = actorService.CaptureActorSnapshot();
                context.AssertEqual(1, actorSnapshot.terrainHazardHitCount, "map.terrain.hazardHitCount");
                AssertNear(context, 0.8f, actorSnapshot.lastTerrainHazardDamageTick, "map.terrain.hazardDamageTick");

                var coverTarget = FindFirstAliveEnemy(actorService);
                context.Assert(coverTarget != null, "Terrain movement diagnostic should find a cover damage target.");
                var sourcePosition = FindRoomCenter(map, TotemRoomType.SpawnRoom);
                SetActorPosition(player, sourcePosition);
                context.Assert(FindCoverSample(map, sourcePosition, out var coverPosition), "Terrain movement diagnostic should find a cover sample far from the source.");
                SetActorPosition(coverTarget, coverPosition);
                float coverHealthBefore = coverTarget.Health;
                actorService.ApplyDamage(coverTarget, 10f, player, "CoverDiagnostic");
                context.Detail("map.terrain.coverHealthBefore", coverHealthBefore);
                context.Detail("map.terrain.coverHealthAfter", coverTarget.Health);
                context.AssertEqual("CoverDiagnostic", actorService.LastDamage.Reason, "map.terrain.coverDamageReason");
                AssertNear(context, 6f, actorService.LastDamage.Amount, "map.terrain.coverAdjustedDamage");
                actorSnapshot = actorService.CaptureActorSnapshot();
                context.AssertEqual(1, actorSnapshot.terrainCoverReducedHitCount, "map.terrain.coverReducedHitCount");
                AssertNear(context, 10f, actorSnapshot.lastTerrainCoverDamageBefore, "map.terrain.coverDamageBefore");
                AssertNear(context, 6f, actorSnapshot.lastTerrainCoverDamageAfter, "map.terrain.coverDamageAfter");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void RegisterMapRuntimeServices(TotemGameRuntime runtime)
        {
            runtime.RegisterService(new TotemGameFlowService());
            runtime.RegisterService(new TotemDataService());
            runtime.RegisterService(new TotemAssetService());
            runtime.RegisterService(new TotemMapService());
        }

        private static void RegisterTerrainMovementRuntimeServices(TotemGameRuntime runtime)
        {
            runtime.RegisterService(new TotemGameFlowService());
            runtime.RegisterService(new TotemDataService());
            runtime.RegisterService(new TotemAssetService());
            runtime.RegisterService(new TotemMapService());
            runtime.RegisterService(new TotemActorService());
        }

        private static void CheckStartupSelectionRuntime(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemStartupSelectionDiagnostic]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterStartupSelectionRuntimeServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var actor = runtime.GetService<TotemActorService>();
                var weapon = runtime.GetService<TotemWeaponService>();
                var tattoo = runtime.GetService<TotemTattooService>();

                flow.SelectCharacter(3);
                flow.ConfirmStartup(2, "hammer_heavy", new[] { 5, 6 });

                var selection = flow.StartupSelection;
                context.AssertEqual(3, selection.CharacterId, "startup.selection.characterId");
                context.AssertEqual(2, selection.ColorId, "startup.selection.colorId");
                context.AssertEqual("hammer_heavy", selection.WeaponId, "startup.selection.weaponId");
                context.AssertEqual(2, selection.PatternIds.Length, "startup.selection.patternCount");
                context.AssertEqual(5, selection.PatternIds[0], "startup.selection.pattern0");
                context.AssertEqual(6, selection.PatternIds[1], "startup.selection.pattern1");

                var player = actor.Player;
                context.Assert(player != null, "Startup selection diagnostic player should spawn.");
                context.AssertEqual(3, player?.ActorId ?? 0, "startup.runtime.playerActorId");
                context.AssertEqual("actor.player.3", TotemActorService.GetPlayerAssetKey(3), "startup.runtime.playerAssetKey.expected");
                context.AssertEqual("actor.player.3", player?.VisualAssetKey ?? string.Empty, "startup.runtime.playerVisualAssetKey");
                context.AssertEqual("hammer_heavy", weapon.GetEquippedWeaponId(player), "startup.runtime.weaponId");

                var tattooSnapshot = tattoo.CaptureSnapshot();
                context.AssertEqual(2, tattooSnapshot.equippedCount, "startup.runtime.tattooCount");
                context.Assert(tattooSnapshot.equippedSummary.Contains("RightArm/Yellow/Bolt"), "Startup selection should equip first selected pattern on RightArm.");
                context.Assert(tattooSnapshot.equippedSummary.Contains("Head/Yellow/Star"), "Startup selection should equip second selected pattern on Head.");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void RegisterStartupSelectionRuntimeServices(TotemGameRuntime runtime)
        {
            runtime.RegisterService(new TotemGameFlowService());
            runtime.RegisterService(new TotemDataService());
            runtime.RegisterService(new TotemAssetService());
            runtime.RegisterService(new TotemMapService());
            runtime.RegisterService(new TotemActorService());
            runtime.RegisterService(new TotemEconomyService());
            runtime.RegisterService(new TotemStatusService());
            runtime.RegisterService(new TotemTattooService());
            runtime.RegisterService(new TotemWeaponService());
        }

        private static void CheckActorAnimationRuntime(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemActorAnimationDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterActorAnimationServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var actor = runtime.GetService<TotemActorService>();
                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });

                var player = actor.Player;
                context.Assert(player != null, "Animation diagnostic player should spawn.");
                var spawned = actor.CaptureAnimationSnapshot(player);
                context.Assert(spawned.hasGameObject, "Animation diagnostic player should have a GameObject.");
                context.Assert(spawned.hasAnimator, "Animation diagnostic player should have an Animator.");
                context.Assert(spawned.animatorHasIsMoving, "Actor Animator should expose IsMoving.");
                context.Assert(spawned.animatorHasDirection, "Actor Animator should expose Direction.");
                context.Assert(spawned.animatorHasAttackTrigger, "Actor Animator should expose AttackTrigger.");
                context.Assert(spawned.animatorHasDie, "Actor Animator should expose Die.");
                context.Assert(spawned.animatorHasDead, "Actor Animator should expose Dead.");
                context.Assert(!spawned.animationMoving && !spawned.animatorIsMoving, "Spawned actor should start idle.");
                context.AssertEqual(0, spawned.animationDirection, "animation.spawn.direction");

                actor.MoveActor(player, new Vector3(1f, 0f, 0f));
                var moving = actor.CaptureAnimationSnapshot(player);
                context.Assert(moving.animationMoving, "Moving actor snapshot should mark animationMoving.");
                context.Assert(moving.animatorIsMoving, "Moving actor Animator should set IsMoving.");
                context.AssertEqual(3, moving.animationDirection, "animation.move.direction");
                context.AssertEqual(3, moving.animatorDirection, "animation.move.animatorDirection");

                actor.Tick(0.016f);
                actor.Tick(0.016f);
                var idle = actor.CaptureAnimationSnapshot(player);
                context.Assert(!idle.animationMoving, "Actor animation should return to idle when no MoveActor call arrives.");
                context.Assert(!idle.animatorIsMoving, "Actor Animator should return IsMoving to false.");

                actor.NotifyActorAttack(player, "DiagnosticAttack");
                var attack = actor.CaptureAnimationSnapshot(player);
                context.AssertEqual(1, attack.attackTriggerCount, "animation.attack.triggerCount");
                context.AssertEqual("DiagnosticAttack", attack.lastReason, "animation.attack.reason");

                var enemy = actor.Actors.FirstOrDefault(TotemActorService.IsEnemy);
                context.Assert(enemy != null, "Animation diagnostic should have an enemy.");
                actor.ApplyDamage(enemy, enemy.Health + 1f, player, "DiagnosticKill");
                var death = actor.CaptureAnimationSnapshot(enemy);
                context.Assert(death.animationDead, "Killed actor should mark animationDead.");
                context.Assert(death.animatorDead, "Killed actor Animator should set Dead.");
                context.AssertEqual(1, death.deathTriggerCount, "animation.death.triggerCount");
                context.Assert(enemy.GameObject == null || enemy.GameObject.activeSelf, "Killed non-player actor should remain visible until death hide delay.");

                actor.Tick(1f);
                context.Assert(enemy.GameObject == null || !enemy.GameObject.activeSelf, "Killed non-player actor should hide after death delay.");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void RegisterActorAnimationServices(TotemGameRuntime runtime)
        {
            runtime.RegisterService(new TotemGameFlowService());
            runtime.RegisterService(new TotemDataService());
            runtime.RegisterService(new TotemAssetService());
            runtime.RegisterService(new TotemMapService());
            runtime.RegisterService(new TotemActorService());
        }

        private static void CheckInputMath(GFDiagnosticScenarioContext context)
        {
            Vector2 diagonal = TotemInputService.NormalizeMove(1f, 1f);
            context.Assert(diagonal.magnitude <= 1.0001f, "Diagonal movement input must be normalized.");
            Vector2 cardinal = TotemInputService.NormalizeMove(1f, 0f);
            context.AssertEqual(1f, cardinal.magnitude, "cardinalMove.magnitude");

            var provider = new FakeInputProvider { UnscaledTime = 10f };
            provider.Hold(KeyCode.W, KeyCode.D);
            provider.Press(KeyCode.E, KeyCode.Q, KeyCode.Space, KeyCode.F, KeyCode.Escape, KeyCode.Tab);
            provider.SetMouse(0, held: true, down: true);

            var service = new TotemInputService();
            service.SetInputProvider(provider);
            var first = service.ReadInputSnapshot();
            context.Assert(first.move.magnitude <= 1.0001f, "Provider diagonal movement must be normalized.");
            context.Assert(!first.hasAimWorldPoint, "Fake provider should not produce aim world points without a valid mouse position.");
            context.Assert(first.attackPressed && first.attackHeld, "Provider should expose attack press/hold.");
            context.Assert(first.skillPressed && first.dodgePressed && first.interactPressed, "Provider should expose gameplay button presses.");
            context.Assert(first.skillSlotEPressed && first.skillSlotQPressed, "Provider should expose both skill slot presses.");
            context.Assert(first.escapePressed && first.selfTattooTogglePressed, "Provider should expose UI button presses.");
            AssertNear(context, 0f, first.attackHoldDuration, "input.attackHold.start");

            provider.ClearPressed();
            provider.UnscaledTime = 11.25f;
            provider.SetMouse(0, held: true, down: false);
            var held = service.ReadInputSnapshot();
            AssertNear(context, 1.25f, held.attackHoldDuration, "input.attackHold.held");

            provider.UnscaledTime = 12f;
            provider.SetMouse(0, held: false, down: false);
            var released = service.ReadInputSnapshot();
            AssertNear(context, 0f, released.attackHoldDuration, "input.attackHold.released");

            context.Assert(!TotemInputService.TryProjectMouseToGround(new Vector3(float.NaN, float.NaN, float.NaN), out _), "Invalid mouse position should not create an aim point.");
        }

        private static void CheckSelfTattooUIInputRouting(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemSelfTattooUIInputDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterSelfTattooUIInputServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var input = runtime.GetService<TotemInputService>();
                var ui = runtime.GetService<TotemUIService>();
                var provider = new FakeInputProvider { UnscaledTime = 20f };
                input.SetInputProvider(provider);

                provider.Press(KeyCode.Tab);
                input.Tick(0.016f);
                ui.Tick(0.016f);
                var mainMenu = ui.CaptureSnapshot();
                context.AssertEqual("MainMenu", mainMenu.lastExclusiveView, "ui.selfTattoo.initialExclusive");
                context.AssertEqual(0, mainMenu.selfTattooToggleRequestCount, "ui.selfTattoo.mainMenuBlocked");
                context.AssertEqual("None", mainMenu.lastOverlayView, "ui.selfTattoo.mainMenuOverlay");

                flow.EnterCombatHud();
                provider.ClearPressed();
                input.Tick(0.016f);
                ui.Tick(0.016f);

                provider.Press(KeyCode.Tab);
                input.Tick(0.016f);
                ui.Tick(0.016f);
                var combat = ui.CaptureSnapshot();
                context.AssertEqual(1, combat.selfTattooToggleRequestCount, "ui.selfTattoo.combatToggleCount");
                context.AssertEqual(1, combat.overlayOpenRequestCount, "ui.selfTattoo.overlayRequestCount");
                context.AssertEqual("SelfTattoo", combat.lastOverlayView, "ui.selfTattoo.overlayView");
                context.AssertEqual(OverlaySortOrderForFirstOverlay(), combat.lastOverlaySortOrder, "ui.selfTattoo.overlaySortOrder");
                context.Assert(combat.lastOverlayAllowEscape, "SelfTattoo overlay should allow escape close.");

                provider.ClearPressed();
                input.Tick(0.016f);
                ui.Tick(0.016f);
                var stable = ui.CaptureSnapshot();
                context.AssertEqual(1, stable.selfTattooToggleRequestCount, "ui.selfTattoo.noRepeatWithoutKeyDown");
                context.AssertEqual(1, stable.overlayOpenRequestCount, "ui.selfTattoo.noRepeatOverlay");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void RegisterSelfTattooUIInputServices(TotemGameRuntime runtime)
        {
            runtime.RegisterService(new TotemGameFlowService());
            runtime.RegisterService(new TotemInputService());
            runtime.RegisterService(new TotemDataService());
            runtime.RegisterService(new TotemUIService());
        }

        private static int OverlaySortOrderForFirstOverlay()
        {
            return 200;
        }

        private static void CheckCombatMath(GFDiagnosticScenarioContext context)
        {
            var actors = new[]
            {
                new TotemActorModel(new TotemActorSpawnInfo { ActorId = 1, Name = "Player", Kind = TotemActorKind.Player, Position = Vector3.zero, MaxHealth = 100f }),
                new TotemActorModel(new TotemActorSpawnInfo { ActorId = 2, Name = "Near", Kind = TotemActorKind.SmartAi, Position = new Vector3(0f, 0f, 5f), MaxHealth = 50f }),
                new TotemActorModel(new TotemActorSpawnInfo { ActorId = 3, Name = "Far", Kind = TotemActorKind.LightAi, Position = new Vector3(0f, 0f, 10f), MaxHealth = 50f }),
                new TotemActorModel(new TotemActorSpawnInfo { ActorId = 4, Name = "Side", Kind = TotemActorKind.LightAi, Position = new Vector3(10f, 0f, 0f), MaxHealth = 50f }),
            };

            var closest = TotemCombatService.FindClosestAliveEnemy(actors, Vector3.zero, maxRange: 30f);
            context.Assert(ReferenceEquals(actors[1], closest), "Closest alive enemy should be selected.");

            var cone = TotemCombatService.FindBestConeTarget(actors, Vector3.zero, Vector3.forward, maxRange: 30f, halfAngleDegrees: 45f);
            context.Assert(ReferenceEquals(actors[1], cone), "Cone targeting should prefer the centered near enemy.");

            var fullLock = TotemCombatService.SelectAimTarget(actors, Vector3.zero, Vector3.forward, maxRange: 1f, halfAngleDegrees: 180f, out string fullLockMode);
            context.AssertEqual("FullLock", fullLockMode, "combat.targeting.fullLock.mode");
            context.Assert(ReferenceEquals(actors[1], fullLock), "Full-lock targeting should ignore range and select the closest alive enemy like the old controller.");

            var strict = TotemCombatService.SelectAimTarget(actors, Vector3.zero, Vector3.forward, maxRange: 30f, halfAngleDegrees: 0f, out string strictMode);
            context.AssertEqual("RaycastGeometry", strictMode, "combat.targeting.strict.mode");
            context.Assert(ReferenceEquals(actors[1], strict), "Strict geometry targeting should select the centered forward enemy.");

            var scoredActors = new[]
            {
                new TotemActorModel(new TotemActorSpawnInfo { ActorId = 1, Name = "Player", Kind = TotemActorKind.Player, Position = Vector3.zero, MaxHealth = 100f }),
                new TotemActorModel(new TotemActorSpawnInfo { ActorId = 2, Name = "NearSide", Kind = TotemActorKind.LightAi, Position = new Vector3(1f, 0f, 1f), MaxHealth = 50f }),
                new TotemActorModel(new TotemActorSpawnInfo { ActorId = 3, Name = "CenteredFar", Kind = TotemActorKind.LightAi, Position = new Vector3(0f, 0f, 5f), MaxHealth = 50f }),
            };
            var scored = TotemCombatService.SelectAimTarget(scoredActors, Vector3.zero, Vector3.forward, maxRange: 30f, halfAngleDegrees: 45f, out string scoredMode);
            context.AssertEqual("Cone", scoredMode, "combat.targeting.cone.mode");
            context.Assert(ReferenceEquals(scoredActors[2], scored), "Cone targeting should use old score formula, not pure nearest distance.");

            var aimForward = TotemCombatService.ResolveAimForward(
                new TotemInputSnapshot { hasAimWorldPoint = true, aimWorldPoint = new Vector3(4f, 0f, 0f) },
                Vector3.zero,
                Vector3.forward);
            AssertNear(context, 1f, aimForward.x, "combat.targeting.aimForward.x");

            actors[1].ApplyDamage(60f);
            context.Assert(!actors[1].IsAlive, "Damage should kill targets at or below zero HP.");
            var nextClosest = TotemCombatService.FindClosestAliveEnemy(actors, Vector3.zero, maxRange: 30f);
            context.Assert(ReferenceEquals(actors[2], nextClosest), "Dead targets must be skipped.");
        }

        private static void CheckCombatStatusControl(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemCombatStatusDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterCombatStatusServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var input = runtime.GetService<TotemInputService>();
                var actor = runtime.GetService<TotemActorService>();
                var status = runtime.GetService<TotemStatusService>();
                var combat = runtime.GetService<TotemCombatService>();
                var skill = runtime.GetService<TotemSkillService>();
                var provider = new FakeInputProvider { UnscaledTime = 20f };
                input.SetInputProvider(provider);
                context.Assert(skill != null, "Combat diagnostic should resolve SkillService.");
                if (skill == null)
                {
                    return;
                }

                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });
                var player = actor.Player;
                context.Assert(player != null, "Combat status diagnostic player should spawn.");
                var combatTarget = actor.Actors.FirstOrDefault(TotemActorService.IsEnemy);
                context.Assert(combatTarget != null, "Combat snapshot diagnostic should have an enemy target.");
                var allCombatEnemies = actor.Actors.Where(TotemActorService.IsEnemy).ToArray();
                for (int i = 0; i < allCombatEnemies.Length; i++)
                {
                    allCombatEnemies[i].Position = player.Position + new Vector3(20f + i, 0f, 20f + i);
                }

                var sideTarget = allCombatEnemies.FirstOrDefault(item => !ReferenceEquals(item, combatTarget));
                context.Assert(sideTarget != null, "Combat snapshot diagnostic should have a side enemy target.");
                combatTarget.Position = player.Position + new Vector3(0f, 0f, 0.8f);
                if (sideTarget != null)
                {
                    sideTarget.Position = player.Position + new Vector3(0.3f, 0f, 0.1f);
                }

                provider.ClearAll();
                provider.SetMouse(0, held: true, down: true);
                input.Tick(0.05f);
                combat.Tick(0.05f);
                var attackSnapshot = combat.CaptureCombatSnapshot();
                context.AssertEqual("Attack", attackSnapshot.lastAction, "combat.snapshot.attack.action");
                context.AssertEqual("Applied", attackSnapshot.lastReason, "combat.snapshot.attack.reason");
                context.AssertEqual(combatTarget.ActorId, attackSnapshot.lastTargetActorId, "combat.snapshot.attack.targetActorId");
                context.AssertEqual("Cone", attackSnapshot.lastTargetingMode, "combat.snapshot.attack.targetingMode");
                AssertNear(context, 120f, attackSnapshot.lastAimSpreadHalfDegrees, "combat.snapshot.attack.aimSpread");
                AssertNear(context, 1f, attackSnapshot.lastAimForward.z, "combat.snapshot.attack.aimForwardZ");
                AssertNear(context, 18f, attackSnapshot.lastDamage, "combat.snapshot.attack.damage");
                context.AssertEqual("knife_basic", attackSnapshot.lastWeaponId, "combat.snapshot.attack.weaponId");
                context.AssertEqual("trait_quickslash", attackSnapshot.lastTraitId, "combat.snapshot.attack.traitId");
                context.AssertEqual(1, attackSnapshot.lastHitCount, "combat.snapshot.attack.hitCount");

                provider.ClearAll();
                provider.Press(KeyCode.E);
                input.Tick(0.05f);
                combat.Tick(0.05f);
                var skillSnapshot = combat.CaptureCombatSnapshot();
                context.AssertEqual("Skill", skillSnapshot.lastAction, "combat.snapshot.skill.action");
                context.AssertEqual("Applied", skillSnapshot.lastReason, "combat.snapshot.skill.reason");
                context.AssertEqual("skill_fireball_01", skillSnapshot.lastSkillId, "combat.snapshot.skill.skillId");
                AssertNear(context, 43.2f, skillSnapshot.lastDamage, "combat.snapshot.skill.damage");
                context.Assert(skillSnapshot.lastHitCount > 0, "Combat skill snapshot should expose hit count.");
                context.Assert(skillSnapshot.lastTargetActorId > 0, "Combat skill snapshot should expose a target actor id when it hits.");

                context.Assert(skill.EquipSkill(player, 1, "skill_chain_lightning_01"), "Combat diagnostic should equip Q skill slot.");
                var qTarget = actor.Actors.FirstOrDefault(item => TotemActorService.IsEnemy(item) && item.IsAlive);
                context.Assert(qTarget != null, "Combat Q skill diagnostic should have an alive enemy target.");
                for (int i = 0; i < allCombatEnemies.Length; i++)
                {
                    allCombatEnemies[i].Position = player.Position + new Vector3(25f + i, 0f, 25f + i);
                }

                qTarget.Position = player.Position + new Vector3(0f, 0f, 1f);
                if (sideTarget != null && !ReferenceEquals(sideTarget, qTarget) && sideTarget.IsAlive)
                {
                    sideTarget.Position = player.Position + new Vector3(0.3f, 0f, 0.1f);
                }

                provider.ClearAll();
                provider.Press(KeyCode.Q);
                input.Tick(0.05f);
                combat.Tick(0.05f);
                var qSkillSnapshot = combat.CaptureCombatSnapshot();
                context.AssertEqual("Skill", qSkillSnapshot.lastAction, "combat.snapshot.skillQ.action");
                context.AssertEqual("Applied", qSkillSnapshot.lastReason, "combat.snapshot.skillQ.reason");
                context.AssertEqual("skill_chain_lightning_01", qSkillSnapshot.lastSkillId, "combat.snapshot.skillQ.skillId");
                context.AssertEqual("Skill:Cone", qSkillSnapshot.lastTargetingMode, "combat.snapshot.skillQ.targetingMode");
                AssertNear(context, 24.3f, qSkillSnapshot.lastDamage, "combat.snapshot.skillQ.damage");
                context.AssertEqual(1, qSkillSnapshot.lastHitCount, "combat.snapshot.skillQ.hitCount");
                context.AssertEqual(qTarget.ActorId, qSkillSnapshot.lastTargetActorId, "combat.snapshot.skillQ.targetActorId");

                provider.Hold(KeyCode.W);
                player.Position = Vector3.zero;
                input.Tick(0.2f);
                combat.Tick(0.2f);
                float baselineMove = player.Position.z;
                context.Assert(baselineMove > 0.9f, "Player should move from InputService before status comparison.");

                player.Position = Vector3.zero;
                status.ApplyStatus(player, TotemStatusService.SlowStatus, 0.5f, 1f);
                input.Tick(0.2f);
                combat.Tick(0.2f);
                context.Assert(player.Position.z > 0.4f && player.Position.z < baselineMove * 0.75f, "Slow status should reduce player movement.");

                status.ClearAllStatuses(player);
                player.Position = Vector3.zero;
                provider.ClearAll();
                provider.Hold(KeyCode.W);
                provider.Press(KeyCode.Space);
                status.ApplyStatus(player, TotemStatusService.StunStatus, 0f, 1f);
                input.Tick(0.2f);
                combat.Tick(0.2f);
                AssertNear(context, 0f, player.Position.z, "combat.status.stun.noMove");
                var dodgeBlocked = combat.CaptureCombatSnapshot();
                context.AssertEqual("DodgeBlockedByStatus", dodgeBlocked.lastAction, "combat.status.stun.dodgeBlocked");
                context.AssertEqual("Status:Stun", dodgeBlocked.lastReason, "combat.status.stun.dodgeReason");

                provider.ClearAll();
                provider.SetMouse(0, held: true, down: true);
                input.Tick(0.2f);
                combat.Tick(0.2f);
                var attackBlocked = combat.CaptureCombatSnapshot();
                context.AssertEqual("AttackBlockedByStatus", attackBlocked.lastAction, "combat.status.stun.attackBlocked");
                context.AssertEqual("Status:Stun", attackBlocked.lastReason, "combat.status.stun.attackReason");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void RegisterCombatStatusServices(TotemGameRuntime runtime)
        {
            runtime.RegisterService(new TotemGameFlowService());
            runtime.RegisterService(new TotemInputService());
            runtime.RegisterService(new TotemDataService());
            runtime.RegisterService(new TotemAssetService());
            runtime.RegisterService(new TotemMapService());
            runtime.RegisterService(new TotemActorService());
            runtime.RegisterService(new TotemEconomyService());
            runtime.RegisterService(new TotemStatusService());
            runtime.RegisterService(new TotemTattooService());
            runtime.RegisterService(new TotemWeaponService());
            runtime.RegisterService(new TotemSkillService());
            runtime.RegisterService(new TotemBossService());
            runtime.RegisterService(new TotemVfxService());
            runtime.RegisterService(new TotemCombatService());
        }

        private static void CheckCameraRuntime(GFDiagnosticScenarioContext context)
        {
            var existingMainCamera = Camera.main;
            var runtimeObject = new GameObject("[TotemCameraDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterCameraRuntimeServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var actor = runtime.GetService<TotemActorService>();
                var camera = runtime.GetService<TotemCameraService>();
                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });

                var player = actor.Player;
                context.Assert(player != null, "Camera diagnostic player should spawn.");

                var initial = camera.CaptureSnapshot();
                context.Assert(initial.hasCamera, "Camera service should create or bind a main camera.");
                context.Assert(initial.following, "Camera service should follow after CombatHud flow.");
                AssertNear(context, 9f, initial.orthographicSize, "camera.orthographicSize");
                AssertNear(context, 55f, initial.tiltX, "camera.tiltX");
                AssertNear(context, 400f, initial.mapSize, "camera.mapSize");

                player.Position = new Vector3(1f, 0f, 2f);
                camera.LateTick(0.1f);
                var edge = camera.CaptureSnapshot();
                AssertNear(context, 1f, edge.rawFocusPosition.x, "camera.clamp.rawMinX");
                AssertNear(context, 2f, edge.rawFocusPosition.z, "camera.clamp.rawMinZ");
                context.Assert(edge.focusClamped, "Camera focus should report clamping at the minimum map edge.");
                context.AssertEqual(1, edge.focusClampCount, "camera.clamp.minCount");
                AssertNear(context, 10f, edge.focusPosition.x, "camera.clamp.minX");
                AssertNear(context, 10f, edge.focusPosition.z, "camera.clamp.minZ");

                player.Position = new Vector3(999f, 0f, 998f);
                camera.LateTick(0.1f);
                var maxEdge = camera.CaptureSnapshot();
                AssertNear(context, 999f, maxEdge.rawFocusPosition.x, "camera.clamp.rawMaxX");
                AssertNear(context, 998f, maxEdge.rawFocusPosition.z, "camera.clamp.rawMaxZ");
                context.Assert(maxEdge.focusClamped, "Camera focus should report clamping at the maximum map edge.");
                context.AssertEqual(2, maxEdge.focusClampCount, "camera.clamp.maxCount");
                AssertNear(context, 390f, maxEdge.focusPosition.x, "camera.clamp.maxX");
                AssertNear(context, 390f, maxEdge.focusPosition.z, "camera.clamp.maxZ");

                player.Position = new Vector3(80f, 0f, 70f);
                camera.LateTick(1f);
                var center = camera.CaptureSnapshot();
                context.Assert(!center.focusClamped, "Camera focus should not clamp when the player is inside the map boundary.");
                AssertNear(context, 80f, center.focusPosition.x, "camera.follow.centerX");
                AssertNear(context, 70f, center.focusPosition.z, "camera.follow.centerZ");
                context.Assert(center.cameraPosition.sqrMagnitude > 0.001f, "Camera snapshot should expose a non-zero camera position.");

                context.Assert(camera.RequestShake(0.2f, 0.5f), "Camera shake request should be accepted.");
                camera.LateTick(0.1f);
                var shake = camera.CaptureSnapshot();
                context.AssertEqual(1, shake.shakeRequestCount, "camera.shake.requestCount");
                context.Assert(shake.shakeRemainingSec > 0f && shake.shakeRemainingSec < 0.5f, "Camera shake remaining time should advance.");
                context.Assert(shake.lastShakeOffset.sqrMagnitude > 0.0001f, "Camera snapshot should expose shake offset.");
                AssertNear(context, 0.2f, shake.lastShakeAmplitude, "camera.shake.amplitude");
                AssertNear(context, 0.5f, shake.lastShakeDuration, "camera.shake.duration");

                camera.LateTick(0.5f);
                var shakeComplete = camera.CaptureSnapshot();
                AssertNear(context, 0f, shakeComplete.shakeRemainingSec, "camera.shake.remainingAfterComplete");
                context.Assert(shakeComplete.lastShakeOffset.sqrMagnitude <= 0.0001f, "Camera shake offset should clear after duration.");
                context.Assert((shakeComplete.cameraPosition - shakeComplete.basePosition).sqrMagnitude <= 0.0001f, "Camera should return to base position after shake completes.");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
                var generatedMainCamera = Camera.main;
                if (existingMainCamera == null && generatedMainCamera != null)
                {
                    UnityEngine.Object.DestroyImmediate(generatedMainCamera.gameObject);
                }
            }
        }

        private static void RegisterCameraRuntimeServices(TotemGameRuntime runtime)
        {
            runtime.RegisterService(new TotemGameFlowService());
            runtime.RegisterService(new TotemDataService());
            runtime.RegisterService(new TotemAssetService());
            runtime.RegisterService(new TotemMapService());
            runtime.RegisterService(new TotemActorService());
            runtime.RegisterService(new TotemCameraService());
        }

        private static void CheckCombatLifecycleCleanup(GFDiagnosticScenarioContext context)
        {
            var existingMainCamera = Camera.main;
            var runtimeObject = new GameObject("[TotemCombatLifecycleDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterCombatLifecycleServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var map = runtime.GetService<TotemMapService>();
                var actor = runtime.GetService<TotemActorService>();
                var economy = runtime.GetService<TotemEconomyService>();
                var status = runtime.GetService<TotemStatusService>();
                var tattoo = runtime.GetService<TotemTattooService>();
                var weapon = runtime.GetService<TotemWeaponService>();
                var chest = runtime.GetService<TotemChestService>();
                var skill = runtime.GetService<TotemSkillService>();
                var zone = runtime.GetService<TotemZoneService>();
                var boss = runtime.GetService<TotemBossService>();
                var ai = runtime.GetService<TotemAIService>();
                var npc = runtime.GetService<TotemNpcService>();
                var camera = runtime.GetService<TotemCameraService>();
                var vfx = runtime.GetService<TotemVfxService>();
                var combat = runtime.GetService<TotemCombatService>();

                flow.SelectCharacter(2);
                flow.ConfirmStartup(1, "knife_basic", new[] { 1, 2 });

                var player = actor.Player;
                var target = actor.Actors.FirstOrDefault(TotemActorService.IsEnemy);
                context.Assert(player != null, "Lifecycle diagnostic should spawn a player.");
                context.Assert(target != null, "Lifecycle diagnostic should spawn enemies.");
                context.Assert(map.CurrentMap != null, "Lifecycle diagnostic should generate a map.");
                context.Assert(map.CaptureRuntimeSnapshot().hasRoot, "Lifecycle diagnostic map root should exist in combat.");
                var actorSnapshot = actor.CaptureActorSnapshot();
                context.AssertEqual(50, actorSnapshot.actorCount, "lifecycle.combat.actorCount");
                context.AssertEqual(1, actorSnapshot.bossCount, "lifecycle.combat.bossCount");
                context.AssertEqual(actorSnapshot.actorCount + actorSnapshot.bossCount, actorSnapshot.visualAssetActorCount, "lifecycle.combat.actorVisualAssetCount");
                context.AssertEqual(0, actorSnapshot.visualFallbackActorCount, "lifecycle.combat.actorVisualFallbackCount");
                context.Assert(chest.CaptureSnapshot().activeChestCount > 0, "Lifecycle diagnostic should spawn chests.");
                context.Assert(npc.CaptureSnapshot().npcCount > 0, "Lifecycle diagnostic should spawn NPCs.");
                context.Assert(zone.CaptureSnapshot().active, "Lifecycle diagnostic zone should be active in combat.");
                context.Assert(boss.CaptureSnapshot().active, "Lifecycle diagnostic boss should be active in combat.");
                context.Assert(ai.CaptureSnapshot().active, "Lifecycle diagnostic AI should be active in combat.");
                context.Assert(camera.CaptureSnapshot().following, "Lifecycle diagnostic camera should follow in combat.");
                context.Assert(combat.CaptureCombatSnapshot().active, "Lifecycle diagnostic combat should be active.");

                status.ApplyStatus(player, TotemStatusService.BurnStatus, 2f, 3f);
                context.AssertEqual(1, status.CaptureSnapshot(player).activeCount, "lifecycle.combat.statusCount");
                context.Assert(!string.IsNullOrEmpty(skill.GetEquippedSkillId(player, 0)), "Lifecycle diagnostic skill loadout should exist in combat.");
                context.Assert(tattoo.CaptureSnapshot().equippedCount > 0, "Lifecycle diagnostic startup tattoos should be equipped.");
                var inventory = economy.CaptureInventory(player);
                context.Assert(inventory.coins > 0 && inventory.inkBottleCount > 0, "Lifecycle diagnostic economy should register player inventory.");
                var initialPickupSnapshot = weapon.CapturePickupSnapshot();
                int mapResourcePickupCount = initialPickupSnapshot.activePickupCount;
                context.AssertEqual(TotemMapService.FindAnchors(map.CurrentMap, TotemMapAnchorKind.Resource).Length, mapResourcePickupCount, "lifecycle.combat.mapResourcePickupCount");
                context.AssertEqual(mapResourcePickupCount, initialPickupSnapshot.visualAssetPickupCount, "lifecycle.combat.mapResourcePickupVisualAssetCount");
                context.AssertEqual(0, initialPickupSnapshot.visualFallbackPickupCount, "lifecycle.combat.mapResourcePickupVisualFallbackCount");
                context.Assert(weapon.SpawnWeaponPickup("hammer_heavy", "LifecycleDiagnostic", player.Position + Vector3.right) != null, "Lifecycle diagnostic should spawn a weapon pickup.");
                var pickupSnapshotAfterSpawn = weapon.CapturePickupSnapshot();
                context.AssertEqual(mapResourcePickupCount + 1, pickupSnapshotAfterSpawn.activePickupCount, "lifecycle.combat.weaponPickupCount");
                context.AssertEqual(mapResourcePickupCount + 1, pickupSnapshotAfterSpawn.visualAssetPickupCount, "lifecycle.combat.weaponPickupVisualAssetCount");
                context.AssertEqual(0, pickupSnapshotAfterSpawn.visualFallbackPickupCount, "lifecycle.combat.weaponPickupVisualFallbackCount");

                if (target != null)
                {
                    actor.ApplyDamage(target, 25f, player, "LifecycleDiagnostic");
                }

                vfx.SpawnAttackHit(player.Position, "knife_basic", charged: false);
                var vfxCombat = vfx.CaptureSnapshot();
                context.Assert(vfxCombat.activeCount > 0, "Lifecycle diagnostic should spawn transient VFX.");
                context.AssertEqual(1, vfxCombat.spriteRequestCount, "lifecycle.combat.vfxSpriteRequestCount");
                context.AssertEqual(0, vfxCombat.spriteMissingCount, "lifecycle.combat.vfxSpriteMissingCount");
                context.AssertEqual("effect.attack.hit", vfxCombat.lastAssetKey, "lifecycle.combat.vfxLastAssetKey");
                context.Assert(vfxCombat.floatingTextActiveCount > 0, "Lifecycle diagnostic should spawn damage floating text.");

                flow.EnterMainMenu();

                context.Assert(flow.CurrentState == TotemGameFlowState.MainMenu, "Lifecycle diagnostic should return to MainMenu.");
                context.Assert(map.CurrentMap == null, "Lifecycle cleanup should clear CurrentMap.");
                context.Assert(!map.CaptureRuntimeSnapshot().hasRoot, "Lifecycle cleanup should destroy map root.");
                context.AssertEqual(0, actor.CaptureActorSnapshot().actorCount, "lifecycle.cleanup.actorCount");
                context.AssertEqual(0, chest.CaptureSnapshot().activeChestCount, "lifecycle.cleanup.chestCount");
                context.AssertEqual(0, npc.CaptureSnapshot().npcCount, "lifecycle.cleanup.npcCount");
                context.AssertEqual(0, weapon.CapturePickupSnapshot().activePickupCount, "lifecycle.cleanup.weaponPickupCount");
                context.AssertEqual(0, economy.PendingDeathChestCount, "lifecycle.cleanup.pendingDeathChestCount");
                context.AssertEqual(0, status.CaptureSnapshot(player).activeCount, "lifecycle.cleanup.statusCount");
                context.Assert(string.IsNullOrEmpty(skill.GetEquippedSkillId(player, 0)), "Lifecycle cleanup should clear skill actor states.");
                context.AssertEqual(0, tattoo.CaptureSnapshot().equippedCount, "lifecycle.cleanup.tattooCount");
                context.Assert(!zone.CaptureSnapshot().active, "Lifecycle cleanup should deactivate zone.");
                context.Assert(!boss.CaptureSnapshot().active, "Lifecycle cleanup should deactivate boss.");
                context.Assert(!ai.CaptureSnapshot().active, "Lifecycle cleanup should deactivate AI.");
                context.Assert(!camera.CaptureSnapshot().following, "Lifecycle cleanup should stop camera follow.");
                var vfxCleanup = vfx.CaptureSnapshot();
                context.AssertEqual(0, vfxCleanup.activeCount, "lifecycle.cleanup.vfxActiveCount");
                context.AssertEqual(0, vfxCleanup.floatingTextActiveCount, "lifecycle.cleanup.floatingTextCount");
                context.Assert(!vfxCleanup.vignetteOverlayActive, "Lifecycle cleanup should destroy vignette overlay.");
                context.Assert(!combat.CaptureCombatSnapshot().active, "Lifecycle cleanup should stop combat service.");

                AssertNoSceneObject(context, "[TotemMap]", "lifecycle.scene.mapRoot");
                AssertNoSceneObject(context, "[TotemActors]", "lifecycle.scene.actorRoot");
                AssertNoSceneObject(context, "[TotemChests]", "lifecycle.scene.chestRoot");
                AssertNoSceneObjectWithPrefix(context, "TotemNpc_", "lifecycle.scene.npcObjects");
                AssertNoSceneObjectWithPrefix(context, "TotemWeaponPickup_", "lifecycle.scene.weaponPickups");
                AssertNoSceneObject(context, "[TotemVFX]", "lifecycle.scene.vfxRoot");
                AssertNoSceneObjectWithPrefix(context, "TotemDamageFloat_", "lifecycle.scene.damageFloats");
                AssertNoSceneObject(context, "[TotemVFXVignette]", "lifecycle.scene.vignette");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
                var generatedMainCamera = Camera.main;
                if (existingMainCamera == null && generatedMainCamera != null)
                {
                    UnityEngine.Object.DestroyImmediate(generatedMainCamera.gameObject);
                }
            }
        }

        private static void RegisterCombatLifecycleServices(TotemGameRuntime runtime)
        {
            runtime.RegisterService(new TotemGameFlowService());
            runtime.RegisterService(new TotemInputService());
            runtime.RegisterService(new TotemDataService());
            runtime.RegisterService(new TotemAssetService());
            runtime.RegisterService(new TotemMapService());
            runtime.RegisterService(new TotemActorService());
            runtime.RegisterService(new TotemEconomyService());
            runtime.RegisterService(new TotemStatusService());
            runtime.RegisterService(new TotemTattooService());
            runtime.RegisterService(new TotemWeaponService());
            runtime.RegisterService(new TotemChestService());
            runtime.RegisterService(new TotemSkillService());
            runtime.RegisterService(new TotemZoneService());
            runtime.RegisterService(new TotemBossService());
            runtime.RegisterService(new TotemAIService());
            runtime.RegisterService(new TotemNpcService());
            runtime.RegisterService(new TotemChoiceService());
            runtime.RegisterService(new TotemInteractionService());
            runtime.RegisterService(new TotemCameraService());
            runtime.RegisterService(new TotemVfxService());
            runtime.RegisterService(new TotemCombatService());
        }

        private static void RequireRoom(GFDiagnosticScenarioContext context, TotemMapSnapshot map, TotemRoomType roomType)
        {
            bool exists = map.Rooms.Any(room => room.RoomType == roomType);
            context.Detail($"room.{roomType}", exists);
            context.Assert(exists, $"Map layout must contain room: {roomType}");
        }

        private static void RequireAnchor(GFDiagnosticScenarioContext context, TotemMapSnapshot map, TotemMapAnchorKind kind, string anchorId)
        {
            var anchor = TotemMapService.FindAnchor(map, kind, anchorId, null);
            context.Detail($"anchor.{anchorId}", anchor != null);
            context.Assert(anchor != null, $"Map layout must contain anchor: {anchorId}");
        }

        private static void AssertAnchorWalkable(GFDiagnosticScenarioContext context, TotemMapSnapshot map)
        {
            var anchors = map?.AnchorPlacements;
            for (int i = 0; anchors != null && i < anchors.Length; i++)
            {
                var anchor = anchors[i];
                context.Assert(anchor != null, $"Map anchor #{i} must not be null.");
                if (anchor == null)
                {
                    continue;
                }

                context.Assert(
                    TotemMapService.IsTerrainWalkable(TotemMapService.QueryTerrain(map, anchor.Position)),
                    $"Map anchor must be walkable: {anchor.AnchorId}");
            }
        }

        private static bool AnchorSetsEqual(TotemMapSnapshot a, TotemMapSnapshot b)
        {
            var anchorsA = a?.AnchorPlacements;
            var anchorsB = b?.AnchorPlacements;
            if (anchorsA == null || anchorsB == null || anchorsA.Length != anchorsB.Length)
            {
                return false;
            }

            for (int i = 0; i < anchorsA.Length; i++)
            {
                var anchorA = anchorsA[i];
                var anchorB = anchorsB[i];
                if (anchorA == null || anchorB == null)
                {
                    return anchorA == anchorB;
                }

                if (anchorA.Kind != anchorB.Kind
                    || anchorA.RoomType != anchorB.RoomType
                    || !string.Equals(anchorA.AnchorId, anchorB.AnchorId, StringComparison.Ordinal)
                    || !string.Equals(anchorA.PayloadId, anchorB.PayloadId, StringComparison.Ordinal)
                    || FlatDistance(anchorA.Position, anchorB.Position) > 0.001f)
                {
                    return false;
                }
            }

            return true;
        }

        private static void AssertTerrainGrid(GFDiagnosticScenarioContext context, TotemMapSnapshot map, string prefix)
        {
            context.Assert(map != null, $"{prefix} map must exist.");
            context.AssertEqual(TotemMapService.TerrainCellSize, map.TerrainCellSize, $"{prefix}.cellSize");
            context.AssertEqual(TotemMapService.TerrainGridResolution, map.TerrainGridWidth, $"{prefix}.width");
            context.AssertEqual(TotemMapService.TerrainGridResolution, map.TerrainGridHeight, $"{prefix}.height");
            context.AssertEqual(TotemMapService.TerrainGridResolution * TotemMapService.TerrainGridResolution, map.TerrainGrid?.Length ?? 0, $"{prefix}.cellCount");
            int total = map.GroundCellCount + map.SlowCellCount + map.BlockedCellCount + map.CoverCellCount + map.HazardCellCount;
            context.AssertEqual(TotemMapService.TerrainGridResolution * TotemMapService.TerrainGridResolution, total, $"{prefix}.countedCells");
            context.Detail($"{prefix}.groundCells", map.GroundCellCount);
            context.Detail($"{prefix}.slowCells", map.SlowCellCount);
            context.Detail($"{prefix}.blockedCells", map.BlockedCellCount);
            context.Detail($"{prefix}.coverCells", map.CoverCellCount);
            context.Detail($"{prefix}.hazardCells", map.HazardCellCount);
            context.Assert(map.GroundCellCount > 0, $"{prefix} must include ground cells.");
            context.Assert(map.SlowCellCount > 0, $"{prefix} must include slow cells.");
            context.Assert(map.BlockedCellCount > 0, $"{prefix} must include blocked cells.");
            context.Assert(map.CoverCellCount > 0, $"{prefix} must include cover cells.");
            context.Assert(map.HazardCellCount > 0, $"{prefix} must include hazard cells.");

            for (int i = 0; map.Rooms != null && i < map.Rooms.Length; i++)
            {
                var room = map.Rooms[i];
                context.Assert(TotemMapService.IsTerrainWalkable(TotemMapService.QueryTerrain(map, room.CenterWorld)), $"{prefix}.{room.RoomType}.centerWalkable");
            }
        }

        private static bool TerrainGridsEqual(TotemMapSnapshot a, TotemMapSnapshot b)
        {
            if (a?.TerrainGrid == null || b?.TerrainGrid == null || a.TerrainGrid.Length != b.TerrainGrid.Length)
            {
                return false;
            }

            for (int i = 0; i < a.TerrainGrid.Length; i++)
            {
                if (a.TerrainGrid[i] != b.TerrainGrid[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool FindSlowMoveSample(TotemMapSnapshot map, out Vector3 start, out Vector3 delta)
        {
            delta = Vector3.right * 8f;
            return FindTerrainCell(map, TotemTerrainType.Slow, delta, out start);
        }

        private static bool FindHazardSample(TotemMapSnapshot map, out Vector3 position)
        {
            return FindTerrainCell(map, TotemTerrainType.Hazard, Vector3.zero, out position);
        }

        private static bool FindCoverSample(TotemMapSnapshot map, Vector3 sourcePosition, out Vector3 position)
        {
            position = default;
            if (map?.TerrainGrid == null)
            {
                return false;
            }

            float minSqr = (TotemActorService.CoverMeleeBypassDistance + 1f) * (TotemActorService.CoverMeleeBypassDistance + 1f);
            for (int z = 1; z < map.TerrainGridHeight - 1; z++)
            {
                for (int x = 1; x < map.TerrainGridWidth - 1; x++)
                {
                    var candidate = CellCenter(map, x, z);
                    if (TotemMapService.QueryTerrain(map, candidate) != TotemTerrainType.Cover)
                    {
                        continue;
                    }

                    if (FlatSqrDistance(candidate, sourcePosition) <= minSqr)
                    {
                        continue;
                    }

                    position = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool FindBlockedMoveSample(TotemMapSnapshot map, out Vector3 start, out Vector3 delta)
        {
            start = default;
            delta = default;
            if (map?.TerrainGrid == null)
            {
                return false;
            }

            for (int z = 1; z < map.TerrainGridHeight - 1; z++)
            {
                for (int x = 1; x < map.TerrainGridWidth - 1; x++)
                {
                    var blocked = CellCenter(map, x, z);
                    if (TotemMapService.QueryTerrain(map, blocked) != TotemTerrainType.Blocked)
                    {
                        continue;
                    }

                    var west = CellCenter(map, x - 1, z);
                    if (TotemMapService.IsTerrainWalkable(TotemMapService.QueryTerrain(map, west)))
                    {
                        start = west;
                        delta = Vector3.right * map.TerrainCellSize;
                        return true;
                    }

                    var south = CellCenter(map, x, z - 1);
                    if (TotemMapService.IsTerrainWalkable(TotemMapService.QueryTerrain(map, south)))
                    {
                        start = south;
                        delta = Vector3.forward * map.TerrainCellSize;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool FindTerrainCell(TotemMapSnapshot map, TotemTerrainType terrainType, Vector3 requiredDelta, out Vector3 position)
        {
            position = default;
            if (map?.TerrainGrid == null)
            {
                return false;
            }

            for (int z = 1; z < map.TerrainGridHeight - 1; z++)
            {
                for (int x = 1; x < map.TerrainGridWidth - 1; x++)
                {
                    var candidate = CellCenter(map, x, z);
                    if (TotemMapService.QueryTerrain(map, candidate) != terrainType)
                    {
                        continue;
                    }

                    if (!TotemMapService.IsTerrainWalkable(TotemMapService.QueryTerrain(map, candidate + requiredDelta)))
                    {
                        continue;
                    }

                    position = candidate;
                    return true;
                }
            }

            return false;
        }

        private static Vector3 CellCenter(TotemMapSnapshot map, int x, int z)
        {
            float cellSize = map.TerrainCellSize <= 0 ? TotemMapService.TerrainCellSize : map.TerrainCellSize;
            return new Vector3((x + 0.5f) * cellSize, 0f, (z + 0.5f) * cellSize);
        }

        private static void SetActorPosition(TotemActorModel actor, Vector3 position)
        {
            actor.Position = position;
            if (actor.GameObject != null)
            {
                actor.GameObject.transform.position = position;
            }
        }

        private static void SetAllActorsPosition(TotemActorService actorService, Vector3 position)
        {
            var actors = actorService?.Actors;
            for (int i = 0; actors != null && i < actors.Count; i++)
            {
                SetActorPosition(actors[i], position);
            }
        }

        private static TotemActorModel FindFirstAliveEnemy(TotemActorService actorService)
        {
            var actors = actorService?.Actors;
            for (int i = 0; actors != null && i < actors.Count; i++)
            {
                var actor = actors[i];
                if (TotemActorService.IsEnemy(actor) && actor.IsAlive)
                {
                    return actor;
                }
            }

            return null;
        }

        private static Vector3 FindRoomCenter(TotemMapSnapshot map, TotemRoomType roomType)
        {
            var rooms = map?.Rooms;
            for (int i = 0; rooms != null && i < rooms.Length; i++)
            {
                if (rooms[i].RoomType == roomType)
                {
                    return rooms[i].CenterWorld;
                }
            }

            return Vector3.zero;
        }

        private static int[] CountEnemyActorsByNearestAnchor(TotemActorSpawnInfo[] roster, TotemMapAnchor[] anchors, out float maxDistance)
        {
            maxDistance = 0f;
            var counts = new int[anchors?.Length ?? 0];
            if (roster == null || anchors == null || anchors.Length <= 0)
            {
                return counts;
            }

            for (int i = 0; i < roster.Length; i++)
            {
                var actor = roster[i];
                if (actor.Kind != TotemActorKind.SmartAi && actor.Kind != TotemActorKind.LightAi)
                {
                    continue;
                }

                int nearestIndex = FindNearestAnchorIndex(anchors, actor.Position);
                if (nearestIndex < 0)
                {
                    continue;
                }

                float distance = FlatDistance(actor.Position, anchors[nearestIndex].Position);
                counts[nearestIndex]++;
                maxDistance = Mathf.Max(maxDistance, distance);
            }

            return counts;
        }

        private static int FindNearestAnchorIndex(TotemMapAnchor[] anchors, Vector3 position)
        {
            int nearestIndex = -1;
            float nearestSqr = float.MaxValue;
            for (int i = 0; anchors != null && i < anchors.Length; i++)
            {
                var anchor = anchors[i];
                if (anchor == null)
                {
                    continue;
                }

                float distance = FlatSqrDistance(position, anchor.Position);
                if (distance < nearestSqr)
                {
                    nearestSqr = distance;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }

        private static int FindAnchorIndexByPayload(TotemMapAnchor[] anchors, string payloadId)
        {
            for (int i = 0; anchors != null && i < anchors.Length; i++)
            {
                var anchor = anchors[i];
                if (anchor != null && string.Equals(anchor.PayloadId, payloadId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            return Mathf.Sqrt(FlatSqrDistance(a, b));
        }

        private static float FlatSqrDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        private static void AssertNoSceneObject(GFDiagnosticScenarioContext context, string objectName, string detailName)
        {
            var go = GameObject.Find(objectName);
            context.Detail(detailName, go == null ? "missing" : go.name);
            context.Assert(go == null, $"Scene object should be cleaned: {objectName}");
        }

        private static void AssertNoSceneObjectWithPrefix(GFDiagnosticScenarioContext context, string prefix, string detailName)
        {
            int count = CountSceneObjectsWithPrefix(prefix);
            context.Detail(detailName, count);
            context.AssertEqual(0, count, detailName);
        }

        private static int CountSceneObjectsWithPrefix(string prefix)
        {
            int count = 0;
            var objects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                var go = objects[i];
                if (go == null || !go.scene.IsValid() || !go.scene.isLoaded || !go.name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private static Type ResolveType(string typeName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .FirstOrDefault(type => type != null);
        }

        private static void AssertNear(GFDiagnosticScenarioContext context, float expected, float actual, string name)
        {
            context.Detail(name + ".expected", expected);
            context.Detail(name + ".actual", actual);
            context.Assert(Mathf.Abs(expected - actual) <= 0.001f, $"{name} expected {expected} actual {actual}");
        }

        private sealed class FakeInputProvider : ITotemInputProvider
        {
            private readonly HashSet<KeyCode> heldKeys = new HashSet<KeyCode>();
            private readonly HashSet<KeyCode> downKeys = new HashSet<KeyCode>();
            private readonly bool[] mouseHeld = new bool[3];
            private readonly bool[] mouseDown = new bool[3];

            public float UnscaledTime { get; set; }

            public Vector3 MousePosition { get; set; } = new Vector3(float.NaN, float.NaN, float.NaN);

            public void Hold(params KeyCode[] keys)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    heldKeys.Add(keys[i]);
                }
            }

            public void Press(params KeyCode[] keys)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    downKeys.Add(keys[i]);
                }
            }

            public void ClearPressed()
            {
                downKeys.Clear();
                for (int i = 0; i < mouseDown.Length; i++)
                {
                    mouseDown[i] = false;
                }
            }

            public void ClearAll()
            {
                heldKeys.Clear();
                downKeys.Clear();
                for (int i = 0; i < mouseHeld.Length; i++)
                {
                    mouseHeld[i] = false;
                    mouseDown[i] = false;
                }
            }

            public void SetMouse(int button, bool held, bool down)
            {
                if (button < 0 || button >= mouseHeld.Length)
                {
                    return;
                }

                mouseHeld[button] = held;
                mouseDown[button] = down;
            }

            public bool GetKey(KeyCode keyCode)
            {
                return heldKeys.Contains(keyCode);
            }

            public bool GetKeyDown(KeyCode keyCode)
            {
                return downKeys.Contains(keyCode);
            }

            public bool GetMouseButton(int button)
            {
                return button >= 0 && button < mouseHeld.Length && mouseHeld[button];
            }

            public bool GetMouseButtonDown(int button)
            {
                return button >= 0 && button < mouseDown.Length && mouseDown[button];
            }
        }
    }
}
#endif
