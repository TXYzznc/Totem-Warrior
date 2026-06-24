using System;
using Tattoo.Data;

namespace Tattoo.Strategies.Elements
{
    public sealed class FrostElementBehavior : IElementBehavior
    {
        public ElementType Element => ElementType.Frost;
        public string ElementName => "Frost";
        public float BaseMultiplier { get; }

        readonly float _slowFactor;
        readonly float _slowDuration;
        readonly int   _maxArmorStacks;

        public FrostElementBehavior(float baseMultiplier, float slowFactor, float slowDuration, int maxArmorStacks = 5)
        {
            BaseMultiplier = baseMultiplier;
            _slowFactor = slowFactor;
            _slowDuration = slowDuration;
            _maxArmorStacks = maxArmorStacks;
        }

        public string ApplyElementEffect(Target target, float damage)
        {
            var tag = $"Slow(-{_slowFactor * 100f:F0}%,{_slowDuration}s)";
            target.Statuses.Add(tag);
            return tag;
        }

        public float ModifyMagnitude(EffectContext ctx, float baseMag) => baseMag;

        public void AffectSelf(PlayerState self, EffectContext ctx, float damage)
        {
            self.Stacks.TryGetValue("FrostArmor", out int cur);
            self.Stacks["FrostArmor"] = Math.Min(cur + 1, _maxArmorStacks);
        }

        public void OnHitExtra(EffectContext ctx, IShapeBehavior shape, Target target, float damage) { }
    }
}
