---
capability: ui-workflow
version: v3
created: 2026-07-01
supersedes: v2 (2026-06-28, capability=ui-planning)
---

## ADDED Requirements

### Requirement: UI 制作强制走 6 阶段结构先行流程

任何**新建 / 重做**的 UI 界面 MUST 按 6 阶段顺序执行：结构设计 → 效果图设计 → 效果图生成 → 素材拆分 → 拼装实现 → 联调微调。跳阶段视为违规。已归档 change 里按 v2 三表流程做的 UI 不回溯改造；纯代码修改（绑事件 / 改文案 / 修 bug 不重做 UI）不受本流程约束。

#### Scenario: 简单弹窗也走完整 6 阶段
- **GIVEN** 用户要求做一个「确认/取消」两按钮的简单弹窗
- **WHEN** 主对话开始编排
- **THEN** MUST 依次走完 6 阶段，MUST NOT 跳过 `prefab-layout.md` 或效果图直接建 Prefab

#### Scenario: 跳阶段视为违规
- **GIVEN** `prefab-layout.md` 未产出或未确认
- **WHEN** 尝试进入阶段 2（写效果图提示词）
- **THEN** MUST 阻塞并回退到阶段 1

### Requirement: 阶段 1 由 art-ui 用 unity-rect-transform SKILL 产出 prefab-layout.md

阶段 1 主导 agent MUST 是 `art-ui`，且 MUST 调用 `unity-rect-transform` SKILL。产出物 MUST 是单文件 `openspec/changes/<change>/art/prefab-layout.md`，含全局约定 + 每页节点树 + 每节点的 RectTransform 四元组（anchor / pivot / sizeDelta / anchoredPosition）+ 状态清单 + 跨页复用组件说明。禁止以「三表」（页面清单 / 复用组件清单 / 组件状态表）任一形式替代。

#### Scenario: layout 缺 RectTransform 数据视为不通过
- **GIVEN** art-ui 产出的 `prefab-layout.md` 某节点只写了名字没写 anchor/pivot/sizeDelta/anchoredPosition
- **WHEN** 用户或主对话审查
- **THEN** MUST 打回阶段 1 补齐四元组

#### Scenario: 三表已被删除
- **GIVEN** ai-art 或 art-ui 试图起草「页面清单 / 复用组件清单 / 组件状态表」
- **WHEN** 主对话检查阶段 1 产出
- **THEN** MUST 拒绝三表，要求改为 `prefab-layout.md`

### Requirement: 阶段 2 效果图提示词必须从 layout 反哺结构约束段

阶段 2 art-ui 产出的 `art/prompts.md` 中每页提示词 MUST 以「结构约束」段开头，段内 MUST 直接引用 layout 中的画布尺寸 + 关键组件的 anchor/sizeDelta 转成人类语言 + 各节点在画布上的占比。状态变体 MUST 每态一条独立提示词。

#### Scenario: 提示词缺「结构约束」段视为不通过
- **GIVEN** `prompts.md` 某页提示词直接开始写"欧美卡通风格 UI 弹窗..."
- **WHEN** 主对话审查阶段 2 产出
- **THEN** MUST 打回补写「结构约束」段

#### Scenario: 状态变体每态独立提示词
- **GIVEN** layout 中的按钮节点声明 states: [normal, pressed, disabled]
- **WHEN** art-ui 写该按钮的效果图提示词
- **THEN** MUST 写 3 条独立提示词（每态一条），MUST NOT 一条提示词里画多态

### Requirement: 阶段 3 效果图生成走 codex-image-gen 且重试上限 3 轮

阶段 3 MUST 由主对话直接调 `codex-image-gen` SKILL。产出 MUST 落到 `openspec/changes/<change>/art/mockups/<PageName>.png`（含 `_state` 后缀区分状态变体），同目录 MUST 写 `生成记录.md`。用户未通过时可重试，累计上限 3 轮；第 3 轮仍未通过 MUST 阻塞交回用户人工介入。

#### Scenario: mockups 目录严格与 raw 分离
- **GIVEN** codex-image-gen 生成完 mockup
- **WHEN** 主对话落盘
- **THEN** MUST 落到 `art/mockups/`，MUST NOT 落到 `art/raw/`

#### Scenario: 效果图第 4 轮直接阻塞
- **GIVEN** 效果图已重试 3 轮用户仍不满意
- **WHEN** 用户要求继续调整
- **THEN** MUST 阻塞并列出选项（手动找参考 / 跳过本页 / 重新设计 layout），MUST NOT 无限重试

