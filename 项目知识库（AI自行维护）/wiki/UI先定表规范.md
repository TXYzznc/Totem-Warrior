---
title: UI 先定表规范（先定表，再出图）
owner: art-director
created: 2026-06-24
last_updated: 2026-06-24
status: active
related_change: openspec/changes/04-ui-planning-first/
---

# UI 先定表规范

> **核心方法论**：独立游戏做 UI，第一步不是出图，而是**先定表**。AI 可以帮你提高视觉效率，但前面的结构判断、页面取舍、状态规划还是要人先定。先定表，再出图，会稳很多。

## 一、为什么必须先定表

实际进项目时最容易卡住的不是第一张图不好看，而是后面发现：**页面不全、按钮没状态、风格不能延续、开发也不知道该怎么拆**。三张表对应解决的三类痛点：

| 痛点 | 缺失的表 | 后果 |
|---|---|---|
| 页面规划缺位 | 表 A 页面清单 | 后面每加一个功能 AI 都可能画成另一套游戏 |
| 复用结构缺位 | 表 B 复用组件清单 | 按钮、标题条、资源栏、弹窗底板各画一套，风格不延续 |
| 组件状态缺位 | 表 C 组件状态表 | 图再好看开发也很难直接用——按钮无 pressed/disabled、页签无 selected/unselected、弹窗无确认/取消/关闭 |

## 二、决策摘要

| 维度 | 选择 |
|---|---|
| **门槛强度** | **全自动**：ai-art 检测到 UI 类型时主动起草三表骨架供用户审阅修订；不是强制阻塞也不是软引导 |
| **量化数字** | **不固定，跟项目走**：表 B「目标数量」由 AI 根据表 A 推算实际项目需要；不预设默认数字（不写"主按钮 4-6 个"这类硬编码） |
| **内容位置** | **内联到 ai-art 相关配置**：drawing-prompt-UI.md 顶部加「UI 出图前置：先定表（强制）」小节 + ai-art/SKILL.md 加 Step 0；不新建独立 SKILL |
| **范围边界** | 仅 UI 类型生效；CHARACTER / ICON / SCENE / COMMON 四种非 UI 类型不受影响 |

## 三、三表样例

### 表 A：页面清单（最小可用 UI）

| 页面 | 优先级 | 备注 |
|---|---|---|
| 主菜单 | 必做 | 核心入口 |
| 战斗 HUD | 必做 | 核心玩法离不开 |
| 背包 | 必做 | 物品系统 |
| 设置 | 必做 | 系统基础 |
| 结算 | 必做 | 战斗反馈 |
| 通用弹窗 | 可复用 | 确认/取消/关闭三按钮 |
| 商店 | 后补 | 非首版功能 |
| 成就 | 后补 | 非首版功能 |

### 表 B：复用组件清单（基于表 A 推算）

| 组件 | 目标数量 | 用途 / 下一步 |
|---|---|---|
| 主按钮 Primary Buttons | <按表 A 估算> | 先补 normal/pressed/disabled |
| 关闭按钮 Close Buttons | <按表 A 估算> | 弹窗必备 |
| 大面板底板 | <按表 A 估算> | 弹窗和背包 |
| 标签页 active/inactive | <按表 A 估算> | 背包/商城/任务页内切换 |
| 进度条 | <按表 A 估算> | 任务/强化/加载 |
| ...（按当前 change 实际需要继续列） | | |

### 表 C：组件状态表

| 组件 | 必备状态 | 备注 |
|---|---|---|
| 按钮 | normal / pressed / disabled | 引擎可做蒙版变色；后期美术优化时一般会二次制作 |
| 页签 | selected / unselected | 必须成套，否则切换没视觉反馈 |
| 弹窗 | 确认 / 取消 / 关闭 | 三按钮配套 |
| 输入框 | 默认 / 聚焦 / 错误 | 输入交互必备 |
| ...（按当前 change 实际需要继续列） | | |

## 四、新的 UI 美术流程

```
用户提 UI 美术需求
       ↓
ai-art 检测到 UI 类型
       ↓
✱ 主动起草三表骨架 → 写入 art/requirements.md
       ↓
明示用户「以下三表为 AI 起草骨架，请审阅修订后再进出图阶段」
       ↓
用户确认（或修订完成）
       ↓
ai-art 按 drawing-prompt-UI.md 模板生成 art/prompts.md
       ↓
codex-image-gen 实际出图到 art/raw/
```

## 五、被否定的备选

| 备选 | 否定理由 |
|---|---|
| 硬门槛—缺三表直接报错阻塞 | 过于刚性；"全自动"体验更好——AI 自己起草骨架，用户审阅修订 |
| 软引导—只提醒不主动起草 | AI 主动驱动 > 被动等用户响应 |
| 硬编码量化模板（写"主按钮 4-6 个"等） | 数量跟项目走；写死反而误导后续项目复用 |
| 升格为独立 SKILL `ui-planning` | 改动太大（SKILL_INDEX/MATRIX/3 agent frontmatter 都要改）；内联到 ai-art 相关配置更轻 |
| 全类型都走"先定表" | CHARACTER/ICON/SCENE/COMMON 出图前不需要这么多前置规划；图片方法论只针对 UI |

## 六、改动覆盖文件

| 文件 | 改动 |
|---|---|
| `.claude/skills/ai-art/SKILL.md` | 美术素材实现流程新增「Step 0：UI 类型前置 — 先定表（强制）」 |
| `.claude/skills/ai-art/references/drawing-prompt-UI.md` | 顶部加「UI 出图前置：先定表（强制）」小节 + 三表骨架模板（数量不写死） |
| `.claude/skills/ai-art/references/drawing-prompt-generator.md` | 工作流程加 UI 分支：先定表 → 用户审阅 → 才进提示词生成 |
| `.claude/CLAUDE.md` §六 | 美术素材生成意图追加 UI 先定表规范引用 |
| `项目知识库（AI自行维护）/INDEX.md` | §四追加 04；§3.4 追加 wiki |
| `openspec/changes/04-ui-planning-first/` | 本次 change 全套 artifact |

## 七、相关 openspec 变更

- [02-skill-routing-unification](../../openspec/changes/02-skill-routing-unification/) — 框架基础（硬墙白名单）
- [03-workflow-on-openspec](../../openspec/changes/03-workflow-on-openspec/) — 工作流沉淀到 openspec change 单目录树（前置）
- [04-ui-planning-first](../../openspec/changes/04-ui-planning-first/) — 本变更
  - [proposal.md](../../openspec/changes/04-ui-planning-first/proposal.md)
  - [tasks.md](../../openspec/changes/04-ui-planning-first/tasks.md)
