# PauseMenuForm 美术资源说明

## 基本信息
对应 Form：PauseMenuForm（Assets/Scripts/Modules/UI/PauseMenuForm.cs）
对应 Prefab：Assets/Resources/Prefab/UI/PauseMenu.prefab
原始需求出处：openspec/changes/12-core-ui-screens/art/requirements.md（表 A，必做）

## 功能说明
全屏遮罩暂停菜单，包含：
- 背景遮罩
- 继续/设置/退出按钮（idle + highlighted 状态）

关键 SerializeField：按钮面板状态绑定。

## 文件清单

| 文件名 | 用途 | 类型 | 状态 |
|---|---|---|---|
| PauseMenuForm_bg.png | 全屏遮罩背景 | 背景 | ✓ OK |
| PauseMenuForm_button_base_idle.png | 按钮基础态 | 按钮框 | ✓ OK |
| PauseMenuForm_button_base_highlighted.png | 按钮高亮态 | 按钮框 | ✓ OK |

## 出处追溯
- 来源：openspec/changes/12-core-ui-screens （批次 1，2026-06-28）
- 生成记录：openspec/changes/12-core-ui-screens/art/raw/PauseMenuForm/生成记录.md

## 备注
- 所有文件均在使用中，无孤儿资源
- 按钮面板命名为 "base"（区别于 MainMenuForm 的 "panel"）
- 与 MainMenuForm 风格保持一致（Hades 风格，深色背景 + 金边）
