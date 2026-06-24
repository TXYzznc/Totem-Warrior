# Proposal — 02-skill-routing-unification

> **范围**：消除 `.claude/` 下两套 SKILL 路由机制（白名单制 vs. 共享原生制）的语义冲突，让框架风格一致。
> **决策日期**：2026-06-24
> **决策方式**：grill-me 三轮反问（弹窗对话），用户在「硬墙白名单 / 全保留 wrapper / 清除 gitnexus」三处给出明确选择
> **预计阶段**：1 阶段（全部为文档与 frontmatter 变更，无代码）

---

## 为什么做

GameDesinger 在两次合并（自研框架 + 116 SKILL 库）后存在三处语义冲突：

| 现状 | 矛盾点 |
|---|---|
| [SKILL_MATRIX.md §四](../../../.claude/SKILL_MATRIX.md) 写「白名单外 SKILL → 立即 escalate」 | [producer.md:42](../../../.claude/agents/producer.md) / [client-unity.md:52](../../../.claude/agents/client-unity.md) 等 7 个 agent prompt 第 42 行又写「白名单外...或 find-skills，或 escalate」——硬墙 vs. 半墙 |
| [SKILLS_INDEX.md L22](../../../.claude/skills/SKILLS_INDEX.md) 强调「项目原生 7 大 SKILL，框架核心，强烈优先」 | 7 个里只有 `openspec` 真正进了 4 个 lead 的 `frontmatter.skills`（推荐列），其余 6 个对所有 agent 都属白名单外 |
| [GITNEXUS.md](../../../.claude/GITNEXUS.md) + [skills/gitnexus/](../../../.claude/skills/gitnexus/) | gitnexus 没有顶层 SKILL.md（不符合 Claude Code 加载规则）；子 skill 里调用的 `gitnexus_query` 等工具名在实际 `.mcp.json` 配置下不存在；功能被 codebase-memory MCP 完全覆盖 |

不解决 → AI 协作框架长期处于「文档说一套、agent 配置另一套」的撕裂状态；任何引用「项目原生 7 大」的话术都名实不副；gitnexus skill 里的工具调用指令永远跑不通。

## 目标（DoD）

- ✅ 白名单语义统一为**硬墙**：白名单外 SKILL 必须 escalate_to: main；agent prompt 第 42 行措辞与 SKILL_MATRIX §四完全一致
- ✅ 同一 SKILL 可在多个 agent 白名单中出现（共享而非独占）
- ✅ 跨 agent 共享的 3 个 SKILL 显式进白名单：
  - `openspec` → producer / gd-lead / client-lead / net-lead（4 lead）
  - `ai-art` → art-director / art-2d / art-ui（3 art）
  - `deep-research` → producer / gd-lead（2 个）
- ✅ wrapper SKILL（路由型聚合）全部保留：unity-dev / dev-tools / document-tools / ai-art / openspec / deep-research
- ✅ gitnexus 完全清除：`.claude/skills/gitnexus/` 整树删除、`.claude/GITNEXUS.md` 删除、所有引用替换为 codebase-memory MCP 准则
- ✅ `deep-research/SKILL.md` 去掉 `user-invocable: false` 阻塞
- ✅ SKILLS_INDEX 「项目原生 7 大」改为「项目原生 6 大」（数字与实际一致）
- ✅ 验收 grep：`.claude/` 下 `gitnexus` / `GITNEXUS` 出现次数 = 0

## 非目标（明确不做）

- ❌ 不动 wrapper 与原子 SKILL 的边界（保持现状并存）
- ❌ 不动 `settings.json` 的 hook（ai-art / 决策关键词触发依旧）
- ❌ 不动 codebase-memory MCP 本身配置
- ❌ 不退役任何 wrapper（用户已确认保留 unity-dev / dev-tools / document-tools）
- ❌ 不动 116 个原子 SKILL 文件本体
- ❌ 不重构 escalate_to / tier 机制本身

## 文件变更清单

| 文件 | 改动 |
|---|---|
| `.claude/skills/gitnexus/` | **删除整目录** |
| `.claude/GITNEXUS.md` | **删除** |
| `.claude/skills/deep-research/SKILL.md` | 去掉 frontmatter `user-invocable: false` |
| `.claude/skills/SKILLS_INDEX.md` | 7 大改 6 大；删 gitnexus 行；快速匹配表「代码分析/架构/影响范围/Bug追踪/重构 → gitnexus」改为「→ codebase-memory MCP」 |
| `.claude/SKILL_MATRIX.md` | §四措辞改硬墙；§二 producer/gd-lead/client-lead/net-lead 把 `openspec` 从「推荐」移到「核心」；producer/gd-lead 加 `deep-research` 到核心；art-director/art-2d/art-ui 加 `ai-art` 到核心 |
| `.claude/CLAUDE.md` | §十三索引表删 GITNEXUS.md 行 |
| `.claude/agents/producer.md` | frontmatter.skills 加 `openspec` + `deep-research`；prompt L42 「或 find-skills」→「立即 escalate_to: main」 |
| `.claude/agents/gd-lead.md` | frontmatter.skills 加 `openspec` + `deep-research`；prompt 同步硬墙措辞 |
| `.claude/agents/client-lead.md` | frontmatter.skills 加 `openspec`；prompt 同步 |
| `.claude/agents/net-lead.md` | frontmatter.skills 加 `openspec`；prompt 同步 |
| `.claude/agents/art-director.md` | frontmatter.skills 加 `ai-art`；prompt 同步 |
| `.claude/agents/art-2d.md` | frontmatter.skills 加 `ai-art`；prompt 同步 |
| `.claude/agents/art-ui.md` | frontmatter.skills 加 `ai-art`；prompt 同步 |
| `项目知识库（AI自行维护）/INDEX.md` | §四「当前活跃」表追加本 change；§3.5 工具链/DevOps 追加 wiki 链接 |
| `项目知识库（AI自行维护）/wiki/SKILL路由统一.md` | **新建**（决策摘要 + 被否定的备选方案） |

## 风险与回滚

| 风险 | 缓解 |
|---|---|
| gitnexus 清理后某些 agent 仍引用相关方法论 | grep 全局检查，把 `gitnexus_*` 字样替换为 `mcp__codebase-memory__*` |
| 用户后续想恢复 gitnexus | git revert 本 commit；GITNEXUS.md 与 skills/gitnexus/ 都在 git 历史里 |
| 白名单加 SKILL 后 agent token 超载 | 加的都是 wrapper 入口（轻量索引型 SKILL.md，详细 references/ 按需加载） |
| 硬墙改了之后 agent 频繁 escalate | 共享 SKILL 已经覆盖 4 lead + 3 art 高频路径；其余 escalate 本就是预期行为 |

回滚路径：

1. `git revert <本次 commit hash>` 即可恢复 gitnexus 与所有 frontmatter
2. 若仅想保留部分：手工 cherry-pick 反向 patch

## 引用

- [.claude/CLAUDE.md](../../../.claude/CLAUDE.md) — 框架戒律
- [.claude/SKILL_MATRIX.md](../../../.claude/SKILL_MATRIX.md) — 路由 source of truth
- [.claude/skills/SKILLS_INDEX.md](../../../.claude/skills/SKILLS_INDEX.md) — SKILL 索引
- 涉及 agents：producer / gd-lead / client-lead / net-lead / art-director / art-2d / art-ui（共 7 个）
