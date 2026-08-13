# Legacy effect coverage matrix

> Purpose: this file answers whether the old `LegacyProjectArchive/Assets/Scripts/Modules/*`
> effects have been rewritten into the active GF_X runtime. It is an audit artifact, not a
> compatibility layer. Old `GameApp`, `ModuleRunner`, `EventBus`, `UIModule` and
> `DataTableModule` remain outside the active runtime.

## Coverage states

- `Covered`: runtime behavior exists in GF_X services and is guarded by automated diagnostics.
- `Covered with boundary`: first-round behavior exists and is tested, but later production tuning
  or visual polish remains intentionally outside this pass. This state is not
  weaker evidence for the implemented GF_X contract; it is a scoped acceptance
  note that points to the explicit boundary class below.
- `Evidence only`: old implementation is archived and indexed, but the behavior is not an active
  first-round runtime requirement yet.

These states describe legacy-effect rows. The first-round closure decision is
recorded in `COMPLETION_AUDIT.md`; the boundaries below are accepted later
product work, not hidden blockers.

## Matrix

| Legacy module | Old effect to preserve | GF_X owner | Automated evidence | State | Remaining boundary |
|---|---|---|---|---|---|
| Audio | BGM/SFX routing for menu, combat, hits, deaths and Boss phases | `TotemAudioService` | `[EditMode] Scenario/BusinessRuntime/Totem Audio Runtime`; `[EditMode] Scenario/BusinessRuntime/Totem Gameplay Catalog` | Covered with boundary | Final audio asset mix and volume tuning remain later polish. |
| Bot | 20 Smart AI, 29 Light AI, build presets, target choice, self tattoo, loot, ResourceAcquisition map-resource pickup and merchant shop-purchase behavior, EnemySpawn-anchored placement groups, Aggressive/Conservative chase contrast, Boss/player priorities | `TotemAIService`, `TotemGameplayCatalog`, `TotemActorService`, `TotemWeaponService`, `TotemNpcService` | `[EditMode] Scenario/BusinessRuntime/Totem AI Runtime`; `[EditMode] Scenario/BusinessRuntime/Totem First Round Contract`; `[EditMode] Scenario/BusinessRuntime/Totem Runtime Causality Smoke`; `[EditMode] Scenario/BusinessRuntime/Totem Gameplay Runtime` | Covered with boundary | First-round playable behavior remains in scope; only detailed personality polish remains later balancing. |
| Camera | 2.5D follow, clamp, shake and runtime cleanup | `TotemCameraService` | `[EditMode] Scenario/BusinessRuntime/Totem Gameplay Runtime`; `[EditMode] Scenario/BusinessRuntime/Totem VFX Runtime` | Covered with boundary | Manual scene feel checks still required for framing. |
| Combat | Player input intent, attacks, skills, target selection, damage edge guards, death repeat suppression, run result | `TotemCombatService`, `TotemInputService`, `TotemSkillService`, `TotemWeaponService`, `TotemActorService` | `[PlayMode] Test/Totem CombatHUD Input Smoke`; `[EditMode] Scenario/BusinessRuntime/Totem Gameplay Runtime`; `[EditMode] Scenario/BusinessRuntime/Totem Extended Gameplay`; `[EditMode] Scenario/BusinessRuntime/Totem AI Runtime`; `[EditMode] Scenario/BusinessRuntime/Totem Runtime Causality Smoke` | Covered with boundary | The automated playable-loop proxy and PM-05 PlayMode input smoke are covered; fine hit-pause, animation aesthetics and full UI visual playtest are later polish. |
| DataTable | Old JSON business tables and generated table workflow | `TotemDataService`, `AIGameDataTableGenerator`, Business AI DataTables | `[EditMode] AI DataTable json`; `[EditMode] Scenario/BusinessRuntime/Totem Gameplay Catalog`; `[EditMode] Scenario/BusinessRuntime/Totem Runtime Catalog Binding` | Covered | No remaining first-round boundary; runtime catalog is generated from Business AI DataTables and old DataTableModule is not mounted. |
| Economy | Coins, ink, inventory, death chest loot, player/AI shop payment/refund, self-tattoo penalties | `TotemEconomyService`, `TotemNpcService`, `TotemTattooService`, `TotemAIService` | `[EditMode] Scenario/BusinessRuntime/Totem Extended Gameplay`; `[EditMode] Scenario/BusinessRuntime/Totem AI Runtime`; `[EditMode] Scenario/BusinessRuntime/Totem Runtime Causality Smoke` | Covered with boundary | Final item economy tuning remains later balancing. |
| Enemy | Light/Elite/Boss stats, rewards, Boss phases and skills | `TotemActorService`, `TotemBossService`, `TotemAIService` | `[EditMode] Scenario/BusinessRuntime/Totem First Round Contract`; `[EditMode] Scenario/BusinessRuntime/Totem AI Runtime`; `[EditMode] Scenario/BusinessRuntime/Totem Gameplay Catalog`; `[EditMode] Scenario/BusinessRuntime/Totem Runtime Causality Smoke` | Covered | No remaining first-round boundary; Boss skill ids are table/catalog driven and guarded. |
| Event | Choice/combat/curse/lore/merchant event selection and map event anchor player interaction trigger | `TotemChoiceService`, `TotemInteractionService`, `TotemNpcService`, `TotemMapService`, `TotemUIService` | `[EditMode] Scenario/BusinessRuntime/Totem Choice Runtime`; `[EditMode] Scenario/BusinessRuntime/Totem Gameplay Runtime`; `[EditMode] Scenario/BusinessRuntime/Totem Extended Gameplay`; `[EditMode] Scenario/BusinessRuntime/Totem Gameplay Catalog` | Covered with boundary | Event pacing/content expansion remains design work. |
| Flow | Main menu -> character -> startup -> combat HUD -> run end | `TotemGameFlowService`, `TotemUIService` | `[PlayMode] Scenario/Startup/Launch To Totem Runtime Smoke`; `[PlayMode] Test/Totem CombatHUD Input Smoke`; `[EditMode] Scenario/BusinessRuntime/Totem First Slice UI`; `[EditMode] Scenario/BusinessRuntime/Totem Gameplay Runtime`; `[EditMode] Scenario/BusinessRuntime/Totem Runtime Causality Smoke` | Covered with boundary | Runtime UI visuals can be verified manually. |
| GameState | Current run state and lifecycle cleanup | `TotemGameFlowService`, `TotemCombatService`, service shutdown hooks | `[EditMode] Scenario/BusinessRuntime/Totem Gameplay Runtime`; `[EditMode] Scenario/BusinessRuntime/Totem Runtime Causality Smoke`; `[EditMode] Scenario/Core/Clean Workspace Contract` | Covered with boundary | Save compatibility with old files is intentionally not required. |
| Input | Movement, attack, dodge, interact, skills, pause/tab routed through one input service | `TotemInputService`, `ITotemInputProvider` | `[PlayMode] Test/Totem CombatHUD Input Smoke`; `[EditMode] Scenario/BusinessRuntime/Totem Gameplay Runtime`; `[EditMode] Scenario/BusinessRuntime/Totem First Slice UI`; `[EditMode] Scenario/BusinessRuntime/Totem Runtime Causality Smoke` | Covered | No remaining first-round boundary; new inputs must continue through the provider interface. |
| MapGen | Three themes, functional terrain grid, movement blocking/slowdown, Hazard terrain damage, Cover combat mitigation, seed-stable spawn/chest/NPC/resource/event anchors, EnemySpawn grouped actor placement, room contracts, minimap data and shrink-zone pressure | `TotemMapService`, `TotemActorService`, `TotemZoneService`, `TotemWeaponService`, `TotemChoiceService` | `[EditMode] Scenario/BusinessRuntime/Totem Gameplay Runtime`; `[EditMode] Scenario/BusinessRuntime/Totem First Slice UI`; `[EditMode] Scenario/BusinessRuntime/Totem Gameplay Catalog`; `[EditMode] Scenario/BusinessRuntime/Totem Runtime Catalog Binding` | Covered with boundary | Final map art, mask-derived assets and richer authored prop/resource/event content remain later work. |
| NPC | Tattooist, merchant, interaction focus, shop/enchant/tattoo UI entry, AI merchant purchase route | `TotemNpcService`, `TotemInteractionService`, `TotemUIService`, `TotemAIService` | `[EditMode] Scenario/BusinessRuntime/Totem Extended Gameplay`; `[EditMode] Scenario/BusinessRuntime/Totem First Slice UI`; `[EditMode] Scenario/BusinessRuntime/Totem AI Runtime`; `[EditMode] Scenario/BusinessRuntime/Totem Runtime Causality Smoke`; `[EditMode] Scenario/BusinessRuntime/Totem Runtime Catalog Binding` | Covered with boundary | Final NPC visuals and dialogue content remain later work. |
| Resource | Runtime asset lookup, indexed sprites/prefabs, observable asset cache lifecycle, placeholder/obsolete review state | `TotemAssetService`, AI manifests | `[EditMode] Scenario/BusinessRuntime/Totem Runtime Assets`; `[EditMode] Scenario/BusinessRuntime/GF_X Rewrite Inventory Contract` | Covered with boundary | Many UI sprites are intentionally marked placeholder; final production art replacement remains later work. |
| Save | New run stats persistence and meta progress; no old save compatibility | `TotemRunStatsService`, `TotemMetaProgressService` | `[EditMode] Scenario/BusinessRuntime/Totem Extended Gameplay`; `[EditMode] Scenario/BusinessRuntime/Totem Meta Progress` | Covered with boundary | Old save migration is explicitly out of scope. |
| Scene | Launch scene and GF_X Procedure/runtime startup | `Assets/Game/Scene/Launch.unity`, `TotemGameRuntime`, GF_X startup | `[PlayMode] Scenario/Startup/Launch To Totem Runtime Smoke`; `[EditMode] Scenario/Migration/Migration Path Contract` | Covered with boundary | Additional production scenes are future content/design expansion. |
| Settings | Runtime settings model, edit/preview/rollback/commit lifecycle, idle operation no-op guards and persistence expectation | `TotemSettingsService`, `TotemUIService` | `[EditMode] Scenario/BusinessRuntime/Totem First Slice UI`; `[EditMode] Scenario/BusinessRuntime/Totem Extended Gameplay` | Covered with boundary | More settings categories remain product polish. |
| Skill | Two-slot runtime skill use, cooldown/charges/hold models, AI/Boss skill routing | `TotemSkillService`, `TotemCombatService`, `TotemAIService`, `TotemBossService` | `[EditMode] Scenario/BusinessRuntime/Totem Gameplay Catalog`; `[EditMode] Scenario/BusinessRuntime/Totem AI Runtime`; `[EditMode] Scenario/BusinessRuntime/Totem Extended Gameplay`; `[EditMode] Scenario/BusinessRuntime/Totem Runtime Causality Smoke`; `[EditMode] Scenario/BusinessRuntime/Totem Runtime Catalog Binding` | Covered with boundary | Final skill balance and VFX timing remain later work. |
| Spawner | 50-actor roster, Boss, EnemySpawn-anchored Smart/Light AI groups, pickups, map-resource pickups consumed by ResourceAcquisition Smart AI, chests, NPC placement through map anchors, merchant anchors consumed by AI shop behavior | `TotemActorService`, `TotemChestService`, `TotemWeaponService`, `TotemNpcService`, `TotemAIService` | `[EditMode] Scenario/BusinessRuntime/Totem First Round Contract`; `[EditMode] Scenario/BusinessRuntime/Totem Gameplay Runtime`; `[EditMode] Scenario/BusinessRuntime/Totem AI Runtime`; `[EditMode] Scenario/BusinessRuntime/Totem Runtime Causality Smoke` | Covered with boundary | Hand-authored spawn layout tuning remains later work. |
| Status | Burn/Poison/Shock/Stun/Slow, status chance routing, merge/refresh edge cases and invalid input guards | `TotemStatusService`, `TotemTattooService`, `TotemWeaponService` | `[EditMode] Scenario/BusinessRuntime/Totem Extended Gameplay`; `[EditMode] Scenario/BusinessRuntime/Totem AI Runtime` | Covered with boundary | Final status values remain future tuning/content. |
| Tattoo | 336 combinations, part/element/shape effects, invalid equip guards, clear/reset behavior, enchant affixes, self tattoo, cancellation penalties | `TotemTattooService`, `TotemEconomyService`, `TotemNpcService` | `[EditMode] Scenario/BusinessRuntime/Totem Extended Gameplay`; `[EditMode] Scenario/BusinessRuntime/Totem Gameplay Catalog`; `[EditMode] Scenario/BusinessRuntime/Totem Runtime Catalog Binding` | Covered with boundary | Final numbers/visual presentation remain later tuning. |
| UI | 12 old UI prefabs as visual evidence, GF_X UI lifecycle, first-flow screens and HUD data | `TotemUIService`, GF_X UI forms | `[PlayMode] Test/Totem CombatHUD Input Smoke`; `[EditMode] Scenario/BusinessRuntime/Totem First Slice UI`; `[EditMode] Scenario/BusinessRuntime/Totem Gameplay Runtime` | Covered with boundary | Runtime UI visual testing can remain manual per user direction. |
| VFX | Hit feedback, floating damage, projectile trails, vignette/shake, residue cleanup | `TotemVfxService`, `TotemCameraService` | `[EditMode] Scenario/BusinessRuntime/Totem VFX Runtime`; `[EditMode] Scenario/Core/Clean Workspace Contract` | Covered with boundary | Final art quality and animation polish remain later work. |
| Weapon | 5 weapons, default equip, direct upgrades, max-level duplicate conversion, projectiles, ammo, traits including Life Steal healing, pickups, drops, merchant upgrades | `TotemWeaponService`, `TotemCombatService`, `TotemSkillService` | `[EditMode] Scenario/BusinessRuntime/Totem Gameplay Catalog`; `[EditMode] Scenario/BusinessRuntime/Totem Extended Gameplay`; `[EditMode] Scenario/BusinessRuntime/Totem Gameplay Runtime`; `[EditMode] Scenario/BusinessRuntime/Totem Runtime Causality Smoke`; `[EditMode] Scenario/BusinessRuntime/Totem Runtime Catalog Binding` | Covered with boundary | Smoke-loop weapon use is covered; exact DPS balance and final impact polish remain later tuning. |

