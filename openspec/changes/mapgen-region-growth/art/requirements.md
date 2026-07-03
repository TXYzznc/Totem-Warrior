# 第一张地图策划与美术需求

## 地图定位

- **主题名**：AI_RUINS_WETLAND
- **一句话目标**：在失控 AI 遗迹周边生成一片湿地化废墟，水道、岸线、草地、荒原和机械残骸像拼图一样自然过渡。
- **玩法意图**：玩家每局从开放地图中的随机安全候选点出生；AI 也分散落地。整局不预设“出生→工作室/商人→Boss”的线性路线，Boss、商人、纹身工作室是可发现的地图热点，缩圈才是唯一持续强化的方向压力。地形本身提供轻量策略：沼泽减速、水体阻隔、山/废墟形成绕行。
- **视觉方向**：饥荒式 2.5D + Hades 风高细节手绘。地面为正俯视无缝 tile，物件为直立 billboard sprite。整体颜色为冷灰蓝遗迹 + 暗绿色湿地 + 霓虹青色 AI 残光 + 黄色警示色。整体氛围要比当前暗稿更明亮、更可读：保留暗色废墟基调，但提高中间调亮度、湿面反光和功能色对比，确保算法测试截图中能看清地形边界、通路、湿地、水体与废墟材质。
- **风格基准**：对齐现有角色 sprite 的暗色手绘质感、清晰轮廓、金属/皮革高光密度。AI_RUINS 地图必须体现失控霓虹工厂、断裂电缆、伺服机械残骸、黄色警示线、电路板纹理、油渍与湿地污水，不做低细节卡通色块。

## 地形集

| Id | Type | 用途 | IsWalkable | MoveSpeedMul | GrowthWeight |
|---:|---|---|---:|---:|---:|
| 1 | Grass | 泛化安全地形、主要连通层 | true | 1.00 | 60 |
| 2 | RuinFloor | 遗迹硬质地面、功能点承载层 | true | 1.00 | 35 |
| 3 | Shore | 水陆过渡岸线 | true | 0.95 | 24 |
| 4 | Water | 河/池塘，不可直接行走 | false | 0.00 | 8 |
| 5 | Swamp | 湿地，减速特色地形 | true | 0.65 | 16 |
| 6 | Wasteland | 荒原/裸土，压低饱和度 | true | 0.90 | 20 |
| 7 | Mountain | 废墟山体/高墙障碍 | false | 0.00 | 6 |
| 8 | Forest | 灌木/芦苇密集区，可行走但视觉遮挡 | true | 0.85 | 16 |

## 邻接规则

核心过渡链：`Water <-> Shore <-> Grass/RuinFloor/Swamp/Wasteland/Forest/Mountain`。

- Water 只能邻接 Water / Shore。
- Shore 可邻接所有地形，承担过渡缓冲。
- Grass / RuinFloor 是最泛化可行走地形，可互相邻接，也可邻接 Shore / Swamp / Wasteland / Forest / Mountain。
- Swamp 可邻接 Shore / Grass / RuinFloor / Forest / Swamp。
- Wasteland 可邻接 Grass / RuinFloor / Mountain / Wasteland / Shore。
- Mountain 可邻接 Shore / Grass / RuinFloor / Wasteland / Mountain。
- Forest 可邻接 Shore / Grass / RuinFloor / Swamp / Forest。

## 地图热点

| Id | PointType | Required | SafeMargin(m) | MinSpacing(m) | PreferredTerrain |
|---:|---|---:|---:|---:|---|
| 2 | Boss | true | 36 | 120 | RuinFloor |
| 3 | Merchant | true | 24 | 80 | RuinFloor |
| 4 | TattooStudio | true | 24 | 80 | RuinFloor |

撒点约束：

- 所有热点必须落在地图边界安全距离内。
- 小图测试（50m / 100m）允许按比例压缩间距；正式 400m 图使用表中数值。
- 重摇上限后可放宽间距，但不得越界，且 Required 热点永不缺失。

## 开放式出生规则

- 出生不是 `FeaturePointConfig` 中的固定功能点，不生成“出生点信标”，不作为玩家路线起点。
- MapGen 在全图可行走连通区域内输出一组 `SpawnCandidates`；Spawner 按 seed 顺序从候选点中为玩家与 AI 分散抽样。
- 候选点必须可行走，避开 Water / Mountain，离地图边界有安全距离，尽量远离 Boss / Merchant / TattooStudio 热点，并保持玩家 / AI 初始间距。
- 若小图或极端地形导致候选不足，允许逐步放宽“远离热点/彼此间距”，但不得落到不可行走格。

## 特征注入

| Id | FeatureName | TerrainType | SpreadMode | CountMin | CountMax | SizeMin | SizeMax |
|---:|---|---|---|---:|---:|---:|---:|
| 1 | BrokenCreek | Water | Line | 1 | 2 | 10 | 24 |
| 2 | WetlandPatch | Swamp | Blob | 3 | 6 | 5 | 12 |
| 3 | RubbleRidge | Mountain | Blob | 2 | 4 | 4 | 10 |
| 4 | DeadZone | Wasteland | Blob | 2 | 5 | 6 | 14 |

规则：

- 河流/水体边缘必须自动补 Shore。
- Blob 特征覆盖时必须遵守邻接合法性；不合法则跳过该格或补过渡。
- 注入后最高频地形占比目标 ≤ 85%，至少 3 种地形同时出现。

## 本期可交互特色

### 沼泽减速

