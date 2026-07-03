## Why

当前 `MapGenModule` 是 v2.1 的 4 房间固定占位版（西南出生 / 西北工作室 / 东北商人 / 东南 Boss），不是真正的程序化生成——每局地图完全一样，且几何是 `CreatePrimitive` 的染色方块。GDD §9.1 原定的 BSP 房间切割方案，在与用户深度探讨（grill-me 5/5）后被推翻：真正需要的是**邻接约束的"拼图式"开放地图生成**——地图由一系列地形素材像拼图一样拼出来，保证"河水旁边是河岸而不是木屋"这种自然过渡，同时热点点位（Boss / 商人 / 工作室）保底存在且分布合理；出生由 Spawner 从可行走候选格随机分散抽样，不作为固定路线起点。

## What Changes

- **BREAKING（对 GDD）**：算法路线从 GDD §9.1 的 **BSP 房间切割** 改为 **区域生长 + 邻接约束 + 特征注入**。GDD §9.1 决策被本 change 覆盖，归档时同步修订 `07-MapGenModule.md`。
- **生成算法**（核心，与现有系统低耦合，独立场景开发验证后再接入）：
  1. **撒热点点位**：在地图有效范围内按 seed 随机放置固定热点区域点（Boss / 商人 / 工作室…），约束"不出边界 + 离边界 ≥ 安全距离"。
  2. **区域生长**：从热点点位向外按邻接白名单逐格生长，填满整张网格（水↔岸↔陆，接口兼容才相邻）。
  3. **开放式出生候选**：在全图可行走连通区域中生成 `SpawnCandidates`，Spawner 用它分散玩家与 AI，缩圈负责后续方向压力。
  4. **特征注入**：生长途中按 seed 随机播种异质地形特征（河流 / 山 / 荒原），避免同质化；特征块自身也按邻接规则扩散。
  5. 全程 `System.Random(seed)`，同 seed → 同图（确定性，为伪联机→真联机迁移铺路）。
- **逻辑层 / 渲染层分离**：算法只输出 `TerrainType[,]` 网格 + 功能点坐标 + 物件放置点；渲染用 **Tilemap 铺地面 tile + billboard 立物件**（复用 change 25 的 `BillboardSprite`），**绝不每格一个 GameObject**。
- **地图尺寸**：正方形 **400m**（正式），逻辑格 **2m/格**；测试用 **50m / 100m** 小图。缩圈配置 `ZoneShrinkConfig` 按 400m 重调。
- **美术管线**：第一张地图详细策划设计 → 效果图→切图产出无缝地形 tile + 直立物件 sprite（本期只做 1 个风格）。饥荒式 2.5D（地面正俯视 tile + 物件直立 billboard，相机 55° 斜看出纵深）。
- **特色内容**：本期实现 1-2 个可交互特色（如沼泽减速），接 Combat / Status，其余留接口。
- **DataTable**：新增地形集 + 邻接规则 + 特征注入配置表；`RoomInfo` 语义调整为区域/功能点信息。

## Capabilities

### New Capabilities
- `map-region-growth`: 区域生长地图生成算法——功能点撒点、邻接约束生长、特征注入、确定性、逻辑网格数据结构。
- `map-terrain-rendering`: 地形渲染层——Tilemap 铺地面 + billboard 物件放置 + 饥荒式 2.5D 呈现。
- `map-terrain-features`: 地图特色可交互内容——地形效果（如沼泽减速）接入 Combat / Status。

### Modified Capabilities
<!-- openspec/specs/ 下暂无既有 spec，本 change 全部为新增 capability。 -->

## Impact

- **代码**：`Assets/Scripts/Modules/MapGen/`（重写生成核心）；独立开发场景 `Assets/Scenes/MapGenSandbox.unity`（新增，验证用）；`Assets/Tests/`（EditMode 确定性测试）。
- **配置表**：新增 `TerrainTypeConfig` / `TerrainAdjacencyRules` / `FeatureInjectionConfig`；调整 `MapTemplateConfig`（MapSize 150→400）与 `ZoneShrinkConfig`（半径按 400m 重调）。
- **事件契约**：`MapGeneratedEvent` 字段可能扩展（网格数据）；下游 `CameraModule`(读 MapSize) / `EventModule`(遍历 Rooms) / `SpawnerModule` / `EnemyModule` / `NPCModule` 已订阅，需保持兼容或走骨架先行裁定。
- **美术**：`Assets/Resources/Sprite/Map/<Theme>/`（地面 tile + 物件 sprite）。
- **依赖**：无新增第三方依赖（不装 AI Navigation；Tilemap/billboard 均为现成能力）。
- **相机**：change 25 的 2.5D `CameraModule` 已就绪，边界 clamp 读 `MapSize` 自动适配 400m，本期不改相机核心。
