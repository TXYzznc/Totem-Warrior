using System;
using System.Collections.Generic;
using System.Threading;
using AttackSystem.Events;
using Cysharp.Threading.Tasks;
using Tattoo.Data;
using Tattoo.Events;

/// <summary>
/// change #20 D4 落地：元素 DoT 状态机。
///
/// 职责：
/// - 订阅 EffectAppliedEvent，解析 result.Status 字符串 → ApplyStatus
/// - 每 0.5s tick 所有 target 的 ActiveStatus 列表：发 StatusEffectTickedEvent（不直接扣血，由订阅方处理）
/// - 同名 status 合并：取 max(DPS) / max(RemainingSec)（不叠层）
/// - duration 耗尽 → 发 StatusEffectExpiredEvent 并移除
///
/// 不做：实际扣血（→ SkillHitResolver / PlayerDamageReceiver 订阅 StatusEffectTickedEvent 后扣）、
///       视觉粒子（→ VFXModule）、决定元素种类（→ Element 策略给出 status 串）。
///
/// CONTRACT §2.1 锁定：
///   ModuleCategory = 1; Dependencies = Type.EmptyTypes。
/// </summary>
public sealed class StatusEffectModule : IGameModule, ITickable
{
    public int    ModuleCategory => 1;
    public Type[] Dependencies   => Type.EmptyTypes;

    readonly ModuleRunner _runner;
    readonly EventBus     _bus;

    // ─────────────────────────────────────────────────────────────
    // 已识别状态名常量（5 种起步占位，效果由订阅方实现）
    // ─────────────────────────────────────────────────────────────

    static class KnownStatus
    {
        public const string Burn   = "Burn";
        public const string Poison = "Poison";
        public const string Shock  = "Shock";
        public const string Stun   = "Stun";
        public const string Slow   = "Slow";

        /// <summary>O(1) 验证：是否是已知状态名（不区分大小写）。</summary>
        public static bool IsKnown(string name)
        {
            return string.Equals(name, Burn,   StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, Poison, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, Shock,  StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, Stun,   StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, Slow,   StringComparison.OrdinalIgnoreCase);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 内部数据结构
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 单个生效中的 status。struct 避免 List 内元素 GC alloc；
    /// OnUpdate 内不要 new；通过索引赋值原地修改。
    /// </summary>
    public struct ActiveStatus
    {
        public string Name;          // "Burn" / "Poison" / ...
        public float  DPS;           // 每秒伤害
        public float  RemainingSec;  // 剩余持续秒
        public float  TickAccum;     // 本 tick 周期内的子累积（0 ~ TickInterval）
        public Target Source;        // 来源（本期保留字段，暂未用于计算）
        public float  Param1;        // Element.Param1（透传，供订阅方扩展）
        public float  Param2;        // Element.Param2
    }

    /// <summary>每 target 的活跃状态列表。预分配避免 OnUpdate alloc。</summary>
    readonly Dictionary<Target, List<ActiveStatus>> _active = new();

    /// <summary>tick 间隔（秒）。0.5s 平衡可见性与性能。</summary>
    const float TickInterval = 0.5f;

    /// <summary>全局 dt 累积器，每到 TickInterval 批处理一次所有 target。</summary>
    float _accum;

    // 复用列表，避免 ToRemove 在 OnUpdate 内 new
    readonly List<Target> _toRemoveTargets  = new();
    readonly List<int>    _expiredIndexBuf  = new();

    public StatusEffectModule(ModuleRunner runner, EventBus bus)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _bus    = bus    ?? throw new ArgumentNullException(nameof(bus));
    }

    // ─────────────────────────────────────────────────────────────
    // IGameModule
    // ─────────────────────────────────────────────────────────────

    public UniTask InitializeAsync(CancellationToken ct = default)
    {
        FrameworkLogger.Info("StatusEffectModule",
            $"Action=Initialized TickInterval={TickInterval}s KnownStatuses=Burn/Poison/Shock/Stun/Slow");
        return UniTask.CompletedTask;
    }

