# Spec — map-fixed-terrain（固定地貌加载与数据层）

> 术语/数据/路径以 [CONTRACT.md](../../CONTRACT.md) 为准。

## ADDED Requirements

### Requirement: 固定地图加载

`MapGenModule` SHALL 在订阅 `RunStartedEvent` 后，按当局 `MapId` 从 `MapDefinitionConfig` 读取主题定义，加载对应的 `BaseMap`（5×5=25 块底图）、`PropLayer`（物件层）与地貌 `mask`，并派生 `TerrainGrid`。加载 MUST 在 ≤1.5s 内完成，完成后发布 `MapGeneratedEvent`（含 `MapSize=400 / TerrainCellSize=4 / TerrainGridWidth=100 / TerrainGridHeight=100`）。`InitializeAsync` 期间 MUST NOT 发任何事件。

#### Scenario: 加载三主题任一图
- **WHEN** 以 `MapId ∈ {AiRuins, Alien, Virus}` 触发 RunStarted
- **THEN** 对应主题的 BaseMap/PropLayer/mask 被加载，`MapGeneratedEvent` 在 1.5s 内发布一次，且 MapSize=400

#### Scenario: 缺美术资源降级
- **WHEN** 某主题（图二/图三）BaseMap 资源缺失
- **THEN** 以纯色占位加载，不抛异常，仍正常发布 `MapGeneratedEvent` 并记 Warn

### Requirement: mask 派生 TerrainGrid

系统 SHALL 提供 `TerrainGridBaker`：把地貌 mask 降采样到 100×100 网格（4m/格），每格取主色反查 `TerrainTypeConfig.TileColorHex` 得 `TerrainType`。同一张 mask MUST 派生出逐格完全一致的 TerrainGrid（确定性）。烘焙结果 SHOULD 可预存为资源直读（避免运行时重复降采样）。

#### Scenario: mask 派生确定性
- **WHEN** 对同一张 mask 连续烘焙 100 次
- **THEN** 每次得到的 `TerrainType[100,100]` 逐格完全一致

#### Scenario: 颜色反查覆盖
- **WHEN** mask 中出现未在 `TerrainTypeConfig` 定义的颜色
- **THEN** 该格降级为 `Ground`（可行走）并记 Warn，不抛异常

### Requirement: 地形查询 API

`MapGenModule` SHALL 提供 `TerrainType QueryTerrain(Vector3 worldPos)`，把世界坐标映射到地形采样格并返回该格 TerrainType，复杂度 O(1)。越界坐标 MUST 返回 `Blocked`。

#### Scenario: 世界坐标查格
- **WHEN** 传入地图内任一世界坐标
- **THEN** 返回其所在 4m 采样格的 TerrainType，无 GC 分配

#### Scenario: 越界返回 Blocked
- **WHEN** 传入超出 [0,400] 范围的坐标
- **THEN** 返回 `Blocked`

### Requirement: 尺寸统一

三张地图 MUST 全部为 400×400m，TerrainCellSize=4，逻辑格 2m。相机边界 clamp、缩圈半径 MUST 按 400m 标定。

#### Scenario: 三图同尺寸
- **WHEN** 读取任一 `MapDefinitionConfig` 行
- **THEN** MapSize=400，TerrainCellSize=4
