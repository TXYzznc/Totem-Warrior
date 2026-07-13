#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemAudioRuntimeDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Audio Runtime";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            CheckAudioCueCatalog(context);
            CheckAudioRuntimeRouting(context);
            context.Pass("Totem audio runtime contract is ready.");
        }

        private static void CheckAudioCueCatalog(GFDiagnosticScenarioContext context)
        {
            var catalog = TotemDataService.LoadGameplayCatalogOrDefault();
            var cues = catalog.CreateAudioCueDefinitions();
            context.Assert(cues.Length >= 14, "Audio cue catalog should include BGM, combat SFX, death SFX and UI click.");
            AssertCue(context, cues, "bgm_main_menu", TotemAudioCueKind.Bgm);
            AssertCue(context, cues, "bgm_in_game", TotemAudioCueKind.Bgm);
            AssertCue(context, cues, "sfx_hit_melee", TotemAudioCueKind.Sfx);
            AssertCue(context, cues, "sfx_kill", TotemAudioCueKind.Sfx);
            AssertCue(context, cues, "sfx_player_died", TotemAudioCueKind.Sfx);
            AssertCue(context, cues, "sfx_ui_click", TotemAudioCueKind.Sfx);

            var cueIds = cues.Select(cue => cue.CueId).ToHashSet(StringComparer.Ordinal);
            var bossPhases = catalog.CreateBossPhases();
            for (int i = 0; i < bossPhases.Length; i++)
            {
                context.Assert(cueIds.Contains(bossPhases[i].PhaseBGMCueId), $"Boss phase BGM cue should exist: {bossPhases[i].PhaseBGMCueId}");
            }
        }

        private static void CheckAudioRuntimeRouting(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemAudioDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            try
            {
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterAudioDiagnosticServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var data = runtime.GetService<TotemDataService>();
                var settings = runtime.GetService<TotemSettingsService>();
                var actor = runtime.GetService<TotemActorService>();
                var weapon = runtime.GetService<TotemWeaponService>();
                var enemies = runtime.GetService<TotemEnemyService>();
                var audio = runtime.GetService<TotemAudioService>();
                var clock = runtime.GetService<TotemMatchClockService>();

                context.Assert(audio != null, "Audio service should be registered.");
                enemies.RegisterCatalogDefinitions(
                    data.GameplayCatalog.CreateEnemyDefinitions(),
                    data.GameplayCatalog.CreateEnemyAbilityDefinitions(),
                    data.GameplayCatalog.CreateBossPhases());
                context.Assert(audio.TryGetCue("bgm_boss_phase3", out var phase3Cue) && phase3Cue.Kind == TotemAudioCueKind.Bgm, "Audio service should expose Boss phase 3 BGM cue.");

                flow.EnterMainMenu();
                var menuSnapshot = audio.CaptureSnapshot();
                context.AssertEqual("bgm_main_menu", menuSnapshot.currentBgmCueId, "audio.menu.bgm");
                context.AssertEqual("BGM/main_menu.ogg", menuSnapshot.currentBgmAssetName, "audio.menu.bgmAsset");
                context.AssertEqual("GameFlow.MainMenu", menuSnapshot.lastReason, "audio.menu.reason");
                context.AssertEqual(1, menuSnapshot.bgmRequestCount, "audio.menu.bgmRequestCount");

                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });
                clock?.SetWorldTimeForDiagnostics(TotemCombatRelationshipService.ParticipantCombatGraceSeconds);
                var combatStartSnapshot = audio.CaptureSnapshot();
                context.AssertEqual("bgm_in_game", combatStartSnapshot.currentBgmCueId, "audio.combat.startBgm");
                context.AssertEqual("GameFlow.CombatHud", combatStartSnapshot.lastReason, "audio.combat.startReason");
                context.AssertEqual(2, combatStartSnapshot.bgmRequestCount, "audio.combat.startBgmRequestCount");
                context.Assert(enemies.TrySpawn(
                    new TotemEnemySpawnRequest(910001, "boss_ai_core_zero", actor.Player.Position + Vector3.forward * 4f, 1, "diagnostic.audio", 0f),
                    out var bossEnemy,
                    out var bossSpawnReason), $"Audio diagnostic should spawn a native Boss enemy: {bossSpawnReason}");
                int bossPhaseEventCount = 0;
                enemies.BossPhaseChanged += evt =>
                {
                    if (evt.Enemy == bossEnemy)
                    {
                        bossPhaseEventCount++;
                    }
                };
                enemies.Tick(0.1f);
                var combatSnapshot = audio.CaptureSnapshot();
                context.AssertEqual("bgm_boss_phase1", combatSnapshot.currentBgmCueId, "audio.combat.bossPhase1Bgm");
                context.AssertEqual(1, combatSnapshot.observedBossPhase, "audio.combat.observedBossPhase");
                context.AssertEqual("bgm_boss_phase1", combatSnapshot.observedBossBgmCueId, "audio.combat.observedBossBgmCueId");
                context.AssertEqual("EnemyBoss.Phase1", combatSnapshot.lastReason, "audio.combat.bossPhase1Reason");
                context.AssertEqual(3, combatSnapshot.bgmRequestCount, "audio.combat.bgmRequestCount");
                context.AssertEqual(1, bossPhaseEventCount, "audio.combat.bossPhaseEventCount");

                settings.BeginEdit();
                settings.Preview(0.55f, 0.25f, 1);
                var settingsSnapshot = audio.CaptureSnapshot();
                AssertNear(context, 0.55f, settingsSnapshot.bgmVolume, "audio.settings.bgm");
                AssertNear(context, 0.25f, settingsSnapshot.sfxVolume, "audio.settings.sfx");

                var player = actor.Player;
                weapon.EquipWeapon(player, "knife_basic");
                var hitTarget = actor.Actors.First(item => item.ControllerKind == TotemParticipantControllerKind.LightBot);
                context.AssertEqual(TotemParticipantLifecycle.Active, player.Lifecycle, "audio.damage.sourceLifecycle");
                context.AssertEqual(TotemParticipantLifecycle.Active, hitTarget.Lifecycle, "audio.damage.targetLifecycle");
                actor.ApplyDamage(hitTarget, 10f, player, "PlayerAttack");
                var hitSnapshot = audio.CaptureSnapshot();
                context.AssertEqual("sfx_hit_melee", hitSnapshot.lastSfxCueId, "audio.damage.hitCue");
                context.AssertEqual("SFX/hit_melee.wav", hitSnapshot.lastSfxAssetName, "audio.damage.hitAsset");
                context.AssertEqual("Damage.PlayerAttack", hitSnapshot.lastReason, "audio.damage.hitReason");
                context.Assert(enemies.TryApplyDamage(
                    bossEnemy.CombatantId,
                    player,
                    bossEnemy.MaxHealth * 0.45f,
                    "DiagnosticAudioBossPhase2",
                    0.2f,
                    out var phase2Damage) && phase2Damage > 0f,
                    "Participant damage should enter EnemyService for Boss phase 2.");
                enemies.Tick(0.1f);
                var phase2Snapshot = audio.CaptureSnapshot();
                context.AssertEqual("bgm_boss_phase2", phase2Snapshot.currentBgmCueId, "audio.boss.phase2Bgm");
                context.AssertEqual("bgm_boss_phase2", phase2Snapshot.observedBossBgmCueId, "audio.boss.phase2ObservedBgm");
                context.AssertEqual("EnemyBoss.Phase2", phase2Snapshot.lastReason, "audio.boss.phase2Reason");

                context.Assert(enemies.TryApplyDamage(
                    bossEnemy.CombatantId,
                    player,
                    bossEnemy.MaxHealth * 0.35f,
                    "DiagnosticAudioBossPhase3",
                    0.3f,
                    out var phase3Damage) && phase3Damage > 0f,
                    "Participant damage should enter EnemyService for Boss phase 3.");
                enemies.Tick(0.1f);
                var phase3Snapshot = audio.CaptureSnapshot();
                context.AssertEqual("bgm_boss_phase3", phase3Snapshot.currentBgmCueId, "audio.boss.phase3Bgm");
                context.AssertEqual(3, phase3Snapshot.observedBossPhase, "audio.boss.phase3Observed");
                context.AssertEqual("bgm_boss_phase3", phase3Snapshot.observedBossBgmCueId, "audio.boss.phase3ObservedBgm");
                context.AssertEqual("EnemyBoss.Phase3", phase3Snapshot.lastReason, "audio.boss.phase3Reason");
                context.AssertEqual(3, bossPhaseEventCount, "audio.boss.phaseEventCount");
                context.AssertEqual(3, enemies.FindController(bossEnemy.CombatantId)?.BossPhase ?? 0, "audio.boss.nativePhase");

                var light = actor.Actors.First(item => item.Kind == TotemActorKind.LightAi);
                actor.ApplyDamage(light, light.Health + 1f, player, "PlayerAttack");
                var killSnapshot = audio.CaptureSnapshot();
                context.AssertEqual("sfx_kill", killSnapshot.lastSfxCueId, "audio.damage.killCue");
                context.AssertEqual("Killed.PlayerAttack", killSnapshot.lastReason, "audio.damage.killReason");

                actor.ApplyDamage(player, player.Health + 1f, bossEnemy, "EnemyAbility:core_zero_beam");
                var playerDiedSnapshot = audio.CaptureSnapshot();
                context.AssertEqual("sfx_player_died", playerDiedSnapshot.lastSfxCueId, "audio.damage.playerDiedCue");
                context.AssertEqual("PlayerDied.EnemyAbility:core_zero_beam", playerDiedSnapshot.lastReason, "audio.damage.playerDiedReason");
                context.AssertEqual(bossEnemy.CombatantId, actor.LastDamage.Source?.CombatantId ?? 0, "audio.damage.playerDiedEnemySource");

                int missingBefore = audio.CaptureSnapshot().missingCueCount;
                context.Assert(!audio.PlaySfxCue("sfx_missing_diagnostic", Vector3.zero, "Diagnostic.MissingCue"), "Missing audio cue should fail without blocking.");
                var missingSnapshot = audio.CaptureSnapshot();
                context.AssertEqual(missingBefore + 1, missingSnapshot.missingCueCount, "audio.missingCue.count");
                context.AssertEqual("sfx_missing_diagnostic", missingSnapshot.lastMissingCueId, "audio.missingCue.lastCueId");
                context.AssertEqual("Diagnostic.MissingCue", missingSnapshot.lastReason, "audio.missingCue.reason");

                context.Assert(audio.PlaySfxCue("sfx_dodge", player.Position, "Diagnostic.Dodge"), "Dodge cue should play once.");
                int skipBefore = audio.CaptureSnapshot().intervalSkipCount;
                context.Assert(!audio.PlaySfxCue("sfx_dodge", player.Position, "Diagnostic.DodgeRepeat"), "Immediate duplicate dodge cue should be interval-skipped.");
                var skippedSnapshot = audio.CaptureSnapshot();
                context.AssertEqual(skipBefore + 1, skippedSnapshot.intervalSkipCount, "audio.intervalSkip.count");
                context.AssertEqual("sfx_dodge", skippedSnapshot.lastSkippedCueId, "audio.intervalSkip.lastCueId");
                context.AssertEqual("Diagnostic.DodgeRepeat", skippedSnapshot.lastReason, "audio.intervalSkip.reason");
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

        private static void RegisterAudioDiagnosticServices(TotemGameRuntime runtime)
        {
            runtime.RegisterService(new TotemGameFlowService());
            runtime.RegisterService(new TotemMatchClockService());
            runtime.RegisterService(new TotemDataService());
            runtime.RegisterService(new TotemAssetService());
            runtime.RegisterService(new TotemSettingsService());
            runtime.RegisterService(new TotemMapService());
            runtime.RegisterService(new TotemCombatRelationshipService());
            runtime.RegisterService(new TotemActorService());
            runtime.RegisterService(new TotemWeaponService());
            runtime.RegisterService(new TotemEnemyService());
            runtime.RegisterService(new TotemAudioService());
        }

        private static void AssertCue(GFDiagnosticScenarioContext context, TotemAudioCueDefinition[] cues, string cueId, TotemAudioCueKind kind)
        {
            var cue = cues.FirstOrDefault(item => string.Equals(item.CueId, cueId, StringComparison.Ordinal));
            context.Assert(cue != null, $"Audio cue should exist: {cueId}");
            context.AssertEqual(kind.ToString(), cue?.Kind.ToString() ?? string.Empty, $"audioCue.{cueId}.kind");
            context.Assert(!string.IsNullOrWhiteSpace(cue?.AssetName), $"Audio cue should define an asset name: {cueId}");
        }

        private static void AssertNear(GFDiagnosticScenarioContext context, float expected, float actual, string name)
        {
            context.Detail($"{name}.actual", actual);
            context.Assert(Mathf.Abs(expected - actual) <= 0.001f, $"{name}: expected={expected}, actual={actual}");
        }
    }
}
#endif
