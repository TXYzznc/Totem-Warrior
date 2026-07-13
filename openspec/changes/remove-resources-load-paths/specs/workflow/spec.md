## ADDED Requirements

### Requirement: 活动资源路径不得依赖已删除 Resources 目录
活动代码、运行时配置、编辑器构建规则和诊断 MUST NOT 引用 `Assets/Resources`；归档历史与诊断报告可以保留迁移证据。

#### Scenario: 迁移后路径检查
- **WHEN** 执行活动资源路径诊断
- **THEN** 报告不得包含对 `Assets/Resources` 的活动依赖
