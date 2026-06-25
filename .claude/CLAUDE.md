# AI 友好型项目 — 项目指南（模板版）

> Unity 6.3 LTS 自研轻量模块化框架的 **AI 协作模板**。
>
> 核心配置：**20 人虚拟开发团队** + **123 个 Claude skills** + **11 个 MCP 工具** + **openspec 一站式工作流** + **决策门槛 hook**。

---

## 一、AI 行为准则（每次任务必须严格执行）

> 以臆猜接口为耻，以认真查询为荣。
> 以模糊执行为耻，以寻求确认为荣。
> 以臆想业务为耻，以人类确认为荣。
> 以创造接口为耻，以复用现有为荣。
> 以跳过验证为耻，以主动测试为荣。
> 以破坏架构为耻，以遵循规范为荣。
> 以假装理解为耻，以诚实无知为荣。
> 以盲目修改为耻，以谨慎重构为荣。

- 始终用**中文**回答。
- 回复尽量简洁，不要加无关的客套话。
- 优先用简单方案，不要过度工程。
- 涉及项目代码和业务开发时，必须先遵循本 CLAUDE.md 与 [conventions.md](./conventions.md)。
- 查找具体系统/问题文档时，先查阅[项目知识库索引 INDEX.md](../项目知识库（AI自行维护）/INDEX.md)。
- 所有按键输入必须走 `InputModule`。

---

## 二、虚拟开发团队 — 路由规则（20 人）

主对话作为 **orchestrator**，**不亲自做专家活**。轻量任务（读文件/解释代码）可自己处理。

| 任务类型 | Agent | Tier |
|---|---|---|
| 项目计划、PRD、排期、风险、竞品 | [`producer`](./agents/producer.md) | lead |
| 核心玩法 vision、GDD、MDA、留存哲学 | [`gd-lead`](./agents/gd-lead.md) | lead (opus) |
| 具体公式、数值表、loot 表、状态机规格 | [`gd-system`](./agents/gd-system.md) | system |
| 关卡布局、节奏、encounter、puzzle、引导 | [`level-designer`](./agents/level-designer.md) | system |
| 美术风格统筹、art bible、风格审稿 | [`art-director`](./agents/art-director.md) | lead (opus) |
| HUD / 菜单 / icon 设计 | [`art-ui`](./agents/art-ui.md) | impl |
| 字体选型、排版、CJK | [`art-font`](./agents/art-font.md) | impl |
| 特效设计、粒子配方（美术侧） | [`art-vfx`](./agents/art-vfx.md) | impl |
| 立绘、sprite、像素美术 | [`art-2d`](./agents/art-2d.md) | impl |
| 3D 模型、UV、贴图、Blender | [`art-3d`](./agents/art-3d.md) | impl |
| 动画、骨骼、Mecanim、Timeline | [`art-anim`](./agents/art-anim.md) | impl |
| 客户端架构、设计模式、性能预算 | [`client-lead`](./agents/client-lead.md) | lead (opus) |
| Unity C# 实现、UI 接入、存档、输入、DataTable | [`client-unity`](./agents/client-unity.md) | impl |
| Shader、URP/HDRP、后处理、TA 工具 | [`client-ta`](./agents/client-ta.md) | impl |
| 服务端架构、协议、匹配、反作弊 | [`net-lead`](./agents/net-lead.md) | lead (opus) |
| API、JWT、Redis、消息队列实现 | [`net-backend`](./agents/net-backend.md) | impl |
| DB schema、索引、迁移、查询优化 | [`net-db`](./agents/net-db.md) | system |
| 测试策略、UTF、bug、crash、playtest | [`qa-engineer`](./agents/qa-engineer.md) | impl |
| CI/CD、Unity 构建、发版、签名 | [`devops-engineer`](./agents/devops-engineer.md) | impl |
| Editor 扩展、内部工具、新建 skill | [`tools-engineer`](./agents/tools-engineer.md) | impl |

**默认行为**：匹配以上任一类，**先 delegate 给对应 agent**。

> 历史上的 4 个项目原生 agent（code-reviewer / bug-tracer / datatable-helper / ui-scaffold）已在重构 v1.0 中砍掉：code-reviewer 走通用 SKILL；bug-tracer 并入 `qa-engineer` + `client-lead`；datatable-helper 写入 `client-unity` 的 system prompt；ui-scaffold 拆为 `art-ui`（设计）+ `client-unity`（接入）。

