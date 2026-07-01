---
created: 2026-07-01
---

# 任务清单：17-ui-structure-first

> 按顺序执行；每完成一项在 checkbox 打勾。

## Phase 1：核心 SKILL 新建

- [ ] T1. 新建 `.claude/skills/unity-rect-transform/SKILL.md`
  - frontmatter：name / description（60-250 字符，带触发关键词）
  - 概述：为 art-ui 阶段 1 出 layout 与 client-unity 阶段 5 拼 Prefab 提供 UGUI 空间语言词典
  - 子技能列表指向 references/*.md
- [ ] T2. 新建 `.claude/skills/unity-rect-transform/references/anchors.md`
  - anchorMin / anchorMax 9 宫格 + stretch 组合的视觉效果表
  - Anchor Preset 快捷键（alt 键行为）
- [ ] T3. 新建 `.claude/skills/unity-rect-transform/references/pivot.md`
  - pivot 的意义 + 旋转 / 缩放锚点原理
  - pivot 与 anchor 的关系
- [ ] T4. 新建 `.claude/skills/unity-rect-transform/references/sizeDelta.md`
  - sizeDelta 在 fixed vs stretch anchor 下的语义差异
  - 常见陷阱：stretch anchor 下 sizeDelta 等于"偏移量"而非"尺寸"
- [ ] T5. 新建 `.claude/skills/unity-rect-transform/references/anchored-position.md`
  - anchoredPosition 与 localPosition 的区别
  - anchoredPosition 相对 anchor 的坐标系
- [ ] T6. 新建 `.claude/skills/unity-rect-transform/references/preserve-aspect.md`
  - Image.preserveAspect 的适用场景
  - 图源尺寸建议（图源长宽应等比于 target sizeDelta）
- [ ] T7. 新建 `.claude/skills/unity-rect-transform/references/common-pitfalls.md`
  - Canvas Scaler UI Scale Mode（Constant / Scale With Screen Size）选型
  - 父子 anchor 冲突导致的"改父节点位置子节点跟着动"
  - Layout Group 与 RectTransform 的交互
- [ ] T8. 新建 `.claude/skills/unity-rect-transform/references/prefab-layout-template.md`
  - 提供 `prefab-layout.md` 的完整模板骨架（见 design.md 决策 2）
  - 供 art-ui 阶段 1 直接复制填充

## Phase 2：改造存量 SKILL

- [ ] T9. 改 `.claude/skills/ai-art/SKILL.md`
  - 删掉整个「Step 0：UI 类型前置 — 先定表（强制）」章节
  - "美术素材实现流程" 序号重新排（原 Step 1 变 Step 0.5 或直接从 Step 1 开始）
  - "美术素材生成意图" 上下文里对三表的引用也要删（若有）
- [ ] T10. 改 `.claude/skills/ai-art/references/drawing-prompt-UI.md`
  - 删掉「UI 出图前置：先定表（强制）」章节
  - 新增「结构长宽反哺提示词」章节，说明如何从 `prefab-layout.md` 提取画布尺寸 + 组件比例注入到提示词（见 design.md 决策 4）
- [ ] T11. 改 `.claude/skills/ui-asset-splitting/SKILL.md`
  - 新增「状态完整性 checklist」章节（按钮 3 态 / 页签 2 态 / 弹窗按钮组，见 design.md 决策 6）
  - 新增「画布不够加新画布」章节（见 design.md 决策 7）
  - 保留现有 chroma_key #00ff00 流程不动

## Phase 3：更新 agent 白名单与职责

- [ ] T12. 改 `.claude/agents/art-ui.md`
  - frontmatter.skills 加 `unity-rect-transform`（顺序：`game-ui-design, art-direction, unity-rect-transform, ai-art, codex-image-gen`）
  - system prompt 更新"UI 制作职责"：阶段 1 产出 `art/prefab-layout.md`（含 RectTransform 数据），阶段 5 不再出标注稿
  - 删除任何"三表"/"标注稿"字样
- [ ] T13. 改 `.claude/agents/client-unity.md`
  - frontmatter.skills 加 `unity-rect-transform`（放在 `unity-ui` 后面）
  - system prompt 更新 UI 表单工作流：阶段 5 单线（读 layout + 素材 → 拼 Prefab + 写脚本）

## Phase 4：更新索引与总表

- [ ] T14. 改 `.claude/SKILL_MATRIX.md`
  - 表二「art-ui」行核心 SKILL 加 `unity-rect-transform`
  - 表二「client-unity」行核心 SKILL 加 `unity-rect-transform`
  - §三「共享 SKILL」表加一行：`unity-rect-transform | art-ui + client-unity | UGUI 空间语言词典`
- [ ] T15. 改 `.claude/skills/SKILLS_INDEX.md`
  - 2.7 Unity 引擎实现分组加 `unity-rect-transform`
  - 顶部 SKILL 总数 109 → 110
- [ ] T16. 改 `.claude/CLAUDE.md`
  - §六「UI 制作子流程 v2」→ 改为「v3 — 2026-07-01 结构先行版」
  - 6 阶段表全部改写（阶段 1 改产出为 prefab-layout.md、阶段 5 改为单线 client-unity）
  - 强制约束 8 条改写为 8 条（新增"不设简单弹窗豁免"、"任何 UI 都走完整 6 阶段"，删除"阶段 5 必须并行"）
  - Agent 编排速查更新（阶段 1 delegate art-ui 出 layout；阶段 5 单线；不再有 Fan-Out）
  - 顶部 SKILL 总数 109 → 110
  - §十三 索引表若有指向 `.claude/UGUI预制体规范.md` 的链接 → 删除；同时把 `unity-rect-transform` 补上（可选，作为流程配套文档）

## Phase 5：清理遗留

- [ ] T17. 检查 `.claude/UGUI预制体规范.md` 是否真的已删（用户主动删过）
  - Bash: `ls .claude/UGUI预制体规范.md 2>&1`（预期 "No such file"）
- [ ] T18. 全项目 grep `UGUI预制体规范` / `三表` / `复用组件清单` / `组件状态表` / `art-ui 标注稿`
  - 逐处清理残留引用（改指向 `unity-rect-transform` / `prefab-layout.md` 或直接删）

## Phase 6：验证与归档

- [ ] T19. `openspec validate 17-ui-structure-first --strict`（若 CLI 支持）通过
- [ ] T20. 让主对话跑一次干读：手动 Read `CLAUDE.md §六` + `art-ui.md` + `client-unity.md`，确认三处描述一致
- [ ] T21. `openspec archive-change 17-ui-structure-first` 归档
- [ ] T22. 更新 `项目知识库（AI自行维护）/INDEX.md`，追加本 change 的一句话摘要
