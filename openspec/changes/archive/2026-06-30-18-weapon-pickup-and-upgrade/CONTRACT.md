# CONTRACT — 18-weapon-pickup-and-upgrade

> **fan-out 阶段的圣经**：任何 agent 改动以下契约 → 主对话立即回滚。
> 改动须先提交修订本文件，由 client-lead 复核后方可执行。
>
> **上游约束**：本 CONTRACT 不得修改 change 20 CONTRACT 的任何内容（事件签名 / 模块依赖 / DataTable fields[]）。

---

## §A 全局事件总表

### §A.1 本 change 新增事件（新建于 `Assets/Scripts/Events/WeaponPickupEvents.cs`）

| 事件 | 签名 | 发布方 | 订阅方 |
|---|---|---|---|
| `WeaponPickedUpEvent` | `(Target actor, string weaponId, Vector3 pickupPosition)` | WeaponPickupTrigger.Update | WeaponUpgradeModule（升级判定）/ WeaponSpawnerModule（Destroy GO） |
| `WeaponUpgradedEvent` | `(Target actor, string weaponId, int newLevel, float damageMul, float rangeAdd, float cooldownMul)` | WeaponUpgradeModule.TryUpgrade | CombatHUDForm（UI 等级显示）/ CombatModule（倍率注入） |
| `ChestOpenedEvent` | `(string chestId, string rewardType, string rewardId, int rewardAmount, Vector3 chestPosition)` | ChestInteractTrigger.OnInteract | WeaponSpawnerModule（结算奖励） |
| `MerchantPurchaseEvent` | `(Target actor, string weaponId, int goldCost)` | MerchantTrigger.OnPurchase（UI 按钮） | WeaponSpawnerModule（扣金 + 触发 WeaponPickedUpEvent） |
| `WeaponMaxLevelConvertEvent` | `(Target actor, string weaponId, int goldConverted)` | WeaponUpgradeModule（满级时） | EconomyModule（加金币）/ CombatHUDForm（提示文字） |

### §A.2 复用 change 20 事件（不改签名）

| 事件 | 来源文件 | 本 change 用途 |
|---|---|---|
| `WeaponEquippedEvent` | `Assets/Scripts/Events/AttackSystemEvents.cs` | WeaponUpgradeModule 触发 WeaponModule.EquipWeapon 后由 WeaponModule 发布（不变） |
| `EnemyDeadEvent` | `Assets/Scripts/Modules/Enemy/EnemyEvents.cs` | WeaponSpawnerModule 订阅，判断 isElite 字段 |
| `AmmoChangedEvent` | `Assets/Scripts/Events/WeaponEvents.cs` | 武器替换后弹药重置，WeaponModule 自动发 |

### §A.3 EnemyDiedEvent 复用（★B2 已裁定 — 2026-07-01）

**经 client-lead B2 阶段核查，现有 `Tattoo.Events.EnemyDiedEvent`（`Assets/Scripts/Modules/Enemy/EnemyEvents.cs`）字段已足够，不追加任何字段，复用现有签名**：

```csharp
public sealed class EnemyDiedEvent
{
    public EnemyActorData DeadActor { get; }   // 含 Tier（EnemyTier.Light/Elite/Boss）字段
    public EnemyActorData Killer    { get; }
    public Vector3        DeathPos  { get; }
}
```

- 精英判定 → `e.DeadActor.Tier == EnemyTier.Elite`
- 死亡坐标 → `e.DeathPos`（注意：不是 `DeathPosition`）
- 事件名 → `EnemyDiedEvent`（不是 `EnemyDeadEvent`），文档中所有 `EnemyDeadEvent` 字样在 18-A 实现时按 `EnemyDiedEvent` 落地

**禁止**：18-A agent 不许追加 `bool IsElite` 等冗余字段——`Tier` 枚举已能区分。

### §A.4 禁止

