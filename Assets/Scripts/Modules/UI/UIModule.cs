using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 通用 UI 容器。
///
/// 设计：
/// - UIModule 不直接持有具体业务 panel；改为暴露 IUIForm 注册 / 切换 API
/// - 业务 UI（如 CombatHUDForm）由独立 MonoBehaviour 实现，自带 UIDocument，并通过 UIModule 注册
/// - UIModule 监听 GameStateChangedEvent 后转发给已注册 form（form 自决定显隐）
/// </summary>
public sealed class UIModule : IGameModule
{
    readonly EventBus _eventBus;
    readonly ModuleRunner _runner;
    readonly List<IUIForm> _forms = new();

    public int ModuleCategory => 2;
    public Type[] Dependencies => Type.EmptyTypes;

    public UIModule(EventBus eventBus, ModuleRunner runner)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _runner   = runner   ?? throw new ArgumentNullException(nameof(runner));
    }

    public UniTask InitializeAsync(CancellationToken ct = default)
    {
        FrameworkLogger.Info("UIModule", $"Action=Initialized RegisteredForms={_forms.Count}");
        return UniTask.CompletedTask;
    }

    public UniTask ShutdownAsync(CancellationToken ct = default)
    {
        _forms.Clear();
        FrameworkLogger.Info("UIModule", "Action=Shutdown");
        return UniTask.CompletedTask;
    }

    /// <summary>由 IUIForm 实例（通常是 MonoBehaviour）自行调用注册。</summary>
    public void Register(IUIForm form)
    {
        if (form == null || _forms.Contains(form)) return;
        _forms.Add(form);
    }

    public void Unregister(IUIForm form)
    {
        _forms.Remove(form);
    }

    [EventHandler]
    void OnGameStateChanged(GameStateChangedEvent e)
    {
        foreach (var f in _forms) f.OnGameStateChanged(e.OldState, e.NewState);
    }
}

/// <summary>UI 表单接口。业务 UI（如 CombatHUDForm）实现本接口并通过 UIModule.Register 注册。</summary>
public interface IUIForm
{
    void OnGameStateChanged(GameState oldState, GameState newState);
}
