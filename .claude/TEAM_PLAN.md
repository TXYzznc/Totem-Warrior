# Multi-Agent Team — Build Plan

> Loop persistence file. Each iteration reads this, executes its step, updates `Progress`.

## Team Roster

| Layer | Agent ID | Role | Model | Status |
|---|---|---|---|---|
| PM | `producer` | 制作人 (单) | sonnet | pending |
| Design | `gd-lead` | 主设计师 (vision/GDD/balance) | opus | pending |
| Design | `gd-system` | 系统设计师 (mechanics impl) | sonnet | pending |
| Design | `level-designer` | 关卡设计师 (单) | sonnet | pending |
| Art Coord | `art-director` | 艺术总监 (lead, 风格统筹) | opus | pending |
| Art | `art-ui` | UI 设计与制作 | sonnet | pending |
| Art | `art-font` | 字体设计与制作 | sonnet | pending |
| Art | `art-vfx` | 特效设计与制作 | sonnet | pending |
| Art | `art-2d` | 2D 美术资源 | sonnet | pending |
| Art | `art-3d` | 3D 美术资源 | sonnet | pending |
| Art | `art-anim` | 动画设计与制作 | sonnet | pending |
| Client | `client-lead` | 客户端主程 (架构/设计模式) | opus | pending |
| Client | `client-unity` | Unity 引擎专家 (实现) | sonnet | pending |
| Client | `client-ta` | TA 专家 (shader/后处理/渲染) | sonnet | pending |
| Net | `net-lead` | 服务端主程 (架构) | opus | pending |
| Net | `net-backend` | 后端工程师 (实现) | sonnet | pending |
| Net | `net-db` | 数据库工程师 | sonnet | pending |
| QA | `qa-engineer` | QA (单) | sonnet | pending |
| DevOps | `devops-engineer` | DevOps (单) | sonnet | pending |
| Tools | `tools-engineer` | 工具/技能工程师 (单) | sonnet | pending |

Total: 20 agents.

## Skill Layout

⚠️ **Claude Code 只递归扫描 `.claude/skills/` 的直接子目录**，子目录嵌套会让 skill 失踪。已验证：移到子文件夹后全部从可用列表消失，回滚后恢复。

→ Skill 保持**扁平**在 `.claude/skills/<skill>/SKILL.md`。

## Skill Ownership Table

| Skill 名 | 主属团队 | 共享给 |
|---|---|---|
| project-management | producer | — |
| grill-me | producer | (all leads) |
| grill-with-docs | producer | (all leads) |
| find-skills | tools-engineer | producer |
| skill-creator | tools-engineer | — |
| xlsx | game-designer | producer, network |
| game-design-core | gd-lead | gd-system, level-designer |
| game-design-theory | gd-lead | gd-system |
| level-design | level-designer | gd-lead |
| gpt-image-2-style-library | art-director | art-ui, art-2d |
| multiplayer-game | net-lead | net-backend |
| database-schema-design | net-db | net-backend |
| backend-testing | qa-engineer | net-backend |
| testing-strategies | qa-engineer | (all) |
| agent-browser | qa-engineer | — |
| devops-deployment | devops-engineer | — |

## Access Rule (Soft Enforcement)

Claude Code 没有 skill-level 的硬隔离。隔离机制：
1. **Agent description** 限定调用场景——制作人不会被分配写 shader
2. **System prompt 白名单**——明列 "Available skills" + "Do not invoke other skills"
3. **Tools 字段**——限制可调用的工具集（Bash/Edit/Write/Skill 等）

新 skill 通过 skill4agent MCP 安装时也写在 `.claude/skills/<name>/` 扁平结构里。

## Progress

- [x] Iter 1: 目录结构 + plan 文件
- [x] Iter 2: ❌ 移动 skills 子目录 — 发现破坏 skill 发现，已回滚为扁平结构
- [x] Iter 3: producer + gd-lead/gd-system/level-designer agent 定义 (4)
- [x] Iter 4: art-director + 6 specialists (art-ui/art-font/art-vfx/art-2d/art-3d/art-anim) (7)
- [x] Iter 5: client-lead / client-unity / client-ta (3)
- [x] Iter 6: net-lead / net-backend / net-db / qa-engineer / devops-engineer / tools-engineer (6)
- [x] Iter 7: 安装新 SKILL — 大部分成功，7 个失败待修
- [x] Iter 8: 重试 6/7（unity-debug-patterns 不存在 skip）+ 团队冲突检查（结论：保留所有 skill，路由文档化）
- [x] Iter 9: 补 font-designer (typeset + font-pairing) + client (unity-foundations + input + async)
- [x] Iter 10: 写 SKILL_MATRIX.md，更新 4 个 client/font agent 文件，验证 20/20 frontmatter OK，退出 loop

## 终态

- **Agent 文件**：20 个，全部 frontmatter 合法
- **SKILL 总数**：116 个
  - 16 个原始（项目自带）
  - 81 个 skill4agent 生态安装
  - 19 个自建（5 个并行 agent 完成）
- **元数据清理**：移除所有 `.skill4agent.json` / `SKILL.en.md` / `SKILL.toon`
- **agent 文件**："待装"标记全部清除（已安装的归并白名单，未来需要的标"已自建"）
- **配套文档**：TEAM_PLAN.md, SKILL_MATRIX.md, install_skills.sh, skill_lists/*.txt

## 自建的 19 个 SKILL

**client-unity** (5)：unity-architecture-di / save-serialization / state-machine / physics-collision / localization-i18n
**gd-system + level-designer** (4)：quest-mission-design / achievement-design / difficulty-curve / player-guidance
**devops** (3)：unity-build-pipeline / steam-deploy / addressables-hotfix
**qa** (3)：crash-analytics / mobile-device-testing / playtest-digital
**art-font + tools** (4)：font-selection-cjk / font-subsetting / pixel-font-rendering / unity-editor-scripting

## Notes

- 模型映射：lead = opus, specialist/single = sonnet
- 字段格式：`name`, `description`, `tools`, `model` in YAML frontmatter
- 已有 16 个 skill 全部入库，无丢弃
- 新装 SKILL 来源：skill4agent MCP（已验证可用）
