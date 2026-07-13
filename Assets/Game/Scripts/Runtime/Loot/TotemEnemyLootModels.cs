using System;
using System.Collections.Generic;
using UnityEngine;

public interface ITotemEnemyLootDefinitionSource
{
    int DefinitionCount { get; }

    TotemEnemyLootDefinition GetDefinition(int index);
}

public interface ITotemEnemyDeathEventSource
{
    event Action<TotemEnemyDiedEvent> EnemyDied;
}

public sealed class TotemEnemyLootDefinitionArraySource : ITotemEnemyLootDefinitionSource
{
    private readonly IReadOnlyList<TotemEnemyLootDefinition> definitions;

    public TotemEnemyLootDefinitionArraySource(IReadOnlyList<TotemEnemyLootDefinition> definitions)
    {
        this.definitions = definitions;
    }

    public int DefinitionCount => definitions?.Count ?? 0;

    public TotemEnemyLootDefinition GetDefinition(int index)
    {
        return definitions != null && index >= 0 && index < definitions.Count
            ? definitions[index]
            : null;
    }
}

public sealed class TotemLootPickupModel
{
    internal TotemLootPickupModel(
        int pickupId,
        int sourceEnemyCombatantId,
        string sourceEnemyId,
        string lootEntryId,
        TotemEnemyLootRewardType rewardType,
        string itemId,
        int count,
        Vector3 position,
        float spawnedWorldTime,
        string duplicateRecipePaintItemId,
        int duplicateRecipePaintCount)
    {
        PickupId = pickupId;
        SourceEnemyCombatantId = sourceEnemyCombatantId;
        SourceEnemyId = sourceEnemyId ?? string.Empty;
        LootEntryId = lootEntryId ?? string.Empty;
        RewardType = rewardType;
        ItemId = itemId ?? string.Empty;
        Count = Mathf.Max(0, count);
        Position = position;
        SpawnedWorldTime = Mathf.Max(0f, spawnedWorldTime);
        DuplicateRecipePaintItemId = duplicateRecipePaintItemId ?? string.Empty;
        DuplicateRecipePaintCount = Mathf.Max(0, duplicateRecipePaintCount);
    }

    public int PickupId { get; }

    public int SourceEnemyCombatantId { get; }

    public string SourceEnemyId { get; }

    public string LootEntryId { get; }

    public TotemEnemyLootRewardType RewardType { get; }

    public string ItemId { get; }

    public int Count { get; }

    public Vector3 Position { get; }

    public float SpawnedWorldTime { get; }

    public string DuplicateRecipePaintItemId { get; }

    public int DuplicateRecipePaintCount { get; }

    public bool IsClaimed { get; private set; }

    public int ClaimedByParticipantId { get; private set; }

    internal void MarkClaimed(int participantId)
    {
        IsClaimed = true;
        ClaimedByParticipantId = participantId;
    }
}

public readonly struct TotemLootPickupResult
{
    public readonly bool Succeeded;
    public readonly string Reason;
    public readonly int PickupId;
    public readonly int ParticipantId;
    public readonly TotemEnemyLootRewardType RewardType;
    public readonly string ItemId;
    public readonly int GrantedCount;
    public readonly bool RecipeUnlocked;
    public readonly bool DuplicateRecipeConverted;
    public readonly string ConversionPaintItemId;
    public readonly int ConversionPaintCount;

    public TotemLootPickupResult(
        bool succeeded,
        string reason,
        int pickupId,
        int participantId,
        TotemEnemyLootRewardType rewardType,
        string itemId,
        int grantedCount,
        bool recipeUnlocked,
        bool duplicateRecipeConverted,
        string conversionPaintItemId,
        int conversionPaintCount)
    {
        Succeeded = succeeded;
        Reason = reason ?? string.Empty;
        PickupId = pickupId;
        ParticipantId = participantId;
        RewardType = rewardType;
        ItemId = itemId ?? string.Empty;
        GrantedCount = Mathf.Max(0, grantedCount);
        RecipeUnlocked = recipeUnlocked;
        DuplicateRecipeConverted = duplicateRecipeConverted;
        ConversionPaintItemId = conversionPaintItemId ?? string.Empty;
        ConversionPaintCount = Mathf.Max(0, conversionPaintCount);
    }
}

[Serializable]
public sealed class TotemLootInventoryStackSnapshot
{
    public string rewardType = string.Empty;
    public string itemId = string.Empty;
    public int count;
}

[Serializable]
public sealed class TotemEnemyLootSnapshot
{
    public int definitionCount;
    public int activePickupCount;
    public int processedEnemyDeathCount;
    public int totalSpawnedPickupCount;
    public int totalClaimedPickupCount;
    public int lastSourceEnemyCombatantId;
    public int lastPickupId;
    public int lastClaimParticipantId;
    public string lastLootEntryId = string.Empty;
    public string lastPickupReason = string.Empty;
}
