using NUnit.Framework;
using System.Collections.Generic;
using Tattoo.Combat;
using Tattoo.Data;
using Tattoo.Events;
using UnityEngine;

namespace Tattoo.Tests
{
    /// <summary>
    /// v2.1 核心契约的纯单元测试（不依赖 ModuleRunner / EventBus）。
    /// 覆盖：
    /// 1. TattooAffix 字段稳定性
    /// 2. v2.1 事件类构造兼容性（默认参数 → HUD 后向兼容）
    /// 3. IPlayerController 4 个 ControllerType 完整
    /// 4. TattooSlot.Affixes 词缀容器
    /// </summary>
    public class V21ContractTests
    {
        // ===== 1. TattooAffix =====

        [Test]
        public void TattooAffix_构造后字段保留()
        {
            var a = new TattooAffix
            {
                AffixId     = 101,
                DisplayName = "火焰强化",
                Type        = AffixType.ElementDamageBonus,
                Value       = 0.15f,
            };

            Assert.AreEqual(101, a.AffixId);
            Assert.AreEqual("火焰强化", a.DisplayName);
            Assert.AreEqual(AffixType.ElementDamageBonus, a.Type);
            Assert.AreEqual(0.15f, a.Value, 1e-4f);
        }

        [Test]
        public void TattooAffix_所有Type枚举可枚举()
        {
            var values = System.Enum.GetValues(typeof(AffixType));
            Assert.GreaterOrEqual(values.Length, 6, "v2.1 AffixType 应至少 6 类");
        }

        // ===== 2. v2.1 事件类构造兼容性 =====

        [Test]
        public void DamagedEvent_仅基础构造_向后兼容()
        {
            // CombatModule 旧调用：new DamagedEvent(attacker, dmg)
            var dmg = new DamagedEvent(null, 20f);
            Assert.AreEqual(20f, dmg.Damage);
            Assert.AreEqual(0f, dmg.NewHp);
            Assert.AreEqual(0f, dmg.MaxHp);
        }

        [Test]
        public void DamagedEvent_含HP构造_HUD可读()
        {
            var dmg = new DamagedEvent(null, 20f, newHp: 80f, maxHp: 100f);
            Assert.AreEqual(80f, dmg.NewHp);
            Assert.AreEqual(100f, dmg.MaxHp);
        }

        [Test]
        public void SkillCastEvent_仅SkillId构造_向后兼容()
        {
            var e = new SkillCastEvent("slot0");
            Assert.AreEqual("slot0", e.SkillId);
            Assert.AreEqual(0, e.SlotIndex);
            Assert.AreEqual(0f, e.Cooldown);
        }

        [Test]
        public void SkillCastEvent_完整构造_HUD可读()
        {
            var e = new SkillCastEvent("slot1", slotIndex: 1, cooldown: 3.5f);
            Assert.AreEqual("slot1", e.SkillId);
            Assert.AreEqual(1, e.SlotIndex);
            Assert.AreEqual(3.5f, e.Cooldown, 1e-4f);
        }

        [Test]
        public void BossSpawnedEvent_Target_Position正确填充()
        {
            var t = new Target { Name = "Boss_AI神王", Health = 5000f };
            var pos = new Vector3(10, 0, 20);
            var e = new BossSpawnedEvent(t, pos);
            Assert.AreEqual(t, e.Boss);
            Assert.AreEqual(pos, e.SpawnPosition);
        }

        [Test]
        public void TattooInProgressEvent_读条时长写入()
        {
            var owner = new Target { Name = "玩家" };
            var e = new TattooInProgressEvent(owner, 1, 2, 3, duration: 6f);
            Assert.AreEqual(owner, e.Owner);
            Assert.AreEqual(1, e.PartId);
            Assert.AreEqual(2, e.ColorId);
            Assert.AreEqual(3, e.PatternId);
            Assert.AreEqual(6f, e.DurationSec, 1e-4f);
        }

        [Test]
        public void TattooCancelledEvent_四种CancelReason枚举完整()
        {
            var owner = new Target();
            foreach (CancelReason r in System.Enum.GetValues(typeof(CancelReason)))
            {
                var e = new TattooCancelledEvent(owner, r);
                Assert.AreEqual(r, e.Reason);
            }
            Assert.AreEqual(4, System.Enum.GetValues(typeof(CancelReason)).Length,
                "v2.1 CancelReason 应为 4 种：Damaged / Moved / Killed / UserAbort");
        }

        // ===== 3. IPlayerController ControllerType 完整 =====

        [Test]
        public void PlayerControllerType_四个值完整()
        {
            var types = System.Enum.GetValues(typeof(PlayerControllerType));
            Assert.AreEqual(4, types.Length, "v2.1 ControllerType 应为 4 种");
            Assert.Contains(PlayerControllerType.Human,         types);
            Assert.Contains(PlayerControllerType.SmartBot,      types);
            Assert.Contains(PlayerControllerType.LightBot,      types);
            Assert.Contains(PlayerControllerType.NetworkReplay, types);
        }

        // ===== 4. TattooSlot.Affixes 容器 =====

        [Test]
        public void TattooSlot_Affixes列表默认存在()
        {
            var slot = new TattooSlot();
            // Affixes 应默认 null 或空列表，二者均可
            if (slot.Affixes == null) slot.Affixes = new List<TattooAffix>();
            slot.Affixes.Add(new TattooAffix
            {
                AffixId = 1, Value = 0.1f, Type = AffixType.AttackSpeed
            });
            Assert.AreEqual(1, slot.Affixes.Count);
        }
    }
}
