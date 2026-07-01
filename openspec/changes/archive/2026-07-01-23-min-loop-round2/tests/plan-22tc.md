---
created: 2026-07-01
scope: 最小完整流程 22 TC = 16 B 闭环 13 TC + 9 条覆盖 change 18/20/22 新能力
source: 16-min-game-loop-closure/tests/min-plan.md + change 18/20/22 归档 spec
---

# 最小完整流程 22 条 TC（Round N 通用回归集）

> 每轮 loop 跑完这 22 条即可判定退出条件 1（TC 全 PASS）。
> 报错口径见 proposal.md：严格 `console_get_stats.errors == 0`；Warning 不阻塞。

## 基线 13 TC（沿用 16-min-game-loop-closure）

| TC | 场景 | 操作 | 通过标准 |
|---|---|---|---|
| TC-01 | Launch 启动 → GameApp 就绪 | `console_clear` → `editor_play` → 轮询 `editor_get_state` | `[GameApp] 模块初始化完成` + `UIModule AllFormsLoaded Success=12` + 0 Error |
| TC-02 | MainMenuForm 自动显示 | `console_get_logs filter=MainMenuForm` | `MainMenuForm Action=Register` + active=True |
| TC-03 | 装配 InputSimulator | `editor_execute_menu Tools/Playtest/01 Enable Simulator` | `EnableSimulator Type=InputSimulator` |
| TC-04 | 点击 StartButton → 进战斗 | `event_invoke name=StartButton componentName=Button eventName=onClick`；失败 fallback `editor_execute_menu Tools/Playtest/Debug/ClickMainStartButton` | `MainMenuForm StartClicked` + `GameStateModule StateChanged Old=MainMenu New=InGame` + CombatHUDForm active=True |
| TC-05 | CombatHUD 订阅就绪 | DumpUIForms + `console_get_logs filter=CombatHUDForm` | CombatHUDForm active=True + `Action=Ready` 日志 |
| TC-06 | WASD 移动 | `Tools/Playtest/Move/Right` → sleep → `Move/Stop` | 两条 SetMove 日志 |
| TC-07 | 鼠左普攻 | `Tools/Playtest/Press/MouseLeft (Attack)` | `PressMouse Button=0` |
| TC-08 | E 技能 | `Tools/Playtest/Press/E (Skill)` | `PressKey Key=E` |
| TC-09 | Space 闪避 | `Tools/Playtest/Press/Space (Dodge)` | `PressKey Key=Space` |
| TC-10 | Tab 打开 SelfTattooForm | `Tools/Playtest/Press/Tab` + `Tools/Playtest/Debug/Dump UIForms (active+inactive)` | `PressKey Key=Tab` + SelfTattooForm active=True（单实例） |
| TC-11 | 再按 Tab 关闭 | `Tools/Playtest/Press/Tab` + `Tools/Playtest/Debug/Dump UIForms (active+inactive)` | SelfTattooForm active=False |
| TC-12 | Esc 暂停 | `Tools/Playtest/Press/Escape (Pause)` + `Tools/Playtest/Debug/Dump UIForms (active+inactive)` | PauseMenuForm active=True + Time.timeScale=0 |
| TC-13 | 全局 Error 检查 + 退 Play | `console_get_logs type=Error` + `console_get_stats` + `Edit/Play` | errors=0 + isPlaying=false |

## 新能力扩展 9 TC（change 18/20/22 归档能力回归）

| TC | 场景 | 操作 | 通过标准 |
|---|---|---|---|
| TC-14 | 击杀一个 Bot | `editor_execute_menu Tools/Playtest/Combat/ForceKillNearestBot`（fan-out 新增 debug 菜单，绕过 3D 瞄准限制） → `console_get_logs filter=TargetKilled` | 至少 1 条 `TargetKilledEvent` 日志 + 击杀点位有 VFX AOEBurst 日志 + Console 可见 `PlaytestDriver Action=ForceKillNearestBot Pos=...`（辅助日志，方便交叉核对 VFX 位置） |
| TC-15 | 场景内出现武器 pickup | `editor_execute_menu Tools/Playtest/Combat/ForceSpawnWeaponPickup` → `console_get_logs filter=WeaponSpawned` | 至少 1 条 `WeaponSpawnedEvent` 日志（`WeaponSpawnerModule` 生成） |
| TC-16 | 玩家接触 pickup → 拾起 | `editor_execute_menu Tools/Playtest/Combat/ForcePickupNearestWeapon`（绕开物理触发直接发事件） → `console_get_logs filter=WeaponPickedUp` | `WeaponPickedUpEvent` 日志 + 武器切换成功日志 |
| TC-17 | 触发武器升级选择 | 连续 2 次组合：`ForceSpawnWeaponPickup` → `ForcePickupNearestWeapon` → `console_get_logs filter=WeaponUpgrade` | `WeaponUpgradeChoice` 日志 或 `WeaponUpgradedEvent` 日志（若数值未触发升级条件 → BLOCKED 记为流程限制而非代码 bug） |
| TC-18 | Hit spark VFX 接线 | `editor_execute_menu Tools/Playtest/Combat/Publish WeaponAttackHit (nearest bot)` → `console_get_logs filter=WeaponAttackHit` | `WeaponAttackHitEvent` + `VFXModule SpawnSpark` 日志 |
| TC-19 | Bot 染色差异 | `editor_execute_menu Tools/Playtest/Combat/ForceRefillEnemies`（保证屏幕有活 Bot） → sleep 1s → camera_screenshot → 目视检查 | 截图中 Smart/Light Bot 各至少 2 种不同 tint 颜色可见（暖 vs 冷） |
| TC-20 | 玩家死亡 → 大 burst | `PlayerDamageReceiver` HP 归零 or `event_invoke PlayerDied` → `console_get_logs filter=PlayerDied` | `PlayerDiedEvent` + `VFXModule SpawnAOEBurst` 日志 |
| TC-21 | RunResultForm 显示 | 战斗结束（玩家死或 Bot 全清）→ DumpUIForms | RunResultForm active=True |
| TC-22 | 二次开局 HP 无残留 | RunResult → 返回 MainMenu → 再点 Start → `console_get_logs filter=SpawnerModule filter=PlayerHp` | 玩家 HP == MaxHp（对应 fix 79e8471） |

## 说明

- **TC-14~TC-18**：验证 change 18（武器拾取升级） + change 22（VFX 三事件接线）
- **TC-19**：验证 change 22 子项 A（Bot 染色 shader）
- **TC-20**：验证 change 22 子项 B（PlayerDied → AOEBurst）+ change 20 D8 玩家受击通路
- **TC-21**：验证 RunResultForm 接线（原本 15/16 未覆盖）
- **TC-22**：验证 commit 79e8471「二次开局 HP 残留」的回归

## 执行工具

- Unity Skills REST：`http://localhost:8091/skill/<name>` (POST + JSON body)
- Playtest 菜单：`Tools/Playtest/*`（见 `Assets/Editor/Playtest/PlaytestDriverEditor.cs`）
- 每 TC 一份独立日志片段，最终汇总到 `results-round-N.md`

## 判定规则

- **PASS**：命中「通过标准」列的全部关键字 + 该 TC 段内 `type=Error` 计数增量 == 0
- **FAIL**：任一关键字未命中，或引入新 Error
- **BLOCKED**：前置 TC FAIL 导致本 TC 无法执行（如 TC-04 fail → TC-05~ 全部 BLOCKED）
