using NUnit.Framework;
using UnityEngine;

public sealed class TotemTattooPatternTargetResolverTests
{
    [Test]
    public void P01_AlwaysKeepsElementOnPrimaryTarget()
    {
        var candidates = new[]
        {
            new TotemElementTargetCandidate(2, 1, new Vector3(1f, 0f, 0f)),
        };

        int target = TotemTattooPatternTargetResolver.ResolveSecondaryTarget(
            TotemFirstPlayablePatternId.P01,
            1,
            1,
            Vector3.zero,
            candidates,
            candidates.Length);

        Assert.That(target, Is.Zero);
    }

    [Test]
    public void P02_SelectsNearestEligibleSameFactionNeighborWithinRadius()
    {
        var candidates = new[]
        {
            new TotemElementTargetCandidate(1, 1, Vector3.zero),
            new TotemElementTargetCandidate(8, 2, new Vector3(0.5f, 0f, 0f)),
            new TotemElementTargetCandidate(7, 1, new Vector3(1f, 0f, 0f), eligible: false),
            new TotemElementTargetCandidate(6, 1, new Vector3(4f, 0f, 0f)),
            new TotemElementTargetCandidate(5, 1, new Vector3(2f, 0f, 0f)),
            new TotemElementTargetCandidate(4, 1, new Vector3(1.5f, 0f, 0f)),
        };

        int target = TotemTattooPatternTargetResolver.ResolveSecondaryTarget(
            TotemFirstPlayablePatternId.P02,
            1,
            1,
            Vector3.zero,
            candidates,
            candidates.Length);

        Assert.That(target, Is.EqualTo(4));
    }

    [Test]
    public void P02_UsesCombatantIdAsStableTieBreak()
    {
        var candidates = new[]
        {
            new TotemElementTargetCandidate(5, 1, new Vector3(2f, 0f, 0f)),
            new TotemElementTargetCandidate(3, 1, new Vector3(-2f, 0f, 0f)),
        };

        int target = TotemTattooPatternTargetResolver.ResolveSecondaryTarget(
            TotemFirstPlayablePatternId.P02,
            1,
            1,
            Vector3.zero,
            candidates,
            candidates.Length);

        Assert.That(target, Is.EqualTo(3));
    }
}
