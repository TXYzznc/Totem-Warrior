# CONTRACT — 20-player-attack-system

> **fan-out 阶段的圣经**：任何 agent 在阶段 3 改动以下契约 → 主对话立即回滚。
> 改动须先提交 PR 修订本文件，由 client-lead 复核。

---

## §1 事件总表

### §1.1 新增事件（本 change 新建于 `Assets/Scripts/Events/AttackSystemEvents.cs`）

| 事件 | 签名 | 发布方 | 订阅方 |
|---|---|---|---|
| `StartupSelectedEvent` | `(int colorId, string weaponId, int[] patternIds)` | StartupSelectForm.OnConfirm | SpawnerModule（装备玩家） |
| `WeaponEquippedEvent` | `(Target actor, string weaponId, string weaponPrefabPath)` | WeaponModule.EquipWeapon | PlayerWeaponMounter（卸旧装新） |
| `StatusEffectAppliedEvent` | `(Target target, string statusName, float dps, float duration, Target source)` | StatusEffectModule.ApplyStatus | CombatHUDForm（小图标） / VFXModule |
| `StatusEffectTickedEvent` | `(Target target, string statusName, float damage)` | StatusEffectModule.OnUpdate（0.5s tick） | CombatHUDForm（浮动数字） |
| `StatusEffectExpiredEvent` | `(Target target, string statusName)` | StatusEffectModule（remaining ≤ 0） | CombatHUDForm（移除图标） |
| `PlayerHealthChangedEvent` | `(float current, float max, float delta)` | PlayerDamageReceiver | CombatHUDForm（血条） |

### §1.2 复用现有事件（**不修改签名**）

| 事件 | 现有定义位置 | 本 change 用途 |
|---|---|---|
| `WeaponAttackHitEvent` | `Assets/Scripts/Events/WeaponEvents.cs` | TattooModule 桥接到 AttackHitEvent；HeadPartBehavior 监听用于触发 CritHit |
| `WeaponChargedAttackEvent` | `Assets/Scripts/Events/WeaponEvents.cs` | 同上 |
| `AttackHitEvent` | `Assets/Scripts/Events/TattooEvents.cs` | TattooModule.Fire 入口（不动） |
| `CritHitEvent` | `Assets/Scripts/Events/TattooEvents.cs` | HeadPartBehavior 按 Pattern 概率发 |
| `DamagedEvent` | `Assets/Scripts/Events/TattooEvents.cs` | PlayerDamageReceiver 玩家受击时发 |
| `SkillActivatedEvent` | `Assets/Scripts/Modules/Skill/SkillEvents.cs` | SkillHitResolver 订阅 |
| `EnemyAttackEvent` | `Assets/Scripts/Modules/Enemy/EnemyModule.cs:496`（namespace `Tattoo.Enemy.Events`） | PlayerDamageReceiver 订阅 |
| `EffectAppliedEvent` | `Assets/Scripts/Events/TattooEvents.cs` | StatusEffectModule 订阅以解析 status 串 |
| `PlayerDiedEvent` | `Assets/Scripts/Events/TattooEvents.cs` | PlayerDamageReceiver 在 HP≤0 发；CombatModule 已订阅 |

### §1.3 禁止改动

- ❌ 不许重命名/移除已有事件字段
- ❌ 不许把现有事件改为 internal / private
- ❌ 不许加 `WeaponAttackHitEvent.IsCrit = true` 当暴击 → 暴击走独立 `CritHitEvent`
- ❌ 不许把 `EnemyAttackEvent` 改为 `EnemyAttackPlayerEvent`（命名冻结）

---

## §2 模块依赖图（GameApp.cs 注册顺序）

```
Category 0:
  - DataTableModule
  - ResourceModule (deps DataTable)
  - InputModule

Category 1:
  - SaveModule
  - AudioModule
  - SettingsModule
  - TattooModule (deps DataTable)
  - StatusEffectModule ★新 (deps 无)

Category 2:
  - GameStateModule
  - SceneModule
  - UIModule

Category 3:
  - MapGenModule
  - SpawnerModule (deps 无)
  - WeaponModule (deps DataTable)
  - SkillModule (deps DataTable, Weapon)
  - SkillHitResolver ★新 (deps Weapon, DataTable)
  - PlayerDamageReceiver ★新 (deps Spawner)
  - CombatModule (deps Tattoo, Spawner, Input)
  - VFXModule
  - EconomyModule
  - EnemyModule
  - BossModule
  - NPCModule
  - BotControllerModule
  - EventModule
```

