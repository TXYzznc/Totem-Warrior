using System.Collections.Generic;
using Economy;
using UnityEngine;

namespace Economy.Events
{
    // ───── 金币与物品事件 ─────

    /// <summary>金币变更原因枚举（CONTRACT §1.4 CoinChangedEvent.Reason）。</summary>
    public enum CoinChangeReason
    {
        ChestLoot,        // 开普通/精品宝箱
        DeathChestLoot,   // 拾取死亡宝箱金币
        ShopBuy,          // 商人购买
        ShopSell,         // 玩家出售物品
        Tattoo,           // 纹身师完成纹身扣费
        TattooInterrupt,  // 自纹身中断扣罚（50金）
        Enchant,          // 附魔消耗（200–500）
        DeathPenalty,     // 死亡折损（50%归零）
    }

    /// <summary>
    /// 任何金币增减后广播（CONTRACT §1.4）。
    /// HUD 订阅后做飘字；BotControllerModule 订阅后更新内部缓存。
    /// </summary>
    public sealed class CoinChangedEvent
    {
        /// <summary>金币所属 Actor。</summary>
        public readonly Actor Owner;
        /// <summary>本次变化量（正=增加，负=减少）。</summary>
        public readonly int Delta;
        /// <summary>变化后的新总量。</summary>
        public readonly int NewTotal;
        /// <summary>变更原因。</summary>
        public readonly CoinChangeReason Reason;

        public CoinChangedEvent(Actor owner, int delta, int newTotal, CoinChangeReason reason)
        {
            Owner    = owner;
            Delta    = delta;
            NewTotal = newTotal;
            Reason   = reason;
        }
    }

    /// <summary>
    /// 物品加入 Actor 库存后广播（CONTRACT §1.4）。
    /// HUD 订阅后做飘字；BotControllerModule 订阅后更新内部缓存。
    /// </summary>
    public sealed class ItemPickedEvent
    {
        /// <summary>拾取者 Actor。</summary>
        public readonly Actor Picker;
        /// <summary>物品 ItemId（对应 ItemConfig 表主键）。</summary>
        public readonly int ItemId;
        /// <summary>拾取数量。</summary>
        public readonly int Count;
        /// <summary>武器弹药（仅 ItemId 为武器类时填写，HUD 显示用，可选）。</summary>
        public readonly int Ammo;

        public ItemPickedEvent(Actor picker, int itemId, int count, int ammo = 0)
        {
            Picker = picker;
            ItemId = itemId;
            Count  = count;
            Ammo   = ammo;
        }
    }

    /// <summary>商店关闭事件。UIModule 关闭 ShopForm。</summary>
    public sealed class ShopClosedEvent
    {
        public readonly Actor Customer;
        public ShopClosedEvent(Actor customer) { Customer = customer; }
    }

    /// <summary>纹身装备变化事件（追加/替换 TattooSlot 后由 TattooModule 发布）。UIModule 刷新 Build 列表。</summary>
    public sealed class TattooEquippedEvent
    {
        public readonly Actor Owner;
        public readonly Tattoo.Data.TattooSlot NewSlot;
        public TattooEquippedEvent(Actor owner, Tattoo.Data.TattooSlot slot)
        {
            Owner   = owner;
            NewSlot = slot;
        }
    }

    // ───── 宝箱事件 ─────

    /// <summary>宝箱类型枚举。</summary>
    public enum ChestType
    {
        Common,    // 普通宝箱
        Premium,   // 精品宝箱
        Death,     // 死亡宝箱（玩家/Bot 掉落）
    }

    /// <summary>
    /// 宝箱打开事件（CONTRACT §1.4）。由 ChestInteractor（场景实体）发布。
    /// EconomyModule 订阅后按 LootTableConfig 掷骰写入 Opener 库存。
    ///
    /// 注意：同帧多个 Actor 开同一宝箱时，后到的 ChestOpenedEvent 被 EconomyModule 丢弃（宝箱已开标志）。
    /// </summary>
    public sealed class ChestOpenedEvent
    {
        /// <summary>开箱的 Actor。</summary>
        public readonly Actor Opener;
        /// <summary>宝箱类型。</summary>
        public readonly ChestType Type;
        /// <summary>宝箱实例 ID（用于防并发重复开箱）。</summary>
        public readonly int ChestInstanceId;

        public ChestOpenedEvent(Actor opener, ChestType type, int chestInstanceId)
        {
            Opener          = opener;
            Type            = type;
            ChestInstanceId = chestInstanceId;
        }
    }

    /// <summary>
    /// 死亡宝箱生成事件（CONTRACT §1.4）。
    /// EconomyModule 在 CalculateDeathChest 快照完成后发布。
    /// MapGenModule 订阅后在地图放置实体宝箱并广播骷髅位置。
    ///
    /// TempRecipeIds：死亡 Actor 身上已刻配方 floor(N/2) 的临时拓本 ID 列表，仅本局可用。
    /// </summary>
    public sealed class DeathChestSpawnedEvent
    {
        /// <summary>死亡的 Actor（快照来源）。</summary>
        public readonly Actor DeadActor;
        /// <summary>宝箱生成世界坐标（死亡位置）。</summary>
        public readonly Vector3 Pos;
        /// <summary>宝箱内所有物品的 ItemId 列表（颜料/武器/解药）。</summary>
        public readonly List<int> ItemIds;
        /// <summary>配方临时拓本 ID 列表（9000+ 段，本局副本，随宝箱一并携带）。</summary>
        public readonly List<int> TempRecipeIds;

