# Tasks — 10-settings-form

> 进度。✅ = 完成；🟡 = 进行中；🔲 = 未开始。
> 每阶段完成后停下来等用户确认才进下一阶段。

## 阶段 1 — 需求设计（主对话 + producer + gd-system + ai-art）

- [🟡] proposal.md — 范围、DoD、非目标、风险
- [🟡] design.md — 数据模型 / SettingsModule API / 状态机 / 跨模块契约 / 关键时序
- [🟡] tasks.md — 本文件
- [🟡] specs/settings/spec.md — 验收契约
- [🟡] art/requirements.md — UI 三表骨架（页面清单 / 复用组件清单 / 组件状态表）
- [🔲] **用户审阅 + 确认** ← 此处停下来

## 阶段 2 — 效果图设计（art-ui）

- [🔲] art/prompts.md — 单页效果图提示词（含风格 / 配色 / 字体 / 构图 / 信息层级 / 负面词）
- [🔲] **用户确认提示词** ← 此处停下来

## 阶段 3 — 效果图生成（codex-image-gen，主对话直接调用）

- [🔲] 调 codex-image-gen 出图
- [🔲] art/mockups/SettingsForm.png 落盘
- [🔲] art/mockups/生成记录.md 同目录写
- [🔲] **用户确认效果图**（重试上限 3 轮，超出阻塞通知用户）← 此处停下来

## 阶段 4 — Prefab + 代码（Fan-Out 并行：art-ui ∥ client-unity）

### Agent A — art-ui
- [🔲] art/annotations/SettingsForm-annotated.png — 标注稿（控件命名 / 尺寸 / 锚点 / 字号 / 配色）

### Agent B — client-unity
- [🔲] 调研 `InputModule` 现有 API；若缺 §4.3 列出的 5 个接口 → 通知用户后补
- [🔲] 调研 `AudioModule` 现有音量接口；若缺 SetBgmVolume/SetSfxVolume → 通知用户后补
- [🔲] Assets/Scripts/Events/SettingsEvents.cs 新建（SettingsAppliedEvent）
- [🔲] Assets/Scripts/Modules/Settings/SettingsModule.cs 新建
- [🔲] Assets/Scripts/Modules/Settings/SettingsData.cs 新建
- [🔲] Assets/Scripts/Modules/UI/SettingsForm.cs 新建（继承 UIFormBase）
- [🔲] Assets/Resources/UI/SettingsForm.prefab — unity-skills MCP 自动建（基础层级 + 控件命名）
- [🔲] Assets/Settings/URP-Low.asset / URP-Med.asset / URP-High.asset — 占位 RP Asset（如不存在）
- [🔲] Assets/Resources/DataTable/UIFormConfig.json 加 SettingsForm 行 + 跑 DataTableGenerator
- [🔲] MainMenuForm / PauseMenuForm 「设置」按钮接 OpenAsync\<SettingsForm\>()
- [🔲] EditMode 测试：SettingsModule_RebindConflict_RejectsAndKeepsOriginal()
- [🔲] GameApp.cs 注册 SettingsModule

### 汇合
- [🔲] await UniTask.WhenAll(A, B)
- [🔲] **用户确认编译通过 + 标注稿一致** ← 此处停下来

## 阶段 5 — 联调微调（client-unity + 用户）

- [🔲] PlayMode 运行 → 打开 SettingsForm → 截图
- [🔲] 截图 vs art/mockups/SettingsForm.png 并排对比
- [🔲] 列偏差清单（间距 / 字号 / 配色），逐项修
- [🔲] 5 项手测全过（拖音量 / 取消恢复 / 画质保留 / 重绑定冲突 / 启动应用上次保存）
- [🔲] tests/results.md 记录结果
- [🔲] **用户确认通过** ← 此处停下来

## 归档

- [🔲] `openspec archive-change 10-settings-form`
- [🔲] 同步 `项目知识库（AI自行维护）/INDEX.md`
