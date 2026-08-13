# ai-information-contract Specification

## ADDED Requirements

### Requirement: 项目必须有 AI 读取地图

项目 MUST 提供 `项目知识库（AI自行维护）/wiki/PROJECT_MAP.md`，说明知识库、openspec、Unity 代码、配置表、美术资源、测试报告之间的关系。

#### Scenario: AI 开始项目级任务
- **GIVEN** AI 接手跨模块任务
- **WHEN** 需要获取项目上下文
- **THEN** MUST 先读取 `项目知识库（AI自行维护）/wiki/PROJECT_MAP.md` 和 `项目知识库（AI自行维护）/wiki/ACTIVE_CONTEXT.md`

### Requirement: 旧模块证据必须有说明卡

每个 `LegacyProjectArchive/Assets/Scripts/Modules/<Module>/` SHOULD 有 `MODULE.md`，记录旧模块职责、关联历史 GDD、配置表、资源和测试入口。该说明卡只作为旧行为证据，MUST NOT 被当作当前 GF_X runtime 入口。

#### Scenario: AI 需要旧行为证据
- **GIVEN** AI 需要复原或对照旧 `WeaponModule` 行为
- **WHEN** 读取 `LegacyProjectArchive/Assets/Scripts/Modules/Weapon/MODULE.md`
- **THEN** 能看到 Weapon 相关历史 GDD、DataTable、Resources 和测试入口
- **AND** MUST 回到 `Assets/Game/Scripts` 查当前 GF_X 实现后再修改代码

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
