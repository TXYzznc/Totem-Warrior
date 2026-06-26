# 07-main-menu-flow — 主菜单 + 角色选择 → 进战斗场景流程

> 状态：草案 / 已实现 MainMenu 场景与 UI 动态加载基础设施
> 创建日期：2026-06-26
> 触发：v2.1 实测发现"启动即战斗"违反产品体验，用户要求补充主菜单流程

## 一、为什么做

v2.1 GDD 全套设计文档专注于战斗系统与三层原子纹身玩法，**没有显式定义启动后的导航流程**。实测发现：

- 游戏一启动直接进 Launch.unity，21 模块就绪后 SpawnerModule 立刻 spawn 50 actor、Bot 开始 AI 决策、缩圈倒计时开始
- 玩家没有"开始游戏"的明确动作 → 体验上是"被强行拽进战斗"
- 也没有让玩家选择"开始 / 退出 / 设置 / 角色"的入口

需要补一个 **MainMenu 场景作为启动场景**，玩家明确点击"开始游戏"才进 Launch 场景。

## 二、目标

1. **MainMenu 场景启动**：玩家打开游戏先见到主菜单（标题 + 开始 + 退出按钮）
2. **导航完全解耦战斗**：MainMenu 场景**不**启动 GameApp（不创建 21 模块，不 spawn 50 actor，零战斗系统开销）
3. **9 个 Form 改为运行时动态加载**：UIModule 在 GameApp 就绪后从 UIFormConfig 表读取 9 个 Form Prefab，统一实例化到 UIRoot（`DontDestroyOnLoad`），不再在 Launch 场景里预先放置 `_Temp` 实例
4. **后续可扩展 CharacterSelect**：MainMenu → CharacterSelect → InGame 三段流程（v2.2 实现，本次只做 MainMenu → InGame）

## 三、关键决策

| 决策 | A 备选 | B 备选 | 选择 | 原因 |
|---|---|---|---|---|
| MainMenu 是否独立场景 | 独立 `MainMenu.unity` | 单场景 + UI 状态机 | **A 独立** | 完全切断战斗系统开销；玩家在主菜单时 GameApp 未启动；切换战斗有明确的"载入"过渡 |
| MainMenu 是否需 GameApp | 挂 GameApp | 不挂 | **不挂** | 主菜单零业务模块依赖；唯一脚本 `MainMenuLauncher` 只管按钮 → `SceneManager.LoadScene("Launch")` |
| 9 个 Form Prefab 何时实例化 | 场景预放 | 动态加载 | **B 动态** | 12-Agent 并行实现时场景预放出 bug（缺脚本/RenderMode 错）；动态加载在 UIModule 中统一处理，可强制矫正 RenderMode/RectTransform |
| 状态转换驱动 | InputModule 按键事件 | UI 按钮 onClick | **B 按钮** | 主菜单玩家只用鼠标点；不走 InputModule 维护成本更低；InputModule 的按键是战斗内操作专用 |

## 四、不做

- 不做角色选择面板 UI（仅留接口；v2.2 实施）
- 不做"继续游戏 / 读档"按钮（v2.2 接入 SaveModule）
- 不做主菜单背景动画与音乐（待美术资源到位）
- 不做设置面板（v2.2）
- 不改任何战斗模块代码（SpawnerModule / BotControllerModule 等保持原状）

## 五、验收标准

- [x] `Assets/Scenes/MainMenu.unity` 存在，含 MainCamera + EventSystem + MenuCanvas（StartButton + QuitButton + 标题）+ MainMenuRoot（挂 MainMenuLauncher）
- [x] `MainMenuLauncher.cs` 创建：StartButton.onClick → `SceneManager.LoadScene("Launch")`；QuitButton.onClick → `Application.Quit()`
- [x] `ProjectSettings/EditorBuildSettings.asset` MainMenu 在 Launch 之前（启动场景）
- [x] `UIModule.cs` 添加 `OnGameReady` 事件订阅，从 UIFormConfig 读 9 Form Prefab 动态实例化到 UIRoot（DontDestroyOnLoad），加载后强制 RenderMode=ScreenSpaceOverlay + SortOrder + 全屏 stretch + 喂初始 GameState
- [x] `MainMenuForm.OnStartClicked` 调 `GameStateModule.StartGame()`（GameState 从 MainMenu → InGame）
- [x] Launch 场景中 4 个 `_Temp` 实例已删除（apply 改动回 Prefab 后清理）
- [x] PlayMode 实测进 MainMenu 场景 → 0 异常 + `[MainMenuLauncher] Action=Ready` 日志

## 六、关键约束

- 切场景时 `DontDestroyOnLoad(UIRoot)` 保证 9 个 Form 不被销毁，从 MainMenu → Launch 时 Form 持续存在并按新场景的 GameState 自动显隐
- MainMenu 场景**不含** GameApp，因此切到 Launch 之前 ModuleRunner 不启动，21 模块不消耗资源
- Launch 场景的 GameApp 在玩家点"开始游戏"后才挂载触发，避免主菜单时 49 Bot AI 决策、缩圈倒计时等空跑

## 七、当前实施已完成

本次 change 已落地代码 + 场景。剩余工作（v2.2 跟进）：

1. CharacterSelect 中间过渡（玩家选择身份后再进战斗）
2. 主菜单背景资源（待美术）
3. SaveModule 接入"继续游戏"按钮
4. SpawnerModule + BotControllerModule 改为 `OnGameStateChanged(InGame)` 才 spawn（减少 Launch 场景启动后的空跑开销，目前 GameApp 启动后所有模块立刻就绪 + 49 Bot 立刻 tick）
