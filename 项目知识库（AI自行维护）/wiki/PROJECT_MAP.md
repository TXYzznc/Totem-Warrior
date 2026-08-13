# AI 项目地图

> 由 `tools/ai_index/build_ai_manifests.py` 生成。项目级任务先读本文件，再进入具体模块。
>
> 生成日期：2026-08-12

## 1. 读取入口

```text
AGENTS.md
→ 项目知识库（AI自行维护）/wiki/INDEX.md
→ 项目知识库（AI自行维护）/wiki/PROJECT_MAP.md
→ 项目知识库（AI自行维护）/wiki/ACTIVE_CONTEXT.md
→ 项目知识库（AI自行维护）/wiki/manifests/*.json
→ Assets/Game/Scripts/Runtime 或 Assets/Game/Scripts/UI（当前 GF_X 业务代码）
→ Assets/Game/Scripts/<Module>/MODULE.md（仅作为旧行为证据）
```

## 2. 信息分层

| 层级 | 路径 | 作用 |
|---|---|---|
| AI 行为入口 | `AGENTS.md` / `.claude/` / `.codex/` | agent 路由、skill 白名单、grill-me、openspec 工作流 |
| 项目知识库 | `项目知识库（AI自行维护）/` | GDD、wiki、历史决策、AI 自维护知识 |
| 变更记录 | `openspec/changes/` / `openspec/specs/` | 中大型变更的 proposal、design、tasks、spec、测试和归档 |
| 当前 GF_X 业务代码 | `Assets/Game/Scripts/` | 当前可编译、可启动的业务运行时；新增业务脚本优先进入这里 |
| GF_X/工具核心 | `Assets/Game/ScriptsBuiltin/` | GF_X runtime/editor/diagnostics/tooling 边界，修改前需要更谨慎 |
| 旧程序模块证据 | `Assets/Game/Scripts/` | 旧业务模块证据，每个模块有 `MODULE.md`；归档后不再作为 Unity 编译入口 |
| 当前业务配置 | `GameData/AIData/DataTables/Business/` + `GameData/DataTables/Business/` | AI 友好 JSON 与策划可读 xlsx；runtime catalog 由 Business JSON 生成 |
| 旧配置证据 | `GameData/AIData/DataTables/Business/` + `Assets/Game/Scripts/DataTable/Business/` | 旧 JSON 与旧生成 C# 只作为字段/行为证据 |
| 功能切片索引 | `项目知识库（AI自行维护）/wiki/manifests/feature_slices.json` | 按功能串联策划表、美术 runtime key、程序服务、UI 和诊断证据 |
| 诊断定位索引 | `项目知识库（AI自行维护）/wiki/manifests/diagnostic_triage.json` | 从失败的 GF_X 诊断反查功能切片、表、服务、UI 和资源 key |
| 美术资源 | `Assets/Resources/Prefab/` / `Sprite/` / `Audio/` / `Effect/` + `Assets/Game/Prefabs/` | 复用资源内容，但加载和生命周期必须走 GF_X；先查 `manifests/art_assets.json` 的 `usage_status`、`runtime_usages`、`ui_form_usages` 反链 |
| 测试证据 | `GameData/Diagnostics/Reports/` / `tools/playtest/reports/` | GF_X 自动诊断与人工 playtest |

## 3. 模块总览

| 模块 | 职责 | 关联配置表 |
|---|---|---|
| `Common` | 未登记职责，请补 MODULE_META。 | 无 |
| `DataTable` | 配置表加载、注册表消费、JSON 到强类型表的运行时入口。 | 无 |
| `Editor` | 未登记职责，请补 MODULE_META。 | 无 |
| `Entity` | 未登记职责，请补 MODULE_META。 | 无 |
| `EventArgs` | 未登记职责，请补 MODULE_META。 | 无 |
| `Extension` | 未登记职责，请补 MODULE_META。 | 无 |
| `Network` | 未登记职责，请补 MODULE_META。 | 无 |
| `Procedures` | 未登记职责，请补 MODULE_META。 | 无 |
| `Runtime` | 未登记职责，请补 MODULE_META。 | 无 |
| `ScriptableObject` | 未登记职责，请补 MODULE_META。 | 无 |
| `Testing` | 未登记职责，请补 MODULE_META。 | 无 |
| `UI` | UGUI Form、HUD、菜单、运行结果、商店、纹身界面与 UI 数据绑定。 | UIFormConfig |

