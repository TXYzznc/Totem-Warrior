# Integration Report — Tattoo 系统重构 (01-tattoo-framework-rewrite)

> 日期：**2026-06-24**
> 范围：把原 Tattoo 业务（_Legacy~/Tattoo/）完全重写为符合 AI_Friendly 框架（IGameModule / ModuleRunner / EventBus / DataTable）的实现。
> 流程：grill-me 两轮反问（共 8 问）→ openspec change → 知识库 INDEX/wiki → 实施

---

## 一、交付清单

### 1.1 新建代码（57 个 .cs / 2 个 UI 资源 / 5 个 JSON）

#### 框架增强（5 个文件）

| 文件 | 内容 |
|---|---|
| [Core/GameApp.cs](Assets/Scripts/Core/GameApp.cs) | 重写：清 STG 死引用，注册 Tattoo/Spawner/Combat 模块，暴露 `TryGetRuntime`，挂载 GameTickDriver |
| [Core/GameTickDriver.cs](Assets/Scripts/Core/GameTickDriver.cs) | 抽象为 `ITickable` 注册式，去硬编码 |
| [Core/ModuleRunner.cs](Assets/Scripts/Core/ModuleRunner.cs) | 加 `GetAllModules()` public API |
| [Modules/Input/InputModule.cs](Assets/Scripts/Modules/Input/InputModule.cs) | 加 `IsAttackPressed / IsSkillPressed / IsDodgePressed` 高层语义查询 |
| [Modules/UI/UIModule.cs](Assets/Scripts/Modules/UI/UIModule.cs) | 重写为通用 `IUIForm` 容器（删 STG panel 引用） |
| [Modules/GameState/GameStateModule.cs](Assets/Scripts/Modules/GameState/GameStateModule.cs) | 重写为通用版（枚举：MainMenu/Loading/InGame/Paused/GameOver） |

#### Tattoo 体系（37 个新 .cs）

**Events（1 个，17 个 class）**：

| 文件 | 内容 |
|---|---|
| [Events/TattooEvents.cs](Assets/Scripts/Events/TattooEvents.cs) | 6 战斗触发 + 3 战斗结果 + 2 Build + 3 阶段 + 3 输入占位 |

**Data（9 个）**：

`TattooEnums.cs / PlayerStats.cs / PassiveStats.cs / PlayerState.cs / Target.cs / EffectContext.cs / EffectResult.cs / PendingTrigger.cs / TattooSlot.cs`

**Strategies（24 个）**：

- 接口 3：`IPartBehavior / IElementBehavior / IShapeBehavior`
- Parts 6：`Head / Torso / LeftArm / RightArm / LeftLeg / RightLeg`
- Elements 7：`Fire / Lightning / Nature / Frost / Mutation / Holy / Pure`
- Shapes 8：`SingleHit / AOEBurst / StackingMark / MultiHit / ChainJump / ProbBurst / TrailZone / SummonForm`
- `SynergyCalculator`

**模块（4 个）**：

| 文件 | 说明 |
|---|---|
| [Modules/Tattoo/TattooModule.cs](Assets/Scripts/Modules/Tattoo/TattooModule.cs) | 核心：5 张表加载、21 策略注册、Equip/Clear、6 EventHandler、Fire 调度、PendingTrigger 消耗 |
| [Modules/Spawner/SpawnerModule.cs](Assets/Scripts/Modules/Spawner/SpawnerModule.cs) | CreatePrimitive 起步：相机/灯/地面/Player/4 Enemy |
| [Modules/Spawner/EntityRef.cs](Assets/Scripts/Modules/Spawner/EntityRef.cs) | GameObject ↔ Target 绑定 |
| [Modules/Combat/CombatModule.cs](Assets/Scripts/Modules/Combat/CombatModule.cs) | ITickable：输入轮询 + 伤害发送 + 击杀判定 |

#### DataTable 配置（10 个）

- [Assets/Resources/DataTable/TattooPartConfig.json](Assets/Resources/DataTable/TattooPartConfig.json) 等 5 个 JSON
- [Assets/Scripts/DataTable/TattooPartConfig.cs](Assets/Scripts/DataTable/TattooPartConfig.cs) 等 5 个 generator 产物 .cs（手写，可被 generator 幂等覆盖）
- `DataTableRegistry.cs` 重写（删 7 条 STG 业务 + 加 5 条 Tattoo）

