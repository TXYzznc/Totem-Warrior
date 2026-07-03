## Context

现状（探查实测）：

- `MapGenModule`（404 行）是 v2.1 的 **4 房间固定占位版**——象限化放 4 个染色 Cube（出生 / 工作室 / 商人 / Boss），`GenerateMap(seed, themeId)` 里 `rng` 甚至没被用到（`_ = rng`），每局地图完全一样。缩圈三段调度（`ITickable`）已内置并可用。
- 配置表：`MapTemplateConfig`（含 `MapSize=150 / MinRoomSize=15 / BspMaxDepth=4`，后两者是 BSP 遗留）、`ZoneShrinkConfig`（半径 65/35/5m，为 150m 图设计）。缺地形集 / 邻接规则表。
- `RoomInfo`：`RoomId / Bounds / CenterWorld / NodeType / Size / ThemeMetadata`，GDD 设计的 `SpawnerNodes / ChestNodes / NpcSlots` 未实现。
- 事件：`MapGeneratedEvent { Seed, ThemeId, Rooms, InitialZoneCenter, MapSize }`；`RoomEnteredEvent`；`ZoneShrinkPhaseEvent`。下游消费很浅——`CameraModule` 只用 `MapSize`；`EventModule` 遍历 `Rooms` + `RoomId`；Enemy/NPC/Weapon 目前是占位没真正消费。
- **change 25（已归档）已交付 2.5D 相机系统**：`CameraModule`（正交 + 俯角 55° + 平滑跟随 + 边界 clamp 读 `MapSize`）+ `BillboardSprite` 组件（sprite 面向相机、俯角校正）+ 深度动态排序。饥荒式 2.5D 的基础设施全部现成。
- 世界是 **3D XZ 平面**（角色在 XZ 移动，Y=高度），非纯 2D。
- 项目**未装 AI Navigation 包**，AI 靠 transform 直接移动。

约束：`System.Random(seed)` 强制（禁 `UnityEngine.Random`，伪联机→真联机迁移前提）；`InitializeAsync` 不发事件；不在 Update 做 GC alloc；所有配置读 DataTable。

## Goals / Non-Goals

**Goals:**

- 实现**区域生长 + 邻接约束 + 特征注入**的确定性开放地图生成算法，产物是纯数据（`TerrainType[,]` 网格 + 热点坐标 + 出生候选点 + 物件放置点），与渲染/现有系统解耦。
- 热点保底：固定热点（Boss / 商人 / 工作室…）每局必定存在、离边界 ≥ 安全距离、分布合理，永不生成失败。出生不属于热点，由 Spawner 从可行走候选点中分散抽样。
- 逻辑层 / 渲染层分离：Tilemap 铺地面 tile + billboard 立物件，支持 400m@2m 格（20万格量级）不卡。
- 完整"策划→美术→算法"管线跑通第一张地图，含 1-2 个可交互特色（沼泽减速）。
- 独立场景 `MapGenSandbox` 开发 + 占位色块 + EditMode 确定性单测，验证后接入现有游戏。

**Non-Goals:**

- ❌ NavMesh 烘焙（本期跳过，AI 继续 transform 移动）。
- ❌ 纯 WFC（回溯重试成本高、功能点保底难）。
- ❌ BSP 房间切割（GDD §9.1 决策被本 change 覆盖）。
- ❌ 多主题（本期只做 1 个风格；表结构支持多主题但只填 1 行数据）。
- ❌ 改相机核心（change 25 已就绪，仅按需微调俯角参数）。
- ❌ 圈外稀有节点（v2.1 已移除，沿用）。

## Decisions

### D1. 算法：分层的区域生长（结构层 + 地形层 + 特征层）

选**区域生长**，而非 BSP 或纯 WFC。理由：功能点保底天然契合"先撒点再生长"；邻接约束保证地形自然过渡；相比纯 WFC 无回溯、不会生成失败、确定性简单。

三阶段单 `UniTask` 内串行：

