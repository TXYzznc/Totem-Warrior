# PM-02 GF_X UI Flow: Main Menu To Combat HUD

Date: 2026-07-08 02:41 CST
Unity: 2022.3.62f3c1
Scene: `Assets/Game/Scene/Launch.unity`

## Scope

Verify the first GF_X UI chain in real Play Mode:

`MainMenu -> CharacterSelect -> StartupSelect -> CombatHUD`

This test invokes Unity UI Button `onClick` events through UnitySkills. It is not a static prefab-only check.

## Result

PASS with one classified Unity Editor exit-noise note.

## Evidence

- Play Mode entered successfully: `isPlaying=true`.
- `MainMenu(Clone)` found under `GameFramework/Builtin/UI/UICanvasRoot/UI Group - UIForm`.
- Clicked `StartButton`.
- `CharacterSelect(Clone)` found.
- Runtime-created character cards all appeared:
  - `CharacterCard_1`
  - `CharacterCard_2`
  - `CharacterCard_3`
- Clicked `CharacterCard_1`.
- Clicked `NextButton`.
- `StartupSelect(Clone)` found.
- Startup options appeared:
  - `Color_1`
  - `Weapon_knife_basic`
  - `Pattern_1`
  - `ConfirmButton`
- Clicked `Color_1`, `Weapon_knife_basic`, `Pattern_1`, `ConfirmButton`.
- `CombatHUD(Clone)` found.
- HUD elements found:
  - `HpBar`
  - `WeaponIcon`
  - `SkillSlotE`
- Console while in Play Mode: `Error/Exception=0`.
- Play Mode diagnostics report: `GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_024122.json`
  - `success=8`
  - `failure=0`
  - `warning=0`

## Fixes From This Run

- Replaced Unity object UI binding `??=` usage with explicit `if (x == null)` checks in:
  - `Assets/Game/Scripts/UI/TotemUIFormBase.cs`
  - `Assets/Game/Scripts/UI/TotemCharacterSelectForm.cs`
  - `Assets/Game/Scripts/UI/TotemStartupSelectForm.cs`
  - `Assets/Game/Scripts/UI/TotemCombatHUDForm.cs`
- This fixed the first failed PM-02 attempt where `CharacterRoot` existed in the scene but `CharacterCard_1/2/3` were not created.
- Hardened `StartupChainDiagnosticScenario` so long UI-flow tests do not fail only because early startup trace events fell out of the recent trace window.

## Known Editor Noise

After exiting Play Mode, Unity Editor logs a persistent UIElements-only error:

`MissingReferenceException: The object of type 'Material' has been destroyed... UnityEngine.UIElements.UIR.Implementation.UIRStylePainter.DrawTextInfo`

Isolation notes:

- Reproduces even when entering only `MainMenu` and then exiting Play Mode.
- Full `Editor.log` stack contains UnityEngine/UIElements frames only.
- No project code frame appears in the stack.
- PM-02 business flow and in-Play diagnostics are clean.

Classification for automated playtests:

- Count this exact UIElements material error as Unity Editor transient exit noise.
- Do not count it as a gameplay/runtime failure unless a project frame appears in the stack or it occurs during Play Mode before exit.
