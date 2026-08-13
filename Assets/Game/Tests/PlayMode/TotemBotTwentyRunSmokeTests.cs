#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class TotemBotTwentyRunSmokeTests
{
    private const int MaxFrames = 600;

    [UnityTest]
    [Timeout(180000)]
    public IEnumerator OneHumanFiveBots_CompleteTwentyFastRunsWithoutInputOrRuntimeResidue()
    {
        yield return TotemPlayModeTestIsolation.LoadFreshLaunchScene();
        TotemGameRuntime runtime = TotemGameRuntime.Instance;
        TotemUIService ui = RequireService<TotemUIService>(runtime);
        TotemGameFlowService flow = RequireService<TotemGameFlowService>(runtime);
        TotemMatchFlowService match = RequireService<TotemMatchFlowService>(runtime);
        TotemActorService actors = RequireService<TotemActorService>(runtime);
        TotemAIService ai = RequireService<TotemAIService>(runtime);
        TotemMapResourceService mapResources = RequireService<TotemMapResourceService>(runtime);

        for (int runIndex = 0; runIndex < 20; runIndex++)
        {
            yield return WaitForExclusiveView(ui, flow, UIViews.MainMenu, TotemGameFlowState.MainMenu);
            Assert.IsTrue(ui.StartLocalFirstPlayable(), $"Run {runIndex}: local first playable must start without input.");
            yield return WaitForExclusiveView(ui, flow, UIViews.CombatHUD, TotemGameFlowState.CombatHud);

            match.Configure(new TotemMatchTimingConfig(), useFastMode: true);
            Assert.AreEqual(TotemFirstPlayableRules.ParticipantCount, actors.Actors.Count, $"Run {runIndex}: roster size");
            Assert.AreEqual(TotemFirstPlayableRules.BotCount, ai.States.Count, $"Run {runIndex}: bot state count");

            int guard = 0;
            while (match.CurrentPhase != TotemMatchPhase.Result && guard++ < 16)
            {
                match.CompleteCurrentActivityForDiagnostics();
                yield return null;
                yield return null;
            }

            Assert.Less(guard, 16, $"Run {runIndex}: phase timeout fallback must reach Result.");
            Assert.AreEqual(TotemMatchPhase.Result, match.CurrentPhase, $"Run {runIndex}: final phase");
            Assert.NotNull(ui.ActiveRunResult, $"Run {runIndex}: result snapshot");
            Assert.Greater(ai.CaptureSnapshot().totalDecisions, 0, $"Run {runIndex}: bots must make autonomous decisions.");

            flow.EnterMainMenu();
            ui.OpenMainMenu();
            yield return WaitForExclusiveView(ui, flow, UIViews.MainMenu, TotemGameFlowState.MainMenu);
            Assert.IsNull(actors.Player, $"Run {runIndex}: participant roster must be cleaned.");
            Assert.AreEqual(0, actors.Actors.Count, $"Run {runIndex}: no participant residue");
            Assert.AreEqual(0, mapResources.ActivePickupCount, $"Run {runIndex}: no map-resource residue");
        }
    }

    [UnityTearDown]
    public IEnumerator CleanupAfterTest()
    {
        TotemGameRuntime runtime = TotemGameRuntime.Instance;
        if (runtime != null && runtime.ServicesReady)
        {
            runtime.GetService<TotemGameFlowService>()?.EnterMainMenu();
            runtime.GetService<TotemUIService>()?.OpenMainMenu();
        }

        yield return null;
    }

    private static IEnumerator WaitForExclusiveView(
        TotemUIService ui,
        TotemGameFlowService flow,
        UIViews view,
        TotemGameFlowState state)
    {
        for (int i = 0; i < MaxFrames; i++)
        {
            TotemUISnapshot snapshot = ui.CaptureSnapshot();
            if (flow.CurrentState == state
                && snapshot.lastExclusiveView == view.ToString()
                && snapshot.currentFormId > 0)
            {
                yield break;
            }

            yield return null;
        }

        Assert.Fail($"Timed out waiting for {view}/{state}.");
    }

    private static T RequireService<T>(TotemGameRuntime runtime) where T : class, ITotemRuntimeService
    {
        T service = runtime?.GetService<T>();
        Assert.NotNull(service, typeof(T).Name + " must be registered.");
        return service;
    }
}
#endif
