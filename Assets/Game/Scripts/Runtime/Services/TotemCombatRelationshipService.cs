public sealed class TotemCombatRelationshipService : TotemRuntimeServiceBase
{
    public const float ParticipantCombatGraceSeconds = 60f;

    private readonly TotemCombatRelationshipSnapshot snapshot = new TotemCombatRelationshipSnapshot();

    public override string ServiceName => "CombatRelationship";

    public TotemCombatRelationshipDecision EvaluateDamage(
        TotemCombatantModel source,
        TotemCombatantModel target,
        TotemCombatRelationshipContext context)
    {
        var decision = Evaluate(source, target, context);
        snapshot.evaluationCount++;
        if (decision.Allowed)
        {
            snapshot.allowedCount++;
        }
        else
        {
            snapshot.blockedCount++;
        }

        snapshot.lastSourceId = source?.CombatantId ?? 0;
        snapshot.lastTargetId = target?.CombatantId ?? 0;
        snapshot.lastReason = decision.Reason.ToString();
        snapshot.lastWorldTime = context.WorldTime;
        return decision;
    }

    public TotemCombatRelationshipSnapshot CaptureSnapshot()
    {
        return new TotemCombatRelationshipSnapshot
        {
            evaluationCount = snapshot.evaluationCount,
            allowedCount = snapshot.allowedCount,
            blockedCount = snapshot.blockedCount,
            lastSourceId = snapshot.lastSourceId,
            lastTargetId = snapshot.lastTargetId,
            lastReason = snapshot.lastReason,
            lastWorldTime = snapshot.lastWorldTime,
        };
    }

    protected override void OnShutdown()
    {
        snapshot.evaluationCount = 0;
        snapshot.allowedCount = 0;
        snapshot.blockedCount = 0;
        snapshot.lastSourceId = 0;
        snapshot.lastTargetId = 0;
        snapshot.lastReason = string.Empty;
        snapshot.lastWorldTime = 0f;
    }

    public static TotemCombatRelationshipDecision Evaluate(
        TotemCombatantModel source,
        TotemCombatantModel target,
        TotemCombatRelationshipContext context)
    {
        if (target == null)
        {
            return Block(TotemCombatRelationshipReason.BlockedNullTarget);
        }

        if (!target.IsAlive)
        {
            return Block(TotemCombatRelationshipReason.BlockedTargetDead);
        }

        if (ReferenceEquals(source, target))
        {
            return Block(TotemCombatRelationshipReason.BlockedSelf);
        }

        if (target is TotemParticipantModel targetParticipant)
        {
            var targetDecision = EvaluateParticipantTarget(targetParticipant);
            if (!targetDecision.Allowed)
            {
                return targetDecision;
            }
        }

        if (source == null)
        {
            if (target.Domain == TotemCombatantDomain.Participant)
            {
                return Allow(TotemCombatRelationshipReason.AllowedWorldToParticipant);
            }

            return context.WorldDamageAffectsEnemies
                ? Allow(TotemCombatRelationshipReason.AllowedWorldToEnemy)
                : Block(TotemCombatRelationshipReason.BlockedWorldEnemyDamage);
        }

        if (!source.IsAlive)
        {
            return Block(TotemCombatRelationshipReason.BlockedSourceDead);
        }

        if (source is TotemParticipantModel sourceParticipant)
        {
            var sourceDecision = EvaluateParticipantSource(sourceParticipant);
            if (!sourceDecision.Allowed)
            {
                return sourceDecision;
            }
        }

        if (source.Domain == TotemCombatantDomain.Participant
            && target.Domain == TotemCombatantDomain.Participant
            && context.WorldTime < ParticipantCombatGraceSeconds)
        {
            return Block(TotemCombatRelationshipReason.BlockedParticipantCombatGracePeriod);
        }

        if (source.Domain == TotemCombatantDomain.Participant && target.Domain == TotemCombatantDomain.Enemy)
        {
            return Allow(TotemCombatRelationshipReason.AllowedParticipantToEnemy);
        }

        if (source.Domain == TotemCombatantDomain.Enemy && target.Domain == TotemCombatantDomain.Participant)
        {
            return Allow(TotemCombatRelationshipReason.AllowedEnemyToParticipant);
        }

        if (source.Domain == TotemCombatantDomain.Participant && target.Domain == TotemCombatantDomain.Participant)
        {
            return Allow(TotemCombatRelationshipReason.AllowedParticipantToParticipant);
        }

        if (source.Domain == TotemCombatantDomain.Enemy && target.Domain == TotemCombatantDomain.Enemy)
        {
            return context.AllowEnemyFriendlyFire
                ? Allow(TotemCombatRelationshipReason.AllowedWorldToEnemy)
                : Block(TotemCombatRelationshipReason.BlockedEnemyFriendlyFire);
        }

        return Block(TotemCombatRelationshipReason.Unknown);
    }

    private static TotemCombatRelationshipDecision EvaluateParticipantSource(TotemParticipantModel participant)
    {
        switch (participant.Lifecycle)
        {
            case TotemParticipantLifecycle.Active:
                return Allow(TotemCombatRelationshipReason.Unknown);
            case TotemParticipantLifecycle.Loading:
            case TotemParticipantLifecycle.Reserved:
                return Block(TotemCombatRelationshipReason.BlockedSourceLoading);
            case TotemParticipantLifecycle.Protected:
                return Block(TotemCombatRelationshipReason.BlockedSourceProtected);
            default:
                return Block(TotemCombatRelationshipReason.BlockedSourceInactive);
        }
    }

    private static TotemCombatRelationshipDecision EvaluateParticipantTarget(TotemParticipantModel participant)
    {
        switch (participant.Lifecycle)
        {
            case TotemParticipantLifecycle.Active:
                return Allow(TotemCombatRelationshipReason.Unknown);
            case TotemParticipantLifecycle.Loading:
            case TotemParticipantLifecycle.Reserved:
                return Block(TotemCombatRelationshipReason.BlockedTargetLoading);
            case TotemParticipantLifecycle.Protected:
                return Block(TotemCombatRelationshipReason.BlockedTargetProtected);
            default:
                return Block(TotemCombatRelationshipReason.BlockedTargetInactive);
        }
    }

    private static TotemCombatRelationshipDecision Allow(TotemCombatRelationshipReason reason)
    {
        return new TotemCombatRelationshipDecision(true, reason);
    }

    private static TotemCombatRelationshipDecision Block(TotemCombatRelationshipReason reason)
    {
        return new TotemCombatRelationshipDecision(false, reason);
    }
}
