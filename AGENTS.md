# GameDesinger 项目指南

Unity 6 LTS 游戏开发项目。本仓库**已封装一支 20 人的虚拟开发团队**（[.Codex/agents/](.Codex/agents/)），主对话默认作为 orchestrator 路由任务，不亲自做专家活。

## 路由规则（任务类型 → 调用哪个 agent）

| 任务类型 | Agent |
|---|---|
| 项目计划、PRD、排期、风险、竞品 | [`producer`](.Codex/agents/producer.md) |
| 核心玩法 vision、GDD、MDA、留存哲学 | [`gd-lead`](.Codex/agents/gd-lead.md) (opus) |
| 具体公式、数值表、loot 表、状态机规格 | [`gd-system`](.Codex/agents/gd-system.md) |
| 关卡布局、节奏、encounter、puzzle、引导 | [`level-designer`](.Codex/agents/level-designer.md) |
| 美术风格统筹、art bible、风格审稿 | [`art-director`](.Codex/agents/art-director.md) (opus) |
| HUD / 菜单 / icon 设计 | [`art-ui`](.Codex/agents/art-ui.md) |
| 字体选型、排版、CJK | [`art-font`](.Codex/agents/art-font.md) |
| 特效设计、粒子配方（美术侧） | [`art-vfx`](.Codex/agents/art-vfx.md) |
| 立绘、sprite、像素美术 | [`art-2d`](.Codex/agents/art-2d.md) |
| 3D 模型、UV、贴图、Blender | [`art-3d`](.Codex/agents/art-3d.md) |
| 动画、骨骼、Mecanim、Timeline | [`art-anim`](.Codex/agents/art-anim.md) |
| 客户端架构、设计模式、性能预算 | [`client-lead`](.Codex/agents/client-lead.md) (opus) |
| Unity C# 实现、UI 接入、存档、输入 | [`client-unity`](.Codex/agents/client-unity.md) |
| Shader、URP/HDRP、后处理、TA 工具 | [`client-ta`](.Codex/agents/client-ta.md) |
| 服务端架构、协议、匹配、反作弊 | [`net-lead`](.Codex/agents/net-lead.md) (opus) |
| API、JWT、Redis、消息队列实现 | [`net-backend`](.Codex/agents/net-backend.md) |
| DB schema、索引、迁移、查询优化 | [`net-db`](.Codex/agents/net-db.md) |
| 测试策略、UTF、bug、crash、playtest | [`qa-engineer`](.Codex/agents/qa-engineer.md) |
| CI/CD、Unity 构建、发版、签名 | [`devops-engineer`](.Codex/agents/devops-engineer.md) |
| Editor 扩展、内部工具、新建 skill | [`tools-engineer`](.Codex/agents/tools-engineer.md) |

**默认行为**：用户提问匹配以上任意一类，**先 delegate 给对应 agent**，不要自己直接干。简单的"读文件/解释代码"这类轻量任务可自己处理。

## 团队与 SKILL 详情

- [.Codex/TEAM_PLAN.md](.Codex/TEAM_PLAN.md) — 团队架构、构建过程、SKILL 总数
- [.Codex/SKILL_MATRIX.md](.Codex/SKILL_MATRIX.md) — skill 归属表 + 5 条重叠路由规则
- [.Codex/agents/](.Codex/agents/) — 20 个 agent 的 system prompt 与 skill 白名单
- [.Codex/skills/](.Codex/skills/) — 116 个 skill 实体

## 项目环境

- **平台**：Unity 6.3 LTS（[Assembly-CSharp.csproj](Assembly-CSharp.csproj) / [GameDesinger.sln](GameDesinger.sln)）
- **OS**：Windows 10，shell 用 bash（不是 PowerShell）—— 用 `/` 路径
- **Python 环境**：[.venv/](.venv/)（frame-ronin MCP 用），见 [setup.md](setup.md)
- **MCP 工具**：blender / frame-ronin / godot / playwright / skill4agent / codebase-memory

### codebase-memory MCP 使用准则

**优先**调用 `codebase-memory` 查询 `Assets/Scripts/` 代码结构（函数定义、调用链、类型层级、跨文件引用），**不要**用 Read + Grep 逐文件扫。

适用场景：
- "X 方法在哪里定义/被谁调用"
- "Player 类的所有 public 接口"
- "TattooComposer 依赖了哪些类"
- 重构前的 impact 分析

不适用：读单个文件具体实现（用 Read）、改代码（用 Edit）。
- **核心系统文档**：[纹身系统三层原子设计.md](纹身系统三层原子设计.md)

## 编码与协作约束

- 改 Unity 代码先看 [Assets/Scripts/](Assets/Scripts/) 既有 conventions
- 不在 Update 里做 GC alloc
- ScriptableObject 是配置不是数据库
- 任何引入新依赖前问"标准库或现有依赖能做吗"
- 路径用 `d:/unity/UnityProject/GameDesinger/...` 或相对路径

## 不要

- 不要绕过 agent 团队自己实现专家任务
- 不要再写"待装"标记的 skill —— 116 个已就位
- 不要把 skill 移到子目录 —— Codex 不递归扫描
- 不要在没有 grill-me / grill-with-docs 的情况下做大型设计决策
