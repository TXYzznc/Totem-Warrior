# SKILL_MATRIX — Agent ↔ SKILL 白名单映射表

> 每个 agent 的 `frontmatter.skills:` 必须严格匹配本表的「核心」列。`escalate_to: main` 时主对话可调用所有 SKILL。
>
> **更新规则**：增/减 skill 时同步更新本表与对应 agent frontmatter。本表是 source of truth。

---

## 一、Tier 与职责

| Tier | 含义 | 决策权 |
|---|---|---|
| **lead** | 决策层（producer / gd-lead / art-director / client-lead / net-lead） | 出策略、定方向、定门槛；不直接产出代码/资源 |
| **system** | 系统层（gd-system / level-designer / net-db） | 把 lead 决策落到具体规格（公式、关卡、Schema） |
| **impl** | 实现层（client-unity / client-ta / net-backend / art-*  / qa-engineer / devops-engineer / tools-engineer） | 把规格落到可运行的代码/资源/流水线 |

---

## 二、Agent × SKILL 白名单

| Agent | Tier | 核心 SKILL（frontmatter.skills） | 推荐 SKILL（按需查阅） |
|---|---|---|---|
| **producer** | lead | `project-management`, `task-estimation`, `risk-assessment`, `milestone-tracker`, `grill-me`, `openspec`, `deep-research` | `competitive-analysis`, `sprint-retrospective`, `grill-with-docs`, `find-skills`, `xlsx` |
| **gd-lead** | lead | `game-design-core`, `game-design-theory`, `brainstorm`, `design-system`, `grill-me`, `openspec`, `deep-research` | `review-all-gdds`, `propagate-design-change`, `game-monetization`, `player-onboarding` |
| **gd-system** | system | `balance-check`, `combat-balancer`, `progression-systems`, `difficulty-curve`, `quest-mission-design` | `achievement-design`, `puzzle-design`, `casino-math-balancer`, `game-monetization`, `xlsx` |
| **level-designer** | system | `level-design`, `player-guidance`, `difficulty-curve` | `puzzle-design`, `playtest-digital` |
| **art-director** | lead | `art-direction`, `game-art`, `grill-me`, `ai-art`, `codex-image-gen` | `gpt-image-2-style-library`, `find-skills` |
| **art-ui** | impl | `game-ui-design`, `art-direction`, `ai-art`, `codex-image-gen` | `gpt-image-2-style-library`, `unity-ui` |
| **art-font** | impl | `typeset`, `font-pairing-suggester`, `font-selection-cjk`, `font-subsetting`, `pixel-font-rendering` | — |
| **art-vfx** | impl | `vfx-realtime`, `shader-effects` | `agency-technical-artist`, `unity-lighting-vfx` |
| **art-2d** | impl | `character-sprite`, `hytale-texture-artist`, `gpt-image-2-style-library`, `ai-art`, `codex-image-gen` | `art-direction`, `pixel-font-rendering` |
| **art-3d** | impl | `3d-modeling`, `texture-art`, `blender-mcp` | `agency-technical-artist`, `rigging` |
| **art-anim** | impl | `animation-systems`, `rigging` | `unity-animation`, `blender-mcp` |
| **client-lead** | lead | `unity-foundations`, `unity-architecture-di`, `unity-async-patterns`, `grill-me`, `openspec` | `unity-ecs-patterns`, `addressables-hotfix`, `state-machine` |
| **client-unity** | impl | `unity-foundations`, `unity-ui`, `unity-input-correctness`, `save-serialization`, `state-machine`, `physics-collision`, `localization-i18n` | `unity-animation`, `addressables-hotfix`, `uloop-execute-dynamic-code`, `uloop-run-tests` |
| **client-ta** | impl | `unity-shaders-rendering`, `unity-lighting-vfx`, `shader-effects`, `agency-unity-shader-graph-artist` | `vfx-realtime`, `agency-technical-artist` |
| **net-lead** | lead | `arch-api`, `game-networking`, `multiplayer-game`, `grill-me`, `openspec` | `algo-rank-trueskill`, `atomic-matchmaking` |
| **net-backend** | impl | `arch-api`, `jwt-auth`, `oauth-implementation`, `backend-testing` | `redis-specialist`, `kafka-development`, `opentelemetry`, `prometheus` |
| **net-db** | system | `database-schema-design`, `redis-best-practices` | `redis-specialist` |
| **qa-engineer** | impl | `testing-strategies`, `backend-testing`, `crash-analytics`, `playtest-digital`, `k6` | `mobile-device-testing`, `uloop-run-tests`, `ab-testing` |
| **devops-engineer** | impl | `devops-deployment`, `github-actions-docs`, `mobile-cicd`, `secrets-management`, `deploy-checklist`, `feature-flags` | `setup-fastlane`, `steam-deploy`, `asc-submission-health`, `cdn-setup`, `opentelemetry`, `prometheus`, `semver` |
| **tools-engineer** | impl | `unity-editor-scripting`, `unity-skills`, `uloop-execute-dynamic-code`, `skill-creator`, `find-skills` | `moai-docs-generation`, `agent-browser` |

