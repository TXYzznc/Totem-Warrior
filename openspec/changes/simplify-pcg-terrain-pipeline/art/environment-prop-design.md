# PCG 环境立物、功能区与主题地标设计

> 状态：**设计待审核**。本文只冻结首批资源的视觉与投放合同；尚未生成图片、导入 Unity、改动运行时代码或新增交互规则。

## 1. 已确认的设计边界

- 三张正式地图：AI 遗迹、异形巢穴、病毒沼泽；每张各有四种正式地貌。
- 每种地貌首批准备 **3 个静态立物 + 2 个可交互立物**，共 60 项。
- 项目通用功能区准备 8 项可复用的建设模块，并增加 6 类通用地表点缀；每张地图准备 **3 个特色静态地标 + 2 个特色交互地标**，共 23 项。
- 原有的 83 项是核心清单。新增的 6 类地表点缀各有 AI 遗迹、异形巢穴、病毒沼泽三种主题化实物版本，因此首批实际绘制目标调整为 **101 项**（83 + 18）。其中「通用」指功能和结构语言通用；出生、纹身师、商人、Boss 等关键地点一律使用各地图自己的主题化外观，不复用一张中性大建筑图。
- 可交互立物只绑定现有锚点与现有行为：`Chest`、`Resource`（当前为武器拾取）、`Event`（当前为选择事件）、`Tattooist`、`Merchant`。本期不增加掉落、战斗、任务、数值或新交互类型。
- 地貌仍直接相邻；立物和交界装饰只丰富空间，不承担地貌融合或特殊边缘拼图职责。

## 2. 统一美术与技术约束

### 视觉语言

- 视角：俯视偏斜的 2D 立物，底部可明确落在地表；大轮廓优先于细碎纹理。
- 风格：中等精细度的手绘像素风。保留像素块、简化体块和有限高光，避免写实高频材质、照片感及过度密集的植物叶片。
- 变体：同一地貌中的物件共享主色、材质和轮廓语言；差异只来自姿态、损坏程度、小型附着物和局部亮点，不能变成另一种生物群或另一套色板。
- 透明：立物原图可先以纯 `#00FF00` 绿幕生成，再统一抠成透明 PNG；绿幕必须纯色、无阴影、无渐变、无绿色反光。运行时资源必须透明背景，不能保留绿边。
- 尺度：地貌格仍为 256×256。小物优先 256×256，中物可为 256×384 / 384×384，大地标可为 512×512 / 512×768；透明画布尺寸可以不同，但同主题维持统一像素密度。
- 锚点：所有立物使用底部中心 pivot；可交互物在可见主体脚边留下无纹理的交互落点，避免角色被高耸图像遮住。

### 第一批投放规则

1. 静态立物只作空间装饰，首批没有碰撞、伤害或阻挡效果；生成器把它们放在可行走区域边缘或空闲格，避免遮住角色和交互物。
2. 可交互立物是现有实体的主题化外观壳：保留原来的 `PayloadId`、交互半径、奖励和 UI 流程。
3. 每项资源有稳定 ID、权重、允许地貌、允许房间和占地尺寸。相同 seed 下选择结果必须可复现。
4. 地图特色地标不是随机满图散落物：它们固定服务于对应房间/锚点；地貌立物才从池中随机抽取。
5. 立物配置以后应放在 `Assets/Game/Sprites/PCG/Props/<Theme>/...`，并由正式 PCG catalog 记录；不放到测试场景专用目录。

## 3. 通用建设模块（8 项）

这些是幸存者/探索者留下的轻量设施，作为各主题功能区的辅件。它们不取代主题地标，而是让同一功能在三张地图中拥有一致的可读性。

