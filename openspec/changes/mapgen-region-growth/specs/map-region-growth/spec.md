## ADDED Requirements

### Requirement: 确定性生成

生成器 SHALL 只使用 `System.Random(seed)` 作为唯一随机源，禁止 `UnityEngine.Random`。相同 `seed` + 相同配置 MUST 产出逐格完全一致的 `TerrainType[,]` 网格、热点坐标与出生候选点。

#### Scenario: 同 seed 同图
- **WHEN** 用相同 seed 与配置连续生成 100 次
- **THEN** 每次的网格数据、热点坐标与出生候选点逐格/逐点完全一致

#### Scenario: 不同 seed 不同图
- **WHEN** 用两个不同 seed 生成
- **THEN** 两张网格至少有一格 TerrainType 不同（不退化为固定布局）

### Requirement: 热点保底与边界约束

生成器 SHALL 保证 `FeaturePointConfig` 中所有 `Required=true` 的地图热点每局必定存在。`FeaturePointConfig` MUST NOT 把玩家出生点作为 Required 功能点；出生由 `SpawnCandidates` 独立表达。热点落点离地图边界的距离 MUST ≥ 该热点的 `SafeMargin`，热点两两间距 MUST ≥ `MinSpacing`（在小地图重摇上限后可放宽，但必定放得下且不越界）。

#### Scenario: 必需热点全部存在
- **WHEN** 用 100 个随机 seed 各生成一次
- **THEN** 每次结果都包含全部 Required 热点，无缺失，且不包含 Required Spawn

#### Scenario: 热点不越界且离边界足够
- **WHEN** 检查任一生成结果的热点坐标
- **THEN** 每个热点到四条边界的距离均 ≥ 其 SafeMargin

#### Scenario: 热点最小间距
- **WHEN** 检查任一生成结果任意两个热点
- **THEN** 它们的距离 ≥ MinSpacing（或已达重摇上限后的放宽值）

### Requirement: 开放式出生候选

生成器 SHALL 输出 `SpawnCandidates`，供 Spawner 在开放地图上分散玩家与 AI。候选点 MUST 落在可行走格，MUST 避开不可行走地形与地图边界，SHOULD 尽量远离 Boss / Merchant / TattooStudio 等热点并保持候选点彼此间距。缩圈是主要方向压力，生成器 MUST NOT 通过固定出生点建立线性路线。

#### Scenario: 出生候选可行走
- **WHEN** 生成任一地图
- **THEN** `SpawnCandidates` 至少包含 1 个候选，且每个候选点所在格均为 `IsWalkable=true`

#### Scenario: 出生候选不是热点
- **WHEN** 检查任一生成结果
- **THEN** `SpawnCandidates` 与 Required 热点分属不同数据集合，且不依赖 `FeaturePointType.Spawn`

### Requirement: 邻接约束合法性

区域生长填入的每一格 TerrainType SHALL 与其已填的 4 邻居满足 `TerrainAdjacencyRules` 的邻接白名单（如"水"不得直接紧邻"陆"，中间必为"岸"）。当某格无任何兼容 TerrainType 可填时，生成器 MUST 降级填最泛化地形并记 Warn，绝不抛异常中断。

#### Scenario: 无非法相邻
- **WHEN** 遍历生成网格所有相邻格对
- **THEN** 不存在任何一对 `Allowed=false` 的 TerrainType 相邻

#### Scenario: 填格堵死时降级
- **WHEN** 某格所有邻居约束导致无兼容 TerrainType
- **THEN** 该格填最泛化地形（陆地）并记 Warn，生成流程继续完成

### Requirement: 全图连通性

生成网格中所有 `IsWalkable=true` 的格子 SHALL 构成一个连通区域。任意出生候选点经 4 邻可达所有 Required 热点。

#### Scenario: 出生候选到热点可达
- **WHEN** 从任一 `SpawnCandidates` 对可行走格做 BFS
- **THEN** 所有 Required 热点所在格均被访问到，且所有可行走格属于同一连通分量

### Requirement: 特征注入避免同质化

生成器 SHALL 按 `FeatureInjectionConfig` 在生长途中注入异质地形特征（河流/山/荒原等），注入数量在 `[CountMin, CountMax]` 内由 seed 决定，且注入后网格 TerrainType 的种类多样性 MUST 高于纯生长基线（不退化为整片单一地形）。

#### Scenario: 特征按配置数量注入
- **WHEN** 生成一次并统计各特征出现次数
- **THEN** 每种特征的出现次数落在其 [CountMin, CountMax] 区间

#### Scenario: 地形不同质
- **WHEN** 统计生成网格的 TerrainType 直方图
- **THEN** 最高频地形占比不超过阈值（如 85%），存在 ≥3 种地形

### Requirement: 逻辑层纯数据输出

生成器 SHALL 是不依赖 UnityEngine 渲染的纯 C# 组件，输出 `MapGridData`（`TerrainType[,] Grid` + `CellSize` + 热点列表 + `SpawnCandidates` + 物件放置点），MUST NOT 在生成过程中创建任何 GameObject。

#### Scenario: 生成不产生 GameObject
- **WHEN** 在 EditMode 单测中调用生成器
- **THEN** 无需进入 PlayMode、无 GameObject 创建即可拿到完整网格数据
