# Proposal — 21-startup-select-form-landing

> **状态**：待启动（前身 `18-weapon-select-flow` 于 2026-07-01 重编号并重写；原「只选武器」范围已被 change#20 的 StartupSelectForm 三选合一取代）

## Why

当前 MVP 闭环（change#16）里，从 MainMenu 进入 Combat 靠 `Tools/Playtest/Debug/StartGame` 菜单直跳 `InGame`，绕过了正常入场链路。真实入场链路的代码资产已经存在但**没通电**：

- `StartupSelectForm.cs` 已实现三选（颜料 3 + 武器 5 + 图案 2）→ 发 `StartupSelectedEvent` → `SpawnerModule.OnStartupSelected` 装备玩家。**这条 code 路径已就位，唯独缺 Prefab / icon / 前置流程接线**。
- `WeaponConfig.json` + `WeaponConfig.cs` 已含 5 把武器（knife_basic / hammer_heavy / pistol_basic / bow_charge / energy_fist），字段齐全（BaseDamage / Range / trait / prefab 路径等）。
- `CharacterSelectForm.cs` 仍是空壳（Awake→SetActive(false) 之外无内容），没有角色卡片也没有 Next 按钮。

**残留问题**：没有正式流程可以从「按开始 → 选角色 → 三选 build → 进战斗」跑通，玩家目前只能通过 debug 菜单进游戏。

## What

把 StartupSelectForm 从「只有 C# code」推到「玩家能玩」，同时补齐前置的 CharacterSelect 落地和入场链路。

### 目标链路

```
MainMenu ──开始──▶ CharacterSelect ──下一步──▶ StartupSelect ──确认──▶ InGame
    │                    │                          │                    │
    │                    ├─ 3 角色卡片              ├─ 颜料 × 3          ├─ SpawnerModule.OnStartupSelected
    │                    └─ Next 按钮               ├─ 武器 × 5           │  装备玩家（已实现）
    └─ MainMenu.prefab   └─ CharacterSelect.prefab  ├─ 图案 × 2 (Meta)   └─ RunStartedEvent
       (已建)              (已建，空 UI)             └─ Confirm 按钮
                                                     └─ StartupSelect.prefab（未建）
```

## Scope

**做**：

1. **StartupSelect.prefab**（新建）
   - 三段布局：ColorRoot / WeaponRoot / PatternRoot + ConfirmButton + CancelButton
   - StartupSelectForm.cs 已有 `Awake` 名兜底绑定 + 卡片代码动态生成，Prefab 只需搭好空的根 Transform 层级
   - 按 CLAUDE.md UI 制作流程：需求→效果图→拆分→Prefab+代码并行→联调
2. **CharacterSelectForm 从空壳到实体**
   - 3 张角色卡片（暂用同一美术，只区分 name / passive 描述占位；解锁数据后续补）
   - Next 按钮 → 触发 `_runner.GetModule<UIModule>().Open<StartupSelectForm>()`
   - CharacterSelect.prefab 已存在但只有壳，需补 UI 内容
3. **入场按钮接线**
   - MainMenuForm 已有「开始」按钮 → 打开 CharacterSelectForm（如已接线则复核；未接线则补）
   - CharacterSelectForm.NextButton → 打开 StartupSelectForm
   - StartupSelectForm.Confirm → 触发 `GameStateModule.StartGame()`（当前 code 只发 StartupSelectedEvent，不切 State；需补 State 迁移）
4. **美术素材**（走 codex-image-gen + ui-asset-splitting）
   - 3 颜料 icon（红/蓝/黄，已有色号 1/4/2 映射）
   - 5 武器 icon（对应 5 个 WeaponId）
   - 2 图案 icon（Line / Ring）
   - 3 角色 icon（占位，后续 change 补正稿）
5. **联调验收**
   - 完整走通 MainMenu → CharSel → StartupSel → InGame → PlayerDied → 回 MainMenu → 再玩一次
   - 玩家装备的武器 = StartupSelect 里选的（对比日志 `SpawnerModule Action=OnStartupSelected Weapon=xxx` 与 `WeaponModule` 实际装备）

**不做**（推迟到其他 change）：

- ❌ 角色系统数值差异（3 角色暂共用同一属性）
- ❌ 角色解锁 / 存档持久化
- ❌ 战斗中换武器 / 武器进阶（已有 WeaponUpgradeModule，但入场时先不接进阶界面）
- ❌ CancelButton 的语义（目前直接 Close，暂不回主菜单）
- ❌ Meta 图案解锁 UI 提示（现走 SaveData.PatternUnlocks，缺时兜底 [1,2]）

## DoD

1. ✅ 从 MainMenu 到 InGame 全程**不依赖 debug 菜单**，用 UI 按钮走完
2. ✅ StartupSelect 里选不同武器 → SpawnerModule / WeaponModule 日志能看到对应 WeaponId
3. ✅ CharacterSelect / StartupSelect 全部 icon 显示正常（3 + 5 + 2 + 3 = 13 张 icon）
4. ✅ 二次开局：MainMenu → CharSel → StartupSel → InGame 复跑无异常
5. ✅ 0 Console Error，0 Console Warning（复用 change#16 loop 退出条件）

## 依赖 / 前置

- **依赖**：change#16 MVP 闭环已通过（HP 重置 + 二次开局链路）；change#20 StartupSelectForm code 已就位
- **前置**：无阻塞——所有 code / 事件 / DataTable 都已实现，本 change 只做 Prefab / icon / 接线

## 当前状态

**Not started**。code 层面 60% 已存在（StartupSelectForm / WeaponConfig / SpawnerModule.OnStartupSelected），需要 UI 美术 + Prefab + 前置 Form 接线补齐。
