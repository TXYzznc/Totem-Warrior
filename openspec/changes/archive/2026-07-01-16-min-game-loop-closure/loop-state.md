---
created: 2026-06-30
purpose: loop 轮次跟踪 + 同 bug 连续未解次数（终止安全网）
---

# Loop 状态

## 进度

| Round | 开始 | 结束 | 修复 agent 数 | 13 TC PASS | Console Errors | 状态 |
|---|---|---|---|---|---|---|
| 0 (baseline) | — | 2026-06-30 12:xx | 0 | 9/13 | 0 | 基线，开始 loop |
| 1 | 2026-06-30 13:00 | 2026-06-30 14:30 | 3 (按根因分组) | 13/13 | 0 | **PASS — 退出成功** |

## Bug 连续未解次数（终止安全网）

> 同一 bug# 连续 5 轮 status=OPEN 且无 status 变化 → loop 终止；用户介入。

| Bug# | 首次出现 Round | 当前 status | 连续未解次数 | 备注 |
|---|---|---|---|---|
| #1 | 0 | DOC-ONLY | — | 文档 typo（菜单名 "Esc" 实际是 "Escape"），代码无问题 |
| #2 | 0 | NOT-A-BUG | — | event_invoke 不能访问 DDOL 是 MCP 限制；StartButton 在 prefab 中真实存在；走 Debug/ClickMainStartButton 菜单替代 |
| #3 | 0 | FIXED | — | CombatHUDForm.cs:132 LateInit fallback 兜底 RunStartedEvent miss |
| #4 | 0 | FIXED | — | 删除 Launch.unity 中 SelfTattoo_BUILD_001 残留 GameObject + SceneRoots 引用 |
| #5 | 0 | FIXED | — | 上一轮修复 |
| #6 | 0 | FIXED | — | Application.runInBackground = true（EnableSimulator 中设置） |
| #7 | 0 | FIXED | — | 同 #6（同一根因） |

## 退出判定

每轮结束自检：
1. 13 TC 全 PASS + Console errors == 0 → **退出成功** ✅
2. 任何一个 bug 连续未解次数 == 5 → 退出失败，交回用户
3. 否则 → 继续下一轮

**最终判定：Round 1 退出条件 1 达成。loop 终止。**

## Round 1 修复摘要

按根因分组 3 个修复批次：

1. **Editor 焦点 freeze（Bug #6, #7）**
   - `Assets/Editor/Playtest/PlaytestDriverEditor.cs` EnableSimulator 中加 `Application.runInBackground = true`
   - 影响：Editor 失焦时 GameTickDriver 不再卡住

2. **CombatHUDForm RunStarted miss（Bug #3）**
   - `Assets/Scripts/Modules/UI/CombatHUDForm.cs:132` LateInit fallback：订阅时若已错过 RunStartedEvent，直接从 SpawnerModule 读 MaxHp 兜底初始化

3. **SelfTattooForm 双实例（Bug #4）**
   - `Assets/Scenes/Launch.unity` 删除残留 GameObject `SelfTattoo_BUILD_001`（lines 125-195）+ SceneRoots `m_Roots` 中对应 fileID 引用
   - 现场识别：Editor 内存场景与磁盘文件不同步 → 调 `asset_refresh` skill 强刷 → 重启 Play 后 batch_query 0 匹配 → dump 单实例

## Changelog
- 2026-06-30 13:00 Round 1 启动；按根因将 7 个 bug 拆为 3 个修复组
- 2026-06-30 14:30 Round 1 完成；13/13 TC PASS + 0 errors；loop 退出成功
