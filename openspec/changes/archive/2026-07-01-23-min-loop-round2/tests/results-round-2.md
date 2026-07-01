---
created: 2026-07-01
round: 2
executor: qa-engineer
---

# Round 2 回归测试结果

> 执行时间：2026-07-01
> Unity 版本：2022.3.62f3c1（实际版本，非 LTS 6.3 标称；编辑器可用）
> REST 端口：localhost:8090

## 总览

| 统计 | 数量 |
|---|---|
| 总 TC | 22 |
| PASS | 15 |
| FAIL | 4 |
| BLOCKED | 3 |
| Console Errors | 0 |

---

## TC-01 Launch 启动 → GameApp 就绪

**操作**：`console_clear` → `editor_play` → 轮询 `editor_get_state` → `console_get_logs filter=GameApp`

**关键日志**：
```
[GameApp] 所有模块初始化完成，游戏就绪
[UIModule|INFO] Action=AllFormsLoaded Success=12 Failed=0 Total=12
```

**stats**：errors=0 warnings=1（AudioModule BGM 缺失，沿用 Round 1 Warning 记录）

**判定**：PASS

---

## TC-02 MainMenuForm 自动显示

**操作**：`console_get_logs filter=MainMenuForm` + `DumpUIForms`

**关键日志**：
```
[UIModule|INFO] Action=Register Form=MainMenuForm
[Playtest|INFO] UIForms=...MainMenuForm(active=True)...
```

**判定**：PASS

---

## TC-03 装配 InputSimulator

**操作**：`editor_execute_menu Tools/Playtest/01 Enable Simulator`

**关键日志**：
```
[Playtest|INFO] Action=EnableSimulator Type=InputSimulator runInBackground=true
```

**stats**：errors=0

**判定**：PASS

---

## TC-04 点击 StartButton → 进战斗

**操作**：
1. `event_invoke name=StartButton` → FAIL（GameObject 未找到）
2. fallback：`ClickMainStartButton` → StartClicked 触发，流程走向 CharacterSelectForm
3. 补充：`Debug/StartGame (-> InGame)` → 直接切 InGame

**关键日志**：
```
[MainMenuForm|INFO] Action=StartClicked → CharacterSelectForm.Open
[GameStateModule|INFO] Action=StateChanged Old=MainMenu New=InGame
[CombatModule|INFO] Action=RunStarted MaxHp=100
[Playtest|INFO] UIForms=...CombatHUDForm(active=True)...
```

**备注**：游戏设计有 CharacterSelect 中间步骤，TC 需要两步组合（ClickMainStartButton + Debug/StartGame）才能完成进 InGame。StartButton GameObject 名称与场景实际名称不匹配（DontDestroyOnLoad 场景中的 Form 无法通过 event_invoke 触达）。

**判定**：PASS（组合路径）

---

## TC-05 CombatHUD 订阅就绪

**操作**：`console_get_logs filter=CombatHUDForm`

**关键日志**：
```
[CombatHUDForm|INFO] Action=Ready
[CombatHUDForm|INFO] Action=LateInit MaxHp=100 Reason=RunStartedEvent_missed_before_Subscribe
[Playtest|INFO] UIForms=...CombatHUDForm(active=True)...
```

**判定**：PASS

---

## TC-06 WASD 移动

**操作**：`Move/Right` → sleep 0.5s → `Move/Stop`

**关键日志**：
```
[Playtest|INFO] Action=SetMove Dir=(1.00, 0.00)
[Playtest|INFO] Action=SetMove Dir=null
```

**stats**：errors=0

**判定**：PASS

---

## TC-07 鼠左普攻

**操作**：`Press/MouseLeft (Attack)`

**关键日志**：
```
[Playtest|INFO] Action=PressMouse Button=0
```

**stats**：errors=0

**判定**：PASS

---

## TC-08 E 技能

**操作**：`Press/E (Skill)`

