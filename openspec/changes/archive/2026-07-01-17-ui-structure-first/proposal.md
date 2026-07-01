---
created: 2026-07-01
status: in-progress
depends-on: none
---

# 提案：UI 制作流程重构 — 结构先行 (Structure-First)

## 一句话目标
把 CLAUDE.md §六「UI 制作子流程 v2」从「效果图先行 + 事后标注稿」重构为「**结构先行**（art-ui 先出 prefab-layout.md 含 RectTransform 数据 → 再画效果图 → 拆分素材 → client-unity 单线拼 Prefab）」，让 UI 从设计到实现有一份**贯穿全流程的空间语言**。

## 为什么
v2 流程有三个真实痛点：

1. **三表（页面清单 / 复用组件清单 / 组件状态表）在实际使用中沦为形式**：art-ui 起草后用户走过场审批，进入阶段 2 之后再也没人回头查表；表格粒度停留在"要哪几页 / 有哪几个按钮"，不解决"按钮放哪 / 多大 / 怎么锚定"这一 UGUI 核心问题。
2. **阶段 5 的"标注稿"是事后补票**：art-ui 在效果图已出 + Prefab 已建之后再画标注稿，Prefab 已经按 client-unity 的直觉搭好，标注稿沦为文档摆设，无法反向指导实现。
3. **RectTransform 是 UGUI 的核心 DNA，但流程里完全没有它的位置**：anchor / pivot / sizeDelta / anchoredPosition 决定"缩放行为、多分辨率适配、层级参照"，art-ui 不出结构 = client-unity 只能靠效果图像素点估算，产出的 Prefab 与效果图对不上。

结构先行把这 3 个痛点一次性解决：**空间结构（RectTransform 数据）是效果图的输入**，而不是效果图的输出。

## 与用户的 5 条共识（grill-me 退出快照）

| 维度 | 决议 |
|---|---|
| 目标 | UI 从「效果图先行 + 事后标注」→「结构先行 + 单线拼装」，让 RectTransform 数据在阶段 1 就产出并贯穿全流程 |
| 关键决策 A/B | (a) 三表**全部删除**（不保留任一表）；(b) art-ui 阶段 1 产出**单文件** `prefab-layout.md`（含节点树 + 每节点 RectTransform 数据 + 关键交互决策）；(c) 阶段 5 **取消 art-ui 标注稿**，改为 client-unity 单线（读 layout + 素材 → 拼 Prefab + 写脚本）；(d) 效果图提示词加入**结构长宽反哺**（把 layout 里的画布尺寸/组件比例注入到 prompt）；(e) 素材拆分保留**纯绿色 #00ff00** 绿幕（不改现有 chroma_key 工具链） |
| 边界 | (a) **不回溯**：已归档 change 里的 UI 不重新按新流程重做；(b) 简单弹窗（如"确认/取消"两按钮）**也走完整流程**（不开豁免口子，避免破窗）；(c) `.claude/UGUI预制体规范.md` 已由用户主动删除（原为 figma 设计，本项目不用 figma）；(d) 不新增第二种绿幕颜色 / 白底方案 |
| 验收 | (a) `.claude/CLAUDE.md §六` 更新为新 6 阶段表 + 强制约束 + Agent 编排速查；(b) `unity-rect-transform` SKILL 新建并同步进 SKILL_MATRIX + SKILLS_INDEX；(c) `art-ui` / `client-unity` 白名单都能拿到 `unity-rect-transform`；(d) `ai-art` SKILL / drawing-prompt-UI.md 删掉 Step 0 三表章节；(e) `ui-asset-splitting` SKILL 加"状态完整性 checklist + 画布不够加新画布"；(f) 生成的 openspec change 通过 `openspec validate` |
| 约束 | 只改 `.claude/` + `openspec/17-ui-structure-first/` 内文件；**不动** `Assets/Scripts/` 与 `Assets/Editor/UISpriteImportProcessor.cs`；本次为文档流程重构，不产生 C# 代码变更；本次为**规范升级**，无 tests/ 子目录（qa-engineer 不介入） |

## 不做什么
- 不回溯已归档的 UI change（历史 CharacterSelect / SelfTattoo / MainMenu 等按 v2 流程做的 Prefab 保持原状，只在下一次动它时按新流程重做）
- 不为「简单弹窗」开豁免通道（一旦豁免就会破窗，任何弹窗都走完整 6 阶段）
- 不改 `Assets/Editor/UISpriteImportProcessor.cs` 的导入参数逻辑（仍自动处理 Texture Type / Sprite Mode）
- 不改 `ui-asset-splitting` 的绿幕颜色（保持 #00ff00 + chroma_key.py 现状）
- 不改 `codex-image-gen` 的 L0/L1/L2 三档批量策略
- 不创建 `.claude/UGUI预制体规范.md`（已由用户主动删除，本项目不采用 figma 工作流）
- 不新增 C# 代码 / 不写测试代码（本次为纯文档流程重构）

## 验收
- [ ] `.claude/CLAUDE.md §六 UI 制作子流程` 已改为 v3（结构先行版），6 阶段表 + 强制约束 + Agent 编排速查 3 处同步更新
- [ ] 新建 `.claude/skills/unity-rect-transform/SKILL.md` + `references/*.md`，涵盖 anchor / pivot / sizeDelta / anchoredPosition + Preserve Aspect + 常见坑
- [ ] `.claude/skills/ai-art/SKILL.md` 已删掉 "Step 0：UI 类型前置 — 先定表（强制）" 章节
- [ ] `.claude/skills/ai-art/references/drawing-prompt-UI.md` 已删掉 "UI 出图前置：先定表（强制）" 章节，改为 "结构长宽反哺提示词" 章节
- [ ] `.claude/skills/ui-asset-splitting/SKILL.md` 已加 "状态完整性 checklist（按钮三态 / 页签双态 / 弹窗按钮组）" + "画布不够加新画布" 两条规则
- [ ] `.claude/agents/art-ui.md` frontmatter.skills 已加 `unity-rect-transform`；system prompt 已更新为"阶段 1 出 prefab-layout.md、不再出标注稿"
- [ ] `.claude/agents/client-unity.md` frontmatter.skills 已加 `unity-rect-transform`；system prompt 已更新 UI 表单工作流为"读 layout + 素材 → 拼 Prefab + 写脚本"单线
- [ ] `.claude/SKILL_MATRIX.md` 已把 `unity-rect-transform` 加入 art-ui + client-unity 核心 SKILL，且在「共享 SKILL」表登记
- [ ] `.claude/skills/SKILLS_INDEX.md` 2.7 Unity 引擎实现分组已加 `unity-rect-transform`，SKILL 总数 109 → 110
- [ ] `openspec validate 17-ui-structure-first --strict` 通过
- [ ] 归档时 `项目知识库（AI自行维护）/INDEX.md` 已更新（追加本 change 摘要）
