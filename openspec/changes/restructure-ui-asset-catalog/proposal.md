## Why

现有 UI 效果图拆分把画面中可见的角色、纹身、物品和预览内容一并当作界面组件，导致素材不能按运行时职责复用，也遗漏了按钮、卡片等交互对象的状态变体。需要以已确认效果图为视觉依据，建立可实现、可复用的 UI 素材目录和结构契约。

## What Changes

- 为 UI-001、UI-002、UI-004 与 UI-005 的 8 张已确认效果图建立结构先行的组件清单，区分 UI 壳体、运行时展示占位和外部数据资源。
- 按可交互对象补齐 normal、focused、pressed、disabled、locked、loading、success、failure 等必要状态资源；普通文案继续由 TMP_Text 承担。
- 新建 `artifacts/美术资源需求/通用UI组件/`，集中存放跨页面复用且可通过 Unity Tint、九宫格或拉伸适配的基础组件。
- 各页面仅保留专属 UI 壳体和页面语义资源；角色模型、纹身贴花、物品图标和材质预览改为运行时加载槽位，不再作为页面 UI 素材生成。
- 旧版误拆产物保留为审计资料，不删除；新版素材通过版本化目录与可追溯生成记录交付。

## Capabilities

### New Capabilities

- `ui-asset-catalog`: 定义从效果图到 Unity UI 素材的职责分类、状态覆盖、跨页复用、绿幕生成、切图与透明验收规则。

### Modified Capabilities

- `ui-workflow`: 补充阶段 1 结构文档中对“运行时展示槽位”和“通用 UI 组件归属”的强制声明，确保阶段 4 仅生成真实 UI 壳体素材。

## Impact

- `artifacts/美术资源需求/UI/UI-001_常驻HUD组件/`
- `artifacts/美术资源需求/UI/UI-002_纹身工作台界面/`（固定以 C_v2 为视觉依据）
- `artifacts/美术资源需求/UI/UI-004_物资死亡箱撤离反馈/`
- `artifacts/美术资源需求/UI/UI-005_熟练度与构筑档案/`
- `artifacts/美术资源需求/通用UI组件/`
- `openspec/changes/restructure-ui-asset-catalog/art/`

不修改 Unity 业务代码、Prefab 或既有确认效果图；后续接入时由 UI 实现层按本变更的结构文档加载槽位内容。
