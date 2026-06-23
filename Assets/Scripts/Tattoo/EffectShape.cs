using UnityEngine;

namespace Tattoo
{
    public abstract class EffectShape : ScriptableObject
    {
        public abstract void Apply(EffectContext ctx, ElementBehavior element,
                                   float magnitude, string partName, float synergyMul);
    }

    [CreateAssetMenu(menuName = "Tattoo/Shape/SingleHit", fileName = "Shape_SingleHit")]
    public class SingleHitShape : EffectShape
    {
        public override void Apply(EffectContext ctx, ElementBehavior element,
                                   float magnitude, string partName, float synergyMul)
        {
            var t = ctx.PrimaryTarget;
            if (t == null) return;
            t.Health -= magnitude;
            var status = element.ApplyElementEffect(t, magnitude);
            ctx.Log.Add(new EffectResult
            {
                Element  = element.ElementName,
                Shape    = nameof(SingleHitShape),
                Part     = partName,
                Damage   = magnitude,
                HitCount = 1,
                Status   = status,
                SynergyMul = synergyMul,
            });
        }
    }

    [CreateAssetMenu(menuName = "Tattoo/Shape/AOEBurst", fileName = "Shape_AOEBurst")]
    public class AOEBurstShape : EffectShape
    {
        public float AreaFactor = 0.6f;
        public int   MaxTargets = 5;

        public override void Apply(EffectContext ctx, ElementBehavior element,
                                   float magnitude, string partName, float synergyMul)
        {
            float per = magnitude * AreaFactor;
            int hits = 0; float total = 0f;
            if (ctx.PrimaryTarget != null)
            {
                ctx.PrimaryTarget.Health -= per;
                element.ApplyElementEffect(ctx.PrimaryTarget, per);
                hits++; total += per;
            }
            foreach (var t in ctx.NearbyTargets)
            {
                if (hits >= MaxTargets) break;
                t.Health -= per;
                element.ApplyElementEffect(t, per);
                hits++; total += per;
            }
            ctx.Log.Add(new EffectResult
            {
                Element  = element.ElementName,
                Shape    = nameof(AOEBurstShape),
                Part     = partName,
                Damage   = total,
                HitCount = hits,
                Status   = element.ElementName + ":AOE",
                SynergyMul = synergyMul,
            });
        }
    }

    [CreateAssetMenu(menuName = "Tattoo/Shape/StackingMark", fileName = "Shape_StackingMark")]
    public class StackingMarkShape : EffectShape
    {
        public int   Threshold = 5;
        public float BurstMul  = 4f;

        public override void Apply(EffectContext ctx, ElementBehavior element,
                                   float magnitude, string partName, float synergyMul)
        {
            var t = ctx.PrimaryTarget;
            if (t == null) return;
            var key = (Shape: (EffectShape)this, Part: partName);
            t.Marks.TryGetValue(key, out int stacks);
            stacks++;
            if (stacks >= Threshold)
            {
                float dmg = magnitude * BurstMul;
                t.Health -= dmg;
                var status = element.ApplyElementEffect(t, dmg);
                t.Marks[key] = 0;
                ctx.Log.Add(new EffectResult
                {
                    Element  = element.ElementName,
                    Shape    = nameof(StackingMarkShape) + ":Burst",
                    Part     = partName,
                    Damage   = dmg,
                    HitCount = 1,
                    Status   = $"BurstAt{Threshold}/{status}",
                    SynergyMul = synergyMul,
                });
            }
            else
            {
                t.Marks[key] = stacks;
                ctx.Log.Add(new EffectResult
                {
                    Element  = element.ElementName,
                    Shape    = nameof(StackingMarkShape) + ":Stack",
                    Part     = partName,
                    Damage   = 0f,
                    HitCount = 0,
                    Status   = $"Stack{stacks}/{Threshold}",
                    SynergyMul = synergyMul,
                });
            }
        }
    }

    [CreateAssetMenu(menuName = "Tattoo/Shape/MultiHit", fileName = "Shape_MultiHit")]
    public class MultiHitShape : EffectShape
    {
        public int Segments = 4;

        public override void Apply(EffectContext ctx, ElementBehavior element,
                                   float magnitude, string partName, float synergyMul)
        {
            var t = ctx.PrimaryTarget;
            if (t == null) return;
            float per = magnitude / Segments;
            string status = null;
            for (int i = 0; i < Segments; i++)
            {
                t.Health -= per;
                status = element.ApplyElementEffect(t, per);
            }
            ctx.Log.Add(new EffectResult
            {
                Element  = element.ElementName,
                Shape    = nameof(MultiHitShape),
                Part     = partName,
                Damage   = magnitude,
                HitCount = Segments,
                Status   = $"x{Segments}/{status}",
                SynergyMul = synergyMul,
            });
        }
    }

