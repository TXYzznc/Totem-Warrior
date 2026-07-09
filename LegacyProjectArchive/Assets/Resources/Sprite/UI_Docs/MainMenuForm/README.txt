# MainMenuForm 美术资源说明

## 基本信息
对应 Form：MainMenuForm（Assets/Scripts/Modules/UI/MainMenuForm.cs）
对应 Prefab：Assets/Resources/Prefab/UI/MainMenu.prefab
原始需求出处：openspec/changes/12-core-ui-screens/art/requirements.md（表 A，必做）

## 功能说明
主菜单全屏界面，包含：
- 标题 logo
- 开始/设置/退出按钮三组（idle + highlighted 状态）

关键 SerializeField：无特殊资源绑定，按钮状态通过样式驱动。

## 文件清单

| 文件名 | 用途 | 类型 | 状态 |
|---|---|---|---|
| MainMenuForm_bg.png | 背景（全屏） | 背景 | ✓ OK |
| MainMenuForm_button_panel_idle.png | 按钮基础态 | 按钮框 | ✓ OK |
| MainMenuForm_button_panel_highlighted.png | 按钮高亮态 | 按钮框 | ✓ OK |
| MainMenuForm_title_logo.png | 标题 logo（游戏名） | 标题 | ✓ OK |

## 出处追溯
- 来源：openspec/changes/12-core-ui-screens （批次 1，2026-06-28）
- 生成记录：openspec/changes/12-core-ui-screens/art/raw/MainMenuForm/生成记录.md
- 关联修改：openspec/changes/07-main-menu-flow（菜单流程细化）

## 备注
- 所有文件均在使用中，无孤儿资源
- 按钮面板采用 idle/highlighted 两态设计，符合 GDD 表 C
- 本目录中不含通用按钮，只有 MainMenuForm 特有的面板框架
