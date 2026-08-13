#if UNITY_EDITOR
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemCombatDomainDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Combat Domain Contract";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            var human = Participant(1, TotemParticipantControllerKind.Human, 0);
            var smart = Participant(2, TotemParticipantControllerKind.SmartBot, 0);
            var lightBot = Participant(3, TotemParticipantControllerKind.LightBot, 1);

            context.AssertEqual(TotemCombatantDomain.Participant, human.Domain, "domain.human");
            context.AssertEqual(TotemCombatantDomain.Participant, smart.Domain, "domain.smartBot");
            context.AssertEqual(TotemCombatantDomain.Participant, lightBot.Domain, "domain.lightBot");

            AssertDecision(context, human, smart, At(0f), false, TotemCombatRelationshipReason.BlockedParticipantFriendlyFire, "participant.teammate.friendlyFire");
            AssertDecision(context, human, lightBot, At(0f), true, TotemCombatRelationshipReason.AllowedParticipantToParticipant, "participant.opponent.round1");
            smart.SetLifecycle(TotemParticipantLifecycle.Protected, "Diagnostics");
            AssertDecision(context, human, smart, At(70f), false, TotemCombatRelationshipReason.BlockedTargetProtected, "target.protected");
            AssertDecision(context, smart, lightBot, At(70f), false, TotemCombatRelationshipReason.BlockedSourceProtected, "source.protected");

            smart.SetLifecycle(TotemParticipantLifecycle.Loading, "Diagnostics");
            AssertDecision(context, human, smart, At(70f), false, TotemCombatRelationshipReason.BlockedTargetLoading, "target.loading");
            context.Pass("Six-player pure-PVP participants use one deterministic relationship matrix.");
        }

        private static TotemParticipantModel Participant(int id, TotemParticipantControllerKind kind, int teamId)
        {
            return new TotemParticipantModel(id, kind.ToString(), kind, 100f, Vector3.zero, TotemParticipantLifecycle.Active, teamId);
        }

        private static TotemCombatRelationshipContext At(float worldTime)
        {
            return new TotemCombatRelationshipContext(worldTime);
        }

        private static void AssertDecision(
            GFDiagnosticScenarioContext context,
            TotemCombatantModel source,
            TotemCombatantModel target,
            TotemCombatRelationshipContext relationshipContext,
            bool expectedAllowed,
            TotemCombatRelationshipReason expectedReason,
            string key)
        {
            var decision = TotemCombatRelationshipService.Evaluate(source, target, relationshipContext);
            context.AssertEqual(expectedAllowed, decision.Allowed, key + ".allowed");
            context.AssertEqual(expectedReason, decision.Reason, key + ".reason");
        }
    }
}
#endif
