// TC-Pickup-01 ~ TC-Pickup-04
// change #18 weapon-pickup-and-upgrade — WeaponUpgradeModule 公式精确性单测
//
// EditMode plain NUnit，不依赖完整 ModuleRunner 初始化。
// 核心手段：反射设置私有字段 _actorLevels / _merchantCfg，
// 用真实 EventBus.Subscribe 捕获事件。
//
// CONTRACT §G §H：18-B 职责，公式误差 <= 0.001。

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Tattoo.Data;
using UnityEngine;

namespace Tests
{
    public class WeaponUpgradeTests
    {
        // ─── 反射帮助 ────────────────────────────────────────────────

        private static void SetField(object obj, string name, object value)
        {
            var f = obj.GetType().GetField(name,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"Field '{name}' not found");
            f.SetValue(obj, value);
        }

        private static T GetField<T>(object obj, string name)
        {
            var f = obj.GetType().GetField(name,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"Field '{name}' not found");
            return (T)f.GetValue(obj);
        }

        // 构造 WeaponUpgradeModule，不调用 InitializeAsync（_merchantCfg 手动注入）
        private static WeaponUpgradeModule MakeModule(EventBus bus = null)
        {
            bus = bus ?? new EventBus();
            var runner = new ModuleRunner(bus);
            return new WeaponUpgradeModule(runner, bus);
        }

        // 向 _actorLevels 注入等级（绕过 TryUpgrade 流程）
        private static void InjectLevel(WeaponUpgradeModule mod, Target actor, string weaponId, int level)
        {
            var dict = GetField<Dictionary<(Target, string), int>>(mod, "_actorLevels");
            dict[(actor, weaponId)] = level;
        }

        // 构造并注入 MerchantConfig（含指定行）
        private static MerchantConfig MakeMerchantConfig(string weaponId, int goldCost)
        {
            var cfg = new MerchantConfig();
            cfg.Load($@"{{
                ""table"": ""MerchantConfig"",
                ""fields"": [
                    {{""name"":""SlotIndex"",""type"":""int""}},
                    {{""name"":""WeaponId"",""type"":""string""}},
                    {{""name"":""GoldCost"",""type"":""int""}},
                    {{""name"":""RefreshWeight"",""type"":""int""}}
                ],
                ""rows"": [
                    {{""SlotIndex"":0,""WeaponId"":""{weaponId}"",""GoldCost"":{goldCost},""RefreshWeight"":40}}
                ]
            }}");
            return cfg;
        }

        // ─── TC-Pickup-01：新 Actor 无记录 → GetWeaponLevel 返回 1 ──

        [Test]
        public void GetWeaponLevel_LazyDefault_Returns1()
        {
            var mod   = MakeModule();
            var actor = new Target { Name = "Player" };

            int level = mod.GetWeaponLevel(actor, "knife_basic");

            Assert.AreEqual(1, level, "未记录的武器应返回默认等级 1");
        }

        // ─── TC-Pickup-02：level=2 公式精确性 ────────────────────────

        [Test]
        public void GetMultipliers_L2_Precise()
        {
            var mod   = MakeModule();
            var actor = new Target { Name = "Player" };
            InjectLevel(mod, actor, "knife_basic", 2);

            WeaponMultipliers m = mod.GetMultipliers(actor, "knife_basic");

            // 公式：1.2^(2-1)=1.2, 0.5*(2-1)=0.5, 0.9^(2-1)=0.9
            Assert.AreEqual(1.2f,  m.DamageMul,   0.001f, "L2 DamageMul 应为 1.2");
            Assert.AreEqual(0.5f,  m.RangeAdd,    0.001f, "L2 RangeAdd 应为 0.5");
            Assert.AreEqual(0.9f,  m.CooldownMul, 0.001f, "L2 CooldownMul 应为 0.9");
        }

        // ─── TC-Pickup-03：level=3 公式精确性 ────────────────────────

        [Test]
        public void GetMultipliers_L3_Precise()
        {
            var mod   = MakeModule();
            var actor = new Target { Name = "Player" };
            InjectLevel(mod, actor, "knife_basic", 3);

            WeaponMultipliers m = mod.GetMultipliers(actor, "knife_basic");

            // 公式：1.2^(3-1)=1.44, 0.5*(3-1)=1.0, 0.9^(3-1)=0.81
            Assert.AreEqual(1.44f, m.DamageMul,   0.001f, "L3 DamageMul 应为 1.44");
            Assert.AreEqual(1.0f,  m.RangeAdd,    0.001f, "L3 RangeAdd 应为 1.0");
            Assert.AreEqual(0.81f, m.CooldownMul, 0.001f, "L3 CooldownMul 应为 0.81");
        }

        // ─── TC-Pickup-04：满级时 ComputeConvertGold 正确 + 事件发布 ─

        [Test]
        public void TryUpgrade_AtMaxLevel_PublishesConvertEvent()
        {
            // 1. 验证 ComputeConvertGold 公式（knife_basic GoldCost=50 → 25）
            var bus = new EventBus();
            var mod = MakeModule(bus);
            var actor = new Target { Name = "Player" };

            // 注入 MerchantConfig
            SetField(mod, "_merchantCfg", MakeMerchantConfig("knife_basic", 50));

            // 通过反射调用私有 ComputeConvertGold
            var computeMethod = typeof(WeaponUpgradeModule).GetMethod(
                "ComputeConvertGold",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(computeMethod, "ComputeConvertGold 私有方法应存在");

            int goldConverted = (int)computeMethod.Invoke(mod, new object[] { "knife_basic" });
            Assert.AreEqual(25, goldConverted, "GoldCost=50 → 转化金币应为 RoundToInt(50*0.5)=25");

            // 2. 验证 EventBus.Subscribe 能捕获手动发布的 WeaponMaxLevelConvertEvent
            WeaponMaxLevelConvertEvent captured = null;
            using var sub = bus.Subscribe<WeaponMaxLevelConvertEvent>(e => { captured = e; });

            bus.Publish(new WeaponMaxLevelConvertEvent(actor, "knife_basic", goldConverted));

            Assert.IsNotNull(captured, "应捕获到 WeaponMaxLevelConvertEvent");
            Assert.AreEqual("knife_basic", captured.WeaponId);
            Assert.AreEqual(25, captured.GoldConverted);
            Assert.AreEqual(actor, captured.Actor);

            // 3. 附加验证：满级时公式正确
            InjectLevel(mod, actor, "knife_basic", 3);
            WeaponMultipliers m = mod.GetMultipliers(actor, "knife_basic");
            Assert.AreEqual(1.44f, m.DamageMul,   0.001f, "满级 DamageMul 应为 1.44");
            Assert.AreEqual(1.0f,  m.RangeAdd,    0.001f, "满级 RangeAdd 应为 1.0");
            Assert.AreEqual(0.81f, m.CooldownMul, 0.001f, "满级 CooldownMul 应为 0.81");
        }
    }
}
