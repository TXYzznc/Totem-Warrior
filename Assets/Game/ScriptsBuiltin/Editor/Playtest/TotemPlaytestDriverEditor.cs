#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public static class TotemPlaytestDriverEditor
    {
        private static readonly TotemEditorInputProvider Provider = new TotemEditorInputProvider();
        private static int clearPressedAfterFrame = -1;
        private static bool clearMouseClickToo;
        private static CombatHudSmokeState combatHudSmokeState = CombatHudSmokeState.Idle;
        private static int combatHudSmokeNextFrame = -1;
        private static int combatHudSmokeStartFrame = -1;
        private static bool combatHudSmokeCombatHudObserved;
        private static bool combatHudSmokeMoveObserved;
        private static bool combatHudSmokeAttackObserved;
        private static bool combatHudSmokeSkillEObserved;
        private static bool combatHudSmokeSkillQObserved;
        private static bool combatHudSmokeDodgeObserved;
        private static bool combatHudSmokeInteractObserved;
        private static bool combatHudSmokeSelfTattooInputObserved;
        private static bool combatHudSmokeEscapeObserved;
        private static bool combatHudSmokeSelfTattooObserved;
        private static bool combatHudSmokePauseObserved;
        private static bool combatHudSmokeReturnInjected;

        [MenuItem("Tools/Playtest/01 Enable Simulator", false, 1)]
        public static void EnableSimulator()
        {
            if (!TryGetInputService(out var input))
            {
                return;
            }

            Provider.ClearAll();
            input.SetInputProvider(Provider);
            Debug.Log("[Playtest|INFO] Action=EnableSimulator Type=TotemEditorInputProvider");
        }

        [MenuItem("Tools/Playtest/02 Disable Simulator", false, 2)]
        public static void DisableSimulator()
        {
            var runtime = TotemGameRuntime.Instance;
            var input = runtime == null ? null : runtime.GetService<TotemInputService>();
            Provider.ClearAll();
            input?.SetInputProvider(null);
            Debug.Log("[Playtest|INFO] Action=DisableSimulator Type=TotemEditorInputProvider");
        }

        [MenuItem("Tools/Playtest/Press/E (Skill Slot E)", false, 100)]
        public static void PressSkillE()
        {
            PressKey(KeyCode.E);
        }

        [MenuItem("Tools/Playtest/Press/Q (Skill Slot Q)", false, 101)]
        public static void PressSkillQ()
        {
            PressKey(KeyCode.Q);
        }

        [MenuItem("Tools/Playtest/Press/Space (Dodge)", false, 110)]
        public static void PressDodge()
        {
            PressKey(KeyCode.Space);
        }

        [MenuItem("Tools/Playtest/Press/Tab (SelfTattoo)", false, 111)]
        public static void PressSelfTattoo()
        {
            PressKey(KeyCode.Tab);
        }

        [MenuItem("Tools/Playtest/Press/Escape (Pause)", false, 112)]
        public static void PressPause()
        {
            PressKey(KeyCode.Escape);
        }

        [MenuItem("Tools/Playtest/Press/Return (Confirm)", false, 113)]
        public static void PressConfirm()
        {
            PressKey(KeyCode.Return);
        }

        [MenuItem("Tools/Playtest/Press/F (Interact)", false, 114)]
        public static void PressInteract()
        {
            PressKey(KeyCode.F);
        }

        [MenuItem("Tools/Playtest/Press/MouseLeft (Attack)", false, 120)]
        public static void PressMouseLeft()
        {
            PressMouse(0, "MouseLeft");
        }

        [MenuItem("Tools/Playtest/Press/MouseRight", false, 121)]
        public static void PressMouseRight()
        {
            PressMouse(1, "MouseRight");
        }

        [MenuItem("Tools/Playtest/Hold/W (Up)", false, 200)]
        public static void ToggleHoldW()
        {
            ToggleHold(KeyCode.W);
        }

        [MenuItem("Tools/Playtest/Hold/A (Left)", false, 201)]
        public static void ToggleHoldA()
        {
            ToggleHold(KeyCode.A);
        }

        [MenuItem("Tools/Playtest/Hold/S (Down)", false, 202)]
        public static void ToggleHoldS()
        {
            ToggleHold(KeyCode.S);
        }

        [MenuItem("Tools/Playtest/Hold/D (Right)", false, 203)]
        public static void ToggleHoldD()
        {
            ToggleHold(KeyCode.D);
        }

        [MenuItem("Tools/Playtest/Hold/Clear All", false, 220)]
        public static void ClearAll()
        {
            Provider.ClearAll();
            Debug.Log("[Playtest|INFO] Action=ClearAll");
        }

        [MenuItem("Tools/Playtest/Move/Right", false, 300)]
        public static void MoveRight()
        {
            SetMove(KeyCode.D);
        }

        [MenuItem("Tools/Playtest/Move/Left", false, 301)]
        public static void MoveLeft()
        {
            SetMove(KeyCode.A);
        }

        [MenuItem("Tools/Playtest/Move/Up", false, 302)]
        public static void MoveUp()
        {
            SetMove(KeyCode.W);
        }

        [MenuItem("Tools/Playtest/Move/Down", false, 303)]
        public static void MoveDown()
        {
            SetMove(KeyCode.S);
        }

        [MenuItem("Tools/Playtest/Move/Stop", false, 320)]
        public static void MoveStop()
        {
            Provider.Release(KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D);
            Debug.Log("[Playtest|INFO] Action=MoveStop");
        }

        [MenuItem("Tools/Playtest/Smoke/CombatHUD Input", false, 400)]
        public static void RunCombatHudInputSmoke()
        {
            if (!EnsureSimulator())
            {
                return;
            }

            var runtime = TotemGameRuntime.Instance;
            var ui = runtime == null ? null : runtime.GetService<TotemUIService>();
            if (runtime == null || ui == null)
            {
                Debug.LogWarning("[Playtest|WARN] Action=CombatHUDInputSmoke Reason=TotemUIServiceMissing");
                return;
            }

            ResetCombatHudSmokeResults();
            EditorApplication.update -= ClearPressedOnNextFrame;
            clearPressedAfterFrame = -1;
            clearMouseClickToo = false;
            Provider.ClearAll();
            ui.OpenCombatHud();
            combatHudSmokeState = CombatHudSmokeState.WaitCombatHud;
            combatHudSmokeStartFrame = Time.frameCount;
            combatHudSmokeNextFrame = Time.frameCount + 6;
            EditorApplication.update -= RunCombatHudSmokeUpdate;
            EditorApplication.update += RunCombatHudSmokeUpdate;
            Debug.Log("[Playtest|INFO] Action=CombatHUDInputSmoke Phase=Start");
        }

        [MenuItem("Tools/Playtest/Smoke/Cancel CombatHUD Input", false, 401)]
        public static void CancelCombatHudInputSmoke()
        {
            FinishCombatHudSmoke("Cancelled");
        }

        private static void PressKey(KeyCode keyCode)
        {
            if (!EnsureSimulator())
            {
                return;
            }

            Provider.Press(keyCode);
            ScheduleClearPressed(clearMouseClick: false);
            Debug.Log($"[Playtest|INFO] Action=PressKey Key={keyCode}");
        }

        private static void PressMouse(int button, string label)
        {
            if (!EnsureSimulator())
            {
                return;
            }

            Provider.PressMouse(button);
            ScheduleClearPressed(clearMouseClick: true);
            Debug.Log($"[Playtest|INFO] Action=PressMouse Button={label}");
        }

        private static void ToggleHold(KeyCode keyCode)
        {
            if (!EnsureSimulator())
            {
                return;
            }

            bool held = Provider.ToggleHold(keyCode);
            Debug.Log($"[Playtest|INFO] Action=ToggleHold Key={keyCode} Held={held}");
        }

        private static void SetMove(KeyCode keyCode)
        {
            if (!EnsureSimulator())
            {
                return;
            }

            Provider.Release(KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D);
            Provider.Hold(keyCode);
            Debug.Log($"[Playtest|INFO] Action=Move Key={keyCode}");
        }

        private static bool EnsureSimulator()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Playtest|WARN] Action=SimulatorUnavailable Reason=NotPlaying");
                return false;
            }

            if (!TryGetInputService(out var input))
            {
                return false;
            }

            if (!ReferenceEquals(input.InputProvider, Provider))
            {
                input.SetInputProvider(Provider);
                Debug.Log("[Playtest|INFO] Action=EnableSimulator Type=TotemEditorInputProvider");
            }

            return true;
        }

        private static bool TryGetInputService(out TotemInputService input)
        {
            var runtime = TotemGameRuntime.Instance;
            input = runtime == null ? null : runtime.GetService<TotemInputService>();
            if (runtime == null || input == null)
            {
                Debug.LogWarning("[Playtest|WARN] Action=SimulatorUnavailable Reason=TotemInputServiceMissing");
                return false;
            }

            return true;
        }

        private static void RunCombatHudSmokeUpdate()
        {
            CaptureCombatHudSmokeObservations();

            if (!Application.isPlaying)
            {
                FinishCombatHudSmoke("Failed_NotPlaying");
                return;
            }

            if (Time.frameCount < combatHudSmokeNextFrame)
            {
                return;
            }

            switch (combatHudSmokeState)
            {
                case CombatHudSmokeState.WaitCombatHud:
                    if (!combatHudSmokeCombatHudObserved && Time.frameCount - combatHudSmokeStartFrame > 120)
                    {
                        FinishCombatHudSmoke("Failed_CombatHUDMissing");
                        return;
                    }

                    if (combatHudSmokeCombatHudObserved)
                    {
                        StartSmokeMove(KeyCode.D, CombatHudSmokeState.MoveRight);
                    }
                    else
                    {
                        combatHudSmokeNextFrame = Time.frameCount + 6;
                    }

                    break;

                case CombatHudSmokeState.MoveRight:
                    Provider.Release(KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D);
                    StartSmokeMove(KeyCode.W, CombatHudSmokeState.MoveUp);
                    break;

                case CombatHudSmokeState.MoveUp:
                    Provider.Release(KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D);
                    StartSmokeMouse(0, CombatHudSmokeState.Attack);
                    break;

                case CombatHudSmokeState.Attack:
                    ClearSmokePress(clearMouseClick: true);
                    StartSmokeKey(KeyCode.E, CombatHudSmokeState.SkillE);
                    break;

                case CombatHudSmokeState.SkillE:
                    ClearSmokePress(clearMouseClick: false);
                    StartSmokeKey(KeyCode.Q, CombatHudSmokeState.SkillQ);
                    break;

                case CombatHudSmokeState.SkillQ:
                    ClearSmokePress(clearMouseClick: false);
                    StartSmokeKey(KeyCode.Space, CombatHudSmokeState.Dodge);
                    break;

                case CombatHudSmokeState.Dodge:
                    ClearSmokePress(clearMouseClick: false);
                    StartSmokeKey(KeyCode.F, CombatHudSmokeState.Interact);
                    break;

                case CombatHudSmokeState.Interact:
                    ClearSmokePress(clearMouseClick: false);
                    StartSmokeKey(KeyCode.Tab, CombatHudSmokeState.Tab);
                    break;

                case CombatHudSmokeState.Tab:
                    ClearSmokePress(clearMouseClick: false);
                    combatHudSmokeState = CombatHudSmokeState.WaitSelfTattoo;
                    combatHudSmokeNextFrame = Time.frameCount + 12;
                    break;

                case CombatHudSmokeState.WaitSelfTattoo:
                    StartSmokeKey(KeyCode.Escape, CombatHudSmokeState.EscapeCloseOverlay);
                    break;

                case CombatHudSmokeState.EscapeCloseOverlay:
                    ClearSmokePress(clearMouseClick: false);
                    combatHudSmokeState = CombatHudSmokeState.WaitOverlayClosed;
                    combatHudSmokeNextFrame = Time.frameCount + 12;
                    break;

                case CombatHudSmokeState.WaitOverlayClosed:
                    StartSmokeKey(KeyCode.Escape, CombatHudSmokeState.EscapeOpenPause);
                    break;

                case CombatHudSmokeState.EscapeOpenPause:
                    ClearSmokePress(clearMouseClick: false);
                    combatHudSmokeState = CombatHudSmokeState.WaitPause;
                    combatHudSmokeNextFrame = Time.frameCount + 12;
                    break;

                case CombatHudSmokeState.WaitPause:
                    StartSmokeKey(KeyCode.Return, CombatHudSmokeState.Return);
                    combatHudSmokeReturnInjected = true;
                    break;

                case CombatHudSmokeState.Return:
                    ClearSmokePress(clearMouseClick: false);
                    FinishCombatHudSmoke(BuildCombatHudSmokeStatus());
                    break;
            }
        }

        private static void StartSmokeMove(KeyCode keyCode, CombatHudSmokeState nextState)
        {
            Provider.Release(KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D);
            Provider.Hold(keyCode);
            combatHudSmokeState = nextState;
            combatHudSmokeNextFrame = Time.frameCount + 8;
            Debug.Log($"[Playtest|INFO] Action=CombatHUDInputSmoke Phase=Move Key={keyCode}");
        }

        private static void StartSmokeKey(KeyCode keyCode, CombatHudSmokeState nextState)
        {
            Provider.Press(keyCode);
            combatHudSmokeState = nextState;
            combatHudSmokeNextFrame = Time.frameCount + 2;
            Debug.Log($"[Playtest|INFO] Action=CombatHUDInputSmoke Phase=PressKey Key={keyCode}");
        }

        private static void StartSmokeMouse(int button, CombatHudSmokeState nextState)
        {
            Provider.PressMouse(button);
            combatHudSmokeState = nextState;
            combatHudSmokeNextFrame = Time.frameCount + 2;
            Debug.Log($"[Playtest|INFO] Action=CombatHUDInputSmoke Phase=PressMouse Button={button}");
        }

        private static void ClearSmokePress(bool clearMouseClick)
        {
            Provider.ClearPressed(clearMouseClick);
        }

        private static void FinishCombatHudSmoke(string status)
        {
            CaptureCombatHudSmokeObservations();
            Provider.ClearAll();
            combatHudSmokeState = CombatHudSmokeState.Idle;
            combatHudSmokeNextFrame = -1;
            EditorApplication.update -= ClearPressedOnNextFrame;
            EditorApplication.update -= RunCombatHudSmokeUpdate;
            Debug.Log($"[Playtest|INFO] Action=CombatHUDInputSmoke Phase=Finish Status={status}");
        }

        private static string BuildCombatHudSmokeStatus()
        {
            bool passed = combatHudSmokeCombatHudObserved
                && combatHudSmokeMoveObserved
                && combatHudSmokeAttackObserved
                && combatHudSmokeSkillEObserved
                && combatHudSmokeSkillQObserved
                && combatHudSmokeDodgeObserved
                && combatHudSmokeInteractObserved
                && combatHudSmokeSelfTattooInputObserved
                && combatHudSmokeEscapeObserved
                && combatHudSmokeSelfTattooObserved
                && combatHudSmokePauseObserved;
            return $"Passed={passed} CombatHUD={combatHudSmokeCombatHudObserved} Move={combatHudSmokeMoveObserved} Attack={combatHudSmokeAttackObserved} SkillE={combatHudSmokeSkillEObserved} SkillQ={combatHudSmokeSkillQObserved} Dodge={combatHudSmokeDodgeObserved} Interact={combatHudSmokeInteractObserved} TabInput={combatHudSmokeSelfTattooInputObserved} SelfTattoo={combatHudSmokeSelfTattooObserved} EscapeInput={combatHudSmokeEscapeObserved} Pause={combatHudSmokePauseObserved} ReturnInjected={combatHudSmokeReturnInjected} Frames={Time.frameCount - combatHudSmokeStartFrame}";
        }

        private static void CaptureCombatHudSmokeObservations()
        {
            var runtime = TotemGameRuntime.Instance;
            if (runtime == null)
            {
                return;
            }

            var input = runtime.GetService<TotemInputService>();
            if (input != null)
            {
                var snapshot = input.Current;
                combatHudSmokeMoveObserved |= snapshot.move.sqrMagnitude > 0.01f;
                combatHudSmokeAttackObserved |= snapshot.attackPressed;
                combatHudSmokeSkillEObserved |= snapshot.skillSlotEPressed;
                combatHudSmokeSkillQObserved |= snapshot.skillSlotQPressed;
                combatHudSmokeDodgeObserved |= snapshot.dodgePressed;
                combatHudSmokeInteractObserved |= snapshot.interactPressed;
                combatHudSmokeSelfTattooInputObserved |= snapshot.selfTattooTogglePressed;
                combatHudSmokeEscapeObserved |= snapshot.escapePressed;
            }

            var ui = runtime.GetService<TotemUIService>();
            if (ui != null)
            {
                var uiSnapshot = ui.CaptureSnapshot();
                combatHudSmokeCombatHudObserved |= uiSnapshot.lastExclusiveView == UIViews.CombatHUD.ToString()
                    && uiSnapshot.lastExclusiveSucceeded
                    && uiSnapshot.currentFormId > 0;
                combatHudSmokeSelfTattooObserved |= uiSnapshot.lastOverlayView == UIViews.SelfTattoo.ToString()
                    || uiSnapshot.selfTattooFormId > 0;
                combatHudSmokePauseObserved |= uiSnapshot.lastOverlayView == UIViews.PauseMenu.ToString();
            }
        }

        private static void ResetCombatHudSmokeResults()
        {
            combatHudSmokeCombatHudObserved = false;
            combatHudSmokeMoveObserved = false;
            combatHudSmokeAttackObserved = false;
            combatHudSmokeSkillEObserved = false;
            combatHudSmokeSkillQObserved = false;
            combatHudSmokeDodgeObserved = false;
            combatHudSmokeInteractObserved = false;
            combatHudSmokeSelfTattooInputObserved = false;
            combatHudSmokeEscapeObserved = false;
            combatHudSmokeSelfTattooObserved = false;
            combatHudSmokePauseObserved = false;
            combatHudSmokeReturnInjected = false;
        }

        private static void ScheduleClearPressed(bool clearMouseClick)
        {
            clearPressedAfterFrame = Application.isPlaying ? Time.frameCount + 1 : 0;
            clearMouseClickToo |= clearMouseClick;
            EditorApplication.update -= ClearPressedOnNextFrame;
            EditorApplication.update += ClearPressedOnNextFrame;
        }

        private static void ClearPressedOnNextFrame()
        {
            if (Application.isPlaying && Time.frameCount <= clearPressedAfterFrame)
            {
                return;
            }

            Provider.ClearPressed(clearMouseClickToo);
            clearMouseClickToo = false;
            clearPressedAfterFrame = -1;
            EditorApplication.update -= ClearPressedOnNextFrame;
        }

        private enum CombatHudSmokeState
        {
            Idle,
            WaitCombatHud,
            MoveRight,
            MoveUp,
            Attack,
            SkillE,
            SkillQ,
            Dodge,
            Interact,
            Tab,
            WaitSelfTattoo,
            EscapeCloseOverlay,
            WaitOverlayClosed,
            EscapeOpenPause,
            WaitPause,
            Return
        }

        private sealed class TotemEditorInputProvider : ITotemInputProvider
        {
            private readonly HashSet<KeyCode> heldKeys = new HashSet<KeyCode>();
            private readonly HashSet<KeyCode> downKeys = new HashSet<KeyCode>();
            private readonly bool[] mouseHeld = new bool[3];
            private readonly bool[] mouseDown = new bool[3];

            public float UnscaledTime => Time.unscaledTime;

            public Vector3 MousePosition => Input.mousePosition;

            public void Press(KeyCode keyCode)
            {
                downKeys.Add(keyCode);
            }

            public void Hold(KeyCode keyCode)
            {
                heldKeys.Add(keyCode);
            }

            public bool ToggleHold(KeyCode keyCode)
            {
                if (heldKeys.Contains(keyCode))
                {
                    heldKeys.Remove(keyCode);
                    return false;
                }

                heldKeys.Add(keyCode);
                return true;
            }

            public void Release(params KeyCode[] keys)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    heldKeys.Remove(keys[i]);
                    downKeys.Remove(keys[i]);
                }
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
}
#endif
