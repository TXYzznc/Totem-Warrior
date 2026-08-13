## ADDED Requirements

### Requirement: 第一阶段视觉打磨优先保证战斗信息可读
命中、弱点、元素层、反应、队友、倒地和危险区反馈 MUST 在 6 人与 PVE 混战中可辨；装饰效果不得遮挡准星、目标轮廓、弱点或关键 HUD。

#### Scenario: 高强度混战截图评审
- **WHEN** 六人和多个不带造型定义的占位目标在同屏触发效果
- **THEN** 评审者仍能定位自身状态、队友、当前通用目标标记和主要反应
- **AND** 评审不产出或固化任何敌人外观、弱点位置或动画结论

### Requirement: 关键反馈必须具备多通道语义
火/冰/雷、弱/标准/强、友方/敌方和可救援/将淘汰状态 MUST 同时使用至少两种视觉通道，如颜色加形状、图标加节奏或轮廓加文字。

#### Scenario: 色盲模拟
- **WHEN** 应用红绿色盲或蓝黄色盲模拟
- **THEN** 关键状态仍能被正确区分

### Requirement: 打磨范围不得扩展到后续版本内容
本 change MUST NOT 为 Boss、Boss 核心、撤离点、第 4/5 轮、高阶资源和局外熟练度制作最终 polish。

#### Scenario: 发现旧撤离效果图
- **WHEN** 旧资源盘点发现已完成撤离视觉
- **THEN** 资源保留为历史参考或后续候选
- **AND** 不计入第一阶段完成度

## REMOVED Requirements

### Requirement: 暴击数字飘字（TC-Polish-01）
**Reason**: 旧暴击/头部机制与新事件队列、弱点反馈需要统一重设，不能沿用固定 2D 表现测试。
**Migration**: 新弱点与伤害数字样式由第一阶段 UI/VFX layout 重新定义。

### Requirement: 头顶状态图标（TC-Polish-02）
**Reason**: 旧状态图标未覆盖三元素三层、六人队伍和非人型弱点。
**Migration**: 使用本 change 的元素/弱点多通道规范替代。

### Requirement: hitspark 粒子 + 镜头抖动（TC-Polish-03）
**Reason**: 旧参数与 3D 单枪械、效果队列和可读性预算不匹配。
**Migration**: 在单枪械与事件队列 VFX 任务中重新确认命中反馈和镜头影响。

### Requirement: HP<30% Vignette 闪烁（TC-Polish-04）
**Reason**: 旧固定阈值表现未考虑护盾、倒地、救援和新 HUD 信息层级。
**Migration**: 由新 HUD/倒地反馈设计统一定义低生命提示。
