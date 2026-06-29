# TattooStudioForm 美术资源说明

## 基本信息
对应 Form：TattooStudioForm（Assets/Scripts/Modules/NPC/UI/TattooStudioForm.cs）
对应 Prefab：Assets/Resources/Prefab/UI/TattooStudio.prefab
原始需求出处：openspec/changes/12-core-ui-screens/art/requirements.md（表 A，必做）

## 功能说明
800×600 居中覆盖层，纹身工作台界面。功能包括：
- 背景
- 人形轮廓示意图（可部位点击）
- 死亡宝箱地图高亮标记
- 读条 UI（进度条 + 圆环）
- 附魔效果图标（火/冰/自然）
- 关闭按钮

关键 SerializeField：部位选择按钮、进度条、符文图标。

## 文件清单

| 文件名 | 用途 | 类型 | 状态 |
|---|---|---|---|
| TattooStudioForm_bg.png | 背景 | 背景 | ✓ OK |
| TattooStudioForm_humanoid_silhouette.png | 人形轮廓示意 | 装饰 | ✓ OK |
| TattooStudioForm_icon_death_chest_marker.png | 死亡宝箱地图高亮 | 标记 | ✓ OK |
| TattooStudioForm_icon_enchant_fire.png | 附魔效果图标（火焰） | 图标 | ✓ OK |
| TattooStudioForm_icon_enchant_frost.png | 附魔效果图标（冰霜） | 图标 | ✓ OK |
| TattooStudioForm_icon_enchant_nature.png | 附魔效果图标（自然） | 图标 | ✓ OK |
| TattooStudioForm_progressbar_fill.png | 进度条填充部分 | 进度条 | ✓ OK |
| TattooStudioForm_progressbar_track.png | 进度条底框 | 进度条 | ✓ OK |
| TattooStudioForm_slot_marker_fire.png | 纹身槽位标记（火焰） | 标记 | ✓ OK |
| TattooStudioForm_button_close.png | 关闭按钮 | 按钮 | ✓ OK |

## 出处追溯
- 来源：openspec/changes/12-core-ui-screens （批次 4，2026-06-28）
- 生成记录：openspec/changes/12-core-ui-screens/art/raw/TattooStudioForm/生成记录.md
- 特别记录：包含 _prompt.txt 原始生成指令
- 设计依据：GDD 01-纹身构筑系统 §2.8/§5.1 工作台流程

## 备注
- 人形轮廓用于部位选择交互（6 个热点）
- 附魔效果图标仅示例 3 个，实际类型可能需要扩展
- 槽位标记仅示例火焰，其他元素类型的标记可能需要额外素材
- 与 TattooEnchantForm 共同构成纹身升级流程
- 此处不含图案选择按钮，图案素材应参考 SelfTattooForm 或 Sprite/Tattoo/Pattern/
