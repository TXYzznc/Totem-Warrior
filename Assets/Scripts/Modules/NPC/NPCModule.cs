using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Economy;
using Economy.Events;
using Tattoo;
using Tattoo.Data;
using Tattoo.Events;
using UnityEngine;

/// <summary>
/// NPCModule v2.1 — 局内两类 NPC（纹身师 / 商人）的交互流程管理。
///
/// 职责：
///   - 从 NPCConfig / ShopStockConfig / TattooEnchantAffixConfig / TattooEnchantRecipeConfig 初始化 NPC 实例
///   - 检测 InteractPressedEvent / BotInteractRequestEvent，做距离检查 + 占用锁判断
///   - 纹身师：执行词缀附魔流程（前置检查 → 随机抽取词缀 → EnchantSlot → TattooEnchantedEvent）
///   - 商人：维护每个商人实例本局库存，处理购买
///   - 发布 NPCInteractStartEvent / ShopPurchaseEvent / ShopRefreshEvent / EnchantSessionCancelledEvent
///
/// 不做：玩家自刻纹身 / 伤害判定 / Build 最终状态写入 / UI 渲染 / NPC AI 巡逻
///
/// ModuleCategory = 3（依赖 DataTableModule/EconomyModule，都在 Category 1-2 完成）。
/// 零 Update：全事件驱动，无 ITickable，无逐帧 GC alloc。
/// </summary>
public sealed class NPCModule : IGameModule
{
    // ───── IGameModule ─────

    public int ModuleCategory => 3;

    /// <summary>
    /// 硬依赖只列 DataTableModule（Category 0/1）。
    /// EconomyModule / TattooModule 在运行时通过 GetModule<T>() 懒获取，避免循环依赖风险。
    /// </summary>
    public Type[] Dependencies => new[] { typeof(DataTableModule) };

    // ───── 外部依赖 ─────

    readonly ModuleRunner _runner;
    readonly EventBus     _bus;

    // ───── DataTable 缓存 ─────

    NPCConfig                  _npcConfig;
    ShopStockConfig            _shopStockConfig;
    TattooEnchantAffixConfig   _affixConfig;
    TattooEnchantRecipeConfig  _recipeConfig;

    // ───── 运行时 NPC 实例 ─────

    /// <summary>每局固定 5 个 NPC（3 纹身师 + 2 商人），由 InitializeAsync 构建。</summary>
    NPCInstance[] _instances = Array.Empty<NPCInstance>();

    // ───── 随机数生成器（词缀抽取用，避免 Update 每帧 new） ─────

    readonly System.Random _rng = new();

    // ───── 占位坐标（MVP 阶段，MapGenModule 就绪后替换）─────

    static readonly Vector3[] DefaultPositions =
    {
        new(-10f, 0f, -10f),  // tattooist_default
        new(-10f, 0f,  10f),  // tattooist_advanced
        new( 10f, 0f,  10f),  // tattooist_alien
        new(  0f, 0f, -15f),  // merchant_general
        new( 15f, 0f,   0f),  // merchant_alien
    };

    // ───────────────────────────────────────────────────────────────────────
    // 构造
    // ───────────────────────────────────────────────────────────────────────