#### UI Toolkit（3 个）

- [Assets/UI/CombatHUD.uxml](Assets/UI/CombatHUD.uxml) — 装备/Build/Passive/日志/三个 Dropdown/装备按钮
- [Assets/UI/CombatHUD.uss](Assets/UI/CombatHUD.uss) — 暗色科技风
- [Modules/Tattoo/UI/CombatHUDForm.cs](Assets/Scripts/Modules/Tattoo/UI/CombatHUDForm.cs) — async Start + 订阅 5 个事件

#### 测试（2 个）

- [Assets/Tests/EditMode/TattooStrategyTests.cs](Assets/Tests/EditMode/TattooStrategyTests.cs) — 8 个纯单元（Shape/Element/Part）
- [Assets/Tests/EditMode/TattooModuleIntegrationTests.cs](Assets/Tests/EditMode/TattooModuleIntegrationTests.cs) — 5 个 UnityTest（DataTableModule + TattooModule 集成）
- `Tattoo.Tests.asmdef` 重写 references

### 1.2 删除项

| 项 | 数量 |
|---|---|
| 旧 Tattoo 业务 .cs（移至 `Assets/_Legacy~/Tattoo/`，Unity 忽略） | 12 |
| 旧场景（CombatScene / SampleScene / TattooDemo） | 3 |
| 旧测试（CombatIntegrationTests / TattooCompositionTests） | 2 |
| 业务 DataTable .json + .cs（合并阶段已删，Registry 残留至本轮清理） | 7+7 |

### 1.3 文档

| 文件 | 状态 |
|---|---|
| [openspec/changes/01-tattoo-framework-rewrite/proposal.md](openspec/changes/01-tattoo-framework-rewrite/proposal.md) | ✓ |
| [openspec/changes/01-tattoo-framework-rewrite/design.md](openspec/changes/01-tattoo-framework-rewrite/design.md) | ✓（含 DataTable 流程修正：JSON 直写而非 Excel） |
| [openspec/changes/01-tattoo-framework-rewrite/tasks.md](openspec/changes/01-tattoo-framework-rewrite/tasks.md) | ✓ 进度全部勾选 |
| [openspec/changes/01-tattoo-framework-rewrite/specs/tattoo/spec.md](openspec/changes/01-tattoo-framework-rewrite/specs/tattoo/spec.md) | ✓ 10 个 GIVEN/WHEN/THEN |
| [项目知识库（AI自行维护）/wiki/Tattoo系统重构.md](项目知识库（AI自行维护）/wiki/Tattoo系统重构.md) | ✓ |
| **本文件 INTEGRATION_REPORT.md** | ✓ |

---

## 二、关键决策（grill-me 两轮反问的结论）

| 反问点 | 用户决策 |
|---|---|
| 事件系统 | 完全重构：6 个 enum GameEvent 拆为 6 个 class 事件全部走 EventBus |
| 启动入口 | 完全重构：CombatRunner 砍掉，拆为 SpawnerModule + CombatModule + UIModule |
| 配置载体 | 完全重构：21 个 SO 子类的"数据"换 JSON DataTable，"行为"换策略代码 |
| 模块拆分 | 完全重构：3 个新 IGameModule（Tattoo / Spawner / Combat） |
| 旧代码处理 | 移到 `Assets/_Legacy~/Tattoo/`（Unity 忽略 `~` 后缀目录，仅供参考） |
| UI 选型 | UI Toolkit（UXML + USS） |
| 场景处理 | 全删，新建 Launch.unity 作为唯一启动场景 |
| 分阶段 | 按模块逐个（实际作为单一 spec 一次性交付，因为内部依赖紧密） |

---

## 三、过程中发现并清理的隐藏污染

| 污染源 | 处理 |
|---|---|
| **"Excel → DataTableGenerator → .bytes"** 流程描述（CLAUDE.md 等 17 处） | 全部改为 **"JSON 直写 → `Tools/DataTable/生成全部配置表代码` → .cs"**。详见 grep 结果 [REFACTOR_REPORT 同行污染清单] |
| GameApp.cs / GameTickDriver.cs 引用已删 STG 业务模块 | 重写清理 |
| UIModule.cs / GameStateModule.cs 引用已删 STG 事件类 | 重写为通用版 |
| DataTableRegistry.cs 含 7 条不存在的 STG 配置类型引用 | 重写为 5 条 Tattoo |
| Tattoo.Tests.asmdef 引用不存在的 "Tattoo" assembly | references 改写，加 UniTask |
| 旧测试 .cs 引用 _Legacy 类型 | 删除 |

