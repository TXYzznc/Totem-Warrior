using UnityEngine;

/// <summary>
/// 按世界 Z 坐标动态计算 SpriteRenderer.sortingOrder，解决正交俯角相机下 actor 互穿问题。
///
/// 公式：sortingOrder = baseOffset + RoundToInt(-worldPos.z * K)
///   K = 100（每 1 世界单位 = 100 个 order 台阶；MapSize=150 → z∈[-75,75] → order 跨度 ±7500）
///   -z：z 越大（越远离相机）order 越小，越靠后渲染，符合俯角视觉语义。
///
/// 同时写入子节点 "Shadow_ch22" 的 SpriteRenderer.sortingOrder = 宿主 order - 1，
/// 保证阴影恒在自身 actor 之下且随深度浮动（废弃 ActorShadowHelper 的固定 -100）。
///
/// 性能：位置脏检查（sqrMagnitude &lt; 1e-4），静止 actor 不重算，全程 0 GC alloc。
///
/// change #25：2.5D 相机系统
/// </summary>
[DisallowMultipleComponent]
public sealed class DepthSortedSprite : MonoBehaviour
{
    /// <summary>主体 actor / NPC / 障碍的 baseOffset 默认为 0。</summary>
    public int BaseOffset = 0;

    const float K         = 100f;
    const float DirtyEpsSq = 1e-4f; // 脏检查阈值（平方距离）

    SpriteRenderer _sr;
    Transform      _shadowTransform;
    SpriteRenderer _shadowSr;

    Vector3 _lastPos;
    bool    _initialized;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        // 查找子节点 Shadow_ch22（ActorShadowHelper 创建的阴影）
        var shadowChild = transform.Find("Shadow_ch22");
        if (shadowChild != null)
        {
            _shadowTransform = shadowChild;
            _shadowSr = shadowChild.GetComponent<SpriteRenderer>();
        }
    }

    void OnEnable()
    {
        // 强制首帧重算
        _initialized = false;
        _lastPos = transform.position + Vector3.one * 9999f; // 使脏检查触发
        ForceRecalculate();
    }

    void Update()
    {
        Vector3 pos = transform.position;
        if (_initialized && (pos - _lastPos).sqrMagnitude < DirtyEpsSq) return;

        _lastPos     = pos;
        _initialized = true;
        ForceRecalculate();
    }

    void ForceRecalculate()
    {
        int order = BaseOffset + Mathf.RoundToInt(-transform.position.z * K);

        if (_sr != null)
            _sr.sortingOrder = order;

        // 联动写子 shadow order（宿主 order - 1）
        if (_shadowSr != null)
            _shadowSr.sortingOrder = order - 1;
    }
}
