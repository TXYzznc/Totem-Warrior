using System;
using System.Collections.Generic;
using UnityEngine;

namespace PCGMap
{
    /// <summary>配置驱动的宏观地貌与微观事件布局器；不包含 Unity 资源或玩法效果。</summary>
    public sealed class PCGWorldPlanner
    {
        readonly PCGWorldProfileCatalog _profiles;

        public PCGWorldPlanner(PCGWorldProfileCatalog profiles)
        {
            _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        }

        public PCGWorldPlan Generate(PCGMapGenerateRequest request)
        {
            var profile = _profiles.Resolve(request.ThemeId);
            if (profile == null || string.IsNullOrWhiteSpace(profile.baseTerrain))
            {
                throw new InvalidOperationException($"No PCG world profile is available for theme {request.ThemeId}.");
            }

            var plan = new PCGWorldPlan
            {
                ThemeId = profile.themeId,
                ThemeIdText = profile.themeKey ?? profile.themeId.ToString(),
                BiomeId = profile.biomeId ?? string.Empty,
                Seed = request.Seed,
                Width = request.Width,
                Height = request.Height,
                ProfileVersion = profile.version ?? string.Empty,
                VisualPlacement = profile.visualPlacement ?? new PCGVisualPlacementProfile(),
                Cells = new PCGWorldCell[request.Width * request.Height],
            };

            FillBase(plan, profile);
            ApplyFeatures(plan, profile);
            EnsureTerrainMinimums(plan, profile);
            BuildRegionsAndDensity(plan);
            plan.EventAnchors = GenerateEventAnchors(plan, profile);
            plan.ContentHash = ComputeHash(plan);
            plan.Diagnostics.Add($"profile={plan.ThemeIdText}@{plan.ProfileVersion}");
            plan.Diagnostics.Add($"terrain={BuildTerrainSummary(plan)}");
            plan.Diagnostics.Add($"events={plan.EventAnchors.Length}");
            return plan;
        }

        static void FillBase(PCGWorldPlan plan, PCGThemeWorldProfile profile)
        {
            var terrain = profile.FindTerrain(profile.baseTerrain);
            PCGCapabilityKind futureCapabilities = ResolveFutureCapabilities(terrain);
            for (int y = 0; y < plan.Height; y++)
            {
                for (int x = 0; x < plan.Width; x++)
                {
                    plan.Cells[y * plan.Width + x] = new PCGWorldCell
                    {
                        X = x,
                        Y = y,
                        TerrainId = profile.baseTerrain,
                        RegionId = "calm",
                        Density = 0f,
                        Capabilities = PCGCapabilityKind.Visual,
                        FutureCapabilities = futureCapabilities,
                    };
                }
            }
        }

        static void ApplyFeatures(PCGWorldPlan plan, PCGThemeWorldProfile profile)
        {
            if (profile.features == null)
            {
                return;
            }

            for (int i = 0; i < profile.features.Count; i++)
            {
                var recipe = profile.features[i];
                if (recipe == null || string.IsNullOrWhiteSpace(recipe.terrainId))
                {
                    continue;
                }

                int minCount = Mathf.Max(0, recipe.minCount);
                int maxCount = Mathf.Max(minCount, recipe.maxCount);
                var random = new System.Random(DeriveSeed(plan.Seed, 1009 + i * 137));
                int count = minCount == maxCount ? minCount : random.Next(minCount, maxCount + 1);
                for (int featureIndex = 0; featureIndex < count; featureIndex++)
                {
                    int radius = random.Next(Mathf.Max(1, recipe.minRadius), Mathf.Max(1, recipe.maxRadius) + 1);
                    string operation = recipe.operation ?? "blob";
                    switch (operation.ToLowerInvariant())
                    {
                        case "ribbon":
                            PaintRibbon(plan, profile, recipe, random, radius, featureIndex);
                            break;
                        case "chain":
                            PaintChain(plan, profile, recipe, random, radius, featureIndex);
                            break;
                        case "scatter":
                            PaintScatter(plan, profile, recipe, random, radius, featureIndex);
                            break;
                        case "fringe":
                            PaintFringe(plan, profile, recipe, random, featureIndex);
                            break;
                        default:
                            PaintBlobAtRandom(plan, profile, recipe, random, radius, featureIndex);
                            break;
                    }
                }
            }
        }