- ❌ 不许重命名/移除 WeaponPickupEvents.cs 中已有字段
- ❌ 不许把 WeaponPickedUpEvent 合并到 WeaponEquippedEvent（职责不同：前者是拾取意图，后者是装备完成）
- ❌ 不许 18-A / 18-B / 18-C agent 自行新增未在此表的事件（新事件 → 修订本 CONTRACT）

---

## §B 模块依赖图（追加到 change 20 CONTRACT §2）

```
Category 3（在 WeaponModule 之后，CombatModule 之前）：
  WeaponSpawnerModule  ★新  deps: [SpawnerModule, DataTableModule]
  WeaponUpgradeModule  ★新  deps: [WeaponModule, DataTableModule]
```

### §B.1 Dependencies 字段值（fan-out 不许改）

```csharp
// WeaponSpawnerModule
public int ModuleCategory => 3;
public Type[] Dependencies => new[] { typeof(SpawnerModule), typeof(DataTableModule) };

// WeaponUpgradeModule
public int ModuleCategory => 3;
public Type[] Dependencies => new[] { typeof(WeaponModule), typeof(DataTableModule) };
```

### §B.2 GameApp 注册顺序（追加到现有 AddModule 序列末尾，Category 3 内）

```csharp
// 在 WeaponModule AddModule 之后，CombatModule AddModule 之前：
_runner.AddModule(new WeaponSpawnerModule(_runner, _bus));
_runner.AddModule(new WeaponUpgradeModule(_runner, _bus));
```

---

## §C 跨模块 GetModule<T> 查询接口表

| 调用方 | 被调方 | 接口 | 用途 |
|---|---|---|---|
| WeaponSpawnerModule | SpawnerModule | `PlayerTarget` | 判断触发拾取的是否为玩家 |
| WeaponSpawnerModule | DataTableModule | `GetTable<WeaponDropConfig>()` | 精英掉落权重 |
| WeaponSpawnerModule | DataTableModule | `GetTable<ChestConfig>()` | 宝箱概率表 |
| WeaponSpawnerModule | DataTableModule | `GetTable<MerchantConfig>()` | 商人刷新槽位 |
| WeaponSpawnerModule | （EconomyModule，直接 Publish 不 GetModule） | 通过 `EconomyAddGoldEvent`（待定命名，B2 确认） | 宝箱开出金币 |
| WeaponUpgradeModule | WeaponModule | `GetEquippedWeapon(actor)` | 判断当前武器 ID |
| WeaponUpgradeModule | DataTableModule | `GetTable<WeaponConfig>()` | 读 BaseDamage/Range/Cooldown 基础值 |
| CombatModule | WeaponUpgradeModule | `GetMultipliers(actor, weaponId)` | 注入升级倍率到攻击参数 |
| CombatHUDForm | WeaponUpgradeModule | `GetWeaponLevel(actor, weaponId)` | 显示武器等级标记（L1/L2/L3） |

### §C.1 禁止反向调用

- ❌ WeaponUpgradeModule 不许 `GetModule<CombatModule>()`
- ❌ WeaponSpawnerModule 不许 `GetModule<UIModule>()`（UI 通过事件订阅）
- ❌ WeaponPickupTrigger（MonoBehaviour）不许 `GetModule<T>()` ——EventBus 由 WeaponSpawnerModule 在 Spawn 时注入

---

## §D DataTable Schema（新增 3 张表）

### §D.1 WeaponDropConfig（新表）

文件路径: `Assets/Resources/DataTable/WeaponDropConfig.json`
生成路径: `Assets/Scripts/DataTable/WeaponDropConfig.cs`（DataTableGenerator 跑后产生）

```
fields[] 顺序（冻结）:
  DropId (string) | WeaponId (string) | DropSource (string) | Weight (int) | MinRoomIndex (int) | MaxRoomIndex (int)
```

枚举约束：`DropSource` 仅允许 `"Elite"` / `"Chest"` / `"Merchant"`

