using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tattoo.Events;
using UnityEngine;

/// <summary>
/// UGUI UI 容器（v2.1）。
///
/// 职责：
/// - 维护 IUIForm 注册列表，监听 GameStateChangedEvent 后批量转发
/// - 管理覆盖层互斥（IExclusiveUIForm），保证同时只有一个覆盖层处于 Open 状态
/// - 在 GameReady 后从 UIFormConfig 动态加载 Prefab + 实例化（DontDestroyOnLoad）
/// </summary>
public sealed class UIModule : IGameModule
{
    readonly EventBus _eventBus;
    readonly ModuleRunner _runner;
    readonly List<IUIForm> _forms = new();

    /// <summary>当前活跃的覆盖层表单（互斥）</summary>
    IExclusiveUIForm _exclusiveForm;

    /// <summary>动态加载的 UI 根容器（DontDestroyOnLoad）</summary>
    GameObject _uiRoot;

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
        if (_uiRoot != null) UnityEngine.Object.Destroy(_uiRoot);
        _uiRoot = null;
        FrameworkLogger.Info("UIModule", "Action=Shutdown");
        return UniTask.CompletedTask;
    }

    /// <summary>
    /// GameApp 全模块就绪后，从 UIFormConfig 读取 9 个 Form Prefab 动态加载。
    /// 加载顺序：MainMenu 类（SortOrder 30）先创建占满屏，覆盖层（10/20）次之，HUD（0）最低。
    /// </summary>
    [EventHandler]
    void OnGameReady(GameReadyEvent e)
    {
        if (_uiRoot != null) return; // 已加载过

        _uiRoot = new GameObject("UIRoot");
        UnityEngine.Object.DontDestroyOnLoad(_uiRoot);

        var dt = _runner.GetModule<DataTableModule>();
        var cfg = dt.GetTable<UIFormConfig>();
        int success = 0, failed = 0;

        foreach (var row in cfg.All.Values)
        {
            try
            {
                var prefab = Resources.Load<GameObject>("Prefab/" + row.PrefabPath);
                if (prefab == null)
                {
                    FrameworkLogger.Warn("UIModule", $"Action=LoadForm Form={row.FormName} 找不到 Prefab Path=Prefab/{row.PrefabPath}");
                    failed++;
                    continue;
                }
                var inst = UnityEngine.Object.Instantiate(prefab, _uiRoot.transform);
                inst.name = row.FormName;

                // 强制 Canvas ScreenSpaceOverlay + SortOrder + 全屏 stretch
                var canvas = inst.GetComponent<Canvas>();
                if (canvas != null)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = row.SortOrder;
                }
                var rt = inst.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;
                    rt.anchoredPosition = Vector2.zero;
                }
                // 实例化后立即注册到 UIModule，不依赖 Form.Start() 的异步轮询。
                // Form.Start() 中的 Register 调用因 _forms.Contains 检查而幂等，不会重复注册。
                // 若不在此处提前注册，Start() 在下一帧才执行，OnGameStateChanged 广播会丢失。
                // 用 GetComponentInChildren(includeInactive=true)：部分 Prefab 的 IUIForm 挂在子节点
                // （如 Canvas/Panel 下），且 Form 自身常在 Awake 中 SetActive(false)，必须 includeInactive
                var form = inst.GetComponentInChildren<IUIForm>(true);
                if (form != null)
                {
                    Register(form);
                    FrameworkLogger.Info("UIModule", $"Action=EarlyRegister Form={row.FormName}");
                    // Bootstrap：让 Form 在 inactive 状态下也能完成事件订阅 / 按钮绑定
                    // 必要原因：MonoBehaviour.Start 不在 inactive GameObject 上运行，
                    // 所以 Form 若在 Awake 中 SetActive(false) 就永远拿不到 Start 触发，
                    // 必须借这个 hook 同步传 bus/runner 完成初始化。
                    if (form is IUIFormBootstrap boot)
                    {
                        try { boot.Bootstrap(_eventBus, _runner); }
                        catch (Exception bex)
                        {
                            FrameworkLogger.Error("UIModule", $"Action=Bootstrap Form={row.FormName} Exception={bex.GetType().Name} Msg=\"{bex.Message}\"");
                        }
                    }
                    // 喂一次当前 GameState，让 Form 决定初始显隐
                    var gs = _runner.GetModule<GameStateModule>();
                    if (gs != null) form.OnGameStateChanged(gs.CurrentState, gs.CurrentState);
                }
                else
                {
                    FrameworkLogger.Warn("UIModule", $"Action=EarlyRegister Form={row.FormName} 找不到 IUIForm 组件（Prefab 结构异常）");
                }
                success++;
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error("UIModule", $"Action=LoadForm Form={row.FormName} Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
                failed++;
            }
        }
        FrameworkLogger.Info("UIModule", $"Action=AllFormsLoaded Success={success} Failed={failed} Total={cfg.All.Count}");
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

/// <summary>
/// UIForm 的"inactive 阶段初始化"钩子（可选）。
///
/// 背景：UIModule.OnGameReady 实例化 Prefab 后，Form 通常在 Awake 中 SetActive(false)；
/// Unity 不在 inactive GameObject 上调用 Start()，因此把订阅 / 按钮绑定写在 Start 里的 Form
/// 会一直收不到事件（如 PauseMenuForm 收不到 PauseRequestedEvent）。
///
/// 实现该接口的 Form 由 UIModule 在 EarlyRegister 之后立即同步调用 Bootstrap，
/// 把 EventBus / ModuleRunner 直接传进来，Form 在此完成事件订阅与按钮 listener 绑定。
/// </summary>
public interface IUIFormBootstrap
{
    /// <summary>
    /// 由 UIModule 在 EarlyRegister 之后立即调用。Form 应在此设置 EventBus / ModuleRunner 引用、
    /// 调用 SubscribeEvents、为按钮绑定 onClick。**禁止**在此调用 SetActive(true)。
    /// </summary>
    void Bootstrap(EventBus bus, ModuleRunner runner);
}
