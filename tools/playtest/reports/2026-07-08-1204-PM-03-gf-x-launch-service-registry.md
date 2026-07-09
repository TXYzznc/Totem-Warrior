# PM-03 GF_X Launch Service Registry Smoke

- Time: 2026-07-08 12:04 Asia/Shanghai
- Scope: `openspec/changes/gf-x-business-runtime-refactor`
- Scene: `Assets/Game/Scene/Launch.unity`
- Unity: `2022.3.62f3c1`

## Steps

1. Confirmed editor was not playing and not compiling.
2. Opened `Assets/Game/Scene/Launch.unity` through `Game Framework/GameTools/Open Launch Scene`.
3. Cleared Console.
4. Entered Play Mode through `Edit/Play`.
5. Waited until `isPlaying=true` and `isCompiling=false`.
6. Ran `Game Framework/GameTools/Diagnostics/Run All`.
7. Checked Play Mode diagnostic report and Console errors.
8. Exited Play Mode through `Edit/Play`.
9. Rechecked Console after exiting Play Mode.

## Evidence

- Play Mode diagnostic report: `GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_120448.json`
- Play Mode diagnostic result: `success=8`, `failure=0`, `warning=0`
- `StartupChainDiagnosticScenario` now verifies the actual Launch path reaches `TotemGameProcedure`, starts `TotemGameRuntime`, and has all 26 default runtime services registered and ready.
- Console while playing: `rawErrorCount=0`, `filteredProjectErrorCount=0`
- Console after exiting Play Mode: `rawErrorCount=1`, `filteredProjectErrorCount=0`

## Result

PASS.

Notes: The one raw error after exiting Play Mode is the known Unity Editor UIElements `UIRStylePainter.DrawTextInfo` destroyed-material noise. It has no project script frame and is classified as editor transient exit noise.
