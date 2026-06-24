# 重构报告 v1.0 — 「去其糟粕，取其精华」

> 日期：**2026-06-24**
> 目标：把合并后的 `AI_Friendly_Project` 重构为**纯模板**——业务清洁、配置高效、协作友好。
> 基础：在 2026-06-24 上半天的 **MERGE v1.0**（GameDesinger 工具链合入）基础上做。

---

## 一、糟粕清单（12 项 → 全部处理）

| # | 糟粕 | 处理 |
|---|---|---|
| 1 | 三处镜像（`.claude/agents/` + `.codex/agents/` + `.agents/skills/`）维护不一致 | **单源 + 生成**：source = `.claude/agents/`，写 [`tools/sync-agents.py`](./tools/sync-agents.py) 一键同步另两处 |
| 2 | 4 个原生 agent（code-reviewer / bug-tracer / datatable-helper / ui-scaffold）与 20 团队职能重叠 | **砍 4 个**：code-reviewer 走通用 SKILL；bug-tracer 并入 qa-engineer；datatable-helper + ui-scaffold 职责写入 client-unity system prompt |
| 3 | skills 冗余（redis-best-practices vs redis-specialist、game-design-core vs theory、agency-* vs 通用）| **不动 skill 内容**（用户选了"保留全部 124"）。仅在 SKILLS_INDEX + SKILL_MATRIX 中明确白名单与推荐顺序 |
| 4 | 项目特定 skills（casino / hytale）→ 与"通用模板"冲突 | **保留**（不进上下文）但归入 system designer / 2D agent 的"可选推荐" |
| 5 | 跨引擎 skills 干扰 Unity 决策 | **保留**为 reference，agent 白名单只放 Unity 主线 |
| 6 | `.mcp.json` 占位符泄露（`YOUR_HUGGINGFACE_TOKEN` 等）| **`${VAR}` + [.env.example](./.env.example)**，凭据走环境变量 |
| 7 | tools/ 687 MB 二进制 | **不动 + `.gitignore` 排除**（用户决策）。新机器按 [setup.md](./setup.md) 重建 |
| 8 | MCP 默认全开（mongodb / atlassian 等强制启用）| **三档分类**：core 默认开 / middle 默认开 / optional 默认关；[.claude/settings.local.json](./.claude/settings.local.json) 的 `enabledMcpjsonServers` 只列 core+middle |
| 9 | hooks 散落（人格 + ai-art 触发）但没决策门槛 | **三重 hook**：人格注入 + 决策门槛（适中关键词集）+ ai-art 触发 |
| 10 | 顶层文档（CLAUDE.md / AGENTS.md / CONTEXT.md / README.md / MERGE_REPORT）内容互抄 | **CLAUDE.md = 单一权威**；AGENTS.md（顶层）= Codex 入口简版；CONTEXT.md = 状态全景；README.md = 一页 README；MERGE_REPORT → 本 REFACTOR_REPORT |
| 11 | 缺 SKILL_MATRIX | **新增** [.claude/SKILL_MATRIX.md](./.claude/SKILL_MATRIX.md) — 20 agent × skill 白名单 + 兜底规则 + 强制门槛 |
| 12 | 飞机大战业务残留（DataTable / Events / Modules / UI / 资源）| **整体清理**：cs 文件 61 → 34；Resources 业务素材删；Templates/ 引入 4 个 `.cs.txt` 模板 |

---

## 二、精华提取与强化

