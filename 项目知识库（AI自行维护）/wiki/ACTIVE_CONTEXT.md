# AI 当前上下文

> 由 `tools/ai_index/build_ai_manifests.py` 生成。用于提醒 AI 当前项目状态和任务前检查项。
>
> 生成日期：2026-08-12

## 1. 活跃 OpenSpec Change

- `3d-pvpve-art-vertical-slice`：openspec/changes/3d-pvpve-art-vertical-slice（proposal=True，design=True，tasks=True）
- `add-oasis-city-lookdev-lighting`：openspec/changes/add-oasis-city-lookdev-lighting（proposal=True，design=True，tasks=True）
- `dynamic-tattoo-visuals`：openspec/changes/dynamic-tattoo-visuals（proposal=True，design=True，tasks=True）
- `generic-visual-destruction`：openspec/changes/generic-visual-destruction（proposal=True，design=True，tasks=True）
- `produce-totem-art-assets`：openspec/changes/produce-totem-art-assets（proposal=True，design=True，tasks=True）
- `rebaseline-pvpve-art-resources`：openspec/changes/rebaseline-pvpve-art-resources（proposal=True，design=True，tasks=True）
- `remove-resources-load-paths`：openspec/changes/remove-resources-load-paths（proposal=True，design=True，tasks=True）
- `simplify-pcg-terrain-pipeline`：openspec/changes/simplify-pcg-terrain-pipeline（proposal=True，design=True，tasks=True）

## 2. 任务前检查

- 先读 `AGENTS.md`，确认是否触发 grill-me / openspec / agent 路由。
- 涉及功能改动时，先读 `manifests/feature_slices.json`，按切片确认策划/美术/程序/QA 交接点。
- 涉及 Unity 代码时，优先读 `Assets/Game/Scripts` 当前 GF_X 业务代码；旧 `MODULE.md` 只作为旧行为证据。
- 涉及配置时，先读 `manifests/datatables.json`、`GameData/AIData/DataTables/Business/*.json` 和 `totem_gameplay_catalog.json`。
- 涉及美术时，先读 `manifests/art_assets.json` 的 `usage_status` / `runtime_usages` / `ui_form_usages` / `usage_guidance`，再对照 runtime asset catalog 或 UIFormConfig。
- 涉及测试或诊断失败时，先读 `manifests/diagnostic_triage.json`，再读 `manifests/tests.json`、最近 `GameData/Diagnostics/Reports/gf-diagnostics-run-all_*.json` 和最近 playtest 报告；需要刷新 GF_X 全量诊断时运行 `python .claude/skills/unity-skills/scripts/unity_skills.py totem_diagnostics_run_all --port 8091`。

## 3. 禁改区 / 谨慎区

- 不直接修改 `.codex/agents/*.toml`，源文件是 `.claude/agents/*.md`。
- 不直接修改 `项目知识库（AI自行维护）/raw/`。
- 不在没有 openspec 的情况下大改 GF_X 框架核心（例如 `Assets/Game/ScriptsBuiltin/`）。
- 不让业务代码绕过 `TotemInputService` / `ITotemInputProvider` 读取按键输入。
- 不在 `Update` / `LateUpdate` 热路径中引入 GC 分配。

## 4. 当前索引健康

状态：`warning`，warning 数：10，功能切片数：14，诊断定位场景数：19

- Common 缺少 MODULE_META 描述
- Editor 缺少 MODULE_META 描述
- Entity 缺少 MODULE_META 描述
- EventArgs 缺少 MODULE_META 描述
- Extension 缺少 MODULE_META 描述
- Network 缺少 MODULE_META 描述
- Procedures 缺少 MODULE_META 描述
- Runtime 缺少 MODULE_META 描述
- ScriptableObject 缺少 MODULE_META 描述
- Testing 缺少 MODULE_META 描述