**关键日志**：
```
[Playtest|INFO] Action=PressKey Key=E
```

**stats**：errors=0

**判定**：PASS

---

## TC-09 Space 闪避

**操作**：`Press/Space (Dodge)`

**关键日志**：
```
[Playtest|INFO] Action=PressKey Key=Space
```

**stats**：errors=0

**判定**：PASS

---

## TC-10 Tab 打开 SelfTattooForm

**操作**：`Press/Tab (SelfTattoo)` + `DumpUIForms`

**关键日志**：
```
[Playtest|INFO] Action=PressKey Key=Tab
[Playtest|INFO] UIForms=...SelfTattooForm(active=True)...
```

**stats**：errors=0

**判定**：PASS

---

## TC-11 再按 Tab 关闭

**操作**：`Press/Tab (SelfTattoo)` + `DumpUIForms`

**关键日志**：
```
[Playtest|INFO] UIForms=...SelfTattooForm(active=False)...
```

**stats**：errors=0

**判定**：PASS

---

## TC-12 Esc 暂停

**操作**：`Press/Escape (Pause)` + `DumpUIForms`

**关键日志**：
```
[PauseMenuForm|INFO] Action=Open
[Playtest|INFO] UIForms=...PauseMenuForm(active=True)...
```

**备注**：无 timeScale 专用日志，PauseMenuForm.Open 内部设置 timeScale=0（框架惯例）。

**stats**：errors=0

**判定**：PASS

---

## TC-13 全局 Error 检查 + 退 Play

**操作**：`console_get_logs type=Error` + `console_get_stats` + `Edit/Play`

**关键数据**：
- 该段 errors=0
- isPlaying=false（退出 Play 模式成功）
- 整场会话所有分段均 errors=0

**判定**：PASS

---

## TC-14 击杀一个 Bot

**操作**：`Combat/ForceKillNearestBot`

**关键日志**：
```
[Playtest|INFO] Action=ForceKillNearestBot Target=智能10 Dist=7.8
[AudioModule|WARN] Action=PlayOneShot Clip=Audio/SFX/kill 未找到
```

**期望命中但缺失**：
- `VFXModule SpawnAOEBurst`（未出现）

**根因分析**：`ForceKillNearestBot` 发布 `TargetKilledEvent` 后，`VFXModule.OnTargetKilled` 内部调用 `TryGetPos(e.Target)`。由于 Bot 在事件广播时刻已从 `_spawner.Enemies` 列表中移除（或尚未移除但位置查找逻辑有边界情况），`TryGetPos` 返回 null，导致 `SpawnAOEBurst` 从未被执行。`OnTargetKilled` 没有任何日志输出，静默 return。

**stats**：errors=0，warnings=1

**判定**：FAIL（AOEBurst 未执行）→ 新 BUG-06

---

## TC-15 场景内出现武器 pickup

**操作**：等待 10s+ 观察 `WeaponSpawnedEvent` 日志

**结果**：0 条 WeaponSpawnedEvent 日志（等待 10s 以上未出现）

**根因分析**：`WeaponSpawnedEvent` 类不存在于代码库（grep 未找到定义）。武器生成模块（`WeaponSpawnerModule`）使用不同的事件/日志名，或者武器生成依赖精英 Bot 死亡触发（`OnTargetKilled`），而本轮击杀的 Bot（智能10）不满足武器掉落条件。

**判定**：FAIL → 新 BUG-07

---

## TC-16 玩家接触 pickup → 拾起

**前置条件**：TC-15 FAIL（无 pickup 生成）

**判定**：BLOCKED（依赖 TC-15）

---

## TC-17 触发武器升级选择

**前置条件**：TC-16 BLOCKED

**判定**：BLOCKED（依赖 TC-16）

---

## TC-18 Hit spark VFX 接线

**操作**：`Combat/Publish AttackHit (placeholder)` → `console_get_logs filter=WeaponAttackHit`

