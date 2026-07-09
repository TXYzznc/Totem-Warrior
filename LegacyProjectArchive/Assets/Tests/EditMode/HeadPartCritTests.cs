using System.Collections.Generic;
using NUnit.Framework;
using Tattoo;
using Tattoo.Data;
using Tattoo.Events;
using Tattoo.Strategies.Parts;
using Weapon.Events;

namespace Combat.Tests
{
    /// <summary>
    /// TC-Crit-01/02 + TC-Damage-D4/D9
    ///
    /// 测试 HeadPartBehavior：
    ///   - 无 Head 槽时不发 CritHitEvent
    ///   - PatternMultiplier × (1 + CritRateBonus) 概率逻辑
    ///   - PatternMultiplier=1.0 必暴击（固定种子 Random.value < 1.0）
    ///   - ContributePassive 累加正确性
    ///
    /// 约束：
    ///   - HeadPartBehavior(bus=null, tattoo=null) 合法：跳过事件订阅（单测用）
    ///   - 有 bus 时订阅 WeaponAttackHitEvent；TattooModule 通过 MockTattooModule 替代
    ///   - 使用 Random.InitState 固定种子，保证暴击输出可预测
    /// </summary>
    public class HeadPartCritTests
    {
        EventBus              _bus;
        List<CritHitEvent>    _critEvents = new();

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _critEvents.Clear();
            _bus.Subscribe<CritHitEvent>(e => _critEvents.Add(e));
        }

        // ════════════════════════════════════════════════
        // TC-Crit-01: 无 Head 槽 → 暴击=0
        // ════════════════════════════════════════════════

        [Test]
        public void NoHeadSlot_DoesNotEmitCritHitEvent()
        {
            var mockTattoo = BuildStubTattoo(new List<TattooSlot>());
            var headPart   = new HeadPartBehavior(_bus, mockTattoo);

            var target = new Target { Name = "Enemy", Health = 100f };
            _bus.Publish(new WeaponAttackHitEvent(null, target, 20f, "knife"));

            Assert.AreEqual(0, _critEvents.Count, "无 Head 槽时不应发 CritHitEvent");
            headPart.Dispose();
        }

        [Test]
        public void HasOnlyRightArmSlot_NoHeadSlot_DoesNotEmitCrit()
        {
            var slots = new List<TattooSlot>
            {
                new TattooSlot { PartName = "RightArm", PatternMultiplier = 1f }
            };
            var mockTattoo = BuildStubTattoo(slots);
            var headPart   = new HeadPartBehavior(_bus, mockTattoo);

            var target = new Target { Name = "Enemy", Health = 100f };
            _bus.Publish(new WeaponAttackHitEvent(null, target, 20f, "hammer"));

            Assert.AreEqual(0, _critEvents.Count, "RightArm 槽不应触发 Head 暴击");
            headPart.Dispose();
        }

        // ════════════════════════════════════════════════
        // TC-Damage-D9 / TC-Crit-02: PatternMultiplier=1.0 必暴击
        // ════════════════════════════════════════════════

        [Test]
        public void HeadSlotPatternMultiplier1_AlwaysCrit()
        {
            var slots = new List<TattooSlot>
            {
                new TattooSlot { PartName = "Head", PatternMultiplier = 1.0f }
            };
            var mockTattoo = BuildStubTattoo(slots, critRateBonus: 0f, critMultiplier: 1.5f);
            var headPart   = new HeadPartBehavior(_bus, mockTattoo);

            var target = new Target { Name = "Enemy", Health = 100f };
            // 固定任意种子，Random.value∈[0,1) 必 < 1.0
            UnityEngine.Random.InitState(42);
            _bus.Publish(new WeaponAttackHitEvent(null, target, 20f, "knife"));

            Assert.AreEqual(1, _critEvents.Count, "PatternMultiplier=1 时必暴击");
            Assert.AreEqual(30f, _critEvents[0].BaseDamage, 0.001f,
                "暴击伤害 = 20 × 1.5 = 30");
            headPart.Dispose();
        }

        [Test]
        public void HeadSlotPatternMultiplier0_NeverCrit()
        {
            var slots = new List<TattooSlot>
            {
                new TattooSlot { PartName = "Head", PatternMultiplier = 0f }
            };
            var mockTattoo = BuildStubTattoo(slots, critRateBonus: 0f, critMultiplier: 1.5f);
            var headPart   = new HeadPartBehavior(_bus, mockTattoo);

            var target = new Target { Name = "Enemy", Health = 100f };
            _bus.Publish(new WeaponAttackHitEvent(null, target, 20f, "pistol"));

            Assert.AreEqual(0, _critEvents.Count, "PatternMultiplier=0 时不应暴击");
            headPart.Dispose();
        }

        [Test]
        public void CritDamage_UsesDefaultMultiplier_WhenConfigZero()
        {
            // Stats.CritMultiplier=0 时 HeadPartBehavior 回退到默认 1.5
            var slots = new List<TattooSlot>
            {
                new TattooSlot { PartName = "Head", PatternMultiplier = 1.0f }
            };
            var mockTattoo = BuildStubTattoo(slots, critRateBonus: 0f, critMultiplier: 0f);
            var headPart   = new HeadPartBehavior(_bus, mockTattoo);

            UnityEngine.Random.InitState(0);
            var target = new Target { Name = "Enemy", Health = 100f };
            _bus.Publish(new WeaponAttackHitEvent(null, target, 10f, "bow"));

            Assert.AreEqual(1, _critEvents.Count, "应触发暴击");
            Assert.AreEqual(15f, _critEvents[0].BaseDamage, 0.001f,
                "CritMultiplier=0 时应使用默认 1.5 → 10×1.5=15");
            headPart.Dispose();
        }

