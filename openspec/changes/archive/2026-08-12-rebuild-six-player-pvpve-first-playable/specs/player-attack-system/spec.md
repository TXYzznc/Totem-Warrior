## REMOVED Requirements

### Requirement: A. 9 种伤害源全部可触发
**Reason**: 旧规范包含短刀、重锤、弓、拳套、主动技能、旧形状多段和弹药耗尽近战降级，不符合第一阶段单枪械与无主动技能边界。
**Migration**: 旧事件、配置与资源从活动工作区物理删除；需要追溯时使用 Git 历史。主流程只注册新的第一阶段攻击与效果队列入口。

## ADDED Requirements

### Requirement: 第一阶段只有单枪械有效直接伤害入口
玩家和 Bot MUST 使用同一款基础枪械产生直接伤害；具体枪型与最终数值由配置决定，但不得在第一阶段启用其他武器、蓄力武器、主动技能伤害或弹药耗尽近战降级。

#### Scenario: 读取第一阶段武器池
- **WHEN** 对局初始化武器目录
- **THEN** 只有一个 active 武器定义
- **AND** 每名参赛者使用该定义

### Requirement: 枪械臂在有效直接伤害后触发
枪械命中 MUST 先经过命中、队伍、护盾/生命和伤害有效性校验；仅当实际产生正数直接伤害时，枪械臂事件才可进入效果队列。

#### Scenario: 射击队友
- **WHEN** 枪械命中同队成员
- **THEN** 直接伤害为 0
- **AND** 枪械臂、元素和反应均不触发

### Requirement: 弱点命中与身体命中必须可区分
所有参赛玩家/Bot MUST 提供身体伤害基线和头部弱点。弱点命中 MUST 产生独立上下文供头部优先级事件使用；第一阶段不实现非人型 Enemy 弱点。

#### Scenario: 命中敌队玩家头部
- **WHEN** 射线命中敌队玩家的头部 collider
- **THEN** 伤害上下文标记为弱点
- **AND** 头部/弱点事件先于枪械臂结算

### Requirement: 输入必须经过 InputModule
真人开火、瞄准、交互、构筑、救援、菜单和暂停输入 MUST 通过 `TotemInputService` / `ITotemInputProvider`；业务 MonoBehaviour 不得直接读取 `Input` 或 Input System device。

#### Scenario: 静态输入审计
- **WHEN** 扫描第一阶段新增或修改的业务代码
- **THEN** 不存在绕过 InputModule 的按键读取