| 来源 | 精华 | 强化方式 |
|---|---|---|
| AI_Friendly | 八荣八耻 AI 行为准则 | 保留在 CLAUDE.md §一与顶层 AGENTS.md |
| AI_Friendly | 5 Phase 工作流 + OpenSpec 集成 | QUICKSTART.md 更新到 20 agents 路由 |
| AI_Friendly | 自研 IGameModule 框架设计哲学 | 框架核心 8 .cs + 5 篇设计文档保留 |
| AI_Friendly | 多 Agent 协作 5 模式（含「骨架先行+并行填充」）| `.claude/AGENTS.md` 保留 |
| AI_Friendly | 项目知识库（AI 自维护） | INDEX.md 模板化 + 增加 wiki 条目规范 |
| GameDesinger | 20 人职能团队（lead/system/impl 分层） | **全量重写 system prompt**：每个 agent 加 frontmatter（tier / skills / escalate_to）+ 标准化「你做 / 你不做 / 工作准则 / SKILL 白名单 / 何时交回主 agent / 输出格式」六段结构 |
| GameDesinger | codebase-memory MCP 准则 | CLAUDE.md §八 + AGENTS.md 写入；MCP core 默认开 |
| GameDesinger | 双轨 .codex 支持 | source = .claude/，sync-agents.py 自动生成 |
| GameDesinger | 与 Unity 强相关的 116 skills | SKILLS_INDEX 按职能分组 + SKILL_MATRIX 显式映射到 20 agents |

---

## 三、变更明细

### 3.1 agents（删 4 + 重写 20）

| 操作 | 数量 | 备注 |
|---|---|---|
| 删除 | 4 (claude) + 4 (codex) | code-reviewer / bug-tracer / datatable-helper / ui-scaffold |
| 重写 | 20 | 统一 frontmatter（含 `tier` / `skills` / `escalate_to: main`）+ 六段 body 结构 |
| 同步 | sync-agents.py | source = `.claude/agents/*.md` → 生成 `.codex/agents/*.toml` + 镜像 `.agents/skills/` |

### 3.2 配置文件

| 文件 | 变化 |
|---|---|
| `.claude/CLAUDE.md` | 重写：移除 4 原生段、新增 hook 说明、兜底说明、引 SKILL_MATRIX、加 tools/ 说明 |
| `.claude/SKILL_MATRIX.md` | **新增** — 20 agents × skill 白名单 + 兜底规则 + 强制门槛 |
| `.claude/settings.json` | 升级：人格 hook + 决策门槛 hook（关键词适中集）+ ai-art 触发 hook |
| `.claude/settings.local.json` | 启用列表精简到 core+middle MCP；permissions 增加 codebase-memory 工具 |
| `.mcp.json` | 三档分类（`_____CORE_____` / `_____MIDDLE_____` / `_____OPTIONAL_____` 分隔符）；占位符全部 `${VAR}` 化 |
| `.env.example` | **新增** — HF_TOKEN / MDB / JIRA / CONFLUENCE / GODOT_PATH / API key 占位 |
| `AGENTS.md`（顶层） | 重写：Codex 入口，20 路由 + 兜底说明 + MCP 准则 + 八荣八耻 |
| `CONTEXT.md` | 重写：模板状态全景 + 目录结构 + 关键设计决策 17 条 |
| `README.md` | 重写：一页 README，定位为「模板」 |
| `REFACTOR_REPORT.md` | **新增**（取代 MERGE_REPORT.md） |
| `工作/QUICKSTART.md` | Edit：Phase 2/3/4 加 agents 路由表 |
| `项目知识库（AI自行维护）/INDEX.md` | 重写：模板化 + 加 wiki 条目规范 |

### 3.3 业务代码清理

| 类别 | 删除项 |
|---|---|
| DataTable .cs（业务） | EnemyBulletConfig / EnemyConfig / PlayerConfig / WaveConfig / SpawnConfig / LevelConfig / ItemTable |
| Events | STGEvents |
| Modules（整目录）| Bullet / Enemy（含 AI/）/ Level / Player |
| Modules/UI | GameHUD / GameOverUI / GameplayBackgroundScroller / LevelSelectUI / MainMenuUI / MenuBackgroundUI / VictoryUI |
| 示例/ | DOTweenExample / DOTweenAdvancedExample，**示例/ 目录整体删** |
| Resources/DataTable | 7 个业务 .json（保留 ResourceConfig.json 与 README） |
| Resources/Sprite | Bullets / Characters / UI 子目录 |
| Resources/Prefab | Bullet.prefab |
| Resources/Font | VONWAONBITMAP（业务字体）|

**保留**：Core/ (8) + Utils/ (8) + DataTableRegistry + ResourceConfig（schema 框架级）+ DataTable Module + Resource + GameState + Input + Scene + UI（仅 UIModule）+ Flow + ExternalAPI/Handlers

