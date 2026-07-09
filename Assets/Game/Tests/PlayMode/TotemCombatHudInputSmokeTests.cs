#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class TotemCombatHudInputSmokeTests
{
    private const string LaunchSceneName = "Launch";
    private static readonly SmokeInputProvider InputProvider = new SmokeInputProvider();

    [UnityTest]
    public IEnumerator CombatHud_InputSmoke_UsesTotemInputService()
    {
        yield return LoadLaunchScene();
        yield return WaitForRuntimeReady();
        var runtime = TotemGameRuntime.Instance;
        Assert.NotNull(runtime, "GF_X Totem runtime should be created by Launch scene.");

        var input = RequireService<TotemInputService>(runtime);
        var ui = RequireService<TotemUIService>(runtime);
        var flow = RequireService<TotemGameFlowService>(runtime);

        input.SetInputProvider(InputProvider);
        InputProvider.ClearAll();

        yield return WaitForExclusiveView(ui, flow, UIViews.MainMenu, TotemGameFlowState.MainMenu);
        ui.OpenCharacterSelect();
        yield return WaitForExclusiveView(ui, flow, UIViews.CharacterSelect, TotemGameFlowState.CharacterSelect);
        flow.SelectCharacter(1);
        ui.OpenStartupSelect();
        yield return WaitForExclusiveView(ui, flow, UIViews.StartupSelect, TotemGameFlowState.StartupSelect);
        flow.ConfirmStartup(1, "knife_basic", new[] { 1 });
        ui.OpenCombatHud();
        yield return WaitForExclusiveView(ui, flow, UIViews.CombatHUD, TotemGameFlowState.CombatHud);

        var uiSnapshot = ui.CaptureSnapshot();
        Assert.AreEqual(TotemGameFlowState.CombatHud, flow.CurrentState, "CombatHUD smoke should enter the GF_X CombatHud flow state.");
        Assert.AreEqual(UIViews.CombatHUD.ToString(), uiSnapshot.lastExclusiveView, "CombatHUD smoke should request the CombatHUD view.");
        Assert.Greater(uiSnapshot.currentFormId, 0, "CombatHUD smoke should open a concrete UI form in the Launch scene.");

        yield return HoldMove(input, KeyCode.D);
        var snapshot = input.Current;
        Assert.Greater(snapshot.move.sqrMagnitude, 0.01f, "Move input must be observed through TotemInputService.");

        yield return PressMouse(input, 0);
        snapshot = input.Current;
        Assert.IsTrue(snapshot.attackPressed, "Attack input must be observed through TotemInputService.");

        yield return PressKey(input, KeyCode.E);
        snapshot = input.Current;
        Assert.IsTrue(snapshot.skillSlotEPressed, "Skill E input must be observed through TotemInputService.");

        yield return PressKey(input, KeyCode.Q);
        snapshot = input.Current;
        Assert.IsTrue(snapshot.skillSlotQPressed, "Skill Q input must be observed through TotemInputService.");

        yield return PressKey(input, KeyCode.Space);
        snapshot = input.Current;
        Assert.IsTrue(snapshot.dodgePressed, "Dodge input must be observed through TotemInputService.");

        yield return PressKey(input, KeyCode.F);
        snapshot = input.Current;
        Assert.IsTrue(snapshot.interactPressed, "Interact input must be observed through TotemInputService.");

        yield return PressKey(input, KeyCode.Tab);
        snapshot = input.Current;
        Assert.IsTrue(snapshot.selfTattooTogglePressed, "Self-tattoo input must be observed through TotemInputService.");

        yield return null;
        uiSnapshot = ui.CaptureSnapshot();
        Assert.AreEqual(UIViews.SelfTattoo.ToString(), uiSnapshot.lastOverlayView, "Tab input should route through TotemUIService to SelfTattoo.");

        yield return PressKey(input, KeyCode.Escape);
        snapshot = input.Current;
        Assert.IsTrue(snapshot.escapePressed, "Escape input must be observed through TotemInputService.");

        yield return null;
        uiSnapshot = ui.CaptureSnapshot();
        Assert.GreaterOrEqual(uiSnapshot.overlayCloseRequestCount, 1, "Escape should close at least one overlay through GF_X UI.");

        yield return PressKey(input, KeyCode.Escape);
        yield return null;
        uiSnapshot = ui.CaptureSnapshot();
        Assert.AreEqual(UIViews.PauseMenu.ToString(), uiSnapshot.lastOverlayView, "Second Escape should open PauseMenu from CombatHud.");

        InputProvider.ClearAll();
        input.SetInputProvider(null);
    }

    private static IEnumerator LoadLaunchScene()
    {
        if (SceneManager.GetActiveScene().name == LaunchSceneName)
        {
            yield break;
        }

        var operation = SceneManager.LoadSceneAsync(LaunchSceneName, LoadSceneMode.Single);
        Assert.NotNull(operation, "Launch scene should be available from build settings.");
        while (!operation.isDone)
        {
            yield return null;
        }
    }

    private static IEnumerator WaitForRuntimeReady()
    {
        const int maxFrames = 240;
        for (int i = 0; i < maxFrames; i++)
        {
            var runtime = TotemGameRuntime.Instance;
            if (runtime != null && runtime.ServicesReady)
            {
                yield break;
            }

            yield return null;
        }

        Assert.Fail("Timed out waiting for TotemGameRuntime.ServicesReady.");
    }

    private static IEnumerator WaitForExclusiveView(TotemUIService ui, TotemGameFlowService flow, UIViews view, TotemGameFlowState state)
    {
        const int maxFrames = 120;
        for (int i = 0; i < maxFrames; i++)
        {
            var snapshot = ui.CaptureSnapshot();
            if (flow.CurrentState == state &&
                snapshot.lastExclusiveView == view.ToString() &&
                snapshot.currentFormId > 0)
            {
                yield break;
            }

            yield return null;
        }

        Assert.Fail($"Timed out waiting for {view} / {state}.");
    }

    private static T RequireService<T>(TotemGameRuntime runtime) where T : class, ITotemRuntimeService
    {
        var service = runtime.GetService<T>();
        Assert.NotNull(service, typeof(T).Name + " should be registered.");
        Assert.AreEqual(TotemRuntimeServiceState.Ready, service.State, typeof(T).Name + " should be ready.");
        return service;
    }

    private static IEnumerator HoldMove(TotemInputService input, KeyCode keyCode)
    {
        InputProvider.ClearAll();
        InputProvider.Hold(keyCode);
        input.Tick(Time.deltaTime);
        yield return null;
    }

    private static IEnumerator PressKey(TotemInputService input, KeyCode keyCode)
    {
        InputProvider.ClearAll();
        InputProvider.Press(keyCode);
        input.Tick(Time.deltaTime);
        yield return null;
        InputProvider.ClearPressed(clearMouseClick: false);
    }

    private static IEnumerator PressMouse(TotemInputService input, int button)
    {
        InputProvider.ClearAll();
        InputProvider.PressMouse(button);
        input.Tick(Time.deltaTime);
        yield return null;
        InputProvider.ClearPressed(clearMouseClick: true);
    }

    private sealed class SmokeInputProvider : ITotemInputProvider
    {
        private readonly HashSet<KeyCode> heldKeys = new HashSet<KeyCode>();
        private readonly HashSet<KeyCode> downKeys = new HashSet<KeyCode>();
        private readonly bool[] mouseHeld = new bool[3];
        private readonly bool[] mouseDown = new bool[3];

        public float UnscaledTime => Time.unscaledTime;

        public Vector3 MousePosition => new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        public void Press(KeyCode keyCode)
        {
            downKeys.Add(keyCode);
        }

        public void Hold(KeyCode keyCode)
        {
            heldKeys.Add(keyCode);
        }

        public void PressMouse(int button)
        {
            if (button < 0 || button >= mouseHeld.Length)
            {
                return;
            }

            mouseHeld[button] = true;
            mouseDown[button] = true;
        }

        public void ClearPressed(bool clearMouseClick)
        {
            downKeys.Clear();
            for (int i = 0; i < mouseDown.Length; i++)
            {
                mouseDown[i] = false;
                if (clearMouseClick)
                {
                    mouseHeld[i] = false;
                }
            }
        }

        public void ClearAll()
        {
            heldKeys.Clear();
            downKeys.Clear();
            for (int i = 0; i < mouseHeld.Length; i++)
            {
                mouseHeld[i] = false;
                mouseDown[i] = false;
            }
        }

        public bool GetKey(KeyCode keyCode)
        {
            return heldKeys.Contains(keyCode);
        }

        public bool GetKeyDown(KeyCode keyCode)
        {
            return downKeys.Contains(keyCode);
        }

        public bool GetMouseButton(int button)
        {
            return button >= 0 && button < mouseHeld.Length && mouseHeld[button];
        }

        public bool GetMouseButtonDown(int button)
        {
            return button >= 0 && button < mouseDown.Length && mouseDown[button];
        }
    }
}
#endif
