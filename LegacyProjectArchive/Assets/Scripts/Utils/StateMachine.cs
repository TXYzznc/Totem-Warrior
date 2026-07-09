using System;
using System.Collections.Generic;

/// <summary>
/// 通用状态机。Enter/Update/Exit 模式。
/// </summary>
public class StateMachine<TState> where TState : Enum
{
    public TState CurrentState { get; private set; }

    readonly Dictionary<TState, Action> _enterCallbacks = new();
    readonly Dictionary<TState, Action> _updateCallbacks = new();
    readonly Dictionary<TState, Action> _exitCallbacks = new();

    /// <summary>
    /// 注册状态及其回调。
    /// </summary>
    public void RegisterState(TState state, Action onEnter = null, Action onUpdate = null, Action onExit = null)
    {
        _enterCallbacks[state] = onEnter;
        _updateCallbacks[state] = onUpdate;
        _exitCallbacks[state] = onExit;
    }

    /// <summary>
    /// 切换状态。自动调用旧状态的 Exit 和新状态的 Enter。
    /// </summary>
    public void ChangeState(TState newState)
    {
        if (_exitCallbacks.TryGetValue(CurrentState, out var exit))
            exit?.Invoke();

        CurrentState = newState;

        if (_enterCallbacks.TryGetValue(newState, out var enter))
            enter?.Invoke();
    }

    /// <summary>
    /// 调用当前状态的 Update 回调。
    /// </summary>
    public void Update()
    {
        if (_updateCallbacks.TryGetValue(CurrentState, out var update))
            update?.Invoke();
    }
}