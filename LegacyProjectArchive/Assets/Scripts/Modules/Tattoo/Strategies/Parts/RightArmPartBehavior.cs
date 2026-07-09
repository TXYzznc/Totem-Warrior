using Tattoo.Data;

namespace Tattoo.Strategies.Parts
{
    /// <summary>右臂：普攻触发，缩放 WeaponDamage。被动 = WeaponDmgBonus + 元素普攻附魔。</summary>
    public sealed class RightArmPartBehavior : IPartBehavior
    {
        public string PartName => "RightArm";

        public void PrepareContext(EffectContext ctx) { }
        public bool InterceptApply(EffectContext ctx, IShapeBehavior shape, IElementBehavior element, float magnitude) => false;
        public void AffectSelf(PlayerState self, EffectContext ctx) { }

        public void ContributePassive(PassiveStats p, ElementType elem, float strength, string colorName, string patternName)
        {
            p.WeaponDmgBonus += strength * 0.3f;
            p.AddElemBonus(elem, strength * 0.008f);
            p.Add($"RightArm × {colorName} × {patternName} : 武器伤害+{strength * 0.3f:F1} / {elem} 普攻附魔+{strength * 0.8f:F1}%");
        }
    }
}
