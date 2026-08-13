#if UNITY_EDITOR
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemFirstPlayableLifecycleDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem First Playable Downed Lifecycle";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            var state = new TotemFirstPlayableParticipantLifeState(100f);
            context.Assert(
                state.TryEnterDowned(true, new TotemParticipantId(4), out TotemDownedStateContract downed),
                "A lethal hit with a living teammate must enter downed state.");
            context.AssertEqual(TotemDownedTransitionReason.LethalDamage, downed.Reason, "downed.reason");
            context.AssertEqual(40f, state.DownedHealth, "downed.health");
            context.AssertEqual(20f, state.BleedoutRemaining, "downed.bleedoutSeconds");
            context.AssertEqual(0.35f, state.MoveSpeedMultiplier, "downed.moveSpeedMultiplier");
            context.Assert(!state.CanAttack && !state.CanBuild, "Downed participant must not attack or build.");

            context.Assert(
                state.TryBeginRevive(new TotemParticipantId(2), out _),
                "A valid teammate must be able to begin revive.");
            context.Assert(
                state.ContinueRevive(2f, TotemReviveContinuationStatus.Valid, out _),
                "Revive must continue before three seconds.");
            context.Assert(
                !state.ContinueRevive(0f, TotemReviveContinuationStatus.OutOfRange, out TotemDownedStateContract cancelled),
                "Out-of-range revive must cancel.");
            context.AssertEqual(TotemDownedTransitionReason.ReviveCancelledOutOfRange, cancelled.Reason, "revive.cancelReason");
            context.AssertEqual(0f, state.ReviveProgress, "revive.progressAfterCancel");

            state.TryBeginRevive(new TotemParticipantId(2), out _);
            context.Assert(
                state.ContinueRevive(3f, TotemReviveContinuationStatus.Valid, out TotemDownedStateContract revived),
                "Three uninterrupted seconds must complete revive.");
            context.AssertEqual(TotemDownedTransitionReason.ReviveCompleted, revived.Reason, "revive.completeReason");
            context.Assert(Mathf.Abs(state.DownedHealth - 30f) < 0.001f, "Revive must restore 30% max health.");
            context.Detail("revive.health", state.DownedHealth.ToString("F3"));
            context.AssertEqual(1f, state.ProtectionRemaining, "revive.protectionSeconds");

            TotemActorModel rangeReviver = CreateActor(1, 0, Vector3.zero);
            TotemActorModel rangeTarget = CreateActor(2, 0, new Vector3(3f, 0f, 0f));
            context.Assert(
                TotemFirstPlayableLifecycleService.IsWithinReviveRange(rangeReviver, rangeTarget),
                "The existing three-meter interaction boundary must allow revive at its edge.");
            rangeTarget.Position = new Vector3(3.001f, 0f, 0f);
            context.Assert(
                !TotemFirstPlayableLifecycleService.IsWithinReviveRange(rangeReviver, rangeTarget),
                "Revive must be rejected outside the three-meter interaction boundary.");
            context.Detail("revive.interactRadius", TotemFirstPlayableLifecycleService.ReviveInteractRadius.ToString("F3"));

            var opponentExecution = new TotemFirstPlayableParticipantLifeState(100f);
            var hunter = new TotemCombatantReference(TotemCombatantDomain.Participant, 1001);
            var shooter = new TotemCombatantReference(TotemCombatantDomain.Participant, 1002);
            opponentExecution.TryEnterDowned(true, hunter, out _);
            opponentExecution.ApplyDownedDamage(40f, shooter, out _, out TotemDownedStateContract opponentEliminated);
            context.AssertEqual(TotemCombatantDomain.Participant, opponentEliminated.Instigator.Domain, "execution.sourceDomain");
            context.AssertEqual(1002, opponentEliminated.Instigator.CombatantId, "execution.sourceCombatantId");

            var boundary = new TotemFirstPlayableParticipantLifeState(100f);
            boundary.TryEnterDowned(true, new TotemParticipantId(5), out _);
            context.Assert(
                boundary.EliminateAtBuildBoundary(out TotemDownedStateContract eliminated),
                "Build boundary must eliminate a still-downed participant.");
            context.AssertEqual(TotemDownedTransitionReason.BuildBoundary, eliminated.Reason, "boundary.reason");
            context.AssertEqual(
                TotemSpectatorState.SpectatingTeammate,
                boundary.ResolveSpectatorState(true),
                "spectator.withLivingTeammate");
            context.AssertEqual(
                TotemSpectatorState.WaitingForResult,
                boundary.ResolveSpectatorState(false),
                "spectator.withoutLivingTeammate");

            var teamWipe = new TotemFirstPlayableParticipantLifeState(100f);
            teamWipe.TryEnterDowned(true, hunter, out _);
            context.Assert(
                teamWipe.EliminateForTeamWipe(shooter, out TotemDownedStateContract teamEliminated),
                "A downed participant must be eliminated when no active teammate remains.");
            context.AssertEqual(TotemDownedTransitionReason.TeamEliminated, teamEliminated.Reason, "teamWipe.reason");
            context.AssertEqual(
                TotemSpectatorState.WaitingForResult,
                teamWipe.ResolveSpectatorState(false),
                "teamWipe.spectatorState");

            context.Pass("Downed pool, bleedout, revive cancellation/completion, build-boundary elimination and spectator routing are deterministic.");
        }

        private static TotemActorModel CreateActor(int participantId, int teamId, Vector3 position)
        {
            return new TotemActorModel(new TotemActorSpawnInfo
            {
                ActorId = participantId,
                TeamId = teamId,
                Name = "P" + participantId,
                Kind = TotemActorKind.Player,
                ControllerKind = TotemParticipantControllerKind.Human,
                Position = position,
                MaxHealth = 100f,
            });
        }
    }
}
#endif
