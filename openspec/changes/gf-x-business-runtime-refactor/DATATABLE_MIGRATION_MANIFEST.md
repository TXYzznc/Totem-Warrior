# DataTable migration manifest

> Source evidence: `项目知识库（AI自行维护）/wiki/manifests/datatables.json` and archived old `LegacyProjectArchive/Assets/Resources/DataTable/*.json`.
>
> User direction: old data is requirement evidence and content seed, but runtime loading/lifecycle must be rewritten through GF_X. Do not keep the old `DataTableModule` as the new runtime host.

## Current decision state

- Total old business tables: 28.
- First gameplay target must eventually cover 50 actors, 20 smart AI, 29 light AI, 336 tattoo combinations, shop/NPC/three-choice/shrinking zone/Boss.
- Current runtime workflow: gameplay tables are promoted into AI-editable JSON catalogs first, then
  loaded by GF_X-native services without old `DataTableModule`.
- Confirmed final data-authoring workflow: all 28 old business tables target the GF_X xlsx/DataTable
  pipeline. During active development, JSON remains the AI-editable intermediate, then the reverse
  export tool updates/creates the corresponding xlsx files for planning review and later DataTable export.
- Executed bridge state: 28 `GameData/AIData/DataTables/Business/*.json` manifests and 28
  `GameData/DataTables/Business/*.xlsx` files now exist. The AI DataTable validator reports 33/33
  success (5 Core + 28 Business), the Business-only reverse export reports 28/28 success, and
  GF_X diagnostics now compare the current 28 Business JSON manifests against the current 28 xlsx
  files with `businessJsonExcelSync.changedCellCount=0`.
- Runtime catalog state: `tools/ai_index/build_gameplay_catalog_from_business_tables.py` now builds
  `GameData/AIData/GameplayCatalogs/totem_gameplay_catalog.json` from the 28 Business AI DataTable
  manifests. The generated catalog records `source=GameData/AIData/DataTables/Business`, while
  preserving GF_X runtime supplemental skill rows that do not exist in the old `SkillConfig`. It also
  records deterministic generation metadata (`sourceFileCount=28` and `sourceContentHash`) so GF_X
  diagnostics can fail fast when the runtime catalog is stale relative to the AI-editable Business JSON.
- Old tables with string or non-`Id` business primary keys preserve those keys as normal data columns.
  A numeric GF_X `Id:int` column is added as the DataTable row id so future generated DataTable code
  is not blocked by legacy key formats.

## Migration table

