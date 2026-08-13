# Unity 草地—河流 PCG Pilot 验证记录

## 交付入口

- 场景：`Assets/Game/Scene/PCGGrassRiverPreview.unity`
- 运行组件：`Assets/Game/Scripts/Testing/PCGGrassRiverPreviewController.cs`
- 布局算法：`Assets/Game/Scripts/Runtime/PCGMap/PCGGrassRiverPreviewLayout.cs`
- 场景构建/自检：`Assets/Game/Editor/PCGGrassRiverPreviewSceneBuilder.cs`
- 资源：`Assets/Game/Sprites/PCG/Pilot/GrassRiver/`

打开场景后进入 Play Mode，会以 seed `20260715` 自动生成 16×12 地图。选中 `PCG Grass Water Preview Controller` 可修改 seed、尺寸、最小/最大水域宽度和装饰概率，并使用 Inspector 按钮在编辑态重新生成。默认水域宽度为 2–5 格：每两行向宽或窄变化一格，相邻行至少保留一个共享水格。

## 自动验证证据

- Unity 版本：2022.3.62f3c1。
- Unity 编译：`isCompiling=false`，控制台编译/运行 Error 数为 0。
- 场景自检：通过 additive 打开测试场景并调用一次 `GeneratePreview()`；默认 seed 的水域行宽为 `2,2,3,3,4,4,5,5,4,4,3,3`，范围为 `2..5`，相邻行保持连通；场景自检在清空控制台后无新增 Error。
- 场景引用：8 张草地、8 张河流、6 张装饰均已序列化到 Controller。
- 导入器抽查：Sprite / Single / PPU 256 / Point / mipmap off / Uncompressed / Clamp；装饰开启 alpha transparency。
- 美术资源校验：16 张地貌均为 256×256 且四边 alpha=255；6 张装饰尺寸与透明角通过。

## GF_X 全量诊断

报告：`GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260715_141810.json`

- 34 项通过，3 项失败，36 项既有 DataTable 时序警告。
- 三项失败均不由本 PCG pilot 引入：
  - `GF_X Rewrite Inventory Contract`：两个未分类资源和一个 `classification_needed` 资源均位于用户正在修改的 TattooVisual 配置目录。
  - `Clean Workspace Contract`：既有 `Assets/Screenshots` 目录。
  - `Totem Runtime Assets`：既有 `Player.prefab` 多出 MonoBehaviour；该 prefab 在本任务开始前已处于修改状态。
- 新增 `PCG/Pilot/GrassRiver` 22 张资源均被索引为 `PCGMap / pcg_catalog_bound`，不是诊断失败来源。

## 隔离性

- 未修改 `TotemMapService`、正式 `PCGMapGenerator`、旧 edge/socket catalog 或正式场景流程。
- 测试场景未加入 Build Settings；只用于用户在 Unity Editor 中验证视觉效果。
