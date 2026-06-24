# Design — 01-tattoo-framework-rewrite

> 详细实现规格。Phase A 完成后此文件作为「实施合同」，Phase B / C 期间禁止修改公共契约（事件签名 / 模块边界 / DataTable schema）。

---

## 一、模块拆分

```
GameApp (MonoBehaviour, 挂在 Launch.unity 唯一 GameObject)
   │
   └─→ ModuleRunner.StartAsync
         │
         ├─ Category 0  基础设施
         │   ├─ ConfigModule        (待建，可放在后续)
         │   ├─ DataTableModule     (已就位)
         │   └─ ResourceModule      (已就位)
         │
         ├─ Category 1  系统服务
         │   └─ TattooModule         ← 本次新增
         │
         ├─ Category 2  应用协调
         │   ├─ InputModule          (已就位)
         │   ├─ SceneModule          (已就位)
         │   └─ UIModule             (已就位)
         │
         ├─ Category 3  项目扩展
         │   ├─ SpawnerModule        ← 本次新增
         │   └─ CombatModule         ← 本次新增
         │
         └─ Category 4  辅助系统
             └─ (空)
```

### Dependencies 关系

| Module | Dependencies |
|---|---|
| `TattooModule`   | `DataTableModule` |
| `SpawnerModule`  | `ResourceModule`, `SceneModule` |
| `CombatModule`   | `TattooModule`, `SpawnerModule`, `InputModule` |
| UI 表单（属 UIModule 管理） | 通过 `EventBus` 与 `CombatModule` 解耦 |

---

## 二、事件总表（`Assets/Scripts/Events/TattooEvents.cs`）

**这是全局契约**——Phase B / C 期间不得修改签名。

```csharp
namespace Tattoo.Events
{
    // 战斗触发事件（替代原 enum GameEvent）
    public class AttackHitEvent      { public Target Target; public float BaseDamage; }
    public class CritHitEvent        { public Target Target; public float BaseDamage; }
    public class DamagedEvent        { public Target Attacker; public float Damage; }
    public class SkillCastEvent      { public string SkillId; }
    public class DodgePressedEvent   { }
    public class MoveTickEvent       { public Target[] Path; public float Distance; }

    // 战斗结果事件
    public class EffectAppliedEvent  { public EffectResult Result; }
    public class TargetKilledEvent   { public Target Target; }
    public class PlayerDiedEvent     { }

    // Build / 装备事件
    public class BuildChangedEvent   { public IReadOnlyList<TattooSlot> Equipped; }
    public class PassiveRecomputedEvent { public PassiveStats Stats; }

    // 阶段事件
    public class GameReadyEvent      { }                  // ModuleRunner 全部就绪
    public class CombatStartedEvent  { public int EnemyCount; }
    public class CombatEndedEvent    { public bool PlayerWin; }
}
```

> **数据类**（`Target` / `EffectResult` / `TattooSlot` / `PassiveStats`）放在 `Assets/Scripts/Modules/Tattoo/Data/`，由 TattooModule 拥有。

---

## 三、DataTable schema（JSON 直写）

**流程**：用户直接在 `Assets/Resources/DataTable/<Name>.json` 编辑 JSON → Unity 菜单 `Tools/DataTable/生成全部配置表代码`（[DataTableGenerator.cs](../../../Assets/Scripts/Modules/DataTable/Editor/DataTableGenerator.cs) 的 `GenerateAll` 入口）→ 自动产出 `Assets/Scripts/DataTable/<Name>.cs`（含 `<Name>Row` 类 + `<Name> : IDataTable` 类）+ 更新 `DataTableRegistry.cs`。

**JSON schema 公共结构**：

```json
{
  "table": "<Name>",
  "fields": [
    { "name": "Id",   "type": "int",    "desc": "主键，必须为第一个字段" },
    { "name": "...",  "type": "...",    "desc": "..." }
  ],
  "rows": [
    { "Id": 1, "...": "..." }
  ]
}
```