        [Test]
        public void CritProbability_WithCritRateBonus_MultipliesCorrectly()
        {
            // critProb = 0.5 × (1 + 1.0) = 1.0 → 必暴击
            var slots = new List<TattooSlot>
            {
                new TattooSlot { PartName = "Head", PatternMultiplier = 0.5f }
            };
            var mockTattoo = BuildStubTattoo(slots, critRateBonus: 1.0f, critMultiplier: 2.0f);
            var headPart   = new HeadPartBehavior(_bus, mockTattoo);

            UnityEngine.Random.InitState(0);
            var target = new Target { Name = "Enemy", Health = 100f };
            _bus.Publish(new WeaponAttackHitEvent(null, target, 10f, "energy_fist"));

            Assert.AreEqual(1, _critEvents.Count, "critProb=1.0（0.5×2）时必暴击");
            Assert.AreEqual(20f, _critEvents[0].BaseDamage, 0.001f, "10 × 2.0 = 20");
            headPart.Dispose();
        }

        // ════════════════════════════════════════════════
        // TC-Damage-D4: ContributePassive 累加
        // ════════════════════════════════════════════════

        [Test]
        public void ContributePassive_Strength10_CritRateBonusCorrect()
        {
            var headPart = new HeadPartBehavior(); // null bus/tattoo，仅测 ContributePassive
            var passive  = new PassiveStats();

            headPart.ContributePassive(passive, ElementType.Fire, 10f, "Red", "Circle");

            Assert.AreEqual(0.05f, passive.CritRateBonus, 0.0001f,
                "strength=10 → CritRateBonus = 10×0.005 = 0.05");
        }

        [Test]
        public void ContributePassive_Strength10_ElemBonusCorrect()
        {
            var headPart = new HeadPartBehavior();
            var passive  = new PassiveStats();

            headPart.ContributePassive(passive, ElementType.Fire, 10f, "Red", "Circle");

            passive.ElementBonus.TryGetValue(ElementType.Fire, out float bonus);
            Assert.AreEqual(0.1f, bonus, 0.0001f,
                "strength=10 → FireBonus = 10×0.01 = 0.1");
        }

        [Test]
        public void ContributePassive_Strength0_NoContribution()
        {
            var headPart = new HeadPartBehavior();
            var passive  = new PassiveStats();

            headPart.ContributePassive(passive, ElementType.Fire, 0f, "Red", "Circle");

            Assert.AreEqual(0f, passive.CritRateBonus, 0.0001f, "strength=0 时 CritRateBonus=0");
            Assert.IsFalse(passive.ElementBonus.ContainsKey(ElementType.Fire),
                "strength=0 时 ElementBonus 无 Fire 条目");
        }

        [Test]
        public void NullBusNullTattoo_DoesNotThrow_OnConstruction()
        {
            Assert.DoesNotThrow(() =>
            {
                var h = new HeadPartBehavior(null, null);
                h.Dispose();
            }, "bus=null, tattoo=null 时构造不抛异常");
        }

        // ════════════════════════════════════════════════
        // StubTattooModule：TattooModule 是 sealed，用反射注入方式构建桩
        // ════════════════════════════════════════════════

        /// <summary>
        /// TattooModule 是 sealed class，无法继承。
        /// 通过反射向真实实例的私有字段注入测试数据：
        ///   - _equipped    List&lt;TattooSlot&gt;
        ///   - Player.Passive.CritRateBonus（直接修改 Player 实例）
        ///   - Stats.CritMultiplier（直接修改 Stats 实例）
        /// </summary>
        static TattooModule BuildStubTattoo(List<TattooSlot> equipped,
                                            float critRateBonus = 0f, float critMultiplier = 1.5f)
        {
            var innerBus    = new EventBus();
            var innerRunner = new ModuleRunner(innerBus);
            var tattoo      = new TattooModule(innerRunner, innerBus);

            // 注入 _equipped
            var equippedField = typeof(TattooModule).GetField("_equipped",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (equippedField?.GetValue(tattoo) is List<TattooSlot> list)
            {
                list.Clear();
                list.AddRange(equipped);
            }

            // 修改 Player.Passive.CritRateBonus（Player 属性返回的是同一个引用对象）
            tattoo.Player.Passive.CritRateBonus = critRateBonus;

            // 修改 Stats.CritMultiplier（Stats 属性返回的是同一个引用对象）
            tattoo.Stats.CritMultiplier = critMultiplier;

            return tattoo;
        }

        class MockPlayerState
        {
            public readonly float CritMultiplier;
            public readonly float CritRateBonus;

            public MockPlayerState(float critRateBonus, float critMultiplier)
            {
                CritRateBonus  = critRateBonus;
                CritMultiplier = critMultiplier;
            }
        }
    }
}