```
阶段① 撒热点点位（结构层）
  读 FeaturePointConfig → 对每个热点在 [safeMargin, size-safeMargin] 内 rng 取坐标
  → 泊松式最小间距校验（热点两两距离 ≥ minSpacing，冲突则重摇，上限 N 次后放宽）
  → 落到最近逻辑格，标记该格及邻域为对应热点 seed

阶段② 区域生长（地形层）
  多源 BFS：以热点格为初始 seed 入队
  → 逐格弹出，向 4 邻取未填格，按邻接白名单 + 权重 rng 选一个兼容 TerrainType 填入
  → 直到网格填满。保证每格与已填邻居"接口兼容"（水↔岸↔陆）

阶段③ 开放式出生候选
  从全图可行走连通区域抽样 SpawnCandidates
  → 避开边界、不可行走地形、热点近旁；候选点彼此保持间距
  → Spawner 用候选点分散玩家和 AI，缩圈提供后续方向压力

阶段④ 特征注入（异质地形）
  读 FeatureInjectionConfig → 按 seed 决定注入哪些特征（河流/山/荒原）及数量
  → 每个特征选一个起点，按该特征自己的扩散规则（河流=沿一条随机折线、山/荒原=半径生长）铺开
  → 特征块边缘用邻接规则插过渡 tile（河流两侧强制岸）
  → 特征注入在生长后做"覆盖"，但覆盖时仍校验邻接合法性
```

**替代方案**：纯 WFC（否——回溯 + 保底难）；区域生长无特征注入（否——用户明确指出会同质化整片陆地）。

### D2. 粒度：逻辑格 2m，渲染用 Tilemap（不是每格 GameObject）

逻辑网格 `TerrainType[width, height]`，`cellSize = 2m`，400m → 200×200 = 4万格（测试 50m→25×25、100m→50×50）。

**渲染**：地面走 Unity **Tilemap**（内部 chunk 合并渲染，4万格无压力），一个 `TerrainType` → 一个 `TileBase`；物件（树/石/芦苇/功能建筑）走 **billboard sprite**（复用 change 25 `BillboardSprite`），只在物件放置点 spawn，数量可控（几百个量级）。

**关键**：算法层完全不碰 GameObject，只吐 `int[,]`。渲染层是可替换的消费者——占位阶段用纯色 `Tile`，美术就绪后换真 tile 资源，算法零改动。

**替代方案**：每格一 GameObject（否——4万~16万 GO 直接卡死）；真 3D mesh chunk 合并（否——Tilemap 更简单且够用，2.5D 观感由相机俯角 + billboard 物件实现）。

### D3. 美术：饥荒式 2.5D = 地面正俯视 tile + 物件直立 billboard

- **地面层**：地形 tile 画**正俯视平面图**，无缝平铺 XZ 地面。相机 55° 斜看时自然产生透视压缩。tile 切图无缝、可旋转复用。
- **物件层**：树/石/芦苇/建筑等画成**直立 sprite**，用 `BillboardSprite` 立在地面、永远面向相机 → 这就是饥荒的"立体透视感"来源。
- **生产管线**：效果图→切图（先出一张地形大效果图定风格/质感，再从中切/扩展出各方向无缝 tile + 物件 sprite），走项目 UI/美术 6 阶段流程的适配版。本期只做 1 个风格。

### D4. 数据契约：新增 3 张配置表 + `RoomInfo` 语义调整

| 表 | 字段（关键） | 用途 |
|---|---|---|
| `TerrainTypeConfig` | `Id, TypeName, TileAssetKey, IsWalkable, MoveSpeedMul, GrowthWeight` | 地形种类字典 |
| `TerrainAdjacencyRules` | `FromType, ToType, Allowed` | 邻接白名单（对称展开） |
| `FeatureInjectionConfig` | `Id, FeatureName, TerrainType, SpreadMode(Line/Blob), CountMin, CountMax, SizeMin, SizeMax` | 特征注入 |
| `FeaturePointConfig`（可并入现有 MapTemplate 关联） | `Id, PointType, Required, MinSpacing, SafeMargin` | Boss / 商人 / 工作室等热点撒点约束；不含出生点 |

`MapTemplateConfig`：`MapSize 150→400`，`MinRoomSize/BspMaxDepth` 弃用（保留列避免动 generator，标注 deprecated）。`ZoneShrinkConfig`：半径按 400m 重调（初始圈半径需覆盖大部分图，约 160-180m）。

