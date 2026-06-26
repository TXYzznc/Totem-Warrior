# Spec — settings

> 设置系统的验收契约。阶段 5 联调与归档前以本文为准绳。

## REQ-SETTINGS-001 — 持久化

- 设置必须保存到 `Application.persistentDataPath/settings.json`
- 启动时自动读取并应用；文件不存在时使用 design.md §2.3 的默认值
- 写盘失败不能崩溃，只 `FrameworkLogger.Warn`

## REQ-SETTINGS-002 — 音量即时响应

- BGM / SFX 滑动条拖动过程中音量必须**立即**变化（无需保存）
- 取值范围 0.0 ~ 1.0，UI 步长 0.01
- 内部走 AudioMixer `LinearToDb(v) = Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20`

## REQ-SETTINGS-003 — 画质即时响应

- 三档单选：Low(0) / Med(1) / High(2)
- 切换即时调 `QualitySettings.SetQualityLevel(level, true)` + 切 RP Asset
- 当前选中档位高亮显示

## REQ-SETTINGS-004 — 按键重绑定

- 三个绑定槽：Move / Attack / Pause
- 点击「重绑定」按钮 → 按钮文字变「按任意键...」
- 必须**走 InputModule 提供的 API**，禁止直接调 `InputSystem.actions[].PerformInteractiveRebinding()`
- 按 Esc 取消重绑定，按钮恢复显示原绑定

## REQ-SETTINGS-005 — 重绑定冲突拒绝

- 玩家把目标键绑到已被其他 Action 占用的键时，**必须**：
  - 弹出 Toast「该按键已被『〈冲突 Action 名〉』占用」
  - 原绑定保持不变
  - 不更新 draft，不调 Preview

## REQ-SETTINGS-006 — 取消回滚

- 打开 SettingsForm 时调 `SettingsModule.BeginEdit()` 拍快照
- 取消按钮 / 右上 X / Esc 关闭面板时调 `Rollback()`
- 回滚必须恢复**所有**可变状态：音量 / 画质 / 重绑定
- 回滚完成后不写盘

## REQ-SETTINGS-007 — 保存提交

- 保存按钮调 `Commit()`
- 写盘成功后 `EventBus.Publish(new SettingsAppliedEvent(current))`
- 写盘失败：保留当前内存值，发 Warn 日志，**不**关闭面板（让玩家重试）

## REQ-SETTINGS-008 — 入口

- 主菜单 `MainMenuForm` 「设置」按钮 → `UIModule.OpenAsync<SettingsForm>()`
- 暂停菜单 `PauseMenuForm` 「设置」按钮 → 同上
- 两处入口共用同一 prefab

## REQ-SETTINGS-009 — 模块生命周期

- `SettingsModule.Category = ModuleCategory.Service`
- `Dependencies = [AudioModule, InputModule]`
- `InitAsync`：读盘 → ApplyAll，**不**发任何事件（项目约束）

## REQ-SETTINGS-010 — UI 视觉契约

- 阶段 5 运行时截图必须与 `art/mockups/SettingsForm.png` 视觉一致：
  - 字号 ±1px
  - 间距 ±2px
  - 配色 hex 完全一致
  - 控件相对位置一致

## 测试映射

| REQ | 测试 |
|---|---|
| REQ-SETTINGS-005 | EditMode `SettingsModule_RebindConflict_RejectsAndKeepsOriginal` |
| REQ-SETTINGS-002 / 003 / 006 | PlayMode 手测（tests/results.md） |
| REQ-SETTINGS-001 | PlayMode 手测：保存 → 重启 → 验证设置保留 |
| REQ-SETTINGS-008 | PlayMode 手测：主菜单/暂停菜单两路径打开 |
| REQ-SETTINGS-010 | 阶段 5 截图对比 |
