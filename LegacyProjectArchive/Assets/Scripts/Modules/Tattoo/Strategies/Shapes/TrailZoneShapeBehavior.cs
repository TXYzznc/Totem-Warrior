using Tattoo.Data;

namespace Tattoo.Strategies.Shapes
{
    public sealed class TrailZoneShapeBehavior : IShapeBehavior
    {
        public string ShapeName => "TrailZone";

        readonly float _tickFactor;
        readonly int   _ticks;

        public TrailZoneShapeBehavior(float tickFactor = 0.4f, int ticks = 3)
        {
            _tickFactor = tickFactor;
            _ticks = ticks;
        }

        public void Apply(EffectContext ctx, IElementBehavior element, float magnitude, string partName, float synergyMul)
        {
            float perTick = magnitude * _tickFactor;
            int hits = 0;
            float total = 0f;
            string lastStatus = null;

            void Tick(Target t)
            {
                for (int i = 0; i < _ticks; i++)
                {
                    t.Health -= perTick;
                    lastStatus = element.ApplyElementEffect(t, perTick);
                    total += perTick;
                    hits++;
                }
                if (!t.Statuses.Contains("InTrail")) t.Statuses.Add("InTrail");
            }

            if (ctx.PrimaryTarget != null) Tick(ctx.PrimaryTarget);
            foreach (var t in ctx.NearbyTargets) Tick(t);

            ctx.Log.Add(new EffectResult
            {
                Element = element.ElementName,
                Shape = ShapeName,
                Part = partName,
                Damage = total,
                HitCount = hits,
                Status = $"trail/{lastStatus}",
                SynergyMul = synergyMul,
            });
        }
    }
}