**统计**：`.cs` 文件 **61 → 34**。

### 3.4 模板（新建）

`Assets/Scripts/Templates/`：

| 模板 | 用途 |
|---|---|
| ModuleTemplate.cs.txt | IGameModule 标准实现 |
| EventTemplate.cs.txt | 广播事件类 |
| RequestTemplate.cs.txt | 请求-响应（RequestAsync / RequestHandler）|
| DataTableTemplate.cs.txt | IDataTable schema 参考（generator 产物示意） |
| README.md | 模板使用规则（`.cs.txt` 不编译）|

### 3.5 工具

| 文件 | 作用 |
|---|---|
| `tools/sync-agents.py` | 从 `.claude/agents/*.md` 生成 `.codex/agents/*.toml` + 镜像 `.agents/skills/`。CI 友好（`--check` 模式）|

---

## 四、关键设计决策

1. **AI_Friendly_Project = 纯模板**，GameDesinger 实战项目不动
2. **20 人团队定型**：lead 5 / system 3 / impl 12
3. **agent system prompt 标准化**：六段 + frontmatter 强约束
4. **每个 agent 都有「何时交回主 agent」清单**（用户明确要求列出条件）
5. **决策门槛硬约束**：hook + 各 lead agent system prompt 双重
6. **MCP 三档**：core 默认开 / middle 默认开 / optional 默认关
7. **凭据 .env 化**：仓库内无明文 token
8. **业务清洁**：Launch.unity 保留但有 missing reference，README 显式告知

---

## 五、验证

```bash
$ ls .claude/agents/      | wc -l    # 20
$ ls .codex/agents/       | wc -l    # 20（待 sync 后保持一致）
$ ls .claude/skills/      | wc -l    # 124（含 SKILLS_INDEX.md）
$ find Assets/Scripts -name "*.cs" | wc -l    # 34
$ find Assets/Scripts/Templates -name "*.cs.txt" | wc -l    # 4
$ test -f .env.example && echo ok    # ok
$ test -f .claude/SKILL_MATRIX.md && echo ok    # ok
$ test -f tools/sync-agents.py && echo ok    # ok
```

`.mcp.json` 三档分隔符存在；`settings.json` 含 3 个 UserPromptSubmit hook 子项（人格 / 决策门槛 / ai-art）。

---

## 六、已知遗留与下一步

| 优先级 | 内容 |
|---|---|
| 🔴 高 | **Launch.unity** 含 missing script reference（业务脚本删除后）。首次打开 Unity 需在场景中清理或重建空场景再挂 `GameApp`。 |
| 🟡 中 | `tools/sync-agents.py` 尚未首次跑：需要 `pip install pyyaml` 后运行 `python tools/sync-agents.py` 让 `.codex/agents/*.toml` 真正匹配重写后的 `.claude/agents/*.md` |
| 🟡 中 | `.codebase-memory/` 索引数据未带；首次 codebase-memory MCP 启动时重新索引 |
| 🟡 中 | `工作/QUICKSTART.md` 仍含"背包系统"案例，模板纯净度可进一步提升 |
| 🟢 低 | OpenSpec 的 `openspec/` 顶层目录现在为空；首次 `/openspec new change` 时会自建 |
| 🟢 低 | `项目知识库（AI自行维护）/wiki/` 是空的，随项目开发由 AI 填充 |

---

## 七、Roll-back 指引

如需回滚到 MERGE v1.0 状态：
1. 还原 `.claude/agents/*.md`（备份 commit 前的版本）
2. 还原 4 个原生 agent toml
3. `git checkout HEAD~ -- .claude/CLAUDE.md .claude/settings.json .mcp.json`
4. 删除 `.env.example` / `tools/sync-agents.py` / `.claude/SKILL_MATRIX.md`
5. 重建 `Assets/Scripts/Modules/Bullet|Enemy|Level|Player|UI` 业务代码（从备份）

> ⚠️ 用户已声明本项目是基于备份操作，**不需要走 git 回滚**。

---

*执行者：Claude Code (Opus 4.7)*
*重构方式：交互式弹窗 + 顺序执行 14 步*
