using System;
using System.Collections.Generic;
using UnityEngine;

public enum TotemSpawnPlanRejectionReason : byte
{
    None = 0,
    MissingMap = 1,
    InvalidConfig = 2,
    MissingEnemyPool = 3,
    MissingAnchor = 4,
    NotWalkable = 5,
    NotReachable = 6,
    ParticipantTooClose = 7,
    SameWaveTooClose = 8,
}

[Serializable]
public sealed class TotemEncounterSpawnConfig
{
    public string EncounterId;
    public string ThemeId;
    public string ZoneRoles;
    public string EnemyPoolIds;
    public TotemEnemyTier Tier;
    public float StartTime;
    public float EndTime = -1f;
    public int InitialCount;
    public int ActiveCap;
    public int TotalCap;
    public int WaveMin;
    public int WaveMax;
    public float WaveInterval;
    public float MinParticipantDistance;
    public float MinSpacing;
    public int Weight = 1;
    public bool Unique;
}

[Serializable]
public sealed class TotemSpawnPlanEntry
{
    public string PlanEntryId;
    public string EncounterId;
    public string EnemyId;
    public string EnemyThemeId;
    public TotemEnemyTier Tier;
    public string AnchorId;
    public string ZoneRole;
    public Vector3 Position;
    public int WaveIndex;
    public int WaveSlot;
    public float TriggerTime;
    public int ActiveCap;
    public int TotalCap;
    public float RetryInterval;
    public float MinParticipantDistance;
    public float MinSpacing;
    public bool Unique;
    public int PlacementSeed;
}

[Serializable]
public sealed class TotemSpawnPlanRejection
{
    public string EncounterId;
    public int WaveIndex;
    public int WaveSlot;
    public float TriggerTime;
    public string AnchorId;
    public TotemSpawnPlanRejectionReason Reason;
}

[Serializable]
public sealed class TotemSpawnPlan
{
    public int Seed;
    public int MapSeed;
    public string ThemeId;
    public float MapSize;
    public TotemSpawnPlanEntry[] Entries = Array.Empty<TotemSpawnPlanEntry>();
    public TotemSpawnPlanRejection[] Rejections = Array.Empty<TotemSpawnPlanRejection>();
}

public readonly struct TotemSpawnPlanBuildRequest
{
    public readonly TotemMapSnapshot Map;
    public readonly string ThemeId;
    public readonly IReadOnlyList<TotemEncounterSpawnConfig> EncounterConfigs;
    public readonly IReadOnlyList<TotemEnemyDefinition> EnemyDefinitions;
    public readonly IReadOnlyList<Vector3> ActiveParticipantPositions;
    public readonly int Seed;

    public TotemSpawnPlanBuildRequest(
        TotemMapSnapshot map,
        string themeId,
        IReadOnlyList<TotemEncounterSpawnConfig> encounterConfigs,
        IReadOnlyList<TotemEnemyDefinition> enemyDefinitions,
        int seed,
        IReadOnlyList<Vector3> activeParticipantPositions = null)
    {
        Map = map;
        ThemeId = themeId ?? string.Empty;
        EncounterConfigs = encounterConfigs ?? Array.Empty<TotemEncounterSpawnConfig>();
        EnemyDefinitions = enemyDefinitions ?? Array.Empty<TotemEnemyDefinition>();
        ActiveParticipantPositions = activeParticipantPositions ?? Array.Empty<Vector3>();
        Seed = seed;
    }
}

public readonly struct TotemEncounterClockContext
{
    public readonly bool WorldActive;
    public readonly float WorldTime;
    public readonly int ActiveLightCount;
    public readonly int ActiveEliteCount;
    public readonly int ActiveBossCount;

    public TotemEncounterClockContext(
        bool worldActive,
        float worldTime,
        int activeLightCount,
        int activeEliteCount,
        int activeBossCount)
    {
        WorldActive = worldActive;
        WorldTime = Mathf.Max(0f, worldTime);
        ActiveLightCount = Mathf.Max(0, activeLightCount);
        ActiveEliteCount = Mathf.Max(0, activeEliteCount);
        ActiveBossCount = Mathf.Max(0, activeBossCount);
    }
}

[Serializable]
public sealed class TotemEncounterClockSnapshot
{
    public float worldTime;
    public int processedEntryCount;
    public int spawnedLightCount;
    public int spawnedEliteCount;
    public int spawnedBossCount;
    public int skippedActiveCapCount;
    public int skippedTotalCapCount;
    public int deferredActiveCapCount;
}

public interface ITotemEncounterClock
{
    TotemSpawnPlan Plan { get; }

    void Reset(TotemSpawnPlan plan);

    int CollectDueSpawns(TotemEncounterClockContext context, List<TotemSpawnPlanEntry> output);

    TotemEncounterClockSnapshot CaptureSnapshot();
}
