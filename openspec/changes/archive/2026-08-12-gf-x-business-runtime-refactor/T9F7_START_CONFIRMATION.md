# T9f7 start confirmation

> Status: confirmed and executed for the first table-driven tuning pass.
> The executed tuning evidence is recorded in
> `T9F7_FIRST_TUNING_REPORT.md`.

## Why this exists

The GF_X rewrite has automated evidence for the first-round contracts, but the
remaining boundary is production tuning:

- combat feel
- weapon DPS and attack timing
- skill balance and VFX timing
- tattoo, status and enchant numeric tuning
- Smart/Light AI behavior weights
- shrink-zone pressure
- shop, NPC, economy and three-choice pacing
- Boss timing, phase HP/damage and reward pressure

The user previously said combat feel and final numbers were not in that phase.
On 2026-07-08 the user explicitly approved proceeding with Codex judgement, so
the first table-driven T9f7 tuning pass was executed.

## Decision result

Chosen path:

| Option | Meaning | Implementation effect |
|---|---|---|
| A. Start T9f7 system tuning | Tune config/data and diagnostics for combat, AI, economy, zone and Boss without requiring final UI visual automation. | Completed in the first tuning pass. |
| B. Accept current migration pass as complete with boundaries | Treat current first-round contracts as sufficient and keep final tuning for a later change. | Mark the current migration goal complete only after final audit confirms all other requirements remain proved. |
| C. Narrow T9f7 to one area first | Start only one area such as combat/weapon, AI, economy, zone or Boss. | Add a smaller task sequence and avoid touching unrelated tables. |

Execution report:

- `T9F7_FIRST_TUNING_REPORT.md`
- `GameData/AIData/Reports/reverse-data-tables-json-to-excel_20260708_201420.json`
- `GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_203959.json`

## Recommended first T9f7 order

1. Define measurable tuning targets before changing numbers.
2. Tune combat envelope: player/enemy HP, weapon damage, cooldown, attack range,
   skill cooldown/charges and status base chance.
3. Tune actor pressure: Smart AI personality weights, Light AI pressure, dodge
   tolerance, target preference and looting greed.
4. Tune run pacing: chest rewards, shop prices, self-tattoo deposit, three-choice
   frequency/rewards and shrink-zone timings.
5. Tune Boss: spawn timing, phase HP/damage multipliers, skill pressure, death
   reward and win pacing.

## Data surfaces likely involved

Primary editable source:

- `GameData/AIData/DataTables/Business/WeaponConfig.json`
- `GameData/AIData/DataTables/Business/WeaponTraitConfig.json`
- `GameData/AIData/DataTables/Business/SkillConfig.json`
- `GameData/AIData/DataTables/Business/BotConfig.json`
- `GameData/AIData/DataTables/Business/BotBuildPreset.json`
- `GameData/AIData/DataTables/Business/EnemyConfig.json`
- `GameData/AIData/DataTables/Business/BossPhaseConfig.json`
- `GameData/AIData/DataTables/Business/ZoneShrinkConfig.json`
- `GameData/AIData/DataTables/Business/ChestConfig.json`
- `GameData/AIData/DataTables/Business/ShopStockConfig.json`
- `GameData/AIData/DataTables/Business/MerchantConfig.json`
- `GameData/AIData/DataTables/Business/ThreeChoiceOptionConfig.json`
- `GameData/AIData/DataTables/Business/TattooElementConfig.json`
- `GameData/AIData/DataTables/Business/TattooEnchantAffixConfig.json`

Required generation path after edits:

1. Business AI JSON
2. Business xlsx reverse export
3. `GameData/AIData/GameplayCatalogs/totem_gameplay_catalog.json`
4. GF_X diagnostics

## Required evidence for the executed pass

- Reverse Business DataTables JSON to xlsx succeeded for all 28 tables.
- Runtime gameplay catalog is generated from
  `GameData/AIData/DataTables/Business`.
- `totem_diagnostics_run_all` reports `failure=0` and `warning=0`.
- OpenSpec strict validation passes.
- AI manifest `--check` passes.
- Active runtime scan remains clean: no old `GameApp`, `ModuleRunner`,
  `EventBus`, `UIModule`, old `DataTableModule`, direct input bypass, missing
  scripts, runtime residue or active obsolete resource roots.

## Non-goals unless separately confirmed

- Do not restore old runtime compatibility layers.
- Do not require automated UI visual judgement.
- Do not generate final production art.
- Do not change old save compatibility policy.
- Do not tune by editing generated xlsx/catalog output directly; edit Business
  AI JSON first, then regenerate.
