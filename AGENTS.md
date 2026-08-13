# AI 友好型项目 — Codex 入口

> Codex（`.codex/`）以本文件为顶层指引；Claude Code 入口为 [.claude/CLAUDE.md](./.claude/CLAUDE.md)。两份保持语义一致。
>
> `.codex/agents/*.toml` 由 `.claude/agents/*.md` 经 [tools/sync-agents.py](./tools/sync-agents.py) 生成。不要直接改 .toml。
>
> **兼容目标**：Claude 继续按原 `.claude/` 工作流运行；Codex 在不改 Claude 源配置的前提下，按本文件把同一套 agent / skill / openspec 流程等价执行。

Unity 2022.3.62f3 + GF_X runtime 的 **AI 协作模板**。主对话作为 **orchestrator**，把任务路由到 20 人虚拟开发团队；不亲自做专家活。

---

## AI 使用统计首次激活（每个新会话优先执行）

在本会话第一次处理用户任务前，先只读执行：

`python tools/log_tool_usage.py doctor --editor codex --json`

- 若 `editors.codex.active` 为 `true`，立即继续用户原任务，本会话不再重复检查或提示。
- 若为 `false`，暂停原任务，说明项目只会把 Tool / SKILL / Agent / MCP 名称和会话元数据写入本地 `.ai/usage/events.jsonl`，不会记录 Prompt、代码、参数、完整命令或完整路径；然后询问用户是否启用。
- 未得到明确确认前，不得运行 `init`、不得修改 Hook 信任、不得继续原任务。
- 用户确认后运行 `python tools/log_tool_usage.py init --editor codex --yes --trust-codex-hooks`。它只能信任当前项目 `.codex/hooks.json` 的当前哈希，不得使用绕过 Hook 信任的启动参数。
- 初始化成功后，请用户重启 Codex 并重新发送原任务；本轮不继续执行被暂停的原任务。
- 若脚本不存在，直接继续原任务，不把统计工具缺失当作项目阻塞。

## Codex 适配层（保持 Claude 原样）

### Source of truth

- `.claude/CLAUDE.md`、`.claude/agents/*.md`、`.claude/skills/` 仍是 Claude 工作流源文件。
- `.codex/agents/*.toml` 是 `.claude/agents/*.md` 的镜像；更新 agent 时只改 `.claude/agents/*.md`，然后运行 `python tools/sync-agents.py`。
- `.claude/skills/` 是唯一项目 skill 源；已移除 skill4agent 与 `.agents/skills/` 镜像。
- `.codex/hooks.json` 只注入 Codex 侧提醒，不替代 `.claude/settings.json`。

### Codex 执行语义

- 本文件中的"delegate 给对应 agent"在 Codex 中解释为：先读取/遵循 `.codex/agents/<agent>.toml` 的职责、边界、skill 白名单与交回规则。
- 如果当前 Codex 运行时提供可用的 sub-agent / delegate 工具，且用户明确要求使用子 agent、委派或并行 agent 工作，则可以按 `.codex/agents/*.toml` 委派。
- 如果 Codex 当前不能原生调用这些项目 agent，或用户没有明确要求子 agent，则主对话必须按对应 agent 的 prompt 与白名单**等价执行**，而不是跳过路由规则。
- 轻量任务（读文件、解释代码、小范围修复）可由主对话直接处理，但仍需遵守目标 agent 的边界和项目规范。
- 多 agent 并行语义在 Codex 中可退化为主对话顺序执行；涉及互相引用的模块时仍必须先走"骨架先行"。

### Codex 工具映射

| Claude 语义 | Codex 等价做法 |
|---|---|
| `Agent` | 优先按 `.codex/agents/*.toml` 路由；可用且获授权时用 Codex sub-agent，否则主对话等价执行 |
| `Skill` | 先用当前会话已暴露 skill；否则读取 `.claude/skills/<skill>/SKILL.md` |
| `Read / Grep / Glob` | Codex shell / `rg` / 文件读取工具；查 `Assets/Game/Scripts/` 结构时优先 codebase-memory MCP（若可用） |
| `Edit / Write` | 使用 Codex 文件编辑工具；不要直接改 `.codex/agents/*.toml` 镜像 |
| `WebSearch / WebFetch` | Codex web 工具；高时效、外部资料、OpenAI 文档等按 Codex 浏览规则执行 |
| `TodoWrite` | Codex plan / 更新说明；不要求一比一工具名 |
| `mcp__*` | 先确认当前 Codex 会话是否暴露对应 MCP；未暴露时降级为本地文件/脚本或明确阻塞 |

### 决策门槛

检测到 `设计 / 架构 / 重构 / 大改 / 重写 / GDD / PRD / 系统 / 范式 / 方案 / 思路` 时，Codex 必须照搬 Claude 的两阶段 FSM：

