# 测试计划 — 19-visual-polish

**change**: 19-visual-polish
**创建**: 2026-07-01
**负责**: qa-engineer
**对应 spec**: `specs/visual-polish/spec.md`（TC-Polish-01~04）

---

## 测试金字塔分布

| 层级 | 覆盖目标 | 方式 |
|---|---|---|
| 单元 | 对象池边界、颜色/字号参数、Tween 逻辑分支 | EditMode 脚本断言（手动检查编译） |
| 集成 | 事件订阅链、TargetId 过滤、Fallback 路径 | PlayMode 手动 + playtest-driver |
| E2E | 视觉合规 + 帧率预算 | Playtest（目视 + Profiler 截图） |

---

## 手动 Playtest 路线（4 条主线）

### 路线 A — 暴击飘字（对应 TC-Polish-01）

**前置条件**
- Play 已启动，战斗 Run 进行中，玩家持武器
- `DamageFloatTextBehaviour` 已挂到场景持久 GO

| 步骤 | 操作 | 预期日志关键字 | 期望视觉 | 通过标准 |
|---|---|---|---|---|
| A-1 | 鼠标左键普攻命中敌人（`Tools/Playtest/Press/MouseLeft`） | `WeaponAttackHitEvent` + `FloatText` + `Type=Normal` | 白字 #FFFFFF，字号 24，0.6s 上浮后消失 | 视觉确认 + 无 Error |
| A-2 | 触发暴击命中 | `CritHitEvent` + `FloatText` + `Type=Crit` | 红字 #FF2222，字号 36，Scale 弹出后上浮 | 视觉确认 + 无 Error |
| A-3 | 快速连续普攻 ≥10 次（菜单连按 MouseLeft 10 次） | 无 NullRef | 同时最多 8 个飘字可见 | Console Error = 0 |
| A-4 | 记录 49 敌人 AoE 帧（Unity Profiler Snapshot） | — | CPU 帧时间增量 < 0.5ms | Profiler 截图存档 |

**playtest-driver 指令**（STEP 4 注入序列）：
```
Tools/Playtest/Press/MouseLeft (Attack)  × 12  （每次间隔 0.1s）
Tools/Playtest/Press/MouseLeft (Attack)  × 1   （触发暴击，如有暴击率）
```

---

### 路线 B — Status 头顶图标（对应 TC-Polish-02）

**前置条件**
- 3 张 sprite 已在 `Assets/Resources/Sprite/UI/StatusIcon/`
- `StatusIconController` 已挂到 EnemyActor prefab 头顶锚点

| 步骤 | 操作 | 预期日志关键字 | 期望视觉 | 通过标准 |
|---|---|---|---|---|
| B-1 | 对一个敌人施加 burn | `StatusEffectAppliedEvent` + `Effect=burn` + `TargetId=<id>` | burn 图标 FadeIn+ScalePop 显示 | 0.15s 内完整显示 |
| B-2 | 等待 burn 到期 | `StatusEffectExpiredEvent` + `Effect=burn` | FadeOut+ScaleShrink 后消失 | SetActive(false)，无残影 |
| B-3 | 同时施加 burn + poison | 两个 Applied 事件 | 两图标水平排列，间距 8px | 不重叠 |
| B-4 | 同时施加 burn + poison + stun | 三个 Applied 事件 | 三图标水平排列 | 正确显示 3 个 |
| B-5 | 施加 burn 到玩家自身 | `TargetId` 命中玩家 ID | 玩家头顶显示 burn 图标 | 过滤逻辑正确 |

---

### 路线 C — Hitspark + Camera Shake（对应 TC-Polish-03）

**前置条件**
- `HitsparkBehaviour` + `CameraShakeBehaviour` 已挂到场景
- `Assets/Resources/Prefab/VFX/Hitspark.prefab` 已手动建好（见 bugs.md KNOWN-03）

