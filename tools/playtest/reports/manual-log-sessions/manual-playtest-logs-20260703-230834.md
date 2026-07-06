---
test_time: 2026-07-03 23:08:34
scenario: manual-playtest-log-session
session_id: 20260703-230834
sections: 12
---

# 手动 Playtest 日志记录

## 1. F01 主菜单显示

- **结论**：正确
- **记录时间**：2026-07-03 22:55:26 - 22:55:32
- **日志数量**：7

### 本次测试内容

进入角色选择界面，主菜单关闭

### 实际效果

进入角色选择界面，主菜单关闭

### 备注 / 截图路径

（无）

### Console 日志

```text
[22:55:26.693] [frame:116] [Log] [ManualPlaytestLogRecorder] StartRecording ClearConsole=False
[22:55:26.696] [frame:116] [Log] [UnitySkills] [Self-Test] Starting (ProcessJobQueue ticks=115, listener=True)
[22:55:26.923] [frame:138] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=100.0 NewHP=92.0 Location=PlayerDamageReceiver.cs:132
[22:55:27.341] [frame:175] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=92.0 NewHP=74.0 Location=PlayerDamageReceiver.cs:132
[22:55:27.342] [frame:175] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=74.0 NewHP=66.0 Location=PlayerDamageReceiver.cs:132
[22:55:28.587] [frame:280] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=66.0 NewHP=58.0 Location=PlayerDamageReceiver.cs:132
[22:55:28.879] [frame:312] [Log] [ManualPlaytestLogRecorder] StopRecording
```

## 2. F02 开始游戏

- **结论**：不正确
- **记录时间**：2026-07-03 22:55:38 - 22:57:26
- **日志数量**：20

### 本次测试内容

进入角色选择界面，主菜单关闭

### 实际效果

主菜单预制体还是激活的，只是没有渲染在屏幕上方

### 备注 / 截图路径

（无）

### Console 日志

```text
[22:55:38.519] [frame:1445] [Log] [ManualPlaytestLogRecorder] StartRecording ClearConsole=True
[22:55:38.836] [frame:1473] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:55:38.836] [frame:1473] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:55:39.674] [frame:1560] [Log] [CharacterSelectForm|INFO] Action=Open Location=CharacterSelectForm.cs:96
[22:55:39.674] [frame:1560] [Log] [MainMenuForm|INFO] Action=StartClicked → CharacterSelectForm.Open Location=MainMenuForm.cs:86
[22:55:39.678] [frame:1560] [Log] [CharacterSelectForm|INFO] Action=Open Location=CharacterSelectForm.cs:96
[22:55:39.679] [frame:1560] [Log] [MainMenuForm|INFO] Action=StartClicked → CharacterSelectForm.Open Location=MainMenuForm.cs:86
[22:55:40.112] [frame:1611] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:55:40.517] [frame:1647] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:55:40.517] [frame:1647] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:55:41.731] [frame:1769] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:55:42.140] [frame:1824] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:55:42.140] [frame:1824] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:55:43.354] [frame:1996] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:55:43.766] [frame:2054] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:55:43.767] [frame:2054] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:57:24.035] [frame:2194] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:57:24.452] [frame:2235] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:57:24.453] [frame:2235] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:57:24.698] [frame:2263] [Log] [ManualPlaytestLogRecorder] StopRecording
```

## 3. F03 角色选择

- **结论**：正确
- **记录时间**：2026-07-03 22:57:39 - 22:58:25
- **日志数量**：18

### 本次测试内容

进入起手选择界面

### 实际效果

进入起手选择界面

### 备注 / 截图路径

（无）

### Console 日志

```text
[22:57:39.628] [frame:2960] [Log] [ManualPlaytestLogRecorder] StartRecording ClearConsole=True
[22:57:40.264] [frame:3037] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:57:40.675] [frame:3080] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:57:40.675] [frame:3080] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:57:40.963] [frame:3110] [Log] [CharacterSelectForm|INFO] Action=SelectCharacter Id=1 Location=CharacterSelectForm.cs:197
[22:57:41.941] [frame:3214] [Log] [StartupSelectForm|INFO] Action=Open Location=StartupSelectForm.cs:152
[22:57:41.941] [frame:3214] [Log] [CharacterSelectForm|INFO] Action=NextClicked Character=1 → StartupSelectForm.Open Location=CharacterSelectForm.cs:216
[22:57:41.943] [frame:3214] [Log] [CharacterSelectForm|INFO] Action=Close Location=CharacterSelectForm.cs:102
[22:57:41.964] [frame:3215] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:57:42.374] [frame:3256] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:57:42.374] [frame:3256] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:57:43.597] [frame:3387] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:57:44.021] [frame:3425] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:57:44.022] [frame:3425] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:58:23.640] [frame:3528] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:58:24.055] [frame:3568] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:58:24.056] [frame:3568] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:58:24.775] [frame:3661] [Log] [ManualPlaytestLogRecorder] StopRecording
```

