# design — 18-weapon-pickup-and-upgrade

**版本**: v1.0
**创建日期**: 2026-07-01
**决策来源**: ROADMAP §0（已冻结）+ change 20 CONTRACT（不可修改）

---

## 1. 总体架构

### 1.1 模块层次图

```
GameApp（注册顺序）
│
├─ Category 0: DataTableModule / ResourceModule / InputModule
├─ Category 1: TattooModule / StatusEffectModule
├─ Category 2: UIModule / GameStateModule / SceneModule
└─ Category 3:
    ├─ SpawnerModule          (已有，负责精英/Boss 生命周期，新增 SpawnElite() 标记)
    ├─ WeaponModule           (已有，装备/攻击；本 change 不改公共 API)
    │
    ├─ WeaponSpawnerModule    ★新  (deps: SpawnerModule, DataTableModule)
    │   ├─ 订阅 EnemyDiedEvent → 判断 Tier==Elite → SpawnDroppedWeapon()
    │   ├─ 订阅 ChestOpenedEvent → SpawnChestReward()
    │   └─ 订阅 MerchantPurchaseEvent → 扣金 + 触发拾取
    │
    ├─ WeaponUpgradeModule    ★新  (deps: WeaponModule, DataTableModule)
    │   ├─ 订阅 WeaponPickedUpEvent → TryUpgrade()
    │   └─ 对外: GetWeaponLevel(actor, weaponId) / UpgradeWeapon(actor, weaponId)
    │
    ├─ CombatModule           (已有，读 WeaponUpgradeModule 的倍率修正攻击参数)
    ├─ EnemyModule / BossModule
    └─ ...（其余 Category 3 模块不动）
```

### 1.2 事件流总图（ASCII）

```
精英死亡路径：
  EnemyDiedEvent(isElite=true)
    └─▶ WeaponSpawnerModule.OnEnemyDead
          └─▶ 按 WeaponDropConfig 滚权重
                ├─ 命中 → Instantiate WeaponPickup prefab → 场上出现拾取圈
                └─ 未命中 → 无掉落（日志记录）

玩家拾取路径：
  WeaponPickupTrigger.OnTriggerEnter（MonoBehaviour）
    └─▶ 显示"[F] 拾取"世界 UI
          └─ 玩家按 F (InputModule.GetKeyDown("Pickup"))
                └─▶ WeaponPickedUpEvent(actor, weaponId)
                      ├─ WeaponUpgradeModule.OnWeaponPickedUp
                      │   ├─ 同武器 & level<3 → UpgradeWeapon → WeaponUpgradedEvent
                      │   └─ 不同武器 or level=3 → WeaponModule.EquipWeapon（替换）
                      └─ WeaponSpawnerModule.OnWeaponPickedUp → Destroy(pickup GO)

宝箱路径：
  ChestInteractTrigger.OnTriggerEnter → 显示"[F] 开箱"
    └─ 玩家按 F → ChestOpenedEvent(chestId, reward)
          └─▶ WeaponSpawnerModule.OnChestOpened
                ├─ reward=weapon → SpawnDroppedWeapon(at chest pos)
                └─ reward=gold  → EconomyModule.AddGold

商人路径：
  MerchantTrigger.OnTriggerEnter → 显示商店 UI（ShopForm）
    └─ 玩家选购 → MerchantPurchaseEvent(weaponId, goldCost)
          └─▶ WeaponSpawnerModule.OnMerchantPurchase
                ├─ EconomyModule.DeductGold(goldCost)
                └─ WeaponPickedUpEvent(actor, weaponId) → 走上述拾取路径
```

---

## 2. 数据结构

### 2.1 PlayerActor 扩展

```csharp
// 在 Assets/Scripts/Modules/Economy/Actor.cs 追加字段
// （不破坏现有字段，追加到末尾）
public class PlayerActor : Actor
{
    // ★ 新增：武器等级字典，key = WeaponId，value = 1~3
    // 默认不含条目 = 等级视为 1（lazy init）
    public Dictionary<string, int> WeaponLevels { get; } = new();

    /// <summary>获取武器等级，未记录返回 1。</summary>
    public int GetWeaponLevel(string weaponId)
        => WeaponLevels.TryGetValue(weaponId, out var lv) ? lv : 1;
}
```

