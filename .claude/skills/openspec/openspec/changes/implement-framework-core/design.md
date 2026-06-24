# Design: 框架核心实现（完整版）

> 基于所有设计文档的详细实现规格

---

## 架构总览

```
GameApp (MonoBehaviour)
  ├─ new EventBus()
  ├─ new ModuleRunner(_bus)
  │   ├─ AddModule() × N  → 依赖校验 + 循环检测
  │   └─ StartAsync()     → WhenAny + 持续扫描
  │       ├─ 运行池启动所有依赖满足的模块
  │       ├─ 完成后注册 [EventHandler]/[RequestHandler] 到 EventBus
  │       └─ 全部完成后游戏就绪
  └─ OnDestroy()  → StopAsync() → 逆序 Shutdown → 自动注销事件
```

---

## 1. Handler Attributes

```
Assets/Scripts/Core/EventHandlerAttribute.cs
约 8 行
```

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class EventHandlerAttribute : Attribute { }
```

```
Assets/Scripts/Core/RequestHandlerAttribute.cs
约 8 行
```

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class RequestHandlerAttribute : Attribute { }
```

两个 Attribute 都是纯标记：
- `[EventHandler]` 用于广播事件，支持 `void Handle(TEvent)` 和 `UniTask HandleAsync(TEvent)`
- `[RequestHandler]` 用于请求响应，支持 `TReply Handle(TRequest)` 和 `UniTask<TReply> HandleAsync(TRequest)`

`RequestAsync<TRequest, TReply>` 不复用 `[EventHandler]`，避免广播事件和单响应查询混在一张订阅表里。

---

## 2. IGameModule.cs

```
Assets/Scripts/Core/IGameModule.cs
约 30 行
```

接口定义，4 个成员全部有默认实现：

```csharp
public interface IGameModule
{
    int ModuleCategory => 4;        // 默认最低优先级
    Type[] Dependencies => Type.EmptyTypes;
    UniTask InitializeAsync(CancellationToken ct = default);
    UniTask ShutdownAsync(CancellationToken ct = default);
}
```

设计要点：
- ModuleCategory 默认 = 4（辅助系统），忘记设定的模块排到最后
- Dependencies 只接受具体 Module 类型，不支持接口
- InitAsync / ShutdownAsync 没有默认实现，模块必须显式编写
- `CancellationToken` 用于启动失败、超时、退出 Play Mode 时协作取消

---

## 3. FrameworkLogger.cs

```
Assets/Scripts/Core/FrameworkLogger.cs
约 50 行
```

4 个静态方法，使用 `[CallerFilePath]` + `[CallerLineNumber]` 自动填充 Location。

**方法签名：**
```csharp
public static class FrameworkLogger
{
    public static void Error(string module, string message,
        [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
    public static void Warn(string module, string message, ...);
    public static void Info(string module, string message, ...);
    public static void Debug(string module, string message, ...);
}
```

**输出格式：**
```
[module|LEVEL] message Location=file:line
```

**实现逻辑：**
1. 拼接 `$"[{module}|{level}] {message}"`
2. 追加 `$" Location={Path.GetFileName(file)}:{line}"`
3. 调用 Unity `Debug.Log` / `Debug.LogWarning` / `Debug.LogError`

**委托/反射调用异常的来源定位：**

`[CallerFilePath]` 只能定位到调用 `FrameworkLogger` 的框架代码行。EventBus 捕获模块 Handler 异常时，`Location=` 会指向 `EventBus.cs`，不能代表业务出错位置。因此委托异常日志必须额外包含：

```
Handler=<DeclaringType.Method>
Origin=<stackTraceFirstUserFrame>
```

实现要求：
1. `FrameworkLogger` 保持统一前缀和 `Location=` 字段
2. EventBus / ModuleRunner 捕获委托异常时，从 `Exception.StackTrace` 提取首个非 Core 框架栈帧作为 `Origin=`
3. 如果无法解析栈帧，至少输出 `Handler=<Type.Method>`，避免 AI 只能跳到 EventBus 内部

**各系统日志规范（在 EventBus 和 ModuleRunner 中调用）：**

