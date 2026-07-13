---
module: Camera
owner: client-unity
generated_at: 2026-07-13
source: tools/ai_index/build_ai_manifests.py
---

# Camera Module

> 这是 AI 读取卡。修改本模块前，先读本文件，再按下面的关联入口补上下文。

## 职责

2.5D 正交相机、LateUpdate 跟随、边界 clamp、震动整合。

## AI 读取顺序

1. 本文件
2. 关联 GDD / wiki
3. 关联 OpenSpec
4. 关联 DataTable
5. 模块源码与测试

## 关联 GDD / Wiki

- 无

## 关联 OpenSpec

- `openspec/specs/camera-system/spec.md`

## 关联 DataTable

- 无

## 关联资源

- 无

## 主要脚本

- `LegacyProjectArchive/Assets/Scripts/Modules/Camera/CameraModule.cs`

## 注意事项

- `依赖 GameTickDriver 的 ILateTickable；避免在 Update/LateUpdate 中分配 GC。`

