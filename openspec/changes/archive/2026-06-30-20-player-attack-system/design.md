# Design — 20-player-attack-system

## 1. 设计共识（来自 brainstorm.md §7 grill 收敛冻结）

| 决策点 | 选择 | 理由 / 后果 |
|---|---|---|
| **Q1 暴击归属** | C：Head 刺青触发 | 删除 `CombatModule.cs:138` 25% 硬编码；`HeadPartBehavior` 改造为 CritHitEvent 源（按 Pattern 概率重发）。玩家不装 Head → 没暴击；装 Head + 火 + ProbBurst → 概率暴伤。**保留 6 部位 trigger 对称性**。 |
| **Q2 蓄力机制** | A：本期开放 | `InputModule.IsAttackHolding()` + `GetAttackHoldDuration()`；`HumanPlayerController.ShouldChargedAttack` 接入。蓄力 ≥ 0.4s 视为 charged，调 `WeaponModule.FireCharged`。 |
| **Q3 武器属性边界** | B：精化版 | 武器 = 基础数值 + 普攻 trait × 1 + 蓄力 trait × 1（5 武器 × 2 = 10 个 trait，新表 `WeaponTraitConfig`）。元素中性（不加 Element 字段）。 |
| **Q3.2 元素归属** | A：武器中性 | 元素完全由 ColorParam（右臂刺青）决定；保证"颜料决定元素"的 build 心流。 |
| **Q4 射击判定** | B：半角可配 | SphereCast 半角由 `WeaponConfig.AimSpreadHalfDeg` 控制：0° = Raycast 严格 / 180° = 自动锁定最近敌（向后兼容）/ 中间值 = 锥形容错。 |
| **Q5 D4 DoT tick** | A：新建 StatusEffectModule | 独立模块、0.5s tick；本期最小集 Burn/Poison。未来扩展寒冻/麻痹/护盾/标记。 |
| **Q 公式骨架** | A：保持 6 层乘区 | TattooModule.Fire 不改公式；现有 336 TC 必须继续 PASS。 |
| **R1 起手 build** | C：扩展版 StartupSelectForm | 颜料三选一 + 武器五选一 + 图案二选一（图案候选 = `SaveData.PatternUnlocks`）。 |
| **动画范式** | 元气骑士模式 | 玩家通用 1 套挥砍动画 + 武器自带 prefab 动画（`WeaponConfig.WeaponPrefabPath`） |
| **#18 重定义** | 拆分 | 原 weapon-select-flow（5 选 1 UI）拆入 #20；新 #18 = 战斗中拾取替换 |
| **Meta 解锁** | 本期只读不写 | `SaveData.PatternUnlocks` 读取；写入触发推 #21 meta-progression |

## 2. 架构图

```
                            GameApp.Start
                                  │
            ┌─────────────────────┼────────────────────────────┐
            │                     │                            │
       Cat 0 基础              Cat 1 服务                  Cat 3 项目
            │                     │                            │
    InputModule           StatusEffectModule★(新)       WeaponModule
    DataTableModule         (tick Burn/Poison)          SkillModule (扩展 SkillHitResolver★)
    ResourceModule          TattooModule                CombatModule (改造)
                                                       SpawnerModule (改造)
                                                       PlayerDamageReceiver★(新)

                            ┌─────────────────────┐
   事件总线（EventBus）：     │  AttackSystemEvents │  ← 新文件，集中本 change 新事件
                            └─────────────────────┘
                                      │
   ┌────────────┬─────────────┬─────────┴────────┬──────────────┬───────────┐
   ▼            ▼             ▼                  ▼              ▼           ▼
StartupSelectedEvent  StatusEffectAppliedEvent  PlayerHealthChangedEvent  ... (见 CONTRACT.md §1)
```

**新建文件清单（B 公共骨架阶段）**：

| 路径 | 类型 | 行数预估 |
|---|---|---|
| `Assets/Scripts/Events/AttackSystemEvents.cs` | 事件总表 | ~80 |
| `Assets/Scripts/Modules/Status/StatusEffectModule.cs` | IGameModule + ITickable | ~150 |
| `Assets/Scripts/Modules/Combat/PlayerDamageReceiver.cs` | IGameModule | ~80 |
| `Assets/Scripts/Modules/Skill/SkillHitResolver.cs` | IGameModule（独立，便于单测） | ~60 |
| `Assets/Scripts/Modules/UI/StartupSelectForm.cs` | MonoBehaviour + IUIForm + IUIFormBootstrap | ~200 |
| `Assets/Scripts/DataTable/WeaponTraitConfig.cs` | IDataTable | ~50 |
| `Assets/Resources/DataTable/WeaponTraitConfig.json` | DataTable JSON | 10 行 |

