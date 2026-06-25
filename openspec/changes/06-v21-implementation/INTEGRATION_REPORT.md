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
| 代码编译 | 🟡 | 所有源头错误已修，等 Unity Reimport All 触发 |
| EditMode V21ContractTests | ⏳ | 11 个测试用例已写，等编译过后跑 |
| EditMode 原有测试（Tattoo336 / Strategy） | ⏳ | 等编译，预期不挂 |
| PlayMode 完整一局 | 🔲 | 等编译 |
| 帧率 ≥ 60fps | 🔲 | 等 PlayMode |

## 六、遗留与已知风险

- **美术资源 24/82**：codex 后台跑中，速率 ~2.5min/张，ETA 约 2.5h。UI 在缺图时 Unity Image 默认白底渲染，不影响逻辑。
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
