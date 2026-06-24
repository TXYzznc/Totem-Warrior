# Spec — SKILL 路由系统（统一硬墙白名单制）

> 本 spec 描述本次变更后 SKILL 路由系统应遵循的最终形态。
> 实施完成后，本 spec 应通过 `openspec sync-specs` 合并到主 spec。

---

## 一、机制总览

```
┌──────────────────┐
│   主对话 (main)   │  ── tier 最高，可调用所有 SKILL
└────────┬─────────┘
         │ 路由
         ▼
┌──────────────────┐    白名单内 ✓      ┌─────────────┐
│   agent          │ ─────────────────▶ │ 执行 SKILL   │
│ frontmatter:     │                    └─────────────┘
│   skills:        │
│     - a          │    白名单外 ✗      ┌─────────────┐
│     - b          │ ─────────────────▶ │ escalate    │
│     - c          │                    │ _to: main   │
└──────────────────┘                    └─────────────┘
```

## 二、规则

### R1 — 白名单是硬墙

- agent 仅可调用 `frontmatter.skills` 中列出的 SKILL
- 调用白名单外 SKILL 必须 `escalate_to: main`
- 主对话决定是否使用 `find-skills` 搜索后再委派给合适 agent

### R2 — 同一 SKILL 可在多个白名单中

- 共享 SKILL 不需要独占归属
- 跨 agent 高频共用的 SKILL 应显式列入每个相关 agent 的 `frontmatter.skills`

### R3 — agent prompt 「白名单外」措辞统一

```markdown
白名单外 SKILL → 立即 escalate_to: main（由主对话决定是否调用 find-skills 后再委派）
```

不允许「通过 find-skills 检索，或 escalate_to: main」式半墙措辞。

### R4 — Wrapper SKILL 与原子 SKILL 共存

- Wrapper SKILL（路由型聚合）：`unity-dev` / `dev-tools` / `document-tools` / `ai-art` / `openspec` / `deep-research`
- 原子 SKILL：其余 110 个
- agent 自行选择，不强制分级

### R5 — 跨 agent 共享 SKILL 分布

| SKILL | 必须出现在 |
|---|---|
| `grill-me` | 5 lead（producer / gd-lead / art-director / client-lead / net-lead） |
| `openspec` | 4 lead（producer / gd-lead / client-lead / net-lead） |
| `ai-art` | art-director / art-2d / art-ui |
| `deep-research` | producer / gd-lead |
| `find-skills` | tools-engineer（主对话内置调用） |

### R6 — 代码索引能力归 codebase-memory MCP

- gitnexus SKILL 体系已退役
- 代码分析 / 架构理解 / 影响分析 / Bug 追踪 / 重构 impact 由 codebase-memory MCP 承担
- 使用准则：CLAUDE.md §八「codebase-memory MCP 使用准则」

## 三、验收（自动化命令）

```bash
# gitnexus 完全清除
test "$(grep -ri 'gitnexus\|GITNEXUS' .claude/ | wc -l)" = "0"

# 半墙措辞清除
test "$(grep -r '或 find-skills' .claude/agents/ | wc -l)" = "0"

# 索引数字一致
grep -q '项目原生 6 大 SKILL' .claude/skills/SKILLS_INDEX.md

# openspec 进 4 lead 核心
for agent in producer gd-lead client-lead net-lead; do
  grep -q "^\s*-\s*openspec" .claude/agents/$agent.md || echo "MISSING openspec in $agent"
done

# ai-art 进 3 art 核心
for agent in art-director art-2d art-ui; do
  grep -q "^\s*-\s*ai-art" .claude/agents/$agent.md || echo "MISSING ai-art in $agent"
done

# deep-research 进 producer/gd-lead 核心
for agent in producer gd-lead; do
  grep -q "^\s*-\s*deep-research" .claude/agents/$agent.md || echo "MISSING deep-research in $agent"
done
```
