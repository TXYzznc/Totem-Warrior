## ADDED Requirements

### Requirement: 统一使用事件协议
系统 MUST 使用逐行 JSON 的版本化协议保存 AI 编辑器使用事件，每个有效事件 MUST 包含时间、来源、事件类型、对象类型、对象名称、会话、项目和唯一事件 ID。

#### Scenario: 公共 CLI 记录规范事件
- **WHEN** 适配器通过命令参数或 stdin 提交合法的 SKILL、Agent、MCP 或会话事件
- **THEN** 系统 MUST 向 `.ai/usage/events.jsonl` 追加一个 `schema_version: 1` 的规范事件

#### Scenario: 非法事件被拒绝
- **WHEN** 显式 CLI 收到缺少来源、对象类型或对象名称的事件
- **THEN** 系统 MUST 返回非零退出码且 MUST NOT 写入不完整事件

### Requirement: 多编辑器适配
系统 MUST 为 Claude Code 和 Codex 提供项目内 Hook 适配，并 MUST 为其他编辑器提供不依赖私有格式的通用 CLI/stdin 接入契约。

#### Scenario: Claude Code 调用被归一化
- **WHEN** Claude Code `PreToolUse` 提交 Skill、Agent 或 `mcp__*` 工具载荷
- **THEN** 适配器 MUST 记录来源为 `claude-code` 的对应规范事件

#### Scenario: Codex 调用被归一化
- **WHEN** Codex Hook 提交 Agent、MCP 或读取 `skills/<name>/SKILL.md` 的工具载荷
- **THEN** 适配器 MUST 记录来源为 `codex` 的对应规范事件，并 MUST NOT 保存原始命令或工具参数

#### Scenario: 任意编辑器显式接入
- **WHEN** 其他编辑器能够执行本地命令或向 stdin 写入 JSON
- **THEN** 它 MUST 能通过公共 CLI 记录带自定义来源名称的规范事件

### Requirement: 隐私与本地边界
采集系统 MUST 只保存统计所需的非敏感元数据，MUST NOT 保存 Prompt、代码、完整路径、完整命令、工具参数或未知 metadata，且 MUST NOT 发送网络请求。

#### Scenario: Hook 载荷包含敏感字段
- **WHEN** 原生 Hook 载荷同时包含 Prompt、command、tool_input 或文件内容
- **THEN** 写入事件 MUST 只包含协议白名单字段和推导出的对象名称

### Requirement: 宿主无阻塞
Hook 模式 MUST 在解析、锁定或写入失败时静默退出成功，并 SHOULD 在正常条件下以短生命周期进程完成记录。

#### Scenario: 日志不可写
- **WHEN** Hook 模式无法创建目录、获取锁或追加日志
- **THEN** 进程 MUST 以退出码 0 结束且 AI 编辑器原操作 MUST 能继续

### Requirement: 旧日志兼容与去重
系统 MUST 保留旧 TSV 日志的读取能力，并 MUST 提供不会重复写入历史事件的幂等迁移。

#### Scenario: 重复迁移旧日志
- **WHEN** 用户对同一份 `.claude/skills/_usage.log` 连续执行两次迁移
- **THEN** 第二次迁移 MUST 不新增重复事件

#### Scenario: 双格式审计
- **WHEN** JSONL 与旧 TSV 同时包含同一个历史事件
- **THEN** 审计报告 MUST 只统计一次

### Requirement: 可解释审计报告
使用频率报告 MUST 支持时间范围过滤，并 MUST 分别展示来源覆盖、对象类型与名称频次、零召回候选和数据时间范围。

#### Scenario: 多来源报告
- **WHEN** 日志包含 Claude Code、Codex 和自定义编辑器事件
- **THEN** 报告 MUST 分来源展示事件数量，并汇总 SKILL、Agent 与 MCP 使用频次

#### Scenario: 覆盖不足提示
- **WHEN** 选定时间窗口内缺少某个已配置一等适配器的事件
- **THEN** 报告 MUST 提示零召回结论可能不完整，而不能把它直接表述为可删除项