## 4. F04 起手三选

- **结论**：不完全正确
- **记录时间**：2026-07-03 22:58:37 - 23:00:03
- **日志数量**：29

### 本次测试内容

进入局内战斗；CombatHUD 显示；起手界面关闭

### 实际效果

CombatHUD显示有点问题，其它正常

### 备注 / 截图路径

（无）

### Console 日志

```text
[22:58:37.385] [frame:4477] [Log] [ManualPlaytestLogRecorder] StartRecording ClearConsole=True
[22:58:38.438] [frame:4599] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:58:38.654] [frame:4624] [Log] [StartupSelectForm|INFO] Action=SelectColor ColorId=1 Location=StartupSelectForm.cs:354
[22:58:38.852] [frame:4645] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:58:38.852] [frame:4645] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:58:40.076] [frame:4781] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:58:40.134] [frame:4788] [Log] [StartupSelectForm|INFO] Action=SelectWeapon WeaponId=pistol_basic Location=StartupSelectForm.cs:362
[22:58:40.490] [frame:4827] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:58:40.490] [frame:4827] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:58:40.789] [frame:4861] [Log] [StartupSelectForm|INFO] Action=TogglePattern PatternId=1 Selected=[1] Location=StartupSelectForm.cs:377
[22:58:41.709] [frame:4964] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[22:58:42.081] [frame:5013] [Log] [WeaponModule|INFO] Action=WeaponEquipped Actor=玩家 WeaponId=pistol_basic Ammo=18 Location=WeaponModule.cs:117
[22:58:42.082] [frame:5013] [Warning] [PlayerWeaponMounter|WARN] Action=FallbackWeapon WeaponId=pistol_basic Prefab=Prefab/Weapon/Pistol missing Location=PlayerWeaponMounter.cs:69
[22:58:42.085] [frame:5013] [Log] [SpawnerModule|INFO] Action=OnStartupSelected Color=1 Weapon=pistol_basic Patterns=[1] Location=SpawnerModule.cs:230
[22:58:42.085] [frame:5013] [Log] [StartupSelectForm|INFO] Action=Confirm Color=1 Weapon=pistol_basic Patterns=[1] Location=StartupSelectForm.cs:442
[22:58:42.095] [frame:5013] [Log] [StartupSelectForm|INFO] Action=Close Location=StartupSelectForm.cs:158
[22:58:42.096] [frame:5013] [Log] [CombatModule|INFO] Action=RunStarted MaxHp=100 Location=CombatModule.cs:215
[22:58:42.098] [frame:5013] [Warning] [AudioModule|WARN] Action=PlayBgm Clip=Audio/BGM/in_game 未找到 Location=AudioModule.cs:116
[22:58:42.099] [frame:5013] [Log] [GameStateModule|INFO] Action=StateChanged Old=MainMenu New=InGame Location=GameStateModule.cs:71
[22:58:42.099] [frame:5013] [Log] [StartupSelectForm|INFO] Action=Confirm → GameState.InGame Location=StartupSelectForm.cs:452
[22:58:42.099] [frame:5013] [Log] [StartupSelectForm|INFO] Action=Close Location=StartupSelectForm.cs:158
[22:58:42.106] [frame:5013] [Log] [CombatHUDForm|INFO] Action=Ready Location=CombatHUDForm.cs:117
[22:58:42.107] [frame:5013] [Log] [CombatHUDForm|INFO] Action=LateInit MaxHp=100 Reason=RunStartedEvent_missed_before_Subscribe Location=CombatHUDForm.cs:133
[22:58:42.118] [frame:5014] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=100.0 NewHP=82.0 Location=PlayerDamageReceiver.cs:132
[22:58:42.118] [frame:5014] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=82.0 NewHP=74.0 Location=PlayerDamageReceiver.cs:132
[22:58:43.360] [frame:5141] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=74.0 NewHP=66.0 Location=PlayerDamageReceiver.cs:132
[22:58:43.774] [frame:5180] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=66.0 NewHP=48.0 Location=PlayerDamageReceiver.cs:132
[22:58:43.774] [frame:5180] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=48.0 NewHP=40.0 Location=PlayerDamageReceiver.cs:132
[23:00:02.543] [frame:5282] [Log] [ManualPlaytestLogRecorder] StopRecording
```

