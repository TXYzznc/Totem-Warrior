using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 玩家输入查询模块。无状态、零依赖，所有按键输入必须经由本模块。
/// Editor / Development Build 下可装配 IInputSimulator 进行 playtest 注入（详见 EnableSimulator）。
/// </summary>
public sealed class InputModule : IGameModule
{
    public int ModuleCategory => 0;
    public Type[] Dependencies => Type.EmptyTypes;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    IInputSimulator _simulator;

    /// <summary>[测试用] 装配虚拟输入源。再次调用覆盖。生产构建剥离。</summary>
    public void EnableSimulator(IInputSimulator sim) => _simulator = sim;

    /// <summary>[测试用] 卸下虚拟输入源。</summary>
    public void DisableSimulator() => _simulator = null;

    /// <summary>[测试用] 取当前 simulator，测试代码用其注入按键。</summary>
    public IInputSimulator GetSimulator() => _simulator;
#endif

    // ===== 蓄力状态追踪 =====
    float _attackHoldStartTime = -1f;
    bool  _attackWasHolding;

    public UniTask InitializeAsync(CancellationToken ct = default)
    {
        FrameworkLogger.Info("InputModule", "Action=Initialized");
        return UniTask.CompletedTask;
    }

    public UniTask ShutdownAsync(CancellationToken ct = default)
    {
        FrameworkLogger.Info("InputModule", "Action=Shutdown");
        return UniTask.CompletedTask;
    }

    /// <summary>WASD/方向键 8方向移动输入，已归一化。</summary>
    public Vector2 GetMoveDirection()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_simulator != null)
        {
            var ov = _simulator.GetMoveOverride();
            if (ov.HasValue) return ov.Value;
        }
#endif

        float x = 0f, y = 0f;
        if (IsKeyHeld(KeyCode.A) || IsKeyHeld(KeyCode.LeftArrow)) x -= 1f;
        if (IsKeyHeld(KeyCode.D) || IsKeyHeld(KeyCode.RightArrow)) x += 1f;
        if (IsKeyHeld(KeyCode.W) || IsKeyHeld(KeyCode.UpArrow)) y += 1f;
        if (IsKeyHeld(KeyCode.S) || IsKeyHeld(KeyCode.DownArrow)) y -= 1f;

        var dir = new Vector2(x, y);
        return dir.sqrMagnitude > 1f ? dir.normalized : dir;
    }

    // ===== 战斗高层动作（CombatModule 在 OnUpdate 中轮询）=====

    /// <summary>玩家按下普攻（鼠标左键）。</summary>
    public bool IsAttackPressed() => IsMouseDown(0);

    // ===== change#20 蓄力 API（骨架阶段返回 false / 0；阶段 3 Agent E 填充）=====

    /// <summary>
    /// change#20: 鼠标左键是否处于"按住"状态（非一次性按下）。
    /// 蓄力开始判定的基础信号；与 IsAttackPressed() 互不冲突。
    /// </summary>
    public bool IsAttackHolding()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_simulator != null)
        {
            bool simHeld = _simulator.IsMouseHeld(0);
            UpdateHoldTracking(simHeld);
            return simHeld;
        }
#endif
        bool held = Input.GetMouseButton(0);
        UpdateHoldTracking(held);
        return held;
    }

    /// <summary>
    /// change#20: 当前一次按住的持续时间（秒）。
    /// 鼠标松开时归零；按下首帧返回 0。
    /// 用 Time.unscaledTime 累积，不受 timeScale 影响（暂停期间不增长）。
    /// </summary>
    public float GetAttackHoldDuration()
    {
        if (!_attackWasHolding || _attackHoldStartTime < 0f) return 0f;
        return UnityEngine.Time.unscaledTime - _attackHoldStartTime;
    }

    void UpdateHoldTracking(bool currentlyHeld)
    {
        if (currentlyHeld && !_attackWasHolding)
        {
            // 按下首帧：记录起始时间
            _attackHoldStartTime = UnityEngine.Time.unscaledTime;
        }
        else if (!currentlyHeld)
        {
            // 松开：清零
            _attackHoldStartTime = -1f;
        }
        _attackWasHolding = currentlyHeld;
    }

    /// <summary>玩家按下技能（E 键）。</summary>
    public bool IsSkillPressed() => IsKeyDown(KeyCode.E);

    /// <summary>玩家按下闪避（空格）。语义化别名，等价于 IsSpacePressed。</summary>
    public bool IsDodgePressed() => IsKeyDown(KeyCode.Space);

    // ===== 系统级 =====

    public bool IsSpacePressed() => IsKeyDown(KeyCode.Space);
    public bool IsEscapePressed() => IsKeyDown(KeyCode.Escape);
    public bool IsReturnPressed() => IsKeyDown(KeyCode.Return);
    public bool IsDebugKeyPressed() => IsKeyDown(KeyCode.F12);

    /// <summary>玩家按下自助纹身面板开关（Tab 键）。</summary>
    public bool IsSelfTattooTogglePressed() => IsKeyDown(KeyCode.Tab);

    // ===== change#18 拾取 / 交互 Action =====

    /// <summary>
    /// change#18: 拾取/交互（F 键）。
    /// 对应 WeaponPickupTrigger / ChestInteractTrigger / MerchantTrigger 的交互触发。
    /// 一次性按下语义（IsKeyDown）。
    /// </summary>
    public bool IsPickupPressed() => IsKeyDown(KeyCode.F);

    // ===== 双源融合内部辅助 =====

    bool IsKeyDown(KeyCode k)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_simulator != null && _simulator.ConsumeKeyDown(k)) return true;
#endif
        return Input.GetKeyDown(k);
    }

    bool IsKeyHeld(KeyCode k)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_simulator != null && _simulator.IsKeyHeld(k)) return true;
#endif
        return Input.GetKey(k);
    }

    bool IsMouseDown(int button)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_simulator != null && _simulator.ConsumeMouseDown(button)) return true;
#endif
        return Input.GetMouseButtonDown(button);
    }
}
