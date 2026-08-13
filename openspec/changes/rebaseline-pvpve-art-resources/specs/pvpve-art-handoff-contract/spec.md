## ADDED Requirements

### Requirement: 美术资源需求表是唯一状态台账
所有继续使用、新设计、返工和暂不需要的资源 MUST 在 `artifacts/美术资源需求/美术资源需求表.xlsx` 记录唯一 ID、名称、类型、相对路径、交付说明和合法状态。

#### Scenario: 新增 UI 素材
- **WHEN** 新素材开始生产
- **THEN** 先在台账登记或归属到既有资源 ID
- **AND** 文件放入台账规定路径

### Requirement: 旧资源必须保留可追溯性
旧 UI、旧七元素和旧武器概念 MUST 保留原文件；状态改为需返工或暂不需要时，说明必须记录与当前需求的冲突，禁止无记录覆盖或删除。

#### Scenario: 新 HUD 获得确认
- **WHEN** 新 HUD 视觉验收通过
- **THEN** 旧 HUD 仍可从历史目录追溯
- **AND** 新交付路径与版本明确写入台账

### Requirement: 每项正式交付必须包含消费合同
正式资源 MUST 提供稳定 key/ID、文件格式、尺寸/比例、透明度/色彩空间、切片或挂点、状态变体、目标导入路径和 fallback；UI 素材还必须说明九宫格与可拉伸区。

#### Scenario: 程序接入元素图标
- **WHEN** 主玩法 change 消费图标
- **THEN** 无需读取源效果图即可知道三元素、三层和禁用状态的稳定 key

### Requirement: 美术与程序变更职责必须分离
本 change MUST NOT 修改 C# 业务逻辑、玩法 DataTable 或 GF_X 框架；若发现接口不足，必须在 handoff 文档记录请求，由主玩法 change 决定并实现。

#### Scenario: 效果图需要新增统计字段
- **WHEN** layout 发现现有程序合同缺少字段
- **THEN** 美术 change 记录字段名称、用途和显示状态
- **AND** 不直接修改 runtime service

### Requirement: 状态推进需要分层验收证据
“详细设计待确认”“设计完成-待制作”“待验收”“已完成-符合” MUST 分别对应设计文档/用户确认、正式资产、导入或视觉检查、最终验收证据；OpenSpec task 完成不得自动把台账改为已完成。

#### Scenario: 只有效果图没有切片
- **WHEN** UI 效果图已确认但正式素材尚未拆分
- **THEN** 资源不得标记为已完成-符合
