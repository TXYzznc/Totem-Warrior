# 首批美术生产 Brief（2026-07-13）

> 状态：首批资源已完成导入和运行时接入；本文件保留生产规格与可追溯路径。

## 首批生产进度

| 交付 | 当前状态 | 原始产物位置 |
|---|---|---|
| 通用玩家（Player / SmartAI / LightAI 共用）最终基准 | **已完成：M02 石墨灰开放短外套、成熟结实体格、中低精细度厚涂；四视图与 96 帧动作已导入** | 源：`openspec/changes/produce-totem-art-assets/art/explorations/player_style_round_01/mature_candidates/M02_graphite_open_jacket_warrior.png`；正式：`Assets/Game/Sprite/Actors/ActorCommonM02/` |
| AI 遗迹执政官概念、四视图、四方向动作帧 | 已完成并绑定 Boss prefab / AnimatorController | 源：`openspec/changes/produce-totem-art-assets/art/raw/boss_ai_ruins_warden/`；正式：`Assets/Game/Sprite/Actors/BossAIruinsWarden/` |
| 纹身师全身 Idle | 已完成并绑定 NpcTattooist prefab | `Assets/Game/Sprite/NPC/NpcTattooist/npc_tattooist_idle.png` |
| 商人全身 Idle | 已完成并绑定 NpcMerchant prefab | `Assets/Game/Sprite/NPC/NpcMerchant/npc_merchant_idle.png` |
| `player_2` 荒原讯号猎手肖像 | 已完成并接入角色选择 | `Assets/Game/Sprite/UI/CharacterSelectForm/Portraits/player_2_signal_hunter_portrait.png` |
| `player_3` 失控改造者肖像 | 已完成并接入角色选择 | `Assets/Game/Sprite/UI/CharacterSelectForm/Portraits/player_3_augmented_subject_portrait.png` |

原始交付保留在 OpenSpec 变更目录；已通过审阅、切图和 Import Settings 校验的版本进入上述正式目录。不得使用已废弃的五个旧 Sprite 目录。

### 通用玩家风格探索 Round 01（已结束）

本轮探索已选定 M02，随后四视图和动作帧均以它为唯一主体依据。统一功能约束：**约 30 岁、自然结实的成熟男性战士**（不取少年脸、纤瘦青年或健美夸张肌肉）、帅气、有可读剪影、无烘焙纹身，并为头部、躯干、双臂、双腿预留贴花区域；风格基准为**中低精细度的 2D 半写实厚涂**：清晰干刷或笔触、3–5 级明暗、简化材质和配件。禁止照片级皮肤、毛孔、金属微划痕、3D 写实渲染和复杂小物堆砌。

Round 01 原始产物目录：`openspec/changes/produce-totem-art-assets/art/explorations/player_style_round_01/`。第一批 B01–B05 未满足成熟年龄基准，只保留作画风参考；新的十张候选会以成熟战士基准重绘。此前 `actor_common_concept_v02`、四视图及可能产生的动画草稿只保留作否决追溯，不得进入 Unity。

### 已选定方案：M02

`M02_graphite_open_jacket_warrior` 是 Player、SmartAI、LightAI 唯一共享的最终角色基准。后续 front/back/left/right、idle/walk/attack/death 四方向帧必须只以该图为主体参考；不得回用此前高精度写实方案或其它探索案。M02 仍为生产参考，不直接作为运行时 Sprite。

## 已确认的方向

- 除 PCG 地形外，首批资源统一采用**半写实厚涂**：清晰的大轮廓、可读的材质块面、有限而功能明确的高饱和能量色。
- `Player`、`SmartAI`、`LightAI` 共享一套完整、无纹身的战斗角色资源；它们不再通过整张角色图染色来区分阵营。
- 阵营识别使用脚下环或头顶标识。脚下环属于后续 Unity 表现接入，不属于本轮 VFX 绘制任务。
- 所有可贴花角色的本体资源不预烘焙纹身；纹身由后续运行时按构筑结果附着。
- 本轮 VFX 不制作位图；攻击、投射物和技能表现留给后续 Shader / 粒子系统管线。

## 主角基准：刻印逃亡者

首批通用玩家角色采用**偏男性、带少量中性实验体气质**的约 30 岁成熟战士：利落男性脸部轮廓、短碎发和侧剃、自然结实但不臃肿的体型、冷峻自信的英雄气质。该角色同时服务于真实玩家、SmartAI 和 LightAI，因此主体服装必须保持低饱和、无阵营色依赖。

### 服装与轮廓

- 剃短侧发或贴头短发，额侧、太阳穴和颈后可见。
- 无袖短斗篷或短披肩搭配交叉胸带；不使用遮住胸腹的硬甲或长外套。
- 锁骨下方至上腹保留连续肤色区域。
- 双臂从肩至前臂外侧保持可见；不使用长袖和宽护腕。
- 短战术裤搭配轻型护膝；双腿大腿外侧和小腿前外侧保留可见肤色区域。
- 基础服装使用炭黑、灰褐、旧帆布白、暗铜色；不大面积使用蓝、红、黄，以免与阵营环及纹身元素混淆。

### 六部位贴花区

