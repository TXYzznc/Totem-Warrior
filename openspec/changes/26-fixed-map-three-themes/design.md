# Design — 固定地貌三主题地图

> 配合 [CONTRACT.md](./CONTRACT.md)（术语/数据/路径/事件真源）与 [proposal.md](./proposal.md) 阅读。

## §0 grill Phase A 共识（5/5 已挖透）

| # | 挖掘项 | 共识 |
|---|---|---|
| 1 | 核心目标一句话 | 三张**固定手工**地貌地图（末日三主题，400m，2.5D），地貌恒定，只有**交互物**按 seed+缩圈规则随机生成。本期只出方案+开发工作包（handoff Codex，不写代码）。 |
| 2 | 关键决策 A/B | ① 地貌：固定手工 vs 程序生成 → **固定手工**（程序生成方案 `mapgen-region-growth` 已实现后 revert，判定不可行/效果差）。② 大图制作：单张大图 vs 切块拼接 vs 大底图+物件 → **mask→矢量→5×5 切块重绘→拼接→物件层叠加**。③ 数据层：手摆 Collider vs 单独 mask vs 区域 TerrainType 网格 → **从美术 mask 派生 TerrainGrid**（最适合 AI 自动）。④ 交互物：自由撒点 vs 预定锚点 → **预定锚点+seed 选取**（AI 可自动、不越界）。 |
| 3 | 不做什么（边界） | 见 §Goals/Non-Goals。 |
| 4 | 验收标准 | 三图策划全写 + 图一完整开发包（策划→美术管线→程序任务，Codex 可直接开发）+ 图二/三仅策划大纲。见 tasks.md 验收清单。 |
| 5 | 关键约束 | 尺寸统一 400m；确定性 `System.Random(seed)`；2.5D 视角复用 change 25 基建但美术全新；主题仅视觉、不绑颜料/成因。 |

## §1 Context（现状探查实测）

- `MapGenModule`（404 行）：v2.1 **4 房间固定占位版**，`GenerateMap` 里 `rng` 未用（`_ = rng`），每局一样。缩圈三段调度（ITickable）已内置可用。
- **`mapgen-region-growth`（commit 4560aef）已实现完整程序化方案后被 revert（commit a48808a）**：687 行 `RegionGrowthGenerator` + 326 行渲染 + 3 张地形/邻接/特征表 + Sandbox 场景 + EditMode 测试 + 完整 openspec。判定不可行/效果差。
- **change 25（已归档）交付 2.5D 相机**：`CameraModule`（正交+俯角 55°+平滑跟随+边界 clamp 读 MapSize）+ `BillboardSprite`（sprite 面向相机、俯角校正）+ 深度动态排序。2.5D 基建全现成。
- 世界是 **3D XZ 平面**；项目**未装 AI Navigation**，AI 靠 transform 移动。
- 移速：玩家 5m/s，敌人 3.5–5m/s（`PlayerStats.MoveSpeed=5f` / `EnemyConfig`）。
- 约束：`System.Random(seed)` 强制；`InitializeAsync` 不发事件；不在 Update 做 GC alloc；配置读 DataTable。

## §2 复用 / 丢弃清单（相对 mapgen-region-growth）

| 资产 | 处置 | 说明 |
|---|---|---|
| `CameraModule`（change 25） | **复用** | 边界 clamp 读 MapSize=400，按需微调俯角。不改核心。 |
| `BillboardSprite`（change 25） | **复用** | 物件层直立 sprite 全靠它。 |
| 缩圈机制（MapGenModule ITickable + ZoneShrinkConfig + ZoneShrinkPhaseEvent） | **复用** | 半径按 400m 重调（数值，非结构）。 |
| `TerrainType` 枚举 + `TerrainTypeConfig` 表 | **复用+改造** | 从"生长权重/邻接"语义改为"6 个机制语义类 + 主题映射"（CONTRACT §4.1）。删 `GrowthWeight`。 |
| `System.Random(seed)` 确定性约束 | **复用** | 交互物布点用；地貌固定无需随机。 |
| "逻辑层/渲染层分离"原则 | **复用** | 数据层（TerrainGrid）与美术层（BaseMap/Props）分离。 |
| `RegionGrowthGenerator`（687 行） | **丢弃** | 程序生成，本方案不需要。 |
| `TerrainAdjacencyRules`（邻接生长表） | **丢弃** | 固定地貌无需邻接约束。 |
| `FeatureInjectionConfig`（特征注入生长） | **丢弃** | 固定地貌无需程序注入特征。 |
| `MapGenSandboxDriver` / Sandbox 场景 | **丢弃/可选** | 若 Codex 需可视化验证锚点布点可另建轻量预览。 |

