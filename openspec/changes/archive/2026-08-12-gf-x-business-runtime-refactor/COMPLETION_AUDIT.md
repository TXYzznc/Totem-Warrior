# GF_X Business Runtime Refactor Completion Audit

Date: 2026-07-10

Purpose: audit the original objective against current evidence. This file
records the closure of the current first-round GF_X-native refactor target, the
evidence that proves it, and the accepted later product-polish boundaries.

## Status Vocabulary

- `Proved`: the named requirement has concrete current evidence in the GF_X
  runtime, active data/workflow, diagnostics, or accepted manual evidence.
- `Covered with boundary`: the old behavior is represented in the GF_X path and
  has evidence for the current first-round contract, but the row also names an
  explicit boundary such as playable-stable smoke, manual visual judgement,
  later tuning, future content, or accepted out-of-scope work. The boundary is
  tracked in `LEGACY_EFFECT_COVERAGE.md`; it must not be treated as hidden
  remaining work.
- `First-round objective closed`: the current pass has proved GF_X-native
  old-function coverage, non-UI automated diagnostics, clean workspace evidence,
  item-by-item audit traceability, stable PlayMode smoke, playable combat-loop
  smoke, and confirmed old-content production in the GF_X data/runtime path.

Read these statuses from narrow to broad: row evidence first, row accepted
boundary second, current-pass closure last.

## Requirement Audit

| Requirement | Current status | Authoritative evidence |
|---|---|---|
| Analyze the old `Assets/Scripts` behavior before rewriting | Proved for confirmed first-round scope | `REQUIREMENTS_INVENTORY.md`, `DECISIONS_NEEDED.md`, `LEGACY_EFFECT_COVERAGE.md`, `BusinessRewriteInventoryDiagnosticScenario` |
| Keep old runtime out of active startup/compile path | Proved | `Assets/Scripts` absent, old code archived under `LegacyProjectArchive/Assets/Scripts`, `oldRuntimeHostHits=0` |
| Use GF_X as the active startup/runtime framework | Proved | `Assets/Game/Scene/Launch.unity`, `StartupChainDiagnosticScenario`, PM-04 PlayMode report |
| Main menu -> character select -> startup select -> combat HUD | Proved as state/runtime flow and PlayMode CombatHUD input smoke; visual quality remains manual | PM-02 UI flow report, `TotemFirstSliceUIDiagnosticScenario`, PM-04 launch smoke, PM-05 CombatHUD input smoke |
| Recreate first-round scope: 50 Participants (1 Human, 20 SmartBot, 29 LightBot), independent Enemy/Boss population, 336 tattoo combinations, shop/NPC/three-choice/zone | Proved by diagnostics for system contracts | `TotemFirstRoundContractDiagnosticScenario`, `TotemEnemyDomainRuntimeDiagnosticScenario`, `TotemGameplayCatalogDiagnosticScenario`, `TotemAIRuntimeDiagnosticScenario` |
| Convert old business config workflow into AI-friendly data flow | Proved | 31 Business AI JSON files, 31 Business xlsx files, current JSON/xlsx sync check, `totem_gameplay_catalog.json` generated from Business tables, deterministic source fingerprint, AI DataTable checks |
| Support AI JSON -> xlsx reverse workflow | Proved | `AIGameDataTableGenerator`, reverse Business DataTables reports, JSON/xlsx sync freshness diagnostics, `DATATABLE_MIGRATION_MANIFEST.md` |
| Update framework/workflow/skills entry points | Proved for current local project entry points | `.claude/CLAUDE.md`, `AGENTS.md`, `.claude/agents/client-unity.md`, `.claude/agents/gd-system.md`, synced `.codex/agents/*.toml`, `tools/sync-agents.py --check` |
| Use GF_X dependency sources for UniTask and DOTween | Proved | `Assets/Plugins/UniTask`, `Assets/Plugins/DOTween`, `Assets/Resources/DOTweenSettings.asset`, `DependencySourceDiagnosticScenario`, `GF_X_TOOL_MIGRATION_MANIFEST.md` |
| Reuse valid art assets but rewrite loading/lifecycle | Proved for indexed first-round assets, observable asset-cache lifecycle and combat visual fallback counters; final art production deferred | `totem_runtime_assets.json`, `art_assets.json` runtime usage reverse links and UI form reverse links, `UIFormConfig`, `TotemAssetService`, runtime asset diagnostics |
| Link design/art/program/QA work by feature | Proved for the current first-round feature set | `feature_slices.json`, `PROJECT_MAP.md`, `ACTIVE_CONTEXT.md`, `BusinessRewriteInventoryDiagnosticScenario` |
| Remove runtime/test pollution from old framework | Proved for active runtime | `Assets/Scripts`, old tests, old scenes, old editor tools, old ToolHub, migration-era playtest reports/screenshots, template tutorial assets, obsolete art folders and UI sidecar docs/logs archived outside active runtime/report paths; `CleanWorkspaceDiagnosticScenario`; missing script scan `totalFound=0` |
| Non-UI effects must be automatically tested and iterated | Proved for current first-round contracts, including independent NPC enemy/Boss runtime, catalog freshness fingerprint, PCG map generation, runtime causality, five Smart AI personalities, JSON/xlsx sync, asset lifecycle and workspace cleanliness | `GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260713_222743.json`, Business data checks, OpenSpec strict validation |
| UI visual tests do not need to be automated | Accepted boundary | User instruction; UI state/data contracts are still covered by diagnostics |

