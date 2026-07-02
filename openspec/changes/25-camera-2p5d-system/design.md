# Design — 25-camera-2p5d-system

> **决策日期**：2026-07-02 ｜ **决策方式**：grill-me 5/5 挖透 ｜ **主导**：client-lead
> **范围**：3D 透视相机 → 正交俯角 2.5D 相机；自写 CameraModule 跟随 + billboard + 深度排序 + 震动整合。
> **世界不动**：XZ 平面 3D 世界保持不变，只改相机投影与渲染表现层，不翻移动/战斗/地图/瞄准。

---

## 一、决策记录

### 1.1 已定共识（grill-me 挖透，不可推翻）

| # | 共识 | 落地要点 |
|---|---|---|
| C1 | 世界保持 3D（XZ 移动，Y=高度） | 不重构 2D，不动移动/战斗/地图/瞄准代码 |
| C2 | 相机投影 perspective → orthographic | 保留俯角（默认 55°，可调），sprite 无透视形变 |
| C3 | 自写 `CameraModule : IGameModule`，零外部依赖 | 不用 Cinemachine；LateUpdate SmoothDamp + 死区跟随 |
| C4 | 边界 clamp 到固定全图 bbox（`MapGenModule.MapSize`） | 边界固定不随缩圈变（缩圈=圈外伤害，不属相机） |
| C5 | lookahead 按玩家移动方向预偏 | 视野看得更靠前 |
| C6 | billboard：sprite 面向相机（俯角下校正） | 避免压扁 |
| C7 | 深度动态排序 | actor/NPC/障碍按世界深度动态算 sortingOrder，解决互穿 |
| C8 | 震动整合进 CameraModule | 跟随基准位 + 震动偏移量叠加 |

### 1.2 client-lead 补充架构决策

以下是 grill 共识落地时必须先定的架构选型，每条给候选 + trade-off。

#### D1. LateUpdate 驱动方式（关键 — 查过 ModuleRunner/GameTickDriver 后的结论）

**背景**：`IGameModule` 无 MonoBehaviour 生命周期。框架现有 tick 基础设施是 `ITickable.OnUpdate(dt)`，由 `GameTickDriver.Update()`（唯一一个 `Update()`）遍历驱动——**没有任何 LateUpdate 转发**。VFXModule 就是走 `ITickable.OnUpdate` 推进实例生命周期。相机跟随必须在 LateUpdate（角色 Update 移动之后读取，否则跟随晚一帧、抖动）。

| 候选 | Pros | Cons |
|---|---|---|
| **A. 扩展 GameTickDriver 加 `ILateTickable.OnLateUpdate`（选）** | 复用现有 tick 基础设施与自动注册机制（`GameApp.RegisterTickables` 已有）；与 VFXModule 的 ITickable 一致心智；单一驱动点便于排序控制；CameraModule 保持纯 IGameModule 无 MonoBehaviour | 触及框架核心文件 `GameTickDriver.cs`（Category 0 基础层）+ `GameApp.RegisterTickables`——需谨慎，属"项目宪法级"边界，实现时通知 |
| B. CameraModule spawn 一个挂 MonoBehaviour driver 的 GameObject（类似 VFX shake 锚点） | 不动框架核心；CameraModule 自包含 | 又多一个游离 MonoBehaviour；与框架统一 tick 机制割裂；driver→module 回调要手接线；两套 tick 心智 |
| C. CameraModule 实现 ITickable，在 `OnUpdate` 里做跟随 | 完全不改框架 | Update 里跟随会晚一帧、和角色移动争序 → 抖动，违背 C3「LateUpdate 跟随」硬要求 |

**决策：A**。在 `GameTickDriver` 增加 `ILateTickable { void OnLateUpdate(float dt); }` 接口 + 一个 `LateUpdate()` 遍历；`GameApp.RegisterTickables` 里补一行 `if (module is ILateTickable lt) _tickDriver.RegisterLate(lt)`。CameraModule 实现 `ILateTickable`。
**回滚成本**：低。若框架改动被否，退回候选 B（CameraModule 内 spawn driver），CameraModule 公开 API 不变，仅换驱动源。
**边界提示**：`GameTickDriver.cs` / `GameApp.cs` 属框架核心，实现阶段动这两个文件前按 CLAUDE.md §五须先通知用户（不可逆/宪法级判断），但本改动是纯增量（加接口 + 加一个遍历，不改现有 ITickable 语义），风险可控。

