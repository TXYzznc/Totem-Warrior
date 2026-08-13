# Native Enemy Domain Verification Report

更新时间：2026-07-10 18:30  
Unity：2022.3.62f3c1  
启动场景：`Assets/Game/Scene/Launch.unity`

## 结果摘要

| 维度 | 结果 | 证据 |
|---|---|---|
| Unity 编译 | 通过 | UnitySkills 强制重编译完成，未产生 C# 编译错误 |
| Console 错误 | 通过 | `console_get_logs filter=Error limit=100`：`count=0` |
| GF_X 全量诊断 | 通过 | `gf-diagnostics-run-all_20260710_182732.json`：37 success / 0 failure / 0 warning |
| Quick PlayMode smoke | 通过 | job `a099d276`：1/1 passed，23 秒 |
| Full PlayMode smoke | 通过 | job `84b58a2e`：1/1 passed，56 秒 |
| 场景运行残留 | 通过 | smoke 退出 CombatHUD 后断言 Enemy、Loot、Readiness、Actor、Map 均清空 |
| LOD / 路径预算 | 通过 | 5m=Hot、30m=Warm、70m=Cold；路径预算接受 1、拒绝超额请求 1 |
| 稳态托管分配 | 通过 | 30 敌人预热 64 帧、测量 256 帧；7680 enemy ticks、9600 decisions、managedBytes=0 |
| OpenSpec 严格校验 | 通过 | `native-enemy-domain-rebuild`：1 passed / 0 failed / 0 issues |

## 需求审计

### 参赛者与胜负

- 运行时固定生成 50 名参赛者：1 Human、20 SmartBot、29 LightBot。
- NPC 敌人使用独立 `TotemEnemyModel` / `TotemEnemyService`，不进入参赛者 roster。
- 前 60 秒 Participant -> Participant 被 `BlockedParticipantCombatGracePeriod` 阻止；NPC -> Active Participant 仍可结算。
- 最后存活者由 `FindUniqueAliveParticipant` 解析真实身份；真人和人机均可成为 winner。
- Full smoke 在场上仍有 NPC 敌人时淘汰 49 名人机，断言真人 `winnerParticipantId`、`win=true`、`aliveParticipantCount=1`。

### 就绪与开局保护

- 本地玩家按 `Loading -> Protected -> Active` 运行；Loading 不显示、不可被索敌或受伤。
- Ready 由 CombatHUD、Camera、Input 和至少一个渲染帧自动触发，测试不再手工调用 Ready。
- Ready 后保护为 5 秒，可由有效操作提前结束；加载超时为 90 秒并进入 Disconnected。

### NPC 敌人运行时

- 15 个定义已绑定：8 Light、4 Elite、3 Boss；共 13 类可复用能力。
- 基础 FSM、固定容量仇恨、1.25 目标切换迟滞、群体警戒、leash、Hot/Warm/Cold LOD、路径缓存和每帧路径预算均有纯逻辑证据。
- Boss 阶段验证覆盖 `1 -> 2 -> 3`、治疗不降级、重复跨阈值不重复发事件。
- Regenerate 验证覆盖 Windup 被伤害打断、进入 Stagger 且不产生治疗。
- Summon 验证覆盖 EncounterActiveCap 阻止、阻止原因和零子敌人生成。

### PCG 遭遇与生命周期

- 同 map/theme/seed 重复构建 SpawnPlan，逐项比较 entry 和 rejection 完全一致。
- 纯 SpawnPlan 构建前后 `[TotemEnemies]` 根及子对象数不变，PCG 规划阶段无 GameObject 副作用。
- Quick 和 Full PCG 都保持 400m 世界尺度；初始 Light、240 秒 Elite、600 秒 Boss 的权威时钟流程通过。

### 掉落与进度

- Light/Elite/Boss 使用公开世界掉落，不绑定击杀者，且与参赛者 death chest 公式分离。
- Boss 奖励在死亡时生成；新配方进入 Meta，重复配方转换为两个高阶颜料。
- Full smoke 实际击杀 Light 并验证死亡事件到 `TotemEnemyLootService` 和运行时拾取物。

### 配置、资源与可观测性

- 31 张 Business JSON 与 31 张 xlsx 同步，包含 EnemyAbility、EncounterSpawn、EnemyLoot 三张原生敌人扩展表。
- 15 个敌人 key 和 Theme/Tier fallback 已显式进入 runtime asset catalog；当前可使用占位 prefab，正式美术属于后续产品化。
- 状态、目标、能力、Boss 阶段、生成拒绝、死亡、掉落和胜者均输出结构化 GFTrace 因果证据。

## 完整性结论

本 change 的代码、数据、运行时、诊断和 PlayMode 验收范围已经实现。留在边界外的是正式敌人美术、动画、VFX/音频品质、战斗手感和数值平衡；这些不阻塞原生 NPC 敌人领域的首轮完成。
