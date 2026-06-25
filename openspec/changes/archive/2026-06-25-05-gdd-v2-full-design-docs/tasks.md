# Tasks — 05-gdd-v2-full-design-docs

> 实施进度。✅ = 已交付；🟡 = 进行中；🔲 = 未开始。
> **本 change 零代码改动**。所有任务的产出是 markdown 文档。

---

## Phase B-1 — 骨架（主对话单干）

- [x] proposal.md
- [x] design.md
- [x] tasks.md（本文件）
- [x] brainstorm.md（Phase A 共识沉淀）
- [ ] CONTRACT.md 骨架（事件总表 + 模块依赖图 + IPlayerController 抽象 + 50 actor 性能预算）
- [ ] 创建 `项目知识库（AI自行维护）/GDD-v2/` + `systems/` + `modules/` 子目录

---

## Phase B-2 — 总策划案 v2（gd-lead + producer）

- [ ] `GDD-v2/00-总策划案v2.md`（1 篇，1500–3000 字）
  - 入口文档，统辖所有系统 GDD + 模块详设
  - 含玩家旅程图（FTUE 第 1 → 第 10 局）
  - 内部链接到 15 系统 GDD + 16 模块详设 + CONTRACT

---

## Phase B-3 — 15 系统 GDD（DAG 并行）

### 第 1 批（无依赖，并行 Fan-Out）

- [ ] `GDD-v2/systems/01-纹身构筑系统.md`（gd-lead，沿用并扩展已有 05B v2）
- [ ] `GDD-v2/systems/06-角色设定与骨架.md`（gd-lead）
- [ ] `GDD-v2/systems/15-世界观与轻剧情.md`（gd-lead）

### 第 2 批（依赖第 1 批）

- [ ] `GDD-v2/systems/02-战斗手感.md`（gd-lead，依赖 01 + 06）
- [ ] `GDD-v2/systems/11-怪物与Boss.md`（gd-system，依赖 06）
- [ ] `GDD-v2/systems/12-数值平衡与曲线.md`（gd-system，等 02 + 03 + 04 + 11 写完后再终稿）

### 第 3 批（依赖第 2 批）

- [ ] `GDD-v2/systems/03-武器系统.md`（gd-system，依赖 02）
- [ ] `GDD-v2/systems/04-主动技能.md`（gd-system，依赖 02）
- [ ] `GDD-v2/systems/05-闪避与身法.md`（gd-system，依赖 02）

### 第 4 批（最后并行）

- [ ] `GDD-v2/systems/07-地图生成.md`（level-designer）
- [ ] `GDD-v2/systems/08-宝箱与探财节奏.md`（gd-system，依赖 07）
- [ ] `GDD-v2/systems/09-纹身师与商人NPC.md`（gd-system，依赖 07 + 08）
- [ ] `GDD-v2/systems/10-事件与三选一.md`（gd-system，依赖 07）
- [ ] `GDD-v2/systems/13-UI与HUD.md`（art-ui，依赖前 12 篇）
- [ ] `GDD-v2/systems/14-音效与环境音.md`（art-director，依赖前 12 篇）

> 第 2 / 3 / 4 批之间显式 `await`（等上一批完成后再调下一批 agent）。批内并行。

---

## Phase B-4 — 16 模块详设（全部并行 Fan-Out）

> 模块详设通过 CONTRACT.md 解耦，可一次性全部派发。

### 客户端架构敏感（client-lead）

- [ ] `GDD-v2/modules/01-TattooModule.md`（基于已交付 + 50 actor 补强）
- [ ] `GDD-v2/modules/02-CombatModule.md`（基于已交付 + 50 actor 补强）
- [ ] `GDD-v2/modules/05-InputModule.md`（基于已有 + IPlayerController 抽象）
- [ ] `GDD-v2/modules/07-MapGenModule.md`（新建）
- [ ] `GDD-v2/modules/16-BotControllerModule.md`（新建，AI 行为核心，分级 LOD）