#### D2. billboard 校正方式（相机俯角固定是关键前提）

**背景**：现相机 `eulerAngles=(55,0,0)` 固定，改正交后**俯角仍固定不变**（CameraModule 不旋转相机，只平移）。sprite 竖直站在 XZ 平面（面朝 -Z 或世界前向），被俯视 55° 相机拍到会"向后躺"被压扁。

| 候选 | Pros | Cons |
|---|---|---|
| **A. sprite 绕世界 X 轴固定旋转 = 相机俯角（选）** | 相机俯角固定→校正角是常量（无需每帧 LookAt）；一次设置或低频设置即可；0 每帧开销；billboard 组件极轻 | 若未来相机可旋转 yaw 需改（当前非目标，YAGNI） |
| B. 每帧 `transform.LookAt(camera)` / billboard 面向相机 | 通用，相机怎么动都对 | 每帧 N 个 sprite 做 LookAt = N 次矩阵运算 + 潜在 GC；正交下相机无透视，"面向相机"退化为"统一朝一个固定方向"，等价于 A 但更贵 |
| C. shader 顶点 billboard | GPU 侧 0 CPU 开销 | 走 client-ta，超出本 change 范围（C3 纯 CameraModule）；4 方向贴图逻辑与顶点 billboard 耦合复杂 |

**决策：A**。sprite 的可视 transform 设 `localEulerAngles = (cameraTiltX, 0, 0)`（cameraTiltX 默认 55）。因俯角固定，此旋转是常量，billboard 组件只需在 enable 时 / 俯角变更事件时设一次，**不进每帧**。4 方向贴图切换（Down/Up/Left/Right）由现有逻辑负责，billboard 只管 X 轴竖立校正。

#### D3. 深度排序方案

**背景**：只有 1 个 `Default` sorting layer；shadow 固定 `sortingOrder=-100`；角色间无前后遮挡。俯角相机下"越靠屏幕下方 = 越靠近相机 = 应盖在上方"。屏幕纵深由世界 Z 决定（相机沿 -Z 看，越小的 Z 越靠后）——实际排序键取**世界 Z（可掺一点 Y）**。

| 候选 | Pros | Cons |
|---|---|---|
| **A. 轻量 `DepthSortedSprite : MonoBehaviour`，按世界坐标算 sortingOrder，移动时重算（选）** | 简单、可控、0 依赖；每 actor 一个组件；脏检查只在位置变化时算，静止不算 | 需给所有 actor/NPC/障碍/shadow 挂组件（可在 spawn 处统一挂） |
| B. `SortingGroup` + 按 Y 排序的第三方/自写管理器 | SortingGroup 让多 sprite 子物体整体排序 | 玩家 prefab 目前仅 1 SpriteRenderer，SortingGroup 收益小；仍需外部算 order |
| C. 用世界坐标直接映射到多个 sorting layer | 层级隔离清晰 | sorting layer 是全局 ProjectSettings，动态分配不现实 |

**决策：A**。
**排序公式**（统一在 `DepthSortedSprite` 内）：
```
sortingOrder = baseOffset + Mathf.RoundToInt(-worldPos.z * K)
```
- `K = 100`（每 1 世界单位 = 100 个 order 台阶；MapSize=150m → z∈[-75,75] → order 跨度 ±7500，远在 short/int 安全区内，且相邻 actor 间隔 0.01m 也能区分）。
- `-worldPos.z`：相机沿 +Z→-Z 方向俯视（cam 在 z=-10 看向 +Z），**z 越大越远离相机应越靠后 → order 越小**，故取负号让"z 大 = order 小"。实现阶段 loop 首帧截图核对正负号，若前后穿反则翻符号（低风险可逆）。
- **区间划分**（避免与 shadow 冲突）：

