# AI 项目地图

> 由 `tools/ai_index/build_ai_manifests.py` 生成。项目级任务先读本文件，再进入具体模块。
>
> 生成日期：2026-07-15

## 1. 读取入口

```text
AGENTS.md
→ 项目知识库（AI自行维护）/wiki/INDEX.md
→ 项目知识库（AI自行维护）/wiki/PROJECT_MAP.md
→ 项目知识库（AI自行维护）/wiki/ACTIVE_CONTEXT.md
→ 项目知识库（AI自行维护）/wiki/manifests/*.json
→ Assets/Game/Scripts/Runtime 或 Assets/Game/Scripts/UI（当前 GF_X 业务代码）
→ LegacyProjectArchive/Assets/Scripts/Modules/<Module>/MODULE.md（仅作为旧行为证据）
```

## 2. 信息分层

| 层级 | 路径 | 作用 |
|---|---|---|
| AI 行为入口 | `AGENTS.md` / `.claude/` / `.codex/` | agent 路由、skill 白名单、grill-me、openspec 工作流 |
| 项目知识库 | `项目知识库（AI自行维护）/` | GDD、wiki、历史决策、AI 自维护知识 |
| 变更记录 | `openspec/changes/` / `openspec/specs/` | 中大型变更的 proposal、design、tasks、spec、测试和归档 |
| 当前 GF_X 业务代码 | `Assets/Game/Scripts/` | 当前可编译、可启动的业务运行时；新增业务脚本优先进入这里 |
| GF_X/工具核心 | `Assets/Game/ScriptsBuiltin/` | GF_X runtime/editor/diagnostics/tooling 边界，修改前需要更谨慎 |
| 旧程序模块证据 | `LegacyProjectArchive/Assets/Scripts/Modules/` | 旧业务模块证据，每个模块有 `MODULE.md`；归档后不再作为 Unity 编译入口 |
| 当前业务配置 | `GameData/AIData/DataTables/Business/` + `GameData/DataTables/Business/` | AI 友好 JSON 与策划可读 xlsx；runtime catalog 由 Business JSON 生成 |
| 旧配置证据 | `LegacyProjectArchive/Assets/Resources/DataTable/` + `LegacyProjectArchive/Assets/Scripts/DataTable/` | 旧 JSON 与旧生成 C# 只作为字段/行为证据 |
| 功能切片索引 | `项目知识库（AI自行维护）/wiki/manifests/feature_slices.json` | 按功能串联策划表、美术 runtime key、程序服务、UI 和诊断证据 |
| 诊断定位索引 | `项目知识库（AI自行维护）/wiki/manifests/diagnostic_triage.json` | 从失败的 GF_X 诊断反查功能切片、表、服务、UI 和资源 key |
| 美术资源 | `Assets/Resources/Prefab/` / `Sprite/` / `Audio/` / `Effect/` + `Assets/Game/Prefabs/` | 复用资源内容，但加载和生命周期必须走 GF_X；先查 `manifests/art_assets.json` 的 `usage_status`、`runtime_usages`、`ui_form_usages` 反链 |
| 测试证据 | `GameData/Diagnostics/Reports/` / `tools/playtest/reports/` / `LegacyProjectArchive/Assets/Tests/` | GF_X 自动诊断、人工 playtest、旧测试证据 |

## 3. 模块总览

| 模块 | 职责 | 关联配置表 |
|---|---|---|
| `Audio` | 音效、BGM、事件驱动的一次性播放与运行时音频桥接。 | 无 |
| `Bot` | AI 对手控制、Bot 配置、构筑预设与战斗行为入口。 | BotConfig, BotBuildPreset |
| `Camera` | 2.5D 正交相机、LateUpdate 跟随、边界 clamp、震动整合。 | 无 |
| `Combat` | 战斗意图、命中、伤害、攻击事件与玩家/敌人战斗流程。 | ProjectileConfig, WeaponConfig, SkillConfig |
| `DataTable` | 配置表加载、注册表消费、JSON 到强类型表的运行时入口。 | 无 |
| `Economy` | 货币、资源、商店库存、宝箱奖励与经济消耗。 | ResourceConfig, ItemConfig, ChestConfig, MerchantConfig, ShopStockConfig |
| `Enemy` | 敌人、Boss、怪物属性、死亡与相关战斗接入。 | EnemyConfig, BossPhaseConfig |
| `Event` | 三选一事件、事件配置与事件 UI/奖励流程。 | EventConfig, ThreeChoiceOptionConfig |
| `Flow` | 流程编排、启动/运行阶段切换和模块间流程上下文。 | 无 |
| `GameState` | 游戏状态机、RunStarted/GameOver 等状态转换事件来源。 | 无 |
| `Input` | 玩家输入、测试输入注入与所有按键入口。 | 无 |
| `MapGen` | 地图生成/加载、缩圈、地形与交互物布点。 | MapTemplateConfig, ZoneShrinkConfig |
| `NPC` | 纹身师、商人等 NPC 的生成、交互与 UI 接入。 | NPCConfig, MerchantConfig, ShopStockConfig |
| `Resource` | 资源定义和轻量资源查询入口。 | ResourceConfig |
| `Save` | 存档、序列化、运行记录保存与恢复。 | 无 |
| `Scene` | GF_X Launch 场景入口、场景切换和旧场景证据边界。 | 无 |
| `Settings` | 设置项、设置 UI 数据接入与运行时选项。 | 无 |
| `Skill` | 主动技能配置、释放、命中效果与技能 UI 数据。 | SkillConfig |
| `Spawner` | 玩家、敌人、Bot、掉落物等运行时生成入口。 | EnemyConfig, WeaponDropConfig, ChestConfig |
| `Status` | 状态效果、DoT、控制、叠层与状态图标事件来源。 | 无 |
| `Tattoo` | 纹身构筑、部位/颜色/元素/形状、附魔、读条与构筑策略。 | TattooColorConfig, TattooElementConfig, TattooPartConfig, TattooPatternConfig, TattooShapeConfig, TattooReadingTimeConfig, TattooEnchantAffixConfig, TattooEnchantRecipeConfig |
| `UI` | UGUI Form、HUD、菜单、运行结果、商店、纹身界面与 UI 数据绑定。 | UIFormConfig |
| `VFX` | 命中特效、粒子、镜头抖动、战斗视觉反馈。 | 无 |
| `Weapon` | 武器配置、攻击、拾取、升级、特性和弹道接入。 | WeaponConfig, WeaponDropConfig, WeaponTraitConfig, ProjectileConfig |

