# Test Min-Plan — 20-player-attack-system

> qa-engineer 在阶段 6 按本计划跑 ≤5 轮 playtest loop；本文件仅覆盖**新增 / 受影响**的 TC。
> 现有 336 TC 由 Unity Test Runner 全量回归，本计划不重复列出。

---

## 一、TC 索引

| TC ID | 类型 | 涉及伤害源 / 系统 | 触发方式 |
|---|---|---|---|
| TC-D1 | EditMode | D1 武器普攻直伤 | 模拟 InputAttack |
| TC-D2 | EditMode | D2 蓄力 | 模拟 InputAttack hold ≥0.4s |
| TC-D3 | EditMode | D3 形状直伤 | 现有 TC 子集（确认未破坏） |
| TC-D4 | PlayMode | D4 元素 DoT | 装 Fire RightArm 命中后等 1.5s 看血条 |
| TC-D5 | EditMode | D5 多段/链式 | 现有 TC 子集 |
| TC-D6 | EditMode | D6 左腿延迟打包 | 现有 TC 子集 |
| TC-D7 | PlayMode | D7 技能直伤 | 按 E 触发 |
| TC-D8 | PlayMode | D8 敌人攻击玩家 | spawn melee enemy |
| TC-D9 | EditMode | D9 弹药耗尽降级 | 现有 TC 子集 |
| TC-Crit | PlayMode | 暴击改 Head 触发 | 装 / 不装 Head 对比 |
| TC-Charge | PlayMode | 蓄力时序 | 短按 vs 长按 |
| TC-Mouse | PlayMode | 鼠标地面投影 + SphereCast | 三种半角 |
| TC-Startup | PlayMode | 起手三选 UI | 完整三选 → 进入战斗 |
| TC-Pickup | placeholder | 战斗中拾取替换武器 | 本期不验，留 #18 |
| TC-Regression-336 | EditMode + PlayMode | 现有 336 TC | Test Runner 全量 |

---

## 二、TC 详细

### TC-D1（EditMode） — 武器普攻直伤

**前置**：
- 装备 `pistol_basic`（BaseDamage=10）
- spawn 1 个 dummy（HP=100）
- 右臂槽 = 空（避免 D3 干扰）

**步骤**：
1. 通过 `IInputSimulator` 模拟 `IsAttackPressed=true` 一帧
2. 等 1 帧 OnUpdate

**Assertion**：
- 收到 1 次 `WeaponAttackHitEvent`（target=dummy）
- dummy.Health = 90
- 未收到 `WeaponChargedAttackEvent`
- 未收到 `EffectAppliedEvent`
- 整个流程从 input 到 health 变化在同帧或下一帧完成

---

### TC-D2（EditMode） — 蓄力直伤

**前置**：装备 `bow_charge`（BaseDamage=15，ChargedMul=2.0），dummy HP=100

**步骤**：
1. `IsAttackHolding=true` 持续 0.5s（模拟 30 帧 @ 60fps）
2. 第 31 帧释放

**Assertion**：
- 收到 1 次 `WeaponChargedAttackEvent`（chargeRatio ∈ [0.2, 1.0]）
- dummy.Health 扣减 ≈ `15 × 2.0 × chargeRatio`
- 未收到 `WeaponAttackHitEvent`（避免双发）

---

### TC-D4（PlayMode） — 元素 DoT

**前置**：装备 RightArm（火 + Line），dummy HP=100

**步骤**：
1. 触发 1 次 D1 命中
2. 等 2.0s（4 个 tick）

**Assertion**：
- 命中后 dummy.Statuses 包含 "Burn"
- 收到 1 次 `StatusEffectAppliedEvent`（statusName="Burn", dps>0）
- 2s 内收到 4 次 `StatusEffectTickedEvent`（每 0.5s 1 次，± 1 帧容差）
- dummy.Health 持续下降
- duration 到期后收到 1 次 `StatusEffectExpiredEvent`

---

### TC-D7（PlayMode） — 技能直伤

**前置**：装备 `pistol_basic`，左臂槽空，玩家已解锁 `skill_fireball`（DamageMul=3.0）

**步骤**：
1. 按 E 释放技能
2. 等技能 Active 帧

**Assertion**：
- 收到 1 次 `SkillCastEvent`
- 收到 1 次 `SkillActivatedEvent`
- SkillHitResolver 处理后对命中目标发 1+ 次 `AttackHitEvent`，BaseDamage ≈ `10 × 3.0 = 30`
- TattooModule.Fire 被调用（左臂空也走，因为 D7 走整体刺青链）

---

### TC-D8（PlayMode） — 敌人攻击玩家

**前置**：玩家 HP=100，spawn 1 名 melee enemy（BaseDamage=20）紧贴玩家

**步骤**：
1. 等 enemy AI 进入攻击循环
2. 等到 enemy 发起一次攻击

