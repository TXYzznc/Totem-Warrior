using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 实现本接口的模块表示需要逐帧 Update 驱动。由 GameTickDriver 统一调用。
/// </summary>
public interface ITickable
{
    void OnUpdate(float deltaTime);
}

/// <summary>
/// 统一驱动需要逐帧 Update 的模块。
/// IGameModule 没有引擎 Update 回调；本 Driver 在所有模块初始化完成后统一驱动 ITickable 列表。
/// </summary>
public sealed class GameTickDriver : MonoBehaviour
{
    readonly List<ITickable> _tickables = new();

    /// <summary>注册一个需要逐帧 Update 的模块。可在 StartAsync 完成后调用。</summary>
    public void Register(ITickable tickable)
    {
        if (tickable == null) return;
        if (_tickables.Contains(tickable)) return;
        _tickables.Add(tickable);
    }

    public void Unregister(ITickable tickable)
    {
        _tickables.Remove(tickable);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        // 复制快照避免 OnUpdate 内增删导致集合修改异常
        for (int i = 0; i < _tickables.Count; i++)
        {
            _tickables[i]?.OnUpdate(dt);
        }
    }
}
