# design — 19-visual-polish

**状态**: 草稿
**创建**: 2026-07-01
**决策来源**: ROADMAP §0（已冻结）

---

## 总体架构

4 项 polish 均为**纯表现层**，不修改任何 IGameModule 的状态机。全部通过订阅 change 20 事件总表中已有的事件触发，无需新增事件或公共 API。

```
EventBus (change 20 既有)
  ├── CritHitEvent           →  DamageFloatTextBehaviour (19-A)
  ├── WeaponAttackHitEvent   →  DamageFloatTextBehaviour (19-A)
  │                          →  HitsparkBehaviour (19-C)
  │                          →  CameraShakeBehaviour (19-C)
  ├── StatusEffectApplied/   →  StatusIconBehaviour (19-B)
  │   StatusEffectExpired
  └── PlayerHealthChanged    →  VignettePulseBehaviour (19-D)
```

---

## 19-A：暴击数字飘字 + 颜色区分

### 事件订阅链

```
CombatModule → HeadPartBehavior → CritHitEvent (已有)
CombatModule → WeaponAttackHitEvent (已有，普通命中)

DamageFloatTextBehaviour
  [EventHandler] OnCritHit(CritHitEvent)   → 红色大字
  [EventHandler] OnAttackHit(WeaponAttackHitEvent，排除 IsCrit 场景) → 白色小字
```

`CritHitEvent` 已包含 `Position`, `Damage` 字段（来自 CONTRACT §1.2）。
`WeaponAttackHitEvent` 已包含 `HitPoint`, `Damage`, `IsCrit` 字段。
实现时对 `IsCrit == true` 的 AttackHit 事件跳过，避免双重飘字。

### 实现位置

`Assets/Scripts/Modules/UI/Behaviors/DamageFloatTextBehaviour.cs`

### 对象池策略

- 预分配 8 个 TextMesh/TMP 飘字对象（最大同屏 8 个，超出复用最旧的）
- 对象位于 Canvas（World Space）或世界坐标 UI 层

### DOTween 动画曲线

```
普通命中（白色，fontSize 24）：
  Sequence:
    DOAnchorPosY(+80, 0.6f).SetEase(Ease.OutCubic)   // 上浮
    DOFade(0, 0.3f).SetDelay(0.3f)                    // 后 0.3s 淡出

暴击（红色，fontSize 36，先放大再浮起）：
  Sequence:
    DOScale(1.4f, 0.1f).SetEase(Ease.OutBack)         // 弹出放大
    DOScale(1.0f, 0.05f)                               // 缩回正常
    DOAnchorPosY(+120, 0.8f).SetEase(Ease.OutCubic)   // 上浮更高
    DOFade(0, 0.35f).SetDelay(0.45f)                   // 淡出
```

### 颜色规范

| 类型 | 颜色值 | fontSize | 字体 |
|---|---|---|---|
| 普通命中 | `#FFFFFF`（白色） | 24 | TMP 默认 |
| 暴击 | `#FF2222`（红色） | 36 | TMP 默认，Bold |
| 爆头暴击（可选增强） | `#FFD700`（金色） | 40 | TMP 默认，Bold |

### 性能预算

- 无 GC alloc：对象池 + DOTween 复用
- 最坏场景（49 敌人 AoE 同帧命中）：8 个飘字上限保证帧时间增量 < 0.1ms

---

## 19-B：Status 状态图标（burn/poison/stun）

### 事件订阅链

```
StatusEffectModule → StatusEffectAppliedEvent { TargetId, EffectType, Duration }
StatusEffectModule → StatusEffectExpiredEvent  { TargetId, EffectType }

StatusIconController（挂在 EnemyActor / PlayerActor 头顶的 MonoBehaviour）
  EventBus.Subscribe<StatusEffectAppliedEvent>(OnApplied)
  EventBus.Subscribe<StatusEffectExpiredEvent>(OnExpired)
  过滤条件：event.TargetId == this.ActorId
```

### 图标布局

```
 [Actor 头顶]
 ┌──────┐ ┌──────┐ ┌──────┐
 │ burn │ │poison│ │ stun │   ← 水平排列，间距 8px，居中对齐
 └──────┘ └──────┘ └──────┘
    64x64 sprite，世界空间 Billboard
```

