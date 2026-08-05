# 多编辑器 AI 工具使用统计

本工具用统一 JSONL 协议统计 SKILL、Agent、MCP 和编辑器会话事件。所有数据只保存在当前项目的 `.ai/usage/events.jsonl`，不会联网。

## 隐私边界

记录字段仅包括：

- `schema_version`
- `timestamp`
- `source`
- `event`
- `kind`
- `name`
- `session_id`
- `project`
- `event_id`
- `adapter_version`
- 可选 `inferred`

不会写入 Prompt、代码、完整路径、完整命令、工具参数、文件内容或任意 metadata。Hook 载荷即使带有这些字段，记录器也只输出白名单字段。

## Git 克隆后的首次激活

Codex、Claude Code、Cursor、Kiro 和 TRAE 都随仓库携带项目配置与首次对话规则。用户不必先打开终端：新会话收到第一条消息后，AI 会先代为运行只读诊断；未激活时暂停原任务并请求一次确认。

只读检查：

```text
python tools/log_tool_usage.py doctor --editor codex --json
```

确认后，AI 根据当前编辑器执行：

```text
python tools/log_tool_usage.py init --editor codex --yes --trust-codex-hooks
python tools/log_tool_usage.py init --editor claude-code --yes
python tools/log_tool_usage.py init --editor cursor --yes
python tools/log_tool_usage.py init --editor kiro --yes
python tools/log_tool_usage.py init --editor trae --yes
```

`--yes` 表示调用它的 AI 已在对话中取得明确确认。不得在首次诊断时直接使用。Codex 初始化通过原生 `hooks/list` 和 `config/batchWrite`，只写当前仓库 `.codex/hooks.json` 各条 Hook 的当前哈希；不会信任其他项目/Hook，也不使用信任绕过参数。

不能由项目脚本完成的宿主安全步骤仍由编辑器处理：Cursor 需要 Trusted Workspace；Claude Code/TRAE 需要批准或 Enable 项目 Hook；Kiro 可能请求 shell 权限。完成后重开会话并重新发送原任务，`SessionStart` 实时记录将使后续诊断通过。

## 已接入编辑器

### Claude Code

`.claude/settings.json` 的 `PreToolUse` Hook 自动执行：

```text
python tools/log_tool_usage.py hook --source claude-code
```

同时记录 `SessionStart`，并支持 Tool、原生 `Skill`、`Agent`、`mcp__*` 工具载荷。

### Codex

`.codex/hooks.json` 在 `SessionStart` 和 `PreToolUse` 调用同一个记录器。当前 Codex 可能把真实工具调用包装在自由格式 `exec` 中；适配器会识别内嵌 `tools.<name>(...)`，并从工具输入中识别 `skills/<name>/SKILL.md` 与 `.codex/agents/<name>.toml`。只保存工具、SKILL、Agent 名称，不保存原始程序、命令、参数或路径。

Hook 是逐条信任的。项目当前至少需要确认 `SessionStart` 中的统计命令和 `PreToolUse` 统计命令；只确认同组中的提示注入命令并不会启用统计。配置变更后请新建 Codex 会话并确认 Hook 信任。

如果 Hook 曾被拒绝、未信任或因 Codex 升级漏采，可以从本机 Codex 会话做幂等补漏：

```bash
python tools/log_tool_usage.py sync-codex --days 3
```

补漏仅处理 `session_meta` 和工具调用记录，且只接受 `cwd` 等于当前项目的会话。输出仍使用同一白名单协议，不会保存 Prompt、回复、代码、完整命令或工具参数。重复执行不会重复追加。

### Cursor

项目级 `.cursor/hooks.json` 使用官方 `version: 1` 格式，在 `sessionStart` 与 `preToolUse` 调用公共记录器，来源为 `cursor`。可信工作区会自动加载项目 Hook。

### Kiro

项目级 `.kiro/hooks/ai-tool-usage.json` 使用 `SessionStart` 与 `PreToolUse` command action，来源为 `kiro`。Kiro 会自动发现该目录；命令权限仍由 Kiro 自身策略决定。

### TRAE

项目级 `.trae/hooks.json` 使用 TRAE 官方项目路径，来源为 `trae`。首次打开时必须阅读安全提示并点击 Enable；TRAE 也支持导入 Claude Code Hook，但本项目无需借此共享来源名称。

## 任意 AI 编辑器接入

只要编辑器能运行本地命令，就可以显式记录：

```bash
python tools/log_tool_usage.py record \
  --source my-editor \
  --kind Skill \
  --name unity-skills \
  --session session-123
```

`--kind` 可选值：

- `Skill`
- `Agent`
- `MCP`
- `Session`
- `Tool`

能够向 stdin 发送 JSON 的 Hook 可以复用通用适配器：

```bash
echo '{"kind":"Skill","name":"unity-skills","session_id":"session-123"}' \
  | python tools/log_tool_usage.py hook --source my-editor
```

通用载荷只需要 `kind` 和 `name`；可选 `event`、`session_id`、`project`、`timestamp`、`event_id`、`inferred`。其他字段会被丢弃。

没有 Hook、任务命令或扩展 API 的编辑器无法做到自动采集。本项目不使用常驻进程扫描编辑器私有日志。

## 历史迁移

旧日志 `.claude/skills/_usage.log` 不会被删除。审计器默认同时读取新旧格式，并按确定性事件 ID 去重。

需要把旧记录写入新 JSONL 时运行：

```bash
python tools/log_tool_usage.py migrate
```

迁移是幂等的，重复执行不会重复追加。

## 审计

```bash
python tools/audit_skill_usage.py
python tools/audit_skill_usage.py --days 30
python tools/audit_skill_usage.py --no-legacy
python tools/log_tool_usage.py report
python tools/log_tool_usage.py report --days 30
```

报告展示：

- 各编辑器来源覆盖；
- SKILL、Agent、MCP、Tool、Session 频次；
- 0 召回项目项；
- 数据时间范围；
- 一等适配器来源缺失警告。

当报告提示来源覆盖不足时，0 召回项只能作为调查候选，不能直接删除。

## 故障策略

`hook` 模式始终 fail-open：JSON 解析、目录权限、锁或写入失败都不会阻塞编辑器原操作。`record`、`migrate`、`init` 是人工/确认后命令，输入错误会返回非零退出码。`doctor` 不修改配置、日志或信任状态。
