# weapon-pickup Specification

## Purpose
TBD - created by archiving change 18-weapon-pickup-and-upgrade. Update Purpose after archive.
## Requirements
### Requirement: 武器拾取事件总表（CONTRACT §A）

系统 MUST 定义并通过 `EventBus` 发布以下 5 个事件，签名与 `Assets/Scripts/Events/WeaponPickupEvents.cs` 一致：`WeaponPickedUpEvent(Actor, WeaponId, PickupPosition)`、`WeaponUpgradedEvent(Actor, WeaponId, NewLevel, DamageMul, RangeAdd, CooldownMul)`、`ChestOpenedEvent(ChestId, RewardType, RewardId, RewardAmount, ChestPosition)`、`MerchantPurchaseEvent(Actor, WeaponId, GoldCost)`、`WeaponMaxLevelConvertEvent(Actor, WeaponId, GoldConverted)`。

#### Scenario: 玩家走入拾取触发器按 F 拾取武器
- **GIVEN** 玩家未持有 `weapon_pistol`，场上有 `weapon_pistol` 拾取 GO
- **WHEN** 玩家进入触发器并按 F（走 `InputModule.IsPickupPressed()`）
- **THEN** `WeaponPickedUpEvent(actor, "weapon_pistol", pos)` publish；`WeaponSpawnerModule` 收事件后 Destroy 对应 GO

#### Scenario: 同类武器升级
- **GIVEN** 玩家持有 L1 `weapon_pistol`
- **WHEN** 拾取场上另一把 `weapon_pistol`
- **THEN** `WeaponUpgradedEvent(actor, "weapon_pistol", NewLevel=2, DamageMul=1.2, RangeAdd=0.5, CooldownMul=0.9)` publish

#### Scenario: 满级武器转金币
- **GIVEN** 玩家持有 L3 `weapon_pistol`
- **WHEN** 拾取场上另一把 `weapon_pistol`
- **THEN** `WeaponMaxLevelConvertEvent(actor, "weapon_pistol", GoldConverted=Round(MerchantConfig.GoldCost*0.5))` publish；`EconomyAddGoldEvent` 紧随其后

---

### Requirement: 升级倍率公式（design.md §2.2 / CONTRACT §H）

`WeaponUpgradeModule.GetMultipliers(actor, weaponId)` MUST 返回 `WeaponMultipliers { DamageMul, RangeAdd, CooldownMul }`，其中 `DamageMul = Mathf.Pow(1.2f, level-1)`、`RangeAdd = 0.5f*(level-1)`、`CooldownMul = Mathf.Pow(0.9f, level-1)`，等级范围 1~3 封顶。

#### Scenario: 未持有武器返回默认乘数 (Identity)
- **GIVEN** Actor 未持有 `weapon_pistol`
- **WHEN** 调用 `GetMultipliers(actor, "weapon_pistol")`
- **THEN** 返回 `WeaponMultipliers.Identity`（DamageMul=1, RangeAdd=0, CooldownMul=1）

#### Scenario: L2 武器乘数精度
- **GIVEN** Actor 持有 L2 `weapon_pistol`
- **WHEN** 调用 `GetMultipliers(actor, "weapon_pistol")`
- **THEN** |DamageMul-1.2| < 0.001、|RangeAdd-0.5| < 0.001、|CooldownMul-0.9| < 0.001

#### Scenario: L3 武器乘数精度
- **GIVEN** Actor 持有 L3 `weapon_pistol`
- **WHEN** 调用 `GetMultipliers(actor, "weapon_pistol")`
- **THEN** |DamageMul-1.44| < 0.001、|RangeAdd-1.0| < 0.001、|CooldownMul-0.81| < 0.001

---

### Requirement: CombatModule 主动注入升级倍率（CONTRACT §H 方案 A）