**改造文件清单**：

| 路径 | 改动 |
|---|---|
| `Assets/Scripts/Events/TattooEvents.cs` | 不动（事件签名锁定，新事件全进 AttackSystemEvents.cs） |
| `Assets/Scripts/Modules/Combat/CombatModule.cs` | 删 L138 暴击硬编码；ShouldAttack 路径改调 WeaponModule.FireWeapon |
| `Assets/Scripts/Modules/Combat/HumanPlayerController.cs` | GetAimTarget 重写；ShouldChargedAttack 接入 |
| `Assets/Scripts/Modules/Input/InputModule.cs` | + IsAttackHolding / GetAttackHoldDuration |
| `Assets/Scripts/Modules/Tattoo/TattooModule.cs` | +[EventHandler] OnWeaponAttackHit/OnWeaponChargedAttack 桥接 |
| `Assets/Scripts/Modules/Tattoo/Strategies/Parts/HeadPartBehavior.cs` | 按 Pattern 概率发 CritHitEvent（取代 CombatModule 25% 硬编码） |
| `Assets/Scripts/Modules/Spawner/SpawnerModule.cs` | 起手硬编码 init 移除，监听 StartupSelectedEvent |
| `Assets/Scripts/DataTable/WeaponConfig.cs` | +4 字段（AimSpreadHalfDeg/NormalTraitId/ChargedTraitId/WeaponPrefabPath） |
| `Assets/Resources/DataTable/WeaponConfig.json` | 5 行各加 4 字段 + 字段元数据 |
| `Assets/Resources/DataTable/UIFormConfig.json` | +1 行 StartupSelectForm（Id=12） |
| `Assets/Scripts/Core/GameApp.cs` | 注册 3 个新模块 |

## 3. 事件流（一次完整左键攻击）

```
鼠标左键按下
    │
    ▼
InputModule.IsAttackPressed() / IsAttackHolding()
    │
    ▼
CombatModule.OnUpdate → HumanPlayerController.ShouldAttack/ShouldChargedAttack
    │
    ▼
CombatModule 调 WeaponModule.FireWeapon(actor, aimTarget, isCharged, chargeRatio)
    │            ↑ aimTarget 由 HumanPlayerController.GetAimTarget（鼠标地面投影 + SphereCast）
    │
    ▼
WeaponModule（近战推 HitboxJob / 远程直接命中）→ 发 WeaponAttackHitEvent / WeaponChargedAttackEvent
    │
    ▼
TattooModule.OnWeaponAttackHit  ↘
TattooModule.OnWeaponChargedAttack → Fire(AttackHitEvent) 内部已有逻辑
    │                              ↑ 6 槽匹配 trigger，右臂被触发
    ▼
Fire 内部对右臂槽：
  - Shape.Apply(ctx, element, magnitude) → target.Health -= dmg
  - Element.OnHitExtra → target.Statuses.Add("Burn:dps:duration") ★ 加 status
    │
    ▼
StatusEffectModule.ApplyStatus(target, "Burn", dps, duration) ★ 新模块接住
    │                                                              ↑ 通过 [EventHandler] OnEffectApplied 监听 EffectAppliedEvent
    │                                                                解析 Result.Status 字符串 → 调 ApplyStatus
    │
    ▼ （每 0.5s tick）
StatusEffectModule.OnUpdate → 遍历每个 target 的 ActiveStatus → target.Health -= dps × 0.5
    │
    ▼ 发布
StatusEffectTickedEvent（target, damage, statusName）  → CombatHUDForm 浮动数字 / VFX 燃烧粒子
    │
    ▼ 持续到 duration 耗尽
StatusEffectExpiredEvent → 清除 status tag
```

**Head 暴击链**：

```
玩家装 Head + 色 + 图案 → TattooModule.Equip
                              │
            （正常 D1 命中走 AttackHitEvent，**不发 CritHitEvent**）
            （特殊：装备 Head 槽后，HeadPartBehavior 在某些时机按概率主动发 CritHitEvent）
                              │
                              ▼
HeadPartBehavior.TryRollCrit(ctx)：
  - 触发时机：[EventHandler] OnWeaponAttackHit （在 TattooModule 之前订阅）
  - 概率 = ColorParam × PatternParam（具体数值 gd-system 平衡，本期占位 = 0.25）
  - 若中：发 CritHitEvent (target, baseDamage * critMul)  ← critMul 由 PatternParam 决定
  - TattooModule.OnCritHit → Fire(CritHitEvent) 走 Head 槽
                              │
                              ▼
              Head 槽内策略叠加暴伤数值（已有 ContributePassive 逻辑）
```