| 类别 | baseOffset | 说明 |
|---|---|---|
| shadow（脚下阴影） | `actorOrder - 1`（跟随宿主 actor 的算出值再 -1） | 保证阴影永远压在自己 actor 之下、但仍随深度浮动；**废弃现有固定 -100**（固定 -100 会让所有阴影盖在同一层，深度错乱） |
| actor / NPC / 障碍 | 0 | 主体，走公式 |
| 地面 VFX（Zone/圈） | -5000（固定低区） | 贴地特效永远在 actor 之下 |
| 头顶 VFX / 飘字 | +5000（固定高区） | 永远在 actor 之上 |

- **重算时机**：`DepthSortedSprite.OnUpdate`（走 ITickable 或自身 Update）里脏检查——`if ((transform.position - _lastPos).sqrMagnitude > epsilon²) 重算`，静止 actor 不算。**0 GC alloc**（纯数值运算，不 new）。shadow 的 order 由宿主 actor 的 DepthSortedSprite 重算时一并写入其子 shadow SpriteRenderer。

#### D4. 震动注入点

**背景**：现 `VFXModule.SpawnCameraShake`（line 631）把 `Camera.main` SetParent 到临时锚点抖动，**仅在 `Camera.main.transform.parent == null` 时生效**。CameraModule 接管相机后相机持续移动（即便无父节点，位置也被 CameraModule 每帧覆盖），此方案彻底失效——CameraModule 的 LateUpdate 会把相机位置写回基准位，冲掉 VFX 写入的抖动偏移。

| 候选 | Pros | Cons |
|---|---|---|
| **A. CameraModule 暴露 `AddShakeTrauma` / `PlayShake(duration, magnitude)`，震动偏移由 CameraModule 内部叠加（选）** | 单一相机权威源；跟随基准 + 震动偏移在同一 LateUpdate 内 `finalPos = basePos + shakeOffset` 叠加，永不冲突；VFX 只管"触发"不管"实现" | VFX→Camera 新增一次跨模块调用 |
| B. VFX 继续自己抖，CameraModule 让出最终写入权 | 改动小 | 两处写 camera.position 打架，谁最后写谁赢，必冲突（就是现状失效的根因） |
| C. 事件解耦：VFX Publish `CameraShakeEvent`，CameraModule 订阅 | 完全解耦 | 震动是高频行为反馈，走 EventBus 异步有延迟；且 CameraModule 需持有 shake 状态机，事件只是触发，收益不比 A 大 |

**决策：A**。
- CameraModule 公开 `void PlayShake(float duration, float magnitude)`。
- VFXModule 拿到 CameraModule 引用（`_runner.GetModule<CameraModule>()`，运行时懒取或加 Dependencies），把 line 306 的 `SpawnCameraShake(0.5f, 0.18f)` 改为 `_camera.PlayShake(0.5f, 0.18f)`，**删除** `SpawnCameraShake` 私有方法（含锚点 SetParent 那套）。
- **保留打击手感公式**：衰减正弦（freq=40，decay=1-elapsed/duration，`sx=Sin(t·freq·1.1)·mag·decay`，`sy=Sin(t·freq·0.9+1.3)·mag·decay`）原样搬进 CameraModule 的 shake 计算，产出 `shakeOffset`（相机局部 XY 偏移），LateUpdate 末尾叠加到最终位置。0 GC。

---

## 二、CameraModule API 设计

```
命名空间：Tattoo（与 SpawnerModule / VFXModule 同）
类型：public sealed class CameraModule : IGameModule, ILateTickable
```

| 成员 | 定义 | 说明 |
|---|---|---|
| ModuleCategory | `=> 2`（应用协调层） | 相机是场景协调能力，且需在 Spawner(3) 之后可用；靠 Dependencies 保序，Category 2 语义正确 |
| Dependencies | `=> new[] { typeof(SpawnerModule), typeof(MapGenModule) }` | 拿 `Player` transform + `MapSize` 边界；具体类型，非接口 |
| InitializeAsync(ct) | 创建/接管相机、设正交参数、GetModule 缓存 Spawner/MapGen 引用、算边界 bbox、把相机对准玩家初始位（瞬移不平滑），**不发事件** | 遵守 InitAsync 不发事件；相机若已存在（Spawner 剥离后不应存在）则接管，否则新建 |
| ShutdownAsync(ct) | 清理相机引用、停 shake | |
| OnLateUpdate(dt) | 每帧跟随主循环（见 §七时序） | 来自 ILateTickable |
| **PlayShake(duration, magnitude)** | 触发一次震动，累加到 shake 状态 | 供 VFXModule 调用 |
| CameraTiltX（属性，默认 55） | 俯角，供 billboard 组件读取校正角 | 可调 |
| OrthographicSize（属性/可调） | 正交视野半高 | §六 |