| Table | Owner/System | Rows | Fields | Primary key | Standard `Id:int` | Key risk | GF_X target | Status |
|---|---:|---:|---:|---|---|---|---|---|
| `BossPhaseConfig` | Enemy/Boss | 3 | 8 | `BossId:string` | No | Non-standard primary key; phase key uses composite runtime lookup with `PhaseIndex`. | `totem_gameplay_catalog.json/bossPhases` | Migrated to GF_X-native runtime catalog |
| `BotBuildPreset` | Bot | 7 | 10 | `PresetId:int` | No | Non-standard primary key; contains JSON/vector-like fields. | `totem_gameplay_catalog.json/botBuildPresets` | Migrated to GF_X-native runtime catalog |
| `BotConfig` | Bot | 23 | 21 | `BotId:int` | No | Non-standard primary key; upgraded from old 10-row sample to confirmed 20 Smart + 3 Light profile set. | `totem_gameplay_catalog.json/botProfiles` | Migrated to GF_X-native runtime catalog |
| `ChestConfig` | Economy/Spawner | 6 | 5 | `ChestId:string` | No | Non-standard primary key; grouped probability rows must sum correctly. | `totem_gameplay_catalog.json/chestRewards` | Migrated to GF_X-native runtime catalog |
| `EnemyConfig` | Enemy/Spawner | 3 | 18 | `EnemyId:string` | No | Non-standard primary key; boss/enemy spawn and loot references are preserved as actor body stats and metadata. | `totem_gameplay_catalog.json/enemies` | Migrated to GF_X-native runtime catalog |
| `EventConfig` | Event | 6 | 10 | `EventId:string` | No | Non-standard primary key; trigger condition JSON field needs typed parsing policy. | `totem_gameplay_catalog.json/events` | Migrated to GF_X-native runtime catalog |
| `ItemConfig` | Economy | 31 | 9 | `ItemId:int` | No | Non-standard primary key name; economy item ids must remain stable. | `totem_gameplay_catalog.json/items` | Migrated to GF_X-native runtime catalog |
| `MapTemplateConfig` | MapGen | 3 | 9 | `Id:int` | Yes | Standard key; map theme fields feed later map slice. | `totem_gameplay_catalog.json/mapTemplates` | Migrated to GF_X-native runtime catalog |
| `MerchantConfig` | NPC/Economy | 9 | 4 | `SlotIndex:int` | No | Non-standard primary key; slot grouping and refresh weights matter for shop. | `totem_gameplay_catalog.json/merchantSlots` | Migrated to GF_X-native runtime catalog |
| `NPCConfig` | NPC | 5 | 12 | `Id:int` | Yes | Standard key; NPC role and interaction references need GF_X lifecycle rewrite. | `totem_gameplay_catalog.json/npcs` | Migrated to GF_X-native runtime catalog |
| `ProjectileConfig` | Combat/Weapon | 2 | 7 | `ProjectileId:string` | No | Non-standard primary key; projectile prefab/weapon references need asset index mapping. | `totem_gameplay_catalog.json/projectiles` | Migrated to GF_X-native runtime catalog |
| `ResourceConfig` | Resource/Economy | 14 | 4 | `Id:int` | Yes | Standard key; economy resource ids must remain stable. | `totem_gameplay_catalog.json/resources` + `totem_runtime_assets.json/tattoo.*` | Migrated to GF_X-native runtime catalog |
| `ShopStockConfig` | Economy/NPC | 15 | 9 | `Id:int` | Yes | Standard key; depends on item/weapon/shop references. | `totem_gameplay_catalog.json/shopStocks` | Migrated to GF_X-native runtime catalog |
| `SkillConfig` | Skill/Combat | 8 | 17 | `SkillId:string` | No | Non-standard primary key; old table has 8 rows and the generated catalog preserves GF_X supplemental Boss/runtime skill rows for 15 total runtime skills. | `totem_gameplay_catalog.json/skills` | Migrated to GF_X-native runtime catalog |
| `TattooColorConfig` | Tattoo | 7 | 4 | `Id:int` | Yes | Standard key; part of 336-combination requirement. | `totem_gameplay_catalog.json/tattooColors` | Migrated to GF_X-native runtime catalog |
| `TattooElementConfig` | Tattoo | 7 | 6 | `Id:int` | Yes | Standard key; element behavior mapping must be rewritten GF_X-native. | `totem_gameplay_catalog.json/tattooElements` | Migrated to GF_X-native runtime catalog |
| `TattooEnchantAffixConfig` | Tattoo/NPC | 24 | 10 | `Id:int` | Yes | Standard key; affix conditions and weights must be testable. | `totem_gameplay_catalog.json/tattooEnchantAffixes` | Migrated to GF_X-native runtime catalog |
| `TattooEnchantRecipeConfig` | Tattoo/NPC | 3 | 5 | `Id:int` | Yes | Standard key; enchant cost and limits connect to NPC/shop. | `totem_gameplay_catalog.json/tattooEnchantRecipes` | Migrated to GF_X-native runtime catalog |
| `TattooPartConfig` | Tattoo | 6 | 7 | `Id:int` | Yes | Standard key; trigger-event mapping must not depend on old event bus. | `totem_gameplay_catalog.json/tattooParts` | Migrated to GF_X-native runtime catalog |
| `TattooPatternConfig` | Tattoo | 8 | 4 | `Id:int` | Yes | Standard key; shape behavior mapping must be rewritten. | `totem_gameplay_catalog.json/tattooPatterns` | Migrated to GF_X-native runtime catalog |
| `TattooReadingTimeConfig` | Tattoo | 6 | 3 | `PartId:int` | No | Non-standard primary key; maps to `TattooPartConfig.Id`. | `totem_gameplay_catalog.json/tattooReadingTimes` | Migrated to GF_X-native runtime catalog |
| `TattooShapeConfig` | Tattoo | 8 | 5 | `Id:int` | Yes | Standard key; param semantics require behavior tests. | `totem_gameplay_catalog.json/tattooShapes` | Migrated to GF_X-native runtime catalog |
| `ThreeChoiceOptionConfig` | Event/UI | 11 | 11 | `OptionId:string` | No | Non-standard primary key; contains JSON weight bonus field. | `totem_gameplay_catalog.json/choiceOptions` | Migrated to GF_X-native runtime catalog |
| `UIFormConfig` | UI | 12 | 5 | `Id:int` | Yes | Standard key; old prefab paths must be remapped to GF_X UI workflow. | `GameData/AIData/DataTables/Core/UITable.json` + `UIViews` + `Assets/Game/Prefabs/UI` | Migrated to GF_X UI table/prefab workflow |
| `WeaponConfig` | Weapon/Combat | 5 | 18 | `WeaponId:string` | No | Non-standard primary key; weapon prefab/projectile/trait references must be preserved. | `totem_gameplay_catalog.json/weapons` | Migrated to GF_X-native runtime catalog |
| `WeaponDropConfig` | Weapon/Spawner | 15 | 6 | `DropId:string` | No | Non-standard primary key; grouped drop ranges and sources need validation. | `totem_gameplay_catalog.json/weaponDrops` | Migrated to GF_X-native runtime catalog |
| `WeaponTraitConfig` | Weapon | 10 | 6 | `TraitId:string` | No | Non-standard primary key; trait ids referenced by `WeaponConfig`. | `totem_gameplay_catalog.json/weaponTraits` | Migrated to GF_X-native runtime catalog |
| `ZoneShrinkConfig` | MapGen | 3 | 7 | `Id:int` | Yes | Standard key; shrinking-zone timing is first-round required. | `totem_gameplay_catalog.json/zonePhases` | Migrated to GF_X-native runtime catalog |

## Required tests after migration

- Schema test: every migrated table loads through GF_X without old `DataTableModule`.
- AI workflow test: all 28 Business AI JSON manifests validate through `AIGameDataTableGenerator`.
- Reverse workflow test: all 28 Business AI JSON manifests can create/update
  `GameData/DataTables/Business/*.xlsx` without warnings or failures.
- Sync freshness test: GF_X diagnostics must report `businessJsonExcelSync.successCount=28`,
  `businessJsonExcelSync.failureCount=0`, and `businessJsonExcelSync.changedCellCount=0`, proving
  the planner-readable xlsx files match the AI-editable Business JSON content.
- Catalog generation test: `tools/ai_index/build_gameplay_catalog_from_business_tables.py --check`
  must pass so the loaded gameplay catalog stays synchronized with Business AI DataTables.
- Reference test: cross-table ids resolve for weapon/projectile/trait, NPC/shop, tattoo, enemy/boss, event/three-choice.
- Combination test: tattoo data produces 336 valid base combinations unless user revises the formula.
- Scale smoke test: actor/bot config can instantiate the first-round population model without missing data.
