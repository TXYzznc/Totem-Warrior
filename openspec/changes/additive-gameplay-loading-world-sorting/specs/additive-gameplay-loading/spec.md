## ADDED Requirements

### Requirement: 常驻 Bootstrap 的 Additive 游戏场景加载
系统 MUST 保持 Launch 已加载，并异步 Additive 加载 TotemGame；PCG 地图和游戏业务对象 MUST 在 TotemGame 场景中创建。

#### Scenario: 游戏场景加载完成
- **WHEN** 玩家从准备流程进入游戏
- **THEN** Launch 保持已加载，TotemGame 以 Additive 方式加载并成为初始化期间的 Active Scene

### Requirement: 阶段化加载反馈
LoadingView MUST 在场景加载和全部初始化阶段保持显示，并展示阶段文本与总进度。

#### Scenario: 初始化完成后进入游戏
- **WHEN** PCG、地图视觉、角色和 UI 均已就绪
- **THEN** LoadingView 隐藏且游戏进入可操作状态
