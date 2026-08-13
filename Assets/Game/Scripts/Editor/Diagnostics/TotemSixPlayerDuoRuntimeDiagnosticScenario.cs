#if UNITY_EDITOR
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemSixPlayerDuoRuntimeDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Six Player Duo Runtime";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            TotemMapSnapshot map = TotemMapService.BuildLayout(62031, 1);
            TotemActorSpawnInfo[] roster = TotemActorService.BuildActorRoster(map);
            context.AssertEqual(6, roster.Length, "sixPlayer.roster.count");

            var slots = new TotemRosterSlot[roster.Length];
            int humanCount = 0;
            for (int i = 0; i < roster.Length; i++)
            {
                var item = roster[i];
                if (item.ControllerKind == TotemParticipantControllerKind.Human)
                {
                    humanCount++;
                }

                slots[i] = new TotemRosterSlot(
                    new TotemParticipantId(item.ActorId),
                    new TotemTeamId(item.TeamId),
                    item.ControllerKind == TotemParticipantControllerKind.Human
                        ? TotemFirstPlayableParticipantKind.Human
                        : TotemFirstPlayableParticipantKind.Bot,
                    TotemFirstPlayableLifeState.Alive);
                context.Assert(TotemMapService.IsTerrainWalkable(TotemMapService.QueryTerrain(map, item.Position)),
                    $"Participant {item.ActorId} must use a legal walkable spawn.");
                for (int j = 0; j < i; j++)
                {
                    context.Assert(FlatDistance(item.Position, roster[j].Position) >= TotemActorService.TeammateSpawnMinDistance - 0.01f,
                        $"Participants {item.ActorId} and {roster[j].ActorId} must not share a spawn position.");
                }
            }

            context.AssertEqual(1, humanCount, "sixPlayer.roster.humanCount");
            context.Assert(TotemRosterContract.Validate(slots, out string rosterError), rosterError);
            for (int team = 0; team < TotemFirstPlayableRules.TeamCount; team++)
            {
                TotemActorSpawnInfo first = roster[team * TotemFirstPlayableRules.TeamSize];
                TotemActorSpawnInfo second = roster[team * TotemFirstPlayableRules.TeamSize + 1];
                context.AssertEqual(team, first.TeamId, $"sixPlayer.team.{team}.first");
                context.AssertEqual(team, second.TeamId, $"sixPlayer.team.{team}.second");
                context.Assert(FlatDistance(first.Position, second.Position) <= TotemActorService.TeammateSpawnRadius * 2f + 0.01f,
                    $"Team {team} must spawn together.");
            }

            var player = Participant(roster[0]);
            var teammate = Participant(roster[1]);
            var opponent = Participant(roster[2]);
            var relationshipContext = new TotemCombatRelationshipContext(0f);
            var friendly = TotemCombatRelationshipService.Evaluate(player, teammate, relationshipContext);
            var hostile = TotemCombatRelationshipService.Evaluate(player, opponent, relationshipContext);
            context.AssertEqual(TotemCombatRelationshipReason.BlockedParticipantFriendlyFire, friendly.Reason, "sixPlayer.friendlyFire");
            context.AssertEqual(TotemCombatRelationshipReason.AllowedParticipantToParticipant, hostile.Reason, "sixPlayer.round1Pvp");

            TotemActorSpawnInfo[] repeated = TotemActorService.BuildActorRoster(map);
            for (int i = 0; i < roster.Length; i++)
            {
                context.AssertEqual(roster[i].Position, repeated[i].Position, $"sixPlayer.seededSpawn.{i}");
            }

            context.Pass("Six participants form three seeded duo teams with adjacent legal spawns, no friendly fire and immediate opponent PvP.");
        }

        private static TotemParticipantModel Participant(TotemActorSpawnInfo info)
        {
            return new TotemParticipantModel(
                info.ActorId,
                info.Name,
                info.ControllerKind,
                info.MaxHealth,
                info.Position,
                TotemParticipantLifecycle.Active,
                info.TeamId);
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return Mathf.Sqrt(x * x + z * z);
        }
    }
}
#endif
