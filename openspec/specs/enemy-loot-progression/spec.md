# enemy-loot-progression Specification

## Purpose
TBD - created by archiving change native-enemy-domain-rebuild. Update Purpose after archive.
## Requirements
### Requirement: Enemy loot MUST be independent from participant death chests
Enemy 死亡 MUST 根据 EnemyLootConfig、LootTableId 和 GuaranteedLootIds 生成战利品；不得读取 Enemy inventory 或调用参赛者死亡箱继承公式。Participant 死亡箱 MUST 继续只继承 Participant inventory。

#### Scenario: 无库存怪物仍按 LootTable 掉落
- **WHEN** 一个没有 Participant inventory 的 Elite 死亡
- **THEN** MUST 生成配置的稀有颜料、金币和加权奖励
- **AND** MUST NOT 创建 Participant death chest snapshot

### Requirement: Enemy loot MUST be immediately public
所有 Enemy 战利品 MUST 在生成后立即允许任意 Active Participant 拾取，不得写入击杀者 OwnerId 或独占倒计时。击杀统计 MUST 与拾取权限分离。

#### Scenario: 非击杀者可以立即拾取
- **WHEN** Participant A 击杀 Elite，Participant B 先到达掉落位置
- **THEN** B MUST 能立即拾取奖励
- **AND** A 仍保留击杀统计但不自动获得物品

#### Scenario: 人机拥有相同拾取权限
- **WHEN** SmartBot、LightBot 和 Human 同时满足拾取距离
- **THEN** 三者 MUST 经过同一拾取校验和库存写入流程

### Requirement: Loot tiers MUST provide distinct rewards
Light MUST 掉落金币并按权重掉普通物资；Elite MUST 必掉 1 份稀有颜料并附加金币和加权武器/装备；Boss MUST 掉主题配方 1 张、颜料 2-3 份和金币。数量与权重 MUST 配置化。

#### Scenario: 精英保底不可被随机表覆盖
- **WHEN** Elite 的加权随机结果为空
- **THEN** Guaranteed rare paint MUST 仍然生成

#### Scenario: Boss 掉落发生在死亡时
- **WHEN** Boss 死亡但 Run 尚未结束
- **THEN** Boss loot MUST 立即生成
- **AND** 奖励 MUST NOT 等待 Victory 或 RunResult

### Requirement: Boss recipe pickup MUST update participant progression immediately
主题配方被拾取时 MUST 立即写入拾取者 profile。真人 profile MUST 通过 `TotemMetaProgressService` 持久化；Bot profile MUST 使用相同接口但仅在本局存在。已拥有配方时 MUST 转换为 2 份配置的高阶颜料。

#### Scenario: 真人拾取新配方后即使落败仍保留
- **WHEN** Human 拾取尚未解锁的 Boss recipe，随后在本局被淘汰
- **THEN** MetaProgress MUST 仍显示该配方已解锁

#### Scenario: 重复配方转换
- **WHEN** Participant 已拥有主题配方并再次拾取同一 recipe
- **THEN** recipe count MUST 不重复增加
- **AND** inventory MUST 增加 2 份高阶颜料

