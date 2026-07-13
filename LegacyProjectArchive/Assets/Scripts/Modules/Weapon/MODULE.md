---
module: Weapon
owner: client-unity
generated_at: 2026-07-13
source: tools/ai_index/build_ai_manifests.py
---

# Weapon Module

> 这是 AI 读取卡。修改本模块前，先读本文件，再按下面的关联入口补上下文。

## 职责

武器配置、攻击、拾取、升级、特性和弹道接入。

## AI 读取顺序

1. 本文件
2. 关联 GDD / wiki
3. 关联 OpenSpec
4. 关联 DataTable
5. 模块源码与测试

## 关联 GDD / Wiki

- `项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/modules/03-WeaponModule.md`
- `项目知识库（AI自行维护）/wiki/历史资料/GDD-v2/systems/03-武器系统.md`

## 关联 OpenSpec

- `openspec/specs/weapon-pickup/spec.md`
- `openspec/specs/player-attack-system/spec.md`

## 关联 DataTable

- `WeaponConfig`
- `WeaponDropConfig`
- `WeaponTraitConfig`
- `ProjectileConfig`

## 关联资源

- `Assets/Resources/Prefab/Weapon`
- `Assets/Resources/Sprite/Weapons`

## 主要脚本

- `LegacyProjectArchive/Assets/Scripts/Modules/Weapon/AttackProjectileView.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Weapon/ChestInteractTrigger.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Weapon/MerchantTrigger.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Weapon/PlayerWeaponMounter.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Weapon/WeaponModule.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Weapon/WeaponPickupTrigger.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Weapon/WeaponSpawnerModule.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Weapon/WeaponUpgradeModule.cs`

## 注意事项

- 无

