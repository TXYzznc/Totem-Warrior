using UnityEngine;

namespace MapGen.Data
{
    /// <summary>
    /// 房间节点类型（v2.1）。MVP 占位地图仅使用其中 4 个固定值。
    /// </summary>
    public enum RoomNodeType
    {
        /// <summary>普通房间</summary>
        Normal,
        /// <summary>玩家出生圈</summary>
        SpawnRoom,
        /// <summary>纹身师工作室</summary>
        TattooStudio,
        /// <summary>商人</summary>
        Merchant,
        /// <summary>Boss 房</summary>
        BossRoom,
        /// <summary>环境叙事点</summary>
        EnvNarrative,
    }

    /// <summary>
    /// 房间尺寸类别。
    /// </summary>
    public enum SizeCategory
    {
        Small,
        Medium,
        Large,
    }

    /// <summary>
    /// 房间运行时信息。事件 payload，不是 DataTable。
    /// MVP 阶段简化版：4 个房间，SpawnerNodes/ChestNodes/NpcSlots 留空，由后续 BSP 实现填充。
    /// </summary>
    public sealed class RoomInfo
    {
        /// <summary>房间唯一 ID（0..N-1）</summary>
        public int RoomId;
        /// <summary>世界坐标 AABB（XZ 平面），用于 RoomEnteredEvent 判定 &amp; MiniMap</summary>
        public Rect Bounds;
        /// <summary>房间地面中心点（世界坐标）</summary>
        public Vector3 CenterWorld;
        /// <summary>房间节点类型</summary>
        public RoomNodeType NodeType;
        /// <summary>房间尺寸类别</summary>
        public SizeCategory Size;
        /// <summary>主题元数据：DominantColor 等的透传字段。MVP 暂未填充。</summary>
        public string ThemeMetadata;
    }
}
