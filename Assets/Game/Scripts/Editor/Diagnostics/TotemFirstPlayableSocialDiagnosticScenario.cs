#if UNITY_EDITOR
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemFirstPlayableSocialDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem First Playable Intelligence And Pigment Trade";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            var counter = new TotemMatchAchievementCounter();
            counter.AddPlayerDamage(1f);
            counter.AddPlayerDown();
            counter.AddPlayerElimination();
            counter.AddAllyHealing(3f);
            counter.AddAllyShieldOrMitigation(4f);
            counter.AddSuccessfulRevive();
            counter.AddCleanseOrControlRemoval();
            counter.AddEffectiveControl(5f);
            counter.AddAllyDamageGainCreated(6f);
            counter.AddResourcesAcquired(7);
            counter.AddResourcesShared(8);
            counter.AddSelfDown();
            counter.AddIndirectElementDamage(9f);
            TotemMatchAchievementSnapshot achievement = counter.Capture();
            context.AssertEqual(1f, achievement.playerDamage, "intelligence.playerDamage");
            context.AssertEqual(5f, achievement.effectiveControlSeconds, "intelligence.controlSeconds");
            context.AssertEqual(9f, achievement.indirectElementDamage, "intelligence.indirectDamage");

            TotemActorModel actor = CreateActor(1, 0);
            var build = new TotemFirstPlayableTattooBuildState();
            build.SetPigment(TotemPigmentKind.Fire, 20);
            build.TryEquip(
                TotemMatchPhase.OpeningBuild,
                TotemTattooSlotId.RightArm,
                TotemFirstPlayablePatternId.P01,
                TotemFirstPlayableElement.Fire,
                out _);
            TotemConstructionIntelligenceSnapshot frozen =
                TotemFirstPlayableSocialService.CreateBoundarySnapshot(actor, build, achievement, TotemMatchPhase.Build2);
            build.TryRemove(TotemMatchPhase.Build2, TotemTattooSlotId.RightArm, out _);
            context.AssertEqual(1, frozen.tattoos.Length, "intelligence.frozenTattooCount");
            context.AssertEqual(TotemFirstPlayablePatternId.P01, frozen.tattoos[0].pattern, "intelligence.frozenPattern");
            context.AssertEqual(3, frozen.attributes.Length, "intelligence.attributeCount");
            context.Assert(
                frozen.tattoos[0].publicEffectText.IndexOfAny("0123456789".ToCharArray()) < 0,
                "Public tattoo effect text must not expose internal numeric values.");

            var ledger = new TotemPigmentTradeLedger();
            var donor = new TotemFirstPlayableTattooBuildState();
            var receiver = new TotemFirstPlayableTattooBuildState();
            donor.SetPigment(TotemPigmentKind.Ice, 8);
            receiver.SetPigment(TotemPigmentKind.Ice, 1);
            context.Assert(
                ledger.TryCreate(
                    new TotemParticipantId(1),
                    new TotemParticipantId(2),
                    TotemPigmentKind.Ice,
                    5,
                    1,
                    (int)TotemMatchPhase.Build2,
                    donor,
                    out TotemPigmentRequest request),
                "A request within the teammate's current inventory must be accepted.");
            context.Assert(
                ledger.TryResolve(
                    request.RequestId,
                    new TotemParticipantId(2),
                    true,
                    donor,
                    receiver,
                    out TotemPigmentRequest resolved,
                    out TotemPigmentTransfer transfer),
                "A valid approval must commit atomically.");
            context.AssertEqual(TotemPigmentRequestState.Approved, resolved.State, "pigment.requestState");
            context.AssertEqual(3, donor.GetPigment(TotemPigmentKind.Ice), "pigment.donorAfter");
            context.AssertEqual(6, receiver.GetPigment(TotemPigmentKind.Ice), "pigment.receiverAfter");
            context.Assert(transfer.RequiresAtomicCommit, "Approved transfer must expose an auditable commit contract.");

            context.Detail("intelligence.snapshotCount", TotemFirstPlayableRules.ParticipantCount.ToString());
            context.Pass("Frozen intelligence, exact achievement fields and atomic teammate pigment transfer contracts are deterministic.");
        }

        private static TotemActorModel CreateActor(int participantId, int teamId)
        {
            return new TotemActorModel(new TotemActorSpawnInfo
            {
                ActorId = participantId,
                TeamId = teamId,
                Name = "P" + participantId,
                Kind = TotemActorKind.Player,
                ControllerKind = TotemParticipantControllerKind.Human,
                Position = Vector3.zero,
                MaxHealth = 100f,
            });
        }
    }
}
#endif
