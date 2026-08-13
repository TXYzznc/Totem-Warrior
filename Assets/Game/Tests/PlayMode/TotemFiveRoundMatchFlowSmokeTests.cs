#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class TotemFiveRoundMatchFlowSmokeTests
{
    [UnityTest]
    public IEnumerator Launch_CompletesFiveRoundFlowAndOpensResult()
    {
        yield return TotemPlayModeTestIsolation.LoadFreshLaunchScene();
        TotemGameRuntime runtime = TotemGameRuntime.Instance;

        Assert.NotNull(runtime, "Launch must create the runtime.");
        Assert.That(runtime.ServicesReady, Is.True, "Launch runtime services must become ready.");

        var ui = runtime.GetService<TotemUIService>();
        var flow = runtime.GetService<TotemGameFlowService>();
        var matchFlow = runtime.GetService<TotemMatchFlowService>();
        var actor = runtime.GetService<TotemActorService>();
        var map = runtime.GetService<TotemMapService>();
        var readiness = runtime.GetService<TotemParticipantReadinessService>();
        Assert.NotNull(ui);
        Assert.NotNull(flow);
        Assert.NotNull(matchFlow);
        Assert.NotNull(actor);
        Assert.NotNull(map);
        Assert.NotNull(readiness);

        yield return WaitForExclusiveView(ui, flow, UIViews.MainMenu, TotemGameFlowState.MainMenu);
        Assert.That(ui.StartLocalFirstPlayable(), Is.True, "Main menu must start the local first-playable flow.");
        yield return WaitForExclusiveView(ui, flow, UIViews.CombatHUD, TotemGameFlowState.CombatHud);
        Assert.That(SceneManager.GetSceneByName("OasisCity").isLoaded, Is.True, "OasisCity must load before CombatHUD becomes active.");
        Assert.NotNull(map.CurrentMap);
        Assert.That(map.CurrentMap.SourceSceneName, Is.EqualTo("OasisCity"));
        Assert.That(TotemMapService.FindAnchors(map.CurrentMap, TotemMapAnchorKind.PlayerSpawn).Length, Is.EqualTo(20));
        Assert.That(TotemMapService.FindAnchors(map.CurrentMap, TotemMapAnchorKind.Resource).Length, Is.EqualTo(20));
        Assert.That(TotemMapService.FindAnchors(map.CurrentMap, TotemMapAnchorKind.Extraction).Length, Is.EqualTo(7));
        Assert.That(matchFlow.CurrentPhase, Is.EqualTo(TotemMatchPhase.OpeningBuild));
        Assert.That(actor.Actors.Count, Is.EqualTo(TotemFirstPlayableRules.ParticipantCount));
        for (int i = 0; i < actor.Actors.Count; i++)
        {
            Assert.That(actor.Actors[i].GameObject.activeInHierarchy, Is.False, $"Actor {i} must wait for opening build completion.");
        }

        yield return WaitForLocalReadiness(readiness, actor.Player);
        matchFlow.Advance(60f);
        Assert.That(matchFlow.CurrentPhase, Is.EqualTo(TotemMatchPhase.Round1Combat));
        for (int i = 0; i < actor.Actors.Count; i++)
        {
            Assert.That(actor.Actors[i].GameObject.activeInHierarchy, Is.True, $"Actor {i} must spawn when round one starts.");
        }

        for (int i = 0; i < 32 && matchFlow.CurrentPhase != TotemMatchPhase.Result; i++)
        {
            matchFlow.Advance(Mathf.Max(0.001f, matchFlow.ActivityRemaining));
            yield return null;
        }

        Assert.That(matchFlow.CurrentPhase, Is.EqualTo(TotemMatchPhase.Result));
        Assert.NotNull(ui.ActiveRunResult, "Five-round completion must produce a result snapshot.");
        Assert.That(
            ui.ActiveRunResult.reason == "FiveRoundScoreResolved" || ui.ActiveRunResult.reason == "FiveRoundExactTie",
            Is.True,
            ui.ActiveRunResult.reason);

        flow.EnterMainMenu();
        ui.OpenMainMenu();
        yield return WaitForExclusiveView(ui, flow, UIViews.MainMenu, TotemGameFlowState.MainMenu);
        for (int i = 0; i < 240 && SceneManager.GetSceneByName("OasisCity").isLoaded; i++)
        {
            yield return null;
        }
        Assert.That(SceneManager.GetSceneByName("OasisCity").isLoaded, Is.False, "Returning to MainMenu must unload OasisCity.");
        Assert.That(actor.Actors.Count, Is.EqualTo(0), "Returning to main menu must clean the participant roster.");
    }

    private static IEnumerator WaitForExclusiveView(
        TotemUIService ui,
        TotemGameFlowService flow,
        UIViews view,
        TotemGameFlowState state)
    {
        for (int i = 0; i < 600; i++)
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

    private static IEnumerator WaitForLocalReadiness(
        TotemParticipantReadinessService readiness,
        TotemActorModel player)
    {
        for (int i = 0; i < 600; i++)
        {
            if (readiness.GetLifecycle(player) != TotemParticipantLifecycle.Loading)
            {
                yield break;
            }

            yield return null;
        }

        Assert.Fail("Timed out waiting for the local participant readiness handshake.");
    }
}
#endif
