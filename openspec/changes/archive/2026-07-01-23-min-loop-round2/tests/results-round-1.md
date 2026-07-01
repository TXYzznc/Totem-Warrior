---
date: 2026-07-01
round: 1
unity_version: 2022.3.62f1c1
editor_pid: 8708
executor: qa-engineer
port_used: localhost:8090
---

# Round 1 测试结果

## TC 结果表（22 条）

| TC# | 状态 | 命中日志摘要 | 关联 Bug# |
|---|---|---|---|
| TC-01 | FAIL | `[GameApp] 所有模块初始化完成` + `UIModule AllFormsLoaded Success=12` 命中；但启动即出现 Error：`VFXModule:SpawnAOEBurst ParticleSystem.duration 设置异常`（0 Error 要求不达标） | BUG-01 |
| TC-02 | PASS | `Action=Register Form=MainMenuForm` + `GameStateModule State=MainMenu` 命中 | — |
| TC-03 | PASS | `[Playtest\|INFO] Action=EnableSimulator Type=InputSimulator` 命中 | — |
| TC-04 | PASS | `MainMenuForm Action=StartClicked` + `StateChanged Old=MainMenu New=InGame` + `CombatHUDForm Action=Ready` 命中（通过 `Tools/Playtest/Change21/Test Full Flow` 完整走通 CharSel→StartupSel→InGame） | — |
| TC-05 | PASS | `CombatHUDForm Action=Ready` + `CombatModule RunStarted MaxHp=100` 命中 | — |
| TC-06 | PASS | `Action=SetMove Dir=(1.00, 0.00)` + `Action=SetMove Dir=null` 两条命中 | — |
| TC-07 | PASS | `Action=PressMouse Button=0` 命中 | — |
| TC-08 | PASS | `Action=PressKey Key=E` 命中 | — |
| TC-09 | PASS | `Action=PressKey Key=Space` 命中 | — |
| TC-10 | PASS | `Action=PressKey Key=Tab` + `UIModule Action=OpenExclusive Form=SelfTattooForm` + DumpUIForms `SelfTattooForm(active=True)` 命中 | — |
| TC-11 | PASS | 再按 Tab 后 DumpUIForms `SelfTattooForm(active=False)` 命中 | — |
| TC-12 | PASS | `PauseMenuForm Action=Open` + DumpUIForms `PauseMenuForm(active=True)` 命中 | — |
| TC-13 | FAIL | `console_get_stats errors=4`（要求 errors==0）；isPlaying=false 退出正常 | BUG-01, BUG-02, BUG-03 |
| TC-14 | FAIL | `TargetKilledEvent` 无实际触发（Subscribe 注册日志存在，但无 publish 日志）；Mouse 注入无法命中 Bot（无自动 aim）；AOEBurst VFX 日志也未见 | BUG-04 |
| TC-15 | FAIL | `WeaponSpawnedEvent` 无触发；WeaponSpawnerModule 初始化正常（DropRows=15）但 pickup 未 spawn（需 EnemyDied 触发，而 TC-14 失败无击杀） | BUG-04（阻断） |
| TC-16 | BLOCKED | 前置 TC-15 FAIL（无 Pickup 存在），玩家无法拾取武器 | — |
| TC-17 | BLOCKED | 前置 TC-16 BLOCKED，武器升级选择无法触发 | — |
| TC-18 | FAIL | `WeaponAttackHitEvent` 无实际触发（菜单 `Publish AttackHit placeholder` 用 Target=null，VFXModule 跳过 SpawnSpark；真实 AttackHit 需命中 Bot） | BUG-04（阻断） |
| TC-19 | PASS | 截图成功（MainCamera 1920x1080）；目视确认 Smart Bot 有 2+ 种暖色调（深红/橙红/橙棕）+ Light Bot 冷色/黑色，通过标准达标 | — |
| TC-20 | FAIL | `PlayerDiedEvent` 命中 + `VFXModule:SpawnAOEBurst` 被调用；但引入 Error：`ParticleSystem.duration 设置异常（系统仍在播放）`，Error 增量 = 1 | BUG-01 |
| TC-21 | FAIL | `RunResultForm(active=False)`；PlayerDied 事件未触发 GameState→GameOver；Debug/GameOver 强制切换后 RunResultForm 仍未显示 | BUG-05 |
| TC-22 | PASS | 二次开局 `CombatModule RunStarted MaxHp=100`，无 HP 残留（79e8471 fix 有效） | — |

