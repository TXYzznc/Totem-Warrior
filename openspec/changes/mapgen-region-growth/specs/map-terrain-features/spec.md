## ADDED Requirements

### Requirement: 地形移速效果

特定地形（如沼泽）SHALL 通过 `TerrainTypeConfig.MoveSpeedMul` 定义移速倍率。当角色处于该地形格时，其有效移速 MUST 按该倍率缩放，效果 MUST 复用现有移速计算链（`MoveSpeed + MoveSpeedBonus`），不新造平行系统。

#### Scenario: 沼泽减速
- **WHEN** 玩家进入 MoveSpeedMul<1 的沼泽格
- **THEN** 玩家有效移速按倍率下降；离开后恢复

#### Scenario: 普通地形无影响
- **WHEN** 玩家处于 MoveSpeedMul=1 的普通地形
- **THEN** 移速不受地形影响

### Requirement: 地形效果轻量检测

地形效果 SHALL 由轻量 `TerrainEffectTracker` 按固定间隔（约 0.2s tick，非每帧）检测角色所在格，MUST NOT 在每帧 Update 中做 GC alloc 或全网格扫描。

#### Scenario: 非每帧检测
- **WHEN** 角色在地图上移动
- **THEN** 地形效果按约 0.2s tick 更新，每帧无 GC alloc

### Requirement: 特色可扩展接口

地形特色 SHALL 通过配置表 + 效果接口驱动，新增一种地形特色 MUST 只需加配置行 + 可选效果实现，不改生成算法核心。本期至少实现 1 个可交互特色（沼泽减速）。

#### Scenario: 新增特色不改算法
- **WHEN** 需要新增一种地形特色效果
- **THEN** 通过加配置 + 效果实现完成，`RegionGrowthGenerator` 无需改动
