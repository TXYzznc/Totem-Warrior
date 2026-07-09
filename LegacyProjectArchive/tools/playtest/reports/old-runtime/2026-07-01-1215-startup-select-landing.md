---
test_time: 2026-07-01 12:15
scenario: startup-select-landing
result: PASS
duration_sec: 180
errors_found: 0
warnings_found: 1
---

# Playtest Report — Change #21 StartupSelectForm Landing

## 概要

- **测试目标**：验证 change #21 完整流程 `MainMenu → CharacterSelect → StartupSelect → InGame`，以及二次 Run 复跑（die/return → MainMenu → 再跑一遍）0 阻断报错。
- **测试结果**：✅ PASS（首战 9 步 + 二战 4 步全通过，Final GameState=InGame，0 Error）。
- **关键发现**：
  1. `CharacterSelectForm` / `StartupSelectForm` Prefab 均由 unity-skills REST（端口 8090）通过 Editor 菜单 `Ch21_BuildAll` 生成落盘，运行时 UIModule 自动扫描到并注册。
  2. UI Form 实例被放进 DontDestroyOnLoad UIRoot，无法通过 `gameobject_find` / `scene_find_objects` 找到；改由 Editor 侧 `FindObjectOfType<T>(true)` + 反射私有 `_colorIds/_weaponIds/_patternIds` List 完成模拟点击。
  3. `StartupSelectedEvent` 正常触发 → `SpawnerModule.OnStartupSelected` → `WeaponModule Action=Equipped 玩家 knife_basic` → `CombatModule RunStarted MaxHp=100`。
- **耗时**：约 3 分钟（含两次 Full Flow + 快照）。

## 测试流程

1. **准备**：拉起 Play 前 `Ch21_BuildAll` → 生成 `Assets/Resources/Prefab/UI/CharacterSelect.prefab` (15772B) + `StartupSelect.prefab` (25399B)。
2. **进入 Play**：`editor_execute_menu Edit/Play` → `isPlaying=true`，UIModule 扫描注册 3 个 Form。
3. **快照 MainMenu**：`Tools/Playtest/Change21/Snapshot MainMenu` → `Assets/Screenshots/pt_ch21_main_menu.png`（1.86 MB，命中「纹身战士」标题 + 开始游戏/设置按钮）。
4. **首次 Full Flow**（`Tools/Playtest/Change21/Test Full Flow`）：
   - Step1: `MainMenuForm.OnStartClicked` → `CharacterSelectForm.Open`
   - Step2a: `CharSel.SetSelectedCharacter(1)` → Next 亮
   - Step2b: `CharSel.OnNextClicked` → `StartupSelectForm.Open`
   - Step3: 反射取 `_colorIds[0]` / `_weaponIds[0]="knife_basic"` / `_patternIds[0]` → `SetSelectedColor` + `SetSelectedWeapon` + `ToggleSelectedPattern` → `OnConfirm`
   - Step4: `StartupSelectedEvent` 发布 → `SpawnerModule.OnStartupSelected Weapon=knife_basic` → `WeaponModule Action=Equipped 玩家 knife_basic` → `CombatModule Action=RunStarted MaxHp=100`
   - Final GameState=**InGame** ✅
5. **快照 InGame**：`Snapshot InGame` → `pt_ch21_ingame.png`（520 KB，红色 HP 条 + 小地图 + 3 技能槽）。
6. **二次 Full Flow**（`Test Second Run (die+replay)`）：
   - Step1: `gs.GoToMainMenu()` → State=MainMenu ✓
   - Step2: 触发 `Ch21_TestFullFlow` 2nd time → Final=**InGame** ✓
   - 二战期间 `_runStarted` 已复位，`CombatModule.OnGameStateChanged` 将 `PlayerTarget.Health` 拉回 `PlayerMaxHp`（复用 MVP #16 修复）。
