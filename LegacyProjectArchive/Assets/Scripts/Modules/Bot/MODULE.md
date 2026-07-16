---
module: Bot
owner: client-unity
generated_at: 2026-07-15
source: tools/ai_index/build_ai_manifests.py
---

# Bot Module

> 这是 AI 读取卡。修改本模块前，先读本文件，再按下面的关联入口补上下文。

## 职责

AI 对手控制、Bot 配置、构筑预设与战斗行为入口。

## AI 读取顺序

1. 本文件
2. 关联 GDD / wiki
3. 关联 OpenSpec
4. 关联 DataTable
5. 模块源码与测试

## 关联 GDD / Wiki

- `项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/16-BotControllerModule.md`
- `项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/12-数值平衡与曲线.md`

## 关联 OpenSpec

- `openspec/specs/playtest-driver/spec.md`

## 关联 DataTable

- `BotConfig`
- `BotBuildPreset`

## 关联资源

- 无

## 主要脚本

- `LegacyProjectArchive/Assets/Scripts/Modules/Bot/BotBuildPlanner.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Bot/BotControllerModule.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Bot/BotVisualBinder.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Bot/LightBotPlayerController.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Bot/SmartBotPlayerController.cs`

## 注意事项

- 无

