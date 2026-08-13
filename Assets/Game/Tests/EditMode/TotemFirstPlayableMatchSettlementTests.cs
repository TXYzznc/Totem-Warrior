#if UNITY_INCLUDE_TESTS
using NUnit.Framework;

public sealed class TotemFirstPlayableMatchSettlementTests
{
    [Test]
    public void Resolve_UsesConfirmedLexicographicPriority()
    {
        var candidates = new[]
        {
            Candidate(1, eliminations: 2, damage: 900f, alive: 2, health: 180f),
            Candidate(2, eliminations: 3, damage: 100f, alive: 1, health: 20f),
            Candidate(3, eliminations: 2, damage: 1200f, alive: 2, health: 200f),
        };

        TotemMatchSettlement result = TotemFirstPlayableMatchSettlement.Resolve(candidates, candidates.Length);

        Assert.That(result.Resolved, Is.True);
        Assert.That(result.Draw, Is.False);
        Assert.That(result.Winner.TeamId, Is.EqualTo(2), "Eliminations must outrank all later criteria.");
    }

    [Test]
    public void Resolve_UsesDamageThenAliveCountThenHealth()
    {
        var candidates = new[]
        {
            Candidate(1, 2, 500f, 1, 90f),
            Candidate(2, 2, 500f, 2, 20f),
            Candidate(3, 2, 499f, 2, 200f),
        };

        TotemMatchSettlement result = TotemFirstPlayableMatchSettlement.Resolve(candidates, candidates.Length);

        Assert.That(result.Winner.TeamId, Is.EqualTo(2));
    }

    [Test]
    public void Resolve_ExactTieProducesDraw()
    {
        var candidates = new[]
        {
            Candidate(1, 2, 500f, 1, 90f),
            Candidate(2, 2, 500f, 1, 90f),
        };

        TotemMatchSettlement result = TotemFirstPlayableMatchSettlement.Resolve(candidates, candidates.Length);

        Assert.That(result.Resolved, Is.True);
        Assert.That(result.Draw, Is.True);
        Assert.That(result.Winner.IsValid, Is.False);
    }

    private static TotemTeamSettlementCandidate Candidate(
        int teamId,
        int eliminations,
        float damage,
        int alive,
        float health)
    {
        return new TotemTeamSettlementCandidate(teamId, eliminations, damage, alive, health, teamId * 2 - 1);
    }
}
#endif
