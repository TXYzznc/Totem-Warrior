#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using NUnit.Framework;
using PCGMap;

public sealed class PCGWorldPlannerTests
{
    private const int Width = 64;
    private const int Height = 64;

    [Test]
    public void BaselineProfiles_ThirtySeedsPerTheme_AreDeterministicAndMeetTerrainAndEventMinimums()
    {
        var catalog = PCGWorldProfileCatalog.LoadFromResources();
        var planner = new PCGWorldPlanner(catalog);
        AssertFeatureOperationCoverage(catalog);

        for (int themeId = 1; themeId <= 3; themeId++)
        {
            var profile = catalog.Resolve(themeId);
            Assert.NotNull(profile, $"Theme {themeId} must have a world-generation profile.");
            Assert.AreEqual(4, profile.terrains.Count, $"Theme {themeId} must define four terrain identities.");

            for (int seedOffset = 0; seedOffset < 30; seedOffset++)
            {
                int seed = 10000 + themeId * 100 + seedOffset;
                var request = new PCGMapGenerateRequest { ThemeId = themeId, Seed = seed, Width = Width, Height = Height };
                var first = planner.Generate(request);
                var second = planner.Generate(request);

                Assert.AreEqual(first.ContentHash, second.ContentHash, $"Theme {themeId}, seed {seed} must be deterministic.");
                Assert.AreEqual(Width * Height, first.Cells.Length);
                AssertBaselineTerrainCoverage(first, profile, themeId, seed);
                AssertAllCellsAreVisualOnly(first, themeId, seed);
                AssertEventMinimums(first, themeId, seed);
            }
        }
    }

    [Test]
    public void Generator_ThirtySeedsPerTheme_AdaptsWorldPlansToVisualOnlyMaps()
    {
        var generator = new PCGMapGenerator(PCGAssetIndex.LoadFromConfig());
        for (int themeId = 1; themeId <= 3; themeId++)
        {
            for (int seedOffset = 0; seedOffset < 30; seedOffset++)
            {
                int seed = 20000 + themeId * 100 + seedOffset;
                var request = new PCGMapGenerateRequest { ThemeId = themeId, Seed = seed, Width = Width, Height = Height, ObjectBudget = 96 };
                var first = generator.Generate(request);
                var second = generator.Generate(request);

                Assert.NotNull(first.WorldPlan, $"Theme {themeId}, seed {seed} must retain its World Plan.");
                Assert.AreEqual(first.ContentHash, second.ContentHash, $"Theme {themeId}, seed {seed} map visual output must be deterministic.");
                Assert.AreEqual(Width * Height, first.Cells.Length);
                Assert.LessOrEqual(first.Visuals.Count, request.ObjectBudget,
                    $"Theme {themeId}, seed {seed} must keep every visual instance within the requested budget.");
                AssertVisualPlacementRules(first, themeId, seed);
                for (int cellIndex = 0; cellIndex < first.Cells.Length; cellIndex++)
                {
                    Assert.IsTrue(first.Cells[cellIndex].Walkable, $"Theme {themeId}, seed {seed}, cell {cellIndex} must be walkable in the visual-only baseline.");
                    Assert.IsFalse(first.Cells[cellIndex].Occupied, $"Theme {themeId}, seed {seed}, cell {cellIndex} must not become occupied by visual placement.");
                }

                for (int visualIndex = 0; visualIndex < first.Visuals.Count; visualIndex++)
                {
                    Assert.IsFalse(first.Visuals[visualIndex].BlocksMovement, $"Theme {themeId}, seed {seed}, visual {visualIndex} must not activate collision.");
                }
            }
        }
    }

