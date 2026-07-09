using System.Collections.Generic;
using AttackSystem.Events;
using NUnit.Framework;
using Tattoo.Data;

namespace Combat.Tests
{
    /// <summary>
    /// TC-Status-01~05 + TC-Damage-D5/D8
    ///
    /// 测试 StatusEffectModule 核心逻辑：
    ///   - tick 周期 (0.5s)
    ///   - 到期清理
    ///   - 同名 status 合并取 max
    ///   - null / 空参数防护
    ///   - ClearAllStatuses 死亡清理
    ///
    /// 约束：
    ///   - 不依赖 ModuleRunner 完整启动
    ///   - StatusEffectModule 用真实 EventBus（直接 new）
    ///   - ModuleRunner 参数传 null 时 StatusEffectModule 构造抛 ArgumentNullException
    ///     → 使用 FakeModuleRunner 桩（继承或包装，见 Helper 内部类）
    /// </summary>
    public class StatusEffectModuleTests
    {
        // ────────────────────────────────────────────────
        // 基础桩：StatusEffectModule 依赖 ModuleRunner + EventBus
        // 构造不走 ModuleRunner 完整生命周期，故用 null 桩
        // ────────────────────────────────────────────────

        EventBus            _bus;
        StatusEffectModule  _module;
        Target              _target;

        // 事件收集
        List<StatusEffectTickedEvent>  _ticked   = new();
        List<StatusEffectExpiredEvent> _expired  = new();
        List<StatusEffectAppliedEvent> _applied  = new();

        [SetUp]
        public void SetUp()
        {
            _bus    = new EventBus();
            // ModuleRunner 是普通类，可直接 new；StatusEffectModule 构造只做 null 检查不调用 runner 方法
            _module = new StatusEffectModule(new ModuleRunner(_bus), _bus);

            _target = new Target { Name = "TestEnemy", Health = 100f };

            _ticked.Clear();
            _expired.Clear();
            _applied.Clear();

            _bus.Subscribe<StatusEffectTickedEvent>(e  => _ticked.Add(e));
            _bus.Subscribe<StatusEffectExpiredEvent>(e => _expired.Add(e));
            _bus.Subscribe<StatusEffectAppliedEvent>(e => _applied.Add(e));
        }

        // ════════════════════════════════════════════════
        // TC-Status-01 / TC-Damage-D5: Burn tick 周期与 DoT 伤害
        // ════════════════════════════════════════════════

        [Test]
        public void BurnStatus_Tick_EmitsDamageEveryHalfSecond()
        {
            // Arrange
            _module.ApplyStatus(_target, "Burn", dps: 10f, duration: 1.5f);
            Assert.AreEqual(1, _applied.Count, "ApplyStatus 应发 StatusEffectAppliedEvent");

            // Act: 第 1 次 tick (0.5s) → RemainingSec = 1.0 > 0 → TickedEvent(damage=5)
            _module.OnUpdate(0.5f);
            Assert.AreEqual(1, _ticked.Count,  "0.5s 后应有 1 次 TickedEvent");
            Assert.AreEqual(0, _expired.Count, "1.0s 剩余时不应 Expired");
            Assert.AreEqual(5f, _ticked[0].Damage, 0.001f, "damage = dps(10) × 0.5 = 5");
            Assert.AreEqual("Burn", _ticked[0].StatusName);

            // Act: 第 2 次 tick (0.5s) → RemainingSec = 0.5 > 0 → TickedEvent(damage=5)
            _module.OnUpdate(0.5f);
            Assert.AreEqual(2, _ticked.Count,  "1.0s 后应有 2 次 TickedEvent");
            Assert.AreEqual(0, _expired.Count, "0.5s 剩余时不应 Expired");

            // Act: 第 3 次 tick (0.5s) → RemainingSec = 0 → 过期 → ExpiredEvent
            _module.OnUpdate(0.5f);
            Assert.AreEqual(2, _ticked.Count,  "过期帧不再发 TickedEvent");
            Assert.AreEqual(1, _expired.Count, "应发 1 次 ExpiredEvent");
            Assert.AreEqual("Burn", _expired[0].StatusName);

            // 状态移除后 GetActiveStatuses 应为空
            var statuses = _module.GetActiveStatuses(_target);
            Assert.AreEqual(0, statuses.Count, "到期后 _active 应清空");
        }

        [Test]
        public void BurnStatus_BeforeTickInterval_NoEventsEmitted()
        {
            // 边缘：dt 不满 0.5s 时不发任何 tick
            _module.ApplyStatus(_target, "Burn", dps: 10f, duration: 2f);
            _module.OnUpdate(0.3f);

            Assert.AreEqual(0, _ticked.Count,  "dt=0.3s 不满 TickInterval，不发 TickedEvent");
            Assert.AreEqual(0, _expired.Count, "dt=0.3s 不满 TickInterval，不发 ExpiredEvent");
        }

