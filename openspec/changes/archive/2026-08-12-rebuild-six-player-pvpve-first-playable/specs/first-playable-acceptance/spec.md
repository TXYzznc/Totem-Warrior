## ADDED Requirements

### Requirement: 第一版必须提供五轮端到端 PlayMode smoke

自动 smoke MUST 从 `Assets/Game/Scene/Launch.unity` 启动，经主菜单确认、五次构筑、五轮战斗、四次缩圈、结果界面并返回主菜单；不得依赖人工点击、最终美术或外部网络。

#### Scenario: 快速模式 smoke
- **WHEN** 使用固定 seed 和快速模式运行
- **THEN** 战斗 60 秒、缩圈 10 秒的配置生效
- **AND** 流程在超时前返回主菜单并明确 PASS/FAIL

### Requirement: 核心规则必须有 EditMode 覆盖

队伍/友伤、五轮转换、四次缩圈、颜料收支、资源刷新、情报快照、元素反应与归因、事件队列、倒地救援和 PVP 结算 MUST 有确定性测试。

#### Scenario: 固定输入重复运行核心规则测试
- **WHEN** 使用相同 seed 和输入连续执行两次 EditMode 规则测试
- **THEN** 两次的阶段、资源、队列、归因和结算结果完全一致

### Requirement: 五轮结算必须输出开发证据

结果 MUST 包含 seed、模式、阶段耗时、队伍存活、构筑快照、精确成果、异常/超时和关键配置版本，并支持保存为可比较证据。

#### Scenario: 快速模式完成第五轮
- **WHEN** 固定 seed 对局完成第五轮结算
- **THEN** 保存包含所有必需字段的结构化证据
- **AND** 同一 seed 的后续证据可以逐字段比较

### Requirement: GF_X 全量诊断必须通过重构范围内检查

与地图锚点、参赛者、五轮流程、输入、UI、资源配置和战斗相关的诊断 MUST 全部通过；非本 change 的工作区告警必须有明确豁免记录。

#### Scenario: 执行 GF_X Run All
- **WHEN** 对当前 first playable 运行全量诊断
- **THEN** 重构范围内检查全部通过且 failure 为 0
- **AND** 范围外告警在证据中单独标记，不得伪装成通过