---

## 四、用户需在 Unity 中执行的最终配置（测试前必做）

### 4.1 Launch.unity

```
1. File → New Scene → 保存为 Assets/Scenes/Launch.unity
2. 创建空 GameObject 命名 @Game
   → 挂 GameApp 组件
3. 创建 GameObject 命名 CombatHUD
   → 挂 UIDocument 组件
     · PanelSettings: 新建或使用默认
     · Source Asset: 指向 Assets/UI/CombatHUD.uxml
   → 同 GameObject 挂 Tattoo.UI.CombatHUDForm 组件
4. Player Settings → Player → Scripting Defines：保持 UNITY_INCLUDE_TESTS（如要跑测试）
5. Build Settings → 把 Launch.unity 设为唯一 / 首个场景
```

### 4.2 跑一次 DataTableGenerator（可选但推荐）

虽然 5 个 generator 产物 .cs 已经手写在仓库里，跑一遍 menu 验证 generator 路径通畅：

```
Unity 菜单：Tools → DataTable → 生成全部配置表代码
```

预期：Console 输出 `[DataTableGenerator] Action=GenerateAllCompleted Count=6`（5 个 Tattoo + 1 个 ResourceConfig）。

### 4.3 跑 EditMode 测试

```
Unity 菜单：Window → General → Test Runner → EditMode → Run All
```

预期：TattooStrategyTests 8 个 + TattooModuleIntegrationTests 5 个全部 PASS。

### 4.4 Play 跑通

进入 Play Mode → Launch.unity 自动启动 → 应看到：
- 相机俯视角，灰色地面 + 1 蓝色玩家方块 + 4 红色敌人方块
- UI Toolkit HUD：左侧装备配置 + Build 列表 + 战斗日志；右侧 3 个 Dropdown + 装备按钮
- 按 WASD 移动 / 鼠标左键攻击 / E 技能 / 空格闪避
- 切换 Dropdown 选择部位/颜色/图案，点击「装入 Build」可看 Build 与 Passive 实时更新

---

## 五、已知遗留

| 优先级 | 项 |
|---|---|
| 🟡 中 | SpawnerModule 用 CreatePrimitive 起步——未来需要正式美术资产时改为 ResourceConfig + ResourceModule.Load，并在 `Assets/Resources/Prefab/` 准备 Player.prefab / Enemy.prefab |
| 🟡 中 | 暴击率 25% 硬编码在 CombatModule —— 应该接入 PassiveStats.CritRateBonus + PlayerStats.CritMultiplier 的最终计算（未来优化） |
| 🟡 中 | DodgePressedEvent / SkillCastEvent 当前只是发布事件让 TattooModule 触发对应槽位；玩家本身的闪避无敌帧、技能冷却尚未消费这些事件实现实际效果（按 MVP 先不做） |
| 🟢 低 | PlayMode 集成测试待 Launch.unity 配置完成后由用户跑 |
| 🟢 低 | `/openspec verify-change` + `/openspec archive-change` 由用户最终决定 |

---

## 六、统计

| 指标 | 值 |
|---|---|
| 新增 .cs 文件 | 38 |
| 新增 JSON 配置 | 5 |
| 新增 UI Toolkit 资源 | 2（UXML + USS） |
| 重写 .cs 文件 | 7（GameApp / GameTickDriver / ModuleRunner / InputModule / UIModule / GameStateModule / DataTableRegistry） |
| 删除 .cs 文件 | 14（Tattoo 12 移至 _Legacy~ + 2 旧测试） |
| 总测试用例 | 13（8 单元 + 5 集成） |
| OpenSpec 文档 | 4（proposal/design/tasks/spec） |

---

*执行：Claude Code (Opus 4.7) — 完全按 openspec/01-tattoo-framework-rewrite/ 落地*
*下一步：Launch.unity 配置 → Play 跑通 → openspec verify-change → archive-change*
