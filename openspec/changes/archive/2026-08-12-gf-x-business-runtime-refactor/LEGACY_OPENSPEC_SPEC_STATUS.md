# Legacy OpenSpec Spec Status

Purpose: old `openspec/specs/*` files can still mention the former `GameApp`,
`ModuleRunner`, `EventBus`, `UIModule`, `DataTableModule`, and `Assets/Scripts`
runtime. During the GF_X rewrite they are historical evidence, not the active
runtime contract, unless a later change explicitly rewrites them.

Active rule: GF_X active replacement requirements live in
`openspec/changes/gf-x-business-runtime-refactor/specs/gf-x-business-runtime/spec.md`,
`REQUIREMENTS_INVENTORY.md`, `LEGACY_EFFECT_COVERAGE.md`, and the automated
diagnostics under `Assets/Game/ScriptsBuiltin/Editor/Diagnostics`.

## Status Table

| Spec file | Status | GF_X active replacement |
|---|---|---|
| `openspec/specs/core-ui-screens/spec.md` | Historical evidence | Flow/UI rows in `LEGACY_EFFECT_COVERAGE.md`; `TotemFirstSliceUIDiagnosticScenario`; `TotemGameplayRuntimeDiagnosticScenario` |
| `openspec/specs/main-menu-flow/spec.md` | GF_X-native active spec; old runtime terms appear only as forbidden dependencies | Launch scene + GF_X Procedure flow in `StartupChainDiagnosticScenario`; no old runtime host rule in active change spec |
| `openspec/specs/player-attack-system/spec.md` | Historical evidence | Combat/Weapon/Skill rows in `LEGACY_EFFECT_COVERAGE.md`; `TotemGameplayRuntimeDiagnosticScenario`; `TotemExtendedGameplayDiagnosticScenario` |
| `openspec/specs/settings/spec.md` | Historical evidence | Settings row in `LEGACY_EFFECT_COVERAGE.md`; `TotemExtendedGameplayDiagnosticScenario` settings lifecycle checks |
| `openspec/specs/tattoo/spec.md` | Historical evidence | Tattoo/Status/Economy rows in `LEGACY_EFFECT_COVERAGE.md`; `TotemExtendedGameplayDiagnosticScenario` |
| `openspec/specs/weapon-pickup/spec.md` | Historical evidence | Weapon/Economy/NPC rows in `LEGACY_EFFECT_COVERAGE.md`; `TotemExtendedGameplayDiagnosticScenario`; `TotemGameplayCatalogDiagnosticScenario` |

## Maintenance Rule

If a new or edited `openspec/specs/*/spec.md` contains old runtime terms, add it
to this table or rewrite it into GF_X-native requirements immediately. The active
runtime must continue to use `Assets/Game/Scripts` and GF_X services rather than
the archived old module host.
