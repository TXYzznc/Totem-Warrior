## ADDED Requirements

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