## 5. F05 局内初始状态

- **结论**：正确
- **记录时间**：2026-07-03 23:03:20 - 23:03:28
- **日志数量**：11

### 本次测试内容

玩家、敌人、地图、HUD 都可见；无报错弹窗；Console 无 Error

### 实际效果

玩家、敌人、地图、HUD 都可见；无报错弹窗；Console 无 Error

### 备注 / 截图路径

（无）

### Console 日志

```text
[23:03:20.957] [frame:5622] [Log] [ManualPlaytestLogRecorder] StartRecording ClearConsole=True
[23:03:21.078] [frame:5634] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:21.481] [frame:5680] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:21.482] [frame:5680] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:22.710] [frame:5834] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:23.116] [frame:5878] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:23.116] [frame:5878] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:24.353] [frame:6011] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:24.760] [frame:6065] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:24.761] [frame:6065] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:25.258] [frame:6127] [Log] [ManualPlaytestLogRecorder] StopRecording
```

## 6. C01 移动

- **结论**：不正确
- **记录时间**：2026-07-03 23:03:45 - 23:04:28
- **日志数量**：22

### 本次测试内容

玩家按 8 方向移动，摄像机/角色表现稳定

### 实际效果

玩家静止在原地不动

### 备注 / 截图路径

（无）

### Console 日志

```text
[23:03:45.153] [frame:6803] [Log] [ManualPlaytestLogRecorder] StartRecording ClearConsole=True
[23:03:45.451] [frame:6836] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:45.451] [frame:6836] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:46.961] [frame:6955] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:47.368] [frame:7007] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:47.369] [frame:7007] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:48.590] [frame:7151] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:49.002] [frame:7203] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:49.003] [frame:7203] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:50.220] [frame:7346] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:50.627] [frame:7398] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:50.627] [frame:7398] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:51.847] [frame:7550] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:52.251] [frame:7596] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:52.252] [frame:7596] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:53.471] [frame:7738] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:53.879] [frame:7786] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:53.880] [frame:7786] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:55.108] [frame:7921] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:55.521] [frame:7955] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:55.521] [frame:7955] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:03:56.315] [frame:8035] [Log] [ManualPlaytestLogRecorder] StopRecording
```

## 7. C02 普攻子弹

- **结论**：不正确
- **记录时间**：2026-07-03 23:04:41 - 23:05:31
- **日志数量**：20

### 本次测试内容

从玩家附近生成可见子弹/弹道，飞向目标方向；能看出普攻发生

### 实际效果

没有生成任何子弹，没有实际效果

### 备注 / 截图路径

（无）

### Console 日志

```text
[23:04:41.618] [frame:8903] [Log] [ManualPlaytestLogRecorder] StartRecording ClearConsole=True
[23:04:42.493] [frame:9003] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:04:42.903] [frame:9040] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:04:42.904] [frame:9040] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:04:44.132] [frame:9158] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:04:44.544] [frame:9202] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:04:44.544] [frame:9202] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:04:45.793] [frame:9321] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:04:46.200] [frame:9368] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:04:46.202] [frame:9368] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:04:47.425] [frame:9489] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:04:47.843] [frame:9536] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:04:47.844] [frame:9536] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:04:49.061] [frame:9670] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:04:49.471] [frame:9715] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:04:49.471] [frame:9715] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:04:50.708] [frame:9835] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:04:51.123] [frame:9876] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:04:51.123] [frame:9876] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:05:30.932] [frame:9982] [Log] [ManualPlaytestLogRecorder] StopRecording
```

## 8. C03 普攻命中

- **结论**：无法测试
- **记录时间**：2026-07-03 23:05:48 - 23:05:50
- **日志数量**：5

### 本次测试内容

（未填写）

### 实际效果

（未填写）

### 备注 / 截图路径

（无）

### Console 日志

