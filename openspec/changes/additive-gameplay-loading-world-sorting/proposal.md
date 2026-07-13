## Why

当前业务运行时直接在 `Launch` 场景中同步完成 PCG 地图和游戏对象初始化，玩家只能看到长时间静止或半成品画面。同时，Tilemap、角色和立体世界对象没有统一的排序约定，角色可能被地表压住，破坏 2.5D 可读性。

## What Changes

- 新增常驻 `Launch` + Additive `TotemGame` 场景的游戏进入流程；PCG 地图和游戏业务对象只创建在 `TotemGame` 场景。
- 扩展现有 `LoadingView`，在保留进度条的同时显示阶段文本，并在场景加载和初始化全过程保持可见。
- 将世界渲染排序统一为：Tilemap 地表最低，立体对象、玩家、NPC 和人机根据世界 Y 坐标排序。
- 在所有加载阶段完成前禁止进入可操作游戏状态；完成后隐藏 LoadingView。

## Capabilities

### New Capabilities

- `additive-gameplay-loading`: 保留 Launch 场景并异步加载、初始化独立游戏场景，向 LoadingView 报告阶段化进度。
- `world-y-sorting`: 为 Tilemap、立体世界对象和角色定义统一的 Y 轴渲染排序契约。

### Modified Capabilities

- `core-ui-screens`: LoadingView 增加当前加载阶段文本并支持阶段化进度展示。

## Impact

- 场景：新增 `Assets/Game/Scene/TotemGame.unity`，调整 Build Settings。
- 启动与运行时：`WorkspaceProcedure`、`TotemGameProcedure`、`TotemGameRuntime`、地图和对象生成服务。
- 表现：`TotemMapService`、角色深度排序、立体对象 SpriteRenderer 排序。
- UI：`BuiltinViewComponent` 与 Launch 场景内的 LoadingView 绑定。
- 验证：EditMode/PlayMode 加载流程与渲染排序诊断。
