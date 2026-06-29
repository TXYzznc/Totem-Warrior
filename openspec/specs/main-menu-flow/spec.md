# main-menu-flow Specification

## Purpose
TBD - created by archiving change 07-main-menu-flow. Update Purpose after archive.
## Requirements
### Requirement: MainMenu.unity MUST 作为启动场景且不挂 GameApp

`Assets/Scenes/MainMenu.unity` MUST 在 `EditorBuildSettings` 中位于 Launch.unity 之前作为启动场景。MainMenu 场景 MUST NOT 含 `GameApp` 组件 — 此时 `ModuleRunner` MUST NOT 启动，21 业务模块 MUST NOT 实例化（避免 SpawnerModule 提前 spawn 50 actor、BotControllerModule 提前空跑 AI、缩圈倒计时空跑）。MainMenu 场景 MUST 含 MainCamera + EventSystem + MenuCanvas（StartButton + QuitButton + 标题）+ MainMenuRoot（挂 `MainMenuLauncher`）。

#### Scenario: 启动游戏第一帧是主菜单

- **GIVEN** 用户启动游戏可执行文件
- **WHEN** Unity 加载第一个场景
- **THEN** MUST 是 `MainMenu.unity`，不是 `Launch.unity`
- **AND** `GameApp` MUST NOT 被实例化
- **AND** `ModuleRunner.StartAsync` MUST NOT 被调用
- **AND** Console MUST 输出 `[MainMenuLauncher] Action=Ready` 日志
- **AND** Console MUST NOT 含任何异常

### Requirement: MainMenuLauncher MUST 处理开始/退出按钮并切换到 Launch 场景

`MainMenuLauncher.cs` MUST 在 `Awake` 时注册 `StartButton.onClick` → `SceneManager.LoadScene("Launch")`、`QuitButton.onClick` → `Application.Quit()`。状态转换 MUST 由 UI 按钮 `onClick` 驱动，MUST NOT 走 `InputModule` 的按键事件路径（`InputModule` 仅用于战斗内操作）。

#### Scenario: 点击开始游戏进入 Launch 场景

- **GIVEN** 玩家正在 MainMenu 场景
- **WHEN** 点击 `StartButton`
- **THEN** MUST 触发 `SceneManager.LoadScene("Launch")`
- **AND** Launch 场景的 `GameApp` MUST 启动 → `ModuleRunner.StartAsync` MUST 跑通 21 模块
- **AND** `MainMenuForm.OnStartClicked` MUST 调 `GameStateModule.StartGame()`（GameState: MainMenu → InGame）

#### Scenario: 点击退出按钮关闭进程

- **GIVEN** 玩家正在 MainMenu 场景
- **WHEN** 点击 `QuitButton`
- **THEN** MUST 调用 `Application.Quit()`
- **AND** 编辑器模式下 MUST 停止 Play

### Requirement: UIModule MUST 在 GameApp 就绪后从 UIFormConfig 动态实例化 9 个 Form

`UIModule.cs` MUST 订阅 `OnGameReady` 事件；事件触发时 MUST 从 `UIFormConfig` 表读取全部 9 个 Form Prefab，统一实例化到 `UIRoot`（挂 `DontDestroyOnLoad`）。每个 Form 实例化后 MUST 强制设置：`Canvas.renderMode = ScreenSpaceOverlay`、`Canvas.sortingOrder` 按表配置、`RectTransform` 全屏 stretch、初始 GameState 喂入。Launch 场景中 MUST NOT 含任何 `_Temp` Form 预放实例。

#### Scenario: 切换场景后 Form 持续存在

- **GIVEN** `GameApp` 已就绪，9 个 Form 已动态实例化到 `UIRoot`
- **WHEN** `SceneManager.LoadScene("Launch")` 触发
- **THEN** `UIRoot` MUST NOT 被销毁（`DontDestroyOnLoad`）
- **AND** 9 个 Form 实例 MUST 持续存在，按新场景的 `GameState` 自动显隐

#### Scenario: Form 实例 RenderMode 矫正

- **GIVEN** UIFormConfig 中某 Form Prefab 因历史原因 `Canvas.renderMode = WorldSpace`
- **WHEN** `UIModule` 实例化该 Form
- **THEN** 实例的 `Canvas.renderMode` MUST 被覆盖为 `ScreenSpaceOverlay`
- **AND** `RectTransform` MUST 被强制设为全屏 stretch（anchorMin=(0,0), anchorMax=(1,1), offsetMin=offsetMax=0）

