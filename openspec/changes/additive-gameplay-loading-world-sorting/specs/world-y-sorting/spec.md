## ADDED Requirements

### Requirement: 世界 Z 深度排序
地表 Tilemap MUST 低于全部立体世界内容；玩家、NPC、人机和立体物件 MUST 按世界 Z 坐标计算稳定的 SpriteRenderer sortingOrder。

#### Scenario: 近处角色遮挡远处对象
- **WHEN** 两个立体对象的世界 Z 不同
- **THEN** Z 值更靠近相机的一方显示在另一方前面，且不被地表 Tilemap 覆盖