- 触发地形：Swamp
- 数值：MoveSpeedMul = 0.65
- 检测：`TerrainEffectTracker` 每 0.2s 根据玩家世界坐标查询当前格。
- 效果：进入沼泽后有效移速乘以 0.65，离开后恢复。
- 约束：不在 `Update` 每帧扫描全图，不新建平行移速系统，后续接现有 MoveSpeed / Status 通道。

## 美术资产清单

### 母图设计（4 张完整场景图）

正式拼图资源不直接散生成 48 张 tile，而是先生成 4 张统一风格的顶视/轻俯视完整场景母图，再从母图碎片化处理成 Tilemap 可用资源。这样能保证统一光照、统一材质密度、统一色调，并模仿 Unity 项目常见的模块化环境包工作流：先有完整环境画面，再拆成可复用地块、边缘件、装饰件和热点件。

| 母图 | 尺寸建议 | 覆盖内容 | 后续拆分目标 |
|---|---:|---|---|
| A. 遗迹湿地通用地表 | 2048×2048 | Grass / Swamp / Shore / Water 的自然过渡，浅水、泥岸、芦苇、污水反光 | 草地、沼泽、水体、岸线基础 tile |
| B. AI 工厂废墟地表 | 2048×2048 | RuinFloor / Wasteland，金属板、电路板、混凝土、警示线、油污 | 遗迹地板、荒原、烧灼地、管线 tile |
| C. 障碍与边缘拼图片区 | 2048×2048 | Mountain / Forest / Shore 边缘，废墟墙基、钢梁碎堆、芦苇根部、藤蔓电缆 | 山体/墙基/森林底层 tile 与边缘过渡件 |
| D. 热点与地标小场景 | 2048×2048 | Boss 核心残骸、商人帐篷、纹身工作室、周边铺地和招牌 | 功能建筑 billboard、地标底座、局部装饰件 |

母图要求：

- 亮度：不做纯黑底；地面中间调可读，青蓝发光与黄色警示线负责导视。
- 视角：地表母图以正顶视为主；地标母图可使用轻俯视，便于切出直立 billboard。
- 统一性：4 张母图使用同一色板、同一笔触、同一材质密度。
- 可碎片化：母图内避免大面积唯一图案占满画面；每 512×512 区块都应能独立成立。

### 地面 Tile（正式风格，48 张）

地形 tile 以 8 类地形 × 6 变体交付，共 48 张。每张建议 512×512 源图，导入 Unity 后可按 2m 逻辑格映射。所有 tile 都必须是可平铺顶视图，中心区域不能出现不可控方向性大物件；方向性边缘资源另起命名，不混入基础变体。

| TerrainType | 数量 | 变体要求 |
|---|---:|---|
| Grass | 6 | 湿草、泥草、断线缆草、轻微电蓝残光、油污草、稀疏碎石草 |
| RuinFloor | 6 | 裂纹金属地板、电路板纹、警示线、破碎混凝土、嵌入管线、湿滑反光 |
| Shore | 6 | 泥岸、碎石岸、金属护岸、污水边缘、芦苇岸、破碎混合岸 |
| Water | 6 | 深污水、浅污水、电蓝反光、油膜、漂浮碎片、暗流纹 |
| Swamp | 6 | 厚泥、气泡、腐植、机械污泥、蓝绿荧光菌斑、脚印扰动 |
| Wasteland | 6 | 裸土、灰烬、废料粉尘、干裂泥、烧灼痕、金属碎屑 |
| Mountain | 6 | 废墟墙基、机械山体、混凝土块、钢梁碎堆、黑岩、警示结构残片 |
| Forest | 6 | 芦苇根部、灌木根部、枯枝、藤蔓线缆、湿苔、暗绿遮挡底层 |

### Billboard Sprite

- 枯树 / 灌木 / 芦苇 / 藤蔓电缆
- 石块 / 机械残骸 / 废墟墙片 / 钢梁碎堆 / 警示牌
- Boss 区大门 / 核心残骸 / 电流塔
- 商人帐篷 / 纹身工作室小屋
- 数量目标：正式风格 24-36 张，用于 400m 地图的密度测试。

## 切图方案

- 地面 tile 以 2m 逻辑格为单位映射，正式风格源图使用 512x512。
- 生成顺序：先出 4 张 2048×2048 完整母图 → 选取/裁切候选 512×512 区块 → 修边成可平铺 tile → 统一亮度/色阶 → 入 Unity 测拼接 → 根据截图回修母图或单 tile。
- 碎片化规则：基础地形 tile 从母图中心区域裁；边缘/岸线 tile 从母图的自然过渡区裁；billboard 从地标母图中抠出，单独做透明背景。
- Unity 模仿目标：采用 Tilemap + RuleTile/Wang Tile 思路组织资源。基础地形先按 TerrainType 随机变体铺设；后续需要更自然岸线时，扩展 Shore/Water 的方向规则 tile（直边、内角、外角、端头、四通），而不是在算法层硬编码图片。
- 岸线需独立方向变体，避免 Water 直接贴 Grass/RuinFloor。
- 普通文字和 UI 标识不做成地图 sprite，功能建筑用图标/招牌 sprite 表达。
- 本次算法效果验收必须使用正式风格 tile；纯色/噪声占位只允许用于性能 smoke test，不能用于地形自然度和风格验收。

## 待用户确认

- 主题是否锁定为 `AI_RUINS_WETLAND`。
- 是否接受“开放式随机出生 + 缩圈驱动方向性”，不做线性出生路线。
- 是否接受本期只实现“沼泽减速”一个交互特色。
- 地图 1 正式风格资源范围已按最大推荐值执行：48 张地形 tile + 24-36 张 billboard sprite。
