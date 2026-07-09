using Tattoo.Data;

namespace Tattoo.Strategies.Shapes
{
    public sealed class SingleHitShapeBehavior : IShapeBehavior
    {
        public string ShapeName => "SingleHit";

        public void Apply(EffectContext ctx, IElementBehavior element, float magnitude, string partName, float synergyMul)
        {
            var t = ctx.PrimaryTarget;
            if (t == null) return;

            t.Health -= magnitude;
            var status = element.ApplyElementEffect(t, magnitude);

            ctx.Log.Add(new EffectResult
            {
                Element = element.ElementName,
                Shape = ShapeName,
                Part = partName,
                Damage = magnitude,
                HitCount = 1,
                Status = status,
                SynergyMul = synergyMul,
            });
        }
    }
}
