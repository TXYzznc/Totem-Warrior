# Tasks: PCG 地图运行时接入

- [x] T1 迁入 PCG runtime/data 代码到 `Assets/Game/Scripts/Runtime/PCGMap`。
- [x] T2 迁入 PCG catalog 与图片资源到 `Assets/Resources/PCG` 和 `Assets/Resources/Sprite/PCG`。
- [x] T3 修复示例生成器迁入后的编译依赖问题。
- [x] T4 扩展 `TotemMapSnapshot` / `TotemMapRuntimeSnapshot` 的 PCG 元数据。
- [x] T5 让 `TotemMapService.BuildLayout` 优先调用 PCG 生成器。
- [x] T6 把 `PCGMapData` 适配为当前 `TerrainGrid`、`Rooms` 和 `AnchorPlacements`。
- [x] T7 用 PCG 资源创建运行时可见地图。
- [x] T8 更新地图诊断，验证 PCG 生成、渲染、缺图和现有地图消费者契约。
- [x] T9 运行 Unity 编译与 GF_X 全量诊断：`GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260709_172937.json`，27 success / 0 failure / 0 warning。
- [x] T10 如有 PlayMode/Smoke 问题，按诊断日志迭代修复；已修复 PCG Cover 对伤害诊断的影响、NPC/地图事件交互焦点优先级、旧 `Assets/Editor` 残留、UI 导入面板编译错误。
