# Totem Warrior 当前项目总结与待做清单

生成日期：2026-07-09  
最近更新：2026-07-09 17:45，本次更新补充 PCG 接入完成状态、诊断报告和性能说明  
适用工程：`<project-root>`，当前工作区为 Totem-Warrior 工程根目录  
Unity 版本：`2022.3.62f3`  
当前启动场景：`Assets/Game/Scene/Launch.unity`  
当前状态：GF_X 原生首轮业务重构基线已闭环，PCG 地图已接入游戏初始化链路，工程已清理 GF_X DemoGame 示例内容，进入后续产品化开发阶段。

## 0. 总览结论

当前工程已经从旧 `Assets/Scripts` 业务框架迁移到 GF_X 运行框架。旧代码、旧测试、旧工具和旧配置表已经作为历史证据归档到 `LegacyProjectArchive`，不再参与当前启动、编译和运行链路。

当前首轮目标已经达成：

- 使用 `Assets/Game/Scene/Launch.unity` 作为唯一启动场景。
- 使用 GF_X 的 `PreloadProcedure -> WorkspaceProcedure -> TotemGameProcedure` 启动链。
- 使用 `TotemGameRuntime` 和 26 个 Totem runtime service 承载业务。
- 复现首轮旧功能范围：主菜单、角色选择、启动选择、战斗 HUD、地图、玩家、输入、相机、战斗、AI、纹身、商店、NPC、三选一、缩圈、Boss。
- 复现首轮规模：1 玩家、20 Smart AI、29 Light AI、1 Boss；非 Boss actor 为 50，含 Boss 总 actor 为 51。
- 建立 AI 友好的配置工作流：28 张 Business JSON、28 张 Business xlsx、runtime gameplay catalog、runtime asset catalog。
- 建立非 UI 自动诊断闭环：最新 GF_X 全量诊断为 `27 success / 0 failure / 0 warning`。
- 接入外部 PCG 示例项目的地图生成能力：游戏初始化时优先生成 PCG 地图，并适配当前地形网格、锚点、缩圈、actor/NPC/resource/event 消费链路；迁入完成后源示例目录已从根目录清理。
- 清理 GF_X 原框架 DemoGame 示例脚本和资源：`Assets/Game/Examples` 与 `GameData/Examples` 已删除。
- 报告目录已收敛：`GameData/Diagnostics/Reports` 与 `GameData/AIData/Reports` 均只保留最新两个报告。
- AI 资源索引已刷新：清理 DemoGame 后当前索引资源数为 349，示例资源不再进入活动资源索引。

当前边界也要明确：

- 这是“首轮可继续开发的干净基线”，不是最终商业品质版本。
- UI 视觉、正式美术、角色帧动画、VFX 表现、音频表现、战斗手感、数值平衡仍需要后续产品化。
- 旧存档不兼容是已确认边界；后续使用新的存档/运行记录体系。
- 旧代码只作为证据，不允许恢复旧 `GameApp`、`ModuleRunner`、`EventBus`、`UIModule`、`DataTableModule`。

## 1. 目前已经实现的功能

### 1.1 GF_X 框架迁移与启动链

当前项目已经以 GF_X 作为主框架。

已实现内容：

- `Assets/Game/Scene/Launch.unity` 是当前唯一有效启动场景。
- `AppConfigs.asset` 配置了当前启动流程。
- 启动流程进入：
  - `ChangeSceneProcedure`
  - `PreloadProcedure`
  - `WorkspaceProcedure`
  - `TotemGameProcedure`
- `WorkspaceProcedure` 只负责切换进入 Totem 业务工作区。
- `TotemGameProcedure` 创建并驱动 `TotemGameRuntime`。
- `TotemGameRuntime` 统一注册、初始化、tick、late tick 和关闭 runtime services。
- 启动链通过 `StartupChainDiagnosticScenario` 和 GF_X 全量诊断验证。
- 旧 `Assets/Scripts` 不再存在于活动路径，旧业务代码已归档到 `LegacyProjectArchive/Assets/Scripts`。
- GF_X DemoGame 示例内容已删除：
  - `Assets/Game/Examples` 不存在。
  - `GameData/Examples` 不存在。
  - `Assets/Game/Scene/Game.unity` 不存在。
  - 示例 `MenuProcedure` / `GameProcedure` / `GameOverProcedure` 不存在。

当前禁止恢复：

- 不要重新创建 `Assets/Scripts`。
- 不要重新创建 `Assets/Resources/DataTable`。
- 不要恢复旧 `GameApp` / `ModuleRunner` / `EventBus`。
- 不要把 GF_X DemoGame 示例重新放回活动工程。

### 1.2 运行时服务架构

当前业务运行时由 26 个 GF_X 原生服务组成。服务集中在：

```text
Assets/Game/Scripts/Runtime/Services
```

已接入服务：

| 服务 | 职责 |
|---|---|
| `TotemDataService` | 加载 `totem_gameplay_catalog.json`，提供业务配置入口 |
| `TotemAssetService` | 加载 `totem_runtime_assets.json`，统一管理运行时资源索引和缓存 |
| `TotemGameFlowService` | 管理主菜单、角色选择、启动选择、战斗 HUD 等流程状态 |
| `TotemUIService` | 打开/关闭 GF_X UI Form，驱动 HUD 与 overlay 数据 |
| `TotemInputService` | 唯一输入入口，所有按键/模拟输入必须经过这里 |
| `TotemMapService` | 游戏初始化时优先生成 PCG 地图，并适配 400m 兼容地图、地形网格、主题、锚点和交互布点 |
| `TotemActorService` | 生成玩家、Smart AI、Light AI、Boss，维护 actor 状态 |
| `TotemAIService` | 20 Smart AI、29 Light AI、Boss AI 决策与行为 |
| `TotemCombatService` | 移动、攻击、技能、闪避、伤害结算与战斗快照 |
| `TotemWeaponService` | 武器装备、发射、升级、掉落和 Life Steal 等特性 |
| `TotemSkillService` | 技能槽、技能释放、技能冷却与技能伤害 |
| `TotemStatusService` | Burn、Poison、Shock、Stun、Slow 等状态效果 |
| `TotemTattooService` | 纹身组合、自纹身读条、触发器、附魔和 actor-scoped 纹身状态 |
| `TotemEconomyService` | 金币、墨水、购买、扣费、奖励等经济状态 |
| `TotemChestService` | 宝箱生成、奖励概率和开启结算 |
| `TotemNpcService` | 商人、纹身师、NPC 交互与商店库存 |
| `TotemChoiceService` | 三选一事件、选项权重、奖励发放 |
| `TotemZoneService` | 缩圈阶段、半径变化、圈外伤害 |
| `TotemBossService` | Boss 阶段、Boss 技能、阶段转换、Boss 奖励 |
| `TotemCameraService` | 2.5D 正交相机、跟随、边界 clamp、shake |
| `TotemVfxService` | 攻击、技能、弹道、Boss 临时 VFX 生命周期 |
| `TotemAudioService` | BGM、SFX、Boss 阶段音频 cue 与重复播放节流 |
| `TotemSettingsService` | 设置预览、提交、回滚和持久化 |
| `TotemRunStatsService` | 单局统计、胜负记录、新存档统计 |
| `TotemMetaProgressService` | Meta 解锁、角色解锁、长期进度 |
| `TotemInteractionService` | 附近交互目标检测、F 键交互、HUD 提示 |

运行时接口：

- `ITotemRuntimeService`：基础生命周期接口。
- `ITotemRuntimeTickService`：参与 runtime tick 的服务。
- `ITotemRuntimeLateTickService`：参与 late tick 的服务，例如相机。
- `TotemRuntimeServiceBase`：基础服务基类。
- `TotemRuntimeServiceStatus`：服务状态记录。

### 1.3 UI 流程与 UI Form

当前已经完成第一条 UI 入口链：

```text
MainMenu -> CharacterSelect -> StartupSelect -> CombatHUD
```

