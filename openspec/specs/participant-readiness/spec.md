# participant-readiness Specification

## Purpose
TBD - created by archiving change native-enemy-domain-rebuild. Update Purpose after archive.
## Requirements
### Requirement: The authoritative world MUST start independently from client readiness
PCG、Encounter、Enemy AI、Bot AI、缩圈和世界时钟 MUST 在权威世界进入 WorldActive 后运行，不得等待所有真人客户端 Ready。

#### Scenario: 快客户端不等待慢客户端
- **WHEN** Participant A 已 Ready，Participant B 仍 Loading
- **THEN** A MUST 能正常移动、战斗和交互
- **AND** worldTime、AI 和缩圈 MUST 继续推进

### Requirement: Each human participant MUST have an independent readiness lifecycle
真人 Participant MUST 按 Reserved、Loading、Protected、Active、Eliminated/Disconnected 转换。Loading MUST 保留参赛名额和出生点，但不得生成可碰撞实体、参与索敌、受伤、输出、移动、交互或拾取。

#### Scenario: Loading 实体不可被利用
- **WHEN** Participant 处于 Loading
- **THEN** 关系策略 MUST 拒绝其输入和所有对其伤害
- **AND** Enemy threat query MUST 不返回该 Participant
- **AND** 场景中 MUST 无阻挡路径的实体碰撞体

### Requirement: Ready MUST require HUD camera input and one rendered frame
本地客户端只有在场景、CombatHUD、相机和 InputModule 可用，并完成至少一个渲染帧后才能提交 Ready。Ready 后 MUST 进入最长 5 秒 Protected，再转 Active。

#### Scenario: HUD 打开但 InputModule 未就绪不能 Ready
- **WHEN** CombatHUD 已打开但 InputModule 或相机不可用
- **THEN** Ready command MUST 不发送

#### Scenario: 五秒后自动激活
- **WHEN** Participant 进入 Protected 后 5 秒内无取消保护的输入
- **THEN** Participant MUST 转为 Active

### Requirement: Any actionable InputModule intent MUST release protection
Protected 状态下，InputModule 报告非零移动、攻击、技能或交互意图时 MUST 在执行该动作前结束保护；保护期间 Participant MUST 不能造成伤害。

#### Scenario: 移动取消保护
- **WHEN** Protected Participant 首次通过 InputModule 提交非零 Move
- **THEN** ProtectionReleased MUST 先于移动应用记录
- **AND** 后续 Enemy 才能把该 Participant 作为目标

#### Scenario: 攻击不能利用保护
- **WHEN** Protected Participant 提交 Attack
- **THEN** 系统 MUST 先结束保护
- **AND** 本次攻击只能在 Active 状态下执行

### Requirement: Loading timeout MUST prevent indefinite invulnerability
Loading 超时 MUST 默认为 90 秒并可配置。达到超时时 Participant MUST 转为 Disconnected，释放保留实体和出生点，不再计入 aliveParticipantCount，并立即重新评估胜负。

#### Scenario: 90 秒超时移除名额
- **WHEN** Participant 从 Loading 开始累计达到 90 秒且未 Ready
- **THEN** 生命周期 MUST 转为 Disconnected
- **AND** aliveParticipantCount MUST 减少 1
- **AND** 必须记录 `Participant.ReadyTimeout`

### Requirement: Participant combat MUST respect the opening grace period
进入 Active 的 Participant 仍 MUST 在对局开始后的前 60 秒免受其它 Participant 的伤害，且不能对其它 Participant 造成伤害。该规则同时适用于 Human、SmartBot 和 LightBot，不适用于 NPC Enemy；NPC 仍可按照正常索敌与关系规则攻击 Active Participant。WorldTime 达到 60 秒后，Active Participant 之间 MUST 恢复正常战斗关系。

#### Scenario: 开局 60 秒内参赛者互伤被阻断但 NPC 可攻击
- **WHEN** WorldTime 小于 60 秒，两个 Active Participant 互相造成直接伤害
- **THEN** 两次伤害 MUST 被阻断且目标生命值不变
- **AND** 同一时间 Active NPC Enemy 对 Participant 的伤害 MUST 允许

#### Scenario: 60 秒后参赛者互伤恢复
- **WHEN** WorldTime 达到 60 秒，两个 Active Participant 互相造成直接伤害
- **THEN** 伤害 MUST 按正常 CombatRelationship 规则结算

