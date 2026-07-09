# ai-information-contract Specification

## Purpose

为 AI 提供稳定、可追溯、可重复生成的项目信息入口，使策划、程序、美术、测试与变更记录之间可以被快速定位和交叉验证。

## Requirements

### Requirement: 项目必须有 AI 读取地图

项目 MUST 提供 `项目知识库（AI自行维护）/PROJECT_MAP.md`，说明知识库、openspec、Unity 代码、配置表、美术资源、测试报告之间的关系。

#### Scenario: AI 开始项目级任务
- **GIVEN** AI 接手跨模块任务
- **WHEN** 需要获取项目上下文
- **THEN** MUST 先读取 `PROJECT_MAP.md` 和 `ACTIVE_CONTEXT.md`

### Requirement: 模块必须有说明卡

每个 `Assets/Scripts/Modules/<Module>/` SHOULD 有 `MODULE.md`，记录模块职责、关联 GDD、配置表、资源和测试入口。

#### Scenario: AI 修改某模块
- **GIVEN** AI 需要修改 `WeaponModule`
- **WHEN** 读取 `Assets/Scripts/Modules/Weapon/MODULE.md`
- **THEN** 能看到 Weapon 相关 GDD、DataTable、Resources 和测试入口

### Requirement: manifest 必须可重复生成

项目 MUST 提供脚本生成 `modules.json`、`datatables.json`、`assets.json`、`tests.json`。

#### Scenario: 项目结构变化
- **WHEN** 新增配置表或模块
- **THEN** 运行 `python tools/ai_index/build_ai_manifests.py` 后 manifest MUST 更新

### Requirement: 索引必须可校验

项目 MUST 提供 `--check` 模式，用于发现生成物是否过期。

#### Scenario: CI 或人工检查
- **WHEN** 执行 `python tools/ai_index/build_ai_manifests.py --check`
- **THEN** 如果生成物与当前项目结构不一致，脚本 MUST 返回非零退出码
