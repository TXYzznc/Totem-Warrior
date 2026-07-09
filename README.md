# Totem Warrior

Unity 2022.3.62f3 项目。当前工程已经迁入 GF_X 框架，旧 `Assets/Scripts` 业务运行时已归档到 `LegacyProjectArchive`，只作为需求证据和资源来源，不作为启动或运行宿主。

## 当前运行入口

- Unity 版本：`2022.3.62f3`
- 默认启动场景：`Assets/Game/Scene/Launch.unity`
- 新业务代码：`Assets/Game/Scripts`
- 旧业务证据：`LegacyProjectArchive`
- 旧资源复用：允许继续引用 `Assets/Resources/Prefab`、`Assets/Resources/Sprite` 等美术资源，但加载和生命周期必须走 GF_X runtime 服务。

## 数据与资源

- AI 可编辑业务配置源：`GameData/AIData/DataTables/Business/*.json`
- 策划可读业务配置表：`GameData/DataTables/Business/*.xlsx`
- 运行配置产物：`GameData/AIData/GameplayCatalogs/totem_gameplay_catalog.json`，由 Business AI DataTable 生成
- 运行资源索引：`GameData/AIData/GameplayCatalogs/totem_runtime_assets.json`
- 旧业务 DataTable 证据：`LegacyProjectArchive/Assets/Resources/DataTable`
- 活动路径中不再使用 `Assets/Resources/DataTable`、旧 `DataTableModule`、旧 `GameApp/ModuleRunner/EventBus`。

当前业务配置工作流为：AI 修改 Business JSON → 逆向生成策划 xlsx → 生成/检查 runtime catalog → 跑 GF_X 诊断。不要把新配置写回旧 `Assets/Resources/DataTable`。

## 诊断

常用验证：

```powershell
cmd /c openspec validate gf-x-business-runtime-refactor --strict
python tools\ai_index\build_ai_manifests.py --check
```

AI 自动诊断优先使用：

```powershell
python .claude\skills\unity-skills\scripts\unity_skills.py totem_diagnostics_run_all --port 8092
```

Unity 内人工复跑菜单：`Game Framework/GameTools/Diagnostics/Run All`。

最新诊断报告输出到：

```text
GameData/Diagnostics/Reports/
```

## AI 协作入口

- Codex 顶层入口：`AGENTS.md`
- Claude 顶层入口：`.claude/CLAUDE.md`
- Agent 源文件：`.claude/agents/*.md`
- Codex agent 镜像：`.codex/agents/*.toml`

修改 `.claude/agents/*.md` 后运行：

```powershell
python tools\sync-agents.py
```

不要直接编辑 `.codex/agents/*.toml`。
