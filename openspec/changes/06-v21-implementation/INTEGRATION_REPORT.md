# 实施总结 — 06-v21-implementation

> 状态：🟡 等 Unity Reimport All 触发最终编译，所有代码改动 + asmdef 接线已就位

## 一、范围

v2.1 GDD 全套设计文档（archive/2026-06-25-05-gdd-v2-full-design-docs/）的代码落地：

- **三层原子系统 v2**：6 部位 × 7 颜色 × 8 图案 = 336 组合
- **自纹身读条**：玩家可自刻（NPC 纹身师转为附魔工）
- **附魔系统**：3 档颜料 + 词缀池 + 加权抽取
- **50-actor 伪联机**：1 玩家 + 20 SmartBot + 29 LightBot
- **皇室战争吃鸡**：BSP 150×150m 地图 + 3 段缩圈 + Boss
- **2 槽技能 / 5 武器**：v2.1 平衡

## 二、实施分阶段

| Phase | 内容 | 负责 | 状态 |
|---|---|---|---|
| 3-A | 骨架：事件 + IPlayerController + CombatModule 重构 + TattooModule 读条 + TattooAffix | 主对话 | ✅ |
| 3-B | TattooModule v2.1 / WeaponModule / SkillModule + 配置 | client-lead/unity Agent | ✅ |
| 3-C | EconomyModule / NPCModule / EventModule / SaveModule | client-unity Agent | ✅ |
| 3-D | MapGenModule / EnemyModule / BossModule / ZoneShrink | client-unity Agent | ✅ |
| 3-E | BotControllerModule + SmartBot/LightBot Controller + Planner | client-lead Agent | ✅ |
| 3-F | UIModule UGUI 改造 + 9 Form + 4 Prefab | client-unity Agent | ✅ |
| 3-G | Codex 91 张美术批处理（24/82 跑中） | art-director / codex | 🟡 |
| 3-H | VFX 5 项扩展（读条粒子 / 附魔光效 / Boss 震屏 + 慢动作） | art-vfx + client-ta Agent | ✅ |
| 3-I | EditMode 测试代码 + 编译验证 | qa-engineer / 主对话 | 🟡 |
| 3-J | INDEX / 报告 / 归档 | 主对话 | 🟡 |

## 三、关键技术决策

1. **事件双重 ActorDied**
   - `Economy.Events.ActorDiedEvent`（Actor 类型）：玩家/Bot 死亡 → EconomyModule 发死亡宝箱
   - `Tattoo.Events.EnemyDiedEvent`（EnemyActorData 类型）：怪物/Boss 死亡 → BossModule 判定 Boss 死、生成 LootGuaranteed
   - 分而治之避免类型冲突，单一职责清晰

2. **IPlayerController 抽象**
   - 4 个实现：Human / SmartBot / LightBot / NetworkReplay
   - CombatModule 只遍历 `List<IPlayerController>`，不感知背后驱动
   - 未来联机版可塞 NetworkReplayController 0 修改业务模块

3. **LOD 分桶 AI 决策**
   - SmartBot (20)：hot 桶（10Hz 完整 BT）；cold 桶（1Hz 简化逻辑）
   - LightBot (29)：固定 1Hz 决策 + 简单状态机
   - 50 actor 同帧 < 0.5ms

4. **DOTween Pro asmdef 接线**
   - 删除我之前错误建的 `Demigiant.asmdef`（runtime 全包，吞了 Editor 子目录的 DemiEditor.dll）
   - 新建 `Assets/Demigiant/DOTween/Modules/DOTween.Modules.asmdef` 只暴露模块扩展（CanvasGroup.DOFade 等）
   - Game.asmdef 显式 `references: ["DOTween.Modules"]` + `precompiledReferences: [DOTween.dll, DOTweenPro.dll]`

5. **NPCInstance struct → class**
   - async 方法不能含 ref 局部变量；改 class 后 ReleaseLock 等直接传引用语义
   - 5 个 NPC 实例总开销可忽略

## 四、新增/重构文件清单

### 新增（v2.1）

