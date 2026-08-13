# PCG 环境美术后续开发交接

> 已废弃：PCG 已退出当前项目资源范围，相关 catalog/config 待后续删除。本文仅保留历史交接记录，文内所有 `Assets/Game/Sprites/PCG/...` 与 `Resources.Load` 路径均不得作为新资源、代码或配置的落点。

> 更新：2026-07-16。本文用于把后续 AI 遗迹、异形巢穴的环境美术和正式 PCG 接入工作交给新的开发窗口。已完成的病毒沼泽可作为唯一的实现范例，不要重做其资源或另起测试流程。

## 1. 已验收、已接入的内容

三张地图的**地貌图块与地貌交界装饰**均已完成并在正式 PCG 中使用；当前待做的是环境立物、主题地标及其现有交互的主题化外观。

首批病毒沼泽环境资源已经用户验收并接入正式业务：共 **31 张透明 PNG**。

- 5 个固定地标：出生、Boss、遭遇、纹身师、商人。
- 12 个按地貌随机投放的静态立物：草地、泥地、腐化地、水域各 3 个。
- 6 个主题化地表点缀。
- 8 个既有交互锚点的外观资源：4 个宝箱、3 个武器拾取、1 个选择事件；其中选择事件外观复用在两个现有事件锚点上。
- 正式默认静态立物预算为 96；`PCGTest` 中的预算为 0 时跟随该正式默认值。

这批资源位于：

`Assets/Game/Sprites/PCG/Props/VirusSwamp/`

运行时配置位于：

`Assets/Resources/PCG/WorldObjectCatalog.json`

运行时回归结果：病毒沼泽以正式 `TotemMapService` 流程成功生成 14 个锚点视觉和 96 个随机静态立物；贴图丢失数为 0，Unity Console 错误为 0。

## 2. 已确认的美术与 PCG规则

### 地貌

- 正式地图是 `AI_RUINS`、`ALIEN_HIVE`、`VIRUS_SWAMP`，每张地图有 4 种地貌。
- 每种地貌有 8 张 256×256、铺满画布、不透明的绘图模型图块变体。
- 同一地貌的 8 个变体必须是**非常接近的微变体**：同色板、明暗范围、密度、材质、像素尺寸、光向；只允许改变少量已有纹理簇的位置。禁止出现另一种植物、明显色相/亮度跳变、强方向性图案或大焦点。
- 不做同地貌无缝拼接，也不做边缘、转角、socket、融合遮罩或特殊拼图。
- 不同地貌直接相邻；每对地貌在附近随机加 6 种透明环境装饰来丰富交界，而不是承担地貌融合。
- 水域不是固定双格河流。其宽度应由 PCG 地貌布局决定，各处可不同。

### 环境立物

- 俯视偏斜的 2D 中等精细度手绘像素风；大轮廓优先，避免照片感、高频微细节和高精度写实笔触。
- 立物可使用不同画布尺寸：小物优先 256×256，中物可为 256×384/384×384，大地标可为 512×512/512×768；画面需为透明背景。
- 运行时采用**底部居中 pivot**。生成时允许纯绿幕，入库前必须去绿幕为透明 PNG，不能有绿边。
- 静态立物仅作视觉点缀，不新增碰撞、阻挡、伤害、掉落或寻路规则。
- 可交互立物只能替换现有 `Chest`、`Resource`（当前为武器拾取）、`Event`、`Tattooist`、`Merchant` 的外观，保留其原始 Payload、交互半径、奖励与 UI 流程，不能新增玩法。
- 主题地标固定绑定房间/锚点；地貌静态物才做按 seed 的加权随机。

## 3. 当前正式代码结构（必须复用）

不要为另两张地图新建平行的 PCG 测试系统，也不要直接在 `PCGTest` 中写专用生成逻辑。`PCGTest` 已复用正式 `TotemMapService`，正式业务改动会自动反映到测试场景。

| 位置 | 责任 |
| --- | --- |
| `Assets/Game/Scripts/Runtime/Services/TotemMapService.cs` | 正式地图生成入口；创建地貌、静态物和锚点视觉。`PcgObjectBudget = 96` 是默认随机静态物预算。 |
| `Assets/Game/Scripts/Runtime/PCGMap/PCGMapGenerator.cs` | 按 seed 生成地图和随机静态物；`allowOnNonWalkable` 允许水面类装饰投放到非可行走水格。 |
| `Assets/Game/Scripts/Runtime/PCGMap/PCGMapCatalogs.cs` | `WorldObjectEntry`、`WorldAnchorVisualEntry`、catalog 加载及资源解析。普通 PNG 可由运行时创建 Sprite，因此应保持 Texture 导入格式。 |
| `Assets/Game/Scripts/Runtime/PCGMap/PCGMapData.cs` | `PCGPlacedVisual.ScaleMultiplier` 数据。 |
| `Assets/Resources/PCG/WorldObjectCatalog.json` | 唯一的环境立物正式 catalog：`objects` 是随机静态物，`anchorVisuals` 是固定锚点外观。 |
| `Assets/Game/Scripts/Runtime/PCGMap/PCGTestSceneController.cs` | PCGTest 控制器；种子、地图主题、参数面板均使用中文说明。 |
| `Assets/Game/Scene/PCGTest.unity` | 长期保留的正式流程测试场景；默认主题已恢复为 AI 遗迹。 |

