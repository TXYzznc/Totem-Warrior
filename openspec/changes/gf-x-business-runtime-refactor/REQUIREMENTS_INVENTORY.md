# GF_X native rewrite requirements inventory

> Status: current confirmed inventory as of 2026-07-09. This file records what
> the archived old project implemented, what the GF_X rewrite must preserve, and
> which parts are intentionally left for later production tuning. It is evidence
> for the rewrite, not a compatibility layer.

## Evidence Sources

- User decisions in the migration/refactor thread. These override old code,
  OpenSpec drafts and GDD v2.1 when they conflict.
- Old implementation evidence:
  `LegacyProjectArchive/Assets/Scripts/**`
- Old business table evidence:
  `LegacyProjectArchive/Assets/Resources/DataTable/*.json`
- Old UI visual/prefab evidence:
  `Assets/Resources/Prefab/UI/*.prefab`
- Active GF_X business runtime:
  `Assets/Game/Scripts/**`
- Active GF_X framework/tools/runtime support:
  `Assets/Game/ScriptsBuiltin/**`
- Active startup scene:
  `Assets/Game/Scene/Launch.unity`
- AI-maintained manifests:
  `项目知识库（AI自行维护）/manifests/*.json`
- Current data workflow:
  `GameData/AIData/DataTables/Business/*.json` ->
  `GameData/DataTables/Business/*.xlsx` ->
  `GameData/AIData/GameplayCatalogs/totem_gameplay_catalog.json`

## Confirmed Product Shape

| Area | Old effect evidence | GF_X rewrite obligation | Current decision/status |
|---|---|---|---|
| Session fantasy | 10-15 minute roguelike BR, 1 player + 20 Smart AI + 29 Light AI | Support a 50 actor single-player match shape | First-round contract is required and covered by diagnostics. |
| Core loop | Explore, fight, loot pigment/recipe, self tattoo, survive shrinking zone, finish run | Model run phases and state transitions in GF_X services | UI-first flow is implemented first: MainMenu -> CharacterSelect -> StartupSelect -> CombatHUD, then combat runtime services. |
| Tattoo build | 6 parts x 7 colors x 8 patterns = 336 combos, self tattoo, enchant affixes | Preserve the combination model and table-driven rules | 336-combo contract is first-round scope; final numbers/feel remain `T9f7`. |
| Combat | Movement, attack, dodge, skills, damage, kill, player damage | Route input through `TotemInputService`; rewrite combat without old EventBus/ModuleRunner | Non-UI combat effects and the automated playable-loop proxy are covered by diagnostics; final feel/tuning remains `T9f7` and manual polish. |
| Weapon | 5 weapon configs, traits, pickups, upgrades, projectiles | Preserve config-driven weapon behavior via `TotemWeaponService` and catalog data | First-round behavior and smoke-loop weapon use are covered; DPS/feel tuning remains `T9f7` and manual polish. |
| Skill | Two runtime slots, old skill configs and hit resolver behavior | Preserve two-slot skill use, cooldowns, charges and target resolution | First-round behavior is covered; final balance/VFX timing remains `T9f7`. |
| Map and zone | Three themes, generated layout, zone shrink table | Load/generate map state and publish shrink-zone state through GF_X services | First-round map/zone contracts are covered; final authored content/art remains later work. |
| AI/Bot | Smart/Light bots, build presets, combat behavior | Preserve 20 Smart + 29 Light bots and table-driven profiles | Five Smart personalities are confirmed and implemented; final tuning remains `T9f7`. |
| Economy | Gold, item, chest, merchant stock, shop | Preserve table-driven prices, rewards, inventory and transactions | Covered for first-round; economy tuning remains later balancing. |
| NPC | Merchant and tattooist interactions | Expose interaction flow and GF_X UI entry through `TotemNpcService`/`TotemInteractionService` | Covered for first-round; final visuals/dialogue remain later work. |
| Event | Three-choice events and rewards | Preserve event/option tables and option application/rejection behavior | Covered for first-round; content expansion remains later design work. |
| UI | 12 old UI prefabs: MainMenu, CharacterSelect, StartupSelect, CombatHUD, PauseMenu, RunResult, Settings, Shop, SelfTattoo, TattooEnchant, TattooStudio, ThreeChoice | Use GF_X UI lifecycle; do not restore old UIModule/IUIForm runtime owners | Old prefabs are visual evidence/resources only. Runtime UI state/data contracts are tested; visual quality may be manual. |
| Save/settings | Save data, settings UI/module | Preserve user-facing settings and new run/meta progress state | Old save compatibility is intentionally not required. |
| Audio/VFX | Audio clips, hit sparks, camera shake, vignette, damage floats, effects | Connect GF_X services to feedback and clean temporary objects | Non-UI feedback/residue contracts are covered; final mix/art polish remains later work. |

## Current Business DataTable Inventory

There are 28 old JSON business tables archived under
`LegacyProjectArchive/Assets/Resources/DataTable`. They are mirrored into the
Business AI DataTable bridge and then into the runtime gameplay catalog.

