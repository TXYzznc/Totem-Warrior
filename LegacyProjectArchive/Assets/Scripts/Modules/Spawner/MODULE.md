---
module: Spawner
owner: client-unity
generated_at: 2026-07-09
source: tools/ai_index/build_ai_manifests.py
---

# Spawner Module

> 这是 AI 读取卡。修改本模块前，先读本文件，再按下面的关联入口补上下文。

## 职责

玩家、敌人、Bot、掉落物等运行时生成入口。

## AI 读取顺序

1. 本文件
2. 关联 GDD / wiki
3. 关联 OpenSpec
4. 关联 DataTable
5. 模块源码与测试

## 关联 GDD / Wiki

- `项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/06-SpawnerModule.md`

## 关联 OpenSpec

- 无

## 关联 DataTable

- `EnemyConfig`
- `WeaponDropConfig`
- `ChestConfig`

## 关联资源

- `Assets/Resources/Prefab`

## 主要脚本

- `LegacyProjectArchive/Assets/Scripts/Modules/Spawner/EntityRef.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Spawner/SpawnerModule.cs`

## 注意事项

- 无

