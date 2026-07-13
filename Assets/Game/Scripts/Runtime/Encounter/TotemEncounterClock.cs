using System;
using System.Collections.Generic;

public sealed class TotemEncounterClock : ITotemEncounterClock
{
    private readonly Dictionary<string, int> spawnedByEncounter = new Dictionary<string, int>(16, StringComparer.Ordinal);
    private bool[] processedEntries = Array.Empty<bool>();
    private float[] retryAfter = Array.Empty<float>();
    private float lastWorldTime;
    private int processedEntryCount;
    private int spawnedLightCount;
    private int spawnedEliteCount;
    private int spawnedBossCount;
    private int skippedActiveCapCount;
    private int skippedTotalCapCount;
    private int deferredActiveCapCount;

    public TotemSpawnPlan Plan { get; private set; }

    public void Reset(TotemSpawnPlan plan)
    {
        Plan = plan;
        int entryCount = plan?.Entries?.Length ?? 0;
        processedEntries = entryCount == 0 ? Array.Empty<bool>() : new bool[entryCount];
        retryAfter = entryCount == 0 ? Array.Empty<float>() : new float[entryCount];
        spawnedByEncounter.Clear();
        for (int i = 0; i < entryCount; i++)
        {
            string encounterId = plan.Entries[i]?.EncounterId ?? string.Empty;
            if (!spawnedByEncounter.ContainsKey(encounterId))
            {
                spawnedByEncounter.Add(encounterId, 0);
            }
        }

        lastWorldTime = 0f;
        processedEntryCount = 0;
        spawnedLightCount = 0;
        spawnedEliteCount = 0;
        spawnedBossCount = 0;
        skippedActiveCapCount = 0;
        skippedTotalCapCount = 0;
        deferredActiveCapCount = 0;
    }

    public int CollectDueSpawns(TotemEncounterClockContext context, List<TotemSpawnPlanEntry> output)
    {
        if (output == null)
        {
            throw new ArgumentNullException(nameof(output));
        }

        if (!context.WorldActive || Plan?.Entries == null || context.WorldTime < lastWorldTime)
        {
            return 0;
        }

        int added = 0;
        int projectedLight = context.ActiveLightCount;
        int projectedElite = context.ActiveEliteCount;
        int projectedBoss = context.ActiveBossCount;
        for (int i = 0; i < Plan.Entries.Length; i++)
        {
            if (processedEntries[i])
            {
                continue;
            }

            var entry = Plan.Entries[i];
            if (entry == null || entry.TriggerTime > context.WorldTime || retryAfter[i] > context.WorldTime)
            {
                continue;
            }

            spawnedByEncounter.TryGetValue(entry.EncounterId ?? string.Empty, out int encounterTotal);
            if (entry.TotalCap > 0 && encounterTotal >= entry.TotalCap)
            {
                processedEntries[i] = true;
                processedEntryCount++;
                skippedTotalCapCount++;
                continue;
            }

            int activeCount = GetActiveCount(entry.Tier, projectedLight, projectedElite, projectedBoss);
            if (entry.ActiveCap > 0 && activeCount >= entry.ActiveCap)
            {
                skippedActiveCapCount++;
                deferredActiveCapCount++;
                retryAfter[i] = context.WorldTime + Math.Max(1f, entry.RetryInterval);
                continue;
            }

            processedEntries[i] = true;
            processedEntryCount++;
            output.Add(entry);
            added++;
            spawnedByEncounter[entry.EncounterId ?? string.Empty] = encounterTotal + 1;
            switch (entry.Tier)
            {
                case TotemEnemyTier.Light:
                    projectedLight++;
                    spawnedLightCount++;
                    break;
                case TotemEnemyTier.Elite:
                    projectedElite++;
                    spawnedEliteCount++;
                    break;
                case TotemEnemyTier.Boss:
                    projectedBoss++;
                    spawnedBossCount++;
                    break;
            }
        }

        lastWorldTime = context.WorldTime;
        return added;
    }

    public TotemEncounterClockSnapshot CaptureSnapshot()
    {
        return new TotemEncounterClockSnapshot
        {
            worldTime = lastWorldTime,
            processedEntryCount = processedEntryCount,
            spawnedLightCount = spawnedLightCount,
            spawnedEliteCount = spawnedEliteCount,
            spawnedBossCount = spawnedBossCount,
            skippedActiveCapCount = skippedActiveCapCount,
            skippedTotalCapCount = skippedTotalCapCount,
            deferredActiveCapCount = deferredActiveCapCount,
        };
    }

    private static int GetActiveCount(TotemEnemyTier tier, int lightCount, int eliteCount, int bossCount)
    {
        switch (tier)
        {
            case TotemEnemyTier.Light:
                return lightCount;
            case TotemEnemyTier.Elite:
                return eliteCount;
            case TotemEnemyTier.Boss:
                return bossCount;
            default:
                return 0;
        }
    }
}