**支持类型**（[DataTableGenerator.cs#L14-L43](../../../Assets/Scripts/Modules/DataTable/Editor/DataTableGenerator.cs#L14-L43)）：`int / float / double / string / bool`、任意维数组（`T[]`、`T[][]`...）、`Dictionary<K, V>`（K 必须基础类型）。

**约束**：`fields[0]` 必须是 `{ "name": "Id", "type": "int" }`；`table` 字段值必须与文件名一致。

### 3.1 `tattoo_part.json`（BodyPart 配置）

| Field | Type | desc |
|---|---|---|
| Id | int | 主键 1-6 |
| Name | string | "Head" / "Torso" / "LeftArm" / "RightArm" / "LeftLeg" / "RightLeg" |
| TriggerEvent | string | 对应事件类名（AttackHitEvent / CritHitEvent / DamagedEvent / SkillCastEvent / DodgePressedEvent / MoveTickEvent） |
| ScaleStat | string | StatType enum 名 |
| SymmetryGroup | string | None / Arms / Legs |
| ScaleFactor | float | 缺省由 PartType 默认值 |
| PassiveDimension | string | 部位 passive 维度的标签 |

### 3.2 `tattoo_color.json`（ColorSO 配置）

| Field | Type |
|---|---|
| Id | int |
| Name | string |
| Element | string | Fire / Lightning / Nature / Frost / Mutation / Holy / Pure |
| ColorMultiplier | float |

### 3.3 `tattoo_pattern.json`（PatternSO 配置）

| Field | Type |
|---|---|
| Id | int |
| Name | string |
| Shape | string | SingleHit / AOEBurst / StackingMark / MultiHit / ChainJump / ProbBurst / TrailZone / SummonForm |
| PatternMultiplier | float |

### 3.4 `tattoo_element.json`（元素行为参数）

| Field | Type | desc |
|---|---|---|
| Id | int |  |
| Name | string | 对应 Element |
| BaseMultiplier | float | 元素基础倍率 |
| StatusName | string | 触发的状态名（Burn / Shock / Poison / Freeze 等） |
| StatusDuration | float | |

### 3.5 `tattoo_shape.json`（形状行为参数）

| Field | Type | desc |
|---|---|---|
| Id | int |
| Name | string | 对应 Shape |
| TargetMode | string | Single / NearbyN / Chain / All |
| HitCount | int |
| ProbabilityCurve | string | JSON 数组字符串，可选 |

---

## 四、TattooModule 设计

```csharp
namespace Tattoo
{
    public sealed class TattooModule : IGameModule
    {
        public int ModuleCategory => 1;
        public Type[] Dependencies => new[] { typeof(DataTableModule) };

        readonly ModuleRunner _runner;
        readonly EventBus _bus;
        TattooPartConfig _parts;
        TattooColorConfig _colors;
        TattooPatternConfig _patterns;
        TattooElementConfig _elements;
        TattooShapeConfig _shapes;

        readonly Dictionary<string, IElementBehavior> _elementBehaviors = new();
        readonly Dictionary<string, IShapeBehavior> _shapeBehaviors = new();

        // 玩家状态（替代原 PlayerSelf）
        public PlayerState Player { get; private set; } = new();

        // 当前 Build
        readonly List<TattooSlot> _equipped = new();
        public IReadOnlyList<TattooSlot> Equipped => _equipped;

        public TattooModule(ModuleRunner runner, EventBus bus)
        {
            _runner = runner; _bus = bus;
        }

        public UniTask InitializeAsync(CancellationToken ct) {
            var dt = _runner.GetModule<DataTableModule>();
            _parts    = dt.GetTable<TattooPartConfig>();
            _colors   = dt.GetTable<TattooColorConfig>();
            _patterns = dt.GetTable<TattooPatternConfig>();
            _elements = dt.GetTable<TattooElementConfig>();
            _shapes   = dt.GetTable<TattooShapeConfig>();
            RegisterStrategies();
            return UniTask.CompletedTask;
        }

        public UniTask ShutdownAsync(CancellationToken ct) { ... }

        // 装备操作
        public void Equip(int partId, int colorId, int patternId) { ... _bus.Publish(new BuildChangedEvent { ... }); }
        public void Clear() { ... }

        // 事件处理：6 个战斗触发事件
        [EventHandler] void OnAttackHit(AttackHitEvent e) { Fire(typeof(AttackHitEvent), e); }
        [EventHandler] void OnCritHit(CritHitEvent e)    { Fire(typeof(CritHitEvent),   e); }
        [EventHandler] void OnDamaged(DamagedEvent e)    { Fire(typeof(DamagedEvent),   e); }
        [EventHandler] void OnSkillCast(SkillCastEvent e){ Fire(typeof(SkillCastEvent), e); }
        [EventHandler] void OnDodgePressed(DodgePressedEvent e) { Fire(typeof(DodgePressedEvent), e); }
        [EventHandler] void OnMoveTick(MoveTickEvent e)  { Fire(typeof(MoveTickEvent),  e); }

        void Fire(Type eventType, object payload) {
            // 在 _equipped 中找出 TriggerEvent == eventType 的 slot，触发对应 strategy
            // 把结果通过 EventBus.Publish(new EffectAppliedEvent { ... }) 广播
        }

        void RegisterStrategies() {
            // 21 个 SO 子类的行为部分迁移到这里
            // _elementBehaviors["Fire"]  = new FireElementBehavior();
            // _shapeBehaviors["SingleHit"] = new SingleHitShapeBehavior();
            // 等等
        }
    }
}
```

### 策略接口

```csharp
public interface IElementBehavior {
    void Apply(EffectContext ctx, ElementConfigRow cfg);
}

public interface IShapeBehavior {
    void Apply(EffectContext ctx, ShapeConfigRow cfg);
}
```

具体策略类放在 `Assets/Scripts/Modules/Tattoo/Strategies/`。

---

## 五、CombatModule 设计

```csharp
public sealed class CombatModule : IGameModule
{
    public int ModuleCategory => 3;
    public Type[] Dependencies => new[] {
        typeof(TattooModule), typeof(SpawnerModule), typeof(InputModule)
    };

    // 战斗主循环（参考自 Combat/CombatRunner.cs）
    [EventHandler] void OnCombatStarted(CombatStartedEvent e) { ... }
    [EventHandler] void OnTargetKilled(TargetKilledEvent e)   { ... }
    [EventHandler] void OnPlayerDied(PlayerDiedEvent e)       { ... }

    // 监听 InputModule 的输入事件（如 InputModule 已有 InputActionEvent）
    [EventHandler] void OnAttackInput(InputAttackEvent e) {
        // 找最近目标 → 计算伤害 → 发 AttackHitEvent / CritHitEvent
    }
}
```

---

## 六、SpawnerModule 设计

```csharp
public sealed class SpawnerModule : IGameModule
{
    public int ModuleCategory => 3;
    public Type[] Dependencies => new[] {
        typeof(ResourceModule), typeof(SceneModule)
    };

    public Player Player { get; private set; }
    public IReadOnlyList<Enemy> Enemies => _enemies;

    public UniTask InitializeAsync(CancellationToken ct) {
        // 通过 ResourceModule.Load 加载 Player/Enemy prefab
        // 创建相机、灯光、地面
        // _bus.Publish(new GameReadyEvent());
        return UniTask.CompletedTask;
    }
}
```

> **资源依赖**：`Player.prefab` / `Enemy.prefab` 需要在 `Assets/Resources/Prefab/` 下手工创建（CLAUDE.md "正确顺序：用户先定义 Prefab → 工具生成 → 再写脚本"），AI 不创建 prefab 资源。

---

## 七、UI Toolkit 设计

| 文件 | 内容 |
|---|---|
| `Assets/UI/CombatHUD.uxml` | 装备面板 / Build 列表 / 战斗日志 / 玩家状态 |
| `Assets/UI/CombatHUD.uss`  | 样式（暗色科技风） |
| `Assets/Scripts/Modules/Tattoo/UI/CombatHUDForm.cs` | UIModule 下注册的表单类 |

UI 与 CombatModule 通过 EventBus 通信：
- UI 订阅 `BuildChangedEvent` 重绘装备列表
- UI 订阅 `EffectAppliedEvent` 追加日志
- UI 按钮 `OnEquipClicked` → 发 `TattooEquipRequest` 给 TattooModule

---

## 八、输入接 InputModule

`InputModule` 已存在。检查它的现有 API：
- 若已有 `OnKeyDown(KeyCode)` / `OnMouseClick(Vector2)` 等事件 → 直接订阅
- 若缺：新增 6 个高层输入事件
  - `InputAttackEvent` (鼠标左键)
  - `InputSkillEvent`  (E 键)
  - `InputDodgeEvent`  (空格)
  - `InputMoveEvent`   (WASD)

→ Phase B 第一步必须确认 InputModule 现有签名，避免 client-unity 重复造轮子。

---

## 九、Launch.unity 场景

```
Launch.unity (Scene)
└── @Game (GameObject)
    └── GameApp.cs (MonoBehaviour)
        - AddModule<DataTableModule>()
        - AddModule<ResourceModule>()
        - AddModule<InputModule>()
        - AddModule<SceneModule>()
        - AddModule<UIModule>()
        - AddModule<TattooModule>()
        - AddModule<SpawnerModule>()
        - AddModule<CombatModule>()
        - StartAsync()
```

⚠️ 用户需在 Unity 中手动 File → New Scene → 保存为 Launch.unity，然后建一个 `@Game` GameObject 挂 `GameApp`。AI 不创建 .unity 文件。

---

## 十、测试策略

| Phase | 测试 |
|---|---|
| A | DataTable schema 加载测试（`DataTableModuleTests`：能读出 6 个部位 / 7 个颜色 / 8 个图案） |
| B | TattooModule 单元测试：Equip → Build 正确；Fire(AttackHitEvent) → 触发右臂 slot 正确 |
| B | EventBus 集成测试：多事件并发触发不漏不重 |
| C | CombatModule 集成测试：Spawner 生成 → Combat 触发输入 → Tattoo 解算 → UI 更新（用 uloop-run-tests） |
| C | Launch.unity 启动冒烟测试：所有模块 InitializeAsync 成功 |

---

## 十一、向后兼容

- `Assets/_Legacy~/Tattoo/` 整目录保留，**仅供参考**，Unity 不扫描
- 新代码命名与旧代码区分：旧 `TattooComposer` vs 新 `TattooModule`；不共享类型
- 旧场景已删，**不存在 missing reference 风险**

---

## 十二、TODO（Open Questions）

- [ ] **InputModule 现有 API**？需要 Phase B 第一步 grep + 决策
- [ ] **是否要 GameStateModule（已有）参与生命周期管理？** CombatStartedEvent / CombatEndedEvent 可能由 GameState 主导
- [ ] **Player.cs / Enemy.cs 是否保留为 MonoBehaviour？** 我倾向保留——它们是实体不是模块；但需要在 SpawnerModule 中通过 prefab 加载
- [ ] **资源 Prefab 谁来做？** SpawnerModule 需要 Player.prefab / Enemy.prefab，需要用户在 Unity 中手工创建后加入 ResourceConfig.json
