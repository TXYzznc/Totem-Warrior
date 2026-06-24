# Tasks — 01-tattoo-framework-rewrite

> 实施进度。✅ = AI 已交付；⏳ = 等用户在 Unity 中手工动作；🔲 = 未完成。

---

## Phase A — 框架就位 + 配置 schema

### A.0 准备工作（机械动作）

- [x] 复制 AI_Friendly/Assets/Scripts/{Core,Utils,DataTable,Events,Modules,Templates,ExternalAPI} 到 GameDesinger
- [x] 旧 Tattoo 12 个 .cs 移到 `Assets/_Legacy~/Tattoo/`
- [x] 删除 CombatScene / SampleScene / TattooDemo 三个旧场景
- [x] 补入 `tools/sync-agents.py`
- [x] 创建 `.env`（从 `.env.example`，凭据需手填）

### A.1 InputModule 现状勘测 ✅

- [x] 现状：InputModule.cs 已有 `GetMoveDirection / IsSpacePressed / IsEscapePressed / IsReturnPressed / IsDebugKeyPressed`
- [x] 扩展：增加 `IsAttackPressed()` / `IsSkillPressed()` / `IsDodgePressed()` 三个语义化高层查询
- [x] 决策：**不引入 InputAttack/Skill/Dodge Event 类**——CombatModule 在 OnUpdate 中直接轮询 InputModule

### A.2 事件总表 ✅

- [x] `Assets/Scripts/Events/TattooEvents.cs` 落地全部 17 个事件类（6 战斗触发 + 3 战斗结果 + 2 Build + 3 阶段 + 3 输入语义占位）

### A.3 DataTable schema 设计与落地 ✅

- [x] AI 写出 5 个 JSON 完整内容（含 schema + 全部行数据），落到 `Assets/Resources/DataTable/`：
  - `TattooPartConfig.json`（6 行）
  - `TattooColorConfig.json`（7 行）
  - `TattooPatternConfig.json`（8 行）
  - `TattooElementConfig.json`（7 行）
  - `TattooShapeConfig.json`（8 行）
- [x] AI 同步手写 5 个 generator 产物 `.cs`（Row + IDataTable）+ 重写 `DataTableRegistry.cs`（删旧 STG 业务条目）
- ⏳ **用户在 Unity 跑 `Tools/DataTable/生成全部配置表代码`**（generator 是幂等的，会用现有 JSON 重新生成与手写文件等价的 .cs；但跑一遍验证 generator 路径通畅）

### A.4 ResourceConfig（暂不需要）

- 决策：**SpawnerModule 先用 `GameObject.CreatePrimitive` 起步**，未来需要 Prefab 时再扩展 ResourceConfig + 用 ResourceModule.Load
- 此项任务推迟到 Phase D（如有）

### A.5 Launch.unity ⏳

- ⏳ **用户在 Unity 中 File → New Scene → 保存为 `Assets/Scenes/Launch.unity`**
- ⏳ **场景中创建 GameObject 命名 `@Game` 挂 `GameApp` 组件**
- ⏳ **再创建一个 GameObject 命名 `CombatHUD`**：挂 `UIDocument`（指向 `Assets/UI/CombatHUD.uxml`）+ `Tattoo.UI.CombatHUDForm`
- 用户跑 Play 即可走通完整流程

---

## Phase B — 核心模块 TattooModule ✅

### B.1 数据类迁移 ✅

- [x] `Assets/Scripts/Modules/Tattoo/Data/`：`TattooEnums.cs` / `PlayerStats.cs` / `PassiveStats.cs` / `PlayerState.cs` / `Target.cs` / `EffectContext.cs` / `EffectResult.cs` / `PendingTrigger.cs` / `TattooSlot.cs`
- [x] 旧 `enum GameEvent` 未复制——已被 class 事件取代

### B.2 策略接口与实现 ✅

- [x] 3 个接口：`IPartBehavior` / `IElementBehavior` / `IShapeBehavior`
- [x] 6 个 PartBehavior：Head / Torso / LeftArm / RightArm / LeftLeg / RightLeg
- [x] 7 个 ElementBehavior：Fire / Lightning / Nature / Frost / Mutation / Holy / Pure
- [x] 8 个 ShapeBehavior：SingleHit / AOEBurst / StackingMark / MultiHit / ChainJump / ProbBurst / TrailZone / SummonForm
- [x] `SynergyCalculator`（替代原 SynergyPipeline）

### B.3 TattooModule 实装 ✅

- [x] `Assets/Scripts/Modules/Tattoo/TattooModule.cs`
- [x] 6 个 `[EventHandler]`：OnAttackHit / OnCritHit / OnDamaged / OnSkillCast / OnDodgePressed / OnMoveTick
- [x] `Fire(eventType, primary, attacker, path?)` 完整调度
- [x] `Equip(partId, colorId, patternId)` / `Clear()` + BuildChangedEvent / PassiveRecomputedEvent
- [x] `ConsumePendingTriggers`（左腿延迟、白×星刷冷却等）

