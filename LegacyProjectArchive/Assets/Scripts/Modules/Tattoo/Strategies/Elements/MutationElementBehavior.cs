using System;
using Tattoo.Data;

namespace Tattoo.Strategies.Elements
{
    public sealed class MutationElementBehavior : IElementBehavior
    {
        public ElementType Element => ElementType.Mutation;
        public string ElementName => "Mutation";
        public float BaseMultiplier { get; }

        readonly int _seed;
        Random _rng;

        public MutationElementBehavior(float baseMultiplier, int seed = 42)
        {
            BaseMultiplier = baseMultiplier;
            _seed = seed;
        }

        public float ModifyMagnitude(EffectContext ctx, float baseMag)
        {
            _rng ??= new Random(_seed);
            return baseMag * (float)(0.9 + _rng.NextDouble() * 0.4);
        }

        public string ApplyElementEffect(Target target, float damage)
        {
            _rng ??= new Random(_seed);
            string[] picks = { "Mutate-眩晕", "Mutate-沉默", "Mutate-虚弱" };
            var tag = picks[_rng.Next(picks.Length)];
            target.Statuses.Add(tag);
            return tag;
        }

        public void AffectSelf(PlayerState self, EffectContext ctx, float damage)
        {
            self.Buffs.Add("异变·随机微Buff");
        }

        public void OnHitExtra(EffectContext ctx, IShapeBehavior shape, Target target, float damage)
        {
            // 紫×星形 = 现实崩塌：重置 RNG seed
            if (shape != null && shape.ShapeName == "ProbBurst")
            {
                _rng = new Random(DateTime.Now.Millisecond);
                ctx.Log.Add(new EffectResult
                {
                    Element = ElementName,
                    Shape = "Mutation×Star:现实崩塌",
                    Part = "OnHitExtra",
                    Damage = 0, HitCount = 0,
                    Status = "RandomSeedReset",
                    Note = "OnHitExtra",
                });
            }
        }
    }
}