### Unity 实现敏感（client-unity）

- [ ] `GDD-v2/modules/03-WeaponModule.md`
- [ ] `GDD-v2/modules/04-SkillModule.md`
- [ ] `GDD-v2/modules/06-SpawnerModule.md`（基于已交付 + 50 actor 补强）
- [ ] `GDD-v2/modules/08-EnemyModule+BossModule.md`
- [ ] `GDD-v2/modules/09-NPCModule.md`
- [ ] `GDD-v2/modules/10-EventModule.md`
- [ ] `GDD-v2/modules/11-EconomyModule.md`
- [ ] `GDD-v2/modules/12-UIModule+各UIForm.md`（基于已有 + 新增表单清单）
- [ ] `GDD-v2/modules/13-AudioModule.md`
- [ ] `GDD-v2/modules/14-SaveModule.md`

### TA 敏感（client-ta）

- [ ] `GDD-v2/modules/15-VFXModule.md`（基于已交付 + 50 actor 性能预算）

---

## Phase B-5 — 验证与归档（主对话）

- [x] CONTRACT.md 终稿（B-4 期间追加 5 个事件：BotLODChangedEvent / AttackHitEvent.WeaponId / EnemySpawnedEvent / BossSpawnedEvent / BossPhaseChangedEvent / AmmoChangedEvent / SkillSlotChangedEvent）
- [x] `项目知识库（AI自行维护）/INDEX.md` 同步 GDD-v2 入口
- [x] 在本 change `tasks.md` 上方追加最终统计（见下方"最终统计"）
- [ ] 跨文档引用一致性 check（mermaid 图模块名 vs 目录文件名）
- [ ] 用户最终 review 后 `openspec archive-change`

---

## 最终统计（v2 + v2.1，2026-06-25）

| 项 | 数量 |
|---|---|
| 总策划案 v2 / v2.1 | 1（升级） |
| 系统 GDD（玩家视角） v2 / v2.1 | 15（11 篇升级） |
| 模块详设（工程视角） v2 / v2.1 | 16（13 篇升级） |
| openspec 骨架文档 | 5 |
| **合计正式文档** | **37** |
| 累计字数估算 | ~70,000 字 |
| Phase B 历时 | 单次 session 内全部产出 |
| 调用的子 Agent 总次数 | ~57 次（多 Agent 并行 fan-out + v2.1 重写） |
| CONTRACT 追加事件 / 字段 | **12 个**（v2: 7 + v2.1: 5） |
| Phase B 阶段 | B-1 骨架 → B-2 总策划 → B-3 15 GDD → B-4 16 详设 → v2.1 grill 16 轮 → 修订全套 |
| 零代码改动 | ✓（Assets/Scripts/ 未触碰一字节） |

## v2.1 修订一览（用户 grill 16 轮共识）

24 项修订全部落地：UGUI + 轻量 MVP / 纹身师=词缀附魔工 / 玩家自纹身读条 3-8s / 局外解锁（去掉初始库存）/ 配方 4 来源 / 颜料三档 / 死亡宝箱半半规则 / 10-15min / 2 技能槽 / 去圈外刷稀有 / 20+29+1 AI 配比 / 关键 NPC 伪语配音 / Hades 精致 2.5D / 异能者身份 / 4-5 部位节奏 / Run 重现 / 初始直线+圆环 / 商人卖品调整 等。

---

## 兜底原则

- 任何 Agent 写到一半遇到与 Phase A 共识冲突 → escalate_to: main，主对话弹窗对齐
- 任何 Agent 试图改 `Assets/Scripts/` 代码 → 立即停（本期零代码改动）
- 任何 Agent 试图调 `Edit` 或 `Bash` 改非自己产出的文件 → 立即停
- 文档内"⭐⭐⭐ 8 项未决"逐项给出推荐方案 + 替代方案 + 取舍，**不打断用户**