### §D.2 ChestConfig（新表）

文件路径: `Assets/Resources/DataTable/ChestConfig.json`
生成路径: `Assets/Scripts/DataTable/ChestConfig.cs`

```
fields[] 顺序（冻结）:
  ChestId (string) | RewardType (string) | RewardId (string) | RewardAmount (int) | Probability (int)
```

约束：同 `ChestId` 的所有行 `Probability` 之和 **必须 = 100**（DataTableModule 加载时 Assert）

### §D.3 MerchantConfig（新表）

文件路径: `Assets/Resources/DataTable/MerchantConfig.json`
生成路径: `Assets/Scripts/DataTable/MerchantConfig.cs`

```
fields[] 顺序（冻结）:
  SlotIndex (int) | WeaponId (string) | GoldCost (int) | RefreshWeight (int)
```

约束：`SlotIndex` 范围 0~2，不允许重复（如需多个候选同 slot，用 `RefreshWeight` 随机刷新）

### §D.4 禁止

- ❌ 不许改 change 20 CONTRACT §4.1 WeaponConfig 的 fields[] 顺序
- ❌ 不许在 WeaponConfig 追加字段（升级倍率由 WeaponUpgradeModule 动态计算，不入表）
- ❌ 不许把三张新表合并为一张（职责分离，方便 gd-system 独立维护）

---

## §E Prefab 文件位置（冻结）

| Prefab | 路径 | 备注 |
|---|---|---|
| short_blade_pickup | `Assets/Resources/Prefab/Weapon/Pickup/short_blade_pickup.prefab` | |
| heavy_hammer_pickup | `Assets/Resources/Prefab/Weapon/Pickup/heavy_hammer_pickup.prefab` | |
| pistol_pickup | `Assets/Resources/Prefab/Weapon/Pickup/pistol_pickup.prefab` | |
| bow_pickup | `Assets/Resources/Prefab/Weapon/Pickup/bow_pickup.prefab` | |
| energy_fist_pickup | `Assets/Resources/Prefab/Weapon/Pickup/energy_fist_pickup.prefab` | |
| chest_common | `Assets/Resources/Prefab/Chest/chest_common.prefab` | |
| merchant | `Assets/Resources/Prefab/NPC/merchant.prefab` | |

fallback 规则：美术未出图 → Cube + SpriteRenderer（sprite=null），不阻塞 18-A 实现。

---

## §F 命名 / 文件位置冻结（本 change 新建文件）

| 类 | 路径 | namespace |
|---|---|---|
| `WeaponPickupEvents`（含 5 事件） | `Assets/Scripts/Events/WeaponPickupEvents.cs` | 顶层（无 namespace，与 TattooEvents 一致） |
| `WeaponSpawnerModule` | `Assets/Scripts/Modules/Weapon/WeaponSpawnerModule.cs` | 顶层 |
| `WeaponUpgradeModule` | `Assets/Scripts/Modules/Weapon/WeaponUpgradeModule.cs` | 顶层 |
| `WeaponPickupTrigger` | `Assets/Scripts/Modules/Weapon/WeaponPickupTrigger.cs` | 顶层 |
| `ChestInteractTrigger` | `Assets/Scripts/Modules/Weapon/ChestInteractTrigger.cs` | 顶层 |
| `MerchantTrigger` | `Assets/Scripts/Modules/Weapon/MerchantTrigger.cs` | 顶层 |
| `WeaponUpgradeTests` | `Assets/Tests/WeaponUpgradeTests.cs` | `Tests` |

---

## §G 责任分界

