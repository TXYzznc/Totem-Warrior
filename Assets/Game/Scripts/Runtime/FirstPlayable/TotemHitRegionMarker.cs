using System.Collections.Generic;
using UnityEngine;

public readonly struct TotemHitRegionBinding
{
    public TotemHitRegionBinding(int combatantId, TotemHitRegion region)
    {
        CombatantId = combatantId;
        Region = region;
    }

    public int CombatantId { get; }

    public TotemHitRegion Region { get; }
}

/// <summary>
/// Central collider metadata registry. It avoids adding gameplay MonoBehaviours
/// to participant presentation prefabs and is only mutated on spawn/despawn.
/// </summary>
public static class TotemHitRegionMarker
{
    public const string WeakpointObjectName = "FirstPlayableWeakpoint";

    private static readonly Dictionary<Collider, TotemHitRegionBinding> Bindings =
        new Dictionary<Collider, TotemHitRegionBinding>(128);

    public static void AttachParticipantMarkers(GameObject root, int targetCombatantId)
    {
        Attach(root, targetCombatantId, showWeakpoint: false);
    }

    public static bool TryGetBinding(Collider collider, out TotemHitRegionBinding binding)
    {
        if (collider != null && Bindings.TryGetValue(collider, out binding))
        {
            return true;
        }

        binding = default;
        return false;
    }

    public static void Detach(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(includeInactive: true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Bindings.Remove(colliders[i]);
        }
    }

    private static void Attach(GameObject root, int targetCombatantId, bool showWeakpoint)
    {
        if (root == null)
        {
            return;
        }

        Detach(root);
        if (root.GetComponentInChildren<Collider>() == null)
        {
            root.AddComponent<CapsuleCollider>();
        }

        Transform weakpointTransform = root.transform.Find(WeakpointObjectName);
        GameObject weakpoint;
        if (weakpointTransform == null)
        {
            weakpoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            weakpoint.name = WeakpointObjectName;
            weakpoint.transform.SetParent(root.transform, false);
            weakpoint.transform.localPosition = ResolveWeakpointLocalPosition(root);
            weakpoint.transform.localRotation = Quaternion.identity;
            weakpoint.transform.localScale = Vector3.one * 0.24f;
        }
        else
        {
            weakpoint = weakpointTransform.gameObject;
        }

        weakpoint.layer = root.layer;
        Collider weakpointCollider = weakpoint.GetComponent<Collider>();
        if (weakpointCollider == null)
        {
            weakpointCollider = weakpoint.AddComponent<SphereCollider>();
        }

        weakpointCollider.isTrigger = true;
        Collider[] colliders = root.GetComponentsInChildren<Collider>(includeInactive: true);
        var bodyBinding = new TotemHitRegionBinding(targetCombatantId, TotemHitRegion.Body);
        for (int i = 0; i < colliders.Length; i++)
        {
            Bindings[colliders[i]] = bodyBinding;
        }

        Bindings[weakpointCollider] = new TotemHitRegionBinding(targetCombatantId, TotemHitRegion.Weakpoint);
        Renderer renderer = weakpoint.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = showWeakpoint;
            if (showWeakpoint)
            {
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", new Color(1f, 0.72f, 0.08f, 1f));
                properties.SetColor("_Color", new Color(1f, 0.72f, 0.08f, 1f));
                renderer.SetPropertyBlock(properties);
            }
        }
    }

    private static Vector3 ResolveWeakpointLocalPosition(GameObject root)
    {
        Renderer renderer = root.GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            return new Vector3(0f, 0.8f, 0f);
        }

        Vector3 worldPoint = new Vector3(renderer.bounds.center.x, renderer.bounds.max.y, renderer.bounds.center.z);
        Vector3 localPoint = root.transform.InverseTransformPoint(worldPoint);
        localPoint.y -= 0.12f;
        return localPoint;
    }
}

public static class TotemHitRegionResolver
{
    private const int RaycastCapacity = 16;
    private static readonly RaycastHit[] HitBuffer = new RaycastHit[RaycastCapacity];

    public static TotemHitRegion ResolveForTarget(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        int targetCombatantId,
        Vector3 fallbackPoint,
        out Vector3 hitPoint,
        out Vector3 hitNormal)
    {
        hitPoint = fallbackPoint;
        Vector3 fallbackNormal = origin - fallbackPoint;
        hitNormal = fallbackNormal.sqrMagnitude > 0.0001f ? fallbackNormal.normalized : Vector3.back;
        if (direction.sqrMagnitude <= 0.0001f || maxDistance <= 0f || targetCombatantId == 0)
        {
            return TotemHitRegion.Body;
        }

        int hitCount = Physics.RaycastNonAlloc(
            origin,
            direction.normalized,
            HitBuffer,
            maxDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide);
        float nearestDistance = float.MaxValue;
        TotemHitRegion resolvedRegion = TotemHitRegion.Body;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit candidate = HitBuffer[i];
            if (!TotemHitRegionMarker.TryGetBinding(candidate.collider, out TotemHitRegionBinding binding)
                || binding.CombatantId != targetCombatantId)
            {
                continue;
            }

            bool weakpointOverridesBody = binding.Region == TotemHitRegion.Weakpoint
                && resolvedRegion != TotemHitRegion.Weakpoint;
            bool sameRegionIsNearer = binding.Region == resolvedRegion
                && candidate.distance < nearestDistance;
            if (!weakpointOverridesBody && !sameRegionIsNearer)
            {
                continue;
            }

            nearestDistance = candidate.distance;
            resolvedRegion = binding.Region;
            hitPoint = candidate.point;
            hitNormal = candidate.normal;
        }

        return resolvedRegion;
    }
}
