# change #20 player-attack-system — 测试计划

| 字段 | 值 |
|---|---|
| 版本 | v1.0 |
| 日期 | 2026-07-01 |
| QA Owner | qa-engineer |
| 覆盖目标 | 单元测试 70% / 集成/playtest 30% |
| 测试环境 | Unity 6.3 LTS, EditMode UTF, PlayMode playtest-driver |

---

## 测试金字塔分布

```
E2E/Playtest (10%)  : TC-Pillar-A
集成 (20%)          : TC-Aim-01~03 (依赖 EntityRef/Physics 桩)
单元 (70%)          : TC-Damage-D1~D9, TC-Crit-01~02, TC-Status-01~05
```

---

## TC-Damage 系列：9 伤害源覆盖

### TC-Damage-D1：玩家普攻（WeaponAttackHitEvent 路径）

| 字段 | 内容 |
|---|---|
| 描述 | 普攻命中后 WeaponAttackHitEvent 携带正确 BaseDamage，HeadPart 可感知 |
| 前置 | 目标 Health=100，武器 BaseDamage=20 |
| 操作 | 构造 WeaponAttackHitEvent(attacker, target, baseDamage=20, "knife")，发布到 EventBus |
| 预期 | target.Health 不由 HeadPartBehavior 直接修改（仅收事件）；事件 BaseDamage==20 |
| 边缘 | baseDamage=0 时事件应发布但不触发暴击（概率检查不跑） |
| 测试类 | HeadPartCritTests |
| 严重度 | P1 |

### TC-Damage-D2：玩家蓄力攻击（WeaponChargedAttackEvent 路径）

| 字段 | 内容 |
|---|---|
| 描述 | 蓄力满档 ChargeRatio=1.0 时伤害 = BaseDamage × 蓄力倍率（由 SkillHitResolver 或 CombatModule 实现） |
| 前置 | InputModule.GetAttackHoldDuration() 返回 ≥ ChargeThreshold(0.4s) |
| 操作 | 构造 WeaponChargedAttackEvent(attacker, target, chargeRatio=1.0f, baseDamage=30, "hammer") |
| 预期 | 事件 ChargeRatio 字段被正确透传；IsCharged 语义成立 |
| 边缘 | chargeRatio=0 时 WeaponAttackHitEvent.IsCharged=false |
| 测试类 | EditMode 集成验证（构造事件对象字段正确性） |
| 严重度 | P2 |

### TC-Damage-D3：玩家技能伤害（SkillActivatedEvent → AttackHitEvent 路径）

| 字段 | 内容 |
|---|---|
| 描述 | SkillHitResolver 收到 SkillActivatedEvent 后对 single 命中目标发布 AttackHitEvent |
| 前置 | SkillConfig 中 skillId="skill_01"，DamageMul=2.0；武器 BaseDamage=15 |
| 操作 | SkillHitResolver（直接 new，注入 mock DataTable + mock WeaponModule）触发 OnSkillActivated |
| 预期 | AttackHitEvent 被发布，BaseDamage ≈ 30（15×2.0）；目标 Health 不被直接修改 |
| 边缘 | DamageMul=0 时 AttackHitEvent 不发布（或 BaseDamage=0 过滤） |
| 测试类 | SkillHitResolver 属于 integration 层，本 TC 用 EventBus 直接订阅验证 |
| 严重度 | P1 |

### TC-Damage-D4：玩家被动加成（PassiveStats.CritRateBonus 影响暴击概率）

| 字段 | 内容 |
|---|---|
| 描述 | HeadPartBehavior.ContributePassive 每 strength=10 贡献 CritRateBonus+=0.05 |
| 前置 | PassiveStats 初始化为零，strength=10，elem=Fire |
| 操作 | headPart.ContributePassive(p, ElementType.Fire, 10f, "Red", "Circle") |
| 预期 | p.CritRateBonus == 0.05f（误差 0.0001f）；p.ElemBonus[Fire] == 0.1f |
| 边缘 | strength=0 时两个值均为 0 |
| 测试类 | HeadPartCritTests |
| 严重度 | P2 |

### TC-Damage-D5：玩家 Buff 伤害（StatusEffectTickedEvent DoT 路径）

| 字段 | 内容 |
|---|---|
| 描述 | StatusEffectModule 每 0.5s tick 对活跃 Burn status 发布 StatusEffectTickedEvent(damage=dps×0.5) |
| 前置 | target 有 Burn(dps=10, duration=2s) |
| 操作 | ApplyStatus → OnUpdate(dt=0.5f) |
| 预期 | StatusEffectTickedEvent 发布，damage==5.0f；第 4 次 tick 后 RemainingSec≤0 发 Expired |
| 边缘 | dps=0 时 damage=0（仍发 tick 事件）；duration 精确到期边界（0.5s tick 粒度） |
| 测试类 | StatusEffectModuleTests |
| 严重度 | P1 |

