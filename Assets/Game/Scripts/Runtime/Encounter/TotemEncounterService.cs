using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TotemEncounterService
{
    private const int PositionSpokes = 16;
    private const int PositionAttempts = 192;

    private readonly TotemEncounterClock clock = new TotemEncounterClock();
    private TotemMapSnapshot cachedMap;
    private TotemEncounterReachability cachedReachability;

    public ITotemEncounterClock Clock => clock;

    public TotemSpawnPlan BuildSpawnPlan(
        TotemMapSnapshot map,
        string themeId,
        IReadOnlyList<TotemEncounterSpawnDefinition> encounterDefinitions,
        IReadOnlyList<TotemEnemyDefinition> enemyDefinitions,
        int seed,
        IReadOnlyList<Vector3> activeParticipantPositions = null)
    {
        return BuildSpawnPlan(new TotemSpawnPlanBuildRequest(
            map,
            themeId,
            AdaptEncounterConfigs(encounterDefinitions),
            enemyDefinitions,
            seed,
            activeParticipantPositions));
    }

    public TotemSpawnPlan BuildSpawnPlan(TotemSpawnPlanBuildRequest request)
    {
        var plan = new TotemSpawnPlan
        {
            Seed = request.Seed,
            MapSeed = request.Map?.Seed ?? 0,
            ThemeId = NormalizeId(string.IsNullOrWhiteSpace(request.ThemeId) ? request.Map?.ThemeName : request.ThemeId),
            MapSize = request.Map?.MapSize ?? 0f,
        };

        var entries = new List<TotemSpawnPlanEntry>(96);
        var rejections = new List<TotemSpawnPlanRejection>(16);
        if (request.Map == null || request.Map.MapSize <= 0f)
        {
            rejections.Add(CreateRejection(string.Empty, 0, 0, 0f, string.Empty, TotemSpawnPlanRejectionReason.MissingMap));
            plan.Rejections = rejections.ToArray();
            clock.Reset(plan);
            return plan;
        }

        cachedMap = request.Map;
        cachedReachability = new TotemEncounterReachability(request.Map);
        var configs = CopyAndSortConfigs(request.EncounterConfigs);
        var wavePositions = new Dictionary<int, List<Vector3>>(32);
        for (int configIndex = 0; configIndex < configs.Count; configIndex++)
        {
            var config = configs[configIndex];
            if (!IsConfigForTheme(config, plan.ThemeId))
            {
                continue;
            }

            if (!IsValidConfig(config))
            {
                rejections.Add(CreateRejection(config?.EncounterId, 0, 0, config?.StartTime ?? 0f, string.Empty, TotemSpawnPlanRejectionReason.InvalidConfig));
                continue;
            }

            var enemies = BuildEnemyPool(request.EnemyDefinitions, config, plan.ThemeId);
            if (enemies.Count <= 0)
            {
                rejections.Add(CreateRejection(config.EncounterId, 0, 0, config.StartTime, string.Empty, TotemSpawnPlanRejectionReason.MissingEnemyPool));
                continue;
            }

            var anchors = BuildAnchorPool(request.Map, config);
            if (anchors.Count <= 0)
            {
                rejections.Add(CreateRejection(config.EncounterId, 0, 0, config.StartTime, string.Empty, TotemSpawnPlanRejectionReason.MissingAnchor));
                continue;
            }

            BuildConfigPlan(
                request,
                config,
                enemies,
                anchors,
                cachedReachability,
                wavePositions,
                entries,
                rejections);
        }

        entries.Sort(ComparePlanEntries);
        plan.Entries = entries.ToArray();
        plan.Rejections = rejections.ToArray();
        clock.Reset(plan);
        return plan;
    }

    public bool TryResolveSpawnPosition(
        TotemMapSnapshot map,
        TotemSpawnPlanEntry entry,
        IReadOnlyList<Vector3> activeParticipantPositions,
        IReadOnlyList<Vector3> sameWavePositions,
        out Vector3 position,
        out TotemSpawnPlanRejectionReason reason)
    {
        position = default;
        reason = TotemSpawnPlanRejectionReason.InvalidConfig;
        if (map == null || entry == null)
        {
            return false;
        }

        var anchor = TotemMapService.FindAnchor(map, TotemMapAnchorKind.Encounter, entry.AnchorId, null);
        if (anchor == null && entry.Tier == TotemEnemyTier.Boss)
        {
            anchor = TotemMapService.FindAnchor(map, TotemMapAnchorKind.BossSpawn, entry.AnchorId, null);
        }

        if (anchor == null)
        {
            reason = TotemSpawnPlanRejectionReason.MissingAnchor;
            return false;
        }

        EnsureReachability(map);
        if (IsValidPosition(
            map,
            cachedReachability,
            entry.Position,
            activeParticipantPositions,
            sameWavePositions,
            entry.MinParticipantDistance,
            entry.MinSpacing,
            out reason))
        {
            position = entry.Position;
            reason = TotemSpawnPlanRejectionReason.None;
            return true;
        }

        return TryPlaceNearAnchor(
            map,
            cachedReachability,
            anchor,
            entry.PlacementSeed,
            1,
            activeParticipantPositions,
            sameWavePositions,
            entry.MinParticipantDistance,
            entry.MinSpacing,
            out position,
            out reason);
    }

    public static TotemEncounterSpawnConfig[] CreateConfirmedDefaultConfigs(string themeId)
    {
        string normalizedTheme = NormalizeId(themeId);
        if (string.IsNullOrEmpty(normalizedTheme))
        {
            normalizedTheme = "common";
        }

        string themePool = $"pool_{normalizedTheme}";
        return new[]
        {
            new TotemEncounterSpawnConfig
            {
                EncounterId = $"encounter.{normalizedTheme}.light",
                ThemeId = normalizedTheme,
                ZoneRoles = "inner,mid,outer",
                EnemyPoolIds = $"pool_common,{themePool}",
                Tier = TotemEnemyTier.Light,
                StartTime = 0f,
                EndTime = -1f,
                InitialCount = 18,
                ActiveCap = 30,
                TotalCap = 60,
                WaveMin = 4,
                WaveMax = 6,
                WaveInterval = 45f,
                MinParticipantDistance = 28f,
                MinSpacing = 8f,
                Weight = 1,
            },
            new TotemEncounterSpawnConfig
            {
                EncounterId = $"encounter.{normalizedTheme}.elite",
                ThemeId = normalizedTheme,
                ZoneRoles = "outer,danger",
                EnemyPoolIds = $"pool_common,pool_elite,{themePool}",
                Tier = TotemEnemyTier.Elite,
                StartTime = 240f,
                EndTime = -1f,
                InitialCount = 6,
                ActiveCap = 6,
                TotalCap = 6,
                WaveMin = 0,
                WaveMax = 0,
                WaveInterval = 0f,
                MinParticipantDistance = 36f,
                MinSpacing = 12f,
                Weight = 1,
            },
            new TotemEncounterSpawnConfig
            {
                EncounterId = $"encounter.{normalizedTheme}.boss",
                ThemeId = normalizedTheme,
                ZoneRoles = "boss",
                EnemyPoolIds = $"pool_boss,{themePool}",
                Tier = TotemEnemyTier.Boss,
                StartTime = 600f,
                EndTime = -1f,
                InitialCount = 1,
                ActiveCap = 1,
                TotalCap = 1,
                WaveMin = 0,
                WaveMax = 0,
                WaveInterval = 0f,
                MinParticipantDistance = 45f,
                MinSpacing = 16f,
                Weight = 1,
                Unique = true,
            },
        };
    }

    public static TotemEncounterSpawnConfig[] AdaptEncounterConfigs(IReadOnlyList<TotemEncounterSpawnDefinition> source)
    {
        if (source == null || source.Count <= 0)
        {
            return Array.Empty<TotemEncounterSpawnConfig>();
        }

        var result = new TotemEncounterSpawnConfig[source.Count];
        for (int i = 0; i < source.Count; i++)
        {
            var definition = source[i];
            if (definition == null)
            {
                continue;
            }

            result[i] = new TotemEncounterSpawnConfig
            {
                EncounterId = definition.EncounterId ?? string.Empty,
                ThemeId = definition.ThemeId ?? string.Empty,
                ZoneRoles = definition.ZoneRoles ?? string.Empty,
                EnemyPoolIds = definition.EnemyPoolIds ?? string.Empty,
                Tier = ResolveEncounterTier(definition),
                StartTime = Mathf.Max(0f, definition.StartTime),
                EndTime = definition.EndTime <= 0f ? -1f : definition.EndTime,
                InitialCount = Mathf.Max(0, definition.InitialCount),
                ActiveCap = Mathf.Max(0, definition.ActiveCap),
                TotalCap = Mathf.Max(0, definition.TotalCap),
                WaveMin = Mathf.Max(0, definition.WaveMin),
                WaveMax = Mathf.Max(0, definition.WaveMax),
                WaveInterval = Mathf.Max(0f, definition.WaveInterval),
                MinParticipantDistance = Mathf.Max(0f, definition.MinParticipantDistance),
                MinSpacing = Mathf.Max(0f, definition.MinSpacing),
                Weight = Mathf.Max(0, definition.Weight),
                Unique = definition.Unique,
            };
        }

        return result;
    }

    private static void BuildConfigPlan(
        TotemSpawnPlanBuildRequest request,
        TotemEncounterSpawnConfig config,
        List<TotemEnemyDefinition> enemies,
        List<TotemMapAnchor> anchors,
        TotemEncounterReachability reachability,
        Dictionary<int, List<Vector3>> wavePositions,
        List<TotemSpawnPlanEntry> entries,
        List<TotemSpawnPlanRejection> rejections)
    {
        int totalCap = config.Unique ? 1 : Mathf.Max(config.TotalCap, config.InitialCount);
        int scheduledCount = 0;
        int waveIndex = 0;
        while (scheduledCount < totalCap)
        {
            int remaining = totalCap - scheduledCount;
            int waveCount = ResolveWaveCount(config, request.Seed, waveIndex, remaining);
            if (waveCount <= 0)
            {
                break;
            }

            float triggerTime = config.StartTime + (waveIndex <= 0 ? 0f : config.WaveInterval * waveIndex);
            if (config.EndTime >= 0f && triggerTime > config.EndTime)
            {
                break;
            }

            int waveKey = Mathf.RoundToInt(triggerTime * 1000f);
            if (!wavePositions.TryGetValue(waveKey, out var placedInWave))
            {
                placedInWave = new List<Vector3>(waveCount);
                wavePositions.Add(waveKey, placedInWave);
            }

            for (int waveSlot = 0; waveSlot < waveCount; waveSlot++)
            {
                int placementSeed = StableHash(request.Seed, config.EncounterId, waveIndex, waveSlot);
                var enemy = SelectEnemy(enemies, request.Seed, config.EncounterId, waveIndex, waveSlot);
                int anchorStart = PositiveModulo(StableHash(placementSeed, enemy.EnemyId, 17, 31), anchors.Count);
                if (TryPlace(
                    request.Map,
                    reachability,
                    anchors,
                    anchorStart,
                    placementSeed,
                    request.ActiveParticipantPositions,
                    placedInWave,
                    config.MinParticipantDistance,
                    config.MinSpacing,
                    out var anchor,
                    out var position,
                    out var rejectionReason))
                {
                    placedInWave.Add(position);
                    entries.Add(new TotemSpawnPlanEntry
                    {
                        PlanEntryId = $"{config.EncounterId}:{waveIndex}:{waveSlot}",
                        EncounterId = config.EncounterId ?? string.Empty,
                        EnemyId = enemy.EnemyId ?? string.Empty,
                        EnemyThemeId = NormalizeId(enemy.ThemeId),
                        Tier = config.Tier,
                        AnchorId = anchor.AnchorId ?? string.Empty,
                        ZoneRole = anchor.ZoneRole ?? anchor.PayloadId ?? string.Empty,
                        Position = position,
                        WaveIndex = waveIndex,
                        WaveSlot = waveSlot,
                        TriggerTime = triggerTime,
                        ActiveCap = Mathf.Max(1, config.ActiveCap),
                        TotalCap = totalCap,
                        RetryInterval = Mathf.Max(1f, config.WaveInterval),
                        MinParticipantDistance = Mathf.Max(0f, config.MinParticipantDistance),
                        MinSpacing = Mathf.Max(0f, config.MinSpacing),
                        Unique = config.Unique,
                        PlacementSeed = placementSeed,
                    });
                }
                else
                {
                    rejections.Add(CreateRejection(config.EncounterId, waveIndex, waveSlot, triggerTime, string.Empty, rejectionReason));
                }
            }

            scheduledCount += waveCount;
            waveIndex++;
            if (config.Unique || (config.WaveInterval <= 0f && scheduledCount < totalCap))
            {
                break;
            }
        }
    }

    private static int ResolveWaveCount(TotemEncounterSpawnConfig config, int seed, int waveIndex, int remaining)
    {
        if (config.Unique)
        {
            return Mathf.Min(1, remaining);
        }

        if (waveIndex == 0 && config.InitialCount > 0)
        {
            return Mathf.Min(config.InitialCount, remaining);
        }

        int min = Mathf.Max(0, Mathf.Min(config.WaveMin, config.WaveMax));
        int max = Mathf.Max(min, Mathf.Max(config.WaveMin, config.WaveMax));
        if (max <= 0)
        {
            return 0;
        }

        int range = max - min + 1;
        int count = min + PositiveModulo(StableHash(seed, config.EncounterId, waveIndex, 7919), range);
        return Mathf.Min(count, remaining);
    }

    private static List<TotemEncounterSpawnConfig> CopyAndSortConfigs(IReadOnlyList<TotemEncounterSpawnConfig> source)
    {
        var result = new List<TotemEncounterSpawnConfig>(source?.Count ?? 0);
        for (int i = 0; source != null && i < source.Count; i++)
        {
            if (source[i] != null)
            {
                result.Add(source[i]);
            }
        }

        result.Sort((a, b) =>
        {
            int byTime = a.StartTime.CompareTo(b.StartTime);
            if (byTime != 0)
            {
                return byTime;
            }

            int byTier = a.Tier.CompareTo(b.Tier);
            return byTier != 0 ? byTier : string.CompareOrdinal(a.EncounterId, b.EncounterId);
        });
        return result;
    }

    private static List<TotemEnemyDefinition> BuildEnemyPool(
        IReadOnlyList<TotemEnemyDefinition> source,
        TotemEncounterSpawnConfig config,
        string themeId)
    {
        var result = new List<TotemEnemyDefinition>(source?.Count ?? 0);
        bool hasThemeBoss = false;
        for (int i = 0; source != null && i < source.Count; i++)
        {
            var enemy = source[i];
            if (enemy == null || enemy.Tier != config.Tier || string.IsNullOrWhiteSpace(enemy.EnemyId))
            {
                continue;
            }

            string enemyTheme = NormalizeId(enemy.ThemeId);
            if (enemyTheme != "common" && enemyTheme != themeId)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(config.EnemyPoolIds) && !TokensIntersect(config.EnemyPoolIds, enemy.PoolIds))
            {
                continue;
            }

            result.Add(enemy);
            hasThemeBoss |= config.Tier == TotemEnemyTier.Boss && enemyTheme == themeId;
        }

        if (hasThemeBoss)
        {
            for (int i = result.Count - 1; i >= 0; i--)
            {
                if (NormalizeId(result[i].ThemeId) != themeId)
                {
                    result.RemoveAt(i);
                }
            }
        }

        result.Sort((a, b) => string.CompareOrdinal(a.EnemyId, b.EnemyId));
        return result;
    }

    private static List<TotemMapAnchor> BuildAnchorPool(TotemMapSnapshot map, TotemEncounterSpawnConfig config)
    {
        var result = new List<TotemMapAnchor>(16);
        var anchors = map?.AnchorPlacements;
        for (int i = 0; anchors != null && i < anchors.Length; i++)
        {
            var anchor = anchors[i];
            bool kindMatches = config.Tier == TotemEnemyTier.Boss
                ? anchor?.Kind == TotemMapAnchorKind.BossSpawn
                : anchor?.Kind == TotemMapAnchorKind.Encounter;
            if (!kindMatches || !anchor.IsReachable)
            {
                continue;
            }

            string zoneRole = string.IsNullOrWhiteSpace(anchor.ZoneRole) ? anchor.PayloadId : anchor.ZoneRole;
            if (!DoesAnchorRoleMatch(config, zoneRole))
            {
                continue;
            }

            result.Add(anchor);
        }

        result.Sort((a, b) => string.CompareOrdinal(a.AnchorId, b.AnchorId));
        return result;
    }

    private static bool DoesAnchorRoleMatch(TotemEncounterSpawnConfig config, string anchorRole)
    {
        if (string.IsNullOrWhiteSpace(config.ZoneRoles) || ContainsToken(config.ZoneRoles, anchorRole))
        {
            return true;
        }

        switch (config.Tier)
        {
            case TotemEnemyTier.Light:
                return ContainsToken(config.ZoneRoles, "EnemySpawn")
                    && (string.Equals(anchorRole, "inner", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(anchorRole, "mid", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(anchorRole, "outer", StringComparison.OrdinalIgnoreCase));
            case TotemEnemyTier.Elite:
                return ContainsToken(config.ZoneRoles, "EliteSpawn")
                    && (string.Equals(anchorRole, "outer", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(anchorRole, "danger", StringComparison.OrdinalIgnoreCase));
            case TotemEnemyTier.Boss:
                return ContainsToken(config.ZoneRoles, "BossSpawn")
                    && string.Equals(anchorRole, "boss", StringComparison.OrdinalIgnoreCase);
            default:
                return false;
        }
    }

    private static TotemEnemyTier ResolveEncounterTier(TotemEncounterSpawnDefinition definition)
    {
        string encounterId = definition?.EncounterId ?? string.Empty;
        string zoneRoles = definition?.ZoneRoles ?? string.Empty;
        if (ContainsToken(zoneRoles, "BossSpawn") || encounterId.EndsWith(".boss", StringComparison.OrdinalIgnoreCase))
        {
            return TotemEnemyTier.Boss;
        }

        if (ContainsToken(zoneRoles, "EliteSpawn") || encounterId.EndsWith(".elite", StringComparison.OrdinalIgnoreCase))
        {
            return TotemEnemyTier.Elite;
        }

        return TotemEnemyTier.Light;
    }

    private static bool TryPlace(
        TotemMapSnapshot map,
        TotemEncounterReachability reachability,
        List<TotemMapAnchor> anchors,
        int anchorStart,
        int placementSeed,
        IReadOnlyList<Vector3> participants,
        IReadOnlyList<Vector3> sameWavePositions,
        float minParticipantDistance,
        float minSpacing,
        out TotemMapAnchor selectedAnchor,
        out Vector3 position,
        out TotemSpawnPlanRejectionReason reason)
    {
        selectedAnchor = null;
        position = default;
        reason = TotemSpawnPlanRejectionReason.MissingAnchor;
        for (int i = 0; i < anchors.Count; i++)
        {
            var anchor = anchors[(anchorStart + i) % anchors.Count];
            int anchorSeed = StableHash(placementSeed, anchor.AnchorId, i, 104729);
            if (TryPlaceNearAnchor(
                map,
                reachability,
                anchor,
                anchorSeed,
                0,
                participants,
                sameWavePositions,
                minParticipantDistance,
                minSpacing,
                out position,
                out reason))
            {
                selectedAnchor = anchor;
                return true;
            }
        }

        return false;
    }

    private static bool TryPlaceNearAnchor(
        TotemMapSnapshot map,
        TotemEncounterReachability reachability,
        TotemMapAnchor anchor,
        int placementSeed,
        int firstAttempt,
        IReadOnlyList<Vector3> participants,
        IReadOnlyList<Vector3> sameWavePositions,
        float minParticipantDistance,
        float minSpacing,
        out Vector3 position,
        out TotemSpawnPlanRejectionReason reason)
    {
        position = default;
        reason = TotemSpawnPlanRejectionReason.NotWalkable;
        float step = Mathf.Max(2f, map.TerrainCellSize);
        float searchRadius = Mathf.Max(step, anchor.SearchRadius > 0f ? anchor.SearchRadius : 48f);
        float angleOffset = DeterministicUnit(placementSeed) * Mathf.PI * 2f;
        for (int attempt = firstAttempt; attempt < PositionAttempts; attempt++)
        {
            Vector3 candidate = anchor.Position;
            if (attempt > 0)
            {
                int zeroBased = attempt - 1;
                int ring = zeroBased / PositionSpokes + 1;
                int spoke = zeroBased % PositionSpokes;
                float radius = Mathf.Min(searchRadius, ring * step);
                float angle = angleOffset + spoke * (Mathf.PI * 2f / PositionSpokes);
                candidate.x += Mathf.Cos(angle) * radius;
                candidate.z += Mathf.Sin(angle) * radius;
            }

            candidate = ClampToMap(map, candidate);
            candidate.y = anchor.Position.y;
            if (!IsValidPosition(
                map,
                reachability,
                candidate,
                participants,
                sameWavePositions,
                minParticipantDistance,
                minSpacing,
                out reason))
            {
                continue;
            }

            position = candidate;
            reason = TotemSpawnPlanRejectionReason.None;
            return true;
        }

        return false;
    }

    private static bool IsValidPosition(
        TotemMapSnapshot map,
        TotemEncounterReachability reachability,
        Vector3 candidate,
        IReadOnlyList<Vector3> participants,
        IReadOnlyList<Vector3> sameWavePositions,
        float minParticipantDistance,
        float minSpacing,
        out TotemSpawnPlanRejectionReason reason)
    {
        if (!TotemMapService.IsTerrainWalkable(TotemMapService.QueryTerrain(map, candidate)))
        {
            reason = TotemSpawnPlanRejectionReason.NotWalkable;
            return false;
        }

        if (reachability != null && !reachability.IsReachable(candidate))
        {
            reason = TotemSpawnPlanRejectionReason.NotReachable;
            return false;
        }

        if (IsWithinDistance(candidate, participants, minParticipantDistance))
        {
            reason = TotemSpawnPlanRejectionReason.ParticipantTooClose;
            return false;
        }

        if (IsWithinDistance(candidate, sameWavePositions, minSpacing))
        {
            reason = TotemSpawnPlanRejectionReason.SameWaveTooClose;
            return false;
        }

        reason = TotemSpawnPlanRejectionReason.None;
        return true;
    }

    private static bool IsWithinDistance(Vector3 candidate, IReadOnlyList<Vector3> positions, float distance)
    {
        if (positions == null || distance <= 0f)
        {
            return false;
        }

        float distanceSqr = distance * distance;
        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 delta = positions[i] - candidate;
            delta.y = 0f;
            if (delta.sqrMagnitude < distanceSqr)
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureReachability(TotemMapSnapshot map)
    {
        if (ReferenceEquals(map, cachedMap) && cachedReachability != null)
        {
            return;
        }

        cachedMap = map;
        cachedReachability = new TotemEncounterReachability(map);
    }

    private static TotemEnemyDefinition SelectEnemy(
        List<TotemEnemyDefinition> enemies,
        int seed,
        string encounterId,
        int waveIndex,
        int waveSlot)
    {
        int index = PositiveModulo(StableHash(seed, encounterId, waveIndex, waveSlot), enemies.Count);
        return enemies[index];
    }

    private static bool IsValidConfig(TotemEncounterSpawnConfig config)
    {
        if (config == null || string.IsNullOrWhiteSpace(config.EncounterId) || config.Tier == TotemEnemyTier.Unknown)
        {
            return false;
        }

        if (config.TotalCap <= 0 && config.InitialCount <= 0 && !config.Unique)
        {
            return false;
        }

        return config.StartTime >= 0f && config.ActiveCap >= 0 && config.TotalCap >= 0;
    }

    private static bool IsConfigForTheme(TotemEncounterSpawnConfig config, string themeId)
    {
        string configTheme = NormalizeId(config?.ThemeId);
        return string.IsNullOrEmpty(configTheme) || configTheme == "common" || configTheme == themeId;
    }

    private static TotemSpawnPlanRejection CreateRejection(
        string encounterId,
        int waveIndex,
        int waveSlot,
        float triggerTime,
        string anchorId,
        TotemSpawnPlanRejectionReason reason)
    {
        return new TotemSpawnPlanRejection
        {
            EncounterId = encounterId ?? string.Empty,
            WaveIndex = waveIndex,
            WaveSlot = waveSlot,
            TriggerTime = Mathf.Max(0f, triggerTime),
            AnchorId = anchorId ?? string.Empty,
            Reason = reason,
        };
    }

    private static int ComparePlanEntries(TotemSpawnPlanEntry a, TotemSpawnPlanEntry b)
    {
        int byTime = a.TriggerTime.CompareTo(b.TriggerTime);
        if (byTime != 0)
        {
            return byTime;
        }

        int byEncounter = string.CompareOrdinal(a.EncounterId, b.EncounterId);
        if (byEncounter != 0)
        {
            return byEncounter;
        }

        int byWave = a.WaveIndex.CompareTo(b.WaveIndex);
        return byWave != 0 ? byWave : a.WaveSlot.CompareTo(b.WaveSlot);
    }

    private static bool TokensIntersect(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        int start = 0;
        for (int i = 0; i <= left.Length; i++)
        {
            if (i < left.Length && !IsTokenSeparator(left[i]))
            {
                continue;
            }

            int length = i - start;
            if (length > 0 && ContainsToken(right, left.Substring(start, length)))
            {
                return true;
            }

            start = i + 1;
        }

        return false;
    }

    private static bool ContainsToken(string source, string expected)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        expected = expected.Trim();
        int start = 0;
        for (int i = 0; i <= source.Length; i++)
        {
            if (i < source.Length && !IsTokenSeparator(source[i]))
            {
                continue;
            }

            int length = i - start;
            if (length == expected.Length && string.Compare(source, start, expected, 0, expected.Length, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return true;
            }

            start = i + 1;
        }

        return false;
    }

    private static bool IsTokenSeparator(char value)
    {
        return value == ',' || value == ';' || value == '|' || char.IsWhiteSpace(value);
    }

    private static Vector3 ClampToMap(TotemMapSnapshot map, Vector3 position)
    {
        float margin = Mathf.Max(0.5f, map.TerrainCellSize * 0.5f);
        position.x = Mathf.Clamp(position.x, margin, map.MapSize - margin);
        position.z = Mathf.Clamp(position.z, margin, map.MapSize - margin);
        return position;
    }

    private static string NormalizeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
    }

    private static float DeterministicUnit(int value)
    {
        unchecked
        {
            uint hash = (uint)value;
            hash ^= hash >> 16;
            hash *= 0x7feb352d;
            hash ^= hash >> 15;
            hash *= 0x846ca68b;
            hash ^= hash >> 16;
            return (hash & 0x00FFFFFF) / 16777215f;
        }
    }

    private static int StableHash(int seed, string value, int a, int b)
    {
        unchecked
        {
            uint hash = 2166136261;
            hash = (hash ^ (uint)seed) * 16777619;
            if (!string.IsNullOrEmpty(value))
            {
                for (int i = 0; i < value.Length; i++)
                {
                    hash = (hash ^ value[i]) * 16777619;
                }
            }

            hash = (hash ^ (uint)a) * 16777619;
            hash = (hash ^ (uint)b) * 16777619;
            return (int)(hash & 0x7fffffff);
        }
    }

    private static int PositiveModulo(int value, int divisor)
    {
        return divisor <= 0 ? 0 : value % divisor;
    }

    private sealed class TotemEncounterReachability
    {
        private readonly TotemMapSnapshot map;
        private readonly bool[] reachable;

        public TotemEncounterReachability(TotemMapSnapshot map)
        {
            this.map = map;
            int width = map?.TerrainGridWidth ?? 0;
            int height = map?.TerrainGridHeight ?? 0;
            if (width <= 0 || height <= 0 || map.TerrainGrid == null || map.TerrainGrid.Length < width * height)
            {
                reachable = Array.Empty<bool>();
                return;
            }

            reachable = new bool[width * height];
            var queue = new int[reachable.Length];
            var playerAnchor = TotemMapService.FindAnchor(map, TotemMapAnchorKind.PlayerSpawn);
            Vector3 origin = playerAnchor?.Position ?? new Vector3(map.MapSize * 0.5f, 0f, map.MapSize * 0.5f);
            if (!TryWorldToCell(origin, out int startX, out int startZ))
            {
                return;
            }

            int startIndex = startZ * width + startX;
            if (!IsCellWalkable(startX, startZ))
            {
                return;
            }

            int head = 0;
            int tail = 0;
            reachable[startIndex] = true;
            queue[tail++] = startIndex;
            while (head < tail)
            {
                int index = queue[head++];
                int x = index % width;
                int z = index / width;
                Enqueue(x - 1, z, queue, ref tail);
                Enqueue(x + 1, z, queue, ref tail);
                Enqueue(x, z - 1, queue, ref tail);
                Enqueue(x, z + 1, queue, ref tail);
            }
        }

        public bool IsReachable(Vector3 position)
        {
            if (reachable.Length <= 0)
            {
                return TotemMapService.IsTerrainWalkable(TotemMapService.QueryTerrain(map, position));
            }

            return TryWorldToCell(position, out int x, out int z) && reachable[z * map.TerrainGridWidth + x];
        }

        private void Enqueue(int x, int z, int[] queue, ref int tail)
        {
            if (x < 0 || z < 0 || x >= map.TerrainGridWidth || z >= map.TerrainGridHeight)
            {
                return;
            }

            int index = z * map.TerrainGridWidth + x;
            if (reachable[index] || !IsCellWalkable(x, z))
            {
                return;
            }

            reachable[index] = true;
            queue[tail++] = index;
        }

        private bool IsCellWalkable(int x, int z)
        {
            int index = z * map.TerrainGridWidth + x;
            return index >= 0
                && index < map.TerrainGrid.Length
                && TotemMapService.IsTerrainWalkable((TotemTerrainType)map.TerrainGrid[index]);
        }

        private bool TryWorldToCell(Vector3 position, out int x, out int z)
        {
            float cellSize = Mathf.Max(1f, map.TerrainCellSize);
            x = Mathf.FloorToInt(position.x / cellSize);
            z = Mathf.FloorToInt(position.z / cellSize);
            return x >= 0 && z >= 0 && x < map.TerrainGridWidth && z < map.TerrainGridHeight;
        }
    }
}
