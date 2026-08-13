# ai-information-contract Specification

## Purpose
TBD - created by archiving change 27-ai-information-contract. Update Purpose after archive.
## Requirements
### Requirement: 项目必须有 AI 读取地图

项目 MUST 提供 `项目知识库（AI自行维护）/wiki/PROJECT_MAP.md`，说明知识库、openspec、Unity 代码、配置表、美术资源、测试报告之间的关系。

#### Scenario: AI 开始项目级任务
- **GIVEN** AI 接手跨模块任务
- **WHEN** 需要获取项目上下文
- **THEN** MUST 先读取 `项目知识库（AI自行维护）/wiki/PROJECT_MAP.md` 和 `项目知识库（AI自行维护）/wiki/ACTIVE_CONTEXT.md`

### Requirement: 旧模块证据必须有说明卡

项目的 AI 信息契约 MUST 只索引当前 GF_X 代码、Business DataTable、OpenSpec 与诊断证据；已删除的旧模块不得作为信息入口。

#### Scenario: AI 需要旧行为证据
- **GIVEN** AI 需要了解 Weapon 当前行为
- **WHEN** 读取当前 GF_X 代码、Business DataTable 与 OpenSpec
- **THEN** MUST 以 `Assets/Game/Scripts` 的实现为准后再修改代码

### Requirement: manifest 必须可重复生成

项目 MUST 提供脚本生成 `项目知识库（AI自行维护）/wiki/manifests/modules.json`、`datatables.json`、`assets.json`、`tests.json`。

#### Scenario: 项目结构变化
- **WHEN** 新增配置表或模块
- **THEN** 运行 `python tools/ai_index/build_ai_manifests.py` 后 `项目知识库（AI自行维护）/wiki/manifests/*.json` MUST 更新

### Requirement: 索引必须可校验

项目 MUST 提供 `--check` 模式，用于发现生成物是否过期。

#### Scenario: CI 或人工检查
- **WHEN** 执行 `python tools/ai_index/build_ai_manifests.py --check`
- **THEN** 如果生成物与当前项目结构不一致，脚本 MUST 返回非零退出码