**D7 技能链**：

```
玩家按 E/Q
   │
   ▼
SkillModule（已有逻辑）：进入 Startup → Active 第 1 帧发 SkillActivatedEvent
   │
   ▼
SkillHitResolver.OnSkillActivated  ★ 新模块
   │  - 读 SkillConfig.DamageMul × WeaponModule.GetBaseDamage(caster)
   │  - 按 SkillConfig.HitShape 计算命中目标列表（single/circle/line/cone）
   │  - 对每个目标发 AttackHitEvent
   ▼
TattooModule.OnAttackHit → Fire（已有逻辑） → 走刺青链
```

**D8 受击链**：

```
EnemyAIController → EnemyModule 发 EnemyAttackEvent(attacker, damage)
   │
   ▼
PlayerDamageReceiver.OnEnemyAttack  ★ 新模块
   │  - 当前 HP -= damage
   │  - 发 DamagedEvent(attacker, damage, newHp, maxHp) → CombatHUDForm 红边/血条
   │  - 发 PlayerHealthChangedEvent(current, max, delta)
   │  - HP <= 0 时发 PlayerDiedEvent → CombatModule.EndCombat(false)
```

## 4. 关键设计点

### 4.1 元气骑士动画范式（重要）

**问题**：5 把武器 × 4 方向 × N 动作 = N 倍美术成本。

**解法**（来自 grill §7.2）：

```
玩家 GameObject
├─ Player1 Animator（仅有 Idle/Walk/Death + 一个通用 GenericAttack 状态）
└─ WeaponHandPoint（空挂点）
   └─ <动态生成> WeaponPrefab（如 Knife_Basic.prefab）
       ├─ SpriteRenderer
       ├─ Animator（武器自有 Idle/Swing/Charge 状态机）
       └─ Optional 武器 trail particle
```

- 玩家攻击时 → 玩家 Animator 进 GenericAttack（仅躯干轻微挥动）+ 武器 Animator 进 Swing（武器自身完整动作）
- 切武器 → 销毁旧 WeaponPrefab 实例 → 实例化新 `Resources.Load<GameObject>(WeaponPrefabPath)`
- 本期：WeaponPrefab 占位（空 GameObject + SpriteRenderer 即可，真实动画留 #19）

**接线**：
- `SpawnerModule` 在 `StartupSelectedEvent` 后 → 调 `WeaponModule.EquipWeapon(actor, weaponId)`
- `WeaponModule.EquipWeapon` → 发 `WeaponEquippedEvent`（新事件，载 prefabPath）
- `PlayerWeaponMounter`（新 MonoBehaviour，挂在玩家身上）→ 订阅 `WeaponEquippedEvent` → 卸旧装新

> **本期范围**：`PlayerWeaponMounter` 的 prefab 加载/挂载逻辑由 client-unity 在 fan-out 阶段实现；骨架阶段仅在 `WeaponEquippedEvent` 定义事件签名 + 在 `SpawnerModule` 改造里加 TODO。

### 4.2 DoT 数据结构

```csharp
// 在 StatusEffectModule.cs 内
public struct ActiveStatus
{
    public string Name;        // "Burn" / "Poison"
    public float  DPS;         // 每秒伤害
    public float  RemainingSec;// 剩余持续时间
    public Target Source;      // 谁加的（暴击放大用，本期保留字段）
}

readonly Dictionary<Target, List<ActiveStatus>> _active = new();  // 50 actor × ≤3 status 预算
const float TickInterval = 0.5f;
float _accum;
```

- `OnUpdate(dt)`：`_accum += dt`，到 `TickInterval` 时遍历每个 actor 的 status list → 扣血 + 发 `StatusEffectTickedEvent` + 减 remaining
- `ApplyStatus`：同名 status 取**较高 DPS / 较长 duration**（不叠加 stack 数，本期最小化）
- `StatusEffectAppliedEvent` 在每次 ApplyStatus 时发；`StatusEffectExpiredEvent` 在 remaining ≤ 0 时发

**EffectAppliedEvent → ApplyStatus 桥接**：

