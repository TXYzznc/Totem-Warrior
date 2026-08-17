# 当前工程开发约定

> 本文件只记录当前 Unity 2022.3 + GF_X 工程约定。游戏规则以 `Docs/GameDesign/目录.md` 为唯一标准信息源。

## 代码与框架边界

- 业务代码只放在 `Assets/Game/Scripts/`；GF_X 框架核心位于 `Assets/Game/ScriptsBuiltin/`。
- 不恢复旧 `Assets/Scripts`、`GameApp`、`ModuleRunner`、`EventBus` 或旧业务 Module 架构。
- 新业务能力优先接入现有 `TotemGameRuntime` 服务，不在场景对象间建立隐式查找链。
- `ScriptableObject` 用作可序列化配置，不作为运行时数据库。

## 输入与时序

- 所有玩家按键必须通过 `TotemInputService` / `ITotemInputProvider`，不得在业务代码直接读取 `Input`。
- Bot 产生同层 gameplay command，不模拟键盘，也不绕过业务服务直接修改生命、库存或阶段。
- 延迟事件、动画事件和异步表现执行前必须校验所属 PhaseEpoch；构筑阶段切换后旧阶段回调失效。
- 订阅必须在对应生命周期结束时解除；构筑边界、离开 CombatHUD 和 Shutdown 都要可重复执行。

## 配置与资源

- Business 配置的活动事实位于 `GameData/AIData/DataTables/Business/*.json`，策划可读镜像位于 `GameData/DataTables/Business/*.xlsx`；修改结构时同步生成 C# DataRow 与 Gameplay Catalog。
- 运行时视觉键位于 `GameData/AIData/GameplayCatalogs/totem_runtime_assets.json`，不得声明不存在资源为“已完成”。
- `Assets/Game` 中现存美术均已确认并已导入；策划需要但目录中不存在的资源表示尚未制作。
- 不新增 `Assets/Resources/PCG`、旧二维角色目录或旧 `ResourceConfig` 玩法表。

## 性能与质量

- 不在 `Update`、`LateUpdate` 或高频 Tick 中产生可避免的 GC 分配。
- 高频生成对象使用池化或受控复用；缓存查找结果，不逐帧执行全场景扫描。
- 修改业务代码后至少完成编译、相关 EditMode/PlayMode smoke 和 GF_X 全量诊断。
- 自动诊断连接当前项目的 `http://localhost:8090/`。