`MapGeneratedEvent` **扩展**（不破坏现有字段）：新增 `TerrainType[,] Grid`（或封装为 `MapGridData`）+ `float CellSize`，供渲染层与下游 MiniMap 用。`Rooms` 语义从"房间"调整为"功能区"，`Bounds/CenterWorld/NodeType` 沿用（下游 `EventModule` 只用 `RoomId` + 遍历，兼容）。

### D5. 独立场景开发 + 接入策略（低耦合）

核心算法封装为纯 C# 类 `RegionGrowthGenerator`（无 UnityEngine 依赖，除 `Vector2Int`），输入 `(seed, config)`，输出 `MapGridData`。`MapGenModule` 是它的 Unity 宿主 + 渲染驱动。

- **独立场景** `MapGenSandbox.unity`：一个 driver MonoBehaviour 调 generator + 渲染层，按键换 seed 重生成，用于 loop 截图迭代。不依赖 GameApp / 其他模块。
- **EditMode 单测**：直接测 `RegionGrowthGenerator`（不进 PlayMode），验确定性/连通性/热点保底/出生候选合法/邻接合法。
- **接入**：算法验证通过后，`MapGenModule.GenerateMap` 内部改调 `RegionGrowthGenerator`，发 `MapGeneratedEvent`（含 Grid）。下游按需消费新字段。

### D6. 特色内容：地形 MoveSpeedMul 接 Status/Combat

沼泽减速 = `TerrainTypeConfig.MoveSpeedMul < 1`。运行时由一个轻量 `TerrainEffectTracker`（0.2s tick，非每帧）查玩家所在格的 `TerrainType`，若有减速则通过现有 Status/移速通道施加。复用现有 `MoveSpeed` 计算链（`CombatModule` 已有 `_tattoo.Stats.MoveSpeed + Passive.MoveSpeedBonus`）。

## Risks / Trade-offs

- [400m@2m = 4万格，生成耗时/内存] → 算法层只操作 `int[,]`（4万 int ≈ 160KB，忽略不计）；BFS 是 O(格数) 线性；渲染 Tilemap 增量 `SetTile`。测试先用 50/100m 验证，再压 400m 实测耗时，超 2.5s 预算则分帧生成。
- [区域生长可能"堵死"——某格所有邻居都无兼容 tile] → 邻接表必须保证连通性（每个 TerrainType 至少有一条通往"通用陆地"的路径）；填格失败时降级填"最泛化"地形（陆地）并记 Warn，绝不抛异常中断。
- [功能点撒点在小地图（50m）可能间距不够] → `MinSpacing` 随 MapSize 缩放；重摇上限后放宽间距，保证一定放得下。
- [`MapGeneratedEvent` 扩展字段] → 只增不改，现有下游字段全兼容；`Grid` 字段下游可忽略。属"骨架先行"裁定项，接入前确认 CONTRACT。
- [覆盖 GDD §9.1 BSP 决策] → design 记录演进理由，接入完成后同步修订 `07-MapGenModule.md` + 系统 GDD §2.2，避免文档与代码长期背离。
- [美术 tile 无缝性] → 效果图→切图管线保证同风格；占位阶段先纯色验证算法，美术接入是独立可回滚的一步。

## Migration Plan

1. 独立场景 + generator + 占位渲染 + 单测（不碰现有 MapGenModule 主路径）。
2. loop 截图迭代到地形自然、用户确认。
3. 美术 tile + 物件 sprite 接入渲染层（仍在 sandbox 验证）。
4. `MapGenModule` 切换到新 generator，发扩展事件；缩圈配置按 400m 重调。
5. 接入现有游戏场景，下游联调（相机边界自动适配、EventModule 遍历兼容）。
6. 回滚策略：新 generator 与旧占位路径可共存（feature 分支），接入步骤 4 前 sandbox 完全独立，任何一步失败可回退到占位版。

## Open Questions

- 缩圈初始圈半径按 400m 具体取值 → 阶段 5 按 grill 共识（圈覆盖大部分图）实测调，不阻塞算法。
- 热点具体清单（除 Boss/商人/工作室是否还有事件房/宝箱区）→ 阶段 0 策划设计时定，配置表驱动，不阻塞算法框架。出生由 `SpawnCandidates` 规则独立控制。
- 物件放置密度/规则（每种地形上长什么物件、多密）→ 阶段 0 策划 + 阶段 2 美术定，先接口后填数据。
