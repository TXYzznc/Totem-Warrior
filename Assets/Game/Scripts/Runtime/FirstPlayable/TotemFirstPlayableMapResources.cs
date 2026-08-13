using System;
using UnityEngine;

public enum TotemMapResourceCategory : byte
{
    None = 0,
    Pigment = 1,
    Basic = 2,
}

[Serializable]
public sealed class TotemMapResourcePickupDefinition
{
    public string PickupId = string.Empty;
    public TotemMapResourceCategory Category;
    public string ResourceId = string.Empty;
    public TotemPigmentKind Pigment;
    public int MinAmount;
    public int MaxAmount;
    public int Weight;
    public int MinRound = 1;
    public int MaxRound = 3;
    public string AssetKey = string.Empty;
    public bool Enabled;

    public bool IsValid =>
        Enabled
        && !string.IsNullOrWhiteSpace(PickupId)
        && !string.IsNullOrWhiteSpace(ResourceId)
        && MinAmount > 0
        && MaxAmount >= MinAmount
        && Weight > 0
        && MinRound >= 1
        && MaxRound >= MinRound
        && Category == TotemMapResourceCategory.Pigment
        && (Pigment == TotemPigmentKind.Fire
            || Pigment == TotemPigmentKind.Ice
            || Pigment == TotemPigmentKind.Lightning);
}

[Serializable]
public sealed class TotemMapResourcePickupCatalogEntry
{
    public string pickupId = string.Empty;
    public string category = string.Empty;
    public string resourceId = string.Empty;
    public string element = string.Empty;
    public int minAmount;
    public int maxAmount;
    public int weight;
    public int minRound = 1;
    public int maxRound = 3;
    public string assetKey = string.Empty;
    public bool enabled;

    public TotemMapResourcePickupDefinition ToDefinition()
    {
        return new TotemMapResourcePickupDefinition
        {
            PickupId = pickupId ?? string.Empty,
            Category = ParseCategory(category),
            ResourceId = resourceId ?? string.Empty,
            Pigment = ParsePigment(element),
            MinAmount = minAmount,
            MaxAmount = maxAmount,
            Weight = weight,
            MinRound = minRound,
            MaxRound = maxRound,
            AssetKey = assetKey ?? string.Empty,
            Enabled = enabled,
        };
    }

    private static TotemMapResourceCategory ParseCategory(string value) =>
        Enum.TryParse(value, true, out TotemMapResourceCategory parsed)
            ? parsed
            : TotemMapResourceCategory.None;

    private static TotemPigmentKind ParsePigment(string value) =>
        Enum.TryParse(value, true, out TotemPigmentKind parsed)
            ? parsed
            : default;
}

public readonly struct TotemMapResourcePickup
{
    public TotemMapResourcePickup(
        int instanceId,
        string pickupId,
        TotemMapResourceCategory category,
        string resourceId,
        TotemPigmentKind pigment,
        int amount,
        int round,
        string anchorId,
        string assetKey,
        Vector3 position)
    {
        InstanceId = instanceId;
        PickupId = pickupId ?? string.Empty;
        Category = category;
        ResourceId = resourceId ?? string.Empty;
        Pigment = pigment;
        Amount = amount;
        Round = round;
        AnchorId = anchorId ?? string.Empty;
        AssetKey = assetKey ?? string.Empty;
        Position = position;
    }

    public int InstanceId { get; }
    public string PickupId { get; }
    public TotemMapResourceCategory Category { get; }
    public string ResourceId { get; }
    public TotemPigmentKind Pigment { get; }
    public int Amount { get; }
    public int Round { get; }
    public string AnchorId { get; }
    public string AssetKey { get; }
    public Vector3 Position { get; }
    public bool IsValid => InstanceId > 0 && Amount > 0 && !string.IsNullOrWhiteSpace(PickupId);
}

public readonly struct TotemMapResourcePickupResult
{
    public TotemMapResourcePickupResult(bool succeeded, string reason, in TotemMapResourcePickup pickup)
    {
        Succeeded = succeeded;
        Reason = reason ?? string.Empty;
        Pickup = pickup;
    }

    public bool Succeeded { get; }
    public string Reason { get; }
    public TotemMapResourcePickup Pickup { get; }
}

