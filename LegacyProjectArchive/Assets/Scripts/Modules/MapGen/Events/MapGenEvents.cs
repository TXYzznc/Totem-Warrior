using System.Collections.Generic;
using MapGen.Data;
using UnityEngine;

namespace MapGen.Events
{
    /// <summary>
    /// 地图生成完成事件（v2.1 锁死字段：Seed / Theme / Rooms / InitialZoneCenter）。
    /// 整个 Run 内仅发布一次，是"地图就绪"的关键信号。
    /// 下游：SpawnerModule / EnemyModule / NPCModule / EconomyModule / ZoneModule / UIModule(MiniMap)。
    /// </summary>
    public sealed class MapGeneratedEvent
    {
        /// <summary>本次生成使用的随机种子（确定性，同 seed → 同地图）</summary>
        public int Seed;
        /// <summary>主题 ID，对应 MapTemplateConfig.Id</summary>
        public int ThemeId;
        /// <summary>所有房间列表</summary>
        public List<RoomInfo> Rooms;
        /// <summary>初始缩圈圆心（世界 XZ 坐标，地图中央 1/3 区域内）</summary>
        public Vector2 InitialZoneCenter;
        /// <summary>地图根边界尺寸（单位 m）</summary>
        public float MapSize;
    }

    /// <summary>
    /// Actor 跨房间事件。运行时由 MapGenModule 内的 RoomTracker 0.2s tick 一次触发。
    /// MVP 阶段暂未实现 RoomTracker，仅声明事件契约。
    /// </summary>
    public sealed class RoomEnteredEvent
    {
        /// <summary>进入房间的 actor 名称（伪联机阶段够用；真联机需换 ActorId）</summary>
        public string ActorName;
        /// <summary>被进入的房间</summary>
        public RoomInfo Room;
    }

    /// <summary>
    /// 缩圈阶段切换事件。由 MapGenModule 内置 ZoneShrink Tick 控制器发布。
    /// v2.1 三段：
    ///   Phase 0：0~3min     初始大圈，缓慢收缩
    ///   Phase 1：3~9min     中段，圈心偏移
    ///   Phase 2：9~15min    末段，急速收缩到死圈
    /// </summary>
    public sealed class ZoneShrinkPhaseEvent
    {
        /// <summary>阶段编号（0/1/2）</summary>
        public int Phase;
        /// <summary>本阶段目标圆心（世界 XZ 坐标）</summary>
        public Vector2 Center;
        /// <summary>本阶段目标半径（米）</summary>
        public float TargetRadius;
        /// <summary>本阶段持续时长（秒）</summary>
        public float Duration;
        /// <summary>圈外伤害（HP/s）</summary>
        public float OutZoneDamage;
        /// <summary>本阶段倒计时剩余秒（HUD 用，MapGenModule 每 tick 写入）。</summary>
        public float SecondsRemaining;
    }
}
