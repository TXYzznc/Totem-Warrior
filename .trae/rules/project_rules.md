# AI 工具使用统计首次激活

每个新会话第一次处理用户任务前，先只读执行 `python tools/log_tool_usage.py doctor --editor trae --json`。

若 `editors.trae.active` 为 true，继续原任务且本会话不再检查。若为 false，暂停原任务，告诉用户本工具只在本地记录 Tool / SKILL / Agent / MCP 名称和会话元数据，不记录 Prompt、代码、参数、完整命令或完整路径，并询问是否启用。未明确确认前不得运行 init 或继续原任务。

确认后执行 `python tools/log_tool_usage.py init --editor trae --yes`，要求用户在 TRAE 的 Hook 安全提示或 Hook 面板中启用项目 `.trae/hooks.json`，然后重开会话并重新发送原任务。本轮不继续被暂停的任务。脚本不存在时直接继续原任务。
