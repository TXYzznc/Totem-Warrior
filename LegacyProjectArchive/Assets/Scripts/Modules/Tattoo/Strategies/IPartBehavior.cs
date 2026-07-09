using Tattoo.Data;

namespace Tattoo.Strategies
{
    /// <summary>
    /// 部位策略层 — 5 个 hook 集中点：事件路由 / 目标重塑 / 拦截延迟 / 自身效果 / 被动属性。
    /// 图案和颜色侧不需要知道部位的存在。
    /// </summary>
    public interface IPartBehavior
    {
        /// <summary>部位名（"Head"/"Torso"/...），用于日志与 SynergyCalculator 标识。</summary>
        string PartName { get; }

        /// <summary>触发前重塑上下文（如躯干把 LastAttacker 设为 PrimaryTarget）。</summary>
        void PrepareContext(EffectContext ctx);

        /// <summary>
        /// 是否拦截本次 shape.Apply？返回 true 则部位自己处理（如左腿打包成 PendingTrigger）。
        /// </summary>
        bool InterceptApply(EffectContext ctx, IShapeBehavior shape, IElementBehavior element, float magnitude);

        /// <summary>触发后对玩家自身的效果（左腿短暂无敌 / 右腿位移加速 等）。</summary>
        void AffectSelf(PlayerState self, EffectContext ctx);

        /// <summary>装备此槽位时贡献的被动 stat modifier。</summary>
        void ContributePassive(PassiveStats passive, ElementType elem, float patternStrength, string colorName, string patternName);
    }
}
