## ADDED Requirements

### Requirement: GF_X 活动区路径契约
GF_X 迁入当前项目后，框架、工具、诊断和数据入口 SHALL 使用 `Assets/Game` 与 `GameData` 作为主活动路径。迁入内容 MUST NOT 引入新的 `AAAGame` 活动路径或本机绝对路径硬编码。

#### Scenario: 路径污染扫描通过
- **WHEN** 运行迁移路径诊断或等价扫描
- **THEN** 报告 MUST 标记 `Assets/Game` 与 `GameData` 为 GF_X 活动路径
- **AND** 报告 MUST 不包含新的 `AAAGame` 活动路径污染
- **AND** 报告 MUST 不包含新的本机绝对路径污染

### Requirement: 当前业务保留
第一阶段迁移 MUST 保留当前项目现有玩法、业务脚本、资源和场景。业务文件 MAY 被移动或重命名，但迁移记录 MUST 能说明原路径、目标路径和保留理由。

#### Scenario: 业务文件未被静默删除
- **WHEN** 第一阶段迁移完成
- **THEN** 当前项目原有业务脚本和资源 MUST 仍可在项目中找到或通过迁移记录追踪
- **AND** 迁移不得以删除业务功能作为完成条件

### Requirement: 依赖冲突先处理
GF_X 依赖迁入前 MUST 对比当前项目已有包和插件。UniTask 与 DOTween MUST 以 GF_X 自带版本为准，当前项目已有版本 MUST 从 Unity 编译路径移除。TextMesh Pro、URP、Newtonsoft 等依赖 MUST 避免重复类型、重复 asmdef 或版本冲突。

#### Scenario: 重复依赖被排除
- **WHEN** 迁入 GF_X 依赖
- **THEN** 迁移记录 MUST 标明哪些依赖被复用、哪些被迁入、哪些被移除或排除
- **AND** Unity 编译 MUST 不因重复 UniTask 或 DOTween 类型失败

### Requirement: 核心文件确认门槛
触及当前旧框架绑定文件前，执行者 MUST 先向用户点名确认。受保护范围包括 `Assets/Scripts/Core/*`、启动入口、Input、DataTable、UI、场景加载、`ProjectSettings/EditorBuildSettings.asset` 和 `Packages/manifest.json`。

#### Scenario: 修改核心文件前确认
- **WHEN** 迁移步骤需要修改受保护文件
- **THEN** 执行者 MUST 先列出文件、修改目的、风险和验证方式
- **AND** 在用户确认前 MUST NOT 修改这些文件

### Requirement: 第一阶段可运行验收
第一阶段完成后，当前项目 SHALL 能打开、编译并启动。现有业务不要求在第一阶段完全接入 GF_X 生命周期，但迁入内容 MUST 不破坏项目基本运行。

#### Scenario: 项目基础运行通过
- **WHEN** 第一阶段迁移完成并运行 Unity 编译或等价验证
- **THEN** 项目 MUST 不出现由迁移引入的编译错误
- **AND** 当前启动路径 MUST 仍可进入可运行状态

### Requirement: 示例内容隔离
GF_X 示例内容如果迁入，MUST 集中放在 `Examples` 目录或等价隔离区域。示例内容 MUST NOT 进入默认启动流程、构建场景顺序或当前业务模块注册链。

#### Scenario: 默认启动不依赖示例
- **WHEN** 检查构建场景、启动场景和运行初始化链
- **THEN** 默认启动 MUST 不依赖 GF_X 示例场景或示例脚本
- **AND** 示例内容 MUST 可被识别为非生产业务内容
