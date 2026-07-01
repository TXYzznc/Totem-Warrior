# ui-planning Specification

## Purpose
TBD - created by archiving change 04-ui-planning-first. Update Purpose after archive.
## Requirements
### Requirement: 「先定表」规范全链路引用

`先定表` 概念 MUST 在 `ai-art/SKILL.md`、`drawing-prompt-UI.md`、`drawing-prompt-generator.md`、`CLAUDE.md §六` 四处统一引用，保证全链路一致。

#### Scenario: grep 验收文档命中
- **WHEN** 执行 `grep -r "先定表" .claude/`
- **THEN** 至少在 ai-art SKILL.md / drawing-prompt-UI.md / drawing-prompt-generator.md / CLAUDE.md 各命中 1 次

