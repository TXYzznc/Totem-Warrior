using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace PCGMap
{
    public sealed class PCGMapGenerator
    {
        readonly PCGAssetIndex _assetIndex;
        readonly List<int> _scratchQueue = new();
        readonly bool[] _scratchVisited;

        public PCGMapGenerator(PCGAssetIndex assetIndex, int maxWidth = 128, int maxHeight = 128)
        {
            _assetIndex = assetIndex ?? throw new ArgumentNullException(nameof(assetIndex));
            _scratchVisited = new bool[maxWidth * maxHeight];
        }

        public PCGMapData Generate(PCGMapGenerateRequest request)
        {
            var totalWatch = Stopwatch.StartNew();
            if (request.Width <= 0 || request.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(request), "PCG map size must be positive.");
            if (request.Width * request.Height > _scratchVisited.Length)
                throw new ArgumentOutOfRangeException(nameof(request), "PCG map size exceeds generator scratch capacity.");

            long stageStartMs = totalWatch.ElapsedMilliseconds;
            var rng = new System.Random(request.Seed);
            var map = new PCGMapData
            {
                Width = request.Width,
                Height = request.Height,
                Seed = request.Seed,
                EdgeMatchTolerance = request.EdgeMatchTolerance,
                Cells = new PCGMapCell[request.Width * request.Height],
            };
            map.Diagnostics.AddStep(
                "Initialize",
                totalWatch.ElapsedMilliseconds - stageStartMs,
                $"seed={request.Seed} size={request.Width}x{request.Height} cells={request.Width * request.Height} " +
                $"budgets={{objects:{request.ObjectBudget},stamps:{request.StampBudget},decals:{request.DecalBudget}}} " +
                $"zoneWeights={{spawn:{request.TeamSpawnZoneWeight},loot:{request.LootZoneWeight},combat:{request.CombatZoneWeight},danger:{request.DangerZoneWeight}}} " +
                $"edgeTolerance={request.EdgeMatchTolerance:0.###}");

            stageStartMs = totalWatch.ElapsedMilliseconds;
            GenerateCells(map, rng, request);
            map.Diagnostics.AddStep("GenerateCells.Total", totalWatch.ElapsedMilliseconds - stageStartMs, GetCellSummary(map));

            stageStartMs = totalWatch.ElapsedMilliseconds;
            PlacePois(map);
            map.Diagnostics.AddStep("PlacePois", totalWatch.ElapsedMilliseconds - stageStartMs, GetVisualSummary(map, PCGPlacedVisualKind.Poi));

            stageStartMs = totalWatch.ElapsedMilliseconds;
            PlaceStamps(map, rng, request.StampBudget);
            map.Diagnostics.AddStep("PlaceStamps", totalWatch.ElapsedMilliseconds - stageStartMs, GetVisualSummary(map, PCGPlacedVisualKind.Stamp));

            stageStartMs = totalWatch.ElapsedMilliseconds;
            PlaceObjects(map, rng, request.ObjectBudget);
            map.Diagnostics.AddStep("PlaceObjects", totalWatch.ElapsedMilliseconds - stageStartMs, GetVisualSummary(map, PCGPlacedVisualKind.Object));

            stageStartMs = totalWatch.ElapsedMilliseconds;
            PlaceDecals(map, rng, request.DecalBudget);
            map.Diagnostics.AddStep("PlaceDecals", totalWatch.ElapsedMilliseconds - stageStartMs, GetVisualSummary(map, PCGPlacedVisualKind.Decal));

            stageStartMs = totalWatch.ElapsedMilliseconds;
            map.Validation = PCGMapValidator.Validate(map);
            map.Diagnostics.AddStep(
                "Validate",
                totalWatch.ElapsedMilliseconds - stageStartMs,
                $"valid={map.Validation.IsValid} walkable={map.Validation.WalkableCells} reachable={map.Validation.ReachableCells} " +
                $"unreachable={map.Validation.UnreachableCells} blockingObjects={map.Validation.BlockingObjects} resources={map.Validation.ResourceObjects} " +
                $"poi={map.Validation.PoiCount} warnings={map.Validation.Warnings.Count}");

            stageStartMs = totalWatch.ElapsedMilliseconds;
            map.ContentHash = ComputeHash(map);
            map.Diagnostics.AddStep("ComputeHash", totalWatch.ElapsedMilliseconds - stageStartMs, $"hash={map.ContentHash}");
            totalWatch.Stop();
            map.Diagnostics.TotalMs = totalWatch.ElapsedMilliseconds;
            return map;
        }

        void GenerateCells(PCGMapData map, System.Random rng, PCGMapGenerateRequest request)
        {
            var watch = Stopwatch.StartNew();
            var roadMask = BuildRoadMask(map, request);
            map.Diagnostics.AddStep("Cells.BuildRoadMask", watch.ElapsedMilliseconds, $"roadCells={CountRoadCells(roadMask)}");

            long stageStartMs = watch.ElapsedMilliseconds;
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    float nx = (float)x / map.Width;
                    float ny = (float)y / map.Height;
                    float height = PCGHash.SmoothValue01(nx * 5.2f, ny * 5.2f, map.Seed, 11);
                    float moisture = PCGHash.SmoothValue01(nx * 4.1f + 12.3f, ny * 4.1f - 8.7f, map.Seed, 29);
                    float forest = PCGHash.SmoothValue01(nx * 6.0f - 4.5f, ny * 6.0f + 3.2f, map.Seed, 41);

                    var terrain = "grass";
                    var biome = "grassland";
                    var walkable = true;

                    if (height < 0.22f)
                    {
                        terrain = "water";
                        biome = "swamp";
                        walkable = false;
                    }
                    else if (height < 0.31f || moisture > 0.74f)
                    {
                        terrain = "mud";
                        biome = "swamp";
                    }
                    else if (forest > 0.62f && moisture > 0.34f)
                    {
                        terrain = "forest_ground";
                        biome = "forest";
                    }

                    if (IsRoadCell(roadMask, map, x, y))
                    {
                        terrain = "road";
                        biome = "neutral";
                        walkable = true;
                    }

                    var zone = ResolveZone(map, x, y, request);
                    var visual = _assetIndex.PickTerrain(terrain, "inner", biome, rng);
                    var cell = new PCGMapCell
                    {
                        X = x,
                        Y = y,
                        Biome = biome,
                        Terrain = terrain,
                        Walkable = walkable,
                        Occupied = !walkable,
                        ZoneId = zone,
                        BaseAsset = visual?.asset,
                    };

                    map.SetCell(x, y, cell);
                }
            }
            map.Diagnostics.AddStep("Cells.FillBase", watch.ElapsedMilliseconds - stageStartMs, GetCellSummary(map));

            stageStartMs = watch.ElapsedMilliseconds;
            var slicedStats = AssignSlicedTerrainAssets(map, request);
            map.Diagnostics.AddStep(
                "Cells.AssignTerrainTiles",
                watch.ElapsedMilliseconds - stageStartMs,
                $"picked={slicedStats.picked} missing={slicedStats.missing} rotated={slicedStats.rotated} flipped={slicedStats.flipped}");

            stageStartMs = watch.ElapsedMilliseconds;
            int visualsBefore = map.Visuals.Count;
            PlaceTransitionOverlays(map, rng);
            map.Diagnostics.AddStep("Cells.PlaceTransitionOverlays", watch.ElapsedMilliseconds - stageStartMs, GetVisualDeltaSummary(map, visualsBefore));
        }

        (int picked, int missing, int rotated, int flipped) AssignSlicedTerrainAssets(PCGMapData map, PCGMapGenerateRequest request)
        {
            int picked = 0;
            int missing = 0;
            int rotated = 0;
            int flipped = 0;
            var selectedTiles = new TerrainTilePickResult[map.Width, map.Height];
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var cell = map.GetCell(x, y);
                    var southTile = y > 0 ? selectedTiles[x, y - 1] : null;
                    var westTile = x > 0 ? selectedTiles[x - 1, y] : null;
                    string northTerrain = GetTileNeighborTerrain(cell.Terrain, GetNeighborTerrain(map, x, y + 1, cell.Terrain));
                    string eastTerrain = GetTileNeighborTerrain(cell.Terrain, GetNeighborTerrain(map, x + 1, y, cell.Terrain));
                    string southTerrain = GetTileNeighborTerrain(cell.Terrain, GetNeighborTerrain(map, x, y - 1, cell.Terrain));
                    string westTerrain = GetTileNeighborTerrain(cell.Terrain, GetNeighborTerrain(map, x - 1, y, cell.Terrain));
                    var pick = _assetIndex.PickTerrainTile(
                        cell.Terrain,
                        northTerrain,
                        eastTerrain,
                        southTerrain,
                        westTerrain,
                        southTile,
                        westTile,
                        request.EdgeMatchTolerance,
                        x,
                        y,
                        map.Seed);
                    if (pick?.Tile == null)
                    {
                        missing++;
                        continue;
                    }

                    selectedTiles[x, y] = pick;
                    cell.BaseAsset = string.IsNullOrEmpty(pick.Asset) ? pick.Tile.asset : pick.Asset;
                    cell.BaseRotationDegrees = pick.RotationDegrees;
                    cell.BaseFlipX = pick.FlipX;
                    map.SetCell(x, y, cell);
                    picked++;
                    if (Math.Abs(pick.RotationDegrees) > 0.01f)
                        rotated++;
                    if (pick.FlipX)
                        flipped++;
                }
            }

            return (picked, missing, rotated, flipped);
        }

        static string GetTileNeighborTerrain(string terrain, string neighborTerrain)
        {
            if (string.IsNullOrEmpty(neighborTerrain) || terrain == neighborTerrain)
                return terrain;

            return ShouldTerrainOwnTransition(terrain, neighborTerrain) ? neighborTerrain : terrain;
        }

        static bool ShouldTerrainOwnTransition(string terrain, string neighborTerrain)
        {
            return GetTransitionOwnerPriority(terrain) >= GetTransitionOwnerPriority(neighborTerrain);
        }

        static int GetTransitionOwnerPriority(string terrain)
        {
            return terrain switch
            {
                "water" => 100,
                "road" => 90,
                "corruption" => 80,
                "mud" => 70,
                "forest_ground" => 60,
                "ruin_floor" => 55,
                _ => 10,
            };
        }

        static string GetNeighborTerrain(PCGMapData map, int x, int y, string fallback)
        {
            if (x < 0 || x >= map.Width || y < 0 || y >= map.Height)
                return fallback;
            return map.GetCell(x, y).Terrain;
        }

        static bool IsRoadCell(bool[] roadMask, PCGMapData map, int x, int y)
        {
            return roadMask[y * map.Width + x];
        }

        static bool[] BuildRoadMask(PCGMapData map, PCGMapGenerateRequest request)
        {
            var roadMask = new bool[map.Width * map.Height];
            int seed = request.Seed;
            int radius = Math.Max(1, Math.Min(map.Width, map.Height) / 64);

            var spawn = JitterPoint(map.Width / 5, map.Height / 5, map.Width, map.Height, seed, 3101, map.Width / 10, map.Height / 10);
            var combat = JitterPoint(map.Width / 2, map.Height / 2, map.Width, map.Height, seed, 3102, map.Width / 5, map.Height / 5);
            var danger = JitterPoint(map.Width * 4 / 5, map.Height * 4 / 5, map.Width, map.Height, seed, 3103, map.Width / 9, map.Height / 9);
            var loot = JitterPoint(map.Width / 4 + SeedOffset(seed, 3104, map.Width / 2), map.Height * 3 / 4, map.Width, map.Height, seed, 3105, map.Width / 8, map.Height / 8);

            AddRoadPath(roadMask, map.Width, map.Height, spawn, combat, seed, 4101, radius);
            AddRoadPath(roadMask, map.Width, map.Height, combat, danger, seed, 4102, radius);
            AddRoadPath(roadMask, map.Width, map.Height, loot, combat, seed, 4103, radius);

            if (PCGHash.Value01(0, 0, seed, 4104) > 0.35f)
            {
                var branch = RandomEdgePoint(map.Width, map.Height, seed, 4105);
                AddRoadPath(roadMask, map.Width, map.Height, branch, combat, seed, 4106, radius);
            }

            return roadMask;
        }

        static (int x, int y) JitterPoint(int x, int y, int width, int height, int seed, int salt, int jitterX, int jitterY)
        {
            int nextX = x + SeedOffset(seed, salt, jitterX);
            int nextY = y + SeedOffset(seed, salt + 17, jitterY);
            return (Clamp(nextX, 2, Math.Max(2, width - 3)), Clamp(nextY, 2, Math.Max(2, height - 3)));
        }

        static int SeedOffset(int seed, int salt, int amount)
        {
            if (amount <= 0)
                return 0;
            return (int)Math.Round((PCGHash.Value01(0, 0, seed, salt) * 2f - 1f) * amount);
        }

        static (int x, int y) RandomEdgePoint(int width, int height, int seed, int salt)
        {
            int side = Math.Min(3, (int)(PCGHash.Value01(0, 0, seed, salt) * 4f));
            int margin = 2;
            int x = Clamp((int)Math.Round(PCGHash.Value01(0, 0, seed, salt + 1) * (width - 1)), margin, Math.Max(margin, width - margin - 1));
            int y = Clamp((int)Math.Round(PCGHash.Value01(0, 0, seed, salt + 2) * (height - 1)), margin, Math.Max(margin, height - margin - 1));

            return side switch
            {
                0 => (x, margin),
                1 => (width - margin - 1, y),
                2 => (x, height - margin - 1),
                _ => (margin, y),
            };
        }

        static void AddRoadPath(bool[] roadMask, int width, int height, (int x, int y) start, (int x, int y) end, int seed, int salt, int radius)
        {
            int dx = end.x - start.x;
            int dy = end.y - start.y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance < 1.0)
            {
                MarkRoadDisc(roadMask, width, height, start.x, start.y, radius, seed, salt);
                return;
            }

            int steps = Math.Max(1, (int)Math.Round(distance * 3.0));
            double perpX = -dy / distance;
            double perpY = dx / distance;
            double bendA = (PCGHash.Value01(start.x, start.y, seed, salt) * 2.0 - 1.0) * Math.Min(width, height) * 0.18;
            double bendB = (PCGHash.Value01(end.x, end.y, seed, salt + 1) * 2.0 - 1.0) * Math.Min(width, height) * 0.10;

            for (int i = 0; i <= steps; i++)
            {
                double t = i / (double)steps;
                double baseX = start.x + dx * t;
                double baseY = start.y + dy * t;
                double curve = Math.Sin(t * Math.PI) * bendA + Math.Sin(t * Math.PI * 2.0) * bendB;
                double wobble = (PCGHash.SmoothValue01((float)(t * 5.0 + salt * 0.017), salt * 0.013f, seed, salt + 2) * 2.0 - 1.0) * Math.Min(width, height) * 0.045;
                int x = Clamp((int)Math.Round(baseX + perpX * (curve + wobble)), 1, width - 2);
                int y = Clamp((int)Math.Round(baseY + perpY * (curve + wobble)), 1, height - 2);
                MarkRoadDisc(roadMask, width, height, x, y, radius, seed, salt + i);
            }
        }

        static void MarkRoadDisc(bool[] roadMask, int width, int height, int centerX, int centerY, int radius, int seed, int salt)
        {
            int extra = PCGHash.Value01(centerX, centerY, seed, salt) > 0.88f ? 1 : 0;
            int roadRadius = radius + extra;
            int radiusSq = roadRadius * roadRadius;

            for (int y = centerY - roadRadius; y <= centerY + roadRadius; y++)
            {
                if (y < 0 || y >= height)
                    continue;

                for (int x = centerX - roadRadius; x <= centerX + roadRadius; x++)
                {
                    if (x < 0 || x >= width)
                        continue;
                    int dx = x - centerX;
                    int dy = y - centerY;
                    if (dx * dx + dy * dy <= radiusSq)
                        roadMask[y * width + x] = true;
                }
            }
        }

        static int Clamp(int value, int min, int max)
        {
            if (max < min)
                return min;
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        static string ResolveZone(PCGMapData map, int x, int y, PCGMapGenerateRequest request)
        {
            int totalWeight = Math.Max(1,
                Math.Max(0, request.TeamSpawnZoneWeight) +
                Math.Max(0, request.LootZoneWeight) +
                Math.Max(0, request.CombatZoneWeight) +
                Math.Max(0, request.DangerZoneWeight));
            float spawnShare = Math.Max(0, request.TeamSpawnZoneWeight) / (float)totalWeight;
            float combatShare = Math.Max(0, request.CombatZoneWeight) / (float)totalWeight;
            float dangerShare = Math.Max(0, request.DangerZoneWeight) / (float)totalWeight;
            float lootShare = Math.Max(0, request.LootZoneWeight) / (float)totalWeight;

            int spawnX = map.Width / 5;
            int spawnY = map.Height / 5;
            int spawnRadius = Math.Max(3, Math.Min(Math.Min(map.Width, map.Height) / 4, (int)(4f + spawnShare * 18f)));
            if (request.TeamSpawnZoneWeight > 0 && DistanceSq(x, y, spawnX, spawnY) <= spawnRadius * spawnRadius)
                return "team_spawn";

            int dangerX = map.Width * 4 / 5;
            int dangerY = map.Height * 4 / 5;
            int dangerRadius = Math.Max(4, Math.Min(Math.Min(map.Width, map.Height) / 3, (int)(5f + dangerShare * 28f)));
            if (request.DangerZoneWeight > 0 && DistanceSq(x, y, dangerX, dangerY) <= dangerRadius * dangerRadius)
                return "danger_zone";

            float lootCompression = 1f - Math.Min(0.65f, lootShare * 0.9f);
            int combatHalfWidth = Math.Max(2, (int)(map.Width * (0.08f + combatShare * 0.55f) * lootCompression));
            int combatHalfHeight = Math.Max(2, (int)(map.Height * (0.08f + combatShare * 0.55f) * lootCompression));
            if (request.CombatZoneWeight > 0 &&
                Math.Abs(x - map.Width / 2) <= combatHalfWidth &&
                Math.Abs(y - map.Height / 2) <= combatHalfHeight)
                return "combat_zone";

            return "loot_zone";
        }

        static int DistanceSq(int x0, int y0, int x1, int y1)
        {
            int dx = x0 - x1;
            int dy = y0 - y1;
            return dx * dx + dy * dy;
        }

        void PlaceTransitionOverlays(PCGMapData map, System.Random rng)
        {
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var cell = map.GetCell(x, y);
                    AddTransitionOverlay(map, rng, cell, x, y + 1, "north");
                    AddTransitionOverlay(map, rng, cell, x + 1, y, "east");
                    AddTransitionOverlay(map, rng, cell, x, y - 1, "south");
                    AddTransitionOverlay(map, rng, cell, x - 1, y, "west");
                    AddTransitionCornerOverlay(map, rng, cell, x, y + 1, x + 1, y, "north_east");
                    AddTransitionCornerOverlay(map, rng, cell, x, y + 1, x - 1, y, "north_west");
                    AddTransitionCornerOverlay(map, rng, cell, x, y - 1, x + 1, y, "south_east");
                    AddTransitionCornerOverlay(map, rng, cell, x, y - 1, x - 1, y, "south_west");
                }
            }
        }

        void AssignWaterEdgeBaseAssets(PCGMapData map, System.Random rng)
        {
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var cell = map.GetCell(x, y);
                    if (!TryFindWaterEdgeNeighbor(map, cell, out var neighbor, out string direction))
                        continue;

                    var underlay = _assetIndex.PickTerrain(neighbor.Terrain, "inner", neighbor.Biome, rng);
                    var edge = _assetIndex.PickTransitionMask("shore_cutback", direction, rng);
                    if (underlay == null || edge == null)
                        continue;

                    cell.UnderlayAsset = underlay.asset;
                    cell.EdgeBaseAsset = edge.asset;
                    map.SetCell(x, y, cell);
                }
            }
        }

        bool TryFindWaterEdgeNeighbor(PCGMapData map, PCGMapCell cell, out PCGMapCell neighbor, out string direction)
        {
            neighbor = default;
            direction = null;

            int cellPriority = _assetIndex.GetTerrainPriority(cell.Terrain);
            if (cellPriority <= 0)
                return false;

            return TryUseWaterEdgeNeighbor(map, cell, cell.X, cell.Y + 1, "north", cellPriority, out neighbor, out direction) ||
                TryUseWaterEdgeNeighbor(map, cell, cell.X + 1, cell.Y, "east", cellPriority, out neighbor, out direction) ||
                TryUseWaterEdgeNeighbor(map, cell, cell.X, cell.Y - 1, "south", cellPriority, out neighbor, out direction) ||
                TryUseWaterEdgeNeighbor(map, cell, cell.X - 1, cell.Y, "west", cellPriority, out neighbor, out direction);
        }

        bool TryUseWaterEdgeNeighbor(
            PCGMapData map,
            PCGMapCell cell,
            int neighborX,
            int neighborY,
            string candidateDirection,
            int cellPriority,
            out PCGMapCell neighbor,
            out string direction)
        {
            neighbor = default;
            direction = null;
            if (!TryGetCell(map, neighborX, neighborY, out var candidate))
                return false;
            if (candidate.Terrain == cell.Terrain)
                return false;
            if (_assetIndex.GetTerrainPriority(candidate.Terrain) >= cellPriority)
                return false;
            if (_assetIndex.GetTransitionRule(candidate.Terrain, cell.Terrain) == null)
                return false;

            neighbor = candidate;
            direction = candidateDirection;
            return true;
        }

        void AddTransitionOverlay(PCGMapData map, System.Random rng, PCGMapCell cell, int neighborX, int neighborY, string direction)
        {
            if (neighborX < 0 || neighborX >= map.Width || neighborY < 0 || neighborY >= map.Height)
                return;

            var neighbor = map.GetCell(neighborX, neighborY);
            if (neighbor.Terrain == cell.Terrain)
                return;

            int cellPriority = _assetIndex.GetTerrainPriority(cell.Terrain);
            int neighborPriority = _assetIndex.GetTerrainPriority(neighbor.Terrain);
            if (neighborPriority <= cellPriority)
                return;

            var rule = _assetIndex.GetTransitionRule(cell.Terrain, neighbor.Terrain);
            if (rule == null)
                return;

            GetBoundaryOverlayPlacement(cell, neighborX, neighborY, direction, out int visualX, out int visualY, out int visualWidth, out int visualHeight);

            var mask = _assetIndex.PickTransitionMask(rule.maskSet, direction, rng);
            if (mask != null)
            {
                map.Visuals.Add(new PCGPlacedVisual
                {
                    Id = mask.id,
                    Asset = mask.asset,
                    Kind = PCGPlacedVisualKind.TransitionMask,
                    X = visualX,
                    Y = visualY,
                    Width = visualWidth,
                    Height = visualHeight,
                    RotationDegrees = mask.rotationDegrees,
                    SortingOrder = mask.sortingOffset,
                    HasSortingOrder = true,
                    Role = $"{cell.Terrain}->{neighbor.Terrain}:{direction}",
                });
            }

            if (rule.detailSets == null || rule.detailSets.Length == 0)
                return;

            float chance = Math.Max(0f, Math.Min(1f, rule.detailChance));
            if (PCGHash.Value01(cell.X, cell.Y, map.Seed, GetDirectionSalt(direction)) > chance)
                return;

            string detailSet = rule.detailSets[rng.Next(rule.detailSets.Length)];
            var detail = _assetIndex.PickTransitionDetail(detailSet, rng);
            if (detail == null)
                return;
            if (PCGHash.Value01(cell.X, cell.Y, map.Seed, GetDirectionSalt(direction) + 17) > Math.Max(0f, Math.Min(1f, detail.chance)))
                return;

            map.Visuals.Add(new PCGPlacedVisual
            {
                Id = detail.id,
                Asset = detail.asset,
                Kind = PCGPlacedVisualKind.TransitionDetail,
                X = visualX,
                Y = visualY,
                Width = visualWidth,
                Height = visualHeight,
                RotationDegrees = mask?.rotationDegrees ?? 0f,
                SortingOrder = detail.sortingOffset,
                HasSortingOrder = true,
                Role = $"{cell.Terrain}->{neighbor.Terrain}:{direction}",
            });
        }

        static void GetBoundaryOverlayPlacement(
            PCGMapCell cell,
            int neighborX,
            int neighborY,
            string direction,
            out int x,
            out int y,
            out int width,
            out int height)
        {
            if (direction == "east" || direction == "west")
            {
                x = Math.Min(cell.X, neighborX);
                y = cell.Y;
                width = 2;
                height = 1;
                return;
            }

            x = cell.X;
            y = Math.Min(cell.Y, neighborY);
            width = 1;
            height = 2;
        }

        static int GetDirectionSalt(string direction)
        {
            return direction switch
            {
                "north" => 101,
                "east" => 103,
                "south" => 107,
                "west" => 109,
                "north_east" => 127,
                "north_west" => 131,
                "south_east" => 137,
                "south_west" => 139,
                _ => 113,
            };
        }

        void AddTransitionCornerOverlay(
            PCGMapData map,
            System.Random rng,
            PCGMapCell cell,
            int firstX,
            int firstY,
            int secondX,
            int secondY,
            string direction)
        {
            if (!TryGetCell(map, firstX, firstY, out var first) ||
                !TryGetCell(map, secondX, secondY, out var second))
                return;
            if (first.Terrain != second.Terrain || first.Terrain == cell.Terrain)
                return;

            int cellPriority = _assetIndex.GetTerrainPriority(cell.Terrain);
            int neighborPriority = _assetIndex.GetTerrainPriority(first.Terrain);
            if (neighborPriority <= cellPriority)
                return;

            var rule = _assetIndex.GetTransitionRule(cell.Terrain, first.Terrain);
            if (rule == null)
                return;

            var mask = _assetIndex.PickTransitionMask(rule.maskSet, direction, rng);
            if (mask == null)
                return;

            GetCornerOverlayPlacement(cell, direction, out int visualX, out int visualY);
            map.Visuals.Add(new PCGPlacedVisual
            {
                Id = mask.id,
                Asset = mask.asset,
                Kind = PCGPlacedVisualKind.TransitionMask,
                X = visualX,
                Y = visualY,
                Width = 2,
                Height = 2,
                RotationDegrees = mask.rotationDegrees,
                SortingOrder = mask.sortingOffset,
                HasSortingOrder = true,
                Role = $"{cell.Terrain}->{first.Terrain}:{direction}",
            });
        }

        static bool TryGetCell(PCGMapData map, int x, int y, out PCGMapCell cell)
        {
            if (x < 0 || x >= map.Width || y < 0 || y >= map.Height)
            {
                cell = default;
                return false;
            }

            cell = map.GetCell(x, y);
            return true;
        }

        static void GetCornerOverlayPlacement(PCGMapCell cell, string direction, out int x, out int y)
        {
            x = direction.EndsWith("west", StringComparison.Ordinal) ? cell.X - 1 : cell.X;
            y = direction.StartsWith("south", StringComparison.Ordinal) ? cell.Y - 1 : cell.Y;
        }

        void PlacePois(PCGMapData map)
        {
            var points = new (float x, float y)[]
            {
                (0.18f, 0.18f),
                (0.28f, 0.72f),
                (0.68f, 0.52f),
                (0.80f, 0.28f),
                (0.78f, 0.78f),
            };

            int count = Math.Min(points.Length, _assetIndex.Pois.Count);
            for (int i = 0; i < count; i++)
            {
                var entry = _assetIndex.Pois[i];
                int width = Math.Max(1, entry.footprint?.width ?? 1);
                int height = Math.Max(1, entry.footprint?.height ?? 1);
                int x = Math.Max(1, Math.Min(map.Width - width - 1, (int)(map.Width * points[i].x) - width / 2));
                int y = Math.Max(1, Math.Min(map.Height - height - 1, (int)(map.Height * points[i].y) - height / 2));

                if (!CanOccupy(map, x, y, width, height, allowRoad: true))
                    continue;

                Occupy(map, x, y, width, height, blocksMovement: true);
                map.Visuals.Add(new PCGPlacedVisual
                {
                    Id = entry.id,
                    Asset = entry.asset,
                    Kind = PCGPlacedVisualKind.Poi,
                    X = x,
                    Y = y,
                    Width = width,
                    Height = height,
                    BlocksMovement = true,
                    BlocksSight = true,
                    Role = entry.zoneRole,
                });
            }
        }

        void PlaceStamps(PCGMapData map, System.Random rng, int budget)
        {
            for (int i = 0; i < budget; i++)
            {
                var entry = _assetIndex.PickByUseCase("stamp", rng);
                if (entry == null)
                    return;

                int width = Math.Max(1, entry.size?.width ?? 3);
                int height = Math.Max(1, entry.size?.height ?? 3);
                int x = rng.Next(1, Math.Max(2, map.Width - width - 1));
                int y = rng.Next(1, Math.Max(2, map.Height - height - 1));
                if (!CanDecorate(map, x, y, width, height))
                    continue;

                map.Visuals.Add(new PCGPlacedVisual
                {
                    Id = entry.id,
                    Asset = entry.asset,
                    Kind = PCGPlacedVisualKind.Stamp,
                    X = x,
                    Y = y,
                    Width = width,
                    Height = height,
                    Role = entry.biome,
                });
            }
        }

        void PlaceObjects(PCGMapData map, System.Random rng, int budget)
        {
            var placed = 0;
            var attempts = budget * 12;
            while (placed < budget && attempts-- > 0)
            {
                int x = rng.Next(1, map.Width - 2);
                int y = rng.Next(1, map.Height - 2);
                var cell = map.GetCell(x, y);
                if (!cell.Walkable || cell.Occupied || cell.ZoneId == "team_spawn")
                    continue;

                var entry = PickObjectForCell(cell, rng);
                if (entry == null)
                    continue;

                int width = Math.Max(1, entry.footprint?.width ?? 1);
                int height = Math.Max(1, entry.footprint?.height ?? 1);
                if (!CanOccupy(map, x, y, width, height, allowRoad: false))
                    continue;

                Occupy(map, x, y, width, height, entry.blocksMovement);
                map.Visuals.Add(new PCGPlacedVisual
                {
                    Id = entry.id,
                    Asset = entry.asset,
                    Kind = PCGPlacedVisualKind.Object,
                    X = x,
                    Y = y,
                    Width = width,
                    Height = height,
                    BlocksMovement = entry.blocksMovement,
                    BlocksSight = entry.blocksSight,
                    Role = entry.combatRole,
                });
                placed++;
            }
        }

        WorldObjectEntry PickObjectForCell(PCGMapCell cell, System.Random rng)
        {
            var total = 0;
            foreach (var entry in _assetIndex.Objects)
            {
                if (!Contains(entry.allowedTerrains, cell.Terrain))
                    continue;
                if (entry.allowedBiomes != null && entry.allowedBiomes.Length > 0 && !Contains(entry.allowedBiomes, cell.Biome))
                    continue;

                int weight = Math.Max(1, entry.weight);
                if (cell.ZoneId == "combat_zone" && entry.combatRole != null && entry.combatRole.Contains("cover"))
                    weight += 40;
                if (cell.ZoneId == "loot_zone" && entry.objectRole != null && entry.objectRole.Contains("gatherable"))
                    weight += 30;
                total += weight;
            }

            if (total <= 0)
                return null;

            var roll = rng.Next(total);
            foreach (var entry in _assetIndex.Objects)
            {
                if (!Contains(entry.allowedTerrains, cell.Terrain))
                    continue;
                if (entry.allowedBiomes != null && entry.allowedBiomes.Length > 0 && !Contains(entry.allowedBiomes, cell.Biome))
                    continue;

                int weight = Math.Max(1, entry.weight);
                if (cell.ZoneId == "combat_zone" && entry.combatRole != null && entry.combatRole.Contains("cover"))
                    weight += 40;
                if (cell.ZoneId == "loot_zone" && entry.objectRole != null && entry.objectRole.Contains("gatherable"))
                    weight += 30;

                roll -= weight;
                if (roll < 0)
                    return entry;
            }

            return null;
        }

        void PlaceDecals(PCGMapData map, System.Random rng, int budget)
        {
            for (int i = 0; i < budget; i++)
            {
                var entry = _assetIndex.PickByUseCase("decal", rng);
                if (entry == null)
                    return;

                int x = rng.Next(0, map.Width);
                int y = rng.Next(0, map.Height);
                var cell = map.GetCell(x, y);
                if (!cell.Walkable || cell.Terrain == "water")
                    continue;

                map.Visuals.Add(new PCGPlacedVisual
                {
                    Id = entry.id,
                    Asset = entry.asset,
                    Kind = PCGPlacedVisualKind.Decal,
                    X = x,
                    Y = y,
                    Width = 1,
                    Height = 1,
                    Role = entry.terrain,
                });
            }
        }

        static int CountRoadCells(bool[] roadMask)
        {
            int count = 0;
            for (int i = 0; i < roadMask.Length; i++)
            {
                if (roadMask[i])
                    count++;
            }
            return count;
        }

        static string GetCellSummary(PCGMapData map)
        {
            var terrainCounts = new Dictionary<string, int>();
            var zoneCounts = new Dictionary<string, int>();
            int walkable = 0;
            int occupied = 0;
            int baseAssets = 0;
            int rotated = 0;
            int flipped = 0;

            for (int i = 0; i < map.Cells.Length; i++)
            {
                var cell = map.Cells[i];
                IncrementCount(terrainCounts, cell.Terrain);
                IncrementCount(zoneCounts, cell.ZoneId);
                if (cell.Walkable)
                    walkable++;
                if (cell.Occupied)
                    occupied++;
                if (!string.IsNullOrEmpty(cell.BaseAsset))
                    baseAssets++;
                if (Math.Abs(cell.BaseRotationDegrees) > 0.01f)
                    rotated++;
                if (cell.BaseFlipX)
                    flipped++;
            }

            return $"walkable={walkable}/{map.Cells.Length} occupied={occupied} baseAssets={baseAssets} " +
                $"rotated={rotated} flipped={flipped} terrains={FormatCounts(terrainCounts)} zones={FormatCounts(zoneCounts)}";
        }

        static string GetVisualSummary(PCGMapData map, PCGPlacedVisualKind kind)
        {
            int count = 0;
            int blocking = 0;
            int resource = 0;
            for (int i = 0; i < map.Visuals.Count; i++)
            {
                var visual = map.Visuals[i];
                if (visual.Kind != kind)
                    continue;

                count++;
                if (visual.BlocksMovement)
                    blocking++;
                if (!string.IsNullOrEmpty(visual.Role) && visual.Role.Contains("resource"))
                    resource++;
            }

            return $"count={count} blocking={blocking} resources={resource} totalVisuals={map.Visuals.Count}";
        }

        static string GetVisualDeltaSummary(PCGMapData map, int startIndex)
        {
            var kindCounts = new Dictionary<string, int>();
            for (int i = startIndex; i < map.Visuals.Count; i++)
            {
                IncrementCount(kindCounts, map.Visuals[i].Kind.ToString());
            }

            return $"added={map.Visuals.Count - startIndex} kinds={FormatCounts(kindCounts)} totalVisuals={map.Visuals.Count}";
        }

        static void IncrementCount(Dictionary<string, int> counts, string key)
        {
            if (string.IsNullOrEmpty(key))
                key = "(none)";

            counts.TryGetValue(key, out int count);
            counts[key] = count + 1;
        }

        static string FormatCounts(Dictionary<string, int> counts)
        {
            if (counts.Count == 0)
                return "{}";

            var parts = new List<string>(counts.Count);
            foreach (var pair in counts)
            {
                parts.Add($"{pair.Key}:{pair.Value}");
            }
            parts.Sort(StringComparer.Ordinal);
            return "{" + string.Join(",", parts) + "}";
        }

        static bool Contains(string[] values, string target)
        {
            if (values == null)
                return false;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == target)
                    return true;
            }
            return false;
        }

        static bool CanDecorate(PCGMapData map, int x, int y, int width, int height)
        {
            for (int yy = y; yy < y + height; yy++)
            {
                for (int xx = x; xx < x + width; xx++)
                {
                    if (xx < 0 || yy < 0 || xx >= map.Width || yy >= map.Height)
                        return false;
                    var cell = map.GetCell(xx, yy);
                    if (!cell.Walkable || cell.Terrain == "water" || cell.Terrain == "road")
                        return false;
                }
            }
            return true;
        }

        static bool CanOccupy(PCGMapData map, int x, int y, int width, int height, bool allowRoad)
        {
            for (int yy = y; yy < y + height; yy++)
            {
                for (int xx = x; xx < x + width; xx++)
                {
                    if (xx < 0 || yy < 0 || xx >= map.Width || yy >= map.Height)
                        return false;
                    var cell = map.GetCell(xx, yy);
                    if (!cell.Walkable || cell.Occupied)
                        return false;
                    if (!allowRoad && cell.Terrain == "road")
                        return false;
                }
            }
            return true;
        }

        static void Occupy(PCGMapData map, int x, int y, int width, int height, bool blocksMovement)
        {
            if (!blocksMovement)
                return;

            for (int yy = y; yy < y + height; yy++)
            {
                for (int xx = x; xx < x + width; xx++)
                {
                    var cell = map.GetCell(xx, yy);
                    cell.Occupied = true;
                    map.SetCell(xx, yy, cell);
                }
            }
        }

        static ulong ComputeHash(PCGMapData map)
        {
            ulong hash = 1469598103934665603UL;
            hash = PCGHash.Combine(hash, (ulong)map.Seed);
            hash = PCGHash.Combine(hash, (ulong)map.Width);
            hash = PCGHash.Combine(hash, (ulong)map.Height);
            hash = PCGHash.Combine(hash, (ulong)Math.Max(0, (int)(map.EdgeMatchTolerance * 10000f)));

            for (int i = 0; i < map.Cells.Length; i++)
            {
                var cell = map.Cells[i];
                hash = PCGHash.Combine(hash, StableStringHash(cell.Terrain));
                hash = PCGHash.Combine(hash, StableStringHash(cell.UnderlayAsset));
                hash = PCGHash.Combine(hash, StableStringHash(cell.BaseAsset));
                hash = PCGHash.Combine(hash, (ulong)Math.Max(0, (int)((cell.BaseRotationDegrees + 360f) * 10f)));
                hash = PCGHash.Combine(hash, cell.BaseFlipX ? 1UL : 0UL);
                hash = PCGHash.Combine(hash, StableStringHash(cell.EdgeBaseAsset));
                hash = PCGHash.Combine(hash, cell.Occupied ? 1UL : 0UL);
            }

            foreach (var visual in map.Visuals)
            {
                hash = PCGHash.Combine(hash, StableStringHash(visual.Id));
                hash = PCGHash.Combine(hash, StableStringHash(visual.Asset));
                hash = PCGHash.Combine(hash, (ulong)visual.Kind);
                hash = PCGHash.Combine(hash, (ulong)(visual.X * 73856093 ^ visual.Y * 19349663));
                hash = PCGHash.Combine(hash, (ulong)(visual.Width * 83492791 ^ visual.Height * 265443576));
            }

            return hash;
        }

        static ulong StableStringHash(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0UL;

            unchecked
            {
                ulong hash = 1469598103934665603UL;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= 1099511628211UL;
                }
                return hash;
            }
        }
    }
}
