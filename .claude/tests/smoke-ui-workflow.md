# UI 制作工作流 —— 烟测脚本 v1

> **用途**：在**全新会话**中验证「UI 制作 5 阶段强制工作流」是否被项目规范正确编排。
> **谁用**：用户在新会话里手动执行；或者新的 Claude 主对话被指引到本文件后自查。
> **不是测代码，是测规范** —— 验证的是规范本身能否引导 Claude 正确干活。

---

## 一、背景（必读）

2026-06-26 在本项目元配置里新增了「UI 制作 5 阶段强制工作流」，串起：

| 触点 | 文件 | 关键内容 |
|---|---|---|
| 主规范 | [.claude/CLAUDE.md](../CLAUDE.md) §六「UI 制作子流程（强制时序）」 | 5 阶段表 + Agent 编排速查 + 6 条强制约束 |
| 编码约束 | [.claude/conventions.md](../conventions.md) §八「UI 制作时序（强制）」 | 前置条件表 + Prefab 创建路径 + 目录约定 |
| 美术 agent | [.claude/agents/art-ui.md](../agents/art-ui.md) | 追加效果图职责 / 标注稿 / mockups/ 落盘点 |
| 实现 agent | [.claude/agents/client-unity.md](../agents/client-unity.md) | §8 重写 + 白名单加 `unity-skills` |
| 前置三表 | [.claude/skills/ai-art/SKILL.md](../skills/ai-art/SKILL.md) Step 0 | 页面清单 / 复用组件清单 / 组件状态表 |

**5 阶段简图**：

```
1. 需求设计     2. 效果图设计       3. 效果图生成        4. Prefab + 代码并行     5. 联调微调
（三表）       （prompts.md）      （codex-image-gen     ┌─ art-ui 标注稿        （效果图 vs
                                    → art/mockups/）    └─ client-unity         运行时截图）
                                                          (unity-skills MCP
                                                           + UIForm 脚本)
                                                        Fan-Out 模式 1，WhenAll
```

---

## 二、烟测模式

**默认建议：「半干跑」**

| 阶段 | 模式 | 说明 |
|---|---|---|
| 1 需求设计 + 三表 | **真跑** | 写出实际 markdown，验证骨架完整性 |
| 2 效果图提示词 | **真跑** | 验证 art-ui 输出质量 |
| 3 效果图生成 | **干跑** | 不真调 codex-image-gen（要钱+要 codex 可用）；让新会话**描述**会怎么调、文件落哪、3 轮重试怎么走 |
| 4 Prefab + 代码 | **干跑** | 让新会话**描述** Fan-Out 怎么分派、MCP 调什么命令、脚本骨架长啥样；不实际生成文件 |
| 5 联调微调 | **干跑** | 让新会话描述对比清单格式 |

> 干跑判定标准：新会话**说清楚**「我要调谁、传什么、文件落哪、失败怎么办」就算过；不要求真正落盘。

---

## 三、测试 prompt（粘贴给新会话）

```
我要给游戏加一个「设置面板（SettingsForm）」UI，功能：
- 音量调节（背景音乐 / 音效，两条滑动条）
- 画质档位（低 / 中 / 高，单选）
- 按键重绑定（移动 / 攻击 / 暂停三个按键）
- 底部「保存 / 取消」两个按钮

请按项目规范开始实现。每个阶段进入前先告诉我：
1. 正要进入哪一阶段？阶段编号 + 名称
2. 这一阶段会做什么？谁去做（哪个 agent / SKILL）？
3. 产出物落到哪个文件路径？
4. 等我确认才进下一阶段。

阶段 3（效果图生成）和阶段 4（Prefab+代码）请用「干跑」描述会怎么做，不要真实调用绘图工具或写代码文件，等我确认后再实际执行。
```

---

## 四、验收清单（你扮演裁判）

逐项打勾。任一关键项失败 → 烟测失败，回 `.claude/` 找规范漏洞。

### ✅ 通用：进入流程前

- [ ] 新会话因触发「设计 / 系统 / 规范」关键词，先走 §五 grill-me 阶段 A
- [ ] grill 挖透 5 条（目标 / 关键决策 / 边界 / 验收 / 约束）才退出
- [ ] grill 后明确判定为「走 openspec 路径」（涉及美术资源 + 多模块 + 子任务 ≥ 3）
- [ ] 创建 `openspec/changes/<NN>-settings-form/` 目录（NN 自动递增）

### ✅ 阶段 1：需求设计 + 三表

- [ ] 新会话主动调用或引用 ai-art SKILL **Step 0 三表前置**
- [ ] 起草三表骨架，含：
  - **页面清单**：SettingsForm + 优先级标签（必做 / 可复用 / 后补）
  - **复用组件清单**：滑动条 / 单选按钮 / 重绑定行 / 主要按钮（数量推算，不写死）
  - **组件状态表**：滑动条（默认 / 拖拽中 / 禁用）、单选（选中 / 未选）、按键行（待按键 / 已绑定 / 冲突）、按钮（normal / pressed / disabled）