**Assertion**：
- 收到 1 次 `EnemyAttackEvent`
- PlayerDamageReceiver 接收 → 玩家 HP = 80
- 收到 1 次 `DamagedEvent`（damage=20, newHp=80, maxHp=100）
- 收到 1 次 `PlayerHealthChangedEvent`（current=80, max=100, delta=-20）
- 反复攻击直到 HP ≤ 0 → 收到**仅 1 次** `PlayerDiedEvent`（重复攻击不再触发）

---

### TC-Crit（PlayMode） — 暴击改 Head 触发

**子用例 A：不装 Head**
- 装备空 Head 槽，连续触发 D1 × 100 次
- **Assertion**：CritHitEvent 次数 = 0

**子用例 B：装 Head + ProbBurst pattern（CritProb=0.25）**
- 连续触发 D1 × 100 次
- **Assertion**：CritHitEvent 次数 ∈ [15, 35]（95% 置信区间）

**子用例 C：现有 336 TC 不破**
- 跑 TattooModule 全套 TC
- **Assertion**：全部 PASS（HeadPartBehavior 改造仅加 [EventHandler]，不动 ContributePassive）

---

### TC-Charge（PlayMode） — 蓄力时序

**子用例 A：短按**
- 装 `bow_charge`，按 0.2s 释放
- **Assertion**：不发 `WeaponChargedAttackEvent`；发 `InputAttackEvent`（走 D1 普攻）

**子用例 B：长按**
- 按 0.5s 释放
- **Assertion**：发 `WeaponChargedAttackEvent`，不发 `InputAttackEvent`

**子用例 C：极长按**
- 按 2.0s 释放
- **Assertion**：chargeRatio = 1.0（封顶），不发多次

---

### TC-Mouse（PlayMode） — 鼠标地面投影 + SphereCast 半角

**子用例 A：AimSpreadHalfDeg = 0**
- pistol_basic，3 名敌人散布在 ±45°
- 鼠标精确指向敌 A
- **Assertion**：只击中 A；偏离 ≥1° 不击中

**子用例 B：AimSpreadHalfDeg = 30**
- knife_basic，敌 A 在鼠标 ±25°
- **Assertion**：A 被 SphereCast 命中

**子用例 C：AimSpreadHalfDeg = 180**
- 任意武器配置半角=180
- 鼠标不指向任何敌
- **Assertion**：回退 FindClosestEnemy，仍能命中最近敌（向后兼容）

---

### TC-Startup（PlayMode） — 起手三选 UI

**前置**：游戏从 MainMenu 进入 → CharacterSelect → 选完角色 → **StartupSelectForm 自动打开**

**步骤**：
1. 选颜料 = 红
2. 选武器 = pistol_basic
3. 选图案 = Line
4. 点 OnConfirm

**Assertion**：
- 收到 1 次 `StartupSelectedEvent(colorId=红, weaponId="pistol_basic", patternIds=[Line])`
- SpawnerModule 接收后：
  - TattooModule 中 RightArm 已 Equip 红+Line
  - WeaponModule 中玩家武器 = pistol_basic
- 收到 1 次 `WeaponEquippedEvent`（actor=player, weaponId=pistol_basic, prefabPath 非空）
- StartupSelectForm 关闭 (gameObject.SetActive(false))
- CombatHUDForm 出现 → 战斗开始

---

### TC-Pickup（占位） — 战斗中拾取替换武器

本期**不验**。记录原因：已拆分到 #18 weapon-pickup change。
Round 1 跑通后在 results.md 标 `SKIP - moved to #18`。

---

### TC-Regression-336（EditMode + PlayMode） — 现有 TC 全量回归

**步骤**：
1. Unity 菜单 Window → General → Test Runner
2. EditMode + PlayMode 全选 → Run All

**Assertion**：
- 336 个原有 TC 全部 PASS（0 fail / 0 error / 0 skip）
- 任一失败立即回退本次 change 的 HeadPartBehavior / CombatModule 改造

---

## 三、退出条件（与 proposal.DoD §1-3 一致）

阶段 6 结束需同时满足：

- ☐ TC-D1 ~ TC-D9 全部 PASS（9/9）
- ☐ TC-Crit / TC-Charge / TC-Mouse / TC-Startup 全部 PASS（4/4）
- ☐ TC-Regression-336 = 336/336 PASS
- ☐ TC-Pickup 标 SKIP
- ☐ Console Errors = 0（Round 最后一次完整跑无任何 Error / Exception）
- ☐ Pillar B 手感：左键按下到 dummy 血条变化 < 0.3s（人工观察 5 次，每次 < 300ms）

---

## 四、Loop 协议

- 单轮 ≤ 90 分钟（含 fix + retest）
- 同一 TC 连续 5 轮未通过 → 立即停 loop 交回主对话
- 每轮在 `tests/results.md` 追加 Round N 段；bug 在 `tests/bugs.md` 编号记录
