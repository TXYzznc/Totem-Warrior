#if UNITY_EDITOR
namespace UGF.EditorTools
{
    public sealed class TotemFirstPlayableElementStateDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem First Playable Element State";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            var state = new TotemFirstPlayableElementState();
            state.Apply(TotemFirstPlayableElement.Fire, new TotemParticipantId(1), 1, 10f);
            state.Apply(TotemFirstPlayableElement.Fire, new TotemParticipantId(2), 2, 10f);
            state.Apply(TotemFirstPlayableElement.Fire, new TotemParticipantId(3), 3, 10f);
            context.AssertEqual(TotemElementTier.Strong, state.Tier, "elementState.strongAfterThreeApplications");
            context.AssertEqual(0, state.AdvanceDecay(3f, gameplaySuspended: true), "elementState.suspendedDecayCount");
            context.AssertEqual(TotemElementTier.Strong, state.Tier, "elementState.suspendedTier");
            context.AssertEqual(1, state.AdvanceDecay(3f, gameplaySuspended: false), "elementState.decayCount");
            context.AssertEqual(TotemElementTier.Standard, state.Tier, "elementState.tierAfterDecay");

            TotemElementApplyResult reaction = state.Apply(
                TotemFirstPlayableElement.Ice,
                new TotemParticipantId(6),
                4,
                10f);
            context.AssertEqual(TotemReactionKind.HeatShock, reaction.Reaction, "elementState.reaction");
            context.AssertEqual(6f, reaction.Attribution.IndirectElementDamage, "elementState.indirectDamage");
            context.AssertEqual(new TotemParticipantId(6), reaction.Attribution.KillOwner, "elementState.killOwner");
            context.AssertEqual(TotemElementTier.Weak, state.Tier, "elementState.tierAfterReaction");
            var lightning = new TotemFirstPlayableElementState();
            lightning.Apply(TotemFirstPlayableElement.Lightning, new TotemParticipantId(2), 1, 10f);
            context.Assert(lightning.TryBeginLightningDischarge(true), "Lightning must discharge on effective direct damage.");
            context.Assert(!lightning.TryBeginLightningDischarge(true), "Lightning must respect its per-target 0.5 second interval.");
            lightning.Advance(0.5f, gameplaySuspended: false);
            context.Assert(lightning.TryBeginLightningDischarge(true), "Lightning per-target interval must reopen after 0.5 seconds.");

            var fire = new TotemFirstPlayableElementState();
            fire.Apply(TotemFirstPlayableElement.Fire, new TotemParticipantId(3), 1, 10f);
            TotemElementAdvanceResult fireTick = fire.Advance(0.5f, gameplaySuspended: false);
            context.AssertEqual(1, fireTick.FireTickCount, "elementState.fireTickCount");
            context.AssertEqual(1f, fireTick.FireTierMultiplier, "elementState.fireWeakMultiplier");

            var hitchFire = new TotemFirstPlayableElementState();
            hitchFire.Apply(TotemFirstPlayableElement.Fire, new TotemParticipantId(1), 10, 10f);
            hitchFire.Apply(TotemFirstPlayableElement.Fire, new TotemParticipantId(2), 11, 10f);
            hitchFire.Apply(TotemFirstPlayableElement.Fire, new TotemParticipantId(3), 12, 10f);
            TotemElementAdvanceResult hitchAdvance = hitchFire.Advance(6.1f, gameplaySuspended: false);
            context.AssertEqual(12, hitchAdvance.FireTickCount, "elementState.hitchFireTickCount");
            context.AssertEqual(16.5f, hitchAdvance.FireTierMultiplier, "elementState.hitchFireMultiplier");
            context.AssertEqual(2, hitchAdvance.DecayedLayerCount, "elementState.hitchDecayCount");
            context.AssertEqual(TotemElementTier.Weak, hitchFire.Tier, "elementState.hitchTier");

            var stasis = new TotemFirstPlayableElementState();
            stasis.Apply(TotemFirstPlayableElement.Ice, new TotemParticipantId(4), 1, 10f);
            stasis.Apply(TotemFirstPlayableElement.Lightning, new TotemParticipantId(5), 2, 10f);
            context.AssertEqual(80f, stasis.ApplyStasisDirectDamageModifier(100f), "elementState.stasisDirectDamage");
            context.Detail("elementState.decaySeconds", TotemFirstPlayableElementRules.LayerDecaySeconds.ToString("F1"));
            context.Detail("elementState.fireTickSeconds", TotemFirstPlayableElementRules.FireTickSeconds.ToString("F1"));
            context.Detail("elementState.iceSlow", "12%,20%,28%");
            context.Pass("Three-tier elements, FIFO sources, paused decay and terminal order-independent reactions are active.");
        }
    }
}
#endif
