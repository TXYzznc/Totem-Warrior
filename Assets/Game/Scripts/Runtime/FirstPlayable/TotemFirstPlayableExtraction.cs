using System;
using UnityEngine;

[Serializable]
public sealed class TotemExtractionConfig
{
    public int pointCount = 3;
    public float interactSeconds = 3f;
    public float interactRadius = 4f;
}

public readonly struct TotemExtractionPoint
{
    public TotemExtractionPoint(int instanceId, string anchorId, Vector3 position)
    {
        InstanceId = instanceId;
        AnchorId = anchorId ?? string.Empty;
        Position = position;
    }

    public int InstanceId { get; }
    public string AnchorId { get; }
    public Vector3 Position { get; }
    public bool IsValid => InstanceId > 0 && !string.IsNullOrWhiteSpace(AnchorId);
}

public static class TotemExtractionPointGenerator
{
    public const int MaxPointCount = 8;

    public static int Generate(
        TotemMapSnapshot map,
        int matchSeed,
        int requestedCount,
        TotemExtractionPoint[] output)
    {
        if (map == null || output == null || output.Length == 0 || requestedCount <= 0)
        {
            return 0;
        }

        TotemMapAnchor[] anchors = TotemMapService.FindAnchors(map, TotemMapAnchorKind.Extraction);
        int validCount = 0;
        for (int i = 0; i < anchors.Length; i++)
        {
            if (anchors[i] != null && anchors[i].IsReachable)
            {
                anchors[validCount++] = anchors[i];
            }
        }

        var random = new System.Random(unchecked(matchSeed * 486187739 + map.Seed * 16777619 + map.ThemeId * 397));
        for (int i = validCount - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            TotemMapAnchor temporary = anchors[i];
            anchors[i] = anchors[swapIndex];
            anchors[swapIndex] = temporary;
        }

        int count = Mathf.Min(Mathf.Min(requestedCount, output.Length), validCount);
        for (int i = 0; i < count; i++)
        {
            output[i] = new TotemExtractionPoint(i + 1, anchors[i].AnchorId, anchors[i].Position);
        }

        for (int i = count; i < output.Length; i++)
        {
            output[i] = default;
        }

        return count;
    }
}

[Serializable]
public sealed class TotemExtractionSnapshot
{
    public bool unlocked;
    public bool completed;
    public int activePointCount;
    public float interactionProgress;
    public float interactionDuration;
    public int focusedPointInstanceId;
    public int extractedTeamId;
    public int[] extractedParticipantIds = Array.Empty<int>();
    public string lastReason = string.Empty;
}
