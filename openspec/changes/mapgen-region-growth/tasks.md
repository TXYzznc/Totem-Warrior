## 1. 阶段0 — 第一张地图策划设计

- [ ] 1.1 delegate gd-system/level-designer 出第一张地图详细策划：主题风格、美术方向
- [x] 1.2 定义地形集清单（6-8 种：陆/草/水/岸/山/荒原/沼泽…）及各自 IsWalkable / MoveSpeedMul
- [x] 1.3 定义邻接规则矩阵（哪些地形可相邻，水↔岸↔陆过渡链）
- [x] 1.4 定义热点清单（Boss/商人/工作室…）及开放式出生候选规则
- [x] 1.5 定义特征注入清单（河流/山/荒原）及 SpreadMode/数量/尺寸范围
- [x] 1.6 定义本期 1-2 个可交互特色（沼泽减速）的具体数值与效果
- [x] 1.7 定义正式风格美术需求：48 张地面 tile + 24-36 张物件 sprite（含拼图切图方案）
- [ ] 1.8 策划文档写入 openspec change 的 art/requirements.md，用户确认

## 2. 阶段1 — 生成算法核心（独立场景 + 占位色块 + 确定性单测）

- [x] 2.1 建独立开发场景 Assets/Scenes/MapGenSandbox.unity
- [x] 2.2 新增 3+1 张配置表 JSON（TerrainTypeConfig/TerrainAdjacencyRules/FeatureInjectionConfig/FeaturePointConfig）+ 跑 DataTableGenerator（需通知用户手动运行菜单）
- [x] 2.3 定义 MapGridData 数据结构（TerrainType[,] + CellSize + 热点列表 + 出生候选点 + 物件放置点）
- [x] 2.4 实现 RegionGrowthGenerator 纯 C# 类：阶段①撒热点点位（边界/间距约束 + 泊松式重摇）+ 开放式出生候选
- [x] 2.5 实现阶段②多源 BFS 区域生长（邻接白名单 + 权重 rng 选型 + 堵死降级）
- [x] 2.6 实现阶段③特征注入（Line/Blob 扩散 + 边缘过渡 tile + 邻接校验）
- [x] 2.7 全程 System.Random(seed)，确保确定性（禁 UnityEngine.Random）
- [x] 2.8 Sandbox driver：占位色块渲染（每 TerrainType 一色）+ 按键换 seed 重生成
- [x] 2.9 EditMode 单测：同seed同图/不同seed不同图/热点保底/出生候选/不越界/最小间距/无非法邻接/全图连通/特征数量/不同质/纯数据无GO
- [x] 2.10 先用 50m/100m 小图验证，再压 400m 实测生成耗时与内存

## 3. 阶段2 — 拼图美术素材（4 张母图→碎片化→Tilemap）

- [ ] 3.1 生成 4 张高精度完整母图：湿地通用地表 / AI 工厂废墟 / 障碍边缘 / 热点地标
- [ ] 3.2 从母图碎片化、修边、统一亮度，产出 48 张正式风格无缝地面 tile（8 类地形 × 6 变体）
- [ ] 3.3 从热点地标母图抠出 24-36 张正式风格物件 sprite（树/石/芦苇/功能建筑，直立 billboard 用）
- [ ] 3.4 正式素材入库 Assets/Resources/Sprite/Map/<Theme>/Formal/，配 TileBase 变体映射；Sandbox 默认用正式 tile 测算法效果
- [ ] 3.5 用户确认 tile 无缝拼接 + 物件风格一致

## 4. 阶段3 — 渲染接入（Tilemap + billboard）

- [x] 4.1 渲染层：MapGridData → Tilemap 批量 SetTile 铺地面（替换占位色块）
- [x] 4.2 物件放置点 → spawn billboard sprite（复用 change 25 BillboardSprite）
- [x] 4.3 验证 400m@2m 地面不逐格建 GameObject、帧率正常
- [ ] 4.4 相机俯角下 2.5D 观感确认（地面 tile + 直立物件）

## 5. 阶段4 — 特色可交互内容

- [x] 5.1 实现 TerrainEffectTracker（0.2s tick 查玩家所在格，非每帧、0 GC）
- [x] 5.2 沼泽减速接入现有移速链（MoveSpeed + MoveSpeedBonus 通道 / Status）
- [x] 5.3 验证进沼泽减速、离开恢复、普通地形无影响

## 6. 阶段5 — 接入现有游戏 + 缩圈重调

- [x] 6.1 MapGenModule.GenerateMap 切换到 RegionGrowthGenerator，发扩展 MapGeneratedEvent（含 Grid）
- [x] 6.2 MapTemplateConfig MapSize 150→400；ZoneShrinkConfig 半径按 400m 重调
- [x] 6.3 骨架先行裁定 MapGeneratedEvent 字段扩展，确认下游兼容（CameraModule/EventModule/Spawner/Enemy/NPC）
- [x] 6.4 接入现有游戏场景，下游联调跑通（相机边界适配、EventModule 遍历兼容）

## 7. 阶段6 — loop 全链路验收

- [x] 7.1 用 playtest-driver/unity-skills 自动进 Play Mode + 俯视截图
- [ ] 7.2 loop 迭代：改算法→自动跑→截图→用户目测地形自然→迭代
- [ ] 7.3 全链路验收：确定性测试通过 + 地形自然 + 真tile无缝 + ≥1特色生效 + 接入现有场景跑通 + 0 Error
- [x] 7.4 修订 GDD 07-MapGenModule.md + 系统 GDD §2.2（BSP→区域生长演进）
- [ ] 7.5 openspec archive-change + 更新知识库 INDEX.md