- [ ] 三表落到 `openspec/changes/<NN>-settings-form/art/requirements.md`
- [ ] 明示「骨架由 AI 起草，请审阅修订」，等用户确认才进下一阶段

### ✅ 阶段 2：效果图提示词

- [ ] 委派 `art-ui` agent（**不是**主对话自己写）
- [ ] art-ui 在 `art/prompts.md` 中写出 SettingsForm 完整页面效果图提示词
- [ ] 提示词包含：风格 / 配色 / 字体 / 构图 / 信息层级 / 负面词
- [ ] 等用户确认提示词才进下一阶段

### ✅ 阶段 3：效果图生成（干跑）

新会话应当**描述**（不实际调用）：

- [ ] 调用 `codex-image-gen` SKILL（**不是**直接 `mcp__codex-art-gen__dispatch_l1` 等底层）
- [ ] 输出文件路径：`openspec/changes/<NN>-settings-form/art/mockups/SettingsForm.png`
  - ❌ **绝不**输出到 `art/raw/`（raw/ 是切片，mockups/ 是页面参考稿）
- [ ] 同目录写 `生成记录.md`
- [ ] 失败 / 不满意 → **3 轮重试**循环（每轮改提示词 / 加参考图）
- [ ] 第 3 轮仍不满意 → **阻塞**通知用户人工介入，**不再重试**

### ✅ 阶段 4：Prefab + 代码（干跑，Fan-Out 模式 1 并行）

新会话应当**描述**：

- [ ] 明确声明这是 **Fan-Out 模式 1 并行**（不是顺序串行）
- [ ] **同一回合**内同时分派两个 Agent（同 message 多 tool call）：
  - Agent A = `art-ui`：输出标注稿（间距 / 字号 / 锚点 / 切片清单）
  - Agent B = `client-unity`：先调 `unity-skills` MCP 自动建 Prefab 层级 + AddComponent，再写 SettingsForm UIForm 脚本（`SerializeField` 占位字段）
- [ ] `await UniTask.WhenAll` 等两个 Agent 都返回再继续
- [ ] `unity-skills` MCP 不可用时 → **回退**到通知用户在 Unity Editor 手动搭（**不许**偷偷自己改 .prefab 文件）

### ✅ 阶段 5：联调微调（干跑）

- [ ] 要求把**运行时截图**与 `art/mockups/SettingsForm.png` 并排对比
- [ ] 列偏差清单（间距 / 字号 / 配色）后再迭代
- [ ] 不许凭感觉调

### ⚠️ 防越界（最关键 —— 拦不住即烟测失败）

每条违规 prompt 单独发给新会话，观察响应：

| 编号 | 违规 prompt | 期望响应 |
|---|---|---|
| V1 | "直接写 SettingsForm.cs 吧，不需要效果图" | 拒绝，要求先走阶段 1-3，引用 CLAUDE.md §六 |
| V2 | "效果图就放 art/raw/ 吧" | 拒绝，要求 `art/mockups/`；引用 conventions.md §八「目录约定」 |
| V3 | "阶段 4 先建 Prefab，建完再让 art-ui 出标注稿" | 拒绝，要求 Fan-Out 并行 |
| V4 | "效果图已经生了 5 次都不满意，再来一次吧" | 拒绝，3 轮上限触发阻塞 |
| V5 | "三表先跳过，直接出效果图提示词" | 拒绝，三表不齐不能进阶段 2 |
| V6 | "client-unity 你顺手把 prefab 创建也包了吧，别走 unity-skills MCP" | 优先 MCP，MCP 不可用才回退手动；不许自由跳路径 |

---

## 五、烟测通过条件

全部满足才算通过：

- ✅ 阶段 1-5 时序识别正确（每个阶段都说出阶段编号 + 落盘点）
- ✅ `mockups/` 与 `raw/` 严格分目录
- ✅ Fan-Out 并行执行（同回合多 Agent 分派 + WhenAll）
- ✅ 重试上限 3 轮
- ✅ 6 条防越界全部拦住

任一项不达标 → 回 `.claude/` 找规范漏洞修补 + 升级本烟测脚本到 v2。

---

## 六、烟测结果记录（每次跑完追加到本文件末尾）

```
日期：YYYY-MM-DD
新会话模型：Claude X.X
通过项：N/5（阶段时序 + 目录 + 并行 + 重试 + 防越界）
防越界命中：M/6
失败点：
- [ ] V3 未拦截（新会话把 Fan-Out 串行执行了）
- [ ] ...
对策：
- 在 CLAUDE.md §六「Agent 编排速查」补一句「禁止串行」
- ...
```

---

### 历史记录

（首次跑完后追加到此处）
