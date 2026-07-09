# RunResultForm 美术资源说明

## 基本信息
对应 Form：RunResultForm（Assets/Scripts/Modules/UI/RunResultForm.cs）
对应 Prefab：Assets/Resources/Prefab/UI/RunResult.prefab
原始需求出处：openspec/changes/12-core-ui-screens/art/requirements.md（表 A，必做）

## 功能说明
全屏游戏结束结算页面（动画进场），展示本局成绩：
- 背景
- 杀敌数、存活时间、深度、金币、纹身获取等统计指标及其对应图标
- Build 快照预览

关键 SerializeField：各类统计图标（死亡/深度/金币/击杀/纹身类型）。

## 文件清单

| 文件名 | 用途 | 所属模块 | 状态 |
|---|---|---|---|
| RunResultForm_bg.png | 背景（全屏结算） | 背景 | ✓ OK |
| RunResultForm_icon_death.png | 死亡统计图标（骷髅） | 统计指标 | ✓ OK |
| RunResultForm_icon_depth.png | 深度统计图标（下箭头） | 统计指标 | ✓ OK |
| RunResultForm_icon_gold.png | 金币统计图标 | 统计指标 | ✓ OK |
| RunResultForm_icon_kills.png | 击杀统计图标（交叉剑） | 统计指标 | ✓ OK |
| RunResultForm_icon_tattoo_curse.png | 纹身类型图标（诅咒） | 纹身元素 | ✓ OK |
| RunResultForm_icon_tattoo_fire.png | 纹身类型图标（火焰） | 纹身元素 | ✓ OK |
| RunResultForm_icon_tattoo_frost.png | 纹身类型图标（冰霜） | 纹身元素 | ✓ OK |
| RunResultForm_icon_tattoo_holy.png | 纹身类型图标（神圣） | 纹身元素 | ✓ OK |
| RunResultForm_icon_tattoo_lightning.png | 纹身类型图标（闪电） | 纹身元素 | ✓ OK |
| RunResultForm_icon_tattoo_nature.png | 纹身类型图标（自然） | 纹身元素 | ✓ OK |
| RunResultForm_icon_tattoo_mutation.png | 纹身类型图标（异变） | 纹身元素 | ✓ OK |
| RunResultForm_icon_tattoo_pure.png | 纹身类型图标（纯能） | 纹身元素 | ✓ OK |

## 出处追溯
- 来源：openspec/changes/12-core-ui-screens （批次 3，2026-06-28）
- 生成记录：openspec/changes/12-core-ui-screens/art/raw/RunResultForm/生成记录.md
- 设计依据：GDD 13-UI与HUD §二 结算页面、01-纹身构筑系统 §5 元素体系

## 图标对应数据
- 火/闪电/冰霜/自然/异变/神圣/纯能：对应 TattooElementConfig 7 种元素
- 诅咒：特殊元素（或应为 Curse 类型，需与后端 API 确认）
- 死亡/深度/金币/击杀：游戏通用统计指标

## 备注
- 注意：原 openspec 记录中发现 batch_3 曾混入"RunResultForm 战绩统计图标"到 SelfTattooForm 生成，已因错漏被发现并纠正，此目录为正确版本
- 纹身图标数量（8 个）与 TattooPatternConfig 形状数量一致，但展示的是"元素类型"而非"图案类型"
- 建议后续与后端 API 确认 CurseElement 的正式定义及是否需要与 6 号金圣圣（Holy）区分
