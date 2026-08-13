## MODIFIED Requirements

### Requirement: A. 9 种伤害源全部可触发
游戏内 MUST 支持 D1-D9 九类伤害来源；所有伤害 MUST 经统一 CombatRelationship 策略校验，Target 可以是合法 Participant 或 Enemy，不得以 Human/SmartBot/LightBot 控制方式推断敌我。

#### Scenario A.D1 — 武器普攻直伤
- **WHEN** Active Participant 装备 `pistol_basic` 并对合法 Combatant 目标按下攻击
- **THEN** Weapon 攻击 MUST 在 0.3 秒内按 WeaponConfig.BaseDamage 结算一次
- **AND** 关系策略拒绝的目标 MUST 不扣血

#### Scenario A.D2 — 武器蓄力直伤
- **WHEN** Active Participant 使用 `bow_charge` 按住攻击达到阈值后释放
- **THEN** 伤害 MUST 按 `BaseDamage * ChargedMul * chargeRatio` 结算
- **AND** 短于阈值的释放 MUST 回退普通攻击

#### Scenario A.D3 — 刺青形状直伤
- **WHEN** Participant 装备 RightArm 颜色与图案并命中合法目标
- **THEN** Tattoo shape MUST 按既有 336 组合公式结算并发布 EffectApplied 证据

#### Scenario A.D4 — 刺青元素 DoT
- **WHEN** 火元素命中合法目标
- **THEN** Burn MUST 按 0.5 秒 tick 结算并在持续时间后过期
- **AND** 每次 tick MUST 重新经过关系策略

#### Scenario A.D5 — 刺青多段链式范围伤害
- **WHEN** Chain、MultiHit 或 AOEBurst 产生二次目标
- **THEN** 每个二次目标 MUST 分别通过关系策略
- **AND** 合法 Participant 与 Enemy MUST 使用同一伤害入口

#### Scenario A.D6 — 闪避后一次性触发
- **WHEN** Participant 闪避后下一次合法命中发生
- **THEN** AfterDodge 一次性 buff MUST 被消费并应用额外伤害

#### Scenario A.D7 — 技能直伤
- **WHEN** Participant 经 InputModule 使用 E 或 Q 释放已装备技能
- **THEN** Skill damage MUST 通过统一伤害入口命中合法 Combatant

#### Scenario A.D8 — NPC 敌人攻击任意参赛者
- **WHEN** Enemy AI 选择 Human、SmartBot 或 LightBot 中任一 Active Participant 并完成攻击前摇
- **THEN** Enemy Ability MUST 对该 Participant 通过统一伤害入口结算
- **AND** 三种 ControllerKind MUST 使用相同关系、状态、死亡和掉落流程
- **AND** Loading 或 Protected Participant MUST 不可被选择或伤害

#### Scenario A.D9 — 远程弹药耗尽降级
- **WHEN** Participant 的远程武器弹药为 0 并再次攻击
- **THEN** MUST 按配置的 fallback 伤害结算且仍经过关系策略

