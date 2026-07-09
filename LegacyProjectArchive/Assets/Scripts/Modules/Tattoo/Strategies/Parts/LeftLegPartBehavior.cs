using Tattoo.Data;
using Tattoo.Events;

namespace Tattoo.Strategies.Parts
{
    /// <summary>
    /// 左腿：闪避触发，缩放 DodgeFrames。
    /// 关键：左腿拦截即时命中，打包为 PendingTrigger 到玩家自身（在下次普攻时消耗）。
    /// </summary>
    public sealed class LeftLegPartBehavior : IPartBehavior
    {
        public string PartName => "LeftLeg";

        public void PrepareContext(EffectContext ctx) { }

        public bool InterceptApply(EffectContext ctx, IShapeBehavior shape, IElementBehavior element, float magnitude)
        {
            if (ctx.Self == null) return false;
            ctx.Self.PendingTriggers.Add(new PendingTrigger
            {
                ConsumeOnEventType = typeof(AttackHitEvent),
                Shape          = shape,
                Element        = element,
                Magnitude      = magnitude,
                Source         = PartName,
                ExpiresAfter   = 1,
            });
            ctx.Log.Add(new EffectResult
            {
                Part = PartName,
                Element = element?.ElementName ?? "?",
                Shape = shape?.ShapeName ?? "?",
                Damage = 0, HitCount = 0, Status = "已打包延迟",
                Note = "Intercepted→PendingTrigger(下次普攻消耗)",
            });
            return true;
        }

        public void AffectSelf(PlayerState self, EffectContext ctx)
        {
            self.ShortInvincible = true;
            self.Buffs.Add("短暂无敌");
        }

        public void ContributePassive(PassiveStats p, ElementType elem, float strength, string colorName, string patternName)
        {
            p.DodgeFramesBonus += strength * 0.02f;
            p.Add($"LeftLeg × {colorName} × {patternName} : 闪避帧+{strength * 0.02f:F2}s / 闪避附{elem}印记");
        }
    }
}
