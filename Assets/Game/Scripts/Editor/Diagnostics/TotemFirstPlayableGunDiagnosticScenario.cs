#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemFirstPlayableGunDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem First Playable Gun";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            var catalog = TotemWeaponService.GetCatalog();
            context.AssertEqual(1, catalog.Count, "firstPlayableGun.activeWeaponCount");
            context.AssertEqual(TotemWeaponService.DefaultWeaponId, catalog[0].WeaponId, "firstPlayableGun.weaponId");
            context.Assert(catalog[0].BaseDamage > 0f, "First playable rifle must have positive damage.");
            context.Assert(catalog[0].Cooldown > 0f, "First playable rifle must have positive cooldown.");
            context.Assert(catalog[0].Range > 0f, "First playable rifle must have positive range.");

            var effectService = new TotemEffectResolutionService();
            var hit = new TotemGunHitContext(
                new TotemParticipantId(1),
                new TotemTeamId(0),
                100,
                new TotemTeamId(-1),
                TotemHitRegion.Weakpoint,
                Vector3.one,
                Vector3.back,
                16f);
            var damage = new TotemDirectDamageResult(hit, 0f, 16f, relationshipAllowed: true);
            effectService.BeginResolution();
            context.AssertEqual(3, effectService.SubmitGunHit(damage), "firstPlayableGun.submittedEffectCount");
            effectService.Resolve(zeroPresentationDelay: true);
            AssertKind(context, effectService.Queue, 0, TotemEffectEventKind.Weakpoint);
            AssertKind(context, effectService.Queue, 1, TotemEffectEventKind.RifleArm);
            AssertKind(context, effectService.Queue, 2, TotemEffectEventKind.Torso);

            var teammateSource = CreateActor(1, 0);
            var teammateTarget = CreateActor(2, 0);
            TotemCombatRelationshipDecision friendlyFire = TotemCombatRelationshipService.Evaluate(
                teammateSource,
                teammateTarget,
                new TotemCombatRelationshipContext(0f));
            context.Assert(!friendlyFire.Allowed, "Same-team rifle hit must be rejected before effect submission.");

            VerifyWeakpointCollider(context);
            VerifySteadyQueueAllocation(context);
            VerifyRuntimeAssetCatalog(context);
            context.Detail("firstPlayableGun.assetKey", TotemFirstPlayableArtHandoff.WeaponKey);
            context.Detail("firstPlayableGun.effectOrder", "Weakpoint > RifleArm > Torso");
            context.Pass("Single rifle, friendly-fire rejection, collider weakpoint context and deterministic zero-allocation effect ordering are available.");
        }

        private static TotemActorModel CreateActor(int id, int teamId)
        {
            return new TotemActorModel(new TotemActorSpawnInfo
            {
                ActorId = id,
                TeamId = teamId,
                Name = "DiagnosticActor" + id,
                Kind = TotemActorKind.Player,
                ControllerKind = TotemParticipantControllerKind.Human,
                Position = Vector3.zero,
                MaxHealth = 100f,
            });
        }

        private static void VerifyWeakpointCollider(GFDiagnosticScenarioContext context)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                root.transform.position = new Vector3(0f, 0f, 5f);
                TotemHitRegionMarker.AttachParticipantMarkers(root, 100);
                Physics.SyncTransforms();
                Transform weakpoint = root.transform.Find("FirstPlayableWeakpoint");
                context.Assert(weakpoint != null, "Participant presentation must expose a head weakpoint collider.");
                if (weakpoint == null)
                {
                    return;
                }

                Vector3 origin = weakpoint.position + Vector3.back * 5f;
                TotemHitRegion region = TotemHitRegionResolver.ResolveForTarget(
                    origin,
                    Vector3.forward,
                    10f,
                    100,
                    root.transform.position,
                    out _,
                    out _);
                context.AssertEqual(TotemHitRegion.Weakpoint, region, "firstPlayableGun.colliderRegion");

                Vector3 bodyOrigin = root.transform.position + Vector3.down * 0.5f + Vector3.back * 5f;
                TotemHitRegion bodyRegion = TotemHitRegionResolver.ResolveForTarget(
                    bodyOrigin,
                    Vector3.forward,
                    10f,
                    100,
                    root.transform.position,
                    out _,
                    out _);
                context.AssertEqual(TotemHitRegion.Body, bodyRegion, "firstPlayableGun.bodyColliderRegion");
            }
            finally
            {
                TotemHitRegionMarker.Detach(root);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void VerifySteadyQueueAllocation(GFDiagnosticScenarioContext context)
        {
            var queue = new TotemEffectResolutionQueue();
            var presentation = new TotemEffectPresentationBuffer();
            ResolveOnce(queue, presentation, 1);
            GC.GetAllocatedBytesForCurrentThread();
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 64; i++)
            {
                ResolveOnce(queue, presentation, i + 2);
            }

            context.AssertEqual(0L, GC.GetAllocatedBytesForCurrentThread() - before, "firstPlayableGun.queueAllocatedBytes");
        }

        private static void VerifyRuntimeAssetCatalog(GFDiagnosticScenarioContext context)
        {
            string catalogPath = TotemAssetService.GetRuntimeAssetCatalogPath();
            bool loaded = TotemAssetService.TryLoadRuntimeAssetCatalogFromFile(catalogPath, out var catalog, out string error);
            context.Assert(loaded, "First-playable runtime asset catalog must load: " + error);
            if (!loaded || catalog?.entries == null)
            {
                return;
            }

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

            context.AssertEqual(1, weaponCount, "firstPlayableGun.runtimeWeaponAssetCount");
            context.AssertEqual(0, playerSkillCount, "firstPlayableGun.runtimePlayerSkillAssetCount");
            context.AssertEqual(0, removedProjectileEffectCount, "firstPlayableGun.removedProjectileAssetCount");
            AssertAssetExists(context, catalog, TotemFirstPlayableArtHandoff.WeaponKey, "firstPlayableGun.rifleAssetExists");
        }

        private static void AssertAssetExists(
            GFDiagnosticScenarioContext context,
            TotemRuntimeAssetCatalog catalog,
            string key,
            string detailKey)
        {
            bool found = catalog.TryGetEntry(key, out var entry);
            context.Assert(found, "Runtime asset key is missing: " + key);
            if (!found)
            {
                return;
            }

            bool assetExists = File.Exists(entry.activeAssetPath);
            bool hasFallback = !string.IsNullOrWhiteSpace(entry.fallbackPrimitive);
            context.Assert(
                assetExists || hasFallback,
                "Runtime asset path is missing and no fallback is configured for " + key + ": " + entry.activeAssetPath);
            context.Detail(detailKey, entry.activeAssetPath);
            context.Detail(detailKey + ".assetExists", assetExists.ToString());
            context.Detail(detailKey + ".fallbackPrimitive", entry.fallbackPrimitive ?? string.Empty);
        }

        private static void ResolveOnce(TotemEffectResolutionQueue queue, TotemEffectPresentationBuffer presentation, int sequence)
        {
            queue.Reset(new TotemResolutionIdentity(62031, sequence));
            queue.TrySubmit(new TotemEffectEvent(TotemEffectEventKind.Torso, new TotemParticipantId(1), 100, 0, 16f));
            queue.TrySubmit(new TotemEffectEvent(TotemEffectEventKind.RifleArm, new TotemParticipantId(1), 100, 1, 16f));
            queue.TrySubmit(new TotemEffectEvent(TotemEffectEventKind.Weakpoint, new TotemParticipantId(1), 100, 2, 16f));
            queue.Resolve();
            presentation.Build(queue, zeroDelay: false);
        }

        private static void AssertKind(
            GFDiagnosticScenarioContext context,
            TotemEffectResolutionQueue queue,
            int index,
            TotemEffectEventKind expected)
        {
            bool found = queue.TryGetResolvedAt(index, out TotemEffectEvent effectEvent);
            context.Assert(found, "Resolved effect event is missing at index " + index + ".");
            if (found)
            {
                context.AssertEqual(expected, effectEvent.Kind, "firstPlayableGun.effectKind" + index);
            }
        }
    }
}
#endif
