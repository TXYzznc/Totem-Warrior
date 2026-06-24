using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;

/// <summary>
/// 模块间事件通信系统。Publish 负责通知，GetModule负责查询。
/// </summary>
public class EventBus
{
    // ========== 内部数据结构 ==========

    readonly Dictionary<Type, List<EventSubscription>> _eventSubs = new();
    readonly Dictionary<Type, List<RequestSubscription>> _requestSubs = new();
    readonly List<ManualSubscription> _manualSubs = new();
    readonly List<string> _recentErrors = new();
    bool _enableDebugTrace;

    /// <summary>
    /// 未就绪模块的预扫描 handler 记录，仅用于 GetDiagnosticReport 诊断，不参与调用。
    /// </summary>
    readonly Dictionary<Type, List<PlannedHandler>> _plannedHandlers = new();

    // ========== 内部类型 ==========

    class EventSubscription
    {
        public object Owner;
        public Delegate Handler;
        public bool IsAsync;
        public string HandlerName;
    }

    class RequestSubscription : EventSubscription
    {
        public Type ReplyType;
    }

    class ManualSubscription : IDisposable
    {
        public Type EventType;
        public Delegate Handler;
        public Action<ManualSubscription> OnDispose;
        public void Dispose() => OnDispose?.Invoke(this);
    }

    /// <summary>
    /// ModuleRunner 预扫描的 handler 描述符。
    /// </summary>
    public struct HandlerDescriptor
    {
        public Type EventType;
        public Delegate Handler;
        public string HandlerName;
        public bool IsAsync;
    }

    /// <summary>
    /// ModuleRunner 预扫描的 request handler 描述符。
    /// </summary>
    public struct RequestHandlerDescriptor
    {
        public Type RequestType;
        public Type ReplyType;
        public Delegate Handler;
        public string HandlerName;
        public bool IsAsync;
    }

    class PlannedHandler
    {
        public string ModuleTypeName;
        public string HandlerName;
    }

    // ========== 公开属性 ==========

    /// <summary>开启调试追踪，记录所有发布/订阅（默认关闭）。</summary>
    public bool EnableDebugTrace
    {
        get => _enableDebugTrace;
        set => _enableDebugTrace = value;
    }

    // ========== 广播：Fire-and-Forget ==========

    /// <summary>
    /// 发布事件，立即返回。订阅者异常记 ERROR 日志，不中断其他订阅者。
    /// </summary>
    public void Publish<T>(T evt)
    {
        var type = typeof(T);
        int subscriberCount = 0;

        // 模块订阅者
        if (_eventSubs.TryGetValue(type, out var subs))
        {
            foreach (var sub in subs)
            {
                InvokeSubscriber(sub, evt);
                subscriberCount++;
            }
        }

        // 手动订阅者
        foreach (var manual in _manualSubs.Where(m => m.EventType == type))
        {
            InvokeManualSubscriber(manual, evt);
            subscriberCount++;
        }

        if (_enableDebugTrace)
        {
            FrameworkLogger.Debug("EventBus", $"Event={type.Name} 已发布 Subscribers={subscriberCount}");
        }

        // 已计划但未就绪的订阅者 → WARN
        if (_plannedHandlers.TryGetValue(type, out var planned) && planned.Count > 0)
        {
            var names = string.Join(", ", planned.Select(p => $"{p.ModuleTypeName}.{p.HandlerName}"));
            FrameworkLogger.Warn("EventBus",
                $"Event={type.Name} 发布时 {planned.Count} 个潜在订阅者未就绪: {names}");
        }
    }

    // ========== 广播：等待全部 ==========

    /// <summary>
    /// 发布事件并等待所有订阅者处理完。异常聚合成 AggregateException。
    /// </summary>
    public async UniTask PublishAndWaitAsync<T>(T evt)
    {
        var type = typeof(T);
        var tasks = new List<UniTask>();
        var exceptions = new List<Exception>();

        if (_eventSubs.TryGetValue(type, out var subs))
        {
            foreach (var sub in subs)
            {
                tasks.Add(InvokeSubscriberAsync(sub, evt, exceptions));
            }
        }

        foreach (var manual in _manualSubs.Where(m => m.EventType == type))
        {
            tasks.Add(InvokeManualSubscriberAsync(manual, evt, exceptions));
        }

        await UniTask.WhenAll(tasks);

        if (exceptions.Count > 0)
            throw new AggregateException(exceptions);
    }

