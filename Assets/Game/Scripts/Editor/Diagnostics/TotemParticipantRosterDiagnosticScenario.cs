#if UNITY_EDITOR
using System.Linq;

namespace UGF.EditorTools
{
    public sealed class TotemParticipantRosterDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Participant Roster Contract";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            var map = TotemMapService.BuildLayout(91, 1);
            var roster = TotemActorService.BuildActorRoster(map);

            int humanCount = roster.Count(item => item.ControllerKind == TotemParticipantControllerKind.Human);
            int smartCount = roster.Count(item => item.ControllerKind == TotemParticipantControllerKind.SmartBot);
            int lightCount = roster.Count(item => item.ControllerKind == TotemParticipantControllerKind.LightBot);
            context.AssertEqual(6, roster.Length, "roster.participantCount");
            context.AssertEqual(1, humanCount, "roster.humanCount");
            context.AssertEqual(3, smartCount, "roster.smartBotCount");
            context.AssertEqual(2, lightCount, "roster.lightBotCount");
            context.Assert(roster.All(item => TotemActorService.IsParticipantKind(item.Kind)),
                "Roster must contain Participant kinds only.");
            var slots = new TotemRosterSlot[roster.Length];
            for (int i = 0; i < roster.Length; i++)
            {
                slots[i] = new TotemRosterSlot(
                    new TotemParticipantId(roster[i].ActorId),
                    new TotemTeamId(roster[i].TeamId),
                    roster[i].ControllerKind == TotemParticipantControllerKind.Human
                        ? TotemFirstPlayableParticipantKind.Human
                        : TotemFirstPlayableParticipantKind.Bot,
                    TotemFirstPlayableLifeState.Alive);
            }

            context.Assert(TotemRosterContract.Validate(slots, out string rosterError), rosterError);
            for (int team = 0; team < TotemFirstPlayableRules.TeamCount; team++)
            {
                var members = roster.Where(item => item.TeamId == team).ToArray();
                context.AssertEqual(2, members.Length, $"roster.team.{team}.count");
                context.Assert(FlatDistance(members[0].Position, members[1].Position) <= TotemActorService.TeammateSpawnRadius * 2f + 0.01f,
                    $"Team {team} members must spawn together.");
            }

            context.Pass("The active Actor roster contains 1 human + 5 bots in three adjacent duo teams.");
        }

        private static float FlatDistance(UnityEngine.Vector3 a, UnityEngine.Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return UnityEngine.Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
#endif
