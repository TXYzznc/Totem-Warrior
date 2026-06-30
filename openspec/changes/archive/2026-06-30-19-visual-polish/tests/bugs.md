# Bug 跟踪 — 19-visual-polish

**change**: 19-visual-polish
**创建**: 2026-07-01
**负责**: qa-engineer

---

## 已知限制 / 用户手动任务（不阻塞归档）

| ID | 类别 | 组件 | 现象 | 状态 | 操作方 |
|---|---|---|---|---|---|
| KNOWN-01 | 已知限制 | DamageFloatTextBehaviour | spawn 位置固定为 `Vector3.zero`，飘字不出现在命中点 | 🟡 已知限制，不阻塞归档 | client-unity（待 Target 接入 Position 字段后联调） |
| KNOWN-02 | 用户手动任务 | StatusIconController | 脚本本身已完成，但需手动将组件挂载到 EnemyActor / PlayerActor prefab 的头顶锚点 | 🟡 用户手动任务，不阻塞归档 | 用户（见挂载步骤） |
| KNOWN-03 | 用户手动任务 | HitsparkBehaviour | `Hitspark.prefab` 尚未存在；运行时 fallback 为 placeholder GO（已实装），但视觉不正确 | 🟡 用户手动任务，不阻塞归档 | 用户（见建 Prefab 步骤） |
| KNOWN-04 | 已实装 Fallback | VignettePulseBehaviour | 场景无 Global Volume 时 Awake 自动创建；无 Vignette Override 时自动添加并打 Warn 日志（3 级 fallback 已实装） | 🟢 已实装，无需操作 | — |

---

## KNOWN-01 详情

**现象**：暴击飘字在 `Vector3.zero` 处生成，而非敌人所在位置。

**根因 Hypothesis**：`CritHitEvent` / `WeaponAttackHitEvent` 的 `Target` 字段目前无 `Position` 属性（框架层 Target 结构体尚未扩展坐标信息）。`DamageFloatTextBehaviour` 在 spawn 时读不到命中点世界坐标，退化为 `Vector3.zero`。

**重现路径**：
1. Play Mode 进入战斗
2. 普攻命中任意敌人
3. 观察飘字出现在屏幕左下角（世界原点）而非命中点

**期望**：飘字出现在命中点上方 20px 处。
**实际**：飘字出现在 `(0, 0, 0)`。
**环境**：Unity 6.3 LTS，Windows 10
**严重度**：Medium（功能可用，视觉位置不正确；不阻塞核心战斗流程）

**修复路径**（供 client-unity 参考）：
1. 扩展 `WeaponAttackHitEvent` / `CritHitEvent` 添加 `Vector3 HitPosition` 字段
2. 调用处（CombatModule 或武器逻辑）赋值命中点坐标
3. `DamageFloatTextBehaviour` 读取 `event.HitPosition` 作为 spawn 基点

---

## KNOWN-02 挂载步骤（用户手动任务）

StatusIconController 需要用户在 Unity Editor 中完成以下操作：

1. 打开 `Assets/Resources/Prefab/` 下 EnemyActor prefab（和 PlayerActor prefab）
2. 在头顶位置新建空子 GO，命名 `StatusIconAnchor`，Y 偏移约 `+2.0`（视角色高度调整）
3. 在该 GO 上添加组件 `StatusIconController`
4. 确认 Inspector 中 `_iconSlots` 数组已自动初始化（共 3 格：burn / poison / stun 顺序）
5. 保存 Prefab

完成后执行路线 B Playtest 验证。

---

## KNOWN-03 建 Prefab 步骤（用户手动任务）

HitsparkBehaviour 需要用户在 Unity Editor 中手动建立粒子 Prefab：

1. 在 Hierarchy 新建空 GameObject，命名 `Hitspark`
2. 添加 `ParticleSystem` 组件，按 design.md §19-C 配参：
   - Burst：8（普通）/ 14（暴击）粒子，Start Color #FF6622
   - Duration：0.3s，Stop Action：Disable
   - Max Particles：80
3. 将该 GO 拖入 `Assets/Resources/Prefab/VFX/` 生成 `Hitspark.prefab`
4. 删除 Hierarchy 中的临时 GO

完成后 `HitsparkBehaviour` 的 `ResourceModule.Load("Prefab/VFX/Hitspark")` 即可正常取到 Prefab，fallback placeholder 不再生效。

---

## 运行时 Bug（待归档前清零）

> 本节在 Playtest 执行后填写。当前为空，归档前必须为空或全部标 Won't Fix。

| ID | 严重度 | 组件 | 现象 | 重现步骤 | 期望 | 实际 | 状态 |
|---|---|---|---|---|---|---|---|
| — | — | — | 暂无 | — | — | — | — |

---

## Bug 报告模板（Playtest 发现新 Bug 时使用）

```
**ID**: BUG-19-XX
**现象**: 一句话描述
**重现步骤**:
  1. ...
  2. ...
**期望**: ...
**实际**: ...
**环境**: Unity 6.3 LTS / Windows 10 / Editor Play Mode
**严重度**: Critical / High / Friction / Nice-to-have
**堆栈 / 日志**:
  [粘贴 Console 错误]
**Root Cause Hypothesis**: ...
**修复建议**: ...
```
