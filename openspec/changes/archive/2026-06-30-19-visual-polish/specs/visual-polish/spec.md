# Spec Delta — visual-polish

## ADDED Requirements

### Requirement: 暴击数字飘字（TC-Polish-01）

系统 MUST 在 `WeaponAttackHitEvent` 与 `CritHitEvent` 触发时 spawn 世界空间 TMP 飘字：普通白色 #FFFFFF 字号 24，暴击红色 #FF2222 字号 36 + Scale 弹出动画。对象池容量 8，第 9 个 reuse 最旧。

#### Scenario: 普通命中显示白色飘字
- **GIVEN** 玩家普攻命中敌人，IsCrit=false
- **WHEN** WeaponAttackHitEvent publish
- **THEN** DamageFloatTextBehaviour spawn TMP（color=#FFFFFF, fontSize=24），DOTween 0.6s 上浮 + 淡出

#### Scenario: 暴击命中显示红色飘字
- **GIVEN** Pillar C Head pattern 概率触发
- **WHEN** CritHitEvent publish
- **THEN** spawn TMP（color=#FF2222, fontSize=36），Scale 1.4→1.0（0.15s）→ 上浮 120px → 0.8s 淡出

#### Scenario: 对象池上限不溢出
- **GIVEN** 连续 10 次命中（< 0.6s 间隔）
- **THEN** 同时存在不超过 8 个飘字，第 9 个复用最旧实例

---

### Requirement: 头顶状态图标（TC-Polish-02）

`StatusIconController` MUST 订阅 `StatusEffectAppliedEvent` / `StatusEffectExpiredEvent`，按 TargetId 过滤后在 Actor 头顶 (0, 1.5, 0) World Canvas 显示对应 sprite（`Resources/Sprite/UI/StatusIcon/{burn,poison,stun}.png`），出现/消失带 CanvasGroup DOFade + Scale 弹出/收缩。

#### Scenario: 给敌人施加 burn 显示头顶图标
- **GIVEN** EnemyActor 已挂 StatusIconController + SetTarget(target)
- **WHEN** StatusEffectAppliedEvent(target, "burn", duration=5s)
- **THEN** 头顶 burn 图标 FadeIn + ScalePop（0.15s）

#### Scenario: 状态到期图标消失
- **GIVEN** burn 已显示
- **WHEN** StatusEffectExpiredEvent(target, "burn")
- **THEN** FadeOut + ScaleShrink（0.2s）→ Hide

#### Scenario: TargetId 不匹配的事件被过滤
- **GIVEN** Actor A 挂 StatusIconController(targetA)
- **WHEN** StatusEffectAppliedEvent(targetB, "burn") publish
- **THEN** Actor A 头顶不出现 burn

---

### Requirement: hitspark 粒子 + 镜头抖动（TC-Polish-03）

`HitsparkBehaviour` MUST 订阅 `WeaponAttackHitEvent`，从对象池取 ParticleSystem 在 HitPoint 播放（缺 prefab 时 fallback placeholder GO 不崩）。`CameraShakeBehaviour` MUST DOShakePosition(Camera.main)，新 shake 到来前 Kill 旧 tween 防叠加。

#### Scenario: 普通命中触发粒子 + 轻抖
- **GIVEN** WeaponAttackHitEvent(hitPoint=P, isCrit=false)
- **THEN** Hitspark Burst 8 粒子，duration 0.3s；Camera DOShakePosition(amplitude=0.05, duration=0.12)

#### Scenario: 暴击命中加强表现
- **GIVEN** WeaponAttackHitEvent(isCrit=true)
- **THEN** Hitspark Burst 14 粒子；Camera DOShakePosition(amplitude=0.1, duration=0.18)

#### Scenario: 连续暴击不叠加抖动
- **GIVEN** 0.1s 内连续 3 次暴击
- **THEN** 旧 shake tween Kill，最后一次的参数生效

---

### Requirement: HP<30% Vignette 闪烁（TC-Polish-04）

`VignettePulseBehaviour` MUST 订阅 `PlayerHealthChangedEvent`，ratio<0.3 → DOTween 修改 vignette.intensity Yoyo（color=#CC0D0D, intensityMax=0.45, period=2s）；ratio≥0.3 → Kill 后淡出到 0（0.4s）。依赖 URP Global Volume + Vignette Override；缺失时 3 级 fallback（auto 创建 Volume → 加 Override → 跳过并 Warn）。

#### Scenario: HP 降至 25% 触发脉冲
- **GIVEN** maxHP=100, currentHP=25
- **WHEN** PlayerHealthChangedEvent publish (ratio=0.25)
- **THEN** vignette.intensity Yoyo 2s 周期，color=#CC0D0D

#### Scenario: HP 恢复到 35% 停止脉冲
- **GIVEN** vignette 正在脉冲
- **WHEN** ratio=0.35 事件 publish
- **THEN** Kill pulse tween，DOTween.To intensity → 0（0.4s）

#### Scenario: URP Volume 缺失自动创建
- **GIVEN** 场景无 Global Volume
- **WHEN** VignettePulseBehaviour.Start
- **THEN** auto 创建 GO + Volume + Profile + Vignette Override（不崩）
