# PCG World Plan 实现交接

## 当前入口

- 主题配置：`Assets/Resources/PCG/WorldGenerationProfiles.json`
- 纯逻辑规划器：`Assets/Game/Scripts/Runtime/PCGMap/PCGWorldPlanner.cs`
- 运行时适配器：`Assets/Game/Scripts/Runtime/PCGMap/PCGMapGenerator.cs`
- 地图服务与锚点兼容：`Assets/Game/Scripts/Runtime/Services/TotemMapService.cs`
- 环境资源目录：`Assets/Resources/PCG/TerrainVisualCatalog.json` 与 `Assets/Resources/PCG/WorldObjectCatalog.json`

配置的 `themeId` 必须与现有地图主题一致；每个主题必须有一个基底地貌、恰好四个地貌档案、至少一个对每个非基底地貌生效的特征配方，以及事件配额。`features.operation` 仅使用 `blob`、`ribbon`、`chain`、`scatter`、`fringe`；形状、数量和半径由配置决定，而不是向 C# 生成器增加主题分支。

## 事件与资源

事件由 `events` 中的 `eventType`、`visualRole`、数量、最小间距和地貌亲和性驱动。运行时会生成稳定的语义 ID，例如 `player.spawn.000`；同一事件类型可出现多次。地图种子只决定候选锚点集合；玩家进入对局时通过 `ResolveRandomAnchorPosition` 从全部有效出生点中选择一个，不会重建地图。锚点视觉优先按精确 ID 查找，未命中时改按“主题 + visualRole/eventType/tag”选择候选，因而新事件实例无需添加固定 ID。

当前只激活视觉能力：所有格子与视觉实例均不得阻挡移动。`futureCapabilities` 是未来碰撞、减速、遮挡、危险和交互的声明，不能仅因配置出现就产生玩法效果。

`ObjectBudget` 是总静态视觉实例预算，不区分地物或边界装饰。当前生成器只投放数据目录中的静态地物；已废弃按相邻地貌配对的边缘装饰，因此不得通过恢复边缘匹配绕过预算。64×64 的独立逻辑基准（三主题各 30 种子）当前 P95 为 6.5–11.4 ms、总视觉数不超过 160、托管分配约 0.70 MB；这些是非 Unity 场景渲染基线，场景帧时间仍需在编辑器中复核。

## BSP 与旧路径

`BspMaxDepth` 已从 PCG 运行时输入、地图快照、缓存键和生成过程移除。通用数据表 `MapTemplateConfig` 中的同名列暂时保留为兼容字段，PCG 不读取它；删除该列必须走独立的数据表 schema/代码生成迁移，不能手改生成的 C# 文件。

`ZoneRuleCatalog`、边缘匹配、过渡遮罩、水边底图、固定主题模板与固定房间中心锚点都不再是活动 PCG 路径的一部分。

## 验证

自动测试位于 `Assets/Game/Tests/PlayMode/PCGWorldPlannerTests.cs`。独立验证可在项目根目录运行：

```powershell
dotnet run --project Temp/PcgWorldPlanVerification/PcgWorldPlanVerification.csproj -v:q
```

它会以三主题各 30 个固定种子检查 World Plan 与运行时适配的确定性、四地貌、事件下限、全图可通行、视觉不阻挡和资源目录可加载。场景截图与 Unity PlayMode 测试需要在编辑器释放项目锁后执行。
