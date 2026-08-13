# M4 单枪械与确定性效果队列 Gate 证据

记录时间：2026-08-11

## 已实现边界

- first-playable 运行时只暴露 `rifle_patrol_v1` 一款基础占位枪械；不启用蓄力、弹药耗尽近战降级、投射物或武器特性。
- 真人与 Bot 共用 `TotemWeaponService.TryResolveFirstPlayableAttack`，统一完成开火冷却、命中部位、有效直接伤害、表现和效果提交。
- 玩家弱点使用隐藏的头部 collider；敌人弱点使用可见的琥珀色占位标记。身体 collider 不会被错误提升为弱点。
- 枪械臂、元素和纹身命中通知只在产生正数有效直接伤害后触发；队友命中被关系层拦截后不会提交后续事件。
- 每次 resolution 独立收集事件，按优先级降序结算；相同优先级使用 match seed 与 resolution identity 派生的稳定顺序。
- 表现延迟只影响表现指令，不改变模拟结算顺序；零延迟开发模式可用。
- 队列、排序缓冲和表现缓冲均预分配；稳态诊断的当前线程托管分配为 0 字节。

## 自动化证据

- Unity 编译刷新：无本次新增 C# 编译错误。
- EditMode 异步发现任务 `00eeb471`：共发现 173 个测试，新增 7 个 `TotemFirstPlayableGunTests` 与 5 个 `TotemEffectResolutionQueueTests` 均为 `Runnable`。
- GF_X 全量诊断报告：`GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260811_122144.json`。
- `Scenario/BusinessRuntime/Totem First Playable Gun`：通过。
  - active weapon count：1
  - weapon id：`rifle_patrol_v1`
  - 弱点 ray：`Weakpoint`
  - 身体 ray：`Body`
  - 事件顺序：`Weakpoint > RifleArm > Torso`
  - queue allocated bytes：0
  - runtime asset key：`weapon.rifle.patrol.v1`
  - runtime weapon asset count：1
  - runtime player skill asset count：0
  - removed projectile asset count：0

## 仍未关闭的 Gate

- `5.1` 未完成：运行时 active 池已经收敛为单枪械，但权威 `WeaponConfig.xlsx` 尚未写入旧武器 inactive 状态。当前工作区依赖中缺少项目指定的 `@oai/artifact-tool`，因此没有改用未授权的替代库直接写表。
- `5.10` 暂不勾选：逻辑诊断已经满足身体/弱点/顺序/零分配要求，但 UnitySkills 当前处于 Auto 模式，`test_run`/`test_run_by_name` 被安全策略标记为仅 Bypass 可执行；本轮没有更改安全模式，也没有伪造测试通过结果。
- 全量诊断当前为 27 success / 14 failure / 36 warning。旧资源清单已主动移除五武器与玩家技能键，因此旧 `Totem Runtime Assets` 场景也进入预期失败；其余失败主要来自旧版 50 人、三段缩圈、五武器、主动技能和旧 UI 的硬编码诊断契约。它们作为后续迁移清单保留，不修改 `Assets/Game/ScriptsBuiltin` 框架核心来掩盖差异。
- 旧运行时服务、箱子武器掉落和待删除资源的精确清单见 `M4_RUNTIME_CLEANUP_AUDIT.md`。
