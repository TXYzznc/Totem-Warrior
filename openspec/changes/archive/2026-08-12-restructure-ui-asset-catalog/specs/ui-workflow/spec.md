## ADDED Requirements

### Requirement: UI 结构文档必须声明运行时展示槽位与通用组件归属

阶段 1 的 `prefab-layout.md` MUST 对每个包含 Image 的节点声明其素材职责：`ui-shell`、`runtime-slot` 或 `data-asset`。每个 `ui-shell` MUST 标明其专属或通用归属；每个 `runtime-slot` MUST 标明其容器、遮罩、比例与运行时加载来源。页面不得把角色、物品、纹身、皮肤或预览内容误列为静态 UI Sprite。

#### Scenario: TattooStudioForm 的预览区域
- **WHEN** art-ui 为 TattooStudioForm 输出 `prefab-layout.md`
- **THEN** 角色预览、纹身贴花和前后材质预览 MUST 声明为 `runtime-slot` 或 `data-asset`，而预览框、遮罩、分隔箭头和状态边框 MUST 声明为 `ui-shell`

#### Scenario: 跨页面复用组件
- **WHEN** art-ui 发现多个页面使用同形的卡片、选择框、九宫格边框、进度轨道或按键提示底板
- **THEN** `prefab-layout.md` MUST 将它们列入跨页复用组件，并指向 `通用UI组件` 中的唯一资源名
