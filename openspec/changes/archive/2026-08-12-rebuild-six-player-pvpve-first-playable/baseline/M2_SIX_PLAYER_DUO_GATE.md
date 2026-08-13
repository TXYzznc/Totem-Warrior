# M2 六人双排 Gate

## 已实现

- 正式 roster 从旧 50 人收敛为 6 人：1 真人 + 5 Bot。
- 5 个 Bot 内部复用 3 个 SmartBot + 2 个 LightBot 控制器；这是 AI 实现层差异，对产品层统一呈现为 Bot。
- 参赛者使用稳定 `ParticipantId` 与 `TeamId`，按 `[0,0,1,1,2,2]` 组成三支双人队。
- 出生选择只使用 match/map seed，不再混入 `DateTime.UtcNow`；三支队伍从合法 PlayerSpawn 锚点的 seed 派生排列中取中心，同队成员在中心附近相邻生成。
- 同队直接伤害及所有复用 `TotemCombatRelationshipService` 的武器、纹身/元素和统计入口被统一阻断；AI 目标筛选也跳过队友。
- 旧 60 秒 Participant PvP grace 常量保留为值 `0` 的迁移 API，敌队从世界时间 0 起即可互相伤害；HUD readiness 的 Loading/Protected 启动保护继续独立生效。

## 验证证据

- `TotemFirstPlayableContractTests`：`12/12 passed`，包含 6 人/3 队、唯一 ID、同 seed 坐标一致、同队相邻、零重复出生、队友友伤与第 1 轮敌队伤害。
- 专用诊断 `Totem Six Player Duo Runtime`：通过；报告 `gf-diagnostics-run-all_20260811_111108.json` 记录 6 人、三队、合法可行走点、seed 坐标、`BlockedParticipantFriendlyFire` 与 `AllowedParticipantToParticipant`。
- Launch PlayMode：`TotemEnemyReadinessPcgSmokeTests.EnemyReadiness_DiagnosticFastPcgSmoke_CoversStartupAndImmediateParticipantCombat`，任务 `5ed0f79f`，`1/1 passed`，23 秒并明确退出；同时验证 Loading/Protected、队友无伤、敌队即时 PvP、PVE 伤害与退出清理。
- 全量诊断当前 `29 success / 10 failure / 36 warning`。其中新六人诊断通过；新增 failure 来自 `Assets/Game/ScriptsBuiltin` 的历史 50 人硬编码诊断（以及其按 49 Bot 数量挑选样本的假设），没有修改 GF_X 框架核心来伪造通过，留待 M12 诊断迁移收口。

## 时序说明

- 一次 PlayMode 运行曾停在空 `InitTestScene` 且 0 tests 未派发；截图确认没有保存弹窗。使用 `editor_stop` 恢复后，重新域加载并复跑同一 smoke 成功。该次记录归类为 Test Runner 基础设施卡死，不计玩法失败。