7. **收尾**：`console_get_logs type=Error limit=40` → 0 条；`type=Warning` → 1 条（`AudioModule Mixer=Audio/MainMixer` 启动 fallback，与 change #21 无关）。`editor_execute_menu Edit/Play` 退出 Play。

## 遇到的问题

- **[WARN]** `AudioModule` 启动 Mixer=Audio/MainMixer fallback —— 与 change #21 无关，MVP #16 后续项已挂账。
- **[OBSERVATION]** `CombatHUDForm Action=LateInit Reason=RunStartedEvent_missed_before_Subscribe` —— 首战 HUD 订阅仍晚于 RunStarted，自愈 fallback 已生效（非 Error）。
- **[OBSERVATION]** DontDestroyOnLoad UIRoot 让 `gameobject_find` / `scene_find_objects` 找不到 UI Form —— 已通过 Editor 菜单 `Ch21_TestFullFlow` + `FindObjectOfType(true)` 绕过。
- 无 change #21 相关 Error / Warning。

## 关键日志摘录

```
[MainMenuForm|INFO]        Action=StartClicked → CharacterSelectForm.Open
[CharacterSelectForm|INFO] Action=Open
[CharacterSelectForm|INFO] Action=SelectCharacter Id=1
[CharacterSelectForm|INFO] Action=NextClicked Character=1 → StartupSelectForm.Open
[StartupSelectForm|INFO]   Action=Confirm Color=1 Weapon=knife_basic Patterns=[1]
[SpawnerModule|INFO]       Action=OnStartupSelected Weapon=knife_basic
[WeaponModule|INFO]        Action=Equipped Target=玩家 WeaponId=knife_basic
[CombatModule|INFO]        Action=RunStarted MaxHp=100
[GameStateModule|INFO]     Action=StateChanged Old=StartupSelect New=InGame

// 二战
[GameStateModule|INFO]     Action=StateChanged Old=InGame New=MainMenu
[MainMenuForm|INFO]        Action=StartClicked → CharacterSelectForm.Open (2nd)
[CombatModule|INFO]        Action=RunStarted MaxHp=100 (HP 复位生效)
```

## 变更摘要

- **代码**（Phase 2 已完成）：
  - [MainMenuForm.cs](Assets/Scripts/Modules/UI/MainMenuForm.cs) `OnStartClicked` 去掉 `gs.StartGame()` 直调，改为打开 `CharacterSelectForm`。
  - [CharacterSelectForm.cs](Assets/Scripts/Modules/UI/CharacterSelectForm.cs) 空壳→实体（3 张角色卡片动态生成 + Next 按钮 → 打开 StartupSelectForm）。
  - `StartupSelectForm.cs` CreateCard 加 sprite 参数；`OnConfirm` 末尾追加 `gs.StartGame()`。
- **Prefab**（Phase 3 已完成，unity-skills REST 端口 8090）：
  - `Assets/Resources/Prefab/UI/CharacterSelect.prefab` 重建（15772B）
  - `Assets/Resources/Prefab/UI/StartupSelect.prefab` 新建（25399B）
- **测试工具**（本次新增）：
  - [PlaytestDriverEditor.cs](Assets/Editor/Playtest/PlaytestDriverEditor.cs) 追加 `Ch21_TestFullFlow` / `Ch21_TestSecondRun` / `Ch21_Snap*` 4 个 MenuItem，用于 Editor 侧模拟点击 DDOL UI Form 并做 UI 快照。

## 后续

- [ ] UIModule Register 时序：考虑把 CharacterSelectForm/StartupSelectForm 加进 UIModule 白名单集中管理，取消 `FindObjectOfType` 兜底。
- [ ] `CombatHUDForm.LateInit` fallback 长期应移除（MVP #16 已挂账，与 #21 无关）。
- [ ] AudioModule MainMixer fallback Warning 清理（MVP #16 已挂账）。
- [ ] change #21 后续可考虑给 3 张角色卡片挂差异化 CharacterConfig（目前 `_selectedCharacterId` 只做日志）。
