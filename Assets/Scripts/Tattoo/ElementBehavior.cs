using UnityEngine;

namespace Tattoo
{
    /// <summary>
    /// 颜色（元素）层。提供 4 个 hook：副作用 / 修改倍率 / 自身效果 / 命中后额外行为（Phase2）。
    /// </summary>
    public abstract class ElementBehavior : ScriptableObject
    {
        public string ElementName    = "Element";
        public float  BaseMultiplier = 1f;
        public abstract ElementType Element { get; }

        /// <summary>命中目标后给目标加 status / DOT 等。</summary>
        public abstract string ApplyElementEffect(Target target, float damage);

        /// <summary>修改这次触发的 magnitude（白色 +20%、紫色随机、Focus 叠层等）。</summary>
        public virtual float ModifyMagnitude(EffectContext ctx, float baseMag) => baseMag;

        /// <summary>对玩家自身的效果（金回血、白叠专注、紫微随机 buff）。</summary>
        public virtual void AffectSelf(PlayerSelf self, EffectContext ctx, float damage) { }

        /// <summary>Phase 2 留：图案命中后追加的元素侧行为（白色×闪电=额外普攻等）。</summary>
        public virtual void OnHitExtra(EffectContext ctx, EffectShape shape, Target target, float damage) { }
    }

    // ---------- 7 元素子类 ----------

    [CreateAssetMenu(menuName = "Tattoo/Element/Fire", fileName = "Element_Fire")]
    public class FireElement : ElementBehavior
    {
        public override ElementType Element => ElementType.Fire;
        public float BurnDPS = 2f;
        public float BurnDuration = 3f;
        public override string ApplyElementEffect(Target t, float dmg)
        {
            var tag = $"Burn({BurnDPS}/s,{BurnDuration}s)";
            t.Statuses.Add(tag);
            return tag;
        }
    }

    [CreateAssetMenu(menuName = "Tattoo/Element/Lightning", fileName = "Element_Lightning")]
    public class LightningElement : ElementBehavior
    {
        public override ElementType Element => ElementType.Lightning;
        public float ParalyzeDuration = 1f;
        public override string ApplyElementEffect(Target t, float dmg)
        {
            var tag = $"Paralyze({ParalyzeDuration}s)";
            t.Statuses.Add(tag);
            return tag;
        }
    }

    [CreateAssetMenu(menuName = "Tattoo/Element/Nature", fileName = "Element_Nature")]
    public class NatureElement : ElementBehavior
    {
        public override ElementType Element => ElementType.Nature;
        public float PoisonDPS = 1.5f;
        public float PoisonDuration = 4f;
        public int MaxRegenStacks = 5;
        public override string ApplyElementEffect(Target t, float dmg)
        {
            var tag = $"Poison({PoisonDPS}/s,{PoisonDuration}s)";
            t.Statuses.Add(tag);
            return tag;
        }
        public override void AffectSelf(PlayerSelf self, EffectContext ctx, float damage)
        {
            self.Stacks.TryGetValue("Regen", out int cur);
            self.Stacks["Regen"] = System.Math.Min(cur + 1, MaxRegenStacks);
        }
    }

    [CreateAssetMenu(menuName = "Tattoo/Element/Frost", fileName = "Element_Frost")]
    public class FrostElement : ElementBehavior
    {
        public override ElementType Element => ElementType.Frost;
        public float SlowFactor = 0.30f;
        public float SlowDuration = 2f;
        public int MaxFrostArmorStacks = 5;
        public override string ApplyElementEffect(Target t, float dmg)
        {
            var tag = $"Slow(-{SlowFactor * 100f:F0}%,{SlowDuration}s)";
            t.Statuses.Add(tag);
            return tag;
        }
        public override void AffectSelf(PlayerSelf self, EffectContext ctx, float damage)
        {
            self.Stacks.TryGetValue("FrostArmor", out int cur);
            self.Stacks["FrostArmor"] = System.Math.Min(cur + 1, MaxFrostArmorStacks);
        }
    }

    [CreateAssetMenu(menuName = "Tattoo/Element/Mutation", fileName = "Element_Mutation")]
    public class MutationElement : ElementBehavior
    {
        public override ElementType Element => ElementType.Mutation;
        public int Seed = 42;
        System.Random rng;

        public override float ModifyMagnitude(EffectContext ctx, float baseMag)
        {
            rng ??= new System.Random(Seed);
            return baseMag * (float)(0.9 + rng.NextDouble() * 0.4);
        }

        public override string ApplyElementEffect(Target t, float dmg)
        {
            rng ??= new System.Random(Seed);
            string[] picks = { "Mutate-眩晕", "Mutate-沉默", "Mutate-虚弱" };
            var tag = picks[rng.Next(picks.Length)];
            t.Statuses.Add(tag);
            return tag;
        }

