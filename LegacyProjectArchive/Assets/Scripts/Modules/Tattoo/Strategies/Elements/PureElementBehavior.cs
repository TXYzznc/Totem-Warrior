using System;
using Tattoo.Data;

namespace Tattoo.Strategies.Elements
{
    public sealed class PureElementBehavior : IElementBehavior
    {
        public ElementType Element => ElementType.Pure;
        public string ElementName => "Pure";
        public float BaseMultiplier { get; }

        readonly float _magnitudeBonus;
        readonly float _focusStackBonus;
        readonly int   _maxFocusStacks;

        public PureElementBehavior(float baseMultiplier, float magnitudeBonus = 0.20f,
                                    float focusStackBonus = 0.01f, int maxFocusStacks = 5)
        {
            BaseMultiplier = baseMultiplier;
            _magnitudeBonus = magnitudeBonus;
            _focusStackBonus = focusStackBonus;
            _maxFocusStacks = maxFocusStacks;
        }

        public float ModifyMagnitude(EffectContext ctx, float baseMag)
        {
            int focus = 0;
            if (ctx.Self != null) ctx.Self.Stacks.TryGetValue("Focus", out focus);
            return baseMag * (1f + _magnitudeBonus + focus * _focusStackBonus);
        }

        public string ApplyElementEffect(Target target, float damage)
        {
            var tag = "纯能印记(暴伤+30%)";
            target.Statuses.Add(tag);
            return tag;
        }

        public void AffectSelf(PlayerState self, EffectContext ctx, float damage)
        {
            self.Stacks.TryGetValue("Focus", out int cur);
            self.Stacks["Focus"] = Math.Min(cur + 1, _maxFocusStacks);
        }

        public void OnHitExtra(EffectContext ctx, IShapeBehavior shape, Target target, float damage)
        {
            if (target == null || shape == null) return;

            // 白色×闪电 = 额外触发"普攻直击"
            if (shape.ShapeName == "ChainJump")
            {
                float extra = damage * 0.5f;
                target.Health -= extra;
                ctx.Log.Add(new EffectResult
                {
                    Element = ElementName,
                    Shape = "Pure×Bolt:额外普攻",
                    Part = "OnHitExtra",
                    Damage = extra, HitCount = 1,
                    Status = "ExtraBasic",
                    Note = "OnHitExtra",
                });
            }
            // 白色×星形 = 刷新所有 PendingTrigger
            else if (shape.ShapeName == "ProbBurst" && ctx.Self != null && ctx.Self.PendingTriggers.Count > 0)
            {
                ctx.Log.Add(new EffectResult
                {
                    Element = ElementName,
                    Shape = "Pure×Star:刷新冷却",
                    Part = "OnHitExtra",
                    Damage = 0, HitCount = 0,
                    Status = "RefreshPendings",
                    Note = "OnHitExtra",
                });
            }
        }
    }
}