## Latest Verification Snapshot

- EditMode diagnostics: `GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260713_222743.json`, `success=37`, `failure=0`, `warning=0`. It covers runtime, catalog, JSON/xlsx, PCG, independent NPC enemy/Boss, resource, feature-slice and workspace-cleanliness contracts.
- Playable combat loop smoke: `TotemRuntimeCausalityDiagnosticScenario` now records `playableLoop.*` evidence in the same report: CombatHUD entry, movement distance `5.00`, dodge distance `4.50`, last dodge action `Dodge`, kill delta `1`, Smart AI decision delta `11`, Smart AI attack delta `1`, player pressure health `74.0 -> 56.4`, final player health `34.4`, alive enemies `49`, shrink-zone radius `195.7`, elapsed time `5.8s`, and actor cleanup count `0`.
- T9f7 first production tuning pass: `T9F7_FIRST_TUNING_REPORT.md`; reverse Business DataTables report `GameData/AIData/Reports/reverse-data-tables-json-to-excel_20260713_215900.json` reports `success=31`, `failure=0`, `warning=0`.
- PlayMode launch smoke: `tools/playtest/reports/2026-07-08-0404-PM-04-gf-x-launch-after-prefab-cleanup.md`, `success=8`, `failure=0`, `warning=0`.
- PlayMode CombatHUD input smoke: `Tools/Playtest/Smoke/CombatHUD Input` is compiled and menu-registered; EditMode execution returns the expected guarded warning `Action=SimulatorUnavailable Reason=NotPlaying`. `TotemCombatHudInputSmokeTests.CombatHud_InputSmoke_UsesTotemInputService` is discovered by Unity Test Runner as PlayMode `Runnable` and passed as PM-05 through a temporary UnitySkills `test_run_by_name` allowlist. Archived result `tools/playtest/test-results/2026-07-09-1225-PM-05-combathud-input-playmode.xml` records `total=1`, `passed=1`, `failed=0`, `result=Passed`, duration `6.670873s`.
- AI DataTables: current source inventory is `businessJsonCount=31` and `businessXlsxCount=31`; the three native Enemy-domain additions are `EnemyAbilityConfig`, `EncounterSpawnConfig` and `EnemyLootConfig`.
- Business DataTable bridge mapping: all 28 archived legacy tables still map one-to-one to Business AI JSON/xlsx evidence, while the three native Enemy-domain tables extend the current Business set to 31. The legacy field-level bridge remains `schemaBridgeTableCount=28`, `schemaBridgeValidTableCount=28`, `missingLegacyFieldTableCount=0`; tracked GF_X extensions are `BotConfig` personality weights, `BossPhaseConfig.AbilityIds`, and Enemy runtime/behavior/spawn fields.
- Runtime catalog source: `GameData/AIData/DataTables/Business`; `dataService.catalog.usingFallback=False`.
- Runtime catalog freshness: `totem_gameplay_catalog.json` records `generation.sourceFileCount=31` and `generation.sourceContentHash=35de5e26cb80451c33a99ba26c5f97a7050843fdf0a37c8dcb780f24a1aa40d9`; it contains 15 Enemy definitions, 25 Enemy abilities, 9 encounter rows, 37 Enemy loot rows and 9 Boss phase rows.
- Native Boss evidence: three Boss definitions are owned by `TotemEnemyService`; each Boss has its own three-row `BossPhaseConfig` group. Boss death publishes Enemy death causality and creates public Coin/Paint/Recipe pickups through `TotemEnemyLootService`; it is not an Actor and does not end the run while multiple Participants remain alive.
- Audio cue causality: latest Audio Runtime diagnostics verify `bgm_main_menu` on main menu, `bgm_in_game` on combat entry, Boss phase BGM cues `bgm_boss_phase1/2/3` with observed phase cue ids, melee hit SFX with `Damage.PlayerAttack`, kill SFX with `Killed.PlayerAttack`, player death SFX with `PlayerDied.BossAttack`, visible missing cue reporting for `sfx_missing_diagnostic`, and visible interval-skip reporting for duplicate `sfx_dodge`.
- Settings edit lifecycle evidence: latest Extended Gameplay diagnostics verify idle `Preview`, `Commit` and `Rollback` are ignored without entering edit mode, do not trigger settings-change events, and do not write the settings file; the normal `BeginEdit -> Preview -> Rollback/Commit` flow still clamps preview values, rolls back to defaults, persists committed values, and keeps audio runtime settings sync working through an explicit edit preview.
- Actor damage edge evidence: latest Extended Gameplay diagnostics verify zero and negative damage leave health/events/sequences unchanged, excess damage clamps target health to 0 while emitting one applied/resolved/killed chain, and repeat damage after death does not emit another damage or death trigger.
- Map runtime contract: Business `MapTemplateConfig`, generated catalog, runtime fallback, validator baselines, diagnostics and AI DataTable manifest descriptions agree on current themes `AI_RUINS`, `ALIEN_HIVE` and `VIRUS_SWAMP`, fixed `400m` map size and `40m` placeholder room footprint. Latest diagnostics report `catalog.mapTemplates.count=3`, runtime map/camera size `400`, theme terrain pools `101/102/103`, runtime map missing asset counts `0`, shrink-zone `radiusAt0=200`, `radiusAt180=65`, `radiusAt540=35`, and `radiusAt900=5`.
- Functional terrain layer: `TotemMapSnapshot` now carries a deterministic `100x100` grid at `4m` per cell. Latest Gameplay Runtime diagnostics verify every theme has `Ground`, `Slow`, `Blocked`, `Cover` and `Hazard` cells; out-of-bounds query returns `Blocked`; slow multiplier is `0.65`; an `8m` requested actor move on Slow travels `5.200001m`; a blocked actor move travels `0m`; Hazard terrain reduces player health from `100` to `99.2` through `TerrainHazard` damage; and Cover reduces a source-based `10` damage hit to `6` while leaving environment/status damage paths separate.
- Camera runtime causality: latest Gameplay Runtime diagnostics verify the 2.5D camera binds or creates a main camera, follows in CombatHud, uses 9 orthographic size and 55 degree tilt, clamps raw focus `(1,2)` to `(10,10)` and raw focus `(999,998)` to `(390,390)` on the fixed 400m map, follows an in-bounds `(80,70)` target without clamping, accepts a `0.2 / 0.5s` shake request, produces non-zero shake offset during the shake, then clears remaining time and returns to base position after the duration.
- Map and encounter placement: map generation keeps seed-stable Participant, NPC, chest, resource and event anchors; EnemySpawn anchors now feed deterministic encounter plans. Only `TotemEnemyService` instantiates Light/Elite/Boss enemies, while SmartBot and LightBot remain Participant roster members.
- Smart AI personality behavior: latest AI Runtime diagnostics verify the five-personality distribution and behavior evidence. Aggressive/Rush chases a visible humanoid target at `16m` and closes distance to `15.5078m`, while Conservative/Camp records `Wander / TargetOutsideChasePreference` and keeps distance `16m`. BossPriority records `Chase / BossPriority`, targets the active Boss, moves `12 -> 11.5147m`, and leaves a nearby pending death chest untouched. PlayerPriority records `Chase / PlayerPriorityTarget`, targets a closer LightAi instead of forcing the real player, and moves `6 -> 5.5527m`.
- ResourceAcquisition Smart AI behavior: latest AI Runtime diagnostics verify map-resource pickup chase/claim plus merchant shop purchase. The shop check records `Shop / ChaseMerchant` at `4.5 -> 4.061096m`, NPC `merchant_general`, then `Shop / PurchaseShopOffer`, reward type `WeaponUpgrade`, stock left `0`, `ShopPurchases=1`, snapshot `totalShopPurchases=1`, and snapshot item id `9000`.
- Weapon runtime evidence: latest Extended Gameplay diagnostics verify the default equipped weapon is `knife_basic` at level 1, direct `knife_basic` upgrades move level 1 -> 2 -> 3 with zero converted gold, max-level duplicate upgrade converts a 50 gold cost into 25 gold while keeping level 3, and the equipped knife/pistol fire paths still report damage `18`/`16`. Gameplay Catalog diagnostics also guard `trait_lifesteal` as active `Quick` behavior with ratio `0.08` and cap `12`; Extended Gameplay diagnostics verify actual Life Steal healing `8` HP and resulting player health `68`.
- Status runtime evidence: latest Extended Gameplay diagnostics verify the old five-status contract in GF_X form: Burn snapshot/tick damage, Poison tick damage and expiration, Shock max-DPS/max-duration refresh, lower-DPS Shock refresh preserving DPS while extending duration, invalid status input no-op guards, Slow movement reduction without tick damage, Stun action/movement blocking, StatusChance routing, and weapon trait routing for both Burn and Poison.
- Tattoo runtime edge evidence: latest Extended Gameplay diagnostics verify invalid tattoo equip rejects without changing equipped/effect-log state, `DodgePressedEvent` creates a visible pending trigger, and `Clear()` resets equipped tattoos, effect logs, pending triggers, active enchant affixes and actor-scoped tattoo states.
- Runtime catalog binding: `TotemRuntimeCatalogBindingDiagnosticScenario` confirms runtime services bind to the external Business catalog counts for map templates, weapons, projectiles, weapon traits, drops, skills, tattoos, reading times, enchant affixes/recipes, zone phases, Boss phases, choices, events, chest rewards, audio cues and NPC definitions.
- Runtime slice workflow evidence: Rewrite Inventory directly verifies 31 Business AI JSON and 31 synchronized xlsx files; `GAMEPLAY_RUNTIME_SLICE.md` remains the workflow description rather than the authoritative count source.
- Workspace cleanliness: `runtimeResidue.count=0`, `prefabMissingScripts.componentCount=0`, UnitySkills prefab missing-script scan `totalFound=0`; `Assets/Scenes`, `Assets/Editor`, `Assets/Tools`, `Assets/TutorialInfo`, `Assets/Screenshots`, `Assets/TestResults`, `Assets/Readme.asset`, confirmed obsolete `Assets/Resources/{Character,Characters,Environments,Recipes,Tattoo}` folders, and UI sprite sidecar files are absent.
- TestRunner and generated project-file cleanliness: old generated root files `Tattoo.Tests.csproj`, `Tattoo.PlayMode.Tests.csproj`, `WeaponUpgrade.Tests.csproj`, stale `Game.csproj`, `Game.Editor.csproj`, `Tattoo.csproj` and `GameDesinger.sln` were removed after they were found to reference archived `Assets\\Tests` or `Assets\\Scripts` paths. Fresh Unity Test Runner discovery reports PlayMode count `1` with only `TotemCombatHudInputSmokeTests.CombatHud_InputSmoke_UsesTotemInputService`; active `.sln/.csproj` files no longer grep old `Combat.Tests`, `Tattoo.Tests`, `WeaponUpgrade.Tests`, `Assets\\Tests` or `Assets\\Scripts` references. UnitySkills `Library/UnitySkills/batch_state.json` can still rewrite old historical report/filter strings from the already-running UnitySkills process, but the current discovery list is clean.
- Runtime residue cleanup: `totem_cleanup_runtime_residuals` is available for edit-mode cleanup without generic menu execution; direct verification reports `removedCount=0` and `remainingCount=0`, while latest diagnostics report `lifecycle.scene.damageFloats=0` and timeline `RuntimeResidueCleanup` after scenarios with `removed=0`.
- Playtest-report cleanliness: migration-era old-runtime reports, manual logs and screenshot folders are archived under `LegacyProjectArchive/tools/playtest/reports/old-runtime`; Rewrite Inventory diagnostics report `activePlaytestReports.directoryCount=0`, `activePlaytestReports.filesChecked=6` and `archivedLegacyPlaytestReports.fileCount=19`, and active `tools/playtest/reports` has no old `GameApp` / `UIModule` / `EventBus` report signatures.
- Resource index: 1766 art/runtime assets are indexed with `usage_status`, `usage_status_reason`, `usage_guidance`, `runtime_usages` and `ui_form_usages`; `Unclassified=0` and `classification_needed=0`. The current index records 252 runtime-bound assets, 12 UI-form-bound assets, 38 runtime catalog entries and no missing active runtime/UI asset paths.
- Runtime asset lifecycle: latest Runtime Assets diagnostics verify asset-cache observability in `TotemAssetService`: initial cache counters are `0`, repeated `weapon.knife_basic` sprite loads record `1` miss and `1` hit, repeated `map.floor.ruins` texture loads bring totals to `2/2`, repeated `actor.player.1` prefab instantiation brings totals to `3/3` with `cachedAssetCount=3`, and `ReloadRuntimeAssetCatalog` clears cached count/hits/misses back to `0`.
- Gameplay visual fallback observability: latest Gameplay Runtime diagnostics verify the combat lifecycle uses indexed visuals for `51` actors with `0` primitive actor fallbacks, `3` map-resource weapon pickups with `0` primitive fallback, and `4` weapon pickups after a `hammer_heavy` spawn with `0` primitive fallback.
- VFX sprite-load observability: latest Gameplay Runtime diagnostics report `effect.attack.hit` requested once with `0` missing sprite loads in the combat lifecycle; VFX Runtime diagnostics report projectile sprite request/missing `1/0`, empty projectile missing key, and a standalone no-asset-service request failing visibly with missing key `effect.attack.hit`.
- Projectile-specific VFX routing: `bullet_pistol` resolves to `effect.projectile.bullet_pistol` and `arrow_bow` resolves to `effect.projectile.arrow_bow`; latest Runtime Assets diagnostics load both sprites, and VFX Runtime diagnostics spawn both projectile trails with `spriteMissingCount=0`.
- Feature slice index: 13 first-round feature slices link design/art/program/QA handoffs; they cover all 24 archived legacy modules, all 31 Business tables, all 31 default runtime services, 25 runtime asset keys and 16 diagnostic scenarios. `TotemBossService` is no longer a feature owner; Enemy world, runtime and loot ownership are indexed independently.
- Diagnostic triage index: 16 GF_X diagnostic scenarios reverse-link to feature slices with complete triage records and registered scenario references `16/16`.
- Runtime guardrails: `oldRuntimeHostHits=0`, `directInputBypassHits=0`, `runtimeTodoHits=0`.
- Dependency guardrails: `package.com.cysharp.unitask=False`, `package.com.demigiant.dotween=False`, `forbidden.Assets/Demigiant=False`, `forbidden.OutPackages/UniTask=False`, `outpackages.unitySkills.exists=True`, `dotween.fileCount=20`, `unitask.fileCount=163`, `unitask.version=2.5.10`.
- External package guardrails: active `OutPackages` contains Unity Skills only; the old upstream UniTask clone is archived under `LegacyProjectArchive/OutPackages/UniTask` and must not return to `OutPackages/UniTask`.
- OpenSpec strict validation: passing.
- AI manifest freshness: passing.
- Console after latest EditMode diagnostics: project diagnostics remain `errors=0`; raw Unity Console may still contain known `ExternalAudioDeviceNoise` FMOD output-device errors when there is no project script frame and `totem_diagnostics_run_all` is green.
- Workflow/agent freshness: `.claude` source and `.codex/agents` mirrors are synced; stale `110/124 skill` counts, old `InputModule` input rule, and catalog-only DataTable wording are absent from active entry points. `.claude/CLAUDE.md` now points to the 113 local project skill index.
- Active change encoding: Rewrite Inventory diagnostics report `activeChangeEncoding.checkedDocumentCount=8`; current `gf-x-business-runtime-refactor` core docs no longer contain the guarded mojibake signatures from the earlier Windows encoding pollution.
- Requirement/decision freshness: `REQUIREMENTS_INVENTORY.md` and `DECISIONS_NEEDED.md` record current confirmed scope, archived old-code evidence, Business DataTable workflow, first UI slice policy, current testing acceptance and implemented Smart AI five-personality state. Rewrite Inventory diagnostics report `decisionEntryCount=11`, `legacyEffectCoverage.moduleCount=24`, `archivedLegacyDataTableCount=28`, `activeUIPrefabCount=12`, `archivedLegacyScriptCount=174`, `archivedLegacyModuleDirectoryCount=24`, `archivedLegacyModuleCardCount=24`, `archivedLegacyModuleSourceDirectoryCount=24`, `archivedLegacyTestCount=11`, `archivedLegacyEditorToolCount=4`.
- Active GF_X UI prefab inventory: Rewrite Inventory diagnostics require the exact 12-name set under `Assets/Game/Prefabs/UI`: `CharacterSelect`, `CombatHUD`, `MainMenu`, `PauseMenu`, `RunResult`, `SelfTattoo`, `Settings`, `Shop`, `StartupSelect`, `TattooEnchant`, `TattooStudio`, `ThreeChoice`.
- Boundary classification and evidence precision: `LEGACY_EFFECT_COVERAGE.md` maps all 24 legacy modules into explicit boundary classes and names actual report evidence with `[EditMode]` / `[PlayMode]` source tags; diagnostics report `legacyEffectCoverage.classifiedBoundaryModuleCount=24`, `legacyEffectCoverage.matrixStateRowsChecked=24`, `legacyEffectCoverage.evidenceRowsChecked=24`, `legacyEffectCoverage.evidenceItemsChecked=80`, and `legacyEffectCoverage.editModeScenarioEvidenceItemsChecked=73`, so `Covered` rows cannot still carry hidden remaining work and evidence cells cannot reference unknown or unregistered EditMode scenario report names.
- PlayMode evidence guard: Rewrite Inventory diagnostics require the retained PM-04 markdown report and PM-05 CombatHUD input smoke markdown/XML pair. Historical PM-04 service counts are not reused as the current 31-service contract; current service coverage comes from runtime registration and feature-slice evidence.

