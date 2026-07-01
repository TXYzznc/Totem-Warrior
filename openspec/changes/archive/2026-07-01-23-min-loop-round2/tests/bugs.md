---
created: 2026-07-01
scope: Round N 累积 bug 表
---

# Bug 表（Round N 累计）

> 每轮 qa-engineer 追加 baseline / fail 观察到的 bug；修复子 agent 更新 status；主对话按根因分组时读这张表。

## Bug 状态字典

- `OPEN`：未修复
- `FIXING`：某个 fan-out 子 agent 正在修
- `FIXED`：修复完毕待回归
- `VERIFIED`：回归 PASS，本 bug 已闭环
- `DOC-ONLY`：文档 typo，代码无问题
- `NOT-A-BUG`：确认非缺陷（如工具限制）
- `DEFERRED`：本 loop 不修，转后续 change

## Severity

- `High`：影响 TC PASS / 引入 Console Error / 阻塞后续 TC
- `Medium`：不阻塞但影响体验（Warning、可视瑕疵）
- `Low`：仅文档/日志文案

## Bug 列表

| Bug# | 首次 Round | 关联 TC | 短描述 | 根因组 | Severity | Status | 负责 agent | 修复文件 |
|---|---|---|---|---|---|---|---|---|
| BUG-01 | 1 | TC-01, TC-13, TC-20 | VFXModule.SpawnAOEBurst 在 ParticleSystem 仍播放时调用 set_duration，引发 Unity Engine Error | VFX粒子系统 | High | VERIFIED | client-unity | Assets/Scripts/Modules/VFX/VFXModule.cs:922-935（AddComponent 后调用 ps.Stop 再设 main.duration） |
| BUG-02 | 1 | TC-04, TC-13 | 测试工具文档中 `Debug/ClickMainStartButton` 路径错误，实际路径为 `Tools/Playtest/Debug/ClickMainStartButton` | 文档偏差 | Medium | VERIFIED | 主对话 | openspec/changes/23-min-loop-round2/tests/plan-22tc.md TC-04（补 fallback 路径） |
| BUG-03 | 1 | TC-10, TC-13 | 测试工具文档中 `DumpUIForms` 菜单路径错误，实际为 `Tools/Playtest/Debug/Dump UIForms (active+inactive)` | 文档偏差 | Medium | VERIFIED | 主对话 | openspec/changes/23-min-loop-round2/tests/plan-22tc.md TC-05/10/11/12（全量替换）|
| BUG-04 | 1 | TC-14, TC-15, TC-18 | 无 Bot 击杀自动化路径：MouseLeft 注入无法命中 Bot（需 3D 碰撞 + 瞄准）；`Publish AttackHit(placeholder)` Target=null 导致 VFXModule 跳过 SpawnSpark；WeaponSpawnedEvent 依赖 EnemyDied 无法触发 | 战斗自动化路径 | High | VERIFIED | client-unity / tools-engineer | R1: VFXModule.cs OnWeaponAttackHit null guard + PlaytestDriverEditor.cs ForceKillNearestBot 菜单；R3 加固：ForceKillNearestBot 前 FrameworkLogger.Info(Pos) 辅助日志 + TryGetKillPos 反查 EntityRef 拿位置 |
| BUG-05 | 1 | TC-21 | RunResultForm 未响应 GameOver 状态：PlayerDied 未级联到 GameState→GameOver；Debug/GameOver 强制切换后 RunResultForm 仍 active=False | 状态机/UI联动 | High | VERIFIED | client-unity | Assets/Scripts/Modules/UI/RunResultForm.cs:28-43（OnGameStateChanged 补 GameOver 分支，SetActive(true)） |
| BUG-06 | 2 | TC-14, TC-20 | VFXModule OnTargetKilled / OnPlayerDied 因 null guard 过于严格而 AOEBurst 完全不执行：OnTargetKilled 中 TryGetPos 返回 null（Bot 击杀时 Enemies 列表位置无法定位）；OnPlayerDied 中 _spawner.Player == null（手动 Publish 时序下 Player 引用为 null），两路均静默 return 无日志 | VFX null guard | High | VERIFIED | client-unity | Assets/Scripts/Modules/VFX/VFXModule.cs:1211-1252（OnTargetKilled/OnPlayerDied：null 时 Warn + Vector3.zero 兜底继续 SpawnAOEBurst，成功 spawn 补 Info 日志） |
| BUG-07 | 2 | TC-15 | WeaponSpawnedEvent 不存在于代码库（grep 全库无定义）：TC-15 通过标准中引用的事件类型未实现，武器生成后无对应事件广播日志 | 事件/日志缺失 | High | VERIFIED | client-unity | Assets/Scripts/Events/WeaponPickupEvents.cs（新增 WeaponSpawnedEvent，字段：WeaponId/Position/Instance）+ WeaponSpawnerModule.cs:166-168（SpawnDroppedWeapon 出口 Publish + Info 日志）；Round 4 TC-15 确认 `WeaponSpawned Id=knife_basic` 日志出现，0 error |
| BUG-08 | 2 | TC-18 | Playtest 菜单 `Publish AttackHit (placeholder)` 发布 AttackHitEvent，与 VFXModule 订阅的 WeaponAttackHitEvent 类型不匹配，Hit Spark VFX 完全无法被 playtest 路径触发 | 事件类型不匹配 | Medium | VERIFIED | tools-engineer | Assets/Editor/Playtest/PlaytestDriverEditor.cs 新方法 PublishWeaponAttackHit（菜单更名为 `Tools/Playtest/Combat/Publish WeaponAttackHit (nearest bot)`，Target 取 SpawnerModule.Enemies 最近活着 Bot via EntityRef，无 Bot fallback Player） |
| BUG-09 | 3 | TC-18 | SpawnSpark 在粒子系统仍播放时调用 set_duration，引发 Unity Engine Error（与 BUG-01 root cause 相同，BUG-01 只修了 AOEBurst 路径，SpawnSpark 路径 VFXModule.cs:991 遗漏） | VFX粒子系统 | High | VERIFIED | client-unity | Assets/Scripts/Modules/VFX/VFXModule.cs L997（SpawnSpark）+ 同根因扫描顺清 7 处：SpawnGatherParticle / SpawnFinishFlash / SpawnCancelScatter / SpawnEnchantSpark / SpawnTrailZone / SpawnSummonForm / SpawnFrostTrail 全补 ps.Stop(true, StopEmittingAndClear)；Round 4 TC-18 全程 0 Engine Error，Engine Error 维度 VERIFIED |
| BUG-10 | 4 | TC-18 | PublishWeaponAttackHit 后 SpawnSpark 无法触发：TC-14 ForceKillNearestBot 后 Bot GO 被销毁/移出 Enemies 列表，VFXModule.TryGetPos 返回 null，OnWeaponAttackHit L1227 silent return，SpawnSpark 未执行 | VFX 测试路径/TryGetPos null | High | VERIFIED | client-unity | Assets/Scripts/Modules/VFX/VFXModule.cs L1227-1237（OnWeaponAttackHit：null 时 Warn + Vector3.zero 兜底继续 SpawnSpark，成功 spawn 补 Info 日志，与 BUG-06 修法一致）；Round 5 TC-18 确认 `VFXModule SpawnSpark Pos=(-1.03, 0.80, -6.51)` 日志出现，0 error |
| BUG-11 | 4 | TC-16, TC-17 | Player 物理移动不触发 WeaponPickupTrigger.OnTriggerEnter：CombatModule 直接赋值 transform.position（不走物理引擎），pickup GO 的 SphereCollider trigger 永远不触发 | 工具路径/物理触发器 | High | VERIFIED | tools-engineer | Assets/Editor/Playtest/PlaytestDriverEditor.cs 新方法 ForcePickupNearestWeapon（菜单 `Tools/Playtest/Combat/ForcePickupNearestWeapon`，priority 705）；Round 5 TC-16 确认 `WeaponPickedUpEvent` + `WeaponEquipped` + `WeaponUpgraded NewLevel=2`，TC-17 确认 NewLevel=3，0 error |

