---
created: 2026-07-01
---

# 设计说明：UI 结构先行流程 (v3)

> 本文件承载 12 条关键决策的详细依据和 trade-off，为 tasks.md / spec.md 提供推理链。

## 决策 1：删除三表（不保留任一表）

### v2 现状
`ai-art/SKILL.md` Step 0 要求 UI 类型 change 必须先在 `art/requirements.md` 里写完三张表：

- **表 A 页面清单**：页面 + 优先级 + 备注
- **表 B 复用组件清单**：组件类型 + 目标数量 + 用途
- **表 C 组件状态表**：组件 + 必备状态 + 备注

### 问题
1. **三表回答的是"要什么"，不是"放哪 / 多大 / 怎么锚定"**：即使三表填满，art-ui 进入阶段 2 还是要凭空想构图，client-unity 进入阶段 5 还是要凭效果图像素点估算 RectTransform。表格与最终 Prefab 之间隔着 3 个阶段，无法反向指导。
2. **形式主义**：过去 3 个 UI change（CharacterSelect / SelfTattoo / MainMenu）的三表填完就再没被回看过；review 时也只看效果图，不核对三表。
3. **粒度错配**：表 B「复用组件目标数量」在 3 页规模的功能里毫无预测价值（AI 只能猜 2-3 个），到了 10+ 页规模又根本猜不准。

### 决策：全删
新流程用 `prefab-layout.md` 一份文档替代三表。layout 的节点树本身就是"页面清单"，节点上的组件类型就是"组件清单"，节点的 `states:` 字段就是"状态表" —— 三表信息被自然吸收进结构化 layout 里，且**每一条都直接对应到未来 Prefab 里的一个节点**，不再是抽象罗列。

### Trade-off
- ✅ 消除形式主义，layout 是活文档
- ✅ 三表内容天然被结构化 layout 吸收
- ⚠️ layout 文档比三表长一些（预计单页 UI 30-80 行 Markdown），但换来了"从设计到实现的空间语言贯通"

## 决策 2：art-ui 阶段 1 产出**单文件** `prefab-layout.md`

### 决策
每个 openspec change 的 UI 需求下，art-ui 在阶段 1 结束时必须产出：

```
openspec/changes/<NN-name>/art/prefab-layout.md
```

**单文件**承载所有 UI 页面的结构（不按页拆多个文件）。原因：跨页复用的组件（如通用按钮尺寸、标题条 anchor 规范）在单文件里可以自然引用，多文件拆分会导致"复用组件到底住哪一个文件"的归属问题。

### 内容骨架

```markdown
# <ChangeName> UI 结构文档

## 全局约定
- 画布基准分辨率：1920×1080
- 通用按钮尺寸：320×80
- 标题条 anchor 规则：top-stretch, height 96

## 页面 1：MainMenuForm

节点树：
- MainMenuForm (RectTransform: stretch-stretch, sizeDelta=0,0)
  - Background (RectTransform: stretch-stretch)
    - components: [Image: preserveAspect=true, source=BG_MainMenu.png (1920×1080)]
  - Title (RectTransform: top-center, anchoredPosition=0,-120, sizeDelta=800,120)
    - components: [Text: 主菜单]
  - ButtonGroup (RectTransform: middle-center, sizeDelta=400,320)
    - StartBtn (anchoredPosition=0,120, sizeDelta=320,80)
      - states: [normal, pressed, disabled]
    - SettingsBtn (anchoredPosition=0,0, sizeDelta=320,80)
    - ExitBtn (anchoredPosition=0,-120, sizeDelta=320,80)

关键决策：
- Background 走 Preserve Aspect，避免多分辨率下拉伸
- ButtonGroup 用 middle-center 而非 top-center，让菜单在竖屏适配时自动居中
```

### 决策要点
- **只到关键决策**：不要求 art-ui 罗列所有节点的每一个字段（那是 client-unity 阶段 5 的事）。layout 只锁"父节点树 + 每节点的 anchor/pivot/sizeDelta/anchoredPosition + 组件列表 + 状态清单 + 显著偏离默认的决策"
- **不含具体贴图内容**：layout 只声明"这里有一张 Image、尺寸是 1920×1080、走 preserveAspect"，不描述贴图画什么（那是效果图阶段的事）

## 决策 3：阶段 5 取消 art-ui 标注稿，改单线 client-unity

### v2 现状
阶段 5 fan-out：
- Agent A：art-ui 出标注稿
- Agent B：client-unity 建 Prefab + 写脚本

