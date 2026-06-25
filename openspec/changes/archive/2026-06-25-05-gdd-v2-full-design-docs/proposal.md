# Proposal — 05-gdd-v2-full-design-docs

> **范围**：在 Phase 1+2 内产出全套设计文档（**零代码改动**），为后续正式实现奠基。
> **决策日期**：2026-06-25
> **决策方式**：grill-me 三轮反问 + AskUserQuestion 弹窗确认 5 条全清
> **承接共识**：[brainstorm.md](./brainstorm.md)
> **预计 artifact**：1 总策划案 v2 + 15 系统 GDD + 16 模块详设 + 1 CONTRACT.md = **33 份文档**

---

## 一、为什么做

### 1.1 现状

- **核心玩法已验证**：6 部位 × 7 颜色 × 8 图案 = 336 组合的纹身战斗框架已在 [01-tattoo-framework-rewrite](../01-tattoo-framework-rewrite/) change 内交付，336 穷举测试通过、VFX ParticleSystem 在 Play 模式实战渲染、Launch.unity 可跑通。
- **GDD 草案已成**：[项目知识库（AI自行维护）/raw/初版GDD-2026-06/](../../../项目知识库（AI自行维护）/raw/初版GDD-2026-06/) 8 份草案文件已沉淀核心方向（类型 = Roguelike+BR、视角 = 俯视角 2.5D、单局 15-25 分钟、50 人含 AI、Solo 主推）。
- **8 项 ⭐⭐⭐ ~ ⭐ 优先级未决问题**：数值平衡、武器系统、配方节奏、纹身师 NPC、死亡宝箱、AI 行为、缩圈细化、美术风格等仍为开放问题。

### 1.2 问题

现有 GDD 草案有 **3 个关键缺陷**，导致无法直接进入"正式实现"：

| 缺陷 | 表现 |
|---|---|
| **颗粒度不统一** | 系统层（如纹身）已有 detailed 设计，其他系统（武器/AI/事件/经济）只有方向，缺机制定义和数值表 |
| **架构契约缺失** | 50 actor 同场、IPlayerController 统一抽象、AI 装备纹身的数据通路等技术决策没有沉淀，多 Agent 并行实施时必然冲突 |
| **没有"工程视角"映射** | 玩家视角的"系统 GDD"与代码侧的"模块详设"之间没有桥梁，client-lead 无法据此动手 |

### 1.3 本次变更

按 Phase A 共识，**先产出全套文档，零代码改动**。

| 维度 | 决策 |
|---|---|
| 单局形态 | Roguelike + 大逃杀，25 分钟单局，50 actor 同场 |
| 本期范围 | **伪联机**：单机起步，所有非玩家位由人机 AI 填充。架构上为未来真联机零改动 |
| AI 完备度 | **分级混合**：8-10 智能 AI（与真人对齐：探宝/纹身/PVP/build 决策） + 40 轻量 AI（走动/还击） |
| 策划案颗粒度 | **两层**：15 份玩家视角的"系统 GDD" + 16 份工程视角的"模块详设" + 1 总策划案 + 1 CONTRACT |
| 本期交付边界 | **先文档后代码**：本 change 不写任何 `Assets/Scripts/` 代码 |

---

## 二、目标（DoD）

- [ ] `项目知识库（AI自行维护）/GDD-v2/` 目录下 33 份文档成型
  - [ ] 1 份 `00-总策划案v2.md`
  - [ ] 15 份系统 GDD（按 [brainstorm.md](./brainstorm.md) 列表）
  - [ ] 16 份模块详设
- [ ] 1 份 [CONTRACT.md](./CONTRACT.md)（全局事件总表 + 模块依赖图 + IPlayerController 抽象 + 50 actor 性能预算）
- [ ] [项目知识库（AI自行维护）/INDEX.md](../../../项目知识库（AI自行维护）/INDEX.md) 已更新链接到 GDD-v2/ 全套
- [ ] 每份系统 GDD 含: 目标 / 机制 / 状态机或公式 / 配置表 schema / 依赖与被依赖 / 风险与开放问题
- [ ] 每份模块详设含: IGameModule 接口签名 / Dependencies / Events / DataTable Schema / 性能预算 / 与其他模块的交互序列
- [ ] CONTRACT.md 中明确列出: 50+ 事件类全签名 / 16 模块依赖图 / IPlayerController 抽象接口 / AI 决策粒度 LOD 规范

---

## 三、非目标（明确不做）

- ❌ 本期**不写代码** —— `Assets/Scripts/` 零改动
- ❌ 不接 Mirror / FishNet / Photon Fusion（真联机延后）
- ❌ 不出美术资源（只在 GDD 内提需求，不调 ai-art / codex-image-gen）
- ❌ 不写测试代码（测试策略写入 qa GDD，但不实施）
- ❌ 不改动现有 `01-tattoo-framework-rewrite` 已交付的代码（仅在 GDD 内引用、补齐 50 actor 视角的性能预算）
- ❌ 不引入新框架依赖（自研 IGameModule 已足够）

---

## 四、阶段拆分

| Phase | 内容 | 主导 Agent | 完成标志 |
|---|---|---|---|
| **B-1** | 写 proposal/design/tasks/CONTRACT 骨架 | 主对话 | 本目录 4 份 .md 完整 |
| **B-2** | 1 总策划案 v2 | gd-lead + producer | `GDD-v2/00-总策划案v2.md` 完整 |
| **B-3** | 15 份系统 GDD | gd-lead / gd-system / level-designer / art-director / art-ui（并行 Fan-Out + DAG） | 15 份文档全部成型 |
| **B-4** | 16 份模块详设 | client-lead / client-unity / client-ta（并行 Fan-Out） | 16 份文档全部成型；CONTRACT 同步定稿 |
| **B-5** | 验证 + 归档 | 主对话 | INDEX 更新，archive 移入 |

---

## 五、风险与回滚

| 风险 | 缓解 |
|---|---|
| 33 份文档相互引用冲突 | CONTRACT.md 作为"骨架先行"提前固定接口；系统 GDD 之间冲突由主对话评审一致性 |
| Agent 并行写时跨职能侵权 | 每份文档 frontmatter 明确写出"主 Agent + 协作 Agent"；副 Agent 仅 review 不动笔 |
| 设计 vs 已交付代码不一致 | 已交付的 TattooModule / VFXModule / CombatModule 三模块的详设按"当前代码 + 50 actor 补强"写 |
| 颗粒度漂移（GDD 过细 / 过粗） | 每份 GDD 设定篇幅区间（500–2000 字），超过用引用 + 链接拆分 |

**回滚路径**：本 change 零代码改动，回滚 = 删 `openspec/changes/05-gdd-v2-full-design-docs/` 目录 + 删 `项目知识库（AI自行维护）/GDD-v2/` 即可。

---

## 六、引用

- [.claude/CLAUDE.md](../../../.claude/CLAUDE.md) §五（决策门槛两阶段 FSM）
- [.claude/AGENTS.md](../../../.claude/AGENTS.md) 模式 5（骨架先行 + 并行填充）
- [.claude/SKILL_MATRIX.md](../../../.claude/SKILL_MATRIX.md)（Agent × SKILL 白名单）
- [项目知识库（AI自行维护）/raw/初版GDD-2026-06/](../../../项目知识库（AI自行维护）/raw/初版GDD-2026-06/)（v1 草案，本期 v2 升级）
- [openspec/changes/01-tattoo-framework-rewrite/](../01-tattoo-framework-rewrite/)（已验证的核心玩法 change）
