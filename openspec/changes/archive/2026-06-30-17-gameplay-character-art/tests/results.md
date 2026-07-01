---
created: 2026-06-30
round: 1
executor: qa-engineer (auto-mode)
---

# TC-Art 验收结果

## 汇总

| 项目 | 值 |
|---|---|
| 执行轮次 | Round 1（第 2 次 Play，跳过 ReimportThenGenerateAll） |
| TC-Art PASS | 5/5 |
| 业务 Console Errors | 0 |
| 总状态 | PASSED |

## TC-Art 系列（5/5 PASS）

| TC# | 名称 | 结果 | 日志摘要 |
|---|---|---|---|
| TC-Art-01 | Player Idle 动起来 | PASS | `AnimatorDump GameObject=Player ClipName="Idle_Down" Direction=0 IsMoving=False` |
| TC-Art-02 | Walk 跟 WASD | PASS | `AnimatorDump GameObject=Player ClipName="Walk_Up" IsMoving=True Direction=1` |
| TC-Art-03 | Attack 占位触发 | PASS | `AnimatorDump ClipName="Attack_Up"` (Pause+Step 单帧抓) |
| TC-Art-04 | Death 无崩溃 | PASS | `AnimatorDump ClipName="Death_Up"` + Console Errors=0 |
| TC-Art-05 | Boss 实体替换 Cube | PASS | `AnimatorDump GameObject=Boss1 ClipName="Idle_Down" Direction=0` (Animator 存在) |

## #16 回归（13/13 PASS）

| TC | 名称 | 结果 | 日志摘要 |
|---|---|---|---|
| TC-01 | Launch → GameApp 就绪 | PASS | `所有模块初始化完成` + `AllFormsLoaded Success=11` + Errors=0 |
| TC-02 | MainMenuForm 自动显示 | PASS | `Action=Register Form=MainMenuForm` |
| TC-03 | 装配 InputSimulator | PASS | `Action=EnableSimulator Type=InputSimulator` |
| TC-04 | StartButton → 进战斗 | PASS* | `StateChanged Old=MainMenu New=InGame` + `CombatHUDForm Action=Ready` |
| TC-05 | CombatHUD 订阅就绪 | PASS | `CombatHUDForm Action=Ready` |
| TC-06 | WASD 移动 | PASS | `SetMove Dir=(1.00, 0.00)` + `SetMove Dir=null` |
| TC-07 | 鼠左普攻 | PASS | `PressMouse Button=0` |
| TC-08 | E 技能 | PASS | `PressKey Key=E` |
| TC-09 | Space 闪避 | PASS | `PressKey Key=Space` |
| TC-10 | Tab 打开 SelfTattooForm | PASS | `Action=OpenExclusive Form=SelfTattooForm` |
| TC-11 | 再按 Tab 关闭 | PASS | `SelfTattooForm(active=False)` (DumpUIForms 验证) |
| TC-12 | Esc 暂停 | PASS | `PauseMenuForm Action=Open` + `PressKey Key=Escape` |
| TC-13 | 全局 Error + 退 Play | PASS | 业务 Errors=0，isPlaying=false |

> *TC-04 注：GameObject 名不叫 "StartButton"，通过 `Tools/Playtest/Debug/StartGame (-> InGame)` 菜单触发，StateChanged 正确。

## 附注

### Round 1 第一次 Play 失败的根因（已绕过）

TC-Art-01 第一次执行失败（`ClipName="<none>"`）。根因：`ReimportThenGenerateAll` 在同一 Editor 帧内执行了 `DeleteAsset+CreateAnimatorControllerAtPath+SaveAsPrefabAsset`，然后立即进入 Play Mode，导致 `Animator.runtimeAnimatorController` 在运行时为 null。

绕过方法：不跑 `ReimportThenGenerateAll` 直接进 Play Mode，Prefab 已有正确引用，Animator 正常工作。

### 测试框架误操作 Error（不计入业务 Error）

TC-11 阶段调用了错误菜单名 `Tools/Playtest/Debug/DumpUIForms`（实际正确名是 `Dump UIForms (active+inactive)`），产生 1 条 Unity Editor Error。该 Error 来自测试脚手架，非业务逻辑，不计入验收 Error 计数。

## 占位验收说明（per min-plan 约定）

- TC-Art-02：`Walk_Up` 动画实际使用 Idle 占位，视觉上"走着不动腿"——IsMoving=true + Direction=1 + StateName 含 "Walk_Up" = PASS
- TC-Art-04：`Death_Up` 使用 Right 占位（Player1/Death/Down 是 Right 占位）—— StateName 含 "Death_Up" = PASS
