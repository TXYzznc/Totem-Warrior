## MODIFIED Requirements

### Requirement: 精英敌人按 WeaponDropConfig 权重掉落
EnemyLootService MUST 仅在真实 `EnemyTier.Elite` 死亡时，根据 EnemyLootConfig/WeaponDropConfig 的 Elite 来源加权选择武器并生成公开拾取物。SmartBot、LightBot 或 Human 的 ControllerKind MUST NOT 触发精英掉落。Light/Boss 是否掉武器 MUST 完全由各自 LootTable 决定，不得复用 Elite 默认规则。

#### Scenario: SmartBot 死亡不等于精英掉落
- **WHEN** ControllerKind=SmartBot 的 Participant 死亡
- **THEN** Elite weapon drop MUST NOT 触发
- **AND** 只允许创建 Participant death chest

#### Scenario: 真实 Elite 按权重掉落武器
- **WHEN** EnemyTier.Elite 在位置 P 死亡且 LootTable 含多个 Elite 武器候选
- **THEN** EnemyLootService MUST 使用确定性 seed 加权选择配置候选
- **AND** 在 P 生成无 OwnerId 的 Weapon pickup

#### Scenario: 普通怪不继承精英默认掉落
- **WHEN** EnemyTier.Light 死亡且其 LootTable 不含武器
- **THEN** 场上 MUST 不生成武器拾取物

