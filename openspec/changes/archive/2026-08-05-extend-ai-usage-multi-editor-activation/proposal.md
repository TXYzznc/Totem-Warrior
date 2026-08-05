## Why

项目目前只有 Claude Code 与 Codex 的部分 Hook 配置，公共脚本也缺少 `report`、初始化和诊断入口。仓库被克隆到新机器后，用户无法在首次对话中可靠判断统计工具是否已经启用，也无法以同一套安全流程覆盖 Cursor、Kiro 和 TRAE。

## What Changes

- 为统计工具增加统一的 `init`、`doctor` 和 `report` 命令，保留现有 `hook`、`record`、`migrate`、`sync-codex` 兼容性。
- 定义首次对话预检协议：未激活时暂停原任务、说明影响并请求一次明确确认；确认后才执行宿主允许的激活流程。
- 提供 Codex、Claude Code、Cursor、Kiro、TRAE 五种适配器，所有适配器继续写入同一份本地 JSONL 事件日志。
- Codex 仅通过原生 Hook 信任状态写入精确项目 Hook 哈希；不使用绕过信任的启动参数，也不改变其他 Hook 的信任状态。
- 为不能由 CLI 完成原生授权的编辑器输出明确的手动启用步骤，并通过 `doctor` 区分“已配置”“待宿主授权”和“已有实时事件”。
- 将首次激活规则放入各编辑器的项目级指令文件，使 Git 克隆后无需用户先知道命令行用法。
- 继续禁止记录 Prompt、代码、完整命令、完整路径和工具参数，也不新增网络上报。

## Capabilities

### New Capabilities

<!-- None. -->

### Modified Capabilities

- `ai-tool-usage-telemetry`: 扩展为五编辑器一等适配、统一初始化与诊断、首次对话确认，以及安全的原生信任处理。

## Impact

- 修改 `tools/log_tool_usage.py`、`tools/audit_skill_usage.py` 的公共 CLI 与测试。
- 更新 `.codex/hooks.json`、`.claude/settings.json`，新增 Cursor、Kiro、TRAE 的项目适配配置与规则。
- 更新 `AGENTS.md`、`.claude/CLAUDE.md` 和工具文档，明确首次对话激活行为。
- 更新 `openspec/specs/ai-tool-usage-telemetry` 的增量规格。
- 完成后将同一套框架级文件同步到 `D:\unity\UnityProject\AI_Friendly_Frame`。
