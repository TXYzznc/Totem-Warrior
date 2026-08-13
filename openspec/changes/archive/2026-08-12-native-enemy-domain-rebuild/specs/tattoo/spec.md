## MODIFIED Requirements

### Requirement: TattooModule MUST 在 Shutdown 与战斗结束时正确收尾
Tattoo runtime MUST 在 Shutdown 和离开 CombatHud 时释放 Participant build、PendingTrigger 和运行时订阅，且不得抛未处理异常。Run 胜负 MUST 由 Participant 生存状态决定；Enemy 全灭 MUST NOT 发布胜利，最后一名 Participant 存活时 MUST 发布包含 winnerParticipantId 的结算结果。

#### Scenario: 关闭时反序
- **WHEN** GF_X runtime 执行 shutdown 或离开 CombatHud
- **THEN** Tattoo runtime MUST 清空 build、pending trigger 和事件订阅
- **AND** MUST NOT 抛未处理异常

#### Scenario: 怪物全灭不触发胜利
- **WHEN** 所有 Light、Elite 和 Boss 均死亡但仍有至少 2 名 Participant 存活
- **THEN** Run MUST 保持进行
- **AND** Tattoo runtime MUST 保持可用

#### Scenario: 最后一名参赛者触发结算
- **WHEN** 只剩 1 名 Participant 存活
- **THEN** Combat result MUST 记录该 Participant 为 winner
- **AND** CombatHUD MUST 显示本地玩家对应的胜利或失败结果

