# Tasks — 20-player-attack-system

> ✅ = 完成；🟡 = 进行中；🔲 = 未开始

## 阶段 1 — 文档骨架（client-lead 本人，已完成）

- [✅] `proposal.md`（Why / What / Scope / DoD / Risk）
- [✅] `design.md`（设计共识表 / 架构图 / 事件流 / 5 个关键设计点 / Agent 编排）
- [✅] `tasks.md`（本文件）
- [✅] `CONTRACT.md`（多模块共享契约——fan-out 圣经）
- [✅] `specs/player-attack-system/spec.md`（9 个 D 伤害源 acceptance + Pillar A 可视化 + 手感指标）
- [✅] `tests/min-plan.md`（TC-D1～D9 + TC-Crit / TC-Charge / TC-Mouse / TC-Startup / TC-Pickup）

## 阶段 2 — 公共骨架代码（client-lead 本人，本阶段产出）

**目标**：编译通过 + 现有 336 TC 不被破坏 + 所有方法体 `// TODO change#20:` 占位。

### 2.1 事件总表（1 个文件）

- [✅] `Assets/Scripts/Events/AttackSystemEvents.cs`
  - `StartupSelectedEvent`（colorId / weaponId / patternIds[]）
  - `WeaponEquippedEvent`（actor / weaponId / weaponPrefabPath）  ← grep 验证无现有定义
  - `StatusEffectAppliedEvent`（target / statusName / dps / duration / source）
  - `StatusEffectTickedEvent`（target / statusName / damage）
  - `StatusEffectExpiredEvent`（target / statusName）
  - `PlayerHealthChangedEvent`（current / max / delta）

### 2.2 新模块空壳（3 个）

- [✅] `Assets/Scripts/Modules/Status/StatusEffectModule.cs`
  - IGameModule + ITickable；ModuleCategory=1；Dependencies=[]
  - public ApplyStatus(Target, string name, float dps, float duration)
  - public ClearAllStatuses(Target)
  - private Dictionary<Target, List<ActiveStatus>> _active = new()
  - struct ActiveStatus { Name, DPS, RemainingSec, Source }
  - [EventHandler] OnEffectApplied(EffectAppliedEvent) → TODO 解析 status 串 → ApplyStatus
  - OnUpdate(dt) → TODO tick 0.5s
- [✅] `Assets/Scripts/Modules/Combat/PlayerDamageReceiver.cs`
  - IGameModule；ModuleCategory=3；Dependencies=[typeof(SpawnerModule)]
  - public float CurrentHP { get; } / float MaxHP { get; }
  - [EventHandler] OnEnemyAttack(EnemyAttackEvent) → TODO 扣 HP + 发 DamagedEvent + PlayerHealthChangedEvent + PlayerDiedEvent
- [✅] `Assets/Scripts/Modules/Skill/SkillHitResolver.cs`
  - IGameModule；ModuleCategory=3；Dependencies=[typeof(WeaponModule), typeof(DataTableModule)]
  - [EventHandler] OnSkillActivated(SkillActivatedEvent) → TODO 按 SkillConfig.DamageMul × WeaponModule.GetBaseDamage 发 AttackHitEvent

### 2.3 新 UI 表单空壳（1 个）

- [✅] `Assets/Scripts/Modules/UI/StartupSelectForm.cs`
  - MonoBehaviour + IUIForm + IUIFormBootstrap
  - 三段 UI：颜料 3 卡片 × 武器 5 卡片 × 图案 2 卡片
  - public OnConfirm() → 发 StartupSelectedEvent
  - GetUnlockedPatternIds()：兜底 [1, 2]，TODO 解析 SaveData.PatternUnlocks

### 2.4 DataTable 骨架

- [✅] 修改 `Assets/Resources/DataTable/WeaponConfig.json` 5 行各加 4 字段 + fields 元数据
- [✅] 新建 `Assets/Resources/DataTable/WeaponTraitConfig.json`（10 行 trait 占位）
- [✅] 修改 `Assets/Scripts/DataTable/WeaponConfig.cs` 加 4 字段（auto-generated 标头一致）
- [✅] 新建 `Assets/Scripts/DataTable/WeaponTraitConfig.cs`（Row + 表 + TryGetById，参考 WeaponConfig.cs 风格）
- [✅] 修改 `Assets/Resources/DataTable/UIFormConfig.json` +1 行 StartupSelectForm（Id=12，SortOrder=30）

### 2.5 改造 6 处 TODO 占位（不实装内部逻辑）

