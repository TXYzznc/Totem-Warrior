using System;
using UnityEngine;

public sealed class TotemMapResourceService : TotemRuntimeServiceBase, ITotemGameplaySimulationService
{
    public const float PickupRadius = 2.5f;

    private readonly TotemMapResourcePickup[] pickups =
        new TotemMapResourcePickup[TotemMapResourceGenerator.MaxPickupCount];
    private readonly bool[] claimed = new bool[TotemMapResourceGenerator.MaxPickupCount];
    private readonly GameObject[] visuals = new GameObject[TotemMapResourceGenerator.MaxPickupCount];

    private TotemMapResourcePickupDefinition[] definitions = Array.Empty<TotemMapResourcePickupDefinition>();
    private TotemMatchFlowService matchFlowService;
    private TotemMapService mapService;
    private TotemFirstPlayableLifecycleService lifecycleService;
    private TotemFirstPlayableTattooBuildService tattooBuildService;
    private TotemFirstPlayableSocialService socialService;
    private int activeCount;
    private int spawnedRound;

    public override string ServiceName => "MapResource";

    public int ActivePickupCount => activeCount;
    public int SpawnedRound => spawnedRound;

    public event Action<TotemActorModel, TotemMapResourcePickup> PickupClaimed;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        TotemDataService dataService = runtime.GetService<TotemDataService>();
        definitions = (dataService?.GameplayCatalog ?? TotemDataService.LoadGameplayCatalogOrDefault())
            .CreateMapResourcePickupDefinitions();
        matchFlowService = runtime.GetService<TotemMatchFlowService>();
        mapService = runtime.GetService<TotemMapService>();
        lifecycleService = runtime.GetService<TotemFirstPlayableLifecycleService>();
        tattooBuildService = runtime.GetService<TotemFirstPlayableTattooBuildService>();
        socialService = runtime.GetService<TotemFirstPlayableSocialService>();
        if (matchFlowService != null)
        {
            matchFlowService.PhaseChanged += OnPhaseChanged;
        }

