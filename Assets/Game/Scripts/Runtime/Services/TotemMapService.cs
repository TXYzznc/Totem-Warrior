using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TotemMapService : TotemRuntimeServiceBase
{
    public const float DefaultMapSize = 768f;
    private const string GameplaySceneName = "OasisCity";
    private static readonly Vector2 AuthoredWorldMin = new Vector2(-256f, -384f);
    private static readonly Vector2 AuthoredWorldMax = new Vector2(256f, 384f);
    private static readonly Vector2[] AuthoredSpawnPositions =
    {
        new(-151.776f, 326.256f), new(-46.737f, 348.267f), new(73.279f, 336.262f), new(173.318f, 296.252f),
        new(221.325f, 229.235f), new(233.327f, 139.221f), new(223.330f, 44.182f), new(235.332f, -53.841f),
        new(218.324f, -150.859f), new(195.311f, -245.885f), new(138.303f, -315.898f), new(48.281f, -353.911f),
        new(-41.746f, -350.904f), new(-128.767f, -322.901f), new(-184.772f, -270.894f), new(-210.774f, -190.867f),
        new(-212.788f, -97.845f), new(-224.786f, -0.828f), new(-216.782f, 99.190f), new(-200.786f, 199.229f),
    };

    private TotemGameFlowService flowService;
    private TotemDataService dataService;
    private TotemMapTemplateDefinition[] runtimeTemplates = Array.Empty<TotemMapTemplateDefinition>();
    private bool hasPendingCombatMapRequest;
    private int pendingCombatMapSeed;
    private int pendingCombatMapThemeId;

    public override string ServiceName => "Map";
    public TotemMapSnapshot CurrentMap { get; private set; }

    public void RequestNextCombatMap(int seed, int themeId)
    {
        hasPendingCombatMapRequest = true;
        pendingCombatMapSeed = seed;
        pendingCombatMapThemeId = themeId;
    }

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        dataService = runtime.GetService<TotemDataService>();
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
        }
        flowService = null;
        dataService = null;
        runtimeTemplates = Array.Empty<TotemMapTemplateDefinition>();
        hasPendingCombatMapRequest = false;
        CurrentMap = null;
    }

    public TotemMapSnapshot GenerateMap(int seed, int themeId, bool createObjects)
    {
        CurrentMap = BuildLayout(seed, themeId, runtimeTemplates);
        GFTrace.Success("TotemMap", "Map.Generated", null, GFTrace.Data(
            "seed", seed.ToString(),
            "themeId", themeId.ToString(),
            "scene", CurrentMap.SourceSceneName,
            "anchorCount", CurrentMap.AnchorPlacements.Length.ToString(),
            "worldMin", $"{CurrentMap.WorldMin.x:F1},{CurrentMap.WorldMin.y:F1}",
            "worldMax", $"{CurrentMap.WorldMax.x:F1},{CurrentMap.WorldMax.y:F1}"));
        GF.Log($"[TotemMap] Authored scene map ready. seed={seed}, scene={CurrentMap.SourceSceneName}, " +
               $"anchors={CurrentMap.AnchorPlacements.Length}, bounds={CurrentMap.WorldMin}..{CurrentMap.WorldMax}.");
        return CurrentMap;
    }

    public IReadOnlyList<TotemMapTemplateDefinition> GetRuntimeTemplates() => runtimeTemplates;

    public TotemMapRuntimeSnapshot CaptureRuntimeSnapshot()
    {
        TotemMapSceneAuthoring authoring = FindLoadedAuthoring();
        return new TotemMapRuntimeSnapshot
        {
            hasRoot = authoring != null,
            rootName = authoring != null ? authoring.gameObject.name : string.Empty,
            spawnedObjectCount = 0,
            rootChildCount = authoring != null ? authoring.transform.childCount : 0,
            mapSize = CurrentMap?.MapSize ?? 0f,
            themeName = CurrentMap?.ThemeName ?? string.Empty,
            sourceSceneName = CurrentMap?.SourceSceneName ?? string.Empty,
            authoredAnchorCount = CurrentMap?.AnchorPlacements?.Length ?? 0,
        };
    }

    public static TotemMapSnapshot BuildLayout(int seed, int themeId) => BuildLayout(seed, themeId, LoadTemplates());

    public static TotemMapSnapshot BuildLayout(int seed, int themeId, IReadOnlyList<TotemMapTemplateDefinition> templates)
    {
        TotemMapTemplateDefinition template = ResolveTemplate(themeId, templates);
        TotemMapSceneAuthoring authoring = FindLoadedAuthoring();
        return authoring != null
            ? BuildFromScene(seed, template, authoring)
            : BuildFromAuthoredContract(seed, template);
    }

    private static TotemMapSnapshot BuildFromScene(int seed, TotemMapTemplateDefinition template, TotemMapSceneAuthoring authoring)
    {
        TotemMapAnchorAuthoring[] authoredAnchors = authoring.GetComponentsInChildren<TotemMapAnchorAuthoring>(true);
        Array.Sort(authoredAnchors, CompareAuthoring);
        var anchors = new TotemMapAnchor[authoredAnchors.Length];
        for (int i = 0; i < authoredAnchors.Length; i++)
        {
            TotemMapAnchorAuthoring source = authoredAnchors[i];
            anchors[i] = new TotemMapAnchor
            {
                AnchorId = source.AnchorId,
                Kind = source.Kind,
                Position = source.transform.position,
                Order = i,
                SearchRadius = source.SearchRadius,
                IsReachable = source.IsReachable,
            };
        }
        ValidateAnchorContract(anchors);
        return CreateSnapshot(seed, template, authoring.WorldMin, authoring.WorldMax, anchors, authoring.gameObject.scene.name);
    }

    private static TotemMapSnapshot BuildFromAuthoredContract(int seed, TotemMapTemplateDefinition template)
    {
        var anchors = new List<TotemMapAnchor>(48);
        for (int i = 0; i < AuthoredSpawnPositions.Length; i++)
        {
            Vector2 spawn = AuthoredSpawnPositions[i];
            Vector2 inward = -spawn.normalized;
            anchors.Add(CreateAnchor($"SP{i + 1:00}", TotemMapAnchorKind.PlayerSpawn, spawn, 8f, anchors.Count));
            anchors.Add(CreateAnchor($"RS{i + 1:00}", TotemMapAnchorKind.Resource, spawn + inward * 14f, 5f, anchors.Count));
            if (i % 3 == 0)
            {
                anchors.Add(CreateAnchor($"EX{i / 3 + 1:00}", TotemMapAnchorKind.Extraction, spawn + inward * 6f, 7f, anchors.Count));
            }
        }
        TotemMapAnchor[] result = anchors.ToArray();
        ValidateAnchorContract(result);
        return CreateSnapshot(seed, template, AuthoredWorldMin, AuthoredWorldMax, result, GameplaySceneName);
    }

    private static TotemMapSnapshot CreateSnapshot(
        int seed,
        TotemMapTemplateDefinition template,
        Vector2 worldMin,
        Vector2 worldMax,
        TotemMapAnchor[] anchors,
        string sourceSceneName)
    {
        Vector2 size = worldMax - worldMin;
        Vector2 center = (worldMin + worldMax) * 0.5f;
        float footprint = Mathf.Max(30f, Mathf.Min(size.x, size.y) * 0.25f);
        return new TotemMapSnapshot
        {
            Seed = seed,
            ThemeId = template.Id,
            ThemeName = template.ThemeName,
            MapSize = Mathf.Max(size.x, size.y),
            WorldMin = worldMin,
            WorldMax = worldMax,
            WorldGroundY = 2f,
            SourceSceneName = sourceSceneName ?? GameplaySceneName,
            MinRoomSize = template.MinRoomSize,
            PrefabPath = template.PrefabPath,
            HudAccentColor = template.HudAccentColor,
            DominantColor = template.DominantColor,
            InitialZoneCenter = center,
            Rooms = BuildQuadrantRooms(worldMin, worldMax, footprint),
            AnchorPlacements = anchors ?? Array.Empty<TotemMapAnchor>(),
        };
    }

    private static TotemRoomInfo[] BuildQuadrantRooms(Vector2 min, Vector2 max, float footprint)
    {
        Vector2 center = (min + max) * 0.5f;
        Vector2 quarter = (max - min) * 0.25f;
        return new[]
        {
            CreateRoom(1, "SouthWest", TotemRoomType.SouthWestArea, center + new Vector2(-quarter.x, -quarter.y), footprint),
            CreateRoom(2, "NorthWest", TotemRoomType.NorthWestArea, center + new Vector2(-quarter.x, quarter.y), footprint),
            CreateRoom(3, "NorthEast", TotemRoomType.NorthEastArea, center + quarter, footprint),
            CreateRoom(4, "SouthEast", TotemRoomType.SouthEastArea, center + new Vector2(quarter.x, -quarter.y), footprint),
        };
    }

    private static TotemMapAnchor CreateAnchor(string id, TotemMapAnchorKind kind, Vector2 position, float radius, int order)
    {
        return new TotemMapAnchor
        {
            AnchorId = id,
            Kind = kind,
            Position = new Vector3(position.x, 2.12f, position.y),
            Order = order,
            SearchRadius = radius,
            IsReachable = true,
        };
    }

    private static void ValidateAnchorContract(TotemMapAnchor[] anchors)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        int playerCount = 0;
        int resourceCount = 0;
        int extractionCount = 0;
        for (int i = 0; anchors != null && i < anchors.Length; i++)
        {
            TotemMapAnchor anchor = anchors[i];
            if (anchor == null || string.IsNullOrWhiteSpace(anchor.AnchorId) || !ids.Add(anchor.Kind + ":" + anchor.AnchorId))
            {
                throw new InvalidOperationException("OasisCity contains a missing or duplicate typed gameplay anchor ID.");
            }
            if (anchor.Kind == TotemMapAnchorKind.PlayerSpawn) playerCount++;
            else if (anchor.Kind == TotemMapAnchorKind.Resource) resourceCount++;
            else if (anchor.Kind == TotemMapAnchorKind.Extraction) extractionCount++;
        }
        if (playerCount < 6 || resourceCount < 8 || extractionCount < 3)
        {
            throw new InvalidOperationException(
                $"OasisCity gameplay anchor contract is incomplete: player={playerCount}, resource={resourceCount}, extraction={extractionCount}.");
        }
    }

    private static int CompareAuthoring(TotemMapAnchorAuthoring left, TotemMapAnchorAuthoring right)
    {
        int kind = left.Kind.CompareTo(right.Kind);
        return kind != 0 ? kind : string.CompareOrdinal(left.AnchorId, right.AnchorId);
    }

    private static TotemMapSceneAuthoring FindLoadedAuthoring()
    {
        Scene scene = SceneManager.GetSceneByName(GameplaySceneName);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            TotemMapSceneAuthoring authoring = roots[i].GetComponent<TotemMapSceneAuthoring>();
            if (authoring != null)
            {
                return authoring;
            }
        }
        return null;
    }

    public TotemTerrainType QueryTerrain(Vector3 worldPos) => QueryTerrain(CurrentMap, worldPos);
    public bool IsWalkable(Vector3 worldPos) => IsTerrainWalkable(QueryTerrain(worldPos));
    public float GetMoveSpeedMultiplier(Vector3 worldPos) => GetTerrainMoveSpeedMultiplier(QueryTerrain(worldPos));

    public static TotemTerrainType QueryTerrain(TotemMapSnapshot map, Vector3 worldPos)
    {
        if (map == null)
        {
            return TotemTerrainType.Blocked;
        }
        Vector2 min = GetWorldMin(map);
        Vector2 max = GetWorldMax(map);
        return worldPos.x >= min.x && worldPos.x <= max.x && worldPos.z >= min.y && worldPos.z <= max.y
            ? TotemTerrainType.Ground
            : TotemTerrainType.Blocked;
    }

    public static Vector2 GetWorldMin(TotemMapSnapshot map)
    {
        if (map == null) return Vector2.zero;
        return map.WorldMax.x > map.WorldMin.x && map.WorldMax.y > map.WorldMin.y ? map.WorldMin : Vector2.zero;
    }

    public static Vector2 GetWorldMax(TotemMapSnapshot map)
    {
        if (map == null) return new Vector2(DefaultMapSize, DefaultMapSize);
        return map.WorldMax.x > map.WorldMin.x && map.WorldMax.y > map.WorldMin.y
            ? map.WorldMax
            : new Vector2(map.MapSize, map.MapSize);
    }

    public static Vector2 GetWorldSize(TotemMapSnapshot map) => GetWorldMax(map) - GetWorldMin(map);
    public static float GetInitialZoneRadius(TotemMapSnapshot map)
    {
        Vector2 size = GetWorldSize(map);
        return Mathf.Max(1f, Mathf.Min(size.x, size.y) * 0.5f);
    }

    public static TotemMapAnchor FindAnchor(TotemMapSnapshot map, TotemMapAnchorKind kind) => FindAnchor(map, kind, null, null);

    public static TotemMapAnchor FindAnchor(TotemMapSnapshot map, TotemMapAnchorKind kind, string anchorId, string payloadId)
    {
        TotemMapAnchor[] anchors = map?.AnchorPlacements;
        for (int i = 0; anchors != null && i < anchors.Length; i++)
        {
            TotemMapAnchor anchor = anchors[i];
            if (anchor == null || anchor.Kind != kind) continue;
            if (!string.IsNullOrWhiteSpace(anchorId) && !string.Equals(anchor.AnchorId, anchorId, StringComparison.Ordinal)) continue;
            if (!string.IsNullOrWhiteSpace(payloadId) && !string.Equals(anchor.PayloadId, payloadId, StringComparison.Ordinal)) continue;
            return anchor;
        }
        return null;
    }

    public static TotemMapAnchor[] FindAnchors(TotemMapSnapshot map, TotemMapAnchorKind kind)
    {
        TotemMapAnchor[] anchors = map?.AnchorPlacements;
        if (anchors == null || anchors.Length == 0) return Array.Empty<TotemMapAnchor>();
        var result = new List<TotemMapAnchor>(anchors.Length);
        for (int i = 0; i < anchors.Length; i++)
        {
            if (anchors[i] != null && anchors[i].Kind == kind) result.Add(anchors[i]);
        }
        return result.ToArray();
    }

    public static Vector3 ResolveAnchorPosition(TotemMapSnapshot map, TotemMapAnchorKind kind, Vector3 fallback) =>
        ResolveAnchorPosition(map, kind, fallback, null, null);

    public static Vector3 ResolveAnchorPosition(TotemMapSnapshot map, TotemMapAnchorKind kind, Vector3 fallback, string anchorId, string payloadId)
    {
        TotemMapAnchor anchor = FindAnchor(map, kind, anchorId, payloadId);
        return anchor != null ? anchor.Position : fallback;
    }

    public static Vector3 ResolveRandomAnchorPosition(TotemMapSnapshot map, TotemMapAnchorKind kind, Vector3 fallback, System.Random random)
    {
        TotemMapAnchor[] anchors = FindAnchors(map, kind);
        if (anchors.Length == 0) return fallback;
        return anchors[random == null || anchors.Length == 1 ? 0 : random.Next(anchors.Length)].Position;
    }

    public static bool IsTerrainWalkable(TotemTerrainType terrainType) =>
        terrainType == TotemTerrainType.Ground || terrainType == TotemTerrainType.Slow ||
        terrainType == TotemTerrainType.Cover || terrainType == TotemTerrainType.Hazard;

    public static float GetTerrainMoveSpeedMultiplier(TotemTerrainType terrainType) => terrainType == TotemTerrainType.Slow ? 0.65f : 1f;
    public static float GetTerrainHazardDps(TotemTerrainType terrainType) => terrainType == TotemTerrainType.Hazard ? 4f : 0f;

    public TotemRoomInfo GetRoom(TotemRoomType roomType)
    {
        TotemRoomInfo[] rooms = CurrentMap?.Rooms;
        for (int i = 0; rooms != null && i < rooms.Length; i++)
        {
            if (rooms[i].RoomType == roomType) return rooms[i];
        }
        return null;
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            int seed = hasPendingCombatMapRequest ? pendingCombatMapSeed : 1;
            int theme = hasPendingCombatMapRequest ? pendingCombatMapThemeId : 1;
            hasPendingCombatMapRequest = false;
            GenerateMap(seed, theme, false);
        }
        else if (previousState == TotemGameFlowState.CombatHud)
        {
            CurrentMap = null;
            GFTrace.Info("TotemMap", "Map.Cleared", null, GFTrace.Data("nextState", nextState.ToString()));
        }
    }

    private static TotemRoomInfo CreateRoom(int id, string label, TotemRoomType type, Vector2 center, float footprint)
    {
        return new TotemRoomInfo
        {
            RoomId = id,
            Label = label,
            RoomType = type,
            CenterWorld = new Vector3(center.x, 0f, center.y),
            Bounds = new Rect(center.x - footprint * 0.5f, center.y - footprint * 0.5f, footprint, footprint),
            Footprint = footprint,
        };
    }

    private static TotemMapTemplateDefinition ResolveTemplate(int themeId, IReadOnlyList<TotemMapTemplateDefinition> templates)
    {
        IReadOnlyList<TotemMapTemplateDefinition> source = templates == null || templates.Count == 0 ? LoadTemplates() : templates;
        for (int i = 0; i < source.Count; i++) if (source[i] != null && source[i].Id == themeId) return source[i];
        for (int i = 0; i < source.Count; i++) if (source[i] != null) return source[i];
        return new TotemMapTemplateDefinition
        {
            Id = 1, ThemeName = "OASIS_CITY", MapSize = DefaultMapSize, MinRoomSize = 40f,
            PrefabPath = "Assets/Game/Scene/OasisCity.unity",
            HudAccentColor = "#66CCFF", DominantColor = "#3A4858",
        };
    }

    private static TotemMapTemplateDefinition[] LoadTemplates() =>
        NonEmpty(TotemDataService.LoadGameplayCatalogOrDefault().CreateMapTemplates(), Array.Empty<TotemMapTemplateDefinition>());

    private static T[] NonEmpty<T>(T[] primary, T[] fallback) => primary == null || primary.Length == 0 ? fallback : primary;
}
