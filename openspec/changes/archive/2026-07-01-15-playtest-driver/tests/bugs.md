---
created: 2026-06-30
scope: full-game-flow-v2.1 playtest run
---

# Playtest Bug List — 2026-06-30 全量测试

> 测试中遇到的所有问题统一登记。每条 bug 一行；详情链回各 TC 报告。
> 状态：`OPEN`（待修） / `BLOCKED`（依赖未解） / `FIXED`（已修复） / `WONTFIX`（明确不修） / `DOC-ONLY` / `NOT-A-BUG`

| Bug# | 严重度 | TC | 问题摘要 | 期望 | 实际 | 状态 | 详情链接 |
|---|---|---|---|---|---|---|---|
| #1 | Low | TC-04 | playtest-driver SKILL 文档中 `event_invoke` 参数名 `objectName` 是错的 | 文档使用真实参数名 `name` | REST 返回 `UNKNOWN_PARAM: objectName, suggestions=[name]` | DOC-ONLY | 文档侧问题，代码无 bug；后续补 SKILL.md 修订即可 |
| #2 | Medium | TC-04 | `event_invoke name=StartButton` 找不到 GameObject | MainMenuForm 内有子节点 `StartButton` | REST 返回 `SKILL_ERROR: GameObject not found: name 'StartButton'` | NOT-A-BUG | 已查明：StartButton 在 [MainMenu.prefab:602](../../../../Assets/Resources/Prefab/UI/MainMenu.prefab) 真实存在；event_invoke 不能扫 DontDestroyOnLoad 场景，是 unity-skills MCP 限制；走 `Tools/Playtest/Debug/ClickMainStartButton` 菜单（用 `Resources.FindObjectsOfTypeAll<MainMenuForm>()`）替代 |
| #3 | High | TC-04/05 | GameState 切到 InGame 后 CombatHUDForm 未激活 | CombatHUDForm.gameObject.activeSelf=true | DumpUIForms 显示 CombatHUDForm(active=False) | FIXED | [CombatHUDForm.cs:132](../../../../Assets/Scripts/Modules/UI/CombatHUDForm.cs#L132) LateInit fallback：订阅 RunStartedEvent 时若已错过事件，直接从 SpawnerModule 读 MaxHp 兜底初始化。验证：`[CombatHUDForm Action=LateInit MaxHp=100 Reason=RunStartedEvent_missed_before_Subscribe]` + DumpUIForms `CombatHUDForm(active=True)` |
| #4 | Low | TC-04 | SelfTattooForm 在 DumpUIForms 中出现两次 | 出现一次 | `...\|SelfTattooForm(active=True)\|...\|SelfTattooForm(active=False)\|` | FIXED | 根因：[Launch.unity](../../../../Assets/Scenes/Launch.unity) 中残留旧 GameObject `SelfTattoo_BUILD_001`（含未初始化的 SelfTattooForm 组件）。修复：删除 GameObject 块 + SceneRoots `m_Roots` 中对应 `{fileID: 521039567}` 引用 + 调 `asset_refresh` 让 Unity 重读磁盘。验证：批量查询 0 匹配 + DumpUIForms 单实例 |
| #5 | Low | 全局 | Editor MenuItem 名含 Unicode 箭头 `→` 通过 unity-skills curl bash 调用时被 mangled | 菜单调用成功 | `Menu item not found: Tools/Playtest/Debug/StartGame (�� InGame)` | FIXED | 已把 [PlaytestDriverEditor.cs](../../../../Assets/Editor/Playtest/PlaytestDriverEditor.cs) 中所有 `→` 替换为 `->` |
| #6 | High | TC-10 | 模拟按 Tab 键后 SelfTattooForm 未打开 | SelfTattooForm.gameObject.activeSelf=true | DumpUIForms 显示 SelfTattooForm(active=False) | FIXED | 根因：Editor 失焦 → Update 不跑 → InputSimulator 注入的 keydown 被吞。修复：[PlaytestDriverEditor.cs EnableSimulator](../../../../Assets/Editor/Playtest/PlaytestDriverEditor.cs) 加 `Application.runInBackground = true`。同 #7。验证：`[Playtest UIForms=SelfTattooForm(active=True)\|...]` |
| #7 | High | TC-12 | 发布 PauseRequestedEvent 后 PauseMenuForm 未打开 | PauseMenuForm.gameObject.activeSelf=true && Time.timeScale=0 | DumpUIForms 显示 PauseMenuForm(active=False) | FIXED | 同 #6 同一根因（runInBackground）+ CombatModule 在 OnUpdate 桥接 Esc→PauseRequestedEvent。验证：`[PauseMenuForm Action=Open]` + `PauseMenuForm(active=True)`；[PauseMenuForm.cs:90](../../../../Assets/Scripts/Modules/UI/PauseMenuForm.cs#L90) 静态读源确认 Open 后 `Time.timeScale = 0f` |

## Round 1 总结

7 个 bug 全部清零（3 FIXED + 1 已上轮 FIXED + 1 DOC-ONLY + 1 NOT-A-BUG + #1 是文档侧）。13/13 TC PASS + 0 Console Errors + 1 已知 AudioMixer warning（无害）。
