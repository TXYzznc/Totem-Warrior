using MapGen.Data;
using MapGen.Generation;
using MapGen.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace Tattoo.Tests.MapGen
{
    public sealed class MapTerrainRendererTests
    {
        [Test]
        public void RenderUsesTilemapCellCountAndObjectPlacementCount()
        {
            var config = MapGenerationConfig.CreateDefault(50f, 2f);
            var map = new RegionGrowthGenerator().Generate(5150, config);
            var root = new GameObject("MapTerrainRendererTest");
            var renderer = root.AddComponent<MapTerrainRenderer>();

            try
            {
                renderer.Render(map);

                Assert.That(renderer.LastRenderedCellCount, Is.EqualTo(map.Width * map.Height));
                Assert.That(renderer.LastRenderedObjectCount, Is.EqualTo(map.ObjectPlacements.Count));
                Assert.That(renderer.LastRenderedObjectCount, Is.LessThan(renderer.LastRenderedCellCount));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ClearRemovesRenderedCounts()
        {
            var config = MapGenerationConfig.CreateDefault(20f, 2f);
            var map = new RegionGrowthGenerator().Generate(7, config);
            var root = new GameObject("MapTerrainRendererTest");
            var renderer = root.AddComponent<MapTerrainRenderer>();

            try
            {
                renderer.Render(map);
                renderer.Clear();

                Assert.That(renderer.LastRenderedCellCount, Is.EqualTo(0));
                Assert.That(renderer.LastRenderedObjectCount, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