活动 UI 预制体位置：

```text
Assets/Game/Prefabs/UI
```

旧 UI 资源复用证据位置：

```text
Assets/Resources/Prefab/UI
```

当前 12 个 UI Form 已接入 GF_X 生命周期：

| Form | 当前用途 |
|---|---|
| `MainMenu` | 主菜单入口 |
| `CharacterSelect` | 角色选择 |
| `StartupSelect` | 初始武器/纹身/启动选项 |
| `CombatHUD` | 战斗 HUD、血量、武器、技能、敌人、缩圈、交互提示 |
| `PauseMenu` | 暂停菜单 |
| `RunResult` | 运行结果/胜负结算 |
| `Settings` | 设置界面 |
| `Shop` | 商人商店 |
| `SelfTattoo` | 自纹身界面 |
| `TattooEnchant` | 纹身附魔 |
| `TattooStudio` | 纹身师交互 |
| `ThreeChoice` | 三选一事件 |

已实现的 UI 验证：

- 12 个活动 UI prefab 均无 missing script。
- `UIFormConfig.json` / `UIFormConfig.xlsx` 记录 UI 表单配置。
- `UITable.json` 当前记录 12 个 UI view。
- `TotemFirstSliceUIDiagnosticScenario` 验证主流程 UI、HUD 文本、状态文本、技能/武器图标、商店和三选一显示数据。
- PlayMode CombatHUD input smoke 已验证 CombatHUD 输入链走 `TotemInputService`。

当前 UI 边界：

- UI 逻辑和数据绑定已接入。
- UI 视觉仍是占位/复用资源，需要后续正式设计。
- UI 自动化主要验证状态、数据、prefab、输入链，不验证最终美术观感。

### 1.4 输入系统

当前所有输入必须走：

```text
TotemInputService / ITotemInputProvider
```

已覆盖输入：

- 移动：WASD / 方向键语义。
- 攻击：鼠标左键。
- 技能：E / Q。
- 闪避：Space。
- 交互：F。
- 自纹身：Tab。
- 暂停/返回：Escape。
- 确认：Return。
- Playtest 菜单模拟输入：
  - `Tools/Playtest/Press/...`
  - `Tools/Playtest/Hold/...`
  - `Tools/Playtest/Move/...`
  - `Tools/Playtest/Smoke/CombatHUD Input`

当前规则：

- 新增 gameplay 输入时，不允许直接读 `Input.GetKey`。
- 自动测试或 PlayMode smoke 也必须通过 `TotemInputService` 或测试输入 provider 注入。

### 1.5 配置表与 AI 友好数据工作流

当前业务配置表有 28 张。AI 优先编辑 JSON，策划查看 xlsx，运行时读取汇总 catalog。

AI 可编辑源：

```text
GameData/AIData/DataTables/Business/*.json
```

策划可读镜像：

```text
GameData/DataTables/Business/*.xlsx
```

运行时配置产物：

```text
GameData/AIData/GameplayCatalogs/totem_gameplay_catalog.json
```

当前 28 张 Business 表：

| 表 | 主要职责 |
|---|---|
| `BossPhaseConfig` | Boss 阶段、技能、BGM、VFX、死亡配方 |
| `BotBuildPreset` | Smart AI build 预设、推荐纹身序列、技能槽倾向 |
| `BotConfig` | Smart/Light AI 配置、五性格字段、权重和行为参数 |
| `ChestConfig` | 宝箱奖励、奖励概率 |
| `EnemyConfig` | Light、Elite、Boss 身体属性、掉落、奖励 |
| `EventConfig` | 地图事件定义 |
| `ItemConfig` | 物品、墨水、奖励条目 |
| `MapTemplateConfig` | 地图主题、大小、地形、锚点基础 |
| `MerchantConfig` | 商人槽位、价格、售卖规则 |
| `NPCConfig` | NPC 类型、交互范围、主题系数 |
| `ProjectileConfig` | 弹道速度、范围、穿透、VFX |
| `ResourceConfig` | 地图资源、武器拾取和资源定义 |
| `ShopStockConfig` | 商店库存、价格、奖励类型 |
| `SkillConfig` | 技能、伤害、冷却、范围、VFX |
| `TattooColorConfig` | 纹身颜色 |
| `TattooElementConfig` | 纹身元素 |
| `TattooEnchantAffixConfig` | 纹身附魔词缀 |
| `TattooEnchantRecipeConfig` | 纹身附魔配方 |
| `TattooPartConfig` | 纹身部位 |
| `TattooPatternConfig` | 纹身图案 |
| `TattooReadingTimeConfig` | 自纹身读条时长 |
| `TattooShapeConfig` | 纹身形状和参数 |
| `ThreeChoiceOptionConfig` | 三选一选项、权重、奖励 |
| `UIFormConfig` | UI 表单路径、排序、是否独占 |
| `WeaponConfig` | 武器属性、伤害、攻速、弹道、trait |
| `WeaponDropConfig` | 武器掉落来源、权重、房间范围 |
| `WeaponTraitConfig` | 武器特性、状态、穿透、连锁、Life Steal 等 |
| `ZoneShrinkConfig` | 缩圈阶段、时间、半径、圈外伤害 |

当前 GF_X Core 表有 5 张：

```text
GameData/AIData/DataTables/Core/*.json
GameData/DataTables/Core/*.xlsx
```

Core 表：

- `EntityGroupTable`
- `LanguagesTable`
- `SoundGroupTable`
- `UIGroupTable`
- `UITable`

已实现工具：

- `Game Framework/GameTools/AI Data/Export DataTables Json`
- `Game Framework/GameTools/AI Data/Validate DataTables Json`
- `Game Framework/GameTools/AI Data/Import DataTables Json`
- `Game Framework/GameTools/AI Data/Reverse DataTables Json To Excel`
- `Game Framework/GameTools/AI Data/Reverse Business DataTables Json To Excel`
- `Game Framework/GameTools/AI Data/Check Business Json Excel Sync`

当前数据验证结果：

- Business JSON：28
- Business xlsx：28
- Core JSON：5
- Core xlsx：5
- `totem_gameplay_catalog.json` 由 28 张 Business JSON 生成。
- JSON/xlsx 同步检查为 0 changed cell、0 changed row。
- `datatables.json` 索引已刷新。

### 1.6 运行时内容与首轮规模

首轮运行时内容已经在 GF_X 原生路径实现。

已验证规模：

| 内容 | 当前数量 |
|---|---:|
| 玩家 | 1 |
| Smart AI | 20 |
| Light AI | 29 |
| Boss | 1 |
| 非 Boss actor，包括玩家 | 50 |
| 总 actor，包括 Boss | 51 |
| AI controller state | 49 |
| Smart AI profile | 20 |
| Light AI profile | 3，被 29 个 Light AI 复用 |
| Bot build preset | 7 |
| 纹身组合 | 336 |
| 武器 | 5 |
| 弹道 | 2 |
| 武器 trait | 10 |
| 武器掉落 | 15 |
| 技能 | 14 |
| Boss phase | 3 |
| NPC | 5 |
| 商店库存 | 15 |
| 商人槽位 | 9 |
| 三选一选项 | 11 |
| 地图事件 | 6 |
| 缩圈阶段 | 3 |
| 音频 cue | 14 |
| 地图模板 | 3 |

### 1.7 PCG 地图、地形、锚点与缩圈

当前地图系统由 `TotemMapService`、`TotemZoneService` 和 `Assets/Game/Scripts/Runtime/PCGMap` 中的 PCG 模块共同实现。游戏初始化时会优先使用 PCG 生成地图，再适配为当前 runtime 消费的 400m 兼容地图、100x100 地形网格、房间、锚点和交互布点。

已实现内容：