        // ════════════════════════════════════════════════
        // TC-Status-02: Poison 持续时间到期清理 _active
        // ════════════════════════════════════════════════

        [Test]
        public void PoisonStatus_AfterExpiry_ActiveDictCleaned()
        {
            _module.ApplyStatus(_target, "Poison", dps: 5f, duration: 2.0f);

            // 4 次 tick → 第 4 次时 RemainingSec = 0 → Expired
            for (int i = 0; i < 4; i++) _module.OnUpdate(0.5f);

            Assert.AreEqual(1, _expired.Count, "2.0s 后应 Expired 恰好 1 次");
            Assert.AreEqual(0, _module.GetActiveStatuses(_target).Count,
                "Poison 到期后 GetActiveStatuses 应为空");
        }

        // ════════════════════════════════════════════════
        // TC-Status-03: Shock 同名叠加取 max
        // TC-Damage-D8: accumulator max 合并规则
        // ════════════════════════════════════════════════

        [Test]
        public void ShockStatus_DoubleApply_MergesMaxDpsAndMaxDuration()
        {
            _module.ApplyStatus(_target, "Shock", dps: 8f, duration: 3f);
            _module.ApplyStatus(_target, "Shock", dps: 12f, duration: 1f);

            var statuses = _module.GetActiveStatuses(_target);
            Assert.AreEqual(1, statuses.Count, "同名 Shock 不应叠层，应合并为 1 条");
            Assert.AreEqual(12f, statuses[0].DPS,          0.001f, "DPS 取 max(8,12) = 12");
            Assert.AreEqual(3f,  statuses[0].RemainingSec, 0.001f, "RemainingSec 取 max(3,1) = 3");
        }

        [Test]
        public void ShockStatus_LowerDpsRefresh_KeepsOldDps_UpdatesDuration()
        {
            // 边缘：低 DPS 刷新只改 duration
            _module.ApplyStatus(_target, "Shock", dps: 12f, duration: 3f);
            _module.ApplyStatus(_target, "Shock", dps: 5f,  duration: 5f);

            var statuses = _module.GetActiveStatuses(_target);
            Assert.AreEqual(12f, statuses[0].DPS,          0.001f, "低 DPS 不覆盖高 DPS");
            Assert.AreEqual(5f,  statuses[0].RemainingSec, 0.001f, "duration 取 max(3,5) = 5");
        }

        // ════════════════════════════════════════════════
        // TC-Status-04: Stun null / 空参数防护
        // ════════════════════════════════════════════════

        [Test]
        public void ApplyStatus_NullTarget_DoesNotThrow_ActiveEmpty()
        {
            Assert.DoesNotThrow(() => _module.ApplyStatus(null, "Stun", dps: 5f, duration: 2f),
                "null target 不应抛异常");
            // 内部 _active 不应写入任何 key
            // 用有效 target 查询不受污染
            Assert.AreEqual(0, _module.GetActiveStatuses(_target).Count);
        }

        [Test]
        public void ApplyStatus_EmptyStatusName_DoesNotThrow_ActiveEmpty()
        {
            Assert.DoesNotThrow(() => _module.ApplyStatus(_target, "", dps: 5f, duration: 2f));
            Assert.AreEqual(0, _module.GetActiveStatuses(_target).Count);
        }

        [Test]
        public void ApplyStatus_ZeroDuration_DoesNotThrow_ActiveEmpty()
        {
            Assert.DoesNotThrow(() => _module.ApplyStatus(_target, "Stun", dps: 5f, duration: 0f));
            Assert.AreEqual(0, _module.GetActiveStatuses(_target).Count);
        }

        // ════════════════════════════════════════════════
        // TC-Status-05: Slow ClearAllStatuses 死亡清理
        // ════════════════════════════════════════════════

        [Test]
        public void ClearAllStatuses_EmitsExpiredForEachStatus()
        {
            _module.ApplyStatus(_target, "Slow",  dps: 3f, duration: 5f);
            _module.ApplyStatus(_target, "Burn",  dps: 8f, duration: 3f);

            _expired.Clear(); // 清理 Applied 过程中的干扰（本方法不发 Expired）
            _module.ClearAllStatuses(_target);

            Assert.AreEqual(2, _expired.Count, "2 种 status 各自发 1 次 ExpiredEvent");
            Assert.AreEqual(0, _module.GetActiveStatuses(_target).Count,
                "ClearAllStatuses 后 GetActiveStatuses 应为空");
        }

        [Test]
        public void ClearAllStatuses_NullTarget_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _module.ClearAllStatuses(null));
        }

    }
}
