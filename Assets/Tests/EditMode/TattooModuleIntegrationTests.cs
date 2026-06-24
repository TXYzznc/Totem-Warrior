using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Tattoo;
using Tattoo.Data;
using Tattoo.Events;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tattoo.Tests
{
    /// <summary>
    /// TattooModule + DataTableModule 集成测试。依赖 Resources/DataTable/ 下 5 张 JSON。
    /// 用 [UnityTest] + UniTask.ToCoroutine 适配异步模块启动。
    /// </summary>
    public class TattooModuleIntegrationTests
    {
        EventBus _bus;
        ModuleRunner _runner;
        TattooModule _tattoo;

        async UniTask SetupAsync()
        {
            _bus = new EventBus();
            _runner = new ModuleRunner(_bus);
            _runner.AddModule(new DataTableModule());
            _runner.AddModule(new TattooModule(_runner, _bus));
            await _runner.StartAsync();
            _tattoo = _runner.GetModule<TattooModule>();
        }

        async UniTask TeardownAsync()
        {
            if (_runner != null) await _runner.StopAsync();
            _bus = null; _runner = null; _tattoo = null;
        }

        [UnityTest]
        public IEnumerator Equip_RightArmRedLine_PublishesBuildChangedAndEquippedHasOne() =>
            UniTask.ToCoroutine(async () =>
            {
                await SetupAsync();
                try
                {
                    int buildChangedCount = 0;
                    using var _ = _bus.Subscribe<BuildChangedEvent>(e => buildChangedCount++);

                    bool ok = _tattoo.Equip(partId: 4, colorId: 1, patternId: 1);
                    Assert.IsTrue(ok);
                    Assert.AreEqual(1, _tattoo.Equipped.Count);
                    Assert.AreEqual("RightArm", _tattoo.Equipped[0].PartName);
                    Assert.AreEqual("Red", _tattoo.Equipped[0].ColorName);
                    Assert.AreEqual("Line", _tattoo.Equipped[0].PatternName);
                    Assert.AreEqual(1, buildChangedCount);
                }
                finally { await TeardownAsync(); }
            });

        [UnityTest]
        public IEnumerator Fire_AttackHitEvent_TriggersRightArmSlot_AndPublishesEffectApplied() =>
            UniTask.ToCoroutine(async () =>
            {
                await SetupAsync();
                try
                {
                    _tattoo.Equip(partId: 4, colorId: 1, patternId: 1);

                    int effectCount = 0;
                    EffectAppliedEvent captured = null;
                    using var _ = _bus.Subscribe<EffectAppliedEvent>(e => { effectCount++; captured = e; });

                    var target = new Target { Name = "敌人", Health = 100f };
                    _bus.Publish(new AttackHitEvent(target, baseDamage: 10f));

                    Assert.AreEqual(1, effectCount);
                    Assert.IsNotNull(captured);
                    Assert.IsTrue(captured.Results.Count >= 1);
                    Assert.Less(target.Health, 100f);
                }
                finally { await TeardownAsync(); }
            });

        [UnityTest]
        public IEnumerator Fire_NonMatchingEvent_DoesNotTriggerAnySlot() =>
            UniTask.ToCoroutine(async () =>
            {
                await SetupAsync();
                try
                {
                    _tattoo.Equip(partId: 4, colorId: 1, patternId: 1);

                    int effectCount = 0;
                    using var _ = _bus.Subscribe<EffectAppliedEvent>(e => effectCount++);

                    _bus.Publish(new CritHitEvent(new Target(), 10f));
                    _bus.Publish(new DodgePressedEvent());

                    Assert.AreEqual(0, effectCount);
                }
                finally { await TeardownAsync(); }
            });

        [UnityTest]
        public IEnumerator Clear_EmptiesEquipped_AndPublishesBuildChanged() =>
            UniTask.ToCoroutine(async () =>
            {
                await SetupAsync();
                try
                {
                    _tattoo.Equip(partId: 1, colorId: 1, patternId: 1);
                    _tattoo.Equip(partId: 4, colorId: 1, patternId: 1);
                    Assert.AreEqual(2, _tattoo.Equipped.Count);

                    int buildChangedCount = 0;
                    using var _ = _bus.Subscribe<BuildChangedEvent>(e => buildChangedCount++);

                    _tattoo.Clear();
                    Assert.AreEqual(0, _tattoo.Equipped.Count);
                    Assert.GreaterOrEqual(buildChangedCount, 1);
                }
                finally { await TeardownAsync(); }
            });

        [UnityTest]
        public IEnumerator Equip_NonExistentColorId_ReturnsFalse_AndDoesNotModify() =>
            UniTask.ToCoroutine(async () =>
            {
                await SetupAsync();
                try
                {
                    // 容错路径会通过 FrameworkLogger.Error 打 Debug.LogError；NUnit 默认把它当失败，需先注册期望。
                    LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*ColorId=999.*NotFound.*"));

                    bool ok = _tattoo.Equip(partId: 1, colorId: 999, patternId: 1);
                    Assert.IsFalse(ok);
                    Assert.AreEqual(0, _tattoo.Equipped.Count);
                }
                finally { await TeardownAsync(); }
            });
    }
}
