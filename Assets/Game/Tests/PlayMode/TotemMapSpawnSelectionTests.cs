#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class TotemMapSpawnSelectionTests
{
    [Test]
    public void RandomPlayerSpawnSelection_ChoosesOnlyFromAllValidCandidates()
    {
        var map = new TotemMapSnapshot
        {
            AnchorPlacements = new TotemMapAnchor[10],
        };
        var validPositions = new HashSet<Vector3>();
        for (int i = 0; i < map.AnchorPlacements.Length; i++)
        {
            var position = new Vector3(i * 3f, 0f, i * 2f);
            validPositions.Add(position);
            map.AnchorPlacements[i] = new TotemMapAnchor
            {
                AnchorId = $"player.spawn.{i:000}",
                Kind = TotemMapAnchorKind.PlayerSpawn,
                Position = position,
            };
        }

        var random = new System.Random(20260716);
        var selected = new HashSet<Vector3>();
        for (int i = 0; i < 48; i++)
        {
            Vector3 position = TotemMapService.ResolveRandomAnchorPosition(map, TotemMapAnchorKind.PlayerSpawn, Vector3.one * -1f, random);
            Assert.IsTrue(validPositions.Contains(position));
            selected.Add(position);
        }

        Assert.Greater(selected.Count, 1, "Player spawning must not be pinned to the first candidate.");
    }
}
#endif
