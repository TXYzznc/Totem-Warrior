# TattooEnchantForm 美术资源说明

## 基本信息
对应 Form：TattooEnchantForm（Assets/Scripts/Modules/NPC/UI/TattooEnchantForm.cs）
对应 Prefab：Assets/Resources/Prefab/UI/TattooEnchant.prefab
原始需求出处：openspec/changes/12-core-ui-screens/art/requirements.md（表 A，必做）

## 功能说明
700×500 居中覆盖层，嵌套于 TattooStudio 流程，用于纹身附魔/升级：
- 背景
- 符文类型图标（诅咒/冰霜/太阳等元素）
- 进度条框架
- 卡片面板（可禁用状态）

关键 SerializeField：符文图标、进度条。

## 文件清单

| 文件名 | 用途 | 类型 | 状态 |
|---|---|---|---|
| TattooEnchantForm_bg.png | 背景 | 背景 | ✓ OK |
| TattooEnchantForm_card_panel_disabled.png | 卡片面板（禁用态） | 面板 | ✓ OK |
| TattooEnchantForm_icon_rune_curse.png | 符文图标（诅咒元素） | 图标 | ✓ OK |
| TattooEnchantForm_icon_rune_frost.png | 符文图标（冰霜元素） | 图标 | ✓ OK |
| TattooEnchantForm_icon_rune_sun.png | 符文图标（太阳/神圣元素） | 图标 | ✓ OK |
| TattooEnchantForm_progress_bar_fill.png | 进度条填充部分 | 进度条 | ✓ OK |
| TattooEnchantForm_progress_bar_track.png | 进度条底框 | 进度条 | ✓ OK |

## 出处追溯
- 来源：openspec/changes/12-core-ui-screens （批次 4，2026-06-28）
- 生成记录：openspec/changes/12-core-ui-screens/art/raw/TattooEnchantForm/生成记录.md
- 设计依据：GDD 01-纹身构筑系统 §5.2 附魔体系

## 备注
- 符文图标仅示例 3 个（诅咒/冰霜/太阳），实际游戏中的符文类型由后端定义，可能需要扩展
- 与 TattooStudioForm 共同构成纹身升级流程
- 进度条采用 fillAmount 驱动，此处仅提供静态框架
