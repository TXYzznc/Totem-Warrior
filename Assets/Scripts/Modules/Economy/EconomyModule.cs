using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Economy.Events;
using UnityEngine;

namespace Economy
{
    // ───────────────────────────────────────────────────────────────────────────
    // 数据结构
    // ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 颜料二元键（颜色 ID + 档位）。struct 无装箱，Dictionary 零 GC。
    /// ColorId: 1–7（对应 TattooColorConfig），Tier: 1=Basic / 2=Standard / 3=Premium。
    /// </summary>
    public readonly struct InkKey : IEquatable<InkKey>
    {
        public readonly int ColorId;
        public readonly int Tier;

        public InkKey(int colorId, int tier)
        {
            ColorId = colorId;
            Tier    = tier;
        }

        public bool Equals(InkKey other) => ColorId == other.ColorId && Tier == other.Tier;
        public override bool Equals(object obj) => obj is InkKey other && Equals(other);
        public override int GetHashCode() => ColorId * 10 + Tier;
        public override string ToString() => $"Ink(Color={ColorId},Tier={Tier})";
    }

    /// <summary>
    /// 单 Actor 运行时库存。
    /// 不存字符串，物品以 ItemId(int) 存储，展示名通过 DataTable 查询。
    /// </summary>
    public sealed class ActorInventory
    {
        /// <summary>金币总量（最低 0，不为负）。</summary>
        public int Coins;

        /// <summary>
        /// 颜料瓶分档存量：(ColorId, Tier) → 数量。
        /// 最多 21 条目（7色×3档），已预分配容量。
        /// </summary>
        public readonly Dictionary<InkKey, int> InkBottles = new(21);

        /// <summary>配方碎片数量（3个可合成一份完整配方）。</summary>
        public int RecipeShards;

        /// <summary>本局已完成的纹身数（用于死亡宝箱配方拓本 floor(N/2) 计算）。</summary>
        public int SessionEngravings;

        /// <summary>永久解锁配方 ID 列表（已学习，可跨局使用）。</summary>
        public readonly List<int> RecipeIds = new();

        /// <summary>
        /// 临时配方拓本 ID 列表（9000+ 段，本局可用，局结束销毁）。
        /// 死亡宝箱拾取后转移至此，Looter 拾取后本局可使用。
        /// </summary>
        public readonly List<int> TempRecipeIds = new();

        /// <summary>当前携带装备道具 ItemId 列表（武器/技能道具，死亡时 100% 进宝箱）。</summary>
        public readonly List<int> EquipmentItemIds = new();

        /// <summary>当前携带解药 ItemId 列表（死亡时 100% 进宝箱）。</summary>
        public readonly List<int> AntidoteItemIds = new();
    }

    /// <summary>
    /// 死亡宝箱内容快照（不可变）。
    /// 由 CalculateDeathChest 返回，不修改原 ActorInventory。
    /// </summary>
    public sealed class DeathChestSnapshot
    {
        /// <summary>颜料部分：各档各色 floor(N/2)。</summary>
        public readonly Dictionary<InkKey, int> InkBottles = new(21);

        /// <summary>配方拓本数量：floor(SessionEngravings/2)。</summary>
        public int RecipeCopyCount;

        /// <summary>金币部分：floor(Coins × 0.5)。</summary>
        public int Coins;

        /// <summary>武器道具 ItemId（100% 进宝箱）。</summary>
        public readonly List<int> EquipmentItemIds = new();

        /// <summary>解药 ItemId（100% 进宝箱）。</summary>
        public readonly List<int> AntidoteItemIds = new();
    }

    // ───────────────────────────────────────────────────────────────────────────
    // EconomyModule
    // ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// v2.1 局内唯一资源账本。
    ///
    /// 职责：
    ///   - 维护每个 Actor（玩家 + Bot）的 ActorInventory。
    ///   - 订阅 ChestOpenedEvent / ShopPurchaseEvent / TattooSessionEndEvent /
    ///     TattooInterruptedEvent / ActorDiedEvent / DeathChestLootedEvent，
    ///     原子地更新库存并发布 CoinChangedEvent / ItemPickedEvent / DeathChestSpawnedEvent。
    ///   - 提供 GetInventory(actor) 同步零开销接口。
    ///   - CalculateDeathChest(actor) 按半半规则计算死亡宝箱内容。
    ///
    /// 不做：战斗伤害、地图宝箱生成、NPC 对话、UI 飘字渲染。
    /// 零 Update：本模块无 ITickable，事件回调为唯一触发点，无逐帧 GC alloc。
    /// </summary>
    public sealed class EconomyModule : IGameModule
    {
        // ───── IGameModule ─────