| Agent | 可改 | 不可改 |
|---|---|---|
| **B2 client-lead** | 骨架文件方法体（返回 CompletedTask）/ EnemyDeadEvent 追加字段（按 §A.3 规则） | 事件签名（一经写入不许改）/ GameApp 注册顺序 |
| **18-A client-unity** | WeaponSpawnerModule 方法体 / Prefab fallback 建立 | 事件签名 / Dependencies |
| **18-B client-unity** | WeaponUpgradeModule 方法体 / UTF 单测 | 升级公式（只能按 design.md §2.2 实现）/ GetMultipliers 签名 |
| **18-C client-unity** | WeaponPickupTrigger 等 MonoBehaviour 方法体 / InputModule Pickup Action 注册 | EventBus 注入方式（必须由 WeaponSpawnerModule 注入） |
| **18-D gd-system** | 三张配置表 JSON 数值 / balance18.md | fields[] 顺序（已冻结于本 CONTRACT §D）|

---

**违反 CONTRACT 一律由 main 主对话立即回滚 + 重新派单。**

---

## §H 升级倍率注入路径裁定（R1 — 已裁定 2026-07-01）

### §H.1 决策结果：方案 A（CombatModule 注入）

**CombatModule 在调用 `WeaponModule.FireWeapon` 之前**，先查询 `WeaponUpgradeModule.GetMultipliers(actor, weaponId)` 取得倍率，将修正后的 damage/range/cooldown 注入到攻击参数。`WeaponUpgradeModule` 作只读数据源，**不 hook `WeaponAttackHitEvent`**，不修改 `WeaponModule.FireWeapon` 签名。

### §H.2 候选方案对比（trade-off 留档）

| 方案 | 实现方式 | Pros | Cons | 选择 |
|---|---|---|---|---|
| **A. CombatModule 注入（采纳）** | CombatModule 在 FireWeapon 前查 GetMultipliers，自己组装最终伤害 | 1. 不改 WeaponModule 公共 API（签名零变更）<br>2. 调用链清晰：Combat→Upgrade→Weapon，单向<br>3. 倍率值在调用点可见，便于日志和调试 | CombatModule 多一次 GetModule 调用（O(1) 哈希查询，可忽略） | ✅ |
| B. WeaponUpgradeModule 订阅 WeaponAttackHitEvent 二次修正 | 事件后处理伤害 | 调用方完全无感 | 1. 事件链拉长，调试困难<br>2. 已发布的事件 BaseDamage 与最终 damage 不一致，下游 VFX/HUD 显示错误<br>3. 违反"事件 = 不可变事实"原则 | ❌ |
| C. WeaponModule 内部查 WeaponUpgradeModule | WeaponModule.FireWeapon 内 GetModule<WeaponUpgradeModule> | 调用方完全无感 | 1. WeaponModule 反向依赖 WeaponUpgradeModule，破坏 Category 3 依赖图<br>2. 循环耦合（Upgrade 也依赖 Weapon） | ❌ |

### §H.3 影响范围（18-A/B/C/E agent 必读）

| Agent | 影响 |
|---|---|
| **18-B WeaponUpgradeModule 实现者** | `GetMultipliers(Target actor, string weaponId)` 是**只读 API**，禁止在该方法内修改任何状态；禁止 publish 事件 |
| **18-E（如有 CombatModule 集成）/ 未来 client-unity** | CombatModule.FireWeapon 调用点改为：`var m = upgradeMod.GetMultipliers(actor, weaponId); float dmg = base * m.DamageMul; ...` 然后 `weaponMod.FireWeapon(...)`。WeaponModule.FireWeapon 签名**不变** |
| **18-A WeaponSpawnerModule 实现者** | 不受影响（不接触伤害路径） |

### §H.4 性能预算

- `GetMultipliers` 调用频率：每次 FireWeapon 一次（玩家攻击频率约 1–5 次/秒）
- 单次开销：1 次 Dictionary TryGetValue + 1 次 readonly struct 拷贝 = O(1)，<0.001ms
- **缓存策略**：WeaponUpgradeModule 内部用 `Dictionary<(Target, string), WeaponMultipliers>` 缓存；`WeaponUpgradedEvent` 触发时 Invalidate 对应 entry
- 不进入 Update / Hot Path，无 GC alloc 风险
