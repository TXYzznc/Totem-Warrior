using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using AttackSystem.Events;
using Cysharp.Threading.Tasks;
using Economy;
using Economy.Events;
using MapGen;
using Tattoo;
using Tattoo.Events;
using UnityEngine;

/// <summary>
/// 场上武器实体（GameObject）的统一出口。
/// 与 WeaponModule（逻辑装备）互补，不重叠。
///
/// 职责（CONTRACT §C 表 + spec.md §2）：
/// 1. 精英死亡 → WeaponDropConfig 权重 → SpawnDroppedWeapon
/// 2. 宝箱开启 → ChestConfig 概率 → spawn 武器 / 加金币
/// 3. 商人槽位刷新 + 购买扣金 → 触发 WeaponPickedUpEvent
/// 4. WeaponPickedUpEvent 后销毁场上 pickup GO
///
/// 不做：
/// - 不计算伤害（→ CombatModule）
/// - 不处理拾取判定（→ WeaponPickupTrigger MonoBehaviour）
/// - 不管理角色状态（→ WeaponUpgradeModule）
/// </summary>
public sealed class WeaponSpawnerModule : IGameModule
{
    // ─── 生命周期声明 ────────────────────────────────────────────────
    public int    ModuleCategory => 3;
    public Type[] Dependencies   => new[] { typeof(SpawnerModule), typeof(DataTableModule), typeof(InputModule) };

    readonly ModuleRunner _runner;
    readonly EventBus     _bus;

    // ─── 状态 ────────────────────────────────────────────────────────
    /// <summary>场上活跃的武器拾取 GO 列表（用于 Shutdown 清理 + OnWeaponPickedUp 销毁）。</summary>
    readonly List<GameObject> _activePickups = new();

    /// <summary>场上宝箱 GO 列表（Shutdown 清理用）。</summary>
    readonly List<GameObject> _activeChests = new();

    /// <summary>场上商人 GO 列表（用于定位 merchantWorldPos）。</summary>
    readonly List<GameObject> _activeMerchants = new();

    /// <summary>商人当前槽位武器 ID 列表（每局刷新，0~2 个 WeaponId，供 GetMerchantSlots 返回）。</summary>
    readonly List<string> _merchantSlots = new();

    /// <summary>商人当前槽位完整配置行（用于 MerchantTrigger.Slots 注入，包含 GoldCost 等字段）。</summary>
    readonly List<MerchantConfigRow> _merchantSlotRows = new();

    // ─── DataTable 缓存（InitializeAsync 后有效） ────────────────────
    WeaponDropConfig _dropConfig;
    ChestConfig      _chestConfig;
    MerchantConfig   _merchantConfig;
    InputModule      _input;
    SpawnerModule    _spawner;