---

## 三、Agent 兜底机制（escalate_to: main）

每个 agent 在 system prompt 中显式声明，出现以下情形之一时 **立即停止并交回主对话**：

1. **白名单外 SKILL**：需要调用 `frontmatter.skills` 之外的 SKILL
2. **跨职能决策**：任务涉及多个 agent 领域
3. **MCP / 外部权限不足**：缺凭据、缺工具、缺文件权限
4. **职责边界外**：任务实质不属于本 agent 职位
5. **多轮收敛失败**：3 轮内无法给出可行方案或反复回退
6. **意图模糊**：用户原始 prompt 含糊
7. **决策门槛触发**：检测到大型决策关键词（见 §五）时，应由主对话发起 grill-me + openspec

主对话因 tier 最高、SKILL 全开放，是最终兜底执行者。

详细的 agent ↔ SKILL 映射 + 兜底规则总览见 [SKILL_MATRIX.md](./SKILL_MATRIX.md)。

---

## 四、多 Agent 协作 5 模式

详见 [AGENTS.md](./AGENTS.md)。

> **关键**：若一个需求拆为多个**互相引用**的子模块（共享事件 / 跨模块 `GetModule<T>` / 共享数据结构），**必须**先走「骨架先行」——单 Agent 生成公共骨架（事件总表 + 模块空壳 + 基础设施 + 入口注册），裁定接口分歧后再并行填充。

---

## 五、决策门槛（两阶段 FSM）

> **设计目标**：前期一次性人在场把需求挖透，后续全自动跑 —— 配合 Auto Mode / Loop 长时间无人值守执行。Auto Mode 的「少打断」前提是「目标已对齐」，grill-me 是**对齐目标**的工具，不算违反 Auto Mode。

### 触发关键词（settings.json 中实装）

```
设计 / 架构 / 重构 / 大改 / 重写 / GDD / PRD / 系统 / 范式 / 方案 / 思路
```

### 阶段 A：需求挖掘（人在场，必须阻塞）

触发关键词后**必须**先调用 `grill-me`（或 `grill-with-docs`）多轮反问，**直到以下 5 条全部明确**才能退出：

- [ ] 核心目标一句话能说清楚（做什么、为什么）
- [ ] 关键决策点有 A/B 比较并明确选了哪个、为什么
- [ ] 不做什么（边界）已明确
- [ ] 验收标准已明确（怎么算完成）
- [ ] 关键约束已明确（性能/兼容/时间）

**任何一条没挖透都不能退出 grill** —— 这是整个流程**唯一**的人在场卡点。

### 阶段 B：自动执行（不再打断用户）

阶段 A 退出后，自动按顺序执行，**不再请求用户审批**：

1. `openspec new change <NN-功能名>` → 写 proposal/design/tasks/specs
2. 按 tasks.md 顺序实现（client-unity / art-director 等 agent 落地）
3. 中途遇到模糊点：**优先按阶段 A 的共识自决**，写日志/spec 备注
4. 完成后 `openspec archive-change <NN-name>` + 同步更新 [项目知识库（AI自行维护）/INDEX.md](../项目知识库（AI自行维护）/INDEX.md)

### 例外打断条件（阶段 B 仅以下情况可中断用户）

只有遇到**真正不可自决**的问题才能打断：

- 与阶段 A 共识**直接冲突**（grill 说了 A，实现发现必须做 B）
- 引入**不可逆变更**（删除/重命名公共 API、迁移数据、改动他人正在用的契约）
- 触及**项目宪法级**文件（`.claude/` / `openspec/` / `Assets/Scripts/Core/` 框架核心）

其他所有模糊点（命名、内部实现选型、测试粒度、日志格式等）一律自决。

**阶段 A 未挖透直接给方案视为违规**。lead/system agent 在此规则下应立即停止并交回主对话。

---

## 六、工作流系统（一站式 openspec change）

每个功能 = 一个 `openspec/changes/<NN-name>/` 目录，承载全生命周期 artifact。