- PCG 运行时接入：
  - PCG 源能力来自外部示例项目，已迁入当前 GF_X runtime；根目录源示例目录已清理，不参与版本控制和运行链路。
  - PCG 代码位置：`Assets/Game/Scripts/Runtime/PCGMap`。
  - PCG catalog 位置：`Assets/Resources/PCG`。
  - PCG 图片资源位置：`Assets/Resources/Sprite/PCG`。
  - 当前 PCG 图片资源：1329 张 PNG，约 53.94 MB。
  - 当前 PCG catalog 包括 `TerrainTileSetCatalog.json`、`TerrainVisualCatalog.json`、`WorldObjectCatalog.json`、`ZoneRuleCatalog.json`、`TerrainMaskOverlayCatalog.json`。
- 地图尺寸兼容层：
  - runtime 仍对外提供 400m 地图边界，保证相机、缩圈、actor 布点、资源和事件消费者无需大改。
  - PCG 内部生成 64x64 数据，再映射到当前 100x100 地形网格。
  - 每格 4m。
- 3 个地图主题：
  - `AI_RUINS`
  - `ALIEN_HIVE`
  - `VIRUS_SWAMP`
- 地形类型：
  - `Ground`
  - `Slow`
  - `Blocked`
  - `Cover`
  - `Hazard`
- 运行时验证：
  - PCG 地图生成有效。
  - PCG 可行走格全部可达。
  - PCG 缺图数量为 0。
  - PCG runtime asset fallback 数量为 0。
  - 越界查询返回 `Blocked`。
  - `Slow` 移速倍率为 0.65。
  - `Blocked` 阻止移动。
  - `Hazard` 造成地形伤害。
  - `Cover` 减少来源型伤害。
- 地图锚点：
  - 玩家出生点。
  - Boss 出生点。
  - 商人。
  - 纹身师。
  - common/rare chest。
  - EnemySpawn socket。
  - map weapon resource。
  - map choice event。
- EnemySpawn 分组：
  - inner：14
  - mid：17
  - outer：18
- 地图渲染：
  - PCG 地面格使用 `Tilemap` 渲染，避免生成 4096+ 个地面 `SpriteRenderer` 对象。
  - 世界对象、POI 等仍可按需生成 `SpriteRenderer` 对象。
  - 旧的海量 `TotemDamageFloat_25` / runtime 临时对象残留已由诊断清理和残留检查覆盖。
- 缩圈：
  - 3 阶段。
  - 半径随时间收缩。
  - 圈外伤害生效。
  - 诊断验证 0s、180s、540s、900s 的半径合同。

当前边界：

- 地图逻辑和 PCG 接入已可测，并已进入 GF_X 全量诊断。
- PCG 当前是“可运行的首轮接入版本”，不是最终关卡设计版本。
- 地图视觉仍需要产品化：正式 tile、过渡边、障碍视觉、hazard 预警、cover 提示、资源点/事件点/NPC 点/Boss 区域表现仍需后续制作。
- 当前 PCG 资源来自示例项目，已验证可以运行；后续正式美术替换时仍需更新 runtime asset catalog 和资源索引。

### 1.8 相机系统

当前相机由 `TotemCameraService` 实现。

已实现内容：

- 2.5D 正交相机。
- orthographic size = 9。
- X 轴倾角约 55 度。
- CombatHUD 进入时激活战斗相机。
- 跟随 actor。
- 根据 400m 地图边界 clamp。
- shake 请求、持续时间、恢复状态可诊断。

已验证行为：

- 原始 focus `(1,2)` clamp 到 `(10,10)`。
- 原始 focus `(999,998)` clamp 到 `(390,390)`。
- 合法目标 `(80,70)` 不被错误 clamp。
- 0.2 / 0.5s shake 请求能产生非零偏移并在结束后恢复。

### 1.9 战斗、武器、技能和状态

当前战斗由 `TotemCombatService` 牵头，武器、技能、状态、VFX、音频分别由独立服务承载。

已实现内容：

- 移动。
- 普通攻击。
- 闪避。
- 技能 E / Q。
- 武器命中。
- 技能命中。
- 伤害结算。
- 死亡事件。
- 重复死亡保护。
- 目标选择跳过死亡目标。
- 武器升级。
- 满级重复武器转换为金币。
- Life Steal。
- 弹道与 projectile-specific VFX。
- 武器 trait 到状态/效果的路由。

当前武器：

- `knife_basic`
- `hammer_heavy`
- `pistol_basic`
- `bow_charge`
- `energy_fist`

当前弹道：

- `bullet_pistol`
- `arrow_bow`

当前状态效果：

| 状态 | 当前合同 |
|---|---|
| Burn | 持续 tick 伤害 |
| Poison | tick 伤害与过期 |
| Shock | 高 DPS / 长 duration 刷新；低 DPS 刷新只延长时长，不降低 DPS |
| Stun | 阻止攻击和闪避 |
| Slow | 降低移动，不造成 tick 伤害 |

当前 `StatusChance` 含义：

- 作为“概率触发状态”的统一入口。
- 由武器 trait、技能、纹身或其它触发源传入。
- 诊断验证其可以路由到实际状态效果，而不是只停留在配置层。

当前 `AfterDodge` 含义：

- “闪避后下一次命中消耗的一次性 buff”。
- 当前放在纹身/触发器链路里验证。

当前边界：

- 战斗链路和数值合同可测。
- 最终手感、hit pause、动画时序、技能视觉节奏还需要人工 playtest 和后续调参。

### 1.10 纹身、自纹身与附魔

当前纹身由 `TotemTattooService` 实现。

已实现内容：

- 6 个部位。
- 7 个颜色。
- 8 个图案。
- 8 个形状。
- 336 个首轮组合合同。
- 读条时长。
- 自纹身读条。
- 自纹身移动/受伤取消。
- 自纹身取消时扣定金。
- actor-scoped tattoo state。
- AI 的纹身状态与玩家全局状态隔离。
- 纹身触发器。
- `AfterDodge` 一次性触发。
- 附魔 affix。
- 附魔 recipe。
- `Clear()` 清空装备、触发器、状态和 actor-scoped 数据。

已验证边界：

- 无效纹身 equip 不改变已有状态。
- 受伤/移动取消会记录取消原因。
- AI actor 的纹身不会误触发玩家纹身。
- 自纹身读条和触发结果能进入诊断报告。

当前边界：

- UI 和图标资源仍为占位。
- 正式纹身图形、纹身视觉组合、纹身动画反馈需要后续美术生产。

### 1.11 AI：Smart AI 五性格与 Light AI

当前 AI 由 `TotemAIService` 实现。

Smart AI 五性格已经接入：

| 性格 | 当前数量 | 行为重点 |
|---|---:|---|
| Aggressive | 5 | 更主动追击可见人形目标 |
| Conservative | 3 | 更保守，远距离目标不轻易追 |
| ResourceAcquisition | 4 | 优先资源、地图拾取、商店购买 |
| BossPriority | 4 | 优先 Boss |
| PlayerPriority | 4 | 优先玩家/人机对象中的人形目标，不强行只锁真实玩家 |

已实现行为：

- Smart AI 根据 `BotConfig`、`BotBuildPreset` 读取性格、权重、反应、攻击冷却、视野、build 倾向。
- Light AI 使用轻量行为：wander、受击短窗口反击。
- Smart AI 可以：
  - 追击目标。
  - 攻击目标。
  - 使用武器。
  - 使用技能。
  - 争抢 death chest。
  - 追逐 MapResource。
  - 消耗地图武器拾取。
  - 追商人。
  - 购买商店 offer。
  - 建立自纹身 build plan。
- Boss AI 使用 Boss phase 技能。
- AI 行为通过 `TotemAIRuntimeDiagnosticScenario` 验证。

当前边界：

- AI 已有“性格差异”和可测试合同。
- 更复杂的战术、组队协作、路径规划、躲避弹道、视野遮挡等属于后续扩展。

### 1.12 NPC、商店、宝箱、三选一和经济

当前经济与交互由以下服务协同：

- `TotemEconomyService`
- `TotemChestService`
- `TotemNpcService`
- `TotemChoiceService`
- `TotemInteractionService`
- `TotemUIService`