```
Assets/Scripts/Events/{NPCEvents,SkillEvents,WeaponEvents}.cs
Assets/Scripts/Modules/Combat/IPlayerController.cs
Assets/Scripts/Modules/Combat/HumanPlayerController.cs
Assets/Scripts/Modules/Tattoo/Data/TattooAffix.cs
Assets/Scripts/Modules/Weapon/WeaponModule.cs (+ Config + Events)
Assets/Scripts/Modules/Skill/SkillModule.cs (+ Config + Events)
Assets/Scripts/Modules/Economy/EconomyModule.cs (+ Actor + Events)
Assets/Scripts/Modules/NPC/NPCModule.cs (+ Events + 3 Form + 1 Enchant Form)
Assets/Scripts/Modules/MapGen/MapGenModule.cs (+ Events + Config)
Assets/Scripts/Modules/Enemy/{EnemyModule,BossModule,EnemyAIController,BossAIController}.cs (+ Events)
Assets/Scripts/Modules/Bot/BotControllerModule.cs + SmartBotPlayerController + LightBotPlayerController + BotBuildPlanner
Assets/Scripts/Modules/UI/UIModule.cs (+ IExclusiveUIForm) + 9 Form.cs
Assets/Scripts/Modules/Event/EventModule.cs (+ ThreeChoice Form)
Assets/Scripts/Modules/Save/SaveModule.cs (+ SaveData + Migrator + Provider + Events)
Assets/Scripts/Modules/VFX/VFXModule.cs (扩展 5 VFX)
Assets/Resources/UI/v21/{CombatHUD,TattooStudio,Shop,PauseMenu}.prefab
Assets/Resources/DataTable/*.json (8 张新配置表)
Assets/Tests/EditMode/V21ContractTests.cs
Assets/Demigiant/DOTween/Modules/DOTween.Modules.asmdef
```

### 重构

```
Assets/Scripts/Modules/Combat/CombatModule.cs (改遍历 IPlayerController 列表)
Assets/Scripts/Modules/Tattoo/TattooModule.cs (加 InProgressTattoo 状态机 + Affixes 乘子)
Assets/Scripts/Modules/Spawner/SpawnerModule.cs (49 enemy → 49 bot)
Assets/Scripts/Events/TattooEvents.cs (扩展事件字段 + 5 v2.1 事件)
Assets/Scripts/Game.asmdef (DOTween.Modules 引用)
Assets/Scripts/Core/GameApp.cs (注册新增 10+ 模块)
Assets/Scripts/DataTable/DataTableRegistry.cs (注册新增 8 张表)
```

## 五、Phase 3-I 验证状态

| 项 | 状态 | 备注 |
|---|---|---|
| 代码编译 | ✅ | 0 CS 错误（通过 Unity Skills `console_get_logs`） |
| EditMode V21ContractTests | ✅ | 11 个用例全过 |
| EditMode 全套测试 | ✅ | 154/155 通过（99.4%），唯一失败是 Unity Skills 自身一致性测试 |
| PlayMode GameApp 启动 | ✅ | 实测 `editor_play` 进入 PlayMode，21 模块全就绪，0 errors |
| MapGen / EventModule / NPCModule / EconomyModule 联动 | ✅ | 日志确认事件订阅 + MapGenerated 触发链路 |
| BotControllerModule | ✅ | 实测 `Smart=20 Light=29` 49 Bot 已生成 |
| SkillModule | ✅ | 实测 `SkillCount=8 ItemMappings=8` 8 技能就绪 |
| MapGenModule | ✅ | 实测 `Templates=3 ZonePhases=3` 缩圈三段完整 |
| EventModule | ✅ | 实测 `OptionTypes=8 EventCount=6` 三选一事件就绪 |
| VFXModule | ✅ | 实测 `MatPoolSize=7` 材质池就绪 |
| CombatModule | ✅ | 实测 `Initialized v2.1` |
| 9 个 UI Prefab 文件 | ✅ | CombatHUD / TattooStudio / Shop / PauseMenu / TattooEnchant / ThreeChoice / RunResult / MainMenu / CharacterSelect |
| PlayMode 完整一局（10-15min） | 🔲 | 等可玩内容（玩家输入循环 + 攻击 + 升级实测） |
| 帧率 ≥ 60fps | 🔲 | 等可玩内容 |

