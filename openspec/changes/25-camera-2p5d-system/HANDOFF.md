# 交接文档 — 25-camera-2p5d-system（阶段 5 loop 中途）

> 生成时间：2026-07-02 ｜ 用途：新会话续跑本 change 的阶段 5 联调 + 归档
> **新会话第一步**：读本文件 + `design.md` + `tasks.md`，然后继续「阶段 5 loop 未完项」

---

## 一、任务目标（一句话）

把当前"3D 透视相机拍 2D 立牌"（渲染观感差的根源）改造成 **正交 + 55°俯角的 2.5D 相机系统**：自写 CameraModule 平滑跟随玩家 + billboard sprite 竖立校正 + 深度动态排序解决角色互穿 + 相机震动整合。GDD 定位「似 Hades 精致 2.5D」。

## 二、grill-me 5/5 已挖透的共识（不可推翻）

| # | 共识 |
|---|---|
| C1 | **世界保持 3D**（XZ 平面移动，Y=高度），不重构 2D，不动移动/战斗/地图/瞄准代码 |
| C2 | 相机 perspective → **orthographic 正交**，保留俯角（默认 55°） |
| C3 | 自写 **CameraModule : IGameModule**（零外部依赖，不用 Cinemachine），LateUpdate SmoothDamp + 死区跟随 |
| C4 | 边界 clamp 到**固定全图 bbox**（`MapGenModule.MapSize`=150），**边界固定不随缩圈变**（缩圈=圈外伤害逻辑，不属相机） |
| C5 | **lookahead** 按玩家移动方向预偏 |
| C6 | **billboard** sprite 面向相机（俯角下校正竖立，避免压扁） |
| C7 | **深度动态排序**（实际用世界 Z）解决角色互穿 |
| C8 | **震动整合**进 CameraModule（跟随基准位 + 震动偏移叠加） |
| 验收 | loop 循环（测试→分析→修→再测）直到效果正确 + 0 Error + 无严重 Warning |

## 三、架构决策（client-lead design.md，已定档）

- **D1 LateUpdate 驱动**：扩展 `GameTickDriver` 加 `ILateTickable { OnLateUpdate(dt) }` 接口 + `LateUpdate()` 遍历（框架现有只有 `ITickable.OnUpdate`，无 LateUpdate）。CameraModule 实现 ILateTickable。
- **D2 billboard**：sprite 绕世界 X 轴固定旋转 = 相机俯角 55°（俯角固定→常量，只在 OnEnable 设一次，不进每帧）
- **D3 深度排序**：`sortingOrder = baseOffset + Mathf.RoundToInt(-worldPos.z * 100)`。区间：shadow=宿主 actor order-1（**废弃原固定 -100**）/ actor=0 / 地面 VFX=-5000 / 头顶 VFX=+5000。位置脏检查重算（sqrMag>1e-4），0 GC。
- **D4 震动注入点**：CameraModule 暴露 `PlayShake(dur, mag)`，VFXModule 删 `SpawnCameraShake` 改调它；`finalPos = 跟随基准位 + 震动偏移` 同帧叠加。保留衰减正弦公式（freq=40）。

## 四、阶段 1-4 实现已完成（0 Error，client-unity 交付）

**新建文件**：
- `Assets/Scripts/Modules/Camera/CameraModule.cs`（namespace Tattoo, IGameModule+ILateTickable, Category=2, Deps={SpawnerModule, MapGenModule}, 构造 `(runner, bus)`）
- `Assets/Scripts/Components/BillboardSprite.cs`（OnEnable 设 localEulerAngles=(tiltX,0,0)，读 Camera.main.eulerAngles.x）
- `Assets/Scripts/Components/DepthSortedSprite.cs`（Update 脏检查重算 sortingOrder，联动写子 Shadow_ch22）

**改动文件**：
- `Assets/Scripts/Core/GameTickDriver.cs`：+ILateTickable 接口 + LateUpdate 遍历 + RegisterLate/UnregisterLate
- `Assets/Scripts/Core/GameApp.cs`：RegisterTickables 补 late 注册 + `AddModule(new CameraModule(_runner,_bus))`（SpawnerModule 之后）
- `Assets/Scripts/Modules/Spawner/SpawnerModule.cs`：删 CreateScene 相机创建段（原行 71-81）；player/49 敌人/Boss 挂 DepthSortedSprite+BillboardSprite
- `Assets/Scripts/Modules/VFX/VFXModule.cs`：删 SpawnCameraShake；line 306 改 `_runner.GetModule<CameraModule>()?.PlayShake(0.5f,0.18f)`（运行时懒取，无 Dependencies）
- `Assets/Scripts/Utils/ActorShadowHelper.cs`：删固定 sortingOrder=-100

**CameraModule 运行时确认成功**：`Action=Initialized orthographic=true orthographicSize=9 tilt=55 camPos=(0,18,-12.6)`

**待调默认值**（loop 里调）：orthographicSize=9 / smoothTime=0.15 / deadZoneHalfW/H / lookaheadDist=3 / boundaryMargin=10 / 深度排序 -z 符号

## 五、阶段 5 loop — Round 1 截图分析结果（已做）

截图：`Assets/Screenshots/cam2p5d_r1.png`（Play+InGame+simulator 后 scene_screenshot 拍的实时帧）