## Boundary Classification

This section prevents "Covered with boundary" from hiding untracked work. Every
legacy module is mapped to one explicit boundary class.

| Legacy module | Boundary class | Why this is allowed for the current pass |
|---|---|---|
| Audio | Manual polish | Runtime routing and cue causality are tested; final mix, asset choice and volume feel are production polish. |
| Bot | T9f7 production tuning | Smart/Light AI contracts, five personalities, Aggressive/Conservative chase contrast, EnemySpawn-anchored placement groups, ResourceAcquisition map-resource pickup behavior and ResourceAcquisition merchant shop purchase behavior are tested; first numeric tuning pass is recorded in `T9F7_FIRST_TUNING_REPORT.md`; first-round playable behavior remains in scope, while detailed personality nuance remains later polish. |
| Camera | Manual visual judgement | Follow, raw-focus clamp, shake progression and cleanup are tested; final scene framing is visual judgement. |
| Combat | T9f7 production tuning + playable-stable smoke | Targeting, damage, status, skill, cleanup, playable-loop proxy and PM-05 PlayMode input contracts are tested; hit-pause finesse, animation aesthetics and full UI visual playtest are deferred. |
| DataTable | No remaining first-round boundary | Business JSON/xlsx/catalog workflow is active and guarded. |
| Economy | T9f7 production tuning | Transactions and penalties are tested; first chest/shop tuning pass is recorded in `T9F7_FIRST_TUNING_REPORT.md`; final reward pacing remains manual playtest polish. |
| Enemy | No remaining first-round boundary | Enemy/Boss contracts are table/catalog driven and guarded. |
| Event | Future content/design expansion | Three-choice, event option behavior and first map event anchor player interaction trigger are tested; pacing and content expansion are later design work. |
| Flow | Manual UI visual judgement | State/runtime flow is tested; runtime UI visual quality can be manual by user direction. |
| GameState | Explicit out-of-scope | Old save compatibility is not required by user decision. |
| Input | No remaining first-round boundary | Input must keep routing through `TotemInputService` / `ITotemInputProvider`. |
| MapGen | Future content/art expansion | Three themes, functional terrain grid, movement blocking/slowdown, Hazard damage, Cover mitigation, spawn/chest/NPC/resource/event anchors, EnemySpawn grouped actor placement and minimap contracts are tested; final map art, mask-derived assets and richer authored prop/resource/event content are later work. |
| NPC | T9f7 production tuning + future content | Merchant/tattooist interactions are tested; final shop/NPC tuning, visuals and dialogue are deferred. |
| Resource | Placeholder art accepted | Runtime asset lookup and cache hit/miss/reload lifecycle are tested; obsolete folders and placeholder UI art are explicitly marked in the art manifest. |
| Save | Explicit out-of-scope | Old save migration is not required; new save/run-stat state is tested. |
| Scene | Future content/design expansion | GF_X Launch startup is tested; additional production scenes are future scope. |
| Settings | Future product polish | Edit/preview/rollback/commit behavior and idle operation guards are tested; more settings categories are later polish. |
| Skill | T9f7 production tuning | Two-slot skill routing, cooldown/charges and AI/Boss skill contracts are tested; first balance pass is recorded in `T9F7_FIRST_TUNING_REPORT.md`; final VFX timing remains manual judgement. |
| Spawner | T9f7 production tuning | 50 actor/Boss/pickup/chest/NPC spawn contracts, EnemySpawn grouped AI placement, map-resource pickups, ResourceAcquisition pickup/shop consumption and first map-anchor consumers are tested; hand-authored layout tuning is deferred. |
| Status | T9f7 production tuning + future content | Burn/Poison/Shock/Stun/Slow, StatusChance routing, merge/refresh edge cases and invalid input guards are tested; final status values are deferred. |
| Tattoo | T9f7 production tuning + manual visual judgement | 336 combos, effects, invalid equip guards, clear/reset behavior, affixes, self tattoo and penalties are tested; final numbers and presentation are deferred. |
| UI | Manual UI visual judgement | GF_X UI lifecycle and state/data contracts are tested; runtime visual quality can remain manual. |
| VFX | Manual visual judgement | VFX routing and residue cleanup are tested; final art/animation quality is manual production polish. |
| Weapon | T9f7 production tuning + playable-stable smoke | Weapon configs, default equip, direct upgrades, max-level duplicate conversion, projectiles, traits, pickups, merchant upgrades and smoke-loop weapon use are tested; first DPS pass is recorded in `T9F7_FIRST_TUNING_REPORT.md`; exact final feel remains later polish. |