`CombatModule` 在执行普攻前 MUST 查询 `WeaponUpgradeModule.GetMultipliers(actor, weaponId)`，再通过 `WeaponModule.SetPendingMultipliers(mul)` 注入；`WeaponModule.FireWeapon` MUST 消费 pending 乘数并应用到 `finalDamage = BaseDamage * mul.DamageMul`，消费后清空。`FireWeapon` 公共签名 MUST 保持不变（兼容 change 20）。

#### Scenario: L2 武器普攻伤害放大 1.2 倍
- **GIVEN** Actor 持有 L2 `weapon_pistol`（BaseDamage=10），CombatModule 处理普攻输入
- **WHEN** CombatModule.ProcessController → SetPendingMultipliers → FireWeapon
- **THEN** 实际造成伤害 = 12（10 * 1.2），Hit event 中 Damage=12

#### Scenario: WeaponUpgradeModule 未注册时 fallback
- **GIVEN** WeaponUpgradeModule 未启用（ModuleRunner.GetModule 返回 null）
- **WHEN** CombatModule.ProcessController 处理普攻
- **THEN** 不抛异常；FireWeapon 按 `WeaponMultipliers.Identity` 处理；BaseDamage 原样输出

---

### Requirement: 精英敌人按 WeaponDropConfig 权重掉落

`WeaponSpawnerModule` MUST 订阅 `EnemyDeadEvent`，仅当 `e.IsElite==true` 时按 `WeaponDropConfig.GetByDropSource("Elite")` 加权随机选 1 把武器；并在 `e.Position` Spawn 拾取 GO。Light/Boss 死亡 MUST NOT 触发。

#### Scenario: 普通 Light 敌人死亡不掉武器
- **GIVEN** Light EnemyActor 死亡
- **WHEN** `EnemyDeadEvent(IsElite=false)` publish
- **THEN** WeaponSpawnerModule.OnEnemyDead 跳过；场上无新拾取 GO

#### Scenario: 精英敌人按权重掉落已知武器
- **GIVEN** Elite EnemyActor 在 RoomIndex=5 死亡，WeaponDropConfig 有 3 行 Elite 来源（Weight 各 30/40/30）
- **WHEN** `EnemyDeadEvent(IsElite=true, Position=P)` publish
- **THEN** WeaponSpawnerModule 按权重随机选 1 行，SpawnDroppedWeapon(weaponId, P)；GO 上挂 WeaponPickupTrigger

---

### Requirement: 宝箱 / 商人 概率与扣金（fallback 容错）

宝箱 MUST 按 `ChestConfig.GetByChestId(chestId)` 概率结算奖励；商人槽位 MUST 按 `MerchantConfig.GetBySlot(slotIndex)` 的 `RefreshWeight` 加权随机刷新。商人购买 MUST 走反射 `EconomyModule.DeductGold(actor, GoldCost)` 检查；EconomyModule 缺失时 MUST log Warn 但不阻塞流程。

#### Scenario: chest_common 概率分布
- **GIVEN** ChestConfig 中 chest_common 3 行（Weapon 60% / Gold 30% / Potion 10%）
- **WHEN** 玩家开宝箱触发 `ChestOpenedEvent(ChestId="chest_common", ...)` publish
- **THEN** WeaponSpawnerModule.OnChestOpened 按 e.RewardType 分支结算（Weapon → SpawnDroppedWeapon / Gold → EconomyAddGoldEvent）

#### Scenario: 商人槽位刷新与购买
- **GIVEN** WeaponSpawnerModule.InitializeAsync 完成（RefreshMerchantSlots 已调用），MerchantConfig 槽位 0/1/2 各 2 候选
- **WHEN** 调用 `GetMerchantSlots()`
- **THEN** 返回 3 个 WeaponId（每槽 1 个，按 RefreshWeight 加权随机）
- **WHEN** 玩家点击购买按钮且金币足够，`MerchantPurchaseEvent(actor, weaponId, GoldCost)` publish
- **THEN** WeaponSpawnerModule.OnMerchantPurchase 反射调 EconomyModule.DeductGold → publish `WeaponPickedUpEvent`