- [✅] `Assets/Scripts/Modules/Combat/CombatModule.cs` L132-142：保留原暴击代码，加 `// TODO change#20:` 注释指明改造方向
- [✅] `Assets/Scripts/Modules/Combat/HumanPlayerController.cs`：在 `GetAimTarget` / `ShouldChargedAttack` 加 TODO 注释
- [✅] `Assets/Scripts/Modules/Input/InputModule.cs`：新增 `IsAttackHolding()` / `GetAttackHoldDuration()` 方法签名（返回 false / 0），加 TODO
- [✅] `Assets/Scripts/Modules/Tattoo/TattooModule.cs`：新增 `[EventHandler] OnWeaponAttackHit(WeaponAttackHitEvent)` / `OnWeaponChargedAttack` 空方法，加 TODO
- [✅] `Assets/Scripts/Modules/Tattoo/Strategies/Parts/HeadPartBehavior.cs`：加 TODO 注释指明改造为 CritHitEvent 源
- [✅] `Assets/Scripts/Modules/Spawner/SpawnerModule.cs`：在 CreateScene 起手 build 处加 TODO「移除硬编码 → 接 StartupSelectedEvent」

### 2.6 GameApp 注册

- [✅] `Assets/Scripts/Core/GameApp.cs`：注册 `StatusEffectModule` / `PlayerDamageReceiver` / `SkillHitResolver` 到 ModuleRunner

### 2.7 编译验证（骨架阶段门禁）

- [🟡] 用户在 Unity Editor 触发编译；用 unity-skills MCP `console_get_logs type=Error` 验证 0 errors
- [🟡] DataTableGenerator 重新生成（WeaponConfig.cs 已手改 / WeaponTraitConfig.cs 已手建 / DataTableRegistry.cs 已手插一行——下次跑生成器应保持等价）
- [📋] 当前文件清单（client-lead 阶段 2 产出）：
  - openspec：`proposal.md` / `design.md` / `tasks.md` / `CONTRACT.md` / `specs/player-attack-system/spec.md` / `tests/min-plan.md`
  - 代码：`Assets/Scripts/Events/AttackSystemEvents.cs`
  - 代码：`Assets/Scripts/Modules/Status/StatusEffectModule.cs`
  - 代码：`Assets/Scripts/Modules/Combat/PlayerDamageReceiver.cs`
  - 代码：`Assets/Scripts/Modules/Skill/SkillHitResolver.cs`
  - 代码：`Assets/Scripts/Modules/UI/StartupSelectForm.cs`
  - 代码：`Assets/Scripts/DataTable/WeaponTraitConfig.cs`
  - 数据：`Assets/Resources/DataTable/WeaponTraitConfig.json`
  - 改造：`Assets/Resources/DataTable/WeaponConfig.json` / `Assets/Scripts/DataTable/WeaponConfig.cs` / `Assets/Scripts/DataTable/DataTableRegistry.cs` / `Assets/Resources/DataTable/UIFormConfig.json`
  - 改造：`Assets/Scripts/Core/GameApp.cs`（+3 模块注册）
  - 改造：CombatModule / HumanPlayerController / InputModule / TattooModule / HeadPartBehavior / SpawnerModule（6 处 TODO 占位）

---

## 阶段 3 — Fan-Out 实现（6+ agent 并行）

**协调约束**：CONTRACT.md 是宪法，所有 agent 不得修改事件签名 / 模块公共方法签名 / DataTable schema / GameApp 注册。

- [🔲] **Agent A（client-unity）**：实装 StatusEffectModule
  - 解析 Result.Status 串「Burn(dps=8,dur=3)」格式
  - tick 0.5s 扣血 + 发 StatusEffectTickedEvent
  - 同名 status 取较高 DPS / 较长 duration
  - 单测：UTF.TC-Status-Burn / TC-Status-Poison
- [🔲] **Agent B（client-unity）**：实装 PlayerDamageReceiver
  - HP 扣减 + 发 DamagedEvent / PlayerHealthChangedEvent / PlayerDiedEvent
  - 死亡阈值 + 一次性触发（防重复）
  - 单测：UTF.TC-Damage-Receive / TC-Player-Dead
- [🔲] **Agent C（client-unity）**：实装 SkillHitResolver
  - SkillConfig.DamageMul × WeaponModule.GetBaseDamage
  - 按 SkillConfig.HitShape 计算命中（single/circle/line/cone）
  - 单测：UTF.TC-SkillHit-Single / TC-SkillHit-Circle
- [🔲] **Agent D（client-unity）**：实装 HumanPlayerController.GetAimTarget
  - 鼠标地面投影 + SphereCast 半角分支
  - 半角 180° fallback 自动锁定（向后兼容）
  - 单测：TC-Mouse 三种半角
