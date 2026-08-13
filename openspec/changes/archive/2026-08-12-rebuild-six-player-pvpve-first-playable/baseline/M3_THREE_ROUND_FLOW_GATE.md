# M3 三轮流程 Gate

## 已实现

- 新增权威 `TotemMatchFlowService`，唯一合法流程为：开局构筑 → 第 1 轮战斗 → 第 2 轮构筑 → 第 1 次缩圈 → 第 2 轮战斗 → 第 3 轮构筑 → 第 2 次缩圈 → 第 3 轮战斗 → 结果。
- 开局构筑固定 60 秒，后续构筑固定 45 秒；正常模式使用 180/30 秒战斗/缩圈，快速模式使用 60/10 秒。
- 构筑阶段不修改全局 `timeScale`。MatchFlow 使用 unscaled 时间推进，MatchClock 分离 UI 时间与世界模拟时间。
- 移动、AI、敌人、武器/投射物、状态/元素、遭遇、缩圈、交互和战斗统计所在的 tick 服务统一实现 `ITotemGameplaySimulationService`，由 Runtime 在构筑阶段中央跳过。
- 所有复用 `TotemCombatRelationshipService` 的直接、世界、状态和元素伤害在构筑阶段返回 `BlockedGameplaySuspended`，避免调试入口或直接方法绕过 tick 门禁。
- 角色模型和资源可在开局构筑时预分配，但世界对象保持隐藏；60 秒构筑结束进入第 1 轮时才统一激活。
- 两次缩圈不再沿用旧表的 180/360 秒累计时间，而是在各自 10/30 秒缩圈活动内完成半径插值；第 2/3 轮构筑 HUD 显示下一次缩圈预告。
- 已同步清理 `ZoneShrinkConfig` 源工作簿、AI JSON、运行时 txt、生成代码注释和 gameplay catalog：删除旧 `Phase2_Rush` 第三段缩圈，只保留 `Shrink1`/`Shrink2`，正常模式各 30 秒且第一版圈心模式固定为 `None`。
- 第 3 轮结束后只生成 `ThreeRoundFlowComplete` 结果并打开结果 UI，不进入 Boss、撤离或第 4/5 轮。

## 验证证据

- `TotemMatchFlowTests`：首次运行 `6/6 passed`；随后新增两段缩圈半径测试，已通过 Unity 编译并被测试程序集发现，待 PlayMode 权限恢复后与完整回归一起复跑。
- 全量 EditMode：`159/160 passed`；唯一失败是 UnitySkills 自身的 `SkillDocumentationConsistencyTests.SkillDocumentation_ShouldMatchCodeDefinitions`，业务测试零失败。
- 全量诊断：`gf-diagnostics-run-all_20260811_112941.json`，`30 success / 10 failure / 36 warning`。新增 `Totem Three Round Match Flow` 诊断通过，记录最终 `Result`、2 次缩圈、7 个业务阶段及构筑/战斗时钟分离。
- 当前 10 个诊断失败与 M2 相同，来自旧重写清单、美术分类以及 `ScriptsBuiltin` 内仍假设 20/50 人和旧样本数的历史诊断；本阶段没有新增失败，也没有修改 GF_X 核心伪造通过。
- PlayMode 用例 `TotemThreeRoundMatchFlowSmokeTests.Launch_CompletesThreeRoundPlaceholderFlowAndOpensResult` 已编译并被 Test Runner 发现为 `Runnable`；执行被 UnitySkills `Auto` 模式的 PlayMode 门禁阻止，因此 4.9 暂不勾选。

## 待完成 Gate

- 将 UnitySkills 切回 `Bypass` 或将 `test_run_by_name` 加入用户 Allowlist 后，执行新增 Launch PlayMode 用例。
- 用例必须证明：开局构筑时 6 个角色世界对象隐藏、构筑结束统一激活、快速模式完整三轮后产生结果快照并退出测试场景。