## §3 Goals / Non-Goals

**Goals**：
- 交付三图完整策划 + 图一"策划→美术→程序"全开发包，Codex 拿到即可实现。
- 确立"**美术 mask 单源驱动美术+数据层**"的管线，零手工摆 Collider。
- 确立"**预定锚点+seed 选取+缩圈分层**"的确定性交互物生成规则。
- 6 个机制语义 TerrainType 跨三主题复用，特色效果代码写一次。

**Non-Goals**：
- ❌ 本期写任何 C# 代码（纯设计包）。
- ❌ 程序生成地貌（region-growth / BSP / WFC 全部排除）。
- ❌ 图二/图三的美术生产（仅策划大纲）。
- ❌ NavMesh 烘焙（AI 继续 transform 移动，遇 Blocked 格绕行由 AI 模块处理）。
- ❌ 改相机核心 / 改颜料系统 / 改缩圈结构。
- ❌ 主动交互机关（可点燃/可砍断等，本期只做纯地形效果）。

## §4 Decisions

### D1. 地貌固定手工，"每局变化"来自交互物+AI+缩圈

固定地貌 → 可背板、地形记忆、美术可控质量。局与局差异由交互物随机布点（seed）+ 20 智能 AI 行为 + 缩圈路径提供，足够撑肉鸽 BR 的重复可玩性。**替代方案**：程序生成（已 revert，否）。

### D2. 大图制作 = mask→矢量/放大→5×5 切块重绘→拼接→物件层

**问题根源**：单张图分辨率上限 2048²，铺 400m 会糊（400m/2048px ≈ 0.2m/px，地面细节严重不足）。
**方案**：切成 5×5=25 块，每块覆盖 80×80m，用 2048² 出图 → 80m/2048px ≈ 0.04m/px，精度 5 倍提升，达到"够清晰"。
**无缝关键**：每块重绘时**以该块的 mask 子图为结构约束**（img2img / 结构控制），保证块 (r,c) 与块 (r,c+1) 在边界的地貌结构（河流走向/区域边界）连续。**接缝纹理风险**由三重手段缓解（见 §5 风险）。
**替代方案**：单张大图（否，糊）；纯小 tile 集 Tilemap 拼（否，用户明确要手绘感底图，非重复 tile）。

### D3. 数据层从 mask 派生（TerrainGridBaker）

mask 每种颜色 = 一种 TerrainType。烘焙工具把 mask 降采样到 100×100（4m/格），每格取主色 → 反查 `TerrainTypeConfig.TileColorHex` → 得 `TerrainType[,]`。运行时 `MapGenModule.QueryTerrain(worldPos)` O(1) 查格。
**收益**：美术改 mask → 重烘焙 → 数据自动更新，**美术与逻辑永不脱节，零手工 Collider**，AI 全自动。
**替代方案**：手摆 Collider（否，AI 难自动、与美术割裂）；单独数据层 mask（否，美术要多画一张、易与底图对不齐）。

### D4. 交互物 = 预定锚点 + seed 选取 + 缩圈分层

`MapAnchorConfig` 每图预定义一批锚点（类型+坐标+归属缩圈阶段+权重）。开局按 `System.Random(seed)` 从各阶段各类别锚点池中按 `SpawnRuleConfig` 的 Min/Max 数量选子集，发 `InteractablesSpawnedEvent`。下游模块订阅后各自实例化实体（MapGen 不 spawn 实体，保持职责单一）。
**收益**：绝不刷进河/墙（锚点是预验证的可行走点）；确定性；缩圈分层让"探宝期锚点先激活、决赛圈锚点后激活"匹配 4/8/2 节奏；**锚点用 JSON 描述，AI 可批量生成与校验**。
**替代方案**：可行走区自由撒点（否，可能刷进视觉障碍或聚堆，且难确定性复现）。

### D5. TerrainType = 6 个跨主题机制语义类

`None/Ground/Slow/Blocked/Cover/Hazard`。三主题的具体地貌都映射到这 6 类（AI 废墟的辐射区、病毒的毒雾、外星的腐蚀池 → 都是 `Hazard`）。特色效果代码（减速/阻挡/遮蔽/扣血）**写一次三图复用**，主题差异只在美术+命名。
**替代方案**：每主题独立地形枚举（否，代码三份、维护爆炸）。

### D6. 特色交互区 = 纯地形效果（TerrainEffectTracker）

