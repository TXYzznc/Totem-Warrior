using Tattoo.Data;
using UnityEngine;

// ============================================================
// change #18 weapon-pickup-and-upgrade —— 新增事件总表
//
// 本文件冻结于 B2 公共骨架；B3 fan-out（18-A/B/C）不得修改任一签名。
// CONTRACT.md §A 是 source of truth。
//
// 命名约定（CONTRACT §F）：
//   - 顶层无 namespace（与项目现有 WeaponPickupEvents / TattooEvents 文件
//     按 CONTRACT 要求统一，方便跨模块直接 using 不引）
//   - 字段命名遵循项目现有事件风格（PascalCase + public field）
//
// 设计裁定（CONTRACT §A.3）：
//   不引入 EnemyDeadEvent，复用现有 Tattoo.Events.EnemyDiedEvent：
//   精英判定 → e.DeadActor.Tier == EnemyTier.Elite
//   死亡坐标 → e.DeathPos
// ============================================================

/// <summary>
/// 玩家确认拾取场上武器时发布。
/// 发布方：WeaponPickupTrigger（MonoBehaviour），在 InputModule.GetKeyDown("Pickup") 后。
/// 订阅方：WeaponUpgradeModule（升级判定）/ WeaponSpawnerModule（销毁拾取 GO）
/// 规格：spec.md §1.1。
/// </summary>
public sealed class WeaponPickedUpEvent
{
    /// <summary>拾取的玩家 Target（MUST NOT be null）。</summary>
    public Target  Actor;
    /// <summary>对应 WeaponConfig.WeaponId（MUST 存在于配置表）。</summary>
    public string  WeaponId;
    /// <summary>武器 GO 的世界坐标（用于特效定位，NOT 玩家坐标）。</summary>
    public Vector3 PickupPosition;

    public WeaponPickedUpEvent(Target actor, string weaponId, Vector3 pickupPosition)
    {
        Actor          = actor;
        WeaponId       = weaponId;
        PickupPosition = pickupPosition;
    }
}

/// <summary>
/// 同类武器升级成功时发布（L1→L2 或 L2→L3）。
/// 发布方：WeaponUpgradeModule.TryUpgrade
/// 订阅方：CombatHUDForm（UI 等级标记）/ CombatModule（倍率注入缓存更新）
/// 规格：spec.md §1.2。
/// </summary>
public sealed class WeaponUpgradedEvent
{
    /// <summary>升级的玩家。</summary>
    public Target Actor;
    /// <summary>升级的武器 ID。</summary>
    public string WeaponId;
    /// <summary>升级后等级（SHALL be 2 or 3）。</summary>
    public int    NewLevel;
    /// <summary>新等级的伤害倍率，= Mathf.Pow(1.2f, NewLevel-1)。</summary>
    public float  DamageMul;
    /// <summary>新等级的射程增量 [m]，= 0.5f * (NewLevel-1)。</summary>
    public float  RangeAdd;
    /// <summary>新等级的冷却倍率，= Mathf.Pow(0.9f, NewLevel-1)。</summary>
    public float  CooldownMul;

    public WeaponUpgradedEvent(Target actor, string weaponId, int newLevel,
        float damageMul, float rangeAdd, float cooldownMul)
    {
        Actor       = actor;
        WeaponId    = weaponId;
        NewLevel    = newLevel;
        DamageMul   = damageMul;
        RangeAdd    = rangeAdd;
        CooldownMul = cooldownMul;
    }
}

/// <summary>
/// 玩家开宝箱后发布。
/// 发布方：ChestInteractTrigger.OnInteract（按 F 后）
/// 订阅方：WeaponSpawnerModule（结算奖励：spawn 武器 GO 或加金币）
/// 规格：spec.md §1.3。
/// </summary>
public sealed class ChestOpenedEvent
{
    /// <summary>对应 ChestConfig.ChestId（如 "chest_common"）。</summary>
    public string  ChestId;
    /// <summary>"Weapon" 或 "Gold"（按 ChestConfig 概率已决定）。</summary>
    public string  RewardType;
    /// <summary>RewardType="Weapon" 时填 WeaponId；"Gold" 时填空串。</summary>
    public string  RewardId;
    /// <summary>Weapon 固定为 1；Gold 为具体金币数。</summary>
    public int     RewardAmount;
    /// <summary>宝箱世界坐标（武器 spawn 位置偏移用）。</summary>
    public Vector3 ChestPosition;

    public ChestOpenedEvent(string chestId, string rewardType, string rewardId,
        int rewardAmount, Vector3 chestPosition)
    {
        ChestId       = chestId;
        RewardType    = rewardType;
        RewardId      = rewardId;
        RewardAmount  = rewardAmount;
        ChestPosition = chestPosition;
    }
}

/// <summary>
/// 玩家在商人处成功购买武器时发布（金币已验证足够）。
/// 发布方：MerchantTrigger（UI 按钮 onClick，金币检查通过后）
/// 订阅方：WeaponSpawnerModule（扣金 + 发 WeaponPickedUpEvent）
/// 规格：spec.md §1.4。
/// </summary>
public sealed class MerchantPurchaseEvent
{
    /// <summary>购买的玩家。</summary>
    public Target Actor;
    /// <summary>购买的武器 ID。</summary>
    public string WeaponId;
    /// <summary>本次扣除的金币数（从 MerchantConfig 读取，非计算值）。</summary>
    public int    GoldCost;

    public MerchantPurchaseEvent(Target actor, string weaponId, int goldCost)
    {
        Actor    = actor;
        WeaponId = weaponId;
        GoldCost = goldCost;
    }
}

/// <summary>
/// 玩家拾取已满级（L3）的同类武器，转化为金币时发布。
/// 发布方：WeaponUpgradeModule（TryUpgrade 中 level==3 分支）
/// 订阅方：EconomyModule（AddGold）/ CombatHUDForm（显示"已满级，转化为 X 金币"提示）
/// 规格：spec.md §1.5。
/// </summary>
public sealed class WeaponMaxLevelConvertEvent
{
    /// <summary>触发玩家。</summary>
    public Target Actor;
    /// <summary>武器 ID。</summary>
    public string WeaponId;
    /// <summary>转化的金币数，= Mathf.RoundToInt(MerchantConfig.GoldCost * 0.5f)。</summary>
    public int    GoldConverted;

    public WeaponMaxLevelConvertEvent(Target actor, string weaponId, int goldConverted)
    {
        Actor         = actor;
        WeaponId      = weaponId;
        GoldConverted = goldConverted;
    }
}

/// <summary>
/// 武器拾取 GO 在场上生成后发布（用于调试日志、UI 提示、TC-15 验证）。
/// 发布方：WeaponSpawnerModule.SpawnDroppedWeapon（Instantiate 完成后）。
/// 订阅方：无强制订阅方，TC-15 通过 console_get_logs filter=WeaponSpawned 验证。
/// 规格：plan-22tc.md TC-15。
/// </summary>
public sealed class WeaponSpawnedEvent
{
    /// <summary>生成的武器 ID（对应 WeaponConfig.WeaponId）。</summary>
    public string  WeaponId;
    /// <summary>生成的世界坐标。</summary>
    public Vector3 Position;
    /// <summary>生成的武器 pickup GameObject 实例（可为 null，仅作调试用）。</summary>
    public GameObject Instance;

    public WeaponSpawnedEvent(string weaponId, Vector3 position, GameObject instance = null)
    {
        WeaponId = weaponId;
        Position = position;
        Instance = instance;
    }
}