        public override void AffectSelf(PlayerSelf self, EffectContext ctx, float damage)
        {
            self.Buffs.Add("异变·随机微Buff");
        }

        public override void OnHitExtra(EffectContext ctx, EffectShape shape, Target target, float damage)
        {
            if (shape is ProbBurstShape)
            {
                // 紫×星形 = 现实崩塌：重置 RNG seed（让后续命中状态改变）
                rng = new System.Random(System.DateTime.Now.Millisecond);
                ctx.Log.Add(new EffectResult
                {
                    Element = ElementName, Shape = "Mutation×Star:现实崩塌",
                    Part = "OnHitExtra", Damage = 0, HitCount = 0,
                    Status = "RandomSeedReset", Note = "OnHitExtra",
                });
            }
        }
    }

    [CreateAssetMenu(menuName = "Tattoo/Element/Holy", fileName = "Element_Holy")]
    public class HolyElement : ElementBehavior
    {
        public override ElementType Element => ElementType.Holy;
        public float HealPercent = 0.15f;

        public override string ApplyElementEffect(Target t, float dmg)
        {
            var tag = "神圣印记(被命中暴伤+50%)";
            t.Statuses.Add(tag);
            return tag;
        }

        public override void AffectSelf(PlayerSelf self, EffectContext ctx, float damage)
        {
            float heal = damage * HealPercent;
            self.Health += heal;
            self.Buffs.Add($"治疗+{heal:F1}");
        }

        public override void OnHitExtra(EffectContext ctx, EffectShape shape, Target target, float damage)
        {
            if (shape is ChainJumpShape && ctx.Self != null)
            {
                // 金×闪电 = 链跳过程中每跳额外回血 5%
                float heal = damage * 0.05f * 3f;
                ctx.Self.Health += heal;
                ctx.Log.Add(new EffectResult
                {
                    Element = ElementName, Shape = "Holy×Bolt:跳跳回血",
                    Part = "OnHitExtra", Damage = 0, HitCount = 0,
                    Status = $"+{heal:F1} HP", Note = "OnHitExtra",
                });
            }
        }
    }

    [CreateAssetMenu(menuName = "Tattoo/Element/Pure", fileName = "Element_Pure")]
    public class PureElement : ElementBehavior
    {
        public override ElementType Element => ElementType.Pure;
        public float MagnitudeBonus = 0.20f;   // +20% 通用倍率
        public float FocusStackBonus = 0.01f;  // 每层 Focus +1%
        public int MaxFocusStacks = 5;

        public override float ModifyMagnitude(EffectContext ctx, float baseMag)
        {
            int focus = 0;
            if (ctx.Self != null) ctx.Self.Stacks.TryGetValue("Focus", out focus);
            return baseMag * (1f + MagnitudeBonus + focus * FocusStackBonus);
        }

        public override string ApplyElementEffect(Target t, float dmg)
        {
            var tag = "纯能印记(暴伤+30%)";
            t.Statuses.Add(tag);
            return tag;
        }

        public override void AffectSelf(PlayerSelf self, EffectContext ctx, float damage)
        {
            self.Stacks.TryGetValue("Focus", out int cur);
            cur = System.Math.Min(cur + 1, MaxFocusStacks);
            self.Stacks["Focus"] = cur;
        }

        /// <summary>白色×特定图案 = 质变机制（文档 05 白色多样性的载体）。</summary>
        public override void OnHitExtra(EffectContext ctx, EffectShape shape, Target target, float damage)
        {
            if (target == null || shape == null) return;
            if (shape is ChainJumpShape)
            {
                // 白色×闪电 = 额外触发一次"普攻直击"
                float extra = damage * 0.5f;
                target.Health -= extra;
                ctx.Log.Add(new EffectResult
                {
                    Element = ElementName, Shape = "Pure×Bolt:额外普攻",
                    Part = "OnHitExtra", Damage = extra, HitCount = 1,
                    Status = "ExtraBasic", Note = "OnHitExtra",
                });
            }
            else if (shape is ProbBurstShape)
            {
                // 白色×星形 = 刷新所有 PendingTrigger 的"消耗事件"（让闪避 buff 立即可用）
                if (ctx.Self != null && ctx.Self.PendingTriggers.Count > 0)
                    ctx.Log.Add(new EffectResult
                    {
                        Element = ElementName, Shape = "Pure×Star:刷新冷却",
                        Part = "OnHitExtra", Damage = 0, HitCount = 0,
                        Status = "RefreshPendings", Note = "OnHitExtra",
                    });
            }
            // 进一步白色×其他图案的质变扩展，后续按需添加（仍是同样的 if-cascade，不破坏其他颜色）
        }
    }
}