轻量 `TerrainEffectTracker`（0.2s tick，非每帧）查玩家/AI 所在格 TerrainType：
- `Slow`：施加移速倍率（复用现有 MoveSpeed 计算链）。
- `Blocked`：不可进入（移动系统 + AI 寻路避让）。
- `Cover`：写入"被遮蔽"状态（供远程命中/视野判定，接 Combat）。
- `Hazard`：每 tick 施加 `HazardDps × dt` 伤害（接 Status/Combat）。
**替代方案**：每帧查（否，GC+开销）；主动交互机关（Non-Goal，本期不做）。

### D7. 三图尺寸统一 400m，差异在布局

三图**同尺寸**（简化相机/缩圈/性能），差异体现在：地貌布局（河流/废墟/开阔比例）、特色区种类与占比、锚点分布密度、主题美术。见三图策划案。

## §5 Risks / Trade-offs

| 风险 | 缓解 |
|---|---|
| **25 块独立重绘接缝纹理跳变**（结构连续但草地质感/明暗断层） | ① 重绘带 overlap 边 + 脚本羽化融合；② 顺序重绘（把已生成相邻块边缘喂下一块 img2img 续接，牺牲并行）；③ 底图刻意低频柔和，高频细节交给物件层。三者组合，本方案默认①+③。 |
| **矢量化失败/效果差**（mask 转矢量不干净） | mask 设计时强制"纯色块+简单形状+无渐变"（矢量化理想输入）；矢量化失败降级为超分放大（Real-ESRGAN 类）。二选一，都产出高清基准图。 |
| **mask 派生 TerrainGrid 边界锯齿** | 4m 采样格足够粗，锯齿在移动手感上不可感知；关键边界（河岸）可在 mask 上画 1 格过渡带。 |
| **400m + 25 块底图内存/加载** | 25×(2048² PNG 压缩后~0.5-1MB) ≈ 15-25MB/图。按 §6 预热策略：只常驻当前图；BaseMap 用 Tilemap 或 25 个 quad，chunk 合并渲染。 |
| **MapGeneratedEvent 语义变更影响下游** | 字段只增不删；`Rooms` 语义放宽但字段不变，`EventModule` 现有用法兼容。骨架先行裁定，接入前对齐 CONTRACT。 |
| **交互物锚点在小范围聚堆** | `MapAnchorConfig.MinSpacing` 约束；选取时做间距校验。 |
| **主题美术只做 1 套，图二三空缺** | 图二三 `MapDefinitionConfig` 行存在但 BaseMap/Props 用占位；`MapGenModule` 不感知完成度，照常加载（缺资源降级纯色）。 |

## §6 性能预算（400m + 50 actor）

| 指标 | 预算 | 依据 |
|---|---|---|
| 地图加载（BaseMap 25 块 + Props + TerrainGrid） | ≤ 1.5s | 只读固定资源，无生成算法；TerrainGrid 可预烘焙成 `byte[10000]` 直读 |
| 交互物布点计算 | ≤ 0.1s | O(锚点数)，锚点数百级 |
| 运行时帧开销 | 0 ms/帧 | 除 TerrainEffectTracker（0.2s tick） |
| TerrainEffectTracker | < 0.05ms / 50 actor / tick | O(1) 查格 |
| GC（运行时） | 0 KB/s | 复用 struct payload |
| 内存常驻 | < 25MB/图 | 只常驻当前图 BaseMap+Props |

## §7 Migration Plan（Codex 实现顺序，详见 tasks.md）

1. 定 mask 规范 + `TerrainTypeConfig` 6 类 + 数据表结构（不碰美术）。
2. `TerrainGridBaker` + `MapMaskSlicer` Editor 工具。
3. `MapGenModule` 重写：加载 BaseMap + 派生/读 TerrainGrid + 读锚点 + seed 布点 + 发事件。
4. `TerrainEffectTracker` + 接 Combat/Status/相机遮挡。
5. 图一美术六步管线产出（mask→矢量→切块重绘→拼接→物件）。
6. 接入游戏场景，下游联调（相机边界/交互物实例化/缩圈）。
7. 归档同步修订 GDD 07 + 系统 GDD + 总策划案 §三。

## §8 Open Questions（不阻塞，实现期定）

- 缩圈初始半径按 400m 具体取值（design 建议初始 ≈180m 覆盖大部分图）→ 数值调优，进 ZoneShrinkConfig。
- 底图渲染用 Unity Tilemap（25 tile）还是 25 个 quad mesh → Codex 按性能实测选，接口无差异。
- 图一具体地貌布局（河流走向/废墟占比）→ 三图策划案给方向，mask 设计稿时定稿（需用户确认 mask）。
- 物件层密度/清单 → 图一美术管线阶段定，先接口后填数据。