### §2.1 Dependencies 字段值（fan-out 不许改）

```csharp
// StatusEffectModule
public int ModuleCategory => 1;
public Type[] Dependencies => Type.EmptyTypes;

// PlayerDamageReceiver
public int ModuleCategory => 3;
public Type[] Dependencies => new[] { typeof(SpawnerModule) };

// SkillHitResolver
public int ModuleCategory => 3;
public Type[] Dependencies => new[] { typeof(WeaponModule), typeof(DataTableModule) };
```

---

## §3 跨模块查询接口表（GetModule<T> 路径）

| 调用方 | 被调方 | 接口 | 用途 |
|---|---|---|---|
| StatusEffectModule | （无） | 内部 `Dictionary<Target, List<ActiveStatus>>` | 自维护 status 表 |
| PlayerDamageReceiver | SpawnerModule | `PlayerTarget` / `PlayerMaxHp` | 玩家身份 + 初始 HP 上限 |
| SkillHitResolver | WeaponModule | `GetBaseDamage(actor)` | 技能伤害以武器 base 为基准 |
| SkillHitResolver | DataTableModule | `GetTable<SkillConfig>()` | 读 DamageMul / HitShape / HitRadius |
| HumanPlayerController | WeaponModule | `GetEquippedWeapon(actor).Weapon` → `AimSpreadHalfDeg` / `Range` | 鼠标射击半角 + 最远距离 |
| StartupSelectForm | SaveModule | `.Data.PatternUnlocks` | 解锁的图案候选 |
| StartupSelectForm | DataTableModule | `GetTable<WeaponConfig>()` / `GetTable<TattooPatternConfig>()` / `GetTable<TattooColorConfig>()` | 三选 UI 候选数据源 |
| SpawnerModule | WeaponModule | `EquipWeapon(actor, weaponId)` | StartupSelectedEvent 后调 |
| SpawnerModule | TattooModule | `Equip(partId, colorId, patternId)` | 同上 |

### §3.1 禁止跨依赖反向调用

- ❌ StatusEffectModule 不许 `GetModule<TattooModule>()`——它只通过 `EffectAppliedEvent` 解析获取信息
- ❌ PlayerDamageReceiver 不许 `GetModule<UIModule>()`——HUD 通过事件订阅自取
- ❌ SkillHitResolver 不许直接动 target.Health——发 AttackHitEvent 走 TattooModule.Fire

---

## §4 DataTable Schema

### §4.1 WeaponConfig 扩展（添加 4 字段）

完整字段列表（**fields[] 顺序冻结**）：

| 字段 | 类型 | 说明 | 新增？ |
|---|---|---|---|
| WeaponId | string | 主键 | 原有 |
| Name | string | 显示名称 | 原有 |
| Class | string | Melee / Ranged / Special | 原有 |
| BaseDamage | float | | 原有 |
| AttackSpeed | float | | 原有 |
| Range | float | | 原有 |
| ChargedMul | float | | 原有 |
| ProjectileId | string | | 原有 |
| Rarity | int | | 原有 |
| MaxAmmo | int | | 原有 |
| BaseStartup | int | | 原有 |
| BaseActive | int | | 原有 |
| BaseRecovery | int | | 原有 |
| RequiresCharge | bool | | 原有 |
| **AimSpreadHalfDeg** | **float** | **0 = Raycast 严格 / 180 = 自动锁定 / 中间 = SphereCast 半角** | ★ 新增 |
| **NormalTraitId** | **string** | **WeaponTraitConfig.TraitId，普攻 trait** | ★ 新增 |
| **ChargedTraitId** | **string** | **同上，蓄力 trait** | ★ 新增 |
| **WeaponPrefabPath** | **string** | **Resources 路径（相对 Resources/，不带扩展名），如 `Prefab/Weapon/Knife`** | ★ 新增 |

### §4.2 WeaponTraitConfig（新表）

字段（**fields[] 顺序冻结**）：

| 字段 | 类型 | 说明 |
|---|---|---|
| TraitId | string | 主键，如 `trait_quickslash` / `trait_pierce` |
| Name | string | 显示名称 |
| Description | string | UI tooltip |
| EffectType | string | 枚举：`Status` / `Pierce` / `Stun` / `Chain` / `Explosive` / `MultiShot` / `Pull` / `Quick` |
| EffectParam1 | float | 主参数（如 Pierce 的穿透数 / Stun 的秒数 / Quick 的减后摇百分比） |
| EffectParam2 | float | 副参数（如 Chain 的衰减系数 / Explosive 的范围半径） |

