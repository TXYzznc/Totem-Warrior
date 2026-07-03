using System;
using System.Collections.Generic;
using MapGen.Data;
using UnityEngine;

namespace MapGen.Generation
{
    public sealed class RegionGrowthGenerator
    {
        static readonly Vector2Int[] Neighbors =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
        };

        readonly Dictionary<TerrainType, TerrainTypeDefinition> _terrainByType = new();
        readonly Dictionary<TerrainPair, bool> _adjacency = new();
        readonly List<TerrainTypeDefinition> _weightedTerrains = new();
        readonly List<string> _warnings = new();
        TerrainTypeDefinition[] _candidateBuffer = Array.Empty<TerrainTypeDefinition>();
        TerrainType _fallbackTerrain = TerrainType.Grass;

        public MapGridData Generate(int seed, MapGenerationConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.CellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(config.CellSize));
            if (config.MapSize <= 0f) throw new ArgumentOutOfRangeException(nameof(config.MapSize));

            CacheConfig(config);

            int width = Mathf.Max(1, Mathf.RoundToInt(config.MapSize / config.CellSize));
            int height = width;
            var grid = new TerrainType[width, height];
            var filled = new bool[width, height];
            var rng = new System.Random(seed);

            var featurePoints = PlaceFeaturePoints(config, width, height, rng);
            GrowBaseTerrain(grid, filled, featurePoints, rng);
            InjectFeatures(grid, rng, config.Features, out var featureInstances);
            RepairWaterEdges(grid);
            EnsureWalkableConnectivity(grid, featurePoints);
            RepairWaterEdges(grid);
            var spawnCandidates = CreateSpawnCandidates(grid, featurePoints, rng, config.CellSize, config.MapSize);
            var placements = CreateObjectPlacements(grid, featurePoints, rng, config.CellSize);

