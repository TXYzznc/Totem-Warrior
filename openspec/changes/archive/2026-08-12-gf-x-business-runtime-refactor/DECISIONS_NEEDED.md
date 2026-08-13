# Decisions before GF_X native gameplay rewrite

> The user explicitly asked to ask immediately when requirements or functionality are uncertain. This file records confirmed decisions and remaining blockers. Implementation must not silently decide unresolved items.

## Confirmed by user on 2026-07-07

- Requirement authority order:
  1. Current user instruction in the thread, with follow-up questions whenever unclear.
  2. Existing old runtime behavior and old code effects.
  3. Archived OpenSpec records, as evidence of previous development process.
  4. GDD v2.1 only as the oldest reference; it contains outdated content.
- First slice:
  - `Launch -> MainMenu -> CharacterSelect -> StartupSelect -> CombatHUD`
  - Then continue with map/player/input/camera/combat.
- Old asset policy:
  - Reuse `Assets/Resources/Prefab/UI`, character, weapon, VFX and related art assets.
  - Rewrite loading and lifecycle through GF_X.
  - Some assets are obsolete or duplicated, so an asset index must track path, inferred purpose and review state.
- Feature equivalence target for first major rewrite:
  - 50 actors.
  - 20 smart AI.
  - 29 light AI.
  - 336 tattoo combinations.
  - Shop/NPC/three-choice/shrinking zone/Boss.
- Old code isolation:
  - Old code should be cleaned out of the new runtime workspace immediately.
  - It must be separated into a standalone folder to avoid polluting the GF_X project.
  - Confirmed archive location: `LegacyProjectArchive/Assets/Scripts`, outside active `Assets`.
- GF_X tool migration:
  - GF_X tools shown by the user, including `AB`, `CompressImageTool`, GF_X `Docs`, `GameData`, `Packages`, `Tools` and related generated project assets, must be migrated or merged as appropriate.
  - Confirmed scope: migrate only `AB` / `CompressImageTool` / GF_X `Docs` / `GameData` / `Tools` / `Packages`; GF_X `Docs` are normalized into `项目知识库（AI自行维护）/wiki`, not kept as a root `Docs` directory.
  - Confirmed exclusion: do not migrate generated `.csproj` / `.sln`.
  - Existing target files must be merged, not blindly overwritten.

## Confirmed by user on 2026-07-08

- Self-tattoo manual cancellation is not free. It keeps an approximately 10% coin deposit based on the total coin cost.
- `AfterDodge` should be a one-shot buff consumed by the next hit after a dodge.
- The 28 old business tables should move to the GF_X xlsx/DataTable workflow. During development, JSON can remain the AI-editable intermediate source, then the existing JSON reverse-export tool can update/create xlsx tables before final DataTable export.
- Asset policy refinement:
  - `Assets/Resources/Character`, `Assets/Resources/Characters`, `Assets/Resources/Environments`, `Assets/Resources/Recipes` and `Assets/Resources/Tattoo` are obsolete and should not be treated as reusable production assets.
  - `Assets/Resources/Sprite/UI` mostly needs regeneration later, but can remain as temporary placeholder art in this phase.
  - Other art folders may be reused normally.
  - The art asset index should use explicit obsolete/placeholder review markers for these folders.
- Save compatibility with the old project is not required. New GF_X save data can start with a clean format.
- `StatusChance` should increase existing element/weapon status application probability. It is not a standalone fixed status effect.
- Smart AI personalities should be simplified to five core archetypes before expanding to 20 concrete Smart AI builds: aggressive, conservative, resource acquisition, Boss priority and player priority. Player priority targets both the real player and other humanoid AI objects. Behaviors such as reading/tattoo-hunting and shop usage are downstream behavior preferences, not personality names.
- Combat feel and final numbers are not in the current phase; leave them for later tuning.

## D1: Requirement Source Priority

Status: confirmed.

Use the confirmed order above.

## D2: First Playable Slice

Status: confirmed.

Implement the UI entry slice first: `Launch -> MainMenu -> CharacterSelect -> StartupSelect -> CombatHUD`.

## D3: Old Assets Reuse Policy

Status: confirmed with review requirement, obsolete-folder exclusions and explicit review-state markers.

Reuse old assets visually where valid, but rewrite loading/lifecycle through GF_X. Track all art assets in `项目知识库（AI自行维护）/wiki/manifests/art_assets.json`; `needs_review=true` means the user or later implementation pass must confirm whether the asset is obsolete, duplicated, or reserved.

