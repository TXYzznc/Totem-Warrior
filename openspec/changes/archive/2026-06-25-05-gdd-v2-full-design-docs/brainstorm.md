# Brainstorm — 05-gdd-v2-full-design-docs

> **Phase A 共识沉淀**（grill-me 多轮反问后退出）
> **沉淀日期**：2026-06-25
> **下一阶段**：Phase B 自动执行，不再请求用户审批

---

## 阶段 A 5 条挖透摘要

| # | 维度 | 共识 |
|---|---|---|
| 1 | 核心目标 | 以纹身构筑（6 部位 × 7 颜色 × 8 图案 = 336 组合）为核心机制的俯视角 2.5D 肉鸽大逃杀。世界观：末日废墟（AI 叛乱 / 外星 / 病毒），玩家身份：实验体。15-25 分钟一局，50 actor 同场。 |
| 2 | A/B 决策 | (a) 单局形态 = Roguelike+BR（已沉淀于 [初版GDD-2026-06](../../../项目知识库（AI自行维护）/raw/初版GDD-2026-06)）。(b) **本期范围 = 伪联机**（单机 + 人机 AI 替代真人；未来开通真联机时部分 AI 替换为真人）。(c) **AI 完备度 = 分级混合**（8-10 智能 AI 完整对齐真人 build/探宝/PVP + 40 轻量 AI 走动还击）。(d) **策划案颗粒度 = 两层都要**（15 系统 GDD + 16 模块详设）。(e) **本期交付边界 = 先文档后代码**。 |
| 3 | 不做（边界） | 本期不写代码。不做真联机/Mirror/FishNet 集成。不做 50 人专用服务器。不做局外解锁系统的实施（系统 GDD 写但不落地）。不做美术资源实施（写 art bible，不出图）。 |
| 4 | 验收标准 | 交付物 = **33 份文档**：1 份总策划案 v2 + 15 份系统 GDD + 16 份模块详设 + 1 份 CONTRACT.md（事件总表 + 模块依赖图 + IPlayerController 抽象）。每份 GDD 含: 目标、机制、状态机/公式/表、依赖与被依赖、风险与开放问题。 |
| 5 | 关键约束 | 1 人 + 多 Agent + Codex/Unity REST/openspec 工具栈，不按传统单人产能估算。文档语言中文。架构契约：从 day 1 起就按"50 actor 同场 + IPlayerController 统一抽象"设计，未来切换网络回放零改动。 |

---

## 隐含技术契约（架构层）

伪联机决策推导出的强约束：

1. **`IPlayerController` 抽象**：玩家与 AI 共用入口，未来加入 `NetworkPlayerController`（接网络回放）只是新增实现，**不需要改任何业务模块**。
2. **战斗与决策完全数据驱动**：所有"装备 / 攻击 / 闪避 / 走位"决策走事件 + DataTable，不区分 actor 是人还是 AI。
3. **50 actor 性能预算**：上来就要做"AI 决策粒度 LOD"——距离玩家近的 actor 高频决策（每帧），远的低频（1-2s）。
4. **AI 装备纹身的数据通路**：智能 AI 走与玩家完全一致的 `TattooModule.Equip` API，但决策由 `BotBuildPlanner` 提供"想刻什么 build"。

---

## 角色分工（多 Agent 编排）

按 [AGENTS.md](../../../.claude/AGENTS.md) 模式 5「骨架先行 + 并行填充」：

### Step 1 — 骨架（串行，主对话单干）

- proposal.md / design.md / tasks.md（本目录）
- CONTRACT.md（事件总表 + 模块依赖图 + IPlayerController + 50 actor 架构）

### Step 2 — 总策划案 + 15 系统 GDD（顺序 + 并行）

| # | 文档 | 主 Agent | 协作 Agent |
|---|---|---|---|
| 总 | 总策划案 v2 | gd-lead | producer |
| 01 | 纹身构筑系统 GDD | gd-lead | gd-system |
| 02 | 战斗手感系统 GDD | gd-lead | client-lead |
| 03 | 武器系统 GDD | gd-system | gd-lead |
| 04 | 主动技能系统 GDD | gd-system | gd-lead |
| 05 | 闪避与身法 GDD | gd-system | gd-lead |
| 06 | 角色设定与骨架 GDD | gd-lead | art-director |
| 07 | 地图生成系统 GDD | level-designer | gd-system |
| 08 | 宝箱与探财节奏 GDD | gd-system | level-designer |
| 09 | 纹身师 / 商人 NPC GDD | gd-system | level-designer |
| 10 | 事件与三选一 GDD | gd-system | gd-lead |
| 11 | 怪物与 Boss GDD | gd-system | level-designer |
| 12 | 数值平衡与曲线 GDD | gd-system | gd-lead |
| 13 | UI / HUD / 打板 GDD | art-ui | gd-lead |
| 14 | 音效与环境音 GDD | art-director | gd-lead |
| 15 | 世界观与轻剧情 GDD | gd-lead | art-director |

### Step 3 — 16 模块详设（并行）

| # | 模块 | 主 Agent |
|---|---|---|
| 01 | TattooModule 详设 | client-lead |
| 02 | CombatModule 详设 | client-lead |
| 03 | WeaponModule 详设 | client-unity |
| 04 | SkillModule 详设 | client-unity |
| 05 | InputModule 详设 (已存在,补齐 IPlayerController 抽象) | client-lead |
| 06 | SpawnerModule 详设 | client-unity |
| 07 | MapGenModule 详设 | client-lead |
| 08 | EnemyModule + BossModule 详设 | client-unity |
| 09 | NPCModule 详设 | client-unity |
| 10 | EventModule 详设 | client-unity |
| 11 | EconomyModule 详设 | client-unity |
| 12 | UIModule + 各 UIForm 详设 | client-unity |
| 13 | AudioModule 详设 | client-unity |
| 14 | SaveModule 详设 | client-unity |
| 15 | VFXModule 详设 (已存在,补齐 50 actor 性能预算) | client-ta |
| 16 | BotControllerModule + AI 行为详设 | client-lead |

### Step 4 — 验证与归档（主对话单干）

- 全文档相互引用一致性 check
- 同步 `项目知识库（AI自行维护）/INDEX.md`
- `openspec verify-change` + `archive-change`
