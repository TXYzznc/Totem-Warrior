# ExternalAPI — 对外 API

> 暴露给外部工具（MCP Server 等）调用的 API 端点。

## 目录

| 目录/文件 | 说明 |
|---|---|
| `APIEndpoint.cs` | 路由注册 |
| `Handlers/` | 具体的 API 处理器 |

## 设计意图

- 供 MCP Server 调用，实现 AI 与游戏运行时的交互
- 通过 HTTP 或 WebSocket 暴露接口
- 查询游戏状态、发送指令等