### 2.2 升级公式（冻结，ROADMAP §0）

```
Level 1 (base):   damage=BaseDamage,       range=BaseRange,     cooldown=BaseCooldown
Level 2 (+1):     damage=BaseDamage*1.2,   range=BaseRange+0.5, cooldown=BaseCooldown*0.9
Level 3 (+2):     damage=BaseDamage*1.44,  range=BaseRange+1.0, cooldown=BaseCooldown*0.81

公式：
  damageMul   = 1.2^(level-1)
  rangeAdd    = 0.5 * (level-1)  [m]
  cooldownMul = 0.9^(level-1)
```

### 2.3 WeaponDropConfig schema（新表）

```json
{
  "table": "WeaponDropConfig",
  "fields": ["DropId","WeaponId","DropSource","Weight","MinRoomIndex","MaxRoomIndex"],
  "rows": [
    { "DropId":"drop_001","WeaponId":"heavy_hammer","DropSource":"Elite","Weight":30,"MinRoomIndex":1,"MaxRoomIndex":10 },
    { "DropId":"drop_002","WeaponId":"bow",          "DropSource":"Elite","Weight":20,"MinRoomIndex":2,"MaxRoomIndex":10 },
    { "DropId":"drop_003","WeaponId":"pistol",       "DropSource":"Elite","Weight":25,"MinRoomIndex":1,"MaxRoomIndex":10 },
    { "DropId":"drop_004","WeaponId":"energy_fist",  "DropSource":"Elite","Weight":15,"MinRoomIndex":3,"MaxRoomIndex":10 },
    { "DropId":"drop_005","WeaponId":"short_blade",  "DropSource":"Elite","Weight":10,"MinRoomIndex":1,"MaxRoomIndex":10 }
  ]
}
```

| 字段 | 类型 | 说明 |
|---|---|---|
| DropId | string | 主键 |
| WeaponId | string | 对应 WeaponConfig.WeaponId |
| DropSource | string | 枚举: `Elite` / `Chest` / `Merchant` |
| Weight | int | 权重（同 DropSource 内按权重滚） |
| MinRoomIndex | int | 最早出现的房间号（关卡进度门控） |
| MaxRoomIndex | int | 最晚出现的房间号 |

### 2.4 ChestConfig schema（新表）

```json
{
  "table": "ChestConfig",
  "fields": ["ChestId","RewardType","RewardId","RewardAmount","Probability"],
  "rows": [
    { "ChestId":"chest_common","RewardType":"Weapon","RewardId":"","RewardAmount":1,"Probability":60 },
    { "ChestId":"chest_common","RewardType":"Gold",  "RewardId":"","RewardAmount":50,"Probability":40 }
  ]
}
```

| 字段 | 类型 | 说明 |
|---|---|---|
| ChestId | string | 主键（同一 ChestId 多行 = 概率池） |
| RewardType | string | 枚举: `Weapon` / `Gold` |
| RewardId | string | Weapon 时填 WeaponId，Gold 时空 |
| RewardAmount | int | Gold 数量 / Weapon 数量（始终 1） |
| Probability | int | 同 ChestId 内百分比，总和必须 =100 |

### 2.5 MerchantConfig schema（新表）

```json
{
  "table": "MerchantConfig",
  "fields": ["SlotIndex","WeaponId","GoldCost","RefreshWeight"],
  "rows": [
    { "SlotIndex":0,"WeaponId":"heavy_hammer","GoldCost":80,"RefreshWeight":30 },
    { "SlotIndex":1,"WeaponId":"bow",          "GoldCost":100,"RefreshWeight":20 },
    { "SlotIndex":2,"WeaponId":"energy_fist",  "GoldCost":120,"RefreshWeight":15 }
  ]
}
```

| 字段 | 类型 | 说明 |
|---|---|---|
| SlotIndex | int | 商人槽位（0~2，最多 3 个） |
| WeaponId | string | 对应 WeaponConfig.WeaponId |
| GoldCost | int | 购买所需金币 |
| RefreshWeight | int | 商人刷新时的随机权重 |