### Requirement: 阶段 4 按 layout 拆素材，多张 mockup Fan-Out 并行

阶段 4 主对话 MUST 按 mockup 页面数 fan-out N 个子 Agent，各自调 `ui-asset-splitting` SKILL。每个 Agent MUST 从 `prefab-layout.md` 读该页节点树，按节点拆背景 / 组件 / 状态变体。状态变体 MUST 每态独立一张贴图；一张画布装不下 MUST 拆多张 batch（如 `_merged/batch_1.png`）。产出 MUST 搬进 `Assets/Resources/Sprite/UI/<PageName>/`。

#### Scenario: 多张 mockup 并行拆分
- **GIVEN** 阶段 3 通过了 3 张 mockup
- **WHEN** 进入阶段 4
- **THEN** 主对话 MUST 同一回合内 fan-out 3 个 Agent，MUST 用 `await UniTask.WhenAll` 等汇合

#### Scenario: 组件绿幕画布不够就拆 batch
- **GIVEN** 某页需要拆 12 个按钮 3 态 = 36 张组件贴图，1024×1024 绿幕画布装不下
- **WHEN** ui-asset-splitting 编排
- **THEN** MUST 拆 `_merged/batch_1.png` / `batch_2.png`，MUST NOT 硬塞到一张画布上

#### Scenario: 导入设置不许手动改
- **GIVEN** 素材已搬进 `Assets/Resources/Sprite/UI/<PageName>/`
- **WHEN** `UISpriteImportProcessor` 自动运行
- **THEN** `.meta` 中 `textureType: 8` MUST 已被自动设置；开发者 MUST NOT 在 Inspector 里手动改

### Requirement: 阶段 5 单线 client-unity，用 unity-skills MCP 按 layout 建 Prefab

阶段 5 MUST 是**单线** client-unity（不再 fan-out art-ui 出标注稿）。client-unity MUST 前置读取 `prefab-layout.md` + `art/mockups/<PageName>.png` + `Assets/Resources/Sprite/UI/<PageName>/`。Prefab 创建 MUST 优先调 `unity-skills` MCP（按 layout 节点树建层级 + 设 RectTransform 四元组 + 贴入拆分素材 + AddComponent）；MCP 不可用 MUST 回退到通知用户在 Unity Editor 手动搭。调用 unity-skills 时若参数含 CJK / Emoji MUST 用 `--stdin-json` 模式。

#### Scenario: v3 阶段 5 禁止 Fan-Out
- **GIVEN** 主对话要进入阶段 5
- **WHEN** 试图同时分派 art-ui（标注稿）+ client-unity（Prefab+脚本）
- **THEN** MUST 拒绝，改为单线 client-unity；art-ui 标注稿 v3 已取消

#### Scenario: Prefab 层级与 layout 一致
- **GIVEN** client-unity 建完 Prefab
- **WHEN** 逐节点核对
- **THEN** 节点树 + 每节点的 anchor/pivot/sizeDelta/anchoredPosition MUST 与 `prefab-layout.md` 完全一致

#### Scenario: CJK 节点名走 stdin-json
- **GIVEN** layout 中某节点名叫「设置面板标题」
- **WHEN** client-unity 调 unity-skills MCP 建该节点
- **THEN** MUST 用 `--stdin-json` 模式传参，MUST NOT 命令行 arg 直接传

### Requirement: 阶段 6 联调必须以效果图为准绳

阶段 6 client-unity MUST 把运行时截图与 `art/mockups/<PageName>.png` 并排对比，MUST 列偏差清单（间距 / 字号 / 配色）后再迭代修复。禁止凭感觉调。若联调发现必须调整 anchor / pivot / sizeDelta MUST 同步回写 `prefab-layout.md`，保持三者一致（layout / Prefab / 效果图）。

#### Scenario: 凭感觉调视为违规
- **GIVEN** 运行时按钮位置与效果图有偏差
- **WHEN** client-unity 直接改 sizeDelta 数值
- **THEN** MUST 先出偏差清单（几像素 / 哪方向），MUST NOT 无对比直接改

#### Scenario: 联调调整回写 layout
- **GIVEN** 联调时把某节点 anchoredPosition 从 (0, -60) 改成 (0, -80)
- **WHEN** 修复通过
- **THEN** MUST 同步更新 `prefab-layout.md` 中该节点的 anchoredPosition