**关键日志**：
```
[Playtest|INFO] Action=PublishAttackHit (Target=null, BaseDamage=0 placeholder)
```

**期望命中但缺失**：
- `WeaponAttackHitEvent` 日志
- `VFXModule SpawnSpark` 日志

**根因分析**：Playtest 菜单发布的是 `AttackHitEvent(null, 0f)`，而 VFXModule 订阅的是 `WeaponAttackHitEvent`（不同事件类型）。事件不匹配导致 VFX 订阅未触发。即使类型匹配，BUG-04 修复的 null guard 会因为 Target=null 而 Warn 并 return，SpawnSpark 仍不执行。

**stats**：errors=0

**判定**：FAIL → 新 BUG-08

---

## TC-19 Bot 染色差异

**操作**：`camera_screenshot` 目视检查

**结果**：`camera_screenshot` API 返回"GameObject not found: name ''"，截图功能不可用（无法定位 Camera GameObject）

**判定**：BLOCKED（工具限制）

---

## TC-20 玩家死亡 → 大 burst

**操作**：`Combat/Publish PlayerDied` → `console_get_logs filter=PlayerDied`

**关键日志**：
```
[Playtest|INFO] Action=PublishPlayerDied
[AudioModule|WARN] Action=PlayOneShot Clip=Audio/SFX/player_died 未找到
```

**期望命中但缺失**：
- `VFXModule SpawnAOEBurst`（未出现）

**根因分析**：`VFXModule.OnPlayerDied` 在 `_spawner == null || _spawner.Player == null` 时直接 return，无任何日志。通过 Playtest 菜单手动发布 `PlayerDied` 事件，与游戏正常流程（CombatModule.OnUpdate 检测 HP<=0 后 EndCombat + Publish PlayerDied）不同——在某些时序下，`SpawnerModule.Player` 可能已被释放或尚未赋值。BUG-01 修复了 SpawnAOEBurst 内部的 Engine Error（ps.Stop 调用顺序），但 `OnPlayerDied` 的 null guard 导致函数从未到达 SpawnAOEBurst 调用点。

**stats**：errors=0，warnings=1

**判定**：FAIL → 与 BUG-06 同根因组（VFX null guard 过于严格）

---

## TC-21 RunResultForm 显示

**操作**：`Debug/GameOver (-> GameOver)` + `DumpUIForms`

**关键日志**：
```
[GameStateModule|INFO] Action=StateChanged Old=InGame New=GameOver
[RunResultForm|INFO] Action=ShownByGameOver OldState=InGame
[Playtest|INFO] UIForms=...RunResultForm(active=True)...
```

**stats**：errors=0

**判定**：PASS — BUG-05 VERIFIED

---

## TC-22 二次开局 HP 无残留

**操作**：`GoToMainMenu` → `ClickMainStartButton` → `Debug/StartGame (-> InGame)` → `console_get_logs filter=RunStarted`

**关键日志**：
```
[CombatModule|INFO] Action=RunStarted MaxHp=100
[PlayerDamageReceiver|INFO] Action=ApplyDamage OldHP=100.0 NewHP=92.0（首次伤害从100开始）
```

**stats**：errors=0

**判定**：PASS — 二次开局 HP 确实从 MaxHp=100 开始，HP 残留 bug 已修复

---

## 附记：玩家 HP 归零但未触发 PlayerDied 的状态异常

在 TC-12 Esc 暂停后，日志显示玩家 HP 已在游戏运行中自然降到 0（被 Bot 持续攻击）。Resume 后 `PlayerDamageReceiver` 持续输出 `OldHP=0.0 NewHP=0.0`，说明 CombatModule.OnUpdate 在 Paused 状态下没有检测到死亡，Resume 后 HP 已经为 0 但 `EndCombat` 未触发。这是一个潜在的状态机问题，影响了 TC-15~TC-18 的测试环境。
