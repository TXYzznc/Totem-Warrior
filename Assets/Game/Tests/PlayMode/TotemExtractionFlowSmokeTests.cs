#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class TotemExtractionFlowSmokeTests
{
    [UnityTest]
    public IEnumerator LocalTeamExtraction_UnlocksThroughInputAndImmediatelyEndsRun()
    {
        yield return TotemPlayModeTestIsolation.LoadFreshLaunchScene();
        TotemGameRuntime runtime = TotemGameRuntime.Instance;

        Assert.NotNull(runtime);
        Assert.That(runtime.ServicesReady, Is.True);
        TotemUIService ui = runtime.GetService<TotemUIService>();
        TotemGameFlowService flow = runtime.GetService<TotemGameFlowService>();
        TotemMatchFlowService match = runtime.GetService<TotemMatchFlowService>();
        TotemActorService actors = runtime.GetService<TotemActorService>();
        TotemInputService input = runtime.GetService<TotemInputService>();
        TotemExtractionService extraction = runtime.GetService<TotemExtractionService>();
        Assert.NotNull(flow);
        Assert.That(ui.StartLocalFirstPlayable(812, true), Is.True);
        yield return WaitForCombatHudAndRoster(ui, flow, actors);

        for (int guard = 0; guard < 16 && match.CurrentPhase != TotemMatchPhase.Round4Combat; guard++)
        {
            match.Advance(Mathf.Max(0.001f, match.ActivityRemaining));
        }

        Assert.That(match.CurrentPhase, Is.EqualTo(TotemMatchPhase.Round4Combat));
        extraction.Configure(new TotemExtractionConfig
        {
            pointCount = 3,
            interactSeconds = 0.1f,
            interactRadius = 4f,
        });

        var provider = new ExtractionSmokeInputProvider();
        input.SetInputProvider(provider);
        provider.Hold(KeyCode.LeftShift);
        provider.Press(KeyCode.Space);
        input.Tick(0f);
        extraction.Tick(0f);
        provider.Clear();

        Assert.That(extraction.IsUnlocked, Is.True);
        Assert.That(extraction.ActivePointCount, Is.EqualTo(3));
        TotemExtractionPoint point = extraction.CaptureActivePoints()[0];
        actors.Player.Position = point.Position;
        actors.Player.GameObject.transform.position = point.Position;
        provider.Hold(KeyCode.F);
        input.Tick(0.1f);
        extraction.Tick(0.1f);

        Assert.That(extraction.IsCompleted, Is.True);
        Assert.That(match.CurrentPhase, Is.EqualTo(TotemMatchPhase.Result));
        Assert.NotNull(ui.ActiveRunResult);
        Assert.That(ui.ActiveRunResult.extracted, Is.True);
        Assert.That(ui.ActiveRunResult.reason, Is.EqualTo("LocalTeamExtracted"));
        Assert.That(extraction.CaptureSnapshot().extractedParticipantIds.Length, Is.EqualTo(2));

        input.SetInputProvider(null);
        runtime.GetService<TotemGameFlowService>()?.EnterMainMenu();
        ui.OpenMainMenu();
        yield return null;
    }

    private static IEnumerator WaitForCombatHudAndRoster(
        TotemUIService ui,
        TotemGameFlowService flow,
        TotemActorService actors)
    {
        for (int i = 0; i < 600; i++)
        {
            TotemUISnapshot snapshot = ui.CaptureSnapshot();
            if (flow.CurrentState == TotemGameFlowState.CombatHud
                && snapshot.lastExclusiveView == UIViews.CombatHUD.ToString()
                && snapshot.currentFormId > 0
                && actors.Player != null
                && actors.Actors.Count == TotemFirstPlayableRules.ParticipantCount)
            {
                yield break;
            }

            yield return null;
        }

        Assert.Fail("Timed out waiting for CombatHUD and the six-participant roster.");
    }

    private sealed class ExtractionSmokeInputProvider : ITotemInputProvider
    {
        private readonly HashSet<KeyCode> held = new HashSet<KeyCode>();
        private readonly HashSet<KeyCode> pressed = new HashSet<KeyCode>();

        public float UnscaledTime => Time.unscaledTime;
        public Vector3 MousePosition => Vector3.zero;
        public void Hold(KeyCode key) => held.Add(key);
        public void Press(KeyCode key) => pressed.Add(key);
        public void Clear()
        {
            held.Clear();
            pressed.Clear();
        }

        public bool GetKey(KeyCode keyCode) => held.Contains(keyCode);
        public bool GetKeyDown(KeyCode keyCode) => pressed.Contains(keyCode);
        public bool GetMouseButton(int button) => false;
        public bool GetMouseButtonDown(int button) => false;
    }
}
#endif
