## ADDED Requirements

### Requirement: 已装备纹身必须向视觉层暴露默认布局描述
现有纹身玩法数据 SHALL 保持既有 PartId、ColorId、PatternId 和战斗效果语义不变，并向视觉层暴露固定默认 `offset=(0.5,0.5)` 与 `scale=1.0`。该描述 MUST NOT 改变任一纹身效果的数值或触发条件。

#### Scenario: 视觉描述不影响战斗纹身
- **WHEN** 视觉层读取任一已装备纹身的默认布局描述
- **THEN** 该纹身的 PartId、ColorId、PatternId 和既有战斗效果 SHALL 与读取前一致
