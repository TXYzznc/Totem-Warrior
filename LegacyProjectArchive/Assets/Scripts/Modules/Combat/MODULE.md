---
module: Combat
owner: client-unity
generated_at: 2026-07-15
source: tools/ai_index/build_ai_manifests.py
---

# Combat Module

> 这是 AI 读取卡。修改本模块前，先读本文件，再按下面的关联入口补上下文。

## 职责

战斗意图、命中、伤害、攻击事件与玩家/敌人战斗流程。

## AI 读取顺序

1. 本文件
2. 关联 GDD / wiki
3. 关联 OpenSpec
4. 关联 DataTable
5. 模块源码与测试

## 关联 GDD / Wiki

- `项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/02-CombatModule.md`
- `项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/02-战斗手感.md`

## 关联 OpenSpec

- `openspec/specs/player-attack-system/spec.md`

## 关联 DataTable

- `ProjectileConfig`
- `WeaponConfig`
- `SkillConfig`

## 关联资源

- 无

## 主要脚本

- `LegacyProjectArchive/Assets/Scripts/Modules/Combat/CombatModule.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Combat/HumanPlayerController.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Combat/IPlayerController.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Combat/PlayerAnimatorBridge.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Combat/PlayerDamageReceiver.cs`

## 注意事项

- 无

