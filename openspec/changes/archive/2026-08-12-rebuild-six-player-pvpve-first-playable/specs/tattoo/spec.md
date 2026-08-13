## MODIFIED Requirements

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

## ADDED Requirements

### Requirement: 图案效果必须具有无数值公开文本
每个可装备图案配置 MUST 提供用于对手情报面板的无精确数值效果文本，并与实际行为语义一致。

#### Scenario: 配置缺少公开文本
- **WHEN** 数据验证发现 P01 或 P02 的公开文本为空
- **THEN** 诊断失败且不得进入发布验收
