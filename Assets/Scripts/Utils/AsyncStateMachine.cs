using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// Small generic async state machine. It owns only switching mechanics, not state meaning.
/// </summary>
public sealed class AsyncStateMachine<TStateId, TContext>
{
    readonly Dictionary<TStateId, IAsyncState<TStateId, TContext>> _states = new();
    readonly EventBus _eventBus;
    readonly string _machineName;

    IAsyncState<TStateId, TContext> _currentState;
    bool _isChanging;

    public AsyncStateMachine(string machineName = null, EventBus eventBus = null)
    {
        _machineName = string.IsNullOrEmpty(machineName)
            ? typeof(TStateId).Name
            : machineName;
        _eventBus = eventBus;
    }

    public bool HasCurrentState => _currentState != null;

    public TStateId CurrentStateId => _currentState != null ? _currentState.Id : default;

    public void RegisterState(IAsyncState<TStateId, TContext> state)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (_states.ContainsKey(state.Id))
            throw new InvalidOperationException($"状态 {state.Id} 已注册");

        _states.Add(state.Id, state);
        FrameworkLogger.Info("StateMachine",
            $"Machine={_machineName} Action=RegisterState State={state.Id}");
    }

    public async UniTask ChangeStateAsync(
        TStateId nextStateId,
        TContext context,
        CancellationToken ct = default)
    {
        if (_isChanging)
            throw new InvalidOperationException($"状态机 {_machineName} 正在切换状态");

        if (!_states.TryGetValue(nextStateId, out var nextState))
            throw new KeyNotFoundException($"状态 {nextStateId} 未注册");

        var hasPrevious = _currentState != null;
        var previousState = _currentState;
        var previousStateId = hasPrevious ? previousState.Id : default;

        if (hasPrevious && EqualityComparer<TStateId>.Default.Equals(previousStateId, nextStateId))
            return;

        _isChanging = true;
        _eventBus?.Publish(new StateChangingEvent<TStateId>(_machineName, previousStateId, nextStateId, hasPrevious));
        FrameworkLogger.Info("StateMachine",
            $"Machine={_machineName} Action=ChangeState From={FormatState(previousStateId, hasPrevious)} To={nextStateId}");

        try
        {
            if (hasPrevious)
            {
                await previousState.ExitAsync(context, ct);
                _currentState = null;
            }

            ct.ThrowIfCancellationRequested();

            await nextState.EnterAsync(context, ct);
            _currentState = nextState;

            _eventBus?.Publish(new StateChangedEvent<TStateId>(_machineName, previousStateId, nextStateId, hasPrevious));
            FrameworkLogger.Info("StateMachine",
                $"Machine={_machineName} Action=StateChanged Current={nextStateId}");
        }
        catch (Exception ex)
        {
            _eventBus?.Publish(new StateChangeFailedEvent<TStateId>(
                _machineName,
                previousStateId,
                nextStateId,
                hasPrevious,
                ex));
            FrameworkLogger.Error("StateMachine",
                $"Machine={_machineName} Action=ChangeStateFailed From={FormatState(previousStateId, hasPrevious)} To={nextStateId} Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
            throw;
        }
        finally
        {
            _isChanging = false;
        }
    }

    public void Tick(TContext context, float deltaTime)
    {
        _currentState?.Tick(context, deltaTime);
    }

    static string FormatState(TStateId stateId, bool hasState)
    {
        return hasState ? Convert.ToString(stateId) : "<none>";
    }
}
