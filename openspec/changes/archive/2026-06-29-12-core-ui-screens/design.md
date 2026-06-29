# Design — 12-core-ui-screens

## 一、核心界面清单重新评估

### 1.1 评估方法

对照 `项目知识库（AI自行维护）/GDD-v2/systems/13-UI与HUD.md` 已定义的 9 Form + `10-settings-form` 的 SettingsForm，逐一过一遍**完整单局游戏循环**（主菜单 → 角色选择 → 进入 Run → 战斗/NPC 交互/事件抽选 → 暂停 → 死亡或撤离 → 结算 → 返回主菜单），检查是否存在循环中用户会撞到、但当前 10 个 Form 无法承载的场景。

### 1.2 现状清单（10 个，均已有 Script + Prefab）

| # | Form | 触发场景 | 代码状态 | 是否已走效果图流程 |
|---|---|---|---|---|
| 1 | MainMenuForm | 游戏启动 / 返回大厅 | ✅ 99 行 | ❌ |
| 2 | CharacterSelectForm | 主菜单"开始" | ✅ 47 行 | ❌ |
| 3 | CombatHUDForm | 战斗中常驻 | ✅ 268 行 | ❌ |
| 4 | TattooStudioForm | 纹身师 NPC 交互 | ✅ 133 行 | ❌ |
| 5 | TattooEnchantForm | 纹身附魔选项 | ✅ 48 行 | ❌ |
| 6 | ShopForm | 商人 NPC 交互 | ✅ 133 行 | ❌ |
| 7 | ThreeChoiceForm | 宝箱三选一事件 | ✅ 60 行 | ❌ |
| 8 | PauseMenuForm | 战斗中 ESC | ✅ 136 行 | ❌ |
| 9 | RunResultForm | 单局结束 | ✅ 47 行 | ❌ |
| 10 | SettingsForm | 主菜单/暂停菜单入口 | ✅ 321 行 | 🟡 进行中（10-settings-form） |

### 1.3 遗漏页面排查结论（已修正）

> ⚠️ **更新**：首轮排查（仅对照 `13-UI与HUD.md` 的 9-Form 清单）误判为"无缺口"。复查 `systems/01-纹身构筑系统.md` §2.8/§5.1/§5.2 后发现 v2.1 新增的"玩家自纹身"机制**完全没有 UI 承载**，是真实缺口，详见下方。

**真实缺口：缺少 `SelfTattooForm`（玩家自纹身工作台）**

- **GDD 依据**：`01-纹身构筑系统.md` §2.8「自纹身读条流程」+ §5.1「自纹身界面线框」+ §5.2「读条 UI 线框」——v2.1 核心改动：纹身师不再刻字，玩家任意地点按 `Tab` 自助刻纹身（选部位/颜色/图案 → 读条 3-8s → 落实）
- **代码现状**：后端 API 已完整实现且 AI 已在用——`TattooModule.StartSelfTattoo()` / `RequestSelfTattooEvent` / `TattooFinishedEvent`（`BotControllerModule.OnRequestSelfTattoo` 已接好 AI 侧）；`HumanPlayerController.cs:60` 注释明确写"UI Form 直接 publish RequestSelfTattooEvent"——**但这个 UI Form 一直未建**，玩家当前无法触发自纹身
- **为何漏在 `13-UI与HUD.md` 9-Form 清单外**：该文档定稿于 2026-06-25，当时 `TattooStudioForm` 还兼任"NPC 刻字入口"；v2.1 后续把纹身师重定位为纯附魔工，自纹身入口移交玩家 Tab 键，但 Form 清单未同步更新
- **范围**：
  1. `SelfTattooForm`（任意地点 Tab 唤出，全屏/居中覆盖层）：部位选择（6 部位高亮）+ 颜色库存（按数量显示，0 灰显）+ 图案解锁状态（未解锁显示锁图标）+ 预览 + 风险提示（读条时长+中断惩罚）+ 开始/取消按钮
  2. 读条悬浮 UI（角色脚下圆环 + 屏幕中央进度条）：**不单独开 Form**，作为 `CombatHUDForm` 的常驻子区块实现（与 Boss HP 条同样的"条件显示"处理方式），减少一个独立 Prefab

**结论**：核心界面清单从 10 个增至 **11 个**（9 GDD Form + SettingsForm + 新增 SelfTattooForm），读条 UI 归入 CombatHUDForm 子区块不单独计数。其余候选页面逐项排查记录：

