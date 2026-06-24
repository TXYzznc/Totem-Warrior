# Spec — Tattoo Capability

> GIVEN/WHEN/THEN 形式的可验证需求。这是新代码必须满足的硬契约。

---

## Scenario 1 — 模块初始化

**GIVEN** GameApp.Start() 已调用 AddModule<TattooModule>() 并 StartAsync
**AND** DataTableModule 与 ResourceModule 已就绪
**WHEN** TattooModule.InitializeAsync 完成
**THEN** TattooModule.Player 不为 null
**AND** 5 个 DataTable 全部载入（part/color/pattern/element/shape）
**AND** 21 个策略已注册（6 PartBehavior + 7 ElementBehavior + 8 ShapeBehavior）

---

## Scenario 2 — 装备触发广播

**GIVEN** TattooModule 已初始化
**WHEN** 调用 TattooModule.Equip(partId=4, colorId=1, patternId=1)  // 右臂红线
**THEN** EventBus 上发出 BuildChangedEvent { Equipped.Count == 1 }
**AND** TattooModule.Equipped[0] 内容 = { Part=RightArm, Color=Red(Fire), Pattern=Line(SingleHit) }

---

## Scenario 3 — 普攻事件触发右臂 slot

**GIVEN** TattooModule.Equipped 含右臂红线 slot
**WHEN** EventBus.Publish(new AttackHitEvent { Target=t1, BaseDamage=10 })
**THEN** EventBus 上发出 EffectAppliedEvent
**AND** result.Element == "Fire" && result.Shape contains "SingleHit"
**AND** result.Damage > 0
**AND** t1.Health 减少

---

## Scenario 4 — 部位不匹配的事件不触发

**GIVEN** TattooModule.Equipped 仅含右臂红线
**WHEN** EventBus.Publish(new CritHitEvent { ... })  // 头部触发的事件
**THEN** 不发出 EffectAppliedEvent
**AND** 不调用任何 strategy

---

## Scenario 5 — PendingTrigger 消耗

**GIVEN** TattooModule.Player.PendingTriggers 含 1 条 { ConsumeOnEvent = SkillCastEvent, Source = "Torso" }
**WHEN** EventBus.Publish(new SkillCastEvent { SkillId="anything" })
**THEN** Player.PendingTriggers.Count == 0  // 已消耗
**AND** 发出 EffectAppliedEvent { Note contains "ConsumedPending" }

---

## Scenario 6 — Build 清空

**GIVEN** TattooModule.Equipped.Count == 6（满 Build）
**WHEN** TattooModule.Clear()
**THEN** TattooModule.Equipped.Count == 0
**AND** EventBus 上发出 BuildChangedEvent { Equipped.Count == 0 }
**AND** Player.PendingTriggers.Count == 0  // 也被清空

---

## Scenario 7 — InitializeAsync 时不发事件（框架戒律）

**GIVEN** ModuleRunner 正在按依赖序初始化模块
**WHEN** TattooModule.InitializeAsync 执行期间
**THEN** 不调用 EventBus.Publish 任何事件
**AND** 不调用 EventBus.RequestAsync 任何请求

---

## Scenario 8 — 关闭时反序

**GIVEN** ModuleRunner.ShutdownAsync 被调用
**WHEN** 关闭流程进行到 TattooModule
**THEN** Player 引用被释放
**AND** _elementBehaviors / _shapeBehaviors / _partBehaviors 清空
**AND** 没有未处理异常

---

## Scenario 9 — 配置不存在时的容错

**GIVEN** tattoo_color.json 中不存在 colorId = 999
**WHEN** TattooModule.Equip(partId=1, colorId=999, patternId=1)
**THEN** FrameworkLogger.Error 输出 "ColorId=999 NotFound"
**AND** 不修改 Equipped
**AND** 不发 BuildChangedEvent

---

## Scenario 10 — 战斗结束广播

**GIVEN** CombatModule + SpawnerModule + TattooModule 都已就绪
**AND** 所有敌人被消灭
**WHEN** CombatModule 检测到 enemy.Count == 0
**THEN** EventBus 上发出 CombatEndedEvent { PlayerWin = true }
**AND** UI Toolkit CombatHUDForm 接收到并显示胜利面板
