# fix-broken-prefabs Specification

## Purpose
TBD - created by archiving change 13-fix-broken-prefabs. Update Purpose after archive.
## Requirements
### Requirement: 4 个问题 Prefab MUST 与权威 mockup 视觉层级一致

`Settings` / `SelfTattoo` / `ThreeChoice` / `TattooEnchant` 4 个 Prefab 在 PlayMode 下运行截图，MUST 与各自权威 mockup（详见 design.md §1）在「视觉分组、信息层级、文案、Sprite」四维上一致。像素级对齐 SHALL NOT 作为验收门槛，但分组与层级偏差 MUST 全部修复。

#### Scenario: Settings 表单运行时与 SettingsForm.png 一致

- **GIVEN** Unity Editor 进 PlayMode 并打开 `Settings.prefab`
- **WHEN** 抓运行时截图与 `openspec/changes/10-settings-form/art/mockups/SettingsForm.png` 对比
- **THEN** 所有可见中文 MUST 无 `�` 字符
- **AND** 所有 `Image` 组件 MUST 显示正确 Sprite（无紫色 / 无空白）
- **AND** 顶部分类 Tab / 左侧选项列表 / 右侧调节区 三大分组 MUST 存在且布局与 mockup 一致

#### Scenario: SelfTattoo 表单运行时与 SelfTattooForm.png 一致

- **GIVEN** PlayMode 打开 `SelfTattoo.prefab`
- **WHEN** 抓截图与 `archive/2026-06-29-12-core-ui-screens/art/mockups/SelfTattooForm.png` 对比
- **THEN** 文案 / Sprite / 分组 MUST 与 mockup 一致

#### Scenario: ThreeChoice 表单运行时与 ThreeChoiceForm.png 一致

- **GIVEN** PlayMode 打开 `ThreeChoice.prefab`
- **WHEN** 抓截图与 `archive/2026-06-29-12-core-ui-screens/art/mockups/ThreeChoiceForm.png` 对比
- **THEN** 三选一卡片布局 / 文案 / Sprite MUST 与 mockup 一致

#### Scenario: TattooEnchant 表单运行时与 TattooEnchantForm.png 一致

- **GIVEN** PlayMode 打开 `TattooEnchant.prefab`
- **WHEN** 抓截图与 `archive/2026-06-29-12-core-ui-screens/art/mockups/TattooEnchantForm.png` 对比
- **THEN** 附魔流程区 / 资源消耗区 / 文案 MUST 与 mockup 一致

### Requirement: Prefab 文本字段 MUST 不再含 `�` REPLACEMENT 字符

4 个 Prefab 的 YAML 中所有 TMP Text / Text 字段 MUST 写正确 UTF-8 中文，不再含 `U+FFFD`（`�`）字符。修复 MUST 通过直接 Edit Prefab YAML 完成，MUST NOT 再次走 `unity-skills` MCP（该 MCP 编码 bug 未修，再走会重新写坏）。

#### Scenario: 4 个 Prefab grep `�` 全部返回 0 匹配

- **GIVEN** 4 个 Prefab 文本字段修复完成
- **WHEN** 在 Settings / SelfTattoo / ThreeChoice / TattooEnchant 4 个 `.prefab` 文件中 grep `�`（YAML 转义形式）
- **THEN** 结果 MUST 为 0 匹配

### Requirement: Prefab Image/RawImage 组件 MUST 全部绑定有效 Sprite GUID

4 个 Prefab 中所有 `Image` / `RawImage` 组件的 `m_Sprite` 字段 MUST 指向有效的 GUID + fileID 组合，MUST NOT 留 `{fileID: 0}` 空引用。Sprite 引用 MUST 来自 `Assets/Resources/Sprite/UI/<PageName>Form/` 子目录下的素材，MUST NOT 跨 PageName 引用。

#### Scenario: Settings.prefab 37 处 Sprite 全部绑定

- **GIVEN** Settings.prefab 修复完成
- **WHEN** grep `m_Sprite: {fileID: 0}` 在该 Prefab YAML 中
- **THEN** 结果 MUST 为 0 匹配
- **AND** 所有绑定的 GUID MUST 在 `Assets/Resources/Sprite/UI/Settings/` 下能找到对应 `.png.meta`

### Requirement: 修复过程 MUST NOT 修改其它 6 个验收通过的 Prefab

修复 MUST 仅触碰 4 个问题 Prefab 文件。`CharacterSelect.prefab` / `CombatHUD.prefab` / `MainMenu.prefab` / `PauseMenu.prefab` / `RunResult.prefab` / `Shop.prefab` / `TattooStudio.prefab` MUST 在本期变更范围之外。验证以**本 session 文件 mtime** 为准（其它 6 个 Prefab mtime MUST 早于本 session 开始时间），不以 `git diff` 为准（因 12-core-ui-screens 历史已留下未 commit 的 working tree 修改）。

#### Scenario: 其它 6 个 Prefab mtime 早于本 session 修复时间

- **GIVEN** 13-fix-broken-prefabs 全部任务完成
- **WHEN** 比较 7 个非目标 Prefab（含 TattooStudio）与 4 个目标 Prefab 的最后修改时间
- **THEN** 7 个非目标 Prefab mtime MUST 早于 4 个目标 Prefab mtime
- **AND** 7 个非目标 Prefab grep `�` 计数 MUST 全 0（证明非本期工作产物）

### Requirement: MCP 编码 bug spike MUST 在 30 分钟时间盒内产出报告

`unity-skills` MCP（`http://localhost:8091/`）中文写入编码 bug 调查 MUST 在 30 分钟时间盒内完成，产出 `art/raw/mcp-spike-report.md` 报告。报告 MUST 包含根因猜测 + 验证路径 + 「本期是否顺手修」决策。本期 MUST NOT 实际修 MCP（除非根因极简单且 spike 已包含修复）。

#### Scenario: spike 时间到点产出报告

- **GIVEN** spike 开始计时
- **WHEN** 30 分钟到点
- **THEN** `openspec/changes/13-fix-broken-prefabs/art/raw/mcp-spike-report.md` MUST 存在
- **AND** 报告 MUST 至少包含「根因猜测」「验证路径」「本期决策」三段