### §4.3 UIFormConfig 新增 1 行（Id=12）

```json
{ "Id": 12, "FormName": "StartupSelectForm", "PrefabPath": "UI/StartupSelect", "SortOrder": 30, "IsExclusive": false }
```

### §4.4 禁止改动

- ❌ 不许改 fields[] 顺序（DataTableGenerator 按顺序生成 C# property）
- ❌ 不许把 WeaponConfig 的 BaseDamage 改为 int（破坏 336 TC）
- ❌ 不许移除 WeaponConfig.RequiresCharge（弓蓄力逻辑依赖）

---

## §5 GameApp 注册（zone of immutability）

`Assets/Scripts/Core/GameApp.cs` 中的 `_runner.AddModule` 调用顺序：

```csharp
// 严格按下列顺序追加 3 个新模块（按 ModuleCategory 排序）：
// Category 1（在 TattooModule 之后）：
_runner.AddModule(new StatusEffectModule(_runner, _bus));

// Category 3（在 SkillModule 之后，CombatModule 之前）：
_runner.AddModule(new SkillHitResolver(_runner, _bus));

// Category 3（在 SpawnerModule 之后，CombatModule 之前）：
_runner.AddModule(new PlayerDamageReceiver(_runner, _bus));
```

- ❌ fan-out agent 不许调整这 3 个模块的注册顺序
- ❌ 不许把新模块挂到其他 GameObject（必须由 GameApp 唯一管理）

---

## §6 UI Form 接口契约

`StartupSelectForm` 必须实现：

- `IUIForm`（Register / Unregister / GameObject）
- `IUIFormBootstrap`（Bootstrap(EventBus, ModuleRunner)）
- 在 `Awake` 中 `gameObject.SetActive(false)`
- 进入 GameState=CharacterSelect 完成后由 `CharacterSelectForm.OnNextClicked` 等触发 Open
- `OnConfirm` 按钮：发 `StartupSelectedEvent` 后关闭自身（**不直接切换 GameState**，由 CombatModule/GameStateModule 处理）

Prefab 路径冻结：`Assets/Resources/Prefab/UI/StartupSelect.prefab`（fan-out 阶段美术建，本阶段不创建）

---

## §7 命名 / 文件位置冻结

| 类 | 路径 | namespace |
|---|---|---|
| `AttackSystemEvents` | `Assets/Scripts/Events/AttackSystemEvents.cs` | 顶层（与 TattooEvents 同样无 namespace 或参照原文件） |
| `StatusEffectModule` | `Assets/Scripts/Modules/Status/StatusEffectModule.cs` | 顶层（与其他 Module 一致） |
| `PlayerDamageReceiver` | `Assets/Scripts/Modules/Combat/PlayerDamageReceiver.cs` | 顶层 |
| `SkillHitResolver` | `Assets/Scripts/Modules/Skill/SkillHitResolver.cs` | 顶层 |
| `StartupSelectForm` | `Assets/Scripts/Modules/UI/StartupSelectForm.cs` | `Tattoo.UI` |
| `WeaponTraitConfig` | `Assets/Scripts/DataTable/WeaponTraitConfig.cs` | 顶层 |
| `PlayerWeaponMounter` | `Assets/Scripts/Modules/Weapon/PlayerWeaponMounter.cs`（fan-out 阶段建） | 顶层 |

---

## §8 责任分界

| Agent | 可改 | 不可改 |
|---|---|---|
| A: StatusEffectModule 实装 | StatusEffectModule.cs 方法体 | 公共方法签名 / 事件定义 |
| B: PlayerDamageReceiver 实装 | PlayerDamageReceiver.cs 方法体 | 公共方法签名 |
| C: SkillHitResolver 实装 | SkillHitResolver.cs 方法体 | 公共方法签名 |
| D: HumanPlayerController 改造 | GetAimTarget / ShouldChargedAttack | IPlayerController 接口 |
| E: CombatModule + HeadPart 改造 | 内部逻辑 | TattooModule.Fire / Equip 公共 API |
| F: StartupSelectForm + Spawner | UI 行为 / Spawner StartupSelectedEvent 处理 | IUIForm 接口 / EntityRef 字段 |
| G: PlayerWeaponMounter | 新建 MonoBehaviour | WeaponModule 公共 API |
| H: gd-system trait 数值 | WeaponTraitConfig.json 数值 | fields[] 顺序 / TraitId 命名约定 |

---

**违反 CONTRACT 一律由 main 主对话立即回滚 + 重新派单。**
