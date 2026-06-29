# Proposal — 03-workflow-on-openspec

> **范围**：把原 `工作/` 5 Phase 工作流的所有职责沉淀到 `openspec/changes/<change-name>/` 单一目录树，消除「双写」与「死链」。
> **决策日期**：2026-06-24
> **前置变更**：02-skill-routing-unification（确立了 agent + openspec 框架）
> **决策方式**：02-skill-routing-unification 后用户主动质疑「工作/ 无法在现有 agent 框架下进行」→ 明确删除整个 `工作/` 目录 → 讨论复现方案 → 采纳「提案 A：5 Phase 全部沉淀到 openspec change 目录」
> **预计阶段**：1 阶段（文档与 SKILL 路径替换，零代码）

---

## Why

02-skill-routing-unification 落地后，agent 框架已自动走 `openspec new change` 路径。但 `工作/` 目录仍然按"人工 mkdir + 人工归档"的 2026-06-14 旧设计运转，造成：

| 现象 | 影响 |
|---|---|
| agent 不写 `工作/2.需求列表/`、`工作/3.正在处理的任务/`，全部写 `openspec/changes/` | `工作/2.需求列表/` 和 `工作/3.正在处理的任务/` 实际成为僵尸目录 |
| ai-art SKILL [L38-49](../../../.claude/skills/ai-art/SKILL.md) 硬编码 `工作/1.美术/...` 和 `工作/工作区/0.AI绘图输出文件（未处理）/...` | 美术资源孤立在 `工作/` 下，与功能 change 分离，归档时不同步 |
| `openspec archive-change` [详文档 L77-85](../../../.claude/skills/openspec/references/archive-change.md) 要给 `工作/...` 加「（已归档）」后缀 | openspec CLI 原生归档机制与人工后缀机制双写 |
| `工作/4.已归档/NN_v1/v2/v3` 版本机制 vs openspec「change 中心」心智模型 | 命名规则冲突 |

用户已在本次会话物理删除整个 `工作/` 目录（含 STG游戏 / 背包示例 / 配置表 3 个进行中功能，用户确认为「老业务，不用管」）。本次变更负责修复因删除产生的 9 处死链，并定义新工作流。

## 目标（DoD）

- ✅ ai-art SKILL 路径从 `工作/1.美术/...` 改为 `openspec/changes/<name>/art/...`
- ✅ openspec archive-change 文档删除「工作/...（已归档）」后缀逻辑
- ✅ CLAUDE.md §六 5 Phase 表简化为 3 节点
- ✅ AGENTS.md L271 全局契约位置改为 `openspec/changes/<name>/CONTRACT.md`
- ✅ 项目知识库 INDEX.md 删除「工作/ 5 Phase 工作流配套」引用
- ✅ 验收 grep：`.claude/`、`项目知识库（AI自行维护）/` 下出现 `工作/` 路径次数 = 0
- ✅ 新工作流文档（openspec/changes/ 一站式目录树）落到 wiki/工作流迁移.md

## 非目标

- ❌ 不动 116 个原子 SKILL + 7 个 wrapper SKILL（02-skill-routing-unification 已完成）
- ❌ 不动 agent frontmatter（02 已完成）
- ❌ 不引入 git LFS（早期项目阶段直接 git 即可；后期再迁）
- ❌ 不动 ImageCut_Tool 集成（项目当前未配置该工具，本次不预设）
- ❌ 不恢复已删除的「工作/」目录

## 新工作流（一站式 openspec change 目录树）

```
openspec/changes/<NN-change-name>/
├─ .openspec.yaml
├─ proposal.md              ← Phase 2: 需求拆解（openspec 原生）
├─ design.md                ← Phase 2/3: 设计方案（openspec 原生）
├─ tasks.md                 ← Phase 3: 任务清单 + 进度（openspec 原生）
├─ specs/<能力>/spec.md     ← openspec 原生（规范级）
├─ brainstorm.md            ← Phase 1: 策划讨论沉淀（新增约定）
├─ CONTRACT.md              ← 多模块全局契约（仅多模块时创建）
├─ art/                     ← Phase 2 美术（ai-art SKILL 落盘点）
│  ├─ requirements.md       ←   美术需求分析
│  ├─ prompts.md            ←   提示词
│  └─ raw/                  ←   AI 出图源图 + 生成记录.md
└─ tests/                   ← Phase 4 测试（qa-engineer 落盘点）
   ├─ plan.md
   ├─ results.md
   └─ bugs.md
```

**归档**：`openspec archive-change <name>` 把整个目录搬到 `openspec/changes/archive/<name>/`。`art/raw/` 大文件、`tests/` 文档跟着走，**无需**版本号 `_v1/_v2` 或「（已归档）」后缀。

**多功能迭代**：同一功能再次迭代 = 创建新 change（如 `04-backpack-v2`），openspec spec-driven 心智模型「change 中心」承载。

## What Changes

| 文件 | 改动 |
|---|---|
| `.claude/skills/ai-art/SKILL.md` | L38-57 路径替换：`工作/1.美术/<需求>/` → `openspec/changes/<name>/art/`；`工作/工作区/0.AI绘图输出文件（未处理）/<需求名>/` → `openspec/changes/<name>/art/raw/` |
| `.claude/skills/openspec/references/archive-change.md` | L77-85 + L117 删除「工作/...（已归档）」后缀逻辑 |
| `.claude/CLAUDE.md` §六 | 5 Phase 表简化为 3 节点（策划讨论 → openspec → ai-art）；删归档标记规则段 |
| `.claude/AGENTS.md` L271 | 「`3.正在处理的任务/NN.功能名/README.md`」→「`openspec/changes/<name>/CONTRACT.md`」 |
| `项目知识库（AI自行维护）/INDEX.md` | L5 + L107 删除 `工作/` 引用；§四追加 03 |
| `项目知识库（AI自行维护）/wiki/工作流迁移.md` | **新建**：决策摘要 + 被否定备选 |
| `openspec/changes/03-workflow-on-openspec/` | **新建**（本目录） |

## 风险与回滚

| 风险 | 缓解 |
|---|---|
| ai-art SKILL 路径替换后，主对话或 art-director 调用时路径错误 | 替换后用 sample change（如本 03）验证 ai-art SKILL 可路由 |
| 用户记忆中的「工作/」工作流惯性 | wiki/工作流迁移.md 写清楚迁移对照表 |
| 已归档功能丢失（已删除）的回溯 | git 历史保留 `工作/` 删除前快照；如需查询用 `git log -- 工作/` |

回滚：`git revert <本次 commit>` 恢复路径硬编码 + `git checkout HEAD~1 -- 工作/` 恢复目录。

## 引用

- [02-skill-routing-unification](../02-skill-routing-unification/) — 前置变更
- [.claude/CLAUDE.md](../../../.claude/CLAUDE.md)
- [.claude/skills/ai-art/SKILL.md](../../../.claude/skills/ai-art/SKILL.md)
- 涉及 agents：art-director / art-2d / art-ui（ai-art SKILL 调用者）+ qa-engineer（tests/ 落盘）
