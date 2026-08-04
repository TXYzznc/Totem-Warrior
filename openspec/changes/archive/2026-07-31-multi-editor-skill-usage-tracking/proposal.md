## Why

现有 SKILL 使用频率工具只通过 Claude Code 的 `PreToolUse` Hook 采集显式调用，Codex 与其他 AI 编辑器没有统一接入，导致日志自 2026-07-02 起停更，零召回报告无法作为治理依据。现在需要建立编辑器无关的最小事件协议，让不同端以适配器方式汇入同一份可审计数据。

## What Changes

- 引入基于 JSONL 的统一使用事件协议，记录来源编辑器、事件类型、名称、会话和项目等非敏感元数据。
- 将现有记录脚本改造成公共 CLI，支持命令行参数、stdin 统一事件和 Claude Code 原生 Hook 载荷。
- 为 Claude Code 与 Codex 提供项目内一等适配；为其他编辑器提供通用 stdin/CLI 模板。
- 升级使用频率报告，按来源、事件类型和时间范围聚合，并兼容读取旧 TSV 日志。
- 增加幂等去重、静默降级、隐私边界和跨平台自动化测试。
- 不引入常驻进程、网络上传、Prompt/代码/工具参数采集，也不维护 Cursor、Windsurf、Cline 的专用适配器。

## Capabilities

### New Capabilities

- `ai-tool-usage-telemetry`: 编辑器无关的本地使用事件协议、采集 CLI、适配器和审计行为。

### Modified Capabilities

- `skill-governance`: 月度防腐审计改为消费多编辑器统一日志，并明确零召回判断所需的数据覆盖信息。

## Impact

- 受影响脚本：`tools/log_tool_usage.py`、`tools/audit_skill_usage.py`，以及新增测试和适配器文档。
- 受影响配置：`.claude/settings.json`、`.codex/hooks.json`、`tools/codex_prompt_hook.js`。
- 受影响数据：新增被 `.gitignore` 忽略的 JSONL 日志；旧 `.claude/skills/_usage.log` 保持只读兼容。
- 不增加第三方 Python 依赖，不访问网络，不改变 Unity 运行时代码。
