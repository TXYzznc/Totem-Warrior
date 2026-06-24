# 项目知识库索引

> AI 自行维护的知识库入口。**任何 lead / system 层 agent 在做决策前必须先查阅本索引**。
>
> 本知识库与 `openspec/changes/` 一站式工作流配套：每次开发都会沉淀新条目。

---

## 一、目录结构

| 目录 | 说明 | 谁来写 |
|---|---|---|
| `outputs/` | AI 工作时输出的临时文件（草稿、中间结论） | AI 自动写 |
| `raw/` | 用户筛选并保留的原始资料（访谈、调研、PRD 草稿） | 用户筛选后归档 |
| `wiki/` | AI 整理后的结构化知识维基（按系统分类） | AI 整理，用户校对 |

## 二、工作流

```
AI 输出 → outputs/ → 用户筛选 → raw/ → 用户指令 → AI 整理到 wiki/ → 更新本 INDEX.md
```

**规则**：

- AI **不要直接修改 `raw/`** 中的文件——那是用户认证后的原始素材。
- `wiki/` 由 AI 维护，但每次提交前回扫一次链接有效性。
- `outputs/` 中的文件是临时产物，定期清理。
- 大型决策结束后（grill-me → openspec → 落地完成），**必须**把决策摘要、关键 trade-off、被否定的备选方案写入 `wiki/`。

---

## 三、Wiki 目录（按系统）

### 3.1 设计层（owner: gd-lead / gd-system）

- _尚无条目_

### 3.2 客户端架构（owner: client-lead）

- **[Tattoo 系统重构（v1.0 → v2.0 框架化）](wiki/Tattoo系统重构.md)** — 2026-06-24 — 把原 Tattoo 业务（21 SO 子类 + Composer + CombatRunner + IMGUI）整体重写为 IGameModule 框架实现。详见 [openspec/changes/01-tattoo-framework-rewrite/](../openspec/changes/01-tattoo-framework-rewrite/)

### 3.3 服务端 / 网络（owner: net-lead）

- _尚无条目（项目当前为单机原型）_

### 3.4 美术（owner: art-director）

- _尚无条目_

### 3.5 工具链 / DevOps（owner: tools-engineer / devops-engineer）

- **[SKILL 路由系统统一（硬墙白名单 + gitnexus 清除）](wiki/SKILL路由统一.md)** — 2026-06-24 — 消除 `.claude/` 下两套 SKILL 路由机制的语义冲突；清除 gitnexus；19 个 agent 措辞统一为硬墙；7 个 agent 加共享 SKILL。详见 [openspec/changes/02-skill-routing-unification/](../openspec/changes/02-skill-routing-unification/)
- **[工作流迁移到 openspec/changes/ 一站式目录](wiki/工作流迁移.md)** — 2026-06-24 — 删除「工作/」整个目录，5 Phase 沉淀到 openspec change：proposal/design/tasks/specs（原生）+ brainstorm.md + CONTRACT.md + art/ + tests/。详见 [openspec/changes/03-workflow-on-openspec/](../openspec/changes/03-workflow-on-openspec/)

### 3.6 已废弃 / 历史决策（owner: producer）

- _尚无条目_

---

## 四、当前活跃的 OpenSpec 变更

| ID | 标题 | 阶段 | 负责 agents |
|---|---|---|---|
| **01-tattoo-framework-rewrite** | Tattoo 业务整体迁移到 IGameModule 框架 | Phase A 进行中（机械动作已完成） | client-lead / client-unity / gd-system / art-ui / qa-engineer |
| **02-skill-routing-unification** | SKILL 路由白名单制硬墙化 + gitnexus 清除 + 共享 SKILL 显式登记 | Phase A 已完成（待 verify-change + archive） | 主对话（tools-engineer 风格） |
| **03-workflow-on-openspec** | 工作流从「工作/」5 Phase 沉淀到 `openspec/changes/<NN>/` 单一目录树；ai-art / archive-change / CLAUDE.md / AGENTS.md 路径迁移 | Phase A 已完成（待 verify-change + archive） | 主对话（tools-engineer 风格） |

详见 [openspec/changes/](../openspec/changes/)。

---

## 五、新建 wiki 条目的规范

每个 wiki 文件头部要包含：

```yaml
---
title: <系统/决策名>
owner: <主负责 agent>
created: YYYY-MM-DD
last_updated: YYYY-MM-DD
status: active | superseded | archived
related_specs:
  - openspec/changes/...
related_skills:
  - <skill-name>
---
```

正文必须含：

1. **背景**：为什么有这个东西
2. **决策**：选了什么 + 为什么
3. **被否定的备选**：≥2 条 + 否定理由
4. **影响范围**：哪些代码 / 哪些 agent / 哪些 skill
5. **过时检查**：何时该 review / 何时该归档

---

## 六、与其他索引的关系

| 索引 | 作用 | 链接 |
|---|---|---|
| `.claude/CLAUDE.md` | AI 行为准则 + 路由 + 工作流主入口 | [→](../.claude/CLAUDE.md) |
| `.claude/SKILL_MATRIX.md` | agent ↔ skill 白名单 | [→](../.claude/SKILL_MATRIX.md) |
| `.claude/skills/SKILLS_INDEX.md` | 124 SKILL 分组索引 | [→](../.claude/skills/SKILLS_INDEX.md) |
| **本文件** | **项目自维护知识库** | — |
| `.claude/AGENTS.md` | 多 Agent 协作 5 模式 | [→](../.claude/AGENTS.md) |
| `openspec/changes/` | 活跃 / 已归档变更（含 art/ + tests/ + brainstorm.md） | [→](../openspec/changes/) |

---

*最后更新：2026-06-24（03-workflow-on-openspec：工作流完全沉淀到 openspec/changes/，「工作/」已删除）*
