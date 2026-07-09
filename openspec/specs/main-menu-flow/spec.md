# main-menu-flow Specification

## Purpose

记录迁移后的 GF_X 原生前端流程。旧 `Assets/Scenes/MainMenu.unity` + `GameApp` + `ModuleRunner` 启动链只作为历史证据保存在 archive 和 `LegacyProjectArchive`，不再作为当前运行规格。

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

主菜单到战斗 HUD 的第一轮流程 MUST 由 GF_X UI form 和 runtime service 驱动：`MainMenu -> CharacterSelect -> StartupSelect -> CombatHUD`。

#### Scenario: 开始游戏进入角色选择

- **GIVEN** `MainMenu` 已打开
- **WHEN** 玩家点击开始按钮
- **THEN** `TotemUIService.OpenCharacterSelect()` MUST 打开 `CharacterSelect`
- **AND** `TotemGameFlowService` MUST 进入 `CharacterSelect` 状态

#### Scenario: 角色选择进入启动选择

- **GIVEN** `CharacterSelect` 已打开
- **WHEN** 玩家选择一个角色并继续
- **THEN** `TotemUIService.OpenStartupSelect()` MUST 打开 `StartupSelect`
- **AND** 选中的角色 MUST 写入 `TotemGameFlowService` 的启动选择状态

#### Scenario: 启动选择进入战斗 HUD

- **GIVEN** `StartupSelect` 已打开
- **WHEN** 玩家确认初始颜色、武器和图案
- **THEN** `TotemGameFlowService.ConfirmStartup(...)` MUST 保存启动选择
- **AND** `TotemUIService.OpenCombatHud()` MUST 打开 `CombatHUD`
- **AND** 战斗 HUD MUST 能读取玩家 HP、武器、技能、敌人数量、缩圈、NPC/交互提示等 GF_X runtime 数据

### Requirement: Legacy scene roots MUST stay archived

旧 `Assets/Scenes/Launch.unity`、`Assets/Scenes/MainMenu.unity` 和旧沙盒场景 MUST 移出活动 `Assets` 根目录，保存在 `LegacyProjectArchive/Assets/Scenes` 作为历史参考。

#### Scenario: 活动场景根保持干净

- **GIVEN** 项目完成 GF_X 启动迁移
- **WHEN** Clean Workspace 诊断运行
- **THEN** `Assets/Scenes` MUST NOT 存在
- **AND** BuildSettings MUST NOT 启用任何旧 `Assets/Scenes/*` 场景
- **AND** `Assets/Game/Scene/Launch.unity` MUST 保持存在并启用
