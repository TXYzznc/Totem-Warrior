using Tattoo.Strategies;

namespace Tattoo.Data
{
    /// <summary>一个装备槽位。引用 3 个策略 + 配置行 Id。</summary>
    public class TattooSlot
    {
        public int PartId;
        public int ColorId;
        public int PatternId;

        public IPartBehavior    Part;
        public IElementBehavior Element;
        public IShapeBehavior   Shape;

        /// <summary>部位触发事件的 .NET 类型，预解析缓存。</summary>
        public System.Type TriggerEventType;

        /// <summary>颜色倍率（来自 tattoo_color.json）。</summary>
        public float ColorMultiplier = 1f;
        /// <summary>图案倍率（来自 tattoo_pattern.json）。</summary>
        public float PatternMultiplier = 1f;
        /// <summary>部位缩放属性（来自 tattoo_part.json）。</summary>
        public StatType ScaleStat;
        /// <summary>部位缩放系数（来自 tattoo_part.json）。</summary>
        public float ScaleFactor = 1f;
        /// <summary>部位对称分组（来自 tattoo_part.json）。</summary>
        public SymmetryGroup SymmetryGroup = SymmetryGroup.None;
        /// <summary>部位名（来自 tattoo_part.json）。</summary>
        public string PartName;
        /// <summary>颜色名（来自 tattoo_color.json）。</summary>
        public string ColorName;
        /// <summary>图案名（来自 tattoo_pattern.json）。</summary>
        public string PatternName;
        /// <summary>颜色对应的元素类型。</summary>
        public ElementType ElementType;
    }
}
