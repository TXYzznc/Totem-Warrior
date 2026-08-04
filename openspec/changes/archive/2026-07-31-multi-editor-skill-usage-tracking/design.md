## Context

当前 `tools/log_tool_usage.py` 直接解析 Claude Code `PreToolUse` 的 stdin，并把三列核心数据追加到 `.claude/skills/_usage.log`。这种实现把数据格式、编辑器 Hook 载荷和存储位置绑定在一起；Codex 即使支持 Hook，也不会产生 Claude Code 的 `Skill` 工具调用，因为 Codex 的 SKILL 通常通过读取 `SKILL.md` 生效。

实现必须兼容现有 77 条 TSV 历史数据，同时满足 Windows 10 主开发环境和 Linux/macOS 适配器使用，不增加依赖、不上传数据、不影响 AI 工具执行。

## Goals / Non-Goals

**Goals:**

- 定义稳定、可扩展、编辑器无关的 JSONL 事件格式。
- 让一个公共 Python CLI 同时服务 Claude Code、Codex 和第三方适配器。
- 对 Claude Code 与 Codex 提供仓库内自动 Hook 接入。
- 让审计报告同时反映来源覆盖、调用频次和零召回可信度。
- 保持旧 TSV 可读，并允许幂等迁移到 JSONL。

**Non-Goals:**

- 不监控进程、键盘、网络或编辑器私有数据库。
- 不采集 Prompt、代码、文件内容、完整工具参数或完整命令。
- 不为 Cursor、Windsurf、Cline 维护随版本变化的专用解析器。
- 不承诺无 Hook API 的编辑器能够零配置自动采集。

## Decisions

### Decision: JSONL 作为规范存储

规范日志位于 `.ai/usage/events.jsonl`，每行一个 `schema_version: 1` 事件。必需字段为 `timestamp`、`source`、`event`、`kind`、`name`、`session_id`、`project` 和 `event_id`。

选择 JSONL 而非扩展 TSV，是因为不同编辑器需要来源和协议版本字段，JSONL 仍可流式追加与逐行容错。SQLite 被排除，因为对仓库内小规模审计过重，也不方便 Hook 静默失败。

### Decision: 单一公共 CLI + 薄适配器

保留 `tools/log_tool_usage.py` 作为稳定入口，提供：

- `hook --source <editor>`：解析该编辑器通过 stdin 发送的 Hook 载荷。
- `record --source ... --kind ... --name ...`：供任何编辑器或脚本显式记录。
- `migrate`：把旧 TSV 事件幂等写入 JSONL。

无参数调用继续按旧 Claude Code 模式运行，避免旧配置瞬间失效。编辑器适配逻辑只负责把原生载荷归一化，写入、去重、隐私过滤共用同一实现。

### Decision: Codex 采用 Hook + 安全推断

`.codex/hooks.json` 增加：

- `SessionStart` 记录来源覆盖；
- `PreToolUse` 把工具调用载荷交给公共 CLI。

Codex 适配器识别原生/兼容 `Skill`、Agent/spawn-agent、MCP 工具名称；对于 shell/read 工具，仅从允许字段中匹配 `.../skills/<skill>/SKILL.md` 路径并记录 SKILL 名称。适配器不持久化原始命令或输入。

这比扫描整个 Codex 会话文件更保守：会话扫描可能接触 Prompt 与代码，也容易绑定私有格式。代价是已经加载、没有产生读取事件的 SKILL 可能漏记，报告必须展示来源覆盖而不能宣称绝对完整。

### Decision: 旧日志双读与幂等迁移

审计器默认读取 JSONL，并把旧 TSV 中尚未出现在 JSONL 的记录合并进内存。`migrate` 为旧行生成确定性 `event_id`，重复运行不会重复追加。旧 TSV 不被删除或改写。

### Decision: 本地最小隐私模型

规范事件仅接受预定义字段。`project` 只保存仓库目录名，`session_id` 截断，`name/source/kind` 经过长度限制和控制字符清理；未知 metadata、Prompt、命令、路径、代码和工具参数全部丢弃。

### Decision: 失败不阻塞宿主

Hook 模式捕获全部异常并以退出码 0 结束。写入采用单行追加和短时锁文件，避免并发写破坏 JSONL；锁超时则放弃该条事件而不是阻塞编辑器。显式 CLI 子命令保留非零退出码，便于人工发现配置错误。

## Risks / Trade-offs

- [Codex Hook 的工具命名随版本变化] → 归一化常见命名并以 fixture 测试；未知事件安全忽略。
- [通配 `PreToolUse` 增加调用开销] → Python 只解析一个小 JSON，非目标事件立即退出；报告中暴露来源覆盖。
- [SKILL 路径推断可能漏记或误记] → 仅匹配以 `SKILL.md` 结尾且位于 `skills/<name>/` 的路径，并记录为 `inferred` 事件。
- [并发 Hook 争用] → 使用短时跨平台锁和追加写；失败不阻塞宿主。
- [旧 TSV 与迁移数据重复] → 使用确定性 legacy event ID，在双读时按 ID 去重。
- [任意编辑器无法完全自动接入] → 提供稳定 CLI 契约和模板，明确“通用接入”不等于“无配置监听”。

## Migration Plan

1. 引入 JSONL writer、公共 CLI 与测试，保持旧无参数入口。
2. 更新 Claude Code Hook，显式传入 `--source claude-code`。
3. 更新 Codex Hook，加入 SessionStart 与 PreToolUse 适配。
4. 升级审计器为 JSONL + TSV 双读，并运行一次幂等迁移验证。
5. 更新月度防腐文档和通用适配器说明。
6. 如需回滚，只需移除新 Hook 项；旧 TSV 和旧审计入口仍保留。

## Open Questions

无。首版范围、格式、验收标准和隐私约束已由用户确认。
