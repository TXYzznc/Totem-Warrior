# Tasks — 25-camera-2p5d-system

> openspec B1 路径，纯代码任务，不走 UI 6 阶段。
> 实现主导 client-unity，架构评审 client-lead，联调 client-unity + 用户。

## 阶段 0：架构定档（client-lead）✅ 本次完成

- [x] grill-me 5/5 挖透（proposal 已落）
- [x] 探查现状（ModuleRunner/GameTickDriver 无 LateUpdate 转发；VFXModule ITickable；Spawner 相机硬编码；shadow 固定 -100）
- [x] D1 LateUpdate 驱动方式定档（扩展 GameTickDriver 加 ILateTickable）
- [x] D2 billboard 校正方式定档（绕 X 轴固定 55°，非每帧）
- [x] D3 深度排序公式定档（`baseOffset + RoundToInt(-z*100)` + 区间划分）
- [x] D4 震动注入点定档（CameraModule.PlayShake，删 SpawnCameraShake）
- [x] 正交参数 / 跟随算法 / 时序 / 跨模块契约成文（design.md）
- [x] spec delta 成文（specs/camera-system/spec.md）

## 阶段 1：框架基建（client-unity，触框架核心先通知用户）

- [x] `GameTickDriver.cs` 新增 `ILateTickable { void OnLateUpdate(float dt); }` + `LateUpdate()` 遍历 + `RegisterLate/UnregisterLate`（纯增量，不改现有 ITickable 语义）
- [x] `GameApp.RegisterTickables` 补 `if (module is ILateTickable lt) _tickDriver.RegisterLate(lt)`
- [x] 编译通过，现有 ITickable（VFXModule 等）行为不受影响

## 阶段 2：CameraModule 实现（client-unity）

- [x] 新建 `Assets/Scripts/Modules/Camera/CameraModule.cs`：`IGameModule, ILateTickable`，Category 2，Deps `{ SpawnerModule, MapGenModule }`
- [x] InitializeAsync：接管/新建相机、设正交参数（size=9、tilt=55、ortho=true、near/far）、缓存 Spawner/MapGen、算 `_worldBounds`、对准玩家初始位（**不发事件**）
- [x] OnLateUpdate：§七 8 步流程（死区→lookahead→SmoothDamp→clamp→叠震动→写入），0 GC
- [x] `PlayShake(duration, magnitude)` + 内部衰减正弦 shakeOffset（沿用现打击手感公式）
- [x] `CameraTiltX` / `OrthographicSize` 可调属性暴露
- [x] `GameApp` 注册 `new CameraModule(_runner, _bus)`（Spawner/MapGen 之后）

## 阶段 3：渲染组件（client-unity）

- [x] 新建 `BillboardSprite.cs`：enable/俯角变更时设 `localEulerAngles=(CameraTiltX,0,0)`，不进每帧
- [x] 新建 `DepthSortedSprite.cs`：位置脏检查重算 `sortingOrder`，联动写子 shadow order（宿主 order -1），0 GC
- [x] `ActorShadowHelper` 去掉固定 `sortingOrder=-100`，改由宿主 DepthSortedSprite 动态写

## 阶段 4：现有模块改造（client-unity）

- [x] `SpawnerModule.CreateScene()` 删相机创建段（行 71-81）
- [x] Spawner spawn player/enemy/boss 处挂 `DepthSortedSprite` + `BillboardSprite`
- [x] NPCModule / EnemyModule / Bot spawn 点挂 DepthSortedSprite + BillboardSprite（确认：NPCModule 无 Instantiate；EnemyModule 目录存在但无独立 spawn；BotControllerModule 复用 SpawnerModule.Enemies 已有 actor，无新 Instantiate；Boss 在 SpawnerModule.SpawnBoss 已挂）
- [x] `VFXModule`：删 `SpawnCameraShake`；line 306 改 `_camera.PlayShake(0.5f,0.18f)`；加 CameraModule 引用（运行时懒取）
- [ ] 地面 VFX（Zone/圈）order 固定低区 -5000，头顶 VFX/飘字固定高区 +5000（VFXModule 当前无 SpriteRenderer/sortingOrder 设置点，待 playtest 阶段按实际 VFX 效果确认后补）

## 阶段 5：联调 loop（client-unity + 用户）

- [ ] playtest 跑局，截图 vs 期望 2.5D 效果对比
- [ ] 核对深度排序正负号（角色前后遮挡正确，无穿插；如穿反则翻 `-z` 符号）
- [ ] 调 orthographicSize / smoothTime / deadZone / lookaheadDist 到手感
- [ ] 核对边界 clamp：地图边缘无露空白（调 margin）
- [ ] 核对 billboard：sprite 竖直不压扁、4 方向贴图切换正常
- [ ] 核对震动：打击震屏正确，与跟随不冲突、不漂移
- [ ] 核对瞄准（GetMouseGroundPoint）+ facing 在正交下正确
- [ ] 0 Error + 无严重 Warning

## 阶段 6：归档

- [ ] `openspec validate 25-camera-2p5d-system --strict` 通过
- [ ] `openspec archive-change 25-camera-2p5d-system`
- [ ] 同步更新 INDEX.md