    public NPCModule(ModuleRunner runner, EventBus bus)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _bus    = bus    ?? throw new ArgumentNullException(nameof(bus));
    }

    // ───────────────────────────────────────────────────────────────────────
    // IGameModule
    // ───────────────────────────────────────────────────────────────────────

    public UniTask InitializeAsync(CancellationToken ct = default)
    {
        var dt = _runner.GetModule<DataTableModule>();
        _npcConfig       = dt.GetTable<NPCConfig>();
        _shopStockConfig = dt.GetTable<ShopStockConfig>();
        _affixConfig     = dt.GetTable<TattooEnchantAffixConfig>();
        _recipeConfig    = dt.GetTable<TattooEnchantRecipeConfig>();

        // 构建 NPC 实例列表（NPCConfig 中有多少行就多少实例，上限 5）
        var rows = _npcConfig.All;
        _instances = new NPCInstance[rows.Count];
        int idx = 0;
        foreach (var kvp in rows)
        {
            var row = kvp.Value;
            var inst = new NPCInstance
            {
                ConfigId = row.Id,
                NPCId    = row.NPCId,
                Type     = Enum.TryParse<NPCType>(row.Type, out var t) ? t : NPCType.Tattooist,
                Position = idx < DefaultPositions.Length ? DefaultPositions[idx] : Vector3.zero,
                InteractRadiusSq = row.InteractRadius * row.InteractRadius,
                ThemePriceMul    = row.ThemePriceMul,
                ShopStockTableId = row.ShopStockTable,
                Stock            = new Dictionary<int, int>(),
            };

            // 商人：初始抽取库存
            if (inst.Type == NPCType.Merchant && !string.IsNullOrEmpty(inst.ShopStockTableId))
                RollInitialStock(inst);

            _instances[idx] = inst;
            idx++;
        }

        // InitializeAsync 中不发事件（框架约定）
        FrameworkLogger.Info("NPCModule",
            $"Action=Initialized NPCCount={_instances.Length}");

        return UniTask.CompletedTask;
    }

    public UniTask ShutdownAsync(CancellationToken ct = default)
    {
        // 若有悬空附魔会话（理论上不应有，ShutdownAsync 的 ct 已传播到 RequestAffixAsync）
        // 发布取消事件通知 UIModule
        for (int i = 0; i < _instances.Length; i++)
        {
            var inst = _instances[i];
            if (inst.IsBusy && inst.CurrentInteractor != null)
            {
                _bus.Publish(new EnchantSessionCancelledEvent(inst.CurrentInteractor, "ShutdownAsync"));
                inst.IsBusy = false;
                inst.CurrentInteractor = null;
            }
        }

        _instances = Array.Empty<NPCInstance>();
        FrameworkLogger.Info("NPCModule", "Action=Shutdown");
        return UniTask.CompletedTask;
    }

    // ───────────────────────────────────────────────────────────────────────
    // 事件订阅
    // ───────────────────────────────────────────────────────────────────────

    /// <summary>玩家按下交互键时，查找最近 NPC 并启动流程。</summary>
    [EventHandler]
    void OnInteractPressed(InteractPressedEvent e)
    {
        if (e?.Interactor == null) return;
        TryStartInteraction(e.Interactor);
    }

    /// <summary>Bot 主动触发交互请求。</summary>
    [EventHandler]
    void OnBotInteractRequest(BotInteractRequestEvent e)
    {
        if (e?.Bot == null) return;
        TryStartInteraction(e.Bot, e.TargetNpcId);
    }

    /// <summary>UI 确认附魔。</summary>
    [EventHandler]
    void OnUIEnchantConfirm(UIEnchantConfirmEvent e)
    {
        if (e?.Interactor == null) return;
        // 注意：RequestAffixAsync 是异步的，不能直接在 [EventHandler]（同步）中 await。
        // 使用 UniTask.Void 在主线程继续执行（EventBus 单线程分发保证无竞态）。
        RequestAffixAsync(e.Interactor, e.PartSlotIndex).Forget();
    }

    /// <summary>UI 确认购买。</summary>
    [EventHandler]
    void OnUIShopBuyConfirm(UIShopBuyConfirmEvent e)
    {
        if (e?.Buyer == null) return;
        ExecutePurchase(e.Buyer, e.NpcConfigId, e.ItemId);
    }

    // ───────────────────────────────────────────────────────────────────────
    // 公开 API
    // ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 纹身师附魔流程：
    ///   1. 前置校验（金币 / 稀有颜料 / 词缀槽是否满）
    ///   2. 消耗资源（EconomyModule.SpendCoin + SpendInk）
    ///   3. 加权随机抽取词缀（最多重试 3 次）
    ///   4. 调 TattooModule.EnchantSlot 写入词缀
    ///   5. 发布 TattooEnchantedEvent
    /// 返回 EnchantResult.None 表示成功。
    /// </summary>
    public async UniTask<EnchantResult> RequestAffixAsync(Actor actor, int partSlotIndex, CancellationToken ct = default)
    {
        if (actor == null) return EnchantResult.InvalidSlot;

        // ── 找到当前绑定该 actor 的纹身师实例
        int npcIdx = FindBusyNpcFor(actor);
        if (npcIdx < 0)
        {
            FrameworkLogger.Warn("NPCModule",
                $"Action=RequestAffix Actor={actor.InstanceId} 找不到已锁定的纹身师实例");
            return EnchantResult.NpcBusy;
        }

        var npc = _instances[npcIdx];

        // ── 获取依赖模块（运行时懒获取，不放入 Dependencies 避免循环依赖）
        var tattoo  = _runner.GetModule<TattooModule>();
        var economy = _runner.GetModule<EconomyModule>();
        var inv     = economy.GetInventory(actor);

        // ── 确认槽位有效
        var equipped = tattoo.Equipped;
        if (partSlotIndex < 0 || partSlotIndex >= equipped.Count)
        {
            ReleaseLock(npc, actor);
            return EnchantResult.InvalidSlot;
        }

        var slot = equipped[partSlotIndex];

        // ── 确认词缀槽未满
        if (slot.Affixes != null && slot.Affixes.Count >= TattooModule.MaxAffixesPerSlot)
        {
            ReleaseLock(npc, actor);
            return EnchantResult.AffixSlotFull;
        }

        // ── 确定颜料档（由 slot.ColorId 对应颜色的 Element 推导 ColorTier）
        string colorTier = ResolveColorTier(slot.ColorId);
        if (!_recipeConfig.TryGetByColorTier(colorTier, out var recipe))
        {
            FrameworkLogger.Error("NPCModule",
                $"Action=RequestAffix ColorTier={colorTier} 找不到配方行");
            ReleaseLock(npc, actor);
            return EnchantResult.InvalidSlot;
        }

        // ── 校验金币
        if (inv.Coins < recipe.CoinCost)
        {
            ReleaseLock(npc, actor);
            return EnchantResult.InsufficientCoin;
        }

        // ── 校验稀有颜料（紫/金/白对应 tier 2/3，此处以 tier >= 2 视为稀有）
        int rareInkColorId = slot.ColorId;
        int rareInkTier    = ResolveInkTier(slot.ColorId);
        inv.InkBottles.TryGetValue(new InkKey(rareInkColorId, rareInkTier), out int ownedInk);
        if (ownedInk < recipe.RarePigmentCost)
        {
            ReleaseLock(npc, actor);
            return EnchantResult.InsufficientRarePigment;
        }

        // ── 加权随机抽取词缀（最多重试 3 次去重）
        var existingAffixIds = slot.Affixes != null
            ? new HashSet<int>(slot.Affixes.ConvertAll(a => a.AffixId))
            : new HashSet<int>();

        TattooAffix? picked = null;
        const int MaxRetry = 3;
        for (int attempt = 0; attempt < MaxRetry; attempt++)
        {
            var candidate = RollAffix(partSlotIndex, colorTier);
            if (candidate.HasValue && !existingAffixIds.Contains(candidate.Value.AffixId))
            {
                picked = candidate;
                break;
            }
        }

        if (!picked.HasValue)
        {
            ReleaseLock(npc, actor);
            return EnchantResult.AffixDuplicate;
        }

        // ── 消耗资源
        ct.ThrowIfCancellationRequested();
        economy.SpendCoin(actor, recipe.CoinCost, CoinChangeReason.Enchant);
        economy.SpendInk(actor, rareInkColorId, rareInkTier, recipe.RarePigmentCost);

        // ── 写入 TattooModule
        var affixList = new System.Collections.Generic.List<TattooAffix> { picked.Value };
        bool ok = tattoo.EnchantSlot(actor.Target, partSlotIndex, affixList, recipe.CoinCost, recipe.RarePigmentCost);
        if (!ok)
        {
            // EnchantSlot 失败（理论上前置检查已覆盖，记录并恢复资源）
            FrameworkLogger.Error("NPCModule",
                $"Action=RequestAffix EnchantSlot 失败 Actor={actor.InstanceId} SlotIndex={partSlotIndex}");
            economy.AddCoin(actor, recipe.CoinCost, CoinChangeReason.ChestLoot);  // 退款
            ReleaseLock(npc, actor);
            return EnchantResult.AffixSlotFull;
        }

        // ── TattooModule.EnchantSlot 内部已发布 TattooEnchantedEvent，此处不重复发布

        ReleaseLock(npc, actor);

        FrameworkLogger.Info("NPCModule",
            $"Action=RequestAffixDone Actor={actor.InstanceId} SlotIndex={partSlotIndex} AffixId={picked.Value.AffixId}");

        return EnchantResult.None;
    }

    // ───────────────────────────────────────────────────────────────────────
    // 内部：交互启动
    // ───────────────────────────────────────────────────────────────────────

    void TryStartInteraction(Actor actor, int preferredNpcId = -1)
    {
        if (actor?.GameObject == null) return;

        // 找到距离最近（且在交互半径内）的 NPC
        int bestIdx = FindNearestNpc(actor.GameObject.transform.position, preferredNpcId);
        if (bestIdx < 0)
        {
            FrameworkLogger.Info("NPCModule",
                $"Action=NoNearbyNPC Actor={actor.InstanceId}");
            return;
        }

        var npc = _instances[bestIdx];

        // 占用锁检查
        if (npc.IsBusy)
        {
            FrameworkLogger.Info("NPCModule",
                $"Action=NPCBusy NPCId={npc.NPCId} Actor={actor.InstanceId}");
            // TODO: 发布"工作室繁忙"提示事件（UIModule 订阅后弹提示）
            return;
        }

        // 服务冷却检查（被攻击后暂时关闭服务）
        if (Time.time < npc.ServiceCooldownUntil)
        {
            FrameworkLogger.Info("NPCModule",
                $"Action=NPCInCooldown NPCId={npc.NPCId} Actor={actor.InstanceId}");
            return;
        }

        // 锁定占用
        npc.IsBusy = true;
        npc.CurrentInteractor = actor;

        var npcRef = MakeRef(npc);

        // 发布 NPCInteractStartEvent（弹窗打开前）
        _bus.Publish(new NPCInteractStartEvent(actor, npcRef));

        FrameworkLogger.Info("NPCModule",
            $"Action=InteractStart NPCId={npc.NPCId} Type={npc.Type} Actor={actor.InstanceId}");

        // 根据 NPC 类型发布 UI 打开指令
        if (npc.Type == NPCType.Merchant)
        {
            // ShopRefreshEvent 供 UIModule 构建商品列表
            _bus.Publish(new ShopRefreshEvent(npcRef, BuildStockList(npc)));
        }
        // 纹身师：UIModule 订阅 NPCInteractStartEvent 后自行弹出附魔界面，
        // 玩家确认后发出 UIEnchantConfirmEvent，本模块再响应
    }

    // ───────────────────────────────────────────────────────────────────────
    // 内部：商人购买
    // ───────────────────────────────────────────────────────────────────────

    void ExecutePurchase(Actor buyer, int npcConfigId, int itemId)
    {
        int npcIdx = FindInstanceByConfigId(npcConfigId);
        if (npcIdx < 0)
        {
            FrameworkLogger.Warn("NPCModule",
                $"Action=ExecutePurchase 找不到 NPC ConfigId={npcConfigId}");
            return;
        }

        var npc = _instances[npcIdx];

        // 校验库存
        if (!npc.Stock.TryGetValue(itemId, out int count) || count <= 0)
        {
            FrameworkLogger.Warn("NPCModule",
                $"Action=ExecutePurchase ItemId={itemId} 库存为 0 或不存在");
            ReleaseLock(npc, buyer);
            return;
        }

        // 查价格（基础价 × ThemePriceMul，取整）
        int basePrice = GetBasePrice(itemId, npc.ShopStockTableId);
        int actualPrice = Mathf.RoundToInt(basePrice * npc.ThemePriceMul);

        // 校验金币
        var economy = _runner.GetModule<EconomyModule>();
        var inv = economy.GetInventory(buyer);
        if (inv.Coins < actualPrice)
        {
            FrameworkLogger.Warn("NPCModule",
                $"Action=ExecutePurchase Actor={buyer.InstanceId} 金币不足 Coins={inv.Coins} Cost={actualPrice}");
            ReleaseLock(npc, buyer);
            return;
        }

        // 扣款 + 扣库存
        economy.SpendCoin(buyer, actualPrice, CoinChangeReason.ShopBuy);
        npc.Stock[itemId] = count - 1;

        // 发布 ShopPurchaseEvent（EconomyModule 订阅此事件后自行将物品加入库存）
        _bus.Publish(new ShopPurchaseEvent(buyer, itemId, actualPrice));

        ReleaseLock(npc, buyer);

        FrameworkLogger.Info("NPCModule",
            $"Action=PurchaseDone Actor={buyer.InstanceId} ItemId={itemId} Cost={actualPrice} StockLeft={npc.Stock[itemId]}");
    }

    // ───────────────────────────────────────────────────────────────────────
    // 内部：初始库存抽取
    // ───────────────────────────────────────────────────────────────────────

    void RollInitialStock(NPCInstance inst)
    {
        // 筛选同 TableId 的所有候选行
        var candidates = new List<ShopStockConfigRow>();
        float totalWeight = 0f;
        foreach (var kvp in _shopStockConfig.All)
        {
            if (kvp.Value.TableId == inst.ShopStockTableId)
            {
                candidates.Add(kvp.Value);
                totalWeight += kvp.Value.Weight;
            }
        }

        if (candidates.Count == 0 || totalWeight <= 0f)
        {
            FrameworkLogger.Warn("NPCModule",
                $"Action=RollInitialStock TableId={inst.ShopStockTableId} 无候选行");
            return;
        }

        // 加权随机抽取：所有候选行各自决定本局库存数量
        foreach (var row in candidates)
        {
            // 按 Weight / totalWeight 概率决定是否上架（简化：Weight >= 1.0 必上架）
            // MinCount 到 MaxCount 之间随机
            int cnt = _rng.Next(row.MinCount, row.MaxCount + 1);
            if (cnt > 0)
                inst.Stock[row.ItemId] = cnt;
        }

        FrameworkLogger.Info("NPCModule",
            $"Action=StockRolled TableId={inst.ShopStockTableId} Items={inst.Stock.Count}");
    }

    // ───────────────────────────────────────────────────────────────────────
    // 内部：词缀抽取
    // ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 从 TattooEnchantAffixConfig 中按 PartId + ColorTier 筛选词缀池，加权随机抽 1 条。
    /// partSlotIndex → PartId：Index 0=脑袋(1), 1=躯干(2), 2=左臂(3), 3=右臂(4), 4=左腿(5), 5=右腿(6)
    /// 词缀池包含 PartId==0（全部位）和 PartId==具体部位的行。
    /// </summary>
    TattooAffix? RollAffix(int partSlotIndex, string colorTier)
    {
        // partSlotIndex (0-based) → PartId (1-based)，0 = 全部位
        int partId = partSlotIndex + 1;

        var pool = new List<(TattooEnchantAffixConfigRow row, float weight)>();
        float totalWeight = 0f;

        foreach (var kvp in _affixConfig.All)
        {
            var row = kvp.Value;
            bool partMatch  = row.PartId == 0 || row.PartId == partId;
            bool tierMatch  = row.ColorTier == colorTier || row.ColorTier == "Any";
            if (partMatch && tierMatch)
            {
                pool.Add((row, row.Weight));
                totalWeight += row.Weight;
            }
        }

        if (pool.Count == 0 || totalWeight <= 0f)
        {
            FrameworkLogger.Warn("NPCModule",
                $"Action=RollAffix 词缀池为空 PartId={partId} ColorTier={colorTier}");
            return null;
        }

        // 加权随机
        float r = (float)(_rng.NextDouble() * totalWeight);
        float acc = 0f;
        foreach (var (row, weight) in pool)
        {
            acc += weight;
            if (r <= acc)
            {
                // 将配置行转为运行时词缀 struct
                Enum.TryParse<AffixType>(row.AffixType, out var affixType);
                return new TattooAffix
                {
                    AffixId     = row.Id,
                    DisplayName = row.DisplayText,
                    Type        = affixType,
                    Value       = row.Value,
                };
            }
        }

        // 浮点精度兜底：返回最后一个
        var last = pool[pool.Count - 1].row;
        Enum.TryParse<AffixType>(last.AffixType, out var lastType);
        return new TattooAffix
        {
            AffixId     = last.Id,
            DisplayName = last.DisplayText,
            Type        = lastType,
            Value       = last.Value,
        };
    }

    // ───────────────────────────────────────────────────────────────────────
    // 内部：工具方法
    // ───────────────────────────────────────────────────────────────────────

    int FindNearestNpc(Vector3 pos, int preferredId)
    {
        int best = -1;
        float bestDist = float.MaxValue;
        for (int i = 0; i < _instances.Length; i++)
        {
            var inst = _instances[i];
            if (preferredId >= 0 && inst.ConfigId != preferredId) continue;
            float dist = (inst.Position - pos).sqrMagnitude;
            if (dist <= inst.InteractRadiusSq && dist < bestDist)
            {
                bestDist = dist;
                best = i;
            }
        }
        return best;
    }

    int FindBusyNpcFor(Actor actor)
    {
        for (int i = 0; i < _instances.Length; i++)
        {
            if (_instances[i].IsBusy && _instances[i].CurrentInteractor == actor)
                return i;
        }
        return -1;
    }

    int FindInstanceByConfigId(int configId)
    {
        for (int i = 0; i < _instances.Length; i++)
        {
            if (_instances[i].ConfigId == configId)
                return i;
        }
        return -1;
    }

    void ReleaseLock(NPCInstance npc, Actor actor)
    {
        npc.IsBusy = false;
        npc.CurrentInteractor = null;
        FrameworkLogger.Info("NPCModule",
            $"Action=ReleaseLock NPCId={npc.NPCId} Actor={actor?.InstanceId}");
    }

    NPCRef MakeRef(NPCInstance inst) => new NPCRef
    {
        ConfigId = inst.ConfigId,
        NPCId    = inst.NPCId,
        Type     = inst.Type,
        Position = inst.Position,
    };

    List<int> BuildStockList(NPCInstance inst)
    {
        var list = new List<int>(inst.Stock.Count);
        foreach (var kvp in inst.Stock)
            if (kvp.Value > 0)
                list.Add(kvp.Key);
        return list;
    }

    int GetBasePrice(int itemId, string tableId)
    {
        foreach (var kvp in _shopStockConfig.All)
        {
            var row = kvp.Value;
            if (row.TableId == tableId && row.ItemId == itemId)
                return row.BasePrice;
        }
        return 0;
    }

    /// <summary>
    /// 根据 ColorId 推导颜料档（ColorTier）。
    /// 颜色 1-4（红/黄/绿/蓝）= Common；5-6（紫/金）= Rare；7（白）= Legendary。
    /// 对应 TattooColorConfig 主键定义。
    /// </summary>
    static string ResolveColorTier(int colorId)
    {
        if (colorId <= 4) return "Common";
        if (colorId <= 6) return "Rare";
        return "Legendary";
    }

    /// <summary>
    /// 根据 ColorId 推导颜料 InkKey.Tier（EconomyModule 颜料分档：1=Basic/2=Standard/3=Premium）。
    /// Common → Tier 1；Rare → Tier 2；Legendary → Tier 3。
    /// </summary>
    static int ResolveInkTier(int colorId)
    {
        if (colorId <= 4) return 1;
        if (colorId <= 6) return 2;
        return 3;
    }
}

