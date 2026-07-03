using System.Collections.Generic;
using MapGen.Data;
using MapGen.Events;
using MapGen.Generation;
using NUnit.Framework;
using UnityEngine;

namespace Tattoo.Tests.MapGen
{
    public sealed class RegionGrowthGeneratorTests
    {
        [Test]
        public void SameSeedProducesSameGridAndFeaturePoints()
        {
            var config = MapGenerationConfig.CreateDefault(100f, 2f);
            var generator = new RegionGrowthGenerator();

            var first = generator.Generate(12345, config);
            for (int i = 0; i < 100; i++)
            {
                var next = generator.Generate(12345, config);
                AssertGridEqual(first, next);
                AssertFeaturePointsEqual(first, next);
                AssertSpawnCandidatesEqual(first, next);
            }
        }

        [Test]
        public void DifferentSeedChangesAtLeastOneCell()
        {
            var config = MapGenerationConfig.CreateDefault(100f, 2f);
            var generator = new RegionGrowthGenerator();

            var a = generator.Generate(1001, config);
            var b = generator.Generate(2002, config);

            Assert.That(HasAnyDifferentCell(a, b), Is.True);
        }

        [Test]
        public void RequiredFeaturePointsExistAndRespectBounds()
        {
            var config = MapGenerationConfig.CreateDefault(100f, 2f);
            var generator = new RegionGrowthGenerator();

            for (int seed = 0; seed < 100; seed++)
            {
                var map = generator.Generate(seed, config);
                Assert.That(map.FeaturePoints.Count, Is.EqualTo(config.FeaturePoints.Count));
                Assert.That(FindPointOrNull(map, FeaturePointType.Spawn), Is.Null);

                foreach (var pointConfig in config.FeaturePoints)
                {
                    var point = FindPoint(map, pointConfig.PointType);
                    float safe = Mathf.Min(pointConfig.SafeMargin, config.MapSize * 0.33f);
                    Assert.That(point.WorldPosition.x, Is.GreaterThanOrEqualTo(safe - config.CellSize));
                    Assert.That(point.WorldPosition.z, Is.GreaterThanOrEqualTo(safe - config.CellSize));
                    Assert.That(point.WorldPosition.x, Is.LessThanOrEqualTo(config.MapSize - safe + config.CellSize));
                    Assert.That(point.WorldPosition.z, Is.LessThanOrEqualTo(config.MapSize - safe + config.CellSize));
                }
            }
        }

        [Test]
        public void FeaturePointsKeepReasonableSpacing()
        {
            var config = MapGenerationConfig.CreateDefault(100f, 2f);
            var generator = new RegionGrowthGenerator();
            var map = generator.Generate(77, config);
            float expectedRelaxedSpacing = config.MapSize * 0.45f * 0.4f;

            for (int i = 0; i < map.FeaturePoints.Count; i++)
            {
                for (int j = i + 1; j < map.FeaturePoints.Count; j++)
                {
                    float distance = Vector3.Distance(
                        map.FeaturePoints[i].WorldPosition,
                        map.FeaturePoints[j].WorldPosition);
                    Assert.That(distance, Is.GreaterThanOrEqualTo(expectedRelaxedSpacing));
                }
            }
        }

