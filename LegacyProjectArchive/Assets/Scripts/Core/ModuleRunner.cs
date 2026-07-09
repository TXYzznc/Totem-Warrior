using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

/// <summary>
/// 模块生命周期状态。
/// </summary>
public enum ModuleState
{
    Pending,
    Initializing,
    Initialized,
    ShuttingDown,
    Shutdown,
    Failed
}

/// <summary>
/// 模块生命周期管理。WhenAny + 持续扫描，最大化并行度。
/// </summary>
public class ModuleRunner
{
    // ========== 内部数据 ==========

    readonly List<IGameModule> _modules = new();
    readonly Dictionary<Type, IGameModule> _moduleMap = new();
    readonly Dictionary<Type, ModuleState> _states = new();
    readonly List<IGameModule> _initOrder = new();
    readonly Dictionary<Type, int> _outDegrees = new();
    readonly EventBus _eventBus;

    bool _started;
    bool _stopping;
    CancellationTokenSource _startupCts;

    // ========== 公开属性 ==========

    /// <summary>最大并发模块数。0 = 不限（默认）。</summary>
    public int MaxConcurrency { get; set; } = 0;

    /// <summary>单个模块初始化超时（秒）。0 = 不限（默认）。</summary>
    public float InitTimeoutSeconds { get; set; } = 0;

    // ========== 构造 ==========