---

## 3. 模块详细设计

### 3.1 WeaponSpawnerModule

**职责**：场上武器实体（GameObject）的统一出口。与 WeaponModule（逻辑装备）互补，不重叠。

**不做**：不计算伤害、不处理拾取判定（→ WeaponPickupTrigger MonoBehaviour）、不管理角色状态。

**状态**：
```csharp
// 场上活跃的武器拾取 GO 列表（用于 Shutdown 清理）
List<GameObject> _activePickups = new();
// 场上宝箱状态字典（是否已开）
Dictionary<GameObject, bool> _chestStates = new();
// 商人当前槽位武器（每局刷新一次）
List<string> _merchantSlots = new();  // 0~2 个 WeaponId
```

**API（对外，fan-out 不改签名）**：
```csharp
public void SpawnDroppedWeapon(string weaponId, Vector3 position);
public void SpawnChest(Vector3 position);
public void SpawnMerchant(Vector3 position);
public List<string> GetMerchantSlots();  // 商人当前槽位
```

**事件订阅**：
```csharp
[EventHandler] void OnEnemyDead(EnemyDiedEvent e);         // 精英掉落判定
[EventHandler] void OnWeaponPickedUp(WeaponPickedUpEvent e); // Destroy pickup GO
[EventHandler] void OnChestOpened(ChestOpenedEvent e);      // 结算宝箱奖励
[EventHandler] void OnMerchantPurchase(MerchantPurchaseEvent e); // 扣金 + 触发拾取
```

**ModuleCategory / Dependencies**：
```csharp
public int ModuleCategory => 3;
public Type[] Dependencies => new[] { typeof(SpawnerModule), typeof(DataTableModule) };
```

**精英掉落逻辑（伪代码）**：
```
OnEnemyDead(e):
  if e.IsElite == false: return
  roomIndex = GetCurrentRoomIndex()  // 从 MapGenModule 读当前房间
  candidates = WeaponDropConfig.All
    .Where(r => r.DropSource == "Elite"
             && roomIndex >= r.MinRoomIndex
             && roomIndex <= r.MaxRoomIndex)
  weaponId = WeightedRandom(candidates, r => r.Weight)
  if weaponId != null:
    SpawnDroppedWeapon(weaponId, e.DeathPosition)
```

### 3.2 WeaponUpgradeModule

**职责**：拾取判定（同武器？）+ 升级公式执行 + 通知 WeaponModule 更新攻击参数。

**不做**：不管场上 GO（→ WeaponSpawnerModule）、不管 UI（→ CombatHUDForm）。

**状态**：
```csharp
// 升级倍率缓存（避免每帧重算）
// key=(actor, weaponId), value=computed multipliers
Dictionary<(Target, string), WeaponMultipliers> _mulCache = new();

public struct WeaponMultipliers
{
    public float DamageMul;   // 1.2^(lv-1)
    public float RangeAdd;    // 0.5*(lv-1)
    public float CooldownMul; // 0.9^(lv-1)
}
```

**API（对外，fan-out 不改签名）**：
```csharp
public int GetWeaponLevel(Target actor, string weaponId);
public WeaponMultipliers GetMultipliers(Target actor, string weaponId);
public bool TryUpgrade(Target actor, string weaponId); // 返回 false 表示已满级
```

**拾取判定逻辑（伪代码）**：
```
OnWeaponPickedUp(e):
  currentWeaponId = WeaponModule.GetEquippedWeapon(e.Actor).Weapon?.WeaponId
  if currentWeaponId == e.WeaponId:
    level = PlayerActor.GetWeaponLevel(e.WeaponId)
    if level < 3:
      PlayerActor.WeaponLevels[e.WeaponId] = level + 1
      InvalidateCache(e.Actor, e.WeaponId)
      bus.Publish(WeaponUpgradedEvent(e.Actor, e.WeaponId, level+1, GetMultipliers()))
    else:
      // 已满级：转化为金币奖励（留 gd-system 数值决定金币数）
      bus.Publish(WeaponMaxLevelConvertEvent(e.Actor, e.WeaponId, goldAmount))
  else:
    WeaponModule.EquipWeapon(e.Actor, e.WeaponId)  // 替换武器，等级从字典读（可能 >1）
```

