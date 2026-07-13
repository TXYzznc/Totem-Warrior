#if UNITY_EDITOR
using System.Linq;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemEnemyDomainRuntimeDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Enemy Domain Runtime";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            GameObject runtimeObject = null;
            TotemGameRuntime runtime = null;
            using (TotemMapService.UsePcgRuntimeProfile(TotemPcgRuntimeProfile.DiagnosticFast))
            {
                try
                {
                    runtimeObject = new GameObject("[TotemEnemyDomainDiagnosticRuntime]");
                    runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                    runtime.RegisterService(new TotemGameFlowService());
                    runtime.RegisterService(new TotemMatchClockService());
                    runtime.RegisterService(new TotemInputService());
                    runtime.RegisterService(new TotemDataService());
                    runtime.RegisterService(new TotemAssetService());
                    runtime.RegisterService(new TotemMetaProgressService());
                    runtime.RegisterService(new TotemMapService());
                    runtime.RegisterService(new TotemCombatRelationshipService());
                    runtime.RegisterService(new TotemActorService());
                    runtime.RegisterService(new TotemParticipantReadinessService());
                    runtime.RegisterService(new TotemEconomyService());
                    runtime.RegisterService(new TotemEnemyWorldService());
                    runtime.RegisterService(new TotemEnemyService());
                    runtime.RegisterService(new TotemEnemyLootService());
                    runtime.StartRuntime();

                    TotemGameFlowService flow = runtime.GetService<TotemGameFlowService>();
                    TotemMatchClockService clock = runtime.GetService<TotemMatchClockService>();
                    TotemActorService actor = runtime.GetService<TotemActorService>();
                    TotemEnemyWorldService world = runtime.GetService<TotemEnemyWorldService>();
                    TotemEnemyService enemies = runtime.GetService<TotemEnemyService>();
                    TotemEnemyLootService loot = runtime.GetService<TotemEnemyLootService>();

                    flow.ConfirmStartup(1, "knife_basic", new[] { 1 });
                    context.AssertEqual(50, actor.Actors.Count, "enemyDomain.participantCount");
                    context.Assert(actor.Actors.All(item => item.Domain == TotemCombatantDomain.Participant), "Actor roster must contain Participants only.");

                    clock.Tick(0.1f);
                    world.Tick(0.1f);
                    TotemEnemyRuntimeSnapshot initial = enemies.CaptureSnapshot();
                    TotemEnemyWorldSnapshot initialWorld = world.CaptureSnapshot();
                    context.Assert(initialWorld.hasPlan && initialWorld.planEntryCount > 0, "Encounter must build a deterministic SpawnPlan.");
                    context.Assert(initial.lightCount > 0, "Initial encounter must spawn Light enemies.");
                    context.AssertEqual(0, initial.eliteCount, "enemyDomain.initialEliteCount");
                    context.AssertEqual(0, initial.bossCount, "enemyDomain.initialBossCount");

                    CheckNonLethalAbilityBridge(context, actor, enemies, clock);

                    TotemEnemyModel firstEnemy = enemies.FindClosestAliveEnemy(actor.Actors[1].Position);
                    context.Assert(firstEnemy != null, "Enemy runtime must expose a spawned target.");
                    TotemActorModel bot = actor.Actors.First(item => item.ControllerKind != TotemParticipantControllerKind.Human);
                    int lootBefore = loot.CaptureSnapshot().totalSpawnedPickupCount;
                    bool damageAccepted = enemies.TryApplyDamage(
                        firstEnemy.CombatantId,
                        bot,
                        firstEnemy.Health + 1f,
                        "DiagnosticEnemyKill",
                        clock.WorldTime,
                        out float appliedDamage);
                    context.Assert(damageAccepted && appliedDamage > 0f, "Participant damage must enter EnemyService.");
                    TotemEnemyLootSnapshot dropped = loot.CaptureSnapshot();
                    context.AssertEqual(lootBefore + 1, dropped.processedEnemyDeathCount, "enemyDomain.processedDeathCount");
                    context.Assert(dropped.totalSpawnedPickupCount > lootBefore, "Enemy death must create public world loot.");

                    TotemLootPickupModel pickup = loot.ActivePickups.FirstOrDefault();
                    context.Assert(pickup != null, "Enemy loot must remain available until a Participant claims it.");
                    bot.Position = pickup.Position;
                    loot.Tick(0.1f);
                    context.Assert(loot.CaptureSnapshot().totalClaimedPickupCount > 0, "SmartBot/LightBot must have equal public pickup permission.");

                    clock.Tick(240f);
                    world.Tick(0.1f);
                    TotemEnemyRuntimeSnapshot eliteWindow = enemies.CaptureSnapshot();
                    context.Assert(eliteWindow.eliteCount > 0, "Elite encounter must open at 240 seconds.");
                    context.AssertEqual(0, eliteWindow.bossCount, "enemyDomain.preBossCount");

                    clock.Tick(360f);
                    world.Tick(0.1f);
                    TotemEnemyRuntimeSnapshot bossWindow = enemies.CaptureSnapshot();
                    context.AssertEqual(1, bossWindow.bossCount, "enemyDomain.bossCountAt600");
                    TotemEnemyModel boss = enemies.FindClosestAliveEnemy(Vector3.zero, 0f, TotemEnemyTier.Boss);
                    context.Assert(boss != null, "The map theme must spawn one configured Boss.");

                    enemies.TryApplyDamage(
                        boss.CombatantId,
                        bot,
                        boss.MaxHealth * 0.45f,
                        "DiagnosticBossPhase",
                        clock.WorldTime,
                        out _);
                    enemies.Tick(0.1f);
                    TotemEnemyControllerBase bossController = enemies.FindController(boss.CombatantId);
                    context.Assert(bossController != null && bossController.BossPhase >= 2, "Boss health threshold must advance the native Boss controller phase.");

                    context.Detail("enemyDomain.planEntries", initialWorld.planEntryCount);
                    context.Detail("enemyDomain.initialLight", initial.lightCount);
                    context.Detail("enemyDomain.elitesAt240", eliteWindow.eliteCount);
                    context.Detail("enemyDomain.bossesAt600", bossWindow.bossCount);
                    context.Detail("enemyDomain.lootSpawned", dropped.totalSpawnedPickupCount);
                    context.Pass("NPC Enemy runtime, encounter clock, Boss phases and public loot complete one native GF_X loop.");
                }
                finally
                {
                    runtime?.ShutdownRuntime();
                    if (runtimeObject != null)
                    {
                        Object.DestroyImmediate(runtimeObject);
                    }
                }
            }
        }

        private static void CheckNonLethalAbilityBridge(
            GFDiagnosticScenarioContext context,
            TotemActorService actor,
            TotemEnemyService enemies,
            TotemMatchClockService clock)
        {
            TotemActorModel target = actor.Actors.FirstOrDefault(item =>
                item != null
                && item.IsAlive
                && item.ControllerKind == TotemParticipantControllerKind.LightBot);
            context.Assert(target != null, "Ability bridge diagnostic requires an active LightBot target.");
            if (target == null)
            {
                return;
            }

            var projectile = CreateAbilityProbeDefinition(
                "diagnostic_enemy_projectile",
                TotemEnemyAbilityType.Projectile,
                radius: 0f);
            var hazard = CreateAbilityProbeDefinition(
                "diagnostic_enemy_hazard",
                TotemEnemyAbilityType.HazardZone,
                radius: 4f);
            enemies.RegisterDefinition(projectile);
            enemies.RegisterDefinition(hazard);

            AssertAbilityDealsOnce(context, enemies, target, projectile, 910101, clock.WorldTime);
            AssertAbilityDealsOnce(context, enemies, target, hazard, 910102, clock.WorldTime);
        }

        private static void AssertAbilityDealsOnce(
            GFDiagnosticScenarioContext context,
            TotemEnemyService enemies,
            TotemActorModel target,
            TotemEnemyRuntimeDefinition definition,
            int combatantId,
            float worldTime)
        {
            context.Assert(
                enemies.TrySpawn(
                    new TotemEnemySpawnRequest(combatantId, definition.enemyId, target.Position, 42, "diagnostic.abilityBridge", worldTime),
                    out var enemy,
                    out var reason),
                $"Ability bridge probe should spawn {definition.enemyId}: {reason}");
            if (enemy == null)
            {
                return;
            }

            float healthBefore = target.Health;
            for (int i = 0; i < 24 && Mathf.Approximately(target.Health, healthBefore); i++)
            {
                enemies.Tick(0.1f);
            }

            float damage = healthBefore - target.Health;
            context.Assert(damage > 0f, definition.enemyId + " should resolve non-lethal damage through ActorService.");
            AssertNear(context, 5f, damage, definition.enemyId + ".damageOnce");
            context.Assert(enemies.CaptureSnapshot().totalAbilityStarts > 0, definition.enemyId + " should start an Enemy ability.");
            enemies.Despawn(combatantId, "DiagnosticAbilityBridgeCleanup");
        }

        private static TotemEnemyRuntimeDefinition CreateAbilityProbeDefinition(
            string enemyId,
            TotemEnemyAbilityType abilityType,
            float radius)
        {
            return new TotemEnemyRuntimeDefinition
            {
                enemyId = enemyId,
                displayName = enemyId,
                themeId = "diagnostic",
                tier = TotemEnemyTier.Light,
                maxHealth = 100f,
                baseDamage = 5f,
                behavior = new TotemEnemyBehaviorDefinition
                {
                    behaviorProfileId = enemyId + ".behavior",
                    detectRange = 12f,
                    attackRange = 8f,
                    leashRange = 24f,
                    moveSpeed = 0f,
                    hotRadius = 20f,
                    warmRadius = 60f,
                    lightHotHz = 20f,
                    lightWarmHz = 20f,
                    lightColdHz = 20f,
                },
                abilities = new[]
                {
                    new TotemEnemyAbilityRuntimeDefinition
                    {
                        abilityId = enemyId + ".ability",
                        abilityType = abilityType,
                        range = 8f,
                        radius = radius,
                        cooldown = 10f,
                        windup = 0f,
                        active = 0.01f,
                        recovery = 0.01f,
                        damageMultiplier = 1f,
                        score = 10f,
                    },
                },
            };
        }

        private static void AssertNear(GFDiagnosticScenarioContext context, float expected, float actual, string key)
        {
            context.Detail(key + ".actual", actual);
            context.Assert(Mathf.Abs(expected - actual) <= 0.001f, key + ": expected=" + expected + ", actual=" + actual);
        }
    }
}
#endif