## console_get_stats 结果（最终）

```
total: 303  logs: 294  warnings: 5  errors: 4
```

## Error 列表（4 条）

| # | 文件 | 行 | 错误摘要 | 首次触发 |
|---|---|---|---|---|
| E1 | VFXModule.cs:1214 / VFXModule.cs:935 | 935 | `ParticleSystem.set_duration` 在系统播放中调用；`OnPlayerDied→SpawnAOEBurst` | 启动后（之前某次 PlayerDied 残留） |
| E2 | VFXModule.cs:1214 / VFXModule.cs:935 | 935 | 同 E1，TC-20 `Publish PlayerDied` 时再次触发 | TC-20 |
| E3 | EditorSkills.cs:139 | — | `ExecuteMenuItem: 'Debug/ClickMainStartButton' 不存在`（路径错误，已修正为 `Tools/Playtest/Debug/ClickMainStartButton`） | TC-04 探测阶段 |
| E4 | EditorSkills.cs:139 | — | `ExecuteMenuItem: 'Tools/Playtest/DumpUIForms' 不存在`（实际路径是 `Tools/Playtest/Debug/Dump UIForms (active+inactive)`） | TC-10 探测阶段 |

> E3/E4 为测试工具菜单路径错误，非游戏 Bug（来自 plan-22tc.md 文档里 `DumpUIForms` 写法与实际菜单不符）。E1/E2 为真实游戏 Bug（VFXModule 粒子系统使用错误）。

## Warning 列表（5 条）

| 关键字 | 内容 | 频次 |
|---|---|---|
| AudioModule PlayBgm | `Audio/BGM/in_game 未找到` | 2（每次 InGame 进入触发 1 次） |
| AudioModule PlayOneShot | `Audio/SFX/player_died 未找到` | 2（TC-20 PlayerDied 触发 2 次） |
| UIModule AllFormsLoaded | `Success=12`（TC-01 通过标准写的是 `11/11`，实际 12 个 Form 注册） | 1 |

## Round 1 结论

**未达标，需进 Round 2。**

- PASS: TC-02, TC-03, TC-04, TC-05, TC-06, TC-07, TC-08, TC-09, TC-10, TC-11, TC-12, TC-19, TC-22 = **13 条 PASS**
- FAIL: TC-01, TC-13, TC-14, TC-15, TC-18, TC-20, TC-21 = **7 条 FAIL**
- BLOCKED: TC-16, TC-17 = **2 条 BLOCKED**

console_get_stats.errors = 4（要求 0）

**首要根因初判**：
1. **VFXModule 粒子系统 Bug（BUG-01）**：`SpawnAOEBurst` 在 `ParticleSystem` 仍在播放时调用 `set_duration`，引发 Unity Engine Error。影响 TC-01（启动残留）、TC-13（全局）、TC-20。root cause：VFXModule.cs:935 直接设置 `ps.main.duration`，需改为先 Stop/Clear 再设。
2. **Bot 击杀路径缺少自动化入口（BUG-04）**：TC-14~TC-18 的"击杀 Bot/命中 Bot"流程无法通过菜单注入实现，因为 MouseLeft 攻击需要 3D 碰撞 + 自动瞄准。`Publish AttackHit (placeholder)` 用 Target=null 导致 VFX 跳过 null 检查。需要：A) 为击杀场景补 Debug 菜单（`ForceKillNearestBot`），或 B) VFX 对 null target 仍生效（在玩家位置 spawn spark）。
3. **RunResultForm 未响应 GameOver（BUG-05）**：`Debug/GameOver` 后 RunResultForm active=False；PlayerDied 未级联到 GameState 切换。GameStateModule 或 RunResultForm 的 OnGameStateChanged 逻辑缺失或未订阅 GameOver→RunResult 路径。
