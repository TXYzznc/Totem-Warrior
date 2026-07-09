using Tattoo.Data;

namespace Tattoo.Strategies
{
    /// <summary>
    /// 颜色（元素）策略层。4 个 hook：副作用 / 修改倍率 / 自身效果 / 命中后额外行为。
    /// </summary>
    public interface IElementBehavior
    {
        ElementType Element { get; }
        string      ElementName { get; }
        float       BaseMultiplier { get; }

        /// <summary>命中目标后给目标加 status / DOT 等。返回 status 标签。</summary>
        string ApplyElementEffect(Target target, float damage);

        /// <summary>修改这次触发的 magnitude（白色 +20%、紫色随机、Focus 叠层等）。</summary>
        float ModifyMagnitude(EffectContext ctx, float baseMag);

        /// <summary>对玩家自身的效果（金回血、白叠专注、紫微随机 buff）。</summary>
        void AffectSelf(PlayerState self, EffectContext ctx, float damage);

        /// <summary>图案命中后追加的元素侧行为（白色×闪电=额外普攻等）。</summary>
        void OnHitExtra(EffectContext ctx, IShapeBehavior shape, Target target, float damage);
    }
}
