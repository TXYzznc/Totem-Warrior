using Tattoo.Data;

namespace Tattoo.Strategies.Shapes
{
    public sealed class ChainJumpShapeBehavior : IShapeBehavior
    {
        public string ShapeName => "ChainJump";

        readonly int   _maxJumps;
        readonly float _decay;

        public ChainJumpShapeBehavior(int maxJumps = 3, float decay = 0.7f)
        {
            _maxJumps = maxJumps;
            _decay = decay;
        }

        public void Apply(EffectContext ctx, IElementBehavior element, float magnitude, string partName, float synergyMul)
        {
            int hits = 0;
            float total = 0f;
            float dmg = magnitude;
            string lastStatus = null;

            if (ctx.PrimaryTarget != null)
            {
                ctx.PrimaryTarget.Health -= dmg;
                lastStatus = element.ApplyElementEffect(ctx.PrimaryTarget, dmg);
                total += dmg; hits++;
                dmg *= _decay;
            }

            foreach (var t in ctx.NearbyTargets)
            {
                if (hits >= _maxJumps) break;
                t.Health -= dmg;
                lastStatus = element.ApplyElementEffect(t, dmg);
                total += dmg; hits++;
                dmg *= _decay;
            }

            ctx.Log.Add(new EffectResult
            {
                Element = element.ElementName,
                Shape = ShapeName,
                Part = partName,
                Damage = total,
                HitCount = hits,
                Status = $"jumps{hits}/{lastStatus}",
                SynergyMul = synergyMul,
            });
        }
    }
}