    public ModuleRunner(EventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    // ========== 注册 ==========

    /// <summary>
    /// 添加模块。不要求依赖已提前注册，校验推迟到 ValidateGraph。
    /// 预扫描 [EventHandler]/[RequestHandler] 用于未就绪诊断。
    /// </summary>
    public void AddModule(IGameModule module)
    {
        if (module == null) throw new ArgumentNullException(nameof(module));
        var type = module.GetType();

        if (_moduleMap.ContainsKey(type))
            throw new InvalidOperationException($"模块 {type.Name} 已注册，不允许重复添加");

        _modules.Add(module);
        _moduleMap[type] = module;
        _states[type] = ModuleState.Pending;

        // 预扫描 handler 并注册到 EventBus 的 planned 诊断表
        var eventDescs = ScanEventHandlers(module);
        var requestDescs = ScanRequestHandlers(module);
        _eventBus.RegisterPlannedHandlers(type.Name, eventDescs, requestDescs);
    }

    // ========== 校验 ==========

    /// <summary>
    /// 依赖校验 + 循环依赖检测。StartAsync 前调用。
    /// </summary>
    public void ValidateGraph()
    {
        // 依赖校验：精确类型匹配
        foreach (var module in _modules)
        {
            foreach (var dep in module.Dependencies)
            {
                if (!_moduleMap.ContainsKey(dep))
                {
                    var msg = $"Module={module.GetType().Name} 依赖 {dep.Name} 未注册";
                    FrameworkLogger.Error("ModuleRunner", msg);
                    throw new InvalidOperationException(msg);
                }
            }
        }

        // 循环检测：Kahn 算法
        var inDegree = new Dictionary<Type, int>();
        var adj = new Dictionary<Type, List<Type>>();

        foreach (var m in _modules)
        {
            var t = m.GetType();
            if (!inDegree.ContainsKey(t)) inDegree[t] = 0;
            if (!adj.ContainsKey(t)) adj[t] = new List<Type>();
        }

        foreach (var m in _modules)
        {
            foreach (var dep in m.Dependencies)
            {
                adj[dep].Add(m.GetType());
                inDegree[m.GetType()] = inDegree.GetValueOrDefault(m.GetType()) + 1;
            }
        }

        var queue = new Queue<Type>();
        foreach (var kv in inDegree)
            if (kv.Value == 0) queue.Enqueue(kv.Key);

        int processed = 0;
        while (queue.Count > 0)
        {
            var t = queue.Dequeue();
            processed++;
            foreach (var next in adj[t])
            {
                inDegree[next]--;
                if (inDegree[next] == 0)
                    queue.Enqueue(next);
            }
        }

        if (processed != _modules.Count)
        {
            // 找出环中涉及的模块
            var cycleNodes = inDegree.Where(kv => kv.Value > 0).Select(kv => kv.Key.Name);
            var chain = string.Join("→", cycleNodes);
            var msg = $"循环依赖 Chain={chain}";
            FrameworkLogger.Error("ModuleRunner", msg);
            throw new InvalidOperationException(msg);
        }

        // 计算出度（在就绪排序中使用）
        ComputeOutDegrees();
    }

    void ComputeOutDegrees()
    {
        _outDegrees.Clear();
        foreach (var m in _modules)
        {
            var t = m.GetType();
            _outDegrees[t] = 0;
        }
        foreach (var m in _modules)
        {
            foreach (var dep in m.Dependencies)
            {
                if (_outDegrees.ContainsKey(dep))
                    _outDegrees[dep]++;
            }
        }
    }

    // ========== 启动 ==========

    /// <summary>
    /// 启动所有模块。WhenAny + 持续扫描，最大化并行。
    /// </summary>
    public async UniTask StartAsync()
    {
        if (_started)
            throw new InvalidOperationException("StartAsync 只能调用一次");

        // 校验（如果还未校验）
        ValidateGraph();

        _started = true;
        _startupCts = new CancellationTokenSource();

        var pending = new HashSet<Type>(_modules.Select(m => m.GetType()));
        // 用Task而非UniTask.Preserve()承载并发任务：Preserve()的MemoizeSource不支持
        // 在底层任务完成前被两次WhenAny注册continuation（会抛Already continuation registered），
        // 而Task原生支持多次await/续延注册，适配本循环"WhenAny+持续扫描"的重复轮询模式。
        var running = new Dictionary<Type, Task>();

        try
        {
            // 退出条件必须同时考虑 pending（待启动）与 running（已启动未标 Initialized）。
            // 仅看 pending 会导致最后一批 Task 同步完成时被 while 提前 break 而漏标 Initialized。
            while (pending.Count > 0 || running.Count > 0)
            {
                // 找出所有依赖满足的模块
                var ready = pending
                    .Where(t => _moduleMap[t].Dependencies.All(d => _states.GetValueOrDefault(d) == ModuleState.Initialized))
                    .Select(t => _moduleMap[t])
                    .ToList();

                // 如果当前无运行中的任务且有就绪模块 → 启动它们
                if (ready.Count > 0)
                {
                    ready.Sort((a, b) => ComputePriority(a).CompareTo(ComputePriority(b)));

                    if (MaxConcurrency > 0 && running.Count >= MaxConcurrency)
                    {
                        // 已达并发上限，等待至少一个完成
                        await Task.WhenAny(running.Values);
                        continue;
                    }

                    int slots = MaxConcurrency > 0 ? MaxConcurrency - running.Count : ready.Count;
                    for (int i = 0; i < Math.Min(slots, ready.Count); i++)
                    {
                        var module = ready[i];
                        var type = module.GetType();
                        pending.Remove(type);
                        _states[type] = ModuleState.Initializing;
                        FrameworkLogger.Info("ModuleRunner",
                            $"Module={type.Name} Status=Pending→Initializing");
                        running[type] = InitModuleAsync(module).AsTask();
                    }
                }

                if (running.Count == 0)
                {
                    if (pending.Count > 0)
                    {
                        // 有未完成模块但就绪集合为空 → 依赖不可解析
                        var remaining = string.Join(", ", pending.Select(t => t.Name));
                        throw new InvalidOperationException($"无法完成初始化，剩余模块依赖未满足: {remaining}");
                    }
                    break;
                }

                // 等待任意一个完成
                await Task.WhenAny(running.Values);
                // 找到完成的 task 对应的 type
                Type completedType = null;
                foreach (var kv in running)
                {
                    if (kv.Value.IsCompleted)
                    {
                        completedType = kv.Key;
                        break;
                    }
                }

                if (completedType != null)
                {
                    running.Remove(completedType);
                    // 成功完成的 → 注册 handler
                    if (_states[completedType] == ModuleState.Initializing)
                    {
                        _states[completedType] = ModuleState.Initialized;
                        var module = _moduleMap[completedType];
                        _initOrder.Add(module);
                        FrameworkLogger.Info("ModuleRunner",
                            $"Module={completedType.Name} Status=Initializing→Initialized");

                        // 注册 handler 到 EventBus
                        RegisterModuleToEventBus(module);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            FrameworkLogger.Error("ModuleRunner",
                $"启动失败 Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");

            // 取消正在运行的初始化任务
            _startupCts.Cancel();

            // 对已初始化的模块反向 Shutdown
            await ShutdownInitializedModules();

            throw;
        }
    }

    async UniTask InitModuleAsync(IGameModule module)
    {
        var type = module.GetType();
        try
        {
            UniTask task = module.InitializeAsync(_startupCts.Token);

            if (InitTimeoutSeconds > 0)
            {
                var result = await UniTask.WhenAny(task, UniTask.Delay(
                    TimeSpan.FromSeconds(InitTimeoutSeconds), cancellationToken: _startupCts.Token));

                if (result == 1) // timeout won
                {
                    FrameworkLogger.Warn("ModuleRunner",
                        $"Module={type.Name} InitAsync 超时 Elapsed={InitTimeoutSeconds}s Timeout={InitTimeoutSeconds}s");
                    throw new TimeoutException($"模块 {type.Name} 初始化超时 ({InitTimeoutSeconds}s)");
                }
            }

            await task;
        }
        catch (Exception ex)
        {
            _states[type] = ModuleState.Failed;
            FrameworkLogger.Error("ModuleRunner",
                $"Module={type.Name} InitAsync 异常 Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
            throw;
        }
    }

    // ========== 关闭 ==========

    /// <summary>
    /// 按初始化完成序的逆序关闭所有模块。幂等。
    /// </summary>
    public async UniTask StopAsync()
    {
        if (_stopping) return;
        _stopping = true;

        for (int i = _initOrder.Count - 1; i >= 0; i--)
        {
            var module = _initOrder[i];
            var type = module.GetType();
            var state = _states.GetValueOrDefault(type);

            if (state == ModuleState.ShuttingDown || state == ModuleState.Shutdown || state == ModuleState.Failed)
                continue;

            _states[type] = ModuleState.ShuttingDown;
            FrameworkLogger.Info("ModuleRunner", $"Module={type.Name} Status={state}→ShuttingDown");

            try
            {
                await module.ShutdownAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error("ModuleRunner",
                    $"Module={type.Name} ShutdownAsync 异常 Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
            }

            _eventBus.UnregisterModuleHandlers(module);
            _states[type] = ModuleState.Shutdown;
            FrameworkLogger.Info("ModuleRunner", $"Module={type.Name} Status=ShuttingDown→Shutdown");
        }

        _startupCts?.Dispose();
    }

    async UniTask ShutdownInitializedModules()
    {
        for (int i = _initOrder.Count - 1; i >= 0; i--)
        {
            var module = _initOrder[i];
            var type = module.GetType();
            if (_states[type] != ModuleState.Initialized) continue;

            try
            {
                await module.ShutdownAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error("ModuleRunner",
                    $"Module={type.Name} ShutdownAsync 异常 Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
            }

            _eventBus.UnregisterModuleHandlers(module);
        }
    }

    // ========== 查询 ==========

    /// <summary>
    /// 获取已初始化的模块引用（高频数据查询入口）。
    /// </summary>
    public T GetModule<T>() where T : IGameModule
    {
        var type = typeof(T);
        if (!_moduleMap.TryGetValue(type, out var module))
            throw new KeyNotFoundException($"模块 {type.Name} 未注册");

        var state = _states.GetValueOrDefault(type);
        if (state != ModuleState.Initialized)
            throw new InvalidOperationException($"模块 {type.Name} 状态为 {state}，只有 Initialized 才能获取");

        return (T)module;
    }

    /// <summary>所有已注册模块的只读视图（按 AddModule 顺序）。供 GameApp 注册 ITickable 等场景。</summary>
    public IReadOnlyList<IGameModule> GetAllModules() => _modules;

    /// <summary>诊断用：查询模块当前状态。</summary>
    public ModuleState GetState(Type type) => _states.GetValueOrDefault(type, ModuleState.Pending);

    // ========== 调度优先级 ==========

    /// <summary>
    /// 调度优先级 = ModuleCategory × 100 - OutDegree。值越小越优先。
    /// </summary>
    int ComputePriority(IGameModule module)
    {
        var type = module.GetType();
        var outDegree = _outDegrees.GetValueOrDefault(type, 0);
        return module.ModuleCategory * 100 - outDegree;
    }

    // ========== Handler 扫描（ModuleRunner 负责反射，EventBus 负责存储）==========

    List<EventBus.HandlerDescriptor> ScanEventHandlers(IGameModule module)
    {
        var list = new List<EventBus.HandlerDescriptor>();
        var type = module.GetType();

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (method.GetCustomAttribute<EventHandlerAttribute>() == null)
                continue;

            ValidateHandlerSignature(method);

            var paramType = method.GetParameters()[0].ParameterType;
            var isAsync = method.ReturnType == typeof(UniTask);
            var handler = isAsync
                ? Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(paramType, typeof(UniTask)), module, method)
                : Delegate.CreateDelegate(typeof(Action<>).MakeGenericType(paramType), module, method);

            list.Add(new EventBus.HandlerDescriptor
            {
                EventType = paramType,
                Handler = handler,
                HandlerName = $"{type.Name}.{method.Name}",
                IsAsync = isAsync
            });
        }
        return list;
    }

    List<EventBus.RequestHandlerDescriptor> ScanRequestHandlers(IGameModule module)
    {
        var list = new List<EventBus.RequestHandlerDescriptor>();
        var type = module.GetType();

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (method.GetCustomAttribute<RequestHandlerAttribute>() == null)
                continue;

            ValidateRequestHandlerSignature(method);

            var paramType = method.GetParameters()[0].ParameterType;
            var returnType = method.ReturnType;
            bool isAsync = returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(UniTask<>);
            var replyType = isAsync ? returnType.GetGenericArguments()[0] : returnType;

            var handler = isAsync
                ? Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(paramType, returnType), module, method)
                : Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(paramType, replyType), module, method);

            list.Add(new EventBus.RequestHandlerDescriptor
            {
                RequestType = paramType,
                ReplyType = replyType,
                Handler = handler,
                HandlerName = $"{type.Name}.{method.Name}",
                IsAsync = isAsync
            });
        }
        return list;
    }

    void RegisterModuleToEventBus(IGameModule module)
    {
        var eventDescs = ScanEventHandlers(module);
        if (eventDescs.Count > 0)
            _eventBus.RegisterModuleHandlers(module, eventDescs);

        var requestDescs = ScanRequestHandlers(module);
        if (requestDescs.Count > 0)
            _eventBus.RegisterModuleRequestHandlers(module, requestDescs);
    }

    // ========== 签名校验 ==========

    static void ValidateHandlerSignature(MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != 1)
            throw new InvalidOperationException(
                $"方法 {method.DeclaringType?.Name}.{method.Name} 签名不符：[EventHandler] 方法必须有且仅有一个参数，当前参数数量: {parameters.Length}");

        if (!parameters[0].ParameterType.IsClass)
            throw new InvalidOperationException(
                $"方法 {method.DeclaringType?.Name}.{method.Name} 签名不符：[EventHandler] 参数必须是引用类型");

        if (method.ReturnType != typeof(void) && method.ReturnType != typeof(UniTask))
            throw new InvalidOperationException(
                $"方法 {method.DeclaringType?.Name}.{method.Name} 签名不符：[EventHandler] 返回类型只能是 void 或 UniTask，当前: {method.ReturnType.Name}");
    }

    static void ValidateRequestHandlerSignature(MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != 1)
            throw new InvalidOperationException(
                $"方法 {method.DeclaringType?.Name}.{method.Name} 签名不符：[RequestHandler] 方法必须有且仅有一个参数，当前参数数量: {parameters.Length}");

        if (!parameters[0].ParameterType.IsClass)
            throw new InvalidOperationException(
                $"方法 {method.DeclaringType?.Name}.{method.Name} 签名不符：[RequestHandler] 参数必须是引用类型");

        var returnType = method.ReturnType;
        if (returnType == typeof(void) || returnType == typeof(UniTask))
            throw new InvalidOperationException(
                $"方法 {method.DeclaringType?.Name}.{method.Name} 签名不符：[RequestHandler] 必须有返回值 (TReply 或 UniTask<TReply>)，当前: {returnType.Name}");
    }
}