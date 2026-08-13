## ADDED Requirements

### Requirement: Combat entities MUST separate participant identity from enemy identity
运行时 MUST 使用独立字段表达 CombatantDomain、ParticipantControllerKind、ParticipantLifecycle 和 EnemyTier。Human、SmartBot、LightBot MUST 都属于 Participant；Light、Elite、Boss MUST 都属于 Enemy。ServiceNpc MUST NOT 进入 Combatant、参赛者统计或怪物统计。

#### Scenario: 50 名参赛者与怪物分开统计
- **WHEN** 一局包含 1 名 Human、20 名 SmartBot、29 名 LightBot、18 只普通怪、2 只精英怪和 1 个 Boss
- **THEN** participantCount MUST 为 50，enemyCount MUST 为 21，bossCount MUST 为 1
- **AND** Human、SmartBot、LightBot MUST NOT 具有 EnemyTier
- **AND** Enemy MUST NOT 具有 ParticipantControllerKind

#### Scenario: 服务型 NPC 不属于战斗实体
- **WHEN** 地图生成商人和纹身师
- **THEN** 二者 MUST NOT 出现在 Participant 或 Enemy 查询中
- **AND** 默认伤害入口 MUST 拒绝以二者为目标

### Requirement: All damage and targeting MUST use one combat relationship policy
直接攻击、范围攻击、连锁、穿透、状态、纹身、武器特性和 Enemy Ability MUST 在命中前调用统一关系策略，并记录允许或阻止的原因码。业务服务 MUST NOT 通过 `actor.Kind != Player` 或等价条件自行推断敌我。

#### Scenario: 开局保护与 NPC 攻击关系正确
- **WHEN** Participant A、Participant B 均为 Active 且 WorldTime 小于 60 秒，A 分别攻击 B 和 Enemy C
- **THEN** A 对 B 的直接和二次伤害 MUST 被 `Blocked.ParticipantCombatGracePeriod` 阻断
- **AND** A 对 C 的伤害 MUST 被 `Allowed.ParticipantToEnemy` 允许
- **WHEN** WorldTime 达到 60 秒，A 攻击 Active Participant B
- **THEN** 伤害 MUST 被 `Allowed.ParticipantToParticipant` 允许

#### Scenario: 怪物不能攻击未激活参赛者
- **WHEN** Enemy 分别评估 Loading、Protected、Active 三名 Participant
- **THEN** Loading 和 Protected MUST 被拒绝
- **AND** Active MUST 可成为目标并受到伤害

#### Scenario: 怪物默认不互相伤害
- **WHEN** Enemy Ability 未配置 `CanHitEnemies` 并命中另一 Enemy
- **THEN** 伤害 MUST 被 `Blocked.EnemyFriendlyFire` 阻止

### Requirement: Victory MUST depend only on surviving participants
Run 结束判定 MUST 只统计未 Disconnected 且仍存活的 Participant。Enemy、Boss 和 ServiceNpc 的存活数量 MUST NOT 参与胜负。

#### Scenario: 怪物全部死亡不结束对局
- **WHEN** 地图所有 Enemy 均死亡但仍有 2 名 Participant 存活
- **THEN** Run MUST 保持进行中

#### Scenario: 最后一名参赛者获胜
- **WHEN** 50 名 Participant 中仅剩 1 名存活
- **THEN** Run MUST 结束并把该 Participant 记录为 winner
- **AND** 无论地图仍有多少 Enemy，结果 MUST 不变

#### Scenario: Loading 超时不阻塞胜负
- **WHEN** 1 名 Participant Active 存活，另 1 名 Loading Participant 达到超时并转为 Disconnected
- **THEN** Active Participant MUST 被判定为 winner

### Requirement: Runtime snapshots MUST expose participant and enemy causality separately
运行时快照 MUST 分别提供 participantCount、aliveParticipantCount、enemyCount、aliveEnemyCount、各 EnemyTier 数量、winnerId 和最后一次关系决策原因，不得复用一个 `aliveEnemyCount` 同时表达人机和怪物。

#### Scenario: HUD 和诊断读取独立统计
- **WHEN** CaptureSnapshot 被调用
- **THEN** HUD 存活人数 MUST 来自 aliveParticipantCount
- **AND** 怪物压力展示 MUST 来自 aliveEnemyCount
- **AND** 两个字段 MUST 可独立变化