        if (!TotemMapResourceGenerator.ValidateDefinitions(definitions, out string error))
        {
            throw new InvalidOperationException(error);
        }
    }

    protected override void OnShutdown()
    {
        if (matchFlowService != null)
        {
            matchFlowService.PhaseChanged -= OnPhaseChanged;
        }

        ClearPickups();
        definitions = Array.Empty<TotemMapResourcePickupDefinition>();
        matchFlowService = null;
        mapService = null;
        lifecycleService = null;
        tattooBuildService = null;
        socialService = null;
        PickupClaimed = null;
    }

    public TotemMapResourcePickup[] CaptureActivePickups()
    {
        int count = 0;
        for (int i = 0; i < activeCount; i++)
        {
            if (!claimed[i] && pickups[i].IsValid)
            {
                count++;
            }
        }

        var result = new TotemMapResourcePickup[count];
        int cursor = 0;
        for (int i = 0; i < activeCount; i++)
        {
            if (!claimed[i] && pickups[i].IsValid)
            {
                result[cursor++] = pickups[i];
            }
        }

        return result;
    }

    public bool TryFindNearest(Vector3 position, float radius, out TotemMapResourcePickup pickup)
    {
        float bestSqr = Mathf.Max(0f, radius) * Mathf.Max(0f, radius);
        int bestIndex = -1;
        for (int i = 0; i < activeCount; i++)
        {
            if (claimed[i] || !pickups[i].IsValid)
            {
                continue;
            }

            float sqr = (pickups[i].Position - position).sqrMagnitude;
            if (sqr <= bestSqr)
            {
                bestSqr = sqr;
                bestIndex = i;
            }
        }

        pickup = bestIndex >= 0 ? pickups[bestIndex] : default;
        return bestIndex >= 0;
    }

    public bool TryPickup(TotemActorModel actor, int instanceId, out TotemMapResourcePickupResult result)
    {
        if (actor == null || instanceId <= 0 || !TotemMatchPhaseContract.IsCombat(matchFlowService?.CurrentPhase ?? TotemMatchPhase.FrontEnd))
        {
            result = new TotemMapResourcePickupResult(false, "NotAvailable", default);
            return false;
        }

        TotemFirstPlayableParticipantLifeState lifeState = lifecycleService?.GetOrCreateState(actor);
        if (lifeState == null || lifeState.LifeState != TotemFirstPlayableLifeState.Alive)
        {
            result = new TotemMapResourcePickupResult(false, "ParticipantNotAlive", default);
            return false;
        }

        for (int i = 0; i < activeCount; i++)
        {
            TotemMapResourcePickup pickup = pickups[i];
            if (pickup.InstanceId != instanceId)
            {
                continue;
            }

            if (claimed[i])
            {
                result = new TotemMapResourcePickupResult(false, "AlreadyClaimed", pickup);
                return false;
            }

            if ((pickup.Position - actor.Position).sqrMagnitude > PickupRadius * PickupRadius)
            {
                result = new TotemMapResourcePickupResult(false, "OutOfRange", pickup);
                return false;
            }

            if (pickup.Category != TotemMapResourceCategory.Pigment || tattooBuildService == null)
            {
                result = new TotemMapResourcePickupResult(false, "UnsupportedResource", pickup);
                return false;
            }

            tattooBuildService.AddPigment(actor, pickup.Pigment, pickup.Amount);
            socialService?.RecordResourcesAcquired(new TotemParticipantId(actor.ParticipantId), pickup.Amount);
            claimed[i] = true;
            DestroyVisual(i);
            PickupClaimed?.Invoke(actor, pickup);
            GFTrace.Success("TotemMapResource", "Pickup.Claimed", null, GFTrace.Data(
                "participantId", actor.ParticipantId.ToString(),
                "pickupId", pickup.PickupId,
                "resourceId", pickup.ResourceId,
                "amount", pickup.Amount.ToString(),
                "round", pickup.Round.ToString(),
                "anchorId", pickup.AnchorId));
            result = new TotemMapResourcePickupResult(true, "Claimed", pickup);
            return true;
        }

        result = new TotemMapResourcePickupResult(false, "NotFound", default);
        return false;
    }

    public int SpawnForRound(int round)
    {
        ClearPickups();
        TotemMapSnapshot map = mapService?.CurrentMap;
        if (map == null || round < 1 || round > 3)
        {
            return 0;
        }

        activeCount = TotemMapResourceGenerator.Generate(definitions, map, map.Seed, round, pickups);
        spawnedRound = round;
        for (int i = 0; i < activeCount; i++)
        {
            claimed[i] = false;
            visuals[i] = CreateVisual(pickups[i]);
        }

        GFTrace.Success("TotemMapResource", "Round.Spawned", null, GFTrace.Data(
            "round", round.ToString(),
            "count", activeCount.ToString(),
            "seed", map.Seed.ToString()));
        return activeCount;
    }

    public void ClearPickups()
    {
        for (int i = 0; i < activeCount; i++)
        {
            DestroyVisual(i);
            pickups[i] = default;
            claimed[i] = false;
        }

        activeCount = 0;
        spawnedRound = 0;
    }

    private void OnPhaseChanged(TotemMatchPhase previous, TotemMatchPhase current)
    {
        switch (current)
        {
            case TotemMatchPhase.Round1Combat:
                SpawnForRound(1);
                break;
            case TotemMatchPhase.Round2Combat:
                SpawnForRound(2);
                break;
            case TotemMatchPhase.Round3Combat:
                SpawnForRound(3);
                break;
            case TotemMatchPhase.FrontEnd:
            case TotemMatchPhase.Result:
                ClearPickups();
                break;
        }
    }

    private static GameObject CreateVisual(in TotemMapResourcePickup pickup)
    {
        var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = $"MapResource_{pickup.InstanceId}_{pickup.PickupId}";
        visual.transform.position = pickup.Position + Vector3.up * 0.45f;
        visual.transform.localScale = Vector3.one * 0.55f;
        Collider collider = visual.GetComponent<Collider>();
        if (collider != null)
        {
            UnityEngine.Object.Destroy(collider);
        }

        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = ResolveColor(pickup.Pigment);
        }

        return visual;
    }

    private void DestroyVisual(int index)
    {
        if (index < 0 || index >= visuals.Length || visuals[index] == null)
        {
            return;
        }

        UnityEngine.Object.Destroy(visuals[index]);
        visuals[index] = null;
    }

    private static Color ResolveColor(TotemPigmentKind pigment)
    {
        switch (pigment)
        {
            case TotemPigmentKind.Fire: return new Color(1f, 0.22f, 0.08f);
            case TotemPigmentKind.Ice: return new Color(0.15f, 0.75f, 1f);
            case TotemPigmentKind.Lightning: return new Color(0.9f, 0.75f, 0.12f);
            default: return Color.white;
        }
    }
}
