---
name: ai-art
description: "AI 绘图提示词生成与美术素材实现工具集。当用户需要生成 AI 绘图提示词、描述图片需求、从参考图反推提示词，或可能想根据 openspec change 下 art/ 子目录里的需求和提示词直接生成图片时使用。覆盖类型：技能/Buff 图标（ICON）、角色立绘（CHARACTER）、场景图（SCENE）、UI 元素（UI）、以及从图片反推提示词。触发场景：用户提到'美术风格'、'初始化美术风格'、'绘图提示词'、'画一个'、'图标设计'、'角色立绘'、'场景图'、'参考图分析'、'反推提示词'、'实现和处理美术素材'、'把美术需求出图'、'按提示词生成素材'、'处理美术资源'等。"
---

# AI Art — AI 绘图提示词生成

## 子技能列表

| 子技能 | 适用场景 | 参考文件 |
|--------|---------|---------|
| **drawing-prompt-generator** | 总入口/路由器：根据绘图需求自动判断类型并调用子模块 | `references/drawing-prompt-generator.md` |
| **drawing-prompt-CHARACTER** | 角色立绘提示词：根据角色职业、外貌、性格生成立绘/头像提示词 | `references/drawing-prompt-CHARACTER.md` |
| **drawing-prompt-ICON** | 技能/Buff 图标提示词：根据技能机制和效果描述生成图标提示词 | `references/drawing-prompt-ICON.md` |
| **drawing-prompt-SCENE** | 场景图提示词：根据场景类型、地域风格、氛围生成背景提示词 | `references/drawing-prompt-SCENE.md` |
| **drawing-prompt-UI** | UI 元素提示词：根据 UI 组件类型生成按钮、面板、边框等提示词 | `references/drawing-prompt-UI.md` |
| **drawing-prompt-COMMON** | 通用素材提示词：子弹、道具、树木、岩石、特效、载具等不属于上述类型的 2D 素材 | `references/drawing-prompt-COMMON.md` |
| **image-to-prompt-generator** | 从参考图片逆向分析，反推出 AI 绘图提示词（输出 JSON） | `references/image-to-prompt-generator.md` |

## 使用流程

1. 先读取 `references/drawing-prompt-generator.md`（总入口），了解分类判断逻辑
2. 根据用户描述的绘图需求类型，读取对应子模块的参考文件
3. 如果用户提供了参考图片要反推提示词，读取 `references/image-to-prompt-generator.md`

## 选择指南

- 用户要**生成提示词**（描述需求） → 先读总入口，再按类型读子模块
- 用户提供**参考图片**要反推 → image-to-prompt-generator
- 用户明确输入**"实现和处理美术素材"** → 执行下方"美术素材实现流程"（输出到 `openspec/changes/<change-name>/art/raw/`）
- 用户表达相近意图（如"把美术需求出图"、"按提示词生成素材"、"处理美术资源"） → 先询问是否按"实现和处理美术素材"流程执行
- 不确定类型 → 读总入口，它会帮你判断

## 美术素材实现流程

当用户明确输入 **"实现和处理美术素材"**，或对相近意图确认要执行该流程后：

1. 定位当前 openspec change：
   - 优先：执行 `openspec status` 找 active change（同一时刻只有 1 个合法）
   - 次选：从用户上下文（最近提到的 change-name）推测
   - 最次：询问用户「目标 change 是哪个？」
2. 读取 `openspec/changes/<change-name>/art/requirements.md` 和 `art/prompts.md`。多模块需求按 prompts.md / requirements.md 中的 `<子模块名>` 字段定位。
3. 处理前检查 `art/requirements.md` 与 `art/prompts.md` 文件头部是否已有 `美术素材状态: 已处理` 或 `美术素材状态: 已归档`。若已存在，先提示用户该需求可能已处理过，并询问是否重新生成。
4. 解析每个资源的：
   - 资源名/文件名
   - 类型（角色、敌人、子弹、UI、背景、道具等）
   - 尺寸、透明背景、风格一致性、负面提示词
   - 是否需要多个候选图
5. 如果当前是 GPT 系列模型且可用绘图模型/图像生成工具，必须调用绘图模型逐项生成图片。
6. 输出目录固定为：
   - 单模块：`openspec/changes/<change-name>/art/raw/`
   - 多模块：`openspec/changes/<change-name>/art/raw/<子模块名>/`
7. 图片保存命名：
   - 优先使用需求或提示词里的明确文件名
   - 否则使用 `序号-资源名.png`
   - 多候选使用 `资源名_v01.png`、`资源名_v02.png`
   - 禁止保留 `ChatGPT Image ...png` 等绘图工具默认文件名
8. 同 `art/raw/` 目录写入 `生成记录.md`，记录每张图的资源名、来源提示词摘要、输出文件名、生成状态、后续处理建议。
9. 生成完成后，在对应 `art/requirements.md` 和 `art/prompts.md` 文件头部写入或更新：`美术素材状态: 已处理`、`处理日期: YYYY-MM-DD`、`输出目录: art/raw/`、`生成记录: art/raw/生成记录.md`。
10. 如果无法调用绘图模型，或工具不能把生成结果保存到本地路径，必须明确告诉用户阻塞点，不得伪造文件或声称已保存。

## 美术风格初始化流程

`references/drawing-prompt-{CHARACTER,ICON,SCENE,UI,COMMON}.md` 中的"核心设计理念"等风格相关内容默认**留空（待初始化）**，因为美术风格因项目而异。当用户表达"初始化美术风格"、"确定/设置项目美术风格"等意图时，执行以下流程：

### 1. 多轮问答，确定项目美术风格

不要凭空猜测或套用其他项目的风格。通过**多轮提问**逐步明确：

- **整体风格定位**：写实/卡通/像素/扁平/赛博朋克/水墨等大方向；是否有参考游戏、作品或图片
- **色彩倾向**：主色调、明暗/饱和度偏好、是否已有项目品牌色
- **角色风格**（若项目有 CHARACTER 需求）：角色比例、服饰/材质特征、表情与姿态倾向
- **图标风格**（若项目有 ICON 需求）：图标整体质感、效果与色彩的对应关系、构图习惯
- **场景风格**（若项目有 SCENE 需求）：世界观背景、典型场景类型、光影与氛围
- **UI 风格**（若项目有 UI 需求）：整体质感（金属/扁平/玻璃拟态/手绘等）、装饰元素、九宫格规范
- **通用素材风格**（子弹/道具/树木/岩石/特效/载具等）：与角色/UI 风格的协调程度、细节繁简程度

每一轮问答后，先用自己的话总结理解并请用户确认或纠正，信息不全时不要直接定稿。

### 2. 更新参考文件

风格确定后，更新对应的 `references/drawing-prompt-{CHARACTER,ICON,SCENE,UI,COMMON}.md`：

- 将"核心设计理念"中的"待初始化"占位说明，替换为实际的风格定位描述
- 按类型补充对应的视觉语言表/分类表/色彩体系（CHARACTER 的职业视觉语言表、ICON 的效果-色彩映射表、SCENE 的场景类型分类表、UI 的元素分类表、COMMON 的常见素材类型推理示例）
- 将"提示词结构"模板中的 `[项目美术风格关键词]`、`[主题描述]` 等占位符替换为符合实际风格的具体描述
- 补充 1~2 个推理示例，供后续生成提示词时参考

### 3. 中途确认

过程中如遇到风格描述模糊、信息冲突，或拿不准如何转化为具体视觉语言，**随时向用户提问确认**，不要自行猜测后直接写入参考文件。
