using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tattoo.Data;
using UnityEngine;

/// <summary>
/// 武器升级公式 + 拾取判定 + 倍率只读查询。
///
/// 职责（CONTRACT §B / spec.md §3 / design.md §3.2）：
/// 1. 订阅 WeaponPickedUpEvent → 同武器 ? TryUpgrade : WeaponModule.EquipWeapon
/// 2. 维护 _actorLevels 字典（兜底等级存储，key=(Target, weaponId)）
/// 3. 对外提供 GetWeaponLevel / GetMultipliers / TryUpgrade 三个只读/事件型 API
///
/// 不做：
/// - 不管场上 GO（→ WeaponSpawnerModule）
/// - 不管 UI（→ CombatHUDForm）
/// - 不订阅 WeaponAttackHitEvent（CONTRACT §H 裁定方案 A — CombatModule 主动注入倍率）
/// </summary>
public sealed class WeaponUpgradeModule : IGameModule
{
    // ─── 生命周期声明 ────────────────────────────────────────────────
    public int    ModuleCategory => 3;
    public Type[] Dependencies   => new[] { typeof(WeaponModule), typeof(DataTableModule) };

    readonly ModuleRunner _runner;
    readonly EventBus     _bus;

    // ─── 倍率缓存（CONTRACT §H.4 性能预算） ──────────────────────────
    /// <summary>升级倍率缓存。key=(actor, weaponId), value=已计算的乘数。WeaponUpgradedEvent 后 Invalidate。</summary>
    readonly Dictionary<(Target, string), WeaponMultipliers> _mulCache = new();

    // ─── 等级存储（兜底：EconomyModule.GetActor 不存在时用此字典） ──
    /// <summary>key=(Target, weaponId), value=武器等级(1~3)。MUST NOT lazy-write default。</summary>
    readonly Dictionary<(Target, string), int> _actorLevels = new();

    // ─── DataTable ────────────────────────────────────────────────────
    MerchantConfig _merchantCfg;

    // ─── 等级上限（ROADMAP §0 冻结） ────────────────────────────────
    public const int MaxLevel = 3;

