using Tattoo.Data;

namespace Tattoo.Strategies.Elements
{
    public sealed class FireElementBehavior : IElementBehavior
    {
        public ElementType Element => ElementType.Fire;
        public string ElementName => "Fire";
        public float BaseMultiplier { get; }

        readonly float _burnDPS;
        readonly float _burnDuration;

        public FireElementBehavior(float baseMultiplier, float burnDPS, float burnDuration)
        {
            BaseMultiplier = baseMultiplier;
            _burnDPS = burnDPS;
            _burnDuration = burnDuration;
        }

        public string ApplyElementEffect(Target target, float damage)
        {
            var tag = $"Burn({_burnDPS}/s,{_burnDuration}s)";
            target.Statuses.Add(tag);
            return tag;
        }

        public float ModifyMagnitude(EffectContext ctx, float baseMag) => baseMag;
        public void AffectSelf(PlayerState self, EffectContext ctx, float damage) { }
        public void OnHitExtra(EffectContext ctx, IShapeBehavior shape, Target target, float damage) { }
    }
}
