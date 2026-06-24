using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// Generic flow registry and transition runner.
/// </summary>
public sealed class FlowModule : IGameModule
{
    readonly Dictionary<string, IFlow> _flows = new();
    readonly EventBus _eventBus;

    IFlow _currentFlow;
    CancellationTokenSource _lifetimeCts;
    bool _isChanging;

    public int ModuleCategory => 0;

    public string CurrentFlowId => _currentFlow?.Id;

    public FlowModule(EventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    public UniTask InitializeAsync(CancellationToken ct = default)
    {
        _lifetimeCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        FrameworkLogger.Info("FlowModule",
            $"Action=Initialized RegisteredFlows={_flows.Count}");
        return UniTask.CompletedTask;
    }

    public async UniTask ShutdownAsync(CancellationToken ct = default)
    {
        _lifetimeCts?.Cancel();

        if (_currentFlow != null)
        {
            var context = new FlowContext(_eventBus);
            FrameworkLogger.Info("FlowModule",
                $"Action=ExitCurrentFlow Flow={_currentFlow.Id}");
            await _currentFlow.ExitAsync(context, ct);
            _eventBus.Publish(new FlowExitedEvent(_currentFlow.Id));
            _currentFlow = null;
        }

        _lifetimeCts?.Dispose();
        _lifetimeCts = null;
        FrameworkLogger.Info("FlowModule", "Action=Shutdown");
    }

    public void RegisterFlow(IFlow flow)
    {
        if (flow == null) throw new ArgumentNullException(nameof(flow));
        if (string.IsNullOrWhiteSpace(flow.Id))
            throw new ArgumentException("Flow Id 不能为空", nameof(flow));
        if (_flows.ContainsKey(flow.Id))
            throw new InvalidOperationException($"Flow {flow.Id} 已注册");

        _flows.Add(flow.Id, flow);
        FrameworkLogger.Info("FlowModule",
            $"Action=RegisterFlow Flow={flow.Id}");
    }

    public async UniTask ChangeFlowAsync(
        string flowId,
        IReadOnlyDictionary<string, object> parameters = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(flowId))
            throw new ArgumentException("Flow Id 不能为空", nameof(flowId));
        if (!_flows.TryGetValue(flowId, out var nextFlow))
            throw new KeyNotFoundException($"Flow {flowId} 未注册");
        if (_isChanging)
            throw new InvalidOperationException("FlowModule 正在切换流程");

        var previousFlow = _currentFlow;
        if (previousFlow != null && previousFlow.Id == flowId)
            return;

        _isChanging = true;
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            ct,
            _lifetimeCts?.Token ?? CancellationToken.None);
        var linkedToken = linkedCts.Token;
        var context = new FlowContext(_eventBus, parameters);

        _eventBus.Publish(new FlowChangingEvent(previousFlow?.Id, flowId, parameters));
        FrameworkLogger.Info("FlowModule",
            $"Action=ChangeFlow From={FormatFlow(previousFlow)} To={flowId}");

        try
        {
            if (previousFlow != null)
            {
                await previousFlow.ExitAsync(context, linkedToken);
                _eventBus.Publish(new FlowExitedEvent(previousFlow.Id));
            }

            linkedToken.ThrowIfCancellationRequested();

            await nextFlow.EnterAsync(context, linkedToken);
            _currentFlow = nextFlow;

            _eventBus.Publish(new FlowChangedEvent(previousFlow?.Id, flowId, parameters));
            FrameworkLogger.Info("FlowModule",
                $"Action=FlowChanged Current={flowId}");
        }
        catch (Exception ex)
        {
            _eventBus.Publish(new FlowChangeFailedEvent(previousFlow?.Id, flowId, ex, parameters));
            FrameworkLogger.Error("FlowModule",
                $"Action=ChangeFlowFailed From={FormatFlow(previousFlow)} To={flowId} Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
            throw;
        }
        finally
        {
            _isChanging = false;
        }
    }

    [EventHandler]
    async UniTask OnFlowChangeRequested(FlowChangeRequestedEvent evt)
    {
        await ChangeFlowAsync(evt.FlowId, evt.Parameters);
    }

    static string FormatFlow(IFlow flow)
    {
        return flow == null ? "<none>" : flow.Id;
    }
}