## Accepted later boundaries

- Every boundary class above has either been accepted as intentionally outside
  this pass or promoted to stricter diagnostics. The remaining bullets describe
  later product work, not blockers for the current first-round closure.
- `T9f7 production tuning` means weapon DPS, skill balance, AI tuning,
  tattoo/status/economy/shrink-zone/shop/NPC/three-choice/Boss numbers and
  production behavior tuning. The first table-driven pass was executed after
  user confirmation and is recorded in `T9F7_FIRST_TUNING_REPORT.md`;
  the automated playable-loop proxy and PM-05 PlayMode input smoke are now part
  of first-round evidence, while later manual polish remains a
  separate product judgement boundary.
- Runtime UI visual quality is intentionally not automatically tested; first-flow UI state and data
  contracts are tested.
- Old OpenSpec specs under `openspec/specs/*` can still describe the former `GameApp/EventBus`
  architecture. They are historical evidence unless updated by the active
  `gf-x-business-runtime-refactor` change.
- Old wiki/GDD wording may also mention `Assets/Scripts`, `GameApp`,
  `EventBus` or the old `InputModule`. Treat those names as migration evidence,
  not as the current implementation contract. Current implementation authority
  is GF_X runtime under `Assets/Game/Scripts`, with player input routed through
  `TotemInputService` / `ITotemInputProvider`.
- `TotemGameplayCatalog.BuildDefault()` remains only as an emergency fallback. Diagnostics require
  the real loaded catalog to come from `GameData/AIData/DataTables/Business`.
