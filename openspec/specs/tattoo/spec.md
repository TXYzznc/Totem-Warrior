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
Equip MUST 验证当前为构筑阶段、部位属于六部位、图案为 P01/P02、元素为火/冰/雷且资源足够；成功后原子扣除 10 份颜料并广播构筑变化。替换已有纹身时 MUST 先按 60% 即 6 份返还旧颜料。

#### Scenario: 合法装备 P01
- **GIVEN** 当前为构筑阶段且玩家拥有至少 10 份目标颜料
- **WHEN** 玩家在空部位装备 P01
- **THEN** 颜料减少 10
- **AND** slot、视觉和 `BuildChangedEvent` 同步更新

#### Scenario: 非构筑阶段装备
- **GIVEN** 当前为战斗阶段
- **WHEN** 真人或 Bot 请求 Equip
- **THEN** 请求失败且不得扣除资源或广播变化

### Requirement: TattooModule MUST 按 slot 部位匹配事件并广播 EffectAppliedEvent
TattooModule MUST 将一次行为的所有合法部位效果提交给确定性效果队列，而不是立即嵌套结算。枪械臂仅在造成有效直接伤害时触发；头部弱点、闪避、移动、躯干和保留的主动技能臂按规格优先级进入队列。

#### Scenario: 无有效直接伤害
- **WHEN** 枪击被无敌、队友免伤或其他规则完全抵消
- **THEN** 枪械臂效果不得触发

### Requirement: TattooModule.Clear MUST 清空 Build 并广播
对局清理时 Clear MUST 清空六部位与运行时来源状态并广播；玩家在构筑阶段主动拆除单个部位时 MUST 只清理该部位并返还 6 份对应颜料。

#### Scenario: 主动拆除单个部位
- **WHEN** 构筑阶段玩家拆除一个已装备纹身
- **THEN** 该部位变为空且返还 6 份颜料
- **AND** 其他五个部位不变化

### Requirement: TattooModule MUST 在 Shutdown 与战斗结束时正确收尾
Tattoo runtime MUST 在 Shutdown 和离开 CombatHud 时释放 Participant build、PendingTrigger 和运行时订阅，且不得抛未处理异常。Run 胜负 MUST 由 Participant 生存状态决定；Enemy 全灭 MUST NOT 发布胜利，最后一名 Participant 存活时 MUST 发布包含 winnerParticipantId 的结算结果。

#### Scenario: 关闭时反序
- **WHEN** GF_X runtime 执行 shutdown 或离开 CombatHud
- **THEN** Tattoo runtime MUST 清空 build、pending trigger 和事件订阅
- **AND** MUST NOT 抛未处理异常

#### Scenario: 怪物全灭不触发胜利
- **WHEN** 所有 Light、Elite 和 Boss 均死亡但仍有至少 2 名 Participant 存活
- **THEN** Run MUST 保持进行
- **AND** Tattoo runtime MUST 保持可用

#### Scenario: 最后一名参赛者触发结算
- **WHEN** 只剩 1 名 Participant 存活
- **THEN** Combat result MUST 记录该 Participant 为 winner
- **AND** CombatHUD MUST 显示本地玩家对应的胜利或失败结果

### Requirement: 图案效果必须具有无数值公开文本
每个可装备图案配置 MUST 提供用于对手情报面板的无精确数值效果文本，并与实际行为语义一致。

#### Scenario: 配置缺少公开文本
- **WHEN** 数据验证发现 P01 或 P02 的公开文本为空
- **THEN** 诊断失败且不得进入发布验收

