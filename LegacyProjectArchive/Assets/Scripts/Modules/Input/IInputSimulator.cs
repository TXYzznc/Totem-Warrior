#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

/// <summary>
/// 测试 / playtest 用：替代 Unity Input.* 的虚拟输入源。
/// 仅 Editor / Development Build 编译。
/// </summary>
public interface IInputSimulator
{
    /// <summary>消费一次 KeyDown（语义与 Input.GetKeyDown 一致）。</summary>
    bool ConsumeKeyDown(KeyCode key);

    /// <summary>持续按住状态（语义与 Input.GetKey 一致）。</summary>
    bool IsKeyHeld(KeyCode key);

    /// <summary>消费一次鼠标按下。button: 0=左 1=右 2=中。</summary>
    bool ConsumeMouseDown(int button);

    /// <summary>持续按住鼠标。</summary>
    bool IsMouseHeld(int button);

    /// <summary>移动方向覆盖。null = 不覆盖，InputModule 走原 WASD 逻辑。</summary>
    Vector2? GetMoveOverride();
}
#endif
