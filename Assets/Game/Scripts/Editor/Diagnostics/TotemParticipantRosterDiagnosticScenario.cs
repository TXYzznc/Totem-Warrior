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
            var roster = TotemActorService.BuildActorRoster(map, new TotemStartupSelection
            {
                CharacterId = 1,
                WeaponId = "knife_basic",
                ColorId = 1,
                PatternIds = new[] { 1 },
            });

            int humanCount = roster.Count(item => item.ControllerKind == TotemParticipantControllerKind.Human);
            int smartCount = roster.Count(item => item.ControllerKind == TotemParticipantControllerKind.SmartBot);
            int lightCount = roster.Count(item => item.ControllerKind == TotemParticipantControllerKind.LightBot);
            context.AssertEqual(50, roster.Length, "roster.participantCount");
            context.AssertEqual(1, humanCount, "roster.humanCount");
            context.AssertEqual(20, smartCount, "roster.smartBotCount");
            context.AssertEqual(29, lightCount, "roster.lightBotCount");
            context.Assert(roster.All(item => TotemActorService.IsParticipantKind(item.Kind)),
                "Roster must contain Participant kinds only.");
            float minDistance = float.MaxValue;
            for (int i = 0; i < roster.Length; i++)
            {
                for (int j = i + 1; j < roster.Length; j++)
                {
                    float distance = FlatDistance(roster[i].Position, roster[j].Position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }
            }

            context.Detail("roster.minimumSpawnDistance", minDistance.ToString("F2"));
            context.Assert(minDistance >= TotemActorService.ParticipantSpawnMinDistance - 0.01f,
                "All 50 participants must preserve the configured minimum spawn distance.");
            context.Pass("The active Actor roster contains exactly 50 equal participants and no Boss or EnemyConfig payload.");
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