### B.4 测试 ✅

- [x] `Assets/Tests/EditMode/TattooStrategyTests.cs`（8 个纯单元测试覆盖 Shape/Element/Part 行为）
- [x] `Assets/Tests/EditMode/TattooModuleIntegrationTests.cs`（5 个 UnityTest 覆盖 Equip / Fire / Clear / 错误处理）
- [x] 旧 `CombatIntegrationTests.cs` + `TattooCompositionTests.cs`（含 _Legacy 死引用）已删除

---

## Phase C — CombatModule + SpawnerModule + UI Toolkit ✅

### C.1 SpawnerModule ✅

- [x] `Assets/Scripts/Modules/Spawner/SpawnerModule.cs`
- [x] `Assets/Scripts/Modules/Spawner/EntityRef.cs`（GameObject ↔ Target 绑定）
- [x] InitializeAsync：CreatePrimitive 创建 相机 / 灯 / 地面 / Player + 4 个 Enemy
- 决策：**暂不依赖 ResourceModule**——CreatePrimitive 起步，后续要 Prefab 时再 swap

### C.2 CombatModule ✅

- [x] `Assets/Scripts/Modules/Combat/CombatModule.cs`（实现 `ITickable`）
- [x] OnUpdate 轮询 InputModule：移动 / 普攻 / 技能 / 闪避
- [x] 25% 暴击占位发 `CritHitEvent`，否则发 `AttackHitEvent`
- [x] 监听 `EffectAppliedEvent` 判定击杀，发 `TargetKilledEvent` / `PlayerDiedEvent` / `CombatEndedEvent`
- [x] 移动 tick 每 0.5s 发一次 `MoveTickEvent`，附路径上敌人

### C.3 UI Toolkit 接入 ✅

- [x] `Assets/UI/CombatHUD.uxml`：左侧装备/Build/Passive/日志 + 右侧 3 个 dropdown + 装备/清空按钮
- [x] `Assets/UI/CombatHUD.uss`：暗色科技风样式
- [x] `Assets/Scripts/Modules/Tattoo/UI/CombatHUDForm.cs`：注册到 UIModule，订阅 BuildChanged / Passive / EffectApplied / TargetKilled / PlayerDied
- [x] async Start：等 GameApp 就绪（10s 超时容错）

### C.4 集成测试 ✅

- [x] EditMode 集成测试（TattooModuleIntegrationTests）覆盖 Equip + Fire + Clear + 错误
- 决策：**暂不写 PlayMode 集成测试**——Launch.unity 由用户最终配置，PlayMode 用户在 Editor 中直接点 Play 验证

---

## 框架基础设施清理（额外发现的死引用）✅

执行过程中发现 GameApp.cs / GameTickDriver.cs / UIModule.cs / GameStateModule.cs / DataTableRegistry.cs / Tattoo.Tests.asmdef 均含 STG 业务死引用，一并清理：

- [x] `GameApp.cs` 重写：清掉 BulletModule/EnemyModule/LevelModule/PlayerModule 注册，加 Tattoo/Spawner/Combat 注册；暴露 `TryGetRuntime`
- [x] `GameTickDriver.cs` 抽象为 `ITickable` 注册式，去掉硬编码 EnemyModule/BulletModule 字段
- [x] `UIModule.cs` 重写为通用 IUIForm 容器（删除 6 个旧 panel 引用 + GameStateChangedEvent/EnemyDead/WaveStart/PlayerHit/LevelComplete/PlayerDead handler）
- [x] `GameStateModule.cs` 重写为通用版（GameState 枚举 = MainMenu/Loading/InGame/Paused/GameOver；GameStateChangedEvent 内联在同一文件；删除 LevelStartEvent/EnemyDeadEvent/LevelCompleteEvent/PlayerDeadEvent handler）
- [x] `DataTableRegistry.cs` 重写：删 7 条 STG 业务条目，加 5 条 Tattoo 配置
- [x] `Tattoo.Tests.asmdef` 重写 references：去掉 "Tattoo"（不存在的 assembly），加 "UniTask"
- [x] `InputModule.cs` 扩展：加 IsAttack/IsSkill/IsDodge 三个语义查询
- [x] `ModuleRunner.cs` 增 `GetAllModules()` API（供 GameApp 注册 ITickable）

---

## 验证与归档

- [x] 写 `INTEGRATION_REPORT.md` 总结
- [x] `项目知识库（AI自行维护）/wiki/Tattoo系统重构.md` 已就位（更早创建）
- ⏳ Launch.unity 配置完成后跑 EditMode 测试套件（用户操作）
- 🔲 `/openspec verify-change` 检查（等 Launch 后跑）
- 🔲 `/openspec archive-change` 归档（最终）