            return new MapGridData(
                grid,
                config.CellSize,
                width * config.CellSize,
                featurePoints,
                spawnCandidates,
                placements,
                featureInstances,
                new List<string>(_warnings));
        }

        public bool IsWalkable(TerrainType terrainType)
        {
            return _terrainByType.TryGetValue(terrainType, out var def) && def.IsWalkable;
        }

        public bool IsAdjacentAllowed(TerrainType a, TerrainType b)
        {
            return _adjacency.TryGetValue(new TerrainPair(a, b), out bool allowed) && allowed;
        }

        void CacheConfig(MapGenerationConfig config)
        {
            _terrainByType.Clear();
            _adjacency.Clear();
            _weightedTerrains.Clear();
            _warnings.Clear();
            _fallbackTerrain = TerrainType.Grass;

            foreach (var terrain in config.Terrains)
            {
                _terrainByType[terrain.Type] = terrain;
                if (terrain.IsFallback)
                    _fallbackTerrain = terrain.Type;

                int weight = Mathf.Max(1, terrain.GrowthWeight);
                for (int i = 0; i < weight; i++)
                    _weightedTerrains.Add(terrain);
            }

            foreach (var rule in config.AdjacencyRules)
                _adjacency[new TerrainPair(rule.FromType, rule.ToType)] = rule.Allowed;

            foreach (TerrainType type in Enum.GetValues(typeof(TerrainType)))
            {
                var same = new TerrainPair(type, type);
                if (!_adjacency.ContainsKey(same))
                    _adjacency[same] = true;
            }

            if (_candidateBuffer.Length < _weightedTerrains.Count)
                _candidateBuffer = new TerrainTypeDefinition[_weightedTerrains.Count];
        }

        List<MapFeaturePoint> PlaceFeaturePoints(MapGenerationConfig config, int width, int height, System.Random rng)
        {
            var result = new List<MapFeaturePoint>(config.FeaturePoints.Count);
            foreach (var point in config.FeaturePoints)
            {
                if (!point.Required)
                    continue;

                var cell = PickFeaturePointCell(config, point, width, height, result, rng);
                result.Add(new MapFeaturePoint(
                    point.PointType,
                    cell,
                    CellToWorld(cell, config.CellSize),
                    point.PreferredTerrain));
            }
            return result;
        }

        Vector2Int PickFeaturePointCell(MapGenerationConfig config, FeaturePointDefinition point, int width, int height,
            IReadOnlyList<MapFeaturePoint> existing, System.Random rng)
        {
            int safeCells = Mathf.Clamp(Mathf.RoundToInt(point.SafeMargin / config.CellSize), 1, Mathf.Max(1, width / 3));
            float spacing = Mathf.Min(point.MinSpacing, config.MapSize * 0.45f);
            int attempts = 96;

            for (int relaxed = 0; relaxed < 6; relaxed++)
            {
                float effectiveSpacing = spacing * (1f - relaxed * 0.12f);
                for (int i = 0; i < attempts; i++)
                {
                    var cell = new Vector2Int(
                        rng.Next(safeCells, Mathf.Max(safeCells + 1, width - safeCells)),
                        rng.Next(safeCells, Mathf.Max(safeCells + 1, height - safeCells)));

                    if (IsFarEnough(cell, existing, effectiveSpacing, config.CellSize))
                        return cell;
                }
            }

            _warnings.Add($"FeaturePointSpacingRelaxed Point={point.PointType}");
            return PickDeterministicFallbackCell(width, height, safeCells, existing.Count);
        }

        static bool IsFarEnough(Vector2Int cell, IReadOnlyList<MapFeaturePoint> existing, float spacing, float cellSize)
        {
            float spacingSqr = spacing * spacing;
            for (int i = 0; i < existing.Count; i++)
            {
                var delta = cell - existing[i].Cell;
                float distSqr = (delta.x * cellSize) * (delta.x * cellSize) + (delta.y * cellSize) * (delta.y * cellSize);
                if (distSqr < spacingSqr)
                    return false;
            }
            return true;
        }

        static Vector2Int PickDeterministicFallbackCell(int width, int height, int safeCells, int index)
        {
            int min = safeCells;
            int maxX = Mathf.Max(min, width - safeCells - 1);
            int maxY = Mathf.Max(min, height - safeCells - 1);
            return (index % 4) switch
            {
                0 => new Vector2Int(min, min),
                1 => new Vector2Int(maxX, maxY),
                2 => new Vector2Int(min, maxY),
                _ => new Vector2Int(maxX, min),
            };
        }

        void GrowBaseTerrain(TerrainType[,] grid, bool[,] filled, IReadOnlyList<MapFeaturePoint> featurePoints,
            System.Random rng)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            var queue = new Queue<Vector2Int>(width * height);

            for (int i = 0; i < featurePoints.Count; i++)
            {
                var point = featurePoints[i];
                PaintSeedArea(grid, filled, queue, point.Cell, point.PreferredTerrain, 2);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int neighborOffset = rng.Next(Neighbors.Length);
                for (int i = 0; i < Neighbors.Length; i++)
                {
                    var next = current + Neighbors[(i + neighborOffset) % Neighbors.Length];
                    if (!InBounds(next, width, height) || filled[next.x, next.y])
                        continue;

                    grid[next.x, next.y] = PickCompatibleTerrain(grid, filled, next, rng);
                    filled[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (filled[x, y])
                        continue;

                    var cell = new Vector2Int(x, y);
                    grid[x, y] = PickCompatibleTerrain(grid, filled, cell, rng);
                    filled[x, y] = true;
                }
            }
        }

        void PaintSeedArea(TerrainType[,] grid, bool[,] filled, Queue<Vector2Int> queue, Vector2Int center,
            TerrainType terrain, int radius)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    var cell = new Vector2Int(center.x + dx, center.y + dy);
                    if (!InBounds(cell, width, height) || filled[cell.x, cell.y])
                        continue;

                    grid[cell.x, cell.y] = terrain;
                    filled[cell.x, cell.y] = true;
                    queue.Enqueue(cell);
                }
            }
        }

        TerrainType PickCompatibleTerrain(TerrainType[,] grid, bool[,] filled, Vector2Int cell, System.Random rng)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            int candidateCount = 0;
            for (int i = 0; i < _weightedTerrains.Count; i++)
            {
                var terrain = _weightedTerrains[i];
                if (IsCompatibleWithFilledNeighbors(grid, filled, cell, width, height, terrain.Type))
                    _candidateBuffer[candidateCount++] = terrain;
            }

            if (candidateCount == 0)
            {
                _warnings.Add($"FallbackTerrain Cell={cell.x},{cell.y}");
                return _fallbackTerrain;
            }

            return _candidateBuffer[rng.Next(candidateCount)].Type;
        }

        bool IsCompatibleWithFilledNeighbors(TerrainType[,] grid, bool[,] filled, Vector2Int cell, int width, int height,
            TerrainType terrainType)
        {
            for (int i = 0; i < Neighbors.Length; i++)
            {
                var neighbor = cell + Neighbors[i];
                if (!InBounds(neighbor, width, height) || !filled[neighbor.x, neighbor.y])
                    continue;

                if (!IsAdjacentAllowed(terrainType, grid[neighbor.x, neighbor.y]))
                    return false;
            }
            return true;
        }

        void InjectFeatures(TerrainType[,] grid, System.Random rng, IReadOnlyList<FeatureInjectionDefinition> features,
            out List<MapFeatureInstance> instances)
        {
            instances = new List<MapFeatureInstance>();
            for (int i = 0; i < features.Count; i++)
            {
                var feature = features[i];
                int count = rng.Next(feature.CountMin, feature.CountMax + 1);
                for (int j = 0; j < count; j++)
                {
                    int size = rng.Next(feature.SizeMin, feature.SizeMax + 1);
                    int painted = feature.SpreadMode == FeatureSpreadMode.Line
                        ? PaintLineFeature(grid, rng, feature.TerrainType, size)
                        : PaintBlobFeature(grid, rng, feature.TerrainType, size);

                    instances.Add(new MapFeatureInstance
                    {
                        FeatureName = feature.FeatureName,
                        TerrainType = feature.TerrainType,
                        SpreadMode = feature.SpreadMode,
                        PaintedCells = painted,
                    });
                }
            }
        }

        int PaintLineFeature(TerrainType[,] grid, System.Random rng, TerrainType terrainType, int length)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            var current = new Vector2Int(rng.Next(2, width - 2), rng.Next(2, height - 2));
            var direction = Neighbors[rng.Next(Neighbors.Length)];
            int painted = 0;

            for (int i = 0; i < length; i++)
            {
                if (!InBounds(current, width, height))
                    break;

                grid[current.x, current.y] = terrainType;
                painted++;
                if (terrainType == TerrainType.Water)
                    PaintShoreAround(grid, current);

                if (rng.NextDouble() < 0.35)
                    direction = Neighbors[rng.Next(Neighbors.Length)];

                current += direction;
                current.x = Mathf.Clamp(current.x, 1, width - 2);
                current.y = Mathf.Clamp(current.y, 1, height - 2);
            }

            return painted;
        }

        int PaintBlobFeature(TerrainType[,] grid, System.Random rng, TerrainType terrainType, int size)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            var start = new Vector2Int(rng.Next(2, width - 2), rng.Next(2, height - 2));
            var frontier = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int>();
            frontier.Enqueue(start);
            visited.Add(start);
            int painted = 0;

            while (frontier.Count > 0 && painted < size)
            {
                var cell = frontier.Dequeue();
                if (!CanPaintFeatureTerrain(grid, cell, terrainType))
                    continue;

                grid[cell.x, cell.y] = terrainType;
                painted++;

                int neighborOffset = rng.Next(Neighbors.Length);
                for (int i = 0; i < Neighbors.Length; i++)
                {
                    var next = cell + Neighbors[(i + neighborOffset) % Neighbors.Length];
                    if (!InBounds(next, width, height) || visited.Contains(next))
                        continue;
                    if (rng.NextDouble() > 0.72)
                        continue;

                    visited.Add(next);
                    frontier.Enqueue(next);
                }
            }

            return painted;
        }

        bool CanPaintFeatureTerrain(TerrainType[,] grid, Vector2Int cell, TerrainType terrainType)
        {
            if (terrainType == TerrainType.Water)
                return true;

            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            for (int i = 0; i < Neighbors.Length; i++)
            {
                var neighbor = cell + Neighbors[i];
                if (!InBounds(neighbor, width, height))
                    continue;

                if (!IsAdjacentAllowed(terrainType, grid[neighbor.x, neighbor.y]))
                    return false;
            }
            return true;
        }

        void PaintShoreAround(TerrainType[,] grid, Vector2Int waterCell)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            for (int i = 0; i < Neighbors.Length; i++)
            {
                var neighbor = waterCell + Neighbors[i];
                if (!InBounds(neighbor, width, height))
                    continue;
                if (grid[neighbor.x, neighbor.y] != TerrainType.Water)
                    grid[neighbor.x, neighbor.y] = TerrainType.Shore;
            }
        }

        void RepairWaterEdges(TerrainType[,] grid)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] != TerrainType.Water)
                        continue;

                    PaintShoreAround(grid, new Vector2Int(x, y));
                }
            }
        }

        void EnsureWalkableConnectivity(TerrainType[,] grid, IReadOnlyList<MapFeaturePoint> featurePoints)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            var anchor = FindConnectivityAnchor(featurePoints);
            bool[,] visited = FloodWalkable(grid, anchor);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!IsWalkable(grid[x, y]) || visited[x, y])
                        continue;

                    CarvePath(grid, new Vector2Int(x, y), FindNearestVisited(visited, new Vector2Int(x, y)));
                    visited = FloodWalkable(grid, anchor);
                }
            }

            for (int i = 0; i < featurePoints.Count; i++)
            {
                var point = featurePoints[i];
                if (!visited[point.Cell.x, point.Cell.y])
                {
                    CarvePath(grid, point.Cell, anchor);
                    visited = FloodWalkable(grid, anchor);
                }
                grid[point.Cell.x, point.Cell.y] = point.PreferredTerrain;
            }
        }

        Vector2Int FindConnectivityAnchor(IReadOnlyList<MapFeaturePoint> featurePoints)
        {
            return featurePoints.Count > 0 ? featurePoints[0].Cell : Vector2Int.zero;
        }

        bool[,] FloodWalkable(TerrainType[,] grid, Vector2Int start)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            var visited = new bool[width, height];
            var queue = new Queue<Vector2Int>();
            if (!InBounds(start, width, height))
                return visited;

            if (!IsWalkable(grid[start.x, start.y]))
                grid[start.x, start.y] = _fallbackTerrain;

            visited[start.x, start.y] = true;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                for (int i = 0; i < Neighbors.Length; i++)
                {
                    var next = cell + Neighbors[i];
                    if (!InBounds(next, width, height) || visited[next.x, next.y] || !IsWalkable(grid[next.x, next.y]))
                        continue;

                    visited[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }
            return visited;
        }

        List<MapSpawnCandidate> CreateSpawnCandidates(TerrainType[,] grid, IReadOnlyList<MapFeaturePoint> featurePoints,
            System.Random rng, float cellSize, float mapSize)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            int target = Mathf.Clamp((width * height) / 500, 24, 96);
            int safeCells = Mathf.Clamp(Mathf.RoundToInt(28f / cellSize), 1, Mathf.Max(1, width / 4));
            float minSpacing = Mathf.Max(12f, mapSize * 0.04f);
            float hotspotAvoidance = Mathf.Max(18f, mapSize * 0.06f);
            var result = new List<MapSpawnCandidate>(target);

            for (int relaxed = 0; relaxed < 4 && result.Count < target; relaxed++)
            {
                float spacing = minSpacing * (1f - relaxed * 0.18f);
                float avoidance = hotspotAvoidance * (1f - relaxed * 0.20f);
                int attempts = target * 48;
                for (int i = 0; i < attempts && result.Count < target; i++)
                {
                    var cell = new Vector2Int(
                        rng.Next(safeCells, Mathf.Max(safeCells + 1, width - safeCells)),
                        rng.Next(safeCells, Mathf.Max(safeCells + 1, height - safeCells)));

                    if (!IsWalkable(grid[cell.x, cell.y]))
                        continue;
                    if (!IsFarFromFeaturePoints(cell, featurePoints, avoidance, cellSize))
                        continue;
                    if (!IsFarFromSpawnCandidates(cell, result, spacing, cellSize))
                        continue;

                    result.Add(new MapSpawnCandidate(cell, CellToWorld(cell, cellSize)));
                }
            }

            if (result.Count == 0)
                AddDeterministicSpawnFallback(grid, result, cellSize);

            return result;
        }

        bool IsFarFromFeaturePoints(Vector2Int cell, IReadOnlyList<MapFeaturePoint> featurePoints, float spacing,
            float cellSize)
        {
            float spacingSqr = spacing * spacing;
            for (int i = 0; i < featurePoints.Count; i++)
            {
                var delta = cell - featurePoints[i].Cell;
                float distSqr = (delta.x * cellSize) * (delta.x * cellSize) + (delta.y * cellSize) * (delta.y * cellSize);
                if (distSqr < spacingSqr)
                    return false;
            }
            return true;
        }

        static bool IsFarFromSpawnCandidates(Vector2Int cell, IReadOnlyList<MapSpawnCandidate> existing, float spacing,
            float cellSize)
        {
            float spacingSqr = spacing * spacing;
            for (int i = 0; i < existing.Count; i++)
            {
                var delta = cell - existing[i].Cell;
                float distSqr = (delta.x * cellSize) * (delta.x * cellSize) + (delta.y * cellSize) * (delta.y * cellSize);
                if (distSqr < spacingSqr)
                    return false;
            }
            return true;
        }

        void AddDeterministicSpawnFallback(TerrainType[,] grid, List<MapSpawnCandidate> result, float cellSize)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            var center = new Vector2Int(width / 2, height / 2);
            int bestDist = int.MaxValue;
            var best = center;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!IsWalkable(grid[x, y]))
                        continue;

                    int dist = Mathf.Abs(x - center.x) + Mathf.Abs(y - center.y);
                    if (dist >= bestDist)
                        continue;

                    bestDist = dist;
                    best = new Vector2Int(x, y);
                }
            }

            result.Add(new MapSpawnCandidate(best, CellToWorld(best, cellSize)));
        }

        Vector2Int FindNearestVisited(bool[,] visited, Vector2Int from)
        {
            int width = visited.GetLength(0);
            int height = visited.GetLength(1);
            int bestDist = int.MaxValue;
            var best = from;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!visited[x, y])
                        continue;

                    int dist = Mathf.Abs(from.x - x) + Mathf.Abs(from.y - y);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = new Vector2Int(x, y);
                    }
                }
            }
            return best;
        }

        void CarvePath(TerrainType[,] grid, Vector2Int from, Vector2Int to)
        {
            var current = from;
            while (current.x != to.x)
            {
                grid[current.x, current.y] = _fallbackTerrain;
                current.x += current.x < to.x ? 1 : -1;
            }
            while (current.y != to.y)
            {
                grid[current.x, current.y] = _fallbackTerrain;
                current.y += current.y < to.y ? 1 : -1;
            }
            grid[current.x, current.y] = _fallbackTerrain;
        }

        List<MapObjectPlacement> CreateObjectPlacements(TerrainType[,] grid, IReadOnlyList<MapFeaturePoint> featurePoints,
            System.Random rng, float cellSize)
        {
            var placements = new List<MapObjectPlacement>(featurePoints.Count + 96);
            for (int i = 0; i < featurePoints.Count; i++)
            {
                var point = featurePoints[i];
                placements.Add(new MapObjectPlacement(
                    MapObjectKind.FeatureBuilding,
                    point.Cell,
                    CellToWorld(point.Cell, cellSize),
                    $"Sprite/Map/AI_RUINS_WETLAND/Feature/{point.PointType}"));
            }

            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            int target = Mathf.Clamp((width * height) / 180, 16, 260);
            for (int i = 0; i < target; i++)
            {
                var cell = new Vector2Int(rng.Next(0, width), rng.Next(0, height));
                var terrain = grid[cell.x, cell.y];
                if (terrain == TerrainType.Water || terrain == TerrainType.Mountain)
                    continue;

                var kind = terrain switch
                {
                    TerrainType.Forest => MapObjectKind.Tree,
                    TerrainType.Swamp => MapObjectKind.Reed,
                    TerrainType.Wasteland => MapObjectKind.RuinDebris,
                    TerrainType.RuinFloor => MapObjectKind.RuinDebris,
                    _ => MapObjectKind.Rock,
                };
                placements.Add(new MapObjectPlacement(kind, cell, CellToWorld(cell, cellSize),
                    $"Sprite/Map/AI_RUINS_WETLAND/Object/{kind}"));
            }
            return placements;
        }

        static bool InBounds(Vector2Int cell, int width, int height)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height;
        }

        static Vector3 CellToWorld(Vector2Int cell, float cellSize)
        {
            return new Vector3((cell.x + 0.5f) * cellSize, 0f, (cell.y + 0.5f) * cellSize);
        }

        readonly struct TerrainPair : IEquatable<TerrainPair>
        {
            readonly TerrainType _from;
            readonly TerrainType _to;

            public TerrainPair(TerrainType from, TerrainType to)
            {
                _from = from;
                _to = to;
            }

            public bool Equals(TerrainPair other)
            {
                return _from == other._from && _to == other._to;
            }

            public override bool Equals(object obj)
            {
                return obj is TerrainPair other && Equals(other);
            }

            public override int GetHashCode()
            {
                return ((int)_from * 397) ^ (int)_to;
            }
        }
    }
}