1. 阶段 A：先用 `grill-me` / `grill-with-docs` 的问题框架澄清目标、关键决策、边界、验收标准、约束。
2. 阶段 B：做任务规模评估；命中 openspec 信号则创建/推进 openspec change，否则走轻量路径。
3. 阶段 B 只有遇到阶段 A 共识冲突、不可逆变更、或触及 `.claude/` / `openspec/` / `Assets/Game/ScriptsBuiltin/` GF_X 框架核心时才中断用户。

Codex 若没有可调用的 `grill-me` 工具，也必须按该 skill 的反问模式执行，不能直接跳到方案。

## 路由规则（20 agents）

| 任务 | Agent | Tier |
|---|---|---|
| 项目计划 / PRD / 排期 / 风险 / 竞品 | [`producer`](./.codex/agents/producer.toml) | lead |
| 核心玩法 vision / GDD / MDA / 留存哲学 | [`gd-lead`](./.codex/agents/gd-lead.toml) | lead (opus) |
| 公式 / 数值表 / loot / 状态机 / 任务规格 | [`gd-system`](./.codex/agents/gd-system.toml) | system |
| 关卡布局 / 节奏 / encounter / puzzle / 引导 | [`level-designer`](./.codex/agents/level-designer.toml) | system |
| 美术风格统筹 / art bible / 风格审稿 | [`art-director`](./.codex/agents/art-director.toml) | lead (opus) |
| HUD / 菜单 / icon 设计 | [`art-ui`](./.codex/agents/art-ui.toml) | impl |
| 字体选型 / 排版 / CJK | [`art-font`](./.codex/agents/art-font.toml) | impl |
| 特效设计 / 粒子配方（美术侧） | [`art-vfx`](./.codex/agents/art-vfx.toml) | impl |
| 立绘 / sprite / 像素美术 | [`art-2d`](./.codex/agents/art-2d.toml) | impl |
| 3D 建模 / UV / 贴图 / Blender | [`art-3d`](./.codex/agents/art-3d.toml) | impl |
| 动画 / 骨骼 / Mecanim / Timeline | [`art-anim`](./.codex/agents/art-anim.toml) | impl |
| 客户端架构 / 设计模式 / 性能预算 | [`client-lead`](./.codex/agents/client-lead.toml) | lead (opus) |
| Unity C# 实现 / UI 接入 / 存档 / 输入 / DataTable | [`client-unity`](./.codex/agents/client-unity.toml) | impl |
| Shader / URP/HDRP / 后处理 / TA 工具 | [`client-ta`](./.codex/agents/client-ta.toml) | impl |
| 服务端架构 / 协议 / 匹配 / 反作弊 | [`net-lead`](./.codex/agents/net-lead.toml) | lead (opus) |
| API / JWT / Redis / 消息队列实现 | [`net-backend`](./.codex/agents/net-backend.toml) | impl |
| DB schema / 索引 / 迁移 / 查询优化 | [`net-db`](./.codex/agents/net-db.toml) | system |
| 测试策略 / UTF / bug / crash / playtest | [`qa-engineer`](./.codex/agents/qa-engineer.toml) | impl |
| CI/CD / Unity 构建 / 发版 / 签名 | [`devops-engineer`](./.codex/agents/devops-engineer.toml) | impl |
| Editor 扩展 / 内部工具 / 新建 skill | [`tools-engineer`](./.codex/agents/tools-engineer.toml) | impl |

匹配以上任一类，**先按对应 agent 路由**。Claude Code 中直接 delegate；Codex 中按"Codex 执行语义"等价执行。简单的"读文件 / 解释代码"轻量任务可自己处理。

---

## Agent 兜底机制

每个 agent 在 system prompt 中显式声明 `escalate_to: main`。出现以下情形之一时 **立即停止本任务并交回主对话**：

1. 需要调用白名单外 SKILL
2. 跨职能决策
3. MCP / 外部权限不足
4. 职责边界外
5. 多轮收敛失败（3 轮）
6. 用户意图模糊
7. 决策门槛触发（设计 / 架构 / 重构 / GDD / PRD / 系统 / 范式 / 方案 / 思路 等关键词）

详见 [.claude/SKILL_MATRIX.md](./.claude/SKILL_MATRIX.md)。

---

## SKILL 系统

