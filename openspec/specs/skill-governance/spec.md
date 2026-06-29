# skill-governance Specification

## Purpose
TBD - created by archiving change 11-skill-governance. Update Purpose after archive.
## Requirements
### Requirement: SKILL description 长度区间

所有 SKILL 的 frontmatter `description` 字段 MUST 落在 60-250 字符区间内，并 MUST 含触发词或「适用 / 触发」前缀。

#### Scenario: 极短 description 必须补强
- **GIVEN** SKILL.md frontmatter description < 60 字符
- **WHEN** 月度审计跑 `python tools/audit_skills.py`
- **THEN** 报告应列入「极短」清单，要求补足触发词

#### Scenario: 极长 description 必须压缩
- **GIVEN** SKILL.md frontmatter description > 250 字符
- **WHEN** 月度审计跑脚本
- **THEN** 报告列入「极长」清单，要求压缩至 ≤250

### Requirement: 重叠 SKILL 显式划界

存在功能重叠的 SKILL（如 `redis-best-practices ↔ redis-specialist`、`game-design-core ↔ game-design-theory` 等）MUST 在 description 中加入 `❌ 不适用：<场景>，请用 <SKILL>` 一行。

#### Scenario: 重叠 SKILL 命中划界
- **WHEN** 读取 redis-best-practices / redis-specialist 等已识别重叠组 SKILL
- **THEN** description 中 MUST 含 `❌ 不适用` 字样

### Requirement: 项目无关 SKILL 标注候选淘汰

项目无关 SKILL（character-sprite / agency-technical-artist 等）MUST 在 SKILLS_INDEX.md 「候选淘汰区」登记，下次清理周期评估正式删除。

#### Scenario: 候选淘汰区存在
- **WHEN** 查阅 `.claude/skills/SKILLS_INDEX.md`
- **THEN** 应能找到「候选淘汰区」段落 + 4 个 SKILL 名

### Requirement: MCP 启用清单按频次分层

`.claude/settings.local.json` 的 `enabledMcpjsonServers` MUST 仅含常驻 + 高频共 4 个：`skill4agent / codebase-memory / codex-art-gen / playwright`。其余 4 个（blender / godot / frame-ronin / atlassian）默认关闭，按需手动启用。

#### Scenario: 默认启用清单 = 4 个
- **WHEN** 读取 `.claude/settings.local.json` 的 `enabledMcpjsonServers`
- **THEN** 长度 = 4，且不含 blender/godot/frame-ronin/atlassian

#### Scenario: CLAUDE.md MCP 表对齐
- **WHEN** 查阅 `.claude/CLAUDE.md` §八 MCP 服务清单
- **THEN** 应有 8 个 MCP 行，其中前 4 个标常驻/高频，后 4 个标按需

### Requirement: 月度防腐审计

仓库 MUST 提供 `tools/audit_skills.py` 与 `tools/audit_skill_usage.py` 两个脚本，分别审计长度回涨与 0 召回 SKILL；CLAUDE.md §十 MUST 引用月度防腐机制。

#### Scenario: 审计脚本存在
- **WHEN** 列 tools/ 目录
- **THEN** audit_skills.py + audit_skill_usage.py 都应存在

#### Scenario: CLAUDE.md 引用防腐机制
- **WHEN** 查阅 CLAUDE.md §十 SKILL 系统
- **THEN** 应能找到「月度防腐机制」段落

