# Tasks: 实现框架核心脚本

> 11 个文件，约 780 行。按依赖顺序执行。

---

## Phase 1: 基础类型（4 个文件，~105 行）

### Task 1.1 — EventHandlerAttribute.cs
- **路径**：`Assets/Scripts/Core/EventHandlerAttribute.cs`
- **内容**：Attribute 定义，`[AttributeUsage(AttributeTargets.Method)]`
- **行数**：~8
- **依赖**：无
- **验证**：编译通过即可

### Task 1.2 — RequestHandlerAttribute.cs
- **路径**：`Assets/Scripts/Core/RequestHandlerAttribute.cs`
- **内容**：Attribute 定义，`[AttributeUsage(AttributeTargets.Method)]`
- **行数**：~8
- **依赖**：无
- **验证**：编译通过即可

### Task 1.3 — IGameModule.cs
- **路径**：`Assets/Scripts/Core/IGameModule.cs`
- **内容**：接口定义，4 个成员（ModuleCategory, Dependencies, InitAsync, ShutdownAsync）
- **行数**：~30
- **依赖**：无（需 UniTask 包引用）
- **规格**：
  - `ModuleCategory => 4`
  - `Dependencies => Type.EmptyTypes`
  - `InitializeAsync(CancellationToken ct = default)`
  - `ShutdownAsync(CancellationToken ct = default)`
- **验证**：编译通过

### Task 1.4 — FrameworkLogger.cs
- **路径**：`Assets/Scripts/Core/FrameworkLogger.cs`
- **内容**：4 个静态方法（Error/Warn/Info/Debug），`[CallerFilePath]` + `[CallerLineNumber]` 自动填充 Location
- **行数**：~50
- **依赖**：无
- **规格**：
  - 格式 `[模块名|级别] message Location=文件名:行号`
  - Error → `Debug.LogError`
  - Warn → `Debug.LogWarning`
  - Info/Debug → `Debug.Log`
- **注意**：委托异常日志必须由调用方在 message 中追加 `Handler=` 和 `Origin=`；`Location=` 只代表 logger 调用点
- **验证**：调用 Error/Warn/Info/Debug，检查 Unity Console 输出格式

---

## Phase 2: 事件系统（1 个文件，~180 行）

### Task 2.1 — EventBus.cs
- **路径**：`Assets/Scripts/Core/EventBus.cs`
- **依赖**：FrameworkLogger
- **行数**：~180
- **实现清单**：
  - [ ] 内部类 `EventSubscription` / `RequestSubscription` / `ManualSubscription`（owner, handler, isAsync, handlerName）
  - [ ] 内部存储：广播订阅表、请求响应订阅表、手动订阅列表、planned handler 诊断表
  - [ ] `Publish<T>(T evt)` — Fire-and-Forget，异常记 Error + 继续
  - [ ] `PublishAndWaitAsync<T>(T evt)` — WhenAll + AggregateException
  - [ ] `RequestAsync<TReq, TRep>(TReq, TRep defaultReply)` — 走请求响应订阅表，取第一个，0 个=defaultReply，>1 个=WARN
  - [ ] `Subscribe<T>(Func<T, UniTask>) → IDisposable` — 手动异步订阅
  - [ ] `Subscribe<T>(Action<T>) → IDisposable` — 手动同步订阅
  - [ ] `RegisterModuleHandlers(IGameModule)` — 反射扫描 [EventHandler]，签名校验
  - [ ] `RegisterModuleRequestHandlers(IGameModule)` — 反射扫描 [RequestHandler]，签名校验
  - [ ] `RegisterPlannedHandlers(IGameModule, descriptors)` — 未就绪诊断，不参与调用
  - [ ] `UnregisterModuleHandlers(IGameModule)` — 移除所有订阅
  - [ ] `GetDiagnosticReport() → string` — 按设计文档格式输出
  - [ ] `EnableDebugTrace` 属性
  - [ ] 精确类型匹配（`typeof(T)` 作为 key，不使用 `IsAssignableFrom`）
  - [ ] 所有异常场景的 FrameworkLogger 调用，委托异常包含 `Handler=` 和 `Origin=`
