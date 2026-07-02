# 联调 loop 结果 — 25-camera-2p5d-system

> 阶段 5 联调 loop 执行记录。验证方式：Play 模式下 unity-skills REST（`gameobject_get_transform` 等）读取真实运行时数值 + `scene_screenshot` 截图目视核对。

## 一、地面修复

**根因**：场景同时存在两套地面几何，坐标系不对齐——
- `SpawnerModule.CreateScene()` 的 `Ground`（`PrimitiveType.Plane`，默认居中于世界原点 (0,0,0)），玩家 + 49 个 actor + Boss 全部以**原点为中心**分布（`SpawnerModule` 的环形布点逻辑）
- `MapGenModule.BuildPlaceholderGeometry()` 的 `MapGen_Ground`（150×150，中心在世界坐标 (75,0,75)，只覆盖正 X 正 Z 象限）

两块地面在原点周围的负 X / 负 Z 区域都盖不到，Round 1 截图（`cam2p5d_r1.png`）里的 L 形深色分界就是这个坐标错位造成的，不是「地面太小」这么简单。

**修复**：`Assets/Scripts/Modules/Spawner/SpawnerModule.cs` 第 77 行，`ground.transform.localScale` 从 `(6,1,6)`（60×60，覆盖不足）改为 `(15,1,15)`（150×150，与 `MapGenModule.MapSize` 一致，仍居中于原点，与 actor 分布坐标系对齐）。

**验证**：`cam2p5d_groundcheck.png` / `cam2p5d_groundcheck2.png` / `cam2p5d_boundary.png` 三张截图背景均为纯灰色，无 L 形暗色分界。✅ 通过。

**未动**：`MapGenModule.cs` 不属于本 change 范围，未修改（正象限的地面重叠对视觉无害，两者颜色接近）。

## 二、相机跟随（C3 核心验收）

用 `gameobject_get_transform` 读取 MainCamera 与 Player 的实时 world position，配合 `gameobject_set_transform` 瞬移玩家模拟移动：

| Player 位置 | Camera 焦点响应 |
|---|---|
| (0,0) | 初始对准 |
| (20,10) | 相机同步平滑跟进 |
| (40,40) | 相机跟进到 (18.53,18,-3.59)（SmoothDamp 渐进，符合死区+lookahead+俯角换算预期） |

✅ 通过。相机基准位随玩家移动持续更新，非静止。

## 三、边界 clamp

Player 瞬移到 (200,200)（远超地图 bbox）后，相机被限制在 (65,65) 附近（即 `MapSize/2 - boundaryMargin` = `75-10=65`），未跟出地图外、未露白。✅ 通过。

## 四、深度排序（C7）

`cam2p5d_depthtest.png` 截图核对：靠近相机（世界 Z 更大，画面靠下）的角色正确遮挡了远处角色，方向正确，`DepthSortedSprite.cs` 的 `-worldPos.z` 符号**未翻转**，维持 design.md 原定公式。✅ 通过。

## 五、Billboard

Round 1 截图已确认 sprite 竖直不被压扁；本轮截图（groundcheck/depthtest/boundary）复检未见回归。✅ 通过。

## 六、震动整合

触发 `WeaponAttackHitEvent` → `VFXModule` 调用 `CameraModule.PlayShake(0.5f, 0.18f)` 后，观测相机位置短暂偏移，随后迅速收敛回正常跟随基准位，无持续漂移、未与跟随逻辑打架。✅ 通过。

## 七、瞄准（GetMouseGroundPoint）

未单独造测试手段验证（Playtest 菜单未提供鼠标模拟点击攻击的入口）。按 design.md §六理论分析：正交相机 `ScreenPointToRay` 仍返回平行于相机 forward 的正确世界射线，Y=0 平面求交结果不受 orthographic 影响，代码未改动此逻辑。**理论验证，未做运行时验证**。

## 八、Console 检查

`console_get_stats`：`errors=1`，`warnings=5`。

- **1 Error**：`ExecuteMenuItem failed because there is no menu named 'Tools/Playtest/Debug/StartGame'` —— 联调过程中菜单路径试错导致的工具调用错误，**非运行时代码错误**，不影响游戏逻辑。
- **5 Warning**：均为 `AudioModule` 找不到音频资源（`Audio/SFX/hit_default`、`Audio/BGM/in_game`、`Audio/SFX/player_died`、`Audio/MainMixer`）+ 1 条 unity-skills 自检端口占用提示，**与本次相机改造无关**，音频资源缺失是既有问题。

0 与本 change 相关的 Error，无严重 Warning。✅ 通过。

## 九、参数最终值（CameraModule.cs 私有字段，未改动 Round 1 默认值）

| 参数 | 值 |
|---|---|
| OrthographicSize | 9 |
| CameraTiltX | 55 |
| smoothTime | 0.15s |
| deadZoneHalfW/H | 1.5 / 1.0 |
| lookaheadDist | 3.0 |
| lookaheadSmooth | 0.3s |
| boundaryMargin | 10 |

实测手感可接受，未做进一步调参（MVP loop 验收标准，不做美术精修）。

## 十、遗留问题（本 change 范围外，留给后续处理）

**EnemyModule 与 SpawnerModule 两套敌人生成系统并存**：`EnemyModule.SpawnInitialEnemies()` 额外 spawn 了 2 个 Light（绿色 `PrimitiveType.Sphere`）+ 1 个 Elite（橙色 `PrimitiveType.Capsule`）占位怪，固定坐标 (5,0.4,0)/(-5,0.4,3)/(0,0.4,7)，与 `SpawnerModule` 的 49-actor 环形布点系统同时存在。这批怪用 3D MeshRenderer 而非 SpriteRenderer，未挂 `DepthSortedSprite`/`BillboardSprite`（也不需要挂，非 sprite）。这是历史遗留的架构重复问题，**不属于本 change 范围**，未修改。建议后续开一个独立 change 清理/整合两套敌人生成逻辑。

同时该遗留怪物会主动攻击玩家直到 HP 归零，手动 playtest 时如果玩家死亡会导致 WASD 移动无响应，干扰后续验证操作——如需长时间手动 playtest 验证相机效果，建议临时禁用这批遗留怪或提高玩家 HP。

## 结论

2.5D 相机系统核心能力（正交投影 + 俯角跟随 + billboard + 深度排序 + 震动整合 + 边界 clamp）全部验证通过，地面坐标错位 bug 已修复，0 与本 change 相关的 Error/严重 Warning。**loop 通过，进入阶段 6 归档**。
