# camera-system Specification

## Purpose
TBD - created by archiving change 25-camera-2p5d-system. Update Purpose after archive.
## Requirements
### Requirement: 正交投影相机

相机 MUST 使用 orthographic 正交投影，MUST 保留可调俯角（默认 55°），sprite 在视野内 MUST 无透视近大远小形变。

#### Scenario: 相机以正交投影渲染
- **WHEN** CameraModule 初始化完成
- **THEN** `Camera.orthographic` MUST 为 true
- **AND** `eulerAngles.x` MUST 等于 CameraTiltX（默认 55）
- **AND** orthographicSize MUST 为可调参数（默认 9）

#### Scenario: 同尺寸 sprite 在远近处大小一致
- **WHEN** 两个同尺寸角色分别处于视野近端与远端
- **THEN** 两者在屏幕上的像素尺寸 MUST 相同（正交无透视缩放）

### Requirement: CameraModule 平滑跟随玩家

系统 MUST 提供 `CameraModule : IGameModule, ILateTickable`，Category=2，Dependencies={SpawnerModule, MapGenModule}，在 LateUpdate 用 SmoothDamp + 死区跟随玩家，全程 0 GC alloc，InitializeAsync 期间 MUST NOT 发事件。

#### Scenario: 玩家移动出死区时相机跟随
- **WHEN** 玩家移动超出相机死区矩形
- **THEN** 相机 MUST 以 SmoothDamp 平滑追向玩家，把玩家拉回死区边缘

#### Scenario: 玩家在死区内相机不动
- **WHEN** 玩家小幅移动仍在死区矩形内
- **THEN** 相机基准位 MUST 保持不变

#### Scenario: 跟随在 LateUpdate 执行且不分配
- **WHEN** 每帧跟随逻辑运行
- **THEN** 跟随 MUST 在 ILateTickable.OnLateUpdate 中执行（角色 Update 移动之后）
- **AND** MUST NOT 产生 GC alloc（Vector3 struct + 缓存 velocity 字段）

#### Scenario: 初始化不发事件
- **WHEN** CameraModule.InitializeAsync 执行
- **THEN** MUST NOT Publish 任何事件（遵循 IGameModule 约束，依赖通信走 Dependencies）

### Requirement: 相机边界 clamp 到固定全图 bbox

相机视野 MUST clamp 到由 `MapGenModule.MapSize` 决定的固定全图 bbox，边界 MUST NOT 随缩圈变化。

#### Scenario: 相机到达地图边缘停止
- **WHEN** 玩家走到贴近地图边界，相机继续跟随会露出 bbox 外空白
- **THEN** 相机焦点 MUST 被 clamp 在 `[-MapSize/2+margin, MapSize/2-margin]` 内，视野不露地图外

#### Scenario: 边界不随缩圈变化
- **WHEN** 游戏内缩圈（安全区收缩）发生
- **THEN** 相机边界 MUST 保持为固定全图 bbox 不变（缩圈属圈外伤害逻辑，不影响相机）

### Requirement: lookahead 移动方向预偏

相机 MUST 根据玩家移动方向沿该方向预偏焦点（lookahead），使视野看得更靠前，预偏 MUST 自身平滑避免转向瞬跳。

#### Scenario: 玩家持续朝一方向移动
- **WHEN** 玩家沿某方向持续移动
- **THEN** 相机焦点 MUST 沿该方向偏移最多 lookaheadDist（默认 3m），且偏移量经 SmoothDamp 平滑

### Requirement: billboard sprite 俯角校正

系统 MUST 提供 `BillboardSprite` 组件使 sprite 面向正交俯角相机，避免俯角下被压扁；因相机俯角固定，校正 MUST NOT 每帧执行。

#### Scenario: sprite 在俯角相机下竖直显示
- **WHEN** BillboardSprite 组件 enable
- **THEN** sprite 可视 transform 的 localEulerAngles.x MUST 被设为 CameraTiltX（55），sprite 在屏幕上竖直不压扁

#### Scenario: 俯角不变时不重复校正
- **WHEN** 相机俯角在一局内保持不变
- **THEN** billboard 校正 MUST 仅在 enable/俯角变更时执行一次，MUST NOT 进入每帧循环

### Requirement: 深度动态排序

系统 MUST 提供 `DepthSortedSprite` 组件，按世界坐标动态计算 sortingOrder 解决角色互穿，重算 MUST 0 GC alloc 且仅在位置变化时执行。

#### Scenario: 靠前角色遮挡靠后角色
- **WHEN** 两角色在 XZ 平面重叠、深度不同
- **THEN** sortingOrder MUST 按公式 `baseOffset + RoundToInt(-worldPos.z * 100)` 计算，靠近相机者 order 更大、渲染在上

#### Scenario: 阴影跟随宿主深度浮动
- **WHEN** 宿主 actor 的 DepthSortedSprite 重算 order
- **THEN** 其脚下 shadow 的 sortingOrder MUST 被设为宿主 order - 1（废弃固定 -100），保证阴影恒在自身之下且随深度浮动

#### Scenario: 静止角色不重算
- **WHEN** 角色位置未变化（位移平方 < 阈值）
- **THEN** DepthSortedSprite MUST NOT 重算 sortingOrder，MUST NOT 产生 GC alloc

#### Scenario: 地面与头顶 VFX 分区
- **WHEN** 渲染贴地 VFX（Zone/圈）与头顶 VFX/飘字
- **THEN** 贴地 VFX order MUST 固定低区（-5000）恒在 actor 之下，头顶 VFX order MUST 固定高区（+5000）恒在 actor 之上

### Requirement: 相机震动整合

相机震动 MUST 由 CameraModule 统一实现，暴露 `PlayShake(duration, magnitude)` 供 VFXModule 调用；震动 MUST 作为偏移量叠加到跟随基准位，MUST 保留现有衰减正弦打击手感，MUST 移除旧的相机父节点锚点方案。

#### Scenario: VFX 触发震动
- **WHEN** VFXModule 需要震屏
- **THEN** MUST 调用 `CameraModule.PlayShake(0.5f, 0.18f)`，MUST NOT 再用 SetParent 到临时锚点的旧方案（该方案已删除）

#### Scenario: 震动与跟随叠加不冲突
- **WHEN** 相机同时跟随玩家且处于震动中
- **THEN** 最终相机位 MUST = 跟随基准位 + 震动偏移量（同一 LateUpdate 内叠加），震动结束后 MUST 无残留漂移

#### Scenario: 保留打击手感
- **WHEN** 震动播放
- **THEN** 偏移 MUST 用衰减正弦公式（freq=40，decay=1-elapsed/duration，sx/sy 异相），MUST 0 GC alloc

### Requirement: 相机从 SpawnerModule 剥离

相机的创建与配置 MUST 从 `SpawnerModule.CreateScene()` 移除，改由 CameraModule 独立负责；瞄准与朝向逻辑 MUST 在正交相机下保持正确无需改动。

#### Scenario: Spawner 不再创建相机
- **WHEN** SpawnerModule.CreateScene 执行
- **THEN** MUST NOT 创建或配置相机（原行 71-81 删除），相机职责完全归 CameraModule

#### Scenario: 正交下瞄准与朝向不变
- **WHEN** 正交相机生效后玩家用鼠标瞄准
- **THEN** `GetMouseGroundPoint` 的 ScreenPointToRay + Y=0 求交 MUST 返回正确世界点（正交射线平行于相机 forward）
- **AND** `GetFacingDirection` 基于 `Camera.main.transform.forward` MUST 保持正确（俯角固定 forward 不变）

