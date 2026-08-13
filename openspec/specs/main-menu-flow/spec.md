# main-menu-flow Specification

## Purpose

记录迁移后的 GF_X 原生前端流程。旧 `Assets/Scenes/MainMenu.unity` + `GameApp` + `ModuleRunner` 启动链已移除，不再作为当前运行规格。
## Requirements
### Requirement: GF_X Launch MUST own the front-end flow

项目 MUST 使用 `Assets/Game/Scene/Launch.unity` 作为默认启动场景，并通过 GF_X Procedure/runtime service 打开前端 UI，而不是切换到旧 `Assets/Scenes/MainMenu.unity`。

#### Scenario: 默认启动进入 GF_X 主菜单

- **GIVEN** Unity 从 EditorBuildSettings 的默认场景启动
- **WHEN** GF_X 完成 Preload 并进入 Workspace/TotemGameProcedure
- **THEN** `TotemGameRuntime` MUST 启动
- **AND** `TotemUIService` MUST 打开 `MainMenu`
- **AND** `GameApp`、`ModuleRunner`、`EventBus`、`UIModule`、旧 `DataTableModule` MUST NOT 作为运行时依赖被挂载

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

### Requirement: Legacy scene roots MUST stay archived

旧 `Assets/Scenes/Launch.unity`、`Assets/Scenes/MainMenu.unity` 和旧沙盒场景 MUST 移出活动 `Assets` 根目录，且不得恢复到工作区。

#### Scenario: 活动场景根保持干净

- **GIVEN** 项目完成 GF_X 启动迁移
- **WHEN** Clean Workspace 诊断运行
- **THEN** `Assets/Scenes` MUST NOT 存在
- **AND** BuildSettings MUST NOT 启用任何旧 `Assets/Scenes/*` 场景
- **AND** `Assets/Game/Scene/Launch.unity` MUST 保持存在并启用

### Requirement: 主菜单必须提供第一版必要功能

主菜单 MUST 提供开始游戏、纹身与元素档案、玩法帮助、设置、制作人员、退出确认和版本/构建信息；开发构建额外提供 seed 与快速模式入口。发布构建不得显示账号、好友、商店、排行榜、继续游戏或角色选择。

#### Scenario: 发布构建打开主菜单
- **WHEN** 玩家在非开发构建进入主菜单
- **THEN** 能访问第一版必要功能和版本信息
- **AND** 看不到账号、好友、商店、排行榜、继续游戏或角色选择入口