public static class TotemMapResourceGenerator
{
    public const int MaxPickupCount = 64;

    public static int Generate(
        TotemMapResourcePickupDefinition[] definitions,
        TotemMapSnapshot map,
        int matchSeed,
        int round,
        TotemMapResourcePickup[] output)
    {
        if (definitions == null || map == null || output == null || output.Length == 0 || round < 1)
        {
            return 0;
        }

        TotemMapAnchor[] anchors = TotemMapService.FindAnchors(map, TotemMapAnchorKind.Resource);
        Array.Sort(anchors, CompareAnchors);
        int count = 0;
        for (int anchorIndex = 0; anchorIndex < anchors.Length && count < output.Length; anchorIndex++)
        {
            TotemMapAnchor anchor = anchors[anchorIndex];
            if (anchor == null || !anchor.IsReachable || string.IsNullOrWhiteSpace(anchor.AnchorId))
            {
                continue;
            }

            int totalWeight = 0;
            for (int i = 0; i < definitions.Length; i++)
            {
                TotemMapResourcePickupDefinition definition = definitions[i];
                if (IsEligible(definition, round))
                {
                    totalWeight += definition.Weight;
                }
            }

            if (totalWeight <= 0)
            {
                break;
            }

            uint selectionRoll = StableUInt(matchSeed, round, anchor.AnchorId, 0xA341316Cu);
            int selectedWeight = (int)(selectionRoll % (uint)totalWeight);
            TotemMapResourcePickupDefinition selected = null;
            for (int i = 0; i < definitions.Length; i++)
            {
                TotemMapResourcePickupDefinition candidate = definitions[i];
                if (!IsEligible(candidate, round))
                {
                    continue;
                }

                if (selectedWeight < candidate.Weight)
                {
                    selected = candidate;
                    break;
                }

                selectedWeight -= candidate.Weight;
            }

            if (selected == null)
            {
                continue;
            }

            int range = selected.MaxAmount - selected.MinAmount + 1;
            uint amountRoll = StableUInt(matchSeed, round, anchor.AnchorId, 0xC8013EA4u);
            int amount = selected.MinAmount + (int)(amountRoll % (uint)range);
            output[count] = new TotemMapResourcePickup(
                round * 1000 + count + 1,
                selected.PickupId,
                selected.Category,
                selected.ResourceId,
                selected.Pigment,
                amount,
                round,
                anchor.AnchorId,
                selected.AssetKey,
                anchor.Position);
            count++;
        }

        return count;
    }

    public static bool ValidateDefinitions(TotemMapResourcePickupDefinition[] definitions, out string error)
    {
        if (definitions == null || definitions.Length == 0)
        {
            error = "At least one map resource pickup definition is required.";
            return false;
        }

        for (int i = 0; i < definitions.Length; i++)
        {
            TotemMapResourcePickupDefinition current = definitions[i];
            if (current == null || !current.IsValid)
            {
                error = $"Map resource pickup definition at index {i} is invalid.";
                return false;
            }

            for (int j = 0; j < i; j++)
            {
                if (string.Equals(definitions[j]?.PickupId, current.PickupId, StringComparison.Ordinal))
                {
                    error = $"Duplicate map resource pickup id: {current.PickupId}";
                    return false;
                }
            }
        }

        error = string.Empty;
        return true;
    }

    private static bool IsEligible(TotemMapResourcePickupDefinition definition, int round) =>
        definition != null
        && definition.IsValid
        && round >= definition.MinRound
        && round <= definition.MaxRound;

    private static int CompareAnchors(TotemMapAnchor left, TotemMapAnchor right) =>
        string.CompareOrdinal(left?.AnchorId, right?.AnchorId);

    private static uint StableUInt(int matchSeed, int round, string anchorId, uint salt)
    {
        uint value = unchecked((uint)matchSeed) ^ (unchecked((uint)round) * 0x9E3779B9u) ^ salt;
        if (!string.IsNullOrEmpty(anchorId))
        {
            for (int i = 0; i < anchorId.Length; i++)
            {
                value ^= anchorId[i];
                value *= 16777619u;
            }
        }

        value ^= value >> 16;
        value *= 0x7FEB352Du;
        value ^= value >> 15;
        value *= 0x846CA68Bu;
        value ^= value >> 16;
        return value;
    }
}