---

## 三、跨 tier 协作规则

- **lead 不直接出代码/资源**；产出 design.md / spec.md / proposal，交 system/impl 落地
- **system 不跨职能**；遇到跨职能问题交回对应 lead 或主 agent
- **impl 只在白名单 SKILL 内工作**；越界即触发 escalate_to: main

### 共享 SKILL（同一 SKILL 出现在多个 agent 白名单中）

| SKILL | 共享于 | 说明 |
|---|---|---|
| `grill-me` | 5 lead | 决策门槛必用 |
| `openspec` | 4 lead（producer / gd-lead / client-lead / net-lead） | 大型决策落 spec 的强制门槛 |
| `ai-art` | 3 art-*（art-director / art-2d / art-ui） | 美术出图前置：提示词与需求沉淀；hook 自动唤起 |
| `codex-image-gen` | 3 art-*（art-director / art-2d / art-ui） | 美术出图后置：调 codex exec 实际生图；出图关键词 hook 自动唤起 |
| `deep-research` | 2 lead（producer / gd-lead） | 竞品/玩法联网研究 |

> 共享 SKILL 不需要独占归属。代码索引/架构理解类需求统一走 `codebase-memory` MCP（详见 [CLAUDE.md §八](./CLAUDE.md)），不进 SKILL 白名单。

---

## 四、escalate_to: main 兜底触发条件（所有 agent 通用）

每个 agent 出现以下情形之一时，**立即停止本 agent 任务并交回主对话**：

1. **白名单外 SKILL**（硬墙）：需要调用 frontmatter.skills 之外的 SKILL → 立即 escalate_to: main，由主对话决定是否调用 `find-skills` 后再委派。**agent 不得自行调用 find-skills 绕开白名单**。
2. **跨职能决策**：任务涉及多个 agent 领域，需主对话协调
3. **MCP / 外部权限不足**：缺凭据、缺 MCP 工具、缺文件系统权限
4. **职责边界外**：任务实质不属于本 agent 职位
5. **多轮收敛失败**：3 轮内无法给出可行方案或反复回退
6. **意图模糊**：用户原始 prompt 含糊，需要二次澄清才能继续
7. **决策门槛触发**：检测到大型设计/架构/重构关键词时（应先走 `grill-me` + `openspec new change`，由主对话发起）

主对话因 tier 最高、SKILL 全开放，可作为最终兜底执行者。

---

## 五、强制门槛规则

任何 **lead 层 agent** 在以下场景**不得直接给方案**：

- 关键词 hit：`设计 / 架构 / 重构 / 大改 / 重写 / GDD / PRD / 系统 / 范式 / 方案 / 思路`
- 必须先调用 `grill-me`（或 `grill-with-docs`），输出至少 3 轮关键反问
- 决策确定后通过 `openspec new change "NN-功能名"` 落地
- 同步更新 `项目知识库（AI自行维护）/INDEX.md`

未走以上三步给出的方案，视为违规——主对话有义务驳回。

---

*最后更新：2026-06-24（02 + 03 follow-up：收录 codex-image-gen 为 3 art-* 共享 SKILL；硬墙白名单 + 共享 SKILL 显式登记 + gitnexus 清除）*
