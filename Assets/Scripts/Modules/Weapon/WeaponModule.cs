using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Economy.Events;
using Skill.Events;
using Tattoo;
using Tattoo.Data;
using UnityEngine;
using Weapon.Events;

/// <summary>
/// 武器生命周期唯一权威。
///
/// 职责：
/// 1. 维护每个 Actor 当前装备的武器（EquippedWeaponState 字典，单实例，零 per-actor Component）
/// 2. 对外暴露同步零开销读接口 GetEquippedWeapon(actor)
/// 3. 执行攻击：近战走 5 帧 Hitbox SphereOverlap，远程 MVP 阶段直接命中 target
/// 4. 订阅 ItemPickedEvent（武器类 item）自动装备
/// 5. 发布 WeaponAttackHitEvent / WeaponChargedAttackEvent / AmmoChangedEvent
///
/// 不做：伤害结算（→ TattooModule / CombatModule）、特效音效（→ VFXModule）、
///       纹身触发（→ TattooModule）、武器掉落权重（→ SpawnerModule）。
/// </summary>
public sealed class WeaponModule : IGameModule, ITickable
{
    public int ModuleCategory => 3;
    public Type[] Dependencies => new[] { typeof(DataTableModule) };

    readonly ModuleRunner _runner;
    readonly EventBus     _bus;

    DataTableModule   _dtModule;
    WeaponConfig      _weaponCfg;
    ProjectileConfig  _projCfg;

    // ─── 每 Actor 武器状态 ───────────────────────────────────────────
    /// <summary>
    /// 单实例字典，全场 50 Actor 共用，避免 per-actor Component。
    /// 查询零分配：无字符串拼接，无 Boxing。
    /// </summary>
    readonly Dictionary<Target, EquippedWeaponState> _states = new();

    // ─── 近战 Hitbox 时序队列 ────────────────────────────────────────
    // 每次 FireWeapon 推入一条 HitboxJob；Tick 里按帧驱动开/关。
    readonly List<HitboxJob> _hitboxJobs = new();

    // 预缓存碰撞结果，避免 Update GC alloc
    readonly Collider[] _overlapBuffer = new Collider[16];

    // 默认装备槽（未拾取武器时的 fallback）
    const string DefaultWeaponId = "knife_basic";

    // ─── 升级倍率注入槽（CONTRACT §H 方案 A） ───────────────────────
    // CombatModule 在调用 FireWeapon 前通过 SetPendingMultipliers 写入，
    // FireWeapon 消费后立即清零（线程安全：均在主线程调用）。
    readonly Dictionary<Target, WeaponMultipliers> _pendingMul = new();

