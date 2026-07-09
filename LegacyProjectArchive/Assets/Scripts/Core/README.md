# Core — 框架核心

> 框架的核心组件，项目通用，不可删除。

## 文件（7 个）

| 文件 | 说明 |
|---|---|
| `IGameModule.cs` | 模块接口（ModuleCategory / Dependencies / InitAsync / ShutdownAsync） |
| `ModuleRunner.cs` | 模块生命周期管理（WhenAny + 持续扫描 + 依赖校验 + 自动注册 Handler） |
| `EventBus.cs` | 事件系统（Publish / PublishAndWaitAsync / RequestAsync / Subscribe） |
| `FrameworkLogger.cs` | AI 友好型结构化日志（Error/Warn/Info/Debug + 自动 Location） |
| `EventHandlerAttribute.cs` | `[EventHandler]` 属性定义（广播事件处理器） |
| `RequestHandlerAttribute.cs` | `[RequestHandler]` 属性定义（请求响应处理器） |
| `GameApp.cs` | 启动入口 MonoBehaviour（创建 EventBus + ModuleRunner） |

## 核心架构

```
GameApp
  ├─ new EventBus()
  └─ new ModuleRunner(_bus)
       ├─ AddModule() → 预扫描 Handler → 注册 Planned
       ├─ ValidateGraph() → 依赖校验 + Kahn 循环检测
       └─ StartAsync() → WhenAny 持续扫描 + 自动注册 Handler
```

## 设计文档

详见 `AI友好型项目探讨/` 下的设计文档。