**私有状态**：
```
Camera _cam;
SpawnerModule _spawner;  MapGenModule _mapGen;
Vector3 _basePos;              // 跟随算出的基准位（不含震动）
Vector3 _followVelocity;       // SmoothDamp ref velocity（缓存，避免每帧 new）
Rect    _worldBounds;          // clamp 用世界 bbox（Init 时按 MapSize 算一次）
float   _shakeTimeLeft, _shakeDuration, _shakeMag;  // 震动状态
Vector3 _camForwardOnPlane;    // 相机在 XZ 的投影方向（算边界/lookahead 用）
```

---

## 三、深度排序方案

见 §一 D3。组件名 `DepthSortedSprite`（挂 actor/NPC/障碍根节点）。
- 公式：`sortingOrder = baseOffset + RoundToInt(-worldPos.z * K)`，`K=100`。
- shadow 排序 = 宿主 actor order - 1，由宿主组件在重算时写子 shadow。
- 重算：位置脏检查（sqrMagnitude > 1e-4），静止不算，0 GC。
- 挂载点：在 SpawnerModule spawn player/enemy、NPCModule spawn NPC、EnemyModule spawn 时统一 `AddComponent<DepthSortedSprite>()`（实现阶段确认各 spawn 点）。

---

## 四、billboard 方案（一句话）

sprite 可视 transform 绕世界 X 轴固定旋转 `= CameraTiltX(55°)`，因俯角固定→常量校正，只在 enable/俯角变更时设一次，不进每帧。组件名 `BillboardSprite`（挂 SpriteRenderer 节点），从 CameraModule 读 `CameraTiltX`。

---

## 五、震动整合方案

见 §一 D4。CameraModule.`PlayShake(dur, mag)` → 内部 `_shakeTimeLeft/_shakeDuration/_shakeMag` 记录；LateUpdate 末尾按衰减正弦公式算 `shakeOffset`（局部 XY），`finalPos = basePos + shakeOffset`。VFXModule 删 `SpawnCameraShake`，改调 `_camera.PlayShake`。基准位与偏移量在同一 LateUpdate 叠加，永不打架。

---

## 六、正交相机参数

| 参数 | 值 | 说明 |
|---|---|---|
| orthographic | `true` | 从 perspective 改 |
| orthographicSize | 默认 **9**（可调，loop 定稿） | 视野半高（世界单位）；玩家周围可见范围 ≈ 上下 18m；实现后 playtest 截图调，兼顾"看得清角色"与"视野够用" |
| eulerAngles | `(55, 0, 0)` 保持 | 俯角，billboard 校正角同值 |
| position | `basePos`（跟随算，初始对准玩家） | 不再固定 (0,18,-10)；由跟随驱动 |
| near / far | near=0.1，far=100 | 正交下 near/far 只影响裁剪；覆盖俯角高度足够 |
| clearFlags / bg | SolidColor / (0.18,0.18,0.22) | 保持现值 |

**正交兼容性确认**：
- `HumanPlayerController.GetMouseGroundPoint()` 用 `Camera.main.ScreenPointToRay` + Y=0 平面求交——**正交相机 ScreenPointToRay 仍返回正确世界射线**（正交下所有射线平行于相机 forward），Y=0 求交结果正确，**不改**。
- `HumanPlayerController.GetFacingDirection()` 用 `Camera.main.transform.forward`——相机俯角固定，forward 不变，**不改**。

---

## 七、跟随算法与关键时序

### 7.1 参数

| 参数 | 默认 | 说明 |
|---|---|---|
| smoothTime | 0.15s | SmoothDamp 平滑时间；越小越跟手 |
| deadZone（矩形，世界单位） | 半宽 1.5 × 半高 1.0（屏幕面内） | 玩家在死区内相机不动，出死区才拉 |
| lookaheadDist | 3.0m | 沿玩家移动方向最大预偏 |
| lookaheadSmooth | 0.3s | lookahead 自身平滑，避免转向瞬跳 |

