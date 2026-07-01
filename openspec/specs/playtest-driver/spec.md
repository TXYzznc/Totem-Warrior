# playtest-driver Specification

## Purpose
TBD - created by archiving change 15-playtest-driver. Update Purpose after archive.
## Requirements
### Requirement: InputModule SHALL expose simulator injection entry points (Editor / Dev)

`InputModule` SHALL expose, under `UNITY_EDITOR || DEVELOPMENT_BUILD`:

- `EnableSimulator(IInputSimulator sim)` — 装配模拟器
- `DisableSimulator()` — 卸下
- `GetSimulator()` — 取当前 simulator（测试代码注入按键时使用）

Release builds SHALL strip these methods at compile time.

#### Scenario: Editor 装配后查询走模拟器

- **WHEN** 测试代码 `EnableSimulator(new InputSimulator())` 并 `PressKey(KeyCode.E)`
- **THEN** 下一帧 `InputModule.IsSkillPressed()` 返回 `true`
- **AND** 再下一帧 `InputModule.IsSkillPressed()` 返回 `false`（消费完毕）

#### Scenario: 未装配模拟器时走 Unity Input

- **WHEN** `_simulator == null`
- **THEN** `InputModule.IsSkillPressed()` 返回 `Input.GetKeyDown(KeyCode.E)`，与改造前行为完全一致

#### Scenario: 生产构建剥离

- **WHEN** 编译目标为 Release Player（无 DEVELOPMENT_BUILD）
- **THEN** `InputSimulator`、`IInputSimulator`、`EnableSimulator` 等符号不参与编译
- **AND** `InputModule.IsSkillPressed()` 仅含 `Input.GetKeyDown(KeyCode.E)` 调用

### Requirement: playtest-driver SKILL SHALL provide a standardized playtest SOP

SKILL file `.claude/skills/playtest-driver/SKILL.md` MUST:

- frontmatter `description` 包含触发词："playtest"、"自动测试"、"模拟输入" 至少 3 个
- SOP 覆盖 5 阶段：准备 → 启动 Play → 装配模拟器 → 注入循环 → 收尾报告
- 给出至少 1 个 PoC 示例（含 uloop 命令模板与日志断言示例）

#### Scenario: AI 提及"跑 playtest"

- **WHEN** 用户说"跑 playtest"或"自动测试一下设置界面"
- **THEN** 主对话或 qa-engineer 优先调用 playtest-driver SKILL
- **AND** 按 SOP 5 阶段顺序执行，不跳步

### Requirement: Playtest report SHALL satisfy structural conventions

Each playtest MUST emit a markdown report under `tools/playtest/reports/`:

- 文件名：`YYYY-MM-DD-HHMM-<scenario>.md`
- 头部 frontmatter：`test_time` / `scenario` / `result` / `duration_sec` / `errors_found` / `warnings_found`
- 正文：概要 / 测试流程 / 遇到的问题 / 后续

#### Scenario: PoC 报告生成

- **WHEN** PoC（按 E 触发技能）完成
- **THEN** `tools/playtest/reports/` 下生成符合规范的报告
- **AND** 报告内容能复现测试流程

