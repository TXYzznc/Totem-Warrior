# Tasks — 固定地貌三主题地图（Codex 开发工作包）

> 执行前必读：[CONTRACT.md](./CONTRACT.md)（术语/数据/事件/路径唯一真源）→ [design.md](./design.md)（决策与理由）→ [proposal.md](./proposal.md)。
> 图一内容依据 [三图策划案.md](./三图策划案.md) §一；美术执行依据 [art/图一美术生产管线.md](./art/图一美术生产管线.md)。
> 本 change **不含已完成代码**，以下全部是待实现任务。按阶段顺序执行，阶段内任务可并行。

---

## 阶段 0：数据契约落地（不碰美术，纯结构）

- [ ] **T0.1** 新建 `Assets/Scripts/Modules/MapGen/Data/TerrainType.cs`：按 CONTRACT §4.1 定义 6 值枚举 `None/Ground/Slow/Blocked/Cover/Hazard`。
- [ ] **T0.2** 新建 `Assets/Scripts/Modules/MapGen/Data/InteractableKind.cs`：按 CONTRACT §4.2 定义 `Enemy/Boss/Npc/Chest/Item/PaintNode/EventRoom`。
- [ ] **T0.3** 新建 DataTable：
  - `Assets/Resources/DataTable/MapDefinitionConfig.json`（3 行：AiRuins/Alien/Virus，字段见 CONTRACT §5）
  - `Assets/Resources/DataTable/TerrainTypeConfig.json`（6 行，字段：`Id,TypeName,TileColorHex,IsWalkable,MoveSpeedMul,BlocksVision,HazardDps`）
  - `Assets/Resources/DataTable/SpawnRuleConfig.json`（图一部分先填，见三图策划案 §一.4）
  - 每图独立：`Assets/Resources/DataTable/MapAnchor_AiRuins.json`（其余两图待美术定稿后再建）
  - 跑 `Tools/DataTable/生成全部配置表代码` 生成对应 `Assets/Scripts/DataTable/*.cs`。**此步骤需人工在 Unity Editor 执行，AI 完成表结构后必须通知用户手动跑一次生成器**。
- [ ] **T0.4** 新建 `Assets/Scripts/Modules/MapGen/Events/MapGenEvents.cs` 追加：
  - 调整 `MapGeneratedEvent`：新增 `TerrainCellSize/TerrainGridWidth/TerrainGridHeight` 三字段（按 CONTRACT §3.1，**不删旧字段**）
  - 新增 `InteractablesSpawnedEvent` + `AnchorPlacement` struct（按 CONTRACT §3.2）
- [ ] **T0.5** 单元测试骨架：`Assets/Tests/EditMode/MapGen/` 新建 `MapDataContractTests.cs`，验证三张 DataTable 能被 `DataTableModule` 正确加载、字段数量与类型匹配 CONTRACT §5。

**验收**：编译通过；DataTableGenerator 跑通；EditMode 测试全绿。**不涉及任何美术资源**。

---

## 阶段 1：Editor 工具（mask → 数据层 / 切块）

- [ ] **T1.1** `Assets/Editor/TerrainGridBaker.cs`：菜单项 `Tools/MapGen/Bake Terrain Grid`。输入一张 mask PNG（Texture2D，Read/Write 开启），按 CONTRACT §2 降采样到 100×100（4m/格），每格取该采样区主色，反查 `TerrainTypeConfig.TileColorHex`（容差匹配，允许 ±10 RGB），未匹配颜色降级为 `Ground` 并 `Debug.LogWarning`。输出 `TerrainType[100,100]` 序列化为二进制资源（`Assets/Resources/DataTable/TerrainGrid_<MapId>.bytes`，逐字节 byte 数组，行主序）。
  - 验收测试（Spec `map-fixed-terrain` Requirement"mask 派生 TerrainGrid"）：同一 mask 连续烘焙 100 次，结果逐字节一致。
