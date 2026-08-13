#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class TotemFirstPlayableExtractionTests
{
    [Test]
    public void MapLayout_ProvidesDedicatedReachableExtractionAnchors()
    {
        TotemMapSnapshot map = TotemMapService.BuildLayout(260812, 1);
        TotemMapAnchor[] anchors = TotemMapService.FindAnchors(map, TotemMapAnchorKind.Extraction);

        Assert.That(anchors.Length, Is.GreaterThanOrEqualTo(4));
        for (int i = 0; i < anchors.Length; i++)
        {
            Assert.That(anchors[i].IsReachable, Is.True, anchors[i].AnchorId);
            Assert.That(anchors[i].Kind, Is.EqualTo(TotemMapAnchorKind.Extraction));
        }
    }

    [Test]
    public void Generator_SameSeedProducesSameThreeDistinctPoints()
    {
        TotemMapSnapshot map = TotemMapService.BuildLayout(260812, 1);
        var first = new TotemExtractionPoint[TotemExtractionPointGenerator.MaxPointCount];
        var second = new TotemExtractionPoint[TotemExtractionPointGenerator.MaxPointCount];

        int firstCount = TotemExtractionPointGenerator.Generate(map, 9917, 3, first);
        int secondCount = TotemExtractionPointGenerator.Generate(map, 9917, 3, second);

        Assert.That(firstCount, Is.EqualTo(3));
        Assert.That(secondCount, Is.EqualTo(firstCount));
        var ids = new HashSet<string>();
        for (int i = 0; i < firstCount; i++)
        {
            Assert.That(second[i].AnchorId, Is.EqualTo(first[i].AnchorId));
            Assert.That(second[i].Position, Is.EqualTo(first[i].Position));
            Assert.That(ids.Add(first[i].AnchorId), Is.True);
        }
    }

    [Test]
    public void ShiftSpace_ProducesUnlockCommandWithoutDodge()
    {
        var provider = new ExtractionInputProvider();
        var input = new TotemInputService();
        input.SetInputProvider(provider);
        provider.Hold(KeyCode.LeftShift);
        provider.Press(KeyCode.Space);

        TotemInputSnapshot snapshot = input.ReadInputSnapshot();

        Assert.That(snapshot.extractionUnlockPressed, Is.True);
        Assert.That(snapshot.dodgePressed, Is.False);
    }

    [Test]
    public void SpaceWithoutShift_RemainsDodge()
    {
        var provider = new ExtractionInputProvider();
        var input = new TotemInputService();
        input.SetInputProvider(provider);
        provider.Press(KeyCode.Space);

        TotemInputSnapshot snapshot = input.ReadInputSnapshot();

        Assert.That(snapshot.dodgePressed, Is.True);
        Assert.That(snapshot.extractionUnlockPressed, Is.False);
    }

    [TestCase(TotemMatchPhase.Round3Combat, false)]
    [TestCase(TotemMatchPhase.Build4, false)]
    [TestCase(TotemMatchPhase.Round4Combat, true)]
    [TestCase(TotemMatchPhase.Build5, true)]
    [TestCase(TotemMatchPhase.Round5Combat, true)]
    [TestCase(TotemMatchPhase.Result, false)]
    public void UnlockPhaseGate_MatchesFourthRoundBoundary(TotemMatchPhase phase, bool expected)
    {
        Assert.That(TotemExtractionService.CanUnlockInPhase(phase), Is.EqualTo(expected));
    }

    private sealed class ExtractionInputProvider : ITotemInputProvider
    {
        private readonly HashSet<KeyCode> held = new HashSet<KeyCode>();
        private readonly HashSet<KeyCode> pressed = new HashSet<KeyCode>();

        public float UnscaledTime => 0f;
        public Vector3 MousePosition => Vector3.zero;
        public void Hold(KeyCode key) => held.Add(key);
        public void Press(KeyCode key) => pressed.Add(key);
        public bool GetKey(KeyCode keyCode) => held.Contains(keyCode);
        public bool GetKeyDown(KeyCode keyCode) => pressed.Contains(keyCode);
        public bool GetMouseButton(int button) => false;
        public bool GetMouseButtonDown(int button) => false;
    }
}
#endif
