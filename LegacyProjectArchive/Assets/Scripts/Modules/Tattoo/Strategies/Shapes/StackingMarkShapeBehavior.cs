using Tattoo.Data;

namespace Tattoo.Strategies.Shapes
{
    public sealed class StackingMarkShapeBehavior : IShapeBehavior
    {
        public string ShapeName => "StackingMark";

        readonly int   _threshold;
        readonly float _burstMul;

        public StackingMarkShapeBehavior(int threshold = 5, float burstMul = 4f)
        {
            _threshold = threshold;
            _burstMul = burstMul;
        }

        public void Apply(EffectContext ctx, IElementBehavior element, float magnitude, string partName, float synergyMul)
        {
            var t = ctx.PrimaryTarget;
            if (t == null) return;

            var key = ((IShapeBehavior)this, partName);
            t.Marks.TryGetValue(key, out int stacks);
            stacks++;

            if (stacks >= _threshold)
            {
                float dmg = magnitude * _burstMul;
                t.Health -= dmg;
                var status = element.ApplyElementEffect(t, dmg);
                t.Marks[key] = 0;

                ctx.Log.Add(new EffectResult
                {
                    Element = element.ElementName,
                    Shape = ShapeName + ":Burst",
                    Part = partName,
                    Damage = dmg,
                    HitCount = 1,
                    Status = $"BurstAt{_threshold}/{status}",
                    SynergyMul = synergyMul,
                });
            }
            else
            {
                t.Marks[key] = stacks;
                ctx.Log.Add(new EffectResult
                {
                    Element = element.ElementName,
                    Shape = ShapeName + ":Stack",
                    Part = partName,
                    Damage = 0f,
                    HitCount = 0,
                    Status = $"Stack{stacks}/{_threshold}",
                    SynergyMul = synergyMul,
                });
            }
        }
    }
}