```
策划讨论（brainstorm.md） → openspec 全程（proposal/design/tasks/specs+art+tests） → openspec archive-change 自动归档
```

### 目录约定

```
openspec/changes/<NN-name>/
├─ .openspec.yaml
├─ proposal.md / design.md / tasks.md / specs/<能力>/spec.md   ← openspec 原生
├─ brainstorm.md         ← Phase 1: 策划讨论沉淀（首次提出功能时建议）
├─ CONTRACT.md           ← 多模块全局契约（仅多模块时创建）
├─ art/                  ← 美术（ai-art SKILL 落盘点）
│  ├─ requirements.md    ←   美术需求分析
│  ├─ prompts.md         ←   提示词
│  └─ raw/               ←   AI 出图源图 + 生成记录.md
└─ tests/                ← 测试（qa-engineer 落盘点）
   ├─ plan.md
   ├─ results.md
   └─ bugs.md
```

| 节点 | 主导 agent | 自动行为 |
|---|---|---|
| **1. 策划讨论** | 主对话 + 用户 | 多轮澄清需求，沉淀 `brainstorm.md`（非必须，但建议） |
| **2. openspec 拆解** | producer / gd-lead / lead 群 | 调 `grill-me` 决策门槛 → `openspec new change <NN-name>` 落地 proposal/design/tasks/specs |
| **3. 实现** | client-lead 给架构、client-unity / net-backend 等落地、art-director 调 ai-art 出图 | 按 tasks.md 推进，资源直接写到对应子目录 |
| **4. 测试** | qa-engineer | 测试方案/结果/bug 写入 `tests/` 子目录；测试代码进 `Assets/Tests/` |
| **5. 归档** | 主对话 / 用户 | `openspec archive-change <NN-name>` 自动移到 `openspec/changes/archive/`，子目录 `art/`、`tests/` 等随行 |

### 美术素材生成意图

详见 [.claude/skills/ai-art/SKILL.md](./skills/ai-art/SKILL.md) 的「美术素材实现流程」。核心规则：

1. 主对话识别意图后，**先定位当前 active openspec change**（`openspec status` / 用户上下文 / 询问用户）
2. **⚠️ UI 类型前置**：若 change 含 UI 类型素材，`art/requirements.md` 必须先有三表（页面清单 / 复用组件清单 / 组件状态表）；缺三表 → ai-art 主动起草骨架供用户审阅修订，未确认不得进出图。详见 [drawing-prompt-UI.md](./skills/ai-art/references/drawing-prompt-UI.md) 顶部「UI 出图前置：先定表（强制）」
3. 读取 `openspec/changes/<change-name>/art/requirements.md` + `art/prompts.md`
4. 调绘图模型逐项生图，输出到 `openspec/changes/<change-name>/art/raw/`
5. 同目录写 `生成记录.md`；更新 `art/requirements.md` 头部状态字段为「已处理」
6. 无可用绘图模型时明确阻塞，不能假装已生成

---

## 七、技术栈

- **框架核心**：自研 IGameModule / ModuleRunner / EventBus（见 §九）
- **UniTask**：所有异步操作用 `await UniTask`，不用协程
- **DOTween**：UI 动画
- **DataTable**：JSON 直写（`Assets/Resources/DataTable/*.json`，每张表 `{ table, fields[], rows[] }`）→ Unity 菜单 `Tools/DataTable/生成全部配置表代码` → 自动生成 `Assets/Scripts/DataTable/<Name>.cs`。**不要用 Excel / .xlsx / .bytes**。
- **ResourceModule**：统一资源加载（见 [资源配置规范.md](./资源配置规范.md)）
- **开发热重载**：Unity Domain Reload + Enter Play Mode Options

---

## 八、项目环境与工具栈

- **平台**：Unity 6.3 LTS
- **OS**：Windows 10，shell 用 **bash**（不是 PowerShell）—— 路径用 `/`
- **Python 环境**：`.venv/`（frame-ronin MCP），见 [setup.md](../setup.md) 与 [requirements.txt](../requirements.txt)
- **凭据**：`.env`（从 [`.env.example`](../.env.example) 复制并填值，已加 .gitignore）

### MCP 服务清单（[.mcp.json](../.mcp.json) + [.codex/config.toml](../.codex/config.toml)）

