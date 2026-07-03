## ADDED Requirements

### Requirement: Tilemap 地面渲染

渲染层 SHALL 将 `MapGridData.Grid` 铺到 Unity Tilemap（一个 `TerrainType` 映射一个 `TileBase`），MUST NOT 为每个逻辑格创建独立 GameObject。渲染层 MUST 是可替换的消费者——占位阶段用纯色 Tile，美术就绪后换真 tile 资源而无需改动生成算法。

#### Scenario: 大地图不逐格建 GameObject
- **WHEN** 渲染 400m@2m（4 万格）网格
- **THEN** 通过 Tilemap 批量 SetTile 完成，场景中地面不出现 4 万个 GameObject

#### Scenario: 占位与真素材可切换
- **WHEN** 从纯色占位 Tile 切换到真 tile 资源
- **THEN** 生成算法代码不需要任何改动，仅替换 TileBase 资源映射

### Requirement: 物件 billboard 放置

渲染层 SHALL 在 `MapGridData` 的物件放置点上 spawn 直立 billboard sprite（复用 change 25 的 `BillboardSprite` 组件），使物件面向相机呈现饥荒式 2.5D 纵深。物件数量 MUST 控制在可控量级（数百，非逐格）。

#### Scenario: 物件面向相机
- **WHEN** 相机以俯角 55° 观察场景
- **THEN** 物件 sprite 直立且面向相机，不被俯角压扁

#### Scenario: 物件按放置点生成
- **WHEN** 生成结果含 N 个物件放置点
- **THEN** 场景中恰好 spawn N 个对应 billboard 物件，不逐格铺满

### Requirement: 2.5D 相机适配

地图渲染 SHALL 复用 change 25 已交付的 2.5D `CameraModule`（正交 + 俯角 + 边界 clamp 读 `MapSize`）。地图尺寸变更（150→400）时相机边界 MUST 自动适配，本 change MUST NOT 重写相机核心。

#### Scenario: 相机边界随地图尺寸适配
- **WHEN** MapSize 从 150 改为 400
- **THEN** CameraModule 的边界 clamp 自动覆盖 400m 全图，无需改相机代码
