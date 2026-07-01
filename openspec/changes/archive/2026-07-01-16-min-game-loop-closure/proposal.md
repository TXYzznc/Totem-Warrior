---
created: 2026-06-30
status: in-progress
depends-on: 15-playtest-driver
---

# 提案：最小游戏闭环 — 自动 loop 收敛

## 一句话目标
通过自动 loop（测试→整理 bug→fan-out 修复→再测试）把当前 14 TC 测试结果从「9 PASS / 1 PARTIAL / 2 FAIL / 3 BLOCKED + 7 bug」收敛到「13 个核心 TC 全 PASS + 0 Console Error」。

## 为什么
[15-playtest-driver](../15-playtest-driver/) 已验证 playtest-driver 能跑通；但实际测下来 11 个 IUIForm 里只有 MainMenuForm 成功 Register，导致 CombatHUD/SelfTattoo/Pause 等核心 Form 全部不响应事件。这不修，GDD v2.1 任何"完整玩家旅程"都无从谈起。本次目标缩到 GDD 的最小闭环（B 方案：MainMenu→战斗→SelfTattoo→Pause→GameOver→MainMenu），不含 NPC/ThreeChoice，验证框架基础能力。

## 与用户的 5 条共识（grill-me 退出快照）

| 维度 | 决议 |
|---|---|
| 目标 | loop 自动收敛到 B 闭环 + 0 Console Error |
| 关键决策 A/B | 修复 agent **按 bug 根因分组 fan-out**；不按文件、不串行 |
| 边界 | B 闭环：6 个核心 Form（MainMenu/CombatHUD/SelfTattoo/PauseMenu/RunResult + Loading）；**不含** NPC/Shop/ThreeChoiceForm |
| 验收 | 13 个最小 TC 全 PASS（沿用 [15 plan.md](../15-playtest-driver/tests/plan.md) 去掉 TC-04 StartButton 真路径外的全部条目）+ Console errors == 0；Warning 单独记 bug list 不阻塞 |
| 约束 | loop 节奏 = 动态自步；不可碰 `Assets/Scripts/Core/*` + `.claude/*`；可改 UI Modules / GameState / Prefab / Editor/Playtest / openspec/；同一 bug 连续 5 轮未解决 → 终止 loop |

## 不做什么
- 不实现 NPCModule / ShopForm / TattooStudioForm / TattooEnchantForm（D 方案的 NPC 流）
- 不实现 ThreeChoiceForm 三选系统（C 方案）
- 不修非阻塞 Warning（只记，不修）
- 不动 `Assets/Scripts/Core/IGameModule.cs` 等框架核心契约
- 不写真实美术素材（继续用现有 Prefab，缺贴图用占位）

## 验收
- [ ] `tests/min-plan.md` 的 13 条 TC 全部 PASS
- [ ] `console_get_stats` 返回 `errors == 0`
- [ ] `tests/bugs.md` 中 status=OPEN 的 High 级别 bug 数量 = 0
- [ ] `loop-state.md` 显示「成功收敛」状态