- 最多同时显示 3 个图标（burn/poison/stun 各占一格，有则显示无则隐藏）
- 图标 UI 层级：`Canvas (World Space)` 或直接用 SpriteRenderer + Billboard shader

### DOTween 动画曲线

```
图标出现（OnApplied）：
  DOFade(0 → 1, 0.15f).SetEase(Ease.OutQuad)
  DOScale(1.3f → 1.0f, 0.2f).SetEase(Ease.OutBack)

图标消失（OnExpired）：
  DOFade(1 → 0, 0.2f).SetEase(Ease.InQuad)
  DOScale(1.0f → 0.8f, 0.2f)
  OnComplete: gameObject.SetActive(false)
```

### 美术依赖

3 张 64x64 PNG sprite：
- `Assets/Resources/Sprite/UI/StatusIcon/burn.png`
- `Assets/Resources/Sprite/UI/StatusIcon/poison.png`
- `Assets/Resources/Sprite/UI/StatusIcon/stun.png`

（由 codex-image-gen 出图，见 art/requirements.md）

### 性能预算

- SpriteAtlas 合批（所有 StatusIcon 打进同一 Atlas）
- 49 敌人 × 3 status = 147 个 sprite，分 3 个 Draw Call（1 Atlas 1 Pass）
- 额外帧时间增量 < 0.2ms（主要是 SpriteRenderer.Update）

---

## 19-C：命中 hitspark 粒子 + camera shake

### 事件订阅链

```
CombatModule → WeaponAttackHitEvent { HitPoint, Damage, IsCrit }

HitsparkBehaviour（MonoBehaviour，挂在 CombatManager 或 VFX 全局对象）
  [EventHandler] OnAttackHit(WeaponAttackHitEvent)
  →  从粒子池取一个 ParticleSystem，设置 position = HitPoint，Play()

CameraShakeBehaviour（MonoBehaviour，挂在 Main Camera）
  [EventHandler] OnAttackHit(WeaponAttackHitEvent)
  →  执行镜头抖动 Tween
```

### 粒子参数规范

```
ParticleSystem 配置：
  Duration: 0.3s（OneShot，非循环）
  Start Lifetime: 0.2 ~ 0.3s（随机）
  Start Speed: 3 ~ 6
  Start Size: 0.05 ~ 0.12
  Start Color: 白色 → 暴击时改为橙红 #FF6622
  Emission: Burst，Count = 8（普通）/ 14（暴击）
  Shape: Sphere，Radius 0.05
  Renderer: Billboard，Material = 白色粒子 Unlit/Sprite
  Max Particles: 80（全局上限）
```

### Camera Shake 参数

```
普通命中：
  amplitude = 0.05 (world units)
  duration   = 0.12s
  frequency  = 25Hz（通过 DOShakePosition 实现）

暴击：
  amplitude = 0.1
  duration   = 0.18s
  frequency  = 20Hz

实现方式：
  DOTween.DOShakePosition(Camera.main.transform, duration, amplitude, vibrato: (int)(frequency*duration))
  新 shake 到来时：先 DOTween.Kill(Camera.main.transform)，再重新播放（不叠加）
```

### 实现位置

- `Assets/Scripts/Modules/Combat/VFX/HitsparkBehaviour.cs`
- `Assets/Scripts/Modules/Combat/VFX/CameraShakeBehaviour.cs`
- 粒子 Prefab: `Assets/Resources/Prefab/VFX/Hitspark.prefab`

### 性能预算

- 粒子对象池大小：4 个 ParticleSystem 实例（4 x 80 粒子 = 320 粒子上限）
- 帧时间增量：< 0.15ms（粒子 GPU 侧，CPU 侧仅 Play/Stop 调用）

---

## 19-D：HP < 30% 边缘血色 vignette 闪烁

### 事件订阅链

