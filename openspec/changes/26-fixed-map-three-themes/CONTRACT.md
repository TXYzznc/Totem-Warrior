# CONTRACT — 固定地貌三主题地图（唯一真源）

> **地位**：本文件是本 change 所有文档（proposal / design / specs / 策划案 / 美术管线 / tasks）与 Codex 实现的**术语、数据结构、文件路径、事件契约的唯一真源（single source of truth）**。任何文档与本文件冲突，以本文件为准。
> **改动规则**：修改本文件任一"锁死项"必须回主对话裁定，不得在下游文档或代码里擅自偏离。

---

## §1 术语表（统一命名，禁止同义词漂移）

| 术语 | 定义 | 禁用同义词 |
|---|---|---|
| **地图（Map）** | 一张 400×400m 的固定手工地貌 + 其锚点集。三张之一。 | 关卡、场景 |
| **主题（Theme）** | 地图的视觉氛围风格，ID 沿用既有世界观 GDD 锁定值：`AI_RUINS`（AI 废墟）/ `ALIEN_HIVE`（外星侵蚀）/ `VIRUS_SWAMP`（病毒变异）。**仅视觉，无机制绑定**。 | 世界、风格池 |
| **地貌 mask** | 策划手绘的低分辨率色块图，每种颜色 = 一种 TerrainType，是美术与数据层的共同源头。 | 蒙版、色图 |
| **底图（BaseMap）** | mask 经矢量/放大→5×5 切块重绘→拼接后的 400m 无缝地面美术图。 | 大图、背景图 |
| **物件层（PropLayer）** | 叠加在底图上的直立 billboard 美术（建筑/自然景物），有景深。 | 装饰、立绘 |
| **TerrainType** | 地形类型枚举（草地/沼泽/河流/废墟/辐射区…），带可行走性与移速倍率。 | 地块、tile 类型 |
| **地形网格（TerrainGrid）** | 从 mask 降采样得到的 `TerrainType[,]`，驱动可行走/减速/遮蔽逻辑。 | tile 网格 |
| **特色交互区（TerrainFeature）** | 纯地形效果区域（沼泽减速等），是 TerrainType 的运行时效果表现，非独立实体。 | 交互区、机关 |
| **锚点（Anchor）** | 地图上预定义的候选生成点（含类型 + 世界坐标 + 约束），交互物只能生成在锚点上。 | 刷新点、spawn point |
| **交互物（Interactable）** | 开局按规则生成在锚点上的对象：敌人/Boss/NPC/宝箱/交互物品/颜料点/事件房。 | 实体、物件（易与物件层混淆，严格区分） |
| **缩圈阶段（ZonePhase）** | 0/1/2 三段，见 GDD。交互物生成规则按阶段分层。 | 圈、阶段 |

> **术语铁律**：文档里"物件层"专指美术 billboard（树/建筑），"交互物"专指可交互实体（敌人/宝箱）。二者严格不混用。

### §1.1 MapId 映射表（锁死；ThemeName 沿用既有世界观 GDD `15-世界观与轻剧情.md` §2.1 锁定值）

| MapId (int) | ThemeName (string) | 中文显示名 | 本 change 完成度 |
|---|---|---|---|
| 1 | `AI_RUINS` | AI 废墟 | **本期完整产出**（图一） |
| 2 | `ALIEN_HIVE` | 外星侵蚀 | 仅策划大纲（图二） |
| 3 | `VIRUS_SWAMP` | 病毒变异 | 仅策划大纲（图三） |

> 显示名/主导色/视觉关键词/怪物风格/环境音关键词已在世界观 GDD 定义，本 change 不重复设计，直接复用（见三图策划案引用）。

---

## §2 空间与网格标准（锁死）

| 项 | 值 | 说明 |
|---|---|---|
| **地图尺寸** | 400 × 400 m | 正方形。世界坐标 (0,0) 为左下角，(400,400) 为右上角。 |
| **世界平面** | XZ 平面 | 角色在 XZ 移动，Y = 高度。**平原无连续爬升**（局部装饰高度差可有）。 |
| **逻辑格（LogicCell）** | 2 m / 格 → 200×200 | 交互物锚点坐标、距离约束以逻辑格或世界米为单位。 |
| **地形采样格（TerrainCell）** | 4 m / 格 → 100×100 | mask 降采样精度。100×100=1万格，`byte[10000]` ≈ 10KB，可整表存。 |
| **移速基准** | 玩家 5 m/s，敌人 3.5–5 m/s | 标定地图尺寸的依据：穿越 400m 直线约 80s；缩圈单局 10–15min。 |
| **相机** | 正交 2.5D，俯角 ≈55° | 复用 change 25 `CameraModule`，边界 clamp 读 MapSize=400。 |

