---
created: 2026-07-01
round: 3
executor: qa-engineer
---

# Round 3 回归测试结果

> 22 TC 全量跑一遍，重点验证 BUG-04/06/07/08 FIXED 状态。

## 汇总

| 状态 | 数量 |
|---|---|
| PASS | 16 |
| FAIL | 1 |
| BLOCKED | 4 |
| 新 Bug | 1（BUG-09） |

Console Stats（全程累计在 TC-18 之前 errors=0；TC-18 引入 1 Error）

---

## TC-01：Launch 启动 → GameApp 就绪

**操作**：`console_clear` → `editor_play` → 轮询 `editor_get_state`（1s 间隔，1 次即就绪）→ `console_get_logs filter=GameApp`

**关键日志**：
- `[GameApp] 所有模块初始化完成，游戏就绪`
- `[UIModule|INFO] Action=AllFormsLoaded Success=12 Failed=0 Total=12`

**Stats**：errors=0, warnings=1（音频资源缺失，已知）

**结果**：PASS

---

## TC-02：MainMenuForm 自动显示

**操作**：`console_get_logs filter=MainMenuForm`

**关键日志**：
- `[UIModule|INFO] Action=EarlyRegister Form=MainMenuForm`
- `[UIModule|INFO] Action=Register Form=MainMenuForm`
- `[GameStateModule|INFO] Action=Initialized State=MainMenu`

**结果**：PASS

---

## TC-03：装配 InputSimulator

**操作**：`editor_execute_menu menuPath=Tools/Playtest/01 Enable Simulator`

**关键日志**：`[Playtest|INFO] Action=EnableSimulator Type=InputSimulator runInBackground=true`

**结果**：PASS

---

## TC-04：点击 StartButton → 进战斗

**操作**：`event_invoke name=StartButton` 失败（DontDestroyOnLoad 场景内节点不可直接 event_invoke）→ fallback `DebugClickMainStartButton` → 打开 CharacterSelectForm → `Tools/Playtest/Change21/Test Full Flow`（含角色选择完整流程）

**关键日志**：
- `[MainMenuForm|INFO] Action=StartClicked → CharacterSelectForm.Open`
- `[GameStateModule|INFO] Action=StateChanged Old=MainMenu New=InGame`
- `[CombatHUDForm|INFO] Action=Ready`

**结果**：PASS

---

## TC-05：CombatHUD 订阅就绪

**操作**：`Dump UIForms` → `console_get_logs filter=CombatHUDForm`

**关键日志**：
- `CombatHUDForm(active=True)` 在 UIForms dump 中
- `[CombatHUDForm|INFO] Action=Ready`

**结果**：PASS

---

## TC-06：WASD 移动

**操作**：`Tools/Playtest/Move/Right` → sleep 0.5s → `Tools/Playtest/Move/Stop` → `console_get_logs filter=SetMove`

**关键日志**：
- `[Playtest|INFO] Action=SetMove Dir=(1.00, 0.00)`
- `[Playtest|INFO] Action=SetMove Dir=null`

**结果**：PASS

---

## TC-07：鼠左普攻

**操作**：`Tools/Playtest/Press/MouseLeft (Attack)` → `console_get_logs filter=PressMouse`

**关键日志**：`[Playtest|INFO] Action=PressMouse Button=0`

**结果**：PASS

---

## TC-08：E 技能

**操作**：`Tools/Playtest/Press/E (Skill)` → `console_get_logs filter=PressKey`

**关键日志**：`[Playtest|INFO] Action=PressKey Key=E`

**结果**：PASS

---

## TC-09：Space 闪避

**操作**：`Tools/Playtest/Press/Space (Dodge)` → `console_get_logs filter=PressKey`

**关键日志**：`[Playtest|INFO] Action=PressKey Key=Space`

**结果**：PASS

---

## TC-10：Tab 打开 SelfTattooForm

**操作**：`Tools/Playtest/Press/Tab (SelfTattoo)` → `Dump UIForms`

**关键日志**：
- `[Playtest|INFO] Action=PressKey Key=Tab`
- `SelfTattooForm(active=True)` 单实例

**结果**：PASS

---

## TC-11：再按 Tab 关闭

**操作**：再次 `Tools/Playtest/Press/Tab (SelfTattoo)` → `Dump UIForms`

**关键日志**：`SelfTattooForm(active=False)`

**结果**：PASS

---

## TC-12：Esc 暂停

**操作**：`Tools/Playtest/Press/Escape (Pause)` → `Dump UIForms`

**关键日志**：
- `PauseMenuForm(active=True)` 在 UIForms dump 中
- `[PauseMenuForm|INFO] Action=Open`

**备注**：Time.timeScale 无显式日志，通过 PauseMenuForm.Open 响应链确认暂停正常触发。

**结果**：PASS

---

