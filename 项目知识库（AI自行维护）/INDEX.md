# 项目知识库索引

> AI 自行维护的知识库入口。**任何 lead / system 层 agent 在做决策前必须先查阅本索引**。
>
> 本知识库与 `openspec/changes/` 一站式工作流配套：每次开发都会沉淀新条目。

---

## 一、目录结构

| 目录 | 说明 | 谁来写 |
|---|---|---|
| `outputs/` | AI 工作时输出的临时文件（草稿、中间结论） | AI 自动写 |
| `raw/` | 用户筛选并保留的原始资料（访谈、调研、PRD 草稿、初版 GDD） | 用户筛选后归档 |
| `raw/初版GDD-2026-06/` | 项目立项阶段产出的初版 GDD（9 份，约 95KB） | 已归档 |
| `wiki/` | AI 整理后的结构化知识维基（按系统分类） | AI 整理，用户校对 |

## 二、工作流

```
AI 输出 → outputs/ → 用户筛选 → raw/ → 用户指令 → AI 整理到 wiki/ → 更新本 INDEX.md
```

**规则**：

- AI **不要直接修改 `raw/`** 中的文件——那是用户认证后的原始素材。
- `wiki/` 由 AI 维护，但每次提交前回扫一次链接有效性。
- `outputs/` 中的文件是临时产物，定期清理。
- 大型决策结束后（grill-me → openspec → 落地完成），**必须**把决策摘要、关键 trade-off、被否定的备选方案写入 `wiki/`。

---

## 三、Wiki 目录（按系统）

### 3.1 设计层（owner: gd-lead / gd-system）

- **[GDD v2.1 全套设计文档（2026-06-25 升级）](GDD-v2/00-总策划案v2.md)** — 2026-06-25 — Phase A grill-me 5 条挖透 + **v2.1 grill 16 轮 24 项修订** 后产出。1 份总策划案 + 15 份系统 GDD（[systems/](GDD-v2/systems/)）+ 16 份模块详设（[modules/](GDD-v2/modules/)）+ 1 份全局契约（[CONTRACT.md](../openspec/changes/05-gdd-v2-full-design-docs/CONTRACT.md) 含 50+ 事件签名，v2.1 追加 5 个新事件）。**核心决策**：UGUI 轻量 MVP / 纹身师=词缀附魔工 / 玩家自纹身读条 3-8s / 死亡宝箱半颜料半配方拓本 / 10-15min 单局 / 2 技能槽 / 20+29+1 AI 配比 / Hades 精致 2.5D / 异能者身份 / 颜料三档 / 配方 4 来源 / 伪联机起步未来真联机零改动。详见 [openspec/changes/05-gdd-v2-full-design-docs/](../openspec/changes/05-gdd-v2-full-design-docs/)。
- **[初版 GDD 设计文档（2026-06 baseline）](wiki/初版GDD设计文档.md)** — 2026-06-25 — 项目立项阶段（2026-06-22~23）的初版 GDD，9 份文档涵盖玩法定位（Roguelike + BR）、纹身构筑系统（6×7×8 三层正交）、世界观（多元末日 + 实验体）、联机三层架构、技术选型。**已被 GDD v2 全面继承+扩展**。原始素材见 [raw/初版GDD-2026-06/](raw/初版GDD-2026-06/)。已派生 [01-tattoo-framework-rewrite](../openspec/changes/01-tattoo-framework-rewrite/) 落地。

### 3.2 客户端架构（owner: client-lead）

- **[Tattoo 系统重构（v1.0 → v2.0 框架化）](wiki/Tattoo系统重构.md)** — 2026-06-24 — 把原 Tattoo 业务（21 SO 子类 + Composer + CombatRunner + IMGUI）整体重写为 IGameModule 框架实现。详见 [openspec/changes/01-tattoo-framework-rewrite/](../openspec/changes/01-tattoo-framework-rewrite/)

### 3.3 服务端 / 网络（owner: net-lead）

- _尚无条目（项目当前为单机原型）_

### 3.4 美术（owner: art-director）

- **[UI 先定表规范](wiki/UI先定表规范.md)** — 2026-06-24 — 独立游戏 UI 第一步不是出图，而是先定「页面清单 / 复用组件清单 / 组件状态表」三表。ai-art 自动起草供用户审阅。详见 [openspec/changes/04-ui-planning-first/](../openspec/changes/04-ui-planning-first/)

### 3.5 工具链 / DevOps（owner: tools-engineer / devops-engineer）

