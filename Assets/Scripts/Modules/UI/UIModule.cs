using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// UGUI UI 容器（v2.1）。
///
/// 职责：
/// - 维护 IUIForm 注册列表，监听 GameStateChangedEvent 后批量转发
/// - 管理覆盖层互斥（IExclusiveUIForm），保证同时只有一个覆盖层处于 Open 状态
/// - 不持有 Prefab 引用，不耦合具体业务
/// </summary>
public sealed class UIModule : IGameModule
{
    readonly EventBus _eventBus;
    readonly ModuleRunner _runner;
    readonly List<IUIForm> _forms = new();

    /// <summary>当前活跃的覆盖层表单（互斥）</summary>
    IExclusiveUIForm _exclusiveForm;

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
        _exclusiveForm = null;
        FrameworkLogger.Info("UIModule", "Action=Shutdown");
        return UniTask.CompletedTask;
    }

    /// <summary>由 IUIForm MonoBehaviour 在 Start() 中调用注册。</summary>
    public void Register(IUIForm form)
    {
        if (form == null || _forms.Contains(form)) return;
        _forms.Add(form);
        FrameworkLogger.Info("UIModule", $"Action=Register Form={form.GetType().Name}");
    }

    /// <summary>由 IUIForm MonoBehaviour 在 OnDestroy() 中调用注销。</summary>
    public void Unregister(IUIForm form)
    {
        _forms.Remove(form);
        // 若注销的是当前覆盖层，清除引用
        if (form is IExclusiveUIForm exc && ReferenceEquals(exc, _exclusiveForm))
            _exclusiveForm = null;
    }

    /// <summary>
    /// 请求打开覆盖层表单（互斥）。
    /// 若已有其他覆盖层处于 Open 状态，先强制关闭再打开新的。
    /// </summary>
    public void RequestOpenExclusive(IExclusiveUIForm form)
    {
        if (form == null) return;

        // 已有其他覆盖层 → 强制关闭
        if (_exclusiveForm != null && !ReferenceEquals(_exclusiveForm, form) && _exclusiveForm.IsOpen)
        {
            FrameworkLogger.Info("UIModule",
                $"Action=ForceClose Form={_exclusiveForm.GetType().Name} Reason=ExclusiveConflict");
            _exclusiveForm.ForceClose();
        }

        _exclusiveForm = form;
        FrameworkLogger.Info("UIModule", $"Action=OpenExclusive Form={form.GetType().Name}");
    }

    /// <summary>关闭当前覆盖层（由 Form 自身在 Close() 完成后调用）。</summary>
    public void CloseCurrentExclusive()
    {
        _exclusiveForm = null;
    }

    [EventHandler]
    void OnGameStateChanged(GameStateChangedEvent e)
    {
        foreach (var f in _forms)
            f.OnGameStateChanged(e.OldState, e.NewState);
    }
}

// ────────────────────────────────────────────────────────
// UI 接口（UGUI 版）
// ────────────────────────────────────────────────────────

/// <summary>
/// UGUI UI 表单接口。
/// 各 Form MonoBehaviour 实现此接口并通过 UIModule.Register 注册。
/// </summary>
public interface IUIForm
{
    /// <summary>游戏状态改变时由 UIModule 回调，Form 自决显隐。</summary>
    void OnGameStateChanged(GameState oldState, GameState newState);

    /// <summary>对应场景中的 GameObject（用于 SetActive 等操作）。</summary>
    GameObject GameObject { get; }
}

/// <summary>
/// 覆盖层表单接口（如 TattooStudioForm / ShopForm / ThreeChoiceForm）。
/// UIModule 保证同时只有一个覆盖层处于 Open 状态。
/// </summary>
public interface IExclusiveUIForm : IUIForm
{
    /// <summary>当前是否处于打开状态。</summary>
    bool IsOpen { get; }

    /// <summary>UIModule 互斥冲突时强制关闭（不播动画，直接 SetActive(false)）。</summary>
    void ForceClose();
}
