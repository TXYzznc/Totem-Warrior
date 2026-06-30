# player-attack-system Specification

## Purpose
TBD - created by archiving change 20-player-attack-system. Update Purpose after archive.
## Requirements
### Requirement: A. 9 种伤害源全部可触发

游戏内必须存在且**只存在** D1～D9 这 9 种伤害源；每种至少有 1 条 acceptance 可观测验证。

#### Scenario A.D1 — 武器普攻直伤
- **前提**：玩家装备 `pistol_basic`（远程），面对 1 名 dummy（HP=100），鼠标指向 dummy
- **操作**：按下鼠标左键 1 次（按下时长 < 0.4s）
- **预期**：
  - WeaponModule 发 `WeaponAttackHitEvent`（target = dummy，damage = WeaponConfig.BaseDamage）
  - dummy.Health 扣减一次（≤ BaseDamage，因为右臂空 → 不走 D3）
  - 从按下到 dummy.Health 变化 < 0.3s（Pillar B "0.3 秒手感"）

#### Scenario A.D2 — 武器蓄力直伤
- **前提**：玩家装备 `bow_charge`（RequiresCharge=true），dummy HP=100
- **操作**：按住鼠标左键 ≥ 0.4s 后释放
- **预期**：
  - WeaponModule 发 `WeaponChargedAttackEvent`（带 chargeRatio ∈ [0,1]）
  - 伤害 ≈ `BaseDamage × ChargedMul × chargeRatio`
  - 按下 < 0.4s 释放 → 不发 ChargedAttack 事件，回退到普攻 D1

#### Scenario A.D3 — 刺青形状直伤
- **前提**：玩家装备 RightArm（红 + Line），命中目标
- **操作**：触发 D1
- **预期**：
  - TattooModule.Fire 内调用对应 IShapeBehavior.Apply（沿用现有 336 TC 公式）
  - 目标承受 `WeaponDamage × ColorMul × PatternMul × SynergyMul × (1 + AffixSum)`
  - 发 `EffectAppliedEvent`，Results 中包含右臂 part 的 EffectResult

#### Scenario A.D4 — 刺青元素 DoT
- **前提**：玩家装备 RightArm（火 + 任意形状），命中 dummy
- **操作**：单次 D1 命中
- **预期**：
  - FireElementBehavior.OnHitExtra 给 dummy.Statuses 添加 "Burn(dps=X,dur=Y)"
  - StatusEffectModule 解析 `EffectAppliedEvent` → ApplyStatus(dummy, "Burn", X, Y)
  - 之后每 0.5s 发 1 次 `StatusEffectTickedEvent`（damage = X × 0.5）
  - 累计 Y 秒后发 `StatusEffectExpiredEvent`

#### Scenario A.D5 — 刺青 Shape 多段/链式/范围
- **前提**：玩家装备 RightArm（任意色 + Chain 或 MultiHit 或 AOEBurst pattern）
- **操作**：D1 命中
- **预期**：现有 IShapeBehavior 行为不变，TC-Chain / TC-MultiHit / TC-AOE 系列 PASS

#### Scenario A.D6 — 左腿延迟打包
- **前提**：玩家装备 LeftLeg（任意色 + Pattern），刚按空格闪避完成
- **操作**：随后 N 秒内触发 D1
- **预期**：TattooModule.ConsumePendingTriggers 释放上一次闪避打包的触发，目标承受额外伤害

#### Scenario A.D7 — 技能直伤
- **前提**：玩家解锁 1 个技能（如 `skill_fireball`），装备好对应武器
- **操作**：按 E 或 Q 释放技能
- **预期**：
  - SkillModule 发 `SkillActivatedEvent`
  - SkillHitResolver.OnSkillActivated 接收 → 按 SkillConfig.DamageMul × WeaponModule.GetBaseDamage(actor) 计算 → 对命中目标发 `AttackHitEvent`
  - TattooModule.OnAttackHit 触发 → 走刺青链

#### Scenario A.D8 — 敌人攻击玩家
- **前提**：1 名 melee enemy 已 spawn，玩家 HP=100
- **操作**：让 enemy 进入攻击距离并触发 AI 攻击
- **预期**：
  - EnemyAIController → 发 `EnemyAttackEvent(attacker, damage)`
  - PlayerDamageReceiver.OnEnemyAttack：扣 HP；发 `DamagedEvent` + `PlayerHealthChangedEvent`
  - 当 HP ≤ 0：发一次性 `PlayerDiedEvent`（不允许重复）→ CombatModule 进入战败结算

#### Scenario A.D9 — 远程弹药耗尽降级
- **前提**：玩家装备 `pistol_basic`（MaxAmmo=12），打到 CurrentAmmo=0
- **操作**：在 ammo=0 状态再次开火
- **预期**：WeaponModule 走 fallback 路径，伤害 = `BaseDamage × 0.4`，并发 `WeaponAttackHitEvent`（不发 Charged）

---

### Requirement B. Pillar A 构筑可见性