    [CreateAssetMenu(menuName = "Tattoo/Shape/ChainJump", fileName = "Shape_ChainJump")]
    public class ChainJumpShape : EffectShape
    {
        public int   MaxJumps = 3;
        public float Decay    = 0.7f;

        public override void Apply(EffectContext ctx, ElementBehavior element,
                                   float magnitude, string partName, float synergyMul)
        {
            int hits = 0; float total = 0f; float dmg = magnitude; string lastStatus = null;
            if (ctx.PrimaryTarget != null)
            {
                ctx.PrimaryTarget.Health -= dmg;
                lastStatus = element.ApplyElementEffect(ctx.PrimaryTarget, dmg);
                total += dmg; hits++; dmg *= Decay;
            }
            foreach (var t in ctx.NearbyTargets)
            {
                if (hits >= MaxJumps) break;
                t.Health -= dmg;
                lastStatus = element.ApplyElementEffect(t, dmg);
                total += dmg; hits++; dmg *= Decay;
            }
            ctx.Log.Add(new EffectResult
            {
                Element  = element.ElementName,
                Shape    = nameof(ChainJumpShape),
                Part     = partName,
                Damage   = total,
                HitCount = hits,
                Status   = $"jumps{hits}/{lastStatus}",
                SynergyMul = synergyMul,
            });
        }
    }

    [CreateAssetMenu(menuName = "Tattoo/Shape/ProbBurst", fileName = "Shape_ProbBurst")]
    public class ProbBurstShape : EffectShape
    {
        [Range(0f, 1f)] public float Probability      = 1.0f;
        public float                BurstMultiplier  = 2.0f;
        public int                  Seed             = 12345;
        System.Random rng;

        public override void Apply(EffectContext ctx, ElementBehavior element,
                                   float magnitude, string partName, float synergyMul)
        {
            var t = ctx.PrimaryTarget;
            if (t == null) return;
            rng ??= new System.Random(Seed);
            bool burst = rng.NextDouble() < Probability;
            float dmg  = burst ? magnitude * BurstMultiplier : 0f;
            string status = null;
            if (burst)
            {
                t.Health -= dmg;
                status = element.ApplyElementEffect(t, dmg);
            }
            ctx.Log.Add(new EffectResult
            {
                Element  = element.ElementName,
                Shape    = burst ? nameof(ProbBurstShape) + ":Burst"
                                 : nameof(ProbBurstShape) + ":Miss",
                Part     = partName,
                Damage   = dmg,
                HitCount = burst ? 1 : 0,
                Status   = burst ? $"x{BurstMultiplier}/{status}" : "miss",
                SynergyMul = synergyMul,
            });
        }
    }

    [CreateAssetMenu(menuName = "Tattoo/Shape/TrailZone", fileName = "Shape_TrailZone")]
    public class TrailZoneShape : EffectShape
    {
        public float TickFactor = 0.4f;
        public int   Ticks      = 3;

        public override void Apply(EffectContext ctx, ElementBehavior element,
                                   float magnitude, string partName, float synergyMul)
        {
            float perTick = magnitude * TickFactor;
            int hits = 0; float total = 0f; string lastStatus = null;
            void Tick(Target t)
            {
                for (int i = 0; i < Ticks; i++)
                {
                    t.Health -= perTick;
                    lastStatus = element.ApplyElementEffect(t, perTick);
                    total += perTick; hits++;
                }
                if (!t.Statuses.Contains("InTrail")) t.Statuses.Add("InTrail");
            }
            if (ctx.PrimaryTarget != null) Tick(ctx.PrimaryTarget);
            foreach (var t in ctx.NearbyTargets) Tick(t);
            ctx.Log.Add(new EffectResult
            {
                Element  = element.ElementName,
                Shape    = nameof(TrailZoneShape),
                Part     = partName,
                Damage   = total,
                HitCount = hits,
                Status   = $"trail/{lastStatus}",
                SynergyMul = synergyMul,
            });
        }
    }

    [CreateAssetMenu(menuName = "Tattoo/Shape/SummonForm", fileName = "Shape_SummonForm")]
    public class SummonFormShape : EffectShape
    {
        public float SummonMultiplier = 2.5f;

        public override void Apply(EffectContext ctx, ElementBehavior element,
                                   float magnitude, string partName, float synergyMul)
        {
            var t = ctx.PrimaryTarget;
            if (t == null) return;
            float dmg = magnitude * SummonMultiplier;
            t.Health -= dmg;
            var status = element.ApplyElementEffect(t, dmg);
            t.Statuses.Add($"Summon[{element.ElementName}]");
            ctx.Log.Add(new EffectResult
            {
                Element  = element.ElementName,
                Shape    = nameof(SummonFormShape),
                Part     = partName,
                Damage   = dmg,
                HitCount = 1,
                Status   = $"summon/{status}",
                SynergyMul = synergyMul,
            });
        }
    }
}
