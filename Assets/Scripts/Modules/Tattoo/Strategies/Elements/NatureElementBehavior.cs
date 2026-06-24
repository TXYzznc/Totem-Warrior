using System;
using Tattoo.Data;

namespace Tattoo.Strategies.Elements
{
    public sealed class NatureElementBehavior : IElementBehavior
    {
        public ElementType Element => ElementType.Nature;
        public string ElementName => "Nature";
        public float BaseMultiplier { get; }

        readonly float _poisonDPS;
        readonly float _poisonDuration;
        readonly int   _maxRegenStacks;

        public NatureElementBehavior(float baseMultiplier, float poisonDPS, float poisonDuration, int maxRegenStacks = 5)
        {
            BaseMultiplier = baseMultiplier;
            _poisonDPS = poisonDPS;
            _poisonDuration = poisonDuration;
            _maxRegenStacks = maxRegenStacks;
        }

        public string ApplyElementEffect(Target target, float damage)
        {
            var tag = $"Poison({_poisonDPS}/s,{_poisonDuration}s)";
            target.Statuses.Add(tag);
            return tag;
        }

        public float ModifyMagnitude(EffectContext ctx, float baseMag) => baseMag;

        public void AffectSelf(PlayerState self, EffectContext ctx, float damage)
        {
            self.Stacks.TryGetValue("Regen", out int cur);
            self.Stacks["Regen"] = Math.Min(cur + 1, _maxRegenStacks);
        }

        public void OnHitExtra(EffectContext ctx, IShapeBehavior shape, Target target, float damage) { }
    }
}