- [🔲] **Agent E（client-unity）**：CombatModule + InputModule + HeadPartBehavior 改造
  - InputModule 蓄力 API 实现（Time.unscaledTime 计时）
  - CombatModule 删 25% 暴击 + 调 WeaponModule.FireWeapon
  - HeadPartBehavior 改为 [EventHandler] OnWeaponAttackHit 内按 ColorParam × PatternParam 概率发 CritHitEvent
  - 单测：TC-Crit（仅 Head 装载时出现暴击）；336 TC 必须继续 PASS
- [🔲] **Agent F（client-unity）**：StartupSelectForm + SpawnerModule 改造
  - SelfTattooForm.cs 同款风格 UI（卡片网格 + 高亮选中）
  - SpawnerModule 移除硬编码起手 Build，监听 StartupSelectedEvent 后 Equip + EquipWeapon
  - 单测：TC-Startup
- [🔲] **Agent G（client-unity）**：PlayerWeaponMounter MonoBehaviour
  - 订阅 WeaponEquippedEvent → 卸旧装新 Resources.Load(WeaponPrefabPath)
  - 挂载到玩家身上"WeaponHandPoint"挂点
  - 本期 WeaponPrefab 可为空 GameObject 占位（fallback Warn 不阻塞）
- [🔲] **Agent H（gd-system）**：平衡 WeaponTraitConfig 10 行
  - 5 武器 × 2 trait 占位数值（如 trait_quickslash {EffectType:Quick, P1:0.2} = 普攻后摇减 20%）
  - 写到 `Assets/Resources/DataTable/WeaponTraitConfig.json`
  - 同步 design.md §4.3 trait 接口（如需建 IWeaponTraitBehavior 接口 + TraitModule 也在此 agent 决定）

await UniTask.WhenAll → 汇合后进阶段 4。

---

## 阶段 4 — DataTable 配置完善（gd-system + client-unity）

- [🔲] gd-system 校准 WeaponConfig.AimSpreadHalfDeg 数值（knife=180/hammer=180/pistol=10/bow=5/fist=30）
- [🔲] gd-system 写 WeaponTraitConfig 占位 trait 效果说明（具体公式留后续 change 平衡）
- [🔲] client-unity 在 Unity 菜单跑 `Tools/DataTable/生成全部配置表代码` 验证 WeaponConfig.cs / WeaponTraitConfig.cs 与 JSON 同步

---

## 阶段 5 — 美术（Codex 5 批次，Fan-Out 并行）

> 沿用 #17 codex-image-gen 流程；3 轮重试上限；不阻塞功能验证。

- [🔲] 批次 1：5 武器 prefab 帧序列（每武器 4 帧，共 20 张 sprite sheet）
- [🔲] 批次 2：StartupSelectForm 颜料卡片 × 3（256×384 PNG）
- [🔲] 批次 3：StartupSelectForm 武器卡片 × 5
- [🔲] 批次 4：起手图案卡片 × 2（Line + Ring）
- [🔲] 批次 5：准星 decal × 1（128×128 PNG，居中）
- [🔲] 走 #17 工具菜单 `Tools/Character/Generate Animator from Sprite Folder` 生成 WeaponPrefab + AnimatorController

---

## 阶段 6 — 联调与测试（qa-engineer 主导，≤5 轮 playtest loop）

- [🔲] `tests/min-plan.md` 已建（阶段 1）
- [🔲] `tests/results.md` Round 1～N
- [🔲] `tests/bugs.md` 记录所有 BUG
- [🔲] `loop-state.md` 每轮状态
- [🔲] 退出条件：TC-D1～D9 全 PASS + TC-Crit/Charge/Mouse/Startup PASS + 现有 336 TC PASS + 0 Console Errors

---

## 阶段 7 — 归档

- [🔲] `openspec archive-change 20-player-attack-system`
- [🔲] 更新 `项目知识库（AI自行维护）/INDEX.md`

---

## 阶段 8 — Follow-up（不阻塞归档，列入下期）

- [🔲] **#18 weapon-pickup**（重定义后）：战斗中拾取替换武器
- [🔲] **#19 visual-polish**：每个伤害源独立 VFX 签名
- [🔲] **#21 meta-progression**：图案解锁触发器（首杀 50/Boss 击杀 → SaveData.PatternUnlocks 写入）
- [🔲] WeaponTraitConfig 真实效果实装（gd-system 平衡 → client-unity 实装 IWeaponTraitBehavior）
- [🔲] 武器 prefab 真实动画（武器自带 Animator 4 帧 swing/charge）