EventBus 日志：
| 场景 | 调用的方法 | message 格式 |
|---|---|---|
| Fire-and-Forget 异常 | `Error("EventBus", ...)` | `Event=<T> Handler=<M.m> Exception=<T> Msg="..."` |
| RequestAsync 无响应 | `Warn("EventBus", ...)` | `Event=<T> RequestAsync 无响应者 DefaultReply=<V> Caller=<M.m>` |
| RequestAsync 多响应 | `Warn("EventBus", ...)` | `Event=<T> 多响应者(N): <list> 使用=<M> 忽略=<list>` |
| 模块未就绪发事件 | `Warn("EventBus", ...)` | `Event=<T> 发布时 N 个潜在订阅者未就绪: <list>` |
| Dispose 时未取消 | `Warn("EventBus", ...)` | `Subscription <T>→<M.m> Dispose 时仍有 N 个未消费事件` |
| 事件发布（调试） | `Debug("EventBus", ...)` | `Event=<T> 已发布 Subscribers=N` |
| 事件注册/注销 | `Info("EventBus", ...)` | `Handler=<M.m> Subscribe=<T>` / `Unsubscribe=<T>` |

ModuleRunner 日志：
| 场景 | 调用的方法 | message 格式 |
|---|---|---|
| InitAsync 异常 | `Error("ModuleRunner", ...)` | `Module=<M> InitAsync 异常 Exception=<T> Msg="..."` |
| InitAsync 超时 | `Warn("ModuleRunner", ...)` | `Module=<M> InitAsync 超时 Elapsed=<s>s Timeout=<s>s` |
| 循环依赖 | `Error("ModuleRunner", ...)` | `循环依赖 Chain=<M1>→<M2>→<M3>→<M1>` |
| 缺失依赖 | `Error("ModuleRunner", ...)` | `Module=<M> 依赖 <Missing> 未注册` |
| ShutdownAsync 异常 | `Error("ModuleRunner", ...)` | `Module=<M> ShutdownAsync 异常 Exception=<T> Msg="..."` |
| 模块状态变更 | `Info("ModuleRunner", ...)` | `Module=<M> Status=<Pending→Initialized→Shutdown>` |

---

## 4. EventBus.cs

```
Assets/Scripts/Core/EventBus.cs
约 180 行
```

**内部数据结构：**

```csharp
// 广播事件订阅
Dictionary<Type, List<EventSubscription>> _eventSubs;

// 请求响应订阅
Dictionary<Type, List<RequestSubscription>> _requestSubs;

// 手动订阅（返回 IDisposable，调用方管理生命周期）
List<ManualSubscription> _manualSubs;

// 计划订阅（ModuleRunner 预扫描后写入，用于未就绪诊断，不参与调用）
Dictionary<Type, List<PlannedHandler>> _plannedHandlers;

// 诊断
List<string> _recentErrors;      // 最近 10 条错误
int _totalPublished;             // 总发布次数
bool _enableDebugTrace;          // 调试追踪开关
```

**订阅内部类：**
```csharp
class EventSubscription
{
    public object Owner;              // IGameModule 实例
    public Delegate Handler;          // Func<T, UniTask> 或 Action<T>
    public bool IsAsync;              // true=UniTask, false=void
    public string HandlerName;        // Type.Method，用于日志
}

class RequestSubscription
{
    public object Owner;
    public Delegate Handler;          // Func<TReq, UniTask<TRep>> 或 Func<TReq, TRep>
    public Type ReplyType;
    public bool IsAsync;
    public string HandlerName;
}
```

**API 实现规格：**

### Publish\<T\>(T evt)
```
1. 从 _eventSubs 和 _manualSubs 查找 typeof(T) 的所有订阅者
2. 遍历每个订阅者：
   a. 如果 IsAsync → 调用 Func<T, UniTask>，用 .Forget() 不等待
   b. 如果 !IsAsync → 调用 Action<T>
   c. 如果抛异常 → FrameworkLogger.Error（含 Handler/Origin）+ 继续执行后续订阅者
3. 如果 EnableDebugTrace → FrameworkLogger.Debug 记录发布
4. 如果订阅者数 = 0 且 EnableDebugTrace → FrameworkLogger.Debug 记录"无订阅者"
5. 如果 _plannedHandlers 中存在未就绪 handler → FrameworkLogger.Warn 记录时序风险
```

### PublishAndWaitAsync\<T\>(T evt)
```
1. 收集所有 UniTask，同步 handler 立即执行
2. await UniTask.WhenAll(tasks)
3. 如果任何 task 抛异常 → 收集所有异常，抛出 AggregateException
4. 每个异常同时记 FrameworkLogger.Error
```