    public UniTask ShutdownAsync(CancellationToken ct = default)
    {
        _active.Clear();
        _toRemoveTargets.Clear();
        _expiredIndexBuf.Clear();
        FrameworkLogger.Info("StatusEffectModule", "Action=Shutdown");
        return UniTask.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────
    // 对外公共 API（CONTRACT §1.1 + §3 冻结签名）
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 给指定 target 添加（或刷新）一个状态。
    /// 同 target 同 statusName 重叠时取 max(DPS) / max(RemainingSec)（不叠层）。
    /// 发 StatusEffectAppliedEvent。
    /// </summary>
    public void ApplyStatus(Target target, string statusName, float dps, float duration,
                            float param1 = 0f, float param2 = 0f, Target source = null)
    {
        if (target == null)
        {
            FrameworkLogger.Warn("StatusEffectModule",
                $"Action=ApplyStatus Status={statusName} Err=target is null, skipped");
            return;
        }
        if (string.IsNullOrEmpty(statusName))
        {
            FrameworkLogger.Warn("StatusEffectModule",
                "Action=ApplyStatus Err=statusName empty, skipped");
            return;
        }
        if (duration <= 0f)
        {
            FrameworkLogger.Warn("StatusEffectModule",
                $"Action=ApplyStatus Status={statusName} Err=duration={duration} <=0, skipped");
            return;
        }

        if (!_active.TryGetValue(target, out var list))
        {
            list = new List<ActiveStatus>(4);
            _active[target] = list;
        }

        // 查找同名状态（不叠层，合并）
        bool found = false;
        for (int i = 0; i < list.Count; i++)
        {
            if (string.Equals(list[i].Name, statusName, StringComparison.OrdinalIgnoreCase))
            {
                var existing = list[i];
                existing.DPS          = Math.Max(existing.DPS, dps);
                existing.RemainingSec = Math.Max(existing.RemainingSec, duration);
                // param / source 以新值为准（刷新效果）
                existing.Param1 = param1;
                existing.Param2 = param2;
                if (source != null) existing.Source = source;
                list[i] = existing;
                found = true;

                FrameworkLogger.Info("StatusEffectModule",
                    $"Action=StatusRefreshed Target={target.Name} Status={statusName} DPS={existing.DPS} Remaining={existing.RemainingSec}");
                break;
            }
        }

        if (!found)
        {
            list.Add(new ActiveStatus
            {
                Name         = statusName,
                DPS          = dps,
                RemainingSec = duration,
                TickAccum    = 0f,
                Source       = source,
                Param1       = param1,
                Param2       = param2,
            });

            FrameworkLogger.Info("StatusEffectModule",
                $"Action=StatusApplied Target={target.Name} Status={statusName} DPS={dps} Duration={duration}");
        }

        _bus.Publish(new StatusEffectAppliedEvent(target, statusName, dps, duration, source));
    }

    /// <summary>清除指定 target 的全部状态（如 target 死亡时调用）。</summary>
    public void ClearAllStatuses(Target target)
    {
        if (target == null) return;
        if (!_active.TryGetValue(target, out var list)) return;

        // 对每条残余状态发 Expired 事件，便于 HUD 清除图标
        foreach (var s in list)
        {
            _bus.Publish(new StatusEffectExpiredEvent(target, s.Name));
        }

        _active.Remove(target);

        FrameworkLogger.Info("StatusEffectModule",
            $"Action=ClearAllStatuses Target={target.Name}");
    }

    /// <summary>查询指定 target 的活跃状态（debug / UI 用）。</summary>
    public IReadOnlyList<ActiveStatus> GetActiveStatuses(Target target)
    {
        if (_active.TryGetValue(target, out var list)) return list;
        return Array.Empty<ActiveStatus>();
    }

    // ─────────────────────────────────────────────────────────────
    // ITickable — 0.5s 批处理
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 累积 dt 算法：_accum += dt，每满 TickInterval 执行一轮全量遍历。
    /// 不在 Update 内 new 任何对象；过期索引用复用 buffer 收集后倒序移除。
    /// </summary>
    public void OnUpdate(float dt)
    {
        _accum += dt;

        // 未到 0.5s 阈值，直接返回（扣 RemainingSec 也在 tick 时批量处理）
        if (_accum < TickInterval) return;

        _accum -= TickInterval;

        // 遍历所有 target 的状态列表
        foreach (var kvp in _active)
        {
            Target            target = kvp.Key;
            List<ActiveStatus> list  = kvp.Value;

            _expiredIndexBuf.Clear();

            for (int i = 0; i < list.Count; i++)
            {
                ActiveStatus s = list[i];

                // tick 扣时间
                s.RemainingSec -= TickInterval;

                if (s.RemainingSec <= 0f)
                {
                    // BUG-20-01 修复：duration 正好是 TickInterval 整数倍时，
                    // 最后一 tick 伤害不应被 expired 分支静默丢失。
                    float lastTickDmg = s.DPS * TickInterval;
                    if (lastTickDmg > 0f)
                        _bus.Publish(new StatusEffectTickedEvent(target, s.Name, lastTickDmg));
                    _expiredIndexBuf.Add(i);
                }
                else
                {
                    // 存活：发 tick 事件（效果由订阅方执行，本模块只发信号）
                    float tickDmg = s.DPS * TickInterval;
                    list[i] = s;   // 写回更新后的 RemainingSec
                    _bus.Publish(new StatusEffectTickedEvent(target, s.Name, tickDmg));
                }
            }

            // 倒序移除过期项（避免索引错乱），并发 Expired 事件
            for (int ri = _expiredIndexBuf.Count - 1; ri >= 0; ri--)
            {
                int idx = _expiredIndexBuf[ri];
                string expiredName = list[idx].Name;
                list.RemoveAt(idx);
                _bus.Publish(new StatusEffectExpiredEvent(target, expiredName));

                FrameworkLogger.Info("StatusEffectModule",
                    $"Action=StatusExpired Target={target.Name} Status={expiredName}");
            }

            // 如果该 target 已无任何状态，记录待清理（不在 foreach 内改 dict）
            if (list.Count == 0)
            {
                _toRemoveTargets.Add(target);
            }
        }

        // 清理空 target 条目
        foreach (var t in _toRemoveTargets)
        {
            _active.Remove(t);
        }
        _toRemoveTargets.Clear();
    }

    // ─────────────────────────────────────────────────────────────
    // 事件桥接：EffectAppliedEvent → ApplyStatus
    // ─────────────────────────────────────────────────────────────

    [EventHandler]
    void OnEffectApplied(EffectAppliedEvent e)
    {
        if (e?.Results == null) return;
        if (e.Target == null) return;

        foreach (var r in e.Results)
        {
            if (string.IsNullOrEmpty(r.Status)) continue;

            if (!TryParseStatus(r.Status, out string name, out float dps, out float duration,
                                out float p1, out float p2))
            {
                FrameworkLogger.Warn("StatusEffectModule",
                    $"Action=ParseStatusFailed Raw=\"{r.Status}\"");
                continue;
            }

            ApplyStatus(e.Target, name, dps, duration, p1, p2);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 死亡清理：订阅 PlayerHealthChangedEvent（HP<=0）
    // ─────────────────────────────────────────────────────────────

    [EventHandler]
    void OnPlayerHealthChanged(PlayerHealthChangedEvent e)
    {
        if (e.Current > 0f) return;

        // 玩家 HP 归零：清理玩家身上所有状态
        // SpawnerModule.PlayerTarget 在 Category 1 不可 GetModule，
        // 改为遍历 _active 寻找名为 "玩家" 或 IsPlayer 标记的 Target。
        // 本期兜底策略：清除 _active 中所有 Health<=0 的 Target。
        _toRemoveTargets.Clear();
        foreach (var kvp in _active)
        {
            if (kvp.Key.Health <= 0f)
            {
                _toRemoveTargets.Add(kvp.Key);
            }
        }
        foreach (var dead in _toRemoveTargets)
        {
            ClearAllStatuses(dead);
        }
        _toRemoveTargets.Clear();
    }

    // EnemyDied 占位：TargetKilledEvent 已在 TattooEvents 定义，订阅后清理敌方状态
    [EventHandler]
    void OnTargetKilled(TargetKilledEvent e)
    {
        if (e?.Target == null) return;
        // 敌人/目标死亡 → 清除其所有 DoT（不再 tick 已死亡目标）
        ClearAllStatuses(e.Target);
    }

    // ─────────────────────────────────────────────────────────────
    // 私有：状态字符串解析
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 解析 status 字符串。
    /// 格式：
    ///   "Burn(dps=8,dur=3)"
    ///   "Poison(dps=5,dur=4,p1=2,p2=0.5)"
    ///   "Shock"（无参数：使用 dps=0,dur=0，调用方负责验证）
    /// 返回 false 表示解析失败（字符串未知 / 格式错误）。
    /// </summary>
    bool TryParseStatus(string raw, out string name, out float dps, out float duration,
                        out float param1, out float param2)
    {
        name     = null;
        dps      = 0f;
        duration = 0f;
        param1   = 0f;
        param2   = 0f;

        if (string.IsNullOrWhiteSpace(raw)) return false;

        raw = raw.Trim();

        // 提取名称部分（左括号前，或整个字符串）
        int parenStart = raw.IndexOf('(');
        string rawName = parenStart >= 0 ? raw.Substring(0, parenStart).Trim() : raw.Trim();

        if (!KnownStatus.IsKnown(rawName))
        {
            FrameworkLogger.Warn("StatusEffectModule",
                $"Action=UnknownStatus Name=\"{rawName}\" Raw=\"{raw}\" Skipped");
            return false;
        }

        name = rawName;

        // 无参数时（仅名称）：dps/dur 保持默认 0，由 ApplyStatus 的 duration<=0 守卫过滤
        if (parenStart < 0) return true;

        // 提取括号内内容
        int parenEnd = raw.IndexOf(')', parenStart);
        if (parenEnd < 0) return false; // 格式错误

        string body = raw.Substring(parenStart + 1, parenEnd - parenStart - 1);
        if (string.IsNullOrWhiteSpace(body)) return true;

        // 按逗号拆分键值对
        string[] pairs = body.Split(',');
        foreach (string pair in pairs)
        {
            int eq = pair.IndexOf('=');
            if (eq < 0) continue;

            string key = pair.Substring(0, eq).Trim();
            string val = pair.Substring(eq + 1).Trim();

            if (!float.TryParse(val,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out float fval))
            {
                continue;
            }

            switch (key)
            {
                case "dps":  dps      = fval; break;
                case "dur":  duration = fval; break;
                case "p1":   param1   = fval; break;
                case "p2":   param2   = fval; break;
            }
        }

        return true;
    }
}
