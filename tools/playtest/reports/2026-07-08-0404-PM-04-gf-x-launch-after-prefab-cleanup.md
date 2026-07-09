# PM-04 GF_X Launch After Prefab Cleanup

- Time: 2026-07-08 16:04 Asia/Shanghai
- Scope: `openspec/changes/gf-x-business-runtime-refactor`
- Scene: `Assets/Game/Scene/Launch.unity`
- Unity: `2022.3.62f3c1`

## Steps

1. Confirmed editor was not playing and not compiling.
2. Confirmed active scene was `Assets/Game/Scene/Launch.unity`.
3. Cleared Console.
4. Entered Play Mode through `Edit/Play`.
5. Waited until `isPlaying=true` and `isCompiling=false`.
6. Ran `Game Framework/GameTools/Diagnostics/Run All`.
7. Checked Play Mode diagnostic report and Console errors.
8. Exited Play Mode through `Edit/Play`.
9. Inspected the one raw exit error after classifying it.

## Evidence

- Play Mode diagnostic report: `GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_160421.json`
- Play Mode diagnostic result: `success=8`, `failure=0`, `warning=0`
- `StartupChainDiagnosticScenario` verified the actual Launch path:
  `Launch -> LoadHotfixDll -> HotfixEntry -> Preload -> Workspace -> TotemGame -> RuntimeReady`.
- Runtime evidence: `currentProcedure=TotemGameProcedure`, `runtime.started=True`, `serviceCount=26`, `readyServiceCount=26`, `failedServiceCount=0`, `preloadFailures=0`.
- Console while playing: `rawErrorCount=0`.
- Prefab cleanup evidence before this smoke: UnitySkills `validate_find_missing_scripts searchInPrefabs=true` reported `totalFound=0`.
- Console after Play Mode exit: `rawErrorCount=1`, `filteredProjectErrorCount=0`.

## Result

PASS.

Notes: The one raw error immediately after exiting Play Mode was the known Unity Editor UIElements `UIRStylePainter.DrawTextInfo` destroyed-material noise. It had no project script frame and matched the PM-03 exit-noise signature.