        [Test]
        public void GeneratedGridHasNoIllegalAdjacency()
        {
            var config = MapGenerationConfig.CreateDefault(100f, 2f);
            var generator = new RegionGrowthGenerator();
            var map = generator.Generate(20260702, config);

            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    var current = map.Grid[x, y];
                    if (x + 1 < map.Width)
                        Assert.That(generator.IsAdjacentAllowed(current, map.Grid[x + 1, y]), Is.True,
                            $"Illegal adjacency at ({x},{y}) -> ({x + 1},{y})");
                    if (y + 1 < map.Height)
                        Assert.That(generator.IsAdjacentAllowed(current, map.Grid[x, y + 1]), Is.True,
                            $"Illegal adjacency at ({x},{y}) -> ({x},{y + 1})");
                }
            }
        }

        [Test]
        public void RequiredFeaturePointsAreReachableFromOpenSpawnCandidate()
        {
            var config = MapGenerationConfig.CreateDefault(100f, 2f);
            var generator = new RegionGrowthGenerator();
            var map = generator.Generate(9090, config);

            var visited = FloodWalkable(map, generator, map.SpawnCandidates[0].Cell);
            foreach (var point in map.FeaturePoints)
            {
                Assert.That(visited.Contains(point.Cell), Is.True, $"{point.PointType} is not reachable");
            }
        }

        [Test]
        public void AllWalkableCellsAreConnected()
        {
            var config = MapGenerationConfig.CreateDefault(100f, 2f);
            var generator = new RegionGrowthGenerator();
            var map = generator.Generate(8080, config);

            var visited = FloodWalkable(map, generator, map.SpawnCandidates[0].Cell);
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    if (!generator.IsWalkable(map.Grid[x, y]))
                        continue;

                    Assert.That(visited.Contains(new Vector2Int(x, y)), Is.True, $"Walkable cell disconnected: {x},{y}");
                }
            }
        }

        [Test]
        public void SpawnCandidatesAreOpenMapWalkableAndAwayFromHotspots()
        {
            var config = MapGenerationConfig.CreateDefault(100f, 2f);
            var generator = new RegionGrowthGenerator();
            var map = generator.Generate(123, config);

            Assert.That(map.SpawnCandidates.Count, Is.GreaterThanOrEqualTo(1));
            foreach (var candidate in map.SpawnCandidates)
            {
                Assert.That(map.IsInBounds(candidate.Cell), Is.True);
                Assert.That(generator.IsWalkable(map.Grid[candidate.Cell.x, candidate.Cell.y]), Is.True);
                Assert.That(candidate.WorldPosition.x, Is.GreaterThanOrEqualTo(28f - config.CellSize));
                Assert.That(candidate.WorldPosition.z, Is.GreaterThanOrEqualTo(28f - config.CellSize));
                Assert.That(candidate.WorldPosition.x, Is.LessThanOrEqualTo(config.MapSize - 28f + config.CellSize));
                Assert.That(candidate.WorldPosition.z, Is.LessThanOrEqualTo(config.MapSize - 28f + config.CellSize));

                foreach (var point in map.FeaturePoints)
                {
                    float distance = Vector3.Distance(candidate.WorldPosition, point.WorldPosition);
                    Assert.That(distance, Is.GreaterThanOrEqualTo(18f - config.CellSize));
                }
            }
        }

        [Test]
        public void FeaturesAreInjectedWithinConfiguredCounts()
        {
            var config = MapGenerationConfig.CreateDefault(100f, 2f);
            var generator = new RegionGrowthGenerator();
            var map = generator.Generate(31337, config);

            foreach (var feature in config.Features)
            {
                int count = 0;
                foreach (var instance in map.FeatureInstances)
                {
                    if (instance.FeatureName == feature.FeatureName)
                        count++;
                }

                Assert.That(count, Is.InRange(feature.CountMin, feature.CountMax), feature.FeatureName);
            }
        }

        [Test]
        public void TerrainHistogramIsNotHomogeneous()
        {
            var config = MapGenerationConfig.CreateDefault(100f, 2f);
            var generator = new RegionGrowthGenerator();
            var map = generator.Generate(4242, config);
            var histogram = new Dictionary<TerrainType, int>();

            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    histogram.TryGetValue(map.Grid[x, y], out int count);
                    histogram[map.Grid[x, y]] = count + 1;
                }
            }

            int max = 0;
            foreach (var pair in histogram)
                max = Mathf.Max(max, pair.Value);

            float maxRatio = max / (float)(map.Width * map.Height);
            Assert.That(histogram.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(maxRatio, Is.LessThanOrEqualTo(0.85f));
        }

        [Test]
        public void GenerateDoesNotCreateGameObjects()
        {
            var before = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;
            var config = MapGenerationConfig.CreateDefault(50f, 2f);
            var generator = new RegionGrowthGenerator();

            var map = generator.Generate(11, config);
            var after = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;

            Assert.That(map.Grid, Is.Not.Null);
            Assert.That(after, Is.EqualTo(before));
        }

        [Test]
        public void MapGeneratedEventCanCarryRegionGrowthGrid()
        {
            var config = MapGenerationConfig.CreateDefault(50f, 2f);
            var generator = new RegionGrowthGenerator();
            var map = generator.Generate(21, config);

            var generated = new MapGeneratedEvent
            {
                Seed = 21,
                ThemeId = 1,
                MapSize = map.MapSize,
                CellSize = map.CellSize,
                GridData = map,
            };

            Assert.That(generated.GridData, Is.SameAs(map));
            Assert.That(generated.CellSize, Is.EqualTo(2f));
            Assert.That(generated.GridData.Width, Is.EqualTo(25));
            Assert.That(generated.GridData.FeaturePoints.Count, Is.EqualTo(config.FeaturePoints.Count));
            Assert.That(generated.GridData.SpawnCandidates.Count, Is.GreaterThan(0));
        }

        static void AssertGridEqual(MapGridData a, MapGridData b)
        {
            Assert.That(b.Width, Is.EqualTo(a.Width));
            Assert.That(b.Height, Is.EqualTo(a.Height));
            for (int x = 0; x < a.Width; x++)
            {
                for (int y = 0; y < a.Height; y++)
                    Assert.That(b.Grid[x, y], Is.EqualTo(a.Grid[x, y]), $"Cell {x},{y}");
            }
        }

        static void AssertFeaturePointsEqual(MapGridData a, MapGridData b)
        {
            Assert.That(b.FeaturePoints.Count, Is.EqualTo(a.FeaturePoints.Count));
            for (int i = 0; i < a.FeaturePoints.Count; i++)
            {
                Assert.That(b.FeaturePoints[i].PointType, Is.EqualTo(a.FeaturePoints[i].PointType));
                Assert.That(b.FeaturePoints[i].Cell, Is.EqualTo(a.FeaturePoints[i].Cell));
            }
        }

        static void AssertSpawnCandidatesEqual(MapGridData a, MapGridData b)
        {
            Assert.That(b.SpawnCandidates.Count, Is.EqualTo(a.SpawnCandidates.Count));
            for (int i = 0; i < a.SpawnCandidates.Count; i++)
                Assert.That(b.SpawnCandidates[i].Cell, Is.EqualTo(a.SpawnCandidates[i].Cell));
        }

        static bool HasAnyDifferentCell(MapGridData a, MapGridData b)
        {
            for (int x = 0; x < a.Width; x++)
            {
                for (int y = 0; y < a.Height; y++)
                {
                    if (a.Grid[x, y] != b.Grid[x, y])
                        return true;
                }
            }
            return false;
        }

        static MapFeaturePoint FindPoint(MapGridData map, FeaturePointType pointType)
        {
            foreach (var point in map.FeaturePoints)
            {
                if (point.PointType == pointType)
                    return point;
            }
            Assert.Fail($"Feature point missing: {pointType}");
            return default;
        }

        static MapFeaturePoint? FindPointOrNull(MapGridData map, FeaturePointType pointType)
        {
            foreach (var point in map.FeaturePoints)
            {
                if (point.PointType == pointType)
                    return point;
            }
            return null;
        }

        static HashSet<Vector2Int> FloodWalkable(MapGridData map, RegionGrowthGenerator generator, Vector2Int start)
        {
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            visited.Add(start);
            queue.Enqueue(start);

            var dirs = new[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, -1),
            };

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var dir in dirs)
                {
                    var next = cell + dir;
                    if (!map.IsInBounds(next) || visited.Contains(next) || !generator.IsWalkable(map.Grid[next.x, next.y]))
                        continue;

                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
            return visited;
        }
    }
}
