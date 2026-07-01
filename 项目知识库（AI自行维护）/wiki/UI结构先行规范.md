---
title: UI 结构先行规范（v3 — 用 prefab-layout.md 替代三表）
owner: art-director
created: 2026-07-01
last_updated: 2026-07-01
status: active
supersedes: wiki/UI先定表规范.md（v2 三表）
related_change: openspec/changes/archive/2026-07-01-17-ui-structure-first/
related_specs:
  - openspec/specs/ui-workflow/
related_skills:
  - unity-rect-transform
  - ai-art
  - codex-image-gen
  - ui-asset-splitting
  - unity-skills
---

# UI 结构先行规范（v3）

> **核心方法论**：独立游戏做 UI，第一步不是出图、也不是"先定表"，而是**先定结构**。一份 `prefab-layout.md`（含 RectTransform 数据）同时喂养效果图长宽反哺（阶段 2）、素材拆分节点树（阶段 4）、Prefab 层级搭建（阶段 5）——**同一份信息只维护一处**。

## 一、为什么从 v2 三表升级到 v3 layout

v2「三表」（页面清单 / 复用组件清单 / 组件状态表）实际用起来暴露了 3 个问题：

| 问题 | 现象 | v3 修法 |
|---|---|---|
| **checklist 化** | 三表容易变成填格子任务，写完就束之高阁，后续阶段不复读 | layout 是活文档，阶段 2/4/5 都强制回读 |
| **信息不能落地** | 三表只写"有按钮 / 有标签页 / 有弹窗"，没说锚点在哪、多大、占几屏 | layout 每节点写死 anchor / pivot / sizeDelta / anchoredPosition |
| **效果图长宽全靠猜** | v2 阶段 2 art-ui 写提示词时没有画布尺寸依据，出图与 Prefab 结构对不上 | layout 直接反哺"结构约束"段进 prompts.md |

## 二、决策摘要

| 维度 | 选择 |
|---|---|
| **结构文档形式** | 单文件 `prefab-layout.md`（Markdown 节点树 + RectTransform 四元组），**不再是三张表** |
| **产出主导 agent** | `art-ui` + `unity-rect-transform` SKILL；v2 曾试的"producer / gd-system 起手"改回美术侧 |
| **阶段数** | 从 v2 的 5 阶段升到 v3 的 6 阶段（新增独立的"结构设计"阶段 1） |
| **阶段 5 编排** | v2 Fan-Out（art-ui 标注稿 ∥ client-unity Prefab）→ v3 **单线** client-unity；标注稿作为冗余中间层已删 |
| **RectTransform 数据传递** | v2 靠效果图估算 → v3 layout 阶段就写死，贯穿到 Prefab |
| **豁免规则** | v2 未明确 → v3 明确**无豁免**：简单弹窗也走完整 6 阶段 |
| **回溯策略** | v2 已归档 UI change 不改造；新 change 强制 v3 |

## 三、6 阶段流程速查

```
1.结构设计           2.效果图设计         3.效果图生成    4.素材拆分            5.拼装实现         6.联调微调
(art-ui +           (art-ui 从 layout   (codex-        (ui-asset-splitting,  (client-unity      (client-unity
 unity-rect-        反哺画布/占比→       image-gen,     多张 mockup           单线：unity-       + 用户对比
 transform SKILL,   写 prompts.md;      3 轮重试上限)  Fan-Out 并行,         skills MCP 按      效果图迭代,
 产出 prefab-       状态每态独立)                      每态独立生素材)       layout 建 + 贴     偏差回写 layout)
 layout.md)                                                                 素材 + UIForm)
```

详见 [openspec/specs/ui-workflow/spec.md](../../openspec/specs/ui-workflow/spec.md)（capability=ui-workflow，7 条 Requirements）。

## 四、prefab-layout.md 模板骨架

```markdown
# <ChangeName> UI Prefab Layout

## 全局约定
- Canvas Scaler: Scale With Screen Size, ReferenceResolution=(1920, 1080), Match=0.5
- 通用按钮尺寸: 200×80 sizeDelta
- 跨页复用组件: MainButton / CloseButton / Divider ...

## <PageName>Form
### 节点树
- Canvas (Screen Space - Overlay)
  - Background (Image, 全屏铺)
    - anchor: (0, 0) - (1, 1), pivot: (0.5, 0.5), sizeDelta: (0, 0), anchoredPosition: (0, 0)
  - Title (Text)
    - anchor: (0.5, 1) - (0.5, 1), pivot: (0.5, 1), sizeDelta: (800, 120), anchoredPosition: (0, -60)
  - ButtonGroup (VerticalLayoutGroup)
    - anchor: (0.5, 0.5) - (0.5, 0.5), pivot: (0.5, 0.5), sizeDelta: (400, 400), anchoredPosition: (0, 0)
    - ConfirmButton (Button)
      - anchor: (0.5, 1) - (0.5, 1), pivot: (0.5, 1), sizeDelta: (400, 80), anchoredPosition: (0, 0)
      - states: [normal, pressed, disabled]
    - CancelButton (Button)
      - anchor: (0.5, 1) - (0.5, 1), pivot: (0.5, 1), sizeDelta: (400, 80), anchoredPosition: (0, -100)
      - states: [normal, pressed, disabled]

### 状态清单
- ConfirmButton: normal / pressed / disabled
- CancelButton: normal / pressed / disabled
```

