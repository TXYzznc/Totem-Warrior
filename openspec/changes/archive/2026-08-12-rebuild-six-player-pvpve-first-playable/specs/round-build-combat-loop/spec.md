## ADDED Requirements

### Requirement: 第一版必须形成五轮完整状态机

运行时 MUST 按以下顺序推进：OpeningBuild(60s) → Round1Combat → Build2(45s) → Shrink1 → Round2Combat → Build3(45s) → Shrink2 → Round3Combat → Build4(45s) → Shrink3 → Round4Combat → Build5(45s) → Shrink4 → Round5Combat → Result。

#### Scenario: 正常完成一局
- **WHEN** 对局未提前结束
- **THEN** 阶段只能按规定顺序推进
- **AND** 第五轮结束后进入结果
- **AND** 当前纯 PVP 版本不生成 Enemy、Boss 或旧 PVE 阶段

### Requirement: 构筑阶段必须暂停世界模拟

构筑阶段 MUST 停止移动、Bot 战斗决策、伤害、元素持续时间与衰减、资源交互、缩圈和战斗时钟；构筑 UI 倒计时 MUST 使用不受世界暂停影响的时钟。

#### Scenario: 带元素状态进入构筑
- **WHEN** 目标带有剩余元素时间并进入任意一次构筑
- **THEN** 构筑期间不得产生 tick 或层级衰减
- **AND** 返回战斗后从原剩余时间继续

### Requirement: 四次缩圈必须配置化并在战斗阶段动态完成

Shrink1～Shrink4 MUST 分别发生在 Round2～Round5 开始时。正常模式每次缩圈 30 秒，快速模式 10 秒；目标半径、圈外伤害与偏移模式 MUST 来自 `ZoneShrinkConfig`。

#### Scenario: 进入第五轮
- **WHEN** Build5 完成
- **THEN** 先执行 Shrink4
- **AND** 收缩完成后进入 Round5Combat

### Requirement: 淘汰跨轮保持且结算同步停止流程

淘汰玩家不得跨轮复活。只剩一支存活队伍时 MUST 立即进入结果并停止 MatchFlow；第五轮超时仍有多队时，MUST 按队伍淘汰数、玩家伤害、存活人数、剩余生命依次排名，全部相同则平局。

#### Scenario: 第五轮数据完全相同
- **WHEN** 所有排名字段相同
- **THEN** 结果明确标记为平局
