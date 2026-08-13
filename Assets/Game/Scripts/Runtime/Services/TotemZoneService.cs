using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TotemZoneService : TotemRuntimeServiceBase, ITotemRuntimeTickService, ITotemGameplaySimulationService
{
    private TotemGameFlowService flowService;
    private TotemMapService mapService;
    private TotemActorService actorService;
    private TotemMatchFlowService matchFlowService;
    private TotemZonePhase[] runtimePhases = Array.Empty<TotemZonePhase>();
    private bool active;
    private float elapsedSec;
    private int lastOutZoneAffectedActorCount;
    private int lastOutZoneKilledActorCount;
    private float lastOutZoneDamageTick;
    private float totalOutZoneDamage;

    public override string ServiceName => "Zone";

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        flowService = runtime.GetService<TotemGameFlowService>();
        mapService = runtime.GetService<TotemMapService>();
        actorService = runtime.GetService<TotemActorService>();
        matchFlowService = runtime.GetService<TotemMatchFlowService>();
        runtimePhases = NonEmpty(runtime.GetService<TotemDataService>()?.GameplayCatalog?.CreateZonePhases(), LoadPhases());
        if (flowService != null)
        {
            flowService.StateChanged += OnFlowStateChanged;
        }
    }

    protected override void OnShutdown()
    {
        if (flowService != null)
        {
            flowService.StateChanged -= OnFlowStateChanged;
            flowService = null;
        }

        mapService = null;
        actorService = null;
        matchFlowService = null;
        runtimePhases = Array.Empty<TotemZonePhase>();
        DeactivateZone();
    }

    public void Tick(float deltaTime)
    {
        if (!active || deltaTime <= 0f || matchFlowService?.IsZoneShrinking != true)
        {
            return;
        }

        elapsedSec += deltaTime;
        ApplyOutZoneDamage(deltaTime);
    }

    public static IReadOnlyList<TotemZonePhase> GetPhases()
    {
        return LoadPhases();
    }

    public static TotemZonePhase GetPhaseAt(float elapsed)
    {
        return GetPhaseAt(elapsed, LoadPhases());
    }

    public static float ComputeRadius(float elapsed, float mapSize)
    {
        var phases = LoadPhases();
        var phase = GetPhaseAt(elapsed, phases);
        int phaseIndex = IndexOfPhase(phases, phase);
        float phaseStartRadius = phaseIndex <= 0 ? mapSize * 0.5f : phases[phaseIndex - 1].TargetRadius;
        float t = phase.Duration <= 0f ? 1f : Mathf.Clamp01((elapsed - phase.StartTime) / phase.Duration);
        return Mathf.Lerp(phaseStartRadius, phase.TargetRadius, t);
    }

    public IReadOnlyList<TotemZonePhase> GetRuntimePhases()
    {
        return runtimePhases;
    }

    public TotemZonePhase GetRuntimePhaseAt(float elapsed)
    {
        return GetPhaseAt(elapsed, NonEmpty(runtimePhases, LoadPhases()));
    }

    public float ComputeRuntimeRadius(float elapsed, float mapSize)
    {
        if (matchFlowService != null)
        {
            return ComputeFirstPlayableRadius(mapSize);
        }

        var phases = NonEmpty(runtimePhases, LoadPhases());
        var phase = GetRuntimePhaseAt(elapsed);
        int phaseIndex = IndexOfPhase(phases, phase);
        float phaseStartRadius = phaseIndex <= 0 ? mapSize * 0.5f : phases[phaseIndex - 1].TargetRadius;
        float t = phase.Duration <= 0f ? 1f : Mathf.Clamp01((elapsed - phase.StartTime) / phase.Duration);
        return Mathf.Lerp(phaseStartRadius, phase.TargetRadius, t);
    }

    public static float ComputeFirstPlayableRadius(
        float mapSize,
        float firstTargetRadius,
        float secondTargetRadius,
        float thirdTargetRadius,
        float fourthTargetRadius,
        TotemMatchPhase phase,
        TotemMatchActivity activity,
        float shrinkProgress)
    {
        float initialRadius = Mathf.Max(1f, mapSize * 0.5f);
        float firstTarget = Mathf.Min(initialRadius, Mathf.Max(1f, firstTargetRadius));
        float secondTarget = Mathf.Min(firstTarget, Mathf.Max(1f, secondTargetRadius));
        float thirdTarget = Mathf.Min(secondTarget, Mathf.Max(1f, thirdTargetRadius));
        float fourthTarget = Mathf.Min(thirdTarget, Mathf.Max(1f, fourthTargetRadius));
        float progress = Mathf.Clamp01(shrinkProgress);
        switch (phase)
        {
            case TotemMatchPhase.Round2Combat:
                return activity == TotemMatchActivity.ZoneShrink
                    ? Mathf.Lerp(initialRadius, firstTarget, progress)
                    : firstTarget;
            case TotemMatchPhase.Build3:
                return firstTarget;
            case TotemMatchPhase.Round3Combat:
                return activity == TotemMatchActivity.ZoneShrink
                    ? Mathf.Lerp(firstTarget, secondTarget, progress)
                    : secondTarget;
            case TotemMatchPhase.Build4:
                return secondTarget;
            case TotemMatchPhase.Round4Combat:
                return activity == TotemMatchActivity.ZoneShrink
                    ? Mathf.Lerp(secondTarget, thirdTarget, progress)
                    : thirdTarget;
            case TotemMatchPhase.Build5:
                return thirdTarget;
            case TotemMatchPhase.Round5Combat:
                return activity == TotemMatchActivity.ZoneShrink
                    ? Mathf.Lerp(thirdTarget, fourthTarget, progress)
                    : fourthTarget;
            case TotemMatchPhase.Result:
                return fourthTarget;
            default:
                return initialRadius;
        }
    }

    public TotemZoneSnapshot CaptureSnapshot()
    {
        int phaseIndex = ResolveFirstPlayablePhaseIndex();
        var phase = GetRuntimePhaseByIndex(phaseIndex);
        TotemMapSnapshot map = mapService?.CurrentMap;
        float mapSize = map != null ? TotemMapService.GetInitialZoneRadius(map) * 2f : TotemMapService.DefaultMapSize;
        return new TotemZoneSnapshot
        {
            active = active,
            elapsedSec = elapsedSec,
            currentPhaseId = phase.Id,
            currentRadius = ComputeFirstPlayableRadius(mapSize),
            outZoneDamage = phase.OutZoneDamage,
            outZoneAffectedActorCount = lastOutZoneAffectedActorCount,
            outZoneKilledActorCount = lastOutZoneKilledActorCount,
            lastOutZoneDamageTick = lastOutZoneDamageTick,
            totalOutZoneDamage = totalOutZoneDamage,
        };
    }

    public bool IsInsideCurrentZone(Vector3 position)
    {
        var map = mapService?.CurrentMap;
        if (map == null)
        {
            return true;
        }

        Vector3 flat = position;
        flat.y = 0f;
        Vector3 center = new Vector3(map.InitialZoneCenter.x, 0f, map.InitialZoneCenter.y);
        float radius = ComputeFirstPlayableRadius(TotemMapService.GetInitialZoneRadius(map) * 2f);
        return (flat - center).sqrMagnitude <= radius * radius;
    }

    private static TotemZonePhase[] LoadPhases()
    {
        return NonEmpty(
            TotemDataService.LoadGameplayCatalogOrDefault().CreateZonePhases(),
            Array.Empty<TotemZonePhase>());
    }

    private static TotemZonePhase GetPhaseAt(float elapsed, TotemZonePhase[] phases)
    {
        phases = NonEmpty(phases, Array.Empty<TotemZonePhase>());
        if (phases.Length <= 0)
        {
            return new TotemZonePhase();
        }

        TotemZonePhase current = phases[0];
        for (int i = 0; i < phases.Length; i++)
        {
            if (elapsed >= phases[i].StartTime)
            {
                current = phases[i];
            }
        }

        return current;
    }

    private static int IndexOfPhase(TotemZonePhase[] phases, TotemZonePhase phase)
    {
        if (phases == null || phase == null)
        {
            return 0;
        }

        for (int i = 0; i < phases.Length; i++)
        {
            if (ReferenceEquals(phases[i], phase) || phases[i].Id == phase.Id)
            {
                return i;
            }
        }

        return 0;
    }

    private TotemZonePhase GetRuntimePhaseByIndex(int index)
    {
        var phases = NonEmpty(runtimePhases, LoadPhases());
        if (phases.Length <= 0)
        {
            return new TotemZonePhase();
        }

        return phases[Mathf.Clamp(index, 0, phases.Length - 1)];
    }

    private int ResolveFirstPlayablePhaseIndex()
    {
        if (matchFlowService == null)
        {
            return 0;
        }

        switch (matchFlowService.CurrentPhase)
        {
            case TotemMatchPhase.Round3Combat:
            case TotemMatchPhase.Build4:
                return 1;
            case TotemMatchPhase.Round4Combat:
            case TotemMatchPhase.Build5:
                return 2;
            case TotemMatchPhase.Round5Combat:
            case TotemMatchPhase.Result:
                return 3;
            default:
                return 0;
        }
    }

    private float ComputeFirstPlayableRadius(float mapSize)
    {
        if (matchFlowService == null)
        {
            return Mathf.Max(1f, mapSize * 0.5f);
        }

        return ComputeFirstPlayableRadius(
            mapSize,
            GetRuntimePhaseByIndex(0).TargetRadius,
            GetRuntimePhaseByIndex(1).TargetRadius,
            GetRuntimePhaseByIndex(2).TargetRadius,
            GetRuntimePhaseByIndex(3).TargetRadius,
            matchFlowService.CurrentPhase,
            matchFlowService.CurrentActivity,
            ResolveShrinkProgress());
    }

    private float ResolveShrinkProgress()
    {
        float duration = matchFlowService?.ActivityDuration ?? 0f;
        return duration <= 0f ? 1f : Mathf.Clamp01(matchFlowService.ActivityElapsed / duration);
    }

    private static T[] NonEmpty<T>(T[] primary, T[] fallback)
    {
        return primary == null || primary.Length <= 0 ? fallback : primary;
    }

    private void OnFlowStateChanged(TotemGameFlowState previousState, TotemGameFlowState nextState)
    {
        if (nextState == TotemGameFlowState.CombatHud)
        {
            ActivateZone();
            GFTrace.Success("TotemZone", "Activated");
            return;
        }

        if (previousState == TotemGameFlowState.CombatHud)
        {
            DeactivateZone();
            GFTrace.Info("TotemZone", "Deactivated", null, GFTrace.Data("nextState", nextState.ToString()));
        }
    }

    private void ActivateZone()
    {
        active = true;
        elapsedSec = 0f;
        ClearDamageCounters();
    }

    private void DeactivateZone()
    {
        active = false;
        elapsedSec = 0f;
        ClearDamageCounters();
    }

    private void ApplyOutZoneDamage(float deltaTime)
    {
        if (actorService == null || mapService?.CurrentMap == null)
        {
            return;
        }

        var map = mapService.CurrentMap;
        var center = new Vector3(map.InitialZoneCenter.x, 0f, map.InitialZoneCenter.y);
        var phase = GetRuntimePhaseByIndex(ResolveFirstPlayablePhaseIndex());
        float radius = ComputeFirstPlayableRadius(TotemMapService.GetInitialZoneRadius(map) * 2f);
        float radiusSqr = radius * radius;
        lastOutZoneAffectedActorCount = 0;
        lastOutZoneKilledActorCount = 0;
        lastOutZoneDamageTick = 0f;
        float damage = phase.OutZoneDamage * deltaTime;
        if (damage <= 0f)
        {
            return;
        }

        for (int i = 0; i < actorService.Actors.Count; i++)
        {
            var actor = actorService.Actors[i];
            if (actor == null || !actor.IsAlive)
            {
                continue;
            }

            Vector3 flat = actor.Position;
            flat.y = 0f;
            if ((flat - center).sqrMagnitude > radiusSqr)
            {
                bool killed = actorService.ApplyDamage(actor, damage, null, "ShrinkZone");
                lastOutZoneAffectedActorCount++;
                if (killed)
                {
                    lastOutZoneKilledActorCount++;
                }

                lastOutZoneDamageTick += damage;
                totalOutZoneDamage += damage;
            }
        }
    }

    private void ClearDamageCounters()
    {
        lastOutZoneAffectedActorCount = 0;
        lastOutZoneKilledActorCount = 0;
        lastOutZoneDamageTick = 0f;
        totalOutZoneDamage = 0f;
    }
}
