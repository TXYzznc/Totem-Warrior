using Tattoo.Data;

namespace Tattoo.Strategies.Parts
{
    /// <summary>躯干：受伤触发，缩放 MaxHealth。被动 = 元素抗性 + MaxHealth。</summary>
    public sealed class TorsoPartBehavior : IPartBehavior
    {
        public string PartName => "Torso";

        public void PrepareContext(EffectContext ctx)
        {
            // 躯干特性：把刚刚的攻击者设为主目标——反击
            if (ctx.LastAttacker != null) ctx.PrimaryTarget = ctx.LastAttacker;
        }

        public bool InterceptApply(EffectContext ctx, IShapeBehavior shape, IElementBehavior element, float magnitude) => false;

        public void AffectSelf(PlayerState self, EffectContext ctx) { }

        public void ContributePassive(PassiveStats p, ElementType elem, float strength, string colorName, string patternName)
        {
            p.AddResist(elem, strength * 0.025f);
            p.MaxHealthBonus += strength * 0.5f;
            p.Add($"Torso × {colorName} × {patternName} : {elem} 抗+{strength * 2.5f:F1}% / 最大生命+{strength * 0.5f:F1}");
        }
    }
}
