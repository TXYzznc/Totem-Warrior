# M4 运行时旧内容清理审计

记录时间：2026-08-11

## 已切断的运行时路径

- 默认服务图不再注册 `TotemSkillService`、`TotemNpcService`、`TotemChoiceService`。
- `TotemInteractionService` 仅处理死亡掉落箱与普通箱子；不再扫描或打开武器拾取、NPC、商店、纹身师和地图三选一事件。
- `TotemChestService` 会从运行时奖励目录中过滤 `TotemChestRewardType.Weapon`，不会生成旧武器拾取物。
- 输入层不再读取 E/Q 主动技能按键，CombatHUD 的技能槽只保持隐藏兼容引用。
- 运行时资源目录只保留一个 `weapon.*` 键、零个 `skill.*` 键和零个旧手枪/弓投射物效果键。

## 已改名并继续保留的占位资产

| 原路径 | 当前路径 | 原因 |
|---|---|---|
| `Assets/Game/Sprites/Weapons/weapon_pistol.png` | 无图片依赖，运行时使用 primitive fallback | 第一阶段单枪械只保留稳定键 `weapon.rifle.patrol.v1`；武器图片目录已按确认清理 |
| `Assets/Game/Sprites/Skills/skill_fireball.png` | `Assets/Game/Sprites/Effects/effect_enemy_ability_burst_placeholder.png` | 暂作敌人能力爆发反馈，不再作为玩家技能图标 |
| `Assets/Game/Sprites/Skills/skill_chain_lightning.png` | `Assets/Game/Sprites/Effects/effect_boss_bolt_placeholder.png` | 暂作 Boss 弹道反馈，不再作为玩家技能图标 |

## 已证明零引用、待 Bypass 物理删除

Unity Cleaner 的 `cleaner_find_unused_assets` 与逐项 usage 查询确认下列资源无 AssetDatabase 引用；运行时资源 JSON 中也已移除对应字符串引用：

- `Assets/Game/Sprites/Weapons/weapon_short_blade.png`
- `Assets/Game/Sprites/Weapons/weapon_heavy_hammer.png`
- `Assets/Game/Sprites/Weapons/weapon_bow.png`
- `Assets/Game/Sprites/Weapons/weapon_energy_fist.png`
- `Assets/Game/Sprites/Skills/skill_heal_aura.png`
- `Assets/Game/Sprites/Skills/skill_ice_field.png`
- `Assets/Game/Sprites/Skills/skill_shield.png`
- `Assets/Game/Sprites/Skills/skill_stealth.png`
- `Assets/Game/Sprites/Skills/skill_summon.png`
- `Assets/Game/Sprites/Skills/skill_time_slow.png`

合计约 10.3 MB。`asset_delete_batch` 在 UnitySkills Auto 模式下属于禁止操作；切换至 Bypass 后应通过 AssetDatabase 批量删除，并同步重建美术资产索引。

## 暂时保留的兼容壳

`Assets/Game/ScriptsBuiltin/Editor` 中的旧 GF_X 诊断和 Prefab migrator 直接以类型引用方式编译依赖 Skill/NPC/Choice/Shop/ThreeChoice/TattooStudio。项目约束禁止修改 `Assets/Game/ScriptsBuiltin` 框架核心，因此业务侧旧类型文件暂时不能物理删除，否则整个 Editor 程序集无法编译。

这些类型不在默认服务图和新主流程中，属于历史兼容壳；后续若框架核心提供新版诊断扩展点，再迁移到 `LegacyProjectArchive` 或删除。

## Bypass 清理收口（2026-08-11）

- 上述四个非步枪武器图片已通过 `asset_delete_batch` 物理删除；玩家技能图片此前已删除。
- 因首版改为纯 PVP，先前临时改名保留的两个 Enemy/Boss Effect 占位图也已删除。
- 同批补充删除旧 Affix、Consumable、Item、Paints、2D PCG、旧 UI Sprite 与无运行引用的 DeerWoman 2D 帧资源。
- 第一阶段枪械不依赖占位图片；`Assets/Game/Sprites/Weapons` 已按确认清理，运行时由 catalog 的 primitive fallback 保证可运行。纹身图集继续保留。
