# AI 友好型项目 — Codex 入口

> Codex（`.codex/`）以本文件为顶层指引；Claude Code 入口为 [.claude/CLAUDE.md](./.claude/CLAUDE.md)。两份保持语义一致。
>
> `.codex/agents/*.toml` 由 `.claude/agents/*.md` 经 [tools/sync-agents.py](./tools/sync-agents.py) 生成。不要直接改 .toml。

Unity 6.3 LTS 自研轻量模块化框架的 **AI 协作模板**。主对话作为 **orchestrator**，把任务路由到 20 人虚拟开发团队；不亲自做专家活。

---

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

匹配以上任一类，**先 delegate 给对应 agent**。简单的"读文件 / 解释代码"轻量任务可自己处理。

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

- **总数**：124，分组索引见 [.claude/skills/SKILLS_INDEX.md](./.claude/skills/SKILLS_INDEX.md)
- **agent ↔ skill 白名单**：[.claude/SKILL_MATRIX.md](./.claude/SKILL_MATRIX.md)
- **skill4agent MCP 镜像**：`.agents/skills/`（由 sync 脚本生成）

---

## 项目环境

- **平台**：Unity 6.3 LTS
- **OS**：Windows 10，shell 用 bash（不是 PowerShell）—— 路径用 `/`
- **Python**：`.venv/`（frame-ronin MCP），见 [setup.md](./setup.md)
- **凭据**：复制 [.env.example](./.env.example) 为 `.env` 后填值
- **MCP**（[.mcp.json](./.mcp.json) + [.codex/config.toml](./.codex/config.toml)）：
  - **core**（默认启用）：skill4agent / codebase-memory / playwright / blender
  - **middle**：godot / frame-ronin / game-asset-mcp
  - **optional**（默认关）：mongodb / atlassian / docker / kubernetes

### codebase-memory MCP 准则

**优先**调用 `codebase-memory` 查询 `Assets/Scripts/` 代码结构；**不要**用 Read + Grep 逐文件扫。

---

## AI 行为准则（八荣八耻）

> 以臆猜接口为耻，以认真查询为荣。以模糊执行为耻，以寻求确认为荣。以臆想业务为耻，以人类确认为荣。以创造接口为耻，以复用现有为荣。以跳过验证为耻，以主动测试为荣。以破坏架构为耻，以遵循规范为荣。以假装理解为耻，以诚实无知为荣。以盲目修改为耻，以谨慎重构为荣。

- 始终用**中文**回答。
- 优先用简单方案。
- 改 Unity 代码先看 [Assets/Scripts/](./Assets/Scripts/) 既有 conventions。
- 不在 Update 里做 GC alloc。
- ScriptableObject 是配置不是数据库。
- 所有按键输入必须走 `InputModule`。

---

## 不要

- 不要绕过 agent 团队自己做专家活
- 不要把 skill 移到子目录 —— Claude Code / Codex 都不递归扫描
- 不要在没有 `grill-me` / `grill-with-docs` 的情况下做大型设计决策
- 不要再写"待装"标记的 skill —— 124 个已就位
- 不要直接改 .codex/agents/*.toml —— source 是 .claude/agents/，跑 `tools/sync-agents.py`
- 不要把业务示例混入框架核心
