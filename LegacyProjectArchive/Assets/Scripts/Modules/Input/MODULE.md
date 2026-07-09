---
module: Input
owner: client-unity
generated_at: 2026-07-09
source: tools/ai_index/build_ai_manifests.py
---

# Input Module

> 这是 AI 读取卡。修改本模块前，先读本文件，再按下面的关联入口补上下文。

## 职责

玩家输入、测试输入注入与所有按键入口。

## AI 读取顺序

1. 本文件
2. 关联 GDD / wiki
3. 关联 OpenSpec
4. 关联 DataTable
5. 模块源码与测试

## 关联 GDD / Wiki

- `项目知识库（AI自行维护）/GDD-v2/modules/05-InputModule.md`
- `项目知识库（AI自行维护）/GDD-v2/systems/05-闪避与身法.md`

## 关联 OpenSpec

- `openspec/specs/playtest-driver/spec.md`

## 关联 DataTable

- 无

## 关联资源

- 无

## 主要脚本

- `LegacyProjectArchive/Assets/Scripts/Modules/Input/IInputSimulator.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Input/InputModule.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Input/InputSimulator.cs`

## 注意事项

- `所有按键输入必须走 TotemInputService / ITotemInputProvider，不允许业务代码直接读 Input。`

