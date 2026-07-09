# Spec — map-terrain-features（纯地形特色效果）

> 术语/数据以 [CONTRACT.md](../../CONTRACT.md) 为准。特色区 = TerrainType 的运行时效果，非独立实体。

## ADDED Requirements

### Requirement: 六语义地形类型

系统 SHALL 用 6 个跨主题机制语义 TerrainType：`None/Ground/Slow/Blocked/Cover/Hazard`（CONTRACT §4.1）。三主题的具体地貌 MUST 映射到这 6 类，机制代码 MUST 只按语义类写一次、三图复用。`TerrainTypeConfig` MUST 提供 `IsWalkable / MoveSpeedMul / BlocksVision / HazardDps` 字段。

#### Scenario: 主题地貌映射到语义类
- **WHEN** AI 废墟"辐射区"、病毒"毒雾"、外星"腐蚀池"分别加载
- **THEN** 三者 TerrainType 均为 `Hazard`，走同一套扣血逻辑，仅美术与命名不同

### Requirement: 减速地形（Slow）

当 actor 处于 `MoveSpeedMul < 1` 的地形格时，系统 SHALL 通过现有 MoveSpeed 计算链施加移速倍率；离开该格后 MUST 恢复。检测 MUST 用 `TerrainEffectTracker` 的 0.2s tick，MUST NOT 每帧检测。

#### Scenario: 沼泽减速
- **WHEN** 玩家进入 `Slow`（MoveSpeedMul=0.65）格
- **THEN** 实际移速降为基准 0.65×；走出后恢复原速

### Requirement: 阻挡地形（Blocked）

`Blocked` 格 MUST 不可进入。玩家移动 SHALL 被阻挡，AI 移动 SHALL 避让 `Blocked` 格绕行（无 NavMesh，AI 按 TerrainGrid 避让）。

#### Scenario: 河流阻挡
- **WHEN** 角色试图移动进入 `Blocked`（河流）格
- **THEN** 移动被阻止，需绕路

### Requirement: 遮蔽地形（Cover）

`Cover` 格 MUST 可行走，但 SHALL 写入"被遮蔽"状态供远程命中与视野判定使用（接 Combat）。相机 SHOULD 对遮挡玩家的物件层做半透明/淡出处理（复用 change 25 能力，若无则记为可选增强）。

#### Scenario: 废墟遮蔽
- **WHEN** 玩家进入 `Cover`（废墟）格
- **THEN** 玩家进入被遮蔽状态，影响远程命中判定

### Requirement: 伤害地形（Hazard）

当 actor 处于 `HazardDps > 0` 的地形格时，`TerrainEffectTracker` SHALL 每 tick 施加 `HazardDps × tickInterval` 伤害（接 Status/Combat）。伤害 MUST 对玩家与 AI 一致生效（50 actor 公平）。

#### Scenario: 辐射区扣血
- **WHEN** 任一 actor 停留在 `Hazard`（HazardDps=5）格
- **THEN** 每秒扣除约 5 HP，玩家与 AI 同规则

### Requirement: 每图特色区数量

每张地图 MUST 包含 2-3 种特色地形区（非 Ground/Blocked 之外的语义类），主题化命名，分布合理。

#### Scenario: 特色区齐备
- **WHEN** 加载任一完整设计的地图
- **THEN** 至少存在 2 种特色地形区（如 Slow + Hazard）
