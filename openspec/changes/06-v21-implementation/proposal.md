# Proposal — 06-v21-implementation

> **范围**：把 [05-gdd-v2-full-design-docs 归档](../archive/2026-06-25-05-gdd-v2-full-design-docs/) 的 v2.1 全套 GDD 落到代码 + 美术资源 + 场景 + UI + VFX，跑通完整 vertical slice。
> **决策日期**：2026-06-25
> **决策方式**：基于 v2.1 GDD 全套（37 份文档已经用户 review 通过），无需新一轮 grill。
> **驱动**：用户 Auto Mode 持续推进，多 Agent 并行实施。

## 为什么做

v2 已交付的代码（[01-tattoo-framework-rewrite](../archive/01-tattoo-framework-rewrite/)）只覆盖：
- 纹身 21 策略 + 5 配置表 + 336 测试
- CombatModule（单玩家、UI Toolkit 版 HUD）
- SpawnerModule（4 红 cube 敌人 + 1 玩家）
- VFXModule

v2.1 GDD 后**几乎所有模块要改 + 新建**，包括：
- 13 个模块详设要更新代码
- 新建 7 个模块：WeaponModule / SkillModule / NPCModule / MapGenModule / EnemyModule+BossModule / EventModule / EconomyModule / SaveModule / BotControllerModule
- UI 全部从 UI Toolkit 改为 UGUI（9 个 Form prefab）
- 美术 50+ 资源
- 场景、特效、AI 配比、测试、平衡

## 目标（DoD）

- [ ] 10-15min vertical slice 可 Play：单人单局 + 49 个 AI（20 智能 + 29 轻量）
- [ ] 玩家自纹身读条 3-8s 可见且可中断
- [ ] 纹身师附魔流程可演示
- [ ] 商人交易（颜料 + 武器 + 技能 + 消耗品）可演示
- [ ] 死亡宝箱半半规则可演示
- [ ] 3 主题地图至少 1 个可玩（Hades 风占位资源）
- [ ] 2 技能槽（Q/E）+ 闪避 + 蓄力 + 普攻 + 缩圈三段全部联动
- [ ] EditMode + PlayMode 测试 >= 90% 通过
- [ ] v2.1 GDD 12 个新 CONTRACT 事件全部接入

## 非目标（明确不做）

- ❌ 音频资源（用户后期导入）
- ❌ 真联机网络代码（伪联机 = AI 替代）
- ❌ 多语言/本地化
- ❌ 移动端适配（PC 优先，移动端接口预留但不实现）
- ❌ 商店发版相关（Steamworks / App Store）

## 阶段拆分

详见 [tasks.md](./tasks.md)。

| Phase | 内容 | 主导 Agent |
|---|---|---|
| **3-A 骨架** | CONTRACT v2.1 事件类 / IPlayerController 抽象 / CombatModule 重构 | 主对话 |
| **3-B 核心模块** | TattooModule 加读条 / WeaponModule / SkillModule MaxSlot=2 | client-lead × N |
| **3-C 装备体系** | EconomyModule / NPCModule / EventModule / SaveModule | client-unity × N |
| **3-D 地图与战斗** | MapGenModule（BSP + 缩圈） / EnemyModule+BossModule / SpawnerModule 改造 | client-lead + client-unity |
| **3-E AI 大脑** | BotControllerModule + SmartBot/LightBot 实现 + BotBuildPlanner | client-lead |
| **3-F UI 全 UGUI 改造** | UIModule 改 + 9 Form prefab | client-unity × 多 |
| **3-G 美术资源** | 角色 prefab / 50+ icon / NPC / 怪物 / 场景占位 | art-2d / art-3d / art-vfx |
| **3-H VFX 升级** | 自纹身读条 / 附魔光效 / Boss 进场 | client-ta |
| **3-I 集成测试** | 跑通整局 / 修 bug / 调平衡 | qa-engineer |
| **3-J 归档** | 同步 INDEX / archive change | 主对话 |

## 风险

| 风险 | 缓解 |
|---|---|
| 模块互相依赖导致编译断 | 骨架先行：事件 + IPlayerController 锁死后再并行填充 |
| 13 个模块同时改 GitHub conflict | 多 Agent 各自负责自己文件，不交叉编辑 |
| 50+ 美术资源量大 | Codex 批量出 icon + Hades 风占位用现成素材包 |
| 50 actor 性能 | 按 v2.1 GDD 的 LOD 分桶严格执行 |
| 数值平衡破坏 | 12-数值平衡 v2.1 已定 公式 + PvpDamageCap 落地 |

## 引用

- [v2.1 GDD 入口](../../../项目知识库（AI自行维护）/GDD-v2/00-总策划案v2.md)
- [CONTRACT.md](../archive/2026-06-25-05-gdd-v2-full-design-docs/CONTRACT.md)
- [.claude/AGENTS.md](../../../.claude/AGENTS.md) 模式 5（骨架先行 + 并行填充）
