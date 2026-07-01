---
updated: 2026-06-30
---

# Playtest Loop 状态

## 当前状态

| 字段 | 值 |
|---|---|
| Round | 1 |
| TC-Art | 5/5 PASS |
| #16 回归 | 13/13 PASS |
| 业务 Console Errors | 0 |
| 总状态 | **PASSED — 退出 loop** |

## 关键发现

1. `ReimportThenGenerateAll` 不能在 Play Mode 前立即执行——会导致 `Animator.runtimeAnimatorController=null`。Pre-flight 应改为：仅在资源真正需要重建时才跑，否则直接进 Play。

2. 正确的 DumpUIForms 菜单路径是 `Tools/Playtest/Debug/Dump UIForms (active+inactive)`，非 `Tools/Playtest/Debug/DumpUIForms`。

3. TC-Art-03（Attack）需要 Pause+Step 单帧验证，否则 0.5s Attack clip 在 0.1s 后已转回 Idle。

## Loop 历史

| Round | TC-Art | #16 | Errors | 状态 |
|---|---|---|---|---|
| 1 (Run 1) | 0/5 | — | 0 | FAIL (ReimportThenGenerateAll 破坏 Animator) |
| 1 (Run 2) | 5/5 | 13/13 | 0 | PASSED |