| 档位 | MCP | 说明 |
|---|---|---|
| **core** | skill4agent | SKILL 注册中心 |
| **core** | codebase-memory | 代码结构索引（优先于 Read+Grep） |
| **core** | playwright | Web E2E |
| **core** | blender | 3D 资产生成与脚本 |
| middle | godot | 跨引擎参考 |
| middle | frame-ronin | 帧/精灵/像素美术 |
| middle | game-asset-mcp | HF 模型 → 3D/纹理 |
| optional | docker / kubernetes | 部署/运行时 |
| optional | mongodb | 后端持久层 |
| optional | atlassian | Jira / Confluence |

> optional MCP 默认不在 `.claude/settings.local.json` 的 `enabledMcpjsonServers` 中。需要时手动加入。

### codebase-memory MCP 使用准则

**优先**调用 `codebase-memory` 查询 `Assets/Scripts/` 代码结构（函数定义、调用链、类型层级、跨文件引用）；**不要**用 Read + Grep 逐文件扫。

适用：
- "X 方法在哪里定义/被谁调用"
- "Foo 类的所有 public 接口"
- "ModuleRunner 依赖了哪些类"
- 重构前的 impact 分析

不适用：读单个文件具体实现（用 Read）、改代码（用 Edit）。

### 工程工具（[tools/](../tools/)）

- `codebase-memory-mcp/` — codebase-memory MCP 二进制
- `game-asset-mcp/` — HF 模型生成（Node）
- `ImageCut_Tool/` / `image-extender-main/` / `rembg-main/` — 图像处理

> tools/ 较大（~687 MB），已加 `.gitignore`。新机器按 [setup.md](../setup.md) 重建。

---

## 九、框架核心概念

### 模块系统

```
IGameModule          — 模块接口（ModuleCategory, Dependencies, InitAsync, ShutdownAsync）
ModuleRunner         — 模块生命周期管理（WhenAny + 持续扫描）
EventBus             — 模块间通信（Publish / Subscribe / RequestAsync）
[EventHandler]       — 属性驱动的事件订阅
ModuleRunner.GetModule<T>() — 高频数据查询缓存入口
```

### 模块通信

| | EventBus | GetModule\<T\> |
|---|---|---|
| 用途 | "某件事发生了" | "当前状态是什么" |
| 方式 | Publish / Subscribe | 读缓存引用 |
| 开销 | 异步 | 同步，零开销 |

### 初始化流程

1. `GameApp.Start()` 创建 EventBus → ModuleRunner → `AddModule` → `StartAsync`
2. ModuleRunner：依赖校验 → 循环检测 → WhenAny + 持续扫描初始化
3. 模块 `InitAsync` 完成后自动扫描 `[EventHandler]` 并注册
4. 所有模块就绪 → 游戏启动

---

## 十、SKILL 系统

- **总数**：123 个，分组索引见 [skills/SKILLS_INDEX.md](./skills/SKILLS_INDEX.md)
- **Agent ↔ SKILL 白名单**：见 [SKILL_MATRIX.md](./SKILL_MATRIX.md)
- **大多数 skill 不进上下文**：仅在对应 agent 触发时按需读取 `SKILL.md` + `references/*.md`
- **找不到合适 skill 时**：用 `find-skills` 语义检索；仍找不到则 escalate_to: main 由主对话决定

---

## 十一、命名规范

| 类型 | 规范 | 示例 |
|---|---|---|
| 模块 | `[Name]Module` | `CombatModule`, `UIModule` |
| 事件 | `[描述]Event` | `CombatEndEvent`, `HPChangedEvent` |
| 事件处理方法 | `On[事件名]` | `OnCombatEnd`, `OnHPChanged` |
| UI 表单 | `[Name]UI` 或 `[Name]UIForm` | `GameUIForm` |
| 管理器（非模块） | `[Name]Manager` | `PlayerInputManager` |

---

## 十二、关键约束与陷阱

### 约束