```csharp
// StatusEffectModule.cs
[EventHandler]
void OnEffectApplied(EffectAppliedEvent e)
{
    foreach (var r in e.Results)
    {
        // r.Status 形如 "Burn(dps=8,dur=3)"，由 FireElementBehavior.OnHitExtra 填入
        if (string.IsNullOrEmpty(r.Status)) continue;
        if (TryParseStatus(r.Status, out var name, out var dps, out var dur))
        {
            ApplyStatus(GetTargetByPart(r), name, dps, dur);  // TODO change#20: 解析目标
        }
    }
}
```

### 4.3 Trait 接口

本期 trait 不实装具体效果，只定义"trait_id → IWeaponTraitBehavior"映射占位：

```csharp
// Assets/Scripts/Modules/Weapon/Traits/IWeaponTraitBehavior.cs   ★ 留给 fan-out 阶段
public interface IWeaponTraitBehavior
{
    string TraitId { get; }
    void OnAttack(WeaponContext ctx);     // 普攻时调
    void OnCharged(WeaponContext ctx);    // 蓄力时调
}
```

骨架阶段 **不创建 IWeaponTraitBehavior 文件**——只在 `WeaponTraitConfig.json` 占位 10 行配置 + `EffectType` 枚举字段串。fan-out 阶段 client-unity / gd-system 共同决定要不要新建 TraitModule。

### 4.4 解锁表 schema（meta）

本期**只读不写**，沿用现有 `SaveData.PatternUnlocks: Dictionary<string, bool[]>`：

- key = patternId（如 "Line" / "Ring"）；value = `bool[6]` （颜色解锁位，6 个颜色对应 6 bits）
- 本期 StartupSelectForm 候选 = **`PatternUnlocks` 中至少有一位 true 的 patternId**
- 默认 SaveData 自带 `Line` + `Ring` 全色解锁（在 SaveMigrator 或 SaveData 默认构造里设；本期不动 SaveModule，留 #21 meta-progression 触发器）

骨架阶段 StartupSelectForm 中 candidatePatternIds 用如下兜底：

```csharp
// StartupSelectForm.cs
List<int> GetUnlockedPatternIds()
{
    var saveData = _runner.GetModule<SaveModule>().Data;
    var result = new List<int>();
    // TODO change#20: 解析 saveData.PatternUnlocks 反查 patternId → int Id
    // 兜底：默认 [1, 2]（Line + Ring）
    if (result.Count == 0) result.AddRange(new[] { 1, 2 });
    return result;
}
```

### 4.5 鼠标地面投影 + SphereCast

```csharp
// HumanPlayerController.GetAimTarget （改造）
public Target GetAimTarget()
{
    var cfg = GetCurrentWeaponConfig();  // WeaponModule.GetEquippedWeapon(OwnerActor).Weapon
    float halfDeg = cfg?.AimSpreadHalfDeg ?? 180f;

    // 半角 = 180° → 回退到原"自动锁定最近敌"行为，保证向下兼容
    if (halfDeg >= 179f) return FindClosestEnemy();

    // 1. 鼠标屏幕坐标 → 相机射线 → Plane(Vector3.up, 0) 交点（地面投影）
    var cam = Camera.main;
    if (cam == null) return null;
    var ray = cam.ScreenPointToRay(Input.mousePosition);
    if (!new Plane(Vector3.up, 0).Raycast(ray, out float dist)) return null;
    Vector3 aimPoint = ray.GetPoint(dist);

    // 2. 玩家位置 → aimPoint 的方向 = 攻击方向
    Vector3 origin = _spawner.Player.transform.position;
    Vector3 dir = (aimPoint - origin); dir.y = 0;
    if (dir.sqrMagnitude < 0.01f) return null;
    dir.Normalize();

    // 3. halfDeg=0 → Raycast / halfDeg>0 → SphereCast（半径按 dir 距离 + tanθ 估算）
    if (halfDeg < 0.5f)
    {
        if (Physics.Raycast(origin + Vector3.up * 0.5f, dir, out var hit,
            cfg.Range, LayerMask.GetMask("Enemy")))
            return hit.collider.GetComponent<EntityRef>()?.Target;
        return null;
    }
    else
    {
        float radius = cfg.Range * Mathf.Tan(halfDeg * Mathf.Deg2Rad);
        if (Physics.SphereCast(origin + Vector3.up * 0.5f, radius, dir, out var hit,
            cfg.Range, LayerMask.GetMask("Enemy")))
            return hit.collider.GetComponent<EntityRef>()?.Target;
        return null;
    }
}
```

