---
created: 2026-06-30
scope: gameplay-character-art TC-Art 验收
---

# TC-Art — 5 条严格用例

> qa-engineer 负责编排 playtest loop 跑这 5 条 + #16 的 13 条 = 18 条全 PASS。
> 每条 TC 必须可被 PlaytestDriverEditor 菜单或 unity-skills MCP 自动驱动。

| TC# | 名称 | 步骤 | 期望（严格） | 期望失败时 status |
|---|---|---|---|---|
| **TC-Art-01** | Player Idle 动起来 | 1. Play 进 InGame<br>2. 等 1s，不按任何键<br>3. DumpAnimatorState Player | Animator.GetCurrentAnimatorStateInfo(0).IsName("Idle_Down") == true（或对应方向）<br>**禁止**仍是 Cube primitive | OPEN |
| **TC-Art-02** | Walk 跟着 WASD | 1. Play 进 InGame<br>2. PressKey W 0.5s<br>3. DumpAnimatorState Player | IsMoving=true && Direction=1（Up） && 当前 State 名含 "Walk_Up" | OPEN |
| **TC-Art-03** | Attack 占位触发 | 1. Play 进 InGame<br>2. 模拟左键单击或调 CombatModule.OnAttack<br>3. DumpAnimatorState Player | AttackTrigger 被消费（IsInTransition==true 或当前 state 名含 "Attack_*"） | OPEN |
| **TC-Art-04** | Death 无崩溃 | 1. Play 进 InGame<br>2. 调 PlayerDiedEvent.Publish<br>3. 等 2s<br>4. DumpAnimatorState + console_get_logs | 当前 state 名含 "Death_*"，**Console Errors == 0** | OPEN |
| **TC-Art-05** | Boss 实体替换 Cube | 1. Play 进 InGame<br>2. batch_query_gameobjects Boss1<br>3. DumpAnimatorState Boss1 | 找到恰好 1 个 Boss1 实例，含 Animator 组件，初始 state 含 "Idle_*"，**禁止**仍是 Cube primitive | OPEN |

## 退出门槛

5/5 TC-Art PASS + 0 Console Errors + #16 的 13 条仍全 PASS → 退出 loop。

## 终止安全网

- 单 bug 连续 5 轮未解 → 终止交回用户
- codex-image-gen 单图 3 轮重试仍失败 → 阻塞通知用户

## 自动化菜单序列（qa-engineer 走 unity-skills MCP editor_execute_menu）

> 前置：[Assets/Editor/Character/AnimatorGenerator.cs](../../../../Assets/Editor/Character/AnimatorGenerator.cs)、[Assets/Editor/Character/CharacterSpriteImportProcessor.cs](../../../../Assets/Editor/Character/CharacterSpriteImportProcessor.cs)、[Assets/Editor/Playtest/PlaytestDriverEditor.cs](../../../../Assets/Editor/Playtest/PlaytestDriverEditor.cs) 已编译通过；40 张 sprite 已落盘。
>
> 每个 TC 之间用 `console_clear_logs` 把 Console 清空，方便单独抓本 TC 期间的错误。

### Pre-flight（每次跑测一次）

1. `Tools/Character/Reimport Then Generate All` — 把 PPU=256 应用到现存 PNG，并批生 anim/controller/prefab
2. （Editor 退出 Play）`editor_play_mode_start` → 等 GameApp Ready
3. `Tools/Playtest/01 Enable Simulator`
4. `Tools/Playtest/Debug/StartGame (-> InGame)`

### TC-Art-01：Idle 默认动起来

1. `Tools/Playtest/Hold/Clear All` （清掉残留方向）
2. 等 1 s
3. `Tools/Playtest/Animator/Dump Player State` → 日志 `ClipName="Idle_Down"`（或 Up/Left/Right）

### TC-Art-02：Walk 跟 WASD

1. `Tools/Playtest/Hold/W (Up)` 
2. 等 0.5 s
3. `Tools/Playtest/Animator/Dump Player State` → 日志 `ClipName="Walk_Up" IsMoving=true Direction=1`
4. `Tools/Playtest/Hold/W (Up)` 取消按住

### TC-Art-03：Attack 占位

1. `Tools/Playtest/Combat/Publish AttackHit (placeholder)`
2. 等 0.1 s（一帧）
3. `Tools/Playtest/Animator/Dump Player State` → 日志包含 `Attack_` 或 `InTransition=true`

### TC-Art-04：Death 不崩

1. `console_clear_logs`
2. `Tools/Playtest/Combat/Publish PlayerDied`
3. 等 2 s
4. `Tools/Playtest/Animator/Dump Player State` → 日志 `ClipName` 包含 `Death_`
5. `console_get_logs level=Error count==0`

### TC-Art-05：Boss 实体

1. `Tools/Playtest/Animator/Dump Boss1 State` → 日志 `ClipName` 包含 `Idle_`、`Animator` 存在；**不**报 `IsCube=true`

### 收尾

- `Tools/Playtest/Debug/GoToMainMenu`
- `editor_play_mode_stop`