已实现内容：

- 商人 NPC。
- 纹身师 NPC。
- NPC 交互范围。
- F 键交互。
- HUD 交互提示。
- 商店价格倍率。
- 商店库存。
- 购买后库存减少。
- 墨水/金币奖励。
- 宝箱生成。
- 宝箱奖励概率。
- death chest。
- 三选一事件。
- 三选一权重。
- 三选一奖励。
- 事件锚点触发。

已验证内容：

- 商店购买能改变库存、金币/墨水状态。
- 宝箱奖励概率按配置汇总。
- 玩家可以 focus 地图事件并打开 `ThreeChoice`。
- 三选一选项数量和数据绑定正常。
- 商人/纹身师使用 map anchor 布点。

### 1.13 Boss

当前 Boss 由 `TotemBossService`、`TotemAIService`、`TotemSkillService`、`TotemVfxService`、`TotemAudioService` 协同实现。

已实现内容：

- Boss actor。
- 3 个 Boss phase。
- phase 1 技能：
  - `skill_stomp`
  - `skill_beam`
- phase 2 技能：
  - `skill_summon`
- phase 3 技能：
  - `skill_enrage_aoe`
- phase BGM cue。
- phase VFX cue。
- phase enrage multiplier。
- Boss 死亡配方奖励。
- BossPriority Smart AI 追 Boss。
- Boss 技能造成伤害并进入 AI/VFX/Audio 诊断。

当前边界：

- Boss 机制合同已接入。
- Boss 动画、技能表现、阶段演出、Boss 战节奏还需要后续正式制作。

### 1.14 音频与 VFX

当前音频由 `TotemAudioService` 管理，VFX 由 `TotemVfxService` 管理。

已实现内容：

- 主菜单 BGM。
- 战斗 BGM。
- Boss 阶段 BGM cue。
- 攻击命中 SFX。
- 击杀 SFX。
- 玩家死亡 SFX。
- 重复音频播放节流。
- 缺失 cue 可见报告。
- 攻击 hit VFX。
- 技能 burst VFX。
- Boss bolt marker。
- pistol bullet trail。
- bow arrow trail。
- VFX 生命周期清理。
- runtime residual cleanup。

当前边界：

- 音频/VFX 触发链路已验证。
- 音效素材、粒子表现、打击反馈仍是后续产品化内容。

### 1.15 资源索引与美术资产管理

当前运行资源索引：

```text
GameData/AIData/GameplayCatalogs/totem_runtime_assets.json
```

AI 资源总索引：

```text
项目知识库（AI自行维护）/wiki/manifests/art_assets.json
```

清理 GF_X DemoGame 后当前资源索引状态：

- 当前索引资源：349。
- runtime asset catalog entry：59。
- runtime-bound asset：49。
- UI form bound asset：12。
- placeholder UI art：113。
- `Assets/Game/Examples` 已不再进入资源索引。
- `Assets/Resources/Sprite/UI` 仍是临时 UI 占位资源。

资源使用规则：

- 运行时资源必须通过 `TotemAssetService` 和 runtime asset catalog。
- UI 表单资源必须通过 `UIFormConfig` / `UITable` / GF_X UI form 生命周期。
- 替换美术时优先保持 runtime key 不变。
- 如果新增资源 key，必须同步：
  - `totem_runtime_assets.json`
  - `art_assets.json`
  - 相关 Business JSON
  - 相关诊断

已确认废弃资源：

- `Assets/Resources/Character`
- `Assets/Resources/Characters`
- `Assets/Resources/Environments`
- `Assets/Resources/Recipes`
- `Assets/Resources/Tattoo`

这些目录不应重新进入活动工程。

### 1.16 自动诊断与测试闭环

当前最新 GF_X 全量诊断：

```text
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260709_172937.json
success=27, failure=0, warning=0
```

当前保留的两个诊断报告：

- `gf-diagnostics-run-all_20260709_172937.json`
- `gf-diagnostics-run-all_20260709_172336.json`

本次 PCG 接入后的关键诊断证据：

- `map.pcg.width=64`
- `map.pcg.height=64`
- `map.pcg.validation=valid=True;walkable=3361;reachable=3361;unreachable=0;warnings=0`
- `map.runtime.pcgMissingSpriteCount.actual=0`
- `map.runtime.legacyGroundObjectCount.actual=0`
- `map.runtime.assetMissingEntryCount.actual=0`
- `map.runtime.assetFallbackRequiredCount.actual=0`
- `forbidden.Assets/Editor=False`
- `runtimeResidue.count=0`

关于“重编译/诊断变慢”的当前结论：

- 真实 C# 脚本编译并不慢，近期 `CompileScripts` 基本为 1-2ms。
- Unity 域重载约 2.3-2.8s，属于当前工程规模下的正常刷新开销。
- 慢感主要来自资源刷新和全量诊断：
  - PCG 首次接入导入了 1329 张 PNG 和较大的 `TerrainTileSetCatalog.json`。
  - GF_X 全量诊断会跑 PCG 生成、地图渲染、战斗初始化、AI、扩展玩法等完整链路。
  - 最新全量诊断耗时约 222s，不应误判为编译失败。
- 已完成性能修正：PCG 地面格从海量 `SpriteRenderer` 对象改为 `Tilemap`，避免运行时生成 4096+ 地面对象导致 UnitySkills 超时。

当前诊断场景：

| 场景 | 作用 |
|---|---|
| `Project layout` | 基础目录布局 |
| `AppConfigs runtime contract` | AppConfigs 启动合同 |
| `Build settings` | BuildSettings 场景检查 |
| `Resource rules` | 资源规则 |
| `AI DataTable json` | AI JSON、xlsx 同步和导表检查 |
| `Registered diagnostic scenarios` | 已注册诊断场景检查 |
| `GF_X Rewrite Inventory Contract` | 重构证据、文档、feature slices、资源/表索引合同 |
| `GF_X Business Runtime Contract` | 业务运行时核心合同 |
| `Clean Workspace Contract` | 干净工作区、旧污染清理、示例删除、missing script |
| `GF_X Dependency Source Contract` | UniTask/DOTween 等依赖来源检查 |
| `Migration Path Contract` | 迁移路径、绝对路径、旧命名污染检查 |
| `Totem AI Runtime` | AI、Boss AI、五性格、资源/商店行为 |
| `Totem Actor Visual Runtime` | actor 视觉资源和 fallback |
| `Totem Audio Runtime` | BGM/SFX/cue/重复播放/缺失 cue |
| `Totem Balance Envelope` | 数值边界与可玩性包络 |
| `Totem Choice Runtime` | 三选一事件 |
| `Totem Extended Gameplay` | 状态、纹身、武器升级、Boss 奖励、边界条件 |
| `Totem First Round Contract` | 首轮规模和内容数量 |
| `Totem First Slice UI` | 12 UI Form 和第一 UI 链 |
| `Totem Gameplay Catalog` | runtime catalog 内容和来源 |
| `Totem Gameplay Runtime` | 地图、相机、战斗、地形、锚点 |
| `Totem Meta Progress` | 新存档/Meta/设置 |
| `Totem Runtime Assets` | 运行时资源、缓存、缺失资源 |
| `Totem Runtime Catalog Binding` | services 和 catalog 绑定 |
| `Totem Runtime Causality Smoke` | 非 UI 因果链、可玩循环 smoke |
| `Totem VFX Runtime` | VFX、弹道、sprite 加载 |
| `Editor snapshot` | 编辑器状态快照 |

常用验证命令：

```powershell
python .claude\skills\unity-skills\scripts\unity_skills.py totem_diagnostics_run_all --port 8092
python tools\ai_index\build_ai_manifests.py --check
cmd /c openspec validate gf-x-business-runtime-refactor --strict
```

### 1.17 工程清理状态

已清理：