骨架阶段：在 `HumanPlayerController.cs` 加 `// TODO change#20: 鼠标地面投影 + SphereCast` 注释占位，保留原 FindClosest 实现以确保编译通过。

### 4.6 暴击改造（Head 替代 25% 硬编码）

**改前**（CombatModule.cs L132-142）：

```csharp
if (c.ShouldAttack())
{
    var t = c.GetAimTarget();
    if (t != null)
    {
        bool crit = UnityEngine.Random.value < 0.25f;
        if (crit) _bus.Publish(new CritHitEvent(t, _tattoo.Stats.WeaponDamage));
        else      _bus.Publish(new AttackHitEvent(t, _tattoo.Stats.WeaponDamage));
    }
}
```

**改后**：

```csharp
if (c.ShouldAttack())
{
    var t = c.GetAimTarget();
    if (t != null)
    {
        // 暴击改由 HeadPartBehavior 在 OnWeaponAttackHit 内按概率发；CombatModule 只发 FireWeapon
        _weapon.FireWeapon(c.OwnerActor, t, isCharged: false);
    }
}
```

骨架阶段：仅在 CombatModule 改造点加 TODO 注释，**不实际删除** L138 暴击代码（保持现状可编译通过）。

## 5. Agent 编排（AGENTS.md 模式 5 骨架先行 + 模式 1 Fan-Out）

```
主对话（orchestrator）
├─ 阶段 1（client-lead 本人）：写 openspec artifact + 公共骨架代码  ← 本文
│      产出：5 文档 + 7 新文件骨架 + 11 处改造 TODO 占位
│      门禁：编译通过 + 现有 336 TC PASS（不实际跑，骨架不动逻辑）
│
├─ 阶段 2（Fan-Out 模式 1，6+ agent 并行实现）：
│   ├─ A: client-unity 实装 StatusEffectModule（解析 Status 串 + tick）
│   ├─ B: client-unity 实装 PlayerDamageReceiver
│   ├─ C: client-unity 实装 SkillHitResolver
│   ├─ D: client-unity 实装 HumanPlayerController.GetAimTarget 鼠标地面投影 + SphereCast
│   ├─ E: client-unity 实装 InputModule 蓄力 API + CombatModule 改造 + HeadPartBehavior 改造
│   ├─ F: client-unity 实装 StartupSelectForm + SpawnerModule 接 StartupSelectedEvent
│   ├─ G: client-unity 实装 PlayerWeaponMounter MonoBehaviour
│   └─ H: gd-system 平衡 10 个 WeaponTraitConfig 占位数值
│   await UniTask.WhenAll
│
├─ 阶段 3（Fan-Out 模式 1，N 个 agent 出图）：
│   ├─ 5 武器 prefab 帧序列（4 帧 × 5 武器 = 20 张，每张一个 codex-image-gen 任务）
│   ├─ StartupSelectForm 颜料卡片 × 3
│   ├─ StartupSelectForm 武器卡片 × 5
│   ├─ 起手图案卡片 × 2
│   └─ 准星 decal × 1
│   产出：~31 张图 + UI prefab + AnimatorController（用 #17 工具复用）
│
├─ 阶段 4（qa-engineer 主导 playtest loop ≤5 轮）：
│   ├─ TC-D1～D9 全部 PASS
│   ├─ TC-Crit / TC-Charge / TC-Mouse / TC-Startup PASS
│   ├─ TC-Pickup 仅占位记录
│   └─ 现有 336 TC PASS
│
└─ 阶段 5（主对话 / 用户）：openspec archive-change 20-player-attack-system
```

## 6. 边界与失败安全网

- **骨架阶段（本阶段）不允许任何方法实装内部逻辑**——全部 `// TODO change#20: <说明>` 占位
- **CONTRACT.md 是 fan-out 阶段宪法**：事件签名 / 模块依赖 / DataTable schema / GameApp 注册 不允许动；动了主对话回滚
- **同一 bug 连续 5 轮未解** → loop 终止交回用户（与 #16/#17 标准一致）
- **codex-image-gen 同一图 3 轮重试仍不通过** → 阻塞通知用户人工介入
- **HeadPartBehavior 改造若破坏 ≥1 条 336 TC** → 立刻回退，HeadPartBehavior 保持原状，CombatModule 25% 暴击留作 fallback（标 TODO 留下期处理）
