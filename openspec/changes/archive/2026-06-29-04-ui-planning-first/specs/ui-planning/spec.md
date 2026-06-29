# Spec Delta — ui-planning

## ADDED Requirements

### Requirement: UI 类型素材出图前必须先定表

ai-art SKILL 处理 `TYPE=UI` 的素材时 MUST 先起草「页面清单 / 复用组件清单 / 组件状态表」三表骨架到 `art/requirements.md`，并 MUST 等待用户审阅修订确认后才进入 `prompts.md` 提示词生成阶段。CHARACTER / ICON / SCENE / COMMON 四种非 UI 类型 MUST NOT 受此流程影响。

#### Scenario: UI 类型自动起草三表
- **GIVEN** 当前 active change 的素材类型判定为 UI
- **WHEN** ai-art 进入需求分析阶段
- **THEN** 主动生成三表骨架到 `art/requirements.md`，并提示用户审阅

#### Scenario: 三表未确认时禁止出图
- **GIVEN** `art/requirements.md` 三表缺失或用户未确认
- **WHEN** 尝试调用 codex-image-gen 出图
- **THEN** ai-art MUST 阻塞并提示「先定表」

#### Scenario: 非 UI 类型不受影响
- **GIVEN** 素材类型为 CHARACTER / ICON / SCENE / COMMON
- **WHEN** 进入 ai-art 流程
- **THEN** 跳过三表骨架步骤，直接按原流程出 prompts

### Requirement: 三表数量字段不写死

三表中的数量字段（每类组件几个）MUST 由 AI 根据当前 change 的页面清单实际推算，不得使用硬编码默认值。

#### Scenario: 数量随页面清单推算
- **GIVEN** 用户给出 3 个页面的 UI change
- **WHEN** AI 起草 B 表「复用组件清单」
- **THEN** 组件目标数量按 3 个页面实际复用情况推算，禁止固定写「主按钮 4-6 个」

### Requirement: 「先定表」规范全链路引用

`先定表` 概念 MUST 在 `ai-art/SKILL.md`、`drawing-prompt-UI.md`、`drawing-prompt-generator.md`、`CLAUDE.md §六` 四处统一引用，保证全链路一致。

#### Scenario: grep 验收文档命中
- **WHEN** 执行 `grep -r "先定表" .claude/`
- **THEN** 至少在 ai-art SKILL.md / drawing-prompt-UI.md / drawing-prompt-generator.md / CLAUDE.md 各命中 1 次