    // ========== 请求-响应 ==========

    /// <summary>
    /// 发送请求并等待第一个应答者。无响应者返回 defaultReply 并记 WARN。多响应者取第一个并记 WARN。
    /// </summary>
    public async UniTask<TReply> RequestAsync<TRequest, TReply>(TRequest req, TReply defaultReply = default)
    {
        var type = typeof(TRequest);
        var replyType = typeof(TReply);

        if (!_requestSubs.TryGetValue(type, out var subs) || subs.Count == 0)
        {
            FrameworkLogger.Warn("EventBus",
                $"Event={type.Name} RequestAsync 无响应者 DefaultReply={defaultReply}");
            return defaultReply;
        }

        var matches = subs.Where(s => s.ReplyType == replyType).ToList();
        if (matches.Count == 0)
        {
            FrameworkLogger.Warn("EventBus",
                $"Event={type.Name} RequestAsync 无匹配 ReplyType={replyType.Name} 的响应者 DefaultReply={defaultReply}");
            return defaultReply;
        }

        if (matches.Count > 1)
        {
            var ignored = string.Join(",", matches.Skip(1).Select(m => m.HandlerName));
            FrameworkLogger.Warn("EventBus",
                $"Event={type.Name} 多响应者({matches.Count}): {string.Join(", ", matches.Select(m => m.HandlerName))} 使用={matches[0].HandlerName} 忽略={ignored}");
        }

        var sub = matches[0];
        try
        {
            if (sub.IsAsync)
            {
                var task = (UniTask<TReply>)sub.Handler.DynamicInvoke(req);
                return await task;
            }
            else
            {
                return (TReply)sub.Handler.DynamicInvoke(req);
            }
        }
        catch (Exception ex)
        {
            var origin = ExtractOrigin(ex);
            FrameworkLogger.Error("EventBus",
                $"Event={type.Name} Handler={sub.HandlerName} Exception={ex.GetType().Name} Msg=\"{ex.Message}\" Origin={origin}");
            throw;
        }
    }

    // ========== 手动订阅 ==========

    /// <summary>手动异步订阅（非 Module 场景，如临时对象）。返回 IDisposable 用于取消。</summary>
    public IDisposable Subscribe<T>(Func<T, UniTask> handler)
    {
        var sub = new ManualSubscription
        {
            EventType = typeof(T),
            Handler = handler,
            OnDispose = RemoveManual
        };
        _manualSubs.Add(sub);

        if (_enableDebugTrace)
            FrameworkLogger.Debug("EventBus", $"手动订阅 Subscribe<{typeof(T).Name}>");

        return sub;
    }

    /// <summary>手动同步订阅（非 Module 场景）。返回 IDisposable 用于取消。</summary>
    public IDisposable Subscribe<T>(Action<T> handler)
    {
        var sub = new ManualSubscription
        {
            EventType = typeof(T),
            Handler = handler,
            OnDispose = RemoveManual
        };
        _manualSubs.Add(sub);
        return sub;
    }

    void RemoveManual(ManualSubscription sub)
    {
        _manualSubs.Remove(sub);
    }

    void RemovePlannedHandler(Type eventType, string moduleTypeName, string handlerName)
    {
        if (!_plannedHandlers.TryGetValue(eventType, out var planned))
            return;

        planned.RemoveAll(p => p.ModuleTypeName == moduleTypeName && p.HandlerName == handlerName);
        if (planned.Count == 0)
            _plannedHandlers.Remove(eventType);
    }

    // ========== ModuleRunner 调用的注册/注销方法 ==========

    /// <summary>
    /// [ModuleRunner] 注册模块的广播事件处理器。
    /// </summary>
    public void RegisterModuleHandlers(IGameModule module, List<HandlerDescriptor> descriptors)
    {
        foreach (var d in descriptors)
        {
            if (!_eventSubs.ContainsKey(d.EventType))
                _eventSubs[d.EventType] = new List<EventSubscription>();

            _eventSubs[d.EventType].Add(new EventSubscription
            {
                Owner = module,
                Handler = d.Handler,
                IsAsync = d.IsAsync,
                HandlerName = d.HandlerName
            });

            RemovePlannedHandler(d.EventType, module.GetType().Name, d.HandlerName);
            FrameworkLogger.Info("EventBus", $"Handler={d.HandlerName} Subscribe={d.EventType.Name}");
        }
    }