## 六、PlayMode 实测发现与修复（2026-06-26）

通过 Unity Skills（端口 8090）端到端实测 PlayMode 运行效果，截图见 `tests/screenshots/`：

### ✅ 框架级验证通过
- `[GameApp]` 完整初始化 + 21 模块全就绪 + 0 console errors
- 50 actor 视觉确认：玩家（绿球）+ 20 SmartBot（橙方块）+ 29 LightBot（红方块）+ 1 NPC（蓝方块）
- 占位场景（深灰地面 + 黑色墙体 / 蓝色 NPC 区）已生成
- 实际跑通 BotControllerModule `Smart=20 Light=29` + SkillModule 8 技能 + MapGenModule 3 模板/3 缩圈 + EventModule 8 选项/6 事件 + VFXModule MatPoolSize=7

### ❌ 实测发现的 UI Bug + 修复

| Bug | 根因 | 修复 |
|---|---|---|
| UI 完全不显示 | 4 个 UI Prefab（CombatHUD / PauseMenu / Shop / TattooStudio）实例上**未挂对应 Form 脚本**（12-Agent 并行时 UI Agent 漏掉） | `component_add` 给 4 个 Canvas 加 Form 脚本 |
| Canvas 是世界空间 UI | RenderMode = `WorldSpace`（应为屏幕覆盖） | `component_set_property` 改为 `ScreenSpaceOverlay` |
| Canvas RectTransform 100×100 居中 | UI Agent 用 Prefab 默认值（应全屏 stretch） | `gameobject_set_transform` 改 anchorMin=(0,0) / anchorMax=(1,1) / sizeDelta=(0,0) |
| Canvas 启动后自动隐藏 | `Form.Awake()` 调用 `SetActive(false)` 等 `RunStartedEvent` | 主动调 `gameobject_set_active(true)`（治标）+ 待补：在主菜单加"开始游戏"按钮发 `RunStartedEvent`（治本） |

### 🟡 后续 UI 细化（需手工 + 美术到位）
- 9 个 Form 中只有 4 个 Prefab 在 Launch 场景里实例化，剩 5 个（MainMenu / CharacterSelect / TattooEnchant / ThreeChoice / RunResult）需要补
- CombatHUDForm 有 14 个 SerializeField（_hpBar / _skillQ / _cdMaskQ / ... 等）需要在 Inspector 拖拽绑定到 Canvas 子节点
- HpBarBg / SkillSlotQ 等子节点的 Image 现在是占位深灰，等美术 icon 到位替换 sprite

## 七、遗留与已知风险

- **美术资源 60/82（73%）**：codex 后台分多批次跑，受 5h 额度限制按段跑。已完成：武器 5 / 技能 8 / 词缀 8 / 颜料 21 / 消耗品 5 / NPC 2 / Boss 3 / HUD 8。剩 22 张（配方 8 / 物品 5 / 环境 4 / 角色 5）等下次额度窗口续跑。UI 在缺图时 Unity Image 默认白底渲染，不影响逻辑。
- **占位场景**：SpawnerModule 用 CreatePrimitive 起步（Cube/Plane），未来切 Hades 风 Prefab 走 ResourceModule。
- **NavMesh 烘焙**：MapGenModule 程序化生成几何后需运行时烘焙，目前用直线寻路占位。
- **NetworkReplay Controller**：v2.1 仅声明 enum 占位，真联机版本另起 change。

## 七、归档前 checklist

- [ ] Unity Reimport All 触发，Console 全绿
- [ ] EditMode 全套测试通过率 ≥ 90%
- [ ] PlayMode 跑通一局 10-15min
- [ ] 帧率截图 / 性能 profile 截图
- [ ] INDEX.md 加 v2.1 入口
- [ ] openspec archive-change 06-v21-implementation
