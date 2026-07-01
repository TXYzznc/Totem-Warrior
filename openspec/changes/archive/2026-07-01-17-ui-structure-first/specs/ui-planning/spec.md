## REMOVED Requirements

### Requirement: UI 类型素材出图前必须先定表

**Reason**: v3 结构先行流程用单文件 `prefab-layout.md`（含 RectTransform 数据）替代「三表」（页面清单 / 复用组件清单 / 组件状态表）。三表容易变成 checklist 摆设，而 layout 一份文档同时喂养效果图长宽反哺（阶段 2）、素材拆分节点树（阶段 4）、Prefab 层级搭建（阶段 5），减少同一信息的多份维护。

**Migration**: 新 UI 走 `ui-workflow` capability 的 6 阶段流程；阶段 1 由 `art-ui` 用 `unity-rect-transform` SKILL 产出 `prefab-layout.md`。已归档的 v2 UI change 不回溯改造。

### Requirement: 三表数量字段不写死

**Reason**: 三表本身已被 `prefab-layout.md` 取代，本约束随之废弃。layout 中的节点数量按每页实际结构写死，不存在"数量默认值"问题。

**Migration**: 见上一条。