- **[SKILL 路由系统统一（硬墙白名单 + gitnexus 清除）](wiki/SKILL路由统一.md)** — 2026-06-24 — 消除 `.claude/` 下两套 SKILL 路由机制的语义冲突；清除 gitnexus；19 个 agent 措辞统一为硬墙；7 个 agent 加共享 SKILL。详见 [openspec/changes/02-skill-routing-unification/](../openspec/changes/02-skill-routing-unification/)
- **[工作流迁移到 openspec/changes/ 一站式目录](wiki/工作流迁移.md)** — 2026-06-24 — 删除「工作/」整个目录，5 Phase 沉淀到 openspec change：proposal/design/tasks/specs（原生）+ brainstorm.md + CONTRACT.md + art/ + tests/。详见 [openspec/changes/03-workflow-on-openspec/](../openspec/changes/03-workflow-on-openspec/)
- **[Codex 批量出图协议（L1+L2）](wiki/Codex批量出图协议.md)** — 2026-06-25 — codex-image-gen SKILL 从「每张图一次 codex exec」改造为「L1 进程内并行（≤12 张/批）+ L2 合并画布（≤256×256 透明 icon 合并到 1024×1024 后 ImageCut 切割）」。9 张 demo 实测：imagegen 调用 9→1，节省 88.9%；tokens 节省 ~81%。详见 [openspec/changes/05-codex-batch-art-protocol/](../openspec/changes/05-codex-batch-art-protocol/)

### 3.6 已废弃 / 历史决策（owner: producer）

- _尚无条目_

---

## 四、当前活跃的 OpenSpec 变更

| ID | 标题 | 阶段 | 负责 agents |
|---|---|---|---|
| **01-tattoo-framework-rewrite** | Tattoo 业务整体迁移到 IGameModule 框架 | Phase A 进行中（机械动作已完成） | client-lead / client-unity / gd-system / art-ui / qa-engineer |
| **06-v21-implementation** | v2.1 GDD 全套落地：30+ 模块新增/重构 + 9 UGUI Form + 91 张美术 + 11 EditMode 测试 + DOTween Pro asmdef 接线 | 编译✅ + EditMode 154/155 通过（99.4%）。PlayMode/帧率/归档进行中 | 主对话 orchestrator + 12 子 agent（骨架先行模式） |
| **07-main-menu-flow** | 启动场景拆出 MainMenu.unity（不挂 GameApp），点开始游戏切 Launch；UIModule 改为 OnGameReady 动态加载 9 Form Prefab | proposal 已完成，PlayMode 实测走通（0 异常） | 主对话（client-unity 风格） |
| **10-settings-form** | SettingsForm 走完整 5 阶段视觉流程（效果图设计 → 生成 → 联调），展示 MVP UI 制作完整链路 | Phase 3 进行中（效果图生成中） | art-ui / codex-image-gen / client-unity / qa-engineer |

