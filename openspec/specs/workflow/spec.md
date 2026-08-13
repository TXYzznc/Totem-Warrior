# workflow Specification

## Purpose
TBD - created by archiving change 03-workflow-on-openspec. Update Purpose after archive.
## Requirements
### Requirement: openspec change 一站式目录树

中大型功能 MUST 通过 `openspec/changes/<NN-name>/` 单一目录承载全生命周期：proposal/design/tasks/specs（原生）+ brainstorm.md + CONTRACT.md（多模块）+ art/（美术）+ tests/（测试）。`工作/` 旧 5 Phase 目录 MUST NOT 再被引用。

#### Scenario: art/ 子目录承载美术
- **GIVEN** ai-art SKILL 触发
- **WHEN** 主对话准备落盘
- **THEN** 输出 MUST 落到 `openspec/changes/<name>/art/raw/`，requirements/prompts 在 `art/` 下

#### Scenario: tests/ 子目录承载测试文档
- **GIVEN** qa-engineer 写测试方案
- **WHEN** 落 tests/plan.md / results.md / bugs.md
- **THEN** MUST 全部进 `openspec/changes/<name>/tests/`

#### Scenario: 归档不依赖手动后缀
- **WHEN** 执行 `openspec archive <name>`
- **THEN** 整个 change 目录 MUST 自动搬到 `openspec/changes/archive/<date>-<name>/`，禁止人工加「（已归档）」后缀

### Requirement: 多模块全局契约位置统一

涉及多模块互相引用的 change MUST 把全局契约写到 `openspec/changes/<NN-name>/CONTRACT.md`，不再使用旧的 `3.正在处理的任务/NN.功能名/README.md`。

#### Scenario: AGENTS.md 指向新位置
- **WHEN** 阅读 `.claude/AGENTS.md`「骨架先行」段
- **THEN** 应看到 `openspec/changes/<NN-name>/CONTRACT.md` 路径

### Requirement: ai-art SKILL 路径硬编码迁移

`ai-art/SKILL.md` 中所有路径 MUST 引用 `openspec/changes/<name>/art/...`，不得包含 `工作/1.美术/` 或 `工作/工作区/` 字样。

#### Scenario: SKILL 路径全部 openspec 化
- **WHEN** 执行 `grep "工作/" .claude/skills/ai-art/SKILL.md`
- **THEN** 输出 0 行

### Requirement: GF_X 框架迁移必须先契约后实现
涉及 GF_X 或其他主框架迁移的 change MUST 先完成 `proposal.md`、`design.md`、`CONTRACT.md`、`specs/**/spec.md` 和 `tasks.md`，再执行跨目录文件迁移或核心启动链修改。

#### Scenario: 迁移前检查契约
- **WHEN** 准备迁入 GF_X 文件或修改启动链
- **THEN** 执行者 MUST 先确认当前 change 下存在 `CONTRACT.md`
- **AND** 执行者 MUST 依据 `tasks.md` 的顺序推进

### Requirement: 核心文件修改必须显式确认
框架迁移触及当前项目核心启动、模块生命周期、事件、输入、配置表、UI、场景加载、包依赖或构建场景时，执行者 MUST 在修改前向用户列出文件、原因、风险和验证方式，并等待确认。

#### Scenario: 用户确认后再改核心文件
- **WHEN** 迁移需要修改受保护核心文件
- **THEN** 执行者 MUST 暂停该修改步骤并请求用户确认
- **AND** 只有确认后才能应用对应文件修改

### Requirement: 迁移记录必须可追踪
框架迁移 MUST 记录新增、移动、排除和适配的关键文件或目录。记录 MUST 能支持后续回滚、诊断和二阶段业务重构。

#### Scenario: 迁移后可追踪来源
- **WHEN** 第一阶段迁移完成
- **THEN** 迁移记录 MUST 说明 GF_X 文件来源、目标路径、排除项和适配项
- **AND** 后续重构 MUST 能根据记录判断文件归属

