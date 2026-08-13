# test-extraction-flow Specification

## Purpose
TBD - created by archiving change rebuild-six-player-pvpve-first-playable. Update Purpose after archive.
## Requirements
### Requirement: 测试输入必须通过权威事件一次性解锁撤离
测试版本 MUST 仅从 `Round4Combat` 开始接受 InputModule 产生的 `Shift + Space` 解锁命令；普通空格仍为闪避。更早阶段和重复命令 MUST 被拒绝。撤离服务 MUST 暴露与输入来源无关的权威解锁入口，使未来 Boss 击杀能够复用同一入口。

#### Scenario: 第四轮按下测试组合键
- **WHEN** 对局进入 Round4Combat 且撤离尚未解锁
- **AND** InputModule 捕获 `Shift + Space`
- **THEN** 发布一次撤离解锁事件
- **AND** 同帧不得执行闪避

### Requirement: 撤离点必须从专用合法锚点确定性生成
地图 MUST 提供专用 `Extraction` 锚点。解锁时系统 MUST 按 match seed 从可达且不重复的合法锚点抽取配置数量，第一版默认 3 个；生成后位置不得改变。不得复用资源拾取或玩家出生锚点冒充撤离锚点。

#### Scenario: 相同 seed 重复解锁
- **WHEN** 两局使用相同地图、seed、锚点集合和生成数量
- **THEN** 生成相同的三个撤离锚点

### Requirement: 本地真人完成交互后整队撤离
存活且未倒地的本地玩家在撤离范围内持续按住 `F` 3 秒 MUST 完成撤离；松开、离开范围、倒地或受到有效伤害 MUST 中断并清零进度。若其 Bot 队友正在倒地，交互不得开始；已淘汰队友不阻止撤离但不得被复活。成功后本地玩家和同队所有未淘汰成员 MUST 一并标记为已撤离并退出战斗。

#### Scenario: 本地玩家完成三秒交互
- **WHEN** 本地玩家和其未倒地队友满足撤离条件
- **AND** 本地玩家在撤离范围内持续按住交互三秒
- **THEN** 本地双人队所有未淘汰成员进入 Extracted 状态

### Requirement: 本地整队撤离必须立即结束本局
第一版不实现后端或真实联机。当地玩家队伍撤离成功时，系统 MUST 立即停止 MatchFlow，生成 `LocalTeamExtracted` 成功结果并进入现有 Result 流程；不得继续模拟其他 Bot 队伍。未来联机实现可替换结算策略，但不得改变解锁和交互合同。

#### Scenario: 本地队伍撤离成功
- **WHEN** 整队撤离事务提交
- **THEN** 当前阶段立即变为 Result
- **AND** 结果标记本地队伍撤离成功
- **AND** 世界模拟不再推进

