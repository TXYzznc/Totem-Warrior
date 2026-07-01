#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IInputSimulator 默认实现。HashSet 存按键队列，O(1) 消费/查询，零 GC。
/// 用法：
///   var sim = new InputSimulator();
///   inputModule.EnableSimulator(sim);
///   sim.PressKey(KeyCode.E);   // 下一帧 InputModule.IsSkillPressed() == true
/// </summary>
public sealed class InputSimulator : IInputSimulator
{
    readonly HashSet<KeyCode> _keyDownQueue = new();
    readonly HashSet<KeyCode> _keyHeld = new();
    readonly HashSet<int> _mouseDownQueue = new();
    readonly HashSet<int> _mouseHeld = new();
    Vector2? _moveOverride;

    // ===== 注入入口（外部测试代码调用）=====

    /// <summary>排入一次 KeyDown（消费一次后清）。</summary>
    public void PressKey(KeyCode key) => _keyDownQueue.Add(key);

    /// <summary>按住 / 松开持续状态。</summary>
    public void HoldKey(KeyCode key, bool held)
    {
        if (held) _keyHeld.Add(key); else _keyHeld.Remove(key);
    }

    /// <summary>排入一次鼠标按下。</summary>
    public void PressMouse(int button) => _mouseDownQueue.Add(button);

    /// <summary>按住 / 松开鼠标持续状态。</summary>
    public void HoldMouse(int button, bool held)
    {
        if (held) _mouseHeld.Add(button); else _mouseHeld.Remove(button);
    }

    /// <summary>覆盖移动方向。传 null 取消覆盖。</summary>
    public void SetMove(Vector2? dir) => _moveOverride = dir;

    /// <summary>清空所有注入状态。一个测试结束、下一个测试开始前调用。</summary>
    public void ClearAll()
    {
        _keyDownQueue.Clear();
        _keyHeld.Clear();
        _mouseDownQueue.Clear();
        _mouseHeld.Clear();
        _moveOverride = null;
    }

    // ===== 消费入口（InputModule 调用，不应被外部代码触发）=====

    public bool ConsumeKeyDown(KeyCode key) => _keyDownQueue.Remove(key);
    public bool IsKeyHeld(KeyCode key) => _keyHeld.Contains(key);
    public bool ConsumeMouseDown(int button) => _mouseDownQueue.Remove(button);
    public bool IsMouseHeld(int button) => _mouseHeld.Contains(button);
    public Vector2? GetMoveOverride() => _moveOverride;
}
#endif