## 4. 配置表入口

- 当前 AI JSON：`GameData/AIData/DataTables/Business/`
- 当前策划 xlsx：`GameData/DataTables/Business/`
- 当前 runtime catalog：`GameData/AIData/GameplayCatalogs/totem_gameplay_catalog.json`
- 旧配置 JSON 证据：`LegacyProjectArchive/Assets/Resources/DataTable/`
- 旧生成 C# 证据：`LegacyProjectArchive/Assets/Scripts/DataTable/`
- 旧运行时加载证据：`LegacyProjectArchive/Assets/Scripts/Modules/DataTable/DataTableModule.cs`
- 旧注册表证据：`LegacyProjectArchive/Assets/Scripts/DataTable/DataTableRegistry.cs`
- 业务配置表数量：28
- 跨岗位功能切片数量：14
- 功能切片覆盖旧模块数量：24/24
- 功能切片覆盖运行时服务数量：31
- 功能切片入口：`项目知识库（AI自行维护）/wiki/manifests/feature_slices.json`
- 诊断定位入口：`项目知识库（AI自行维护）/wiki/manifests/diagnostic_triage.json`（19 个诊断场景）

## 5. 活跃 OpenSpec

| Change | 路径 | Artifact 状态 |
|---|---|---|
| `26-fixed-map-three-themes` | `openspec/changes/26-fixed-map-three-themes` | proposal=True / design=True / tasks=True |
| `27-ai-information-contract` | `openspec/changes/27-ai-information-contract` | proposal=True / design=True / tasks=True |
| `28-pcg-map-runtime-integration` | `openspec/changes/28-pcg-map-runtime-integration` | proposal=True / design=True / tasks=True |
| `additive-gameplay-loading-world-sorting` | `openspec/changes/additive-gameplay-loading-world-sorting` | proposal=True / design=True / tasks=True |
| `dynamic-tattoo-visuals` | `openspec/changes/dynamic-tattoo-visuals` | proposal=True / design=True / tasks=True |
| `gf-x-business-runtime-refactor` | `openspec/changes/gf-x-business-runtime-refactor` | proposal=True / design=True / tasks=True |
| `gf-x-framework-migration-phase1` | `openspec/changes/gf-x-framework-migration-phase1` | proposal=True / design=True / tasks=True |
| `native-enemy-domain-rebuild` | `openspec/changes/native-enemy-domain-rebuild` | proposal=True / design=True / tasks=True |
| `produce-totem-art-assets` | `openspec/changes/produce-totem-art-assets` | proposal=True / design=True / tasks=True |
| `remove-resources-load-paths` | `openspec/changes/remove-resources-load-paths` | proposal=True / design=True / tasks=True |
| `simplify-pcg-terrain-pipeline` | `openspec/changes/simplify-pcg-terrain-pipeline` | proposal=True / design=True / tasks=True |

## 6. AI 修改任务的推荐流程

1. 先读 `ACTIVE_CONTEXT.md`，确认 active change、禁改区和当前风险。
2. 功能改动先查 `manifests/feature_slices.json`，确认对应策划表、美术 key、程序服务、UI 和诊断。
3. 再查 `Assets/Game/Scripts` 中的当前 GF_X 服务/UI/Procedure；需要旧效果证据时再读 `LegacyProjectArchive/Assets/Scripts/Modules/<Module>/MODULE.md`。
4. 配置改动优先改 `GameData/AIData/DataTables/Business`，再用导表/逆向导表流程同步 xlsx 和 runtime catalog。
5. 小改直接修改并验证；中大型改动先创建/推进 openspec change。
6. 改完优先运行 `python .claude/skills/unity-skills/scripts/unity_skills.py totem_diagnostics_run_all --port 8091` 生成 GF_X 诊断报告。
7. 若诊断失败，先用 `manifests/diagnostic_triage.json` 从失败场景反查功能切片和改动面。
8. 至少运行 `python tools/ai_index/build_ai_manifests.py --check` 确认索引未过期。
