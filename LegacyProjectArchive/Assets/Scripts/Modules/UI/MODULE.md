---
module: UI
owner: client-unity
generated_at: 2026-07-10
source: tools/ai_index/build_ai_manifests.py
---

# UI Module

> 这是 AI 读取卡。修改本模块前，先读本文件，再按下面的关联入口补上下文。

## 职责

UGUI Form、HUD、菜单、运行结果、商店、纹身界面与 UI 数据绑定。

## AI 读取顺序

1. 本文件
2. 关联 GDD / wiki
3. 关联 OpenSpec
4. 关联 DataTable
5. 模块源码与测试

## 关联 GDD / Wiki

- `项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/12-UIModule+各UIForm.md`
- `项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/13-UI与HUD.md`

## 关联 OpenSpec

- `openspec/specs/ui-workflow/spec.md`
- `openspec/specs/core-ui-screens/spec.md`

## 关联 DataTable

- `UIFormConfig`

## 关联资源

- `Assets/Resources/Prefab/UI`
- `Assets/Resources/Sprite/UI`

## 主要脚本

- `LegacyProjectArchive/Assets/Scripts/Modules/UI/CharacterSelectForm.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/UI/CombatHUDForm.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/UI/DamageFloatTextBehaviour.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/UI/MainMenuForm.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/UI/PauseMenuForm.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/UI/RunResultForm.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/UI/SettingsForm.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/UI/StartupSelectForm.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/UI/StatusIconController.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/UI/UIModule.cs`

## 注意事项

- `新 UI 必须走结构先行 6 阶段流程。`