- GF_X DemoGame 示例资源和数据。
- 旧 `Assets/Scripts` 活动代码。
- 旧 `Assets/Resources/DataTable` 活动配置表。
- 旧 `Assets/Scenes`。
- 旧 `Assets/Editor`。
- 旧 `Assets/Tools`。
- Unity template `Assets/Readme.asset`。
- `Assets/Screenshots` / `Assets/TestResults`。
- 确认废弃的旧美术目录。
- 旧诊断报告，仅保留最新两个。
- AIData 导表报告，仅保留最新两个。
- AIData xlsx 备份目录。
- 空 `GameData/Configs`。

当前仍保留但不是运行入口：

- `LegacyProjectArchive`：旧代码、旧测试、旧工具、旧表、旧报告证据。
- `Assets/Resources/Prefab/UI`：旧 UI prefab 作为复用/对照资源。
- `Assets/Resources/Sprite/UI`：临时 UI 占位资源。
- `tools/playtest/reports`：少量当前 playtest 证据。

## 2. 项目结构与各文件夹作用

### 2.1 根目录

| 路径 | 作用 | 是否应手动改 |
|---|---|---|
| `.claude` | Claude 工作流源文件、agents、skills、配置。语义源。 | 可以，但要谨慎；改 agent 后需同步 `.codex` |
| `.codex` | Codex 适配镜像、hooks、配置。`.codex/agents/*.toml` 由 `.claude/agents/*.md` 生成。 | 不要直接改 agents 镜像 |
| `.vscode` | IDE 配置。 | 少改 |
| `AB` | GF_X/AB 相关工具或资源构建目录。 | 工具维护时改 |
| `API` | 外部/API 相关内容。 | 按需 |
| `Assets` | Unity 活动资源和代码。 | 核心开发目录 |
| `CompressImageTool` | 图片压缩工具。 | 工具维护时改 |
| `项目知识库（AI自行维护）` | 人类与 AI 共同使用的知识层；生成文档进 `outputs`，稳定知识进 `wiki`，外部原始输入进 `raw`。 | 按目录规则维护 |
| `GameData` | 当前配置表、catalog、诊断报告。 | 配置和诊断核心目录 |
| `LegacyProjectArchive` | 旧工程证据归档。 | 只读为主 |
| `Library` | Unity 自动生成缓存。 | 不要手动改，不提交 |
| `Logs` | Unity/工具日志。 | 不提交或只读 |
| `openspec` | 大中型变更的 spec/proposal/design/tasks。 | 设计/重构变更时维护 |
| `OutPackages` | 外部包输出/Unity Skills 等。当前不应恢复旧 UniTask clone。 | 谨慎 |
| `output` | 临时输出目录。 | 可清理 |
| `Packages` | Unity package manifest 和 lock。 | 包变更时改 |
| `ProjectSettings` | Unity 项目设置。 | 谨慎 |
| `Temp` | Unity 临时目录。 | 不手动改，不提交 |
| `tmp` | 临时工作目录。 | 可清理 |
| `tools` | AI 索引、playtest、同步、图片、Jenkins、MCP 等工程工具。 | 工具开发时改 |
| `UserSettings` | Unity 用户本地设置。 | 不提交 |
| `输出日志` | 本地输出日志。 | 可清理 |
| `项目知识库（AI自行维护）` | AI 生成的项目地图、上下文、manifest 索引。 | 由工具生成，少手改 |

### 2.2 `Assets`

| 路径 | 作用 |
|---|---|
| `Assets/Game` | 当前 GF_X 活动游戏目录，代码、prefab、scene、DataTable、UI 都在这里 |
| `Assets/HybridCLRData` | HybridCLR 数据 |
| `Assets/Plugins` | 插件依赖；当前 UniTask/DOTween 使用 GF_X 版本 |
| `Assets/Resources` | 复用资源目录，旧美术可短期复用，但加载生命周期必须通过 GF_X 服务 |
| `Assets/Settings` | Unity/渲染相关设置 |
| `Assets/Shader` | Shader 资源 |
| `Assets/TextMesh Pro` | TMP 资源 |

### 2.3 `Assets/Game`

| 路径 | 作用 |
|---|---|
| `Assets/Game/Audio` | GF_X 游戏音频资源 |
| `Assets/Game/Config` | GF_X 配置输出目录 |
| `Assets/Game/DataTable` | GF_X Core DataTable 输出，非业务旧表入口 |
| `Assets/Game/Font` | 项目字体和 TMP 字体资源 |
| `Assets/Game/HotfixDlls` | 热更 DLL 相关目录 |
| `Assets/Game/Language` | GF_X 语言输出 |
| `Assets/Game/Materials` | 项目材质 |
| `Assets/Game/Prefabs` | 当前活动 prefab，包括 Core、Entity、UI |
| `Assets/Game/Scene` | 当前活动场景，核心是 `Launch.unity` |
| `Assets/Game/ScriptableAssets` | GF_X ScriptableObject 配置，如 `AppConfigs.asset` |
| `Assets/Game/Scripts` | 当前业务代码。后续游戏功能优先放这里 |
| `Assets/Game/ScriptsBuiltin` | GF_X 框架、编辑器工具、诊断、导表工具。改动需谨慎 |
| `Assets/Game/Shader` | GF_X/项目 shader |
| `Assets/Game/Tests` | 当前测试入口 |

### 2.4 `Assets/Game/Scripts`

| 路径 | 作用 |
|---|---|
| `Common` | 通用常量、工具和基础定义 |
| `Common/Core` | GF_X 常用核心代码 |
| `DataTable/Core` | GF_X Core 表对应 C# row 类型 |
| `Entity` | 实体相关脚本 |
| `Entity/Core` | 实体基类/核心 |
| `EventArgs` | 事件参数 |
| `Extension` | 扩展方法和 GF_X 辅助扩展 |
| `Extension/Animation` | 动画扩展 |
| `Extension/AwaitExtension` | UniTask/await 扩展 |
| `Extension/DataModel` | 数据模型扩展 |
| `Extension/Variable` | GF 变量封装 |
| `Extension/VariablePool` | 变量池 |
| `Network` | 网络基础代码 |
| `Network/Packet` | packet 定义 |
| `Network/PacketHandler` | packet handler |
| `Procedures` | GF_X Procedure，当前含 `WorkspaceProcedure` / `TotemGameProcedure` |
| `Runtime` | Totem runtime、catalog、模型、快照 |
| `Runtime/PCGMap` | 从外部 PCG 示例迁入并适配的 PCG 地图数据、catalog、生成器和验证器 |
| `Runtime/Services` | 26 个 Totem runtime service |
| `ScriptableObject` | `AppConfigs` 等配置对象访问 |
| `UI` | 当前 Totem UI Form 脚本 |
| `UI/Core` | UI 基类、参数、item、对象池封装 |

开发规则：

- 新业务代码优先进入 `Assets/Game/Scripts/Runtime` 或 `Assets/Game/Scripts/UI`。
- 输入只走 `TotemInputService`。
- 不在 `Update` / `LateUpdate` 热路径引入 GC 分配。
- 业务配置不要写死在代码里，优先放 Business JSON。
- 旧代码只去 `LegacyProjectArchive` 查证，不复制旧架构。

### 2.5 `Assets/Game/ScriptsBuiltin`

这是 GF_X 和项目工具的核心区域。

主要内容：

| 子区域 | 作用 |
|---|---|
| Editor 工具 | 导表、AI Data、资源规则、UI prefab 创建、右键工具 |
| Diagnostics | GF_X 全量诊断、业务诊断、清理工具 |
| Playtest | PlayMode 输入模拟和 smoke 测试菜单 |
| ResourceRuleEditor | 资源规则配置 |
| HybridCLR 工具 | 热更 DLL 生成、AOT 拷贝、Obfuz |
| UI 工具 | UIForm inspector、绑定字段、按钮事件工具 |

当前重要菜单：

