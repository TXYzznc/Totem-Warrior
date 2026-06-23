using UnityEngine;

namespace Tattoo
{
    /// <summary>
    /// 部位层 - 把所有"事件路由 / 目标重塑 / 拦截延迟 / 自身效果 / 被动属性"5 个 hook 集中在这一层。
    /// 图案和颜色侧不需要知道部位的存在，所有复杂度都在这里。
    /// </summary>
    public abstract class BodyPart : ScriptableObject
    {
        public string         PartName;
        public GameEvent      TriggerEvent;
        public StatType       ScaleStat;
        public SymmetryGroup  SymmetryGroup = SymmetryGroup.None;
        public float          ScaleFactor   = 1f;

        /// <summary>各部位推荐的 ScaleFactor 默认值，让跨部位单次伤害在 8~15 范围内。</summary>
        public virtual float DefaultScaleFactor => 1f;

        // ---- 5 个 hook ----

        /// <summary>触发前重塑上下文（如躯干把 LastAttacker 设为目标）。</summary>
        public virtual void PrepareContext(EffectContext ctx) { }

        /// <summary>是否拦截本次 shape.Apply？返回 true 则部位自己处理（如左腿打包成 PendingTrigger）。</summary>
        public virtual bool InterceptApply(EffectContext ctx, EffectShape shape, ElementBehavior element, float magnitude)
            => false;

        /// <summary>触发后对玩家自身的效果（左腿瞬间无敌 / 右腿位移加速 等）。</summary>
        public virtual void AffectSelf(PlayerSelf self, EffectContext ctx) { }

        /// <summary>装备此槽位时贡献的被动 stat modifier。由 colorElement + patternStrength 拼出具体内容。</summary>
        public virtual void ContributePassive(PassiveStats passive, ColorSO color, PatternSO pattern)
        {
            // 默认实现：用部位维度 + 颜色元素 + 图案强度生成一条 passive entry
            var elem = color != null && color.Element != null ? color.Element.Element : ElementType.Pure;
            var strength = pattern != null ? pattern.PatternMultiplier * 10f : 10f;
            ApplyDimensionContribution(passive, elem, strength);
            passive.Add($"{PartName} × {color?.ColorName} × {pattern?.PatternName} : {DimensionLabel(elem, strength)}");
        }

        /// <summary>由子类决定"被动维度是什么"。例如躯干=抗性，脑袋=暴击率。</summary>
        protected abstract void ApplyDimensionContribution(PassiveStats passive, ElementType elem, float strength);
        protected abstract string DimensionLabel(ElementType elem, float strength);
    }

    // ---------- 6 个固定子类 ----------

    [CreateAssetMenu(menuName = "Tattoo/Part/Head", fileName = "Part_Head")]
    public class HeadPart : BodyPart
    {
        public override float DefaultScaleFactor => 10f; // CritMul 1.5 × 10 = 15
        // 默认 PrepareContext = 不动；目标已经是被暴击的敌人
        // 默认 InterceptApply = false；默认 AffectSelf = 无
        protected override void ApplyDimensionContribution(PassiveStats p, ElementType e, float s)
        {
            p.CritRateBonus += s * 0.005f; // 每 1 strength → +0.5% 暴击率
            p.AddElemBonus(e, s * 0.01f);   // 元素暴击伤害
        }
        protected override string DimensionLabel(ElementType e, float s)
            => $"暴击率+{s * 0.5f:F1}% / {e} 暴伤+{s:F1}%";
    }

    [CreateAssetMenu(menuName = "Tattoo/Part/Torso", fileName = "Part_Torso")]
    public class TorsoPart : BodyPart
    {
        public override float DefaultScaleFactor => 0.12f; // MaxHp 100 × 0.12 = 12
        public override void PrepareContext(EffectContext ctx)
        {
            if (ctx.LastAttacker != null) ctx.PrimaryTarget = ctx.LastAttacker;
        }
        protected override void ApplyDimensionContribution(PassiveStats p, ElementType e, float s)
        {
            p.AddResist(e, s * 0.025f);       // 元素抗性 +X%
            p.MaxHealthBonus += s * 0.5f;     // 最大生命 +X
        }
        protected override string DimensionLabel(ElementType e, float s)
            => $"{e} 抗+{s * 2.5f:F1}% / 最大生命+{s * 0.5f:F1}";
    }

