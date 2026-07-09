# Change 28: PCG 地图运行时接入

## 背景

当前 GF_X 原生重构已经有 `TotemMapService`、地形网格、房间、16 个地图锚点、缩圈和下游 Actor/NPC/Chest/Weapon/Event 消费链路。但地图内容仍是代码内置的确定性主题函数与占位几何，不能承载已验证的 2.5D 美术指导型 PCG 地图。

`PCG示例` 已在另一个项目中验证过生成器、资源 catalog 与可视化资源。本变更把它接入当前项目，作为游戏进入战斗 HUD 时的地图初始化模块。

## 目标

- 把 `PCG示例` 的 PCG runtime/data 代码迁入 `Assets/Game/Scripts/Runtime/PCGMap`。
- 把 PCG catalog 与图片资源按原 `Resources` 相对路径迁入，避免批量改写 1300+ 资源引用。
- 让 `TotemMapService.BuildLayout` 优先使用 PCG 生成地图，并适配为现有 `TotemMapSnapshot`。
- 保留现有下游契约：`Rooms`、`AnchorPlacements`、`TerrainGrid`、`InitialZoneCenter`、`MapSize`。
- 在运行时用 PCG 地表切片、对象和 POI 创建可见 2.5D 地图。
- 扩展诊断，确认 PCG 生成、地形语义、锚点、运行时渲染和资源加载都可验证。

## 非目标

- 本轮不重做 Tilemap/mesh 合批优化。
- 本轮不补齐示例 catalog 中已经标记为 TODO 的旧过渡贴花资源。
- 本轮不调整战斗手感、AI 行为深度或数值平衡。
- 本轮不把地图生成改为服务端权威或联网同步。