    // ─── 构造 ────────────────────────────────────────────────────────
    public WeaponUpgradeModule(ModuleRunner runner, EventBus bus)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _bus    = bus    ?? throw new ArgumentNullException(nameof(bus));
    }

    // ─── IGameModule ─────────────────────────────────────────────────
    public UniTask InitializeAsync(CancellationToken ct = default)
    {
        _merchantCfg = _runner.GetModule<DataTableModule>().GetTable<MerchantConfig>();
        FrameworkLogger.Info("WeaponUpgradeModule", "Action=Initialized");
        return UniTask.CompletedTask;
    }

    public UniTask ShutdownAsync(CancellationToken ct = default)
    {
        _mulCache.Clear();
        _actorLevels.Clear();
        FrameworkLogger.Info("WeaponUpgradeModule", "Action=Shutdown");
        return UniTask.CompletedTask;
    }

    // ─── 对外 API（签名冻结于 CONTRACT §C / spec.md §3） ────────────

    /// <summary>
    /// 获取指定 actor 对指定武器的当前等级。
    /// 未记录时返回 1（不抛异常，lazy default）。
    /// MUST NOT 修改任何字典。
    /// </summary>
    public int GetWeaponLevel(Target actor, string weaponId)
    {
        if (actor == null || string.IsNullOrEmpty(weaponId)) return 1;
        if (_actorLevels.TryGetValue((actor, weaponId), out var lv))
            return lv;
        return 1;
    }

    /// <summary>
    /// 获取指定 actor + weaponId 的当前升级乘数（CombatModule 调用，CONTRACT §H 方案 A）。
    /// 按 (actor, weaponId) 预计算缓存；WeaponUpgradedEvent 后 Invalidate。
    /// 公式（design.md §2.2）：
    ///   DamageMul   = 1.2^(level-1)
    ///   RangeAdd    = 0.5 * (level-1)  [m]
    ///   CooldownMul = 0.9^(level-1)
    /// 只读 API：禁止在此方法内修改任何状态 / publish 事件（CONTRACT §H.3）。
    /// </summary>
    public WeaponMultipliers GetMultipliers(Target actor, string weaponId)
    {
        if (actor == null || string.IsNullOrEmpty(weaponId))
            return WeaponMultipliers.Identity;

        if (_mulCache.TryGetValue((actor, weaponId), out var cached))
            return cached;

        int level = GetWeaponLevel(actor, weaponId);
        var m = new WeaponMultipliers(
            damageMul:   Mathf.Pow(1.2f, level - 1),
            rangeAdd:    0.5f * (level - 1),
            cooldownMul: Mathf.Pow(0.9f, level - 1));

        _mulCache[(actor, weaponId)] = m;
        return m;
    }

    /// <summary>
    /// 尝试升级武器。
    /// - 当前装备武器与 weaponId 不同 → return false（不在此 API 范畴）
    /// - 同类 + 未满级 → 升级 → WeaponUpgradedEvent → return true
    /// - 已满级 → WeaponMaxLevelConvertEvent → return false
    ///
    /// MUST NOT 直接修改 PlayerStats（升级只改 _actorLevels + 发事件，
    /// 倍率由 GetMultipliers 动态算）。
    /// </summary>
    public bool TryUpgrade(Target actor, string weaponId)
    {
        if (actor == null || string.IsNullOrEmpty(weaponId)) return false;

        var weaponMod = _runner.GetModule<WeaponModule>();
        var currentWeaponId = weaponMod.GetEquippedWeapon(actor).Weapon?.WeaponId;
        if (currentWeaponId != weaponId)
        {
            FrameworkLogger.Info("WeaponUpgradeModule",
                $"Action=TryUpgrade Actor={actor.Name} WeaponId={weaponId} Result=Skip Reason=NotEquipped CurrentWeapon={currentWeaponId}");
            return false;
        }

        int level = GetWeaponLevel(actor, weaponId);

        if (level < MaxLevel)
        {
            int newLevel = level + 1;
            _actorLevels[(actor, weaponId)] = newLevel;
            InvalidateCache(actor, weaponId);

            var m = GetMultipliers(actor, weaponId);
            _bus.Publish(new WeaponUpgradedEvent(actor, weaponId, newLevel, m.DamageMul, m.RangeAdd, m.CooldownMul));

            FrameworkLogger.Info("WeaponUpgradeModule",
                $"Action=WeaponUpgraded Actor={actor.Name} WeaponId={weaponId} NewLevel={newLevel} " +
                $"DamageMul={m.DamageMul:F3} RangeAdd={m.RangeAdd:F2} CooldownMul={m.CooldownMul:F3}");
            return true;
        }
        else
        {
            int goldConverted = ComputeConvertGold(weaponId);
            _bus.Publish(new WeaponMaxLevelConvertEvent(actor, weaponId, goldConverted));

            FrameworkLogger.Info("WeaponUpgradeModule",
                $"Action=MaxLevelConvert Actor={actor.Name} WeaponId={weaponId} GoldConverted={goldConverted}");
            return false;
        }
    }

    // ─── [EventHandler] ─────────────────────────────────────────────

    /// <summary>
    /// 玩家拾取武器后的升级 / 替换分支。
    /// MUST: e.WeaponId == currentWeaponId → TryUpgrade；否则 WeaponModule.EquipWeapon。
    /// </summary>
    [EventHandler]
    void OnWeaponPickedUp(WeaponPickedUpEvent e)
    {
        if (e?.Actor == null || string.IsNullOrEmpty(e.WeaponId)) return;

        var weaponMod = _runner.GetModule<WeaponModule>();
        var currentWeaponId = weaponMod.GetEquippedWeapon(e.Actor).Weapon?.WeaponId;

        if (currentWeaponId == e.WeaponId)
        {
            TryUpgrade(e.Actor, e.WeaponId);
        }
        else
        {
            weaponMod.EquipWeapon(e.Actor, e.WeaponId);
            FrameworkLogger.Info("WeaponUpgradeModule",
                $"Action=WeaponReplaced Actor={e.Actor.Name} OldWeapon={currentWeaponId} NewWeapon={e.WeaponId}");
        }
    }

    // ─── 私有辅助 ────────────────────────────────────────────────────

    void InvalidateCache(Target actor, string weaponId)
    {
        _mulCache.Remove((actor, weaponId));
    }

    /// <summary>
    /// 满级转化金币 = Round(MerchantConfig.GoldCost * 0.5f)。
    /// 找不到配置行时 fallback 25。
    /// </summary>
    int ComputeConvertGold(string weaponId)
    {
        foreach (var row in _merchantCfg.Rows)
        {
            if (row.WeaponId == weaponId)
                return Mathf.RoundToInt(row.GoldCost * 0.5f);
        }
        return 25;
    }
}

/// <summary>
/// 升级乘数（只读 readonly struct，零堆分配）。
/// CONTRACT §H.4：CombatModule 每次 FireWeapon 前查一次，O(1) Dictionary 查询。
/// </summary>
public readonly struct WeaponMultipliers
{
    /// <summary>伤害倍率，L1=1.0, L2=1.2, L3=1.44。</summary>
    public readonly float DamageMul;
    /// <summary>射程增量 [m]，L1=0, L2=0.5, L3=1.0。</summary>
    public readonly float RangeAdd;
    /// <summary>冷却倍率，L1=1.0, L2=0.9, L3=0.81。</summary>
    public readonly float CooldownMul;

    public WeaponMultipliers(float damageMul, float rangeAdd, float cooldownMul)
    {
        DamageMul   = damageMul;
        RangeAdd    = rangeAdd;
        CooldownMul = cooldownMul;
    }

    /// <summary>L1 默认乘数（无升级时返回，调用方零分支即可直接乘）。</summary>
    public static readonly WeaponMultipliers Identity = new(1f, 0f, 1f);
}
