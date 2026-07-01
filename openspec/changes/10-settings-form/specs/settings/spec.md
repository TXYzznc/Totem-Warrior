---
capability: settings
version: v1
created: 2026-06-26
updated: 2026-07-01
---

# 设置系统规范 (v1.0)

> 本规范固化玩家侧设置面板 + `SettingsModule` 数据层的对外契约。
> v2（2026-07-01）范围调整：按键重绑定砍到 UI disabled 占位，交互延后到 InputModule 升级到 New Input System 后另开 change。

## 背景

游戏原本无系统设置入口，玩家无法调音量、改画质。本 change 新增 `SettingsForm` UI + `SettingsModule` 服务层，覆盖：BGM/SFX 音量拖动即时生效、URP 画质三档、取消回滚、保存持久化到 `SaveModule.Data.Settings`。按键重绑定在 v1.0 只出 UI 占位（3 按钮 disabled + 「即将推出」），实际交互等 InputModule 升级到 Unity New Input System 后再另开 change。

## ADDED Requirements

### Requirement: 持久化到 SaveModule

设置数据 MUST 通过 `SaveModule` 统一存档管道持久化，不能自建独立文件路径。启动时 `SettingsModule.InitAsync` MUST 从 `SaveModule.Data.Settings` 读取上次保存值并 `ApplyAll`；文件不存在或字段为 null 时使用默认值。

#### Scenario: 首次启动无存档

- **WHEN** 首次启动，`SaveModule.Data.Settings` 为 null
- **THEN** `SettingsModule.InitAsync` 用 `new SettingsData()`（BGM=1.0 / SFX=1.0 / QualityLevel=1）作为初始值
- **AND** 立即 `ApplyAll` 到 `AudioModule` + `QualitySettings`
- **AND** 不发任何事件（Init 阶段约束）

#### Scenario: 保存后重启保留

- **WHEN** 玩家把 BGM 拖到 0.3，点保存 → 退出 → 重新启动
- **THEN** 启动后 `SettingsModule.GetCurrent().MusicVolume == 0.3`
- **AND** `AudioModule` 输出音量应用为 0.3

### Requirement: 音量拖动即时生效

BGM / SFX 滑动条 MUST 在玩家拖动过程中即时改变输出音量，无需等玩家点保存。数值范围 0.0 ~ 1.0，UI 步长 0.01。内部走 `AudioModule.SetBgmVolume` / `SetSfxVolume` 接口。

#### Scenario: 拖动 BGM 滑动条

- **WHEN** SettingsForm 打开中，玩家拖 BGM slider 从 1.0 → 0.5
- **THEN** 每次 `Slider.onValueChanged` 触发 `SettingsModule.Preview(draft)`
- **AND** `AudioModule.SetBgmVolume(0.5)` 被调用
- **AND** 不写盘、不发事件

### Requirement: 画质三档切换即时套用

画质三档单选 MUST 立即调用 `QualitySettings.SetQualityLevel(level, applyExpensiveChanges: true)`。三档对应项目现有 URP 资产：`URP-Performant` / `URP-Balanced` / `URP-HighFidelity`。

#### Scenario: 玩家切到 High

- **WHEN** 玩家点「高」radio 按钮
- **THEN** `SettingsModule.Preview(draft.with(QualityLevel=2))` 被调用
- **AND** `QualitySettings.currentLevel == 2`
- **AND** UI 三个 radio 按钮的选中态更新，选中项高亮

### Requirement: 按键重绑定 UI 占位（v1.0 交付）

v1.0 交付 SettingsForm 时 MUST 包含重绑定区（3 行：Move / Attack / Pause），但每个按钮 `interactable = false`，section header 下方显示「即将推出」文字。按钮显示当前默认键名（`WASD` / 鼠标左键 / `Esc`）。真正的重绑定交互延后到未来 change。

#### Scenario: 玩家点重绑定按钮

- **WHEN** SettingsForm 打开中，玩家点「Move」重绑定按钮
- **THEN** 按钮 `interactable == false`，点击无响应
- **AND** 按钮文字保持为 `WASD` 或已保存的默认键名
- **AND** Console 无 Error / Warning

### Requirement: 取消回滚快照

