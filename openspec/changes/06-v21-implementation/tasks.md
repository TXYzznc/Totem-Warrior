# Tasks — 06-v21-implementation

> 进度。✅ = 完成；🟡 = 进行中；🔲 = 未开始。

## Phase 3-A — 骨架（主对话，必须先做完）

- [x] Assets/Scripts/Events/TattooEvents.cs 加 5 个 v2.1 新事件（RequestSelfTattooEvent / TattooInProgressEvent / TattooFinishedEvent / TattooCancelledEvent / TattooEnchantedEvent）✅
- [x] Assets/Scripts/Events/{NPCEvents,SkillEvents,WeaponEvents}.cs（新建）+ Modules/.../{Economy,MapGen,Save,Event}/*Events.cs 合计 30+ 个 v2.1 事件类 ✅
- [x] Assets/Scripts/Modules/Combat/IPlayerController.cs（新建）核心抽象 ✅
- [x] Assets/Scripts/Modules/Combat/HumanPlayerController.cs（新建）接 InputModule ✅
- [x] CombatModule.cs 重构为遍历 IPlayerController 列表（消费意图，不再直接轮询 InputModule）✅
- [x] SpawnerModule.cs 改 20 SmartBot 占位 + 29 LightBot 占位 + 1 玩家（49 enemy 数 → BotControllerModule 注入）✅
- [🟡] 编译通过 + EditMode 测试不挂 → 已修 30+ 个编译错误，等 Unity Reimport All

## Phase 3-B — 核心模块 v2.1（并行）

- [x] TattooModule 加 InProgressTattoo 状态机（client-lead）✅
- [x] TattooReadingTimeConfig.json + 生成 cs ✅
- [x] WeaponModule 新建（client-unity）含 5 武器 + Projectile / Hitbox ✅
- [x] SkillModule 新建 MaxSlot=2（client-unity）✅
- [x] TattooEnchantAffixConfig.json + TattooEnchantRecipeConfig.json + 生成 cs ✅

## Phase 3-C — 装备与经济（并行）

- [x] EconomyModule 新建（颜料三档 / 死亡宝箱半半规则）✅
- [x] NPCModule 新建（纹身师=附魔工 / 商人 4 类货）✅
- [x] EventModule 新建（事件房 / 三选一）✅
- [x] SaveModule 新建（局外解锁 9 角色位 + 6 配方位 + 装饰）✅

## Phase 3-D — 地图与战斗（并行）

- [x] MapGenModule 新建（BSP 150×150m + NavMesh）✅
- [x] EnemyModule + BossModule 新建（含 GuaranteedLootIds / DeathPatternRecipeId）✅
- [x] ZoneShrinkConfig.json 三段缩圈 ✅

## Phase 3-E — AI 大脑

- [x] BotControllerModule 新建（含 SmartBot/LightBot 两种 controller + BotBuildPlanner）✅

## Phase 3-F — UI 全 UGUI 改造（并行）

- [x] UIModule 改造为 UGUI 注册器 + IUIForm UGUI 版 ✅
- [x] 9 个 Form prefab + Form.cs：CombatHUD / TattooStudio / TattooEnchant / Shop / ThreeChoice / PauseMenu / RunResult / MainMenu / CharacterSelect ✅
- [x] 删除 CombatHUD.uxml / uss / UI Toolkit CombatHUDForm.cs ✅

## Phase 3-G — 美术资源（并行，后台跑）

- [🟡] Codex 批量出图：词缀图标 16 / 武器 5 / 技能 8 / 怪物 / Boss 3 / NPC 2 / 角色 / 场景 / UI = 91 张总共（当前 24/82）
- [x] Hades 风占位场景：1 主题（AI 废墟）地面 + 墙体 + 灯光 — SpawnerModule.cs CreatePrimitive 实现 ✅
- [x] 角色 prefab（玩家 + Bot 共用骨架）— SpawnerModule 内置 ✅

## Phase 3-H — VFX 升级

- [x] 自纹身读条粒子（圆环聚拢 + 部位光斑）✅
- [x] 附魔光效（词缀注入闪光）✅
- [x] Boss 进场震屏 + 慢动作 ✅

## Phase 3-I — 集成与测试

- [🟡] 编译通过（等 Unity Reimport All）
- [🟡] EditMode 测试代码就绪（PlayerControllerTests / EconomyDeathChestTests / TattooSelfReadingTests）
- [🔲] PlayMode 完整一局测试（10-15min，从主菜单到结算）
- [🔲] 帧率 ≥ 60fps（50 actor + 32 VFX）

## Phase 3-J — 归档

- [🔲] 同步 INDEX
- [🔲] 写实施总结 INTEGRATION_REPORT.md
- [🔲] archive-change
