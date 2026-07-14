## ADDED Requirements

### Requirement: Tattoo visuals SHALL support skeletal body anchors

纹身视觉层在存在骨骼角色预览时 SHALL 能按 Head、Torso、LeftArm、RightArm、LeftLeg、RightLeg 查找部位锚点。锚点 MUST 提供对应 Transform、固定局部裁切边界、默认 offset 和默认 scale；本要求 MUST NOT 改变现有纹身装配、效果触发或数值规则。

#### Scenario: Resolving a fixed body placement
- **WHEN** 视觉层请求已装备纹身的固定部位位置
- **THEN** 它 MUST 获得与该部位骨骼一起运动的锚点及裁切边界
- **AND** 它 MUST 在未提供编辑输入时使用默认 offset 和 scale
