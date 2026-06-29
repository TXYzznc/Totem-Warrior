# Spec Delta — core-ui-screens

## ADDED Requirements

### Requirement: 核心 UI 清单覆盖完整游戏循环

项目 MUST 至少包含以下 10 个核心 Form 覆盖完整游戏循环：MainMenuForm / CharacterSelectForm / CombatHUDForm / TattooStudioForm / TattooEnchantForm / ShopForm / ThreeChoiceForm / PauseMenuForm / RunResultForm / SettingsForm，外加 SelfTattooForm（玩家自纹身机制载体）。

#### Scenario: 主循环 Form 全在 Resources/Prefab/UI
- **WHEN** 列 `Assets/Resources/Prefab/UI/*.prefab`
- **THEN** 至少应找到上述 11 个 Form Prefab

### Requirement: 每个 Form 必须有效果图作准绳

除已在 10-settings-form 单独处理的 SettingsForm 外，其余 9 个 Form（含 SelfTattoo）MUST 在 `art/mockups/` 下有用户确认过的 PNG 效果图，且 Prefab 视觉 MUST 与效果图对齐。

#### Scenario: mockups 数量等于 Form 数量
- **WHEN** 列 `openspec/changes/12-core-ui-screens/art/mockups/` (归档后路径同步)
- **THEN** 至少含 9 张 PNG（含 SelfTattoo），与 Form 一一对应

#### Scenario: 视觉对齐
- **GIVEN** 任一 Form 的 mockup 已确认
- **WHEN** PlayMode 打开该 Form 截图
- **THEN** 间距 / 字号 / 配色 与 mockup 一致；如不一致 MUST 在新建 follow-up change 中修复

### Requirement: Form 间交互链路与时序

10 个 Form MUST 通过 UIModule 的 Sort Order 分层（0=底层 / 10=覆盖层 / 20=弹窗 / 30=系统）+ EventBus 异步事件 + ESC/B 键关闭逻辑相互衔接，构成可玩端到端循环。

#### Scenario: 主菜单进入战斗循环
- **GIVEN** MainMenu 显示
- **WHEN** 选「开始」→ CharacterSelect → 战斗开始
- **THEN** UIModule 应在每一步正确关闭上层 Form 并打开下层，不出现层级遮挡死锁

#### Scenario: 暂停弹窗与 Settings 复用
- **GIVEN** 战斗中按 ESC
- **WHEN** PauseMenu 打开
- **THEN** Settings 可从此处入口打开；关闭 Settings 返回 PauseMenu；再关闭返回 HUD

### Requirement: 联调发现 bug 修复路径

UI 显示 / 交互 bug MUST 在本 change 内修复；非 UI 范畴（如战斗数值、AI 行为）MUST 记录到 `tests/bugs.md` 并转给对应系统 owner，不在本 change 处理。

#### Scenario: bug 分流
- **GIVEN** PlayMode 联调发现某 Form 渲染错位
- **WHEN** 定位是 RectTransform 配置错误
- **THEN** 当场修；若是 CombatModule 的数据错误，记 bugs.md 转 client-unity