**尺寸标准化依据（供 tasks/策划核对）**：单局 10–15min，缩圈三段压缩活动范围。400m 对角线 ≈566m ≈113s 全程步行，保证"探宝期能跑图、决赛圈被压到小范围"。三图**尺寸统一 400×400**，不同的是地貌布局与特色区，不是尺寸。

---

## §3 事件契约（锁死签名）

> 现有 `MapGenModule.Events` 已有 `MapGeneratedEvent` / `RoomEnteredEvent` / `ZoneShrinkPhaseEvent`。本 change **调整 `MapGeneratedEvent` 语义**（字段只增不删，保持下游兼容），新增交互物相关事件。

### 3.1 MapGeneratedEvent（语义调整，字段只增不删）

```csharp
public sealed class MapGeneratedEvent
{
    public int         Seed;              // [沿用] 本局种子，决定交互物布局
    public int         ThemeId;           // [沿用] 主题 ID → MapDefinitionConfig.Id
    public List<RoomInfo> Rooms;          // [语义调整] 从"程序房间"改为"地图功能区/命名区域"（可为空列表）
    public Vector2     InitialZoneCenter; // [沿用] 初始缩圈圆心（世界 XZ）
    public float       MapSize;           // [沿用] = 400
    // ===== 新增（只增不删，下游可忽略）=====
    public int         TerrainCellSize;   // [新增] = 4，地形采样格边长（m）
    public int         TerrainGridWidth;  // [新增] = 100
    public int         TerrainGridHeight; // [新增] = 100
    // 地形网格数据本身不进事件（10KB），下游通过 GetModule<MapGenModule>().QueryTerrain(worldPos) 查询
}
```

### 3.2 InteractablesSpawnedEvent（新增）

```csharp
// 交互物布点计算完成后发布（MapGeneratedEvent 之后）。
// 只发布"布点结果清单"，实体实例化由各下游模块订阅后各自完成（MapGen 不 spawn 实体）。
public sealed class InteractablesSpawnedEvent
{
    public int Seed;
    public List<AnchorPlacement> Placements; // 每个被选中的锚点 + 分配到的交互物类型
}

public struct AnchorPlacement
{
    public int          AnchorId;      // 对应 MapAnchorConfig.Id
    public Vector3      WorldPos;      // 世界坐标（Y 由地面高度决定，平原恒定）
    public InteractableKind Kind;      // Enemy/Boss/Npc/Chest/Item/PaintNode/EventRoom
    public int          ZonePhase;     // 该锚点归属的缩圈阶段（0/1/2），决定何时激活
    public int          VariantId;     // 具体刷什么（敌人配置 Id / 宝箱档位 / NPC 类型…）
}
```

### 3.3 ZoneShrinkPhaseEvent（沿用，不改）

沿用现有签名（Phase/Center/TargetRadius/Duration/OutZoneDamage/SecondsRemaining）。**缩圈半径按 400m 重调**（数值进 `ZoneShrinkConfig`，非契约锁死项）。

---

## §4 数据结构（锁死）

### 4.1 TerrainType（枚举，复用旧实验结构）

```csharp
public enum TerrainType : byte
{
    None      = 0,
    Ground    = 1,  // 通用可行走地面（各主题美术不同，语义一致）
    Slow       = 2, // 减速区（沼泽/泥沼/黏液…主题化命名），MoveSpeedMul<1
    Blocked   = 3,  // 不可行走（河流/深渊/高墙），阻挡绕路
    Cover     = 4,  // 遮蔽区（废墟/林冠），遮挡视野/远程，可行走
    Hazard    = 5,  // 伤害区（辐射/毒雾/酸池），可行走但持续扣血
    // 主题各自映射到这 6 个语义类；美术不同、机制相同。
}
```

> **关键设计**：TerrainType 是**跨主题的机制语义类**（6 个），三主题的具体地貌（AI 废墟的"辐射区" / 病毒的"毒雾" / 外星的"腐蚀池"）都映射到同一个 `Hazard`。这样**特色效果代码写一次，三图复用**。主题差异只在美术与命名。

### 4.2 InteractableKind（枚举，新增）

```csharp
public enum InteractableKind : byte
{
    Enemy = 1, Boss = 2, Npc = 3, Chest = 4, Item = 5, PaintNode = 6, EventRoom = 7,
}
```

### 4.3 RoomInfo（沿用现有，语义放宽）

现有 `RoomInfo { RoomId, Bounds, CenterWorld, NodeType, Size, ThemeMetadata }` 保留。本 change 中 `Rooms` 用于**命名功能区**（Boss 区/商人区/出生候选区），非程序房间。下游 `EventModule` 现有"遍历 Rooms + RoomId"用法兼容。