完整词典见 [.claude/skills/unity-rect-transform/references/](../../.claude/skills/unity-rect-transform/references/)。

## 五、被否定的备选

| 备选 | 否定理由 |
|---|---|
| 继续用三表 + 增加"数量硬编码"约束 | 治标不治本，checklist 化问题依旧；RectTransform 数据还是缺 |
| 用图形化工具（Figma / Framer）替代 markdown layout | AI 无法直接读 Figma 数据；markdown 让 agent 与用户都能看/改 |
| 阶段 1 仍由 producer / gd-system 主导 | 结构设计属于美术侧职责（涉及 anchor / pivot / 画面比例），美术 agent 更合适 |
| 阶段 5 保留 art-ui 标注稿并行 | v2 已证明标注稿是冗余中间层——layout 已含 RectTransform 数据，标注稿再抄一遍纯浪费 |
| 简单弹窗走"轻量流程"豁免完整 6 阶段 | 破窗效应；豁免通道一开，后续每个 UI 都能找理由跳阶段 |
| 保留 v2 spec (ui-planning) 与 v3 spec (ui-workflow) 双活 | 语义冲突；v3 归档时把 ui-planning 的 2 条 Requirement 标 REMOVED，ui-workflow 成为唯一 SoT |

## 六、影响范围

| 文件 | 改动 |
|---|---|
| `.claude/CLAUDE.md` §六 | UI 制作子流程整段 v2→v3 重写（6 阶段 + 10 条强制约束 + Agent 编排速查） |
| `.claude/conventions.md` §八 | 5 阶段图 → 6 阶段图；前置条件表更新；Prefab 创建路径改为按 layout 建 |
| `.claude/tests/smoke-ui-workflow.md` | v1（5 阶段）→ v2（6 阶段）；防越界从 6 条扩到 10 条 |
| `.claude/agents/art-ui.md` | 目标改为「阶段 1 出 layout + 阶段 2 反哺 prompts」；"标注稿 v3 已取消"；白名单加 `unity-rect-transform` |
| `.claude/agents/client-unity.md` | UI 工作流 item 8 重写为 v3；白名单加 `unity-skills` + `unity-rect-transform` |
| `.claude/SKILL_MATRIX.md` | art-ui 与 client-unity 白名单更新；共享 SKILL 表新增 `unity-rect-transform` / `unity-skills` |
| `.claude/skills/SKILLS_INDEX.md` | §2.7 Unity 引擎实现新增 `unity-rect-transform` 行；标题计数 109→110 |
| `.claude/skills/unity-rect-transform/` | **新建 SKILL**：SKILL.md + references（4 篇：word-dictionary / examples / prefab-layout-template / common-pitfalls） |
| `.claude/skills/ai-art/SKILL.md` | Step 0 三表强制删除；改为「UI 前置：layout 缺失即阻塞」 |
| `.claude/skills/ai-art/references/drawing-prompt-UI.md` | 「UI 出图前置：先定表（强制）」段整段删除，替换为「UI 出图前置：结构先行（强制）」+ 结构长宽反哺格式 |
| `.claude/skills/ai-art/references/drawing-prompt-generator.md` | 工作流程 Step 4 从「检查三表」改为「检查 prefab-layout.md，缺失即阻塞」 |
| `.claude/skills/ui-asset-splitting/SKILL.md` | §二前置条件 + §3.1 分析 都改为「从 layout 读节点树」+ 加「状态每态独立」+ 「画布不够拆多张」 |
| `openspec/specs/ui-planning/` | v2 的 2 条 Requirement 标 REMOVED（三表规范废弃） |
| `openspec/specs/ui-workflow/` | **新建 capability**：7 条 Requirement 覆盖 6 阶段 + 每阶段的 GIVEN/WHEN/THEN Scenario |

## 七、相关 openspec 变更

- [04-ui-planning-first](../../openspec/changes/archive/2026-06-29-04-ui-planning-first/) — v2 三表规范（**已被本变更取代**）
- [17-ui-structure-first](../../openspec/changes/archive/2026-07-01-17-ui-structure-first/) — 本变更（v3 结构先行）
  - [proposal.md](../../openspec/changes/archive/2026-07-01-17-ui-structure-first/proposal.md)
  - [design.md](../../openspec/changes/archive/2026-07-01-17-ui-structure-first/design.md)
  - [tasks.md](../../openspec/changes/archive/2026-07-01-17-ui-structure-first/tasks.md)
  - [specs/ui-workflow/spec.md](../../openspec/changes/archive/2026-07-01-17-ui-structure-first/specs/ui-workflow/spec.md)
  - [specs/ui-planning/spec.md](../../openspec/changes/archive/2026-07-01-17-ui-structure-first/specs/ui-planning/spec.md)（REMOVED）

## 八、过时检查

- 若发现某个 change 走完 v3 6 阶段流程后遗留问题（如 layout 与 Prefab 不同步、阶段 5 单线不够快），回来更新本 wiki 与 openspec spec
- 若 unity-rect-transform SKILL 词典迭代（新增 preserveAspect / Layout Group 陷阱等），同步更新本 wiki §四 模板骨架
- 若阶段 5 单线证明确实是瓶颈，重新评估是否引入并行子任务（例如 client-unity 内部 fan-out 多个 Prefab）