### 配置模式

随机静态物写入 `objects`：

```json
{
  "id": "ai_floor_cable_bloom",
  "asset": "Sprite/PCG/Props/AiRuins/Floor/ai_floor_cable_bloom.png",
  "objectRole": "decoration",
  "allowedBiomes": ["ai_ruins"],
  "allowedTerrains": ["ruins_floor"],
  "footprint": { "width": 1, "height": 1 },
  "blocksMovement": false,
  "blocksSight": false,
  "weight": 2,
  "scaleMultiplier": 1.0,
  "tags": ["static", "ruins"]
}
```

固定锚点外观写入 `anchorVisuals`：

```json
{
  "id": "ai_ruins_fallen_gate",
  "themeId": 1,
  "anchorId": "player.spawn",
  "asset": "Sprite/PCG/Props/AiRuins/Landmarks/ai_ruins_fallen_gate.png",
  "footprint": { "width": 1, "height": 1 },
  "offsetZ": 2,
  "scaleMultiplier": 1.0,
  "sortingOffset": 0,
  "tags": ["landmark", "spawn"]
}
```

注意：以下 PCG 资源路径与 `Resources.Load` 规则均为废弃历史记录；不得据此新建 `Assets/Game/Resources/` 或恢复 PCG 资源。水面静态物的 `"allowOnNonWalkable": true` 仅供历史查阅。

## 4. 后续生产清单（70 项）

总设计目标是 101 项。病毒沼泽已完成 31 项，剩余 **70 项 = AI 遗迹 31 + 异形巢穴 31 + 通用功能模块 8**。

### 4.1 AI 遗迹（31 项）

资源根目录：`Assets/Game/Sprites/PCG/Props/AiRuins/`

固定地标（5）：

- `ai_ruins_fallen_gate` → `player.spawn`
- `ai_ruins_command_spire` → `boss.spawn`
- `ai_ruins_power_relay` → `encounter.mid.center`
- `ai_ruins_ink_terminal` → `npc.tattooist.base`
- `ai_ruins_scrap_kiosk` → `npc.merchant.base`

地貌静态物和既有交互外观（20）：

| 地貌 | 随机静态物（3） | 既有交互外观（2） |
| --- | --- | --- |
| `ruins_floor` | `ai_floor_cable_bloom`、`ai_floor_service_cart`、`ai_floor_sensor_post` | `ai_floor_data_cache`（Chest）、`ai_floor_decision_console`（Event） |
| `ruins_metal` | `ai_metal_cooling_fan`、`ai_metal_valve_cluster`、`ai_metal_chain_hoist` | `ai_metal_sealed_locker`（Chest）、`ai_metal_weapon_clamp`（Resource） |
| `ruins_growth` | `ai_growth_overgrown_panel`、`ai_growth_rooted_drone`、`ai_growth_broken_rail` | `ai_growth_root_cache`（Chest）、`ai_growth_bio_signal_node`（Event） |
| `ruins_coolant` | `ai_coolant_pipe_arch`、`ai_coolant_pump_skid`、`ai_coolant_insulation_drum` | `ai_coolant_emergency_case`（Chest）、`ai_coolant_weapon_cradle`（Resource） |

主题化地表点缀（6）：

- `ai_ruins_groundcover`：苔痕、短线缆、裂缝杂草。
- `ai_ruins_bloom`：发光苔点、故障指示灯簇。
- `ai_ruins_shrub`：藤蔓包住的设备残片、低矮线束团。
- `ai_ruins_vertical_growth`：锈蚀支架垂蔓、断裂天线柱。
- `ai_ruins_fallen_debris`：倒塌护栏、短管、断电缆盘。
- `ai_ruins_outcrop`：混凝土残块、机械基座、锈蚀板岩。

视觉关键词：冷灰蓝石材、氧化金属、低饱和蓝青冷却液、少量阴湿苔痕；避免科幻 UI、发光网格和高频机械细节。

### 4.2 异形巢穴（31 项）

资源根目录：`Assets/Game/Sprites/PCG/Props/AlienHive/`

固定地标（5）：

- `alien_hive_brood_gate` → `player.spawn`
- `alien_hive_queen_core` → `boss.spawn`
- `alien_hive_resin_bridge` → `encounter.mid.center`
- `alien_hive_spore_mender` → `npc.tattooist.base`
- `alien_hive_trade_larva` → `npc.merchant.base`

