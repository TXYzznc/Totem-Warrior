## ADDED Requirements

### Requirement: AI DataTable 工具迁入
当前项目 SHALL 提供 GF_X AI DataTable 工具入口，支持 Json 校验、Json 逆向更新 xlsx、备份、回滚和 diff 报告。工具路径 MUST 使用当前项目相对路径或 Unity 项目路径解析，不得依赖旧本机绝对路径。

#### Scenario: Json 逆向更新 xlsx
- **WHEN** AI 生成符合规则的 Json 并触发逆向导表
- **THEN** 工具 MUST 校验 Json 结构
- **AND** 工具 MUST 在写入 xlsx 前创建备份
- **AND** 工具 MUST 输出变更行、变更单元格和回滚状态信息

### Requirement: 诊断报告工具迁入
当前项目 SHALL 提供 GF_X 诊断工具入口，诊断报告 MUST 写入 `GameData/Diagnostics/Reports` 或明确配置的等价目录。报告 MUST 包含场景名称、开始结束时间、结果、失败原因和关键日志上下文。

#### Scenario: 诊断报告可供 AI 阅读
- **WHEN** 运行诊断工具
- **THEN** 工具 MUST 生成机器可读的报告文件
- **AND** 报告 MUST 能表达成功、失败、警告和关键上下文

### Requirement: 功能测试点长期化
GF_X 诊断能力 SHALL 作为后续实际开发中的长期测试基础设施，而不是一次性脚本。新增或修改功能时，相关输入、状态变化、事件、资源加载和输出结果 SHOULD 被纳入测试点或诊断场景。

#### Scenario: 新功能效果异常时定位问题
- **WHEN** 新增功能脚本后表现不符合预期
- **THEN** 开发者或 AI SHOULD 先补充或运行相关诊断场景
- **AND** 诊断结果 SHOULD 帮助定位输入、时序、状态、事件或资源路径中的问题

### Requirement: 迁移路径污染诊断
当前项目 SHALL 提供迁移路径诊断，用于检查 GF_X 迁入后是否存在 `AAAGame`、旧本机绝对路径、示例混入默认流程或重复依赖风险。

#### Scenario: 污染项被报告
- **WHEN** 项目中存在旧路径、旧命名或示例混入默认流程
- **THEN** 诊断报告 MUST 列出污染类型、文件路径和处理建议

### Requirement: 启动链诊断边界
第一阶段诊断 SHALL 记录当前项目启动链与 GF_X 工具可用性，但 MUST NOT 在未确认核心文件前强制切换当前业务启动链到 GF_X。

#### Scenario: 只记录不强切
- **WHEN** 运行第一阶段启动链诊断
- **THEN** 报告 MUST 描述当前启动入口和关键模块状态
- **AND** 诊断 MUST NOT 修改启动场景或核心启动代码