- **ModuleCategory 必须正确设置**（参 [IGameModule.cs](../Assets/Scripts/Core/IGameModule.cs#L12-L18)）
- **Dependencies 必须是具体类型**，不支持接口
- **InitAsync 期间不发事件**，初始化通信用 Dependencies
- **行为修改走事件，数据读取走 `GetModule<T>`**
- 异步方法名必须以 `Async` 结尾，返回 `UniTask` 或 `UniTask<T>`
- 不要硬编码数值，所有配置读 DataTable
- **不要硬编码资源路径**，所有资源通过 ResourceModule + ResourceConfig 加载（见 [资源配置规范.md](./资源配置规范.md)）
- 所有游戏资源必须存放在 `Assets/Resources/` 下，按类型分子目录
- 不在 Update 里做 GC alloc
- ScriptableObject 是配置不是数据库
- 任何引入新依赖前先问"标准库或现有依赖能做吗"
- **遇到必须手动完成的任务时，必须先通知用户完成后再继续开发**：
  - 配置表更新（新增字段、新建表 → 运行 DataTableGenerator）
  - Prefab 创建与 UI 层级搭建
  - **正确顺序**：用户先定义 Prefab/配置表 → 工具生成 → 再编写对应逻辑脚本
- 输出日志使用 `FrameworkLogger`（见 [02-AI友好型日志规范.md](../AI友好型项目探讨/02-AI友好型日志规范.md)）

### 日志格式

```csharp
FrameworkLogger.Error("EventBus", $"Event=CombatEndEvent Handler=CombatModule.OnCombatEnd Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
FrameworkLogger.Warn("ModuleRunner", $"Module=QuestModule InitAsync 超时 Elapsed=30s");
FrameworkLogger.Info("EventBus", $"Handler=CombatModule Subscribe=CombatEndEvent");
```

### 常见陷阱

- `async void` 方法无法被 `await`——一律改为返回 `UniTask`
- UI 关闭时 DOTween 动画可能还在播放，需 `DOTween.Kill(target)` 或 `DOComplete`
- `[EventHandler]` 方法签名错误不会在编译期报错，ModuleRunner 注册时会校验
- 临时订阅（`EventBus.Subscribe`）必须用 `IDisposable` 管理，否则内存泄漏
- Domain Reload 会导致静态字段归零，ModuleRunner 不做静态单例
- `InitializeAsync` 中调用非 Dependencies 模块的 `GetModule<T>()` 会抛异常——改为 OnUpdate / 事件处理方法（运行时）中调用

---

## 十三、设计文档与索引

| 文档 | 内容 |
|---|---|
| [SKILL_MATRIX.md](./SKILL_MATRIX.md) | agent × skill 白名单 + 兜底规则 |
| [AGENTS.md](./AGENTS.md) | 多 Agent 协作 5 模式 |
| [conventions.md](./conventions.md) | 编码规范 |
| [资源配置规范.md](./资源配置规范.md) | ResourceModule + ResourceConfig |
| [skills/SKILLS_INDEX.md](./skills/SKILLS_INDEX.md) | 123 SKILL 分组索引 |
| [01-框架核心设计概述.md](../AI友好型项目探讨/01-框架核心设计概述.md) | 框架架构 |
| [02-AI友好型日志规范.md](../AI友好型项目探讨/02-AI友好型日志规范.md) | 结构化日志 |
| [03-模块系统详细设计.md](../AI友好型项目探讨/03-模块系统详细设计.md) | IGameModule / ModuleRunner |
| [04-事件系统详细设计.md](../AI友好型项目探讨/04-事件系统详细设计.md) | EventBus 三种模式 |
| [05-项目文件结构.md](../AI友好型项目探讨/05-项目文件结构.md) | 目录结构 |

---

## 十四、不要

- 不要绕过 agent 团队自己实现专家任务
- 不要把 skill 移到子目录 —— Claude Code 不递归扫描
- 不要在没有 `grill-me` / `grill-with-docs` 的情况下做大型设计决策（hook 会注入提醒）
- 不要在 .codex/agents/ / .agents/skills/ 直接改文件 —— source of truth 是 `.claude/agents/` 与 `.claude/skills/`，跑 `tools/sync-agents.py` 同步
- 不要把业务示例代码混入框架核心 —— 模板需要长期保持纯净

---

## 十五、压缩时保留

- 已修改的文件列表
- 当前 Phase 编号和完成状态
- 关键架构决策（如为什么选某个方案）
- 当前任务涉及的 Agent 与协作模式
