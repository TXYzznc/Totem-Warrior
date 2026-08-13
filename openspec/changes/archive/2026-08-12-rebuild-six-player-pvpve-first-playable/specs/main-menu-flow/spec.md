## MODIFIED Requirements

### Requirement: Main menu flow MUST progress through GF_X UI services

主流程 MUST 为 `MainMenu → LocalMatchConfirm → OpeningBuild → CombatHUD → FirstPlayableResult → MainMenu`，不得进入 CharacterSelect、StartupSelect 或武器选择。

#### Scenario: 开始本地对局
- **WHEN** 玩家点击开始游戏
- **THEN** 显示 6 人、3 支双人队、Bot 补位、五轮和四次缩圈范围
- **AND** 确认后创建 roster/team/seed 并进入 60 秒开局构筑

#### Scenario: 五轮结果返回主菜单
- **GIVEN** 第五轮已经结算
- **WHEN** 玩家选择返回
- **THEN** 对局 runtime 必须完整清理
- **AND** 主菜单重新可交互

## ADDED Requirements

### Requirement: 主菜单必须提供第一版必要功能

主菜单 MUST 提供开始游戏、纹身与元素档案、玩法帮助、设置、制作人员、退出确认和版本/构建信息；开发构建额外提供 seed 与快速模式入口。发布构建不得显示账号、好友、商店、排行榜、继续游戏或角色选择。

#### Scenario: 发布构建打开主菜单
- **WHEN** 玩家在非开发构建进入主菜单
- **THEN** 能访问第一版必要功能和版本信息
- **AND** 看不到账号、好友、商店、排行榜、继续游戏或角色选择入口
