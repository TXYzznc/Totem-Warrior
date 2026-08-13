using NUnit.Framework;
using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class TotemFirstPlayableGunTests
{
    [Test]
    public void WeaponCatalog_ExposesOnlyTheFirstPlayableRifle()
    {
        var catalog = TotemWeaponService.GetCatalog();

        Assert.That(catalog.Count, Is.EqualTo(1));
        Assert.That(catalog[0].WeaponId, Is.EqualTo(TotemWeaponService.DefaultWeaponId));
        Assert.That(catalog[0].BaseDamage, Is.GreaterThan(0f));
        Assert.That(catalog[0].Cooldown, Is.GreaterThan(0f));
        Assert.That(catalog[0].Range, Is.GreaterThan(0f));
        Assert.That(TotemWeaponService.TryGetDefinition("knife_basic", out _), Is.False);
        Assert.That(TotemWeaponService.TryGetDefinition("bow_charge", out _), Is.False);
    }

    [Test]
    public void RuntimeAssetCatalog_ContainsOneWeaponAndNoPlayerSkillResidue()
    {
        Assert.That(
            TotemAssetService.TryLoadRuntimeAssetCatalogFromFile(
                TotemAssetService.GetRuntimeAssetCatalogPath(),
                out var catalog,
                out string error),
            Is.True,
            error);

        int weaponCount = 0;
        int playerSkillCount = 0;
        int removedProjectileEffectCount = 0;
        for (int i = 0; i < catalog.entries.Length; i++)
        {
            TotemRuntimeAssetEntry entry = catalog.entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
            {
                continue;
            }

            if (entry.key.StartsWith("weapon.", StringComparison.Ordinal))
            {
                weaponCount++;
            }
            else if (entry.key.StartsWith("skill.", StringComparison.Ordinal))
            {
                playerSkillCount++;
            }
            else if (string.Equals(entry.key, "effect.projectile.bullet_pistol", StringComparison.Ordinal)
                || string.Equals(entry.key, "effect.projectile.arrow_bow", StringComparison.Ordinal))
            {
                removedProjectileEffectCount++;
            }
        }

        Assert.That(weaponCount, Is.EqualTo(1));
        Assert.That(playerSkillCount, Is.Zero);
        Assert.That(removedProjectileEffectCount, Is.Zero);
        Assert.That(catalog.TryGetEntry(TotemFirstPlayableArtHandoff.WeaponKey, out var rifle), Is.True);
        Assert.That(rifle.fallbackPrimitive, Is.Not.Empty);
        Assert.That(
            string.IsNullOrWhiteSpace(rifle.activeAssetPath) || File.Exists(rifle.activeAssetPath),
            Is.True,
            rifle.activeAssetPath);
        Assert.That(rifle.activeAssetPath, Does.Not.Contain("Sprites/Weapons"));
    }

    [Test]
    public void EffectiveWeakpointHit_SubmitsWeakpointThenRifleArmThenTorso()
    {
        var service = new TotemEffectResolutionService();
        var hit = new TotemGunHitContext(
            new TotemParticipantId(1),
            new TotemTeamId(0),
            100,
            new TotemTeamId(-1),
            TotemHitRegion.Weakpoint,
            Vector3.one,
            Vector3.back,
            16f);
        var damage = new TotemDirectDamageResult(hit, 0f, 12f, relationshipAllowed: true);

        service.BeginResolution();
        Assert.That(service.SubmitGunHit(damage), Is.EqualTo(3));
        Assert.That(service.Resolve(zeroPresentationDelay: true), Is.EqualTo(3));

        AssertResolvedKind(service.Queue, 0, TotemEffectEventKind.Weakpoint);
        AssertResolvedKind(service.Queue, 1, TotemEffectEventKind.RifleArm);
        AssertResolvedKind(service.Queue, 2, TotemEffectEventKind.Torso);
    }

    [Test]
    public void IneffectiveHit_DoesNotSubmitRifleArmOrAnyFollowUpEvent()
    {
        var service = new TotemEffectResolutionService();
        var hit = new TotemGunHitContext(
            new TotemParticipantId(1),
            new TotemTeamId(0),
            2,
            new TotemTeamId(0),
            TotemHitRegion.Weakpoint,
            Vector3.zero,
            Vector3.back,
            16f);
        var damage = new TotemDirectDamageResult(hit, 0f, 0f, relationshipAllowed: false);

        service.BeginResolution();
        Assert.That(service.SubmitGunHit(damage), Is.Zero);
        Assert.That(service.Resolve(zeroPresentationDelay: true), Is.Zero);
    }

    [Test]
    public void ParticipantMarker_ProvidesHiddenHeadWeakpointAndBodyCollider()
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        try
        {
            root.transform.position = new Vector3(0f, 0f, 5f);
            TotemHitRegionMarker.AttachParticipantMarkers(root, 42);
            Physics.SyncTransforms();

            Transform weakpoint = root.transform.Find("FirstPlayableWeakpoint");
            Assert.That(weakpoint, Is.Not.Null);
            Assert.That(weakpoint.GetComponent<Renderer>().enabled, Is.False);
            Vector3 origin = weakpoint.position + Vector3.back * 5f;
            TotemHitRegion region = TotemHitRegionResolver.ResolveForTarget(
                origin,
                Vector3.forward,
                10f,
                42,
                root.transform.position,
                out _,
                out _);

            Assert.That(region, Is.EqualTo(TotemHitRegion.Weakpoint));
        }
        finally
        {
            TotemHitRegionMarker.Detach(root);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void BodyRay_ResolvesBodyWithoutPromotingToWeakpoint()
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        try
        {
            root.transform.position = new Vector3(0f, 0f, 5f);
            TotemHitRegionMarker.AttachParticipantMarkers(root, 77);
            Physics.SyncTransforms();
            Vector3 origin = root.transform.position + Vector3.down * 0.5f + Vector3.back * 5f;

            TotemHitRegion region = TotemHitRegionResolver.ResolveForTarget(
                origin,
                Vector3.forward,
                10f,
                77,
                root.transform.position,
                out _,
                out _);

            Assert.That(region, Is.EqualTo(TotemHitRegion.Body));
        }
        finally
        {
            TotemHitRegionMarker.Detach(root);
            Object.DestroyImmediate(root);
        }
    }

    private static void AssertResolvedKind(TotemEffectResolutionQueue queue, int index, TotemEffectEventKind expected)
    {
        Assert.That(queue.TryGetResolvedAt(index, out TotemEffectEvent effectEvent), Is.True);
        Assert.That(effectEvent.Kind, Is.EqualTo(expected));
    }
}
