using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TotemMapService : TotemRuntimeServiceBase
{
    public const float DefaultMapSize = 400f;
    public const int TerrainCellSize = 4;
    public const int TerrainGridResolution = 100;

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

    public override string ServiceName => "Map";

    public TotemMapSnapshot CurrentMap { get; private set; }

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
        if (IsTerrainWalkable(QueryTerrain(map, preferred)))
        {
            return preferred;
        }

        Vector3 roomCenter = room?.CenterWorld ?? fallback;
        roomCenter.y = 0.5f;
        roomCenter = ClampToMap(roomCenter, map);
        if (IsTerrainWalkable(QueryTerrain(map, roomCenter)))
        {
            return roomCenter;
        }

        float step = Mathf.Max(1f, map?.TerrainCellSize ?? TerrainCellSize);
        for (int radius = 1; radius <= 4; radius++)
        {
            for (int dz = -radius; dz <= radius; dz++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dz) != radius)
                    {
                        continue;
                    }

                    var candidate = ClampToMap(roomCenter + new Vector3(dx * step, 0f, dz * step), map);
                    candidate.y = 0.5f;
                    if (IsTerrainWalkable(QueryTerrain(map, candidate)))
                    {
                        return candidate;
                    }
                }
            }
        }

        return roomCenter;
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