        /// <summary>Category 3：项目扩展层，DataTableModule 就绪后初始化。</summary>
        public int ModuleCategory => 3;

        /// <summary>仅依赖 DataTableModule 读取 ItemConfig。其他模块通过事件单向通知本模块。</summary>
        public Type[] Dependencies => new[] { typeof(DataTableModule) };

        // ───── 外部依赖 ─────

        readonly ModuleRunner _runner;
        readonly EventBus     _bus;

        DataTableModule _dtModule;

        // ───── 运行时状态 ─────

        /// <summary>Actor 库存主存储（键 = Actor.InstanceId）。预分配 64 槽位（50 Actor + 余量）。</summary>
        readonly Dictionary<int, ActorInventory> _inventories = new(64);

        /// <summary>死亡宝箱快照缓存（键 = DeadActor.InstanceId）。供 DeathChestLootedEvent 转移物资。</summary>
        readonly Dictionary<int, DeathChestSnapshot> _pendingChests = new(8);

        /// <summary>已开宝箱实例 ID 集合（防同帧并发重复开箱）。</summary>
        readonly HashSet<int> _openedChestIds = new(16);

        // ───── 预分配零 GC 缓冲区（事件回调复用）─────

        /// <summary>掉落掷骰临时缓冲，每次 OnChestOpened 使用前 Clear()，避免 new List。</summary>
        static readonly List<(int ItemId, int Count)> _lootBuffer = new(8);

        // ───── 临时配方 ID 自增（9000+ 段）─────

        int _nextTempRecipeId = 9000;

        // ───── 中断惩罚常量 ─────

        const int TattooInterruptPenalty = 50;

        // ───────────────────────────────────────────────────────────────────────
        // 构造
        // ───────────────────────────────────────────────────────────────────────

        public EconomyModule(ModuleRunner runner, EventBus bus)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _bus    = bus    ?? throw new ArgumentNullException(nameof(bus));
        }

        // ───────────────────────────────────────────────────────────────────────
        // IGameModule
        // ───────────────────────────────────────────────────────────────────────

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            _dtModule = _runner.GetModule<DataTableModule>();

            // 验证 ItemConfig 已加载（DataTableModule 保证，此处做防御性断言）
            var itemCfg = _dtModule.GetTable<ItemConfig>();
            FrameworkLogger.Info("EconomyModule",
                $"Action=Initialized ItemConfigRows={itemCfg.All.Count}");

