---
test_time: 2026-07-01 01:30
scenario: mvp-loop-closure
result: PASS
duration_sec: 240
errors_found: 0
warnings_found: 0
---

# Playtest Report — MVP 最小游戏闭环

## 概要

- **测试目标**：验证「最小完整运行的功能基本实现」+「不存在影响运行的报错」两个 loop 退出条件。
- **测试结果**：✅ PASS（首次 RunStart→PlayerDied + 再次 RunStart→PlayerDied 全程 0 Error / 0 Warning）。
- **关键发现**：
  1. 首战 HP 100→0 正常掉血至 PlayerDied。
  2. 再战 HP 已被 `CombatModule.OnGameStateChanged` 重置回 100，第二局完整复跑。
  3. 5 项输入注入（Space/E/MouseLeft/Move/Right/Tab）全部命中无报错。
- **耗时**：约 4 分钟（含两次 Run + 输入注入）。

## 测试流程

1. **准备**：编辑 [CombatModule.cs:200-220](Assets/Scripts/Modules/Combat/CombatModule.cs#L200-L220) 在进入 InGame 时复位 `PlayerTarget.Health = PlayerMaxHp`。
2. **退出旧 Play + 触发重编译**：`editor_execute_menu Edit/Play` → `asset_refresh` → `editor_get_state isCompiling=false`。
3. **进入 Play（已 allowlist）**：`editor_play` → `isPlaying=true`，console 清空。
4. **首战**：
   - `Tools/Playtest/01 Enable Simulator` → simulator ready
   - `Tools/Playtest/Debug/StartGame (-> InGame)` → `RunStarted MaxHp=100`
   - 敌人陆续命中，HP 100→92→84→66→58→50→32→24→16→6→0 → `PlayerDied`
5. **再战（核心验证点）**：
   - `Tools/Playtest/Debug/GoToMainMenu (-> MainMenu)` → State 切回 MainMenu，`_runStarted=false`
   - `Tools/Playtest/Debug/StartGame (-> InGame)` → `RunStarted MaxHp=100`（HP 复位生效）
   - HP 再次 100→92→…→16→0 → `PlayerDied`
6. **输入注入 PT-01..05**：Space (Dodge) / E (Skill) / MouseLeft (Attack) / Move/Right / Tab (SelfTattoo) 各一次。
7. **收尾**：`console_get_logs type=Error limit=40` 0 条；`console_get_logs type=Warning limit=40` 0 条；`Edit/Play` 退出 Play 模式。

## 遇到的问题

- **[OBSERVATION]** 首战中 `CombatHUDForm` 一次 `Action=LateInit Reason=RunStartedEvent_missed_before_Subscribe` —— HUD 订阅滞后于 RunStarted，已通过 LateInit fallback 自补全（非 Error / 非 Warning）。
- **[OBSERVATION]** 再战时观察到两条 `OldHP=0 NewHP=0` 残留 ApplyDamage —— 是 GoToMainMenu→StartGame 切换窗口内已在飞行中的敌方攻击事件触底；`PlayerDamageReceiver` clamp 到 0 即返回，无副作用。
- 无 Error / Warning。

## 关键日志摘录

```
[CombatModule|INFO]      Action=RunStarted MaxHp=100                    ← 首战
[PlayerDamageReceiver|INFO] Action=ApplyDamage OldHP=100.0 NewHP=92.0
... 持续掉血到 NewHP=0 ...
[GameStateModule|INFO]   Action=StateChanged Old=MainMenu New=InGame    ← 再战触发
[CombatModule|INFO]      Action=RunStarted MaxHp=100                    ← HP 已复位
[PlayerDamageReceiver|INFO] Action=ApplyDamage OldHP=100.0 NewHP=92.0   ← 复位有效
```

## 变更摘要

- [CombatModule.cs](Assets/Scripts/Modules/Combat/CombatModule.cs)
  - `Dependencies` 增加 `typeof(WeaponModule)`：修复 `InitializeAsync` 中 `GetModule<WeaponModule>()` 状态未 Initialized 抛 `InvalidOperationException`。
  - `OnGameStateChanged` 在 `MainMenu→InGame` 切换时把 `PlayerTarget.Health` 拉到 `PlayerMaxHp`：修复二次 StartGame 玩家 HP 残留为 0 无法再战。

## 后续

- [ ] StartupSelectForm 缺 Prefab 的 Warning（启动期，非阻塞）— 等 PRD/资源确认后再补。
- [ ] AudioModule MainMixer 缺失 fallback Warning（启动期，非阻塞）— 等音频资源接入后清理。
- [ ] [.claude/skills/playtest-driver/SKILL.md](.claude/skills/playtest-driver/SKILL.md) 中端口号 8091 → 8090（或改为动态读取）。
- [ ] HUD `LateInit` 路径长期看应拿掉：让 CombatHUDForm 在 `Bootstrap` 钩子内即注册 `RunStartedEvent`，避免 Start 时再二次补救。
