# Proposal — 12-core-ui-screens

> **范围**：审视游戏全部核心界面清单，对已有 10 个 Form（9 GDD Form + SettingsForm）补齐效果图、修复联调问题，确认无遗漏页面。
> **决策日期**：2026-06-28
> **决策方式**：用户 grill-me 阶段 A 三轮反问收敛（目标 / 范围边界 / 验收标准 / 约束）
> **前置变更**：06-v21-implementation（Form 代码框架）/ 10-settings-form（首个走效果图流程的 Form，进行中）

---

## 为什么做

调研发现：

1. **代码已基本到位**：`CombatHUDForm` / `MainMenuForm` / `PauseMenuForm` / `RunResultForm` / `CharacterSelectForm` / `TattooStudioForm` / `TattooEnchantForm` / `ShopForm` / `ThreeChoiceForm` / `SettingsForm` 共 10 个 Form，脚本与 Prefab 均已存在（47~321 行不等，0 个 TODO）。
2. **视觉流程缺位**：除 `10-settings-form` 外，其余 9 个 Form 从未走过 CLAUDE.md §六「UI 制作子流程」的「效果图设计 → 效果图生成 → 联调微调」三阶段——目前是程序员摆位（UGUI 默认样式），未经美术确认。
3. **联调未做完整验证**：`06-v21-implementation` 的 PlayMode 测试仍在进行（EditMode 154/155 通过），10 个 Form 之间的触发/关闭时序（如 ESC/B 键、3s 防误触锁、Sort Order 层级遮挡）未做过端到端联调。

因此本次 **不是从零设计新界面**，而是：
- 重新审视 GDD 9 Form + SettingsForm 清单是否覆盖完整游戏循环（结论见下）
- 给 9 个尚未走效果图流程的 Form 补齐视觉
- 做一次完整可玩的端到端联调，修复发现的问题

## 目标（DoD）

- [ ] 确认核心界面清单（见 design.md §一），如有遗漏页面在此阶段提出
- [ ] 9 个旧 Form（不含已在 10-settings-form 进行中的 SettingsForm）补齐 `art/mockups/` 效果图并经用户确认
- [ ] 10 个 Form 的 Prefab 视觉与效果图对齐（间距/字号/配色）
- [ ] 完整跑通一局游戏（主菜单 → 角色选择 → 战斗 HUD → 纹身师/商人/三选一交互 → 暂停 → 结算 → 返回主菜单），无阻断性 bug
- [ ] 联调中发现的背后逻辑缺口（如某 Module 接口缺失）随手补齐，不开新 change

## 非目标

- ❌ 不重写已有 Form 的代码架构（IUIForm 接口、事件订阅方式不变）
- ❌ 不引入 MVVM 框架或重构 UIModule 加载机制
- ❌ 不做成就面板 / 排行榜 / 教程引导等 GDD 未定义的新系统（单人 roguelike-BR，无此类需求）
- ❌ 不等待 `10-settings-form` 完全归档才开始（两个 change 并行，SettingsForm 视觉已有 mockups，本次只做联调接入）

## 决策摘要（grill-me 阶段 A 出口）

| 决策点 | 选择 | 理由 |
|---|---|---|
| 清单评估范围 | 允许新增，但需在设计阶段给出依据 | 不预设"清单一定对"，但避免无依据扩需求 |
| 背后逻辑范围 | 联调中发现缺口才补，不预先重做业务逻辑 | 06-v21-implementation 已实现核心 Module，本次只管 UI 层 + 联调缝隙 |
| 验收标准 | 完整可玩联调（端到端无阻断 bug + 视觉与效果图一致） | 用户明确要求"完整可玩"而非"能打开就行" |
| 生产流程约束 | 沿用现有 CLAUDE.md §六 UI 5 阶段流程（效果图 3 轮重试上限等） | 无特殊要求，按现有规范推进 |

## 文件变更清单

| 文件/目录 | 改动 |
|---|---|
| `openspec/changes/12-core-ui-screens/design.md` | **新建**：核心界面清单评估结论 |
| `openspec/changes/12-core-ui-screens/art/requirements.md` | **新建**：三表（页面清单/复用组件清单/组件状态表），基于 GDD 13-UI与HUD.md 整理 |
| `openspec/changes/12-core-ui-screens/art/prompts.md` | 待阶段 2：art-ui 撰写 9 个 Form 效果图提示词 |
| `openspec/changes/12-core-ui-screens/art/mockups/*.png` | 待阶段 3：codex-image-gen 出图 |
| `Assets/Resources/Prefab/UI/*.prefab` | 阶段 4-5：按效果图调整视觉（间距/字号/配色），不改层级结构 |
| `Assets/Scripts/Modules/**/UI/*Form.cs` | 联调中发现 bug 时修复，不做架构改动 |

## 风险与回滚

| 风险 | 缓解 |
|---|---|
| 9 个 Form 一次性出图，codex-image-gen 调用量大 | 按优先级分批（HUD/MainMenu 先做，覆盖层面板后做），每批确认后再继续 |
| 联调发现 Module 接口缺口导致范围蔓延 | 仅修复"导致 UI 显示/交互异常"的缺口；纯数值/玩法问题转给对应系统 owner，不在本 change 内处理 |
| 效果图与现有 Prefab 结构冲突（如要求新增容器） | 优先调整效果图提示词适配现有层级，确需改层级再评估 |

回滚：`git revert <本次 commit>`，Prefab/脚本改动均为视觉微调，可单独回退。
