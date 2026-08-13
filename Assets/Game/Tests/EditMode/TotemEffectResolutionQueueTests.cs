using System;
using NUnit.Framework;

public sealed class TotemEffectResolutionQueueTests
{
    [Test]
    public void Resolve_OrdersFrozenPrioritiesDescending()
    {
        var queue = new TotemEffectResolutionQueue();
        queue.Reset(new TotemResolutionIdentity(62031, 1));
        Submit(queue, TotemEffectEventKind.Torso, 0);
        Submit(queue, TotemEffectEventKind.RifleArm, 1);
        Submit(queue, TotemEffectEventKind.Weakpoint, 2);
        Submit(queue, TotemEffectEventKind.Move, 3);
        Submit(queue, TotemEffectEventKind.ReservedActiveSkillArm, 4);
        Submit(queue, TotemEffectEventKind.Dodge, 5);

        queue.Resolve();

        AssertKind(queue, 0, TotemEffectEventKind.ReservedActiveSkillArm);
        AssertKind(queue, 1, TotemEffectEventKind.Dodge);
        AssertKind(queue, 2, TotemEffectEventKind.Move);
        AssertKind(queue, 3, TotemEffectEventKind.Weakpoint);
        AssertKind(queue, 4, TotemEffectEventKind.RifleArm);
        AssertKind(queue, 5, TotemEffectEventKind.Torso);
    }

    [Test]
    public void Resolve_SameSeedAndResolutionReplaySameTieOrder()
    {
        var left = BuildEqualPriorityQueue(777, 42);
        var right = BuildEqualPriorityQueue(777, 42);
        left.Resolve();
        right.Resolve();

        for (int i = 0; i < left.Count; i++)
        {
            Assert.That(left.TryGetResolvedAt(i, out TotemEffectEvent leftEvent), Is.True);
            Assert.That(right.TryGetResolvedAt(i, out TotemEffectEvent rightEvent), Is.True);
            Assert.That(leftEvent.SubmissionSequence, Is.EqualTo(rightEvent.SubmissionSequence));
        }
    }

    [Test]
    public void Queue_RejectsOverflowAndResolvedMutationThenResetsIndependently()
    {
        var queue = new TotemEffectResolutionQueue(2);
        queue.Reset(new TotemResolutionIdentity(1, 1));
        Assert.That(queue.TrySubmit(Event(TotemEffectEventKind.RifleArm, 0)), Is.True);
        Assert.That(queue.TrySubmit(Event(TotemEffectEventKind.Torso, 1)), Is.True);
        Assert.That(queue.TrySubmit(Event(TotemEffectEventKind.Weakpoint, 2)), Is.False);
        queue.Resolve();
        Assert.That(queue.TrySubmit(Event(TotemEffectEventKind.Weakpoint, 3)), Is.False);

        queue.Reset(new TotemResolutionIdentity(1, 2));
        Assert.That(queue.Count, Is.Zero);
        Assert.That(queue.IsResolved, Is.False);
        Assert.That(queue.TrySubmit(Event(TotemEffectEventKind.Weakpoint, 4)), Is.True);
    }

    [Test]
    public void PresentationDelay_DoesNotChangeResolvedSimulationOrder()
    {
        var queue = new TotemEffectResolutionQueue();
        queue.Reset(new TotemResolutionIdentity(9, 3));
        Submit(queue, TotemEffectEventKind.RifleArm, 0);
        Submit(queue, TotemEffectEventKind.Weakpoint, 1);
        Submit(queue, TotemEffectEventKind.Torso, 2);
        queue.Resolve();

        var delayed = new TotemEffectPresentationBuffer();
        var immediate = new TotemEffectPresentationBuffer();
        Assert.That(delayed.Build(queue, zeroDelay: false, 0.1f), Is.EqualTo(3));
        Assert.That(immediate.Build(queue, zeroDelay: true, 0.1f), Is.EqualTo(3));
        for (int i = 0; i < queue.Count; i++)
        {
            Assert.That(delayed.TryGetAt(i, out TotemEffectPresentationInstruction delayedInstruction), Is.True);
            Assert.That(immediate.TryGetAt(i, out TotemEffectPresentationInstruction immediateInstruction), Is.True);
            Assert.That(delayedInstruction.AssetKey, Is.EqualTo(immediateInstruction.AssetKey));
            Assert.That(delayedInstruction.DelaySeconds, Is.EqualTo(0.1f * i).Within(0.0001f));
            Assert.That(immediateInstruction.DelaySeconds, Is.Zero);
        }
    }

    [Test]
    public void SteadyResolution_AllocatesZeroBytesOnCurrentThread()
    {
        var queue = new TotemEffectResolutionQueue();
        var presentation = new TotemEffectPresentationBuffer();
        ResolveOnce(queue, presentation, 1);
        GC.GetAllocatedBytesForCurrentThread();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 128; i++)
        {
            ResolveOnce(queue, presentation, i + 2);
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.Zero);
    }

    private static TotemEffectResolutionQueue BuildEqualPriorityQueue(int seed, int resolution)
    {
        var queue = new TotemEffectResolutionQueue(8);
        queue.Reset(new TotemResolutionIdentity(seed, resolution));
        for (int i = 0; i < 8; i++)
        {
            Submit(queue, TotemEffectEventKind.RifleArm, i);
        }

        return queue;
    }

    private static void ResolveOnce(TotemEffectResolutionQueue queue, TotemEffectPresentationBuffer presentation, int resolution)
    {
        queue.Reset(new TotemResolutionIdentity(12345, resolution));
        queue.TrySubmit(Event(TotemEffectEventKind.Torso, 0));
        queue.TrySubmit(Event(TotemEffectEventKind.RifleArm, 1));
        queue.TrySubmit(Event(TotemEffectEventKind.Weakpoint, 2));
        queue.Resolve();
        presentation.Build(queue, zeroDelay: false);
    }

    private static TotemEffectEvent Event(TotemEffectEventKind kind, int submissionSequence)
    {
        return new TotemEffectEvent(kind, new TotemParticipantId(1), 100, submissionSequence, 1f);
    }

    private static void Submit(TotemEffectResolutionQueue queue, TotemEffectEventKind kind, int submissionSequence)
    {
        Assert.That(queue.TrySubmit(Event(kind, submissionSequence)), Is.True);
    }

    private static void AssertKind(TotemEffectResolutionQueue queue, int index, TotemEffectEventKind expected)
    {
        Assert.That(queue.TryGetResolvedAt(index, out TotemEffectEvent effectEvent), Is.True);
        Assert.That(effectEvent.Kind, Is.EqualTo(expected));
    }
}