    private static void AssertBaselineTerrainCoverage(PCGWorldPlan plan, PCGThemeWorldProfile profile, int themeId, int seed)
    {
        var counts = new Dictionary<string, int>();
        for (int i = 0; i < plan.Cells.Length; i++)
        {
            string terrain = plan.Cells[i].TerrainId;
            counts.TryGetValue(terrain, out int count);
            counts[terrain] = count + 1;
        }

        for (int i = 0; i < profile.terrains.Count; i++)
        {
            string terrainId = profile.terrains[i].terrainId;
            Assert.IsTrue(counts.TryGetValue(terrainId, out int count) && count > 0,
                $"Theme {themeId}, seed {seed} omitted terrain {terrainId}.");
        }
    }

    private static void AssertAllCellsAreVisualOnly(PCGWorldPlan plan, int themeId, int seed)
    {
        for (int i = 0; i < plan.Cells.Length; i++)
        {
            Assert.AreEqual(PCGCapabilityKind.Visual, plan.Cells[i].Capabilities,
                $"Theme {themeId}, seed {seed}, cell {i} must not activate a gameplay capability.");
        }
    }

    private static void AssertEventMinimums(PCGWorldPlan plan, int themeId, int seed)
    {
        Assert.GreaterOrEqual(Count(plan, "player_spawn"), 10, $"Theme {themeId}, seed {seed} lacks spawn candidates.");
        Assert.GreaterOrEqual(Count(plan, "boss_spawn"), 1, $"Theme {themeId}, seed {seed} lacks a boss anchor.");
        Assert.GreaterOrEqual(Count(plan, "merchant"), 3, $"Theme {themeId}, seed {seed} lacks merchants.");
        Assert.GreaterOrEqual(Count(plan, "tattooist"), 5, $"Theme {themeId}, seed {seed} lacks tattooists.");
        Assert.GreaterOrEqual(Count(plan, "chest"), 30, $"Theme {themeId}, seed {seed} lacks chests.");
        var ids = new HashSet<string>();
        for (int i = 0; i < plan.EventAnchors.Length; i++)
        {
            Assert.IsTrue(ids.Add(plan.EventAnchors[i].Id), $"Theme {themeId}, seed {seed} contains a duplicate event ID.");
        }
    }

    private static void AssertVisualPlacementRules(PCGMapData map, int themeId, int seed)
    {
        var usedCells = new HashSet<int>();
        for (int i = 0; i < map.Visuals.Count; i++)
        {
            var visual = map.Visuals[i];
            Assert.IsTrue(usedCells.Add(visual.Y * map.Width + visual.X),
                $"Theme {themeId}, seed {seed} contains overlapping static visuals at {visual.X},{visual.Y}.");
            for (int anchorIndex = 0; anchorIndex < map.WorldPlan.EventAnchors.Length; anchorIndex++)
            {
                var anchor = map.WorldPlan.EventAnchors[anchorIndex];
                int anchorX = (int)(anchor.NormalizedX * map.Width);
                int anchorY = (int)(anchor.NormalizedY * map.Height);
                float dx = visual.X - anchorX;
                float dy = visual.Y - anchorY;
                Assert.Greater(dx * dx + dy * dy, 4f,
                    $"Theme {themeId}, seed {seed} places a static visual in an event clearance zone.");
            }
        }
    }

    private static int Count(PCGWorldPlan plan, string eventType)
    {
        int count = 0;
        for (int i = 0; i < plan.EventAnchors.Length; i++)
        {
            if (string.Equals(plan.EventAnchors[i].EventType, eventType, StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    private static void AssertFeatureOperationCoverage(PCGWorldProfileCatalog catalog)
    {
        var operations = new HashSet<string>(StringComparer.Ordinal);
        for (int themeIndex = 0; themeIndex < catalog.themes.Count; themeIndex++)
        {
            var profile = catalog.themes[themeIndex];
            for (int i = 0; i < profile.features.Count; i++) operations.Add(profile.features[i].operation);
        }
        CollectionAssert.IsSubsetOf(new[] { "blob", "ribbon", "chain", "scatter", "fringe" }, operations,
            "The baseline catalog must exercise every generic terrain feature operation.");
    }
}
#endif
