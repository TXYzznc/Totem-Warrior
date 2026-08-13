## ADDED Requirements

### Requirement: 第一阶段角色美术采用现有 3D 方向
第一阶段 MUST 使用现有角色相关 3D、统一骨骼、服装和六部位纹身载体方向；美术 change 不重新进行 2D sprite 角色生产或角色选择阵容扩展。

#### Scenario: 建立默认角色交付
- **WHEN** 第一阶段需要一个默认可玩角色
- **THEN** 从现有确认方向中选择/复用可用 3D 资产
- **AND** 不创建多角色选择所需额外资产

### Requirement: 角色资产必须支持第一与第三人称消费
角色交付 MUST 明确完整第三人称身体、第一人称手臂/武器挂点、统一骨骼和六部位纹身显示的兼容关系。

#### Scenario: 视角切换检查
- **WHEN** 使用同一角色在第一与第三人称查看
- **THEN** 不出现重复身体、明显穿插或纹身挂载丢失

## REMOVED Requirements

### Requirement: 角色 sprite 资源组织
**Reason**: 当前产品已转为 3D 第一/第三人称表现，旧 2D sprite 组织不再是第一阶段角色交付合同。
**Migration**: 旧 sprite 保留为历史资料，不进入当前 runtime asset catalog 主路径。

### Requirement: AnimatorController 参数契约
**Reason**: 旧规范绑定 2D 四方向动画参数，不能约束当前 3D Animator。
**Migration**: 3D 动画参数由后续角色动画/程序接入规格单独定义。

### Requirement: SpawnerModule 禁用 Cube 占位
**Reason**: 占位与正式资源切换属于主玩法 change 的运行时职责，不由美术生产规格直接约束模块。
**Migration**: 美术提供稳定 Prefab key，主玩法负责替换占位。

### Requirement: PlayerAnimatorBridge 事件桥接
**Reason**: C# 动画事件桥接属于程序实现，不属于独立美术任务。
**Migration**: 由主玩法 change 或后续动画接入 change 管理。

### Requirement: 0 Console Error 退出门槛
**Reason**: 项目级 Console 门槛保留在程序/QA 验收，不作为角色美术 capability 的独立 requirement。
**Migration**: 美术导入检查仍需提供无导入错误证据。

### Requirement: 美术素材失败重试上限
**Reason**: 生成工具重试策略属于生产流程，不应固化为角色 runtime 规格。
**Migration**: 按当前 AI 美术生产规范和人工评审处理。
