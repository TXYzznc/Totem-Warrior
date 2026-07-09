using Tattoo.Data;

namespace Tattoo.Strategies.Parts
{
    /// <summary>右腿：移动 tick 触发，缩放 MoveSpeed。路径上的敌人是目标。</summary>
    public sealed class RightLegPartBehavior : IPartBehavior
    {
        public string PartName => "RightLeg";

        public void PrepareContext(EffectContext ctx)
        {
            if (ctx.MovementPath != null && ctx.MovementPath.Count > 0)
            {
                ctx.PrimaryTarget = ctx.MovementPath[0];
                for (int i = 1; i < ctx.MovementPath.Count; i++)
                    ctx.NearbyTargets.Add(ctx.MovementPath[i]);
            }
        }

        public bool InterceptApply(EffectContext ctx, IShapeBehavior shape, IElementBehavior element, float magnitude) => false;
        public void AffectSelf(PlayerState self, EffectContext ctx) { }

        public void ContributePassive(PassiveStats p, ElementType elem, float strength, string colorName, string patternName)
        {
            p.MoveSpeedBonus += strength * 0.05f;
            p.Add($"RightLeg × {colorName} × {patternName} : 移速+{strength * 0.05f:F2} / 路径{elem}印记");
        }
    }
}