- `Game Framework/GameTools/Diagnostics/Run All`
- `Game Framework/GameTools/Diagnostics/Cleanup Runtime Residuals`
- `Game Framework/GameTools/Diagnostics/Cleanup Prefab Missing Scripts`
- `Game Framework/GameTools/Open Launch Scene`
- `Game Framework/GameTools/AI Data/...`
- `Tools/Playtest/...`
- `Assets/GF Tools/...`

注意：

- 这里属于框架/工具核心，改动前要明确是否影响通用 GF_X。
- 新业务逻辑不应随便写进 `ScriptsBuiltin`。
- 诊断场景可以放这里，但应保持清晰、可重复、无随机污染。

### 2.6 `GameData`

当前 `GameData` 已清理，只剩必要目录。

| 路径 | 作用 |
|---|---|
| `GameData/AIData/DataTables/Business` | 28 张业务 JSON，AI/程序优先编辑 |
| `GameData/AIData/DataTables/Core` | 5 张 GF_X Core JSON 镜像 |
| `GameData/AIData/GameplayCatalogs` | 运行时 catalog，包含 gameplay 和 runtime assets |
| `GameData/AIData/Reports` | 导表/校验报告，只保留最新两个 |
| `GameData/DataTables/Business` | 28 张业务 xlsx，策划可读 |
| `GameData/DataTables/Core` | 5 张 GF_X Core xlsx |
| `GameData/Diagnostics/Reports` | GF_X 诊断报告，只保留最新两个 |
| `GameData/Languages` | 当前语言表 |

已删除：

- `GameData/Examples`
- `GameData/AIData/Backups`
- `GameData/Configs`

### 2.7 `Assets/Resources`

当前这里仍保留可复用资源，但必须遵守新生命周期。

| 路径 | 作用 |
|---|---|
| `Anim` / `Animation` | 可复用动画资源 |
| `Audio` | 可复用音频 |
| `Effect` | 可复用特效资源 |
| `Font` | 字体资源 |
| `Material` | 材质资源 |
| `Model` | 模型资源 |
| `Obfuz` | 混淆相关资源 |
| `Prefab` | 可复用 prefab，尤其 UI prefab 证据 |
| `PCG` | PCG runtime catalog：地形 tile set、视觉配置、世界对象、区域规则、mask overlay |
| `Sprite/PCG` | PCG 地图图片资源，当前含 Terrain、Objects、POI、Route 等 1329 张 PNG |
| `Sprite` | sprite 资源，当前 UI sprite 多为占位 |
| `Texture` | 纹理资源 |

规则：

- 可以复用资源内容。
- 不复用旧加载方式。
- 新运行时必须通过 `TotemAssetService` / runtime asset catalog。
- UI 必须通过 GF_X UI Form 生命周期。

### 2.8 `LegacyProjectArchive`

旧工程证据归档。

| 路径 | 作用 |
|---|---|
| `LegacyProjectArchive/Assets/Scripts` | 旧业务代码证据 |
| `LegacyProjectArchive/Assets/Scripts/Modules` | 24 个旧模块说明和源证据 |
| `LegacyProjectArchive/Assets/Resources/DataTable` | 旧配置表 JSON 证据 |
| `LegacyProjectArchive/Assets/Scripts/DataTable` | 旧生成 C# 表类型证据 |
| `LegacyProjectArchive/Assets/Tests` | 旧测试证据 |
| `LegacyProjectArchive/tools` | 旧工具/旧 playtest 报告证据 |
| `LegacyProjectArchive/OutPackages` | 旧外部包证据 |

规则：

- 只读为主。
- 用来论证旧功能和需求。
- 不要把旧 runtime 架构复制回来。
- 不要把归档内容移动回活动 `Assets`。

### 2.9 `tools`

| 路径 | 作用 |
|---|---|
| `tools/ai_index` | 生成 AI 知识库和 manifest 的工具 |
| `tools/playtest` | Playtest 报告、测试结果、截图和 smoke 工作流 |
| `tools/sync-agents.py` | 从 `.claude/agents/*.md` 生成 `.codex/agents/*.toml` |
| `tools/codebase-memory-mcp` | codebase-memory MCP |
| `tools/codex-art-gen-*` | 美术生成相关工具 |
| `tools/chroma_key_tool` | 抠图/绿幕工具 |
| `tools/CompressImageTools` | 图片压缩 |
| `tools/ImageCut_Tool` | 切图工具 |
| `tools/PSD2UGUI` | PSD 到 UGUI 工具 |
| `tools/FontMinify` | 字体精简 |
| `tools/LocalizationStringScanner` | 本地化扫描 |
| `tools/Jenkins` | Jenkins 构建工具，大文件需单独处理 |

注意：

- `tools/Jenkins/jenkins.war` 和 `tools/LocalizationStringScanner/LocalizationCodeScanner` 是之前识别出的超过 10MB 文件，提交时应按大文件策略单独处理。

### 2.10 `openspec`

| 路径 | 作用 |
|---|---|
| `openspec/changes/gf-x-business-runtime-refactor` | 当前 GF_X 原生重构主变更 |
| `openspec/changes/gf-x-framework-migration-phase1` | GF_X 框架迁移一期 |
| `openspec/changes/26-fixed-map-three-themes` | 固定 400m / 三主题地图 |
| `openspec/changes/27-ai-information-contract` | AI 信息合同 |
| `openspec/changes/28-pcg-map-runtime-integration` | PCG 地图运行时接入，已完成 T1-T10 并通过 `172937` 全量诊断 |
| `openspec/specs` | 已沉淀的规格文档 |
| `openspec/changes/archive` | 已归档或历史变更 |

规则：

- 大功能、架构、重构、系统设计变化应走 openspec。
- 小修复可以直接做，但要补诊断/文档。
- 当前 `gf-x-business-runtime-refactor` 已作为首轮重构主要证据。

### 2.11 `项目知识库（AI自行维护）`

这是 AI 使用的项目索引和上下文。

重要文件：

| 文件 | 作用 |
|---|---|
| `PROJECT_MAP.md` | 项目地图 |
| `ACTIVE_CONTEXT.md` | 当前上下文 |
| `manifests/art_assets.json` | 美术/资源索引 |
| `manifests/datatables.json` | DataTable 索引 |
| `manifests/feature_slices.json` | 功能切片索引 |
| `manifests/diagnostic_triage.json` | 诊断失败反查索引 |
| `manifests/tests.json` | 测试索引 |
| `manifests/health.json` | 索引健康状态 |

维护方式：

```powershell
python tools\ai_index\build_ai_manifests.py
python tools\ai_index\build_ai_manifests.py --check
```

## 3. 详细待做清单

### 3.1 近期必须由开发者手动确认/执行的事项

这些事项需要人类判断或 Unity 编辑器现场确认，不建议完全交给 AI 静默处理。

#### 3.1.1 Git 与大文件处理

- [ ] 审查本次清理和文档变更。
- [ ] 确认是否把 GF_X DemoGame 示例删除作为一个单独 commit。
- [ ] 确认是否把 `项目知识库（AI自行维护）/wiki/项目总结/PROJECT_STATUS_AND_TODO.md` 作为文档 commit。
- [ ] 大文件超过 10MB 的内容继续单独处理，不要混入普通功能提交：
  - `tools/Jenkins/jenkins.war`
  - `tools/LocalizationStringScanner/LocalizationCodeScanner`
- [ ] 提交前确认 `GameData/Diagnostics/Reports` 只留两个最新报告。
- [ ] 提交前确认 `GameData/AIData/Reports` 只留两个最新报告。
- [ ] 提交前确认 `python tools\ai_index\build_ai_manifests.py --check` 通过。
- [ ] 提交前确认 GF_X 全量诊断 27/0/0。

#### 3.1.2 Unity 编辑器人工验收

