using System;

public readonly struct TotemTeamSettlementCandidate
{
    public TotemTeamSettlementCandidate(
        int teamId,
        int eliminations,
        float playerDamage,
        int aliveCount,
        float remainingHealth,
        int representativeParticipantId)
    {
        TeamId = teamId;
        Eliminations = Math.Max(0, eliminations);
        PlayerDamage = Math.Max(0f, playerDamage);
        AliveCount = Math.Max(0, aliveCount);
        RemainingHealth = Math.Max(0f, remainingHealth);
        RepresentativeParticipantId = Math.Max(0, representativeParticipantId);
    }

    public int TeamId { get; }
    public int Eliminations { get; }
    public float PlayerDamage { get; }
    public int AliveCount { get; }
    public float RemainingHealth { get; }
    public int RepresentativeParticipantId { get; }
    public bool IsValid => TeamId > 0 && RepresentativeParticipantId > 0;
}

public readonly struct TotemMatchSettlement
{
    public TotemMatchSettlement(bool resolved, bool draw, in TotemTeamSettlementCandidate winner)
    {
        Resolved = resolved;
        Draw = draw;
        Winner = winner;
    }

    public bool Resolved { get; }
    public bool Draw { get; }
    public TotemTeamSettlementCandidate Winner { get; }
}

public static class TotemFirstPlayableMatchSettlement
{
    private const float FloatTieEpsilon = 0.001f;

    public static TotemMatchSettlement Resolve(TotemTeamSettlementCandidate[] candidates, int count)
    {
        if (candidates == null || count <= 0)
        {
            return default;
        }

        int validCount = Math.Min(count, candidates.Length);
        int bestIndex = -1;
        bool tied = false;
        for (int i = 0; i < validCount; i++)
        {
            if (!candidates[i].IsValid)
            {
                continue;
            }

            if (bestIndex < 0)
            {
                bestIndex = i;
                tied = false;
                continue;
            }

            int comparison = Compare(candidates[i], candidates[bestIndex]);
            if (comparison > 0)
            {
                bestIndex = i;
                tied = false;
            }
            else if (comparison == 0)
            {
                tied = true;
            }
        }

        if (bestIndex < 0)
        {
            return default;
        }

        return new TotemMatchSettlement(true, tied, tied ? default : candidates[bestIndex]);
    }

    public static int Compare(in TotemTeamSettlementCandidate left, in TotemTeamSettlementCandidate right)
    {
        int comparison = left.Eliminations.CompareTo(right.Eliminations);
        if (comparison != 0) return comparison;

        comparison = CompareFloat(left.PlayerDamage, right.PlayerDamage);
        if (comparison != 0) return comparison;

        comparison = left.AliveCount.CompareTo(right.AliveCount);
        if (comparison != 0) return comparison;

        return CompareFloat(left.RemainingHealth, right.RemainingHealth);
    }

    private static int CompareFloat(float left, float right)
    {
        float difference = left - right;
        return Math.Abs(difference) <= FloatTieEpsilon ? 0 : difference > 0f ? 1 : -1;
    }
}
