# CharacterSelectForm 美术资源说明

## 基本信息
对应 Form：CharacterSelectForm（Assets/Scripts/Modules/UI/CharacterSelectForm.cs）
对应 Prefab：Assets/Resources/Prefab/UI/CharacterSelect.prefab
原始需求出处：openspec/changes/12-core-ui-screens/art/requirements.md（表 A，必做）

## 功能说明
全屏角色选择界面。包含背景、人物卡片框架（解锁/锁定状态）、锁定图标。
关键 SerializeField：无已知的色块按钮资源引用（此 Form 主要展示人物卡片 GridLayout）。

## 文件清单

| 文件名 | 用途 | 所属类型 | Prefab 引用 | 状态 |
|---|---|---|---|---|
| CharacterSelectForm_bg.png | 背景（全屏遮罩） | 背景 | ✓ | OK |
| CharacterSelectForm_button_idle.png | 按钮基础态 | 装饰 | ✓ | OK |
| CharacterSelectForm_button_primary.png | 按钮高亮态（开始游戏） | 装饰 | ✓ | OK |
| CharacterSelectForm_card_frame_locked.png | 人物卡片框架（锁定） | 装饰 | ✓ | OK |
| CharacterSelectForm_card_frame_unlocked.png | 人物卡片框架（解锁可用） | 装饰 | ✓ | OK |
| CharacterSelectForm_lock_icon.png | 小锁图标（卡片锁定指示） | 图标 | ✓ | OK |

## 出处追溯
- 来源：openspec/changes/12-core-ui-screens （批次 1，2026-06-28）
- 生成记录：openspec/changes/12-core-ui-screens/art/raw/CharacterSelectForm/生成记录.md
- 出图方式：codex-image-gen（AI 生成 + 手工切割）

## 备注
- 所有文件均在使用中，无孤儿资源
- 文件命名规范：<FormName>_<用途>.png
- 无数据不对齐问题