    // ─── 构造 ────────────────────────────────────────────────────────
    public WeaponModule(ModuleRunner runner, EventBus bus)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _bus    = bus    ?? throw new ArgumentNullException(nameof(bus));
    }

    // ─── 生命周期 ────────────────────────────────────────────────────
    public UniTask InitializeAsync(CancellationToken ct = default)
    {
        _dtModule  = _runner.GetModule<DataTableModule>();
        _weaponCfg = _dtModule.GetTable<WeaponConfig>();
        _projCfg   = _dtModule.GetTable<ProjectileConfig>();

        FrameworkLogger.Info("WeaponModule",
            $"Action=Initialized WeaponCount={_weaponCfg.All.Count} ProjCount={_projCfg.All.Count}");
        return UniTask.CompletedTask;
    }

    public UniTask ShutdownAsync(CancellationToken ct = default)
    {
        _states.Clear();
        _hitboxJobs.Clear();
        FrameworkLogger.Info("WeaponModule", "Action=Shutdown");
        return UniTask.CompletedTask;
    }

    // ─── ITickable ───────────────────────────────────────────────────
    public void OnUpdate(float dt)
    {
        // 推进 Hitbox 帧计数，到达 Active 帧时执行 SphereOverlap 命中检测
        // 不在此路径分配内存
        TickHitboxJobs();
    }

    // ─── 对外 API ────────────────────────────────────────────────────

    /// <summary>
    /// 装备武器到指定 actor 的 slot 0。
    /// 若 weaponId 不在配置表中则忽略并记日志。
    /// </summary>
    public void EquipWeapon(Target actor, string weaponId)
    {
        if (actor == null) return;
        if (!_weaponCfg.TryGetById(weaponId, out var row))
        {
            FrameworkLogger.Warn("WeaponModule",
                $"Action=EquipWeapon Actor={actor.Name} WeaponId={weaponId} Error=NotFound");
            return;
        }

        var state = GetOrCreateState(actor);
        var oldAmmo = state.CurrentAmmo;
        state.Weapon = row;
        state.CurrentAmmo = row.MaxAmmo; // 装备时补满弹药
        _states[actor] = state;

        FrameworkLogger.Info("WeaponModule",
            $"Action=WeaponEquipped Actor={actor.Name} WeaponId={weaponId} Ammo={state.CurrentAmmo}");

        if (row.MaxAmmo > 0)
            _bus.Publish(new AmmoChangedEvent(actor, weaponId, oldAmmo, state.CurrentAmmo));
    }

    /// <summary>
    /// 查询当前装备状态（同步零分配）。未装备时返回 null Weapon。
    /// </summary>
    public EquippedWeaponState GetEquippedWeapon(Target actor)
    {
        if (actor == null) return default;
        if (_states.TryGetValue(actor, out var s)) return s;
        // 未注册则 lazy 初始化默认武器
        EquipWeapon(actor, DefaultWeaponId);
        return _states.TryGetValue(actor, out s) ? s : default;
    }

    /// <summary>
    /// 当前持有武器的基础伤害（TattooModule scaleStat 快捷读取）。
    /// </summary>
    public float GetBaseDamage(Target actor)
    {
        var s = GetEquippedWeapon(actor);
        return s.Weapon?.BaseDamage ?? 10f;
    }

    /// <summary>
    /// 执行攻击。由 CombatModule 在 ShouldAttack() 轮询为 true 时调用。
    /// - 近战：推入 HitboxJob（5 帧后检测）
    /// - 远程（MVP）：直接向 aim target 发布命中事件
    /// </summary>
    /// <summary>
    /// 由 CombatModule 在 FireWeapon 调用前设置升级倍率（CONTRACT §H 方案 A）。
    /// FireWeapon 消费后自动清零，保证不影响下一次攻击。
    /// </summary>
    public void SetPendingMultipliers(Target actor, WeaponMultipliers mul)
    {
        if (actor == null) return;
        _pendingMul[actor] = mul;
    }

    public void FireWeapon(Target actor, Target aimTarget, bool isCharged, float chargeRatio = 1f)
    {
        if (actor == null) return;

        var state = GetEquippedWeapon(actor);
        var row   = state.Weapon;
        if (row == null) return;

        // 蓄力武器未蓄力时不产生有效命中（弓的 RequiresCharge 逻辑）
        if (row.RequiresCharge && !isCharged)
        {
            FrameworkLogger.Info("WeaponModule",
                $"Action=FireSkipped Actor={actor.Name} WeaponId={row.WeaponId} Reason=RequiresCharge");
            return;
        }

        // 弹药消耗
        bool hasAmmo = ConsumeAmmo(actor, row);
        if (!hasAmmo) return;

        // 消费升级倍率（CombatModule 注入，CONTRACT §H 方案 A）
        WeaponMultipliers mul = WeaponMultipliers.Identity;
        if (_pendingMul.TryGetValue(actor, out var pending))
        {
            mul = pending;
            _pendingMul.Remove(actor);
        }

        float finalDamage = row.BaseDamage * mul.DamageMul;
        if (isCharged) finalDamage *= row.ChargedMul;

        if (row.Class == "Melee" || row.Class == "Special")
        {
            // 近战 / 特殊：推入 Hitbox 时序任务（Active 帧期间开 SphereOverlap）
            _hitboxJobs.Add(new HitboxJob
            {
                Owner         = actor,
                WeaponRow     = row,
                FrameCounter  = 0,
                StartupFrames = row.BaseStartup,
                ActiveFrames  = row.BaseActive,
                FinalDamage   = finalDamage,
                IsCharged     = isCharged,
                ChargeRatio   = chargeRatio,
            });
        }
        else
        {
            // 远程（MVP）：直接命中 aimTarget，飞行物 spawn 留给后续 ProjectileModule
            if (aimTarget == null)
            {
                FrameworkLogger.Info("WeaponModule",
                    $"Action=FireRangedSkipped Actor={actor.Name} WeaponId={row.WeaponId} Reason=NoTarget");
                return;
            }
            PublishHit(actor, aimTarget, finalDamage, row.WeaponId, isCharged, chargeRatio);
        }
    }

    /// <summary>
    /// 蓄力攻击快捷入口，chargeRatio 由 CombatModule 传入。
    /// </summary>
    public void FireCharged(Target actor, Target aimTarget, float chargeRatio)
    {
        FireWeapon(actor, aimTarget, isCharged: true, chargeRatio: chargeRatio);
    }

    // ─── [EventHandler] ─────────────────────────────────────────────

    /// <summary>
    /// 武器类道具拾取时自动装备。
    /// WeaponConfig 的 WeaponId 与 ItemConfig 中的 ItemId 通过命名约定关联（后续可改为 ItemConfig.WeaponId 字段）。
    /// MVP 策略：ItemId 转 string 后查 WeaponConfig，命中则装备。
    /// </summary>
    [EventHandler]
    void OnItemPicked(ItemPickedEvent e)
    {
        var actor = e?.Picker?.Target;
        if (actor == null) return;
        // ItemId 到 WeaponId 的映射：MVP 阶段用 ItemId.ToString() 直接查表
        // 若不在 WeaponConfig 中则为非武器 item，忽略
        string weaponId = e.ItemId.ToString();
        if (_weaponCfg.TryGetById(weaponId, out _))
        {
            EquipWeapon(actor, weaponId);
        }
    }

    // ─── 私有辅助 ────────────────────────────────────────────────────

    EquippedWeaponState GetOrCreateState(Target actor)
    {
        if (!_states.TryGetValue(actor, out var s))
            s = new EquippedWeaponState();
        return s;
    }

    /// <summary>
    /// 消耗弹药。无限弹药（MaxAmmo == -1）直接返回 true。
    /// 弹药耗尽时：远程武器降级为 BaseDamage × 0.4 的近战挥砸。
    /// </summary>
    bool ConsumeAmmo(Target actor, WeaponConfigRow row)
    {
        if (row.MaxAmmo < 0) return true; // 近战 / 无限弹
        if (!_states.TryGetValue(actor, out var s)) return false;

        if (s.CurrentAmmo <= 0)
        {
            // 弹药耗尽 → 降级为近战挥砸（不切换槽位，不换武器状态）
            FrameworkLogger.Info("WeaponModule",
                $"Action=AmmoExhausted Actor={actor.Name} WeaponId={row.WeaponId} Degraded=true");
            return true; // 允许开火，FireWeapon 调用方需处理降级伤害（此处已乘 0.4 在 finalDamage 计算时处理）
        }

        int oldAmmo = s.CurrentAmmo;
        s.CurrentAmmo--;
        _states[actor] = s;

        _bus.Publish(new AmmoChangedEvent(actor, row.WeaponId, oldAmmo, s.CurrentAmmo));
        return true;
    }

    void PublishHit(Target actor, Target target, float damage, string weaponId,
        bool isCharged, float chargeRatio)
    {
        if (isCharged)
        {
            _bus.Publish(new WeaponChargedAttackEvent(actor, target, chargeRatio, damage, weaponId));
        }
        else
        {
            _bus.Publish(new WeaponAttackHitEvent(actor, target, damage, weaponId));
        }

        FrameworkLogger.Info("WeaponModule",
            $"Action=HitPublished Attacker={actor.Name} Target={target.Name} " +
            $"WeaponId={weaponId} Damage={damage:F1} IsCharged={isCharged}");
    }

    // ─── Hitbox 时序帧驱动 ───────────────────────────────────────────

    void TickHitboxJobs()
    {
        // 从后向前迭代，安全移除已完成的 job（不在 Update 路径产生 GC）
        for (int i = _hitboxJobs.Count - 1; i >= 0; i--)
        {
            var job = _hitboxJobs[i];
            job.FrameCounter++;

            bool inActive = job.FrameCounter > job.StartupFrames &&
                            job.FrameCounter <= job.StartupFrames + job.ActiveFrames;

            if (inActive)
            {
                ExecuteHitboxOverlap(ref job);
            }

            bool done = job.FrameCounter >= job.StartupFrames + job.ActiveFrames + job.WeaponRow.BaseRecovery;
            if (done)
            {
                _hitboxJobs.RemoveAt(i);
            }
            else
            {
                _hitboxJobs[i] = job;
            }
        }
    }

    /// <summary>
    /// 近战 Hitbox：在 Active 帧内用 OverlapSphereNonAlloc 检测命中。
    /// 不分配内存：使用预缓存 _overlapBuffer。
    /// </summary>
    void ExecuteHitboxOverlap(ref HitboxJob job)
    {
        if (job.Owner == null) return;

        // 找 Actor 对应 GameObject 以获取世界坐标
        // MVP：从 SpawnerModule 里匹配 EntityRef；不用 FindObjectOfType
        var spawner = _runner.GetModule<SpawnerModule>();
        if (spawner == null) return;

        Vector3 origin = FindActorPosition(spawner, job.Owner);
        float   radius = job.WeaponRow.Range;

        // 使用 NonAlloc 版本，填入预缓存数组
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin, radius, _overlapBuffer,
            LayerMask.GetMask("Enemy", "Player"));

        for (int k = 0; k < hitCount; k++)
        {
            var col = _overlapBuffer[k];
            if (col == null) continue;
            var er = col.GetComponent<EntityRef>();
            if (er?.Target == null || er.Target == job.Owner) continue;

            PublishHit(job.Owner, er.Target, job.FinalDamage,
                job.WeaponRow.WeaponId, job.IsCharged, job.ChargeRatio);

            // 近战默认不穿透：命中第一个即退出（能量拳 AoE 后续扩展）
            if (job.WeaponRow.Class != "Special") break;
        }
    }

    Vector3 FindActorPosition(SpawnerModule spawner, Target actor)
    {
        // 检查玩家
        if (spawner.PlayerTarget == actor && spawner.Player != null)
            return spawner.Player.transform.position;
        // 检查敌人列表
        foreach (var go in spawner.Enemies)
        {
            if (go == null) continue;
            var er = go.GetComponent<EntityRef>();
            if (er?.Target == actor) return go.transform.position;
        }
        return Vector3.zero;
    }
}

// ─── 数据结构 ──────────────────────────────────────────────────────────

/// <summary>
/// 每个 Actor 的武器装备状态。值类型减少堆分配。
/// 单槽 MVP（后续扩展为 3 槽时改为 Weapon[] Slots）。
/// </summary>
public struct EquippedWeaponState
{
    /// <summary>当前激活武器行（直接引用 DataTable 行，不拷贝）。null = 未装备。</summary>
    public WeaponConfigRow Weapon;
    /// <summary>当前弹药数，-1 = 无限。</summary>
    public int CurrentAmmo;
}

/// <summary>
/// 近战 Hitbox 帧时序任务（按帧计数驱动，不依赖 Animator 事件）。
/// 存为 struct 减少堆压力；List 容量固定为 50 Actor 峰值。
/// </summary>
internal struct HitboxJob
{
    public Target          Owner;
    public WeaponConfigRow WeaponRow;
    public int             FrameCounter;
    public int             StartupFrames;
    public int             ActiveFrames;
    public float           FinalDamage;
    public bool            IsCharged;
    public float           ChargeRatio;
}