## 4. 配置表入口

- 当前 AI JSON：`GameData/AIData/DataTables/Business/`
- 当前策划 xlsx：`GameData/DataTables/Business/`
- 当前 runtime catalog：`GameData/AIData/GameplayCatalogs/totem_gameplay_catalog.json`
- 旧配置 JSON 证据：`GameData/AIData/DataTables/Business/`
- 旧生成 C# 证据：`Assets/Game/Scripts/DataTable/Business/`
- 旧运行时加载证据：`Assets/Game/Scripts/DataTable/DataTableModule.cs`
- 旧注册表证据：`Assets/Game/Scripts/DataTable/Business/DataTableRegistry.cs`
- 业务配置表数量：7
- 跨岗位功能切片数量：14
- 功能切片覆盖旧模块数量：24/12
- 功能切片覆盖运行时服务数量：31
- 功能切片入口：`项目知识库（AI自行维护）/wiki/manifests/feature_slices.json`
- 诊断定位入口：`项目知识库（AI自行维护）/wiki/manifests/diagnostic_triage.json`（19 个诊断场景）

## 5. 活跃 OpenSpec

| Change | 路径 | Artifact 状态 |
|---|---|---|
| `3d-pvpve-art-vertical-slice` | `openspec/changes/3d-pvpve-art-vertical-slice` | proposal=True / design=True / tasks=True |
| `add-oasis-city-lookdev-lighting` | `openspec/changes/add-oasis-city-lookdev-lighting` | proposal=True / design=True / tasks=True |
| `dynamic-tattoo-visuals` | `openspec/changes/dynamic-tattoo-visuals` | proposal=True / design=True / tasks=True |
| `generic-visual-destruction` | `openspec/changes/generic-visual-destruction` | proposal=True / design=True / tasks=True |
| `produce-totem-art-assets` | `openspec/changes/produce-totem-art-assets` | proposal=True / design=True / tasks=True |
| `rebaseline-pvpve-art-resources` | `openspec/changes/rebaseline-pvpve-art-resources` | proposal=True / design=True / tasks=True |
| `remove-resources-load-paths` | `openspec/changes/remove-resources-load-paths` | proposal=True / design=True / tasks=True |
| `simplify-pcg-terrain-pipeline` | `openspec/changes/simplify-pcg-terrain-pipeline` | proposal=True / design=True / tasks=True |

## 6. AI 修改任务的推荐流程

1. 先读 `ACTIVE_CONTEXT.md`，确认 active change、禁改区和当前风险。
2. 功能改动先查 `manifests/feature_slices.json`，确认对应策划表、美术 key、程序服务、UI 和诊断。
3. 再查 `Assets/Game/Scripts` 中的当前 GF_X 服务/UI/Procedure；需要旧效果证据时再读 `Assets/Game/Scripts/<Module>/MODULE.md`。
4. 配置改动优先改 `GameData/AIData/DataTables/Business`，再用导表/逆向导表流程同步 xlsx 和 runtime catalog。
5. 小改直接修改并验证；中大型改动先创建/推进 openspec change。
6. 改完优先运行 `python .claude/skills/unity-skills/scripts/unity_skills.py totem_diagnostics_run_all --port 8091` 生成 GF_X 诊断报告。
7. 若诊断失败，先用 `manifests/diagnostic_triage.json` 从失败场景反查功能切片和改动面。
8. 至少运行 `python tools/ai_index/build_ai_manifests.py --check` 确认索引未过期。
