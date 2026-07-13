using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TotemEnemyLootGenerator
{
    private const int WeightedRollBasis = 100;
    private const int DuplicateRecipePaintCount = 2;

    private readonly List<TotemEnemyLootDefinition> definitions = new List<TotemEnemyLootDefinition>(64);

    public int DefinitionCount => definitions.Count;

    public void ReloadDefinitions(ITotemEnemyLootDefinitionSource source)
    {
        definitions.Clear();
        int count = source?.DefinitionCount ?? 0;
        for (int i = 0; i < count; i++)
        {
            var definition = source.GetDefinition(i);
            if (!IsValid(definition))
            {
                continue;
            }

            definitions.Add(Copy(definition));
        }

        definitions.Sort(CompareDefinitions);
    }

    public int Generate(
        in TotemEnemyDiedEvent evt,
        int runSeed,
        List<TotemLootPickupModel> output,
        ref int nextPickupId)
    {
        if (evt.Enemy == null || output == null || string.IsNullOrWhiteSpace(evt.Enemy.LootTableId))
        {
            return 0;
        }

        var enemy = evt.Enemy;
        var random = new DeterministicRandom(CreateSeed(runSeed, enemy));
        string duplicateRecipePaintItemId = FindDuplicateRecipePaintItemId(enemy);
        int generatedCount = 0;

        for (int i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            if (!IsApplicable(definition, enemy) || !IsGuaranteed(definition, enemy.GuaranteedLootIds))
            {
                continue;
            }

            AddPickup(
                evt,
                definition,
                duplicateRecipePaintItemId,
                ref random,
                output,
                ref nextPickupId,
                generatedCount++);
        }

        int totalWeight = 0;
        for (int i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            if (IsApplicable(definition, enemy) && !IsGuaranteed(definition, enemy.GuaranteedLootIds))
            {
                totalWeight += definition.Weight;
            }
        }

        if (totalWeight <= 0)
        {
            return generatedCount;
        }

        int weightedRoll = random.NextInt(Mathf.Max(WeightedRollBasis, totalWeight));
        if (weightedRoll >= totalWeight)
        {
            return generatedCount;
        }

        int cumulativeWeight = 0;
        for (int i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            if (!IsApplicable(definition, enemy) || IsGuaranteed(definition, enemy.GuaranteedLootIds))
            {
                continue;
            }

            cumulativeWeight += definition.Weight;
            if (weightedRoll >= cumulativeWeight)
            {
                continue;
            }

            AddPickup(
                evt,
                definition,
                duplicateRecipePaintItemId,
                ref random,
                output,
                ref nextPickupId,
                generatedCount++);
            break;
        }

        return generatedCount;
    }

    public bool ValidateTierRules(TotemEnemyModel enemy, out string reason)
    {
        reason = string.Empty;
        if (enemy == null || string.IsNullOrWhiteSpace(enemy.LootTableId))
        {
            reason = "Enemy or LootTableId is missing.";
            return false;
        }

        bool guaranteedCoin = false;
        bool guaranteedPaint = false;
        bool guaranteedRecipe = false;
        bool weightedSupply = false;
        bool weightedEquipment = false;
        bool bossPaintRange = false;

        for (int i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            if (!IsApplicable(definition, enemy))
            {
                continue;
            }

            bool guaranteed = IsGuaranteed(definition, enemy.GuaranteedLootIds);
            guaranteedCoin |= guaranteed && definition.RewardType == TotemEnemyLootRewardType.Coin;
            guaranteedPaint |= guaranteed && definition.RewardType == TotemEnemyLootRewardType.Paint;
            guaranteedRecipe |= guaranteed && definition.RewardType == TotemEnemyLootRewardType.Recipe;
            weightedSupply |= !guaranteed && definition.Weight > 0;
            weightedEquipment |= !guaranteed
                && definition.Weight > 0
                && (definition.RewardType == TotemEnemyLootRewardType.Weapon
                    || definition.RewardType == TotemEnemyLootRewardType.Equipment);
            bossPaintRange |= guaranteed
                && definition.RewardType == TotemEnemyLootRewardType.Paint
                && definition.MinCount == 2
                && definition.MaxCount == 3;
        }

        switch (enemy.Tier)
        {
            case TotemEnemyTier.Light:
                if (guaranteedCoin && weightedSupply)
                {
                    return true;
                }

                reason = "Light loot requires guaranteed Coin and at least one weighted supply row.";
                return false;
            case TotemEnemyTier.Elite:
                if (guaranteedCoin && guaranteedPaint && weightedEquipment)
                {
                    return true;
                }

                reason = "Elite loot requires guaranteed Coin/Paint and weighted Weapon or Equipment.";
                return false;
            case TotemEnemyTier.Boss:
                if (guaranteedCoin && guaranteedRecipe && guaranteedPaint && bossPaintRange)
                {
                    return true;
                }

                reason = "Boss loot requires guaranteed Recipe, Coin and configured Paint count 2-3.";
                return false;
            default:
                reason = "Enemy tier is unknown.";
                return false;
        }
    }

    private string FindDuplicateRecipePaintItemId(TotemEnemyModel enemy)
    {
        for (int i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            if (IsApplicable(definition, enemy)
                && definition.RewardType == TotemEnemyLootRewardType.Paint
                && IsGuaranteed(definition, enemy.GuaranteedLootIds))
            {
                return definition.ItemId;
            }
        }

        return string.Empty;
    }

    private static void AddPickup(
        in TotemEnemyDiedEvent evt,
        TotemEnemyLootDefinition definition,
        string duplicateRecipePaintItemId,
        ref DeterministicRandom random,
        List<TotemLootPickupModel> output,
        ref int nextPickupId,
        int spawnIndex)
    {
        int count = random.NextInclusive(definition.MinCount, definition.MaxCount);
        float angle = random.NextInt(360) * Mathf.Deg2Rad;
        float radius = 0.3f + 0.15f * (spawnIndex % 4);
        var offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        output.Add(new TotemLootPickupModel(
            nextPickupId++,
            evt.Enemy.CombatantId,
            evt.Enemy.EnemyId,
            definition.LootEntryId,
            definition.RewardType,
            definition.ItemId,
            count,
            evt.Enemy.Position + offset,
            evt.WorldTime,
            definition.RewardType == TotemEnemyLootRewardType.Recipe ? duplicateRecipePaintItemId : string.Empty,
            definition.RewardType == TotemEnemyLootRewardType.Recipe ? DuplicateRecipePaintCount : 0));
    }

    private static bool IsApplicable(TotemEnemyLootDefinition definition, TotemEnemyModel enemy)
    {
        if (!string.Equals(definition.LootTableId, enemy.LootTableId, StringComparison.Ordinal)
            || (definition.TierFilter != TotemEnemyTier.Unknown && definition.TierFilter != enemy.Tier))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(definition.ThemeId)
            || string.Equals(definition.ThemeId, "common", StringComparison.OrdinalIgnoreCase)
            || string.Equals(definition.ThemeId, enemy.ThemeId, StringComparison.Ordinal);
    }

    private static bool IsGuaranteed(TotemEnemyLootDefinition definition, string guaranteedLootIds)
    {
        return definition.Guaranteed || ContainsDelimitedId(guaranteedLootIds, definition.LootEntryId);
    }

    private static bool ContainsDelimitedId(string source, string expected)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        int start = 0;
        for (int i = 0; i <= source.Length; i++)
        {
            bool atEnd = i == source.Length;
            if (!atEnd && source[i] != ',' && source[i] != ';' && source[i] != '|')
            {
                continue;
            }

            int tokenStart = start;
            int tokenEnd = i;
            while (tokenStart < tokenEnd && char.IsWhiteSpace(source[tokenStart])) tokenStart++;
            while (tokenEnd > tokenStart && char.IsWhiteSpace(source[tokenEnd - 1])) tokenEnd--;
            int tokenLength = tokenEnd - tokenStart;
            if (tokenLength == expected.Length
                && string.CompareOrdinal(source, tokenStart, expected, 0, tokenLength) == 0)
            {
                return true;
            }

            start = i + 1;
        }

        return false;
    }

    private static bool IsValid(TotemEnemyLootDefinition definition)
    {
        return definition != null
            && !string.IsNullOrWhiteSpace(definition.LootEntryId)
            && !string.IsNullOrWhiteSpace(definition.LootTableId)
            && !string.IsNullOrWhiteSpace(definition.ItemId)
            && definition.RewardType != TotemEnemyLootRewardType.Unknown
            && definition.MinCount > 0
            && definition.MaxCount >= definition.MinCount
            && (definition.Guaranteed || definition.Weight > 0);
    }

    private static TotemEnemyLootDefinition Copy(TotemEnemyLootDefinition source)
    {
        return new TotemEnemyLootDefinition
        {
            LootEntryId = source.LootEntryId ?? string.Empty,
            LootTableId = source.LootTableId ?? string.Empty,
            ItemId = source.ItemId ?? string.Empty,
            RewardType = source.RewardType,
            MinCount = source.MinCount,
            MaxCount = source.MaxCount,
            Weight = source.Weight,
            Guaranteed = source.Guaranteed,
            TierFilter = source.TierFilter,
            ThemeId = source.ThemeId ?? string.Empty,
        };
    }

    private static int CompareDefinitions(TotemEnemyLootDefinition left, TotemEnemyLootDefinition right)
    {
        return string.CompareOrdinal(left?.LootEntryId, right?.LootEntryId);
    }

    private static uint CreateSeed(int runSeed, TotemEnemyModel enemy)
    {
        unchecked
        {
            uint hash = 2166136261u;
            hash = Mix(hash, (uint)runSeed);
            hash = Mix(hash, (uint)enemy.CombatantId);
            hash = Mix(hash, (uint)enemy.EncounterInstanceId);
            hash = MixString(hash, enemy.EnemyId);
            hash = MixString(hash, enemy.LootTableId);
            return hash == 0u ? 0x9E3779B9u : hash;
        }
    }

    private static uint Mix(uint hash, uint value)
    {
        unchecked
        {
            hash ^= value;
            return hash * 16777619u;
        }
    }

    private static uint MixString(uint hash, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Mix(hash, 0u);
        }

        unchecked
        {
            for (int i = 0; i < value.Length; i++)
            {
                hash = Mix(hash, value[i]);
            }

            return hash;
        }
    }

    private struct DeterministicRandom
    {
        private uint state;

        public DeterministicRandom(uint seed)
        {
            state = seed == 0u ? 0x9E3779B9u : seed;
        }

        public int NextInclusive(int minimum, int maximum)
        {
            if (maximum <= minimum)
            {
                return minimum;
            }

            return minimum + NextInt(maximum - minimum + 1);
        }

        public int NextInt(int exclusiveMaximum)
        {
            if (exclusiveMaximum <= 1)
            {
                return 0;
            }

            uint value = NextUInt();
            return (int)(value % (uint)exclusiveMaximum);
        }

        private uint NextUInt()
        {
            uint value = state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            state = value;
            return value;
        }
    }
}
