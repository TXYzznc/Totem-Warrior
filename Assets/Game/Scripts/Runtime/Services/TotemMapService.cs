using System;
using System.Collections.Generic;
using PCGMap;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TotemPcgRuntimeProfile
{
    Full = 0,
    DiagnosticFast = 1,
}

public sealed class TotemMapService : TotemRuntimeServiceBase
{
    public const float DefaultMapSize = 400f;
    public const int TerrainCellSize = 4;
    public const int TerrainGridResolution = 100;
    public const int PcgMapWidth = 64;
    public const int PcgMapHeight = 64;
    public const int DiagnosticPcgMapWidth = 32;
    public const int DiagnosticPcgMapHeight = 32;
    public const float PcgEdgeMatchTolerance = 0.18f;

    private readonly List<GameObject> spawnedObjects = new List<GameObject>(16);
    private GameObject mapRoot;
    private TotemGameFlowService flowService;
    private TotemDataService dataService;
    private TotemAssetService assetService;
    private TotemMapTemplateDefinition[] runtimeTemplates = Array.Empty<TotemMapTemplateDefinition>();
    private int groundObjectCount;
    private int wallObjectCount;
    private int roomMarkerObjectCount;
    private int materialRequestCount;
    private int materialFallbackCount;
    private string lastMaterialAssetKey = string.Empty;
    private string lastMaterialFallbackAssetKey = string.Empty;
    private int pcgCellObjectCount;
    private int pcgVisualObjectCount;
    private int pcgMissingSpriteCount;
    private int pcgSpriteLoadCount;
    private int pcgSpriteCreateCount;

    private static PCGAssetIndex cachedPcgAssetIndex;
    private static string cachedPcgAssetIndexError = string.Empty;
    private static readonly Dictionary<string, TotemMapSnapshot> pcgSnapshotCache = new Dictionary<string, TotemMapSnapshot>(32);
    private static readonly Dictionary<string, Sprite> pcgSpriteCache = new Dictionary<string, Sprite>(512);
    private static readonly Dictionary<string, Tile> pcgTileCache = new Dictionary<string, Tile>(512);
    private static TotemPcgRuntimeProfile pcgRuntimeProfile = TotemPcgRuntimeProfile.Full;

    public override string ServiceName => "Map";

    public TotemMapSnapshot CurrentMap { get; private set; }

    public static TotemPcgRuntimeProfile CurrentPcgRuntimeProfile => pcgRuntimeProfile;

    public static int ActivePcgMapWidth => GetPcgRuntimeSettings().Width;

    public static int ActivePcgMapHeight => GetPcgRuntimeSettings().Height;

    public static IDisposable UsePcgRuntimeProfile(TotemPcgRuntimeProfile profile)
    {
        return new PcgRuntimeProfileScope(profile);
    }

    private static PcgRuntimeSettings GetPcgRuntimeSettings()
    {
        return pcgRuntimeProfile == TotemPcgRuntimeProfile.DiagnosticFast
            ? PcgRuntimeSettings.DiagnosticFast
            : PcgRuntimeSettings.Full;
    }

    private readonly struct PcgRuntimeSettings
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int ObjectBudget;
        public readonly int StampBudget;
        public readonly int DecalBudget;
        public readonly int MaxVisualSprites;
        public readonly bool RenderUnderlay;

        private PcgRuntimeSettings(
            int width,
            int height,
            int objectBudget,
            int stampBudget,
            int decalBudget,
            int maxVisualSprites,
            bool renderUnderlay)
        {
            Width = width;
            Height = height;
            ObjectBudget = objectBudget;
            StampBudget = stampBudget;
            DecalBudget = decalBudget;
            MaxVisualSprites = maxVisualSprites;
            RenderUnderlay = renderUnderlay;
        }

        public static PcgRuntimeSettings Full => new PcgRuntimeSettings(PcgMapWidth, PcgMapHeight, 160, 24, 180, int.MaxValue, true);

