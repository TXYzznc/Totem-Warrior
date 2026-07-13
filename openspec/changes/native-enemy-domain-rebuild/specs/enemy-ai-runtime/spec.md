## ADDED Requirements

### Requirement: Every enemy MUST run through the shared Enemy FSM
所有 Enemy MUST 由 `TotemEnemyControllerBase` 驱动，并使用 Dormant、Spawn、Patrol、Alert、Chase、AttackWindup/Cast、AttackActive、Recover、Return、Stagger、Dead 中的合法状态。每次状态变化 MUST 记录 from、to、reason、enemyId、targetId 和 worldTime。

#### Scenario: 普通近战怪完成基础战斗循环
- **WHEN** Active Participant 进入 DetectRange，随后进入 AttackRange 并离开 LeashRange
- **THEN** Enemy MUST 依次经过 Patrol、Alert、Chase、AttackWindup、AttackActive、Recover 和 Return
- **AND** 每次转换 MUST 有非空 reason

#### Scenario: 死亡状态不可逆
- **WHEN** Enemy HP 降至 0 后再次收到伤害或 AI Tick
- **THEN** EnemyDied MUST 只发布一次
- **AND** 状态 MUST 保持 Dead，不能再次攻击、寻路或掉落

### Requirement: Enemy threat MUST treat all active participants equally
候选目标 MUST 包含所有 Active、Alive、可达 Participant，不得按 Human、SmartBot 或 LightBot 提供固定身份加权。仇恨 MUST 由伤害、距离、近期受击和 Ability 修正构成；新目标未达到当前目标 1.25 倍仇恨时 MUST NOT 切换。

#### Scenario: 人机造成更高伤害时成为目标
- **WHEN** Human 距 Enemy 更近但 SmartBot 对 Enemy 造成显著更高伤害
- **THEN** SmartBot MUST 可超过 Human 成为最高仇恨目标

#### Scenario: 目标切换具有迟滞
- **WHEN** 新候选 Threat 仅为当前目标的 1.20 倍
- **THEN** Enemy MUST 保持当前目标
- **WHEN** 新候选 Threat 达到当前目标的 1.25 倍
- **THEN** Enemy MUST 切换并记录 `ThreatOverride`

#### Scenario: Protected 参赛者从仇恨表失效
- **WHEN** 当前目标进入 Protected 或 Disconnected
- **THEN** Enemy MUST 在下一次决策中清除该目标并重新选取 Active Participant

### Requirement: Enemy differences MUST be composed from reusable abilities
Enemy Ability MUST 使用独立 EnemyAbilityConfig 和通用能力执行器，不得把参赛者 SkillConfig 当作 Enemy Ability。首轮 MUST 支持 Melee、Projectile、Charge、Leap、Beam、ConeSweep、AreaPulse、HazardZone、Shield、Summon、Regenerate、DeathBurst 和 PhaseTransition。

#### Scenario: 能力遵守前摇和后摇
- **WHEN** Enemy 开始一个 Windup=0.4s、Active=0.1s、Recovery=0.6s 的 Ability
- **THEN** 0.4 秒前 MUST 不造成命中
- **AND** Active 窗口 MUST 最多结算一次配置允许的命中
- **AND** Recovery 完成前 MUST 不开始另一个不可并行能力

#### Scenario: 可打断再生
- **WHEN** 孢子宿主开始 Regenerate 且在 Windup 内受到满足打断条件的伤害
- **THEN** Regenerate MUST Cancel，不能恢复 HP，并进入 Stagger 或 Recover

#### Scenario: 召唤遵守 Encounter 上限
- **WHEN** Summon Ability 请求生成子怪但 ActiveCap 已满
- **THEN** 请求 MUST 被拒绝并记录 `Blocked.EncounterActiveCap`

### Requirement: The first content pass MUST contain 15 distinct enemy definitions
运行时 catalog MUST 包含 8 个 Light、4 个 Elite 和 3 个 Boss 定义，覆盖 common、ai_ruins、alien_hive、virus_swamp，且每个定义的 Ability、Behavior、Loot 和 RuntimeAssetKey 外键可解析。

#### Scenario: 15 种敌人内容完整
- **WHEN** GameplayCatalog 进行内容验证
- **THEN** MUST 找到 proposal/design 指定的 15 个 EnemyId
- **AND** 每个主题 MUST 有 2 个 Light、1 个 Elite、1 个 Boss
- **AND** common MUST 有 2 个 Light 和 1 个 Elite

#### Scenario: 缺失最终美术使用可观测占位
- **WHEN** RuntimeAssetKey 无法加载最终 Prefab 或 Sprite
- **THEN** Enemy MUST 使用按 Theme/Tier 可区分的 fallback 表现
- **AND** 资源快照 MUST 记录 missing key 与 fallback key

### Requirement: Bosses MUST use deterministic three-phase behavior
每个 Boss MUST 使用 BossPhaseConfig，并在 HP 首次跨过 60% 和 30% 阈值时各转换一次。阶段转换 MUST 发布事件、更新能力集、VFX/BGM cue 和伤害倍率，且转换过程不得锁定玩家镜头。

#### Scenario: Boss 阶段只转换一次
- **WHEN** Boss HP 从 100% 依次降至 55%、25%，期间受到治疗回到 65% 后再次降至 55%
- **THEN** Phase1->2 和 Phase2->3 MUST 各发布一次
- **AND** 治疗回升 MUST NOT 让 Boss 降回旧阶段或重复发布事件

### Requirement: Enemy AI MUST obey performance LOD budgets
AI LOD MUST 以最近的任意 Active Participant 为基准。Hot/Warm/Cold 决策频率 MUST 可配置，Enemy AI Tick MUST 不产生每帧托管分配；寻路 MUST 有每帧请求预算和缓存。

#### Scenario: 远处怪物降频
- **WHEN** Light Enemy 距所有 Active Participant 超过 60m
- **THEN** 其决策频率 MUST 不高于 0.5Hz
- **AND** 无目标时 MUST 不请求 A* 路径

#### Scenario: 最近目标可以是人机
- **WHEN** Human 距 Enemy 80m，LightBot 距 Enemy 10m
- **THEN** Enemy MUST 进入 Hot LOD，而不是按 Human 距离进入 Cold