| 步骤 | 操作 | 预期日志关键字 | 期望视觉 | 通过标准 |
|---|---|---|---|---|
| C-1 | 普攻命中 | `WeaponAttackHitEvent` + `Hitspark=Play` | 8 粒子白色爆发，镜头振幅 0.05/0.12s | 0.3s 内粒子消失，镜头归位 |
| C-2 | 暴击命中 | `CritHitEvent` + `Hitspark=Play` | 14 粒子橙红 #FF6622，振幅 0.1/0.18s | 颜色偏橙红，无叠加 |
| C-3 | 0.05s 间隔连击 ×10 | — | 每次新 shake 重置，镜头不飞出 | 无位置漂移 |
| C-4 | 记录粒子爆发帧（Profiler） | — | GPU 增量 < 0.15ms | Profiler 截图存档 |

**playtest-driver 指令**（精准时序，用 Edit/Step 单帧推进）：
```
Edit/Pause
Tools/Playtest/Press/MouseLeft (Attack)  × 4  （同帧）
Edit/Step
console_get_logs filter=Hitspark limit=20
```

---

### 路线 D — Vignette 脉冲（对应 TC-Polish-04）

**前置条件**
- `VignettePulseBehaviour` 已挂到场景
- URP Global Volume 存在（或 Awake 自动创建）

| 步骤 | 操作 | 预期日志关键字 | 期望视觉 | 通过标准 |
|---|---|---|---|---|
| D-1 | 手动降 HP 到 29% | `PlayerHealthChangedEvent` + `Ratio=0.29` + `StartPulse` | 红色 vignette 边缘脉冲，周期 2s | intensity 在 0~0.45 循环 |
| D-2 | 回血到 31% | `Ratio=0.31` + `StopPulse` | vignette 0.4s 淡出至 0 | 屏幕恢复，无残留红边 |
| D-3 | HP 在 28%→32%→28% 反复切换 ×3 | 无 Tween 泄漏日志 | 脉冲状态正确切换 | Console Error = 0 |
| D-4 | 新建空场景（无 Global Volume）进入 Play | `FrameworkLogger.Warn` + `VignettePulse` + `AutoCreated` | Warn 日志一条，不 NullRef | Console Error = 0 |
| D-5 | 记录脉冲进行中帧（Profiler） | — | Vignette Pass GPU 增量 < 0.05ms | Profiler 截图存档 |

---

## Profiler 截图存档清单

| TC | 指标 | 阈值 | 文件名规范 |
|---|---|---|---|
| TC-01-5 | CPU 帧时间增量（飘字 AoE 帧） | < 0.5ms | `profiler-TC01-aoe-cpu.png` |
| TC-03-5 | GPU 增量（粒子爆发帧） | < 0.15ms | `profiler-TC03-hitspark-gpu.png` |
| TC-04-5 | GPU 增量（Vignette Pass） | < 0.05ms | `profiler-TC04-vignette-gpu.png` |

截图存放目录：`openspec/changes/19-visual-polish/tests/`

---

## 综合归档门槛（来自 spec）

- [ ] TC-Polish-01 ~ 04 全部通过（零失败项）
- [ ] 新增代码编译 0 error 0 warning
- [ ] Profiler 截图 3 张存档
- [ ] status icon 3 张图片存在于 `Assets/Resources/Sprite/UI/StatusIcon/`
- [ ] `Assets/Resources/Prefab/VFX/Hitspark.prefab` 已建好
- [ ] 无新增 IGameModule

---

## playtest-driver 自动化建议

以下场景适合接入 playtest-driver 自动化跑：

1. **A-3（对象池边界）**：自动注入 12 次 MouseLeft，读 console 断言无 NullRef + 飘字数量 ≤ 8，适合每次迭代回归。
2. **C-3（shake 不叠加）**：Edit/Step 精准控帧，10 连击后断言无位置漂移 Error，可作为 smoke test。
3. **D-3（Tween 边界）**：事件注入模拟 HP 反复穿越 30% 阈值，断言无 Tween 泄漏，覆盖率高且纯逻辑。

帧率类（TC-01-5 / 03-5 / 04-5）依赖 Unity Profiler 截图，**不纳入自动化**，维持人工验收。