```text
[23:05:48.228] [frame:10274] [Log] [ManualPlaytestLogRecorder] StartRecording ClearConsole=True
[23:05:48.234] [frame:10275] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:05:48.656] [frame:10302] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:05:48.656] [frame:10302] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:05:48.946] [frame:10325] [Log] [ManualPlaytestLogRecorder] StopRecording
```

## 9. C04 蓄力攻击

- **结论**：PENDING
- **记录时间**：2026-07-03 23:06:11 - 23:06:35
- **日志数量**：20

### 本次测试内容

蓄力攻击路径触发；具体效果取决于武器配置，不应报错

### 实际效果

没有任何实际效果

### 备注 / 截图路径

（无）

### Console 日志

```text
[23:06:11.770] [frame:10625] [Log] [ManualPlaytestLogRecorder] StartRecording ClearConsole=True
[23:06:12.334] [frame:10667] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:06:12.966] [frame:10724] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:06:12.966] [frame:10724] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:06:14.026] [frame:10818] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:06:14.637] [frame:10872] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:06:14.638] [frame:10872] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:06:15.670] [frame:10969] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:06:16.291] [frame:11026] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:06:16.292] [frame:11026] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:06:17.313] [frame:11114] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:06:17.925] [frame:11168] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:06:17.926] [frame:11168] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:06:18.968] [frame:11266] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:06:19.586] [frame:11326] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:06:19.586] [frame:11326] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:06:20.614] [frame:11429] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:06:21.248] [frame:11480] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:06:21.249] [frame:11480] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:06:21.829] [frame:11526] [Log] [ManualPlaytestLogRecorder] StopRecording
```

## 10. C05 闪避

- **结论**：无法测试
- **记录时间**：2026-07-03 23:07:09 - 23:07:14
- **日志数量**：8

### 本次测试内容

（未填写）

### 实际效果

（未填写）

### 备注 / 截图路径

（无）

### Console 日志

```text
[23:07:09.826] [frame:12103] [Log] [ManualPlaytestLogRecorder] StartRecording ClearConsole=True
[23:07:10.086] [frame:12130] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:07:10.706] [frame:12187] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:07:10.707] [frame:12187] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:07:11.735] [frame:12276] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:07:12.351] [frame:12330] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:07:12.352] [frame:12330] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:07:13.093] [frame:12392] [Log] [ManualPlaytestLogRecorder] StopRecording
```

## 11. C06 技能

- **结论**：无法测试
- **记录时间**：2026-07-03 23:07:28 - 23:07:43
- **日志数量**：14

### 本次测试内容

（未填写）

### 实际效果

（未填写）

### 备注 / 截图路径

（无）

### Console 日志

```text
[23:07:28.057] [frame:12657] [Log] [ManualPlaytestLogRecorder] StartRecording ClearConsole=True
[23:07:28.677] [frame:12713] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:07:28.677] [frame:12713] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:07:29.695] [frame:12818] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:07:30.326] [frame:12881] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:07:30.326] [frame:12881] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:07:31.376] [frame:12988] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:07:31.982] [frame:13040] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:07:31.982] [frame:13040] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:07:33.040] [frame:13141] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:07:33.655] [frame:13192] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:07:33.656] [frame:13192] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:07:34.666] [frame:13281] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:07:35.278] [frame:13321] [Log] [ManualPlaytestLogRecorder] StopRecording
```

## 12. C07 Q 键状态

- **结论**：无法测试
- **记录时间**：2026-07-03 23:07:59 - 23:08:10
- **日志数量**：11

### 本次测试内容

（未填写）

### 实际效果

（未填写）

### 备注 / 截图路径

（无）

### Console 日志

```text
[23:07:59.987] [frame:13622] [Log] [ManualPlaytestLogRecorder] StartRecording ClearConsole=True
[23:08:00.436] [frame:13660] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:08:01.053] [frame:13726] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:08:01.054] [frame:13726] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:08:02.103] [frame:13832] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:08:02.797] [frame:13884] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:08:02.799] [frame:13884] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:08:03.613] [frame:13968] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:08:09.276] [frame:14038] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_elite_01 Amount=18.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:08:09.276] [frame:14038] [Log] [PlayerDamageReceiver|INFO] Action=ApplyDamage Source=enemy_common_light_01 Amount=8.0 OldHP=0.0 NewHP=0.0 Location=PlayerDamageReceiver.cs:132
[23:08:09.344] [frame:14045] [Log] [ManualPlaytestLogRecorder] StopRecording
```

