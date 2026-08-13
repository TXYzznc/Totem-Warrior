#if UNITY_EDITOR
namespace UGF.EditorTools
{
    public sealed class TotemFirstPlayableTattooBuildDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem First Playable Tattoo Build";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            var config = new TotemFirstPlayableContractConfig();
            var errors = new System.Collections.Generic.List<string>();
            context.Assert(TotemFirstPlayableContractValidator.Validate(config, errors), string.Join(" | ", errors));
            context.AssertEqual(2, config.tattooBuild.patterns.Length, "tattooBuild.configPatternCount");
            context.Assert(!string.IsNullOrWhiteSpace(config.tattooBuild.patterns[0].publicEffectText), "P01 public effect text is required in config.");
            context.Assert(!string.IsNullOrWhiteSpace(config.tattooBuild.patterns[1].publicEffectText), "P02 public effect text is required in config.");

            var state = new TotemFirstPlayableTattooBuildState();
            context.AssertEqual(6, state.CaptureLoadout().Length, "tattooBuild.slotCount");
            context.Assert(TotemFirstPlayableTattooBuildState.IsAvailablePattern(TotemFirstPlayablePatternId.P01), "P01 must be initially available.");
            context.Assert(TotemFirstPlayableTattooBuildState.IsAvailablePattern(TotemFirstPlayablePatternId.P02), "P02 must be initially available.");
            context.Assert(!TotemFirstPlayableTattooBuildState.IsAvailablePattern((TotemFirstPlayablePatternId)3), "First playable must not expose P03+.");

            state.SetPigment(TotemPigmentKind.Fire, 10);
            bool equipped = state.TryEquip(
                TotemMatchPhase.OpeningBuild,
                TotemTattooSlotId.Head,
                TotemFirstPlayablePatternId.P01,
                TotemFirstPlayableElement.Fire,
                out var equipResult);
            context.Assert(equipped, "Opening build must allow tattoo mutation.");
            context.AssertEqual(10, equipResult.SpentAmount, "tattooBuild.equipCost");
            context.AssertEqual(0, state.GetPigment(TotemPigmentKind.Fire), "tattooBuild.fireAfterEquip");

            bool combatMutation = state.TryRemove(
                TotemMatchPhase.Round1Combat,
                TotemTattooSlotId.Head,
                out var combatResult);
            context.Assert(!combatMutation, "Combat must keep the tattoo loadout read-only.");
            context.AssertEqual(TotemTattooMutationCode.NotBuildPhase, combatResult.Code, "tattooBuild.combatMutationCode");

            bool removed = state.TryRemove(TotemMatchPhase.Build2, TotemTattooSlotId.Head, out var removeResult);
            context.Assert(removed, "Later build phases must allow tattoo removal.");
            context.AssertEqual(6, removeResult.RefundedAmount, "tattooBuild.removeRefund");
            context.AssertEqual(6, state.GetPigment(TotemPigmentKind.Fire), "tattooBuild.fireAfterRemove");
            context.Assert(state.ClearForMatchCleanup(), "Match cleanup must clear a non-empty first-playable build.");
            context.AssertEqual(0, state.GetPigment(TotemPigmentKind.Fire), "tattooBuild.fireAfterCleanup");
            context.Detail("tattooBuild.patternIds", "P01,P02");
            context.Detail("tattooBuild.pigments", "Fire,Ice,Lightning");
            context.Pass("Six tattoo slots, P01/P02, three pigment wallets, 10/6 accounting and build-phase locking are active.");
        }
    }
}
#endif