| ID | 类型 | 名称与外观 | 对应现有用途 |
| --- | --- | --- | --- |
| `common_field_lamp` | 静态 | 可折叠野外灯柱，暖白小灯、磨损金属底座 | 出生区、商人区、宝箱角落的引导光 |
| `common_shelter_canopy` | 静态 | 破旧帆布棚、两根支架、收纳网 | 安全角/商人区的上方轮廓 |
| `common_supply_stack` | 静态 | 帆布包、旧箱和捆扎线组成的补给堆 | 出生区、补给区、房间边缘 |
| `common_signal_beacon` | 静态 | 三角支架信标，低频闪烁色由地图主题决定 | 出生与遭遇区定位 |
| `common_barricade` | 静态 | 可移动护板、绳索和警示布条 | 房间边缘、通道口，纯装饰 |
| `common_cache_nook` | 交互辅件 | 有遮雨盖的储藏凹位，前方放现有普通/稀有宝箱 | `Chest` |
| `common_weapon_rack` | 交互辅件 | 竖直的武器架或固定夹具，当前武器拾取挂在其上 | `Resource` |
| `common_choice_plinth` | 交互辅件 | 简洁双面石/金属台座，表达“二选一”而不写文字 | `Event` |

### 3.1 通用地表点缀层（6 类 × 3 个主题版本）

这层全部为静态、低占地、无碰撞资源，用于打散大块地貌的重复感。它们不是“所有地图共用同一张草或树”的贴图：三张地图共享的是类别和投放逻辑，实物必须按各自的生态与材质重新绘制。这样既保留树、花、草的自然层次，也不会把自然植被硬放进 AI 遗迹或异形巢穴。

| 通用类别 ID | 病毒沼泽版本 | AI 遗迹版本 | 异形巢穴版本 | 建议地貌/投放限制 |
| --- | --- | --- | --- | --- |
| `terrain_groundcover` | 草簇、蕨叶、湿苔 | 苔痕、短线缆、裂缝杂草 | 膜丝、细小菌毯 | 除深水/强酸外的低密度空闲格 |
| `terrain_bloom` | 小花、菌伞、发光花蕾 | 发光苔点、故障指示灯簇 | 孢子灯泡、小型发光囊 | 仅作为稀疏亮点；不得铺满地表 |
| `terrain_shrub` | 灌木、藤团、芦苇丛 | 藤蔓包住的设备残片、低矮线束团 | 触须簇、幼体囊群 | 草地、腐化地、遗迹生长区、巢穴膜区 |
| `terrain_vertical_growth` | 小树、枯树、弯曲红树林 | 锈蚀支架上长出的垂蔓、断裂天线柱 | 高孢子茎、细骨刺柱 | 远离交互点；只放房间边缘或通道外侧 |
| `terrain_fallen_debris` | 倒木、树桩、漂木 | 倒塌护栏、短管、断电缆盘 | 脱落甲壳、空卵壳、骨片堆 | 可放于大多数非水面格；不覆盖角色路线 |
| `terrain_outcrop` | 苔石、树根包石、泥岩 | 混凝土残块、机械基座、锈蚀板岩 | 树脂瘤、钙化组织、酸蚀骨岩 | 房间边缘和地貌交界附近，作为空间锚点 |

这 18 张主题化静态资源的优先级低于固定房间地标与可交互物：在性能或制作批次需要收缩时，先减少它们的变体数量，不削减交互可读性。

## 4. 三张地图的特色建筑与区域（15 项）

每张地图的五项共同承担其正式房间的记忆点。其中两项直接包裹既有 NPC 锚点；不会创建新的 NPC 或服务。

