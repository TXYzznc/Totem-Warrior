---
module: MapGen
owner: client-unity
generated_at: 2026-07-09
source: tools/ai_index/build_ai_manifests.py
---

# MapGen Module

> 这是 AI 读取卡。修改本模块前，先读本文件，再按下面的关联入口补上下文。

## 职责

地图生成/加载、缩圈、地形与交互物布点。

## AI 读取顺序

1. 本文件
2. 关联 GDD / wiki
3. 关联 OpenSpec
4. 关联 DataTable
5. 模块源码与测试

## 关联 GDD / Wiki

- `项目知识库（AI自行维护）/GDD-v2/modules/07-MapGenModule.md`
- `项目知识库（AI自行维护）/GDD-v2/systems/07-地图生成.md`

## 关联 OpenSpec

- `openspec/changes/26-fixed-map-three-themes/specs/map-fixed-terrain/spec.md`
- `openspec/changes/26-fixed-map-three-themes/specs/map-interactive-spawn/spec.md`

## 关联 DataTable

- `MapTemplateConfig`
- `ZoneShrinkConfig`

## 关联资源

- 无

## 主要脚本

- `LegacyProjectArchive/Assets/Scripts/Modules/MapGen/MapGenModule.cs`

## 注意事项

- 无

