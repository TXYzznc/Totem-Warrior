# Design: GF_X business runtime refactor

## 1. Baseline Analysis

### 1.1 已实现效果

当前 `Assets/Scripts` 已实现的游戏效果不是空壳，主要包括：

| 系统 | 已实现效果 | 主要旧入口 |
|---|---|---|
| 启动/生命周期 | `GameApp` 创建 `EventBus`、`ModuleRunner`，按依赖初始化 20+ 模块，注册 `ITickable` / `ILateTickable` | `Assets/Scripts/Core/GameApp.cs` |
| 配置表 | 28 张 JSON 配置表，强类型 C# 生成类，运行时同步加载 | `Assets/Resources/DataTable` + `DataTableModule` |
| 输入 | WASD、鼠标左键、E、Space、Tab、Esc、F、F12 语义化输入，支持测试 simulator | `InputModule` |
| UI | UGUI 主菜单、角色选择、设置、HUD、暂停、结算、商店、纹身、三选一等 12 个 prefab | `UIModule` + `UIFormConfig` |
| 战斗 | 玩家移动/攻击/技能/闪避事件，伤害和击杀事件，武器系统接入 | `CombatModule` / `WeaponModule` / `SkillModule` |
| 纹身 | 336 组合、部位/颜色/图案策略、自纹身读条、附魔、状态效果和 VFX 事件 | `TattooModule` |
| AI/Bot | Smart/Light Bot、构筑预设、战斗行为、伤害响应 | `BotControllerModule` |
| 地图/相机 | 2.5D 固定地图主题、缩圈、相机跟随/边界、Billboard sprite | `MapGenModule` / `CameraModule` |
| 经济/NPC/事件 | 金币、宝箱、商人、纹身师、三选一事件与奖励 | `EconomyModule` / `NPCModule` / `EventModule` |
| 资源/美术 | Character prefab、UI prefab、Sprite、Anim、部分 fallback procedural objects | `Resources.Load` / `ResourceModule` |
| 诊断/测试 | EditMode/PlayMode 测试文件、playtest driver、GF_X 诊断 | `Assets/Tests` / `项目知识库（AI自行维护）/wiki/AI_DIAGNOSTICS_GUIDE.md` |

### 1.2 当前结构性问题

1. **GF_X 启动与旧业务断开**  
   `Assets/Game/Scene/Launch.unity` 会进入 GF_X `WorkspaceProcedure`，但 `WorkspaceProcedure` 当前只是空工作区日志，不创建旧业务 runtime，也不进入游戏流程。

2. **双 DataTable 体系并行**  
   GF_X 用 `GameData/DataTables/*.xlsx -> txt/json/bytes/cs`，旧业务用 `Assets/Resources/DataTable/*.json -> Assets/Scripts/DataTable/*.cs`。两边都叫 DataTable，但加载器、路径、代码生成规则不同。

3. **双 UI/资源体系并行**  
   GF_X 提供 `GF.UI`、`UITable`、`UIFormBase`、UIGroup；旧业务用 `UIModule + Resources/Prefab/UI + IUIForm`。后续如果继续新增 UI，会产生两套 UI 生命周期。

4. **双事件/生命周期体系并行**  
   GF_X 用 GameFramework Event/Fsm/Procedure；旧业务用自研 `EventBus`、`ModuleRunner`、`ITickable`。这会让启动时序、因果链和测试日志分裂。

5. **资源加载仍大量走硬编码 `Resources.Load`**  
   业务中存在大量 `Resources.Load("Prefab/...")`、`Resources.Load("Sprite/...")` 和 CreatePrimitive fallback。GF_X 已有 Resource/DataTable/UI 管线，需要逐步接管。

6. **测试基础设施需要修复**  
   当前 `uloop` CLI 不可用；Unity Editor 已打开时 batchmode TestRunner 不能并发打开项目；UnitySkills 能连接但没有直接 run-tests skill。后续验收必须补可复跑的测试入口。

## 2. Target Architecture

### 2.1 启动链

目标启动链：

```text
Assets/Game/Scene/Launch.unity
-> Builtin GF components
-> HotfixEntry.StartHotfixLogic
-> PreloadProcedure
-> WorkspaceProcedure
-> TotemGameProcedure
-> TotemGameRuntime
```

`WorkspaceProcedure` 只负责进入项目工作区和切换到业务 Procedure；实际业务由 `TotemGameProcedure` 与 `TotemGameRuntime` 承载。

### 2.2 业务运行时演进

分三层逐步迁移：

| 层 | 作用 | 迁移状态目标 |
|---|---|---|
| GF_X Procedure 层 | 启动、预加载、流程切换、诊断时序 | 第一批建立 |
| Business Runtime 层 | 提供 GF_X 原生业务入口、服务注册和状态快照 | 第一批建立，后续按需求切片扩展 |
| GF_X Native 层 | GF.DataTable / GF.Event / GF.UI / GF.Resource 原生使用 | 按模块原生重写业务 |

执行规则：