        static void PaintBlobAtRandom(PCGWorldPlan plan, PCGThemeWorldProfile profile, PCGTerrainFeatureRecipe recipe, System.Random random, int radius, int salt)
        {
            int margin = Mathf.Clamp(recipe.edgeMargin, 0, Mathf.Min(plan.Width, plan.Height) / 3);
            int x = random.Next(Mathf.Max(margin, radius), Mathf.Max(Mathf.Max(margin, radius) + 1, plan.Width - Mathf.Max(margin, radius)));
            int y = random.Next(Mathf.Max(margin, radius), Mathf.Max(Mathf.Max(margin, radius) + 1, plan.Height - Mathf.Max(margin, radius)));
            PaintBlob(plan, profile, recipe, x, y, radius, Mathf.Max(1, Mathf.RoundToInt(radius * (0.65f + (float)random.NextDouble() * 0.7f))), salt);
        }

        static void PaintScatter(PCGWorldPlan plan, PCGThemeWorldProfile profile, PCGTerrainFeatureRecipe recipe, System.Random random, int radius, int salt)
        {
            int scatterCount = Mathf.Max(2, radius);
            for (int i = 0; i < scatterCount; i++)
            {
                PaintBlobAtRandom(plan, profile, recipe, random, Mathf.Max(1, radius / 2), salt * 31 + i);
            }
        }

        static void PaintChain(PCGWorldPlan plan, PCGThemeWorldProfile profile, PCGTerrainFeatureRecipe recipe, System.Random random, int radius, int salt)
        {
            int margin = Mathf.Max(recipe.edgeMargin, radius + 1);
            int startX = random.Next(margin, Mathf.Max(margin + 1, plan.Width - margin));
            int startY = random.Next(margin, Mathf.Max(margin + 1, plan.Height - margin));
            int endX = Mathf.Clamp(startX + random.Next(-plan.Width / 3, plan.Width / 3 + 1), margin, plan.Width - margin - 1);
            int endY = Mathf.Clamp(startY + random.Next(-plan.Height / 3, plan.Height / 3 + 1), margin, plan.Height - margin - 1);
            int steps = Mathf.Max(3, Mathf.RoundToInt(Vector2.Distance(new Vector2(startX, startY), new Vector2(endX, endY)) / Mathf.Max(1f, radius * 1.3f)));
            for (int step = 0; step <= steps; step++)
            {
                float t = step / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(startX, endX, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(startY, endY, t));
                PaintBlob(plan, profile, recipe, x, y, radius, Mathf.Max(1, radius - 1), salt * 67 + step);
            }
        }

        static void PaintRibbon(PCGWorldPlan plan, PCGThemeWorldProfile profile, PCGTerrainFeatureRecipe recipe, System.Random random, int radius, int salt)
        {
            int margin = Mathf.Max(recipe.edgeMargin, radius + 1);
            bool horizontal = random.Next(0, 2) == 0;
            int previousX = horizontal ? margin : random.Next(margin, Mathf.Max(margin + 1, plan.Width - margin));
            int previousY = horizontal ? random.Next(margin, Mathf.Max(margin + 1, plan.Height - margin)) : margin;
            int steps = horizontal ? Mathf.Max(4, plan.Width - margin * 2) : Mathf.Max(4, plan.Height - margin * 2);
            for (int step = 0; step <= steps; step++)
            {
                float t = step / (float)steps;
                int main = Mathf.RoundToInt(Mathf.Lerp(margin, horizontal ? plan.Width - margin - 1 : plan.Height - margin - 1, t));
                float bend = Mathf.Sin((t * 2.6f + (float)random.NextDouble()) * Mathf.PI) * Mathf.Max(2f, radius * 2f);
                int x = horizontal ? main : Mathf.Clamp(Mathf.RoundToInt(previousX + bend + random.Next(-1, 2)), margin, plan.Width - margin - 1);
                int y = horizontal ? Mathf.Clamp(Mathf.RoundToInt(previousY + bend + random.Next(-1, 2)), margin, plan.Height - margin - 1) : main;
                PaintLine(plan, profile, recipe, previousX, previousY, x, y, Mathf.Max(1, recipe.width + radius / 2), salt * 101 + step);
                previousX = x;
                previousY = y;
            }
        }

