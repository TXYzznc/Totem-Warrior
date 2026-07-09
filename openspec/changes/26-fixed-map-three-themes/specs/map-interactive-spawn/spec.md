# Spec — map-interactive-spawn（交互物锚点生成）

> 术语/数据/事件以 [CONTRACT.md](../../CONTRACT.md) 为准。

## ADDED Requirements

### Requirement: 预定锚点集

每张地图 SHALL 在 `MapAnchorConfig`（每图一份）中预定义候选锚点，每个锚点含 `Id / MapId / AnchorKind / WorldX / WorldZ / ZonePhase / Weight / MinSpacing`。所有锚点坐标 MUST 落在 `IsWalkable=true` 的地形格上（不得在 `Blocked` 格）。

#### Scenario: 锚点全部可行走
- **WHEN** 校验任一图的全部锚点
- **THEN** 每个锚点所在地形格 IsWalkable=true；否则报错阻塞（数据错误）

### Requirement: seed 驱动确定性选取

开局时系统 SHALL 用注入的 `System.Random(seed)` 从锚点池中按 `SpawnRuleConfig` 的 `MinCount/MaxCount` 选取子集，MUST NOT 使用 `UnityEngine.Random`。相同 `seed + MapId` MUST 产出逐个完全一致的 `AnchorPlacement` 列表。选取 MUST 满足同类锚点间距 ≥ `MinSpacing`。

#### Scenario: 同 seed 同布局
- **WHEN** 用相同 seed + MapId 连续选取 100 次
- **THEN** 每次 `InteractablesSpawnedEvent.Placements` 逐项完全一致

#### Scenario: 不同 seed 不同布局
- **WHEN** 用两个不同 seed
- **THEN** 至少一个锚点的选取结果不同

### Requirement: 缩圈阶段分层

每个锚点 SHALL 归属一个缩圈阶段（ZonePhase 0/1/2）。系统 MUST 按阶段分层选取与激活，使探宝期（Phase 0）锚点先激活、决赛圈（Phase 2）锚点后激活，匹配 GDD 4/8/2 节奏。

#### Scenario: 阶段化激活
- **WHEN** 缩圈进入 Phase N
- **THEN** 归属 Phase N 的交互物锚点在此阶段可用/激活

### Requirement: 关键类保底

`SpawnRuleConfig` 中标记为必需的交互物类别（至少：Boss=1、纹身师≥1、商人≥1）MUST 每局必定被选中，永不缺失。普通类（敌人/宝箱/颜料点）按 Min/Max 区间随机数量。

#### Scenario: Boss/NPC 保底
- **WHEN** 用 100 个随机 seed 各选取一次
- **THEN** 每次结果都含 Boss=1、纹身师≥1、商人≥1

### Requirement: 职责边界——只发布布点，不实例化实体

`MapGenModule` MUST NOT 实例化任何交互物实体（敌人/宝箱/NPC GameObject）。它只计算并发布 `InteractablesSpawnedEvent`（布点清单）。实体实例化 SHALL 由订阅该事件的对应模块（EnemyModule/EconomyModule/NPCModule/EventModule）各自完成。

#### Scenario: 下游各自实例化
- **WHEN** `InteractablesSpawnedEvent` 发布
- **THEN** EnemyModule 只处理 `Kind=Enemy/Boss` 的 placement，EconomyModule 只处理 `Kind=Chest`，互不越界；MapGen 未创建任何实体