**ModuleCategory / Dependencies**：
```csharp
public int ModuleCategory => 3;
public Type[] Dependencies => new[] { typeof(WeaponModule), typeof(DataTableModule) };
```

### 3.3 WeaponPickupTrigger（MonoBehaviour）

**职责**：场上每个武器拾取 GO 的碰撞检测 + 玩家按键确认。

```csharp
public class WeaponPickupTrigger : MonoBehaviour
{
    public string WeaponId;           // 由 WeaponSpawnerModule 赋值
    EventBus _bus;                    // 由 WeaponSpawnerModule 赋值（非 GetComponent）
    bool _playerInRange;
    GameObject _promptUI;             // 世界 UI"[F] 拾取"

    void OnTriggerEnter(Collider other) { /* 判断 EntityRef.IsPlayer → 显示 prompt */ }
    void OnTriggerExit(Collider other)  { /* 隐藏 prompt */ }
    void Update()
    {
        if (_playerInRange && InputModule.GetKeyDown("Pickup"))
            _bus.Publish(new WeaponPickedUpEvent(playerActor, WeaponId));
    }
}
```

**按键绑定**：`"Pickup"` 在 InputModule 中映射 KeyCode.F（本 change 需在 InputModule 注册新 Action）。

---

## 4. 状态机 — 武器等级

```
         拾取同武器 & lv<3           拾取同武器 & lv<3
[L1] ─────────────────────▶ [L2] ─────────────────────▶ [L3]
 │                                                          │
 │  拾取不同武器                   拾取不同武器               │  拾取同武器（满级）
 ▼                                                          ▼
[替换=新武器 L1 or 已有等级]                            [转化金币]

状态不变条件：
  - 满级（L3）+ 同武器 → 仅转化金币，不改等级
  - WeaponLevels 字典 key 保留（换武器后旧 key 仍在，再次拾取恢复历史等级）
```

---

## 5. Prefab 设计

### 5.1 武器拾取 Prefab 结构（5 个，按武器 ID 命名）

```
Prefab/Weapon/Pickup/short_blade_pickup.prefab
  ├─ SpriteRenderer (sprite = Resources/Sprite/Weapons/short_blade)
  ├─ BoxCollider (isTrigger=true, size=约 1x1x0.1)
  └─ WeaponPickupTrigger (MonoBehaviour)
```

fallback：若 sprite 缺失，用白色 Cube + SpriteRenderer（空）替代。

### 5.2 宝箱 Prefab

```
Prefab/Chest/chest_common.prefab
  ├─ SpriteRenderer (待 art-ui 出图，fallback Cube)
  ├─ BoxCollider (isTrigger=true)
  ├─ Animator (open/idle 两个 state)
  └─ ChestInteractTrigger (MonoBehaviour)
```

### 5.3 商人 Prefab

```
Prefab/NPC/merchant.prefab
  ├─ SpriteRenderer (待 art-ui 出图，fallback Cube)
  ├─ BoxCollider (isTrigger=true)
  └─ MerchantTrigger (MonoBehaviour，进范围显示 ShopForm)
```

---

## 6. GameApp 注册（追加到 change 20 CONTRACT §5 末尾）

```csharp
// Category 3（在 WeaponModule 之后，CombatModule 之前）：
_runner.AddModule(new WeaponSpawnerModule(_runner, _bus));
_runner.AddModule(new WeaponUpgradeModule(_runner, _bus));
```

**注册顺序约束**：
- WeaponSpawnerModule 必须在 SpawnerModule 之后（因 deps SpawnerModule）
- WeaponUpgradeModule 必须在 WeaponModule 之后（因 deps WeaponModule）

---

## 7. CombatModule 集成

CombatModule 在计算攻击参数时需要读取升级倍率：

