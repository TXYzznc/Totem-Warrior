using System;
using Tattoo.Data;

namespace Tattoo.Strategies.Shapes
{
    public sealed class ProbBurstShapeBehavior : IShapeBehavior
    {
        public string ShapeName => "ProbBurst";

        readonly float _probability;
        readonly float _burstMultiplier;
        readonly int   _seed;
        Random _rng;

        public ProbBurstShapeBehavior(float probability = 1.0f, float burstMultiplier = 2.0f, int seed = 12345)
        {
            _probability = probability;
            _burstMultiplier = burstMultiplier;
            _seed = seed;
        }

        public void Apply(EffectContext ctx, IElementBehavior element, float magnitude, string partName, float synergyMul)
        {
            var t = ctx.PrimaryTarget;
            if (t == null) return;

            _rng ??= new Random(_seed);
            bool burst = _rng.NextDouble() < _probability;
            float dmg = burst ? magnitude * _burstMultiplier : 0f;
            string status = null;
            if (burst)
            {
                t.Health -= dmg;
                status = element.ApplyElementEffect(t, dmg);
            }

            ctx.Log.Add(new EffectResult
            {
                Element = element.ElementName,
                Shape = burst ? ShapeName + ":Burst" : ShapeName + ":Miss",
                Part = partName,
                Damage = dmg,
                HitCount = burst ? 1 : 0,
                Status = burst ? $"x{_burstMultiplier}/{status}" : "miss",
                SynergyMul = synergyMul,
            });
        }
    }
}
