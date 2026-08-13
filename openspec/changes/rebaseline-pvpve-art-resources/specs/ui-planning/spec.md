## ADDED Requirements

### Requirement: UI 规划必须执行结构先行流程
每个 UI 组 MUST 先产出经确认的 `prefab-layout.md`，再产出效果图提示词、效果图、素材拆分和程序交接；任一阶段未确认不得越级进入下一阶段。

#### Scenario: 从玩法需求启动 HUD 设计
- **WHEN** HUD 只有功能清单
- **THEN** 首个正式产出是 layout 和状态树
- **AND** 不是完整效果图

### Requirement: layout 必须成为程序与美术共同合同
layout MUST 包含节点树、RectTransform、Canvas Scaler、安全区、焦点顺序、动态字段、状态变体和各分辨率 fallback，并使用稳定节点/slot ID。

#### Scenario: 程序用占位资源拼装
- **WHEN** 效果图尚未完成但 layout 已确认
- **THEN** 程序可按 layout 建立可交互 Prefab
- **AND** 后续替换素材不改变业务节点 ID

## REMOVED Requirements

### Requirement: 「先定表」规范全链路引用
**Reason**: 项目 UI 工作流已升级为 v3 结构先行，旧“先定表”规范被正式标记为 superseded。
**Migration**: 所有新 UI 需求改引用 `项目知识库（AI自行维护）/wiki/UI结构先行规范.md` 和本 change 的 layout。
