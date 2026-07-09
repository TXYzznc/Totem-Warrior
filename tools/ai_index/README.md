# AI Index 工具

`build_ai_manifests.py` 用来生成 AI 可读取的项目信息契约层。

## 生成

```powershell
python tools\ai_index\build_ai_manifests.py
```

会更新：

- `项目知识库（AI自行维护）/PROJECT_MAP.md`
- `项目知识库（AI自行维护）/ACTIVE_CONTEXT.md`
- `项目知识库（AI自行维护）/manifests/*.json`
- `LegacyProjectArchive/Assets/Scripts/Modules/*/MODULE.md`（旧行为证据卡）

## 校验

```powershell
python tools\ai_index\build_ai_manifests.py --check
```

`--check` 只判断生成物是否与当前项目结构一致，不判断业务逻辑正确性。当前 GF_X 业务代码入口是 `Assets/Game/Scripts`；旧 `LegacyProjectArchive/Assets/Scripts` 只作为需求反推和历史证据。

历史 wiki/GDD 降权规则：旧文档中出现的 `Assets/Scripts`、`GameApp`、`EventBus`、旧 `InputModule` 只作为迁移前证据或需求反推线索，不能覆盖当前 GF_X 运行时口径。当前实现以 `Assets/Game/Scripts`、GF_X runtime、`TotemInputService` / `ITotemInputProvider` 为准。

## 美术资源索引

`项目知识库（AI自行维护）/manifests/art_assets.json` 是逐文件资源索引，覆盖 `Assets/Resources` 与 `Assets/Game` 下的美术、音频、Prefab、动画、材质、字体等资源。

它记录：

- `path`：Unity 工程内路径。
- `resource_key`：位于 `Assets/Resources` 下时可用的 Resources key。
- `inferred_system` / `inferred_role`：根据路径和文件名推断的系统归属与用途。
- `lifecycle_policy`：资源复用和 GF_X 生命周期接入策略。
- `needs_review`：路径无法确定用途、名称重复、或来自 GF_X 示例目录时标记为需要人工确认。

注意：这个索引是 AI 辅助管理文件，不替代用户对资源是否过时、重复、保留的最终判断。

## 功能切片索引

`项目知识库（AI自行维护）/manifests/feature_slices.json` 是跨岗位协作索引，按功能把以下内容放在同一条记录里：

- 策划侧：Business JSON/xlsx 配置表、GDD/OpenSpec 文档。
- 美术侧：runtime asset key、`art_assets.json` 查询入口和资源替换约束。
- 程序侧：GF_X runtime service、UI form、当前业务模块。
- QA 侧：必须覆盖该功能的 GF_X diagnostic scenario。

引用顺序建议：先读当前索引和 GF_X runtime，再读 GDD/OpenSpec/wiki 的历史描述；当历史描述和当前 GF_X 证据冲突时，以当前 GF_X 证据和 active change 审计文档为准。

新增或重做功能时，先查这个索引，确认该功能牵涉哪些策划表、美术资源、程序服务和诊断，再开始修改。

## 诊断定位索引

`项目知识库（AI自行维护）/manifests/diagnostic_triage.json` 是从 GF_X diagnostic scenario 反查功能切片的索引。

当 `totem_diagnostics_run_all` 出现失败时，先用失败的 `Scenario/...` 名称查这个文件，再按它列出的 feature slice、Business 表、runtime service、UI form、runtime asset key 和 docs 去定位问题。

## 维护边界

- 脚本不写 `raw/`。
- 脚本不改 `.claude/` 或 `.codex/`。
- 脚本不改运行时代码，只生成文档和 JSON 索引。
- 脚本不得把旧 `Assets/Scripts` 当成当前运行时入口。
