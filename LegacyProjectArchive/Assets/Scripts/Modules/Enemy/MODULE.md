---
module: Enemy
owner: client-unity
generated_at: 2026-07-09
source: tools/ai_index/build_ai_manifests.py
---

# Enemy Module

> 这是 AI 读取卡。修改本模块前，先读本文件，再按下面的关联入口补上下文。

## 职责

敌人、Boss、怪物属性、死亡与相关战斗接入。

## AI 读取顺序

1. 本文件
2. 关联 GDD / wiki
3. 关联 OpenSpec
4. 关联 DataTable
5. 模块源码与测试

## 关联 GDD / Wiki

- `项目知识库（AI自行维护）/GDD-v2/modules/08-EnemyModule+BossModule.md`
- `项目知识库（AI自行维护）/GDD-v2/systems/11-怪物与Boss.md`

## 关联 OpenSpec

- 无

## 关联 DataTable

- `EnemyConfig`
- `BossPhaseConfig`

## 关联资源

- `Assets/Resources/Prefab/Enemy`
- `Assets/Resources/Sprite/Characters/Enemies`

## 主要脚本

- `LegacyProjectArchive/Assets/Scripts/Modules/Enemy/BossAIController.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Enemy/BossModule.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Enemy/EnemyAIController.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Enemy/EnemyEvents.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Enemy/EnemyModule.cs`

## 注意事项

- 无

