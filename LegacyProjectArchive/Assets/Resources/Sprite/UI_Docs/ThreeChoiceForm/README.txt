# ThreeChoiceForm 美术资源说明

## 基本信息
对应 Form：ThreeChoiceForm（Assets/Scripts/Modules/Event/UI/ThreeChoiceForm.cs）
对应 Prefab：Assets/Resources/Prefab/UI/ThreeChoice.prefab
原始需求出处：openspec/changes/12-core-ui-screens/art/requirements.md（表 A，必做）

## 功能说明
900×400 居中覆盖层，强制选择对话框（事件触发）。功能包括：
- 背景
- 3 张 CardPanel（idle/hover/selected/locked 四态）
- 纹身类型图标（用于标记选择效果，如火焰/冰霜等）

关键 SerializeField：卡片面板、图标。

## 文件清单

| 文件名 | 用途 | 类型 | 状态 |
|---|---|---|---|
| ThreeChoiceForm_bg.png | 背景 | 背景 | ✓ OK |
| cardpanel_idle.png | 卡片基础态 | 卡片 | ✓ OK |
| cardpanel_hover.png | 卡片悬停态 | 卡片 | ✓ OK |
| cardpanel_locked.png | 卡片锁定态（3s 内禁用） | 卡片 | ✓ OK |
| icon_tattoo_crit.png | 纹身图标（暴击） | 图标 | ✓ OK |
| icon_tattoo_fire.png | 纹身图标（火焰） | 图标 | ✓ OK |
| icon_tattoo_ice.png | 纹身图标（冰霜） | 图标 | ✓ OK |

## 出处追溯
- 来源：openspec/changes/12-core-ui-screens （批次 2，2026-06-28）
- 生成记录：openspec/changes/12-core-ui-screens/art/raw/ThreeChoiceForm/生成记录.md
- 设计依据：GDD 13-UI与HUD §二 事件选择表 C

## 图标与配置对齐分析
- icon_tattoo_crit/fire/ice 均为纹身元素示例（3/8，共 8 个可能的 TattooElement）
- 实际游戏中事件选择的效果由后端驱动，UI 仅显示对应的元素图标
- 建议与后端事件表对齐，确认是否所有 8 种元素都需要对应的图标
- 目前仅提供 3 个示例，可能需要扩展至 8 种 TattooElement 完整集合

## 备注
- CardPanel 四态（idle/hover/selected/locked）符合 GDD 表 C 规范
- 锁定态用于实现选择后延迟开始（3s 冷却，参考脚本逻辑）
- 卡片采用 scale 1.03 缩放效果（hover），此处仅提供静态框架
- 与 SelfTattooForm 类似，需确认元素图标的完整性及后端驱动逻辑
