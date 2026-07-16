using System;
using System.Collections.Generic;

namespace PCGMap
{
    [Flags]
    public enum PCGCapabilityKind
    {
        None = 0,
        Visual = 1 << 0,
        Collision = 1 << 1,
        MovementModifier = 1 << 2,
        Occlusion = 1 << 3,
        Hazard = 1 << 4,
        Interaction = 1 << 5,
        EventAffinity = 1 << 6,
    }

    public struct PCGWorldCell
    {
        public int X;
        public int Y;
        public string TerrainId;
        public string RegionId;
        public float Density;
        /// <summary>当前已启用的运行时能力；本版本固定仅 Visual。</summary>
        public PCGCapabilityKind Capabilities;
        /// <summary>由主题配置声明、留待后续能力系统显式启用的能力。</summary>
        public PCGCapabilityKind FutureCapabilities;
    }

    public sealed class PCGWorldEventAnchor
    {
        public string Id;
        public string EventType;
        public string VisualRole;
        public string TerrainId;
        public string RegionId;
        public float NormalizedX;
        public float NormalizedY;
        public int Order;
    }

    /// <summary>
    /// 单次地图生成的只读逻辑产物。渲染、事件和未来能力系统只消费该计划，
    /// 不再从 Tilemap 或实例化物体反推生成规则。
    /// </summary>
    public sealed class PCGWorldPlan
    {
        public int ThemeId;
        public string ThemeIdText;
        public string BiomeId;
        public int Seed;
        public int Width;
        public int Height;
        public string ProfileVersion;
        public PCGVisualPlacementProfile VisualPlacement;
        public PCGWorldCell[] Cells = Array.Empty<PCGWorldCell>();
        public PCGWorldEventAnchor[] EventAnchors = Array.Empty<PCGWorldEventAnchor>();
        public readonly List<string> Diagnostics = new();
        public ulong ContentHash;

        public PCGWorldCell GetCell(int x, int y) => Cells[y * Width + x];
    }
}
