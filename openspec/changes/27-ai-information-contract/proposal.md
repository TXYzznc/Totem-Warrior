# Proposal — AI 信息契约层（27-ai-information-contract）

> 状态：实现中
> 范围：只补信息索引、模块说明、manifest 与校验脚本；不搬目录、不改框架核心、不改业务逻辑。

## Why

当前项目已经具备 AI 协作的主要材料：`项目知识库（AI自行维护）`、`openspec/changes/`、`Assets/Scripts/Modules/`、`Assets/Resources/DataTable/`、美术资源目录与 playtest 报告。

问题在于这些材料之间还缺一层稳定的“信息契约”：AI 想改一个系统时，需要在 GDD、wiki、openspec、DataTable、模块代码、美术资源、测试报告之间来回搜索，容易遗漏上下文，也容易被过期索引误导。

本变更把现有材料串成可机器读取的入口层，让 AI 先走固定索引，再按系统链路取上下文。

## What Changes

- 新增项目级信息入口：
  - `项目知识库（AI自行维护）/PROJECT_MAP.md`
  - `项目知识库（AI自行维护）/ACTIVE_CONTEXT.md`
  - `项目知识库（AI自行维护）/manifests/*.json`
- 为 `Assets/Scripts/Modules/*` 生成 `MODULE.md` 说明卡，记录模块职责、读取顺序、关联 GDD、配置表、资源与测试入口。
- 新增 `tools/ai_index/build_ai_manifests.py`，统一生成/校验 AI 信息索引。
- 更新知识库入口和脚本目录说明，明确 AI 读取项目时的推荐路径。

## Capabilities

### New Capabilities

- `ai-information-contract`：项目知识、代码、配置、美术、测试之间的可追溯索引。

## Impact

- 文档：知识库入口、项目地图、活跃上下文、模块说明卡。
- 工具：新增只读扫描 + 生成 manifest 的 Python 脚本。
- 验证：脚本 `--check` 可检测 manifest 是否与当前项目结构一致。
- 不影响运行时代码和 Unity 场景。
