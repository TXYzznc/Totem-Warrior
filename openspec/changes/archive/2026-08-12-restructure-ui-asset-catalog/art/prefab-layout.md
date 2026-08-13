# restructure-ui-asset-catalog UI 结构文档

## 全局约定

- **画布基准分辨率**：1920×1080，效果图参考分辨率 1672×941。
- **Canvas Scaler**：Scale With Screen Size，Reference Resolution 1920×1080，Match 0.5。
- **安全区**：关键交互与文本置于 90% title-safe 内；HUD 关键状态置于 93% action-safe 内。
- **输入**：所有操作提示由 InputModule 提供当前设备 glyph 与绑定文本；所有可交互项必须有 controller focused 态。
- **文本**：标题、标签、数量、百分比、按键名均使用 TMP_Text；不生成普通文字贴图。
- **节点职责**：`ui-shell` 为静态 Sprite；`runtime-slot` 为运行时加载数据的容器/遮罩；`data-asset` 由业务系统提供；`text` 由 TMP_Text 提供。
- **通用资源**：同形壳体引用 `artifacts/美术资源需求/通用UI组件/`，由 Image Tint、Sliced 或 Filled 变形；页面只持有语义专属壳体。

## 页面清单与视觉依据

| Form | 视觉依据 | 重制目录 |
|---|---|---|
| CombatHUDForm | UI-001 清透机能 HUD v1 | UI-001/已确认方案/结构重制 |
| TattooStudioForm | UI-002 颜料失序工作台 C_v2 | UI-002/已确认方案/结构重制 |
| LootEvacForm | UI-004 清透机能全状态 A | UI-004/已确认方案/结构重制 |
| MasteryOverviewForm | UI-005 A 01 | UI-005/已确认方案/结构重制/熟练总览 |
| PatternDetailForm | UI-005 A 02 | UI-005/已确认方案/结构重制/图案详情 |
| BuildSnapshotForm | UI-005 A 03 | UI-005/已确认方案/结构重制/构筑快照 |
| ProfileShowcaseForm | UI-005 A 04 | UI-005/已确认方案/结构重制/战绩外观 |
| UnlockFeedbackForm | UI-005 A 05 | UI-005/已确认方案/结构重制/解锁反馈 |

## 跨页复用组件

### PanelFrame

`ui-shell`，Sliced，四角 12px；适用于侧栏、详情区、弹窗和通知容器。禁止为颜色差异复制资源。

### ContentCard / ListRow

`ui-shell`，Sliced；卡片内图标、头像、纹身和物品为 `runtime-slot`。卡片有 normal/focused/pressed/disabled；locked 由通用遮罩叠加。

### FocusOutline / SelectionFrame

`ui-shell`，Simple，焦点、选择和危险状态分别通过色彩参数表达。focused 必须有非色彩依赖的 2px 描边/角标。

### ProgressBar / ProgressRing

`ui-shell`，轨道为 Sliced 或 Simple，填充为 Filled/Mask；数值由 TMP_Text。加载状态使用 `state_loading`，不把读条数值烘焙到贴图。

### InputPrompt

`ui-shell`，Sliced 底板；glyph 与绑定文字为运行时子节点。所有操作必须支持 InputModule 替换 glyph。

## 页面结构

以下页面的完整节点树与素材归属由同级 `结构重制/素材清单.md` 细化；该文档只定义跨页契约。每个 `runtime-slot` 保持 `preserveAspect=true`，壳体背景保持 `false` 以适应拉伸。

### CombatHUDForm

- 左上生存状态、右上任务/撤离状态、底部武器/交互提示：关键节点 anchor 到安全区边缘。
- 武器图、角色状态图、物资缩略图、数值均为 `runtime-slot` / `data-asset` / `text`。

### TattooStudioForm

- 左栏部位行、颜料库存；中部角色预览；右栏样式卡和前后对比；底部输入提示、读条和取消。
- 角色模型、纹身贴花、颜料物品、样式图案、材质前后图全部为运行时内容；只生成其容器框、槽位、选择与状态壳体。

### LootEvacForm

- 物资槽、死亡箱标识、撤离环、事件时间线和补给提示为页面壳体；物资本体、掉落图、玩家身份数据为运行时内容。

### UI-005 档案页组

- 图案、角色外观、构筑关系、头像、战绩数据均为运行时内容；仅生成成长容器、权限壳、记录卡、解锁反馈和通用状态层。

## 变更日志

- 2026-07-27：依据用户确认的四项决策创建；UI-002 固定使用 C_v2。
