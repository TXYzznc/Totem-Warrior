# 项目上下文 — AI 快速理解指南

> AI 在新会话中先读本文件了解项目状态。

---

## 一、是什么

**AI 友好型轻量游戏开发框架 + 完整 AI 协作工具链（模板）**

两层定位：

1. **游戏侧**：Unity 6.3 LTS 模块化框架。框架只做两件事——模块生命周期管理 + 模块间通信。**无 DI 容器**，依赖显式声明在模块本身。
2. **AI 协作侧**：20 人虚拟开发团队（双轨 `.claude` / `.codex`）+ 124 SKILL + 11 MCP 工具 + 5 Phase 工作流 + OpenSpec 规范化变更 + 决策门槛 hook。

**模板纯净**——清掉了所有飞机大战业务（飞机/敌人/子弹/关卡/玩家/UI），保留框架核心 + 模板 + 启动场景。

---

## 二、目录结构

```
AI_Friendly_Project/
├── CONTEXT.md            ← 本文件（项目状态全景）
├── README.md             ← 一页 README（模板介绍）
├── AGENTS.md             ← Codex 顶层入口
├── REFACTOR_REPORT.md    ← 重构 v1.0 记录
├── setup.md              ← 环境搭建
├── requirements.txt      ← Python deps
├── .env.example          ← 凭据模板
├── .mcp.json             ← MCP 配置（核心/中/可选三档，${VAR} 占位）
├── .gitignore / .gitattributes
│
├── Assets/Scripts/       ← 框架核心 + 模板（无业务）
│   ├── Core/             ← 8 文件（IGameModule / ModuleRunner / EventBus / Logger / GameApp / TickDriver / Attrs）
│   ├── Utils/            ← 8 文件（StateMachine / CompositeDisposable / GenericObjectPool / FlowPipeline 等）
│   ├── DataTable/        ← DataTableRegistry + ResourceConfig（schema，框架级）
│   ├── Modules/
│   │   ├── DataTable/    ← 整套（含 Editor/DataTableGenerator）
│   │   ├── Resource/     ← ResourceModule
│   │   ├── GameState/    ← GameStateModule
│   │   ├── Input/        ← InputModule
│   │   ├── Scene/        ← SceneModule + SceneEvents
│   │   ├── UI/           ← UIModule（业务 UI 已删）
│   │   └── Flow/         ← FlowModule + FlowContext + IFlow + FlowEvents
│   ├── Events/           ← 空（业务事件 STGEvents 已删；新事件按 Templates）
│   ├── ExternalAPI/      ← Handlers/ 骨架
│   └── Templates/        ← 4 .cs.txt 模板（不编译，AI 参考）
│
├── Assets/Resources/     ← 业务素材已清；空目录保留作为占位
├── Assets/Prefabs/       ← UI / Shared / Effects 空目录（保留 README）
├── Assets/Scenes/
│   └── Launch.unity      ← ⚠️ 保留但内部有 missing script reference（业务脚本已删），首次开启 Unity 需手动清理或新建场景
│
├── .claude/              ← Claude Code 配置（source of truth）
│   ├── CLAUDE.md         ← AI 行为准则 + 路由 + 工作流 + 框架约束
│   ├── AGENTS.md         ← 多 Agent 协作 5 模式
│   ├── conventions.md
│   ├── GITNEXUS.md
│   ├── 资源配置规范.md
│   ├── SKILL_MATRIX.md   ← agent × skill 白名单 + 兜底规则
│   ├── settings.json     ← 人格 hook + 决策门槛 hook
│   ├── settings.local.json   ← MCP enabledMcpjsonServers + permissions
│   ├── agents/  (20)     ← 重写后的 system prompt（含 frontmatter / skills / escalate_to）
│   └── skills/  (124)    ← 不动（含 SKILLS_INDEX.md）
│
├── .codex/               ← OpenAI Codex 镜像（sync-agents.py 自动生成）
│   ├── config.toml       ← Codex MCP 配置（与 .mcp.json 一致）
│   ├── hooks.json
│   └── agents/  (20)
│
├── .agents/skills/       ← skill4agent MCP 镜像（sync-agents.py 自动生成）
│
├── tools/                ← 工程工具（.gitignore 排除大文件）
│   ├── sync-agents.py    ← agents 同步脚本（source = .claude/agents/）
│   ├── codebase-memory-mcp/
│   ├── game-asset-mcp/
│   ├── ImageCut_Tool / image-extender-main / rembg-main
│
├── 工作/                  ← 5 Phase 工作流（QUICKSTART 已更新到 20 agents 体系）
│   ├── 1.策划/  1.美术/  2.需求列表/  3.正在处理的任务/  3.测试/  4.已归档任务/  工作区/
│
├── 项目知识库（AI自行维护）/   ← AI 维护的知识库（模板状态：空）
│   ├── INDEX.md          ← 入口（含 wiki 条目规范）
│   ├── wiki/  raw/  outputs/
│
└── AI友好型项目探讨/       ← 5 篇框架设计文档（保留）
    ├── 01-框架核心设计概述.md
    ├── 02-AI友好型日志规范.md
    ├── 03-模块系统详细设计.md
    ├── 04-事件系统详细设计.md
    └── 05-项目文件结构.md
```

