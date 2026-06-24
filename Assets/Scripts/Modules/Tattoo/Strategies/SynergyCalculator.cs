using System.Collections.Generic;
using System.Linq;
using Tattoo.Data;

namespace Tattoo.Strategies
{
    /// <summary>
    /// 装备联动计算器（替代原 SynergyPipeline）：
    /// - 同色 3 / 5 共鸣
    /// - 同图案 3 回响
    /// - 对称部位（左右臂 / 左右腿）颜色+图案完全一致额外加成
    /// - 颜色反应：装备里同时存在两种特定元素 → 这两种元素的槽位伤害额外加成
    /// </summary>
    public sealed class SynergyCalculator
    {
        public float Resonance3    = 1.20f;
        public float Resonance5    = 1.35f;
        public float PatternEcho3  = 1.15f;
        public float SymmetryBonus = 1.25f;

        public readonly (ElementType A, ElementType B, float Bonus, string Name)[] Reactions =
        {
            (ElementType.Fire,     ElementType.Lightning, 1.30f, "爆裂"),
            (ElementType.Frost,    ElementType.Nature,    1.25f, "寒毒"),
            (ElementType.Mutation, ElementType.Holy,      1.40f, "混乱奇迹"),
        };

        public float Compute(IReadOnlyList<TattooSlot> all, TattooSlot self)
        {
            float mul = 1f;

            int sameColor = all.Count(s => s.ColorId == self.ColorId);
            if      (sameColor >= 5) mul *= Resonance5;
            else if (sameColor >= 3) mul *= Resonance3;

            int samePattern = all.Count(s => s.PatternId == self.PatternId);
            if (samePattern >= 3) mul *= PatternEcho3;

            if (self.SymmetryGroup != SymmetryGroup.None)
            {
                bool hasPair = all.Any(s =>
                    s != self &&
                    s.SymmetryGroup == self.SymmetryGroup &&
                    s.ColorId == self.ColorId &&
                    s.PatternId == self.PatternId);
                if (hasPair) mul *= SymmetryBonus;
            }

            var elems = new HashSet<ElementType>();
            foreach (var s in all) elems.Add(s.ElementType);

            foreach (var r in Reactions)
            {
                if (elems.Contains(r.A) && elems.Contains(r.B) &&
                    (self.ElementType == r.A || self.ElementType == r.B))
                {
                    mul *= r.Bonus;
                    break;
                }
            }

            return mul;
        }
    }
}
