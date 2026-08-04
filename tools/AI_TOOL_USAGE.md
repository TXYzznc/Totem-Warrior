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

## 已接入编辑器

### Claude Code

`.claude/settings.json` 的 `PreToolUse` Hook 自动执行：

```text
python tools/log_tool_usage.py hook --source claude-code
```

支持原生 `Skill`、`Agent`、`mcp__*` 工具载荷。

### Codex

`.codex/hooks.json` 在 `SessionStart` 和 `PreToolUse` 调用同一个记录器。当前 Codex 可能把真实工具调用包装在自由格式 `exec` 中；适配器会识别内嵌 `tools.<name>(...)`，并从工具输入中识别 `skills/<name>/SKILL.md` 与 `.codex/agents/<name>.toml`。只保存工具、SKILL、Agent 名称，不保存原始程序、命令、参数或路径。

Hook 是逐条信任的。项目当前至少需要确认 `SessionStart` 中的统计命令和 `PreToolUse` 统计命令；只确认同组中的提示注入命令并不会启用统计。配置变更后请新建 Codex 会话并确认 Hook 信任。

如果 Hook 曾被拒绝、未信任或因 Codex 升级漏采，可以从本机 Codex 会话做幂等补漏：

```bash
python tools/log_tool_usage.py sync-codex --days 3
```

补漏仅处理 `session_meta` 和工具调用记录，且只接受 `cwd` 等于当前项目的会话。输出仍使用同一白名单协议，不会保存 Prompt、回复、代码、完整命令或工具参数。重复执行不会重复追加。

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
```

报告展示：

- 各编辑器来源覆盖；
- SKILL、Agent、MCP、Tool、Session 频次；
- 0 召回项目项；
- 数据时间范围；
- 一等适配器来源缺失警告。

当报告提示来源覆盖不足时，0 召回项只能作为调查候选，不能直接删除。

## 故障策略

`hook` 模式始终 fail-open：JSON 解析、目录权限、锁或写入失败都不会阻塞编辑器原操作。`record` 和 `migrate` 是人工命令，输入错误会返回非零退出码。