```
CombatModule / HealthModule → PlayerHealthChangedEvent { Current, Max }

VignettePulseBehaviour（MonoBehaviour，挂在全局 PP 管理对象）
  [EventHandler] OnPlayerHealthChanged(PlayerHealthChangedEvent)
  →  ratio = Current / Max
  →  if ratio < 0.3 && !pulsing → StartPulse()
  →  if ratio >= 0.3 && pulsing → StopPulse()
```

### Shader / Post-Processing 方案

采用 URP **Global Volume + Vignette Override** 方式：
- 不新建 Volume，复用场景中已有 Global Volume
- 运行时通过代码获取 `volume.profile.TryGet<Vignette>()` 拿到 Vignette 组件
- 修改 `vignette.intensity.value` 实现脉冲

```csharp
// 脉冲参数
intensityMin = 0.0f   // 无效果
intensityMax = 0.45f  // 最大红色边缘
pulsePeriod  = 2.0f   // 2 秒一个完整周期
colorOverride = new Color(0.8f, 0.05f, 0.05f)  // 深红
```

### DOTween 动画曲线

```
StartPulse()：
  DOTween.To(() => vignette.intensity.value,
             x => vignette.intensity.value = x,
             intensityMax, pulsePeriod * 0.5f)
    .SetEase(Ease.InOutSine)
    .SetLoops(-1, LoopType.Yoyo)
    .SetId("vignette_pulse")

StopPulse()：
  DOTween.Kill("vignette_pulse")
  DOTween.To(..., 0f, 0.4f).SetEase(Ease.OutQuad)  // 淡出到 0
```

### URP 版本约束

- 要求 URP 14+（Unity 6 自带）
- `UniversalRenderPipelineAsset` 须开启 Post Processing
- 若场景无 Global Volume，VignettePulseBehaviour 在 Awake() 中自动创建并注册

### 实现位置

`Assets/Scripts/Modules/Combat/VFX/VignettePulseBehaviour.cs`

### 性能预算

- Vignette 是 URP 内置 Pass，额外 GPU 开销 < 0.05ms
- CPU 侧仅 DOTween 更新 1 个 float，忽略不计

---

## 性能总预算汇总

| 项 | 最坏场景 | 额外帧时间增量 |
|---|---|---|
| 19-A 飘字 | 8 飘字同屏 | < 0.10ms |
| 19-B Status 图标 | 49 敌 × 3 status | < 0.20ms |
| 19-C hitspark + shake | 4 粒子系统同帧播 | < 0.15ms |
| 19-D vignette | 常驻 PP Pass | < 0.05ms |
| **合计** | 全部同时触发 | **< 0.50ms** |

目标机器：PC 60fps（帧预算 16.67ms），0.5ms 增量占比 3%，在可接受范围内。

---

## 模块依赖图

```
（无新 IGameModule）

DamageFloatTextBehaviour    → 订阅 EventBus（EventBus 由 ModuleRunner 管理）
StatusIconController        → 订阅 EventBus；读 ResourceModule 加载 sprite
HitsparkBehaviour           → 订阅 EventBus；管理 ParticleSystem 对象池
CameraShakeBehaviour        → 订阅 EventBus；控制 Main Camera DOTween
VignettePulseBehaviour      → 订阅 EventBus；控制 URP Global Volume
```

所有 Behaviour 均在 InitAsync 完成后（ModuleRunner 就绪信号）才开始订阅，避免 §十二 陷阱。

---

## 文件产出清单

| 文件 | Owner |
|---|---|
| `Assets/Scripts/Modules/UI/Behaviors/DamageFloatTextBehaviour.cs` | client-unity |
| `Assets/Scripts/Modules/Combat/VFX/StatusIconController.cs` | client-unity |
| `Assets/Scripts/Modules/Combat/VFX/HitsparkBehaviour.cs` | client-ta |
| `Assets/Scripts/Modules/Combat/VFX/CameraShakeBehaviour.cs` | client-ta |
| `Assets/Scripts/Modules/Combat/VFX/VignettePulseBehaviour.cs` | client-ta |
| `Assets/Resources/Prefab/VFX/Hitspark.prefab` | client-ta（通知用户在 Editor 建） |
| `Assets/Resources/Sprite/UI/StatusIcon/{burn,poison,stun}.png` | codex-image-gen |