- 旧 `Assets/Scripts` 只作为需求、时序、数据字段、资源引用和测试点的反推来源。
- 不挂载旧 `GameApp`、`ModuleRunner`、`EventBus`、`UIModule` 或旧 `DataTableModule` 作为运行时宿主。
- 新业务必须从 GF_X Procedure/runtime/service 进入，模块之间用 GF_X 事件、数据表、资源和 UI 流程连接。
- 如果需要读取旧实现，应先产出“需求提炼记录”，再写 GF_X 原生实现。

### 2.3 DataTable 迁移策略

候选方案：

| 方案 | 优点 | 缺点 | 决策 |
|---|---|---|---|
| A. 旧 JSON 全保留，GF_X 只启动旧 DataTableModule | 快 | 双轨永久化，不解决 AI/策划表流 | 不采用 |
| B. 一次性把 28 张业务 JSON 全转 GF_X xlsx/txt/cs | 彻底 | 风险大，主键类型与生成器约束复杂 | 不作为第一批 |
| C. 先建立 GF_X DataTable facade，再按模块迁表 | 可验证、可回滚 | 初期多一层适配 | 采用 |

第一批建立 facade/诊断：列出 28 张旧业务表、主键异常、GF_X 目标表名与迁移优先级。后续每迁一个模块，就迁该模块依赖表。

### 2.4 UI 迁移策略

GF_X UI 目标：

- 新 UI 走 `GF.UI.OpenUIForm` / `UITable` / `UIFormBase`。
- 已有 UGUI prefab 不立即重做，但由 GF_X UIGroup 管理加载/生命周期。
- `IUIForm` 的状态回调逐步转为 GF_X UIForm 生命周期和 GF Event。

第一批优先迁移启动可见链：

```text
MainMenu -> CharacterSelect -> StartupSelect -> InGame HUD
```

### 2.5 Input 迁移策略

硬约束：所有按键输入仍必须走 `InputModule` 或其 GF_X 后继接口。

第一批不改变输入语义，只把输入服务挂到 GF_X runtime 上；后续可将其重命名/迁移为 `TotemInputService`，并保留 simulator 注入入口。

## 3. Migration Slices

### Slice 0: 规格与基线

- 生成本 change 的 proposal/design/tasks/spec。
- 记录旧业务效果清单。
- 记录当前自动测试限制。
- 跑 GF_X 诊断和 AI DataTable 校验作为框架基线。

### Slice 1: GF_X 业务入口

- 新增 `TotemGameProcedure`。
- `WorkspaceProcedure` 切换到 `TotemGameProcedure`。
- `AppConfigs.asset` 启用新 Procedure。
- 新增 runtime 状态日志和诊断场景。

### Slice 2: GF_X 原生业务 runtime

- 建立 `TotemGameRuntime`，由 GF_X Procedure 创建/销毁。
- 建立原生服务注册、状态快照、诊断输出和需求映射入口。
- 不承接旧模块初始化，不创建旧 `GameApp`。
- 不再依赖旧 `Assets/Scenes/MainMenu.unity` 或 `Assets/Scenes/Launch.unity`。

### Slice 3: DataTable facade

- 建立业务表清单和 GF_X 表迁移 manifest。
- 先不一次性转 28 张表，优先迁 UI、GameState、Input 需要的表。
- 为主键非 `Id:int` 的表记录生成器适配需求。

### Slice 4: UI 首屏链

- 将 MainMenu/CharacterSelect/StartupSelect/HUD 纳入 GF_X UI 管理。
- 旧 `FindObjectOfType<GameApp>` 只作为“旧实现依赖点”记录，新 UI 直接使用 GF_X runtime context。

### Slice 5: 模块逐步原生化

优先级：

1. GameState / Flow / Input / UI
2. DataTable / Resource
3. Spawner / MapGen / Camera
4. Combat / Weapon / Skill / Tattoo / Status
5. Economy / NPC / Event / Bot / Audio / VFX

每个模块迁移必须保持测试或诊断通过。

## 4. Verification Strategy

不自动执行运行界面 playtest，但必须自动验证：

- Unity 编译/资源刷新。
- GF_X 诊断报告。
- AI DataTable JSON 校验。
- 配置表清单检查。
- 资源路径检查。
- 可用时运行 EditMode/PlayMode 测试。
- 若 TestRunner 不可用，新增 headless 诊断场景作为临时替代。

界面测试保留为手动/半自动 playtest，用例先写入 `tests/plan.md`。

## 5. Risks

- 一次性替换旧框架会导致大量脚本同时失效。缓解：先反推需求清单和 GF_X 原生骨架，再按依赖切片重写，不挂旧 runtime。
- 旧代码残留可能误导实现继续走旧框架。缓解：旧代码只能作为只读证据；新增诊断扫描 GF_X 业务层对旧入口的直接依赖。
- 配置表迁移可能破坏策划数据。缓解：先做 manifest + 校验，再迁单表。
- UI Prefab 直接改动风险高。缓解：首轮只接生命周期，不做视觉重排。
- 当前测试工具不稳定。缓解：把 TestRunner 修复列为 T2，GF_X 诊断作为兜底。