### RequestAsync\<TRequest, TReply\>(TRequest, TReply defaultReply)
```
1. 查找 _requestSubs[typeof(TRequest)] 中 ReplyType == typeof(TReply) 的订阅者
2. 如果 count = 0：
   a. FrameworkLogger.Warn("EventBus", "Event=<T> RequestAsync 无响应者 DefaultReply=<V>")
   b. 返回 defaultReply
3. 如果 count > 1：
   a. FrameworkLogger.Warn("EventBus", "Event=<T> 多响应者(N): <list> 使用=<M> 忽略=<list>")
4. 调用第一个订阅者：
   a. 同步 handler：Func<TRequest, TReply>
   b. 异步 handler：Func<TRequest, UniTask<TReply>>
5. 如果 handler 抛异常 → FrameworkLogger.Error（含 Handler/Origin）并重新抛出
```

### Subscribe\<T\>(Func\<T, UniTask\> handler) / Subscribe\<T\>(Action\<T\> handler) → IDisposable
```
1. 创建 ManualSubscription，加入 _manualSubs
2. 返回 IDisposable，Dispose 时从 _manualSubs 移除
3. 正常 Dispose 不记录 WARN；这是 UI 临时订阅的预期用法
4. FrameworkLogger.Info("EventBus", "手动订阅 Subscribe=<T>")
```

### RegisterModuleHandlers(IGameModule module)
```
由 ModuleRunner 调用，不在 EventBus 的公开 API 中。
1. 接收 ModuleRunner 预扫描得到的 [EventHandler] descriptor
2. 创建 Delegate（同步用 Action<T>，异步用 Func<T, UniTask>）
3. 加入 _eventSubs[eventType]
4. FrameworkLogger.Info("EventBus", "Handler=<M.m> Subscribe=<T>")
```

### RegisterModuleRequestHandlers(IGameModule module)
```
由 ModuleRunner 调用，不在 EventBus 的公开 API 中。
1. 接收 ModuleRunner 预扫描得到的 [RequestHandler] descriptor
2. 创建 Delegate（同步用 Func<TReq, TRep>，异步用 Func<TReq, UniTask<TRep>>）
3. 加入 _requestSubs[requestType]
4. FrameworkLogger.Info("EventBus", "Handler=<M.m> Request=<TReq> Reply=<TRep>")
```

### UnregisterModuleHandlers(IGameModule module)
```
由 ModuleRunner 调用。
1. 遍历 _eventSubs / _requestSubs / _manualSubs，移除所有 owner == module 的订阅
2. FrameworkLogger.Info("EventBus", "Handler=<M> 已注销所有订阅")
```

### GetDiagnosticReport() → string
```
输出格式（严格按设计文档）：
=== EventBus 诊断报告 ===
注册事件类型: {count}
活跃订阅总数: {total}
手动订阅未消费: {unconsumed}

最近错误 (最新 10):
  [ERROR] CombatEndEvent → CombatModule.OnCombatEnd: NullReferenceException @CombatModule.cs:142
  ...

当前订阅明细:
  CombatEndEvent (3): CombatModule.OnCombatEnd, UIModule.ShowResult, QuestModule.OnCombatEnd
  ...

未就绪订阅者:
  QuestModule.Status=Pending → 订阅了 CombatEndEvent
```

### EnableDebugTrace
```
public bool EnableDebugTrace { get; set; } = false;
开启后记录所有 Publish/Subscribe/Unsubscribe
```

**设计约束（EventBus 自身）：**
- 不支持事件继承（精确类型匹配 `typeof(T)`，不使用 `IsAssignableFrom`）
- 不保证处理顺序（多个订阅者时）
- RequestAsync 多响应者 → 取第一个，WARN 记录被忽略的

---

## 5. ModuleRunner.cs

```
Assets/Scripts/Core/ModuleRunner.cs
约 250 行
```

**内部数据结构：**
```csharp
List<IGameModule> _modules;                    // 所有已注册模块
Dictionary<Type, IGameModule> _moduleMap;      // Type → 实例（GetModule<T> 用）
Dictionary<Type, ModuleState> _states;         // Type → 状态
List<Type> _initOrder;                         // 初始化完成顺序（Shutdown 逆序用）
Dictionary<Type, int> _outDegrees;             // 出度（ComputePriority 用）
EventBus _eventBus;                            // 注入的 EventBus
CancellationTokenSource _startupCts;           // 启动失败/退出时协作取消
bool _started;
bool _stopping;
```

**ModuleState 枚举：**
```csharp
enum ModuleState { Pending, Initializing, Initialized, ShuttingDown, Shutdown, Failed }
```

**API 实现规格：**

### AddModule(IGameModule module)
```
1. 加入 _modules 和 _moduleMap
2. 如果同一具体类型重复注册 → 抛出 InvalidOperationException
3. _states[type] = Pending
4. 预扫描 [EventHandler]/[RequestHandler] 的签名和计划订阅，供诊断报告使用
5. 不要求依赖已提前注册；依赖校验在 StartAsync 前统一执行，避免注册顺序变成隐式依赖
```

