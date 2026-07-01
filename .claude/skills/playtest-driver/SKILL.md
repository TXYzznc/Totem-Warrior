---
name: playtest-driver
description: 在 Unity Editor 中自动驱动 playtest——通过 unity-skills CLI 触发 Tools/Playtest/* 菜单注入虚拟键鼠输入、点击 UI 按钮、控制 Play Mode、读取 FrameworkLogger 日志、生成 markdown 测试报告。触发：playtest、自动测试、模拟输入、模拟按键、模拟点击、跑界面、跑功能、playtest 报告。依赖 InputModule.EnableSimulator + unity-skills CLI（端口自动路由）+ PlaytestDriverEditor 菜单。
tags: playtest, unity-editor-automation, input-simulation, e2e-testing, test-report
tags_cn: Playtest 自动化, Unity 编辑器驱动, 输入模拟, 端到端测试, 测试报告
---

# playtest-driver

> 自动化 playtest 驱动 SOP。让 AI 在 Unity Editor 中"扮演玩家"：模拟键鼠输入、点击 UI、控制 Play Mode、读日志、出报告。

## 工具依赖

| 工具 | 用途 |
|---|---|
| **unity-skills CLI** (端口自动路由，见 [unity-skills SKILL](../unity-skills/SKILL.md) 的「多项目路由」章节) | `editor_play` / `editor_get_state` / `editor_execute_menu` / `console_*` / `event_invoke` / `asset_refresh` |
| **PlaytestDriverEditor** 菜单 (`Assets/Editor/Playtest/PlaytestDriverEditor.cs`) | `Tools/Playtest/01 Enable Simulator` / `Tools/Playtest/Press/<键>` / `Tools/Playtest/Hold/<键>` / `Tools/Playtest/Move/<方向>` |
| **InputModule.EnableSimulator** | 注入虚拟键鼠（仅 `UNITY_EDITOR / DEVELOPMENT_BUILD` 可用） |

**不再依赖 uloop CLI**。所有运行时操作改走 Editor 菜单 + `editor_execute_menu`，更稳定、可读、人工也能复跑。

**调用形式**：本 SKILL 所有示例统一用 `python .claude/skills/unity-skills/scripts/unity_skills.py <skill> [key=value]...`（下称 `us`）。端口从 `~/.unity_skills/registry.json` 按 cwd 自动匹配，跨项目用 `--target=<name>` 或 `--port=<num>` 覆盖。含 CJK/复杂 body 时改用 `--stdin-json`。

## 强制：跑之前必先写用例 + 预期（TDD-Lite）

> 任何 **multi-scenario / 全量 / 全流程 / 一组 N 个界面** 的 playtest 任务，**开跑前必须先沉淀测试用例文档**，**未沉淀禁止 `editor_play`**。
> 单条临时调试（如"按一下 E 看看日志"）不在此约束内。

**为什么**：playtest 自动化最大坑是"没预期就跑了一通"——日志一大堆，没人判断 PASS/FAIL，跑完只剩一句"看起来还行"。
先写"预期日志关键字 + 预期 UI 状态 + 通过标准"，跑完才能机械化对账，AI / 人都能复核。

**用例文档落点**（按优先级择一）：

1. **正在做的 openspec change** → `openspec/changes/<NN-name>/tests/plan.md`（强烈推荐——跟随归档不丢失）
2. **临时全量回归** → `tools/playtest/test-plans/<YYYY-MM-DD>-<scope>.md`

**每个用例必填 7 字段**：

| 字段 | 说明 | 例 |
|---|---|---|
| `TC-编号` | 唯一 ID | `TC-03` |
| `场景描述` | 一句话 | "按 Tab 打开自助纹身面板" |
| `前置条件` | 跑前必须满足的状态 | "已 STEP 2 进入 InGame，CombatHUD 显示" |
| `操作步骤` | 调哪些菜单 / 哪些 curl，按序 | "Tools/Playtest/Press/Tab (SelfTattoo)" |
| `预期日志` | console 抓什么关键字才算过 | `[SelfTattooForm` + `Action=Open` |
| `预期 UI / 状态` | 哪个 Form active / 哪个 state 切换 | `SelfTattooForm.IsOpen == true` |
| `通过标准 (PASS/FAIL)` | 命中预期 = PASS；少任意一条 = FAIL；新增 Error 直接 FAIL | "命中预期日志 + 0 Error" |

**写完后必须做的两件事**：

1. 把用例表贴在对话里给用户确认（无应答超过 1 轮 → 按草案执行，对账时标注"用户未确认"）
2. **对每条用例**在跑完后单独写一份报告 `tools/playtest/reports/YYYY-MM-DD-HHMM-<TC-编号>-<scope>.md`，最终汇总成 `YYYY-MM-DD-<scope>-summary.md` 把所有 TC 状态拉成一张表

**禁止行为**：
- ❌ 没写用例 → 直接 `editor_play` 一通乱跑
- ❌ 用例只有"步骤"没有"预期" → 跑完无法判 PASS/FAIL
- ❌ 用 UIForm 清单当用例 → 清单是覆盖率视角，不是行为视角；行为视角必须沿真实玩家路径（主菜单 → 战斗 → 互动 → 结算）

## 标准 SOP（5 阶段，禁止跳步）

> **别名约定**：下文所有示例把 `us` 视作 `python .claude/skills/unity-skills/scripts/unity_skills.py` 的短写；实际执行按完整命令写。

### STEP 1 — 准备

```bash
# 如已 playing 先停（注意：editor_stop 可能因 mode=auto 被拒，用 Edit/Play 切换式更稳）
us editor_execute_menu menuPath=Edit/Play  # 仅在 isPlaying=true 时调
us console_clear
```

### STEP 2 — 启动 Play 并等就绪

```bash
us editor_play
# 轮询：每 1s 一次，最多 15s，等 isPlaying=true && isCompiling=false
for i in $(seq 1 15); do
  sleep 1
  us editor_get_state
done
# 验 GameApp 就绪
us console_get_logs filter=GameApp limit=5
# 期望命中："所有模块初始化完成" / "GameReadyEvent"
```

### STEP 3 — 装配 InputSimulator

```bash
echo '{"menuPath":"Tools/Playtest/01 Enable Simulator"}' | us editor_execute_menu --stdin-json
# 期望日志："[Playtest|INFO] Action=EnableSimulator Type=InputSimulator"
```

失败 → 中止，写报告标 FAIL。常见原因：未在 Play Mode / GameApp 未就绪 / InputModule 未注册。

### STEP 4 — 输入注入循环

每个测试步骤一个迭代。

**单帧按键（玩法）**：菜单路径含空格 / 括号，一律走 `--stdin-json`：

```bash
# 按一次 E 触发技能
echo '{"menuPath":"Tools/Playtest/Press/E (Skill)"}' | us editor_execute_menu --stdin-json

# 鼠标左键（普攻）
echo '{"menuPath":"Tools/Playtest/Press/MouseLeft (Attack)"}' | us editor_execute_menu --stdin-json

# 空格闪避
echo '{"menuPath":"Tools/Playtest/Press/Space (Dodge)"}' | us editor_execute_menu --stdin-json
```

**持续按住（移动方向键）**（toggle 式：调一次开，再调一次关）：

```bash
echo '{"menuPath":"Tools/Playtest/Hold/W (Up)"}' | us editor_execute_menu --stdin-json
# … 业务运行一段时间 …
echo '{"menuPath":"Tools/Playtest/Hold/Clear All"}' | us editor_execute_menu --stdin-json
```

**移动方向覆盖**（直接强制一个向量，比 4 个按键组合更精准）：

```bash
us editor_execute_menu menuPath=Tools/Playtest/Move/Right
# 停下
us editor_execute_menu menuPath=Tools/Playtest/Move/Stop
```

**点击 UI 按钮**（绕过 InputModule，更直接；节点名含 CJK 时走 `--stdin-json`）：

```bash
echo '{"objectName":"<按钮节点名>","componentName":"Button","eventName":"onClick"}' | us event_invoke --stdin-json
```

**精准时序（可选）**：

```bash
# 暂停 → 注入 → 单帧推进 → 检查
us editor_execute_menu menuPath=Edit/Pause
echo '{"menuPath":"Tools/Playtest/Press/E (Skill)"}' | us editor_execute_menu --stdin-json
us editor_execute_menu menuPath=Edit/Step
us console_get_logs filter=Skill limit=20
```

**每步后读日志校验**：

```bash
sleep 0.3   # 让业务 OnUpdate 跑至少一帧
us console_get_logs filter=<关键字> limit=30
# 期望命中断言关键字；同时 type=Error 列错误
```

### STEP 5 — 收尾 + 报告

```bash
us console_get_logs type=Error limit=50
us console_get_logs type=Warning limit=20
us console_get_stats

# 停 Play（Edit/Play 是切换式，等价于退 Play Mode）
us editor_execute_menu menuPath=Edit/Play
```

写报告：`tools/playtest/reports/YYYY-MM-DD-HHMM-<scenario>.md`，套用 [`tools/playtest/reports/_TEMPLATE.md`](../../../tools/playtest/reports/_TEMPLATE.md)。

## 可用菜单清单（PlaytestDriverEditor）

| 菜单路径 | 说明 |
|---|---|
| `Tools/Playtest/01 Enable Simulator` | 装配 `InputSimulator`（必须先调） |
| `Tools/Playtest/02 Disable Simulator` | 卸下；恢复纯 `Input.GetXxx` 真实键鼠 |
| `Tools/Playtest/Press/E (Skill)` | 单帧按 E（技能） |
| `Tools/Playtest/Press/Space (Dodge)` | 单帧按 Space（闪避） |
| `Tools/Playtest/Press/Tab (SelfTattoo)` | 单帧按 Tab（自助纹身面板） |
| `Tools/Playtest/Press/Escape (Pause)` | 单帧按 Esc（暂停） |
| `Tools/Playtest/Press/Return (Confirm)` | 单帧按 Enter（确认） |
| `Tools/Playtest/Press/F12 (Debug)` | 单帧按 F12（debug 入口） |
| `Tools/Playtest/Press/MouseLeft (Attack)` | 单帧按鼠左（普攻） |
| `Tools/Playtest/Press/MouseRight` | 单帧按鼠右 |
| `Tools/Playtest/Hold/{W,A,S,D}` | toggle 持续按住 |
| `Tools/Playtest/Hold/Clear All` | 释放所有持续按键 |
| `Tools/Playtest/Move/{Right,Left,Up,Down}` | 强制覆盖移动向量 |
| `Tools/Playtest/Move/Stop` | 取消移动覆盖 |

## 触发判定规则（重要）

下列任何用户表述命中即应**立刻**调用本 SKILL，不要走任何 agent 委派绕路：

- "跑 playtest" / "自动测试 X" / "模拟玩家 X" / "模拟按键 X"
- "测一下 <某界面> 能不能用" / "自动跑通所有界面"
- "playtest 报告" / "写一份测试报告"
- 用户提及"模拟点击按钮"、"模拟鼠标"、"模拟输入"

**不走本 SKILL 的情况**：
- 单元测试 / EditMode / PlayMode 测试 → 走 `uloop-run-tests`
- Web E2E 测试 → 走 `playwright`
- 性能压测 → 走 `k6`

## 常见陷阱

| 陷阱 | 应对 |
|---|---|
| `editor_stop` 被 `MODE_FORBIDDEN` 拒 | unity-skills `mode=auto` 默认拒高风险。绕道：调 `editor_execute_menu menuPath=Edit/Play` 切换式退 Play |
| InputSimulator 注入后立即查 → KeyDown 被吞 | 至少 sleep 0.3s 让业务 OnUpdate 跑一帧；或用 `Edit/Step` 推进 1 帧再 `console_get_logs` |
| `isCompiling=true` 时调菜单 → 菜单未注册 | STEP 2 必须等 `!isCompiling` |
| Play 未启动时调 `Tools/Playtest/01` → WARN "当前不在 Play Mode" | 菜单本身有保护，按 STEP 顺序走就不会触发 |
| 多个 simulator 实例覆盖 | 菜单内部已对 `GetSimulator() != null` 判断，重复点不会爆 |
| `console_get_logs` 参数名 | 是 `type` / `filter` / `limit`，不是 `logTypes` |
| 报告路径 / 文件名碰撞 | 用时间戳：`date +"%Y-%m-%d-%H%M"`-scenario |

## PoC 参考报告

首份 PoC（验证菜单链路）见 [`tools/playtest/reports/2026-06-30-1205-poc-skill-input-injection.md`](../../../tools/playtest/reports/2026-06-30-1205-poc-skill-input-injection.md)。

## 跑当前所有界面（场景化）

项目当前 UI Form 清单（截至 2026-06-30）：

| 类别 | Form | 触发方式 |
|---|---|---|
| 主菜单 | MainMenuForm | 启动场景默认显示 |
| 角色选择 | CharacterSelectForm | MainMenu → 角色按钮 |
| 设置 | SettingsForm | Menu → 设置按钮 |
| 暂停菜单 | PauseMenuForm | 战斗中按 Esc |
| 战斗 HUD | CombatHUDForm | 进入战斗场景显示 |
| 跑酷结算 | RunResultForm | 战斗结束 |
| 商店 | ShopForm | NPC 交互 |
| 纹身工作台 | TattooStudioForm | NPC 交互 |
| 纹身附魔 | TattooEnchantForm | NPC 交互 |
| 自助纹身 | SelfTattooForm | Tab 键（`Tools/Playtest/Press/Tab`） |
| 三选一事件 | ThreeChoiceForm | 事件触发 |

**全量跑通模板**：

1. **先**按上方「强制：跑之前必先写用例 + 预期」规则在 `openspec/changes/<active-change>/tests/plan.md` 落地用例矩阵（按真实玩家旅程而非 Form 字典序）
2. 用户确认 / 1 轮无应答 → 按 TC 编号顺序执行
3. 每条 TC 一份独立报告 `tools/playtest/reports/YYYY-MM-DD-HHMM-<TC-编号>-<scope>.md`
4. 全跑完汇总 `tools/playtest/reports/YYYY-MM-DD-<scope>-summary.md`，含每条 TC 的 PASS/FAIL 表 + 错误聚合 + 后续 bug list

> 真实玩家旅程示例（v2.1 GDD）：MainMenu.unity 启动 → 点 Start → Launch 场景 → GameApp 就绪 → MainMenuForm 点 Start → InGame → CombatHUD → Tab 自助纹身 / 鼠左普攻 / E 技能 / 空格闪避 → Esc 暂停 → Resume → RunEnded → RunResultForm → ReturnToMenu。

## 与其他 SKILL 边界

- ❌ 单元测试（EditMode/PlayMode）→ 用 `uloop-run-tests`
- ❌ 写测试策略文档 → 用 `testing-strategies`
- ❌ Web 页面 E2E → 用 `playwright`
- ✅ Editor 中跑游戏 + 模拟玩家输入 + 验证日志 = 本 SKILL
