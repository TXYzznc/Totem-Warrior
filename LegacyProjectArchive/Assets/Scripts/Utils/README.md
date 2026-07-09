# Utils — 通用工具类

> 独立于框架和业务的通用工具类。

## 文件

| 文件 | 说明 |
|---|---|
| `StateMachine.cs` | 通用状态机（RegisterState / ChangeState / Update） |
| `IAsyncState.cs` | 异步状态接口 |
| `AsyncStateMachine.cs` | 通用异步状态机（EnterAsync / ExitAsync / Tick） |
| `StateMachineEvents.cs` | 异步状态机的通用事件 |
| `FlowPipeline.cs` | 可取消的顺序异步步骤流水线 |
| `FlowPipelineEvents.cs` | 流水线步骤进度、完成、失败事件 |
| `CompositeDisposable.cs` | 聚合 IDisposable 管理类（Add / Dispose，幂等） |

## 使用规则

- 不放和特定模块耦合的代码
- 不放和框架核心耦合的代码
- 工具类应当是无状态的（或状态极简）
