# Proposal — 25-camera-2p5d-system

> **范围**：把当前"3D 透视相机拍 2D 立牌"的错误组合，改造为正交 + 俯角的 2.5D 相机系统，配自写 CameraModule 平滑跟随 + billboard sprite + 深度动态排序 + 相机震动整合。
> **决策日期**：2026-07-02
> **决策方式**：grill-me 5/5 挖透（见 design.md §决策记录）
> **驱动**：GDD 定位「似 Hades 精致 2.5D」，但当前相机是 3D 透视 55°、静止不跟随、硬编码在 SpawnerModule 里；角色是 2D SpriteRenderer 4 方向贴图，被透视相机拍出近大远小 + 边缘压缩 + 层级靠 3D 深度不可控。这是当前渲染观感差的根源。

## 为什么做

当前状态（探查实测）：
- **相机**：[SpawnerModule.cs:71-81](../../../Assets/Scripts/Modules/Spawner/SpawnerModule.cs) 里 `new GameObject("MainCamera")` 运行时硬编码创建，`perspective` 投影、`position=(0,18,-10)`、`eulerAngles=(55,0,0)`、**完全静止不跟随玩家**。
- **世界**：3D，角色在 XZ 平面移动，`position.y=0.4` 抬高，3D Plane 地面。移动/战斗/地图/瞄准（`GetMouseGroundPoint` Y=0 射线）全部基于 XZ 平面。
- **角色渲染**：2D SpriteRenderer（4 方向贴图 Down/Up/Left/Right），玩家 prefab 仅 1 个 SpriteRenderer，无 SortingGroup；只有 1 个 Default sorting layer；shadow 固定 `sortingOrder=-100`；角色之间无前后遮挡管理。
- **震动**：[VFXModule.SpawnCameraShake](../../../Assets/Scripts/Modules/VFX/VFXModule.cs) 用"临时 ShakeAnchor 做 Camera 父节点"方案，且**仅在 Camera 无父节点时生效**——一旦引入 CameraModule 让相机持续移动/有父节点，此方案失效。

## 目标（DoD）

- [ ] 相机改 **orthographic 正交投影**，保留俯角（默认 55°，可调），sprite 无透视形变
- [ ] 新增 **CameraModule : IGameModule**，LateUpdate 平滑跟随玩家（SmoothDamp + 死区）
- [ ] 相机边界 **clamp 到固定全图 bbox**（`MapGenModule.MapSize`），不露地图外空白；**边界固定不随缩圈变**
- [ ] **lookahead 预烧**：根据玩家移动方向预偏相机，视野看得更靠前
- [ ] **billboard**：sprite 面向相机（绕自身校正，避免俯角下被压扁）
- [ ] **深度动态排序**：给角色/NPC/障碍挂轻量组件，按世界深度（俯角相机下等效"越靠下越靠前"）动态算 sortingOrder，解决角色互相穿插
- [ ] **相机震动整合**：VFXModule 的震动改为对接 CameraModule（相机移动 + 震动叠加，不冲突）
- [ ] 相机创建从 SpawnerModule 剥离到 CameraModule（SpawnerModule 不再硬编码相机）
- [ ] 编译通过 + playtest loop 验证 2.5D 效果正确 + 0 Error + 无严重 Warning

## 非目标（明确不做）

- ❌ **重构为纯 2D 世界（XY 平面）** —— 保持现有 3D XZ 世界，只改相机 + 渲染表现，不翻移动/战斗/地图/瞄准代码
- ❌ 相机缩放 / 拉远拉近 / 多目标取中
- ❌ 镜头过场动画 / cutscene 编排
- ❌ 缩圈联动相机边界（缩圈是圈外伤害逻辑，属缩圈系统；相机边界固定）
- ❌ Cinemachine（自写 CameraModule，零外部依赖）
- ❌ 多相机 / 分屏 / 小地图相机

## 关键约束

- 保持 3D 世界坐标（XZ 平面移动，Y=高度），**改动面最小化**
- CameraModule 遵循 IGameModule 框架规范（ModuleCategory / Dependencies / InitAsync 不发事件）
- 相机跟随在 LateUpdate（角色移动在 Update/FixedUpdate 之后，避免抖动）
- 深度排序不在 Update 里做 GC alloc
- 震动与跟随叠加：跟随算"基准位置"，震动算"偏移量"，最终 = 基准 + 偏移

## 风险

| 风险 | 缓解 |
|---|---|
| 正交 + 55°俯角下 billboard 校正角度需实测调 | loop 阶段 playtest 截图逐步调俯角/正交 size |
| 深度排序与现有 shadow `sortingOrder=-100` 冲突 | 排序公式给 shadow 预留固定低区间，actor 用高区间 |
| 震动整合破坏现有打击感 | 保留 VFX 震动的衰减正弦公式，只改"注入点"从父节点锚点改为 CameraModule 偏移量 |
| `GetMouseGroundPoint` 瞄准依赖相机 ScreenPointToRay，正交后射线方向变化 | 正交相机 ScreenPointToRay 仍返回正确世界射线，Y=0 平面求交不变；loop 验证瞄准正确 |

## 阶段（openspec B1，纯代码任务，不走 UI 6 阶段）

1. **架构定档**：client-lead 评审 CameraModule 结构 + 震动整合方案 + 深度排序公式
2. **实现**：client-unity 落地 CameraModule / 正交相机 / billboard / 深度排序组件 / Spawner+VFX 改造
3. **联调 loop**：playtest 跑局 → 截图分析 → 修 → 再跑，直到 2.5D 效果正确 + 0 Error
