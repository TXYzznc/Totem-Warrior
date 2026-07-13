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
            var human = Participant(1, TotemParticipantControllerKind.Human);
            var smart = Participant(2, TotemParticipantControllerKind.SmartBot);
            var lightBot = Participant(3, TotemParticipantControllerKind.LightBot);
            var enemy = new TotemEnemyModel(1001, "enemy_common_hunter", "Hunter", "common", TotemEnemyTier.Light, 50f, Vector3.forward);
            var secondEnemy = new TotemEnemyModel(1002, "enemy_common_shooter", "Shooter", "common", TotemEnemyTier.Light, 40f, Vector3.right);

            context.AssertEqual(TotemCombatantDomain.Participant, human.Domain, "domain.human");
            context.AssertEqual(TotemCombatantDomain.Participant, smart.Domain, "domain.smartBot");
            context.AssertEqual(TotemCombatantDomain.Participant, lightBot.Domain, "domain.lightBot");
            context.AssertEqual(TotemCombatantDomain.Enemy, enemy.Domain, "domain.enemy");

            AssertDecision(context, human, smart, At(0f), false, TotemCombatRelationshipReason.BlockedParticipantCombatGracePeriod, "participant.participant.gracePeriod");
            AssertDecision(context, human, smart, At(TotemCombatRelationshipService.ParticipantCombatGraceSeconds), true, TotemCombatRelationshipReason.AllowedParticipantToParticipant, "participant.participant.active");
            AssertDecision(context, human, enemy, At(10f), true, TotemCombatRelationshipReason.AllowedParticipantToEnemy, "participant.enemy");
            AssertDecision(context, enemy, smart, At(10f), true, TotemCombatRelationshipReason.AllowedEnemyToParticipant, "enemy.smartBot");
            AssertDecision(context, enemy, lightBot, At(10f), true, TotemCombatRelationshipReason.AllowedEnemyToParticipant, "enemy.lightBot");
            AssertDecision(context, enemy, secondEnemy, At(10f), false, TotemCombatRelationshipReason.BlockedEnemyFriendlyFire, "enemy.friendlyFire");

            smart.SetLifecycle(TotemParticipantLifecycle.Protected, "Diagnostics");
            AssertDecision(context, enemy, smart, At(70f), false, TotemCombatRelationshipReason.BlockedTargetProtected, "target.protected");
            AssertDecision(context, smart, enemy, At(70f), false, TotemCombatRelationshipReason.BlockedSourceProtected, "source.protected");

            smart.SetLifecycle(TotemParticipantLifecycle.Loading, "Diagnostics");
            AssertDecision(context, enemy, smart, At(70f), false, TotemCombatRelationshipReason.BlockedTargetLoading, "target.loading");

            AssertDecision(context, null, enemy, At(70f), false, TotemCombatRelationshipReason.BlockedWorldEnemyDamage, "world.enemy.default");
            AssertDecision(context, null, enemy, new TotemCombatRelationshipContext(70f, worldDamageAffectsEnemies: true), true, TotemCombatRelationshipReason.AllowedWorldToEnemy, "world.enemy.enabled");
            context.Pass("Participant and Enemy domains use one deterministic relationship matrix.");
        }

        private static TotemParticipantModel Participant(int id, TotemParticipantControllerKind kind)
        {
            return new TotemParticipantModel(id, kind.ToString(), kind, 100f, Vector3.zero, TotemParticipantLifecycle.Active);
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
