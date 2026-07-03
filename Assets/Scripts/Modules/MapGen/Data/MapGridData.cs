using System.Collections.Generic;
using UnityEngine;

namespace MapGen.Data
{
    public readonly struct MapFeaturePoint
    {
        public readonly FeaturePointType PointType;
        public readonly Vector2Int Cell;
        public readonly Vector3 WorldPosition;
        public readonly TerrainType PreferredTerrain;

        public MapFeaturePoint(FeaturePointType pointType, Vector2Int cell, Vector3 worldPosition,
            TerrainType preferredTerrain)
        {
            PointType = pointType;
            Cell = cell;
            WorldPosition = worldPosition;
            PreferredTerrain = preferredTerrain;
        }
    }

    public readonly struct MapObjectPlacement
    {
        public readonly MapObjectKind Kind;
        public readonly Vector2Int Cell;
        public readonly Vector3 WorldPosition;
        public readonly string AssetKey;

        public MapObjectPlacement(MapObjectKind kind, Vector2Int cell, Vector3 worldPosition, string assetKey)
        {
            Kind = kind;
            Cell = cell;
            WorldPosition = worldPosition;
            AssetKey = assetKey;
        }
    }

    public readonly struct MapSpawnCandidate
    {
        public readonly Vector2Int Cell;
        public readonly Vector3 WorldPosition;

        public MapSpawnCandidate(Vector2Int cell, Vector3 worldPosition)
        {
            Cell = cell;
            WorldPosition = worldPosition;
        }
    }

    public sealed class MapFeatureInstance
    {
        public string FeatureName;
        public TerrainType TerrainType;
        public FeatureSpreadMode SpreadMode;
        public int PaintedCells;
    }

    public sealed class MapGridData
    {
        public TerrainType[,] Grid { get; }
        public float CellSize { get; }
        public float MapSize { get; }
        public IReadOnlyList<MapFeaturePoint> FeaturePoints { get; }
        public IReadOnlyList<MapSpawnCandidate> SpawnCandidates { get; }
        public IReadOnlyList<MapObjectPlacement> ObjectPlacements { get; }
        public IReadOnlyList<MapFeatureInstance> FeatureInstances { get; }
        public IReadOnlyList<string> Warnings { get; }

        public int Width => Grid.GetLength(0);
        public int Height => Grid.GetLength(1);

        public MapGridData(
            TerrainType[,] grid,
            float cellSize,
            float mapSize,
            IReadOnlyList<MapFeaturePoint> featurePoints,
            IReadOnlyList<MapSpawnCandidate> spawnCandidates,
            IReadOnlyList<MapObjectPlacement> objectPlacements,
            IReadOnlyList<MapFeatureInstance> featureInstances,
            IReadOnlyList<string> warnings)
        {
            Grid = grid;
            CellSize = cellSize;
            MapSize = mapSize;
            FeaturePoints = featurePoints;
            SpawnCandidates = spawnCandidates;
            ObjectPlacements = objectPlacements;
            FeatureInstances = featureInstances;
            Warnings = warnings;
        }

        public bool IsInBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height;
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            return new Vector3((cell.x + 0.5f) * CellSize, 0f, (cell.y + 0.5f) * CellSize);
        }

        public Vector2Int WorldToCell(Vector3 world)
        {
            return new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt(world.x / CellSize), 0, Width - 1),
                Mathf.Clamp(Mathf.FloorToInt(world.z / CellSize), 0, Height - 1));
        }
    }
}
