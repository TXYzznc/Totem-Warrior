---
created: 2026-06-30
scope: full-game-flow-v2.1
driver: playtest-driver SKILL (unity-skills REST + Tools/Playtest/* MenuItem)
status: 待用户确认
依赖前置: GameApp 在 Launch.unity 已就绪；InputSimulator 已通过 PoC 验证 (报告 2026-06-30-1205-poc-skill-input-injection.md)
---

# 全量 Playtest 测试计划 — GDD v2.1 真实玩家旅程

> 按 GDD v2.1 真实玩家旅程拆解，**不**按 UIForm 字典序。每条 TC 跑完单出一份报告，最后汇总到 `tools/playtest/reports/2026-06-30-full-flow-summary.md`。
> 用例字段遵循 [playtest-driver SKILL 强制规则](../../../../.claude/skills/playtest-driver/SKILL.md#强制跑之前必先写用例--预期tdd-lite)。

---

## 玩家旅程总图

```
┌────────────────┐  load   ┌─────────────────────────────────────────────────────────┐
│ MainMenu.unity │ ──────► │ Launch.unity                                            │
│ MainMenuLaunch │         │ ┌─ GameApp.Start 22 模块 ─ GameState.MainMenu          │
│  └ StartButton │         │ └ UIModule 注册 9 个 Form                              │
└────────────────┘         │                                                         │
                           │ MainMenuForm 显示                                       │
                           │     └ StartButton → GameStateModule.StartGame()         │
                           │                                                         │
                           │ GameState.InGame ── CombatHUDForm 显示                  │
                           │   ├ WASD 移动                                           │
                           │   ├ 鼠左 普攻 (WeaponModule)                            │
                           │   ├ Q/E 技能 (SkillModule)                              │
                           │   ├ Space 闪避 (DodgeModule)                            │
                           │   ├ Tab → SelfTattooForm (IExclusiveUIForm)             │
                           │   │    └ 选 部位/颜色/图案 → StartSelfTattoo → 读条     │
                           │   └ Esc → PauseRequestedEvent → PauseMenuForm           │
                           │        └ timeScale=0；Resume / Settings / Quit          │
                           │                                                         │
                           │ RunEndedEvent → GameState.GameOver                      │
                           │   └ RunResultForm                                       │
                           │        └ ReturnToMenuBtn → GoToMainMenu                 │
                           └─────────────────────────────────────────────────────────┘
```

---

## 测试用例矩阵（共 13 条 TC）

### 分组 A：启动与状态机（基线）

| TC | 场景 | 前置 | 操作步骤 | 预期日志 | 预期 UI/状态 | 通过标准 |
|---|---|---|---|---|---|---|
| **TC-01** | Launch 场景启动 → GameApp 就绪 | `editor_stop` 已退 Play；当前 Active Scene=Launch | 1. `console_clear`<br>2. `editor_play`<br>3. 轮询 `editor_get_state` 直到 `isPlaying=true && !isCompiling`<br>4. `console_get_logs filter=GameApp` | `[GameApp] 所有模块初始化完成，游戏就绪` + `[UIModule\|INFO] Action=AllFormsLoaded Success=11 Failed=0` | `isPlaying=true`；MainMenuForm GameObject active | 命中两条预期日志 + 0 Error |
| **TC-02** | MainMenuForm 自动显示 | TC-01 PASS | `console_get_logs filter=MainMenuForm limit=5` | `[MainMenuForm` 任意 Info（无 ERROR / Warn） | `MainMenuForm.gameObject.activeSelf==true` | 无 ERROR / 无 "等待 GameApp 超时" |
| **TC-03** | 装配 InputSimulator | TC-01 PASS | `editor_execute_menu Tools/Playtest/01 Enable Simulator` | `[Playtest\|INFO] Action=EnableSimulator Type=InputSimulator` | `InputModule.GetSimulator()!=null` | 命中预期日志 |

### 分组 B：进入战斗

| TC | 场景 | 前置 | 操作步骤 | 预期日志 | 预期 UI/状态 | 通过标准 |
|---|---|---|---|---|---|---|
| **TC-04** | 点击 MainMenu/StartButton → 进战斗 | TC-02 PASS | `event_invoke objectName=StartButton componentName=Button eventName=onClick`<br>（或直接调 `MainMenuForm.OnStartClicked` via menu，如缺则备用调用 `event_invoke`） | `[MainMenuForm\|INFO] Action=StartClicked → GameState.InGame` + `[GameStateModule\|INFO] Action=StateChanged Old=MainMenu New=InGame` | `GameStateModule.CurrentState==InGame`；CombatHUDForm.gameObject.activeSelf=true；MainMenuForm 隐藏 | 命中两条状态切换日志 + CombatHUD 显示 |
| **TC-05** | CombatHUD 订阅就绪 | TC-04 PASS | `console_get_logs filter=CombatHUDForm` | `[CombatHUDForm\|INFO] Action=Ready` | `CombatHUDForm.IsReady==true` | 命中日志 |

### 分组 C：核心战斗输入

| TC | 场景 | 前置 | 操作步骤 | 预期日志 | 预期 UI/状态 | 通过标准 |
|---|---|---|---|---|---|---|
| **TC-06** | WASD 移动覆盖 | TC-03 PASS | `editor_execute_menu Tools/Playtest/Move/Right`<br>sleep 0.5<br>`editor_execute_menu Tools/Playtest/Move/Stop` | `[Playtest\|INFO] Action=SetMove Dir=(1.0, 0.0)` + `Action=SetMove Dir=null` | 玩家朝右位移（如有 SpawnerModule.PlayerTarget 位置日志则验之） | 命中两条 SetMove 日志 |
| **TC-07** | 鼠左普攻 | TC-03 PASS | `editor_execute_menu Tools/Playtest/Press/MouseLeft (Attack)` | `[Playtest\|INFO] Action=PressMouse Button=0`；若 WeaponModule 触发：`[WeaponModule` 攻击事件 | DamagedEvent / 普攻动画 trigger | 命中 PressMouse 日志（WeaponModule 后续事件为加分项，不强制） |
| **TC-08** | E 技能（已 PoC 验证） | TC-03 PASS | `editor_execute_menu Tools/Playtest/Press/E (Skill)` | `[Playtest\|INFO] Action=PressKey Key=E` | `SkillModule` 内部 Q/E 槽 cd 启动；CombatHUD `_cdMaskE` fillAmount 1→0 | 命中 PressKey 日志（SkillCastEvent 为加分项） |
| **TC-09** | 空格闪避 | TC-03 PASS | `editor_execute_menu Tools/Playtest/Press/Space (Dodge)` | `[Playtest\|INFO] Action=PressKey Key=Space` | DodgeModule 触发闪避；玩家瞬移 | 命中 PressKey 日志 |

### 分组 D：自助纹身（Tab）

| TC | 场景 | 前置 | 操作步骤 | 预期日志 | 预期 UI/状态 | 通过标准 |
|---|---|---|---|---|---|---|
| **TC-10** | 按 Tab 打开 SelfTattooForm | TC-04 + TC-03 PASS | `editor_execute_menu Tools/Playtest/Press/Tab (SelfTattoo)` | `[Playtest\|INFO] Action=PressKey Key=Tab`；UIModule 互斥日志 | `SelfTattooForm.IsOpen==true`；CombatHUD 仍 active（叠加） | 命中 PressKey=Tab 日志 + Form 显示（用 event_invoke 检查 SetActive=true 或读 IsOpen 属性） |
| **TC-11** | 再按一次 Tab 关闭 | TC-10 PASS | `editor_execute_menu Tools/Playtest/Press/Tab (SelfTattoo)` | `Action=PressKey Key=Tab` | `SelfTattooForm.IsOpen==false` | DOFade 完成后 form 隐藏 |

### 分组 E：暂停 / 恢复

| TC | 场景 | 前置 | 操作步骤 | 预期日志 | 预期 UI/状态 | 通过标准 |
|---|---|---|---|---|---|---|
| **TC-12** | 按 Esc 暂停 | TC-04 PASS | `editor_execute_menu Tools/Playtest/Press/Escape (Pause)` | `[Playtest\|INFO] Action=PressKey Key=Escape` + `[PauseMenuForm\|INFO] Action=Open` | `PauseMenuForm.gameObject.activeSelf==true`；`Time.timeScale==0` | 命中两条日志 |
| **TC-13** | 点 Resume 退出暂停 | TC-12 PASS | `event_invoke objectName=ResumeButton componentName=Button eventName=onClick` | `[PauseMenuForm\|INFO] Action=Close` | `Time.timeScale==1`；PauseMenuForm 隐藏 | 命中 Action=Close + timeScale 恢复 |

### 分组 F：收尾

| TC | 场景 | 前置 | 操作步骤 | 预期日志 | 预期 UI/状态 | 通过标准 |
|---|---|---|---|---|---|---|
| **TC-14** | 全局错误检查 + 退 Play | 任意 TC 之后 | 1. `console_get_logs type=Error limit=50`<br>2. `console_get_stats`<br>3. `editor_execute_menu Edit/Play`（切换式退 Play） | — | `editor_get_state` 中 `isPlaying=false` | errors=0 或所有 error 已被前面 TC 标注；isPlaying=false |

---

## 暂不覆盖（明确边界）

| 项 | 不覆盖原因 |
|---|---|
| **MainMenu.unity 启动 + Launch 场景切换** | 当前 Editor 默认从 Launch 启动；需要场景切换的 TC 后续单出，本批不混 |
| **CharacterSelectForm** | 当前为空壳（仅 SetActive(false) + UIModule.Register），无交互可测 |
| **ThreeChoiceForm** | 当前为空壳；事件 `ThreeChoiceShownEvent` 触发链未连通；待 EventModule 实装后补 |
| **ShopForm / TattooStudioForm / TattooEnchantForm** | 依赖 NPCModule + 玩家进入互动半径，需要先有地图 / NPC 生成；后续在 NPC 实装的 change 中补 |
| **RunResultForm** | 需要 RunEndedEvent 触发；当前缺"主动结束本局"的命令式入口；待补 `Tools/Playtest/Debug/ForceEndRun` 菜单后再测 |
| **MapGenModule / SpawnerModule 50 actor 性能** | 不是 UI 行为测试，应该走 perf benchmark / Profiler，不在本批 |
| **多帧时序精度测试**（Edit/Step 单帧推进） | 本批是覆盖性测试，时序精度走后续单独 batch |

---

## 执行约定

1. **顺序**：A → B → C → D → E → F，分组内可不严格按编号但前置条件必须满足
2. **每条 TC 一份报告**：路径 `tools/playtest/reports/2026-06-30-HHMM-<TC-编号>-<slug>.md`，套用 `_TEMPLATE.md`
3. **汇总报告**：全跑完输出 `tools/playtest/reports/2026-06-30-full-flow-summary.md`，表头 `TC编号 / 场景 / 状态 / 耗时 / 备注 / 报告链接`
4. **任意 TC FAIL** → 写 FAIL 报告 + 追加 bug 到 `openspec/changes/15-playtest-driver/tests/bugs.md`，**继续下一个 TC**（用户 2026-06-30 决策：不阻塞继续）。若该 FAIL 是后续 TC 的硬前置（如 TC-03 Simulator 装配失败 → C/D 全 BLOCKED），后续 TC 状态标记为 `BLOCKED` 写入汇总，不重复尝试
5. **Error 计数**：每条 TC 跑完都要单独 `console_get_logs type=Error`，新增 Error 必须在该 TC 报告里登记；TC-14 做全局合计
6. **CJK 调用**：调 unity-skills 含中文菜单参数（如 `Tools/Playtest/Press/E (Skill)` 不含 CJK，OK；如需含中文按钮名走 `--stdin-json` 模式）

---

## 用户确认点

✅ **2026-06-30 用户已确认**：
1. 当前 Active Scene = **Launch**（按本计划直接 `editor_play`，无需切场景）
2. 失败处理 = **记录 + 跳过继续**（FAIL 不阻塞后续 TC，仅前置硬依赖 FAIL 后置 TC 标 BLOCKED）

---

## Changelog

- 2026-06-30：初稿，基于 GDD v2.1 + 7 个实装 UIForm 行为分析；14 条 TC
