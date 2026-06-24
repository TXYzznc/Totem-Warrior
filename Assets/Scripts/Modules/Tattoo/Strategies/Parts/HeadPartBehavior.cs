using Tattoo.Data;

namespace Tattoo.Strategies.Parts
{
    /// <summary>脑袋：暴击触发，缩放 CritMultiplier。被动 = 暴击率 + 元素暴伤。</summary>
    public sealed class HeadPartBehavior : IPartBehavior
    {
        public string PartName => "Head";

        public void PrepareContext(EffectContext ctx) { /* 目标已经是被暴击的敌人 */ }

        public bool InterceptApply(EffectContext ctx, IShapeBehavior shape, IElementBehavior element, float magnitude) => false;

        public void AffectSelf(PlayerState self, EffectContext ctx) { }

        public void ContributePassive(PassiveStats p, ElementType elem, float strength, string colorName, string patternName)
        {
            p.CritRateBonus += strength * 0.005f;
            p.AddElemBonus(elem, strength * 0.01f);
            p.Add($"Head × {colorName} × {patternName} : 暴击率+{strength * 0.5f:F1}% / {elem} 暴伤+{strength:F1}%");
        }
    }
}