        public static PcgRuntimeSettings DiagnosticFast => new PcgRuntimeSettings(DiagnosticPcgMapWidth, DiagnosticPcgMapHeight, 36, 8, 48, 64, false);
    }

    private sealed class PcgRuntimeProfileScope : IDisposable
    {
        private readonly TotemPcgRuntimeProfile previousProfile;
        private bool disposed;

        public PcgRuntimeProfileScope(TotemPcgRuntimeProfile nextProfile)
        {
            previousProfile = pcgRuntimeProfile;
            pcgRuntimeProfile = nextProfile;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            pcgRuntimeProfile = previousProfile;
            disposed = true;
        }
    }

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        dataService = runtime.GetService<TotemDataService>();
        assetService = runtime.GetService<TotemAssetService>();
        runtimeTemplates = NonEmpty(dataService?.GameplayCatalog?.CreateMapTemplates(), LoadTemplates());

        if (flowService != null)
        {
            flowService.StateChanged += OnFlowStateChanged;
        }
    }

    protected override void OnShutdown()
    {
        if (flowService != null)
        {
            flowService.StateChanged -= OnFlowStateChanged;
            flowService = null;
        }

        ClearRuntimeMap();
        dataService = null;
        assetService = null;
        runtimeTemplates = Array.Empty<TotemMapTemplateDefinition>();
    }

    public TotemMapSnapshot GenerateMap(int seed, int themeId, bool createObjects)
    {
        DestroySpawnedObjects();
        CurrentMap = BuildLayout(seed, themeId, runtimeTemplates);
        if (createObjects)
        {
            CreateMapObjects(CurrentMap);
        }

        GFTrace.Success("TotemMap", "Map.Generated", null, GFTrace.Data(
            "seed", seed.ToString(),
            "themeId", themeId.ToString(),
            "roomCount", CurrentMap.Rooms.Length.ToString(),
            "mapSize", CurrentMap.MapSize.ToString("F1"),
            "zoneCenter", $"{CurrentMap.InitialZoneCenter.x:F1},{CurrentMap.InitialZoneCenter.y:F1}"));

        return CurrentMap;
    }

    public IReadOnlyList<TotemMapTemplateDefinition> GetRuntimeTemplates()
    {
        return runtimeTemplates;
    }

    public TotemMapRuntimeSnapshot CaptureRuntimeSnapshot()
    {
        int childCount = mapRoot == null ? 0 : mapRoot.transform.childCount;
        return new TotemMapRuntimeSnapshot
        {
            hasRoot = mapRoot != null,
            rootName = mapRoot == null ? string.Empty : mapRoot.name,
            spawnedObjectCount = mapRoot == null ? 0 : childCount + 1,
            rootChildCount = childCount,
            groundObjectCount = groundObjectCount,
            wallObjectCount = wallObjectCount,
            roomMarkerObjectCount = roomMarkerObjectCount,
            materialRequestCount = materialRequestCount,
            materialFallbackCount = materialFallbackCount,
            lastMaterialAssetKey = lastMaterialAssetKey,
            lastMaterialFallbackAssetKey = lastMaterialFallbackAssetKey,
            mapSize = CurrentMap?.MapSize ?? 0f,
            themeName = CurrentMap?.ThemeName ?? string.Empty,
            isPcgGenerated = CurrentMap?.IsPcgGenerated ?? false,
            pcgCellObjectCount = pcgCellObjectCount,
            pcgVisualObjectCount = pcgVisualObjectCount,
            pcgMissingSpriteCount = pcgMissingSpriteCount,
            pcgSpriteLoadCount = pcgSpriteLoadCount,
            pcgSpriteCreateCount = pcgSpriteCreateCount,
            pcgContentHash = CurrentMap?.PcgContentHash ?? 0UL,
        };
    }

    public static TotemMapSnapshot BuildLayout(int seed, int themeId)
    {
        return BuildLayout(seed, themeId, LoadTemplates());
    }

    public static TotemMapSnapshot BuildLayout(int seed, int themeId, IReadOnlyList<TotemMapTemplateDefinition> templates)
    {
        var rng = new System.Random(seed);
        var template = ResolveTemplate(themeId, templates);
        float mapSize = template.MapSize;
        float third = mapSize / 3f;
        float zoneX = (float)rng.NextDouble() * third + third;
        float zoneY = (float)rng.NextDouble() * third + third;
        float roomFootprint = Mathf.Max(30f, template.MinRoomSize * 2f);

        if (TryBuildPcgLayout(seed, template, zoneX, zoneY, roomFootprint, out var pcgMap, out string pcgError))
        {
            return pcgMap;
        }

        GFTrace.Warning("TotemMap", "PCG.FallbackToLegacyLayout", null, GFTrace.Data("error", pcgError ?? string.Empty));

        var rooms = BuildThemeRooms(template.Id, mapSize, roomFootprint);
        var terrainGrid = BuildTerrainGrid(template.Id, mapSize, rooms, out int groundCount, out int slowCount, out int blockedCount, out int coverCount, out int hazardCount);

        var map = new TotemMapSnapshot
        {
            Seed = seed,
            ThemeId = template.Id,
            ThemeName = template.ThemeName,
            MapSize = mapSize,
            MinRoomSize = template.MinRoomSize,
            BspMaxDepth = template.BspMaxDepth,
            TerrainPoolId = template.TerrainPoolId,
            PrefabPath = template.PrefabPath,
            HudAccentColor = template.HudAccentColor,
            DominantColor = template.DominantColor,
            InitialZoneCenter = new Vector2(zoneX, zoneY),
            Rooms = rooms,
            TerrainCellSize = TotemMapService.TerrainCellSize,
            TerrainGridWidth = TerrainGridResolution,
            TerrainGridHeight = TerrainGridResolution,
            TerrainGrid = terrainGrid,
            GroundCellCount = groundCount,
            SlowCellCount = slowCount,
            BlockedCellCount = blockedCount,
            CoverCellCount = coverCount,
            HazardCellCount = hazardCount,
        };

        map.AnchorPlacements = BuildAnchorPlacements(map);
        return map;
    }

    private static bool TryBuildPcgLayout(
        int seed,
        TotemMapTemplateDefinition template,
        float initialZoneX,
        float initialZoneY,
        float roomFootprint,
        out TotemMapSnapshot map,
        out string error)
    {
        map = null;
        error = string.Empty;
        if (template == null)
        {
            error = "Missing map template.";
            return false;
        }

        var settings = GetPcgRuntimeSettings();
        string cacheKey = BuildPcgCacheKey(seed, template, settings);
        if (pcgSnapshotCache.TryGetValue(cacheKey, out var cached))
        {
            map = CloneMapSnapshot(cached);
            return true;
        }

        try
        {
            if (!TryGetPcgAssetIndex(out var assetIndex, out error))
            {
                return false;
            }

            int pcgSeed = unchecked(seed * 1009 + template.Id * 9176);
            var generator = new PCGMapGenerator(assetIndex);
            var pcgMap = generator.Generate(new PCGMapGenerateRequest
            {
                Seed = pcgSeed,
                Width = settings.Width,
                Height = settings.Height,
                ObjectBudget = settings.ObjectBudget,
                StampBudget = settings.StampBudget,
                DecalBudget = settings.DecalBudget,
                EdgeMatchTolerance = PcgEdgeMatchTolerance,
                TeamSpawnZoneWeight = 100,
                LootZoneWeight = 100,
                CombatZoneWeight = 100,
                DangerZoneWeight = 100,
            });

            if (pcgMap == null || pcgMap.Cells == null || pcgMap.Cells.Length <= 0)
            {
                error = "PCG generator returned an empty map.";
                return false;
            }

            var rooms = BuildPcgRooms(pcgMap, template.MapSize, roomFootprint);
            var terrainGrid = BuildTerrainGridFromPcg(pcgMap, template.MapSize, out int groundCount, out int slowCount, out int blockedCount, out int coverCount, out int hazardCount);
            map = new TotemMapSnapshot
            {
                Seed = seed,
                ThemeId = template.Id,
                ThemeName = template.ThemeName,
                MapSize = template.MapSize,
                MinRoomSize = template.MinRoomSize,
                BspMaxDepth = template.BspMaxDepth,
                TerrainPoolId = template.TerrainPoolId,
                PrefabPath = template.PrefabPath,
                HudAccentColor = template.HudAccentColor,
                DominantColor = template.DominantColor,
                InitialZoneCenter = new Vector2(initialZoneX, initialZoneY),
                Rooms = rooms,
                TerrainCellSize = TerrainCellSize,
                TerrainGridWidth = TerrainGridResolution,
                TerrainGridHeight = TerrainGridResolution,
                TerrainGrid = terrainGrid,
                GroundCellCount = groundCount,
                SlowCellCount = slowCount,
                BlockedCellCount = blockedCount,
                CoverCellCount = coverCount,
                HazardCellCount = hazardCount,
                IsPcgGenerated = true,
                PcgWidth = pcgMap.Width,
                PcgHeight = pcgMap.Height,
                PcgVisualCount = pcgMap.Visuals.Count,
                PcgReachableCells = pcgMap.Validation?.ReachableCells ?? 0,
                PcgUnreachableCells = pcgMap.Validation?.UnreachableCells ?? 0,
                PcgContentHash = pcgMap.ContentHash,
                PcgValidationSummary = BuildPcgValidationSummary(pcgMap),
                PcgMapData = pcgMap,
            };

            map.AnchorPlacements = BuildAnchorPlacements(map);
            pcgSnapshotCache[cacheKey] = CloneMapSnapshot(map);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string BuildPcgCacheKey(int seed, TotemMapTemplateDefinition template, PcgRuntimeSettings settings)
    {
        return $"{pcgRuntimeProfile}|{settings.Width}x{settings.Height}|{settings.ObjectBudget}|{settings.StampBudget}|{settings.DecalBudget}|{seed}|{template.Id}|{template.MapSize:0.###}|{template.MinRoomSize:0.###}|{template.BspMaxDepth}|{template.TerrainPoolId}|{template.ThemeName}";
    }

    private static TotemMapSnapshot CloneMapSnapshot(TotemMapSnapshot source)
    {
        if (source == null)
        {
            return null;
        }

        return new TotemMapSnapshot
        {
            Seed = source.Seed,
            ThemeId = source.ThemeId,
            ThemeName = source.ThemeName,
            MapSize = source.MapSize,
            MinRoomSize = source.MinRoomSize,
            BspMaxDepth = source.BspMaxDepth,
            TerrainPoolId = source.TerrainPoolId,
            PrefabPath = source.PrefabPath,
            HudAccentColor = source.HudAccentColor,
            DominantColor = source.DominantColor,
            InitialZoneCenter = source.InitialZoneCenter,
            Rooms = CloneRooms(source.Rooms),
            AnchorPlacements = CloneAnchors(source.AnchorPlacements),
            TerrainCellSize = source.TerrainCellSize,
            TerrainGridWidth = source.TerrainGridWidth,
            TerrainGridHeight = source.TerrainGridHeight,
            TerrainGrid = source.TerrainGrid == null ? Array.Empty<byte>() : (byte[])source.TerrainGrid.Clone(),
            GroundCellCount = source.GroundCellCount,
            SlowCellCount = source.SlowCellCount,
            BlockedCellCount = source.BlockedCellCount,
            CoverCellCount = source.CoverCellCount,
            HazardCellCount = source.HazardCellCount,
            IsPcgGenerated = source.IsPcgGenerated,
            PcgWidth = source.PcgWidth,
            PcgHeight = source.PcgHeight,
            PcgVisualCount = source.PcgVisualCount,
            PcgReachableCells = source.PcgReachableCells,
            PcgUnreachableCells = source.PcgUnreachableCells,
            PcgContentHash = source.PcgContentHash,
            PcgValidationSummary = source.PcgValidationSummary,
            PcgMapData = source.PcgMapData,
        };
    }

    private static TotemRoomInfo[] CloneRooms(TotemRoomInfo[] rooms)
    {
        if (rooms == null || rooms.Length <= 0)
        {
            return Array.Empty<TotemRoomInfo>();
        }

        var clone = new TotemRoomInfo[rooms.Length];
        for (int i = 0; i < rooms.Length; i++)
        {
            var room = rooms[i];
            if (room == null)
            {
                continue;
            }

            clone[i] = new TotemRoomInfo
            {
                RoomId = room.RoomId,
                Label = room.Label,
                RoomType = room.RoomType,
                Bounds = room.Bounds,
                CenterWorld = room.CenterWorld,
                Footprint = room.Footprint,
            };
        }

        return clone;
    }

    private static TotemMapAnchor[] CloneAnchors(TotemMapAnchor[] anchors)
    {
        if (anchors == null || anchors.Length <= 0)
        {
            return Array.Empty<TotemMapAnchor>();
        }

        var clone = new TotemMapAnchor[anchors.Length];
        for (int i = 0; i < anchors.Length; i++)
        {
            var anchor = anchors[i];
            if (anchor == null)
            {
                continue;
            }

            clone[i] = new TotemMapAnchor
            {
                AnchorId = anchor.AnchorId,
                Kind = anchor.Kind,
                RoomType = anchor.RoomType,
                Position = anchor.Position,
                Order = anchor.Order,
                PayloadId = anchor.PayloadId,
            };
        }

        return clone;
    }

    private static bool TryGetPcgAssetIndex(out PCGAssetIndex index, out string error)
    {
        index = cachedPcgAssetIndex;
        error = string.Empty;
        if (index != null)
        {
            return true;
        }

        try
        {
            cachedPcgAssetIndex = PCGAssetIndex.LoadFromResources();
            cachedPcgAssetIndexError = string.Empty;
            index = cachedPcgAssetIndex;
            return index != null;
        }
        catch (Exception ex)
        {
            cachedPcgAssetIndexError = ex.Message;
            error = cachedPcgAssetIndexError;
            index = null;
            return false;
        }
    }

    private static TotemRoomInfo[] BuildPcgRooms(PCGMapData pcgMap, float mapSize, float roomFootprint)
    {
        return new[]
        {
            CreateRoom(0, "PCG_TeamSpawn", TotemRoomType.SpawnRoom, FindPcgZoneWorldPosition(pcgMap, mapSize, "team_spawn", new Vector2(0.18f, 0.18f)), roomFootprint),
            CreateRoom(1, "PCG_TattooStudio", TotemRoomType.TattooStudio, FindPcgZoneWorldPosition(pcgMap, mapSize, "loot_zone", new Vector2(0.28f, 0.72f)), roomFootprint),
            CreateRoom(2, "PCG_Merchant", TotemRoomType.Merchant, FindPcgZoneWorldPosition(pcgMap, mapSize, "combat_zone", new Vector2(0.68f, 0.52f)), roomFootprint),
            CreateRoom(3, "PCG_BossRoom", TotemRoomType.BossRoom, FindPcgZoneWorldPosition(pcgMap, mapSize, "danger_zone", new Vector2(0.80f, 0.80f)), roomFootprint),
        };
    }

    private static Vector2 FindPcgZoneWorldPosition(PCGMapData pcgMap, float mapSize, string zoneId, Vector2 normalizedTarget)
    {
        int bestX = -1;
        int bestY = -1;
        float bestScore = float.MaxValue;
        float targetX = normalizedTarget.x * pcgMap.Width;
        float targetY = normalizedTarget.y * pcgMap.Height;

        for (int y = 0; y < pcgMap.Height; y++)
        {
            for (int x = 0; x < pcgMap.Width; x++)
            {
                var cell = pcgMap.GetCell(x, y);
                if (!cell.Walkable || cell.Occupied || !string.Equals(cell.ZoneId, zoneId, StringComparison.Ordinal))
                {
                    continue;
                }

                float dx = x - targetX;
                float dy = y - targetY;
                float score = dx * dx + dy * dy;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestX = x;
                    bestY = y;
                }
            }
        }

        if (bestX < 0)
        {
            bestX = Mathf.Clamp(Mathf.RoundToInt(targetX), 1, Math.Max(1, pcgMap.Width - 2));
            bestY = Mathf.Clamp(Mathf.RoundToInt(targetY), 1, Math.Max(1, pcgMap.Height - 2));
        }

        return new Vector2(
            (bestX + 0.5f) / Mathf.Max(1, pcgMap.Width) * mapSize,
            (bestY + 0.5f) / Mathf.Max(1, pcgMap.Height) * mapSize);
    }

    private static byte[] BuildTerrainGridFromPcg(
        PCGMapData pcgMap,
        float mapSize,
        out int groundCount,
        out int slowCount,
        out int blockedCount,
        out int coverCount,
        out int hazardCount)
    {
        var grid = new byte[TerrainGridResolution * TerrainGridResolution];
        float worldCellSize = mapSize / TerrainGridResolution;
        for (int z = 0; z < TerrainGridResolution; z++)
        {
            float worldZ = (z + 0.5f) * worldCellSize;
            int pcgY = Mathf.Clamp(Mathf.FloorToInt(worldZ / mapSize * pcgMap.Height), 0, pcgMap.Height - 1);
            for (int x = 0; x < TerrainGridResolution; x++)
            {
                float worldX = (x + 0.5f) * worldCellSize;
                int pcgX = Mathf.Clamp(Mathf.FloorToInt(worldX / mapSize * pcgMap.Width), 0, pcgMap.Width - 1);
                var cell = pcgMap.GetCell(pcgX, pcgY);
                grid[z * TerrainGridResolution + x] = (byte)ResolvePcgTerrainType(cell, pcgMap.Seed);
            }
        }

        CountTerrainCells(grid, out groundCount, out slowCount, out blockedCount, out coverCount, out hazardCount);
        return grid;
    }

    private static TotemTerrainType ResolvePcgTerrainType(PCGMapCell cell, int seed)
    {
        if (!cell.Walkable || string.Equals(cell.Terrain, "water", StringComparison.Ordinal))
        {
            return TotemTerrainType.Blocked;
        }

        if (cell.Occupied)
        {
            return TotemTerrainType.Blocked;
        }

        if (string.Equals(cell.ZoneId, "danger_zone", StringComparison.Ordinal) && IsPcgHazardCell(cell.X, cell.Y, seed))
        {
            return TotemTerrainType.Hazard;
        }

        switch (cell.Terrain)
        {
            case "mud":
                return TotemTerrainType.Slow;
            case "forest_ground":
                return TotemTerrainType.Cover;
            case "corruption":
                return TotemTerrainType.Hazard;
            default:
                return TotemTerrainType.Ground;
        }
    }

    private static bool IsPcgHazardCell(int x, int y, int seed)
    {
        unchecked
        {
            int hash = seed;
            hash = hash * 397 ^ x * 73856093;
            hash = hash * 397 ^ y * 19349663;
            return (hash & 0x0F) == 0;
        }
    }

    private static string BuildPcgValidationSummary(PCGMapData pcgMap)
    {
        var report = pcgMap.Validation;
        if (report == null)
        {
            return "ValidationMissing";
        }

        return $"valid={report.IsValid};walkable={report.WalkableCells};reachable={report.ReachableCells};unreachable={report.UnreachableCells};warnings={report.Warnings.Count}";
    }

    public TotemTerrainType QueryTerrain(Vector3 worldPos)
    {
        return QueryTerrain(CurrentMap, worldPos);
    }

    public bool IsWalkable(Vector3 worldPos)
    {
        if (CurrentMap == null)
        {
            return true;
        }

        return IsTerrainWalkable(QueryTerrain(worldPos));
    }

    public float GetMoveSpeedMultiplier(Vector3 worldPos)
    {
        if (CurrentMap == null)
        {
            return 1f;
        }

        return GetTerrainMoveSpeedMultiplier(QueryTerrain(worldPos));
    }

    public static TotemTerrainType QueryTerrain(TotemMapSnapshot map, Vector3 worldPos)
    {
        if (map == null || map.MapSize <= 0f)
        {
            return TotemTerrainType.Blocked;
        }

        if (worldPos.x < 0f || worldPos.z < 0f || worldPos.x >= map.MapSize || worldPos.z >= map.MapSize)
        {
            return TotemTerrainType.Blocked;
        }

        var grid = map.TerrainGrid;
        if (grid == null || grid.Length <= 0 || map.TerrainGridWidth <= 0 || map.TerrainGridHeight <= 0 || map.TerrainCellSize <= 0)
        {
            return TotemTerrainType.Ground;
        }

        int x = Mathf.FloorToInt(worldPos.x / map.TerrainCellSize);
        int z = Mathf.FloorToInt(worldPos.z / map.TerrainCellSize);
        if (x < 0 || z < 0 || x >= map.TerrainGridWidth || z >= map.TerrainGridHeight)
        {
            return TotemTerrainType.Blocked;
        }

        int index = z * map.TerrainGridWidth + x;
        if (index < 0 || index >= grid.Length)
        {
            return TotemTerrainType.Blocked;
        }

        return NormalizeTerrainType((TotemTerrainType)grid[index]);
    }

    public static TotemMapAnchor FindAnchor(TotemMapSnapshot map, TotemMapAnchorKind kind)
    {
        return FindAnchor(map, kind, null, null);
    }

    public static TotemMapAnchor FindAnchor(TotemMapSnapshot map, TotemMapAnchorKind kind, string anchorId, string payloadId)
    {
        var anchors = map?.AnchorPlacements;
        if (anchors == null)
        {
            return null;
        }

        for (int i = 0; i < anchors.Length; i++)
        {
            var anchor = anchors[i];
            if (anchor == null || anchor.Kind != kind)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(anchorId) && !string.Equals(anchor.AnchorId, anchorId, StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(payloadId) && !string.Equals(anchor.PayloadId, payloadId, StringComparison.Ordinal))
            {
                continue;
            }

            return anchor;
        }

        return null;
    }

    public static TotemMapAnchor[] FindAnchors(TotemMapSnapshot map, TotemMapAnchorKind kind)
    {
        var anchors = map?.AnchorPlacements;
        if (anchors == null || anchors.Length <= 0)
        {
            return Array.Empty<TotemMapAnchor>();
        }

        var result = new List<TotemMapAnchor>(anchors.Length);
        for (int i = 0; i < anchors.Length; i++)
        {
            var anchor = anchors[i];
            if (anchor != null && anchor.Kind == kind)
            {
                result.Add(anchor);
            }
        }

        return result.ToArray();
    }

    public static Vector3 ResolveAnchorPosition(TotemMapSnapshot map, TotemMapAnchorKind kind, Vector3 fallback)
    {
        return ResolveAnchorPosition(map, kind, fallback, null, null);
    }

    public static Vector3 ResolveAnchorPosition(TotemMapSnapshot map, TotemMapAnchorKind kind, Vector3 fallback, string anchorId, string payloadId)
    {
        var anchor = FindAnchor(map, kind, anchorId, payloadId);
        return anchor == null ? fallback : anchor.Position;
    }

    public static bool IsTerrainWalkable(TotemTerrainType terrainType)
    {
        return terrainType == TotemTerrainType.Ground
            || terrainType == TotemTerrainType.Slow
            || terrainType == TotemTerrainType.Cover
            || terrainType == TotemTerrainType.Hazard;
    }

    public static float GetTerrainMoveSpeedMultiplier(TotemTerrainType terrainType)
    {
        return terrainType == TotemTerrainType.Slow ? 0.65f : 1f;
    }

    public static float GetTerrainHazardDps(TotemTerrainType terrainType)
    {
        return terrainType == TotemTerrainType.Hazard ? 4f : 0f;
    }

    private static TotemTerrainType NormalizeTerrainType(TotemTerrainType terrainType)
    {
        switch (terrainType)
        {
            case TotemTerrainType.Ground:
            case TotemTerrainType.Slow:
            case TotemTerrainType.Blocked:
            case TotemTerrainType.Cover:
            case TotemTerrainType.Hazard:
                return terrainType;
            default:
                return TotemTerrainType.Blocked;
        }
    }

    private static TotemMapAnchor[] BuildAnchorPlacements(TotemMapSnapshot map)
    {
        if (map == null)
        {
            return Array.Empty<TotemMapAnchor>();
        }

        var anchors = new List<TotemMapAnchor>(16);
        var rng = new System.Random(unchecked(map.Seed * 1009 + map.ThemeId * 9176));

        AddAnchor(anchors, map, rng, "player.spawn", TotemMapAnchorKind.PlayerSpawn, TotemRoomType.SpawnRoom, Vector3.zero, string.Empty);
        AddAnchor(anchors, map, rng, "boss.spawn", TotemMapAnchorKind.BossSpawn, TotemRoomType.BossRoom, Vector3.zero, string.Empty);
        AddAnchor(anchors, map, rng, "npc.tattooist.base", TotemMapAnchorKind.Tattooist, TotemRoomType.TattooStudio, Vector3.zero, "tattooist");
        AddAnchor(anchors, map, rng, "npc.merchant.base", TotemMapAnchorKind.Merchant, TotemRoomType.Merchant, Vector3.zero, "merchant");
        AddAnchor(anchors, map, rng, "chest.common.spawn", TotemMapAnchorKind.Chest, TotemRoomType.SpawnRoom, new Vector3(6f, 0f, 5f), "chest_common");
        AddAnchor(anchors, map, rng, "chest.common.tattoo", TotemMapAnchorKind.Chest, TotemRoomType.TattooStudio, new Vector3(-5f, 0f, -5f), "chest_common");
        AddAnchor(anchors, map, rng, "chest.rare.merchant", TotemMapAnchorKind.Chest, TotemRoomType.Merchant, new Vector3(5f, 0f, -5f), "chest_rare");
        AddAnchor(anchors, map, rng, "chest.rare.boss", TotemMapAnchorKind.Chest, TotemRoomType.BossRoom, new Vector3(-6f, 0f, 6f), "chest_rare");
        AddAnchor(anchors, map, rng, "enemy.spawn.inner", TotemMapAnchorKind.EnemySpawn, TotemRoomType.SpawnRoom, new Vector3(0f, 0f, 8f), "inner");
        AddAnchor(anchors, map, rng, "enemy.spawn.mid", TotemMapAnchorKind.EnemySpawn, TotemRoomType.SpawnRoom, new Vector3(0f, 0f, 13f), "mid");
        AddAnchor(anchors, map, rng, "enemy.spawn.outer", TotemMapAnchorKind.EnemySpawn, TotemRoomType.SpawnRoom, new Vector3(0f, 0f, 18f), "outer");
        AddAnchor(anchors, map, rng, "resource.weapon.spawn", TotemMapAnchorKind.Resource, TotemRoomType.SpawnRoom, new Vector3(18f, 0f, -12f), "pistol_basic");
        AddAnchor(anchors, map, rng, "resource.weapon.merchant", TotemMapAnchorKind.Resource, TotemRoomType.Merchant, new Vector3(-14f, 0f, 12f), "hammer_heavy");
        AddAnchor(anchors, map, rng, "resource.weapon.tattoo", TotemMapAnchorKind.Resource, TotemRoomType.TattooStudio, new Vector3(14f, 0f, -10f), "bow_charge");
        AddAnchor(anchors, map, rng, "event.choice.altar", TotemMapAnchorKind.Event, TotemRoomType.TattooStudio, new Vector3(-14f, 0f, 10f), "event_choice_001");
        AddAnchor(anchors, map, rng, "event.choice.forge", TotemMapAnchorKind.Event, TotemRoomType.Merchant, new Vector3(14f, 0f, 10f), "event_choice_002");

        return anchors.ToArray();
    }

    private static void AddAnchor(
        List<TotemMapAnchor> anchors,
        TotemMapSnapshot map,
        System.Random rng,
        string anchorId,
        TotemMapAnchorKind kind,
        TotemRoomType roomType,
        Vector3 offset,
        string payloadId)
    {
        var room = FindRoom(map, roomType);
        Vector3 basePosition = room?.CenterWorld ?? FallbackRoomCenter(roomType);
        Vector3 jitter = BuildAnchorJitter(rng, kind);
        Vector3 position = basePosition + offset + jitter;
        position.y = 0.5f;
        position = ResolveWalkableAnchorPosition(map, room, position, basePosition);
        anchors.Add(new TotemMapAnchor
        {
            AnchorId = anchorId ?? string.Empty,
            Kind = kind,
            RoomType = roomType,
            Position = position,
            Order = anchors.Count,
            PayloadId = payloadId ?? string.Empty,
        });
    }

    private static Vector3 BuildAnchorJitter(System.Random rng, TotemMapAnchorKind kind)
    {
        if (rng == null || kind == TotemMapAnchorKind.PlayerSpawn || kind == TotemMapAnchorKind.BossSpawn)
        {
            return Vector3.zero;
        }

        float radius = kind == TotemMapAnchorKind.Chest ? 2.25f : 1.25f;
        float x = ((float)rng.NextDouble() * 2f - 1f) * radius;
        float z = ((float)rng.NextDouble() * 2f - 1f) * radius;
        return new Vector3(x, 0f, z);
    }

    private static Vector3 ResolveWalkableAnchorPosition(TotemMapSnapshot map, TotemRoomInfo room, Vector3 preferred, Vector3 fallback)
    {
        preferred = ClampToMap(preferred, map);
        if (IsGroundTerrain(QueryTerrain(map, preferred)))
        {
            return preferred;
        }

        if (TryFindNearbyAnchorPosition(map, preferred, requireGround: true, out var groundNearPreferred))
        {
            return groundNearPreferred;
        }

        Vector3 walkableFallback = default;
        bool hasWalkableFallback = false;
        if (IsTerrainWalkable(QueryTerrain(map, preferred)))
        {
            walkableFallback = preferred;
            hasWalkableFallback = true;
        }

        Vector3 roomCenter = room?.CenterWorld ?? fallback;
        roomCenter.y = 0.5f;
        roomCenter = ClampToMap(roomCenter, map);
        if (IsGroundTerrain(QueryTerrain(map, roomCenter)))
        {
            return roomCenter;
        }

        if (TryFindNearbyAnchorPosition(map, roomCenter, requireGround: true, out var groundNearRoom))
        {
            return groundNearRoom;
        }

        if (hasWalkableFallback)
        {
            return walkableFallback;
        }

        if (IsTerrainWalkable(QueryTerrain(map, roomCenter)))
        {
            return roomCenter;
        }

        if (TryFindNearbyAnchorPosition(map, preferred, requireGround: false, out var walkableNearPreferred))
        {
            return walkableNearPreferred;
        }

        if (TryFindNearbyAnchorPosition(map, roomCenter, requireGround: false, out var walkableNearRoom))
        {
            return walkableNearRoom;
        }

        return roomCenter;
    }

    private static bool TryFindNearbyAnchorPosition(TotemMapSnapshot map, Vector3 origin, bool requireGround, out Vector3 position)
    {
        position = default;
        origin = ClampToMap(origin, map);
        float step = Mathf.Max(1f, map?.TerrainCellSize ?? TerrainCellSize);
        for (int radius = 1; radius <= 8; radius++)
        {
            for (int dz = -radius; dz <= radius; dz++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dz) != radius)
                    {
                        continue;
                    }

                    var candidate = ClampToMap(origin + new Vector3(dx * step, 0f, dz * step), map);
                    candidate.y = 0.5f;
                    var terrain = QueryTerrain(map, candidate);
                    if (requireGround ? IsGroundTerrain(terrain) : IsTerrainWalkable(terrain))
                    {
                        position = candidate;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool IsGroundTerrain(TotemTerrainType terrainType)
    {
        return terrainType == TotemTerrainType.Ground;
    }

    private static Vector3 ClampToMap(Vector3 position, TotemMapSnapshot map)
    {
        if (map == null || map.MapSize <= 0f)
        {
            return position;
        }

        float margin = Mathf.Max(0.5f, map.TerrainCellSize * 0.5f);
        position.x = Mathf.Clamp(position.x, margin, map.MapSize - margin);
        position.z = Mathf.Clamp(position.z, margin, map.MapSize - margin);
        return position;
    }

    private static TotemRoomInfo FindRoom(TotemMapSnapshot map, TotemRoomType roomType)
    {
        var rooms = map?.Rooms;
        for (int i = 0; rooms != null && i < rooms.Length; i++)
        {
            if (rooms[i] != null && rooms[i].RoomType == roomType)
            {
                return rooms[i];
            }
        }

        return null;
    }

    private static Vector3 FallbackRoomCenter(TotemRoomType roomType)
    {
        switch (roomType)
        {
            case TotemRoomType.TattooStudio:
                return new Vector3(94f, 0f, 314f);
            case TotemRoomType.Merchant:
                return new Vector3(308f, 0f, 300f);
            case TotemRoomType.BossRoom:
                return new Vector3(324f, 0f, 82f);
            default:
                return new Vector3(82f, 0f, 82f);
        }
    }

    private static TotemRoomInfo[] BuildThemeRooms(int themeId, float mapSize, float roomFootprint)
    {
        if (themeId == 2)
        {
            return new[]
            {
                CreateRoom(0, "SpawnCavity", TotemRoomType.SpawnRoom, new Vector2(72f, 84f), roomFootprint),
                CreateRoom(1, "TattooSpore", TotemRoomType.TattooStudio, new Vector2(118f, 308f), roomFootprint),
                CreateRoom(2, "TraderRib", TotemRoomType.Merchant, new Vector2(316f, 266f), roomFootprint),
                CreateRoom(3, "QueenCore", TotemRoomType.BossRoom, new Vector2(312f, 78f), roomFootprint),
            };
        }

        if (themeId == 3)
        {
            return new[]
            {
                CreateRoom(0, "CleanDock", TotemRoomType.SpawnRoom, new Vector2(76f, 72f), roomFootprint),
                CreateRoom(1, "TattooLab", TotemRoomType.TattooStudio, new Vector2(96f, 286f), roomFootprint),
                CreateRoom(2, "SupplyTower", TotemRoomType.Merchant, new Vector2(292f, 318f), roomFootprint),
                CreateRoom(3, "PatientZero", TotemRoomType.BossRoom, new Vector2(326f, 104f), roomFootprint),
            };
        }

        return new[]
        {
            CreateRoom(0, "SpawnRoom", TotemRoomType.SpawnRoom, new Vector2(82f, 82f), roomFootprint),
            CreateRoom(1, "TattooStudio", TotemRoomType.TattooStudio, new Vector2(94f, 314f), roomFootprint),
            CreateRoom(2, "Merchant", TotemRoomType.Merchant, new Vector2(308f, 300f), roomFootprint),
            CreateRoom(3, "BossRoom", TotemRoomType.BossRoom, new Vector2(324f, 82f), roomFootprint),
        };
    }

    private static byte[] BuildTerrainGrid(
        int themeId,
        float mapSize,
        TotemRoomInfo[] rooms,
        out int groundCount,
        out int slowCount,
        out int blockedCount,
        out int coverCount,
        out int hazardCount)
    {
        var grid = new byte[TerrainGridResolution * TerrainGridResolution];
        float cellSize = mapSize / TerrainGridResolution;
        for (int z = 0; z < TerrainGridResolution; z++)
        {
            float worldZ = (z + 0.5f) * cellSize;
            for (int x = 0; x < TerrainGridResolution; x++)
            {
                float worldX = (x + 0.5f) * cellSize;
                grid[z * TerrainGridResolution + x] = (byte)ResolveThemeTerrain(themeId, worldX, worldZ, mapSize);
            }
        }

        StampRoomClearings(grid, rooms, cellSize);
        CountTerrainCells(grid, out groundCount, out slowCount, out blockedCount, out coverCount, out hazardCount);
        return grid;
    }

    private static TotemTerrainType ResolveThemeTerrain(int themeId, float x, float z, float mapSize)
    {
        if (themeId == 2)
        {
            return ResolveAlienHiveTerrain(x, z, mapSize);
        }

        if (themeId == 3)
        {
            return ResolveVirusSwampTerrain(x, z, mapSize);
        }

        return ResolveAIRuinsTerrain(x, z, mapSize);
    }

    private static TotemTerrainType ResolveAIRuinsTerrain(float x, float z, float mapSize)
    {
        var terrain = TotemTerrainType.Ground;
        if (x > 44f && x < 148f && z > 156f && z < 252f)
        {
            terrain = TotemTerrainType.Slow;
        }

        if ((x > 228f && x < 360f && z > 250f && z < 340f) || (x > 150f && x < 250f && Mathf.Abs(z - 200f) < 10f))
        {
            terrain = TotemTerrainType.Cover;
        }

        if (DistanceSqr(x, z, 274f, 192f) < 42f * 42f || DistanceSqr(x, z, 238f, 118f) < 24f * 24f)
        {
            terrain = TotemTerrainType.Hazard;
        }

        if (Mathf.Abs(x - 200f) < 10f && (z < 148f || z > 252f))
        {
            terrain = TotemTerrainType.Blocked;
        }

        return terrain;
    }

    private static TotemTerrainType ResolveAlienHiveTerrain(float x, float z, float mapSize)
    {
        var terrain = TotemTerrainType.Ground;
        float ribCenter = 92f + z * 0.52f;
        if (Mathf.Abs(x - ribCenter) < 11f && z > 64f && z < 334f)
        {
            terrain = TotemTerrainType.Blocked;
        }

        if ((x > 52f && x < 164f && z > 184f && z < 258f) || DistanceSqr(x, z, 226f, 326f) < 44f * 44f)
        {
            terrain = TotemTerrainType.Slow;
        }

        if (DistanceSqr(x, z, 284f, 224f) < 46f * 46f || DistanceSqr(x, z, 142f, 330f) < 28f * 28f)
        {
            terrain = TotemTerrainType.Hazard;
        }

        if (x > 236f && x < 360f && z > 80f && z < 158f)
        {
            terrain = TotemTerrainType.Cover;
        }

        return terrain;
    }

    private static TotemTerrainType ResolveVirusSwampTerrain(float x, float z, float mapSize)
    {
        var terrain = TotemTerrainType.Ground;
        if ((z > 170f && z < 232f && x > 42f && x < 354f) || DistanceSqr(x, z, 132f, 292f) < 50f * 50f)
        {
            terrain = TotemTerrainType.Slow;
        }

        if (DistanceSqr(x, z, 258f, 140f) < 48f * 48f || DistanceSqr(x, z, 306f, 278f) < 34f * 34f)
        {
            terrain = TotemTerrainType.Hazard;
        }

        if ((Mathf.Abs(z - (70f + x * 0.45f)) < 12f && x > 70f && x < 336f) || (x > 184f && x < 214f && z > 258f && z < 384f))
        {
            terrain = TotemTerrainType.Blocked;
        }

        if (x > 56f && x < 144f && z > 104f && z < 166f)
        {
            terrain = TotemTerrainType.Cover;
        }

        return terrain;
    }

    private static void StampRoomClearings(byte[] grid, TotemRoomInfo[] rooms, float cellSize)
    {
        if (grid == null || rooms == null)
        {
            return;
        }

        for (int z = 0; z < TerrainGridResolution; z++)
        {
            float worldZ = (z + 0.5f) * cellSize;
            for (int x = 0; x < TerrainGridResolution; x++)
            {
                float worldX = (x + 0.5f) * cellSize;
                if (IsInsideAnyRoom(rooms, worldX, worldZ))
                {
                    grid[z * TerrainGridResolution + x] = (byte)TotemTerrainType.Ground;
                }
            }
        }
    }

    private static bool IsInsideAnyRoom(TotemRoomInfo[] rooms, float worldX, float worldZ)
    {
        for (int i = 0; i < rooms.Length; i++)
        {
            var room = rooms[i];
            if (room != null && room.Bounds.Contains(new Vector2(worldX, worldZ)))
            {
                return true;
            }
        }

        return false;
    }

    private static void CountTerrainCells(byte[] grid, out int groundCount, out int slowCount, out int blockedCount, out int coverCount, out int hazardCount)
    {
        groundCount = 0;
        slowCount = 0;
        blockedCount = 0;
        coverCount = 0;
        hazardCount = 0;
        for (int i = 0; i < grid.Length; i++)
        {
            switch (NormalizeTerrainType((TotemTerrainType)grid[i]))
            {
                case TotemTerrainType.Ground:
                    groundCount++;
                    break;
                case TotemTerrainType.Slow:
                    slowCount++;
                    break;
                case TotemTerrainType.Blocked:
                    blockedCount++;
                    break;
                case TotemTerrainType.Cover:
                    coverCount++;
                    break;
                case TotemTerrainType.Hazard:
                    hazardCount++;
                    break;
            }
        }
    }

    private static float DistanceSqr(float x, float z, float centerX, float centerZ)
    {
        float dx = x - centerX;
        float dz = z - centerZ;
        return dx * dx + dz * dz;
    }

    private static TotemMapTemplateDefinition ResolveTemplate(int themeId, IReadOnlyList<TotemMapTemplateDefinition> templates)
    {
        var source = templates == null || templates.Count <= 0 ? LoadTemplates() : templates;
        for (int i = 0; i < source.Count; i++)
        {
            var item = source[i];
            if (item != null && item.Id == themeId)
            {
                return item;
            }
        }

        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] != null)
            {
                return source[i];
            }
        }

        return new TotemMapTemplateDefinition
        {
            Id = 1,
            ThemeName = "AI_RUINS",
            MapSize = DefaultMapSize,
            MinRoomSize = 40f,
            BspMaxDepth = 4,
            TerrainPoolId = 101,
            PrefabPath = string.Empty,
            HudAccentColor = "#66CCFF",
            DominantColor = "#3A4858",
        };
    }

    private static TotemMapTemplateDefinition[] LoadTemplates()
    {
        return NonEmpty(
            TotemDataService.LoadGameplayCatalogOrDefault().CreateMapTemplates(),
            Array.Empty<TotemMapTemplateDefinition>());
    }

    private static T[] NonEmpty<T>(T[] primary, T[] fallback)
    {
        return primary == null || primary.Length <= 0 ? fallback : primary;
    }

    public TotemRoomInfo GetRoom(TotemRoomType roomType)
    {
        var rooms = CurrentMap?.Rooms;
        if (rooms == null)
        {
            return null;
        }

        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i].RoomType == roomType)
            {
                return rooms[i];
            }
        }

        return null;
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            GenerateMap(seed: 1, themeId: 1, createObjects: true);
            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            ClearRuntimeMap();
            GFTrace.Info("TotemMap", "Map.Cleared", null, GFTrace.Data("nextState", nextState.ToString()));
        }
    }

    private void ClearRuntimeMap()
    {
        DestroySpawnedObjects();
        CurrentMap = null;
    }

    private static TotemRoomInfo CreateRoom(int roomId, string label, TotemRoomType roomType, Vector2 center, float footprint)
    {
        return new TotemRoomInfo
        {
            RoomId = roomId,
            Label = label,
            RoomType = roomType,
            CenterWorld = new Vector3(center.x, 0f, center.y),
            Bounds = new Rect(center.x - footprint * 0.5f, center.y - footprint * 0.5f, footprint, footprint),
            Footprint = footprint,
        };
    }

    private void CreateMapObjects(TotemMapSnapshot map)
    {
        ResetRuntimeVisualCounters();
        mapRoot = new GameObject("[TotemMap]");
        spawnedObjects.Add(mapRoot);

        if (map.PcgMapData != null)
        {
            CreatePcgMapObjects(map);
            CreateBoundaryWalls(map.MapSize);
            return;
        }

        float planeScale = map.MapSize / 10f;
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "TotemMap_Ground";
        ground.transform.SetParent(mapRoot.transform, false);
        ground.transform.position = new Vector3(map.MapSize * 0.5f, 0f, map.MapSize * 0.5f);
        ground.transform.localScale = new Vector3(planeScale, 1f, planeScale);
        groundObjectCount++;
        ApplyVisualMaterial(ground, ResolveFloorAssetKey(TotemRoomType.SpawnRoom), new Color(0.28f, 0.30f, 0.34f));

        CreateBoundaryWalls(map.MapSize);
        for (int i = 0; i < map.Rooms.Length; i++)
        {
            CreateRoomMarker(map.Rooms[i], GetRoomColor(map.Rooms[i].RoomType));
        }
    }

    private void CreatePcgMapObjects(TotemMapSnapshot map)
    {
        var pcgMap = map.PcgMapData;
        var settings = GetPcgRuntimeSettings();
        float cellSize = map.MapSize / Mathf.Max(1, pcgMap.Width);
        var tileRoot = CreatePcgTileRoot(cellSize);
        var underlayTilemap = CreatePcgTilemap(tileRoot, "PCG_UnderlayTilemap", -5);
        var groundTilemap = CreatePcgTilemap(tileRoot, "PCG_GroundTilemap", 0);

        for (int y = 0; y < pcgMap.Height; y++)
        {
            for (int x = 0; x < pcgMap.Width; x++)
            {
                var cell = pcgMap.GetCell(x, y);
                if (settings.RenderUnderlay && !string.IsNullOrEmpty(cell.UnderlayAsset))
                {
                    SetPcgTile(underlayTilemap, cell.UnderlayAsset, x, y, 0f, false, false);
                }

                bool useEdgeBase = !string.IsNullOrEmpty(cell.EdgeBaseAsset);
                if (SetPcgTile(
                    groundTilemap,
                    useEdgeBase ? cell.EdgeBaseAsset : cell.BaseAsset,
                    x,
                    y,
                    useEdgeBase ? 0f : cell.BaseRotationDegrees,
                    !useEdgeBase && cell.BaseFlipX,
                    true))
                {
                    pcgCellObjectCount++;
                }
            }
        }

        int visualLimit = Mathf.Min(settings.MaxVisualSprites, pcgMap.Visuals.Count);
        for (int i = 0; i < visualLimit; i++)
        {
            CreatePcgVisualSprite(pcgMap.Visuals[i], cellSize);
        }
    }

    private Transform CreatePcgTileRoot(float cellSize)
    {
        var go = new GameObject("PCG_TileRoot");
        go.transform.SetParent(mapRoot.transform, false);
        go.transform.position = new Vector3(0f, 0.02f, 0f);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        var grid = go.AddComponent<Grid>();
        grid.cellSize = new Vector3(cellSize, cellSize, 1f);
        return go.transform;
    }

    private static Tilemap CreatePcgTilemap(Transform parent, string objectName, int sortingOrder)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        var tilemap = go.AddComponent<Tilemap>();
        tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;
        return tilemap;
    }

    private bool SetPcgTile(Tilemap tilemap, string assetPath, int x, int y, float rotationDegrees, bool flipX, bool countMissing)
    {
        var sprite = GetPcgSprite(assetPath, new Vector2(0.5f, 0.5f), countMissing);
        if (sprite == null)
        {
            return false;
        }

        var tile = GetPcgTile(assetPath, sprite);
        var position = new Vector3Int(x, y, 0);
        tilemap.SetTile(position, tile);

        if (Mathf.Abs(rotationDegrees) > 0.01f || flipX)
        {
            var scale = flipX ? new Vector3(-1f, 1f, 1f) : Vector3.one;
            tilemap.SetTransformMatrix(position, Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, rotationDegrees), scale));
        }

        return true;
    }

    private static Tile GetPcgTile(string assetPath, Sprite sprite)
    {
        string key = assetPath ?? string.Empty;
        if (pcgTileCache.TryGetValue(key, out var tile) && tile != null)
        {
            return tile;
        }

        tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.None;
        pcgTileCache[key] = tile;
        return tile;
    }

    private void CreatePcgVisualSprite(PCGPlacedVisual visual, float cellSize)
    {
        if (visual == null)
        {
            return;
        }

        int sorting = visual.Kind switch
        {
            PCGPlacedVisualKind.TransitionMask => 40,
            PCGPlacedVisualKind.TransitionDetail => 50,
            PCGPlacedVisualKind.Stamp => 20,
            PCGPlacedVisualKind.Decal => 30,
            PCGPlacedVisualKind.Poi => 9000 - visual.Y * 10,
            PCGPlacedVisualKind.Object => 10000 - visual.Y * 10,
            _ => 100,
        };

        if (visual.HasSortingOrder)
        {
            sorting = visual.SortingOrder;
        }

        string safeId = string.IsNullOrEmpty(visual.Id) ? "unnamed" : visual.Id;
        if (visual.Kind == PCGPlacedVisualKind.Object || visual.Kind == PCGPlacedVisualKind.Poi)
        {
            CreatePcgStandingSprite(
                $"PCG_{visual.Kind}_{safeId}_{visual.X}_{visual.Y}",
                visual.Asset,
                visual.X,
                visual.Y,
                Mathf.Max(1, visual.Width),
                cellSize,
                sorting,
                visual.Kind == PCGPlacedVisualKind.Poi ? 1f : 1.35f);
            return;
        }

        float width = Mathf.Max(1, visual.Width);
        float height = Mathf.Max(1, visual.Height);
        CreatePcgGroundSprite(
            $"PCG_{visual.Kind}_{safeId}_{visual.X}_{visual.Y}",
            visual.Asset,
            visual.X + width * 0.5f - 0.5f,
            visual.Y + height * 0.5f - 0.5f,
            width,
            height,
            cellSize,
            sorting,
            visual.RotationDegrees,
            false,
            true);
    }

    private void CreatePcgGroundSprite(
        string objectName,
        string assetPath,
        float cellX,
        float cellY,
        float widthCells,
        float heightCells,
        float cellSize,
        int sortingOrder,
        float rotationDegrees,
        bool flipX,
        bool countAsVisual = false)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(mapRoot.transform, false);
        go.transform.position = new Vector3((cellX + 0.5f) * cellSize, 0.02f, (cellY + 0.5f) * cellSize);
        go.transform.rotation = Quaternion.Euler(90f, 0f, rotationDegrees);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = sortingOrder;
        renderer.flipX = flipX;

        var sprite = GetPcgSprite(assetPath, new Vector2(0.5f, 0.5f), !countAsVisual);
        if (sprite != null)
        {
            renderer.sprite = sprite;
            Vector2 size = sprite.bounds.size;
            if (size.x > 0f && size.y > 0f)
            {
                go.transform.localScale = new Vector3(widthCells * cellSize / size.x, heightCells * cellSize / size.y, 1f);
            }
        }
        else
        {
            if (countAsVisual)
            {
                DestroyObject(go);
                return;
            }

            renderer.sprite = CreateMissingPcgSprite(new Vector2(0.5f, 0.5f));
            renderer.color = Color.magenta;
            go.transform.localScale = new Vector3(widthCells * cellSize, heightCells * cellSize, 1f);
        }

        if (countAsVisual)
        {
            pcgVisualObjectCount++;
        }
        else
        {
            pcgCellObjectCount++;
        }
    }

    private void CreatePcgStandingSprite(
        string objectName,
        string assetPath,
        float cellX,
        float cellY,
        float footprintWidth,
        float cellSize,
        int sortingOrder,
        float scaleMultiplier)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(mapRoot.transform, false);
        go.transform.position = new Vector3((cellX + footprintWidth * 0.5f - 0.5f) * cellSize, 0.08f, cellY * cellSize);
        go.transform.rotation = Quaternion.identity;

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = sortingOrder;

        var sprite = GetPcgSprite(assetPath, new Vector2(0.5f, 0f), true);
        if (sprite != null)
        {
            renderer.sprite = sprite;
            Vector2 size = sprite.bounds.size;
            if (size.x > 0f)
            {
                float scale = footprintWidth * cellSize / size.x * scaleMultiplier;
                go.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }
        else
        {
            renderer.sprite = CreateMissingPcgSprite(new Vector2(0.5f, 0f));
            renderer.color = Color.magenta;
            go.transform.localScale = new Vector3(footprintWidth * cellSize, footprintWidth * cellSize, 1f);
        }

        pcgVisualObjectCount++;
    }

    private Sprite GetPcgSprite(string assetPath, Vector2 pivot, bool countMissing)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            if (countMissing)
            {
                pcgMissingSpriteCount++;
            }

            return null;
        }

        string cacheKey = $"{assetPath}|{pivot.x:0.###},{pivot.y:0.###}";
        if (pcgSpriteCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var sprite = Resources.Load<Sprite>(assetPath);
        if (sprite != null)
        {
            pcgSpriteLoadCount++;
            pcgSpriteCache[cacheKey] = sprite;
            return sprite;
        }

        var texture = Resources.Load<Texture2D>(assetPath);
        if (texture != null)
        {
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, 128f, 0, SpriteMeshType.FullRect);
            pcgSpriteLoadCount++;
            pcgSpriteCreateCount++;
            pcgSpriteCache[cacheKey] = sprite;
            return sprite;
        }

        if (countMissing)
        {
            pcgMissingSpriteCount++;
            GFTrace.Warning("TotemMap", "PCG.SpriteMissing", null, GFTrace.Data("asset", assetPath));
        }

        pcgSpriteCache[cacheKey] = null;
        return null;
    }

    private Sprite CreateMissingPcgSprite(Vector2 pivot)
    {
        string key = $"__missing|{pivot.x:0.###},{pivot.y:0.###}";
        if (pcgSpriteCache.TryGetValue(key, out var sprite) && sprite != null)
        {
            return sprite;
        }

        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(255, 0, 255, 255);
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        sprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), pivot, 4f);
        pcgSpriteCache[key] = sprite;
        return sprite;
    }

    private void CreateBoundaryWalls(float mapSize)
    {
        const float wallHeight = 3f;
        const float wallThickness = 1f;
        CreateWall("TotemMap_Wall_S", new Vector3(mapSize * 0.5f, wallHeight * 0.5f, -wallThickness * 0.5f), new Vector3(mapSize, wallHeight, wallThickness));
        CreateWall("TotemMap_Wall_N", new Vector3(mapSize * 0.5f, wallHeight * 0.5f, mapSize + wallThickness * 0.5f), new Vector3(mapSize, wallHeight, wallThickness));
        CreateWall("TotemMap_Wall_W", new Vector3(-wallThickness * 0.5f, wallHeight * 0.5f, mapSize * 0.5f), new Vector3(wallThickness, wallHeight, mapSize));
        CreateWall("TotemMap_Wall_E", new Vector3(mapSize + wallThickness * 0.5f, wallHeight * 0.5f, mapSize * 0.5f), new Vector3(wallThickness, wallHeight, mapSize));
    }

    private void CreateWall(string wallName, Vector3 position, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = wallName;
        wall.transform.SetParent(mapRoot.transform, false);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wallObjectCount++;
        ApplyVisualMaterial(wall, ResolveWallAssetKey(), new Color(0.15f, 0.15f, 0.18f));
    }

    private void CreateRoomMarker(TotemRoomInfo room, Color tint)
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = $"TotemMap_Room_{room.RoomId}_{room.Label}";
        floor.transform.SetParent(mapRoot.transform, false);
        floor.transform.position = new Vector3(room.CenterWorld.x, 0.06f, room.CenterWorld.z);
        floor.transform.localScale = new Vector3(room.Footprint, 0.1f, room.Footprint);
        roomMarkerObjectCount++;
        ApplyVisualMaterial(floor, ResolveFloorAssetKey(room.RoomType), tint);
    }

    private void DestroySpawnedObjects()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            DestroyObject(spawnedObjects[i]);
        }

        spawnedObjects.Clear();
        mapRoot = null;
        ResetRuntimeVisualCounters();
    }

    private void ResetRuntimeVisualCounters()
    {
        groundObjectCount = 0;
        wallObjectCount = 0;
        roomMarkerObjectCount = 0;
        materialRequestCount = 0;
        materialFallbackCount = 0;
        lastMaterialAssetKey = string.Empty;
        lastMaterialFallbackAssetKey = string.Empty;
        pcgCellObjectCount = 0;
        pcgVisualObjectCount = 0;
        pcgMissingSpriteCount = 0;
        pcgSpriteLoadCount = 0;
        pcgSpriteCreateCount = 0;
    }

    private static void DestroyObject(UnityEngine.Object obj)
    {
        if (obj == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(obj);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }

    private static void SetColor(GameObject go, Color color)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        renderer.material = material;
    }

    private void ApplyVisualMaterial(GameObject go, string assetKey, Color fallbackColor)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        materialRequestCount++;
        lastMaterialAssetKey = assetKey ?? string.Empty;
        if (assetService != null && assetService.TryCreateTexturedMaterial(assetKey, fallbackColor, out var material) && material != null)
        {
            renderer.material = material;
            return;
        }

        materialFallbackCount++;
        lastMaterialFallbackAssetKey = assetKey ?? string.Empty;
        SetColor(go, fallbackColor);
    }

    private static string ResolveFloorAssetKey(TotemRoomType roomType)
    {
        switch (roomType)
        {
            case TotemRoomType.Merchant:
            case TotemRoomType.TattooStudio:
                return "map.floor.metal";
            case TotemRoomType.BossRoom:
                return "map.floor.blood";
            default:
                return "map.floor.ruins";
        }
    }

    private static string ResolveWallAssetKey()
    {
        return "map.wall.ruins";
    }

    private static Color GetRoomColor(TotemRoomType roomType)
    {
        switch (roomType)
        {
            case TotemRoomType.SpawnRoom:
                return new Color(0.30f, 0.70f, 1.00f);
            case TotemRoomType.TattooStudio:
                return new Color(0.85f, 0.45f, 0.90f);
            case TotemRoomType.Merchant:
                return new Color(1.00f, 0.85f, 0.30f);
            case TotemRoomType.BossRoom:
                return new Color(0.95f, 0.30f, 0.30f);
            default:
                return Color.gray;
        }
    }
}
