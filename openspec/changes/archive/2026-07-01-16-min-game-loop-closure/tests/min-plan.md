---
created: 2026-06-30
scope: B 闭环 = MainMenu→战斗→SelfTattoo→Pause→GameOver→MainMenu
source: 从 15-playtest-driver/tests/plan.md 裁剪
---

# 最小闭环 13 条 TC（Round N 通用回归集）

> 沿用 15-playtest-driver 的设计；剔除"D 方案 NPC"等本期 out-of-scope 的项。每轮 loop 跑完这 13 条即可判定退出条件 1。

| TC | 场景 | 操作 | 通过标准 |
|---|---|---|---|
| TC-01 | Launch 启动 → GameApp 就绪 | console_clear → editor_play → 轮询 editor_get_state | `[GameApp] 模块初始化完成` + `UIModule AllFormsLoaded Success=11/11` + 0 Error |
| TC-02 | MainMenuForm 自动显示 | console_get_logs filter=MainMenuForm | `MainMenuForm Action=Register` + active=True |
| TC-03 | 装配 InputSimulator | editor_execute_menu Tools/Playtest/01 Enable Simulator | `EnableSimulator Type=InputSimulator` |
| TC-04 | **真实**点击 StartButton → 进战斗 | event_invoke name=StartButton componentName=Button eventName=onClick | `MainMenuForm StartClicked` + `GameStateModule StateChanged Old=MainMenu New=InGame` + CombatHUDForm active=True |
| TC-05 | CombatHUD 订阅就绪 | DumpUIForms + console_get_logs filter=CombatHUDForm | CombatHUDForm active=True + `Action=Ready` 日志 |
| TC-06 | WASD 移动 | Tools/Playtest/Move/Right → sleep → Move/Stop | 两条 SetMove 日志 |
| TC-07 | 鼠左普攻 | Tools/Playtest/Press/MouseLeft (Attack) | PressMouse Button=0 |
| TC-08 | E 技能 | Tools/Playtest/Press/E (Skill) | PressKey Key=E |
| TC-09 | Space 闪避 | Tools/Playtest/Press/Space (Dodge) | PressKey Key=Space |
| TC-10 | Tab 打开 SelfTattooForm | Tools/Playtest/Press/Tab + DumpUIForms | PressKey Key=Tab + SelfTattooForm active=True（单实例，无双实例） |
| TC-11 | 再按 Tab 关闭 | Tools/Playtest/Press/Tab + DumpUIForms | SelfTattooForm active=False |
| TC-12 | Esc 暂停 | Tools/Playtest/Press/Escape (Pause) + DumpUIForms | PressKey Escape + PauseMenuForm active=True + Time.timeScale=0 |
| TC-13 | 全局 Error + 退 Play | console_get_logs type=Error + console_get_stats + Edit/Play | errors=0 + isPlaying=false |

> TC-13 Resume 暂时合并到 TC-12 的"暂停成立"标准里，因为本 loop 关注「能不能进入暂停态」而不是「从暂停恢复」。Resume 留到 B 闭环 PASS 后再补。
