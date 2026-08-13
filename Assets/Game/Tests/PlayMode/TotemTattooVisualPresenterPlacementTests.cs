#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;

public sealed class TotemTattooVisualPresenterPlacementTests
{
    [Test]
    public void PlacementApi_NormalizesValuesAndInitializesAllPartsWithoutPrefabSetup()
    {
        var host = new GameObject("TattooPlacementTest");
        try
        {
            var presenter = host.AddComponent<TotemTattooVisualPresenter>();

            Assert.IsTrue(presenter.SetPartPlacement(4, new Vector2(-0.25f, 1.25f), -2f));
            TotemTattooVisualPlacement placement = presenter.GetPartPlacement(4);

            Assert.AreEqual(0f, placement.offset.x);
            Assert.AreEqual(1f, placement.offset.y);
            Assert.AreEqual(0.01f, placement.scale);
            Assert.AreEqual(0.5f, presenter.GetPartPlacement(1).offset.x);
            Assert.AreEqual(0.5f, presenter.GetPartPlacement(1).offset.y);
            Assert.AreEqual(1f, presenter.GetPartPlacement(1).scale);
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void PlacementApi_RejectsUnknownBodyParts()
    {
        var host = new GameObject("TattooPlacementInvalidPartTest");
        try
        {
            var presenter = host.AddComponent<TotemTattooVisualPresenter>();
            Assert.IsFalse(presenter.SetPartPlacement(0, Vector2.one, 1f));
            Assert.IsFalse(presenter.SetPartPlacement(TotemFirstPlayableTattooBuildState.SlotCount + 1, Vector2.one, 1f));
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }
}
#endif