## Warning 记录（不阻塞）

> 沿用 16 决策：Warning 不阻塞退出，但要在这里记录以便后续 change 治理。

| 首次 Round | 关键字 | 频次 | 处置 |
|---|---|---|---|
| 1 | AudioModule PlayBgm `Audio/BGM/in_game 未找到` | 2（每次进入 InGame 触发 1 次） | 待处理（音频资源缺失） |
| 1 | AudioModule PlayOneShot `Audio/SFX/player_died 未找到` | 2（TC-20 PlayerDied 触发） | 待处理（音频资源缺失） |
| 1 | UIModule AllFormsLoaded `Success=12`（plan 写 11/11，实际 12 个 Form） | 1 | 已修：plan-22tc.md TC-01 通过标准更新为 `Success=12` |

## Warning — 工具配置偏差（非游戏 Bug）

| 首次 Round | 关键字 | 描述 | Severity | Status |
|---|---|---|---|---|
| 1（任务说明） | unity-skills 端口偏差 | playtest-driver SKILL.md 文档写 `localhost:8091`，实际服务在 `localhost:8090` | Medium | DEFERRED（`.claude/**` 属本 loop 不可动范围，转后续独立 change 修 SKILL 文档；本 loop 内 qa-engineer 传参时明示 `:8090`） |
| 3, 5 | TC-19 camera_screenshot | Unity Skills REST `camera_screenshot` 返回 `GameObject not found`，无法自动化 Bot 染色目视验证。Bot 染色 shader 代码本身无问题（TC-19 阻塞仅在工具链，Round 3/5 两轮同结论） | Medium | NOT-A-BUG（工具限制；Bot 染色差异需 Unity Editor 内目视人工验证，或后续 change 修 camera_screenshot skill） |