| 候选新页面 | 是否需要 | 理由 |
|---|---|---|
| 加载/转场画面（Loading Screen） | ❌ 不需要 | Unity 单场景内 Form 切换（非 Scene 异步加载），无明显加载耗时，DOTween 进退场动画已覆盖过渡感知 |
| 教程/新手引导浮层 | ❌ 本次不做 | GDD 未定义教学需求；roguelike-BR 单局 10-15 分钟，倾向环境引导而非强制教程，若后续需要走独立 change（player-onboarding SKILL） |
| 成就/Achievement 面板 | ❌ 不需要 | GDD 无成就系统设计，无对应 DataTable 和业务逻辑，本次不无依据新增 |
| 背包/装备总览面板 | ❌ 不需要 | 装备信息（纹身 Build）已在 CombatHUDForm 左侧 Sidebar + TattooStudioForm 内呈现，无需独立页面 |
| 死亡确认/复活弹窗 | ❌ 不需要 | Roguelike 单命设计，死亡直接触发 RunEndedEvent → RunResultForm，无复活机制 |
| Credits/关于页面 | ❌ 不需要 | 非 MVP 必需，无 GDD 依据 |

**结论**：清单维持 10 个 Form，**本次 change 不新增页面类型**，工作重心转向"补齐视觉 + 联调验证"。若执行过程中发现真实遗漏（如某场景确实卡住没有对应 UI 反馈），按 CLAUDE.md §五"例外打断条件"判断是否需要中断升级。

## 二、本次工作分层

### 2.1 视觉补齐（9 个 Form，SettingsForm 除外）

按 CLAUDE.md §六 UI 制作 5 阶段流程：

1. **需求设计**（已完成，见 `art/requirements.md` 三表，基于 GDD 13-UI与HUD.md 整理）
2. **效果图设计**：art-ui 撰写每个 Form 的效果图提示词（风格对齐已出图的 SettingsForm + art-director 风格指南）
3. **效果图生成**：codex-image-gen 出图，3 轮重试上限
4. **Prefab 视觉调整**：client-unity 按效果图微调现有 Prefab（间距/字号/配色/Sprite），**不改 RectTransform 层级结构和脚本逻辑**
5. **联调微调**：与下方联调验证合并执行

**分批顺序**（按玩家接触频率/重要性排序，每批确认后再继续）：
- 批次 1（最高频）：CombatHUDForm / MainMenuForm
- 批次 2（每局必经）：CharacterSelectForm / PauseMenuForm / RunResultForm
- 批次 3（条件触发）：TattooStudioForm / TattooEnchantForm / ShopForm / ThreeChoiceForm

### 2.2 完整可玩联调验证

由 client-unity + qa-engineer 跑一次端到端手动联调，覆盖：

| 检查项 | 验证点 |
|---|---|
| Form 触发链路 | 每个 Form 的触发事件（如 `NPCInteractStartEvent`）确实能从对应业务模块正确发出并被 UIModule 接住 |
| Form 关闭链路 | ESC/B 键统一关闭逻辑、`ThreeChoiceForm` 3s 防误触锁、`TattooEnchantForm` 读条期间按钮锁定 |
| Sort Order 层级 | 0（HUD）/10（覆盖层）/20（系统层）/30（全屏层）遮挡关系正确，不出现暂停菜单被 HUD 盖住等问题 |
| 跨 Form 状态同步 | 如 ShopForm 购买后 金币数 在 CombatHUDForm（如有显示）同步刷新 |
| 异常路径 | 战斗中死亡时 TattooStudioForm/ShopForm 等覆盖层是否正确强制关闭，不残留遮挡 RunResultForm |

发现的 bug 修复原则：**只修"导致 UI 显示/交互异常"的问题**；如果是数值平衡或玩法设计问题，记录到 `tests/bugs.md` 转给对应系统 owner，不在本 change 内处理。

## 三、与 06-v21-implementation / 10-settings-form 的关系

- 不重复 06-v21-implementation 已完成的 Module/Form 代码框架，仅做视觉+联调补强
- SettingsForm 视觉流程独立在 10-settings-form 跟踪；本 change 仅在联调验证表中把 SettingsForm 的打开/关闭链路一并验证（确保从 MainMenu/PauseMenu 两个入口都能正确打开）