// ───────────────────────────────────────────────────────────────────────
// 运行时 NPC 实例（struct，固定数组，零 GC）
// ───────────────────────────────────────────────────────────────────────

/// <summary>
/// NPCInstance：轻量运行时对象（非 MonoBehaviour，避免 GetComponent 开销）。
/// 全量 5 个实例存于 _instances 数组。改用 class 以兼容 async 方法的非 ref 拷贝。
/// </summary>
public sealed class NPCInstance
{
    public int      ConfigId;
    public string   NPCId;
    public NPCType  Type;
    public Vector3  Position;
    public float    InteractRadiusSq;       // InteractRadius * InteractRadius（缓存，避免开方）
    public float    ThemePriceMul;
    public string   ShopStockTableId;
    public bool     IsBusy;
    public float    ServiceCooldownUntil;   // Time.time 时间戳（被攻击后 ServiceCooldown 秒内锁定）
    public int      ManualRefreshUsed;      // 0 | 1，每局上限 1 次（仅 Merchant）
    public Actor    CurrentInteractor;      // 当前正在交互的 Actor（IsBusy=true 时有效）
    public Dictionary<int, int> Stock;      // ItemId → 当前库存数量（仅 Merchant 有效）
}

// ───────────────────────────────────────────────────────────────────────
// 附魔结果枚举
// ───────────────────────────────────────────────────────────────────────

/// <summary>RequestAffixAsync 返回值。None = 成功。</summary>
public enum EnchantResult
{
    None,                   // 成功
    InsufficientCoin,       // 金币不足
    InsufficientRarePigment,// 稀有颜料不足
    AffixSlotFull,          // 词缀槽已满 2 个
    AffixDuplicate,         // 重试 3 次仍抽到重复词缀
    NpcBusy,                // 工作室繁忙
    InvalidSlot,            // 槽位索引越界
}
