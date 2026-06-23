using System.Collections.Generic;
using System.Linq;

namespace Tattoo
{
    public class TattooComposer
    {
        public List<TattooSlot> Equipped { get; } = new();
        public PlayerStats     Stats   = new();
        public PlayerSelf      Player  = new();
        public SynergyPipeline Synergy = new();

        public void Equip(TattooSlot slot)
        {
            Equipped.Add(slot);
            RecomputePassive();
        }
        public void Unequip(TattooSlot slot)
        {
            Equipped.Remove(slot);
            RecomputePassive();
        }

        /// <summary>装备/卸下时重算被动 stats（Phase 1：静态被动）。</summary>
        public void RecomputePassive()
        {
            Player.Passive = new PassiveStats();
            foreach (var slot in Equipped)
                slot.Part.ContributePassive(Player.Passive, slot.Color, slot.Pattern);
        }

        public void Fire(GameEvent ev, EffectContext ctx)
        {
            ctx.Event = ev;
            ctx.Stats = Stats;
            ctx.Self  = Player;

            // 1. 触发本轮匹配的槽位
            var snapshot = Equipped.ToArray();
            foreach (var slot in snapshot)
            {
                if (slot.Part.TriggerEvent != ev) continue;

                slot.Part.PrepareContext(ctx);

                float scale      = Stats.Get(slot.Part.ScaleStat) * slot.Part.ScaleFactor;
                float elementMul = slot.Color.ColorMultiplier * slot.Color.Element.BaseMultiplier;
                float patternMul = slot.Pattern.PatternMultiplier;
                float synergyMul = Synergy.Compute(Equipped, slot);
                float magnitude  = scale * elementMul * patternMul * synergyMul;
                magnitude = slot.Color.Element.ModifyMagnitude(ctx, magnitude);

                bool intercepted = slot.Part.InterceptApply(ctx, slot.Pattern.Shape, slot.Color.Element, magnitude);
                if (!intercepted)
                {
                    slot.Pattern.Shape.Apply(ctx, slot.Color.Element, magnitude, slot.Part.PartName, synergyMul);
                    slot.Color.Element.AffectSelf(Player, ctx, magnitude);
                    // 命中后元素侧的"质变"扩展（白色×闪电=额外普攻 等）
                    slot.Color.Element.OnHitExtra(ctx, slot.Pattern.Shape, ctx.PrimaryTarget, magnitude);
                }
                slot.Part.AffectSelf(Player, ctx);
            }

            // 2. 消耗 PendingTriggers（如左腿打包的"下次普攻触发"）
            ConsumePendingTriggers(ev, ctx);
        }

        void ConsumePendingTriggers(GameEvent ev, EffectContext ctx)
        {
            if (Player.PendingTriggers.Count == 0) return;
            var consumed = new List<PendingTrigger>();
            // 复制一份遍历，避免触发时的递归影响
            var snap = Player.PendingTriggers.ToArray();
            foreach (var pt in snap)
            {
                if (pt.ConsumeOnEvent != ev) continue;
                if (pt.Shape == null) continue;
                pt.Shape.Apply(ctx, pt.Element, pt.Magnitude, $"[来自{pt.Source}]延迟", 1f);
                pt.Element.AffectSelf(Player, ctx, pt.Magnitude);
                ctx.Log.Add(new EffectResult
                {
                    Part = pt.Source, Element = pt.Element.ElementName,
                    Shape = pt.Shape.GetType().Name,
                    Damage = pt.Magnitude, HitCount = 1, Status = "ConsumedPending",
                    Note = "ConsumePending@" + ev,
                });
                if (pt.ExpiresAfter > 0) pt.ExpiresAfter--;
                if (pt.ExpiresAfter == 0) consumed.Add(pt);
            }
            foreach (var pt in consumed) Player.PendingTriggers.Remove(pt);
        }
    }

    public class SynergyPipeline
    {
        public float Resonance3    = 1.20f;
        public float Resonance5    = 1.35f;
        public float PatternEcho3  = 1.15f;
        public float SymmetryBonus = 1.25f;

        /// <summary>颜色反应：装备里同时存在两种元素 → 这两种元素的槽位伤害额外加成。</summary>
        public readonly (ElementType A, ElementType B, float Bonus, string Name)[] Reactions =
        {
            (ElementType.Fire,     ElementType.Lightning, 1.30f, "爆裂"),
            (ElementType.Frost,    ElementType.Nature,    1.25f, "寒毒"),
            (ElementType.Mutation, ElementType.Holy,      1.40f, "混乱奇迹"),
        };

        public float Compute(List<TattooSlot> all, TattooSlot self)
        {
            float mul = 1f;

            int sameColor = all.Count(s => s.Color == self.Color);
            if      (sameColor >= 5) mul *= Resonance5;
            else if (sameColor >= 3) mul *= Resonance3;

            int samePattern = all.Count(s => s.Pattern == self.Pattern);
            if (samePattern >= 3) mul *= PatternEcho3;

            if (self.Part.SymmetryGroup != SymmetryGroup.None)
            {
                bool hasPair = all.Any(s =>
                    s != self &&
                    s.Part.SymmetryGroup == self.Part.SymmetryGroup &&
                    s.Color   == self.Color &&
                    s.Pattern == self.Pattern);
                if (hasPair) mul *= SymmetryBonus;
            }

            // 颜色反应：装备里两种特定元素都存在，且 self 槽位属于这两种之一
            var elems = new HashSet<ElementType>();
            foreach (var s in all) if (s.Color?.Element != null) elems.Add(s.Color.Element.Element);
            var selfElem = self.Color?.Element?.Element ?? ElementType.Pure;
            foreach (var r in Reactions)
            {
                if (elems.Contains(r.A) && elems.Contains(r.B) && (selfElem == r.A || selfElem == r.B))
                {
                    mul *= r.Bonus;
                    break;
                }
            }

            return mul;
        }
    }
}
