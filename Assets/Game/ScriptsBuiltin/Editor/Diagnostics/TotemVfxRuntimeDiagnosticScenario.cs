#if UNITY_EDITOR
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemVfxRuntimeDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem VFX Runtime";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            CheckEffectKeyMapping(context);
            CheckEffectSprites(context);
            CheckProjectileSignal(context);
            CheckCombatFeedback(context);
            context.Pass("Totem VFX runtime contract is ready.");
        }

        private static void CheckEffectKeyMapping(GFDiagnosticScenarioContext context)
        {
            context.AssertEqual("effect.attack.hit", TotemVfxService.ResolveAttackHitKey("knife_basic"), "vfx.attack.key");
            context.AssertEqual("effect.skill.burst", TotemVfxService.ResolveSkillEffectKey("skill_fireball_01"), "vfx.skill.burst.key");
            context.AssertEqual("effect.boss.bolt", TotemVfxService.ResolveSkillEffectKey("skill_beam"), "vfx.boss.beam.key");
            context.AssertEqual("effect.skill.burst", TotemVfxService.ResolveSkillEffectKey("boss_phase_bolt"), "vfx.boss.legacyBolt.notActiveBossPhaseKey");
            context.AssertEqual("effect.projectile.bullet_pistol", TotemVfxService.ResolveProjectileEffectKey("bullet_pistol"), "vfx.projectile.bullet.key");
            context.AssertEqual("effect.projectile.arrow_bow", TotemVfxService.ResolveProjectileEffectKey("arrow_bow"), "vfx.projectile.arrow.key");
            context.AssertEqual("effect.attack.hit", TotemVfxService.ResolveProjectileEffectKey("unknown_projectile"), "vfx.projectile.unknownFallback.key");
        }

        private static void CheckEffectSprites(GFDiagnosticScenarioContext context)
        {
            var service = new TotemAssetService();
            service.ReloadRuntimeAssetCatalog();
            context.Assert(service.RuntimeAssetCatalogLoadedFromFile, $"Runtime asset catalog should load from file: {service.RuntimeAssetCatalogMessage}");
            AssertSpriteLoads(context, service, "effect.attack.hit");
            AssertSpriteLoads(context, service, "effect.skill.burst");
            AssertSpriteLoads(context, service, "effect.boss.bolt");
            AssertSpriteLoads(context, service, "effect.projectile.bullet_pistol");
            AssertSpriteLoads(context, service, "effect.projectile.arrow_bow");
        }

        private static void AssertSpriteLoads(GFDiagnosticScenarioContext context, TotemAssetService service, string assetKey)
        {
            bool loaded = service.TryLoadSprite(assetKey, out var sprite) && sprite != null;
            context.Detail($"{assetKey}.vfxSpriteLoaded", loaded);
            context.Assert(loaded, $"{assetKey} should load for runtime VFX.");
        }

        private static void CheckProjectileSignal(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemVfxProjectileDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                runtime.RegisterService(new TotemAssetService());
                runtime.RegisterService(new TotemVfxService());
                runtime.StartRuntime();

                var vfx = runtime.GetService<TotemVfxService>();
                var bullet = new TotemProjectileDefinition
                {
                    ProjectileId = "bullet_pistol",
                    Speed = 30f,
                    MaxRange = 20f,
                    AoeRadius = 0f,
                };

                context.Assert(vfx.SpawnProjectileTrail(Vector3.zero, Vector3.forward * 6f, bullet, true, false), "Bullet projectile trail signal should spawn with the catalog projectile sprite.");
                var snapshot = vfx.CaptureSnapshot();
                context.AssertEqual(1, vfx.ProjectileSpawnedCount, "vfx.projectile.spawnedCount");
                context.AssertEqual("bullet_pistol", vfx.LastProjectileId, "vfx.projectile.lastId");
                context.AssertEqual(1, snapshot.spriteRequestCount, "vfx.projectile.spriteRequestCount");
                context.AssertEqual(0, snapshot.spriteMissingCount, "vfx.projectile.spriteMissingCount");
                context.AssertEqual("effect.projectile.bullet_pistol", snapshot.lastAssetKey, "vfx.projectile.lastAssetKey");
                context.AssertEqual(string.Empty, snapshot.lastMissingAssetKey ?? string.Empty, "vfx.projectile.lastMissingAssetKey");

                var arrow = new TotemProjectileDefinition
                {
                    ProjectileId = "arrow_bow",
                    Speed = 24f,
                    MaxRange = 28f,
                    AoeRadius = 0f,
                };
                context.Assert(vfx.SpawnProjectileTrail(Vector3.zero, Vector3.forward * 8f, arrow, true, true), "Arrow projectile trail signal should spawn with its catalog projectile sprite.");
                var arrowSnapshot = vfx.CaptureSnapshot();
                context.AssertEqual(2, vfx.ProjectileSpawnedCount, "vfx.projectile.arrow.spawnedCount");
                context.AssertEqual("arrow_bow", vfx.LastProjectileId, "vfx.projectile.arrow.lastId");
                context.AssertEqual(2, arrowSnapshot.spriteRequestCount, "vfx.projectile.arrow.spriteRequestCount");
                context.AssertEqual(0, arrowSnapshot.spriteMissingCount, "vfx.projectile.arrow.spriteMissingCount");
                context.AssertEqual("effect.projectile.arrow_bow", arrowSnapshot.lastAssetKey, "vfx.projectile.arrow.lastAssetKey");
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

        private static void CheckCombatFeedback(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemVfxFeedbackDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                runtime.RegisterService(new TotemGameFlowService());
                runtime.RegisterService(new TotemDataService());
                runtime.RegisterService(new TotemAssetService());
                runtime.RegisterService(new TotemMapService());
                runtime.RegisterService(new TotemCombatRelationshipService());
                runtime.RegisterService(new TotemActorService());
                runtime.RegisterService(new TotemCameraService());
                runtime.RegisterService(new TotemVfxService());
                runtime.RegisterService(new TotemEnemyService());
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var actor = runtime.GetService<TotemActorService>();
                var camera = runtime.GetService<TotemCameraService>();
                var vfx = runtime.GetService<TotemVfxService>();
                var enemies = runtime.GetService<TotemEnemyService>();
                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });

                context.Assert(actor.Player != null, "VFX feedback diagnostic should spawn a player.");
                context.Assert(enemies.TrySpawn(
                    new TotemEnemySpawnRequest(920001, "boss_ai_core_zero", actor.Player.Position + Vector3.forward * 2f, 1, "diagnostic.vfx", 0f),
                    out var boss,
                    out var spawnReason), $"VFX feedback diagnostic should spawn a native Boss enemy: {spawnReason}");
                context.AssertEqual(1, enemies.CaptureSnapshot().bossCount, "vfx.feedback.enemyBossCount");

                actor.ApplyDamage(actor.Player, 10f, boss, "EnemyBossAbility:core_zero_pulse");
                var firstFeedback = vfx.CaptureSnapshot();
                context.AssertEqual(1, firstFeedback.cameraShakeRequestCount, "vfx.feedback.shakeRequestCount");
                AssertNear(context, 0.10f, firstFeedback.lastCameraShakeAmplitude, "vfx.feedback.bossShakeAmplitude");
                AssertNear(context, 0.18f, firstFeedback.lastCameraShakeDuration, "vfx.feedback.bossShakeDuration");
                context.AssertEqual(1, firstFeedback.floatingTextSpawnedCount, "vfx.feedback.floatCountAfterBossAttack");
                context.AssertEqual("10", firstFeedback.lastFloatingText, "vfx.feedback.lastFloatText");
                context.Assert(firstFeedback.lastFloatingTextStrong, "Boss attack damage float should use the strong style.");

                camera.LateTick(0.016f);
                var cameraSnapshot = camera.CaptureSnapshot();
                context.Assert(cameraSnapshot.shakeRemainingSec > 0f, "Camera shake should remain active after one tick.");
                context.Assert(cameraSnapshot.lastShakeOffset.sqrMagnitude > 0f, "Camera shake should produce a non-zero offset.");

                actor.ApplyDamage(actor.Player, 70f, boss, "EnemyBossAbility:core_zero_beam");
                vfx.Tick(0.25f);
                var danger = vfx.CaptureSnapshot();
                context.Assert(danger.vignettePulsing, "Low player health should start vignette pulse.");
                context.Assert(danger.vignetteOverlayActive, "Low player health should create a vignette overlay.");
                context.Assert(danger.vignetteIntensity > 0f, "Vignette intensity should become visible while pulsing.");
                context.AssertEqual(1, danger.vignettePulseCount, "vfx.feedback.vignettePulseCount");
                context.Assert(danger.playerHealthRatio < 0.3f, "VFX snapshot should expose low player health ratio.");
                context.AssertEqual(boss.CombatantId, actor.LastDamage.Source?.CombatantId ?? 0, "vfx.feedback.enemyDamageSource");

                int shakeCountBeforeStatus = danger.cameraShakeRequestCount;
                int floatCountBeforeStatus = danger.floatingTextSpawnedCount;
                actor.Player.Heal(100f);
                vfx.Tick(0.1f);
                context.Assert(!vfx.CaptureSnapshot().vignettePulsing, "Vignette pulse should stop after player heals above danger threshold.");

                actor.ApplyDamage(actor.Player, 1f, null, "Status:Burn");
                context.AssertEqual(shakeCountBeforeStatus, vfx.CaptureSnapshot().cameraShakeRequestCount, "vfx.feedback.statusShouldNotShake");
                context.AssertEqual(floatCountBeforeStatus, vfx.CaptureSnapshot().floatingTextSpawnedCount, "vfx.feedback.statusShouldNotFloat");
            }
            finally
            {
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                Object.DestroyImmediate(runtimeObject);
            }

            var standalone = new TotemVfxService();
            var source = new TotemActorModel(new TotemActorSpawnInfo { ActorId = 1, Name = "Player", Kind = TotemActorKind.Player, Position = Vector3.zero, MaxHealth = 100f });
            var target = new TotemActorModel(new TotemActorSpawnInfo { ActorId = 2, Name = "Target", Kind = TotemActorKind.LightAi, Position = Vector3.forward, MaxHealth = 100f });
            context.Assert(!standalone.SpawnAttackHit(Vector3.zero, "knife_basic", false), "Standalone VFX without asset service should fail sprite spawning visibly.");
            var standaloneSprite = standalone.CaptureSnapshot();
            context.AssertEqual(1, standaloneSprite.spriteRequestCount, "vfx.sprite.standaloneRequestCount");
            context.AssertEqual(1, standaloneSprite.spriteMissingCount, "vfx.sprite.standaloneMissingCount");
            context.AssertEqual("effect.attack.hit", standaloneSprite.lastMissingAssetKey, "vfx.sprite.standaloneLastMissingKey");
            bool requested = standalone.RequestCombatFeedback(new TotemDamageRecord
            {
                Source = source,
                Target = target,
                Amount = 25f,
                Reason = "PlayerAttack",
            });
            context.Assert(!requested, "Standalone VFX feedback without camera service should skip shake cleanly.");
            context.AssertEqual(1, standalone.CaptureSnapshot().cameraShakeSkippedCount, "vfx.feedback.noCameraSkippedCount");
            context.AssertEqual(1, standalone.CaptureSnapshot().floatingTextActiveCount, "vfx.feedback.standaloneFloatActiveBeforeCleanup");
            standalone.Tick(1f);
            context.AssertEqual(0, standalone.CaptureSnapshot().floatingTextActiveCount, "vfx.feedback.standaloneFloatCleaned");
            context.AssertEqual(0, TotemRuntimeResidueCleaner.FindRuntimeResiduals().Count, "vfx.feedback.runtimeResidueAfterStandalone");
        }

        private static void AssertNear(GFDiagnosticScenarioContext context, float expected, float actual, string name)
        {
            context.Detail($"{name}.actual", actual);
            context.Assert(Mathf.Abs(expected - actual) <= 0.001f, $"{name}: expected={expected}, actual={actual}");
        }
    }
}
#endif
