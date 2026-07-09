# Proposal — 固定地貌三主题地图（26-fixed-map-three-themes）

> **状态**：设计包（handoff to Codex，本期不写代码）
> **前身**：`mapgen-region-growth`（区域生长程序化方案，已于 2026-07-03 commit `4560aef` 实现后整体 `revert`，见 §附录 A）
> **决策门槛**：grill Phase A 已挖透 5/5（见 design.md §0）
> **交付范围**：三图策划全写 + 图一（AI 废墟）完整开发包 + 图二/图三仅策划大纲

---

## Why（为什么做这次变更）

`MapGenModule` 当前是 v2.1 的 **4 房间固定占位版**（象限放 4 个染色 Cube，`GenerateMap` 里 `rng` 未被使用，每局完全一样）。

上一次尝试（`mapgen-region-growth`）走的是 **seed 程序化生成地貌**路线（像 Minecraft：一个 seed 生成一整张不同的地貌，687 行区域生长算法 + 邻接约束 + 特征注入）。该方案在实现完成后被判定 **不具备可行性、且实际效果不佳**，整体 revert。

**本次换根本思路**：

> **地貌是固定的、手工设计的。** 三张地图各有一套完全不同的固定地貌，同一张图每次进入长得完全一样。**只有地图上的交互对象**（敌人 / Boss / NPC / 宝箱 / 交互物 / 颜料点 / 事件房）在开局时按规则（seed + 缩圈阶段）随机生成。

这与 GDD 的肉鸽 BR 核心兼容：局与局之间的变化来自"**交互物随机 + 对手 AI + 缩圈路径**"，而非地貌本身。地貌固定反而带来**可背板的地形记忆**（老玩家熟悉沼泽/掩体位置），符合"学策略 → 学自己"的留存主轴（GDD 00 §四）。

## What Changes（变更内容）

### 玩法/内容
- **三张固定地图**，对应 GDD 末日三主题：`AI 叛乱（废墟机械）` / `外星（异星侵蚀）` / `病毒（生化蔓延）`。**主题仅为视觉氛围**，与颜料颜色、与末日成因均**无机制绑定**（对 GDD 00 §三 USP3 "颜色绑定末日成因" 的更正，见 CONTRACT §7 更正项）。
- 每图 **400×400m**（沿用），逻辑格 **2m/格**（数据网格 200×200；地貌 TerrainType 采样网格粗粒度可用 4m/格 = 100×100，见 CONTRACT §2）。
- **特色交互区 = 纯地形效果**（踩上去自动生效，无需按键）：沼泽减速 / 河流阻挡绕路 / 废墟遮蔽视野 / 辐射区持续扣血。每图 2-3 种，主题化命名。
- **交互物生成 = 预定锚点 + seed 选取**：地图预定义候选锚点集（刷怪点/宝箱点/NPC 位/颜料点/事件房），开局按 `seed + 缩圈阶段` 从锚点集选子集填充。可控、绝不刷进河里/墙里、**全流程 AI 可自动完成**（锚点用数据文件描述，非手摆场景）。

### 美术生产管线（本次核心工程创新）
400×400m 无法用单张图（分辨率上限 2048²，铺满会糊）。采用 **地貌 mask → 矢量/放大 → 5×5 切块重绘 → 拼接 → 物件层叠加** 的六步管线（见 `art/图一美术生产管线.md`）：
1. 设计 **地貌 mask**（简单色块，不同色 = 不同 TerrainType，平原无高度差）→ 用户确认
2. mask **矢量化 / 超分放大** 到高清基准图
3. 脚本把高清基准图 **均匀切成 5×5=25 块**
4. 逐块调绘图工具**按美术风格重绘**，**结构必须与该块 mask 完全一致**（保证无缝拼接）
5. 25 块拼回 400m 无缝底图
6. 叠加 **物件层**（建筑/自然景物等直立 billboard，有景深）

### 渲染 / 视角
- **沿用 2.5D**（Hades 式精致俯视 2.5D，相机俯角约 55°）。程序基建（`CameraModule` / `BillboardSprite` / 缩圈调度，来自已归档 change 25）**复用**，按需微调；**美术资源全部重做**。

