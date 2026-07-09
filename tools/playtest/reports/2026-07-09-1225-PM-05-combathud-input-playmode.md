# PM-05 CombatHUD Input PlayMode Smoke

Date: 2026-07-09 12:25 CST

## Scope

Verify the real GF_X PlayMode path for CombatHUD input:

```text
Launch -> TotemGameRuntime ready -> MainMenu -> CharacterSelect -> StartupSelect -> CombatHUD
```

The smoke injects movement, attack, E/Q skill, dodge, interact, Tab and Escape
through `TotemInputService` / `ITotemInputProvider`.

## Result

- Test: `TotemCombatHudInputSmokeTests.CombatHud_InputSmoke_UsesTotemInputService`
- Mode: PlayMode
- Result: `Passed`
- Total: `1`
- Passed: `1`
- Failed: `0`
- Duration: `6.670873s`
- Start time: `2026-07-09 04:25:03Z`
- End time: `2026-07-09 04:25:10Z`
- Archived XML: `tools/playtest/test-results/2026-07-09-1225-PM-05-combathud-input-playmode.xml`

## Notes

- `test_run_by_name` was temporarily added to UnitySkills allowlist for this run
  and removed after completion; final allowlist count returned to `0`.
- The earlier failing attempt proved the test must wait for the initial MainMenu
  open to settle before driving the true UI/flow transition chain.
