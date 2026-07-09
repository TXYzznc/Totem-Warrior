## Why

当前项目已有一套自研 `GameApp + ModuleRunner + EventBus + DataTableModule` 运行框架，但配置表、诊断、AI 协作和后续框架演进需要迁入刚优化过的 GF_X 能力。直接一次性替换启动和业务代码风险过高，因此第一阶段先建立 GF_X 框架、工具、诊断和迁移契约，让当前项目能继续编译、启动，并为后续逐块重构提供干净边界。

## What Changes

- 迁入 GF_X 第一阶段所需框架资产、Editor 工具、AI DataTable 工具、诊断工具和文档入口。
- 保留当前项目现有玩法、业务脚本和资源；允许移动/重命名业务文件，但不得删除功能。
- 建立当前项目与 GF_X 的路径契约：GF_X 活动区使用 `Assets/Game` 和 `GameData`；当前业务代码先作为被保留业务层继续存在。
- 建立依赖迁移规则：不得原样复制会与当前项目冲突的重复依赖，例如 UniTask、DOTween；UnityGameFramework、GF_X 自带工具依赖需先做冲突检查。
- 建立核心文件修改门槛：涉及 `Assets/Scripts/Core/*`、启动入口、Input、DataTable、UI、场景加载等旧框架绑定文件时，执行前必须点名向用户确认。
- 第一阶段不要求当前业务完全接入 GF_X 运行流程，不做主要玩法大重构。

## Capabilities

### New Capabilities

- `gf-x-framework-migration`: GF_X 框架第一阶段迁移契约，覆盖目录、依赖、启动边界、核心文件确认、保留业务和后续适配路径。
- `gf-x-ai-diagnostics`: GF_X AI DataTable 与诊断工具在当前项目中的迁入、运行、报告和测试点规范。

### Modified Capabilities

- `workflow`: 中大型框架迁移必须落入 openspec change，并把多模块核心契约写入 `CONTRACT.md`。

## Impact

- 受影响目录：`Assets/Game`、`GameData`、`项目知识库（AI自行维护）/outputs`、`项目知识库（AI自行维护）/wiki`、`Assets/Scripts`、`Assets/Resources`、`Assets/Scenes`、`Assets/Tests`、`Packages/manifest.json`。
- 受影响系统：当前 `GameApp` 启动、`ModuleRunner` 生命周期、`InputModule` 输入入口、`DataTableModule` 配置加载、`UIModule` UI 加载、Unity Test/诊断报告入口。
- 第一阶段验收：当前项目能正常打开、编译、启动；GF_X 框架/工具/诊断迁入；现有业务代码和资源不丢；不要求业务完全改用 GF_X 启动流程。
