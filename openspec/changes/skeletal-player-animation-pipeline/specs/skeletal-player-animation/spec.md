## ADDED Requirements

### Requirement: M02 SHALL provide an isolated Transform skeletal preview pipeline

系统 SHALL 为 `ActorCommonM02` 提供独立的纯 Transform 骨骼预览管线。该管线 MUST 使用分层 Sprite 部件、骨骼 Transform 层级和骨骼动画片段；它 MUST NOT 以逐帧 Sprite 片段作为其动画来源。

#### Scenario: Preview asset isolation
- **WHEN** 导入骨骼版 M02 资源
- **THEN** Sprite、Controller、Clip 和 Preview Prefab MUST 均位于包含 `ActorCommonM02Skeletal` 的独立路径
- **AND** 它们 MUST NOT 覆盖现有 `ActorCommonM02` 资源

### Requirement: Skeletal M02 SHALL provide four directional layered part sets

骨骼版 M02 SHALL 为 Down、Up、Left、Right 四个方向提供独立的分层部件集。每个方向 MUST 至少含头部、躯干、服装覆盖、双臂、双手、骨盆/下装、双腿和双脚，并保证相邻骨骼部件可重叠。

#### Scenario: Directional asset completeness
- **WHEN** 骨骼资源验证工具扫描一个方向
- **THEN** 它 MUST 报告该方向缺失的必需部件
- **AND** 只有四个方向均完整时验证才可通过

### Requirement: Skeletal M02 SHALL expose tattoo anchors

骨骼预览 SHALL 暴露 Head、Torso、LeftArm、RightArm、LeftLeg、RightLeg 六个稳定的纹身锚点。每个锚点 MUST 绑定到对应部位骨骼，并提供局部裁切边界、默认 offset 与默认 scale。

#### Scenario: Tattoo anchor lookup
- **WHEN** 视觉层按任一六部位请求锚点
- **THEN** 骨骼预览 MUST 返回该部位的 Transform 与局部裁切边界

### Requirement: Skeletal controller SHALL preserve gameplay parameter contract

骨骼控制器 SHALL 声明 `Direction`、`IsMoving`、`AttackTrigger`、`HitTrigger`、`DodgeTrigger`、`IsSprinting`、`Die` 和 `Dead` 参数，以便未来可复用当前玩家动画桥接层。

#### Scenario: Basic locomotion parameter
- **WHEN** `Direction` 和 `IsMoving` 被写入骨骼控制器
- **THEN** 控制器 MUST 能选择相应方向的 Idle 或 Walk 状态

## MODIFIED Requirements

### Requirement: 逐帧角色美术资源 SHALL 保留并可与骨骼资源并行

系统 SHALL 保留现有 `ActorCommonM02` 的逐帧 Sprite、Animation Clip、标准 `ActorCommonM02.controller` 及其现有 Prefab 引用。系统 MAY 新增独立的 `ActorCommonM02Skeletal` 预览资源，但在用户确认运行时切换前 MUST NOT 替换 Player、SmartAI 或 LightAI 使用的现有逐帧控制器。

#### Scenario: Importing skeletal preview
- **WHEN** 新增或重新导入骨骼预览资源
- **THEN** 现有 168 张 M02 逐帧 Sprite、28 个 Clip 和标准控制器 MUST 仍存在且引用不变

