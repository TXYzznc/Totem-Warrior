using Tattoo.Data;

namespace Tattoo.Strategies.Shapes
{
    public sealed class MultiHitShapeBehavior : IShapeBehavior
    {
        public string ShapeName => "MultiHit";

        readonly int _segments;

        public MultiHitShapeBehavior(int segments = 4)
        {
            _segments = segments;
        }

        public void Apply(EffectContext ctx, IElementBehavior element, float magnitude, string partName, float synergyMul)
        {
            var t = ctx.PrimaryTarget;
            if (t == null) return;

            float per = magnitude / _segments;
            string status = null;
            for (int i = 0; i < _segments; i++)
            {
                t.Health -= per;
                status = element.ApplyElementEffect(t, per);
            }

            ctx.Log.Add(new EffectResult
            {
                Element = element.ElementName,
                Shape = ShapeName,
                Part = partName,
                Damage = magnitude,
                HitCount = _segments,
                Status = $"x{_segments}/{status}",
                SynergyMul = synergyMul,
            });
        }
    }
}