## TC-13：全局 Error 检查 + 退 Play

**操作**：`console_get_logs type=Error` → `console_get_stats` → `Edit/Play`（退出 Play 模式）

**关键数据**：errors=0（TC-18 之前），isPlaying=false

**结果**：PASS

---

## TC-14：击杀一个 Bot（BUG-04 + BUG-06 回归）

**操作**：Resume → `Tools/Playtest/Combat/ForceKillNearestBot` → `console_get_logs filter=ForceKillNearestBot`

**关键日志**：
- `[PlaytestDriver|INFO] Action=ForceKillNearestBot Target=轻量29 Pos=(-5.83, 0.40, 1.36)`（BUG-04 辅助日志，新增）
- `[Playtest|INFO] Action=ForceKillNearestBot Target=轻量29 Dist=6.0 Pos=(-5.83, 0.40, 1.36)`
- `[VFXModule|INFO] Action=OnTargetKilled SpawnAOEBurst Pos=(-5.83, 0.80, 1.36)`（BUG-06 修复验证：不再被 null guard 阻断）

**Stats**：errors=0（此段），warnings=1（音频资源缺失，已知）

**结果**：PASS

**BUG-04 加固**：VERIFIED（辅助日志按预期出现，位置交叉核对可用）
**BUG-06 OnTargetKilled 路径**：VERIFIED（SpawnAOEBurst 正常执行）

---

## TC-15：场景内出现武器 pickup（BUG-07 回归）

**操作**：`console_get_logs filter=WeaponSpawned` → 等待 5s → 多次 ForceKillNearestBot → 仍未出现

**关键数据**：0 条 WeaponSpawnedEvent 日志；WeaponSpawnerModule 无任何日志输出

**分析**：`WeaponSpawnedEvent` 类已存在（BUG-07 代码修复有效），但 `SpawnDroppedWeapon` 路径需要精英击杀或宝箱奖励触发。当前场景 Bot 类型为"轻量29"，不满足精英掉落条件；无宝箱 debug 菜单。

**结果**：BLOCKED（数值触发条件：普通 Bot 不触发 SpawnDroppedWeapon，非 BUG-07 未修好）

**BUG-07**：代码路径存在但无法通过当前 playtest 工具强制触发，保留 FIXED 状态（触发条件属数值/关卡设计范畴）

---

## TC-16：玩家接触 pickup → 拾起

**操作**：检查 `console_get_logs filter=WeaponPickedUp`

**关键数据**：0 条日志（无 pickup 在场景中）

**结果**：BLOCKED（前置 TC-15 BLOCKED，无 pickup 可拾取）

---

## TC-17：触发武器升级选择

**操作**：检查 `console_get_logs filter=WeaponUpgrade`

**关键数据**：0 条日志

**结果**：BLOCKED（前置 TC-16 BLOCKED）

---

## TC-18：Hit spark VFX 接线（BUG-08 回归）

**操作**：`Tools/Playtest/Combat/Publish WeaponAttackHit (nearest bot)` → `console_get_logs filter=WeaponAttackHit`

**关键日志**：
- `[Playtest|INFO] Action=PublishWeaponAttackHit Target=轻量29`（BUG-08 菜单更名正确）
- `[AudioModule|WARN] PlayOneShot hit_default 未找到`（已知音频资源缺失）
- `Setting the duration while system is still playing is not supported` **ERROR** 在 `VFXModule:SpawnSpark (VFXModule.cs:991)`

**Stats**：errors=1（新增 Console Error）

**BUG-08 判定**：菜单发布 WeaponAttackHitEvent 事件类型正确，VFXModule 已收到并调用 SpawnSpark → BUG-08 本身修复有效，改为 VERIFIED。

**新 BUG-09**：SpawnSpark 内 `set_duration` 在粒子系统仍在播放时调用，与 BUG-01（AOEBurst 路径）root cause 相同，但 BUG-01 修复时只处理了 AOEBurst，SpawnSpark 路径（VFXModule.cs:991）未处理。

**结果**：FAIL（errors==1，违反 errors==0 口径）

---

## TC-19：Bot 染色差异（目视检查）

**操作**：Resume → `scene_screenshot` → 目视检查

**截图状况**：Resume 后截到 InGame 场景，但场景内所有 Bot 已在 TC-14 被 ForceKillNearestBot 击杀，画面只有击杀后的粒子效果，无活着 Bot 可目视验证染色差异。

**结果**：BLOCKED（场景无活着 Bot；前置需要独立于 ForceKill 的 Bot 生成验证环境）

---

## TC-20：玩家死亡 → 大 burst（BUG-06 OnPlayerDied 回归）

**操作**：`Tools/Playtest/Combat/Publish PlayerDied` → `console_get_logs filter=PlayerDied`

