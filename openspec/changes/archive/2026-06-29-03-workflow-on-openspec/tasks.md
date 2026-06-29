# Tasks — 03-workflow-on-openspec

> 实施进度。✅ = 已交付；🔲 = 未完成。

---

## Phase A — 路径硬编码替换

- [x] `.claude/skills/ai-art/SKILL.md` L38-57 路径替换：
  - [x] L38 删 `工作/README.md` 引用（README 已删）
  - [x] L39 `工作/1.美术/<需求名>/` → `openspec/changes/<change-name>/art/`
  - [x] L48-50 `工作/工作区/0.AI绘图输出文件（未处理）/需求名/` → `openspec/changes/<change-name>/art/raw/`（单模块）或 `art/raw/<子模块名>/`（多模块）
  - [x] L57 状态头部字段：`输出目录: art/raw/`、`生成记录: art/raw/生成记录.md`
- [x] `.claude/skills/openspec/references/archive-change.md` 删除 L77-85 + L117 「工作/...（已归档）」后缀逻辑

## Phase B — 框架文档修订

- [x] `.claude/CLAUDE.md` §六 5 Phase 表简化为 3 节点
- [x] `.claude/CLAUDE.md` §六 美术素材生成意图段：路径全部改 `openspec/changes/<name>/art/`
- [x] `.claude/CLAUDE.md` §六 「归档标记规则」段删除（openspec archive 自动）
- [x] `.claude/AGENTS.md` L271 全局契约位置：`3.正在处理的任务/NN.功能名/README.md` → `openspec/changes/<name>/CONTRACT.md`

## Phase C — 知识库与本变更归档准备

- [x] `项目知识库（AI自行维护）/INDEX.md` L5：删「工作/ 5 Phase 工作流配套」
- [x] `项目知识库（AI自行维护）/INDEX.md` L107：删 `工作/README.md` 行
- [x] `项目知识库（AI自行维护）/INDEX.md` §四「当前活跃」追加 03
- [x] `项目知识库（AI自行维护）/INDEX.md` §3.5 追加 wiki 链接
- [x] 新建 `项目知识库（AI自行维护）/wiki/工作流迁移.md`

## Phase D — 验收

- [x] `grep -ri "工作/" .claude/ 项目知识库（AI自行维护）/` 输出 0 行（除本次 change 文档元说明 / wiki 迁移记录）
- [x] `grep "art/requirements.md\|art/raw/" .claude/skills/ai-art/SKILL.md` 命中
- [x] `grep "CONTRACT.md" .claude/AGENTS.md` 命中（AGENTS.md L271）
