# Proposal — 04-ui-planning-first

> **范围**：把"独立游戏做 UI，第一步不是出图，而是先定表"的方法论固化进 ai-art 流程——仅 UI 类型素材生效，CHARACTER/ICON/SCENE/COMMON 不受影响。
> **决策日期**：2026-06-24
> **前置变更**：02-skill-routing-unification + 03-workflow-on-openspec（新工作流目录树）
> **决策方式**：用户分享的 7 张图（独立游戏 UI 工作流图文）→ grill-me 弹窗 3 轮反问收敛→「全自动 + 不固定数字 + 内联到配置」
> **预计阶段**：1 阶段（文档与 SKILL 流程改造，零代码）

---

## Why

当前 ai-art 流程默认"用户已经知道要画啥"——拿到需求直接路由到 `drawing-prompt-{TYPE}` 出提示词。但对**独立游戏 UI** 场景这是错的，原因：

1. **页面规划缺位**：用户经常没列「最小可用 UI 清单」就要 AI 出图，导致后面页面不全/风格不延续/开发不知怎么拆
2. **复用结构缺位**：没有"按钮/标题条/资源栏/弹窗底板/图标框"等复用元素的预先规划，每新增功能 AI 都可能画成另一套
3. **组件状态缺位**：UI 最容易漏的不是大界面而是状态——按钮 normal/pressed/disabled、页签 selected/unselected、弹窗 确认/取消/关闭，少了这些图再好看开发也很难直接用
4. **量化感缺失**：没有"主按钮 4-6 个、关闭按钮 2-3 个"这种数量级感知，用户拍脑袋报数

参考用户提供的方法论：
> 在让 AI 出图前，最好先准备三样东西：**页面清单、功能优先级、组件状态表**。先定表，再出图，会稳很多。

## 目标（DoD）

- ✅ ai-art 在处理 UI 类型素材时，**自动**起草「页面清单 / 复用组件清单 / 组件状态表」三表骨架，写入 `art/requirements.md`，提交用户审阅修订
- ✅ 用户修订完成（或确认骨架）后，才进入 `prompts.md` 提示词生成阶段
- ✅ 三表的数量字段（每类组件几个）**不写死**，AI 根据当前 change 的页面清单实际推算
- ✅ 修改 `drawing-prompt-UI.md`、`drawing-prompt-generator.md`、`ai-art/SKILL.md`、`CLAUDE.md §六`、`项目知识库/wiki` 五处，保证 UI 美术规范全链路一致
- ✅ CHARACTER / ICON / SCENE / COMMON 四种非 UI 类型完全不受影响

## 非目标

- ❌ 不引入硬编码量化模板（如"必须 6 个主按钮"）
- ❌ 不新建独立 SKILL（不做 ui-planning SKILL）
- ❌ 不动 codex-image-gen（后置出图流程不变）
- ❌ 不强制 UI 之外的类型走"先定表"流程

## 用户决策摘要（grill-me 阶段 A 出口）

| 决策点 | 选择 | 理由 |
|---|---|---|
| 门槛强度 | **全自动起草三表骨架** | 体验最好；AI 主动驱动而不是等用户被动响应 |
| 量化清单 | **不固定，跟项目走** | 不预设默认数字；AI 根据页面清单实际推算 |
| 内容位置 | **内联到相关配置** | 不新建独立 SKILL；改 drawing-prompt-UI.md + ai-art/SKILL.md 即可 |

## 三表骨架（仅形态约束，数量字段由 AI 推算）

### 表 A：页面清单（最小可用 UI）
| 页面 | 优先级 | 备注 |
|---|---|---|
| <页面名> | 必做 / 可复用 / 后补 | <为什么这个优先级> |

### 表 B：复用组件清单（基于表 A 推算）
| 组件 | 目标数量 | 用途 / 下一步 |
|---|---|---|
| <组件名> | <AI 根据表 A 估算> | <用在哪些页面> |

### 表 C：组件状态表
| 组件 | 必备状态 | 备注 |
|---|---|---|
| 按钮 | normal / pressed / disabled | 引擎可做蒙版变色 |
| 页签 | selected / unselected | 必须成套 |
| 弹窗 | 确认 / 取消 / 关闭 | 三按钮配套 |

## What Changes

| 文件 | 改动 |
|---|---|
| `.claude/skills/ai-art/SKILL.md` | 美术素材实现流程新增「Step 0：UI 类型前置 — 先定表」 |
| `.claude/skills/ai-art/references/drawing-prompt-UI.md` | 加「UI 出图前置：先定表（强制）」小节 + 三表骨架模板（不写死数字） |
| `.claude/skills/ai-art/references/drawing-prompt-generator.md` | 工作流程加 UI 类型分支：先定表 → 用户审阅 → 再进提示词 |
| `.claude/CLAUDE.md` §六 「美术素材生成意图」 | 追加 UI 类型先定表规范引用 |
| `项目知识库（AI自行维护）/INDEX.md` | §四追加 04；§3.4 美术追加 wiki 链接 |
| `项目知识库（AI自行维护）/wiki/UI先定表规范.md` | **新建**：决策摘要 + 三表样例 + 被否定备选 |
| `openspec/changes/04-ui-planning-first/` | **新建**（本目录） |

## 风险与回滚

| 风险 | 缓解 |
|---|---|
| AI 起草的三表骨架质量差，用户嫌弃 | 三表为"骨架"非"完成"，明示用户必须审阅修订；预留迭代空间 |
| 用户做小 UI（如单页/单组件）也被迫走三表流程 | "全自动"是 AI 主动建议；用户可明确说"跳过先定表"快速通道 |
| 三表写完后 prompts 阶段还按旧流程 → 没真正用上 | drawing-prompt-UI.md 提示词阶段也引用三表内容作上下文 |

回滚：`git revert <本次 commit>` 即可。

## 引用

- 03-workflow-on-openspec — 前置（新 art/ 子目录树）
- [.claude/skills/ai-art/SKILL.md](../../../.claude/skills/ai-art/SKILL.md) — 主入口
- [.claude/skills/ai-art/references/drawing-prompt-UI.md](../../../.claude/skills/ai-art/references/drawing-prompt-UI.md) — UI 类型模板
