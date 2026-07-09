using Tattoo.Data;

namespace Tattoo.Strategies.Parts
{
    /// <summary>左臂：技能触发，缩放 SkillPower。被动 = SkillPowerBonus + 元素技能加成。</summary>
    public sealed class LeftArmPartBehavior : IPartBehavior
    {
        public string PartName => "LeftArm";

        public void PrepareContext(EffectContext ctx) { }
        public bool InterceptApply(EffectContext ctx, IShapeBehavior shape, IElementBehavior element, float magnitude) => false;
        public void AffectSelf(PlayerState self, EffectContext ctx) { }

        public void ContributePassive(PassiveStats p, ElementType elem, float strength, string colorName, string patternName)
        {
            p.SkillPowerBonus += strength * 0.5f;
            p.AddElemBonus(elem, strength * 0.005f);
            p.Add($"LeftArm × {colorName} × {patternName} : 技能强度+{strength * 0.5f:F1} / {elem} 技能加成+{strength * 0.5f:F1}%");
        }
    }
}