### TC-Damage-D6：敌人普攻伤害（EnemyAttackEvent → ApplyDamage 路径）

| 字段 | 内容 |
|---|---|
| 描述 | PlayerDamageReceiver 收 EnemyAttackEvent 后扣减 PlayerTarget.Health 并发 DamagedEvent + PlayerHealthChangedEvent |
| 前置 | PlayerTarget.Health=100, MaxHP=100，敌人 Damage=20 |
| 操作 | ApplyDamage(20f, "Enemy") |
| 预期 | PlayerTarget.Health==80；DamagedEvent.Damage==20；PlayerHealthChangedEvent.Delta==-20 |
| 边缘 | Damage=0 时无任何事件发布（ApplyDamage 冒泡守卫 ≤0 return） |
| 测试类 | PlayerDamageReceiverTests |
| 严重度 | P1 |

### TC-Damage-D7：敌人技能伤害（大额伤害 → HP Clamp 至 0）

| 字段 | 内容 |
|---|---|
| 描述 | 敌人技能伤害超过当前 HP 时 HP clamp 到 0，不出现负值 |
| 前置 | PlayerTarget.Health=30, MaxHP=100 |
| 操作 | ApplyDamage(50f, "EnemySkill") |
| 预期 | PlayerTarget.Health==0（不为负）；PlayerHealthChangedEvent.Current==0 |
| 边缘 | 连续两次 ApplyDamage(30) 后 HP 先到 0 再调第二次，第二次不再改变 HP（因为 _diedFired=true 且 _dying） |
| 测试类 | PlayerDamageReceiverTests |
| 严重度 | P1 |

### TC-Damage-D8：状态 DoT 累加（同名 status 合并取 max）

| 字段 | 内容 |
|---|---|
| 描述 | 对同一 target 连续 Apply 两次 Burn，DPS 取 max，duration 取 max（不叠层） |
| 前置 | target 已有 Burn(dps=5, dur=3) |
| 操作 | ApplyStatus(target, "Burn", dps=10, duration=1f) |
| 预期 | GetActiveStatuses(target) 中 Burn 条目仅 1 条；DPS==10；RemainingSec==3（max(1,3)=3） |
| 边缘 | DPS 更低的刷新只改 duration：ApplyStatus(dps=2, dur=5) → DPS 仍==5，RemainingSec==5 |
| 测试类 | StatusEffectModuleTests |
| 严重度 | P1 |

### TC-Damage-D9：暴击伤害（CritHitEvent 路径）

| 字段 | 内容 |
|---|---|
| 描述 | 命中暴击时 HeadPartBehavior 发布 CritHitEvent，BaseDamage = original × CritMultiplier |
| 前置 | Head 槽 PatternMultiplier=1.0（必暴击），CritRateBonus=0，CritMultiplier=1.5；Random.InitState(固定种子使 Random.value<1.0) |
| 操作 | 发布 WeaponAttackHitEvent(baseDamage=20)；HeadPartBehavior 处理 |
| 预期 | CritHitEvent 被发布；BaseDamage≈30（20×1.5） |
| 边缘 | PatternMultiplier=0 时 HeadPartBehavior 不发 CritHitEvent |
| 测试类 | HeadPartCritTests |
| 严重度 | P0 |

---

## TC-Aim 系列：GetAimTarget 三分支

### TC-Aim-01：全方位锁定（AimSpreadHalfDeg ≥ 180°）

| 字段 | 内容 |
|---|---|
| 描述 | AimSpreadHalfDeg=180 时走 FindClosestEnemy 分支，返回距离最近的存活敌人 |
| 前置 | 2 个 mock 敌人：dist=5 和 dist=10，均 Health>0 |
| 操作 | HumanPlayerControllerAimTests 直接测 GetAimTarget 逻辑（纯几何 mock） |
| 预期 | 返回 dist=5 的敌人 Target |
| 边缘 | 所有敌人 Health≤0 时返回 null |
| 测试类 | HumanPlayerControllerAimTests |
| 严重度 | P1 |

### TC-Aim-02：严格 Raycast（AimSpreadHalfDeg ≤ 0.01°）

| 字段 | 内容 |
|---|---|
| 描述 | AimSpreadHalfDeg=0 时走 Raycast 分支，只命中射线方向上的目标 |
| 前置 | 两个目标：一个在正前方 dist=8，一个在侧面 |
| 操作 | 测试分支判定逻辑（aimSpreadHalfDeg <= 0.01f → 返回前方目标） |
| 预期 | 返回正前方 dist=8 目标 |
| 边缘 | 正前方无目标时返回 null（射线无命中） |
| 测试类 | HumanPlayerControllerAimTests |
| 严重度 | P2 |

