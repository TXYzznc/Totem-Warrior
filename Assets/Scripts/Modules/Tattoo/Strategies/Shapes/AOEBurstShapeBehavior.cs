using Tattoo.Data;

namespace Tattoo.Strategies.Shapes
{
    public sealed class AOEBurstShapeBehavior : IShapeBehavior
    {
        public string ShapeName => "AOEBurst";

        readonly float _areaFactor;
        readonly int   _maxTargets;

        public AOEBurstShapeBehavior(float areaFactor = 0.6f, int maxTargets = 5)
        {
            _areaFactor = areaFactor;
            _maxTargets = maxTargets;
        }

        public void Apply(EffectContext ctx, IElementBehavior element, float magnitude, string partName, float synergyMul)
        {
            float per = magnitude * _areaFactor;
            int hits = 0;
            float total = 0f;

            if (ctx.PrimaryTarget != null)
            {
                ctx.PrimaryTarget.Health -= per;
                element.ApplyElementEffect(ctx.PrimaryTarget, per);
                hits++; total += per;
            }

            foreach (var t in ctx.NearbyTargets)
            {
                if (hits >= _maxTargets) break;
                t.Health -= per;
                element.ApplyElementEffect(t, per);
                hits++; total += per;
            }

            ctx.Log.Add(new EffectResult
            {
                Element = element.ElementName,
                Shape = ShapeName,
                Part = partName,
                Damage = total,
                HitCount = hits,
                Status = element.ElementName + ":AOE",
                SynergyMul = synergyMul,
            });
        }
    }
}
