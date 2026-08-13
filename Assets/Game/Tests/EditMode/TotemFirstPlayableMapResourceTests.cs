#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;

public sealed class TotemFirstPlayableMapResourceTests
{
    [Test]
    public void GameplayCatalog_UsesConfigDrivenPickupRanges()
    {
        TotemGameplayCatalog catalog = TotemDataService.LoadGameplayCatalogOrDefault();
        TotemMapResourcePickupDefinition[] definitions = catalog.CreateMapResourcePickupDefinitions();

        Assert.That(TotemMapResourceGenerator.ValidateDefinitions(definitions, out string error), Is.True, error);
        Assert.That(definitions, Has.Length.EqualTo(9));
        Assert.That(Find(definitions, "pigment.fire.small").MinAmount, Is.EqualTo(4));
        Assert.That(Find(definitions, "pigment.fire.small").MaxAmount, Is.EqualTo(6));
        Assert.That(Find(definitions, "pigment.ice.medium").MinAmount, Is.EqualTo(8));
        Assert.That(Find(definitions, "pigment.ice.medium").MaxAmount, Is.EqualTo(12));
        Assert.That(Find(definitions, "pigment.lightning.large").MinAmount, Is.EqualTo(16));
        Assert.That(Find(definitions, "pigment.lightning.large").MaxAmount, Is.EqualTo(20));
    }

    [Test]
    public void Generator_SameSeedAndAnchors_ProducesSameTypesAndAmounts()
    {
        TotemMapResourcePickupDefinition[] definitions = TotemGameplayCatalog.BuildDefault().CreateMapResourcePickupDefinitions();
        TotemMapSnapshot map = CreateMap(reverse: false);
        var first = new TotemMapResourcePickup[8];
        var second = new TotemMapResourcePickup[8];

        int firstCount = TotemMapResourceGenerator.Generate(definitions, map, 407, 2, first);
        int secondCount = TotemMapResourceGenerator.Generate(definitions, map, 407, 2, second);

        Assert.That(firstCount, Is.EqualTo(3));
        Assert.That(secondCount, Is.EqualTo(firstCount));
        for (int i = 0; i < firstCount; i++)
        {
            Assert.That(second[i].AnchorId, Is.EqualTo(first[i].AnchorId));
            Assert.That(second[i].PickupId, Is.EqualTo(first[i].PickupId));
            Assert.That(second[i].Amount, Is.EqualTo(first[i].Amount));
        }
    }

    [Test]
    public void Generator_SortsAnchorsAndSkipsUnreachableAnchors()
    {
        TotemMapResourcePickupDefinition[] definitions = TotemGameplayCatalog.BuildDefault().CreateMapResourcePickupDefinitions();
        var ordered = new TotemMapResourcePickup[8];
        var reversed = new TotemMapResourcePickup[8];

        int orderedCount = TotemMapResourceGenerator.Generate(definitions, CreateMap(reverse: false), 811, 1, ordered);
        int reversedCount = TotemMapResourceGenerator.Generate(definitions, CreateMap(reverse: true), 811, 1, reversed);

        Assert.That(orderedCount, Is.EqualTo(3));
        Assert.That(reversedCount, Is.EqualTo(orderedCount));
        for (int i = 0; i < orderedCount; i++)
        {
            Assert.That(reversed[i].AnchorId, Is.EqualTo(ordered[i].AnchorId));
            Assert.That(reversed[i].PickupId, Is.EqualTo(ordered[i].PickupId));
            Assert.That(reversed[i].Amount, Is.EqualTo(ordered[i].Amount));
            TotemMapResourcePickupDefinition selected = Find(definitions, ordered[i].PickupId);
            Assert.That(ordered[i].Amount, Is.InRange(selected.MinAmount, selected.MaxAmount));
        }
    }

    private static TotemMapResourcePickupDefinition Find(TotemMapResourcePickupDefinition[] definitions, string pickupId)
    {
        for (int i = 0; i < definitions.Length; i++)
        {
            if (definitions[i].PickupId == pickupId)
            {
                return definitions[i];
            }
        }

        Assert.Fail($"Missing pickup definition: {pickupId}");
        return null;
    }

    private static TotemMapSnapshot CreateMap(bool reverse)
    {
        var anchors = new[]
        {
            Anchor("resource.c", 30f, true),
            Anchor("resource.a", 10f, true),
            Anchor("resource.blocked", 20f, false),
            Anchor("resource.b", 20f, true),
        };
        if (reverse)
        {
            System.Array.Reverse(anchors);
        }

        return new TotemMapSnapshot
        {
            Seed = 811,
            MapSize = 100f,
            AnchorPlacements = anchors,
        };
    }

    private static TotemMapAnchor Anchor(string id, float x, bool reachable)
    {
        return new TotemMapAnchor
        {
            AnchorId = id,
            Kind = TotemMapAnchorKind.Resource,
            Position = new Vector3(x, 0f, 10f),
            IsReachable = reachable,
        };
    }
}
#endif