    /// <summary>
    /// [ModuleRunner] 注册模块的请求响应处理器。
    /// </summary>
    public void RegisterModuleRequestHandlers(IGameModule module, List<RequestHandlerDescriptor> descriptors)
    {
        foreach (var d in descriptors)
        {
            if (!_requestSubs.ContainsKey(d.RequestType))
                _requestSubs[d.RequestType] = new List<RequestSubscription>();

            _requestSubs[d.RequestType].Add(new RequestSubscription
            {
                Owner = module,
                Handler = d.Handler,
                IsAsync = d.IsAsync,
                ReplyType = d.ReplyType,
                HandlerName = d.HandlerName
            });

            RemovePlannedHandler(d.RequestType, module.GetType().Name, d.HandlerName);
            FrameworkLogger.Info("EventBus", $"Handler={d.HandlerName} RequestSubscribe={d.RequestType.Name}");
        }
    }

    /// <summary>
    /// [ModuleRunner] 记录模块预扫描到的 handler 计划，用于未就绪诊断。
    /// </summary>
    public void RegisterPlannedHandlers(string moduleTypeName, List<HandlerDescriptor> eventDescriptors,
        List<RequestHandlerDescriptor> requestDescriptors)
    {
        foreach (var d in eventDescriptors)
        {
            if (!_plannedHandlers.ContainsKey(d.EventType))
                _plannedHandlers[d.EventType] = new List<PlannedHandler>();

            _plannedHandlers[d.EventType].Add(new PlannedHandler
            {
                ModuleTypeName = moduleTypeName,
                HandlerName = d.HandlerName
            });
        }

        foreach (var d in requestDescriptors)
        {
            if (!_plannedHandlers.ContainsKey(d.RequestType))
                _plannedHandlers[d.RequestType] = new List<PlannedHandler>();

            _plannedHandlers[d.RequestType].Add(new PlannedHandler
            {
                ModuleTypeName = moduleTypeName,
                HandlerName = d.HandlerName
            });
        }
    }

    /// <summary>
    /// [ModuleRunner] 移除模块的所有订阅（Shutdown 时调用）。
    /// </summary>
    public void UnregisterModuleHandlers(IGameModule module)
    {
        foreach (var kv in _eventSubs)
            kv.Value.RemoveAll(s => s.Owner == module);

        foreach (var kv in _requestSubs)
            kv.Value.RemoveAll(s => s.Owner == module);

        FrameworkLogger.Info("EventBus", $"Handler={module.GetType().Name} 已注销所有订阅");
    }

    // ========== 诊断 ==========

    /// <summary>
    /// AI 可调用的全局状态快照。
    /// </summary>
    public string GetDiagnosticReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== EventBus 诊断报告 ===");

        int eventTypes = _eventSubs.Count(kv => kv.Value.Count > 0);
        int requestTypes = _requestSubs.Count(kv => kv.Value.Count > 0);
        int totalSubs = _eventSubs.Sum(kv => kv.Value.Count) + _requestSubs.Sum(kv => kv.Value.Count);
        sb.AppendLine($"注册事件类型: {eventTypes}");
        sb.AppendLine($"注册请求类型: {requestTypes}");
        sb.AppendLine($"活跃订阅总数: {totalSubs}");
        sb.AppendLine($"手动订阅活跃: {_manualSubs.Count}");
        sb.AppendLine();

        sb.AppendLine($"最近错误 (最新 {Math.Min(_recentErrors.Count, 10)}):");
        if (_recentErrors.Count == 0)
            sb.AppendLine("  (无)");
        else
            foreach (var err in _recentErrors.TakeLast(10).Reverse())
                sb.AppendLine($"  {err}");
        sb.AppendLine();

        sb.AppendLine("当前广播订阅明细:");
        foreach (var kv in _eventSubs.Where(kv => kv.Value.Count > 0))
        {
            var names = string.Join(", ", kv.Value.Select(s => s.HandlerName));
            sb.AppendLine($"  {kv.Key.Name} ({kv.Value.Count}): {names}");
        }
        sb.AppendLine();

