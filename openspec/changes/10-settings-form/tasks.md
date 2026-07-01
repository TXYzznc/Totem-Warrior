# Tasks — 10-settings-form

> ✅ = 完成；🟡 = 进行中；🔲 = 未开始。
> **v1 阶段（2026-06-26）**：数据 / 状态机 / EditMode 单测 / 入口接线全部落地，此处补勾。
> **v2 阶段（2026-07-01）**：UI 表现层重做走 v3 6 阶段，前 5 阶段串行 + 用户逐阶段确认，阶段 6 启 loop。

## v1 阶段 — 数据层已完成（补勾）

- [x] proposal.md — 范围、DoD、非目标、风险（v2 已增补 UI 重做节）
- [x] design.md — 数据模型 / SettingsModule API / 三态机 / 跨模块契约
- [x] specs/settings/spec.md — 验收契约
- [x] `Assets/Scripts/Events/SettingsEvents.cs`（`SettingsAppliedEvent`）
- [x] `Assets/Scripts/Modules/Settings/SettingsModule.cs`（三态 BeginEdit/Preview/Commit/Rollback）
- [x] `Assets/Scripts/Modules/Settings/SettingsData.cs`
- [x] `Assets/Resources/DataTable/UIFormConfig.json` — 加 `Id=10 SettingsForm` 行 + 跑 DataTableGenerator
- [x] `MainMenuForm` / `PauseMenuForm` 「设置」按钮接 `SettingsForm.Open()`
- [x] `Assets/Settings/URP-Performant/Balanced/HighFidelity.asset`（原设计说 Low/Med/High，实际用了默认 URP 命名）
- [x] `Assets/Tests/EditMode/Settings/SettingsModuleTests.cs`
- [x] `GameApp.cs` 注册 SettingsModule

## v2 阶段 — UI 表现层重做（走 v3 6 阶段）

### 阶段 0 — 清理旧资产（主对话）
- [ ] 删 `Assets/Resources/Prefab/UI/Settings.prefab` + `.meta`（阶段 5 生成新 Prefab 前保留，防编译崩）
- [ ] 删 `Assets/Scripts/Modules/UI/SettingsForm.cs` 内容（阶段 5 原地重写；保留 class stub 防编译崩）
- [ ] 清 `openspec/changes/10-settings-form/art/{mockups,raw,annotations}` + `prompts.md` + `requirements.md`

### 阶段 1 — 结构设计（art-ui + unity-rect-transform）
- [ ] 产出 `art/prefab-layout.md`：全局约定 / 节点树 / RectTransform 数据 / 状态清单 / 跨页复用
- [ ] **用户确认 layout** ← 停下来

### 阶段 2 — 效果图设计（art-ui）
- [ ] 产出 `art/prompts.md`：单页提示词，开头带「结构约束」段（画布 + 各节点占比，从 layout 提取）
- [ ] **用户确认提示词** ← 停下来

### 阶段 3 — 效果图生成（codex-image-gen）
- [ ] `art/mockups/SettingsForm.png` + 同目录 `生成记录.md`
- [ ] **用户确认效果图**（重试上限 3 轮） ← 停下来

### 阶段 4 — 素材拆分（fan-out ui-asset-splitting）
- [ ] `art/raw/SettingsForm/*`（背景 1 张 + 组件/状态变体）
- [ ] 搬进 `Assets/Resources/Sprite/UI/SettingsForm/`（UISpriteImportProcessor 自动设导入参数）

### 阶段 5 — 拼装实现（client-unity）
- [ ] `Assets/Resources/Prefab/UI/Settings.prefab` 用 unity-skills MCP 按 layout 建 + 贴阶段 4 素材
- [ ] 重写 `Assets/Scripts/Modules/UI/SettingsForm.cs`（保持 `Tattoo.UI.SettingsForm` 类名 / namespace 不变，走新结构）
- [ ] 保证 `Settings.prefab` PrefabPath / MainMenu / PauseMenu 引用不破
- [ ] **用户确认编译通过 + Prefab 层级与 layout 一致** ← 停下来

### 阶段 6 — 联调微调（LOOP）
- [ ] 打开 Editor → PlayMode → SettingsForm 打开
- [ ] 截图 vs `art/mockups/SettingsForm.png` 并排对比
- [ ] 3 项手测：拖 BGM/SFX 即时（看 Preview 日志） / 切三档画质 / 取消回滚
- [ ] Console `type=Error==0`
- [ ] 偏差列表 → 修 Prefab/代码 → 再跑
- [ ] loop 退出条件：3 项手测 PASS + 0 Error（连续 1 轮）
- [ ] tests/results.md 记录结果

## 归档

- [ ] uloop-run-tests EditMode/Settings 全绿
- [ ] `openspec validate 10-settings-form --strict`
- [ ] `openspec archive 10-settings-form --yes`
- [ ] 同步 `项目知识库（AI自行维护）/INDEX.md`
