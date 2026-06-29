# Design — 02-skill-routing-unification

> 设计决策的展开版。本文回答 grill-me 三轮反问的每个分支与被否定的备选方案。

---

## 决策 1：白名单语义 = 硬墙

### 选择

**硬墙**：agent 仅可使用 `frontmatter.skills` 列出的 SKILL；超出 → 立即 `escalate_to: main`。
同一 SKILL 可在多个 agent 的白名单中重复出现（共享而非独占）。

### 被否定的备选

| 备选 | 否定理由 |
|---|---|
| 半墙 + find-skills | 实际 7 个 agent prompt 第 42 行已经在用，但与 SKILL_MATRIX §四硬墙矛盾。继续走半墙 = 让矛盾合法化，AI 行为不可预测 |
| 引入 `scope: global` 层 | 新概念引入复杂度；且与「同一 SKILL 可在多个白名单」组合起来等价于硬墙 + 多 owner，更简单 |

### 实施细节

- SKILL_MATRIX.md §四第 1 条保持原措辞「白名单外 SKILL → 立即停止本 agent 任务并交回主对话」
- 7 个 agent prompt 第 42 行的「通过 find-skills 检索，或 escalate_to: main」改为「立即 escalate_to: main（如确需，由主对话调用 find-skills 后再委派）」
- find-skills 仍存在但仅主对话使用；agent 不再绕开白名单

---

## 决策 2：wrapper SKILL 全保留

### 选择

保留全部 6 个 wrapper 类 SKILL：
- **冗余 wrapper**（子能力已独立为原子 SKILL）：`unity-dev` / `dev-tools` / `document-tools`
- **聚合 wrapper**（子能力仅在 references/）：`ai-art` / `openspec` / `deep-research`

agent 自行选择用 wrapper（一站式入口）还是原子 SKILL（精细控制），不强制分级规则。

### 被否定的备选

| 备选 | 否定理由 |
|---|---|
| 退役冗余 wrapper（unity-dev / dev-tools / document-tools） | 用户希望保留路由型聚合作为统一入口模式；这与项目里很多 SKILL 的「索引型 SKILL.md + references/」结构一致 |
| 全退役 wrapper、聚合子能力升格为独立 SKILL | 工作量最大（要拆 18+ 个 references → 独立 SKILL）；破坏 hook 里对 `ai-art` 名字的引用 |

### 实施细节

- 不改 SKILLS_INDEX 对 wrapper 的描述
- agent frontmatter 按现状选用：
  - `client-unity` 用原子 `unity-foundations` / `unity-ui` 等（既有）
  - `tools-engineer` 用原子 `skill-creator` / `find-skills`（既有）
  - 不强制改成 wrapper 路径

---

## 决策 3：gitnexus 完全清除

### 选择

删除 `.claude/skills/gitnexus/` 整目录与 `.claude/GITNEXUS.md`；功能完全由 codebase-memory MCP 承担。

### 关键事实链

1. `tools/codebase-memory-mcp/codebase-memory-mcp.exe`（已配置在 [.mcp.json](../../../.mcp.json) core 档）暴露的工具是 `mcp__codebase-memory__query_graph` / `search_code` / `trace_path` / `get_architecture` 等
2. `skills/gitnexus/gitnexus-*/SKILL.md` 里调用的工具名是 `gitnexus_query` / `gitnexus_context` / `gitnexus_impact` 等——**在当前 MCP 配置下不存在**
3. 用户明确反馈「它们两个完全没有依赖关系，是两个功能相同的不同的工具，目前项目选择使用 codebase-memory MCP」
4. CLAUDE.md §八已经写了 codebase-memory 的使用准则（适用场景 / 不适用场景），方法论已经覆盖
5. gitnexus 没有顶层 SKILL.md，[CLAUDE.md §十四](../../../.claude/CLAUDE.md) 明确「skill 不能放子目录，Claude Code 不递归扫描」——**它本来就不被加载**

### 被否定的备选

| 备选 | 否定理由 |
|---|---|
| 补顶层 SKILL.md + 改写工具名为 mcp__codebase-memory__* | 用户明确要求清除，不要双引擎并存 |
| 保留 SKILL 文档作为方法论 | 与 CLAUDE.md §八重复；方法论已经在那里 |
| 平铺 6 个子目录到 skills/ 根 | 同样违背用户清除意图，且 SKILL 总数膨胀 |

### 实施细节

- `rm -rf .claude/skills/gitnexus/`
- `rm .claude/GITNEXUS.md`
- SKILLS_INDEX.md：
  - 「项目原生 7 大 SKILL」→「项目原生 6 大 SKILL」
  - 删除表中 `gitnexus` 行
  - 快速匹配表行 `代码分析、架构、影响范围、Bug追踪、重构 → gitnexus` 改为 `→ codebase-memory MCP（详见 CLAUDE.md §八）`
- CLAUDE.md §十三索引表删 `[GITNEXUS.md](./GITNEXUS.md)` 行

---

## 决策 4：跨 agent 共享 SKILL 分布

### 选择

| SKILL | 加入白名单 | 理由 |
|---|---|---|
| `openspec` | producer / gd-lead / client-lead / net-lead | 决策门槛 hook 强制走「grill-me → openspec → INDEX」，4 lead 全部命中 |
| `ai-art` | art-director / art-2d / art-ui | hook 「初始化美术」关键词命中触发；3 个 agent 已在 prompt 里引用 |
| `deep-research` | producer / gd-lead | 竞品调研 / 玩法参考需要联网研究，其余 agent 调用不到该场景 |

### 被否定的备选

| 备选 | 否定理由 |
|---|---|
| ai-art 全 art-* 都加（含 art-font / art-vfx / art-2d / art-3d / art-anim） | art-font / art-vfx / art-3d / art-anim 走子专家自己的 SKILL（typeset / vfx-realtime / 3d-modeling / animation-systems），不直接出图 |
| deep-research 全 lead 都加 | client-lead / net-lead 决策依据是代码与架构文献，由 codebase-memory MCP + WebSearch 工具覆盖足够 |
| openspec 全 agent 都加 | 只有 lead 发起决策；system / impl agent 不应直接 new change，应通过 lead 协调 |

---

## 验收清单

```bash
# 1. gitnexus 残留检查
grep -ri "gitnexus\|GITNEXUS" .claude/ \
  --exclude-dir=skills/gitnexus  # 此目录应已删
# 期望输出：0

# 2. agent 白名单一致性
grep -A2 "frontmatter\|skills:" .claude/agents/producer.md | grep -E "openspec|deep-research"
# 期望：两行都出现

# 3. SKILL_MATRIX 与 agent prompt 措辞一致
grep "或 find-skills" .claude/agents/*.md
# 期望：0（全部硬墙）

# 4. SKILLS_INDEX 数字
grep "项目原生.*大 SKILL" .claude/skills/SKILLS_INDEX.md
# 期望：「项目原生 6 大 SKILL」
```
