## Context

`Launch` 同时承担 Bootstrap、UI 与业务运行时；`TotemGameProcedure` 同步启动服务，导致 PCG 生成和角色创建发生在玩家可见画面中。当前 Tilemap 使用固定排序层，角色只对自身排序，立体物件没有统一契约。

## Goals / Non-Goals

**Goals:**
- 保留 Launch 作为常驻 Bootstrap 场景，以 Additive 方式异步加载 `TotemGame`。
- 将地图、角色、NPC、人机、POI 与地图立体物件创建到 `TotemGame`，完成前持续展示阶段化 LoadingView。
- 统一世界渲染：地表 Tilemap 最低，其余世界 SpriteRenderer 按世界 Z 排序。

**Non-Goals:**
- 不卸载 Launch，不重做现有加载界面视觉，不改变 PCG 布局算法或资源导入参数。

## Decisions

### 1. Launch 常驻，TotemGame Additive 加载

使用 `SceneManager.LoadSceneAsync("TotemGame", Additive)`；场景可用后设为 Active Scene，再启动业务运行时。这样后续 `new GameObject` 的地图和游戏对象归属 TotemGame，LoadingView 与框架继续保留在 Launch。单场景替换会销毁 Bootstrap，因此不采用。

### 2. 阶段化加载由单一协调器驱动

协调器按“加载场景 → PCG 布局 → 地图视觉 → 角色/世界对象 → UI 就绪”汇报 `stage` 与归一化总进度。LoadingView 仅展示状态，不参与初始化决策；所有阶段成功才隐藏。

### 3. Z 轴作为 2.5D 深度轴

世界平面为 XZ，Y 为高度；排序公式统一为 `baseOrder - RoundToInt(worldZ * precision)`。Tilemap 地表使用低于任何动态世界对象的固定 order；角色、NPC、人机与立体对象共享公式。按世界 Y 会导致大多数对象同 order，故不采用。

## Risks / Trade-offs

- [场景加载成功但初始化失败] → LoadingView 显示失败阶段并保留，日志包含阶段与异常。
- [新对象错误落入 Launch] → 在启动业务前 SetActiveScene(TotemGame)，并在测试中断言地图根节点所属场景。
- [同 Z 闪烁] → 保留对象类型的稳定次序偏移，脚底 pivot 为排序基准。

## Migration Plan

1. 创建空的 TotemGame 场景并加入 Build Settings。
2. 扩展 LoadingView 状态文本与阶段化 API。
3. 加入 Additive 协调流程，迁移业务启动时机。
4. 统一 Tilemap/世界 SpriteRenderer 排序并添加诊断。
5. 通过 PlayMode 与 GF_X 诊断验证；失败时停留在加载页而非进入半成品游戏。

## Open Questions

无。