        static void PaintFringe(PCGWorldPlan plan, PCGThemeWorldProfile profile, PCGTerrainFeatureRecipe recipe, System.Random random, int salt)
        {
            if (string.IsNullOrWhiteSpace(recipe.sourceTerrain))
            {
                return;
            }

            var candidates = new List<int>();
            for (int y = 1; y < plan.Height - 1; y++)
            {
                for (int x = 1; x < plan.Width - 1; x++)
                {
                    var cell = plan.GetCell(x, y);
                    if (!string.Equals(cell.TerrainId, profile.baseTerrain, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (HasNeighborTerrain(plan, x, y, recipe.sourceTerrain))
                    {
                        candidates.Add(y * plan.Width + x);
                    }
                }
            }

            Shuffle(candidates, random);
            int count = Mathf.Min(candidates.Count, Mathf.Max(0, recipe.maxCount) * Mathf.Max(1, recipe.maxRadius));
            for (int i = 0; i < count; i++)
            {
                int index = candidates[i];
                int x = index % plan.Width;
                int y = index / plan.Width;
                SetTerrain(plan, profile, recipe.terrainId, x, y);
            }
        }

        static bool HasNeighborTerrain(PCGWorldPlan plan, int x, int y, string terrainId)
        {
            return IsTerrain(plan, x - 1, y, terrainId) || IsTerrain(plan, x + 1, y, terrainId) ||
                   IsTerrain(plan, x, y - 1, terrainId) || IsTerrain(plan, x, y + 1, terrainId);
        }

        static bool IsTerrain(PCGWorldPlan plan, int x, int y, string terrainId)
        {
            return x >= 0 && y >= 0 && x < plan.Width && y < plan.Height &&
                   string.Equals(plan.GetCell(x, y).TerrainId, terrainId, StringComparison.Ordinal);
        }

        static void PaintLine(PCGWorldPlan plan, PCGThemeWorldProfile profile, PCGTerrainFeatureRecipe recipe, int x0, int y0, int x1, int y1, int radius, int salt)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 0f : i / (float)steps;
                PaintBlob(plan, profile, recipe, Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), radius, radius, salt + i);
            }
        }

        static void PaintBlob(PCGWorldPlan plan, PCGThemeWorldProfile profile, PCGTerrainFeatureRecipe recipe, int centerX, int centerY, int radiusX, int radiusY, int salt)
        {
            float noise = Mathf.Clamp01(recipe.noise);
            for (int y = Mathf.Max(0, centerY - radiusY - 1); y <= Mathf.Min(plan.Height - 1, centerY + radiusY + 1); y++)
            {
                for (int x = Mathf.Max(0, centerX - radiusX - 1); x <= Mathf.Min(plan.Width - 1, centerX + radiusX + 1); x++)
                {
                    float dx = (x - centerX) / (float)Mathf.Max(1, radiusX);
                    float dy = (y - centerY) / (float)Mathf.Max(1, radiusY);
                    float boundary = 1f + (PCGHash.Value01(x, y, plan.Seed, salt) - 0.5f) * noise;
                    if (dx * dx + dy * dy <= boundary)
                    {
                        SetTerrain(plan, profile, recipe.terrainId, x, y);
                    }
                }
            }
        }

        static void SetTerrain(PCGWorldPlan plan, PCGThemeWorldProfile profile, string terrainId, int x, int y)
        {
            int index = y * plan.Width + x;
            var cell = plan.Cells[index];
            cell.TerrainId = terrainId;
            cell.Capabilities = PCGCapabilityKind.Visual;
            cell.FutureCapabilities = ResolveFutureCapabilities(profile.FindTerrain(terrainId));
            plan.Cells[index] = cell;
        }

