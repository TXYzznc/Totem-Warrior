# 病毒沼泽环境立物：首批绘制生产单

> 状态：**已验收并接入正式 PCG**（2026-07-16）。31 张立物已以透明 PNG 导入 `Assets/Game/Sprites/PCG/Props/VirusSwamp/`，并由正式 `WorldObjectCatalog` 配置静态地物与既有交互锚点视觉。

## 目标与数量

- 5 个地图特色地标：3 个静态、2 个既有 NPC 交互锚点外观。
- 20 个地貌立物：`swamp_grass`、`swamp_mud`、`swamp_corruption`、`swamp_water` 各 3 个静态与 2 个既有锚点外观。
- 6 个地表点缀：草被、花簇、灌木、小树/枯树、倒木/残骸、岩石/根系。
- 合计 31 个独立 PNG 源图；每张只画一个对象，不制作拼图或组合图集。

## 统一绘制合同

- 中等精细手绘像素风，俯视偏斜 2D 立物；硬像素边缘、大轮廓清晰、纹理不密集。
- 统一左上方柔和光照。沼泽基调为深青黑水、橄榄湿苔、腐木褐、芦苇土黄，感染亮点只能小面积使用暗紫与黯青光。
- 每张使用完全平整的 `#00FF00` 绿幕；不画阴影、地面、渐变、边框、文字、UI 或水印。主体不可出现纯亮绿，避免抠图时丢失边缘。
- 主体完整可见、四周留足空白；底部中心为 pivot。静态物与地标均不暗示可破坏、伤害或阻挡。
- 交互物只表达现有 `Chest`、`Resource`（武器拾取）或 `Event`（选择事件）的视觉语义，绝不替换掉落内容或新增采集、净化、传送等行为。

## 批次与输出目录

| 并行批次 | 数量 | 输出目录 |
| --- | ---: | --- |
| `vs_landmarks` | 5 | `art/raw/environment/virus-swamp/landmarks/` |
| `vs_grass_props` | 5 | `art/raw/environment/virus-swamp/grass/` |
| `vs_mud_props` | 5 | `art/raw/environment/virus-swamp/mud/` |
| `vs_corruption_props` | 5 | `art/raw/environment/virus-swamp/corruption/` |
| `vs_water_props` | 5 | `art/raw/environment/virus-swamp/water/` |
| `vs_ground_detail` | 6 | `art/raw/environment/virus-swamp/ground-detail/` |

## 后续验收门

1. 绿幕必须连续、纯净且主体无绿边；抠图后四角 alpha=0。
2. 在深色与浅色地貌上检查轮廓，不能因深色物体丢失读性。
3. 交互物在缩放后须能一眼读为宝箱、武器架/武器残骸或选择台座；地标中的纹身师、商人位置须保留前方可站立的空间。
4. 已完成自动检查：31 张 RGBA 输出均有透明四角；绿幕源图均保留在 `raw/environment/virus-swamp/`，透明候选位于同级 `alpha/`。
5. 已完成用户验收：31 项已正式导入 `Assets/Game/Sprites/PCG/Props/VirusSwamp/` 并接入正式 PCG catalog；其中 18 项参与地貌随机生成，14 条锚点视觉复用 13 项交互/地标资源（选择祭坛复用两次）。
