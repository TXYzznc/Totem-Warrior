# 项目上下文 — AI 快速理解指南

> AI 在新会话中先读本文件了解当前真实状态。历史实现、旧模块和旧 DataTable 只作为需求证据，不作为新运行时入口。

---

## 一、当前定位

这是一个 Unity 2022.3.62f3 项目，当前已迁入并采用 GF_X runtime。

当前目标不是兼容旧 `GameApp / ModuleRunner / EventBus` 框架，而是在 GF_X 基础上重写《Totem Warrior》业务功能：

1. 主流程：主菜单 -> 角色选择 -> 启动选择 -> 战斗 HUD。
2. 第一轮玩法合同：50 个非 Boss actor、20 Smart AI、29 Light AI、336 纹身组合、商店/NPC、三选一、缩圈、Boss。
3. 资源策略：复用旧美术内容，但加载、生命周期、索引和 fallback 走 GF_X。
4. 配置策略：当前以 AI 友好的 gameplay/runtime asset catalog 为运行配置入口；旧 xlsx/DataTable 仅作证据，正式 GF_X DataTable 工作流后续再定。
5. 测试策略：所有新增运行时能力必须进入 GF_X diagnostics，确保 AI 能从报告、日志和快照定位问题。

---

## 二、关键路径

| 类型 | 当前路径 | 说明 |
|---|---|---|
| 启动场景 | `Assets/Game/Scene/Launch.unity` | Build Settings 第一项 |
| 新业务代码 | `Assets/Game/Scripts` | Totem runtime/UI/services |
| GF_X 框架/工具代码 | `Assets/Game/ScriptsBuiltin` | diagnostics、helper、editor 工具等 |
| 旧业务证据 | `LegacyProjectArchive` | 不回挂启动或运行流程 |
| 运行配置 | `GameData/AIData/GameplayCatalogs/totem_gameplay_catalog.json` | AI 可读玩法目录 |
| 运行资源索引 | `GameData/AIData/GameplayCatalogs/totem_runtime_assets.json` | key -> 旧路径/当前路径/用途/fallback |
| 总资源索引 | `项目知识库（AI自行维护）/manifests/art_assets.json` | 全量美术资源路径、类型、用途、生命周期策略 |
| 诊断报告 | `GameData/Diagnostics/Reports` | `gf-diagnostics-run-all_*.json` |
| OpenSpec 变更 | `openspec/changes/gf-x-business-runtime-refactor` | 当前大重构跟踪 |

---

## 三、禁止事项

- 不要恢复或依赖旧 `GameApp`、`ModuleRunner`、`EventBus`、`UIModule`、`DataTableModule`、`SaveModule`。
- 不要新建 `Assets/Resources/DataTable` 作为运行配置源。
- 不要把旧 `Assets/Scripts` 业务代码移回活动编译路径。
- 不要让示例项目内容进入启动/运行流程；示例必须隔离。
- 不要绕过 `TotemInputService / ITotemInputProvider` 直接在业务中读按键输入。
- 不要在 service 内复制隐藏静态玩法目录；静态辅助查询也要从 `TotemDataService.LoadGameplayCatalogOrDefault()` 读取。

---

## 四、当前验证入口

常用完整验证链：

```powershell
Invoke-RestMethod -Method Post -Uri 'http://127.0.0.1:8092/skill/asset_refresh' -ContentType 'application/json' -Body '{}'
Invoke-RestMethod -Method Post -Uri 'http://127.0.0.1:8092/skill/editor_execute_menu' -ContentType 'application/json' -Body '{"menuPath":"Game Framework/GameTools/Diagnostics/Run All"}'
cmd /c openspec validate gf-x-business-runtime-refactor --strict
python tools\ai_index\build_ai_manifests.py --check
```

最近已验证状态：

- GF diagnostics：`gf-diagnostics-run-all_20260708_113622.json`，`success=23`，`failure=0`，`warning=0`。
- Unity Console：项目错误 `0`。
- OpenSpec：`gf-x-business-runtime-refactor` strict valid。
- AI manifest：最新。
- 活动脚本污染扫描：`Assets/Game/Scripts` 不含旧宿主 token。

---

## 五、诊断覆盖

当前全量诊断包含：

- GF_X Rewrite Inventory Contract
- GF_X Business Runtime Contract
- Clean Workspace Contract
- Migration Path Contract
- Totem AI Runtime
- Totem Actor Visual Runtime
- Totem Audio Runtime
- Totem Choice Runtime
- Totem Extended Gameplay
- Totem First Round Contract
- Totem First Slice UI
- Totem Gameplay Catalog
- Totem Gameplay Runtime
- Totem Meta Progress
- Totem Runtime Assets
- Totem VFX Runtime

`Totem First Round Contract` 是第一轮复现范围的汇总验收项；后续改业务时它失败就说明核心承诺被破坏。

---

## 六、AI 重新进入顺序

1. 读本文件。
2. 读 [AGENTS.md](./AGENTS.md) 和 [.claude/CLAUDE.md](./.claude/CLAUDE.md)。
3. 查 [项目知识库（AI自行维护）/PROJECT_MAP.md](./项目知识库（AI自行维护）/PROJECT_MAP.md) 与 [项目知识库（AI自行维护）/ACTIVE_CONTEXT.md](./项目知识库（AI自行维护）/ACTIVE_CONTEXT.md)。
4. 查 `GameData/AIData/GameplayCatalogs/*.json`。
5. 查最近的 `GameData/Diagnostics/Reports/gf-diagnostics-run-all_*.json`。

---

*最后更新：2026-07-08（GF_X 业务重构阶段）*
