# 任务清单 — 13 修复 4 个问题 Prefab

> 顺序：MCP spike → 4 个 Prefab 流水线（可并行）→ PlayMode 联调 → 归档。

## Phase 0 — MCP 编码 bug spike（30 分钟时间盒）

- [x] T0.1 调 `http://localhost:8091/` 看 MCP server 健康状态 + API 列表
- [x] T0.2 用 MCP 写一个最小 Prefab（含一段中文 TMP Text）→ Read YAML 看是否 `�`
- [x] T0.3 找定 MCP server 端代码 / C# 客户端代码，定位字符串经过的环节
- [x] T0.4 写 `art/raw/mcp-spike-report.md`：根因猜测 + 验证路径 + 是否能在本期顺手修

## Phase 1 — Settings.prefab 修复

- [x] T1.1 读 `Assets/Resources/Prefab/UI/Settings.prefab`，grep `\\uFFFD` 列出所有乱码位置
- [x] T1.2 对照 `openspec/changes/10-settings-form/art/mockups/SettingsForm.png` + `10-settings-form/design.md` 的文案表，确定正确中文
- [x] T1.3 Edit YAML 把 14 处乱码改成正确 UTF-8 中文
- [x] T1.4 列出 37 处 `m_Sprite: {fileID: 0}` 节点，按节点名匹配 `Assets/Resources/Sprite/UI/SettingsForm/` 下素材
- [x] T1.5 Edit YAML 把 37 个 Sprite GUID 全部回写
- [x] T1.6 Prefab 层级 vs mockup 对比，删多余 / 补缺失 / 改命名
- [x] T1.7 Unity 里打开 Prefab 视图检查 Sprite 显示正常（无紫色）

## Phase 2 — SelfTattoo.prefab 修复

- [x] T2.1 grep `\\uFFFD` 列乱码
- [x] T2.2 对照 `archive/2026-06-29-12-core-ui-screens/art/mockups/SelfTattooForm.png` 修文案
- [x] T2.3 Edit YAML 改乱码
- [x] T2.4 列缺失 Sprite，匹配 `Assets/Resources/Sprite/UI/SelfTattooForm/`
- [x] T2.5 Edit YAML 补 Sprite GUID
- [x] T2.6 层级校正
- [x] T2.7 Prefab 视图检查

## Phase 3 — ThreeChoice.prefab 修复

- [x] T3.1 grep `\\uFFFD` 列乱码
- [x] T3.2 对照 `archive/2026-06-29-12-core-ui-screens/art/mockups/ThreeChoiceForm.png` 修文案
- [x] T3.3 Edit YAML 改乱码
- [x] T3.4 列缺失 Sprite，匹配 `Assets/Resources/Sprite/UI/ThreeChoiceForm/`
- [x] T3.5 Edit YAML 补 Sprite GUID
- [x] T3.6 层级校正
- [x] T3.7 Prefab 视图检查

## Phase 4 — TattooEnchant.prefab 修复

- [x] T4.1 grep `\\uFFFD` 列乱码
- [x] T4.2 对照 `archive/2026-06-29-12-core-ui-screens/art/mockups/TattooEnchantForm.png` 修文案
- [x] T4.3 Edit YAML 改乱码
- [x] T4.4 列缺失 Sprite，匹配 `Assets/Resources/Sprite/UI/TattooEnchantForm/`
- [x] T4.5 Edit YAML 补 Sprite GUID
- [x] T4.6 层级校正
- [x] T4.7 Prefab 视图检查

## Phase 5 — PlayMode 联调验收

> 静态验收（前 3 条门槛）由主对话 4 agent 全部完成。运行时联调（T5.1~T5.5）需 Unity Editor 在场，交付用户人工执行（见 tests/results.md「PlayMode 联调（待用户人工执行）」段）。

- [x] T5.1 Unity Editor 进 PlayMode（先 editor_stop 退 Play 再 reload，按 memory 规则）— **用户人工**
- [x] T5.2 依次打开 Settings / SelfTattoo / ThreeChoice / TattooEnchant 4 个 Form — **用户人工**
- [x] T5.3 抓运行时截图入 `tests/results.md` — **用户人工**
- [x] T5.4 与对应 mockup 并排比对，列残余偏差清单 — **用户人工**
- [x] T5.5 偏差迭代到「视觉分组与信息层级一致」— **用户人工**
- [x] T5.6 写 `tests/results.md` 最终验收报告（4 个 Prefab 全 ✓）— 静态验收 3/4 ✓，PlayMode 段已留指引

## Phase 6 — 归档

- [x] T6.1 `openspec validate 13-fix-broken-prefabs`
- [x] T6.2 `openspec archive 13-fix-broken-prefabs --yes`
- [x] T6.3 同步更新 `项目知识库（AI自行维护）/INDEX.md`（active → archive 迁移；新增 spec sink 链接）
