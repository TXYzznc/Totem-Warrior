# SettingsForm 美术资源说明

## 基本信息
对应 Form：SettingsForm（Assets/Scripts/Modules/UI/SettingsForm.cs）
对应 Prefab：Assets/Resources/Prefab/UI/Settings.prefab
原始需求出处：openspec/changes/10-settings-form/art/requirements.md

## 功能说明
游戏设置面板，包含：
- 背景
- 单选框（已选/未选 2 态）
- 滑块控件（拖拽球）
- 关闭按钮
- 面板框架（金色描边）

关键 SerializeField：单选图标、滑块标记。

## 文件清单

| 文件名 | 用途 | 类型 | 状态 |
|---|---|---|---|
| settingsform_bg.png | 背景（半透明遮罩） | 背景 | ✓ OK |
| panel_frame_gold.png | 面板框架（金色边框） | 装饰框 | ✓ OK |
| icon_close_button.png | 关闭按钮图标 | 按钮 | ✓ OK |
| icon_radio_selected.png | 单选框已选态 | 控件 | ✓ OK |
| icon_radio_unselected.png | 单选框未选态 | 控件 | ✓ OK |
| slider_thumb_gold.png | 滑块球（金色） | 控件 | ✓ OK |

## 出处追溯
- 来源：openspec/changes/10-settings-form（独立 change，先于 12-core-ui-screens）
- 生成时间：2026-06-中旬
- 确认风格：Hades 精致 2.5D，与 12-core-ui-screens 其他 Form 一致

## 备注
- 此目录的设计确认早于 12-core-ui-screens，作为风格基线参考
- panel_frame_gold 可复用于其他面板，建议后续与 Buttons/ 或 Backgrounds/ 共享
- 所有文件均在使用中