- [ ] 打开 Unity，确认当前场景为 `Assets/Game/Scene/Launch.unity`。
- [ ] 进入 Play Mode，从主菜单走到 CombatHUD。
- [ ] 手动确认 UI 没有明显挡住按钮、文字、HUD、交互提示。
- [ ] 手动确认角色选择、启动选择、战斗 HUD 的按钮能正常点击。
- [ ] 手动确认 F 交互、商店、纹身师、三选一事件可理解。
- [ ] 手动确认战斗可玩性：移动、攻击、闪避、技能、受伤、击杀、Boss 压力。
- [ ] 手动确认音效和 VFX 不会造成严重干扰。
- [ ] 手动确认运行时不会在 Hierarchy 留下大量临时对象。

#### 3.1.3 产品方向确认

- [ ] 确认下一阶段优先级：视觉重做、战斗手感、关卡地图、AI、内容扩展、UI polish 哪个先做。
- [ ] 确认正式美术风格文档是否需要单独整理为 art bible。
- [ ] 确认角色帧动画的首批角色数量和优先级。
- [ ] 确认 Boss 首个正式版本的视觉和技能演出目标。
- [ ] 确认地图三主题的视觉基准：AI 废墟、异星巢穴、病毒沼泽。
- [ ] 确认是否保留当前 5 种武器作为第一阶段正式武器池。
- [ ] 确认是否继续以 20 Smart AI + 29 Light AI 的规模做调试基线。
- [ ] 确认正式 UI 信息密度：更像动作 roguelite HUD，还是更像策略/构筑 HUD。

#### 3.1.4 美术资源人工确认

- [ ] 人工确认 `Assets/Resources/Sprite/UI` 中哪些占位图可以短期继续用。
- [ ] 人工确认 349 个资源索引里 `duplicate_name_review` 资源是否需要重命名/合并。
- [ ] 人工确认当前 actor prefab 是否只是临时视觉。
- [ ] 人工确认武器 sprite 是否需要全部重做。
- [ ] 人工确认纹身图案/部位资源是否后续全部 AI 生成。
- [ ] 人工确认正式角色帧动画制作标准：一个方向四帧、四方向、每动作单角色连续处理。

### 3.2 AI 可以继续自动完成的事项

这些事项适合交给 AI 逐步推进，前提是每次大改前先走 openspec 或明确小任务边界。

#### 3.2.1 文档和索引维护

- [ ] 更新 `README.md` 中的编码污染文本，改成可读中文。
- [ ] 更新 `.claude/CLAUDE.md` 中可能残留的历史污染描述。
- [ ] 检查 `项目知识库（AI自行维护）/*.md` 是否仍有 mojibake，并分批修复。
- [x] 更新 `openspec/changes/gf-x-business-runtime-refactor/COMPLETION_AUDIT.md`，当前引用 `gf-diagnostics-run-all_20260709_172937.json`。
- [ ] 更新 `openspec/changes/gf-x-business-runtime-refactor/tasks.md` 中旧报告硬引用说明，避免和“只保留最新两个报告”的策略冲突。
- [ ] 给 `GameData` 加一个简短 README，说明 JSON/xlsx/catalog/report 的关系。
- [ ] 给 `Assets/Game/Scripts/Runtime/Services` 加一份 runtime service map。
- [ ] 给 `tools/playtest` 加一份 PlayMode smoke 使用说明。

#### 3.2.2 配置表工作流增强

- [ ] 把“AI 修改 JSON -> xlsx -> catalog -> 诊断”的命令封装成一个一键脚本。
- [ ] 逆向导表时自动生成报告，并只保留最新 N 个。
- [ ] 逆向导表前自动校验 JSON schema。
- [ ] 逆向导表后自动检查 xlsx 与 JSON 行列一致。
- [ ] catalog 生成后自动写入 source hash。
- [ ] 配置表新增字段时自动提示需要同步的 runtime model 和 diagnostics。
- [ ] 生成更适合 AI 阅读的 DataTable 字段说明文档。
- [ ] 修复/规避 manifest JSON 中由换行字符串导致 PowerShell `ConvertFrom-Json` 解析不稳定的问题。

#### 3.2.3 诊断系统增强

- [ ] 新增“最新报告保留策略”诊断，确保 Reports 目录不会再次膨胀。
- [ ] 新增“示例资源不得复活”诊断，继续守住 `Assets/Game/Examples` 和 `GameData/Examples`。
- [ ] 新增“业务表改动影响面”诊断，从变更表反查 runtime services。
- [ ] 新增“美术资源 runtime key 失配”诊断。
- [ ] 新增“UI 文本溢出/空文本”基础诊断。
- [ ] 新增“PlayMode 长时间 smoke”，模拟 3-5 分钟战斗循环。
- [ ] 新增“Boss phase 全链路 PlayMode smoke”。
- [ ] 新增“商店/NPC/三选一/纹身完整交互 PlayMode smoke”。
- [ ] 新增“AI 行为统计报告”，输出每种性格的目标选择、资源争抢、攻击频率、死亡率。
- [ ] 把失败诊断自动链接到 `diagnostic_triage.json` 对应 feature slice。

#### 3.2.4 Runtime 代码后续重构

- [ ] 按服务拆分更清晰的 model 文件，避免 `TotemGameplayModels.cs` 继续膨胀。
- [ ] 检查所有 runtime tick 是否有 GC 分配。
- [ ] 检查 service 初始化顺序，给依赖关系补清晰注释或诊断。
- [ ] 把 runtime snapshot 输出统一成更适合测试比较的结构。
- [ ] 为 `TotemGameRuntime` 增加更明确的服务依赖声明。
- [ ] 把部分诊断 helper 从 scenario 中抽出，避免测试代码过长。
- [ ] 继续减少硬编码默认 fallback，把可调参数迁到 Business JSON。
- [ ] 为每个 service 写最小职责说明。

#### 3.2.5 UI 后续开发

- [ ] 主菜单视觉重做。
- [ ] 角色选择视觉重做。
- [ ] 启动选择视觉重做。
- [ ] CombatHUD 布局优化。
- [ ] PauseMenu、Settings、RunResult 视觉优化。
- [ ] Shop、TattooStudio、TattooEnchant、SelfTattoo、ThreeChoice 视觉优化。
- [ ] UI 文本本地化 key 整理。
- [ ] UI 音效触发补齐。
- [ ] UI 动效接入 DOTween。
- [ ] 鼠标/键盘/手柄焦点切换策略确认并实现。
- [ ] UI 自动截图验证，至少检查无明显重叠和核心文本存在。

#### 3.2.6 美术资源生产

- [ ] 角色帧动画生产流水线：
  - 单角色连续批处理。
  - 传入角色参考图。
  - 每动作四方向。
  - 每方向四帧。
  - 每张画布只处理同一角色。
  - 统一抠图、切图、命名。
  - 命名格式建议：`{character_id}_{action}_{direction}_{frame:00}.png`。
- [ ] 首批角色资源：
  - 玩家 1。
  - 玩家 2。
  - 玩家 3。
  - Smart AI。
  - Light AI。
  - Boss。
  - 商人。
  - 纹身师。
- [ ] 首批动作：
  - idle。
  - walk/run。
  - attack。
  - dodge。
  - hit。
  - death。
  - skill cast。
- [ ] 武器图标正式化：
  - knife。
  - hammer。
  - pistol。
  - bow。
  - energy fist。
- [ ] 技能图标正式化。
- [ ] 纹身图标正式化。
- [ ] 地图三主题 tile/texture 正式化。
- [ ] Boss phase VFX 正式化。
- [ ] 攻击命中、弹道、技能、死亡、宝箱、资源拾取 VFX 正式化。
- [ ] 每次新增或替换资源后更新：
  - `totem_runtime_assets.json`
  - `art_assets.json`
  - 相关 Business JSON
  - 相关诊断

#### 3.2.7 战斗手感与数值