### 问题
- 标注稿是"事后画的效果图注解"，此时 Prefab 已经在建 → 标注稿沦为文档摆设
- 双 Agent 需要 WhenAll 汇合，浪费编排开销
- 标注稿信息与 layout 信息大量重叠

### 决策：单线 client-unity
新阶段 5：
- **单 Agent**：client-unity
- **输入**：`art/prefab-layout.md`（阶段 1 产出）+ `Assets/Resources/Sprite/UI/<PageName>/`（阶段 4 产出的拆分素材）+ `art/mockups/<PageName>.png`（阶段 3 效果图，用于视觉核对）
- **输出**：`Assets/Resources/Prefab/UI/<PageName>.prefab` + `Assets/Scripts/Modules/UI/<PageName>Form.cs`

### Trade-off
- ✅ 编排简化：Fan-Out 5 阶段 → 单线，删掉 WhenAll
- ✅ 消除标注稿摆设
- ⚠️ art-ui 在阶段 5 完全退场，联调（阶段 6）由 client-unity + 用户执行；若偏差严重需要 art-ui 介入，走 escalate_to: main 由主对话协调

## 决策 4：效果图提示词加入"结构长宽反哺"

### 决策
阶段 2（art-ui 写 prompts.md）时，必须把 layout 里的**画布尺寸 + 关键组件比例**注入到 prompt 里，让效果图生成时就按结构比例产出，避免"效果图长宽与 Prefab 结构冲突"的经典问题。

### 具体形式
```markdown
## MainMenuForm 效果图提示词

结构约束（来自 prefab-layout.md）：
- 画布 1920×1080
- 标题占用 top 12% 高度，宽度居中 42%
- 按钮组占用中部 middle-center，共 3 个按钮堆叠，每按钮 320×80

提示词：
"A game main menu UI at 1920x1080 resolution, title bar occupying top 12% height,
 centered 42% width; three primary buttons stacked in middle-center area,
 each button 4:1 aspect ratio (320x80); ..."
```

### Trade-off
- ✅ 效果图长宽 = layout 长宽 = 最终 Prefab 长宽，三者对齐
- ✅ 让 codex-image-gen 生成的图直接可用于素材拆分
- ⚠️ 提示词长度增加约 20-40%，但换来后置阶段的稳定性

## 决策 5：素材拆分保留纯绿色 #00ff00 绿幕

### 讨论过程
grill-me 第 3 轮 Q6 用户一开始说"白色背景"（想简化 chroma_key 阈值），第 4 轮更正为"纯绿色 #00ff00"。

### 决策：不动现有 chroma_key 工具链
- 保留 `openspec/changes/<NN>/art/raw/<PageName>/` 下拆分素材的**背景层 = 一张整图（可带渐变或纯色）**，**前景组件 = 绿幕背景 + 组件**
- 通过 `.claude/skills/ui-asset-splitting/references/chroma_key.py` 去绿幕
- 不引入白底 / 透明底方案（避免多套阈值参数）

### Trade-off
- ✅ 复用已验证的 chroma_key.py + image_cut.py 工具链
- ✅ 绿幕对 UI 常见配色（蓝紫红金）冲突最少（白/黑底会与 UI 底色冲突）

## 决策 6：状态每态独立生成（不做状态合成）

### 决策
按钮的 3 态（normal / pressed / disabled）、页签的 2 态（selected / unselected）、弹窗的按钮组 —— 每一态**都要 art-ui 在 prompts.md 单独列一条**，codex-image-gen 各生成一张，绝不允许 client-unity 用代码"给 normal 图加个半透明黑遮罩当 pressed"。

### 原因
- 状态合成的视觉效果永远差于原生设计（尤其在暗色 UI + pressed 需要"高光边"而非"变暗"的场景）
- 一旦开状态合成的口子，art-ui 会偷懒只出 normal，pressed/disabled 全交 client-unity 用代码糊 → 破窗

### 落到 SKILL
`ui-asset-splitting/SKILL.md` 新增「状态完整性 checklist」：
- 按钮：normal / pressed / disabled 3 张
- 页签：selected / unselected 2 张
- 弹窗按钮组：确认 / 取消（两态独立，不用同一贴图配色变体）
- 拆分完成时若发现少态 → 阻塞流程回阶段 3 补生成

## 决策 7：画布不够时增加新画布

### 决策
codex-image-gen 有画布尺寸上限（当前 L2 档最大 1536×1024）。若单页 UI 结构长宽超过该上限：