打开 SettingsForm 时 MUST 调 `SettingsModule.BeginEdit()` 拍当前值快照。取消按钮 / 右上 X 关闭时 MUST 调 `Rollback()` 恢复快照并 `ApplyAll`，不写盘。回滚范围覆盖 v1.0 所有可变字段（BGM / SFX / QualityLevel）。

#### Scenario: 拖音量后点取消

- **WHEN** 打开面板时 BGM=1.0，拖到 0.3，点「取消」
- **THEN** `SettingsModule.Rollback()` 被调用
- **AND** `AudioModule` 音量恢复为 1.0
- **AND** SettingsForm 关闭
- **AND** SaveModule 存档不变

### Requirement: 保存提交写盘 + 发事件

保存按钮 MUST 调 `Commit()`：写盘成功后 `EventBus.Publish(new SettingsAppliedEvent(current))`。写盘失败保留当前内存值，`FrameworkLogger.Warn`，**不关闭面板**（让玩家重试）。

#### Scenario: 玩家点保存成功

- **WHEN** SettingsForm 编辑中，玩家点「保存」
- **THEN** `SettingsModule.Commit()` 被调用
- **AND** `SaveModule.SetSettings(current) + SaveAsync()` 被调用
- **AND** 写盘成功后 `SettingsAppliedEvent` 被发布
- **AND** SettingsForm 关闭

### Requirement: 入口接入主菜单与暂停菜单

`MainMenuForm` / `PauseMenuForm` 「设置」按钮 MUST 打开同一个 `SettingsForm` prefab。两处入口共用同一实例，不出现独立状态。

#### Scenario: 主菜单打开

- **WHEN** 玩家在 MainMenu 点「设置」按钮
- **THEN** `SettingsForm.Open()` 被调用
- **AND** Console 无 Error

#### Scenario: 暂停菜单打开

- **WHEN** 玩家在 InGame 按 Esc → PauseMenu 打开 → 点「设置」按钮
- **THEN** 同一 `SettingsForm` 实例被打开
- **AND** `Time.timeScale == 0` 时 SettingsForm 仍能交互（SetUpdate(true) 不受 timeScale 影响）

### Requirement: SettingsModule 生命周期

- `SettingsModule.ModuleCategory = 1`（Service 层）
- `Dependencies = [AudioModule, SaveModule]`
- `InitAsync` 内部：读 `SaveModule.Data.Settings` → `ApplyAll` → 完成
- `InitAsync` 期间 MUST NOT 发任何事件（项目约束）

#### Scenario: InitAsync 阶段

- **WHEN** GameApp 启动，ModuleRunner 初始化 SettingsModule
- **THEN** `AudioModule` 与 `SaveModule` 必须先完成 InitAsync（Dependencies 保证）
- **AND** SettingsModule 读到 `SaveModule.Data.Settings` 后立即 `ApplyAll`
- **AND** 无事件发布

### Requirement: UI 视觉契约（v2 UI 重做）

阶段 6 联调后的运行时截图 MUST 与 v2 版 `art/mockups/SettingsForm.png` 视觉一致：字号误差 ±1px、间距误差 ±2px、配色 hex 完全一致、控件相对位置一致。视觉一致由用户在阶段 6 loop 中裁定。

#### Scenario: 阶段 6 loop 视觉验收

- **WHEN** 阶段 6 loop 每轮结束
- **THEN** 主对话截取运行时截图 + 并排贴 `art/mockups/SettingsForm.png`
- **AND** 列偏差清单（间距 / 字号 / 配色）
- **AND** 3 项手测 PASS + Console `type=Error==0` + 视觉一致 → 退出 loop

### Requirement: Prefab 层级契约（v2 UI 重做）

`Assets/Resources/Prefab/UI/Settings.prefab` 的层级结构 MUST 与 `openspec/changes/10-settings-form/art/prefab-layout.md` 定义的节点树一致。所有 RectTransform 参数（anchor / pivot / sizeDelta / anchoredPosition）以 layout 为准。阶段 5 client-unity 拼装时 SHALL NOT 偏离 layout 自行推断结构。

#### Scenario: 阶段 5 拼装完成后

- **WHEN** client-unity 完成阶段 5 Prefab 建构
- **THEN** Prefab 每个节点的名字、路径、父子关系与 `prefab-layout.md` 完全一致
- **AND** 每个节点的 RectTransform 参数与 layout 表格一致
- **AND** 编译通过，无 CS 错误
