#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UGF.EditorTools
{
    public sealed class TotemChoiceRuntimeDiagnosticScenario : GFDiagnosticScenarioBase
    {
        public override string Name => "Totem Choice Runtime";

        public override string Category => "BusinessRuntime";

        public override void Run(GFDiagnosticScenarioContext context)
        {
            CheckStaticAvailabilityFilters(context);
            CheckRuntimeChoiceState(context);
            context.Pass("Totem choice runtime contract is ready.");
        }

        private static void CheckStaticAvailabilityFilters(GFDiagnosticScenarioContext context)
        {
            var catalog = new[]
            {
                new TotemChoiceOption { OptionId = "unique_recipe", DisplayName = "Unique Recipe", WeightBase = 10, IsUnique = true },
                new TotemChoiceOption { OptionId = "coin_now", DisplayName = "Coin Now", WeightBase = 10 },
                new TotemChoiceOption { OptionId = "heal_now", DisplayName = "Heal Now", WeightBase = 10 },
                new TotemChoiceOption { OptionId = "late_scroll", DisplayName = "Late Scroll", WeightBase = 10, MinRunElapsedSec = 120f },
            };

            var usedUnique = new HashSet<string> { "unique_recipe" };
            var early = TotemChoiceService.BuildThreeChoices("filter_early", 3, catalog, 0f, usedUnique);
            context.Assert(!early.Options.Any(option => option.OptionId == "unique_recipe"), "Used unique choice should be excluded.");
            context.Assert(!early.Options.Any(option => option.OptionId == "late_scroll"), "MinRunElapsedSec choice should be excluded before elapsed time.");

            var lateCatalog = new[]
            {
                catalog[1],
                catalog[2],
                catalog[3],
            };
            var late = TotemChoiceService.BuildThreeChoices("filter_late", 3, lateCatalog, 130f, null);
            context.AssertEqual(3, late.Options.Length, "choice.filter.late.count");
            context.Assert(late.Options.Any(option => option.OptionId == "late_scroll"), "Late choice should become available after elapsed time.");
        }

        private static void CheckRuntimeChoiceState(GFDiagnosticScenarioContext context)
        {
            var runtimeObject = new GameObject("[TotemChoiceDiagnosticRuntime]");
            TotemGameRuntime runtime = null;
            float originalTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 1f;
                runtime = runtimeObject.AddComponent<TotemGameRuntime>();
                RegisterChoiceDiagnosticServices(runtime);
                runtime.StartRuntime();

                var flow = runtime.GetService<TotemGameFlowService>();
                var choice = runtime.GetService<TotemChoiceService>();
                flow.ConfirmStartup(1, "knife_basic", new[] { 1 });

                choice.Tick(130f);
                var shown = choice.RollThreeChoices("event_choice_001", 11);
                context.AssertEqual(TotemChoiceRuntimeState.Showing, shown.State, "choice.runtime.stateAfterRoll");
                AssertNear(context, 20f, shown.TimeoutSec, "choice.runtime.timeout");
                AssertNear(context, 20f, shown.RemainingSec, "choice.runtime.remainingAtStart");
                context.Assert(shown.RunElapsedSec >= 130f, "Choice snapshot should expose run elapsed seconds.");
                AssertNear(context, 0f, Time.timeScale, "choice.runtime.pausedTimeScale");
                context.Assert(TotemThreeChoiceForm.FormatChoiceHeader(shown).Contains("20.0s"), "Choice header should expose remaining seconds while showing.");

                choice.Tick(5f);
                var afterFive = choice.Current;
                AssertNear(context, 15f, afterFive.RemainingSec, "choice.runtime.remainingAfterFive");
                choice.CloseCurrentChoice("Diagnostic.Close");
                context.AssertEqual(TotemChoiceRuntimeState.Closed, choice.Current.State, "choice.runtime.closedState");
                AssertNear(context, 1f, Time.timeScale, "choice.runtime.restoredTimeScaleAfterClose");

                var timeout = choice.RollThreeChoices("event_choice_001", 17);
                choice.Tick(25f);
                context.AssertEqual(TotemChoiceRuntimeState.Timeout, timeout.State, "choice.runtime.timeoutState");
                context.Assert(timeout.TimedOut, "Timeout snapshot should mark TimedOut.");
                context.Assert(!string.IsNullOrWhiteSpace(timeout.SelectedOptionId), "Timeout should select one option.");
                AssertNear(context, 0f, timeout.RemainingSec, "choice.runtime.timeoutRemaining");
                AssertNear(context, 1f, Time.timeScale, "choice.runtime.restoredTimeScaleAfterTimeout");

                var manual = choice.RollThreeChoices("event_choice_001", 19);
                var external = new TotemChoiceOption
                {
                    OptionId = "diagnostic_coin_choice",
                    DisplayName = "Diagnostic Coin",
                    EffectType = TotemChoiceEffectType.CoinReward,
                    ValueInt = 10,
                    WeightBase = 1,
                };
                context.Assert(!choice.ApplyChoice(external), "External choice should be rejected when it is not in the current snapshot.");
                context.AssertEqual(TotemChoiceRuntimeState.Showing, manual.State, "choice.runtime.externalRejectedKeepsShowing");
                AssertNear(context, 0f, Time.timeScale, "choice.runtime.externalRejectedKeepsPause");

                var selected = FindPredictablyApplicableOption(manual);
                for (int seed = 20; selected == null && seed < 100; seed++)
                {
                    manual = choice.RollThreeChoices("event_choice_001", seed);
                    selected = FindPredictablyApplicableOption(manual);
                }

                context.Assert(selected != null, "Choice diagnostic should find an applicable option in the runtime catalog.");
                context.Assert(choice.ApplyChoice(selected), "Manual choice should apply through runtime services.");
                context.AssertEqual(TotemChoiceRuntimeState.Resolved, manual.State, "choice.runtime.resolvedState");
                context.AssertEqual(selected.OptionId, manual.SelectedOptionId, "choice.runtime.selectedOptionId");
                context.Assert(!manual.TimedOut, "Manual choice should not be marked as timed out.");
                AssertNear(context, 1f, Time.timeScale, "choice.runtime.restoredTimeScaleAfterManualApply");
                context.Assert(!choice.ApplyChoice(selected), "Resolved choice should reject repeated application.");
            }
            finally
            {
                Time.timeScale = originalTimeScale;
                if (runtime != null)
                {
                    runtime.ShutdownRuntime();
                }

                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void RegisterChoiceDiagnosticServices(TotemGameRuntime runtime)
        {
            runtime.RegisterService(new TotemGameFlowService());
            runtime.RegisterService(new TotemDataService());
            runtime.RegisterService(new TotemAssetService());
            runtime.RegisterService(new TotemMapService());
            runtime.RegisterService(new TotemActorService());
            runtime.RegisterService(new TotemEconomyService());
            runtime.RegisterService(new TotemStatusService());
            runtime.RegisterService(new TotemTattooService());
            runtime.RegisterService(new TotemWeaponService());
            runtime.RegisterService(new TotemSkillService());
            runtime.RegisterService(new TotemChoiceService());
        }

        private static TotemChoiceOption FindPredictablyApplicableOption(TotemChoiceSnapshot snapshot)
        {
            if (snapshot?.Options == null)
            {
                return null;
            }

            return snapshot.Options.FirstOrDefault(option =>
                option != null &&
                (option.EffectType == TotemChoiceEffectType.CoinReward ||
                 option.EffectType == TotemChoiceEffectType.StatusCleanse ||
                 option.EffectType == TotemChoiceEffectType.SkillAcquire ||
                 option.EffectType == TotemChoiceEffectType.SkillRefresh ||
                 option.EffectType == TotemChoiceEffectType.RecipeUnlock));
        }

        private static void AssertNear(GFDiagnosticScenarioContext context, float expected, float actual, string name)
        {
            context.Detail($"{name}.actual", actual);
            context.Assert(Mathf.Abs(expected - actual) <= 0.001f, $"{name}: expected={expected}, actual={actual}");
        }
    }
}
#endif