#### Scenario B.1 — 同 Build 攻击与裸装攻击画面可区分
- **前提**：A 玩家裸装 / B 玩家装 RightArm（火 + Chain），其他完全相同
- **操作**：双方各发 1 次 D1 命中相同 dummy
- **预期**：
  - A：仅发 `WeaponAttackHitEvent`，无 `EffectAppliedEvent`，无 `StatusEffectAppliedEvent`
  - B：发 `WeaponAttackHitEvent` + `EffectAppliedEvent`（右臂 part）+ `StatusEffectAppliedEvent`（Burn）+ 后续 `StatusEffectTickedEvent`
  - **可观测事件数差 ≥ 3**（保证 Build 在 EventBus 层面已可见，VFX 表现层留 #19）

#### Scenario B.2 — 起手三选确认进入战斗
- **前提**：玩家在 `StartupSelectForm`
- **操作**：选 1 颜料 + 1 武器 + 1 图案 → 点 OnConfirm
- **预期**：
  - 发 `StartupSelectedEvent(colorId, weaponId, patternIds[])`
  - SpawnerModule 调 `TattooModule.Equip(part=RightArm, color, pattern)` + `WeaponModule.EquipWeapon(player, weaponId)`
  - WeaponModule 发 `WeaponEquippedEvent`（带 prefabPath）
  - StartupSelectForm 自闭

---

### Requirement C. Pillar C 伤害可归因（暴击只来自 Head）

#### Scenario C.1 — 不装 Head 时无暴击
- **前提**：玩家未装 Head 槽
- **操作**：连续触发 100 次 D1
- **预期**：CritHitEvent 触发次数 = 0；CombatModule 内已删 25% 硬编码（或保留但走 TODO 占位 → fan-out 阶段 E 删除）

#### Scenario C.2 — 装 Head 时按 Pattern 概率出暴击
- **前提**：玩家装 Head（任意色 + ProbBurst pattern，PatternParam.CritProb = 0.25）
- **操作**：触发 100 次 D1
- **预期**：CritHitEvent 触发次数 ∈ [15, 35]（25% ± 10% 容差）；每次都伴随 EffectAppliedEvent（Head 槽生效）

---

### Requirement D. 蓄力 API（D2 时序）

#### Scenario D.1 — 短按不算蓄力
- **前提**：装备 `bow_charge`
- **操作**：按下 → 0.2s 后释放
- **预期**：不发 `WeaponChargedAttackEvent`，按 `IsAttackPressed()` 旧路径仍发 `InputAttackEvent`

#### Scenario D.2 — 长按满足阈值才算蓄力
- **前提**：装备 `bow_charge`
- **操作**：按下 → 0.5s 后释放
- **预期**：
  - InputModule.IsAttackHolding() 期间返回 true，GetAttackHoldDuration() 单调递增
  - 释放时 chargeRatio = clamp((0.5 - 0.4) / window, 0, 1)，发 `WeaponChargedAttackEvent`

---

### Requirement E. 鼠标地面投影 + SphereCast 半角

#### Scenario E.1 — 半角 0°（pistol_basic）→ Raycast 严格
- **前提**：WeaponConfig[pistol_basic].AimSpreadHalfDeg = 10（接近 0）；3 名敌人 spread 在 45° 范围内
- **操作**：鼠标指向敌人 A（精确瞄准）
- **预期**：只命中 A；偏离 ≥10° 不命中

#### Scenario E.2 — 半角 30°（knife/hammer 近战）→ 锥形容错
- **前提**：AimSpreadHalfDeg = 30；敌人 A 在鼠标方向 ±25°
- **操作**：开火
- **预期**：A 被 SphereCast 命中

#### Scenario E.3 — 半角 180°（fist / 占位）→ 自动锁定最近敌
- **前提**：AimSpreadHalfDeg = 180
- **操作**：开火（鼠标方向无关）
- **预期**：回退到原 `FindClosestEnemy()` 行为，向后兼容 336 TC

---

### Requirement F. StatusEffectModule tick 性能

#### Scenario F.1 — tick 频率
- **前提**：StatusEffectModule 加载，1 名玩家 + 50 名 dummy 全部带 1 个 Burn 状态
- **操作**：运行 2 秒
- **预期**：
  - 每 0.5s 触发 1 次全量遍历
  - StatusEffectTickedEvent 总数 = 50 × 4 = 200 ± 2
  - OnUpdate 单帧 alloc = 0（用预分配 List + struct）

#### Scenario F.2 — 同名 status 合并策略
- **前提**：dummy 已有 "Burn(dps=5, dur=2)"
- **操作**：再次 ApplyStatus("Burn", dps=8, dur=3)
- **预期**：合并后内存中只有 1 个 Burn，DPS=8（较高），RemainingSec=3（较长）

---

### Requirement G. CONTRACT 接口不变性

#### Scenario G.1 — 现有 336 TC 全部 PASS
- **前提**：本 change 落地后
- **操作**：运行 Unity Test Runner（EditMode + PlayMode）
- **预期**：原有 336 TC 全部 PASS（TattooModule.Fire 公式不重写、CritHitEvent 签名不变、AttackHitEvent 字段不变）

#### Scenario G.2 — fan-out agent 不改公共契约
- **前提**：阶段 3 任一 agent 完成代码
- **操作**：diff 检查 `AttackSystemEvents.cs` / `GameApp.cs` / WeaponConfig schema / 模块公共方法签名
- **预期**：以上文件签名与骨架阶段 byte-for-byte 一致（fan-out 只填实现，不改契约）

---

