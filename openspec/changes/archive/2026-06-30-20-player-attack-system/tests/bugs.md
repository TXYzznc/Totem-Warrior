# change #20 — Bug 报告

| 字段 | 值 |
|---|---|
| 报告人 | qa-engineer |
| 日期 | 2026-07-01 |
| 来源 | 代码审查（静态分析 + 逻辑追踪） |
| 修复状态 | BUG-20-01 ✅ Fixed (主对话直修) / BUG-20-02 ✅ Fixed (agent ae4502a7daa7cf041) |

---

## BUG-20-01: StatusEffectModule.OnUpdate 过期帧不发 TickedEvent 但已扣 RemainingSec  ✅ Fixed (2026-07-01)

**修复**：`Assets/Scripts/Modules/Status/StatusEffectModule.cs` L249-257，expired 分支内先发 `StatusEffectTickedEvent(lastTickDmg)` 再 `_expiredIndexBuf.Add(i)`。


### 现象
`OnUpdate` 第 N 次 tick 时 `RemainingSec -= TickInterval` 后 `<= 0`，该帧**直接进入 expired 分支**，不发 `StatusEffectTickedEvent`。
这意味着最后半秒的 DoT 伤害**静默丢失**。

### 重现步骤
1. `ApplyStatus(target, "Burn", dps=10, duration=0.5f)`（正好 1 tick 时长）
2. `OnUpdate(0.5f)`
3. 观察：`StatusEffectTickedEvent` 发布次数 = 0，`StatusEffectExpiredEvent` 发布 1 次
4. 预期：应先发 `TickedEvent(damage=5)`，再发 `ExpiredEvent`（本 tick 伤害应结算）

### 实际
`RemainingSec = 0.5f - 0.5f = 0f`，进入 `_expiredIndexBuf.Add(i)` 分支，**跳过** Tick 发布。
最后半秒的 5 点伤害不触达订阅方（PlayerDamageReceiver 或 SkillHitResolver）。

### 期望
`RemainingSec <= 0` 时应先结算本 tick 伤害（`tickDmg = dps × TickInterval`，发 TickedEvent），再标记 expired。

### 环境
- 文件：`Assets/Scripts/Modules/Status/StatusEffectModule.cs`，行 ~248-265
- Unity 6.3 LTS，EditMode 可复现（纯逻辑）

### 严重度
P2 — 数值偏差（最后半秒 DoT 静默丢失），非崩溃但影响战斗平衡可感知。

### 堆栈定位
```
StatusEffectModule.OnUpdate(float dt)
  → _accum >= TickInterval → 进入 tick 循环
  → s.RemainingSec -= TickInterval → 0f
  → 条件 s.RemainingSec <= 0f → _expiredIndexBuf.Add(i) (行 ~251)
  → 跳过 tickDmg 计算 / _bus.Publish(StatusEffectTickedEvent) (行 ~257-259)
```

### Root Cause Hypothesis
`if (s.RemainingSec <= 0f)` 与 `else { tickDmg ... }` 是互斥分支。
精确等于 0 的情况（duration 是 TickInterval 整数倍）触发 expired，丢失当帧 DoT。

### 修复建议（交 client-unity）
在 expired 分支内、`_expiredIndexBuf.Add(i)` 之前，先发一次最后的 tick 伤害：
```csharp
if (s.RemainingSec <= 0f)
{
    // 最后一 tick 伤害（duration 正好整除时不丢失）
    float lastTickDmg = s.DPS * TickInterval;
    if (lastTickDmg > 0f)
        _bus.Publish(new StatusEffectTickedEvent(target, s.Name, lastTickDmg));
    _expiredIndexBuf.Add(i);
}
```

---

## BUG-20-02: OnEffectApplied 目标路由 TODO 未实装（功能缺口，非 crash）  ✅ Fixed (2026-07-01)

**修复**：`Assets/Scripts/Events/TattooEvents.cs` L81-88 加 `Target` 字段；`Assets/Scripts/Modules/Tattoo/TattooModule.cs` L520 调 `new EffectAppliedEvent(ctx.PrimaryTarget, ctx.Log)`；`Assets/Scripts/Modules/Status/StatusEffectModule.cs` L302/316 加 null 检查并改用 `e.Target`。


### 现象
`StatusEffectModule.OnEffectApplied` 里解析了 `r.Status` 字符串成功后，注释 `// TODO change#20: EffectResult 无 Target 字段` 意味着**状态效果通过 EffectAppliedEvent 路径永远不会实际调用 ApplyStatus**。

### 影响范围
凡是通过 `TattooModule.Fire → EffectAppliedEvent` 路径携带 `Status` 字段的效果（如技能触发 Burn），**不会进入 StatusEffectModule 的 tick 循环**，也不会发 `StatusEffectAppliedEvent`。

只有调用方直接调 `StatusEffectModule.ApplyStatus(target, ...)` 的路径（如 SkillHitResolver 的 Status EffectType 分支）才生效。

### 严重度
P1 — 技能 → 状态效果的核心路径断裂；DoT 在 TattooModule.Fire 路径下失效。

### Root Cause Hypothesis
`EffectResult` 数据结构设计时未预留 `Target` 字段，OnEffectApplied 无法知道"谁被命中"。
需要 client-unity 在 `EffectResult` 中增加 `Target` 字段，并在 OnEffectApplied 中补全路由。

### 修复建议
转 client-unity：在 `EffectResult` 加 `public Target HitTarget;`，`OnEffectApplied` 中用 `r.HitTarget` 调用 `ApplyStatus`。

---

## 未发现的潜在风险（标记观察）

| 项目 | 说明 |
|---|---|
| R3：GetAimTarget score 公式 100× 系数 | 理论上当 dist < 1m 时 dist 权重可能被 dot 项压制，导致选择更远但更正对的目标。需 playtest 主观验证。非 bug，标为 design 待观察。 |
| R4：49 敌人 × 5 种状态 tick 性能 | OnUpdate 全量遍历未做空间剔除。49×5=245 次 tick/0.5s，编辑器下可接受，移动端压测待验。 |
| PlayerDamageReceiver 死亡防抖依赖 Time.realtimeSinceStartup | EditMode 测试中 `Time.realtimeSinceStartup` 不递增（始终为 0 附近），导致 `_dying` 状态永不复位。TC-Damage-D7 的第二次连击测试可能因此误判。**已在测试中通过 `_diedFired=true` 路径绕开，但生产代码的 300ms 复位逻辑无法在 EditMode 被充分验证。** |
