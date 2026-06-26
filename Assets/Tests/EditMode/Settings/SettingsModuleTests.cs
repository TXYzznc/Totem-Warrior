using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Tattoo.Tests
{
    /// <summary>
    /// SettingsModule EditMode 测试（v1.0）。
    ///
    /// 范围：本期不做按键重绑定，重点验证三态状态机与即时预览/取消回滚契约。
    /// 测试用例：
    ///   - StateMachine_IdleIgnoresPreviewCommitRollback（无 BeginEdit 时操作幂等且不抛）
    ///   - BeginEdit_ThenRollback_RestoresOriginalVolume（REQ-SETTINGS-006）
    ///   - BeginEdit_ThenCommit_StateReturnsToIdle（REQ-SETTINGS-007）
    /// </summary>
    public class SettingsModuleTests
    {
        EventBus     _bus;
        ModuleRunner _runner;
        SettingsModule _settings;

        async UniTask SetupAsync()
        {
            _bus    = new EventBus();
            _runner = new ModuleRunner(_bus);

            _runner.AddModule(new DataTableModule());
            _runner.AddModule(new ResourceModule(_runner));
            _runner.AddModule(new SaveModule(_runner, _bus));
            _runner.AddModule(new AudioModule(_runner));

            _settings = new SettingsModule(_runner, _bus);
            _runner.AddModule(_settings);

            await _runner.StartAsync();
        }

        async UniTask TeardownAsync()
        {
            if (_runner != null) await _runner.StopAsync();
            _bus = null; _runner = null; _settings = null;
        }

        // ────────────────────────────────────────────────────────────────
        // REQ-SETTINGS-006/007：状态机 Idle 时 Preview / Commit / Rollback 应该幂等无副作用
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void StateMachine_IdleIgnoresPreviewCommitRollback()
        {
            // Arrange：未进入 ModuleRunner，直接 new SettingsModule 验证状态机分支
            var bus    = new EventBus();
            var runner = new ModuleRunner(bus);
            var sm     = new SettingsModule(runner, bus);

            // Act + Assert：Idle 状态下三个操作不抛、不崩
            Assert.DoesNotThrow(() => sm.Preview(new SettingsData()),
                "Idle 状态下 Preview 应记 Warn 并忽略，不抛异常");
            Assert.DoesNotThrow(() => sm.Commit(),
                "Idle 状态下 Commit 应记 Warn 并忽略，不抛异常");
            Assert.DoesNotThrow(() => sm.Rollback(),
                "Idle 状态下 Rollback 应记 Warn 并忽略，不抛异常");
        }

        // ────────────────────────────────────────────────────────────────
        // REQ-SETTINGS-006：Rollback 必须恢复 BeginEdit 时的快照值
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void BeginEdit_ThenRollback_RestoresOriginalVolume()
        {
            // 同样不通过 ModuleRunner 启动（避免 SaveModule 异步 IO），直接测纯逻辑
            var bus    = new EventBus();
            var runner = new ModuleRunner(bus);
            var sm     = new SettingsModule(runner, bus);

            // 起始值：SettingsData 默认 MusicVolume=0.8
            var original = sm.GetCurrent();
            Assert.AreEqual(0.8f, original.MusicVolume, 0.001f);

            // BeginEdit → Preview 改音量 → Rollback
            sm.BeginEdit();
            sm.Preview(new SettingsData { MusicVolume = 0.3f, SfxVolume = 0.3f });
            sm.Rollback();

            // 回滚后应恢复到 BeginEdit 时的快照
            var restored = sm.GetCurrent();
            Assert.AreEqual(original.MusicVolume, restored.MusicVolume, 0.001f);
            Assert.AreEqual(original.SfxVolume,   restored.SfxVolume,   0.001f);
        }

        // ────────────────────────────────────────────────────────────────
        // REQ-SETTINGS-007：Commit 后状态机回到 Idle，第二次 Commit 应被忽略
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void BeginEdit_ThenCommit_StateReturnsToIdle()
        {
            var bus    = new EventBus();
            var runner = new ModuleRunner(bus);
            var sm     = new SettingsModule(runner, bus);

            sm.BeginEdit();
            sm.Preview(new SettingsData { MusicVolume = 0.5f });

            // Commit 后应回到 Idle
            Assert.DoesNotThrow(() => sm.Commit());

            // 第二次 Commit（Idle 状态）应记 Warn 并忽略
            Assert.DoesNotThrow(() => sm.Commit(),
                "Idle 状态下重复 Commit 应忽略，不抛异常");
        }
    }
}
