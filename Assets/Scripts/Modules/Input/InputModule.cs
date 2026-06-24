using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 玩家输入查询模块。无状态、零依赖，所有按键输入必须经由本模块。
/// </summary>
public sealed class InputModule : IGameModule
{
    public int ModuleCategory => 0;
    public Type[] Dependencies => Type.EmptyTypes;

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
        float x = 0f, y = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;

        var dir = new Vector2(x, y);
        return dir.sqrMagnitude > 1f ? dir.normalized : dir;
    }

    // ===== 战斗高层动作（CombatModule 在 OnUpdate 中轮询）=====

    /// <summary>玩家按下普攻（鼠标左键）。</summary>
    public bool IsAttackPressed() => Input.GetMouseButtonDown(0);

    /// <summary>玩家按下技能（E 键）。</summary>
    public bool IsSkillPressed() => Input.GetKeyDown(KeyCode.E);

    /// <summary>玩家按下闪避（空格）。语义化别名，等价于 IsSpacePressed。</summary>
    public bool IsDodgePressed() => Input.GetKeyDown(KeyCode.Space);

    // ===== 系统级 =====

    public bool IsSpacePressed() => Input.GetKeyDown(KeyCode.Space);
    public bool IsEscapePressed() => Input.GetKeyDown(KeyCode.Escape);
    public bool IsReturnPressed() => Input.GetKeyDown(KeyCode.Return);
    public bool IsDebugKeyPressed() => Input.GetKeyDown(KeyCode.F12);
}
