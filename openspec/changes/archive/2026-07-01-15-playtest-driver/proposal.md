# Proposal — 15-playtest-driver

## Why

AI 协作流程当前卡在**测试**这一环：

- 改完代码后没有自动化手段验证游戏运行时行为
- 玩家输入（键盘 / 鼠标 / UI 按钮）只能人在场手点，无法被 AI 主动驱动
- 跑一次完整界面/功能链路必须人盯着，无法 Auto Mode 长跑

unity-skills MCP 已经具备 `editor_play/pause/stop`、`console_get_logs`、`event_invoke`；uloop CLI 具备运行时 C# 注入。**唯一缺口是 `InputModule` 没有模拟入口** —— 业务在 OnUpdate 轮询 `IsAttackPressed()` 等接口，目前直接转发 `Input.GetKey*`，无法被外部注入。

## What

新增**自动化 playtest 基础设施**，由三部分组成：

1. **`InputSimulator`（接口 + 实现）**：与 InputModule 解耦的测试专用注入器。Editor / Dev 构建编译进去，生产构建剥离。
2. **`InputModule` 改造**：内部"双源融合"——优先读 `_simulator`，fallback 到 Unity `Input.*`。业务调用方零改动。
3. **`playtest-driver` SKILL**：AI 可调用的标准化测试驱动 SOP，封装「启 Play → 等就绪 → 注入输入 → 读日志 → 写报告」流程，配合 unity-skills MCP 与 uloop CLI 工作。

## Scope

**做**：

- `IInputSimulator` 接口与 `InputSimulator` 默认实现（队列式 KeyDown + 状态式 KeyHold + Mouse + Move）
- `InputModule` partial class 拆分：核心查询 + 模拟双源融合层（条件编译）
- `.claude/skills/playtest-driver/SKILL.md` + 标准 playtest 流程文档
- `tools/playtest/reports/` 目录 + 报告 markdown 模板（头部 = 时间 + 概要；正文 = 流程 + 问题）
- PoC：自动跑通"按 E 释放技能"完整链路，输出首份报告

**不做**：

- 不引入 Unity Test Framework（UTF）—— playtest 走 Editor Play Mode + 日志校验，不重写为 EditMode/PlayMode 单测
- 不做截图比对 / 像素 diff（后续可加）
- 不做覆盖率统计
- 不替换现有 UTF 测试（如果已有的话）
- 当前 change 只跑通 **PoC**；"跑通所有界面和功能"作为 SKILL 上线后的常态化任务，单独排期

## DoD

1. `InputSimulator` 与 `InputModule` 改造完成，编译通过；生产构建（剥离 `UNITY_EDITOR / DEVELOPMENT_BUILD`）下无注入入口
2. `playtest-driver` SKILL 上线，description 含明确触发词（"playtest"、"自动测试"、"模拟输入"、"跑界面" 等），AI 调用稳定可触发
3. PoC 报告生成成功：`tools/playtest/reports/<timestamp>-poc.md`，记录"E 键 → SkillModule 响应"完整流程
4. 测试报告模板可复用，后续每次 playtest 套用即可

## Risk

| 风险 | 影响 | 缓解 |
|---|---|---|
| Unity Editor pause 后无单帧推进 → 注入与帧消费时序不可控 | 中 | 用 `editor_execute_menu Edit/Step` 触发单帧推进（用户已确认 Ctrl+Alt+P 存在 Step 菜单） |
| `[CallerFilePath]` 在 uloop 动态代码中可能拿不到，干扰日志读取 | 低 | 不影响主流程，仅日志 location 字段会缺失 |
| InputSimulator 在生产构建下未剥离，被滥用 | 中 | `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` 全包；CI 加 grep 检查 |
| SKILL description 写得不好，AI 该调用时漏调 | 中 | description 写明 ≥3 触发关键词；触发后 SKILL 内步骤化指引，禁止漏步 |
