#if UNITY_EDITOR
using System.Linq;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemStartupProtectionDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Startup Protection Runtime";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            GameObject runtimeObject = null;
            TotemGameRuntime runtime = null;
            using (TotemMapService.UsePcgRuntimeProfile(TotemPcgRuntimeProfile.DiagnosticFast))
            {
                try
                {
                    runtimeObject = new GameObject("[TotemStartupProtectionDiagnosticRuntime]");
                    runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                    runtime.RegisterService(new TotemGameFlowService());
                    runtime.RegisterService(new TotemMapService());
                    runtime.RegisterService(new TotemCombatRelationshipService());
                    runtime.RegisterService(new TotemActorService());
                    runtime.RegisterService(new TotemEnemyService());
                    runtime.RegisterService(new TotemAIService());
                    runtime.StartRuntime();

                    var flow = runtime.GetService<TotemGameFlowService>();
                    var actor = runtime.GetService<TotemActorService>();
                    var enemies = runtime.GetService<TotemEnemyService>();
                    var ai = runtime.GetService<TotemAIService>();
                    actor.BeginPlayerStartupProtection("Diagnostics.Startup");
                    flow.ConfirmStartup(1, "knife_basic", new[] { 1 });

                    var player = actor.Player;
                    context.Assert(player != null, "Startup protection diagnostic requires a player Participant.");
                    if (player == null)
                    {
                        return;
                    }

                    context.Assert(enemies.TrySpawn(
                        new TotemEnemySpawnRequest(900001, "boss_ai_core_zero", player.Position + Vector3.forward * 2f, 1, "diagnostic.startup", 0f),
                        out var boss,
                        out var spawnReason), $"Startup protection diagnostic should spawn a native Boss enemy: {spawnReason}");
                    context.Assert(boss != null, "Startup protection diagnostic requires an EnemyModel Boss.");
                    context.AssertEqual(50, actor.Actors.Count, "startupProtection.participantCount");
                    context.Assert(actor.Actors.All(item => item.Domain == TotemCombatantDomain.Participant), "ActorService must contain Participants only.");
                    context.AssertEqual(1, enemies.CaptureSnapshot().bossCount, "startupProtection.enemyBossCount");
                    context.Assert(actor.PlayerStartupInvulnerable, "Player must remain invulnerable before Combat HUD readiness.");
                    context.Assert(!actor.CanEnemyTarget(player), "Enemy target selection must suppress the protected player.");
                    context.Assert(ai.CaptureSnapshot().playerStartupTargetSuppressed, "AI snapshot must expose startup player target suppression.");

                    float healthBefore = player.Health;
                    actor.ApplyDamage(player, 999f, boss, "BossAttack");
                    actor.ApplyDamage(player, 999f, null, "TerrainHazard");
                    context.AssertEqual(healthBefore, player.Health, "startupProtection.playerHealth");
                    context.AssertEqual(2, actor.PlayerStartupDamageBlockedCount, "startupProtection.blockedDamageCount");

                    var stalePlayer = new TotemActorModel(new TotemActorSpawnInfo
                    {
                        ActorId = -1,
                        Name = "StalePlayer",
                        Kind = TotemActorKind.Player,
                        MaxHealth = 100f,
                    });
                    context.Assert(!actor.TryReleasePlayerStartupProtection(stalePlayer, "Diagnostics.StaleHUD"), "A stale HUD callback must not release protection for a new player instance.");
                    context.Assert(actor.TryReleasePlayerStartupProtection(player, "Diagnostics.HUDReady"), "The active HUD/player pair must release startup protection.");
                    context.Assert(!actor.PlayerStartupInvulnerable, "Startup protection must be disabled after HUD readiness.");
                    context.Assert(actor.CanEnemyTarget(player), "Player must become targetable after HUD readiness.");

                    actor.ApplyDamage(player, 10f, boss, "BossAttack");
                    context.AssertEqual(healthBefore - 10f, player.Health, "startupProtection.healthAfterRelease");
                    context.AssertEqual(boss.CombatantId, actor.LastDamage.Source?.CombatantId ?? 0, "startupProtection.enemyDamageSource");
                    context.AssertEqual(TotemCombatantDomain.Enemy, actor.LastDamage.Source?.Domain ?? TotemCombatantDomain.Participant, "startupProtection.enemyDamageDomain");
                    context.Detail("startupProtection.releaseReason", actor.CaptureActorSnapshot().playerStartupProtectionReason);
                    context.Pass("HUD-gated startup protection blocks damage and enemy targeting until the player is controllable.");
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
