using System.Collections.Generic;
using MapGen.Data;
using MapGen.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Tattoo.Tests.MapGen
{
    public sealed class TerrainEffectTrackerTests
    {
        [Test]
        public void TickOnSwampAppliesConfiguredSlowMultiplier()
        {
            var tracker = new TerrainEffectTracker();
            tracker.SetMap(CreateMap(TerrainType.Swamp));

            float multiplier = tracker.Tick(new Vector3(1f, 0f, 1f), 0f);

            Assert.That(tracker.CurrentTerrain, Is.EqualTo(TerrainType.Swamp));
            Assert.That(multiplier, Is.EqualTo(0.65f).Within(0.0001f));
        }

        [Test]
        public void TickLeavingSwampRestoresDefaultMultiplierAfterInterval()
        {
            var grid = CreateMap(TerrainType.Swamp);
            grid.Grid[1, 0] = TerrainType.Grass;
            var tracker = new TerrainEffectTracker();
            tracker.SetMap(grid);

            tracker.Tick(new Vector3(1f, 0f, 1f), 0f);
            float cached = tracker.Tick(new Vector3(3f, 0f, 1f), 0.1f);
            float refreshed = tracker.Tick(new Vector3(3f, 0f, 1f), 0.1f);

            Assert.That(cached, Is.EqualTo(0.65f).Within(0.0001f));
            Assert.That(refreshed, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(tracker.CurrentTerrain, Is.EqualTo(TerrainType.Grass));
        }

        [Test]
        public void ClearReturnsToNeutralState()
        {
            var tracker = new TerrainEffectTracker();
            tracker.SetMap(CreateMap(TerrainType.Swamp));
            tracker.Tick(new Vector3(1f, 0f, 1f), 0f);

            tracker.Clear();
            float multiplier = tracker.Tick(new Vector3(1f, 0f, 1f), 0.2f);

            Assert.That(multiplier, Is.EqualTo(1f));
            Assert.That(tracker.CurrentTerrain, Is.EqualTo(TerrainType.Grass));
        }

        static MapGridData CreateMap(TerrainType fill)
        {
            var grid = new TerrainType[2, 2];
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                    grid[x, y] = fill;
            }

            return new MapGridData(
                grid,
                cellSize: 2f,
                mapSize: 4f,
                featurePoints: new List<MapFeaturePoint>(),
                spawnCandidates: new List<MapSpawnCandidate>(),
                objectPlacements: new List<MapObjectPlacement>(),
                featureInstances: new List<MapFeatureInstance>(),
                warnings: new List<string>());
        }
    }
}
