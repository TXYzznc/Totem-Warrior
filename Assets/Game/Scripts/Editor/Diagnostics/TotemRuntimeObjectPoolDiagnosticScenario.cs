#if UNITY_EDITOR
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemRuntimeObjectPoolDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Runtime Object Pool";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            GameObject runtimeObject = null;
            TotemGameRuntime runtime = null;
            TotemEnemyWorldService world = null;
            TotemEnemyLootService loot = null;
            using (TotemMapService.UsePcgRuntimeProfile(TotemPcgRuntimeProfile.DiagnosticFast))
            {
                try
                {
                    runtimeObject = new GameObject("[TotemRuntimeObjectPoolDiagnostic]");
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
                    TotemEnemyService enemies = runtime.GetService<TotemEnemyService>();
                    world = runtime.GetService<TotemEnemyWorldService>();
                    loot = runtime.GetService<TotemEnemyLootService>();

                    flow.ConfirmStartup(1, "knife_basic", new[] { 1 });
                    clock.Tick(0.1f);
                    world.Tick(0.1f);

                    TotemEnemyModel firstEnemy = enemies.FindClosestAliveEnemy(Vector3.zero);
                    context.Assert(firstEnemy != null && firstEnemy.GameObject != null, "Pool diagnostic requires a spawned enemy visual.");
                    GameObject firstEnemyVisual = firstEnemy.GameObject;
                    int enemyCreatedBeforeReuse = world.VisualCreatedCount;
                    int enemyReusedBeforeReuse = world.VisualReusedCount;
                    int replacementCombatantId = firstEnemy.CombatantId + 500000;
                    var replacementRequest = new TotemEnemySpawnRequest(
                        replacementCombatantId,
                        firstEnemy.EnemyId,
                        firstEnemy.Position,
                        firstEnemy.EncounterInstanceId,
                        "pool_diagnostic",
                        clock.WorldTime);

                    context.Assert(enemies.Despawn(firstEnemy.CombatantId, "PoolDiagnostic"), "Enemy visual must return to the pool on despawn.");
                    context.Assert(!firstEnemyVisual.activeSelf && world.PooledVisualCount > 0, "Despawned enemy visual must be inactive in the pool.");
                    context.Assert(
                        enemies.TrySpawn(replacementRequest, out TotemEnemyModel replacementEnemy, out TotemEnemySpawnBlockReason spawnReason),
                        "Replacement enemy must spawn for pool verification. Reason=" + spawnReason);
                    context.Assert(ReferenceEquals(firstEnemyVisual, replacementEnemy.GameObject), "Matching enemy visual must reuse the pooled GameObject.");
                    context.AssertEqual(enemyCreatedBeforeReuse, world.VisualCreatedCount, "objectPool.enemyCreatedCount");
                    context.AssertEqual(enemyReusedBeforeReuse + 1, world.VisualReusedCount, "objectPool.enemyReusedCount");

                    int generatedCount = loot.HandleEnemyDied(new TotemEnemyDiedEvent(replacementEnemy, null, "PoolDiagnostic", clock.WorldTime));
                    context.Assert(generatedCount > 0 && loot.ActiveVisualCount > 0, "Pool diagnostic requires generated loot visuals.");
                    int lootCreatedBeforeReuse = loot.VisualCreatedCount;
                    int generatedVisualCount = loot.ActiveVisualCount;
                    loot.ResetRun();
                    context.Assert(loot.PooledVisualCount >= generatedVisualCount, "Reset must retain inactive loot visuals for reuse.");

                    int regeneratedCount = loot.HandleEnemyDied(new TotemEnemyDiedEvent(replacementEnemy, null, "PoolDiagnostic", clock.WorldTime));
                    context.AssertEqual(generatedCount, regeneratedCount, "objectPool.regeneratedLootCount");
                    context.AssertEqual(lootCreatedBeforeReuse, loot.VisualCreatedCount, "objectPool.lootCreatedCount");
                    context.Assert(loot.VisualReusedCount >= regeneratedCount, "Matching loot visuals must come from the pool.");

                    runtime.ShutdownRuntime();
                    context.AssertEqual(0, world.ActiveVisualCount, "objectPool.enemyActiveAfterShutdown");
                    context.AssertEqual(0, world.PooledVisualCount, "objectPool.enemyPooledAfterShutdown");
                    context.Assert(!world.HasVisualRoot, "Enemy pool root must be destroyed on shutdown.");
                    context.AssertEqual(0, loot.ActiveVisualCount, "objectPool.lootActiveAfterShutdown");
                    context.AssertEqual(0, loot.PooledVisualCount, "objectPool.lootPooledAfterShutdown");
                    context.Assert(!loot.HasVisualRoot, "Loot pool root must be destroyed on shutdown.");

                    context.Detail("objectPool.enemyCreated", world.VisualCreatedCount);
                    context.Detail("objectPool.enemyReused", world.VisualReusedCount);
                    context.Detail("objectPool.lootCreated", loot.VisualCreatedCount);
                    context.Detail("objectPool.lootReused", loot.VisualReusedCount);
                    context.Pass("Enemy and loot visuals reuse inactive objects and fully dispose their pools on shutdown.");
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
    }
}
#endif
