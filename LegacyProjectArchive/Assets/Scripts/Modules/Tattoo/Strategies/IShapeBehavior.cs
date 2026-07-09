using Tattoo.Data;

namespace Tattoo.Strategies
{
    /// <summary>图案（形状）策略层。负责把 magnitude 应用到目标。</summary>
    public interface IShapeBehavior
    {
        string ShapeName { get; }

        void Apply(EffectContext ctx, IElementBehavior element, float magnitude, string partName, float synergyMul);
    }
}
