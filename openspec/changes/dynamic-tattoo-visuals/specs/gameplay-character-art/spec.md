## ADDED Requirements

### Requirement: M02 动画资源必须包含纹身映射配套资源
`ActorCommonM02` 的每个运行时角色 Sprite SHALL 有同名、同尺寸的 TattooMap 资源，并由可查询索引关联。标准 AnimatorController 路径 MUST 为 `Assets/Game/Animation/Actors/ActorCommonM02/ActorCommonM02.controller`。

#### Scenario: 导入验证 M02 资源集
- **WHEN** 执行 Actor Common M02 导入验证
- **THEN** 验证 SHALL 确认所有角色 Sprite 都有一张同尺寸 TattooMap，且控制器路径为标准路径
