---
created: 2026-06-30
---

# Bug 报告

## BUG-17-01：ReimportThenGenerateAll 后立即 Play 导致 Animator.runtimeAnimatorController=null

**严重度**：Medium（阻塞特定 Pre-flight 流程，非玩家可见 crash）

**现象**：在 Editor 中执行 `Tools/Character/Reimport Then Generate All` 后立即进入 Play Mode，Player GameObject 上的 Animator 组件 `runtimeAnimatorController` 为 null，PlayerAnimatorBridge 无法设置参数，DumpAnimatorState 返回 `ClipName="<none>"`。

**重现步骤**：
1. 在 Editor（非 Play Mode）执行 `Tools/Character/Reimport Then Generate All`
2. 立即（同一 Editor 帧或短时间内）进入 Play Mode
3. DumpAnimatorState Player → `ClipName="<none>"`

**期望**：Animator 正常加载 Controller，`ClipName="Idle_Down"` 或对应默认状态。

**实际**：`ClipName="<none>"` + `Animator is not playing an AnimatorController` 警告反复出现。

**环境**：Unity 2022.3.62f1 Windows 10

**根因 Hypothesis**：`AnimatorController.CreateAnimatorControllerAtPath` 在 `DeleteAsset` 后创建新 asset，Unity AssetDatabase 在同帧内完成引用解析，但在该帧内调用 `PrefabUtility.SaveAsPrefabAsset` 后，随即进入 Play Mode 时序过快导致 runtime 引用丢失（可能是 Unity 2022 asset pipeline 的刷新时序问题）。

**修复建议**：
- 在 `ReimportThenGenerateAll` 末尾加 `AssetDatabase.Refresh()` 并等待下一个编辑器刷新周期
- 或在 Pre-flight 文档中注明：执行完后需等 1-2 秒再进 Play Mode
- 或 Pre-flight 不跑 Reimport，仅在确认资源变更时才手动运行

**绕过方法**：不跑 `ReimportThenGenerateAll`，直接进 Play Mode，Prefab 已有正确 Controller 引用，运行正常。
