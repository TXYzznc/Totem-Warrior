using NUnit.Framework;
using Tattoo.Data;
using Tattoo.Strategies.Elements;
using Tattoo.Strategies.Parts;
using Tattoo.Strategies.Shapes;

namespace Tattoo.Tests
{
    /// <summary>
    /// 策略层纯单元测试。不依赖 ModuleRunner / DataTableModule，
    /// 直接对 PartBehavior / ElementBehavior / ShapeBehavior 测行为契约。
    /// </summary>
    public class TattooStrategyTests
    {
        // ===== ShapeBehavior =====

        [Test]
        public void SingleHit_DealsDamage_AndAddsStatusTag()
        {
            var t = new Target { Name = "敌人", Health = 100f };
            var ctx = new EffectContext { PrimaryTarget = t };
            var element = new FireElementBehavior(baseMultiplier: 1f, burnDPS: 2f, burnDuration: 3f);
            var shape   = new SingleHitShapeBehavior();

            shape.Apply(ctx, element, magnitude: 20f, partName: "RightArm", synergyMul: 1f);

            Assert.AreEqual(80f, t.Health, 0.01f);
            Assert.AreEqual(1, t.Statuses.Count);
            Assert.IsTrue(t.Statuses[0].StartsWith("Burn"));
            Assert.AreEqual(1, ctx.Log.Count);
            Assert.AreEqual(20f, ctx.Log[0].Damage, 0.01f);
        }

        [Test]
        public void AOEBurst_HitsPrimary_AndSpreadsToNearby()
        {
            var primary = new Target { Name = "p", Health = 100f };
            var n1 = new Target { Name = "n1", Health = 100f };
            var n2 = new Target { Name = "n2", Health = 100f };
            var ctx = new EffectContext { PrimaryTarget = primary };
            ctx.NearbyTargets.Add(n1);
            ctx.NearbyTargets.Add(n2);

            var shape = new AOEBurstShapeBehavior(areaFactor: 0.5f, maxTargets: 5);
            shape.Apply(ctx, new LightningElementBehavior(1f, 1f), magnitude: 10f, partName: "RightArm", synergyMul: 1f);

            Assert.AreEqual(95f, primary.Health, 0.01f); // 10 * 0.5 = 5
            Assert.AreEqual(95f, n1.Health, 0.01f);
            Assert.AreEqual(95f, n2.Health, 0.01f);
            Assert.AreEqual(1, ctx.Log.Count);
            Assert.AreEqual(3, ctx.Log[0].HitCount);
        }

        [Test]
        public void StackingMark_BurstsAtThreshold()
        {
            var t = new Target { Name = "敌人", Health = 500f };
            var ctx = new EffectContext { PrimaryTarget = t };
            var element = new FireElementBehavior(1f, 2f, 3f);
            var shape = new StackingMarkShapeBehavior(threshold: 3, burstMul: 5f);

            // 前 2 次只叠层
            shape.Apply(ctx, element, 10f, "Head", 1f);
            shape.Apply(ctx, element, 10f, "Head", 1f);
            Assert.AreEqual(500f, t.Health);

            // 第 3 次爆发
            shape.Apply(ctx, element, 10f, "Head", 1f);
            Assert.AreEqual(450f, t.Health, 0.01f); // 10 * 5 = 50
        }

        // ===== ElementBehavior =====

        [Test]
        public void Pure_ModifyMagnitude_ScalesWithFocusStacks()
        {
            var ctx = new EffectContext { Self = new PlayerState() };
            var pure = new PureElementBehavior(baseMultiplier: 1f, magnitudeBonus: 0.20f, focusStackBonus: 0.10f, maxFocusStacks: 5);

            var first = pure.ModifyMagnitude(ctx, 100f);
            Assert.AreEqual(120f, first, 0.01f); // 1 + 0.2 + 0 = 1.2

            ctx.Self.Stacks["Focus"] = 3;
            var withFocus = pure.ModifyMagnitude(ctx, 100f);
            Assert.AreEqual(150f, withFocus, 0.01f); // 1 + 0.2 + 0.3 = 1.5
        }

        [Test]
        public void Holy_AffectSelf_HealsByDamagePercent()
        {
            var self = new PlayerState { Health = 50f };
            var ctx = new EffectContext { Self = self };
            var holy = new HolyElementBehavior(baseMultiplier: 1f, healPercent: 0.20f);

            holy.AffectSelf(self, ctx, damage: 50f);
            Assert.AreEqual(60f, self.Health, 0.01f); // +10
            Assert.AreEqual(1, self.Buffs.Count);
        }

        // ===== PartBehavior =====

        [Test]
        public void LeftLeg_InterceptApply_PacksAsPendingTrigger()
        {
            var self = new PlayerState();
            var ctx = new EffectContext { Self = self };
            var leftLeg = new LeftLegPartBehavior();
            var fakeShape = new SingleHitShapeBehavior();
            var fakeElement = new FireElementBehavior(1f, 2f, 3f);

            bool intercepted = leftLeg.InterceptApply(ctx, fakeShape, fakeElement, magnitude: 30f);

            Assert.IsTrue(intercepted);
            Assert.AreEqual(1, self.PendingTriggers.Count);
            Assert.AreEqual(30f, self.PendingTriggers[0].Magnitude, 0.01f);
        }

        [Test]
        public void Torso_PrepareContext_SetsLastAttackerAsPrimary()
        {
            var attacker = new Target { Name = "attacker" };
            var ctx = new EffectContext { LastAttacker = attacker };
            var torso = new TorsoPartBehavior();

            torso.PrepareContext(ctx);
            Assert.AreSame(attacker, ctx.PrimaryTarget);
        }

        [Test]
        public void Head_ContributePassive_AddsCritRateAndElementBonus()
        {
            var passive = new PassiveStats();
            new HeadPartBehavior().ContributePassive(passive, ElementType.Fire, strength: 10f, "Red", "Line");

            Assert.AreEqual(0.05f, passive.CritRateBonus, 0.001f); // 10 * 0.005
            Assert.IsTrue(passive.ElementBonus.ContainsKey(ElementType.Fire));
            Assert.AreEqual(0.1f, passive.ElementBonus[ElementType.Fire], 0.001f); // 10 * 0.01
            Assert.AreEqual(1, passive.EntryLog.Count);
        }
    }
}