| 地图 | ID | 类型 | 地标设计 | 绑定位置/锚点 |
| --- | --- | --- | --- | --- |
| AI 遗迹 | `ai_ruins_fallen_gate` | 静态 | 倾倒的环形闸门、断裂线路和冷色导向灯 | `PlayerSpawn` / 出生区 |
| AI 遗迹 | `ai_ruins_command_spire` | 静态 | 斜立指挥尖塔、坏掉的屏幕与环绕电缆 | `BossSpawn` / Boss 区背景 |
| AI 遗迹 | `ai_ruins_power_relay` | 静态 | 三角电力继电器、裸露管束和低亮故障灯 | 遭遇区/通道视觉节点 |
| AI 遗迹 | `ai_ruins_ink_terminal` | 交互 | 维修舱改造的纹身终端，NPC 在舱前工作 | `Tattooist` |
| AI 遗迹 | `ai_ruins_scrap_kiosk` | 交互 | 半自动废料交易亭、悬挂货篮和价签屏 | `Merchant` |
| 异形巢穴 | `alien_hive_brood_gate` | 静态 | 骨质肋拱与破壳堆成的入口腔 | `PlayerSpawn` / 出生区 |
| 异形巢穴 | `alien_hive_queen_core` | 静态 | 巨型空巢心室、搏动膜壁与脊骨冠 | `BossSpawn` / Boss 区背景 |
| 异形巢穴 | `alien_hive_resin_bridge` | 静态 | 树脂横桥、垂落粘丝和包裹物 | 遭遇区/通道视觉节点 |
| 异形巢穴 | `alien_hive_spore_mender` | 交互 | 孢囊修复台，纹身师站在半透明孢膜前 | `Tattooist` |
| 异形巢穴 | `alien_hive_trade_larva` | 交互 | 肋骨围成的交易囊室，货品以树脂包裹悬挂 | `Merchant` |
| 病毒沼泽 | `virus_swamp_stilt_landing` | 静态 | 倾斜木栈桥、系绳浮桶和昏黄油灯 | `PlayerSpawn` / 出生区 |
| 病毒沼泽 | `virus_swamp_flooded_lab` | 静态 | 被根系吞没的实验舱、破窗与黑水泵 | `BossSpawn` / Boss 区背景 |
| 病毒沼泽 | `virus_swamp_signal_tower` | 静态 | 腐蚀的无线电塔、缠绕藤蔓和救援布条 | 遭遇区/通道视觉节点 |
| 病毒沼泽 | `virus_swamp_herbal_shack` | 交互 | 高脚草药棚、挂瓶与净化火盆，纹身师在棚前 | `Tattooist` |
| 病毒沼泽 | `virus_swamp_salvager_skiff` | 交互 | 搁浅小艇、打捞网和悬挂货包 | `Merchant` |

## 5. 地貌立物资源池（60 项）

说明：`Chest` 代表既有普通/稀有宝箱实体的主题外壳；`Resource` 代表既有武器拾取实体的放置器；`Event` 代表既有选择事件的视觉台座。括号中的内容是用途，不是新增玩法。

### AI 遗迹

| 地貌 | 静态立物（3） | 可交互立物（2） |
| --- | --- | --- |
| `ruins_floor` | `ai_floor_cable_bloom` 盘绕线束；`ai_floor_service_cart` 翻倒维修车；`ai_floor_sensor_post` 折断感应柱 | `ai_floor_data_cache`（Chest）；`ai_floor_decision_console`（Event） |
| `ruins_metal` | `ai_metal_cooling_fan` 停转风机；`ai_metal_valve_cluster` 阀门管组；`ai_metal_chain_hoist` 轨道吊钩 | `ai_metal_sealed_locker`（Chest）；`ai_metal_weapon_clamp`（Resource） |
| `ruins_growth` | `ai_growth_overgrown_panel` 长满苔痕的面板；`ai_growth_rooted_drone` 被根须缠住的无人机；`ai_growth_broken_rail` 塌陷轨道 | `ai_growth_root_cache`（Chest）；`ai_growth_bio_signal_node`（Event） |
| `ruins_coolant` | `ai_coolant_pipe_arch` 结霜弯管；`ai_coolant_pump_skid` 泵机底座；`ai_coolant_insulation_drum` 隔热桶 | `ai_coolant_emergency_case`（Chest）；`ai_coolant_weapon_cradle`（Resource） |

### 异形巢穴