## Accepted Later Boundaries

These are not hidden failures; they are explicit boundaries from user decisions
or product-polish scope:

- 2026-07-09 target clarification: the first-round closure gate required old
  first-round functionality to be GF_X-native, non-UI diagnostics to be closed,
  the workspace to remain clean for continued development, audit/docs evidence
  to be traceable item by item, PlayMode smoke to be stable, combat feel to
  reach a playable-stable baseline, and old confirmed content to be produced in
  the new data/runtime path. Current evidence proves that gate; art, animation
  and UI visual refinement continue after this baseline.
- T9f7 production tuning first pass is complete for table-driven weapon, skill,
  AI pressure, enemy HP, shrink-zone, Boss, chest and shop numbers. The
  automated playable-loop proxy and PM-05 PlayMode CombatHUD input smoke are
  now covered; later manual feel judgement for final hit-pause, animation,
  VFX/audio timing and encounter pacing is accepted product polish beyond this
  pass.
- Runtime UI visual quality remains a manual/visual judgement. Automated checks
  cover state, data binding, prefab contracts and first-flow behavior, not final
  screenshots.
- Old save compatibility is intentionally not required; new save/run-stat state
  is covered instead.
- Placeholder UI art under `Assets/Resources/Sprite/UI` is accepted temporarily
  and marked in `art_assets.json`.
- Obsolete art folders confirmed by the user remain excluded from new production
  runtime content.
- Every remaining `Covered with boundary` item is classified as `T9f7`
  production tuning, manual UI/visual judgement, future content/design
  expansion, explicit out-of-scope work, or accepted placeholder art.

## Current Decision

The current first-round GF_X-native business/runtime refactor target is closed.
The final audit re-checked confirmed old-content production in GF_X data/runtime,
workspace cleanliness, item-by-item evidence traceability, non-UI automated
diagnostics, PM-05 PlayMode CombatHUD smoke, and playable combat-loop smoke
against the latest state. The accepted later work is final art, animation, UI
visual refinement, old save compatibility, and fine-grained balance polish
beyond the first playable baseline.