- **验证**：
  - 空 EventBus GetDiagnosticReport 不崩溃
  - Publish 无订阅者不崩溃
  - Subscribe + Publish 正常触发
  - Action 手动订阅 + Publish 正常触发
  - Publish 中 Handler 抛异常不中断其他 Handler
  - RequestAsync 无响应返回 defaultReply
  - RequestAsync 通过 [RequestHandler] 返回响应
  - RequestAsync 多响应记录 WARN 且稳定选择第一个

---

## Phase 3: 模块运行器（1 个文件，~250 行）

### Task 3.1 — ModuleRunner.cs
- **路径**：`Assets/Scripts/Core/ModuleRunner.cs`
- **依赖**：IGameModule, EventBus, FrameworkLogger
- **行数**：~250
- **实现清单**：
  - [ ] 内部枚举 `ModuleState { Pending, Initializing, Initialized, ShuttingDown, Shutdown, Failed }`
  - [ ] 内部数据结构（_modules, _moduleMap, _states, _initOrder, _outDegrees, _eventBus, _startupCts, _started, _stopping）
  - [ ] `AddModule(IGameModule)` — 注册 + 重复类型检查 + handler 预扫描
    - [ ] 不要求依赖已提前注册
    - [ ] 预扫描 [EventHandler]/[RequestHandler]，签名不符立即抛清晰异常
  - [ ] `ValidateGraph()` — StartAsync 前统一依赖校验 + 循环检测
    - [ ] 依赖校验：检查每个 dep 是否存在于最终注册集合（精确类型匹配）
    - [ ] 循环检测：Kahn 算法，存在环 → Error 日志 + 抛异常
  - [ ] `ComputePriority(IGameModule, outDegrees)` — `Category × 100 - OutDegree`
  - [ ] `StartAsync()` — WhenAny running pool + 持续扫描主循环
    - [ ] 每轮扫描就绪模块（Dependencies ⊆ {Initialized}）
    - [ ] 就绪集合按 ComputePriority 排序
    - [ ] MaxConcurrency 限流（0 = 不限）
    - [ ] 维护 running 字典，不使用 `await UniTask.WhenAll(ready...)` 作为批次屏障
    - [ ] `UniTask.WhenAny` 等待任意 running 初始化完成
    - [ ] 完成后标记 Initialized，加入 _initOrder
    - [ ] 调用 RegisterModuleHandlers / RegisterModuleRequestHandlers 自动注册
    - [ ] 初始化失败或超时 → Cancel running → 对已初始化模块反向 StopAsync → 重新抛异常
    - [ ] 循环直到全部 Initialized
  - [ ] `StopAsync()` — 逆序 Shutdown
    - [ ] 幂等：重复调用不重复关闭模块
    - [ ] 按 _initOrder 逆序遍历
    - [ ] ShutdownAsync 异常 → Error 日志 + 继续下一个
    - [ ] 调用 UnregisterModuleHandlers 注销事件
  - [ ] `GetModule<T>()` — 获取已初始化模块引用
    - [ ] 不存在 → KeyNotFoundException
    - [ ] 状态不是 `Initialized` → InvalidOperationException
  - [ ] 自动 [EventHandler] 扫描：
    - [ ] `BindingFlags.Public | NonPublic | Instance`
    - [ ] 签名校验：1 个参数，class 类型
    - [ ] 返回类型 UniTask → 异步，void → 同步
    - [ ] 签名不符 → 清晰的异常信息
  - [ ] 自动 [RequestHandler] 扫描：
    - [ ] 签名校验：1 个参数，class 类型
    - [ ] 返回类型 TReply 或 UniTask<TReply>
    - [ ] void / UniTask / 多参数 → 清晰的异常信息
  - [ ] 所有异常场景的 FrameworkLogger 调用（5 种场景，对应 5 种日志格式）
