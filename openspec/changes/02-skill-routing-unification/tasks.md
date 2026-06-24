# Tasks — 02-skill-routing-unification

> 实施进度。✅ = 已交付；🔲 = 未完成。

---

## Phase A — 文档与配置统一（一次性，无代码改动）

### A.1 删除 gitnexus 残留

- [ ] 删除 `.claude/skills/gitnexus/` 整目录
- [ ] 删除 `.claude/GITNEXUS.md`

### A.2 解锁 deep-research

- [ ] 编辑 `.claude/skills/deep-research/SKILL.md`，从 frontmatter 删除 `user-invocable: false`

### A.3 更新 SKILLS_INDEX.md

- [ ] 标题「项目原生 7 大 SKILL（框架核心，强烈优先）」→「项目原生 6 大 SKILL」
- [ ] 删除表中 `gitnexus` 行
- [ ] 快速匹配表中 `代码分析、架构、影响范围、Bug追踪、重构 → gitnexus` 替换为 `→ codebase-memory MCP（详见 CLAUDE.md §八）`
- [ ] 文末「最后更新」改为本次日期

### A.4 更新 SKILL_MATRIX.md

- [ ] §四第 1 条措辞确认硬墙（已是硬墙，仅核对）
- [ ] §二表格：
  - [ ] `producer` 核心列加 `openspec` + `deep-research`
  - [ ] `gd-lead` 核心列加 `openspec` + `deep-research`
  - [ ] `client-lead` 核心列加 `openspec`
  - [ ] `net-lead` 核心列加 `openspec`
  - [ ] `art-director` 核心列加 `ai-art`
  - [ ] `art-2d` 核心列加 `ai-art`
  - [ ] `art-ui` 核心列加 `ai-art`
- [ ] 文末「最后更新」改为本次日期

### A.5 更新 CLAUDE.md

- [ ] §十三索引表删 `[GITNEXUS.md](./GITNEXUS.md)` 行

### A.6 更新 7 个 agent frontmatter

- [ ] `producer.md` frontmatter.skills 末尾加 `openspec` + `deep-research`
- [ ] `gd-lead.md` frontmatter.skills 末尾加 `openspec` + `deep-research`
- [ ] `client-lead.md` frontmatter.skills 末尾加 `openspec`
- [ ] `net-lead.md` frontmatter.skills 末尾加 `openspec`
- [ ] `art-director.md` frontmatter.skills 末尾加 `ai-art`
- [ ] `art-2d.md` frontmatter.skills 末尾加 `ai-art`
- [ ] `art-ui.md` frontmatter.skills 末尾加 `ai-art`

### A.7 修改 7 个 agent prompt 的「白名单外」措辞

将「白名单外（X / Y / Z）通过 find-skills 检索，或 escalate_to: main」改为「白名单外 SKILL → 立即 escalate_to: main（由主对话决定是否调用 find-skills 后再委派）」

- [ ] `producer.md` L42
- [ ] `gd-lead.md` 对应行
- [ ] `client-lead.md` 对应行
- [ ] `net-lead.md` 对应行
- [ ] `art-director.md` 对应行
- [ ] `art-2d.md` 对应行
- [ ] `art-ui.md` 对应行

### A.8 更新项目知识库 INDEX

- [ ] `项目知识库（AI自行维护）/INDEX.md` §四「当前活跃」表追加 `02-skill-routing-unification`
- [ ] `项目知识库（AI自行维护)/INDEX.md` §3.5 工具链/DevOps 追加 wiki 链接
- [ ] 新建 `项目知识库（AI自行维护）/wiki/SKILL路由统一.md`（决策摘要 + 被否定备选）

### A.9 验收

- [ ] `grep -ri "gitnexus\|GITNEXUS" .claude/` 输出 0 行
- [ ] `grep -r "或 find-skills" .claude/agents/` 输出 0 行
- [ ] `grep "项目原生 6 大 SKILL" .claude/skills/SKILLS_INDEX.md` 输出 1 行
- [ ] 7 个 agent frontmatter 的 skills 字段包含对应共享 SKILL（人工或脚本核对）