### TC-Aim-03：半角扇形 score 排序（0.01° < deg < 180°）

| 字段 | 内容 |
|---|---|
| 描述 | 扇形内多个目标时，按 score=(1-dot)*100+dist 取最小，优先正前方近距目标 |
| 前置 | 3 个目标：A(dot=1.0,dist=5,score=5)，B(dot=0.9,dist=3,score=13)，C(dot=0.5,dist=8,score=58) |
| 操作 | 调用评分逻辑，验证 A 被选中（score 最小） |
| 预期 | 返回目标 A |
| 边缘 | 扇形角度边界（cosHalfAngle 恰好等于 dot 的目标）：dot == cosHalfAngle 时应被包含（`<` 判断：dot < cosHalfAngle 才剔除） |
| 测试类 | HumanPlayerControllerAimTests |
| 严重度 | P1 |

---

## TC-Crit 系列：HeadPartBehavior 暴击概率

### TC-Crit-01：无 Head 槽时暴击概率为 0

| 字段 | 内容 |
|---|---|
| 描述 | TattooModule.Equipped 列表中无 Head PartName 时，OnWeaponAttackHit 不发 CritHitEvent |
| 前置 | HeadPartBehavior(bus, mockTattoo)，Equipped 为空列表 |
| 操作 | EventBus 发布 WeaponAttackHitEvent(baseDamage=20)，等待处理 |
| 预期 | 没有 CritHitEvent 被发布（订阅计数=0） |
| 边缘 | Equipped 有 RightArm 但没有 Head 时同样不发 |
| 测试类 | HeadPartCritTests |
| 严重度 | P0 |

### TC-Crit-02：PatternMultiplier × (1 + CritRateBonus) 概率计算

| 字段 | 内容 |
|---|---|
| 描述 | Head 槽 PatternMultiplier=0.5，CritRateBonus=0（critProb=0.5）；固定种子下 Random.value 可预测 |
| 前置 | Random.InitState(0) 使 Random.value 输出已知序列（第一次 > 0.5 则无 crit，< 0.5 则有） |
| 操作 | 多次发 WeaponAttackHitEvent，统计 CritHitEvent 数量 |
| 预期 | 100 次触发中，CritHitEvent 数量在概率范围内（≈50，允许 ±20 误差）；单 PatternMultiplier=1.0 时必暴击 |
| 边缘 | CritRateBonus=1.0 时 critProb = PatternMultiplier×2，上限由 Random.value∈[0,1) 约束 |
| 测试类 | HeadPartCritTests |
| 严重度 | P1 |

---

## TC-Status 系列：5 种状态机

### TC-Status-01：Burn — tick 周期与 DoT 伤害

| 字段 | 内容 |
|---|---|
| 描述 | Burn(dps=10, dur=1.5s)：经 OnUpdate(0.5)×3 后共发 2 次 TickedEvent(damage=5) + 1 次 ExpiredEvent |
| 操作 | ApplyStatus → OnUpdate(0.5)×4（第3次 tick 时 remaining=0.5>0 发 tick；第4次 remaining=-0.5 发 expired） |
| 预期 | TickedEvent 2 次(damage=5)；ExpiredEvent 1 次；GetActiveStatuses 返回空 |
| 边缘 | 初始 dt 不满 0.5 时不发任何 tick |
| 测试类 | StatusEffectModuleTests |
| 严重度 | P1 |

### TC-Status-02：Poison — 持续时间到期清理

| 字段 | 内容 |
|---|---|
| 描述 | Poison(dps=5, dur=2.0s)：到期后 _active 字典中 target 条目被移除 |
| 操作 | ApplyStatus → OnUpdate(0.5)×5 |
| 预期 | 第 4 次 OnUpdate 发 ExpiredEvent；之后 GetActiveStatuses(target) 返回空列表 |
| 边缘 | ClearAllStatuses(target) 立即移除并发 Expired，无需等 tick |
| 测试类 | StatusEffectModuleTests |
| 严重度 | P1 |

### TC-Status-03：Shock — 同名叠加取 max

| 字段 | 内容 |
|---|---|
| 描述 | 对同一 target 先后 ApplyStatus("Shock", dps=8, dur=3) 再 ApplyStatus("Shock", dps=12, dur=1)，合并取 max |
| 预期 | 列表中 Shock 条目 1 条；DPS=12；RemainingSec=3 |
| 边缘 | 再 Apply 一次更低 dps=5, dur=5 → DPS 不变(12)，RemainingSec=5(max(3,5)) |
| 测试类 | StatusEffectModuleTests |
| 严重度 | P1 |

### TC-Status-04：Stun — target 为 null 时防护

