using Tattoo;
using UnityEngine;

/// <summary>
/// 俯角 billboard 校正组件。
///
/// 使 sprite 的可视 transform 绕世界 X 轴旋转 = 相机俯角，
/// 避免正交俯角相机把竖直 sprite 压扁。
///
/// 因相机俯角在一局内固定不变，校正只在 OnEnable 时执行一次，不进每帧循环（0 Update 开销）。
/// 若运行时俯角变更，可调用 ApplyTilt(tiltX) 手动刷新。
///
/// change #25：2.5D 相机系统
/// </summary>
[DisallowMultipleComponent]
public sealed class BillboardSprite : MonoBehaviour
{
    /// <summary>
    /// 默认俯角（度）。CameraModule 未就绪时兜底值。
    /// </summary>
    const float DefaultTiltX = 55f;

    void OnEnable()
    {
        float tiltX = GetTiltX();
        ApplyTilt(tiltX);
    }

    /// <summary>手动刷新俯角（供 CameraModule 俯角变更时调用）。</summary>
    public void ApplyTilt(float tiltX)
    {
        transform.localEulerAngles = new Vector3(tiltX, 0f, 0f);
    }

    static float GetTiltX()
    {
        // 尝试从 ModuleRunner 取 CameraModule（运行时懒查）
        // GameApp 提供 TryGetRuntime；此处通过 Camera tag 查找后取属性更简洁：
        // CameraModule 设的相机 euler.x 就是 CameraTiltX。
        var cam = Camera.main;
        if (cam != null)
        {
            float ex = cam.transform.eulerAngles.x;
            // eulerAngles.x 范围 [0,360]；55° 在此范围内直接返回
            if (ex > 0f && ex < 90f) return ex;
        }
        return DefaultTiltX;
    }
}