| 部位 | 贴花主区域 | 禁止遮挡 |
|---|---|---|
| 头部 | 左右太阳穴、额侧、颈后 | 长发、头盔、面罩 |
| 躯干 | 锁骨下至上腹中央 | 胸甲、长外套、宽腰封 |
| 左臂 | 左肩至前臂外侧 | 长袖、宽护腕 |
| 右臂 | 右肩至前臂外侧 | 长袖、宽护腕 |
| 左腿 | 大腿外侧至小腿前外侧 | 长裤、重护胫、垂布 |
| 右腿 | 大腿外侧至小腿前外侧 | 长裤、重护胫、垂布 |

## 动画生产顺序与规格

1. 主角 / Boss 的半身或全身概念立绘。
2. 同一主体的正面、背面、左面、右面四视图。
3. 以已确认四视图为唯一主体参考，依次制作四方向动作帧。

- 动画角色仅有：通用玩家角色、Boss。
- 动作：`idle`、`walk`、`attack`、`death`。
- 方向：`down`、`up`、`left`、`right`。
- 帧数：简单动作 4 帧，标准动作 6 帧，复杂动作 8 帧；建议 `idle=4`、`walk=6`、`attack=6`、`death=8`。
- 单帧最大 `512 x 512`，透明背景，所有帧的脚底锚点一致。
- 一张源画布只允许一个角色、一个动作、一个方向；横向排列该方向的全部帧，不混入其它方向或动作。
- 推荐源文件名：`<character_id>_<action>_<direction>_sheet.png`；切分帧名：`<character_id>_<action>_<direction>_<frame:02>.png`。

## Boss 基准：AI 遗迹执政官

Boss 为两倍于玩家的直立双足巨构守卫，服务于 `AI_RUINS` 主题与践踏、光束、召唤三个已配置技能。主体由黑曜石装甲、锈铜骨架、断裂图腾石板构成；胸腔内有明确可见的能量核心。

- 轮廓：宽肩、窄腰、厚重双腿，头部简化为面甲或单一感应器，避免细小的人脸特征。
- 关键识别：胸腔核心用于光束蓄能；双足厚底用于践踏读条；肩背的悬浮碎片或折叠构件用于召唤前摇。
- 色彩：黑曜石灰、氧化铜绿、暗锈铜为基础；核心仅使用冷白到青蓝的高亮，不与主角的裸露纹身区竞争。
- 四视图：正面能看见核心与对称肩甲；背面显示能量导管和碎片挂载；左右侧面保留清晰的前冲与践踏轮廓。
- 动画：`idle=4`、`walk=6`、`attack=6`、`death=8`，上下左右四方向。`attack` 的身体动作必须同时适配 stomp、beam、summon 三种技能，由后续 VFX 和技能逻辑区分具体效果。

## 未来角色占位立绘

本轮只制作半身立绘，用于 `player_2`、`player_3` 的角色选择占位；不制作战斗四视图、帧动画、独立 prefab 或能力表现。两者的服装结构仍要预留未来六部位贴花区。

| 标识 | 暂定名 | 方向 |
|---|---|---|
| `player_2` | 荒原讯号猎手 | 精干女性，局部剃发与短辫、短披肩、无袖上装、轻型短裤与绑腿；轻快、侦察和远程气质。 |
| `player_3` | 失控改造者 | 高挑男性，短发、破损实验束带、裸露上身与手臂、轻护膝；沉稳、近战和能量拳气质。 |

## 本轮生产边界

- 优先：通用玩家角色、AI 遗迹执政官、纹身师和商人的静态视觉、`player_2` 与 `player_3` 占位立绘。
- 延后：技能图标的质量替换、所有 VFX 位图、Shader、粒子系统和阵营环的运行时表现。

## NPC 静态交付

- 纹身师保留现有草图中的成熟男性、紫色发光纹身与深色工作室服饰语言；商人保留卷发、暖铜 / 红褐服装、金币与挎包语言。
- 两者各输出至少一张透明背景、三分之四正面、**全身 Idle 站立姿势** Sprite；这是场景摆放的最低交付，不得只交付半身立绘。不做动画、不做四视图。
- 世界 Sprite 的脚底为唯一锚点，角色下方预留阴影与地形接触空间，首轮导入后按实际 Game View 缩放校验。

## 已排除或延后

- 不恢复 `Assets/Game/Sprite/Character`、`Characters`、`Environments`、`Recipes`、`Tattoo` 五个旧目录。
- PCG 主地形风格与资源不在本轮统一重绘范围。此前 50 张“缺图”的结论由 catalog 路径错误造成；资源已回归 `Assets/Game/Sprite/PCG/`，路径校验已确认没有该美术缺口。
- 玩家 2 / 玩家 3 首批只有独立立绘占位，没有战斗四视图、帧动画、独立 prefab 或独立能力设计。

## PCG 误创建清理

- 本轮曾依据错误路径生成并导入 50 张 PCG 图片；这些误创建资源及其 raw、预览、检查文件均已移除。项目既有 PCG 资源位于 `Assets/Game/Sprite/PCG/...`，现行 catalog 引用已通过路径校验。
- 这 50 张 `Assets/Resources/PCG/Terrain/` 图片、对应 `.meta`、raw、预览与检查文件均不属于项目资产，必须移除；不删除任何既有 PCG 切片、对象、POI 或 route 资源。

## 后续接入约束

- `TotemAssetService` 已不再通过全图 tint 区分共享角色；玩家、SmartAI、LightAI 使用脚点下方的扁平阵营环，颜色分别为蓝、红、黄。
- 阵营环已作为运行时简单 Renderer 接入，不属于本轮 VFX 资源生产。
