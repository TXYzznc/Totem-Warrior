## Why

GF_X 框架已经迁入并成为 Unity BuildSettings 的启动入口，但原项目业务仍主要运行在旧的 `Assets/Scripts` 自研框架上：

```text
GameApp -> ModuleRunner -> EventBus -> DataTableModule -> Resources/DataTable + Resources/Prefab
```

GF_X 当前启动链只进入 `WorkspaceProcedure`，没有接管旧业务模块、业务配置表、UGUI Form、美术资源和测试入口。因此“框架迁移完成”不等于“业务已经 GF_X 化”。如果现在继续开发，会出现两套生命周期、两套数据表、两套路由/日志/资源加载并行，AI 后续定位问题会越来越困难。

## What Changes

- 先分析并锁定旧业务已经实现的效果，包括主菜单、角色选择、战斗 HUD、玩家/AI/Bot、纹身构筑、武器、技能、经济、NPC、三选一事件、VFX、音频、存档、设置、地图与相机。
- 以 GF_X `Assets/Game/Scene/Launch.unity -> HotfixEntry -> PreloadProcedure -> WorkspaceProcedure` 为唯一启动链，把业务运行时接入 GF_X Procedure 流程。
- 将旧 `GameApp/ModuleRunner/EventBus/DataTableModule/UIModule/InputModule` 只作为需求证据来源，反推出职责后用 GF_X 原生 Procedure、服务、GF Event、GF DataTable、GF UIForm 和诊断场景重写。
- 迁移业务配置表与资源引用，避免继续扩大旧 `Resources/DataTable` 与 GF_X `GameData` 的双轨裂缝。
- 保留已实现玩法效果，不以“重写丢功能”换取表面上的框架统一。
- 自动运行非界面验证：编译、GF_X 诊断、AI DataTable 校验、EditMode/PlayMode 或等价 headless 验证；运行界面上的 playtest 不要求自动执行。

## Current Baseline

当前事实：

- GF_X 启动入口已启用：`ProjectSettings/EditorBuildSettings.asset -> Assets/Game/Scene/Launch.unity`。
- GF_X 诊断在迁移阶段已全绿。
- 旧业务没有被整体重构到 GF_X；`Assets/Scripts/Core/GameApp.cs` 仍手动注册 20+ 旧模块。
- 旧业务配置表已归档到 `LegacyProjectArchive/Assets/Resources/DataTable/*.json`，当前运行配置由 GF_X-native catalogs / DataTables 接管。
- 旧 UI 仍通过 `UIModule` 从 `Resources/Prefab/UI` 加载并依赖旧 `GameApp` 取得 runtime。
- 当前 Unity Editor 正在打开项目，batchmode TestRunner 不能并发打开同一项目；`uloop` CLI 当前不可用。

## Capabilities

### New Capabilities

- `gf-x-business-runtime`: GF_X Procedure 驱动的 Totem Warrior 原生业务运行时入口。
- `gf-x-business-diagnostics`: 覆盖需求反推、启动链、数据表、资源引用、输入边界和模块时序的诊断/测试证据。

### Modified Capabilities

- `gf-x-framework-migration`: 从“框架迁入”推进到“业务接入 GF_X”，不再允许默认启动进入空 Workspace 后无业务。
- `workflow`: 大型业务重构必须以效果清单和测试证据为验收，不以单纯编译通过为完成。

## Impact

- 高影响文件/系统：`Assets/Game/Scripts/Procedures/WorkspaceProcedure.cs`、`Assets/Game/Scripts/ScriptableObject/AppConfigs.cs`、GF_X DataTable/UI/Procedure、旧 `Assets/Scripts/Core/*`、旧 `Assets/Scripts/Modules/*`、归档旧 `LegacyProjectArchive/Assets/Resources/DataTable`、`Assets/Resources/Prefab`。
- 需要谨慎处理的受保护旧核心：`Assets/Scripts/Core/GameApp.cs`、`ModuleRunner.cs`、`EventBus.cs`、`IGameModule.cs`、`GameTickDriver.cs`、`InputModule`、`DataTableModule`、`UIModule`、`SceneModule`。
- 第一批目标是建立 GF_X 原生业务入口与需求反推清单；旧框架入口不得作为运行时宿主，只能作为对照材料，后续按 GF_X 原生切片重写并替换。
