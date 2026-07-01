# Art Requirements — 21-startup-select-form-landing

> **状态**：**已处理**（2026-07-01；素材复用率 100%，无需 codex-image-gen 出图）

## 决策

Phase 0 素材盘点确认所有需要的 sprite 都已存在于 `Assets/Resources/Sprite/`。本 change 不新绘任何 UI 素材，只做**素材映射到 code + Prefab**。

## 素材映射清单（code 里按此表 Resources.Load）

### CharacterSelectForm

| 元素 | Resources 路径（不带扩展名） | 备注 |
|---|---|---|
| 面板背景 | `Sprite/UI/CharacterSelectForm/CharacterSelectForm_bg` | 铺满 Canvas |
| Next 按钮底图 | `Sprite/UI/CharacterSelectForm/CharacterSelectForm_button_primary` | 主要按钮 |
| 卡片框 | `Sprite/UI/CharacterSelectForm/CharacterSelectForm_card_frame_unlocked` | 每张角色卡片外框 |
| 角色 1 icon | `Sprite/Character/Player1/Idle/Down` | Idle 朝下第一帧 |
| 角色 2 icon | `Sprite/Character/Player2/Idle/Down` | 同上 |
| 角色 3 icon | `Sprite/Character/Player3/Idle/Down` | 同上 |

### StartupSelectForm

| 元素 | Resources 路径 | 对应 ColorId / WeaponId / PatternId |
|---|---|---|
| 面板背景 | `Sprite/UI/CharacterSelectForm/CharacterSelectForm_bg` | 复用 CharSel 底图 |
| Confirm 按钮 | `Sprite/UI/CharacterSelectForm/CharacterSelectForm_button_primary` | 复用 |
| Cancel 按钮 | `Sprite/UI/CharacterSelectForm/CharacterSelectForm_button_idle` | 复用 |
| 颜料 Red | `Sprite/Paints/paint_red_common` | ColorId=1 |
| 颜料 Blue | `Sprite/Paints/paint_blue_common` | ColorId=4 |
| 颜料 Yellow | `Sprite/Paints/paint_yellow_common` | ColorId=2 |
| 武器 短刀 | `Sprite/Weapons/weapon_short_blade` | WeaponId=knife_basic |
| 武器 重锤 | `Sprite/Weapons/weapon_heavy_hammer` | WeaponId=hammer_heavy |
| 武器 手枪 | `Sprite/Weapons/weapon_pistol` | WeaponId=pistol_basic |
| 武器 弓 | `Sprite/Weapons/weapon_bow` | WeaponId=bow_charge |
| 武器 能量拳 | `Sprite/Weapons/weapon_energy_fist` | WeaponId=energy_fist |
| 图案 Line | `Sprite/Tattoo/Pattern/Line` | PatternId=1 |
| 图案 Ring | `Sprite/Tattoo/Pattern/Ring` | PatternId=2 |

## Prefab 层级约定

见 `annotations.md`。

## 复用一致性说明

- CharSel 与 StartupSel 共用同一套背景 + 按钮素材 → 视觉风格自动一致
- 角色 icon 复用 Player1/2/3 Idle/Down（战斗中同一 sprite）→ 玩家 selection ↔ 战斗角色视觉一致