### ValidateGraph()（内部）
```
1. 遍历所有 module.Dependencies，检查每个 dep 是否存在于 _moduleMap
   - 未注册 → FrameworkLogger.Error("ModuleRunner", "Module=<M> 依赖 <Missing> 未注册")
   - 抛出 InvalidOperationException
2. 在所有已注册模块上运行 Kahn 算法
   - 存在环 → FrameworkLogger.Error("ModuleRunner", "循环依赖 Chain=<...>")
   - 抛出 InvalidOperationException
3. 计算并缓存 _outDegrees
```

### StartAsync()
```
算法：
1. 如果 _started == true → 直接返回或抛出清晰异常（二选一，建议直接返回）
2. ValidateGraph()
3. 创建 _startupCts
4. running = Dictionary<Type, UniTask<InitResult>>
5. while 仍有 Pending 或 running 非空：
   a. availableSlots = MaxConcurrency == 0 ? int.MaxValue : MaxConcurrency - running.Count
   b. 计算 ready = {Pending 且 Dependencies 全部 state == Initialized}
   c. ready 按 ComputePriority 排序，取 availableSlots 个
   d. 对每个 selected：
      - state = Initializing
      - running[type] = InitModuleAsync(module, _startupCts.Token)
   e. 如果 running 为空且仍有 Pending → 抛出不可解析依赖异常
   f. await UniTask.WhenAny(running.Values)
   g. 找到完成的 task，移出 running
   h. 成功：
      - state = Initialized
      - _initOrder.Add(type)
      - EventBus.RegisterModuleHandlers(module)
      - EventBus.RegisterModuleRequestHandlers(module)
   i. 失败：
      - state = Failed
      - _startupCts.Cancel()
      - 等待或取消 remaining running，避免后台初始化继续污染状态
      - 对 _initOrder 中已完成模块调用 StopAsync()
      - 重新抛出原始异常
6. _started = true
```

### InitModuleAsync(IGameModule module)（内部）
```
1. 如果有 InitTimeoutSeconds > 0：
   → 用 UniTask.Timeout 包裹 module.InitializeAsync(ct)
2. 正常调用 → await module.InitializeAsync(ct)
3. 如果抛异常：
   → FrameworkLogger.Error("ModuleRunner", "Module=<M> InitAsync 异常 Exception=<T> Msg=...")
   → 终止整个启动（重新抛出异常，不跳过）
4. 如果超时：
   → FrameworkLogger.Warn("ModuleRunner", "Module=<M> InitAsync 超时 Elapsed=...")
   → 终止整个启动
```

### StopAsync()
```
1. 如果 _stopping == true 或所有模块已 Shutdown → 直接返回
2. _stopping = true
3. _startupCts?.Cancel()
4. 按 _initOrder 的逆序遍历
5. foreach module（逆序）：
   a. 如果 state != Initialized → 跳过
   b. state = ShuttingDown
   c. await module.ShutdownAsync(CancellationToken.None)
   d. 如果 ShutdownAsync 抛异常：
      → FrameworkLogger.Error(...) + 继续处理剩余模块
   e. UnregisterModuleHandlers(module) → 从 EventBus 移除该模块的所有订阅
   f. _states[type] = Shutdown
   g. FrameworkLogger.Info(...) 记录状态变更
6. 清空 _initOrder，_started = false，_stopping = false
```

### GetModule\<T\>() → T
```
1. 如果 _moduleMap 不存在 typeof(T) → 抛出 KeyNotFoundException
2. 如果 _states[typeof(T)] != ModuleState.Initialized → 抛出 InvalidOperationException
3. 返回对应实例
```

### ComputePriority(IGameModule module, Dictionary<Type, int> outDegrees)
```
return module.ModuleCategory * 100 - outDegrees.GetValueOrDefault(module.GetType(), 0);
```