Do not treat these folders as reusable production assets: `Character`, `Characters`, `Environments`, `Recipes`, `Tattoo`. Treat `Assets/Resources/Sprite/UI` as temporary placeholder art for this phase. The asset manifest should mark these states explicitly so AI tooling can avoid accidentally reusing obsolete assets.

## D4: Feature Equivalence Depth

Status: confirmed.

- 50 actors.
- 20 smart AI.
- 29 light AI.
- 336 tattoo combinations
- Merchant/tattooist/shop/enchant
- Three-choice events
- Shrinking zone
- Boss/death chest/recipe rubbing

## D5: Old Code Isolation Timing

Status: confirmed and executed.

Old code is isolated in a standalone folder so it no longer pollutes the new GF_X runtime. Moving it outside `Assets` avoids Unity compilation without renaming old `.cs` files.

Execution: old `Assets/Scripts` was archived to `LegacyProjectArchive/Assets/Scripts`; active `Assets/Scripts` must remain absent.

## D6: DataTable Target Workflow

Status: confirmed.

Target workflow: convert all 28 business tables to the GF_X xlsx/DataTable pipeline. During gameplay rewrite, keep JSON as the AI-editable intermediate source, then use the JSON reverse-export tool to update/create xlsx before final DataTable export.

## D7: UI Implementation Policy

Status: confirmed and executed for the first UI slice.

For existing UI prefabs:

- Old UI prefabs under `Assets/Resources/Prefab/UI` are visual/reference
  resources only.
- Runtime lifecycle is owned by GF_X UI forms and `TotemUIService`; old
  `UIModule`, old `IUIForm`, `GameApp`, `ModuleRunner` and `EventBus` must not
  return as UI owners.
- First slice acceptance is the state/runtime flow:
  `Launch -> MainMenu -> CharacterSelect -> StartupSelect -> CombatHUD`.
- Runtime UI visual quality can remain manual. Automated diagnostics cover
  state, prefab wiring, data binding and non-UI effects.

## D8: Save Compatibility

Status: confirmed.

Old save compatibility is not required. The GF_X rewrite can use a clean new save format.

## D9: Testing Acceptance

Status: confirmed and executed for the current migration pass.

- Non-UI effects must be covered by GF_X diagnostics and/or targeted editor
  diagnostics that can fail on behavior regressions.
- UnitySkills scene validation, prefab missing-script scans, console checks and
  PlayMode smoke reports are used where they provide stronger evidence.
- Runtime visual UI playtest remains manual unless the user asks otherwise.
- A green diagnostic run is only evidence when the scenario actually covers the
  requirement being claimed.

## D10: Tattoo Enchant Conditional Semantics

Status: confirmed and implemented for current GF_X runtime.

Two migrated enchant affix concepts are preserved in data:

- `StatusChance`: confirmed and implemented as an additive bonus to existing element/weapon status application probability. It must not spawn a standalone fixed status by itself.
- `AfterDodge`: confirmed and implemented as a one-shot next-hit bonus consumed by the first valid hit after a dodge, including pending-trigger hits that are created by the dodge and consumed by the following attack.

Current GF_X implementation applies the confirmed stat effects: AttackSpeed, CooldownReduction, RangeBonus, unconditional ElementDamageBonus, DistanceGt8m ElementDamageBonus, AfterDodge ElementDamageBonus, SelfHealOnHit, CritChance, CritDamage and StatusChance. Because migrated runtime data does not yet expose a per-status base chance, the current base status application chance defaults to `1.0`; `StatusChance` is wired into the formula and result diagnostics now, and the base chance should move to DataTable/config when the table schema is expanded.

## D11: AI Behavior Depth

Status: confirmed and implemented for the current first-round runtime.

Design five core Smart AI personality archetypes first:

- Aggressive.
- Conservative.
- Resource acquisition.
- Boss priority.
- Player priority, where "player" includes the real player and other humanoid AI actors.

Confirmed rules:

- The first-round 20 Smart AI do not need an even split. Current design uses 5 Aggressive, 3 Conservative, 4 Resource acquisition, 4 Boss priority and 4 Player priority.
- Player priority does not currently force the real player to outrank humanoid AI when both are visible. A later toggle/weight can add that.
- Boss priority must let Boss targeting override resource targeting once the Boss is active, except for immediate survival.

The first-round 20 Smart AI builds are derived from these five personalities
plus behavior parameters in Business `BotConfig`, the generated gameplay
catalog and `TotemAIService`. Combat feel and final numeric tuning are out of
scope for the current pass.