        sb.AppendLine("当前请求订阅明细:");
        foreach (var kv in _requestSubs.Where(kv => kv.Value.Count > 0))
        {
            var names = string.Join(", ", kv.Value.Select(s => $"{s.HandlerName}→{s.ReplyType.Name}"));
            sb.AppendLine($"  {kv.Key.Name} ({kv.Value.Count}): {names}");
        }
        sb.AppendLine();

        sb.AppendLine("未就绪订阅者:");
        bool hasPlanned = false;
        foreach (var kv in _plannedHandlers.Where(kv => kv.Value.Count > 0))
        {
            // 检查是否已有实际订阅（模块可能已就绪）
            bool alreadyReady =
                (_eventSubs.ContainsKey(kv.Key) && _eventSubs[kv.Key].Count > 0) ||
                (_requestSubs.ContainsKey(kv.Key) && _requestSubs[kv.Key].Count > 0);
            if (alreadyReady) continue;

            hasPlanned = true;
            var names = string.Join(", ", kv.Value.Select(p => $"{p.ModuleTypeName}.{p.HandlerName}"));
            sb.AppendLine($"  {kv.Key.Name}: {names}");
        }
        if (!hasPlanned)
            sb.AppendLine("  (无)");

        return sb.ToString();
    }

    // ========== 内部方法 ==========

    void InvokeSubscriber<T>(EventSubscription sub, T evt)
    {
        try
        {
            if (sub.IsAsync)
            {
                var task = (UniTask)sub.Handler.DynamicInvoke(evt);
                HandleFireAndForgetAsync(task, typeof(T).Name, sub.HandlerName).Forget();
            }
            else
            {
                sub.Handler.DynamicInvoke(evt);
            }
        }
        catch (Exception ex)
        {
            LogSubscriberException(typeof(T).Name, sub.HandlerName, ex);
        }
    }

    void InvokeManualSubscriber<T>(ManualSubscription sub, T evt)
    {
        try
        {
            var result = sub.Handler.DynamicInvoke(evt);
            if (result is UniTask task)
                HandleFireAndForgetAsync(task, typeof(T).Name, "Manual").Forget();
        }
        catch (Exception ex)
        {
            LogSubscriberException(typeof(T).Name, "Manual", ex);
        }
    }

    async UniTask InvokeSubscriberAsync<T>(EventSubscription sub, T evt, List<Exception> exceptions)
    {
        try
        {
            if (sub.IsAsync)
            {
                var task = (UniTask)sub.Handler.DynamicInvoke(evt);
                await task;
            }
            else
            {
                sub.Handler.DynamicInvoke(evt);
            }
        }
        catch (Exception ex)
        {
            exceptions.Add(ex);
            LogSubscriberException(typeof(T).Name, sub.HandlerName, ex);
        }
    }

    async UniTask InvokeManualSubscriberAsync<T>(ManualSubscription sub, T evt, List<Exception> exceptions)
    {
        try
        {
            var result = sub.Handler.DynamicInvoke(evt);
            if (result is UniTask task)
                await task;
        }
        catch (Exception ex)
        {
            exceptions.Add(ex);
            LogSubscriberException(typeof(T).Name, "Manual", ex);
        }
    }

    async UniTask HandleFireAndForgetAsync(UniTask task, string eventName, string handlerName)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            LogSubscriberException(eventName, handlerName, ex);
        }
    }

    void LogSubscriberException(string eventName, string handlerName, Exception ex)
    {
        var origin = ExtractOrigin(ex);
        var msg = $"Event={eventName} Handler={handlerName} Exception={ex.GetType().Name} Msg=\"{ex.Message}\" Origin={origin}";
        FrameworkLogger.Error("EventBus", msg);
        RecordError(msg);
    }

    void RecordError(string msg)
    {
        _recentErrors.Add(msg);
        if (_recentErrors.Count > 10)
            _recentErrors.RemoveAt(0);
    }

    /// <summary>
    /// 从异常堆栈中提取第一个非 Core 框架的用户代码帧。
    /// </summary>
    static string ExtractOrigin(Exception ex)
    {
        var stack = ex.StackTrace;
        if (string.IsNullOrEmpty(stack)) return "unknown";

        // 取第一个不在 Core 框架内的栈帧
        var lines = stack.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            if (!trimmed.Contains("EventBus") && !trimmed.Contains("ModuleRunner") && !trimmed.Contains("FrameworkLogger"))
                return trimmed;
        }
        return "unknown";
    }
}