### 注册阶段的事件相关操作（内部）
```
ScanModuleHandlers(module)（AddModule 阶段预扫描）:
  1. 反射 module.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
  2. 筛选有 [EventHandler] / [RequestHandler] 的方法
  3. 每个 [EventHandler] 方法：
     a. 校验：参数数量 == 1 && 参数类型 is class
        不满足 → 抛出清晰的异常："方法 X 签名不符：[EventHandler] 方法必须有且仅有一个引用类型参数"
     b. 参数类型 = eventType
     c. 返回类型 == UniTask → 异步，返回类型 == void → 同步
     d. 返回类型不符 → 抛出清晰异常
  4. 每个 [RequestHandler] 方法：
     a. 参数数量 == 1 && 参数类型 is class
     b. 返回类型 == TReply 或 UniTask<TReply>
     c. 返回 void / UniTask / 参数不符 → 抛出清晰异常
  5. 把合法 handler descriptor 交给 EventBus 作为 planned handler，用于未就绪诊断

RegisterModuleHandlers(module)（初始化完成后）:
  1. 使用预扫描结果创建 Delegate
  2. 注册 [EventHandler] 到 EventBus event subscriptions
  3. 注册 [RequestHandler] 到 EventBus request subscriptions
  4. FrameworkLogger.Info(...)

UnregisterModuleHandlers(module):
  1. 调用 EventBus 的内部方法移除所有该模块的订阅
  2. FrameworkLogger.Info(...)
```

---

## 6. GameApp.cs

```
Assets/Scripts/Core/GameApp.cs
约 40 行
```

```csharp
public class GameApp : MonoBehaviour
{
    EventBus _bus;           // 非静态，Domain Reload 后重建
    ModuleRunner _runner;    // 非静态

    async void Start()
    {
        try
        {
            _bus = new EventBus();
            _runner = new ModuleRunner(_bus);

            // 示例注册（实际项目中按需添加）
            // _runner.AddModule(new ConfigModule(_bus, _runner));
            // _runner.AddModule(new ResourceModule(_bus, _runner));
            // ...

            await _runner.StartAsync();
            FrameworkLogger.Info("GameApp", "所有模块初始化完成，游戏就绪");
        }
        catch (Exception ex)
        {
            FrameworkLogger.Error("GameApp", $"启动失败 Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
            throw;
        }
    }

    async void OnDestroy()
    {
        try
        {
            if (_runner != null)
                await _runner.StopAsync();
        }
        catch (Exception ex)
        {
            FrameworkLogger.Error("GameApp", $"关闭失败 Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
        }
    }
}
```

设计要点：
- `_bus` 和 `_runner` 是实例字段，不是静态单例
- Domain Reload 后 `Start()` 重新调用，全新创建
- 模块通过构造函数注入 `_bus` 和 `_runner`
- `OnDestroy` 时调用 `StopAsync()` 逆序关闭
- `Start()` 必须显式记录启动异常，避免 `async void` 异常在 Unity 中难以追踪
- `StopAsync()` 自身必须幂等，避免 OnDestroy / 启动失败回滚重复关闭同一模块

---

## 7. StateMachine.cs

```
Assets/Scripts/Utils/StateMachine.cs
约 60 行
```

```csharp
public class StateMachine<TState> where TState : Enum
{
    public TState CurrentState { get; private set; }
    Dictionary<TState, Action> _enterCallbacks;
    Dictionary<TState, Action> _updateCallbacks;
    Dictionary<TState, Action> _exitCallbacks;

    public void RegisterState(TState state, Action onEnter, Action onUpdate, Action onExit);
    public void ChangeState(TState newState);  // Exit old → Enter new
    public void Update();                       // 调用当前状态的 Update
}
```

---

## 8. CompositeDisposable.cs

```
Assets/Scripts/Utils/CompositeDisposable.cs
约 30 行
```

```csharp
public class CompositeDisposable : IDisposable
{
    List<IDisposable> _list = new();
    bool _disposed;

    public void Add(IDisposable d)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(CompositeDisposable));
        _list.Add(d);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var d in _list) d.Dispose();
        _list.Clear();
    }
}
```

正常 `Dispose()` 是 UI 临时订阅的预期清理路径，不应记录 WARN。泄漏诊断由 EventBus 的活跃手动订阅统计负责。

---

## 9. ModuleTemplate.cs

```
Assets/Scripts/Templates/ModuleTemplate.cs.txt
约 50 行
```

一个完整的 IGameModule 实现示例，含详细注释说明每个部分的作用。必须是 `.cs.txt` 或 `.md`，不能作为 Unity 运行时代码参与编译。

应包括：
- ModuleCategory 选择说明
- Dependencies 声明示例
- InitAsync 中的初始化流程（加载配置 → 持有其他模块引用 → 订阅事件）
- ShutdownAsync 中的清理流程
- [EventHandler] 方法示例（同步 + 异步）

---

## 10. EventTemplate.cs

```
Assets/Scripts/Templates/EventTemplate.cs.txt
约 20 行
```

事件类型定义模板，含详细注释。应包括：
- 事件类的结构（不可变数据）
- 命名规范说明
- 何时放在 Events/ 目录 vs 模块内部 Events/