- [ ] **T1.2** `Assets/Editor/MapMaskSlicer.cs`：菜单项 `Tools/MapGen/Slice Base Map`。输入一张高清基准图（矢量化/放大后的 mask，建议边长 ≥10240px，5 的整数倍）与目标输出目录，均匀切成 5×5=25 块，按 `tile_r{行}_c{列}.png` 命名（行列 0-4，行=Y 方向从上到下，列=X 方向从左到右，与世界坐标 (0,0) 左下角的映射关系在工具注释中写清楚：`row 0` 对应世界 Z 高值，`row 4` 对应世界 Z=0`附近，需在实现时与美术核对一次，避免上下颠倒）。
  - 验收测试（Spec `map-art-pipeline` Requirement"切块可逆"）：切块后按同一坐标映射拼回，与原图逐像素一致（用简单的图像拼接脚本验证，PSNR=∞ 或 MD5 一致）。
- [ ] **T1.3** `Assets/Editor/MapSpriteImportProcessor.cs`（可复用旧实验版本思路，重新实现）：`AssetPostprocessor`，对 `Assets/Resources/Sprite/Map/**` 下贴图自动设置 `TextureType=Sprite`、关闭 mipmap（地面 tile 用 Point 平铺时按需）、设置合适的 `maxTextureSize`。

**验收**：两个 Editor 工具在 Unity 中可手动运行且有明确日志输出；EditMode 测试覆盖确定性与可逆性。

---

## 阶段 2：MapGenModule 重写（加载 + 布点，不再生成）

- [ ] **T2.1** 重写 `Assets/Scripts/Modules/MapGen/MapGenModule.cs`：
  - 删除 `BuildPlaceholderGeometry` / `BuildBoundaryWalls` / `BuildPlaceholderRooms` 等占位几何代码（CreatePrimitive 系列全部移除）。
  - 新增 `LoadMap(int mapId, int seed)`：读 `MapDefinitionConfig` → 加载 `TerrainGrid_<MapId>.bytes` → 加载 BaseMap 25 块 Sprite → 加载 PropLayer → 发布调整后的 `MapGeneratedEvent`。
  - 新增 `public TerrainType QueryTerrain(Vector3 worldPos)`：按 CONTRACT §2 换算到 4m 采样格索引，越界返回 `Blocked`，O(1) 查表，**不产生 GC 分配**（数组直接索引，不用 LINQ）。
  - 新增交互物布点逻辑 `GenerateInteractablePlacements(int seed)`：读 `MapAnchorConfig` + `SpawnRuleConfig`，用 `System.Random(seed)`（**严禁 `UnityEngine.Random`**）按缩圈阶段分层选取，做 `MinSpacing` 间距校验，保底类（Boss/纹身师/商人）强制选中，产出 `List<AnchorPlacement>`，发布 `InteractablesSpawnedEvent`。**本方法不实例化任何 GameObject**（职责边界见 Spec `map-interactive-spawn` 最后一条 Requirement）。
  - 缩圈逻辑（`ITickable` 部分）**保留现有实现**，仅将 `_mapSize` 固定读取为 400（来自 `MapDefinitionConfig`），半径数值待 T2.2 重调。
- [ ] **T2.2** 更新 `Assets/Resources/DataTable/ZoneShrinkConfig.json`：三段半径按 400m 重新标定（design.md §8 建议初始圈 ≈180m 覆盖大部分图，具体数值实现时可调优，非契约锁死项）。
- [ ] **T2.3** 新增 `Assets/Scripts/Modules/MapGen/Runtime/TerrainEffectTracker.cs`：0.2s tick（非每帧），遍历当前活跃 actor 列表，查 `QueryTerrain(actor.position)`：
  - `Slow`：施加 `MoveSpeedMul` 到现有移速计算链（复用 `CombatModule` 的 `MoveSpeed` 叠加点，具体接入方式实现时读 `PlayerStats`/`PassiveStats` 现状确定）。
  - `Blocked`：本 tick 不做主动阻挡（阻挡应在移动系统层拦截，见 T2.4），此处仅用于状态查询。
  - `Cover`：写入一个轻量"被遮蔽"标记（供 Combat 命中判定读取，具体消费在 Combat 侧后续任务，本次只需暴露状态查询接口）。
  - `Hazard`：按 `HazardDps × 0.2` 每 tick 施加伤害，走现有 Status/Combat 伤害通道。
- [ ] **T2.4** 移动系统接入 `Blocked` 阻挡：需求方定位 `HumanPlayerController` / `LightBotPlayerController` / `SmartBotPlayerController` 的位移写入点，在写入前调用 `QueryTerrain` 校验目标格非 `Blocked`，是则位移分量清零或投影到可行走方向（简单实现：整体取消该帧位移，不做滑墙优化）。AI 侧（无 NavMesh）同理，在其移动决策后加一次 `Blocked` 校验。
- [ ] **T2.5** `ShutdownAsync` 清理：释放 BaseMap/PropLayer 资源引用，清空 TerrainGrid 缓存，清空布点结果缓存。

**验收**：`RunStartedEvent` 触发后 1.5s 内发布 `MapGeneratedEvent`；EditMode 测试验证同 seed 同布点、Boss/NPC 保底、锚点全部落在可行走格。

---

## 阶段 3：下游联调

- [ ] **T3.1** `SpawnerModule` / `EnemyModule` / `NPCModule` / `EconomyModule` / `EventModule` 订阅 `InteractablesSpawnedEvent`，各自按 `AnchorPlacement.Kind` 过滤并实例化对应实体（现状这些模块多为占位，具体实例化逻辑遵循各自模块详设文档，本 change 只保证事件契约到位）。
- [ ] **T3.2** `CameraModule`（change 25）确认边界 clamp 读取新的 `MapSize=400` 无需改动核心代码，只需联调验证。
- [ ] **T3.3** UIModule MiniMap（若已存在）确认能消费调整后的 `Rooms`/`InitialZoneCenter` 字段，无需改动核心代码。
- [ ] **T3.4** PlayMode 集成测试：`RunStarted_Triggers_MapGenerated_Within1_5s`（发 `RunStartedEvent`，1.5 秒内收到 `MapGeneratedEvent`）；`InteractablesSpawned_AfterMapGenerated`（`InteractablesSpawnedEvent` 必须在 `MapGeneratedEvent` 之后发布）。

**验收**：三事件按正确顺序发布；下游模块各自消费无越界（不实例化非自己职责的 Kind）。

---

## 阶段 4：图一美术资源产出

> 详细 SOP 见 [art/图一美术生产管线.md](./art/图一美术生产管线.md)。本阶段产出物直接落盘到 T1/T2 消费的路径。

- [ ] **T4.1** 图一（AiRuins）地貌 mask 设计稿：`openspec/changes/26-fixed-map-three-themes/art/maps/01-ai-ruins/mask-design.png` + 配色说明（6 种 TerrainType 对应色值，需与 `TerrainTypeConfig.TileColorHex` 完全一致）。**用户确认后**才能进入下一步。
- [ ] **T4.2** mask 矢量化/超分放大得高清基准图（≥10240×10240，5 的整数倍）。
- [ ] **T4.3** 用 `MapMaskSlicer`（T1.2）切成 25 块，逐块调用绘图工具按 AI 废墟风格重绘（结构约束=对应 mask 子图），产出 `Assets/Resources/Sprite/Map/AiRuins/BaseMap/tile_r{0-4}_c{0-4}.png`。
- [ ] **T4.4** 25 块无缝拼接校验（脚本拼接后目视/直方图比对边界，无明显断缝）。
- [ ] **T4.5** 物件层（建筑/自然景物）设计与出图，落盘 `Assets/Resources/Sprite/Map/AiRuins/Props/`，含图一策划案 §一.3 列出的全部物件清单。
- [ ] **T4.6** mask 落盘 `Assets/Resources/Sprite/Map/AiRuins/Mask/mask.png`，跑 T1.1 烘焙工具生成 `TerrainGrid_AiRuins.bytes`。
- [ ] **T4.7** 图一 `MapAnchorConfig`/`MapAnchor_AiRuins.json` 按三图策划案 §一.4 的锚点清单落表。

**验收**：图一在 Editor 中可视化拼接后与 mask 结构一致；TerrainGrid 烘焙无未匹配颜色警告；锚点全部落在可行走格（脚本校验）。

---

## 阶段 5：文档归档

- [ ] **T5.1** 修订 `项目知识库（AI自行维护）/GDD-v2/modules/07-MapGenModule.md`：反映"加载固定地图 + 布点交互物"新职责，删除 BSP 相关描述。
- [ ] **T5.2** 修订 `项目知识库（AI自行维护）/GDD-v2/systems/07-地图生成.md`：同步新方案（若该文件为空/待写，直接以本 change 内容起草）。
- [ ] **T5.3** 修订 `项目知识库（AI自行维护）/GDD-v2/00-总策划案v2.md` §三：更正"颜色绑定末日成因"表述（CONTRACT §7 更正项 #1）。
- [ ] **T5.4** `openspec archive-change 26-fixed-map-three-themes`，同步更新 `项目知识库（AI自行维护）/INDEX.md`。

---

## 全局验收清单（对应 grill 共识 #4）

- [ ] 三图策划案全部写完（内容/特色区/锚点清单/尺寸标定）
- [ ] 图一完整开发包：数据契约 + Editor 工具 + MapGenModule 重写方案 + 美术六步管线 + 全部美术资源落盘
- [ ] 图二/图三仅策划大纲，`MapDefinitionConfig` 行存在但美术占位
- [ ] 确定性验证：同 seed 同图同布点（EditMode 测试覆盖）
- [ ] 三事件（MapGenerated → InteractablesSpawned → ZoneShrinkPhase）顺序正确
- [ ] 特色地形效果（Slow/Blocked/Cover/Hazard）对玩家与 AI 生效一致
- [ ] 归档同步 GDD 三处文档