- ✅ **billboard 生效**：所有角色 sprite 竖直正立，没被 55° 压扁 → C6 通过
- 🔴 **地面尺寸不够**：Ground Plane 只有 `scale 6×1×6`=60×60 单位，比相机视野小，画面右上+中间有 **L 形深色分界**（相机拍到地面外的背景色）。地图逻辑是 150×150 但物理地面只 60。**需修**：地面放大到覆盖视野 or 匹配 MapSize。
- 🟢 **画面正中偏下一个孤立绿色小球** —— 可疑（玩家占位/marker？待查是不是本 change 引入）
- 🟡 **左上绿色长条**（疑似血条 world-space UI，飘在最上层）
- 🟡 **右上金色大圆环**（疑似缩圈 zone 指示器，盖住了角色 —— 若是地面 VFX 应在 -5000 层沉底，可能没挂对排序）
- ❓ **相机跟随未验证**：Move/Right 菜单注入后玩家没动（player 仍在 (0,0.4,0)），相机也没动。`gameobject_set_transform` 参数名 x/y/z 报错。

## 六、阶段 5 loop — 未完项（新会话从这里继续）

1. **验证相机跟随（最高优先，C3 核心验收）**：
   - `gameobject_set_transform` 正确参数名未知（x/y/z 报 "Unknown parameters"）——先跑 `us --list` 查它的 params（可能是 posX/posY/posZ 或 position）。或改用真实按键：`editor_execute_menu menuPath=Tools/Playtest/Hold/D`（持续按住）跑 2s 看玩家+相机是否一起移动。
   - 期望：玩家移动出死区后相机 SmoothDamp 跟过去，cam.position 变化。
2. **核对深度排序正负号（C7）**：让角色靠拢（或截多个重叠角色的图），确认靠下=靠前=盖住靠上；若穿反 → 翻 DepthSortedSprite 的 `-z` 符号。
3. **修地面尺寸**：Ground Plane 放大到覆盖相机视野（SpawnerModule.CreateScene 里 `ground.transform.localScale`），消除 L 形深色分界。
4. **排查绿球/血条/金环**：判断是本 change 引入还是既有（git diff 或对比旧行为）。金色圆环若是缩圈 VFX 且盖住角色 → 确认它的 sortingOrder 是否进了 -5000 地面 VFX 区间。
5. **调参到手感**：orthographicSize / smoothTime / deadZone / lookaheadDist / boundaryMargin。
6. **核对 billboard 4 方向贴图切换 + 边界 clamp 无露白 + 震动打击感 + 正交下瞄准(GetMouseGroundPoint)正确**。
7. loop 退出条件：2.5D 效果正确 + 0 Error + 无严重 Warning。

## 七、阶段 6 归档（loop 通过后）

- `openspec validate 25-camera-2p5d-system --strict`（已确认能过）
- `openspec archive 25-camera-2p5d-system --yes`（注：CLI `new` 不接受数字开头，手动建的目录；archive 用 `archive <name> --yes`）
- 更新 `项目知识库（AI自行维护）/INDEX.md` §3.2 客户端架构 加条目 + 「最后更新」+ §4 活跃 change（本 change 归档后可能清零）
- tests/results.md 记录 loop 结果

## 八、环境 & 工具备忘

- **unity-skills**：端口 8090，cwd 自动路由（`us() { python .claude/skills/unity-skills/scripts/unity_skills.py "$@" 2>/dev/null; }`）
- **当前状态**：isPlaying=True（InGame，simulator 已装），会话结束后可能已退 Play
- **截图**：`us scene_screenshot filename=xxx.png width=1280 height=720` → 落到 `Assets/Screenshots/xxx.png`（filename 的路径前缀会被吃掉，只认文件名）。`camera_screenshot` 需指定相机（name/path），别用。截完 `Read Assets/Screenshots/xxx.png` 看图。Edit 模式截的是静态帧，要 Play 模式截实时帧。
- **Play 切换**：`editor_execute_menu menuPath=Edit/Play`（`editor_play`/`editor_stop`/`test_run_by_name` 被 semi 模式 never-in-semi 拦截）
- **改代码前必须退 Play**（isPlaying=true 时无法编译，REST 会卡）
- **playtest 菜单**：`Tools/Playtest/01 Enable Simulator` / `Tools/Playtest/Debug/StartGame (-> InGame)` / `Tools/Playtest/Hold/{W,A,S,D}` / `Tools/Playtest/Move/{Right,Left,Stop}`（CJK/带空格路径走 `--stdin-json`）
- **改框架核心**（GameTickDriver/GameApp）用户本轮已批准，无需再问

## 九、openspec 文档位置

- `openspec/changes/25-camera-2p5d-system/proposal.md`（范围/DoD/非目标/风险）
- `openspec/changes/25-camera-2p5d-system/design.md`（8 决策 + API + 排序公式 + 时序 + 跨模块契约，**最全，必读**）
- `openspec/changes/25-camera-2p5d-system/tasks.md`（阶段 0-6，阶段 0-4 已勾）
- `openspec/changes/25-camera-2p5d-system/specs/camera-system/spec.md`（8 Requirement / 24 Scenario，strict validate 已过）
