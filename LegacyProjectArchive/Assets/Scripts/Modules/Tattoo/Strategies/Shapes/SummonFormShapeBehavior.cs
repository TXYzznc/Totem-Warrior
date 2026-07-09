using Tattoo.Data;

namespace Tattoo.Strategies.Shapes
{
    public sealed class SummonFormShapeBehavior : IShapeBehavior
    {
        public string ShapeName => "SummonForm";

        readonly float _summonMultiplier;

        public SummonFormShapeBehavior(float summonMultiplier = 2.5f)
        {
            _summonMultiplier = summonMultiplier;
        }

        public void Apply(EffectContext ctx, IElementBehavior element, float magnitude, string partName, float synergyMul)
        {
            var t = ctx.PrimaryTarget;
            if (t == null) return;

            float dmg = magnitude * _summonMultiplier;
            t.Health -= dmg;
            var status = element.ApplyElementEffect(t, dmg);
            t.Statuses.Add($"Summon[{element.ElementName}]");

            ctx.Log.Add(new EffectResult
            {
                Element = element.ElementName,
                Shape = ShapeName,
                Part = partName,
                Damage = dmg,
                HitCount = 1,
                Status = $"summon/{status}",
                SynergyMul = synergyMul,
            });
        }
    }
}