| 字段 | 内容 |
|---|---|
| 描述 | ApplyStatus(null, "Stun", ...) 应 Warn 日志并不抛异常、不写入 _active |
| 预期 | _active.Count == 0；无异常抛出 |
| 边缘 | statusName="" 时同样无效；duration=0 时同样跳过 |
| 测试类 | StatusEffectModuleTests |
| 严重度 | P2 |

### TC-Status-05：Slow — ClearAllStatuses 在死亡时清理

| 字段 | 内容 |
|---|---|
| 描述 | target 有 Slow(dps=3, dur=5) 时 ClearAllStatuses(target) 发 ExpiredEvent 并移除条目 |
| 操作 | ApplyStatus → ClearAllStatuses |
| 预期 | StatusEffectExpiredEvent 发布 1 次；GetActiveStatuses(target) 返回空 |
| 边缘 | 多种 status 同时存在时各自发 ExpiredEvent（每种 1 次） |
| 测试类 | StatusEffectModuleTests |
| 严重度 | P2 |

---

## TC-Pillar-A：核心战斗手感（Playtest E2E）

| 字段 | 内容 |
|---|---|
| 描述 | 以 knife 武器（AimSpreadHalfDeg=120°）vs 标准敌人(HP=100)，测试 TTK 与暴击体感 |
| 方法 | playtest-driver E2E（status: pending，本期写场景描述） |
| 评估维度 | TTK / 暴击频率可感知 / 状态效果 HUD 可见性 / 输入响应 |
| 通过条件 | - TTK 在合理范围（knife 4-8 次普攻击杀，≈8-16 秒@1 次/秒节奏） |
|  | - 有 Head 槽时暴击视觉反馈可被观察到 |
|  | - Burn 图标在 DoT 期间持续显示（依赖 change#19 polish，本期仅观察日志） |
|  | - 输入到攻击响应延迟 < 2 帧 |
| 工具 | PlaytestDriverEditor + InputSimulator |
| 报告落点 | tools/playtest/reports/_change20_TC-Damage-Pillar-A.md.tmpl |
| 严重度 | P0 体感 |

---

## 测试用例总览

| ID | 类型 | 覆盖点 | 优先级 |
|---|---|---|---|
| TC-Damage-D1 | 单元 | 玩家普攻 WeaponAttackHitEvent 字段 | P1 |
| TC-Damage-D2 | 单元 | 蓄力攻击 ChargeRatio 透传 | P2 |
| TC-Damage-D3 | 集成 | 技能 SkillActivatedEvent → AttackHitEvent | P1 |
| TC-Damage-D4 | 单元 | 被动 ContributePassive 累加 | P2 |
| TC-Damage-D5 | 单元 | DoT tick 事件 damage 值 | P1 |
| TC-Damage-D6 | 单元 | 敌人普攻 ApplyDamage 扣血 | P1 |
| TC-Damage-D7 | 单元 | 大额伤害 HP clamp 至 0 | P1 |
| TC-Damage-D8 | 单元 | 同名 status 合并取 max | P1 |
| TC-Damage-D9 | 单元 | 暴击 CritHitEvent + 1.5× 伤害 | P0 |
| TC-Aim-01 | 单元 | 全锁定分支 FindClosestEnemy | P1 |
| TC-Aim-02 | 单元 | Raycast 分支边界 | P2 |
| TC-Aim-03 | 单元 | 扇形 score 排序选优 | P1 |
| TC-Crit-01 | 单元 | 无 Head 槽暴击=0 | P0 |
| TC-Crit-02 | 单元 | PatternMultiplier × (1+Bonus) 概率 | P1 |
| TC-Status-01 | 单元 | Burn tick × 2 + Expired | P1 |
| TC-Status-02 | 单元 | Poison 到期清理 _active | P1 |
| TC-Status-03 | 单元 | Shock 同名叠加取 max | P1 |
| TC-Status-04 | 单元 | Stun null 防护 | P2 |
| TC-Status-05 | 单元 | Slow ClearAllStatuses 死亡清理 | P2 |
| TC-Damage-D8b | 单元 | accumulator 低 dps 刷新 duration 分支 | P2 |
| TC-Pillar-A | Playtest E2E | TTK + 暴击体感 + 输入响应 | P0 |

**总计：21 个 TC**（含 D8 边缘拆开计数为 1b）

---

## 风险标记

| 风险 | 来自 ROADMAP | 测试覆盖 |
|---|---|---|
| R3：评分公式 100× 系数是否合适 | §五 R3 | TC-Aim-03 数值验证，playtest 补 |
| R4：49 敌人 + 多 status 性能 | §五 R4 | 本期标记 pending，change#19 收尾后压测 |
| 死亡防抖 300ms 边界 | PlayerDamageReceiver 实现 | TC-Damage-D7 边缘 |