        static void EnsureTerrainMinimums(PCGWorldPlan plan, PCGThemeWorldProfile profile)
        {
            for (int terrainIndex = 0; terrainIndex < profile.terrains.Count; terrainIndex++)
            {
                var terrain = profile.terrains[terrainIndex];
                if (terrain == null || string.IsNullOrWhiteSpace(terrain.terrainId))
                {
                    continue;
                }

                int required = Mathf.CeilToInt(plan.Cells.Length * Mathf.Clamp01(terrain.minAreaRatio));
                int actual = CountTerrain(plan, terrain.terrainId);
                var random = new System.Random(DeriveSeed(plan.Seed, 4001 + terrainIndex * 149));
                int attempts = plan.Cells.Length * 8;
                while (actual < required && attempts-- > 0)
                {
                    int x = random.Next(1, Mathf.Max(2, plan.Width - 1));
                    int y = random.Next(1, Mathf.Max(2, plan.Height - 1));
                    var cell = plan.GetCell(x, y);
                    if (!string.Equals(cell.TerrainId, profile.baseTerrain, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    SetTerrain(plan, profile, terrain.terrainId, x, y);
                    actual++;
                }

                if (actual < required)
                {
                    plan.Diagnostics.Add($"terrain-minimum-unsatisfied:{terrain.terrainId}:{actual}/{required}");
                }
            }
        }

        static void BuildRegionsAndDensity(PCGWorldPlan plan)
        {
            for (int y = 0; y < plan.Height; y++)
            {
                for (int x = 0; x < plan.Width; x++)
                {
                    int index = y * plan.Width + x;
                    var cell = plan.Cells[index];
                    float density = PCGHash.SmoothValue01(x * 0.15f, y * 0.15f, plan.Seed, 6001);
                    float danger = PCGHash.SmoothValue01(x * 0.09f, y * 0.09f, plan.Seed, 6007);
                    cell.Density = density;
                    cell.RegionId = danger > 0.74f ? "danger" : density > 0.62f ? "combat" : density < 0.28f ? "quiet" : "explore";
                    plan.Cells[index] = cell;
                }
            }
        }

        static PCGWorldEventAnchor[] GenerateEventAnchors(PCGWorldPlan plan, PCGThemeWorldProfile profile)
        {
            var anchors = new List<PCGWorldEventAnchor>(64);
            if (profile.events == null)
            {
                return anchors.ToArray();
            }

            for (int ruleIndex = 0; ruleIndex < profile.events.Count; ruleIndex++)
            {
                var rule = profile.events[ruleIndex];
                if (rule == null || string.IsNullOrWhiteSpace(rule.eventType) || rule.minCount <= 0)
                {
                    continue;
                }

                var selected = SelectEventCells(plan, rule, DeriveSeed(plan.Seed, 7001 + ruleIndex * 173));
                for (int i = 0; i < selected.Count; i++)
                {
                    int cellIndex = selected[i];
                    var cell = plan.Cells[cellIndex];
                    float offsetX = 0.18f + PCGHash.Value01(cell.X, cell.Y, plan.Seed, 7109 + i) * 0.64f;
                    float offsetY = 0.18f + PCGHash.Value01(cell.X, cell.Y, plan.Seed, 7127 + i) * 0.64f;
                    anchors.Add(new PCGWorldEventAnchor
                    {
                        Id = $"{rule.eventType.Replace('_', '.')}.{i:000}",
                        EventType = rule.eventType,
                        VisualRole = string.IsNullOrWhiteSpace(rule.visualRole) ? rule.eventType : rule.visualRole,
                        TerrainId = cell.TerrainId,
                        RegionId = cell.RegionId,
                        NormalizedX = (cell.X + offsetX) / plan.Width,
                        NormalizedY = (cell.Y + offsetY) / plan.Height,
                        Order = anchors.Count,
                    });
                }

                if (selected.Count < rule.minCount)
                {
                    plan.Diagnostics.Add($"event-minimum-unsatisfied:{rule.eventType}:{selected.Count}/{rule.minCount}");
                }
            }

            return anchors.ToArray();
        }

        static List<int> SelectEventCells(PCGWorldPlan plan, PCGEventLayoutRule rule, int seed)
        {
            var candidates = new List<EventCandidate>(plan.Cells.Length);
            int margin = Mathf.Clamp(rule.edgeMargin, 0, Mathf.Min(plan.Width, plan.Height) / 3);
            for (int y = margin; y < plan.Height - margin; y++)
            {
                for (int x = margin; x < plan.Width - margin; x++)
                {
                    var cell = plan.GetCell(x, y);
                    float terrainWeight = ResolveAffinity(rule.terrainAffinity, cell.TerrainId);
                    float regionWeight = ResolveAffinity(rule.regionAffinity, cell.RegionId);
                    if (terrainWeight <= 0f || regionWeight <= 0f)
                    {
                        continue;
                    }

                    float score = PCGHash.Value01(x, y, seed, 7207) / Mathf.Max(0.01f, terrainWeight * regionWeight);
                    candidates.Add(new EventCandidate(y * plan.Width + x, score));
                }
            }

            candidates.Sort(EventCandidateComparer.Instance);
            int maxCount = Mathf.Max(rule.minCount, rule.maxCount);
            int targetCount = rule.minCount == maxCount ? rule.minCount : new System.Random(seed).Next(rule.minCount, maxCount + 1);
            var selected = new List<int>(targetCount);
            float spacingSquared = Mathf.Max(0f, rule.minSpacingCells) * Mathf.Max(0f, rule.minSpacingCells);
            for (int i = 0; i < candidates.Count && selected.Count < targetCount; i++)
            {
                int candidate = candidates[i].CellIndex;
                if (IsFarEnough(plan, selected, candidate, spacingSquared))
                {
                    selected.Add(candidate);
                }
            }

            return selected;
        }

        static bool IsFarEnough(PCGWorldPlan plan, List<int> selected, int candidate, float spacingSquared)
        {
            int candidateX = candidate % plan.Width;
            int candidateY = candidate / plan.Width;
            for (int i = 0; i < selected.Count; i++)
            {
                int x = selected[i] % plan.Width;
                int y = selected[i] / plan.Width;
                float dx = candidateX - x;
                float dy = candidateY - y;
                if (dx * dx + dy * dy < spacingSquared)
                {
                    return false;
                }
            }

            return true;
        }

        static float ResolveAffinity(Dictionary<string, float> affinities, string key)
        {
            if (affinities == null || affinities.Count == 0)
            {
                return 1f;
            }

            return affinities.TryGetValue(key ?? string.Empty, out float weight) ? weight : 0f;
        }

        static PCGCapabilityKind ResolveFutureCapabilities(PCGTerrainProfile terrain)
        {
            PCGCapabilityKind result = PCGCapabilityKind.None;
            if (terrain?.futureCapabilities == null)
            {
                return result;
            }

            for (int i = 0; i < terrain.futureCapabilities.Length; i++)
            {
                if (Enum.TryParse(terrain.futureCapabilities[i], true, out PCGCapabilityKind capability))
                {
                    result |= capability;
                }
            }

            return result;
        }

        static int CountTerrain(PCGWorldPlan plan, string terrainId)
        {
            int count = 0;
            for (int i = 0; i < plan.Cells.Length; i++)
            {
                if (string.Equals(plan.Cells[i].TerrainId, terrainId, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        static void Shuffle(List<int> values, System.Random random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }

        static int DeriveSeed(int seed, int salt)
        {
            unchecked
            {
                return seed * 486187739 + salt * 16777619;
            }
        }

        static ulong ComputeHash(PCGWorldPlan plan)
        {
            ulong hash = 1469598103934665603UL;
            hash = PCGHash.Combine(hash, (ulong)plan.Seed);
            hash = PCGHash.Combine(hash, (ulong)plan.ThemeId);
            hash = PCGHash.Combine(hash, StableStringHash(plan.ProfileVersion));
            for (int i = 0; i < plan.Cells.Length; i++)
            {
                var cell = plan.Cells[i];
                hash = PCGHash.Combine(hash, StableStringHash(cell.TerrainId));
                hash = PCGHash.Combine(hash, StableStringHash(cell.RegionId));
                hash = PCGHash.Combine(hash, (ulong)cell.Capabilities);
                hash = PCGHash.Combine(hash, (ulong)cell.FutureCapabilities);
            }

            for (int i = 0; i < plan.EventAnchors.Length; i++)
            {
                var anchor = plan.EventAnchors[i];
                hash = PCGHash.Combine(hash, StableStringHash(anchor.Id));
                hash = PCGHash.Combine(hash, (ulong)Mathf.RoundToInt(anchor.NormalizedX * 100000f));
                hash = PCGHash.Combine(hash, (ulong)Mathf.RoundToInt(anchor.NormalizedY * 100000f));
            }

            return hash;
        }

        static ulong StableStringHash(string value)
        {
            ulong hash = 1469598103934665603UL;
            if (string.IsNullOrEmpty(value))
            {
                return hash;
            }

            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 1099511628211UL;
            }

            return hash;
        }

        static string BuildTerrainSummary(PCGWorldPlan plan)
        {
            var counts = new Dictionary<string, int>();
            for (int i = 0; i < plan.Cells.Length; i++)
            {
                string key = plan.Cells[i].TerrainId ?? string.Empty;
                counts.TryGetValue(key, out int count);
                counts[key] = count + 1;
            }

            var parts = new List<string>(counts.Count);
            foreach (var pair in counts)
            {
                parts.Add($"{pair.Key}:{pair.Value}");
            }

            parts.Sort(StringComparer.Ordinal);
            return string.Join(",", parts);
        }

        readonly struct EventCandidate
        {
            public readonly int CellIndex;
            public readonly float Score;

            public EventCandidate(int cellIndex, float score)
            {
                CellIndex = cellIndex;
                Score = score;
            }
        }

        sealed class EventCandidateComparer : IComparer<EventCandidate>
        {
            public static readonly EventCandidateComparer Instance = new();

            public int Compare(EventCandidate left, EventCandidate right)
            {
                int score = left.Score.CompareTo(right.Score);
                return score != 0 ? score : left.CellIndex.CompareTo(right.CellIndex);
            }
        }
    }
}