### 数据 / 契约
- **地貌数据层从 mask 派生**：同一张 mask 既驱动美术呈现，又降采样成 `TerrainType[,]` 粗网格驱动"可行走 / 减速 / 遮蔽"逻辑 → **美术与逻辑永不脱节，零手工摆 Collider**。
- 新增/调整 DataTable：`MapDefinitionConfig`（三图定义）、`TerrainTypeConfig`（地形字典，复用旧结构）、`MapAnchorConfig`（锚点集，或每图独立 JSON）、`SpawnRuleConfig`（缩圈阶段 × 交互物类别的生成规则）。
- `MapGeneratedEvent` 语义调整：从"程序生成的房间"改为"加载的固定地图 + 本局交互物布点结果"。

## Capabilities

### New Capabilities
- `map-fixed-terrain`：固定地貌地图的**加载与数据层**——mask 派生 TerrainType 网格、可行走/移速查询、地图定义表驱动、确定性（同 seed 同交互物布局）。
- `map-art-pipeline`：**大图美术生产管线**——mask→矢量→5×5 切块→逐块重绘→无缝拼接→物件层，产出 400m 底图 + 物件 sprite 的可复现流程。
- `map-interactive-spawn`：**交互物锚点生成**——预定义锚点集 + seed 选取 + 缩圈阶段驱动的敌人/Boss/NPC/宝箱/颜料/事件房布点。
- `map-terrain-features`：**纯地形特色效果**——沼泽减速 / 河流阻挡 / 废墟遮蔽视野 / 辐射扣血，接入 Combat / Status / 相机遮挡。

## Impact

- **代码**（Codex 侧实现，本期不动）：`Assets/Scripts/Modules/MapGen/` 重写（加载而非程序生成）；`Assets/Editor/`（mask 切块工具、TerrainType 采样工具）；`Assets/Tests/EditMode/MapGen/`（确定性 + 锚点保底测试）。
- **配置表**：新增 `MapDefinitionConfig` / `MapAnchorConfig` / `SpawnRuleConfig`；复用/调整 `TerrainTypeConfig`；`MapTemplateConfig` → 语义并入 `MapDefinitionConfig`（保留兼容或标 deprecated）。
- **事件契约**：`MapGeneratedEvent` 字段调整（见 CONTRACT §3）；下游 `CameraModule`（读 MapSize=400）/ `SpawnerModule` / `EnemyModule` / `NPCModule` / `EconomyModule` / `EventModule` / `ZoneModule` 需按新契约消费。属"骨架先行"裁定项。
- **美术**：`Assets/Resources/Sprite/Map/<Theme>/`（底图 25 块 tile + 物件 sprite）。图一完整产出，图二/三占位。
- **相机**：change 25 的 2.5D `CameraModule` 已就绪，边界 clamp 读 MapSize 自动适配 400m，本期不改核心。
- **依赖**：无新增第三方依赖。

## 附录 A：为何不沿用 mapgen-region-growth

| 维度 | mapgen-region-growth（已 revert） | 本 change |
|---|---|---|
| 地貌来源 | seed 程序生成，每局不同 | **手工固定**，同图恒定 |
| 每局变化来源 | 地貌 + 交互物 | **仅交互物 + AI + 缩圈** |
| 核心算法 | 687 行区域生长 + 邻接生长 + 特征注入 | **无生成算法**，改为加载 + 数据派生 |
| 判定 | 不可行、效果不佳，整体 revert | 本方案取代 |

**可复用的旧资产**（详见 design.md §2）：2.5D 相机、`BillboardSprite`、缩圈机制、`TerrainType`/`TerrainTypeConfig` 数据结构、`System.Random(seed)` 确定性约束、"逻辑层/渲染层分离"原则。
**丢弃的旧资产**：`RegionGrowthGenerator`（687 行）、`TerrainAdjacencyRules`（邻接生长）、`FeatureInjectionConfig`（特征注入生长）——这些正是被否决的程序化生成部分。