- **总数**：113 个本地项目 skill，分组索引见 [.claude/skills/SKILLS_INDEX.md](./.claude/skills/SKILLS_INDEX.md)
- **agent ↔ skill 白名单**：[.claude/SKILL_MATRIX.md](./.claude/SKILL_MATRIX.md)
- **唯一来源**：`.claude/skills/<skill>/SKILL.md`
- **Codex 使用**：触发 skill 时先读取该 skill 的 `SKILL.md`；若 skill 在当前 Codex skills 列表中已暴露，按 Codex skill 规则执行；否则从 `.claude/skills/` 读取源说明后执行。
- **`/graphify`**：Codex 中映射到 `graphify-windows` skill。用户输入 `/graphify` 时，先读取并执行该 skill，再做其他事。
- **使用频率统计**：Claude Code / Codex Hook 统一写入 `.ai/usage/events.jsonl`；其他 AI 编辑器通过 `python tools/log_tool_usage.py record|hook` 接入，详见 [tools/AI_TOOL_USAGE.md](./tools/AI_TOOL_USAGE.md)。旧 `.claude/skills/_usage.log` 仅作兼容输入。

---

## 项目环境

- **平台**：Unity 2022.3.62f3
- **版本覆盖**：若旧文档、skill 参考或外部链接写 Unity 6 / 6000.3，只能作为通用思路参考；落地代码、包版本、API 用法必须按 Unity 2022.3.62f3 校验。
- **GF_X 全量诊断**：AI 自动验证优先运行 `python .claude/skills/unity-skills/scripts/unity_skills.py totem_diagnostics_run_all --port 8092`；`Game Framework/GameTools/Diagnostics/Run All` 只作为人工菜单复跑入口，不要通过通用 `editor_execute_menu` 路由这个菜单。
- **OS**：Windows 10。Claude Code 按原约定使用 bash、路径用 `/`；Codex 桌面当前可能运行在 PowerShell，执行命令时以当前 shell 为准，但输出和文档路径尽量使用 `/` 或明确的绝对路径。
- **Python**：`.venv/`（frame-ronin MCP），见 [setup.md](./setup.md)
- **凭据**：复制 [.env.example](./.env.example) 为 `.env` 后填值
- **MCP**（[.mcp.json](./.mcp.json) + [.codex/config.toml](./.codex/config.toml)）：
  codebase-memory / codex-art-gen / playwright / blender / godot / frame-ronin / atlassian

### codebase-memory MCP 准则

**优先**调用 `codebase-memory` 查询 `Assets/Game/Scripts/` 代码结构；**不要**用 Read + Grep 逐文件扫。若当前 Codex 会话未暴露 codebase-memory 工具，先用 `rg` 做最小范围查询，并在结果中说明该 MCP 当前不可用。

### 美术资源生产约束

- 需要美术资源时，可按已确定项目美术风格直接使用 art subagent / codex-art-gen / frame-ronin 等能力生成；资源生成过程中不用反复阻塞等待用户确认，只有工具不可用、缺少必要参考图或触及不可逆工程变更时才交回主对话。
- 角色帧动画必须按**单角色连续批处理**：传入角色参考图和动作需求后，连续生成该角色本轮所有动作；每个动作 4 个方向，每个方向 4 帧，方向为 `down` / `up` / `left` / `right`。
- 角色帧动画每张画布只能包含同一个角色；按精细度每张画布一个动画或最多两个动画，然后统一抠图、切图、去背景、重命名、导入检查，避免和其它角色动画混在一起。
- 角色帧动画优先输出到 `openspec/changes/<change>/art/raw/characters/<character_id>/`，单帧命名 `{character_id}_{action}_{direction}_{frame:00}.png`；入库后同步更新资源索引 / runtime asset catalog，加载仍走 `TotemAssetService`。

---

## AI 行为准则（八荣八耻）

> 以臆猜接口为耻，以认真查询为荣。以模糊执行为耻，以寻求确认为荣。以臆想业务为耻，以人类确认为荣。以创造接口为耻，以复用现有为荣。以跳过验证为耻，以主动测试为荣。以破坏架构为耻，以遵循规范为荣。以假装理解为耻，以诚实无知为荣。以盲目修改为耻，以谨慎重构为荣。

- 始终用**中文**回答。
- 优先用简单方案。
- 改 Unity 业务代码先看 [Assets/Game/Scripts/](./Assets/Game/Scripts/) 既有 conventions；已移除的旧 `Assets/Scripts` 不再作为项目资料来源。
- 不在 Update 里做 GC alloc。
- ScriptableObject 是配置不是数据库。
- 所有按键输入必须走 `TotemInputService` / `ITotemInputProvider`。

---

## 不要

- 不要绕过 agent 团队自己做专家活
- 不要把 skill 移到子目录 —— `.claude/skills/<skill>/SKILL.md` 是统一入口
- 不要在没有 `grill-me` / `grill-with-docs` 的情况下做大型设计决策
- 不要再写"待装"标记的 skill —— 113 个本地项目 skill 已就位
- 不要直接改 .codex/agents/*.toml —— source 是 .claude/agents/，跑 `tools/sync-agents.py`
- 不要把业务示例混入框架核心