    [CreateAssetMenu(menuName = "Tattoo/Part/LeftArm", fileName = "Part_LeftArm")]
    public class LeftArmPart : BodyPart
    {
        public override float DefaultScaleFactor => 0.6f; // Skill 20 × 0.6 = 12
        protected override void ApplyDimensionContribution(PassiveStats p, ElementType e, float s)
        {
            p.SkillPowerBonus += s * 0.5f;
            p.AddElemBonus(e, s * 0.005f);
        }
        protected override string DimensionLabel(ElementType e, float s)
            => $"技能强度+{s * 0.5f:F1} / {e} 技能加成+{s * 0.5f:F1}%";
    }

    [CreateAssetMenu(menuName = "Tattoo/Part/RightArm", fileName = "Part_RightArm")]
    public class RightArmPart : BodyPart
    {
        public override float DefaultScaleFactor => 0.8f; // Wpn 10 × 0.8 = 8（高频部位单次轻）
        protected override void ApplyDimensionContribution(PassiveStats p, ElementType e, float s)
        {
            p.WeaponDmgBonus += s * 0.3f;
            p.AddElemBonus(e, s * 0.008f);
        }
        protected override string DimensionLabel(ElementType e, float s)
            => $"武器伤害+{s * 0.3f:F1} / {e} 普攻附魔+{s * 0.8f:F1}%";
    }

    [CreateAssetMenu(menuName = "Tattoo/Part/LeftLeg", fileName = "Part_LeftLeg")]
    public class LeftLegPart : BodyPart
    {
        public override float DefaultScaleFactor => 40f; // DodgeFrames 0.3 × 40 = 12
        /// <summary>关键：左腿拦截即时命中，打包为 PendingTrigger 到玩家自身。</summary>
        public override bool InterceptApply(EffectContext ctx, EffectShape shape, ElementBehavior element, float magnitude)
        {
            ctx.Self?.PendingTriggers.Add(new PendingTrigger
            {
                ConsumeOnEvent = GameEvent.OnAttack,
                Shape          = shape,
                Element        = element,
                Magnitude      = magnitude,
                Source         = PartName,
                ExpiresAfter   = 1,
            });
            ctx.Log.Add(new EffectResult
            {
                Part = PartName, Element = element.ElementName, Shape = shape ? shape.GetType().Name : "?",
                Damage = 0, HitCount = 0, Status = "已打包延迟",
                Note = "Intercepted→PendingTrigger(下次普攻消耗)",
            });
            return true;
        }
        public override void AffectSelf(PlayerSelf self, EffectContext ctx)
        {
            self.ShortInvincible = true; // 闪避瞬间无敌
            self.Buffs.Add("短暂无敌");
        }
        protected override void ApplyDimensionContribution(PassiveStats p, ElementType e, float s)
        {
            p.DodgeFramesBonus += s * 0.02f;
        }
        protected override string DimensionLabel(ElementType e, float s)
            => $"闪避帧+{s * 0.02f:F2}s / 闪避附{e}印记";
    }

    [CreateAssetMenu(menuName = "Tattoo/Part/RightLeg", fileName = "Part_RightLeg")]
    public class RightLegPart : BodyPart
    {
        public override float DefaultScaleFactor => 1.6f; // MoveSpd 5 × 1.6 = 8（高频 tick，单次轻）
        public override void PrepareContext(EffectContext ctx)
        {
            // 移动 tick：把"路径上的敌人"作为 PrimaryTarget + Nearby
            if (ctx.MovementPath != null && ctx.MovementPath.Count > 0)
            {
                ctx.PrimaryTarget = ctx.MovementPath[0];
                for (int i = 1; i < ctx.MovementPath.Count; i++) ctx.NearbyTargets.Add(ctx.MovementPath[i]);
            }
        }
        protected override void ApplyDimensionContribution(PassiveStats p, ElementType e, float s)
        {
            p.MoveSpeedBonus += s * 0.05f;
        }
        protected override string DimensionLabel(ElementType e, float s)
            => $"移速+{s * 0.05f:F2} / 路径{e}印记";
    }
}
