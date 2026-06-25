# AI 友好型项目模板

> Unity 6.3 LTS 自研轻量游戏框架 + **对 AI 开发最完善、配置最高效、协作最友好**的项目模板。
>
> **状态**：v1.0 重构完成。业务代码已清；框架核心 + AI 协作配置 + 工作流 + 124 SKILL + 20 人虚拟团队就绪。

## 这是什么

两层定位：

1. **游戏框架** — 自研 `IGameModule` / `ModuleRunner` / `EventBus`，**无 DI 容器**，依赖显式声明。
2. **AI 协作模板** — 20 人虚拟开发团队（lead / system / impl 三层）+ 124 SKILL + 11 MCP + 5 Phase 工作流 + OpenSpec + 决策门槛 hook。

## 配置即开即用

| 区域 | 内容 |
|---|---|
| [.claude/](./.claude/) | Claude Code：20 agents（含 frontmatter / skill 白名单 / escalate_to）+ 124 skills + 行为准则 + 工作流 |
| [.codex/](./.codex/) | OpenAI Codex：由 `.claude/` 经 `sync-agents.py` 自动生成 |
| [.agents/](./.agents/) | skill4agent MCP 镜像（自动同步） |
| [.mcp.json](./.mcp.json) | 7 MCP，凭据走 `${VAR}` + [.env.example](./.env.example) |
| [tools/](./tools/) | codebase-memory-mcp + 图像工具 + `sync-agents.py` |

## 技术栈

- Unity 6.3 LTS + UniTask + DOTween
- 自研 IGameModule / ModuleRunner / EventBus（详见 [AI友好型项目探讨/](./AI友好型项目探讨/)）
- DataTable：JSON 直写（`Assets/Resources/DataTable/*.json`）→ Unity 菜单 `Tools/DataTable/生成全部配置表代码` → `.cs`
- 统一 `ResourceModule` + `ResourceConfig`（见 [.claude/资源配置规范.md](./.claude/资源配置规范.md)）

## 目录速览

| 目录 | 说明 |
|---|---|
| `Assets/Scripts/Core/` | 框架核心 8 文件 |
| `Assets/Scripts/Utils/` | 通用工具类 |
| `Assets/Scripts/Modules/` | 框架级模块（DataTable / Resource / GameState / Input / Scene / UI / Flow） |
| `Assets/Scripts/Templates/` | 4 个 `.cs.txt` 模板（Module / Event / Request / DataTable） |
| `工作/` | 5 Phase 工作流（策划 → 需求 → 执行 → 测试 → 归档） |
| `AI友好型项目探讨/` | 5 篇框架设计文档 |
| `项目知识库（AI自行维护）/` | AI 维护的知识库（模板状态空，随开发填充） |

## 快速开始

1. **环境**：
   ```bash
   python -m venv .venv
   .venv/Scripts/pip install -r requirements.txt   # Windows
   cp .env.example .env                            # 按需填值
   ```
   详见 [setup.md](./setup.md)。
2. **打开**：Unity 6.3 LTS 打开本目录。
3. **第一次清理 Launch 场景**：⚠️ `Assets/Scenes/Launch.unity` 残留对已删业务脚本的引用，会出现 missing script。请在 Unity 中清理 GameObject 或新建空场景再挂 `GameApp`。
4. **让 AI 进入**：
   - Claude Code 自动读 `.claude/CLAUDE.md`
   - Codex 自动读根 `AGENTS.md`

## 核心文档

| 文档 | 内容 |
|---|---|
| [CONTEXT.md](./CONTEXT.md) | 项目状态全景（AI 新会话必读） |
| [.claude/CLAUDE.md](./.claude/CLAUDE.md) | AI 行为准则 / 20 agents 路由 / 工作流 / MCP 准则 / 框架约束 |
| [.claude/AGENTS.md](./.claude/AGENTS.md) | 多 Agent 协作 5 模式 |
| [.claude/SKILL_MATRIX.md](./.claude/SKILL_MATRIX.md) | agent ↔ skill 白名单 + 兜底规则 |
| [.claude/skills/SKILLS_INDEX.md](./.claude/skills/SKILLS_INDEX.md) | 124 SKILL 分组索引 |
| [REFACTOR_REPORT.md](./REFACTOR_REPORT.md) | 重构 v1.0 决策与变更明细 |

## 维护准则

- **agents 单源**：源是 `.claude/agents/*.md`；改完跑 `python tools/sync-agents.py` 同步到 `.codex/` 与 `.agents/`。
- **凭据 .env 化**：不要把 token 直接写 `.mcp.json` / `.codex/config.toml`。
- **大型决策三步**：grill-me → openspec new change → 更新知识库 INDEX。settings.json 的 hook 会自动提醒。
- **业务清洁**：往 `Templates/` 里加新模式时用 `.cs.txt`；不要往框架核心混业务示例。

## 设计原则

- 优先复用，少造轮子
- 简单方案，杜绝过度工程
- 明确依赖，拒绝隐式耦合
- 严格类型化的事件契约
- AI 与人类共同遵守编码规范

---

*v1.0 重构日期：2026-06-24*
