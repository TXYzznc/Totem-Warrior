## Context

现有记录器已经把 Claude Code 与 Codex Hook 载荷归一化到 `.ai/usage/events.jsonl`，但仓库克隆后的启用状态依赖宿主编辑器，且不同编辑器的 Hook 文件、信任模型和事件名称不同。Codex 还会按 Hook 文件哈希保存信任，修改配置后旧信任应自然失效。公共脚本目前没有统一的初始化、诊断和报告入口。

这项改动横跨项目指令、五种编辑器配置、CLI 和本地安全状态。设计必须保持原有日志协议与隐私边界，并让不支持程序化授权的宿主继续使用自身的确认 UI。

## Goals / Non-Goals

**Goals:**

- 用一个 Python 入口完成配置检查、初始化、诊断和报告。
- 为 Codex、Claude Code、Cursor、Kiro、TRAE 提供项目级适配和首次对话说明。
- 只有获得用户明确确认后才执行激活；原任务在需要重启或宿主确认时暂停。
- Codex 仅信任当前仓库 `.codex/hooks.json` 的当前哈希，不影响其他 Hook。
- 所有适配器输出相同事件协议且不采集敏感内容。

**Non-Goals:**

- 绕过任何编辑器的工作区信任、命令权限或 Hook 安全提示。
- 保证所有编辑器版本都提供可编程的授权 API。
- 自动修改用户全局 AI 编辑器配置。
- 将事件上传到网络或采集 Prompt、代码、参数和完整路径。

## Decisions

### 统一核心，保留宿主适配层

`tools/log_tool_usage.py` 继续是唯一写入器，并增加 `init`、`doctor`、`report`。各编辑器只负责把原生 Hook 事件送入 `hook --source ...`。相比为每个编辑器维护独立统计脚本，这能保持协议、去重和隐私处理一致。

### 激活状态以配置、原生信任和实时证据分层表达

`doctor` 不使用一个可被误提交的布尔标记宣称“已激活”，而是分别报告：适配配置是否存在、宿主信任是否可验证、是否出现过非推断实时事件。相比单一标记，这能避免 Git 克隆后把别人的状态当成本机状态。

### 首次对话由项目规则触发预检

Hook 尚未获信任时不可能主动运行，因此首次提示由各编辑器会自动读取的项目规则触发。规则先运行只读 `doctor --editor <editor> --json`；若状态不足，暂停原任务并请求确认。确认后运行 `init`，需要宿主 UI 或重启时明确交还用户。不存在强制系统弹窗的通用机制。

### Codex 使用原生 app-server 精确信任

Codex 初始化通过 `codex app-server --stdio` 查询 `hooks/list`，只对来源路径等于当前仓库 `.codex/hooks.json` 的条目写入 `hooks.state.<key>.trusted_hash`，随后再次查询验证。该写入必须由显式 `--trust-codex-hooks` 开关触发。不会使用 `--dangerously-bypass-hook-trust`，也不会重写整个 `hooks.state`。

### 不可编程授权保留为宿主步骤

Claude Code、TRAE 等宿主若只提供安全提示或 UI Enable，`init` 只校验/准备项目配置并返回待授权说明。Cursor 的可信工作区与 Kiro 的命令权限仍由宿主控制。项目工具不伪造成功状态。

### TRAE 使用官方项目级 Hook 路径

TRAE 适配提交官方文档规定的 `$PROJECT_FOLDER/.trae/hooks.json`，命令明确使用来源 `trae`。用户仍需在 TRAE 的 Hook 安全提示或面板中点击 Enable；也保留官方的 Claude Code Hook 导入作为人工兼容路径。

## Risks / Trade-offs

- [编辑器 Hook 格式随版本变化] → `doctor` 校验必要字段，文档标注适配版本，并用配置夹具测试。
- [首次规则被用户或宿主禁用] → `doctor` 和 `init` 始终可由对话内 AI 调用，README 保留手动入口。
- [Codex app-server 协议变化] → 失败时不修改信任并输出原生手动审核路径；Hook 记录仍保持 fail-open。
- [已有项目配置包含其他 Hook] → 初始化只做结构化合并或验证，不整体覆盖已有数组；提交到模板的配置保留现有 Hook。
- [TRAE 不提供项目脚本可调用的授权接口] → 使用官方 `.trae/hooks.json`，但仍由宿主安全面板完成 Enable，不声称全自动。

## Migration Plan

1. 保留现有子命令和事件协议，先增加新 CLI 与测试。
2. 更新 Claude/Codex Hook，新增 Cursor/Kiro 配置和五端规则。
3. 用 `doctor` 验证当前项目，执行本地 Hook 事件冒烟测试。
4. 将框架级文件同步到模板仓库；旧项目仍可继续使用原有 `hook`/`record`。
5. 回滚时删除新增适配文件并恢复 Hook 配置；事件日志无需迁移。

## Open Questions

- 无。
