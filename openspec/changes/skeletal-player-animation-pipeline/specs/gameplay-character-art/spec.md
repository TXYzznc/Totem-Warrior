## ADDED Requirements

### Requirement: Character art pipelines SHALL preserve legacy frame animation while adding skeletal previews

当项目增加角色骨骼动画资源时，系统 SHALL 将其作为独立预览管线导入。系统 MUST 保留既有逐帧角色资源、逐帧 Clip、既有 Animator Controller 和现有运行时 Prefab 绑定，除非后续变更明确要求并完成切换验证。

#### Scenario: Adding a skeletal M02 preview
- **WHEN** `ActorCommonM02Skeletal` 资源被创建或重新导入
- **THEN** `ActorCommonM02` 的既有逐帧资源与标准控制器 MUST 保持存在
- **AND** 既有 Player、SmartAI、LightAI Prefab MUST 继续引用标准逐帧控制器