- **不做**：把 UI 挤压到 1536×1024 里凑
- **做**：art-ui 在 `art/prompts.md` 里拆成多张画布（如 1920×1080 UI 拆成 1920×540 上半 + 1920×540 下半），素材拆分时分别处理，最后在 Prefab 阶段拼接

### 落到 SKILL
`ui-asset-splitting/SKILL.md` 新增「画布不够加新画布」小节，附上判定阈值和拆分策略示例。

## 决策 8：新建 `unity-rect-transform` SKILL

### 触发条件
- art-ui 在阶段 1 出 layout 时需要 RectTransform 知识（anchor / pivot / sizeDelta / anchoredPosition 与视觉效果的映射）
- client-unity 在阶段 5 拼 Prefab 时需要同样的知识
- 两个 agent 共享此 SKILL，作为 UGUI 空间语言的公共词典

### SKILL 内容大纲
```
SKILL.md  — 概述 + 触发关键词
references/
  anchors.md         — anchorMin/anchorMax 9 宫格 + stretch 组合的视觉效果表
  pivot.md           — pivot 的意义 + 旋转/缩放锚点原理
  sizeDelta.md       — sizeDelta 在 fixed vs stretch anchor 下的语义差异（常见陷阱）
  anchored-position.md — anchoredPosition 与 localPosition 的区别
  preserve-aspect.md   — Image.preserveAspect 的适用场景 + 图源尺寸建议
  common-pitfalls.md   — Canvas Scaler / Anchor Preset 快捷键 alt / 父子 anchor 冲突等
```

### 归属
共享 SKILL：art-ui + client-unity 白名单都持有。

## 决策 9：移除 `.claude/UGUI预制体规范.md`

### 现状
文件已由用户在会话中主动删除（原为 figma 设计流程写的，本项目不用 figma）。

### 决策
- 检查 `.claude/CLAUDE.md §十三 设计文档与索引` 表格是否有指向该文件的链接 → 若有则删除
- 检查 `.claude/skills/` 下所有 SKILL 是否有 references 指向该文件 → 若有则改指向 `unity-rect-transform` 或 `prefab-layout.md` 模板

## 决策 10：不回溯已归档 UI

### 决策
已归档 change 里按 v2 三表流程做的 UI（CharacterSelect / SelfTattoo / MainMenu 等）**不重新按新流程重做**。规则：

- 下一次任何 change 动到这些 UI（改布局 / 加组件 / 加状态） → **触发按新流程重做本页 UI**（补 prefab-layout.md → 重生效果图 → 素材拆分 → 拼 Prefab）
- 只是绑事件 / 改文案 / 修 bug → 不需要重做

### 原因
- 已归档 UI 数量约 5-8 页，全部重做成本高
- 已归档 UI 大多数还没做过多分辨率适配，未来动它时自然会触发重做

## 决策 11：简单弹窗也走完整流程

### 讨论
grill-me 第 4 轮用户明确：即使是"确认/取消"两按钮的简单弹窗，也走完整 6 阶段流程，**不开豁免口子**。

### 原因
- 豁免口子会破窗：一旦允许"简单弹窗跳过 layout"，art-ui 会把越来越多的弹窗归为"简单"，最终回到"效果图先行"的老路
- 单个弹窗的 layout 也就 5-10 行 Markdown，成本极低
- 简单弹窗恰恰是新 art-ui 练手 layout 语言的好机会

### 落到 CLAUDE.md
§六 强制约束里加一条：**任何 UI，无论简繁，都走完整 6 阶段。不设"简单弹窗豁免"**。

## 决策 12：本次为纯文档流程重构

### 决策
本次 change 不产生 C# 代码变更，不新增测试用例。落地范围仅限：

- `.claude/CLAUDE.md`
- `.claude/SKILL_MATRIX.md`
- `.claude/skills/SKILLS_INDEX.md`
- `.claude/skills/ai-art/`（改）
- `.claude/skills/ui-asset-splitting/`（改）
- `.claude/skills/unity-rect-transform/`（新建）
- `.claude/agents/art-ui.md`（改）
- `.claude/agents/client-unity.md`（改）
- `openspec/changes/17-ui-structure-first/`（本目录）

### 原因
本次是"UI 制作流程"的规范升级，不动任何运行时代码；qa-engineer 不介入，因此不建 `tests/` 子目录。第一次按新流程真做的 UI change 时，联调阶段（阶段 6）自然会跑通新流程 —— 那才是最好的验证。
