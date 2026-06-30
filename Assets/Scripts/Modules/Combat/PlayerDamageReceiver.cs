using System;
using System.Threading;
using AttackSystem.Events;
using Cysharp.Threading.Tasks;
using Tattoo;
using Tattoo.Events;
using UnityEngine;

/// <summary>
/// change #20 D8 落地：敌人攻击玩家 → 扣 HP → 发战斗事件链。
///
/// 职责：
/// - 订阅 EnemyAttackEvent（namespace Tattoo.Events，EnemyModule.cs:496）
/// - 扣减 SpawnerModule.PlayerTarget.Health
/// - 发 DamagedEvent（触发 Torso 部位刺青）+ PlayerHealthChangedEvent（HUD）
/// - HP ≤ 0 时一次性发 PlayerDiedEvent（防重复）
///
/// 不做：玩家死亡后的结算（→ CombatModule.EndCombat 已订阅 PlayerDiedEvent）。
///
/// CONTRACT §2.1 锁定：
///   ModuleCategory = 3; Dependencies = [SpawnerModule]
/// </summary>
public sealed class PlayerDamageReceiver : IGameModule
{
    public int    ModuleCategory => 3;
    public Type[] Dependencies   => new[] { typeof(SpawnerModule) };

    readonly ModuleRunner _runner;
    readonly EventBus     _bus;

    SpawnerModule _spawner;

    /// <summary>当前 HP（实时；HP 实际存储仍在 PlayerTarget.Health，本字段为查询缓存）。</summary>
    public float CurrentHP { get; private set; }

    /// <summary>HP 上限，等于 SpawnerModule.PlayerMaxHp。</summary>
    public float MaxHP { get; private set; }

    /// <summary>是否已发过 PlayerDiedEvent（防止反复触发战败结算）。</summary>
    bool _diedFired;

    /// <summary>
    /// 死亡防抖：使用 bool 标志 + 时间戳实现。
    /// _dying = true 时后续 ApplyDamage 只扣 HP、发 HealthChanged，不重复发 PlayerDiedEvent。
    /// InitializeAsync 时重置，ShutdownAsync 不处理（战场销毁时已无意义）。
    /// </summary>
    bool  _dying;
    float _dyingResetTime; // Time.realtimeSinceStartup，300ms 后复位

    const float DyingDebounceSeconds = 0.3f;

    public PlayerDamageReceiver(ModuleRunner runner, EventBus bus)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _bus    = bus    ?? throw new ArgumentNullException(nameof(bus));
    }

    public UniTask InitializeAsync(CancellationToken ct = default)
    {
        _spawner = _runner.GetModule<SpawnerModule>();
        MaxHP = _spawner?.PlayerMaxHp ?? 100f;

        // 同步 PlayerTarget.Health：PlayerTarget 可能在 InitializeAsync 期间已建立
        if (_spawner?.PlayerTarget != null)
        {
            _spawner.PlayerTarget.Health = MaxHP;
            CurrentHP = MaxHP;
        }
        else
        {
            CurrentHP = MaxHP;
        }

        _diedFired = false;
        _dying     = false;

        FrameworkLogger.Info("PlayerDamageReceiver",
            $"Action=Initialized MaxHP={MaxHP}");
        return UniTask.CompletedTask;
    }

    public UniTask ShutdownAsync(CancellationToken ct = default)
    {
        _diedFired = false;
        _dying     = false;
        FrameworkLogger.Info("PlayerDamageReceiver", "Action=Shutdown");
        return UniTask.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────
    // 事件处理
    // ─────────────────────────────────────────────────────────────

    [EventHandler]
    void OnEnemyAttack(EnemyAttackEvent e)
    {
        // 防抖复位：若 _dying 且超过 300ms，重置标志（允许后续伤害再次触发死亡检测）
        if (_dying && Time.realtimeSinceStartup - _dyingResetTime > DyingDebounceSeconds)
            _dying = false;

        ApplyDamage(e.Damage, e.Attacker?.EnemyId ?? "Enemy");
    }

    // ─────────────────────────────────────────────────────────────
    // 公共 API（CombatModule / SkillHitResolver 等直接调用）
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 对玩家施加伤害。调用方传入最终计算值（不含伤害公式），本方法只负责扣血和发事件。
    /// </summary>
    /// <param name="amount">伤害量（正数）。≤ 0 时直接忽略。</param>
    /// <param name="source">伤害来源描述，用于日志和浮动数字标签。</param>
    public void ApplyDamage(float amount, string source = "Unknown")
    {
        if (amount <= 0f) return;

        var playerTarget = _spawner?.PlayerTarget;
        if (playerTarget == null)
        {
            FrameworkLogger.Warn("PlayerDamageReceiver",
                $"Action=ApplyDamage Source={source} Skipped=PlayerTargetNull");
            return;
        }

        float oldHp = playerTarget.Health;

        // 扣血并 clamp
        float newHp = Mathf.Clamp(oldHp - amount, 0f, MaxHP);
        playerTarget.Health = newHp;
        CurrentHP = newHp;

        FrameworkLogger.Info("PlayerDamageReceiver",
            $"Action=ApplyDamage Source={source} Amount={amount:F1} OldHP={oldHp:F1} NewHP={newHp:F1}");

        // 发 DamagedEvent（Torso 刺青监听此事件）
        // attacker 传 null：EnemyActorData 不是 Tattoo.Data.Target，Torso 行为已兜底 null attacker
        _bus.Publish(new DamagedEvent(attacker: null, damage: amount, newHp: newHp, maxHp: MaxHP));

        // 发 PlayerHealthChangedEvent（HUD 血条）
        _bus.Publish(new PlayerHealthChangedEvent(current: newHp, max: MaxHP, delta: -amount));

        // 死亡判定（防重复触发）
        if (newHp <= 0f && !_diedFired)
        {
            // 死亡防抖：300ms 内再调 ApplyDamage 不重发 PlayerDiedEvent
            if (!_dying)
            {
                _dying = true;
                _dyingResetTime = Time.realtimeSinceStartup;
                _diedFired = true;

                FrameworkLogger.Info("PlayerDamageReceiver",
                    $"Action=PlayerDied Source={source} FinalHP={newHp:F1}");

                _bus.Publish(new PlayerDiedEvent());
            }
        }
    }
}