---

## 三、关键设计决策

| 决策 | 内容 |
|---|---|
| **无 DI 容器** | 依赖通过 `Type[] Dependencies` 显式声明 |
| **接口不作为依赖** | Dependencies 只接受具体 Module 类型 |
| **事件继承不支持** | 精确类型匹配（`typeof(T)` 作 key） |
| **InitAsync 异常终止** | 不允许跳过失败模块 |
| **ModuleRunner 非静态** | 挂在 GameApp MonoBehaviour 上，Domain Reload 兼容 |
| **`[EventHandler]` vs `[RequestHandler]` 分离** | 广播 / 请求响应分开 |
| **广播/请求响应分开存储** | EventBus 内部两张表 |
| **模板 .cs.txt** | 防止 Unity 编译不完整的示例代码 |
| **Category 显式声明** | 强制模块自报家门 |
| **Category × 100 - OutDegree** | 调度优先级公式 |
| **20 人虚拟团队 orchestrator** | 主对话路由不亲自做 |
| **双轨 .claude / .codex** | source = .claude/，.codex 自动同步 |
| **agent 显式 frontmatter** | tier / skills 白名单 / escalate_to |
| **决策门槛 hook** | 关键词触发 grill-me + openspec 提醒 |
| **MCP 三档** | core 默认开 / middle 默认开 / optional 默认关 |
| **codebase-memory 优先** | 代码结构查询不再用 Read+Grep |
| **业务清洁** | 飞机大战业务全清，Launch.unity 残留 missing reference 需手动处理 |

---

## 四、AI 重新进入推荐顺序

1. 读本文件
2. [.claude/CLAUDE.md](./.claude/CLAUDE.md) —— AI 行为准则 / 20 agents 路由 / 工作流 / MCP 准则
3. [.claude/AGENTS.md](./.claude/AGENTS.md) —— 多 Agent 协作 5 模式
4. [.claude/SKILL_MATRIX.md](./.claude/SKILL_MATRIX.md) —— agent ↔ skill 映射
5. [.claude/skills/SKILLS_INDEX.md](./.claude/skills/SKILLS_INDEX.md) —— 124 SKILL 索引（按职能）
6. [.claude/conventions.md](./.claude/conventions.md) —— 编码规范
7. 深入某系统：查 [项目知识库（AI自行维护）/INDEX.md](./项目知识库（AI自行维护）/INDEX.md) 或 [AI友好型项目探讨/](./AI友好型项目探讨/)

---

## 五、已知遗留 / 待办

| 优先级 | 内容 |
|---|---|
| 🔴 高 | **Launch.unity 含 missing script reference**（业务脚本删除后）。首次打开 Unity 需在场景中清理或重建。 |
| 🟡 中 | `tools/` 巨型二进制已加 .gitignore；新机器按 setup.md 重建 |
| 🟡 中 | `项目知识库（AI自行维护）/wiki/` 是空的——随项目开发由 AI 逐步填充 |
| 🟡 中 | `.codebase-memory/` 索引需首次运行 `codebase-memory` MCP 时重建 |
| 🟢 低 | `工作/QUICKSTART.md` 仍含部分项目案例（如背包系统），可改为更纯粹模板 |
| 🟢 低 | 编辑 `.env.example` 后实际项目要新建 `.env` |

---

*最后更新：2026-06-24（重构 v1.0 落地）*