```csharp
// CombatModule.OnUpdate 或 FireWeapon 调用路径中：
var upgradeMod = _runner.GetModule<WeaponUpgradeModule>();
var mults = upgradeMod.GetMultipliers(actor, weapon.WeaponId);

// 最终参数
float finalDamage   = weapon.BaseDamage * mults.DamageMul;
float finalRange    = weapon.Range + mults.RangeAdd;
float finalCooldown = baseCooldown * mults.CooldownMul;
```

**注意**：WeaponModule.FireWeapon 内部已有 `BaseDamage`，升级倍率由 CombatModule 在
调用前注入给 FireWeapon。

**已裁定方案 A（client-lead 2026-07-01，详见 CONTRACT.md §H）**：
- CombatModule 在 FireWeapon 调用前查 `WeaponUpgradeModule.GetMultipliers(actor, weaponId)`
- WeaponModule.FireWeapon 签名零变更
- WeaponUpgradeModule 仅作只读数据源，不订阅 WeaponAttackHitEvent
- 否决方案 B（事件链过长 + 已发布事件值不一致）和方案 C（WeaponModule 反向依赖破坏依赖图）

---

## 8. 边界条件 & 异常处理

| 情形 | 处理方式 |
|---|---|
| WeaponId 不在 WeaponConfig | WeaponSpawnerModule 日志 Warn，不 Spawn |
| 精英死亡但 WeaponDropConfig 无命中 | 日志 Info"无掉落"，场上不出现拾取圈 |
| ChestConfig 概率总和 ≠ 100 | DataTableModule 加载时 Assert，Editor 期警告 |
| 金币不足购买商人武器 | MerchantTrigger UI 按钮灰显，不发事件 |
| WeaponLevels 读取 missing key | 返回 1（lazy default），无异常 |
| 玩家同时进入两个拾取圈 | 按 OnTriggerEnter 顺序，最后进入的 prompt 覆盖 |
| 满级（L3）再次拾取同武器 | 转化为 goldAmount 金币，WeaponMaxLevelConvertEvent（金币数由 gd-system 填 MerchantConfig.GoldCost × 0.5） |
| 宝箱已开仍触发交互 | ChestInteractTrigger 判断 isOpened=true → 忽略按键 |

---

## 9. 新增 InputModule Action

| Action 名 | KeyCode | 说明 |
|---|---|---|
| `Pickup` | F | 拾取武器 / 开宝箱 / 与商人交互（统一单键） |

---

## 10. 测试策略（qa-engineer B4 阶段）

| 测试 ID | 类型 | 场景 | 验收 |
|---|---|---|---|
| TC-Pickup-01 | UTF 单测 | TryUpgrade(L1 同武器) → 返回 true, level=2 | level==2 |
| TC-Pickup-02 | UTF 单测 | TryUpgrade(L3 同武器) → 返回 false, 发 Convert事件 | level==3, event fired |
| TC-Pickup-03 | UTF 单测 | GetMultipliers(L2) → damageMul=1.2, rangeAdd=0.5, cooldownMul=0.9 | 精确到 0.001 |
| TC-Pickup-04 | UTF 单测 | 不同武器拾取 → EquipWeapon 被调用 | WeaponModule.EquipWeapon called |
| TC-Pickup-E2E | playtest-driver | 精英死亡 → 拾取圈出现 → 按 F → UI 更新 | 全链路无报错 |

---

## 11. 风险登记

| ID | 描述 | 概率 | 影响 | 缓解 |
|---|---|---|---|---|
| R1 | CombatModule 注入倍率路径与 WeaponModule.FireWeapon 签名冲突 | 中 | 高 | client-lead 骨架阶段裁定方案 A/B |
| R2 | EnemyDiedEvent 未携带 isElite / DeathPosition 字段 | 中 | 中 | 骨架阶段检查 EnemyEvents.cs，按需扩展 |
| R3 | InputModule 新增 Pickup Action 与现有按键冲突 | 低 | 低 | 检查 InputModule 已有 Action 清单 |
| R4 | WeaponPickupTrigger 访问 EventBus 时机（InitializeAsync 前） | 低 | 中 | WeaponSpawnerModule 在 SpawnDroppedWeapon 时注入 bus 引用 |
