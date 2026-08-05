# ai-tool-usage-telemetry Specification

## Purpose

定义项目内多编辑器 AI 工具使用事件的本地采集、兼容迁移、隐私边界与可解释审计要求，使 Claude Code、Codex 和其他可执行本地 Hook 的编辑器共享稳定统计口径，同时避免采集 Prompt、代码和完整工具参数。
## Requirements
### Requirement: 统一使用事件协议
系统 MUST 使用逐行 JSON 的版本化协议保存 AI 编辑器使用事件，每个有效事件 MUST 包含时间、来源、事件类型、对象类型、对象名称、会话、项目和唯一事件 ID。

#### Scenario: 公共 CLI 记录规范事件
- **WHEN** 适配器通过命令参数或 stdin 提交合法的 SKILL、Agent、MCP 或会话事件
- **THEN** 系统 MUST 向 `.ai/usage/events.jsonl` 追加一个 `schema_version: 1` 的规范事件

#### Scenario: 非法事件被拒绝
- **WHEN** 显式 CLI 收到缺少来源、对象类型或对象名称的事件
- **THEN** 系统 MUST 返回非零退出码且 MUST NOT 写入不完整事件

### Requirement: 多编辑器适配
系统 MUST 为 Codex、Claude Code、Cursor、Kiro 和 TRAE 提供项目内一等 Hook 适配或官方导入流程，并 MUST 为其他编辑器提供不依赖私有格式的通用 CLI/stdin 接入契约。

#### Scenario: Claude Code 调用被归一化
- **WHEN** Claude Code `PreToolUse` 提交 Skill、Agent 或 `mcp__*` 工具载荷
- **THEN** 适配器 MUST 记录来源为 `claude-code` 的对应规范事件

#### Scenario: Codex 调用被归一化
- **WHEN** Codex Hook 提交 Agent、MCP 或读取 `skills/<name>/SKILL.md` 的工具载荷
- **THEN** 适配器 MUST 记录来源为 `codex` 的对应规范事件，并 MUST NOT 保存原始命令或工具参数

#### Scenario: Cursor 调用被归一化
- **WHEN** Cursor 项目 Hook 提交会话或工具载荷
- **THEN** 适配器 MUST 记录来源为 `cursor` 的对应规范事件

#### Scenario: Kiro 调用被归一化
- **WHEN** Kiro 项目 Hook 提交会话或工具载荷
- **THEN** 适配器 MUST 记录来源为 `kiro` 的对应规范事件

#### Scenario: TRAE 调用被归一化
- **WHEN** TRAE 启用或导入项目 Hook 后提交会话或工具载荷
- **THEN** 适配器 MUST 记录来源为 `trae` 的对应规范事件

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

### Requirement: 统一初始化与诊断
系统 MUST 提供 `init`、`doctor` 和 `report` 公共命令，并 MUST 保留现有记录、迁移与 Codex 回填命令的兼容性。

#### Scenario: 新项目执行只读诊断
- **WHEN** AI 在首次对话中执行 `doctor --editor <editor> --json`
- **THEN** 系统 MUST 返回适配配置、宿主信任可验证性和实时事件证据，且 MUST NOT 修改任何文件或信任状态

#### Scenario: 生成频率报告
- **WHEN** 用户执行 `report` 命令
- **THEN** 系统 MUST 使用现有审计口径输出来源、对象频次、零召回候选和数据覆盖提示

### Requirement: 首次对话确认协议
每个一等编辑器适配 MUST 提供项目级首次对话规则；当诊断未确认激活时，AI MUST 暂停原任务、解释将执行的本地操作并请求用户明确确认。

#### Scenario: 用户尚未确认激活
- **WHEN** 首次对话诊断显示适配未激活或需要宿主授权
- **THEN** AI MUST NOT 执行原任务或改变 Hook 信任，并 MUST 请求一次明确确认

#### Scenario: 用户确认后激活
- **WHEN** 用户明确确认激活
- **THEN** AI MUST 执行当前编辑器对应的 `init` 流程，并在需要重启、重新发送任务或点击宿主 Enable 时停止并给出准确步骤

### Requirement: 原生安全边界
初始化 MUST 遵守宿主原生工作区与 Hook 信任机制，MUST NOT 使用全局绕过开关，MUST NOT 自动信任无关 Hook。

#### Scenario: Codex 精确信任当前项目 Hook
- **WHEN** 用户确认后以 `--trust-codex-hooks` 初始化 Codex
- **THEN** 系统 MUST 只写入当前项目 `.codex/hooks.json` 当前哈希对应的信任项，并 MUST 保留其他 Hook 信任项

#### Scenario: 宿主只支持 UI 授权
- **WHEN** 编辑器不提供可编程的项目 Hook 授权接口
- **THEN** 初始化 MUST 报告待宿主授权并给出 UI 操作，且 MUST NOT 将状态伪报为已激活

### Requirement: 五编辑器一等适配
系统 MUST 为 Codex、Claude Code、Cursor、Kiro 和 TRAE 提供项目内适配说明或配置，并 MUST 将支持的 SessionStart 与工具调用事件送入同一公共记录器。

#### Scenario: 克隆项目后发现适配
- **WHEN** 用户用任一一等编辑器打开新克隆的仓库
- **THEN** 该编辑器 MUST 能从仓库内项目规则发现首次预检与激活流程，而无需用户事先知道 CLI 命令

#### Scenario: 实时事件验证
- **WHEN** 已激活编辑器开始会话并调用工具
- **THEN** 日志 MUST 出现同一来源的非推断 `session-start` 与 `tool-use` 事件，并继续遵守隐私白名单
