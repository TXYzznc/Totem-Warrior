---
module: VFX
owner: client-unity
generated_at: 2026-07-09
source: tools/ai_index/build_ai_manifests.py
---

# VFX Module

> 这是 AI 读取卡。修改本模块前，先读本文件，再按下面的关联入口补上下文。

## 职责

命中特效、粒子、镜头抖动、战斗视觉反馈。

## AI 读取顺序

1. 本文件
2. 关联 GDD / wiki
3. 关联 OpenSpec
4. 关联 DataTable
5. 模块源码与测试

## 关联 GDD / Wiki

- `项目知识库（AI自行维护）/GDD-v2/modules/15-VFXModule.md`

## 关联 OpenSpec

- `openspec/specs/visual-polish/spec.md`

## 关联 DataTable

- 无

## 关联资源

- `Assets/Resources/Effect`
- `Assets/Resources/Sprite/Effects`

## 主要脚本

- `LegacyProjectArchive/Assets/Scripts/Modules/VFX/CameraShakeBehaviour.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/VFX/HitsparkBehaviour.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/VFX/VFXModule.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/VFX/VignettePulseBehaviour.cs`

## 注意事项

- 无

