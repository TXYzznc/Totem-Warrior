---
created: 2026-07-01
status: in-progress
depends-on: 16-min-game-loop-closure
---

# 提案：最小完整流程 loop — Round 2（含 change 18/20/22 回归）

## 一句话目标
以 loop 自动跑「22 TC 测试 → bug 归纳 → 根因分组 fan-out 修复 → 回归」，收敛到 **22 TC 全 PASS + Console errors==0**，覆盖 change 18/19/20/22 归档后的新能力。

## 为什么
[16-min-game-loop-closure](../16-min-game-loop-closure/) Round 1 已把 B 闭环（13 TC）跑成 PASS，但自 2026-06-30 至今又归档了 4 个 change：
- 18-weapon-pickup-upgrade（武器拾取升级）
- 19-visual-polish（视觉打磨）
- 20-player-attack-system（玩家攻击系统 D4/D7/D8）
- 22-gameplay-visual-polish（Bot 染色 + 阴影 + VFX + Audio 桥）

期间产生了两条修复 commit（a393635 "loop2 归档后遗留错误" + 79e8471 "二次开局 HP 残留"）已进主干，但**未跑一次覆盖全部新能力的完整回归**。本轮 loop 补齐这一缺口，并将 13 TC 扩至 22 TC 把新能力钉死。

## 与用户的 5 条共识（grill-me 退出快照 · 2026-07-01）

| 维度 | 决议 |
|---|---|
| 目标 | loop 自动收敛到 22 TC + 0 Console Error |
| 关键决策 A/B | TC 范围：13 基线 + 9 新（合计 22）；报错口径：严格 Error==0，Warning 不阻塞；修复协作：根因分组 fan-out；终止安全网：仅「同 bug 5 轮未解」 |
| 边界 | 22 TC：MainMenu → 战斗（含武器拾取升级/攻击/技能/闪避/hit VFX/Bot 染色/击杀）→ SelfTattoo → Pause → RunResult → 回主菜单；**不含** NPC/Shop/ThreeChoiceForm/TattooStudio |
| 验收 | 22 条 TC 全 PASS + `console_get_stats.errors == 0`；Warning 单独记 `tests/bugs.md` 不阻塞 |
| 约束 | loop 节奏 = 动态自步；**不可碰** `Assets/Scripts/Core/*` + `.claude/*` + `openspec/changes/archive/*`；**可改** UI Modules / GameState / Prefab / Editor/Playtest / Shader / 当前 change 目录；同一 bug 连续 5 轮未解 → 终止 loop |

## 不做什么
- 不实现 NPCModule / ShopForm / TattooStudioForm / TattooEnchantForm / ThreeChoiceForm
- 不修非阻塞 Warning（只记）
- 不动 `Assets/Scripts/Core/**`（IGameModule / ModuleRunner / EventBus）
- 不改 `.claude/**`（agent / skill / CLAUDE.md）
- 不动 `openspec/changes/archive/**`

## 验收
- [ ] [tests/plan-22tc.md](./tests/plan-22tc.md) 的 22 条 TC 全部 PASS
- [ ] `console_get_stats` 返回 `errors == 0`
- [ ] [tests/bugs.md](./tests/bugs.md) 中 `status=OPEN` 且 severity=High 的 bug 数量 = 0
- [ ] [loop-state.md](./loop-state.md) 显示「成功收敛」

## Loop 编排（模式 5 骨架先行 + 模式 1 Fan-Out）

```
Round N：
  Step 1 (顺序): qa-engineer 跑 22 TC → 产 tests/results-round-N.md + 更新 tests/bugs.md
  Step 2 (主对话): 阅读 bugs.md，按根因分组（同一根因归一组）
  Step 3 (Fan-Out): N 个子 agent（client-unity ∥ client-ta ∥ art-ui 视根因领域分配）并行修
                    await WhenAll
  Step 4 (顺序): qa-engineer 回归验证
  Step 5: 判定 → 达标 archive / 未达标进 Round N+1 / 命中安全网终止
```

## 终止安全网

见 [loop-state.md](./loop-state.md)：同一 bug ID 连续 5 轮 status=OPEN 且无变化 → 终止交回用户。