> 已归档：
> - 05-codex-batch-art-protocol → `openspec/changes/archive/2026-06-25-05-codex-batch-art-protocol/`，详见 [wiki 条目](wiki/Codex批量出图协议.md)
> - **05-gdd-v2-full-design-docs** → `openspec/changes/archive/2026-06-25-05-gdd-v2-full-design-docs/`（2026-06-25 用户 review 通过），交付物 = 1 总策划 + 15 系统 GDD + 16 模块详设 + CONTRACT（v2.1 含 50+ 事件 / 模块依赖图 / IPlayerController / 50 actor 预算 / 12 个 v2.1 追加事件）。详见 [GDD v2.1 入口](GDD-v2/00-总策划案v2.md)
> - **08-codex-art-gen-mcp** → `openspec/changes/archive/2026-06-26-08-codex-art-gen-mcp/`（2026-06-26 归档）。把 Codex CLI 生图固化为常驻 MCP 服务（`dispatch_l1` / `dispatch_l2` / `write_record` 三件套 + cwd 隔离 + 并发 2 路 + L2 合图 + chroma_key 工作流去绿）。实测 v21 22 张资产单次跑完（L1 14 张 13.5 min + L2 8 张 1.5 min）。
> - **09-mcp-decouple** → `openspec/changes/archive/2026-06-26-09-mcp-decouple/`（2026-06-26 归档）。把 08 MCP 内的 v21 业务硬编码全部下沉到调用方：MCP 只剩跑 codex + 后处理，业务（STYLE_BASE / 分类 / 命名规则）写入 `tools/codex-art-gen-helper/expand_v21.py`。跨项目复用此 MCP 时只需新建一个 `expand_<project>.py`。同期修复两个关键 bug：subprocess 在 MCP stdio 下必须 `stdin=DEVNULL` 防协议管道死锁；prompt 不能给完整返回 JSON 占位，server.py 改用纯磁盘核验（防 codex 复读模板假装成功）。
> - **02-skill-routing-unification** → `openspec/changes/archive/2026-06-29-02-skill-routing-unification/`（2026-06-29 归档）。SKILL 路由白名单制硬墙化 + gitnexus 清除 + 共享 SKILL 显式登记 + 7 个 agent 措辞统一。spec 沉淀到 [openspec/specs/skill-routing/](../openspec/specs/skill-routing/)。
> - **03-workflow-on-openspec** → `openspec/changes/archive/2026-06-29-03-workflow-on-openspec/`（2026-06-29 归档）。工作/ 整目录删除 + 全职责沉淀到 openspec change 一站式目录树（proposal/design/tasks/specs + brainstorm + CONTRACT + art/ + tests/）。spec 沉淀到 [openspec/specs/workflow/](../openspec/specs/workflow/)。
> - **04-ui-planning-first** → `openspec/changes/archive/2026-06-29-04-ui-planning-first/`（2026-06-29 归档）。UI 类型素材出图前必须先定表（页面清单/复用组件清单/组件状态表）；ai-art 自动起草供用户审阅。spec 沉淀到 [openspec/specs/ui-planning/](../openspec/specs/ui-planning/)。
> - **11-skill-governance** → `openspec/changes/archive/2026-06-29-11-skill-governance/`（2026-06-29 归档）。109 SKILL description 全部 60-250 字符 + 触发词；6 组重叠 SKILL 加 ❌ 不适用 划界；MCP 启用从 8 降到 4（每次启动节省 5-15k token）；新增 audit_skills.py / audit_skill_usage.py 月度防腐。spec 沉淀到 [openspec/specs/skill-governance/](../openspec/specs/skill-governance/)。
> - **12-core-ui-screens** → `openspec/changes/archive/2026-06-29-12-core-ui-screens/`（2026-06-29 归档）。10 个核心 Form 走完整 UI 5 阶段流程；9 个 mockup 已确认；7 个 Form 视觉对齐；4 个 Prefab（Settings / SelfTattoo / ThreeChoice / TattooEnchant）因 MCP 中文编码 bug 移交 13-fix-broken-prefabs follow-up。spec 沉淀到 [openspec/specs/core-ui-screens/](../openspec/specs/core-ui-screens/)。
> - **13-fix-broken-prefabs** → `openspec/changes/archive/2026-06-29-13-fix-broken-prefabs/`（2026-06-29 归档）。修复 4 个 Prefab（Settings / SelfTattoo / ThreeChoice / TattooEnchant）—— unity-skills MCP（`http://localhost:8091/`）中文编码 bug 导致的乱码 + Sprite 未绑。Fan-Out 模式 1：4 个 client-unity agent 并行原地 Edit YAML（不走 MCP），4/4 Prefab `�` 残留清零 + 必要 Sprite 全部绑定 + 仅 4 个目标 Prefab mtime 落在本 session。MCP 编码 bug 根因 spike 完成（在 Codex/Claude → unity_skills.py CLI 胶水层 Windows cp936/argv 编码），本期不修，留下一期 **14-mcp-encoding-fix**。PlayMode 运行时联调（4 个 Form 与 mockup 视觉分组比对）交付用户人工执行。spec 沉淀到 [openspec/specs/fix-broken-prefabs/](../openspec/specs/fix-broken-prefabs/)。

详见 [openspec/changes/](../openspec/changes/)。

---

## 五、新建 wiki 条目的规范

每个 wiki 文件头部要包含：

```yaml
---
title: <系统/决策名>
owner: <主负责 agent>
created: YYYY-MM-DD
last_updated: YYYY-MM-DD
status: active | superseded | archived
related_specs:
  - openspec/changes/...
related_skills:
  - <skill-name>
---
```

正文必须含：

1. **背景**：为什么有这个东西
2. **决策**：选了什么 + 为什么
3. **被否定的备选**：≥2 条 + 否定理由
4. **影响范围**：哪些代码 / 哪些 agent / 哪些 skill
5. **过时检查**：何时该 review / 何时该归档

---

## 六、与其他索引的关系

| 索引 | 作用 | 链接 |
|---|---|---|
| `.claude/CLAUDE.md` | AI 行为准则 + 路由 + 工作流主入口 | [→](../.claude/CLAUDE.md) |
| `.claude/SKILL_MATRIX.md` | agent ↔ skill 白名单 | [→](../.claude/SKILL_MATRIX.md) |
| `.claude/skills/SKILLS_INDEX.md` | 124 SKILL 分组索引 | [→](../.claude/skills/SKILLS_INDEX.md) |
| **本文件** | **项目自维护知识库** | — |
| `.claude/AGENTS.md` | 多 Agent 协作 5 模式 | [→](../.claude/AGENTS.md) |
| `openspec/changes/` | 活跃 / 已归档变更（含 art/ + tests/ + brainstorm.md） | [→](../openspec/changes/) |

---

*最后更新：2026-06-29（openspec 13-fix-broken-prefabs 归档：Fan-Out 4 agent 并行修 4 个 Prefab，静态验收全过；PlayMode 联调留用户人工；MCP 中文编码 bug 留 14-mcp-encoding-fix follow-up）*
