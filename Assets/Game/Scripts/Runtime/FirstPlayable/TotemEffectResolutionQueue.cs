using System;

public sealed class TotemEffectResolutionQueue
{
    public const int DefaultCapacity = 16;

    private readonly TotemEffectEvent[] events;
    private TotemResolutionIdentity identity;
    private int count;
    private bool resolved;

    public TotemEffectResolutionQueue(int capacity = DefaultCapacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        events = new TotemEffectEvent[capacity];
    }

    public int Count => count;
    public int Capacity => events.Length;
    public bool IsResolved => resolved;
    public TotemResolutionIdentity Identity => identity;

    public void Reset(TotemResolutionIdentity resolutionIdentity)
    {
        identity = resolutionIdentity;
        count = 0;
        resolved = false;
    }

    public bool TrySubmit(in TotemEffectEvent effectEvent)
    {
        if (resolved || count >= events.Length)
        {
            return false;
        }

        events[count++] = effectEvent;
        return true;
    }

    public void Resolve()
    {
        if (resolved)
        {
            return;
        }

        for (int i = 1; i < count; i++)
        {
            TotemEffectEvent candidate = events[i];
            int insertion = i - 1;
            while (insertion >= 0 && Compare(candidate, events[insertion]) < 0)
            {
                events[insertion + 1] = events[insertion];
                insertion--;
            }

            events[insertion + 1] = candidate;
        }

        resolved = true;
    }

    public bool TryGetResolvedAt(int index, out TotemEffectEvent effectEvent)
    {
        if (!resolved || index < 0 || index >= count)
        {
            effectEvent = default;
            return false;
        }

        effectEvent = events[index];
        return true;
    }

    private int Compare(in TotemEffectEvent left, in TotemEffectEvent right)
    {
        int priorityOrder = right.Priority.CompareTo(left.Priority);
        if (priorityOrder != 0)
        {
            return priorityOrder;
        }

        uint leftOrder = identity.DeriveStableOrder(left.SubmissionSequence);
        uint rightOrder = identity.DeriveStableOrder(right.SubmissionSequence);
        int seededOrder = leftOrder.CompareTo(rightOrder);
        return seededOrder != 0
            ? seededOrder
            : left.SubmissionSequence.CompareTo(right.SubmissionSequence);
    }
}

public sealed class TotemEffectPresentationBuffer
{
    public const float DefaultStepDelaySeconds = 0.08f;

    private readonly TotemEffectPresentationInstruction[] instructions;
    private int count;

    public TotemEffectPresentationBuffer(int capacity = TotemEffectResolutionQueue.DefaultCapacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        instructions = new TotemEffectPresentationInstruction[capacity];
    }

    public int Count => count;
    public int Capacity => instructions.Length;

    public int Build(TotemEffectResolutionQueue queue, bool zeroDelay, float stepDelaySeconds = DefaultStepDelaySeconds)
    {
        count = 0;
        if (queue == null || !queue.IsResolved)
        {
            return 0;
        }

        float delayStep = zeroDelay ? 0f : Math.Max(0f, stepDelaySeconds);
        int limit = Math.Min(queue.Count, instructions.Length);
        for (int i = 0; i < limit; i++)
        {
            if (!queue.TryGetResolvedAt(i, out TotemEffectEvent effectEvent))
            {
                break;
            }

            instructions[count++] = new TotemEffectPresentationInstruction(
                ResolvePresentationKey(effectEvent.Kind),
                i,
                delayStep * i);
        }

        return count;
    }

    public bool TryGetAt(int index, out TotemEffectPresentationInstruction instruction)
    {
        if (index < 0 || index >= count)
        {
            instruction = default;
            return false;
        }

        instruction = instructions[index];
        return true;
    }

    private static string ResolvePresentationKey(TotemEffectEventKind kind)
    {
        switch (kind)
        {
            case TotemEffectEventKind.Weakpoint: return TotemFirstPlayableArtHandoff.VfxKeys.QueueWeakpoint;
            case TotemEffectEventKind.RifleArm: return TotemFirstPlayableArtHandoff.VfxKeys.QueueRifleArm;
            case TotemEffectEventKind.Torso: return TotemFirstPlayableArtHandoff.VfxKeys.QueueTorso;
            default: return TotemFirstPlayableArtHandoff.FallbackKeys.MissingVfx;
        }
    }
}