---

## §5 DataTable 契约（表名 + 关键字段锁死；完整字段见 spec）

| 表 | 用途 | 关键字段 | 每图独立? |
|---|---|---|---|
| `MapDefinitionConfig` | 三图定义 | `Id, ThemeName, MaskAssetKey, BaseMapKeyPrefix, MapSize=400, TerrainCellSize=4, HudAccentColor` | 否（3 行） |
| `TerrainTypeConfig` | 地形字典 | `Id, TypeName, TileColorHex, IsWalkable, MoveSpeedMul, BlocksVision, HazardDps` | 否（6 行，跨主题共用语义） |
| `MapAnchorConfig` | 锚点集 | `Id, MapId, AnchorKind, WorldX, WorldZ, ZonePhase, Weight, MinSpacing` | **是**（每图一套；数据量大时可拆 `MapAnchor_<mapid>.json`） |
| `SpawnRuleConfig` | 生成规则 | `Id, MapId, ZonePhase, InteractableKind, MinCount, MaxCount, VariantPoolId` | 是 |

> 生成流程：`MapDefinitionConfig` 定主题 → 加载 mask → 派生 TerrainGrid（查 `TerrainTypeColor` 反查）→ 读 `MapAnchorConfig` 得候选锚点 → 读 `SpawnRuleConfig` 按 seed+阶段选子集 → 发 `InteractablesSpawnedEvent`。

---

## §6 文件路径契约（锁死；美术/程序/数据落盘位置）

```
# 策划/设计包（本 change 内，不进 Assets）
openspec/changes/26-fixed-map-three-themes/
  ├─ 三图策划案.md                       # 三图内容全写
  ├─ art/图一美术生产管线.md              # 六步管线 SOP
  ├─ art/maps/01-ai-ruins/               # 图一（AI_RUINS）：mask 设计稿 + 切块清单 + 物件清单
  ├─ art/maps/02-alien-hive/              # 图二（ALIEN_HIVE）大纲
  ├─ art/maps/03-virus-swamp/             # 图三（VIRUS_SWAMP）大纲
  ├─ art/mockups/                        # 效果图
  └─ art/raw/                            # AI 出图源 + 生成记录.md

# 运行时资源（Codex 实现时落盘）
Assets/Resources/Sprite/Map/<Theme>/     # Theme ∈ {AI_RUINS, ALIEN_HIVE, VIRUS_SWAMP}
  ├─ BaseMap/tile_r{行}_c{列}.png         # 底图 5×5=25 块，行列 0-4
  ├─ Props/<propName>.png                 # 物件层 billboard sprite
  └─ Mask/mask.png                        # 地貌 mask（也用于运行时派生 TerrainGrid）
Assets/Resources/DataTable/
  ├─ MapDefinitionConfig.json
  ├─ TerrainTypeConfig.json
  ├─ MapAnchor_<mapid>.json               # 每图一份
  └─ SpawnRuleConfig.json
Assets/Scripts/Modules/MapGen/            # Codex 重写：加载而非生成
Assets/Editor/MapMaskSlicer.cs            # mask/底图 5×5 切块工具
Assets/Editor/TerrainGridBaker.cs         # mask → TerrainType[,] 烘焙工具
```

---

## §7 更正项（相对既有 GDD）

| # | 既有 GDD 表述 | 本 change 更正 | 理由 |
|---|---|---|---|
| 1 | GDD 00 §三 USP3："颜色绑定末日成因" | **颜料颜色与地图主题、与末日成因均无机制绑定**。地图主题纯视觉氛围。 | 用户明确纠正（7/3）。颜料系统独立于地图。 |
| 2 | GDD 07 §9.1：地图用 **BSP 房间切割** | 改为**固定手工地貌**（既非 BSP，也非 region-growth 程序生成） | 两次演进：BSP→region-growth（revert）→固定手工。 |
| 3 | GDD 07：`MapGenModule` = Run 开始时**程序生成**大地图 | 改为**加载固定地图 + 按规则布点交互物** | 本 change 核心思路变更。 |

> 归档时同步修订 `项目知识库/GDD-v2/modules/07-MapGenModule.md` + `systems/07-地图生成.md` + `00-总策划案v2.md §三`。

---

## §8 确定性约束（锁死，伪联机→真联机前提）

- 交互物布点的**所有随机**必须用注入的 `System.Random(seed)`，**禁止 `UnityEngine.Random`**。
- 同 `seed` + 同 `MapId` → **逐个锚点选取结果完全一致**。
- 地貌固定（无随机），因此地貌部分天然确定性。
- `InitializeAsync` 期间**不发事件**（框架戒律）；生成触发点为订阅 `RunStartedEvent`。