| 地貌 | 静态立物（3） | 可交互立物（2） |
| --- | --- | --- |
| `hive_chitin` | `hive_chitin_rib_spire` 肋骨尖柱；`hive_chitin_shell_heap` 破壳堆；`hive_chitin_fence` 骨片栅栏 | `hive_chitin_brood_cache`（Chest）；`hive_chitin_rite_pod`（Event） |
| `hive_membrane` | `hive_membrane_hanging_sac` 垂挂囊袋；`hive_membrane_web_column` 膜丝柱；`hive_membrane_vent` 呼吸孔 | `hive_membrane_weapon_nest`（Resource）；`hive_membrane_choice_organ`（Event） |
| `hive_resin` | `hive_resin_stalagmite` 树脂柱；`hive_resin_cocoon_stack` 包裹物堆；`hive_resin_arch` 半透明树脂拱 | `hive_resin_trapped_cache`（Chest）；`hive_resin_weapon_sheath`（Resource） |
| `hive_acid` | `hive_acid_bubble_vent` 冒泡酸孔；`hive_acid_bone_ridge` 腐蚀骨脊；`hive_acid_shed_shell` 脱落甲壳 | `hive_acid_resistant_cache`（Chest）；`hive_acid_choice_gland`（Event） |

### 病毒沼泽

| 地貌 | 静态立物（3） | 可交互立物（2） |
| --- | --- | --- |
| `swamp_grass` | `swamp_grass_reed_clump` 高草簇；`swamp_grass_dead_stump` 白化树桩；`swamp_grass_fallen_log` 苔藓倒木 | `swamp_grass_fisher_cache`（Chest）；`swamp_grass_weapon_stake`（Resource） |
| `swamp_mud` | `swamp_mud_uprooted_tree` 翘根枯树；`swamp_mud_stake_totem` 木桩图腾；`swamp_mud_broken_skiff` 破小船 | `swamp_mud_buried_cache`（Chest）；`swamp_mud_choice_shrine`（Event） |
| `swamp_corruption` | `swamp_corruption_tumor_reeds` 病变芦苇；`swamp_corruption_black_crystal` 黑色感染晶；`swamp_corruption_cocoon` 破裂感染茧 | `swamp_corruption_quarantine_case`（Chest）；`swamp_corruption_weapon_root`（Resource） |
| `swamp_water` | `swamp_water_lily_islet` 莲叶浮岛；`swamp_water_half_sunk_barrel` 半沉桶组；`swamp_water_drifting_log` 浮木 | `swamp_water_floating_case`（Chest）；`swamp_water_weapon_raft`（Resource） |

## 6. 正式 PCG 的接入合同（后续实现）

| 资源组 | 选择方式 | 放置位置 | 现有运行时契约 |
| --- | --- | --- | --- |
| 地貌静态立物 | 同一地貌池内按 seed 加权随机 | 匹配地貌的空闲格；优先房间边缘和通道侧 | 仅视觉，不影响锚点、奖励、战斗或寻路 |
| 地貌交互立物 | 由既有锚点种类和其所在格的地貌过滤 | `Chest`、`Resource`、`Event` 锚点 | 完整保留原 `PayloadId` 和交互服务 |
| 通用建设模块 | 由房间类型选择，低频出现 | 出生、商人、宝箱、遭遇、事件周边 | 为现有功能提供可读性，不是新实体 |
| 主题特色地标 | 地图主题 + 房间类型确定，不随机换主题 | 出生、纹身师、商人、Boss、遭遇房 | 纹身师/商人仍由原 NPC 服务生成 |

首批不改地貌生成、不增加特殊边缘拼图，也不让装饰资源决定地形通行性。实现时新增的仅应是：资源 catalog 条目、稳定选择规则、房间/地貌过滤与渲染层级。

## 7. 生成与验收顺序

1. 先制作一个主题的 5 个地图地标、20 个地貌立物和 6 个地表点缀版本，放入正式 `PCGTest` 场景以验证遮挡、透明抠图、比例和锚点读性。
2. 用户确认该主题的成图质量后，再批量制作另外两个主题；不在未经目测确认时直接生成 83 张正式资源。
3. 每批资源经自动检查：透明背景、无绿边、底部 pivot、稳定 ID、catalog 路径存在、同 seed 可复现。
4. 人工检查重点：同地貌池的色板和密度一致；交互物一眼可读；大地标不遮挡人物或交互提示；三张地图在功能语义上可识别、在材质和轮廓上明显不同。

推荐从 **病毒沼泽** 开始首批出图：该主题可复用已确认的草地/水域基线，更快发现立物在自然地貌上的尺寸和遮挡问题。
