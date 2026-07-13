## MODIFIED Requirements

### Requirement: LoadingView 显示加载进度与当前阶段
内置 LoadingView MUST 显示归一化加载进度和当前加载阶段文本，并在进入游戏前保持可见。

#### Scenario: 阶段文本更新
- **WHEN** 场景加载或任一初始化阶段推进
- **THEN** LoadingView 同步更新百分比和人类可读的阶段文本