地貌静态物和既有交互外观（20）：

| 地貌 | 随机静态物（3） | 既有交互外观（2） |
| --- | --- | --- |
| `hive_chitin` | `hive_chitin_rib_spire`、`hive_chitin_shell_heap`、`hive_chitin_fence` | `hive_chitin_brood_cache`（Chest）、`hive_chitin_rite_pod`（Event） |
| `hive_membrane` | `hive_membrane_hanging_sac`、`hive_membrane_web_column`、`hive_membrane_vent` | `hive_membrane_weapon_nest`（Resource）、`hive_membrane_choice_organ`（Event） |
| `hive_resin` | `hive_resin_stalagmite`、`hive_resin_cocoon_stack`、`hive_resin_arch` | `hive_resin_trapped_cache`（Chest）、`hive_resin_weapon_sheath`（Resource） |
| `hive_acid` | `hive_acid_bubble_vent`、`hive_acid_bone_ridge`、`hive_acid_shed_shell` | `hive_acid_resistant_cache`（Chest）、`hive_acid_choice_gland`（Event） |

主题化地表点缀（6）：

- `alien_hive_groundcover`：膜丝、细小菌毯。
- `alien_hive_bloom`：孢子灯泡、小型发光囊。
- `alien_hive_shrub`：触须簇、幼体囊群。
- `alien_hive_vertical_growth`：高孢子茎、细骨刺柱。
- `alien_hive_fallen_debris`：脱落甲壳、空卵壳、骨片堆。
- `alien_hive_outcrop`：树脂瘤、钙化组织、酸蚀骨岩。

视觉关键词：有机骨质、树脂、半透明膜、克制的酸性亮点；保持黑暗手绘像素质感，避免写实昆虫解剖、湿滑照片感与密集高频肌理。

### 4.3 通用功能模块（8 项）

这些是功能语义通用、但可在后续按主题做轻度换色或附件变化的辅助资源；它们不能取代三张地图各自的出生、Boss、纹身师、商人地标。

- `common_field_lamp`
- `common_shelter_canopy`
- `common_supply_stack`
- `common_signal_beacon`
- `common_barricade`
- `common_cache_nook`（Chest 辅件）
- `common_weapon_rack`（Resource 辅件）
- `common_choice_plinth`（Event 辅件）

建议在两张主题地图的 31 项都完成并验收后再制作。若要先落地，应由现有房间类型选择低频投放，不应覆盖主题地标或替代主题化交互外观。

## 5. 推荐执行顺序

1. 先绘制并验收 AI 遗迹 31 项；透明化、导入、配置和 PCGTest 回归都完全参照病毒沼泽。
2. 为 AI 遗迹在 `WorldObjectCatalog.json` 增加 18 个 `objects` 和 14 条 `anchorVisuals`；交互外观只选现有对应锚点，不改服务逻辑。
3. 切换 `PCGTest` 到 AI 遗迹，使用多个固定种子检查：主题一致性、地貌投放限制、锚点可读性、无缺图/Console 错误。
4. 以相同模式完成异形巢穴 31 项及配置接入。
5. 最后根据正式房间语义决定是否制作并接入 8 个通用功能模块。

每一主题完成后必须先独立验收，不要一次生成并导入 70 项。

## 6. 必做验证与导入约定

1. 原始绘图模型图先保存到 `openspec/changes/simplify-pcg-terrain-pipeline/art/raw/environment/<theme>/`；去绿幕后再导入 `Assets/Game/Sprites/PCG/Props/<Theme>/`。
2. 每张立物 PNG 检查 RGBA 透明背景、四角透明、无绿边；Texture 导入为 `Default`、`Point`、关闭 Mipmap、`Clamp`、`NPOT None`、`Uncompressed`，不要强制切成 Sprite。
3. 每张资源均需稳定 ID、正确的 Resources 路径、对应 `allowedBiomes`/`allowedTerrains` 或固定 `anchorId`。
4. 固定同一 seed 连续生成两次时，资源选择必须一致；换种子时只改变随机静态物与地貌布局，不应丢失既有交互锚点。
5. Unity 检查至少包括：编译无错误、Console 错误为 0、资源路径可解析、PCGTest 与正式 `TotemMapService` 流程一致。

## 7. 关联文档

- `terrain-plan.md`：三张地图四种地貌、8 变体与交界装饰的完整清单。
- `art/variant-coherence.md`：地貌变体必须保持和谐的硬性提示词与验收规则。
- `art/environment-prop-design.md`：101 项环境立物的原始设计总表和投放合同。
- `art/virus-swamp-environment-production.md`：已验收的病毒沼泽生产记录。
- `design.md`、`proposal.md`：移除特殊边缘拼图、保留正式加载机制的 PCG 改造背景。
