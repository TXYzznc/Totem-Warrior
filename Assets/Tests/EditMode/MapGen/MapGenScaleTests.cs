using System.Diagnostics;
using MapGen.Data;
using MapGen.Generation;
using MapGen.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace Tattoo.Tests.MapGen
{
    public sealed class MapGenScaleTests
    {
        [Test]
        public void Generate400mMapCompletesWithinEditModeBudget()
        {
            var config = MapGenerationConfig.CreateDefault(400f, 2f);
            var generator = new RegionGrowthGenerator();
            var stopwatch = Stopwatch.StartNew();

            var map = generator.Generate(20260702, config);

            stopwatch.Stop();
            Assert.That(map.Width * map.Height, Is.EqualTo(40000));
            Assert.That(map.Warnings, Is.Empty, string.Join(", ", map.Warnings));
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(3000), "400m generation exceeded edit-mode budget");
        }

        [Test]
        public void Render400mMapUsesTilemapAndLimitedBillboards()
        {
            var config = MapGenerationConfig.CreateDefault(400f, 2f);
            var map = new RegionGrowthGenerator().Generate(20260703, config);
            var root = new GameObject("MapGenScaleRendererTest");
            var renderer = root.AddComponent<MapTerrainRenderer>();

            try
            {
                renderer.Render(map);

                Assert.That(renderer.LastRenderedCellCount, Is.EqualTo(40000));
                Assert.That(renderer.LastRenderedObjectCount, Is.EqualTo(map.ObjectPlacements.Count));
                Assert.That(renderer.LastRenderedObjectCount, Is.LessThan(2000));
                Assert.That(root.GetComponentsInChildren<Transform>(includeInactive: true).Length, Is.LessThan(2100));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