- [ ] 手动 playtest 当前攻击节奏。
- [ ] 调整近战攻击范围、前摇、后摇。
- [ ] 调整远程武器射速、弹速、射程、散布。
- [ ] 调整闪避距离、无敌帧、冷却。
- [ ] 调整技能伤害、冷却、范围、命中反馈。
- [ ] 调整 Light AI 压力。
- [ ] 调整 Smart AI 追击和资源争抢权重。
- [ ] 调整 Boss phase 阈值、技能频率、伤害。
- [ ] 调整缩圈节奏和圈外伤害。
- [ ] 调整宝箱、商店、三选一奖励曲线。
- [ ] 用 `TotemBalanceEnvelopeDiagnosticScenario` 锁住调参后的安全范围。

#### 3.2.8 地图与关卡

- [ ] 在已接入 PCG 的基础上设计三主题正式房间/区域结构。
- [ ] 替换临时地形视觉。
- [ ] 增加遮挡/cover 的视觉提示。
- [ ] 增加 hazard 的清晰预警。
- [ ] 增加地图边界表现。
- [ ] 增加资源点、事件点、商人点、纹身师点的视觉区分。
- [ ] 增加 Boss 区域表现。
- [x] PCG 地图运行时接入已创建并完成 `openspec/changes/28-pcg-map-runtime-integration`。
- [ ] 后续继续做 PCG 产品化：主题规则调优、关卡节奏、tile 过渡、美术替换、生成耗时优化和长时间 PlayMode smoke。

#### 3.2.9 AI 行为深化

- [ ] Aggressive：增加攻击窗口判断和风险承受。
- [ ] Conservative：增加撤退、绕圈、保命行为。
- [ ] ResourceAcquisition：增加资源价值评估、商店价值评估。
- [ ] BossPriority：增加 Boss 近战/远程安全距离。
- [ ] PlayerPriority：增加目标切换冷却，避免抖动。
- [ ] Light AI：增加群体压迫但不做复杂规划。
- [ ] AI 寻路从直线移动升级到简单避障。
- [ ] AI Debug HUD 或诊断快照显示当前目标、原因、权重。
- [ ] AI 行为调参仍优先从 `BotConfig.json` 和 `BotBuildPreset.json` 开始。

#### 3.2.10 存档与 Meta

- [ ] 设计新存档结构。
- [ ] 确认 Meta progression 的长期目标。
- [ ] 角色解锁规则。
- [ ] 武器/纹身/技能解锁规则。
- [ ] 运行结束奖励结算。
- [ ] Settings 保存路径和回滚策略。
- [ ] Save 数据版本号。
- [ ] Save 损坏 fallback。
- [ ] 注意：不需要兼容旧存档。

#### 3.2.11 构建与发布

- [ ] 确认 HybridCLR 当前是否进入近期目标。
- [ ] 确认 AB 构建流程是否要接入当前资源索引。
- [ ] 梳理 `AB`、`CompressImageTool`、`tools` 的使用方式。
- [ ] Jenkins 大文件处理。
- [ ] Windows 构建 smoke。
- [ ] 构建前自动运行：
  - manifest check。
  - GF_X diagnostics。
  - PlayMode smoke。
  - resource missing check。
- [ ] 输出构建报告。

### 3.3 后续开发推荐顺序

推荐顺序如下：

1. 提交当前清理和总结文档。
2. 修复可读文档中的编码污染，尤其是 `README.md` 和 AI 知识库里被污染的中文。
3. 补一键数据工作流脚本：JSON -> xlsx -> catalog -> manifest -> diagnostics。
4. 做 UI 可读性和布局基础 polish，不追求最终美术。
5. 做 3-5 分钟 PlayMode 战斗 smoke。
6. 做第一轮正式角色/武器/地图占位替换。
7. 做战斗手感调参。
8. 做 Boss 战和 AI 行为深化。
9. 做正式 UI 视觉。
10. 做正式美术批量生产和资源索引稳定。

## 4. 后续开发的标准工作流

### 4.1 改配置

1. 修改 `GameData/AIData/DataTables/Business/*.json`。
2. 运行 JSON 校验。
3. 逆向生成 `GameData/DataTables/Business/*.xlsx`。
4. 重新生成 `totem_gameplay_catalog.json`。
5. 重新生成 AI manifests。
6. 跑 GF_X 全量诊断。
7. 只保留最新两个报告。

### 4.2 改代码

1. 先查 `Assets/Game/Scripts` 当前实现。
2. 需要旧行为证据时再查 `LegacyProjectArchive`。
3. 新业务优先放 `Assets/Game/Scripts/Runtime/Services` 或 `Assets/Game/Scripts/UI`。
4. 不恢复旧框架。
5. 不绕过 `TotemInputService`。
6. 不把配置写死。
7. 给可诊断的行为补 diagnostic。
8. 跑 GF_X 全量诊断。

### 4.3 改美术

1. 先查 `项目知识库（AI自行维护）/wiki/manifests/art_assets.json`。
2. 确认资源当前状态：
   - `runtime_bound`
   - `runtime_bound_placeholder`
   - `ui_form_bound`
   - `placeholder`
   - `reusable_candidate`
   - `duplicate_name_review`
   - `gf_x_core_support`
3. 替换资源时尽量保持 runtime key 不变。
4. 新增资源时同步 `totem_runtime_assets.json`。
5. 重新生成 manifests。
6. 跑资源诊断。

### 4.4 增加功能

1. 如果是大功能，先走 openspec。
2. 明确目标、边界、验收标准。
3. 先补 Business JSON。
4. 再补 runtime service。
5. 再补 UI。
6. 再补资源 key。
7. 最后补诊断。

### 4.5 处理诊断失败

1. 打开最新 `GameData/Diagnostics/Reports/gf-diagnostics-run-all_*.json`。
2. 找失败 item。
3. 查 `项目知识库（AI自行维护）/wiki/manifests/diagnostic_triage.json`。
4. 根据 feature slice 反查：
   - Business 表。
   - runtime service。
   - UI form。
   - runtime asset key。
   - 相关 docs。
5. 修复后重新跑全量诊断。

## 5. 当前特别注意事项

- `Assets/Game/Examples` 和 `GameData/Examples` 已删除，后续不要重建。
- `GameData/AIData/Backups` 已删除，逆向导表如果重新生成备份，需要定期清理。
- `GameData/Diagnostics/Reports` 当前策略是只保留最新两个。
- `GameData/AIData/Reports` 当前策略是只保留最新两个。
- PCG 已接入初始化地图生成链路；最新全量诊断耗时约 222s，主要是完整诊断和 PCG/战斗初始化开销，不是 C# 编译异常。
- PCG 地面渲染必须继续走 `Tilemap`，不要退回每格一个 `SpriteRenderer` 的实现。
- `项目知识库（AI自行维护）/wiki/manifests/*.json` 是生成物，通常通过 `tools/ai_index/build_ai_manifests.py` 更新。
- `.codex/agents/*.toml` 是镜像，不要直接改。
- 所有按键输入必须走 `TotemInputService` / `ITotemInputProvider`。
- 旧代码只能作为证据，不作为运行宿主。
- UI 视觉、正式美术、动画和战斗手感仍是后续重点。

## 6. 当前验收命令

推荐每轮重要修改后至少运行：

```powershell
python tools\ai_index\build_ai_manifests.py --check
python .claude\skills\unity-skills\scripts\unity_skills.py totem_diagnostics_run_all --port 8092
```

涉及 openspec 时运行：

```powershell
cmd /c openspec validate gf-x-business-runtime-refactor --strict
```

需要刷新 Unity AssetDatabase 时：

```powershell
python .claude\skills\unity-skills\scripts\unity_skills.py asset_refresh --port 8092
```

## 7. 一句话交接

当前工程已经是一个干净的 GF_X 原生 Totem Warrior 开发基线：旧框架已归档、DemoGame 示例已删除、首轮旧功能已迁到 26 个 GF_X runtime service、PCG 地图已接入初始化链路、28 张业务表已进入 AI JSON/xlsx/catalog 工作流、资源索引和诊断闭环已建立。下一阶段应围绕 UI/美术/动画/战斗手感/AI 深化/PCG 地图产品化继续推进，并保持“配置先行、服务承载、诊断闭环、旧代码只作证据”的开发纪律。