    // ─── 构造 ────────────────────────────────────────────────────────
    public WeaponSpawnerModule(ModuleRunner runner, EventBus bus)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _bus    = bus    ?? throw new ArgumentNullException(nameof(bus));
    }

    // ─── IGameModule ─────────────────────────────────────────────────
    public UniTask InitializeAsync(CancellationToken ct = default)
    {
        var dtModule = _runner.GetModule<DataTableModule>();
        _input          = _runner.GetModule<InputModule>();
        _spawner        = _runner.GetModule<SpawnerModule>();
        _dropConfig     = dtModule.GetTable<WeaponDropConfig>();
        _chestConfig    = dtModule.GetTable<ChestConfig>();
        _merchantConfig = dtModule.GetTable<MerchantConfig>();

        RefreshMerchantSlots();

        FrameworkLogger.Info("WeaponSpawnerModule",
            $"Action=Initialized DropRows={_dropConfig.Rows.Count} " +
            $"ChestRows={_chestConfig.Rows.Count} MerchantRows={_merchantConfig.Rows.Count} " +
            $"MerchantSlots={_merchantSlots.Count}");

        return UniTask.CompletedTask;
    }

    public UniTask ShutdownAsync(CancellationToken ct = default)
    {
        foreach (var go in _activePickups)
        {
            if (go != null)
                UnityEngine.Object.Destroy(go);
        }
        _activePickups.Clear();

        foreach (var go in _activeChests)
        {
            if (go != null)
                UnityEngine.Object.Destroy(go);
        }
        _activeChests.Clear();

        foreach (var go in _activeMerchants)
        {
            if (go != null)
                UnityEngine.Object.Destroy(go);
        }
        _activeMerchants.Clear();
        _merchantSlots.Clear();
        _merchantSlotRows.Clear();

        FrameworkLogger.Info("WeaponSpawnerModule", "Action=Shutdown");
        return UniTask.CompletedTask;
    }

    // ─── 对外 API（签名冻结于 CONTRACT §C / spec.md §2） ────────────

    /// <summary>
    /// 在指定世界坐标 Spawn 武器拾取 GO。
    /// 由 [EventHandler] OnEnemyDied / OnChestOpened 调用。
    /// </summary>
    public void SpawnDroppedWeapon(string weaponId, Vector3 position)
    {
        if (string.IsNullOrEmpty(weaponId))
        {
            FrameworkLogger.Warn("WeaponSpawnerModule", "Action=SpawnDroppedWeapon WeaponId=null/empty 跳过");
            return;
        }

        GameObject go;
        var prefab = Resources.Load<GameObject>($"Prefab/Weapon/Pickup/{weaponId}_pickup");
        if (prefab != null)
        {
            go = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            FrameworkLogger.Info("WeaponSpawnerModule",
                $"Action=SpawnDroppedWeapon WeaponId={weaponId} Source=Prefab Position={position}");
        }
        else
        {
            // fallback：Cube + SphereCollider trigger，颜色 cyan 区分
            go = BuildFallbackPickup(weaponId);
            go.name = "pickup_" + weaponId;
            go.transform.position = position;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = Color.cyan;
            }

            // 移除默认 BoxCollider，加 SphereCollider trigger
            var boxCol = go.GetComponent<BoxCollider>();
            if (boxCol != null)
                UnityEngine.Object.Destroy(boxCol);
            var sphere = go.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 1.2f;

            FrameworkLogger.Warn("WeaponSpawnerModule",
                $"Action=SpawnDroppedWeapon WeaponId={weaponId} Source=Fallback Position={position}");
        }

        // 添加 WeaponPickupTrigger（18-C 实装该 MB）
        var trigger = go.AddComponent<WeaponPickupTrigger>();
        trigger.WeaponId = weaponId;
        trigger.Bus = _bus;
        trigger.Input = _input;
        trigger.PlayerTarget = _spawner?.PlayerTarget;
        trigger.PlayerTransform = _spawner?.Player != null ? _spawner.Player.transform : null;

        _activePickups.Add(go);

        // TC-15：武器生成后 Publish WeaponSpawnedEvent，QA 通过 filter=WeaponSpawned 验证
        _bus.Publish(new WeaponSpawnedEvent(weaponId, position, go));
        FrameworkLogger.Info("WeaponSpawnerModule",
            $"WeaponSpawned Id={weaponId} Pos={position}");
    }

    /// <summary>
    /// 在指定坐标 Spawn 宝箱 GO。
    /// </summary>
    public void SpawnChest(Vector3 position, string chestId = "chest_common")
    {
        GameObject go;
        var prefab = Resources.Load<GameObject>($"Prefab/Chest/{chestId}");
        if (prefab != null)
        {
            go = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            FrameworkLogger.Info("WeaponSpawnerModule",
                $"Action=SpawnChest ChestId={chestId} Source=Prefab Position={position}");
        }
        else
        {
            go = BuildFallbackChest(chestId);
            go.name = "chest_" + chestId;
            go.transform.position = position;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = Color.yellow;
            }

            var boxCol = go.GetComponent<BoxCollider>();
            if (boxCol == null) boxCol = go.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector3(1.4f, 1.1f, 1.4f);

            FrameworkLogger.Warn("WeaponSpawnerModule",
                $"Action=SpawnChest ChestId={chestId} Source=Fallback Position={position}");
        }

        var trigger = go.AddComponent<ChestInteractTrigger>();
        trigger.ChestId = chestId;
        trigger.Bus = _bus;
        trigger.Cfg = _chestConfig;
        trigger.Input = _input;
        trigger.PlayerTarget = _spawner?.PlayerTarget;
        trigger.PlayerTransform = _spawner?.Player != null ? _spawner.Player.transform : null;

        _activeChests.Add(go);
    }

    /// <summary>
    /// 在指定坐标 Spawn 商人 GO。
    /// </summary>
    public void SpawnMerchant(Vector3 position)
    {
        GameObject go;
        var prefab = Resources.Load<GameObject>("Prefab/NPC/merchant");
        if (prefab != null)
        {
            go = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            FrameworkLogger.Info("WeaponSpawnerModule",
                $"Action=SpawnMerchant Source=Prefab Position={position}");
        }
        else
        {
            go = BuildFallbackMerchant();
            go.name = "merchant";
            go.transform.position = position;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = Color.magenta;
            }

            var boxCol = go.GetComponent<BoxCollider>();
            if (boxCol == null) boxCol = go.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector3(1.2f, 2.2f, 1.2f);

            FrameworkLogger.Warn("WeaponSpawnerModule",
                "Action=SpawnMerchant Source=Fallback");
        }

        var trigger = go.AddComponent<MerchantTrigger>();
        // 注入槽位配置行列表（MerchantTrigger.Slots 类型为 IReadOnlyList<MerchantConfigRow>）
        trigger.Slots = BuildMerchantSlotRows();
        trigger.Bus = _bus;
        trigger.Input = _input;
        trigger.PlayerTarget = _spawner?.PlayerTarget;
        trigger.PlayerActor = _spawner?.PlayerActor;
        trigger.Economy = TryGetEconomyModule();
        trigger.PlayerTransform = _spawner?.Player != null ? _spawner.Player.transform : null;

        _activeMerchants.Add(go);
    }

    /// <summary>
    /// 返回当前商人槽位的 WeaponId 列表（只读副本，0~3 个）。
    /// </summary>
    public IReadOnlyList<string> GetMerchantSlots() => _merchantSlots;

    // ─── [EventHandler]（签名冻结） ─────────────────────────────────

    /// <summary>
    /// 精英死亡掉落判定。复用现有 Tattoo.Events.EnemyDiedEvent（CONTRACT §A.3）。
    /// 仅在 e.DeadActor.Tier == EnemyTier.Elite 时执行掉落逻辑。
    /// </summary>
    [EventHandler]
    void OnEnemyDied(EnemyDiedEvent e)
    {
        if (e?.DeadActor?.Tier != EnemyTier.Elite) return;

        // 取当前房间号（MapGenModule 可选；不存在时 fallback 1）
        int roomIndex = TryGetCurrentRoomIndex();

        var candidates = new List<WeaponDropConfigRow>();
        foreach (var row in _dropConfig.Rows)
        {
            if (row.DropSource == "Elite"
                && roomIndex >= row.MinRoomIndex
                && roomIndex <= row.MaxRoomIndex)
            {
                candidates.Add(row);
            }
        }

        if (candidates.Count == 0)
        {
            FrameworkLogger.Info("WeaponSpawnerModule",
                $"Action=OnEnemyDied RoomIndex={roomIndex} 精英掉落无候选行，跳过");
            return;
        }

        string weaponId = WeightedRandom(candidates, r => r.Weight);
        if (string.IsNullOrEmpty(weaponId))
        {
            FrameworkLogger.Warn("WeaponSpawnerModule",
                "Action=OnEnemyDied WeightedRandom 未选中任何武器，跳过");
            return;
        }

        SpawnDroppedWeapon(weaponId, e.DeathPos);
        FrameworkLogger.Info("WeaponSpawnerModule",
            $"Action=EliteDrop WeaponId={weaponId} RoomIndex={roomIndex} DeathPos={e.DeathPos}");
    }

    /// <summary>
    /// 玩家拾取后 Destroy 对应场上 GO（按 Position 匹配 _activePickups）。
    /// </summary>
    [EventHandler]
    void OnWeaponPickedUp(WeaponPickedUpEvent e)
    {
        if (e.Source != "Pickup")
            return;

        const float kMatchSqrDist = 0.5f * 0.5f;
        GameObject closest = null;
        float closestSqr = float.MaxValue;

        foreach (var go in _activePickups)
        {
            if (go == null) continue;
            float sqr = (go.transform.position - e.PickupPosition).sqrMagnitude;
            if (sqr < kMatchSqrDist && sqr < closestSqr)
            {
                closestSqr = sqr;
                closest = go;
            }
        }

        if (closest != null)
        {
            _activePickups.Remove(closest);
            UnityEngine.Object.Destroy(closest);
            FrameworkLogger.Info("WeaponSpawnerModule",
                $"Action=PickupDestroyed WeaponId={e.WeaponId} PickupPos={e.PickupPosition}");
        }
        else
        {
            FrameworkLogger.Warn("WeaponSpawnerModule",
                $"Action=OnWeaponPickedUp WeaponId={e.WeaponId} PickupPos={e.PickupPosition} 未找到匹配 GO（距离>0.5m）");
        }
    }

    /// <summary>
    /// 宝箱开启结算（reward=Weapon → SpawnDroppedWeapon；reward=Gold → EconomyAddGoldEvent）。
    /// </summary>
    [EventHandler]
    void OnChestOpened(ChestOpenedEvent e)
    {
        if (string.IsNullOrEmpty(e.ChestId)) return;

        switch (e.RewardType)
        {
            case "Weapon":
            {
                string weaponId = e.RewardId;
                if (string.IsNullOrEmpty(weaponId))
                {
                    // 从 WeaponDropConfig 过滤 DropSource=="Chest" 加权随机选
                    var candidates = new List<WeaponDropConfigRow>();
                    foreach (var row in _dropConfig.Rows)
                    {
                        if (row.DropSource == "Chest")
                            candidates.Add(row);
                    }
                    weaponId = WeightedRandom(candidates, r => r.Weight);
                }

                if (!string.IsNullOrEmpty(weaponId))
                {
                    SpawnDroppedWeapon(weaponId, e.ChestPosition + Vector3.up * 0.5f);
                    FrameworkLogger.Info("WeaponSpawnerModule",
                        $"Action=ChestWeaponSpawn ChestId={e.ChestId} WeaponId={weaponId}");
                }
                else
                {
                    FrameworkLogger.Warn("WeaponSpawnerModule",
                        $"Action=OnChestOpened ChestId={e.ChestId} Weapon 候选为空，跳过 spawn");
                }
                break;
            }

            case "Gold":
            {
                if (TryGetEconomyActor(_spawner?.PlayerTarget, out var actor)
                    && TryGetEconomyModule() is { } economy)
                {
                    economy.AddCoin(actor, e.RewardAmount, CoinChangeReason.ChestLoot);
                }
                else
                {
                    FrameworkLogger.Warn("WeaponSpawnerModule",
                        $"Action=ChestGoldFailed ChestId={e.ChestId} Amount={e.RewardAmount} Reason=EconomyActorMissing");
                }

                _bus.Publish(new EconomyAddGoldEvent
                {
                    Amount         = e.RewardAmount,
                    SourcePosition = e.ChestPosition
                });
                FrameworkLogger.Info("WeaponSpawnerModule",
                    $"Action=ChestGoldPublish ChestId={e.ChestId} Amount={e.RewardAmount}");
                break;
            }

            case "Potion":
            {
                var player = _spawner?.PlayerTarget;
                if (player == null)
                {
                    FrameworkLogger.Warn("WeaponSpawnerModule",
                        $"Action=ChestPotionFailed ChestId={e.ChestId} Amount={e.RewardAmount} Reason=PlayerTargetMissing");
                    break;
                }

                float maxHp = _spawner.PlayerMaxHp > 0f ? _spawner.PlayerMaxHp : 100f;
                float heal = Mathf.Max(1, e.RewardAmount) * 25f;
                float oldHp = player.Health;
                player.Health = Mathf.Min(maxHp, player.Health + heal);
                float delta = player.Health - oldHp;

                _bus.Publish(new PlayerHealthChangedEvent(player.Health, maxHp, delta));
                FrameworkLogger.Info("WeaponSpawnerModule",
                    $"Action=ChestPotionHeal ChestId={e.ChestId} Amount={e.RewardAmount} Heal={delta:F1} NewHp={player.Health:F1}/{maxHp:F1}");
                break;
            }

            default:
                FrameworkLogger.Warn("WeaponSpawnerModule",
                    $"Action=OnChestOpened ChestId={e.ChestId} 未知 RewardType={e.RewardType}");
                break;
        }
    }

    /// <summary>
    /// 商人购买：扣金币后触发 WeaponPickedUpEvent。
    /// </summary>
    [EventHandler]
    void OnMerchantPurchase(MerchantPurchaseEvent e)
    {
        if (!TryGetEconomyActor(e.Actor, out var buyer) || TryGetEconomyModule() is not { } economy)
        {
            FrameworkLogger.Warn("WeaponSpawnerModule",
                $"Action=MerchantPurchaseRejected Reason=EconomyActorMissing WeaponId={e.WeaponId} GoldCost={e.GoldCost}");
            return;
        }

        var inv = economy.GetInventory(buyer);
        if (inv.Coins < e.GoldCost)
        {
            FrameworkLogger.Warn("WeaponSpawnerModule",
                $"Action=MerchantPurchaseRejected Reason=InsufficientCoins WeaponId={e.WeaponId} Coins={inv.Coins} GoldCost={e.GoldCost}");
            return;
        }

        economy.SpendCoin(buyer, e.GoldCost, CoinChangeReason.ShopBuy);

        // 取商人 GO 世界坐标
        Vector3 merchantPos = Vector3.zero;
        if (_activeMerchants.Count == 1 && _activeMerchants[0] != null)
        {
            merchantPos = _activeMerchants[0].transform.position;
        }
        else if (_activeMerchants.Count > 1)
        {
            FrameworkLogger.Warn("WeaponSpawnerModule",
                $"Action=OnMerchantPurchase 场上有 {_activeMerchants.Count} 个商人，无法精确定位，使用 Vector3.zero");
        }

        _bus.Publish(new WeaponPickedUpEvent(e.Actor, e.WeaponId, merchantPos, "Merchant"));
        FrameworkLogger.Info("WeaponSpawnerModule",
            $"Action=MerchantSold WeaponId={e.WeaponId} GoldCost={e.GoldCost} MerchantPos={merchantPos}");
    }

    // ─── 私有辅助方法 ────────────────────────────────────────────────

    /// <summary>
    /// 刷新商人槽位（按 SlotIndex 0~2，各槽按 RefreshWeight 加权随机选一条）。
    /// InitializeAsync 调用，运营也可在特定事件时重调。
    /// </summary>
    void RefreshMerchantSlots()
    {
        _merchantSlots.Clear();
        _merchantSlotRows.Clear();

        for (int slot = 0; slot <= 2; slot++)
        {
            var candidates = _merchantConfig.GetBySlot(slot);
            if (candidates.Count == 0) continue;

            var selectedRow = WeightedRandomRow(candidates, r => r.RefreshWeight);
            if (selectedRow != null)
            {
                _merchantSlots.Add(selectedRow.WeaponId);
                _merchantSlotRows.Add(selectedRow);
            }
        }

        FrameworkLogger.Info("WeaponSpawnerModule",
            $"Action=RefreshMerchantSlots Slots=[{string.Join(",", _merchantSlots)}]");
    }

    /// <summary>返回商人槽位配置行副本，用于 MerchantTrigger.Slots 注入。</summary>
    IReadOnlyList<MerchantConfigRow> BuildMerchantSlotRows()
        => new List<MerchantConfigRow>(_merchantSlotRows);

    /// <summary>
    /// 从 items 中按 weightSelector 加权随机选一个，返回完整行对象；空列表返回 null。
    /// </summary>
    static T WeightedRandomRow<T>(IReadOnlyList<T> items, Func<T, int> weightSelector)
        where T : class
    {
        if (items == null || items.Count == 0) return null;

        int total = 0;
        foreach (var item in items)
            total += weightSelector(item);

        if (total <= 0) return null;

        int roll = UnityEngine.Random.Range(0, total);
        int acc  = 0;
        foreach (var item in items)
        {
            acc += weightSelector(item);
            if (roll < acc) return item;
        }
        return null;
    }

    /// <summary>
    /// 从 items 中按 weightSelector 加权随机选一个，返回 WeaponId；空列表返回 null。
    /// 不在 Update 内调用，一次性运算，允许局部变量。
    /// </summary>
    static string WeightedRandom<T>(IReadOnlyList<T> items, Func<T, int> weightSelector)
        where T : class
    {
        var row = WeightedRandomRow(items, weightSelector);
        if (row == null) return null;

        // 从 row 中提取 WeaponId（duck typing via reflection）
        var prop = row.GetType().GetProperty("WeaponId");
        return prop?.GetValue(row) as string;
    }

    /// <summary>
    /// 尝试从 MapGenModule 读取当前房间号。模块不存在或无该方法时 fallback 返回 1。
    /// 使用反射避免编译期对 MapGenModule.GetCurrentRoomIndex 的硬依赖。
    /// </summary>
    int TryGetCurrentRoomIndex()
    {
        try
        {
            var mapGen = _runner.GetModule<MapGenModule>();
            if (mapGen == null) return 1;

            var method = mapGen.GetType().GetMethod("GetCurrentRoomIndex",
                BindingFlags.Public | BindingFlags.Instance);
            if (method == null) return 1;

            var result = method.Invoke(mapGen, null);
            return result is int i ? i : 1;
        }
        catch
        {
            return 1;
        }
    }

    /// <summary>
    /// 运行时安全获取 EconomyModule。EconomyModule 在同一 category 中，可能晚于本模块初始化。
    /// </summary>
    EconomyModule TryGetEconomyModule()
    {
        try
        {
            return _runner.GetModule<EconomyModule>();
        }
        catch
        {
            return null;
        }
    }

    bool TryGetEconomyActor(Tattoo.Data.Target target, out Actor actor)
    {
        actor = null;
        if (target == null || _spawner?.PlayerActor == null) return false;
        if (!ReferenceEquals(target, _spawner.PlayerTarget)) return false;

        actor = _spawner.PlayerActor;
        return true;
    }

    static GameObject BuildFallbackPickup(string weaponId)
    {
        var root = new GameObject("pickup_" + weaponId);
        AddPart(root.transform, "GlowBase", PrimitiveType.Cylinder,
            new Vector3(0f, -0.18f, 0f), new Vector3(0.7f, 0.04f, 0.7f),
            new Color(0.05f, 0.55f, 0.75f, 0.7f));
        var blade = AddPart(root.transform, "WeaponSilhouette", PrimitiveType.Cube,
            new Vector3(0f, 0.12f, 0f), new Vector3(0.14f, 0.65f, 0.06f),
            new Color(0.75f, 0.95f, 1f));
        blade.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
        AddPart(root.transform, "GoldGuard", PrimitiveType.Cube,
            new Vector3(0f, -0.02f, 0f), new Vector3(0.36f, 0.06f, 0.08f),
            new Color(1f, 0.72f, 0.22f));
        return root;
    }

    static GameObject BuildFallbackChest(string chestId)
    {
        var root = new GameObject("chest_" + chestId);
        AddPart(root.transform, "ChestBody", PrimitiveType.Cube,
            new Vector3(0f, 0.25f, 0f), new Vector3(1.0f, 0.55f, 0.75f),
            new Color(0.45f, 0.23f, 0.08f));
        AddPart(root.transform, "ChestLid", PrimitiveType.Cube,
            new Vector3(0f, 0.6f, 0f), new Vector3(1.08f, 0.22f, 0.82f),
            new Color(0.62f, 0.34f, 0.12f));
        AddPart(root.transform, "Lock", PrimitiveType.Cube,
            new Vector3(0f, 0.38f, -0.42f), new Vector3(0.18f, 0.22f, 0.08f),
            new Color(0.95f, 0.7f, 0.2f));
        return root;
    }

    static GameObject BuildFallbackMerchant()
    {
        var root = new GameObject("merchant");
        AddPart(root.transform, "Body", PrimitiveType.Capsule,
            new Vector3(0f, 0.75f, 0f), new Vector3(0.55f, 0.95f, 0.55f),
            new Color(0.48f, 0.18f, 0.58f));
        AddPart(root.transform, "Head", PrimitiveType.Sphere,
            new Vector3(0f, 1.55f, 0f), new Vector3(0.42f, 0.42f, 0.42f),
            new Color(0.95f, 0.78f, 0.55f));
        AddPart(root.transform, "ShopSign", PrimitiveType.Cube,
            new Vector3(0f, 1.15f, -0.45f), new Vector3(0.8f, 0.28f, 0.08f),
            new Color(0.95f, 0.72f, 0.2f));
        return root;
    }

    static GameObject AddPart(Transform parent, string name, PrimitiveType type,
        Vector3 localPos, Vector3 localScale, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = color;
        }

        var collider = go.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
        return go;
    }
}
