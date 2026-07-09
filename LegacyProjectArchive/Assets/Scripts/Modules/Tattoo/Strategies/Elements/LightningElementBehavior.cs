using Tattoo.Data;

namespace Tattoo.Strategies.Elements
{
    public sealed class LightningElementBehavior : IElementBehavior
    {
        public ElementType Element => ElementType.Lightning;
        public string ElementName => "Lightning";
        public float BaseMultiplier { get; }

        readonly float _paralyzeDuration;

        public LightningElementBehavior(float baseMultiplier, float paralyzeDuration)
        {
            BaseMultiplier = baseMultiplier;
            _paralyzeDuration = paralyzeDuration;
        }

        public string ApplyElementEffect(Target target, float damage)
        {
            var tag = $"Paralyze({_paralyzeDuration}s)";
            target.Statuses.Add(tag);
            return tag;
        }

        public float ModifyMagnitude(EffectContext ctx, float baseMag) => baseMag;
        public void AffectSelf(PlayerState self, EffectContext ctx, float damage) { }
        public void OnHitExtra(EffectContext ctx, IShapeBehavior shape, Target target, float damage) { }
    }
}
