# Tasks — 04-ui-planning-first

> 实施进度。✅ = 已交付；🔲 = 未完成。

---

## Phase A — 流程改造

- [ ] `.claude/skills/ai-art/SKILL.md` 美术素材实现流程开头增加「Step 0：UI 类型前置 — 先定表（强制）」段
- [ ] `.claude/skills/ai-art/references/drawing-prompt-UI.md` 在「核心设计理念」后加「UI 出图前置：先定表（强制）」小节，含：
  - [ ] 方法论引述（独立游戏 UI 第一步不是出图）
  - [ ] 三表骨架模板（A 页面清单 / B 复用组件清单 / C 组件状态表）
  - [ ] AI 自动起草行为约束
  - [ ] 用户审阅修订门槛
- [ ] `.claude/skills/ai-art/references/drawing-prompt-generator.md` 工作流程加 UI 分支：
  - [ ] 类型判断后若为 UI，先调用先定表流程
  - [ ] 等 requirements.md 三表存在且用户确认 → 才进入提示词生成

## Phase B — 框架文档同步

- [ ] `.claude/CLAUDE.md` §六「美术素材生成意图」追加一条：「UI 类型素材必须先定表（页面/组件/状态），ai-art 自动起草供用户审阅，详见 drawing-prompt-UI.md」

## Phase C — 知识库与归档准备

- [ ] `项目知识库（AI自行维护）/INDEX.md` §四「当前活跃」追加 04
- [ ] `项目知识库（AI自行维护）/INDEX.md` §3.4 美术追加 wiki 链接
- [ ] 新建 `项目知识库（AI自行维护）/wiki/UI先定表规范.md`

## Phase D — 同步与验收

- [ ] 跑 `python3 tools/sync-agents.py` 同步 `.codex/agents` + `.agents/skills`
- [ ] grep 验收：`先定表` 在 ai-art SKILL + drawing-prompt-UI + CLAUDE.md 都出现
- [ ] grep 验收：CHARACTER/ICON/SCENE/COMMON 四个非 UI 类型 references 文件零改动