        public DeathChestSpawnedEvent(Actor deadActor, Vector3 pos, List<int> itemIds, List<int> tempRecipeIds)
        {
            DeadActor    = deadActor;
            Pos          = pos;
            ItemIds      = itemIds;
            TempRecipeIds = tempRecipeIds;
        }
    }

    /// <summary>
    /// 死亡宝箱被拾取事件（CONTRACT §1.4）。由 ChestInteractor 发布（Type=Death）。
    /// EconomyModule 订阅后将宝箱内物资转移至 Looter 库存，并发布 ItemPickedEvent / CoinChangedEvent。
    /// </summary>
    public sealed class DeathChestLootedEvent
    {
        /// <summary>拾取者 Actor。</summary>
        public readonly Actor Looter;
        /// <summary>宝箱原主人（已死亡的 Actor）。</summary>
        public readonly Actor DeadActor;

        public DeathChestLootedEvent(Actor looter, Actor deadActor)
        {
            Looter    = looter;
            DeadActor = deadActor;
        }
    }

    // ───── NPC 事件 ─────

    /// <summary>
    /// 商人购买事件（CONTRACT §1.5）。由 NPCModule 在确认购买后发布。
    /// EconomyModule 订阅后扣除 Buyer 金币并写入物品。
    /// </summary>
    public sealed class ShopPurchaseEvent
    {
        /// <summary>购买者 Actor。</summary>
        public readonly Actor Buyer;
        /// <summary>购买物品 ItemId（对应 ItemConfig 表主键）。</summary>
        public readonly int ItemId;
        /// <summary>本次购买金币花费。</summary>
        public readonly int CostCoin;

        public ShopPurchaseEvent(Actor buyer, int itemId, int costCoin)
        {
            Buyer    = buyer;
            ItemId   = itemId;
            CostCoin = costCoin;
        }
    }

    /// <summary>
    /// 纹身师完成纹身会话事件（CONTRACT §1.5）。由 NPCModule 在纹身完成后发布。
    /// EconomyModule 订阅后扣除 Customer 颜料+金币，自增 SessionEngravings。
    /// </summary>
    public sealed class TattooSessionEndEvent
    {
        /// <summary>接受纹身的 Actor。</summary>
        public readonly Actor Customer;
        /// <summary>新刻的纹身槽（含 ColorId + Tier 信息）。</summary>
        public readonly Tattoo.Data.TattooSlot NewSlot;
        /// <summary>本次纹身金币费用。</summary>
        public readonly int CostCoin;
        /// <summary>颜色 ID（对应 TattooColorConfig）。</summary>
        public readonly int ColorId;
        /// <summary>颜料档位（1=Basic / 2=Standard / 3=Premium）。</summary>
        public readonly int InkTier;
        /// <summary>消耗颜料瓶数量（通常为 1）。</summary>
        public readonly int InkCount;

        public TattooSessionEndEvent(Actor customer, Tattoo.Data.TattooSlot newSlot,
            int costCoin, int colorId, int inkTier, int inkCount = 1)
        {
            Customer = customer;
            NewSlot  = newSlot;
            CostCoin = costCoin;
            ColorId  = colorId;
            InkTier  = inkTier;
            InkCount = inkCount;
        }
    }

    /// <summary>
    /// 纹身 Actor 死亡事件（CONTRACT §1.2）。由 CombatModule 在判定 Actor 血量归零后发布。
    /// EconomyModule 订阅后执行死亡宝箱半半规则并发布 DeathChestSpawnedEvent。
    /// </summary>
    public sealed class ActorDiedEvent
    {
        /// <summary>死亡的 Actor。</summary>
        public readonly Actor DeadActor;
        /// <summary>击杀者 Actor（可为 null，如毒圈伤害）。</summary>
        public readonly Actor Killer;
        /// <summary>死亡世界坐标（用于宝箱生成位置）。</summary>
        public readonly Vector3 DeathPos;

        public ActorDiedEvent(Actor deadActor, Actor killer, Vector3 deathPos)
        {
            DeadActor = deadActor;
            Killer    = killer;
            DeathPos  = deathPos;
        }
    }

    /// <summary>
    /// 自纹身读条中断事件（CONTRACT §1.3 TattooCancelledEvent 扩展）。
    /// 由 TattooModule 在读条被中断时发布（Damaged / Moved / Killed / UserAbort）。
    /// EconomyModule 订阅后扣除 50 金币中断惩罚。
    /// </summary>
    public sealed class TattooInterruptedEvent
    {
        /// <summary>被中断的 Actor（执行自纹身的玩家/Bot）。</summary>
        public readonly Actor Actor;
        /// <summary>中断原因。</summary>
        public readonly Tattoo.Events.CancelReason Reason;

        public TattooInterruptedEvent(Actor actor, Tattoo.Events.CancelReason reason)
        {
            Actor  = actor;
            Reason = reason;
        }
    }
}