- **公开属性**：
  - [ ] `int MaxConcurrency { get; set; } = 0`
  - [ ] `float InitTimeoutSeconds { get; set; } = 0`
- **验证**：
  - 注册 0 个模块 → StartAsync 正常完成
  - 注册 1 个简单模块 → InitAsync 被调用 → GetModule 能拿到
  - 依赖模块可以乱序 AddModule，只要 StartAsync 前都注册即可
  - 注册有依赖的模块 → 顺序正确
  - 依赖缺失 → StartAsync/ValidateGraph 时抛异常
  - 循环依赖 → StartAsync/ValidateGraph 时抛异常
  - Resource 先完成时，只依赖 Resource 的 UI 不等待无关 Audio
  - 初始化失败 → 已初始化模块被反向 Shutdown
  - Shutdown → 逆序调用
  - [EventHandler] 自动注册 → EventBus 能收到事件
  - [RequestHandler] 自动注册 → RequestAsync 能收到响应

---

## Phase 4: 启动入口（1 个文件，~40 行）

### Task 4.1 — GameApp.cs
- **路径**：`Assets/Scripts/Core/GameApp.cs`
- **依赖**：所有 Core 组件
- **行数**：~40
- **实现清单**：
  - [ ] MonoBehaviour，非静态字段
  - [ ] `Start()` — try/catch 包裹 new EventBus → new ModuleRunner(_bus) → AddModule(示例) → StartAsync
  - [ ] `Start()` 失败 → FrameworkLogger.Error 并重新抛出
  - [ ] `OnDestroy()` — try/catch 包裹 StopAsync
- **验证**：挂载到 GameObject → Enter Play Mode → 不崩溃

---

## Phase 5: 工具类（2 个文件，~90 行）

### Task 5.1 — StateMachine.cs
- **路径**：`Assets/Scripts/Utils/StateMachine.cs`
- **依赖**：无
- **行数**：~60
- **实现**：通用泛型状态机（Register/ChangeState/Update）
- **验证**：编译通过

### Task 5.2 — CompositeDisposable.cs
- **路径**：`Assets/Scripts/Utils/CompositeDisposable.cs`
- **依赖**：FrameworkLogger（Warn 级别）
- **行数**：~30
- **实现**：聚合 IDisposable，Dispose 时有未取消项 → WARN
- **实现**：聚合 IDisposable，正常 Dispose 不记录 WARN；Add after Dispose 抛 ObjectDisposedException
- **验证**：Add 多个 Disposable → Dispose → 全部被 Dispose；重复 Dispose 无副作用

---

## Phase 6: 模板（2 个文件，~70 行）

### Task 6.1 — ModuleTemplate.cs.txt
- **路径**：`Assets/Scripts/Templates/ModuleTemplate.cs.txt`
- **依赖**：IGameModule
- **行数**：~50
- **内容**：完整 IGameModule 实现示例，含详细注释
  - ModuleCategory 选择指南
  - Dependencies 设置示例
  - InitAsync 标准流程
  - ShutdownAsync 清理流程
  - [EventHandler] 方法示例
  - [RequestHandler] 方法示例
  - CancellationToken 使用示例

### Task 6.2 — EventTemplate.cs.txt
- **路径**：`Assets/Scripts/Templates/EventTemplate.cs.txt`
- **依赖**：无
- **行数**：~20
- **内容**：事件类型定义模板，含命名规范注释

---

## 依赖图

```
Phase 1 ────→ Phase 2 ────→ Phase 3 ────→ Phase 4
  │                                           │
  └──→ Phase 5（可并行）                       │
  └──→ Phase 6（可并行）                       │
                                               │
                            Phase 5+6 ←────────┘
```

## 总览

| Phase | 文件数 | 估计行数 |
|---|---|---|
| 1 基础 | 4 | ~105 |
| 2 事件 | 1 | ~180 |
| 3 模块 | 1 | ~250 |
| 4 入口 | 1 | ~40 |
| 5 工具 | 2 | ~90 |
| 6 模板 | 2 | ~70 |
| **总计** | **11** | **~780** |
