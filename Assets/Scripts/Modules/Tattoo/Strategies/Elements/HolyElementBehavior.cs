using Tattoo.Data;

namespace Tattoo.Strategies.Elements
{
    public sealed class HolyElementBehavior : IElementBehavior
    {
        public ElementType Element => ElementType.Holy;
        public string ElementName => "Holy";
        public float BaseMultiplier { get; }

        readonly float _healPercent;

        public HolyElementBehavior(float baseMultiplier, float healPercent = 0.15f)
        {
            BaseMultiplier = baseMultiplier;
            _healPercent = healPercent;
        }

        public string ApplyElementEffect(Target target, float damage)
        {
            var tag = "神圣印记(被命中暴伤+50%)";
            target.Statuses.Add(tag);
            return tag;
        }

        public float ModifyMagnitude(EffectContext ctx, float baseMag) => baseMag;

        public void AffectSelf(PlayerState self, EffectContext ctx, float damage)
        {
            float heal = damage * _healPercent;
            self.Health += heal;
            self.Buffs.Add($"治疗+{heal:F1}");
        }

        public void OnHitExtra(EffectContext ctx, IShapeBehavior shape, Target target, float damage)
        {
            // 金×闪电 = 链跳过程中每跳额外回血 5%
            if (shape != null && shape.ShapeName == "ChainJump" && ctx.Self != null)
            {
                float heal = damage * 0.05f * 3f;
                ctx.Self.Health += heal;
                ctx.Log.Add(new EffectResult
                {
                    Element = ElementName,
                    Shape = "Holy×Bolt:跳跳回血",
                    Part = "OnHitExtra",
                    Damage = 0, HitCount = 0,
                    Status = $"+{heal:F1} HP",
                    Note = "OnHitExtra",
                });
            }
        }
    }
}