| Table | Rows | Fields | Primary key | Standard `Id:int` | Likely owner |
|---|---:|---:|---|---|---|
| BossPhaseConfig | 3 | 8 | BossId:string | No | Enemy/Boss |
| BotBuildPreset | 7 | 10 | PresetId:int | No | Bot |
| BotConfig | 10 | 13 | BotId:int | No | Bot |
| ChestConfig | 6 | 5 | ChestId:string | No | Economy/Spawner |
| EnemyConfig | 3 | 18 | EnemyId:string | No | Enemy/Spawner |
| EventConfig | 6 | 10 | EventId:string | No | Event |
| ItemConfig | 31 | 9 | ItemId:int | No | Economy |
| MapTemplateConfig | 3 | 9 | Id:int | Yes | MapGen |
| MerchantConfig | 9 | 4 | SlotIndex:int | No | NPC/Economy |
| NPCConfig | 5 | 12 | Id:int | Yes | NPC |
| ProjectileConfig | 2 | 7 | ProjectileId:string | No | Combat/Weapon |
| ResourceConfig | 14 | 4 | Id:int | Yes | Resource/Economy |
| ShopStockConfig | 15 | 9 | Id:int | Yes | Economy/NPC |
| SkillConfig | 8 | 17 | SkillId:string | No | Skill/Combat |
| TattooColorConfig | 7 | 4 | Id:int | Yes | Tattoo |
| TattooElementConfig | 7 | 6 | Id:int | Yes | Tattoo |
| TattooEnchantAffixConfig | 24 | 10 | Id:int | Yes | Tattoo/NPC |
| TattooEnchantRecipeConfig | 3 | 5 | Id:int | Yes | Tattoo/NPC |
| TattooPartConfig | 6 | 7 | Id:int | Yes | Tattoo |
| TattooPatternConfig | 8 | 4 | Id:int | Yes | Tattoo |
| TattooReadingTimeConfig | 6 | 3 | PartId:int | No | Tattoo |
| TattooShapeConfig | 8 | 5 | Id:int | Yes | Tattoo |
| ThreeChoiceOptionConfig | 11 | 11 | OptionId:string | No | Event/UI |
| UIFormConfig | 12 | 5 | Id:int | Yes | UI |
| WeaponConfig | 5 | 18 | WeaponId:string | No | Weapon/Combat |
| WeaponDropConfig | 15 | 6 | DropId:string | No | Weapon/Spawner |
| WeaponTraitConfig | 10 | 6 | TraitId:string | No | Weapon |
| ZoneShrinkConfig | 3 | 7 | Id:int | Yes | MapGen |

## Current Old UI Inventory

Existing prefabs under `Assets/Resources/Prefab/UI`:

- `CharacterSelect.prefab`
- `CombatHUD.prefab`
- `MainMenu.prefab`
- `PauseMenu.prefab`
- `RunResult.prefab`
- `SelfTattoo.prefab`
- `Settings.prefab`
- `Shop.prefab`
- `StartupSelect.prefab`
- `TattooEnchant.prefab`
- `TattooStudio.prefab`
- `ThreeChoice.prefab`

GF_X rewrite rule: these may be reused as visual/reference resources only. The
old `UIModule`, old `IUIForm`, `GameApp`, `ModuleRunner` and `EventBus`
lifecycle must not become the runtime owner.

## Art Production Constraints

- When new art resources are needed, the main development flow may start a
  parallel art subagent to generate them under the established project art
  style. This should not block unrelated runtime/code work.
- Art subagent work should proceed from the confirmed style and concrete asset
  need without repeatedly stopping for opinion checks. The main runtime/code
  flow should continue while art generation, cutout, slicing or import work is
  in progress.
- Character frame-animation generation must be processed as one continuous
  batch per character: pass the character reference image, generate the required
  animation frames, then crop/cut/rename/import that character's frames before
  starting another character.
- Do not mix frames from different characters in the same processing batch or
  output folder. This prevents cross-character frame pollution.
- Frame-animation baseline precision: each action has four directions, each
  direction has four frames. Depending on required fidelity, use one canvas per
  animation or two canvases per animation, then perform unified cutout, slicing
  and naming.
- Art generation and processing should update the art asset index/runtime asset
  mapping after import so design, art, code and QA can see the asset path,
  purpose and lifecycle owner.

## Non-Negotiable Engineering Constraints

- Default launch scene remains `Assets/Game/Scene/Launch.unity`.
- New business code enters from GF_X Procedure/runtime.
- Old `GameApp`, `ModuleRunner`, `EventBus`, old `UIModule`, and old
  `DataTableModule` must not be mounted as runtime hosts.
- All gameplay input must go through `TotemInputService` /
  `ITotemInputProvider`.
- No GC allocation in Update/LateUpdate paths.
- Runtime UI visual tests do not need automatic execution; non-UI verification
  must be run and recorded.
- `T9f7` production tuning was explicitly started by the user on 2026-07-08 and
  the first table-driven pass is complete. The automated playable-loop proxy is
  covered by diagnostics; final combat feel and presentation still require
  manual playtest judgement after animation/VFX/audio timing and real encounter
  layouts are worked on.
