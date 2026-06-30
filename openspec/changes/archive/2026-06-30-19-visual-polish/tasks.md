# tasks — 19-visual-polish

**状态**: 待执行
**创建**: 2026-07-01
**并行策略**: 19-A / 19-B / 19-C / 19-D 四个子任务互相无依赖，主对话 Fan-Out 并行执行。
**前置条件**: codex-image-gen 出 3 status icon 完成后，19-B 才能联调贴图。19-B 代码可以先写，贴图到位后 Hook 联调。

---

## 19-A：暴击数字飘字

**Owner**: client-unity
**估算**: 5 Story Points (M)
**阻塞**: 无

- 创建 `DamageFloatTextBehaviour.cs`，订阅 `CritHitEvent`（红色大字）+ `WeaponAttackHitEvent`（白色小字，跳过 IsCrit==true）
- 对象池 8 个 TMP 飘字对象，World Space Canvas
- DOTween Sequence：普通 0.6s 上浮 + 淡出；暴击先 Scale 弹出再上浮
- 颜色：普通 #FFFFFF / 暴击 #FF2222，fontSize 24 / 36
- 完成后编译 0 error，自测：普攻和暴击均出飘字

---

## 19-B：Status 头顶图标

**Owner**: client-unity
**估算**: 5 Story Points (M)
**阻塞**: 3 status icon 图片（可先用占位色块，图片到位后替换）

- 创建 `StatusIconController.cs`，挂到 EnemyActor / PlayerActor prefab 头顶锚点
- 订阅 `StatusEffectAppliedEvent` + `StatusEffectExpiredEvent`，按 TargetId 过滤
- 3 个图标格位（burn/poison/stun）水平排列，间距 8px，有则显示无则隐藏
- DOTween：出现 FadeIn+ScalePop(0.15s)，消失 FadeOut+ScaleShrink(0.2s)
- sprite 路径：`Assets/Resources/Sprite/UI/StatusIcon/{burn,poison,stun}.png`（ResourceModule 加载）
- 编译 0 error，自测：给敌人施加 burn，头顶 burn 图标出现；到期消失

---

## 19-C：hitspark + camera shake

**Owner**: client-ta
**估算**: 5 Story Points (M)
**阻塞**: 无（粒子 Prefab 需通知用户在 Editor 建）

- 创建 `HitsparkBehaviour.cs`：订阅 `WeaponAttackHitEvent`，从对象池取 ParticleSystem，设 position=HitPoint，Play()
- 粒子参数见 design.md §19-C：Burst 8/14 粒子，Duration 0.3s，Max 80
- 创建 `CameraShakeBehaviour.cs`：订阅同事件，DOShakePosition；新 shake 到来先 Kill 再播（防叠加）
- 普通 amplitude=0.05 duration=0.12s；暴击 amplitude=0.1 duration=0.18s
- **通知用户手动建** `Assets/Resources/Prefab/VFX/Hitspark.prefab`（空 GO + ParticleSystem 组件）
- 编译 0 error，自测：攻击命中时有粒子爆发 + 轻微镜头抖动

---

## 19-D：HP < 30% vignette 闪烁

**Owner**: client-ta
**估算**: 3 Story Points (S)
**阻塞**: 场景需有 Global Volume（若无则 Awake 自动创建）

- 创建 `VignettePulseBehaviour.cs`：订阅 `PlayerHealthChangedEvent`
- ratio < 0.3 → StartPulse()：DOTween.To 修改 vignette.intensity，SetLoops(-1, Yoyo)，周期 2s
- ratio >= 0.3 → StopPulse()：Kill pulse Tween，再 Tween 淡出到 0（0.4s）
- 颜色 #CC0D0D，intensityMax=0.45
- 依赖 URP Global Volume + Vignette Override（Awake 中 TryGet，失败则 Warn 日志并跳过）
- 编译 0 error，自测：手动降 HP 到 30% 以下，边缘红色脉冲；回血后脉冲停止

---

## 汇总表

| 子任务 | Owner | Effort | Deadline | 阻塞 |
|---|---|---|---|---|
| 19-A 暴击飘字 | client-unity | 5 SP | 2026-07-03 | 无 |
| 19-B Status 图标 | client-unity | 5 SP | 2026-07-03 | codex icon（代码可先行） |
| 19-C hitspark+shake | client-ta | 5 SP | 2026-07-03 | 用户建 Prefab |
| 19-D vignette | client-ta | 3 SP | 2026-07-03 | 无 |

**编排方式**: Fan-Out（主对话同时派 4 个 Agent），WhenAll 汇合后进 QA。
