# Proposal — 10-settings-form

> **范围**：新增「设置面板（SettingsForm）」UI，覆盖音量调节 / 画质档位 / 按键重绑定 UI 占位 / 保存取消。
> **决策日期**：2026-06-26（v1 初版） / 2026-07-01（v2 UI 重做）
> **决策方式**：用户在 prompt 中已锁定主体方案，阶段 1 启动前完成关键决策（见 design.md §决策记录）。
> **驱动**：游戏目前无系统设置入口，玩家无法调音量、改画质。

---

## v2 阶段（2026-07-01 grill 5/5 挖透）

**驱动**：v1 已落地代码层（SettingsModule 数据 + SettingsForm.cs + Settings.prefab），但**旧 Prefab 结构混乱**、UI 制作走的是已废弃的 v2 三表 + 标注稿流程。用户裁定：**从头重做 UI 表现层**，走 CLAUDE.md §六 UI 子流程 v3 完整 6 阶段。

**保留（不重做）**：
- `Assets/Scripts/Modules/Settings/SettingsModule.cs`（数据 / 三态 / SaveModule 持久化 — 已验证）
- `Assets/Scripts/Events/SettingsEvents.cs`
- `Assets/Tests/EditMode/Settings/SettingsModuleTests.cs`
- `Assets/Resources/DataTable/UIFormConfig.json` 中 `Id=10 SettingsForm` 条目
- `MainMenuForm` / `PauseMenuForm` 「设置」按钮接口
- URP 三档 asset：`Performant / Balanced / HighFidelity`（原 proposal 说的 Low/Med/High 是历史命名偏差）

**删重做**：
- `Assets/Resources/Prefab/UI/Settings.prefab`
- `Assets/Scripts/Modules/UI/SettingsForm.cs`（类名 / namespace / Prefab 路径保持不变，用新结构原地重写）
- `openspec/changes/10-settings-form/art/{mockups,prompts.md,requirements.md,raw,annotations}`

**v2 范围**：3 大组 UI（音量 / 画质 / 重绑定 disabled 占位） + 底栏；面板 55%×75%；重绑定按钮 disabled + "即将推出"文字，交互留待未来 change。

**loop 边界**：阶段 1-5 串行推进 + 用户逐阶段确认；**阶段 6 联调启动 loop**（截图↔mockup 对比 → 修 → 再跑），直到 `playtest 3 项手测 PASS + Console type=Error==0 + EditMode 归档前跑一次全绿` 三条件满足即退出。

## 为什么做

当前框架已有：
- `InputModule`（项目硬约束：所有按键输入必须走它）
- `AudioModule`（背景音乐 + 音效双通道，目前音量固定）
- URP 渲染管线（多档 `UniversalRenderPipelineAsset` 待挂载）
- `UIModule` + `UIFormConfig` 配置驱动的表单系统

缺少把这些能力暴露给玩家的入口。本次新增一个 UI 表单 + 一个轻量 `SettingsModule` 承载持久化与运行时应用。

## 目标（DoD）

- [x] 玩家从主菜单 / 暂停菜单可打开 `SettingsForm`
- [x] 音量：两条滑动条（BGM / SFX），**拖动即时生效**
- [x] 画质：三档单选（Performant / Balanced / HighFidelity），切换即时套用 URP RP Asset
- [ ] ~~按键重绑定 3 槽（v2 砍到「未纳入 v1.0」，UI 保留 disabled 占位 + 「即将推出」文字）~~
- [x] 保存按钮：把当前值写入存档，关闭面板
- [x] 取消按钮：**回滚到打开面板时的快照**（音量恢复、画质恢复），关闭面板
- [ ] ~~重绑定冲突弹提示（随重绑定砍掉）~~
- [x] 设置持久化到 `SaveModule.Data.Settings`（PersistentDataPath）
- [x] 下次启动自动应用上次保存的设置
- [x] 编译通过 + EditMode 单元测试（`SettingsModuleTests`）

## 非目标（明确不做）

- ❌ **按键重绑定交互（v2 明确砍到 v1.0 未纳入）**，UI 画上但按钮 disabled + "即将推出"文字。原因：InputModule 尚未升级到 New Input System，`PerformInteractiveRebinding()` API 不可用。未来升级后另开 change 加回来。
- ❌ 多语言切换（Q4 决策范围外）
- ❌ 手柄 / 触屏支持（PC 键鼠优先）
- ❌ 灵敏度 / 反转轴 / 死区调节
- ❌ 画质细分参数（仅切 RP Asset 三档，不暴露阴影/抗锯齿/后处理粒度）
- ❌ 移动端适配
- ❌ 设置项搜索 / 分类多 Tab（单一 Form 一屏铺开）
- ❌ Cloud Save / Steam Cloud 同步

## 阶段拆分（UI v3 子流程 6 阶段 — 2026-07-01 重做）

> v2 阶段以 CLAUDE.md §六 UI v3 6 阶段为准，v1 的 5 阶段规划已作废。

| 阶段 | 内容 | 主导 Agent |
|---|---|---|
| **1 结构设计** | `art/prefab-layout.md`（含全局约定 + 节点树 + RectTransform 数据 + 状态清单 + 跨页复用） | art-ui（+ `unity-rect-transform` SKILL） |
| **2 效果图设计** | `art/prompts.md`：单页提示词，开头带「结构约束」段（画布尺寸 + 各节点占比） | art-ui |
| **3 效果图生成** | `art/mockups/SettingsForm.png` + 同目录生成记录（重试上限 3 轮） | codex-image-gen（主对话直调） |
| **4 素材拆分** | `art/raw/SettingsForm/*` → 搬进 `Assets/Resources/Sprite/UI/SettingsForm/` | 主对话 fan-out ui-asset-splitting |
| **5 拼装实现** | `Settings.prefab` 用 unity-skills MCP 按 layout 建 + 贴入阶段 4 素材；重写 `SettingsForm.cs` | client-unity（单线） |
| **6 联调微调（LOOP）** | 运行时截图 vs mockups 对比 + 偏差修复 → 直到 3 项手测 PASS + Console `type=Error==0` | client-unity + 用户（loop） |

前 5 阶段串行推进，每阶段产出后等用户确认；阶段 6 启动 loop 循环。

## 风险

| 风险 | 缓解 |
|---|---|
| InputModule 的 RebindingOperation API 现状未知，可能需要扩展 | 阶段 4 前由 client-unity 先做 spike，缺什么补什么并通知用户 |
| URP RP Asset 三档资产尚未创建 | 阶段 4 由 client-ta / client-unity 创建占位 RP Asset，命名固定 `URP-Low/Med/High.asset` |
| 取消回滚需要打开时快照所有可变状态 | design.md 中明确"快照模型 + 应用 + 提交"三态分离 |
| 重绑定冲突检测漏判（如同一物理键不同 modifier） | MVP 只查 Path 字符串相等，不做 modifier 组合 |

## 引用

- [.claude/CLAUDE.md §六 工作流系统](../../../.claude/CLAUDE.md) UI 制作子流程强制时序
- [.claude/skills/ai-art/references/drawing-prompt-UI.md](../../../.claude/skills/ai-art/references/drawing-prompt-UI.md) UI 出图前置三表
- [Assets/Scripts/Modules/InputModule](../../../Assets/Scripts/Modules/) InputModule 实现入口（待 client-unity 阶段 4 调研）
