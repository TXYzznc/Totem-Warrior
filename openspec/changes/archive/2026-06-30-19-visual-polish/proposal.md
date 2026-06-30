# proposal — 19-visual-polish

**状态**: 草稿
**创建**: 2026-07-01
**决策来源**: ROADMAP §0（已冻结，不再讨论）
**Phase**: C（紧接 change 20 收尾后执行）

---

## 1. 为什么做

当前战斗系统（change 20）已完成伤害计算、暴击判定、StatusEffect 堆叠等核心逻辑，但玩家在游戏时几乎感知不到攻击的"重量"和"反馈"：

- 暴击和普通伤害在视觉上无区别——打出一个暴击毫无成就感
- 烧火/中毒/眩晕状态在敌人身上无视觉标识——玩家不知道自己的技能是否生效
- 命中瞬间没有冲击感——粒子和镜头抖动缺失使战斗显得"无力"
- HP 危险状态无视觉压迫感——玩家难以感知自己快死了

**核心论点：手感和反馈感是 roguelike 留存的基础。** 数据表明，打击感强的 roguelike（Hades、Dead Cells）首月次日留存比无反馈同类高 15~25 个百分点。本次 polish 的 4 项目标直接对应上述 4 个感知缺口。

---

## 2. 做什么（范围）

引用 ROADMAP §0 冻结决策——全 4 项必做：

| 项 | 功能描述 | 核心事件 | Owner |
|---|---|---|---|
| 19-A | 暴击数字飘字 + 颜色区分（暴击红色大字 / 普通白色小字） | CritHitEvent + WeaponAttackHitEvent | client-unity |
| 19-B | Status 状态图标（burn/poison/stun）敌人/玩家头顶 sprite + Tween | StatusEffectAppliedEvent + StatusEffectExpiredEvent | client-unity |
| 19-C | 命中 hitspark 粒子 + camera shake | WeaponAttackHitEvent | client-ta |
| 19-D | HP < 30% 边缘血色 vignette 闪烁（post-processing） | PlayerHealthChangedEvent | client-ta |

---

## 3. 不做什么（边界）

- 不引入任何新事件——全部复用 change 20 已有的事件总表
- 不修改伤害计算公式或暴击概率（gd-system 管）
- 不新建 IGameModule（均为 MonoBehaviour Behaviour 或 Shader，挂在已有对象上）
- 不做音效（Sound 模块不在本期范围）
- 不做敌人死亡特效（超出范围，可 FOLLOWUP）
- 不做武器攻击动画（Phase E 单独处理）

---

## 4. 验收定义（DoD）

- [ ] 暴击数字飘字：暴击命中时出现红色放大文字，普通命中出现白色小字，两者在 DOTween 动画曲线下浮起并淡出
- [ ] 3 种 Status 图标：burn/poison/stun 生效时在目标头顶 64x64 sprite 显示，到期时淡出；多 status 同时生效时水平排列
- [ ] hitspark：每次 AttackHit 在命中点产生粒子爆发（持续 < 0.3s，不占 Draw Call 额度导致掉帧）
- [ ] camera shake：AttackHit 触发振幅 0.05 单位、持续 0.12s 的镜头抖动
- [ ] vignette：HP < 30% 时屏幕边缘出现红色脉冲（周期 2s），HP >= 30% 时 Tween 关闭
- [ ] 性能：49 敌人 + 5 status 同时启用时帧时间增量 < 0.5ms（在目标机上验证）
- [ ] 编译 0 error 0 warning（新增代码）

---

## 5. 里程碑

| 节点 | 日期 | 完成信号 |
|---|---|---|
| C1 骨架就绪 | 2026-07-01 | 本 proposal/design/tasks/specs 文件完整 |
| C2 美术出图 | 2026-07-01~02 | codex-image-gen 出 3 status icon |
| C3 并行实现 | 2026-07-02~03 | 19-A/B/C/D 全部编译通过 |
| C4 QA + 归档 | 2026-07-03 | TC-Polish-01~04 全绿；archive 完成 |

---

## 6. 风险

| 编号 | 描述 | 概率 | 影响 | 缓解 |
|---|---|---|---|---|
| R1 | 49 敌人同时 Status 图标导致 Draw Call 超预算 | 中 | 中 | 使用 SpriteAtlas 合批；最多显示 3 status/目标 |
| R2 | URP post-processing vignette 与已有 PP Volume 冲突 | 低 | 高 | 复用已有 Global Volume，只改权重参数 |
| R3 | codex-image-gen 额度不够 3 张全出完 | 低 | 中 | 按优先级：burn > poison > stun；stun 可 fallback 纯色圆形 |
| R4 | Camera shake 在多次快速攻击时叠加过强 | 中 | 低 | shake 实例互斥：新 shake 重置计时器而不叠加振幅 |