            return UniTask.CompletedTask;
        }

        public UniTask ShutdownAsync(CancellationToken ct = default)
        {
            _inventories.Clear();
            _pendingChests.Clear();
            _openedChestIds.Clear();
            _nextTempRecipeId = 9000;
            FrameworkLogger.Info("EconomyModule", "Action=Shutdown");
            return UniTask.CompletedTask;
        }

        // ───────────────────────────────────────────────────────────────────────
        // 公开查询接口
        // ───────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 同步零开销读取 Actor 库存引用。
        /// BotControllerModule / HUD 每次调用直接返回引用，不做拷贝，无 GC alloc。
        ///
        /// 调用者**不应修改**返回的 ActorInventory——修改请通过 Add*/Spend* 方法进行。
        /// </summary>
        public ActorInventory GetInventory(Actor actor)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (_inventories.TryGetValue(actor.InstanceId, out var inv))
                return inv;
            throw new KeyNotFoundException($"EconomyModule: Actor={actor.InstanceId} 未注册库存");
        }

        /// <summary>
        /// 注册 Actor（通常由 SpawnerModule 在实体生成后调用）。
        /// 已注册的 Actor 重复调用会被忽略（幂等）。
        /// </summary>
        public void RegisterActor(Actor actor)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (_inventories.ContainsKey(actor.InstanceId)) return;
            _inventories[actor.InstanceId] = new ActorInventory();
            FrameworkLogger.Info("EconomyModule", $"Action=ActorRegistered ActorId={actor.InstanceId} Name={actor.DisplayName}");
        }

        // ───────────────────────────────────────────────────────────────────────
        // 金币操作 API
        // ───────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 增减金币。delta 可正可负。不允许使金币低于 0（自动 clamp）。
        /// 发布 CoinChangedEvent。
        /// </summary>
        public void AddCoin(Actor actor, int delta, CoinChangeReason reason)
        {
            var inv = GetInventory(actor);
            int before = inv.Coins;
            inv.Coins = Mathf.Max(0, inv.Coins + delta);
            int actualDelta = inv.Coins - before;

            if (actualDelta == 0 && delta != 0)
            {
                FrameworkLogger.Warn("EconomyModule",
                    $"Actor={actor.InstanceId} AddCoin Delta={delta} 被 clamp 归零（余额不足）");
            }

            _bus.Publish(new CoinChangedEvent(actor, actualDelta, inv.Coins, reason));
            FrameworkLogger.Info("EconomyModule",
                $"Actor={actor.InstanceId} CoinDelta={actualDelta} NewTotal={inv.Coins} Reason={reason}");
        }

        /// <summary>
        /// 花费金币（SpendCoin = AddCoin 负值的语义别名）。
        /// 余额不足时扣至 0 并记录 Warn，不抛异常。
        /// </summary>
        public void SpendCoin(Actor actor, int amount, CoinChangeReason reason)
            => AddCoin(actor, -amount, reason);

        // ───────────────────────────────────────────────────────────────────────
        // 颜料操作 API
        // ───────────────────────────────────────────────────────────────────────

        /// <summary>添加颜料瓶。</summary>
        public void AddInk(Actor actor, int colorId, int tier, int count)
        {
            if (count <= 0) return;
            var inv = GetInventory(actor);
            var key = new InkKey(colorId, tier);
            inv.InkBottles.TryGetValue(key, out int cur);
            inv.InkBottles[key] = cur + count;

            // 发布 ItemPickedEvent（通过 ItemConfig 查找对应 ItemId）
            int itemId = ResolveInkItemId(colorId, tier);
            _bus.Publish(new ItemPickedEvent(actor, itemId, count));
            FrameworkLogger.Info("EconomyModule",
                $"Actor={actor.InstanceId} AddInk {key} Count={count} Total={inv.InkBottles[key]}");
        }

        /// <summary>
        /// 消耗颜料瓶。余量不足时消耗至 0，记录 Warn，不抛异常。
        /// 返回实际消耗量。
        /// </summary>
        public int SpendInk(Actor actor, int colorId, int tier, int count)
        {
            if (count <= 0) return 0;
            var inv = GetInventory(actor);
            var key = new InkKey(colorId, tier);
            inv.InkBottles.TryGetValue(key, out int cur);
            int actual = Mathf.Min(cur, count);
            if (actual < count)
            {
                FrameworkLogger.Warn("EconomyModule",
                    $"Actor={actor.InstanceId} InkBottle {key} Deficit={count - actual} 颜料不足警告");
            }
            inv.InkBottles[key] = cur - actual;
            return actual;
        }

        // ───────────────────────────────────────────────────────────────────────
        // 配方操作 API
        // ───────────────────────────────────────────────────────────────────────

        /// <summary>查询 Actor 是否已解锁指定配方（永久解锁）。</summary>
        public bool HasRecipe(Actor actor, int recipeId)
        {
            var inv = GetInventory(actor);
            return inv.RecipeIds.Contains(recipeId);
        }

        /// <summary>解锁永久配方（加入 RecipeIds）。</summary>
        public void UnlockRecipe(Actor actor, int recipeId)
        {
            var inv = GetInventory(actor);
            if (!inv.RecipeIds.Contains(recipeId))
                inv.RecipeIds.Add(recipeId);
        }

        /// <summary>给 Actor 添加临时配方拓本（本局可用，局结束销毁）。</summary>
        public void AddTempRecipe(Actor actor, int tempRecipeId)
        {
            var inv = GetInventory(actor);
            if (!inv.TempRecipeIds.Contains(tempRecipeId))
                inv.TempRecipeIds.Add(tempRecipeId);
        }

        // ───────────────────────────────────────────────────────────────────────
        // 死亡宝箱核心逻辑
        // ───────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 计算死亡宝箱内容（半半规则）。不修改 Actor 库存，纯计算并返回快照。
        ///
        /// 半半规则：
        ///   - 颜料：各档各色 floor(N/2)
        ///   - 配方拓本：floor(SessionEngravings/2) 份本局副本
        ///   - 金币：floor(Coins × 0.5)
        ///   - 武器/技能道具：100%
        ///   - 解药：100%
        /// </summary>
        public DeathChestSnapshot CalculateDeathChest(Actor actor)
        {
            var inv = GetInventory(actor);
            var snap = new DeathChestSnapshot();

            // 颜料：各档各色 floor(N/2)
            foreach (var kvp in inv.InkBottles)
            {
                int half = kvp.Value / 2; // floor（整除）
                if (half > 0)
                    snap.InkBottles[kvp.Key] = half;
            }

            // 配方拓本副本数量
            snap.RecipeCopyCount = inv.SessionEngravings / 2;

            // 金币 50%
            snap.Coins = inv.Coins / 2;

            // 武器/技能 100%
            snap.EquipmentItemIds.AddRange(inv.EquipmentItemIds);

            // 解药 100%
            snap.AntidoteItemIds.AddRange(inv.AntidoteItemIds);

            return snap;
        }

        // ───────────────────────────────────────────────────────────────────────
        // 事件处理
        // ───────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 普通/精品宝箱打开。
        /// 当前为简化实现（直接分配固定金币）——待 LootTableConfig 表就绪后替换为掷骰逻辑。
        /// 并发防护：同一个 ChestInstanceId 只处理一次。
        /// </summary>
        [EventHandler]
        void OnChestOpened(Economy.Events.ChestOpenedEvent e)
        {
            if (e.Opener == null) return;

            // 宝箱并发防护：同帧多个 Actor 开同一宝箱，第一个到达后标记，后续丢弃
            if (!_openedChestIds.Add(e.ChestInstanceId))
            {
                FrameworkLogger.Warn("EconomyModule",
                    $"Action=ChestAlreadyOpened ChestId={e.ChestInstanceId} IgnoredOpener={e.Opener.InstanceId}");
                return;
            }

            // TODO: 替换为真实 LootTableConfig 掷骰
            // 当前占位：按宝箱类型给固定金币
            int coinGain = e.Type == ChestType.Premium ? 120 : 40;
            AddCoin(e.Opener, coinGain, CoinChangeReason.ChestLoot);

            FrameworkLogger.Info("EconomyModule",
                $"Action=ChestOpened Opener={e.Opener.InstanceId} ChestId={e.ChestInstanceId} Type={e.Type} CoinGain={coinGain}");
        }

        /// <summary>
        /// 商人购买处理。
        /// 金币不足时拒绝购买，记录 Warn，不修改库存。
        /// </summary>
        [EventHandler]
        void OnShopPurchase(ShopPurchaseEvent e)
        {
            if (e.Buyer == null) return;
            var inv = GetInventory(e.Buyer);

            if (inv.Coins < e.CostCoin)
            {
                FrameworkLogger.Warn("EconomyModule",
                    $"Actor={e.Buyer.InstanceId} ShopPurchase 拒绝 ItemId={e.ItemId} 金币不足 Coins={inv.Coins} Cost={e.CostCoin}");
                return;
            }

            // 扣金币
            SpendCoin(e.Buyer, e.CostCoin, CoinChangeReason.ShopBuy);

            // 写入物品库存
            ApplyItemToInventory(e.Buyer, inv, e.ItemId, 1);

            FrameworkLogger.Info("EconomyModule",
                $"Actor={e.Buyer.InstanceId} ShopBuy ItemId={e.ItemId} CostCoin={e.CostCoin}");
        }

        /// <summary>
        /// 纹身师完成纹身，扣除颜料+金币，自增本局刻纹身计数。
        /// </summary>
        [EventHandler]
        void OnTattooSessionEnd(TattooSessionEndEvent e)
        {
            if (e.Customer == null) return;

            // 扣颜料
            SpendInk(e.Customer, e.ColorId, e.InkTier, e.InkCount);

            // 扣金币
            if (e.CostCoin > 0)
                SpendCoin(e.Customer, e.CostCoin, CoinChangeReason.Tattoo);

            // 自增本局刻纹身计数（用于死亡宝箱配方拓本计算）
            GetInventory(e.Customer).SessionEngravings++;

            FrameworkLogger.Info("EconomyModule",
                $"Actor={e.Customer.InstanceId} TattooSessionEnd ColorId={e.ColorId} Tier={e.InkTier} CostCoin={e.CostCoin} SessionEngravings={GetInventory(e.Customer).SessionEngravings}");
        }

        /// <summary>
        /// 自纹身读条中断，扣除 50 金币惩罚（最低归零，不为负）。
        /// </summary>
        [EventHandler]
        void OnTattooInterrupted(TattooInterruptedEvent e)
        {
            if (e.Actor == null) return;
            var inv = GetInventory(e.Actor);
            int deduct = Mathf.Min(inv.Coins, TattooInterruptPenalty);
            inv.Coins -= deduct;

            if (deduct > 0)
                _bus.Publish(new CoinChangedEvent(e.Actor, -deduct, inv.Coins, CoinChangeReason.TattooInterrupt));

            FrameworkLogger.Info("EconomyModule",
                $"Actor={e.Actor.InstanceId} TattooInterruptPenalty={deduct} NewCoins={inv.Coins} Reason={e.Reason}");
        }

        /// <summary>
        /// Actor 死亡处理：
        ///   1. CalculateDeathChest 快照
        ///   2. 按半半规则更新 Actor 库存（颜料减半、武器/解药清零、金币归零）
        ///   3. 生成临时配方拓本 ID（9000+ 段）
        ///   4. 发布 DeathChestSpawnedEvent（携带 ItemIds + TempRecipeIds）
        /// </summary>
        [EventHandler]
        void OnActorDied(ActorDiedEvent e)
        {
            if (e.DeadActor == null) return;
            if (!_inventories.ContainsKey(e.DeadActor.InstanceId))
            {
                FrameworkLogger.Warn("EconomyModule",
                    $"Action=ActorDied ActorId={e.DeadActor.InstanceId} 未注册库存，跳过宝箱生成");
                return;
            }

            var inv  = GetInventory(e.DeadActor);
            var snap = CalculateDeathChest(e.DeadActor);

            // ── 颜料：原库存减去宝箱半数（保留另一半在 Actor 身上，规则意义：死亡后失去一半）
            foreach (var kvp in snap.InkBottles)
            {
                var key = kvp.Key;
                inv.InkBottles.TryGetValue(key, out int cur);
                inv.InkBottles[key] = cur - kvp.Value; // 减去进宝箱的一半，另一半留存
            }

            // ── 金币：Actor 归零（50% 进宝箱，视为死亡折损）
            inv.Coins = 0;

            // ── 武器/解药：清零（100% 进宝箱）
            inv.EquipmentItemIds.Clear();
            inv.AntidoteItemIds.Clear();

            // ── 生成临时配方拓本 ID 列表（9000+ 段）
            var tempRecipeIds = new List<int>(snap.RecipeCopyCount);
            for (int i = 0; i < snap.RecipeCopyCount; i++)
                tempRecipeIds.Add(_nextTempRecipeId++);

            // ── 缓存快照供 DeathChestLootedEvent 使用
            _pendingChests[e.DeadActor.InstanceId] = snap;

            // ── 构建 ItemIds 列表（颜料 + 武器 + 解药）
            var itemIds = new List<int>(snap.InkBottles.Count + snap.EquipmentItemIds.Count + snap.AntidoteItemIds.Count);
            foreach (var kvp in snap.InkBottles)
            {
                int itemId = ResolveInkItemId(kvp.Key.ColorId, kvp.Key.Tier);
                // 每瓶单独一个 ItemId 条目（Count 由外部解析）；简化：重复 Count 次
                for (int i = 0; i < kvp.Value; i++)
                    itemIds.Add(itemId);
            }
            itemIds.AddRange(snap.EquipmentItemIds);
            itemIds.AddRange(snap.AntidoteItemIds);

            // ── 发布死亡宝箱事件
            _bus.Publish(new DeathChestSpawnedEvent(e.DeadActor, e.DeathPos, itemIds, tempRecipeIds));

            // ── 发布金币归零事件
            if (snap.Coins > 0)
                _bus.Publish(new CoinChangedEvent(e.DeadActor, -snap.Coins, 0, CoinChangeReason.DeathPenalty));

            FrameworkLogger.Info("EconomyModule",
                $"Action=DeathChestSpawned Actor={e.DeadActor.InstanceId} " +
                $"ChestCoins={snap.Coins} InkEntries={snap.InkBottles.Count} " +
                $"Equipment={snap.EquipmentItemIds.Count} Antidotes={snap.AntidoteItemIds.Count} " +
                $"TempRecipes={tempRecipeIds.Count}");
        }

        /// <summary>
        /// 死亡宝箱被拾取，将宝箱物资转移至 Looter 库存。
        /// </summary>
        [EventHandler]
        void OnDeathChestLooted(DeathChestLootedEvent e)
        {
            if (e.Looter == null || e.DeadActor == null) return;

            if (!_pendingChests.TryGetValue(e.DeadActor.InstanceId, out var snap))
            {
                FrameworkLogger.Warn("EconomyModule",
                    $"Action=DeathChestLooted DeadActor={e.DeadActor.InstanceId} 找不到对应快照，跳过");
                return;
            }

            _pendingChests.Remove(e.DeadActor.InstanceId);

            // 金币转移
            if (snap.Coins > 0)
                AddCoin(e.Looter, snap.Coins, CoinChangeReason.DeathChestLoot);

            // 颜料转移
            foreach (var kvp in snap.InkBottles)
                AddInk(e.Looter, kvp.Key.ColorId, kvp.Key.Tier, kvp.Value);

            // 武器/解药转移
            var looterInv = GetInventory(e.Looter);
            foreach (var id in snap.EquipmentItemIds)
            {
                looterInv.EquipmentItemIds.Add(id);
                _bus.Publish(new ItemPickedEvent(e.Looter, id, 1));
            }
            foreach (int id in snap.AntidoteItemIds)
            {
                looterInv.AntidoteItemIds.Add(id);
                _bus.Publish(new ItemPickedEvent(e.Looter, id, 1));
            }

            FrameworkLogger.Info("EconomyModule",
                $"Action=DeathChestLooted Looter={e.Looter.InstanceId} DeadActor={e.DeadActor.InstanceId} " +
                $"Coins={snap.Coins} InkEntries={snap.InkBottles.Count}");
        }

        // ───────────────────────────────────────────────────────────────────────
        // 内部工具
        // ───────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 将 ItemConfig 中的物品写入 Actor 库存（单件）。
        /// 颜料走 AddInk，装备/解药追加 ItemId，配方追加 RecipeIds，金币走 AddCoin。
        /// </summary>
        void ApplyItemToInventory(Actor actor, ActorInventory inv, int itemId, int count)
        {
            var cfg = _dtModule.GetTable<ItemConfig>();
            if (!cfg.TryGetById(itemId, out var row))
            {
                FrameworkLogger.Warn("EconomyModule",
                    $"Actor={actor.InstanceId} ApplyItem 未知 ItemId={itemId}");
                return;
            }

            switch (row.ItemType)
            {
                case "Coin":
                    AddCoin(actor, count, CoinChangeReason.ChestLoot);
                    break;

                case "InkBottle":
                    if (int.TryParse(row.SubType, out int colorId))
                        AddInk(actor, colorId, row.Tier, count);
                    break;

                case "RecipeShard":
                    inv.RecipeShards += count;
                    _bus.Publish(new ItemPickedEvent(actor, itemId, count));
                    break;

                case "RecipeFull":
                    // 简化：直接用 ItemId 作为配方 ID 索引
                    for (int i = 0; i < count; i++)
                        if (!inv.RecipeIds.Contains(itemId))
                            inv.RecipeIds.Add(itemId);
                    _bus.Publish(new ItemPickedEvent(actor, itemId, count));
                    break;

                case "Equipment":
                    for (int i = 0; i < count; i++)
                        inv.EquipmentItemIds.Add(itemId);
                    _bus.Publish(new ItemPickedEvent(actor, itemId, count));
                    break;

                case "Antidote":
                    for (int i = 0; i < count; i++)
                        inv.AntidoteItemIds.Add(itemId);
                    _bus.Publish(new ItemPickedEvent(actor, itemId, count));
                    break;

                default:
                    FrameworkLogger.Warn("EconomyModule",
                        $"Actor={actor.InstanceId} 未知 ItemType={row.ItemType} ItemId={itemId}");
                    break;
            }
        }

        /// <summary>
        /// 通过 ColorId + Tier 查找对应颜料的 ItemId。
        /// 命名规则：ItemId = 2X01/2X02/2X03（X=颜色 1–7，末位=Tier）。
        /// </summary>
        static int ResolveInkItemId(int colorId, int tier)
            => 2000 + colorId * 100 + tier;
    }
}
