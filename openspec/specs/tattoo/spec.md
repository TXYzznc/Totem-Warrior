# tattoo Specification

## Purpose
TBD - created by archiving change 01-tattoo-framework-rewrite. Update Purpose after archive.
## Requirements
### Requirement: TattooModule MUST 在依赖就绪后完成初始化并注册全部策略

`TattooModule` MUST 在 `DataTableModule` 与 `ResourceModule` 就绪后由 `ModuleRunner` 启动；`InitializeAsync` 完成后 MUST 注册 6 PartBehavior + 7 ElementBehavior + 8 ShapeBehavior 共 21 个策略，并加载 5 张 DataTable（part/color/pattern/element/shape）。`InitializeAsync` 期间 MUST NOT 发布或请求任何 EventBus 事件。

#### Scenario: 模块初始化完成

- **GIVEN** `GameApp.Start()` 已调用 `AddModule<TattooModule>()` 并 `StartAsync`
- **AND** `DataTableModule` 与 `ResourceModule` 已就绪
- **WHEN** `TattooModule.InitializeAsync` 完成
- **THEN** `TattooModule.Player` MUST NOT 为 null
- **AND** 5 个 DataTable MUST 全部载入（part/color/pattern/element/shape）
- **AND** MUST 注册 21 个策略（6 PartBehavior + 7 ElementBehavior + 8 ShapeBehavior）

#### Scenario: InitializeAsync 期间不发事件（框架戒律）

- **GIVEN** `ModuleRunner` 正在按依赖序初始化模块
- **WHEN** `TattooModule.InitializeAsync` 执行期间
- **THEN** MUST NOT 调用 `EventBus.Publish` 任何事件
- **AND** MUST NOT 调用 `EventBus.RequestAsync` 任何请求

### Requirement: TattooModule.Equip MUST 在配置有效时挂载 slot 并广播 BuildChangedEvent

`Equip(partId, colorId, patternId)` MUST 校验三个 id 都在对应 DataTable 行集合内；校验通过 MUST 把对应 slot 写入 `Equipped` 并 `Publish(BuildChangedEvent)`。配置缺失（如 colorId 不存在）MUST 记 `FrameworkLogger.Error` 且 MUST NOT 修改 `Equipped`、MUST NOT 发事件。

#### Scenario: 装备触发广播

- **GIVEN** `TattooModule` 已初始化
- **WHEN** 调用 `TattooModule.Equip(partId=4, colorId=1, patternId=1)`（右臂红线）
- **THEN** EventBus MUST 发出 `BuildChangedEvent { Equipped.Count == 1 }`
- **AND** `TattooModule.Equipped[0]` MUST = `{ Part=RightArm, Color=Red(Fire), Pattern=Line(SingleHit) }`

#### Scenario: 配置不存在时的容错

- **GIVEN** `tattoo_color.json` 中不存在 `colorId = 999`
- **WHEN** `TattooModule.Equip(partId=1, colorId=999, patternId=1)`
- **THEN** `FrameworkLogger.Error` MUST 输出 `"ColorId=999 NotFound"`
- **AND** MUST NOT 修改 `Equipped`
- **AND** MUST NOT 发 `BuildChangedEvent`

### Requirement: TattooModule MUST 按 slot 部位匹配事件并广播 EffectAppliedEvent

收到战斗事件（AttackHit / CritHit / Damaged / SkillCast / DodgePressed / MoveTick）时，TattooModule MUST 遍历 `Equipped` 找出部位匹配的 slot 并触发对应 strategy 链；命中 MUST `Publish(EffectAppliedEvent)`；部位不匹配 MUST NOT 触发任何 strategy。`PendingTrigger` 列表中匹配当前事件类型的条目 MUST 被消耗（从 `Player.PendingTriggers` 移除）。

#### Scenario: 普攻事件触发右臂 slot

- **GIVEN** `TattooModule.Equipped` 含右臂红线 slot
- **WHEN** `EventBus.Publish(new AttackHitEvent { Target=t1, BaseDamage=10 })`
- **THEN** EventBus MUST 发出 `EffectAppliedEvent`
- **AND** `result.Element` MUST == `"Fire"` 且 `result.Shape` MUST contains `"SingleHit"`
- **AND** `result.Damage` MUST > 0
- **AND** `t1.Health` MUST 减少

#### Scenario: 部位不匹配的事件不触发

- **GIVEN** `TattooModule.Equipped` 仅含右臂红线
- **WHEN** `EventBus.Publish(new CritHitEvent { ... })`（头部触发的事件）
- **THEN** MUST NOT 发出 `EffectAppliedEvent`
- **AND** MUST NOT 调用任何 strategy

#### Scenario: PendingTrigger 消耗

- **GIVEN** `Player.PendingTriggers` 含 1 条 `{ ConsumeOnEvent = SkillCastEvent, Source = "Torso" }`
- **WHEN** `EventBus.Publish(new SkillCastEvent { SkillId="anything" })`
- **THEN** `Player.PendingTriggers.Count` MUST == 0（已消耗）
- **AND** MUST 发出 `EffectAppliedEvent { Note contains "ConsumedPending" }`

### Requirement: TattooModule.Clear MUST 清空 Build 并广播

`Clear()` MUST 把 `Equipped` 与 `Player.PendingTriggers` 同时清空 并 `Publish(BuildChangedEvent { Equipped.Count == 0 })`。

#### Scenario: Build 清空

- **GIVEN** `TattooModule.Equipped.Count == 6`（满 Build）
- **WHEN** `TattooModule.Clear()`
- **THEN** `TattooModule.Equipped.Count` MUST == 0
- **AND** EventBus MUST 发出 `BuildChangedEvent { Equipped.Count == 0 }`
- **AND** `Player.PendingTriggers.Count` MUST == 0

### Requirement: TattooModule MUST 在 Shutdown 与战斗结束时正确收尾

`ShutdownAsync` MUST 释放 `Player` 引用、清空三个策略字典；MUST NOT 抛未处理异常。`CombatModule` 检测到 `enemy.Count == 0` 时 MUST `Publish(CombatEndedEvent { PlayerWin = true })`，UI Toolkit `CombatHUDForm` MUST 接收并显示胜利面板。

#### Scenario: 关闭时反序

- **GIVEN** `ModuleRunner.ShutdownAsync` 被调用
- **WHEN** 关闭流程进行到 `TattooModule`
- **THEN** `Player` 引用 MUST 被释放
- **AND** `_elementBehaviors / _shapeBehaviors / _partBehaviors` MUST 清空
- **AND** MUST NOT 抛未处理异常

#### Scenario: 战斗结束广播

- **GIVEN** `CombatModule + SpawnerModule + TattooModule` 都已就绪
- **AND** 所有敌人被消灭
- **WHEN** `CombatModule` 检测到 `enemy.Count == 0`
- **THEN** EventBus MUST 发出 `CombatEndedEvent { PlayerWin = true }`
- **AND** UI Toolkit `CombatHUDForm` MUST 接收到并显示胜利面板

