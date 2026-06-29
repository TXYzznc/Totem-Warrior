# Proposal — 13-ui-screens-complete

> **范围**：GDD v2.1 新增机制"玩家自纹身"的 UI 承载（SelfTattooForm）+ 原有 10 个 Form 的完整视觉补齐 + 端到端联调验收。
> 
> **决策日期**：2026-06-29
> 
> **决策方式**：用户 grill-me 阶段 A 收敛（5 / 5 条全透）
> 
> **前置变更**：06-v21-implementation（Form 代码框架）/ 10-settings-form（首个走效果图流程的 Form，进行中）/ 12-core-ui-screens（原始需求，范围扩展）

---

## 为什么做

1. **GDD v2.1 核心机制缺 UI**：玩家自纹身（Tab 键唤出、选部位/颜色/图案 → 读条 3-8s）是本版的主要差异化，代码 API 完整（`TattooModule.StartSelfTattoo` / `RequestSelfTattooEvent`），但 UI Form 一直未建
2. **9 个旧 Form 视觉缺失**：除 SettingsForm 外，其余 9 个 Form 从未走过"效果图设计 → 生成 → 联调"流程，目前是程序员摆位默认样式
3. **完整联调未验证**：10 个 Form 之间的触发链路、关闭逻辑、Sort Order 遮挡关系等无法确保一局游戏完整可玩
4. **核心目标**：完成 **11 个 Form 的视觉补齐 + 端到端无阻断联调**，使游戏循环（主菜单 → 角色选 → 战斗 → NPC/自纹身 → 暂停 → 结算 → 返回）完整可玩

---

## 目标（DoD）

- [ ] **SelfTattooForm 新建**：部位选择（6 部位高亮）+ 颜色库存显示 + 图案解锁状态 + 预览 + 开始/取消按钮
- [ ] **读条 UI（子区块）**：角色脚下圆环 + 屏幕中央进度条，归入 CombatHUDForm 的条件显示逻辑
- [ ] **9 个旧 Form 视觉补齐**：按完整 5 阶段流程（效果图设计 → 生成 → Prefab 微调），每个 Form 最多 3 轮重试
- [ ] **完整可玩联调**：跑通一局（主菜单 → 战斗 → NPC/自纹身交互 → 暂停 → 死亡/撤离 → 结算 → 返回），无阻断性 bug，Form 触发链路、关闭逻辑、Sort Order 验证
- [ ] **业务逻辑缺口补齐**：联调中若发现 Module 接口缺失或事件链路不通，仅补"导致 UI 显示/交互异常"的缺口，不主动重做 Module

---

## 非目标

- ❌ 不重写 Form 代码架构（IUIForm 接口、事件订阅方式不变）
- ❌ 不引入新 UI 框架或重构 UIModule 加载机制
- ❌ 不补齐"纯数值/玩法问题"的业务逻辑（如怪物伤害不对、金币掉落不对），这些转给对应系统 owner，记录到 `tests/bugs.md`
- ❌ 不补齐非阻断性 bug（如美术资源缺失、声音贴不上），defer 到后续优化

---

## 决策摘要（阶段 A 共识）

| 决策点 | 选择 | 理由 |
|---|---|---|
| SelfTattooForm 范围 | **包含** | GDD v2.1 核心机制，代码 API 完整，仅缺 UI，本次一并补齐确保"完整可玩" |
| 效果图流程 | **完整 5 阶段** | 按 CLAUDE.md §六规范，每个 Form 最多 3 轮重试，确保视觉一致性 |
| 验收标准 | **完整可玩联调** | 不仅"Form 能打开"，而是一局游戏从头到尾无阻断 bug，视觉与效果图一致 |
| 业务逻辑补齐范围 | **仅 UI 阻断缺口** | 联调中发现缺口才补，不预先重做 Module；纯数值问题记录转给 owner |
| 时间约束 | **紧凑周期 3-5 天** | 效果图 3 轮重试上限；超限则停下通知决策 |
| 核心目标补充 | **完整交互 + 日志占位** | 本版核心是交互完整可玩，效果部分（美术素材、音效）可用日志占位，后续版本实现 |

---

## 工作分层

### 阶段 1：需求设计（已完成）
- 三表（页面清单 / 复用组件清单 / 组件状态表）基于 GDD 13-UI与HUD.md 整理
- SelfTattooForm 界面需求文档

### 阶段 2：效果图设计（art-ui 负责）
- 11 个 Form 的效果图提示词（含 SelfTattooForm）
- 风格对齐已出图的 SettingsForm + art-director 风格指南

### 阶段 3：效果图生成（主对话 → codex-image-gen）
- 批量生成 mockups，每个 Form 最多 3 轮重试
- 分批顺序：HUD / MainMenu 先 → 每局必经 → 条件触发 → SelfTattooForm

### 阶段 4：Prefab 视觉调整（client-unity 负责）
- 按效果图微调现有 Prefab（间距 / 字号 / 配色 / Sprite）
- **不改 RectTransform 层级结构和脚本逻辑**
- SelfTattooForm：新建 Prefab + 脚本（参考 TattooStudioForm 框架）

### 阶段 5：联调验收（client-unity + qa-engineer 负责）
- 完整一局游戏流程验证（主菜单 → 战斗 → NPC → 暂停 → 结算）
- Form 触发链路、关闭逻辑、Sort Order 验证
- 阻断性 bug 修复，非阻断性问题记录转给 owner

---

## 与 10-settings-form / 12-core-ui-screens 的关系

- **10-settings-form**：独立进行，本 change 在联调验证表中只验证 SettingsForm 的打开/关闭链路（确保从 MainMenu/PauseMenu 两个入口都能正确打开）
- **12-core-ui-screens**：本 change 是其扩展版本，增加了 SelfTattooForm 新建 + 核心目标调整为"完整交互 + 日志占位"

---

## 风险与回滚

| 风险 | 缓解 |
|---|---|
| 11 个 Form 一次性出图，codex-image-gen 调用量大 | 按优先级分批生成，每批确认后再继续；3 轮重试上限自动卡 |
| SelfTattooForm 界面与现有 Form 层级冲突 | 参考 ShopForm / TattooStudioForm 的全屏覆盖模式设计，Sort Order 与 ThreeChoiceForm 同层（20） |
| 联调发现 Module 接口大量缺失 | 仅修复"导致 UI 显示/交互异常"的缺口；若牵涉系统性重做，停下通知用户决策 |

回滚：`git revert <本次 commit>`，Prefab/脚本改动均为视觉微调 + 新增 Form，可单独回退。

