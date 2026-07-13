#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class TotemEnemyReadinessPcgSmokeTests
{
    private const string LaunchSceneName = "Launch";
    private const int SceneLoadMaxFrames = 600;
    private const int RuntimeReadyMaxFrames = 600;
    private const int ViewReadyMaxFrames = 240;

    [UnityTest]
    [Timeout(120000)]
    public IEnumerator EnemyReadiness_DiagnosticFastPcgSmoke_CoversStartupAndImmediateParticipantCombat()
    {
        using (TotemMapService.UsePcgRuntimeProfile(TotemPcgRuntimeProfile.DiagnosticFast))
        {
            yield return LoadFreshLaunchScene();
            TotemGameRuntime runtime = TotemGameRuntime.Instance;
            yield return EnterCombatHud(runtime);

            var map = RequireService<TotemMapService>(runtime);
            var clock = RequireService<TotemMatchClockService>(runtime);
            var actors = RequireService<TotemActorService>(runtime);
            var readiness = RequireService<TotemParticipantReadinessService>(runtime);
            var enemyWorld = RequireService<TotemEnemyWorldService>(runtime);
            var enemies = RequireService<TotemEnemyService>(runtime);
            var loot = RequireService<TotemEnemyLootService>(runtime);
            var flow = RequireService<TotemGameFlowService>(runtime);

            AssertPcgProfile(map, TotemPcgRuntimeProfile.DiagnosticFast, TotemMapService.DiagnosticPcgMapWidth);

            TotemActorModel player = actors.Player;
            TotemActorModel bot = FindFirstBot(actors);
            Assert.NotNull(player, "CombatHud should spawn the local human participant.");
            Assert.NotNull(bot, "CombatHud should spawn at least one authority-ready bot participant.");
            Assert.NotNull(player.GameObject, "CombatHud should create the local player's runtime object.");
            Assert.AreEqual(TotemParticipantLifecycle.Loading, readiness.GetLifecycle(player));
            Assert.IsFalse(player.GameObject.activeSelf, "Loading local player must remain invisible.");

            float loadingHealth = player.Health;
            Assert.IsFalse(actors.ApplyDamage(player, 3f, bot, "Smoke.LoadingBlocked"));
            Assert.AreEqual(loadingHealth, player.Health, "Loading local player must not take damage.");

            yield return WaitForParticipantLifecycle(readiness, player, TotemParticipantLifecycle.Protected);
            Assert.AreEqual(TotemParticipantLifecycle.Protected, readiness.GetLifecycle(player));
            Assert.IsTrue(player.GameObject.activeSelf, "HUD/camera/input readiness should make the local player visible.");
            Assert.IsFalse(actors.ApplyDamage(player, 3f, bot, "Smoke.ProtectedBlocked"));
            Assert.AreEqual(loadingHealth, player.Health, "Protected local player must not take damage.");

            float originalProtectionSeconds = readiness.ProtectionSeconds;
            readiness.ProtectionSeconds = 0f;
            readiness.Tick(0f);
            readiness.ProtectionSeconds = originalProtectionSeconds;

            Assert.AreEqual(TotemParticipantLifecycle.Active, readiness.GetLifecycle(player));
            Assert.Less(clock.WorldTime, 60f, "Participant combat must not wait for a global 60-second protection window.");

            float playerHealth = player.Health;
            float botHealth = bot.Health;
            Assert.IsFalse(actors.ApplyDamage(player, 1f, bot, "Smoke.BotToPlayer.BeforeGrace"));
            Assert.IsFalse(actors.ApplyDamage(bot, 1f, player, "Smoke.PlayerToBot.BeforeGrace"));
            Assert.AreEqual(playerHealth, player.Health, "Participant damage must be blocked during the first 60 seconds.");
            Assert.AreEqual(botHealth, bot.Health, "Participant damage must be blocked during the first 60 seconds.");

            clock.SetWorldTimeForDiagnostics(1f);
            enemyWorld.Tick(0.1f);
            TotemEnemyRuntimeSnapshot enemySnapshot = enemies.CaptureSnapshot();
            Assert.Greater(enemySnapshot.lightCount, 0, "EnemyWorld should spawn Light enemies from the authority clock plan.");

            TotemEnemyModel lightEnemy = FindAliveEnemy(enemies, TotemEnemyTier.Light);
            Assert.NotNull(lightEnemy, "A Light enemy is required to verify NPC damage during the participant grace period.");
            float npcDamageHealth = player.Health;
            Assert.IsFalse(actors.ApplyDamage(player, 1f, lightEnemy, "Smoke.NpcToPlayer.DuringGrace"));
            Assert.Less(player.Health, npcDamageHealth, "NPC damage must remain active during the participant grace period.");

            clock.SetWorldTimeForDiagnostics(TotemCombatRelationshipService.ParticipantCombatGraceSeconds);
            playerHealth = player.Health;
            botHealth = bot.Health;
            Assert.IsFalse(actors.ApplyDamage(player, 1f, bot, "Smoke.BotToPlayer.AfterGrace"));
            Assert.IsFalse(actors.ApplyDamage(bot, 1f, player, "Smoke.PlayerToBot.AfterGrace"));
            Assert.Less(player.Health, playerHealth, "Participant damage should be enabled after the first 60 seconds.");
            Assert.Less(bot.Health, botHealth, "Participant damage should be enabled after the first 60 seconds.");

            yield return ExitCombatHudAndAssertClean(flow, actors, readiness, map, enemyWorld, enemies, loot);
        }
    }

    [UnityTest]
    [Timeout(240000)]
    public IEnumerator EnemyReadiness_FullPcgSmoke_CoversEliteBossLootAndExitCleanup()
    {
        using (TotemMapService.UsePcgRuntimeProfile(TotemPcgRuntimeProfile.Full))
        {
            yield return LoadFreshLaunchScene();
            TotemGameRuntime runtime = TotemGameRuntime.Instance;
            yield return EnterCombatHud(runtime);

            var map = RequireService<TotemMapService>(runtime);
            var clock = RequireService<TotemMatchClockService>(runtime);
            var actors = RequireService<TotemActorService>(runtime);
            var readiness = RequireService<TotemParticipantReadinessService>(runtime);
            var enemyWorld = RequireService<TotemEnemyWorldService>(runtime);
            var enemies = RequireService<TotemEnemyService>(runtime);
            var loot = RequireService<TotemEnemyLootService>(runtime);
            var combat = RequireService<TotemCombatService>(runtime);
            var flow = RequireService<TotemGameFlowService>(runtime);

            AssertPcgProfile(map, TotemPcgRuntimeProfile.Full, TotemMapService.PcgMapWidth);
            TotemActorModel player = actors.Player;
            yield return WaitForParticipantLifecycle(readiness, player, TotemParticipantLifecycle.Protected);
            ReleaseLocalPlayerProtection(readiness, player);

            var bossPhaseEventCounts = new int[4];
            System.Action<TotemBossPhaseChangedEvent> onBossPhaseChanged = evt =>
            {
                if (evt.Enemy != null
                    && evt.Enemy.Tier == TotemEnemyTier.Boss
                    && evt.CurrentPhase > 0
                    && evt.CurrentPhase < bossPhaseEventCounts.Length)
                {
                    bossPhaseEventCounts[evt.CurrentPhase]++;
                }
            };
            enemies.BossPhaseChanged += onBossPhaseChanged;

            clock.SetWorldTimeForDiagnostics(601f);
            enemyWorld.Tick(0.1f);

            TotemEnemyRuntimeSnapshot enemySnapshot = enemies.CaptureSnapshot();
            Assert.Greater(enemySnapshot.lightCount, 0, "Full PCG should cover the Light encounter schedule.");
            Assert.Greater(enemySnapshot.eliteCount, 0, "Advancing authority time should cover the Elite encounter schedule.");
            Assert.Greater(enemySnapshot.bossCount, 0, "Advancing authority time should cover the Boss encounter schedule.");

            TotemEnemyModel boss = FindAliveEnemy(enemies, TotemEnemyTier.Boss);
            Assert.NotNull(boss, "A Boss enemy is required to verify monotonic native phase progression.");
            TotemEnemyControllerBase bossController = enemies.FindController(boss.CombatantId);
            Assert.NotNull(bossController, "The spawned Boss should have a native controller.");

            enemies.Tick(0.1f);
            Assert.AreEqual(1, bossController.BossPhase, "A full-health Boss should enter phase 1.");
            AssertBossPhaseEventCounts(bossPhaseEventCounts, 1, 0, 0);

            DamageEnemyToHealthRatio(enemies, boss, player, 0.55f, clock.WorldTime, "Smoke.BossPhase2");
            enemies.Tick(0.1f);
            Assert.AreEqual(2, bossController.BossPhase, "Boss should enter phase 2 at or below 60% health.");
            AssertBossPhaseEventCounts(bossPhaseEventCounts, 1, 1, 0);

            Assert.IsTrue(enemies.TryHeal(boss.CombatantId, boss.MaxHealth * 0.15f, out float healedBossHealth));
            Assert.Greater(healedBossHealth, 0f, "Boss healing should be applied before checking phase monotonicity.");
            enemies.Tick(0.1f);
            Assert.AreEqual(2, bossController.BossPhase, "Healing above the phase-2 threshold must not downgrade the Boss phase.");
            AssertBossPhaseEventCounts(bossPhaseEventCounts, 1, 1, 0);

            DamageEnemyToHealthRatio(enemies, boss, player, 0.55f, clock.WorldTime, "Smoke.BossPhase2Repeat");
            enemies.Tick(0.1f);
            Assert.AreEqual(2, bossController.BossPhase, "Crossing the phase-2 threshold again must not re-enter phase 2.");
            AssertBossPhaseEventCounts(bossPhaseEventCounts, 1, 1, 0);

            DamageEnemyToHealthRatio(enemies, boss, player, 0.25f, clock.WorldTime, "Smoke.BossPhase3");
            enemies.Tick(0.1f);
            Assert.AreEqual(3, bossController.BossPhase, "Boss should enter phase 3 at or below 30% health.");
            AssertBossPhaseEventCounts(bossPhaseEventCounts, 1, 1, 1);
            enemies.BossPhaseChanged -= onBossPhaseChanged;

            TotemEnemyModel lightEnemy = FindAliveEnemy(enemies, TotemEnemyTier.Light);
            Assert.NotNull(lightEnemy, "A Light enemy is required to verify the native death-to-loot chain.");
            Assert.IsTrue(enemies.TryApplyDamage(
                lightEnemy.CombatantId,
                player,
                lightEnemy.MaxHealth + 1000f,
                "Smoke.LootKill",
                clock.WorldTime,
                out float appliedDamage));
            Assert.Greater(appliedDamage, 0f, "The active player should damage the spawned enemy.");

            TotemEnemyLootSnapshot lootSnapshot = loot.CaptureSnapshot();
            Assert.Greater(lootSnapshot.processedEnemyDeathCount, 0, "Enemy death should reach TotemEnemyLootService.");
            Assert.Greater(lootSnapshot.totalSpawnedPickupCount, 0, "Enemy death should produce runtime loot.");

            int killedBotCount = KillAllBots(actors, player);
            Assert.AreEqual(TotemActorService.ParticipantCount - 1, killedBotCount, "The smoke should eliminate all 49 bot participants.");
            yield return null;

            TotemRunResultSnapshot runResult = combat.LastRunResult;
            Assert.NotNull(runResult, "Eliminating the other 49 participants should finish the run.");
            Assert.AreEqual(player.ActorId, runResult.winnerParticipantId, "The sole surviving human must be resolved as the winner.");
            Assert.IsTrue(runResult.win, "The local human should win as the last surviving participant.");
            Assert.AreEqual(1, runResult.aliveParticipantCount, "Exactly one participant should remain alive.");
            Assert.Greater(runResult.aliveEnemyCount, 0, "Alive NPC enemies must not prevent last-participant victory.");

            yield return ExitCombatHudAndAssertClean(flow, actors, readiness, map, enemyWorld, enemies, loot);
        }
    }

    [UnityTearDown]
    public IEnumerator CleanupAfterTest()
    {
        yield return CleanupCurrentRuntime();
    }

    private static IEnumerator LoadFreshLaunchScene()
    {
        yield return CleanupCurrentRuntime();

        AsyncOperation operation = SceneManager.LoadSceneAsync(LaunchSceneName, LoadSceneMode.Single);
        Assert.NotNull(operation, "Launch scene should be available from build settings.");
        for (int frame = 0; frame < SceneLoadMaxFrames && !operation.isDone; frame++)
        {
            yield return null;
        }

        Assert.IsTrue(operation.isDone, $"Timed out after {SceneLoadMaxFrames} frames loading {LaunchSceneName}.");
        for (int frame = 0; frame < RuntimeReadyMaxFrames; frame++)
        {
            TotemGameRuntime runtime = TotemGameRuntime.Instance;
            if (runtime != null && runtime.ServicesReady)
            {
                yield break;
            }

            yield return null;
        }

        Assert.Fail($"Timed out after {RuntimeReadyMaxFrames} frames waiting for TotemGameRuntime.ServicesReady.");
    }

    private static IEnumerator EnterCombatHud(TotemGameRuntime runtime)
    {
        Assert.NotNull(runtime, "GF_X Totem runtime should be created by Launch scene.");
        var ui = RequireService<TotemUIService>(runtime);
        var flow = RequireService<TotemGameFlowService>(runtime);

        yield return WaitForExclusiveView(ui, flow, UIViews.MainMenu, TotemGameFlowState.MainMenu);
        ui.OpenCharacterSelect();
        yield return WaitForExclusiveView(ui, flow, UIViews.CharacterSelect, TotemGameFlowState.CharacterSelect);
        flow.SelectCharacter(1);
        ui.OpenStartupSelect();
        yield return WaitForExclusiveView(ui, flow, UIViews.StartupSelect, TotemGameFlowState.StartupSelect);
        flow.ConfirmStartup(1, "knife_basic", new[] { 1 });
        ui.OpenCombatHud();
        yield return WaitForExclusiveView(ui, flow, UIViews.CombatHUD, TotemGameFlowState.CombatHud);
    }

    private static IEnumerator WaitForExclusiveView(
        TotemUIService ui,
        TotemGameFlowService flow,
        UIViews view,
        TotemGameFlowState state)
    {
        for (int frame = 0; frame < ViewReadyMaxFrames; frame++)
        {
            TotemUISnapshot snapshot = ui.CaptureSnapshot();
            if (flow.CurrentState == state
                && snapshot.lastExclusiveView == view.ToString()
                && snapshot.currentFormId > 0)
            {
                yield break;
            }

            yield return null;
        }

        Assert.Fail($"Timed out after {ViewReadyMaxFrames} frames waiting for {view} / {state}.");
    }

    private static void AssertPcgProfile(TotemMapService map, TotemPcgRuntimeProfile profile, int expectedSize)
    {
        Assert.AreEqual(profile, TotemMapService.CurrentPcgRuntimeProfile);
        Assert.NotNull(map.CurrentMap, "CombatHud should generate a map.");
        Assert.IsTrue(map.CurrentMap.IsPcgGenerated, "Smoke must exercise the native PCG map path.");
        Assert.AreEqual(expectedSize, map.CurrentMap.PcgWidth);
        Assert.AreEqual(expectedSize, map.CurrentMap.PcgHeight);
    }

    private static IEnumerator WaitForParticipantLifecycle(
        TotemParticipantReadinessService readiness,
        TotemActorModel player,
        TotemParticipantLifecycle expectedLifecycle)
    {
        Assert.NotNull(player, "CombatHud should spawn the local player.");
        for (int frame = 0; frame < ViewReadyMaxFrames; frame++)
        {
            if (readiness.GetLifecycle(player) == expectedLifecycle)
            {
                yield break;
            }

            yield return null;
        }

        Assert.Fail($"Timed out after {ViewReadyMaxFrames} frames waiting for participant lifecycle {expectedLifecycle}.");
    }

    private static void ReleaseLocalPlayerProtection(TotemParticipantReadinessService readiness, TotemActorModel player)
    {
        float originalProtectionSeconds = readiness.ProtectionSeconds;
        readiness.ProtectionSeconds = 0f;
        readiness.Tick(0f);
        readiness.ProtectionSeconds = originalProtectionSeconds;
        Assert.AreEqual(TotemParticipantLifecycle.Active, readiness.GetLifecycle(player));
    }

    private static void DamageEnemyToHealthRatio(
        TotemEnemyService enemies,
        TotemEnemyModel enemy,
        TotemActorModel source,
        float targetHealthRatio,
        float worldTime,
        string reason)
    {
        float targetHealth = enemy.MaxHealth * Mathf.Clamp01(targetHealthRatio);
        float requestedDamage = enemy.Health - targetHealth;
        Assert.Greater(requestedDamage, 0f, reason + " should lower enemy health.");
        Assert.IsTrue(enemies.TryApplyDamage(
            enemy.CombatantId,
            source,
            requestedDamage,
            reason,
            worldTime,
            out float appliedDamage,
            canInterrupt: false));
        Assert.Greater(appliedDamage, 0f, reason + " should apply damage through TotemEnemyService.");
        Assert.AreEqual(targetHealth, enemy.Health, 0.01f, reason + " should reach the requested health ratio.");
    }

    private static void AssertBossPhaseEventCounts(int[] eventCounts, int phase1, int phase2, int phase3)
    {
        Assert.AreEqual(phase1, eventCounts[1], "Boss phase 1 should be emitted exactly once.");
        Assert.AreEqual(phase2, eventCounts[2], "Boss phase 2 should be emitted exactly once.");
        Assert.AreEqual(phase3, eventCounts[3], "Boss phase 3 should be emitted exactly once.");
    }

    private static int KillAllBots(TotemActorService actors, TotemActorModel player)
    {
        int killedCount = 0;
        for (int i = 0; i < actors.Actors.Count; i++)
        {
            TotemActorModel actor = actors.Actors[i];
            if (actor == null || actor == player || !actor.IsAlive)
            {
                continue;
            }

            Assert.IsTrue(
                actors.ApplyDamage(actor, actor.MaxHealth + 1000f, player, "Smoke.LastParticipantStanding"),
                "Each bot should be eliminated through TotemActorService.");
            killedCount++;
        }

        return killedCount;
    }

    private static TotemActorModel FindFirstBot(TotemActorService actors)
    {
        for (int i = 0; i < actors.Actors.Count; i++)
        {
            TotemActorModel actor = actors.Actors[i];
            if (actor != null && actor != actors.Player)
            {
                return actor;
            }
        }

        return null;
    }

    private static TotemEnemyModel FindAliveEnemy(TotemEnemyService enemies, TotemEnemyTier tier)
    {
        var buffer = new TotemEnemyModel[enemies.Capacity];
        int count = enemies.CopyAliveEnemies(buffer);
        for (int i = 0; i < count; i++)
        {
            if (buffer[i] != null && buffer[i].Tier == tier)
            {
                return buffer[i];
            }
        }

        return null;
    }

    private static IEnumerator ExitCombatHudAndAssertClean(
        TotemGameFlowService flow,
        TotemActorService actors,
        TotemParticipantReadinessService readiness,
        TotemMapService map,
        TotemEnemyWorldService enemyWorld,
        TotemEnemyService enemies,
        TotemEnemyLootService loot)
    {
        flow.EnterMainMenu();
        yield return null;

        TotemEnemyRuntimeSnapshot enemySnapshot = enemies.CaptureSnapshot();
        TotemEnemyLootSnapshot lootSnapshot = loot.CaptureSnapshot();
        Assert.AreEqual(0, enemySnapshot.enemyCount, "CombatHud exit must despawn every enemy runtime model.");
        Assert.AreEqual(0, enemySnapshot.aliveEnemyCount, "CombatHud exit must leave no alive enemy.");
        Assert.AreEqual(0, enemyWorld.ActiveVisualCount, "CombatHud exit must leave no active enemy visual.");
        Assert.IsFalse(enemyWorld.CaptureSnapshot().hasPlan, "CombatHud exit must release the encounter plan.");
        Assert.AreEqual(0, lootSnapshot.activePickupCount, "CombatHud exit must clear runtime loot pickups.");
        Assert.AreEqual(0, loot.ActiveVisualCount, "CombatHud exit must leave no active loot visual.");
        Assert.AreEqual(0, readiness.CaptureSnapshot().participantCount, "CombatHud exit must clear readiness entries.");
        Assert.AreEqual(0, actors.Actors.Count, "CombatHud exit must despawn participant runtime models.");
        Assert.IsNull(map.CurrentMap, "CombatHud exit must release the runtime map.");
        AssertRootHasNoActiveChildren("[TotemEnemies]");
        AssertRootHasNoActiveChildren("[TotemEnemyLoot]");
    }

    private static void AssertRootHasNoActiveChildren(string rootName)
    {
        GameObject root = GameObject.Find(rootName);
        if (root == null)
        {
            return;
        }

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 1; i < transforms.Length; i++)
        {
            Assert.IsFalse(transforms[i].gameObject.activeInHierarchy, rootName + " contains an active runtime child after exit.");
        }
    }

    private static IEnumerator CleanupCurrentRuntime()
    {
        TotemGameRuntime runtime = TotemGameRuntime.Instance;
        if (runtime == null)
        {
            yield break;
        }

        TotemGameFlowService flow = runtime.GetService<TotemGameFlowService>();
        if (flow?.CurrentState == TotemGameFlowState.CombatHud)
        {
            flow.EnterMainMenu();
            yield return null;
        }

        Object.Destroy(runtime.gameObject);
        yield return null;
    }

    private static T RequireService<T>(TotemGameRuntime runtime) where T : class, ITotemRuntimeService
    {
        T service = runtime.GetService<T>();
        Assert.NotNull(service, typeof(T).Name + " should be registered.");
        Assert.AreEqual(TotemRuntimeServiceState.Ready, service.State, typeof(T).Name + " should be ready.");
        return service;
    }
}
#endif
