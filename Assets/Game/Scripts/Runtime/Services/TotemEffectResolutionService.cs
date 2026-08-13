using System;

/// <summary>
/// Owns the first-playable deterministic effect queue. Gameplay is resolved
/// immediately in priority order; presentation may replay the frozen result
/// with a small delay between steps without changing simulation timing.
/// </summary>
public sealed class TotemEffectResolutionService : TotemRuntimeServiceBase
{
    private readonly TotemEffectResolutionQueue queue = new TotemEffectResolutionQueue();
    private readonly TotemEffectPresentationBuffer presentation = new TotemEffectPresentationBuffer();
    private TotemMapService mapService;
    private int resolutionSequence;
    private int submissionSequence;
    private int resolvedEventCount;

    public override string ServiceName => "EffectResolution";

    public TotemEffectResolutionQueue Queue => queue;

    public TotemEffectPresentationBuffer Presentation => presentation;

    public int ResolutionSequence => resolutionSequence;

    public int ResolvedEventCount => resolvedEventCount;

    public event Action<TotemEffectEvent> EffectResolved;

    protected override void OnInitialize(TotemGameRuntime runtime)
    {
        mapService = runtime.GetService<TotemMapService>();
        queue.Reset(new TotemResolutionIdentity(ResolveMatchSeed(), 0));
    }

    protected override void OnShutdown()
    {
        mapService = null;
        resolutionSequence = 0;
        submissionSequence = 0;
        resolvedEventCount = 0;
        EffectResolved = null;
        queue.Reset(default);
        presentation.Build(queue, zeroDelay: true);
    }

    public void BeginResolution()
    {
        unchecked
        {
            resolutionSequence++;
        }

        submissionSequence = 0;
        queue.Reset(new TotemResolutionIdentity(ResolveMatchSeed(), resolutionSequence));
    }

    public bool TrySubmit(TotemEffectEventKind kind, TotemParticipantId sourceParticipantId, int targetCombatantId, float scalar = 0f)
    {
        var effectEvent = new TotemEffectEvent(
            kind,
            sourceParticipantId,
            targetCombatantId,
            submissionSequence,
            scalar);
        if (!queue.TrySubmit(effectEvent))
        {
            return false;
        }

        submissionSequence++;
        return true;
    }

    public int SubmitGunHit(in TotemDirectDamageResult damageResult)
    {
        if (!damageResult.IsEffectiveDirectDamage)
        {
            return 0;
        }

        int submitted = 0;
        TotemGunHitContext hit = damageResult.Hit;
        if (hit.IsWeakpoint && TrySubmit(
                TotemEffectEventKind.Weakpoint,
                hit.SourceParticipantId,
                hit.TargetCombatantId,
                damageResult.EffectiveDamage))
        {
            submitted++;
        }

        if (damageResult.CanSubmitRifleArmEvent && TrySubmit(
                TotemEffectEventKind.RifleArm,
                hit.SourceParticipantId,
                hit.TargetCombatantId,
                damageResult.EffectiveDamage))
        {
            submitted++;
        }

        if (TrySubmit(
                TotemEffectEventKind.Torso,
                hit.SourceParticipantId,
                hit.TargetCombatantId,
                damageResult.EffectiveDamage))
        {
            submitted++;
        }

        return submitted;
    }

    public int Resolve(bool zeroPresentationDelay = false)
    {
        queue.Resolve();
        presentation.Build(queue, zeroPresentationDelay);
        for (int i = 0; i < queue.Count; i++)
        {
            if (!queue.TryGetResolvedAt(i, out TotemEffectEvent effectEvent))
            {
                break;
            }

            resolvedEventCount++;
            EffectResolved?.Invoke(effectEvent);
        }

        return queue.Count;
    }

    private int ResolveMatchSeed()
    {
        return mapService?.CurrentMap?.Seed ?? 1;
    }
}