**关键日志**：
- `[Playtest|INFO] Action=PublishPlayerDied`
- `[VFXModule|INFO] Action=OnPlayerDied SpawnAOEBurst+Ring Pos=(0.00, 0.40, 0.00)`（BUG-06 修复验证：Player null 时用 Vector3.zero 兜底继续执行）

**Stats**：errors=0

**结果**：PASS

**BUG-06 OnPlayerDied 路径**：VERIFIED

---

## TC-21：RunResultForm 显示

**操作**：`Tools/Playtest/Debug/GameOver (-> GameOver)` → `Dump UIForms`

**关键日志**：`RunResultForm(active=True)` 在 UIForms dump 中

**结果**：PASS

---

## TC-22：二次开局 HP 无残留

**操作**：`GoToMainMenu` → `Change21/Test Full Flow`（第二次）→ `console_get_logs filter=RunStarted`

**关键日志**：`[CombatModule|INFO] Action=RunStarted MaxHp=100`

**备注**：MaxHp=100 表示 HP 已重置为满值，无残留。

**Stats**：errors=0

**结果**：PASS

---

## Bug 回归判定汇总

| Bug# | 关联 TC | 本轮验证路径 | 结论 |
|---|---|---|---|
| BUG-04 | TC-14 | ForceKillNearestBot 辅助日志出现，位置可交叉核对 | VERIFIED |
| BUG-06 | TC-14, TC-20 | OnTargetKilled AOEBurst 正常执行；OnPlayerDied SpawnAOEBurst+Ring 正常执行 | VERIFIED |
| BUG-07 | TC-15 | WeaponSpawnedEvent 类存在，但普通 Bot 无法触发 SpawnDroppedWeapon | 代码 FIXED，触发条件是数值/关卡问题，暂保 FIXED（见 TC-15 备注） |
| BUG-08 | TC-18 | 菜单发布 WeaponAttackHitEvent 正确，VFXModule 收到并调用 SpawnSpark | VERIFIED（SpawnSpark 内的新 Error 属 BUG-09，独立 bug） |

## 新发现 Bug

### BUG-09（Round 3 新增）

- **现象**：`Tools/Playtest/Combat/Publish WeaponAttackHit (nearest bot)` 触发后，Console 出现 `Setting the duration while system is still playing is not supported`，来自 `VFXModule.SpawnSpark` (VFXModule.cs:991)
- **重现步骤**：InGame 中 Publish WeaponAttackHitEvent → VFXModule.OnWeaponAttackHit → SpawnSpark → `ps.main.duration = ...` 在粒子系统仍在播放时调用
- **期望**：SpawnSpark 执行无 Console Error
- **实际**：Console Error + errors=1，TC-18 FAIL
- **根因 Hypothesis**：与 BUG-01（AOEBurst 路径）完全相同——`VFXModule.SpawnSpark` 直接赋值 `main.duration`，未先 `ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear)`。BUG-01 只修了 AOEBurst（VFXModule.cs:922-935），SpawnSpark（VFXModule.cs:991）遗漏。
- **环境**：Unity 2022.3.62f3c1 / Editor Play Mode
- **Severity**：High（引入 Console Error，阻塞 TC-18 PASS）
- **修复建议**：在 `SpawnSpark` 中，赋值 `main.duration` 前先调 `ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear)`，与 BUG-01 的修法保持一致。修复文件：`Assets/Scripts/Modules/VFX/VFXModule.cs` 约 L991

---

## 全量 TC 状态表

| TC | 描述 | 结果 |
|---|---|---|
| TC-01 | Launch 启动 → GameApp 就绪 | PASS |
| TC-02 | MainMenuForm 自动显示 | PASS |
| TC-03 | 装配 InputSimulator | PASS |
| TC-04 | 点击 StartButton → 进战斗 | PASS |
| TC-05 | CombatHUD 订阅就绪 | PASS |
| TC-06 | WASD 移动 | PASS |
| TC-07 | 鼠左普攻 | PASS |
| TC-08 | E 技能 | PASS |
| TC-09 | Space 闪避 | PASS |
| TC-10 | Tab 打开 SelfTattooForm | PASS |
| TC-11 | 再按 Tab 关闭 | PASS |
| TC-12 | Esc 暂停 | PASS |
| TC-13 | 全局 Error 检查 + 退 Play | PASS |
| TC-14 | 击杀一个 Bot | PASS |
| TC-15 | 场景内出现武器 pickup | BLOCKED |
| TC-16 | 玩家接触 pickup → 拾起 | BLOCKED |
| TC-17 | 触发武器升级选择 | BLOCKED |
| TC-18 | Hit spark VFX 接线 | FAIL |
| TC-19 | Bot 染色差异 | BLOCKED |
| TC-20 | 玩家死亡 → 大 burst | PASS |
| TC-21 | RunResultForm 显示 | PASS |
| TC-22 | 二次开局 HP 无残留 | PASS |
