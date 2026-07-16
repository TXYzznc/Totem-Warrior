using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace PCGMap
{
    /// <summary>
    /// World Plan 的视觉适配器。它不生成主题地貌、固定道路、边缘拼接或玩法阻挡；
    /// 所有空间决策均由 <see cref="PCGWorldPlanner"/> 完成。
    /// </summary>
    public sealed class PCGMapGenerator
    {
        readonly PCGAssetIndex _assetIndex;
        readonly PCGWorldPlanner _worldPlanner;
        readonly int _maxCells;

        public PCGMapGenerator(PCGAssetIndex assetIndex, int maxWidth = 128, int maxHeight = 128, PCGWorldProfileCatalog worldProfiles = null)
        {
            _assetIndex = assetIndex ?? throw new ArgumentNullException(nameof(assetIndex));
            _worldPlanner = new PCGWorldPlanner(worldProfiles ?? PCGWorldProfileCatalog.LoadFromResources());
            _maxCells = Mathf.Max(1, maxWidth * maxHeight);
        }

        public PCGMapData Generate(PCGMapGenerateRequest request)
        {
            if (request == null || request.Width <= 0 || request.Height <= 0 || request.Width * request.Height > _maxCells)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "PCG map dimensions exceed generator capacity.");
            }

            var stopwatch = Stopwatch.StartNew();
            var map = new PCGMapData
            {
                Width = request.Width,
                Height = request.Height,
                Seed = request.Seed,
                Cells = new PCGMapCell[request.Width * request.Height],
            };
            var visualRandom = new System.Random(DeriveSeed(request.Seed, 31337));

            long stageStart = stopwatch.ElapsedMilliseconds;
            GenerateCells(map, request, visualRandom);
            map.Diagnostics.AddStep("WorldPlan.Cells", stopwatch.ElapsedMilliseconds - stageStart, GetCellSummary(map));

            stageStart = stopwatch.ElapsedMilliseconds;
            PlaceObjects(map, visualRandom, Mathf.Max(0, request.ObjectBudget));
            map.Diagnostics.AddStep("Visual.Objects", stopwatch.ElapsedMilliseconds - stageStart, $"count={CountVisuals(map, PCGPlacedVisualKind.Object)}");

            map.Validation = BuildVisualOnlyReport(map);
            map.ContentHash = ComputeHash(map);
            stopwatch.Stop();
            map.Diagnostics.TotalMs = stopwatch.ElapsedMilliseconds;
            map.Diagnostics.AddStep("Hash", 0, $"hash={map.ContentHash}");
            return map;
        }

        void GenerateCells(PCGMapData map, PCGMapGenerateRequest request, System.Random random)
        {
            map.WorldPlan = _worldPlanner.Generate(request);
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var worldCell = map.WorldPlan.GetCell(x, y);
                    var terrainVisual = _assetIndex.PickTerrain(worldCell.TerrainId, "inner", map.WorldPlan.BiomeId, random);
                    map.SetCell(x, y, new PCGMapCell
                    {
                        X = x,
                        Y = y,
                        Biome = map.WorldPlan.BiomeId,
                        Terrain = worldCell.TerrainId,
                        ZoneId = worldCell.RegionId,
                        Walkable = true,
                        Occupied = false,
                        BaseAsset = _assetIndex.PickTerrainAsset(terrainVisual, random),
                    });
                }
            }
        }

        void PlaceObjects(PCGMapData map, System.Random random, int budget)
        {
            if (budget <= 0)
            {
                return;
            }

            var profile = map.WorldPlan?.VisualPlacement ?? new PCGVisualPlacementProfile();
            var reservedCells = new HashSet<int>(budget * 2);
            int ambientTarget = Mathf.RoundToInt(budget * Mathf.Clamp01(profile.ambientRatio));
            int placed = PlaceAmbientObjects(map, random, ambientTarget, 0f, reservedCells, profile);
            placed += PlaceClusteredObjects(map, random, budget - ambientTarget, reservedCells, profile);
            placed += PlaceAmbientObjects(map, random, budget - placed, 0f, reservedCells, profile);
        }

        int PlaceAmbientObjects(PCGMapData map, System.Random random, int target, float densityBias,
            HashSet<int> reservedCells, PCGVisualPlacementProfile profile)
        {
            int placed = 0;
            int attempts = Mathf.Max(1, target * 20);
            while (placed < target && attempts-- > 0)
            {
                int x = random.Next(1, Mathf.Max(2, map.Width - 1));
                int y = random.Next(1, Mathf.Max(2, map.Height - 1));
                var cell = map.GetCell(x, y);
                float densityAcceptance = Mathf.Lerp(1f, 0.20f + 0.80f * Mathf.Clamp01(map.WorldPlan.GetCell(x, y).Density), densityBias);
                if (random.NextDouble() > densityAcceptance || !TryPlaceObject(map, random, x, y, reservedCells, profile))
                {
                    continue;
                }

                placed++;
            }

            return placed;
        }

        int PlaceClusteredObjects(PCGMapData map, System.Random random, int target,
            HashSet<int> reservedCells, PCGVisualPlacementProfile profile)
        {
            int placed = 0;
            int attempts = Mathf.Max(1, target * 16);
            int radius = Mathf.Max(1, profile.clusterRadius);
            while (placed < target && attempts-- > 0)
            {
                int centerX = random.Next(1, Mathf.Max(2, map.Width - 1));
                int centerY = random.Next(1, Mathf.Max(2, map.Height - 1));
                float acceptance = Mathf.Lerp(1f, 0.20f + 0.80f * Mathf.Clamp01(map.WorldPlan.GetCell(centerX, centerY).Density),
                    Mathf.Clamp01(profile.clusterDensityBias));
                if (random.NextDouble() > acceptance)
                {
                    continue;
                }

                int clusterSize = Mathf.Min(target - placed, random.Next(2, 5));
                for (int i = 0; i < clusterSize; i++)
                {
                    int x = Mathf.Clamp(centerX + random.Next(-radius, radius + 1), 1, map.Width - 2);
                    int y = Mathf.Clamp(centerY + random.Next(-radius, radius + 1), 1, map.Height - 2);
                    if (TryPlaceObject(map, random, x, y, reservedCells, profile))
                    {
                        placed++;
                    }
                }
            }

            return placed;
        }

        bool TryPlaceObject(PCGMapData map, System.Random random, int x, int y,
            HashSet<int> reservedCells, PCGVisualPlacementProfile profile)
        {
            if (IsInsideEventClearance(map.WorldPlan, x, y, profile.eventClearanceCells))
            {
                return false;
            }

            var cell = map.GetCell(x, y);
            var entry = PickObjectForCell(cell, random);
            if (entry == null)
            {
                return false;
            }

            int width = Mathf.Max(1, entry.footprint?.width ?? 1);
            int height = Mathf.Max(1, entry.footprint?.height ?? 1);
            if (!IsInside(map, x, y, width, height) || IsReserved(reservedCells, map.Width, x, y, width, height))
            {
                return false;
            }

            map.Visuals.Add(new PCGPlacedVisual
            {
                Id = entry.id,
                Asset = entry.asset,
                Kind = PCGPlacedVisualKind.Object,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                BlocksMovement = false,
                BlocksSight = false,
                ScaleMultiplier = entry.scaleMultiplier,
                Role = entry.objectRole,
            });
            Reserve(reservedCells, map.Width, x, y, width, height);
            return true;
        }

        static bool IsInsideEventClearance(PCGWorldPlan plan, int x, int y, float clearanceCells)
        {
            if (plan?.EventAnchors == null || clearanceCells <= 0f)
            {
                return false;
            }

            float clearanceSquared = clearanceCells * clearanceCells;
            for (int i = 0; i < plan.EventAnchors.Length; i++)
            {
                var anchor = plan.EventAnchors[i];
                int anchorX = Mathf.Clamp(Mathf.FloorToInt(anchor.NormalizedX * plan.Width), 0, plan.Width - 1);
                int anchorY = Mathf.Clamp(Mathf.FloorToInt(anchor.NormalizedY * plan.Height), 0, plan.Height - 1);
                float dx = x - anchorX;
                float dy = y - anchorY;
                if (dx * dx + dy * dy <= clearanceSquared)
                {
                    return true;
                }
            }

            return false;
        }

        static bool IsReserved(HashSet<int> reservedCells, int mapWidth, int x, int y, int width, int height)
        {
            for (int offsetY = 0; offsetY < height; offsetY++)
            {
                for (int offsetX = 0; offsetX < width; offsetX++)
                {
                    if (reservedCells.Contains((y + offsetY) * mapWidth + x + offsetX)) return true;
                }
            }

            return false;
        }

        static void Reserve(HashSet<int> reservedCells, int mapWidth, int x, int y, int width, int height)
        {
            for (int offsetY = 0; offsetY < height; offsetY++)
            {
                for (int offsetX = 0; offsetX < width; offsetX++)
                {
                    reservedCells.Add((y + offsetY) * mapWidth + x + offsetX);
                }
            }
        }

        WorldObjectEntry PickObjectForCell(PCGMapCell cell, System.Random random)
        {
            int totalWeight = 0;
            for (int i = 0; i < _assetIndex.Objects.Count; i++)
            {
                var entry = _assetIndex.Objects[i];
                if (CanUseObject(entry, cell)) totalWeight += Mathf.Max(1, entry.weight);
            }
            if (totalWeight <= 0) return null;

            int roll = random.Next(totalWeight);
            for (int i = 0; i < _assetIndex.Objects.Count; i++)
            {
                var entry = _assetIndex.Objects[i];
                if (!CanUseObject(entry, cell)) continue;
                roll -= Mathf.Max(1, entry.weight);
                if (roll < 0) return entry;
            }
            return null;
        }

        static bool CanUseObject(WorldObjectEntry entry, PCGMapCell cell)
        {
            return entry != null && Contains(entry.allowedTerrains, cell.Terrain) &&
                (entry.allowedBiomes == null || entry.allowedBiomes.Length == 0 || Contains(entry.allowedBiomes, cell.Biome));
        }

        static bool IsInside(PCGMapData map, int x, int y, int width, int height)
        {
            return x >= 0 && y >= 0 && x + width <= map.Width && y + height <= map.Height;
        }

        static PCGValidationReport BuildVisualOnlyReport(PCGMapData map)
        {
            return new PCGValidationReport
            {
                IsValid = true,
                WalkableCells = map.Cells.Length,
                ReachableCells = map.Cells.Length,
                UnreachableCells = 0,
                BlockingObjects = 0,
            };
        }

        static int CountVisuals(PCGMapData map, PCGPlacedVisualKind kind)
        {
            int count = 0;
            for (int i = 0; i < map.Visuals.Count; i++) if (map.Visuals[i].Kind == kind) count++;
            return count;
        }

        static string GetCellSummary(PCGMapData map)
        {
            var terrainCounts = new Dictionary<string, int>();
            for (int i = 0; i < map.Cells.Length; i++)
            {
                string terrain = map.Cells[i].Terrain ?? string.Empty;
                terrainCounts.TryGetValue(terrain, out int count);
                terrainCounts[terrain] = count + 1;
            }
            var parts = new List<string>(terrainCounts.Count);
            foreach (var pair in terrainCounts) parts.Add($"{pair.Key}:{pair.Value}");
            parts.Sort(StringComparer.Ordinal);
            return $"walkable={map.Cells.Length}/{map.Cells.Length} terrains={{" + string.Join(",", parts) + "}";
        }

        static bool Contains(string[] values, string value)
        {
            if (values == null) return false;
            for (int i = 0; i < values.Length; i++) if (string.Equals(values[i], value, StringComparison.Ordinal)) return true;
            return false;
        }

        static int DeriveSeed(int seed, int salt)
        {
            unchecked { return seed * 486187739 + salt * 16777619; }
        }

        static ulong ComputeHash(PCGMapData map)
        {
            ulong hash = 1469598103934665603UL;
            hash = PCGHash.Combine(hash, (ulong)map.Seed);
            for (int i = 0; i < map.Cells.Length; i++)
            {
                var cell = map.Cells[i];
                hash = PCGHash.Combine(hash, StableStringHash(cell.Terrain));
                hash = PCGHash.Combine(hash, StableStringHash(cell.BaseAsset));
                hash = PCGHash.Combine(hash, StableStringHash(cell.ZoneId));
            }
            for (int i = 0; i < map.Visuals.Count; i++)
            {
                var visual = map.Visuals[i];
                hash = PCGHash.Combine(hash, StableStringHash(visual.Id));
                hash = PCGHash.Combine(hash, (ulong)(visual.X * 73856093 ^ visual.Y * 19349663));
            }
            return map.WorldPlan == null ? hash : PCGHash.Combine(hash, map.WorldPlan.ContentHash);
        }

        static ulong StableStringHash(string value)
        {
            ulong hash = 1469598103934665603UL;
            if (string.IsNullOrEmpty(value)) return hash;
            for (int i = 0; i < value.Length; i++) { hash ^= value[i]; hash *= 1099511628211UL; }
            return hash;
        }
    }
}
