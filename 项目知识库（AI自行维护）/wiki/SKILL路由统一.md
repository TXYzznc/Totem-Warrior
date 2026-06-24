---
title: SKILL 路由系统统一（硬墙白名单 + gitnexus 清除）
owner: tools-engineer
created: 2026-06-24
last_updated: 2026-06-24
status: active
related_change: openspec/changes/02-skill-routing-unification/
---

# SKILL 路由系统统一

> 消除 `.claude/` 下两套 SKILL 路由机制的语义冲突，让框架风格一致。

## 一、决策摘要

| 维度 | 决策 |
|---|---|
| 白名单语义 | **硬墙**：agent 仅可调用 frontmatter.skills 列出的 SKILL；超出 → 立即 escalate_to: main，由主对话决定是否调用 find-skills 后再委派 |
| 同一 SKILL 多 owner | **允许**：openspec / ai-art / deep-research / grill-me 都是显式列入多个 agent 白名单的共享 SKILL |
| Wrapper SKILL | **全保留**：6 个 wrapper（unity-dev / dev-tools / document-tools / ai-art / openspec / deep-research）与原子 SKILL 并存；agent 自行选择 |
| gitnexus | **完全清除**：`.claude/skills/gitnexus/` 与 `.claude/GITNEXUS.md` 删除；功能由 codebase-memory MCP 承担 |
| 「项目原生」数字 | 7 大 → 6 大（gitnexus 退出） |

## 二、共享 SKILL 分布

| SKILL | 必须出现在的 agent 白名单 |
|---|---|
| `grill-me` | 5 lead |
| `openspec` | 4 lead（producer / gd-lead / client-lead / net-lead） |
| `ai-art` | 3 art-*（art-director / art-2d / art-ui） |
| `deep-research` | 2（producer / gd-lead） |

## 三、被否定的备选方案

### 备选 A：半墙 + find-skills（agent 可自行 find-skills 探索）

- **否定理由**：与 SKILL_MATRIX §四原始硬墙措辞矛盾；让 agent 自主性过大，AI 路由行为不可预测；用户明确选择硬墙。

### 备选 B：引入 `scope: global` 层

- **否定理由**：新概念引入复杂度；用「同一 SKILL 多 owner」的硬墙形态可以等价表达，更简单。

### 备选 C：退役冗余 wrapper（unity-dev / dev-tools / document-tools）

- **否定理由**：用户希望保留路由型聚合作为统一入口模式；这种 wrapper 与项目里许多 SKILL 的「索引 + references」结构一致。

### 备选 D：补 gitnexus 顶层 SKILL.md + 替换工具名为 `mcp__codebase-memory__*`

- **否定理由**：gitnexus 与 codebase-memory MCP 是两个功能相同的不同工具，用户明确要求清除避免双引擎并存；方法论已在 CLAUDE.md §八「codebase-memory MCP 使用准则」覆盖。

### 备选 E：deep-research 加入所有 lead 白名单

- **否定理由**：client-lead / net-lead 决策依据是代码与架构文献，由 codebase-memory MCP + WebSearch 工具覆盖足够；只有 producer/gd-lead 真正需要联网竞品/玩法调研。

## 四、为什么 gitnexus 必须删

关键事实链：

1. `tools/codebase-memory-mcp/codebase-memory-mcp.exe` 已配置在 `.mcp.json` core 档
2. 暴露的工具是 `mcp__codebase-memory__query_graph / search_code / trace_path / get_architecture` 等
3. `skills/gitnexus/gitnexus-*/SKILL.md` 里调用的工具名是 `gitnexus_query / gitnexus_context / gitnexus_impact`——**在当前 MCP 配置下不存在**
4. gitnexus 没有顶层 SKILL.md，[CLAUDE.md §十四](../../.claude/CLAUDE.md) 明确「skill 不能放子目录，Claude Code 不递归扫描」——**它本来就不被加载**
5. CLAUDE.md §八已经写了 codebase-memory 的使用准则，方法论已经覆盖

结论：gitnexus 是名实不副的「方法论文档 + 失效的工具调用指令」，删除无损失。

## 五、变更影响

- **不动**：116 个原子 SKILL 文件、wrapper SKILL 内容、settings.json hook、codebase-memory MCP 配置、ResourceModule / DataTable 等业务系统
- **改动**：5 份核心文档（CLAUDE.md / SKILL_MATRIX.md / SKILLS_INDEX.md / INDEX.md / 本 wiki）+ 19 个 agent frontmatter（6 个新增 SKILL，全部统一硬墙措辞）
- **删除**：`.claude/skills/gitnexus/` 树 + `.claude/GITNEXUS.md`

## 六、相关 openspec 变更

- [02-skill-routing-unification](../../openspec/changes/02-skill-routing-unification/)
  - [proposal.md](../../openspec/changes/02-skill-routing-unification/proposal.md)
  - [design.md](../../openspec/changes/02-skill-routing-unification/design.md)
  - [tasks.md](../../openspec/changes/02-skill-routing-unification/tasks.md)
  - [specs/skill-routing/spec.md](../../openspec/changes/02-skill-routing-unification/specs/skill-routing/spec.md)