### 7.2 边界 clamp 数学

Init 时按 `MapGenModule.MapSize`（150）算世界 bbox：地图中心 (0,0)，半边 `half = MapSize/2 = 75`。相机在俯角下能看到的世界范围由 orthographicSize + tilt 决定；clamp 目标是"相机视野不露出地图 bbox 外"。
- 简化：clamp 相机**焦点在 XZ 平面的落点**（相机 forward 与 Y=0 交点）到 `[-half + margin, half - margin]`，margin 按 orthographicSize 投影到 XZ 的可见半径估算。实现阶段 loop 截图核对边缘无空白后定 margin。
- 因俯角固定 + orthographicSize 固定，可见半径是常量，Init 时一次算好存 `_worldBounds`。

### 7.3 每帧 LateUpdate 流程（OnLateUpdate）

```
1. 读玩家世界位 playerPos = _spawner.Player.transform.position（含空引用保护）
2. 死区：算 playerPos 相对当前焦点的偏移，若在 deadZone 矩形内 → 目标焦点不变；
   出死区 → 目标焦点 = 把玩家拉回死区边缘的位置
3. lookahead：读玩家移动方向（速度或朝向），目标焦点 += dir * lookaheadDist（自身 SmoothDamp 平滑）
4. 把"目标焦点(XZ)"换算成"相机基准位"：basePosTarget = focus - camForward * camDistance
   （camDistance 由 tilt + 期望相机高度反推，Init 时定）
5. SmoothDamp：_basePos = SmoothDamp(_basePos, basePosTarget, ref _followVelocity, smoothTime)
6. 边界 clamp：把 _basePos 对应的焦点 clamp 到 _worldBounds，反算回 _basePos
7. 叠震动：若 _shakeTimeLeft>0，按衰减正弦算 shakeOffset（局部 XY），_shakeTimeLeft -= dt
8. 写入：_cam.transform.position = _basePos + shakeOffset（shakeOffset 转世界或按相机局部轴）
   相机旋转不改（俯角固定）
```
**全程 0 GC alloc**（Vector3 是 struct，SmoothDamp velocity 缓存字段，无 new / 无闭包）。

---

## 八、跨模块契约与触达改动点

| 文件 | 改动 | 类型 |
|---|---|---|
| `Assets/Scripts/Core/GameTickDriver.cs` | 新增 `ILateTickable` 接口 + `LateUpdate()` 遍历 + `RegisterLate/UnregisterLate` | 框架核心（增量，通知后改） |
| `Assets/Scripts/Core/GameApp.cs` | `RegisterTickables` 补 `ILateTickable` 注册；`AddModule(new CameraModule(...))`（Category 2，Spawner/MapGen 之后靠 deps 保序） | 框架核心（增量） |
| `Assets/Scripts/Modules/Camera/CameraModule.cs` | **新增** | 新文件 |
| `Assets/Scripts/Components/BillboardSprite.cs` | **新增** | 新文件 |
| `Assets/Scripts/Components/DepthSortedSprite.cs` | **新增** | 新文件 |
| `Assets/Scripts/Modules/Spawner/SpawnerModule.cs` | `CreateScene()` **删相机创建段（行 71-81）**；spawn player/enemy 处挂 `DepthSortedSprite` + `BillboardSprite` | 改现有 |
| `Assets/Scripts/Modules/VFX/VFXModule.cs` | 删 `SpawnCameraShake`；line 306 改调 `_camera.PlayShake`；加 CameraModule 引用（deps 或运行时懒取） | 改现有 |
| `Assets/Scripts/Utils/ActorShadowHelper.cs` | shadow 不再固定 `sortingOrder=-100`，改由宿主 `DepthSortedSprite` 动态写 | 改现有 |
| 其他 spawn 点（NPCModule/EnemyModule） | 挂 DepthSortedSprite/BillboardSprite | 改现有（实现阶段确认） |

**新增事件**：无（震动走直接方法调用 D4-A，跟随/排序均为本地行为，无需 EventBus）。

**注册顺序**：CameraModule 加在 `MapGenModule`(行60)、`SpawnerModule`(行61) 之后即可（Dependencies 保证 Init 序，AddModule 顺序仅影响诊断）。

