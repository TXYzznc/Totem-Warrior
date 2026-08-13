## ADDED Requirements

### Requirement: PCG MUST produce encounter anchors without instantiating enemies
PCG/MapService MUST 产出可达的 Encounter anchor 数据；`TotemEncounterService` MUST 根据 MapSnapshot、主题、配置和 seed 构建 SpawnPlan；只有 `TotemEnemyService` 可以实例化 Enemy。

#### Scenario: 相同 seed 生成相同 SpawnPlan
- **WHEN** 对同一 MapSnapshot、主题、配置和 seed 连续调用 BuildSpawnPlan
- **THEN** EnemyId、位置、波次和触发时间 MUST 完全一致

#### Scenario: PCG 阶段没有 Enemy GameObject 副作用
- **WHEN** 仅执行 PCG Generate 和 BuildSpawnPlan
- **THEN** 场景中的 Enemy GameObject 数量 MUST 保持 0

### Requirement: Encounter selection MUST combine common and theme-specific pools
每张地图 MUST 从 common 池和当前主题池选择普通/精英/Boss，不得生成其它主题专属敌人。所有 PoolId 和 EnemyId 外键 MUST 在载入时验证。

#### Scenario: AI 遗迹不会生成异星专属怪
- **WHEN** theme 为 ai_ruins 并构建完整 SpawnPlan
- **THEN** 所有非 common Enemy 的 ThemeId MUST 为 ai_ruins
- **AND** Boss MUST 为 `boss_ai_core_zero`

### Requirement: Encounter lifecycle MUST follow the confirmed world clock
默认配置 MUST 在 WorldActive 时生成 18 只 Light，以 ActiveCap=30、每 45 秒 4-6 只的波次补充且 TotalCap=60；Elite MUST 从 240 秒开始，总数 5-8 且不重生；Boss MUST 在 600 秒生成一只且不重生。所有参数 MUST 来自 EncounterSpawnConfig。

#### Scenario: 普通怪不会原地立即重生
- **WHEN** 一只 Light 死亡
- **THEN** 同一位置 MUST 不立即生成替代者
- **AND** 只有下一次波次评估且 ActiveCount 低于上限时才可在其它有效 anchor 补充

#### Scenario: 精英在四分钟前不生成
- **WHEN** worldTime 小于 240 秒
- **THEN** Elite spawn count MUST 为 0
- **WHEN** worldTime 达到 240 秒
- **THEN** Encounter MAY 按计划生成首批 Elite，且本局总数 MUST 在配置范围内

#### Scenario: Boss 每局只生成一次
- **WHEN** worldTime 先后跨过 600 秒且 Boss 被击杀
- **THEN** Boss Spawned 事件 MUST 只出现一次
- **AND** 后续波次 MUST NOT 补充 Boss

### Requirement: Spawn positions MUST be safe, walkable and distributed
SpawnPlan 中每个位置 MUST 可行走、满足同批 MinSpacing，并与任意 Active Participant 保持 MinParticipantDistance。运行时若原位置失效 MUST 在同 anchor 邻域确定性寻找替代点，否则跳过并记录原因。

#### Scenario: 敌人不会贴脸生成
- **WHEN** 一个计划点距 Active Participant 小于 MinParticipantDistance
- **THEN** 该点 MUST 被重定位或拒绝
- **AND** 不得在原位置生成 Enemy

#### Scenario: 快速 PCG 与完整 PCG 保持世界尺度
- **WHEN** 诊断使用减少 tile 和 object 数量的快速 PCG 配置
- **THEN** MapSize 和 Encounter world-space 距离 MUST 与完整模式一致

