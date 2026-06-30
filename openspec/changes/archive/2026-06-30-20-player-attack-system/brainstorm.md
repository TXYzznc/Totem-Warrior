# Change #20 — Player Attack System · 战斗策划案（gd-lead brainstorm）

> **状态**：草案 v0.1，待主对话 grill
> **作者**：gd-lead
> **基线代码**：CombatModule v2.1 / TattooModule v2.1 / WeaponModule（闲置）/ SkillModule / EnemyModule
> **已敲定**：玩家攻击 = 鼠标左键 + 朝鼠标方向（不锁定）
> **后续顺序**：#20 → #18（weapon-select-flow）→ #19（visual-polish）

---

## §1 战斗 Vision —— 3 条 pillars

### Pillar A：**"我的刺青在帮我打架"** —— 构筑可见、构筑可炫
玩家每一次按下鼠标左键，看到的不是"一个伤害数字 + 一个普攻动画"，而是"一次按键 → 6 个身体部位的刺青被点亮一连串 → 屏幕上飞出火链/冰刺/雷弧/印记爆裂"。**核心心流追求**：玩家拼装的 Build 必须能 *被看见、被听见、被读到*；如果同一个 Build 的攻击和裸装攻击在画面上没区别，刺青系统就没存在意义。

**why it's different**：动作 Roguelike（如《哈迪斯》《死亡细胞》）的"构筑"主要体现在被动数值和少数特效，本作的构筑 = 每次攻击的事件链本身。

---

### Pillar B：**"短决策、长循环"** —— 0.3 秒手感 × 30 秒爆发 × 10 分钟成型
- **0.3 秒**：左键按下到看到首个反馈（武器 startup → 命中 → 刺青触发链开始）必须 < 0.3s，不可让玩家"等"。
- **30 秒**：一波 5-8 个敌人的小遭遇，玩家在这 30 秒里要做"该普攻清杂 / 还是攒触发器打 Elite / 还是骗对方贴脸用闪避反打"的小决策。
- **10 分钟**：一局 Run 的 Boss spawn 在第 10 分钟（EnemyModule.BossSpawnTimeSec = 600s）—— 玩家在这 10 分钟里逐步集齐 6 个身体部位的刺青槽，到 Boss 前 Build 基本成型。

**why it's different**：避免传统 ARPG 那种"前期 30 分钟试探、后期 1 小时收割"的拖沓。本作每一局都是高密度短回合。

---

### Pillar C：**"伤害必须有'来源故事'"** —— 反对无源数字
每一个跳出的伤害数字，玩家都能解释它"为什么是这个数"："因为我右臂是火 + 多段，所以普攻触发了 3 段火伤 + 烧伤 DoT"。**反例**：传统 ARPG"暴击 × 元素加成 × 易伤 × 攻速 × 暴伤"五层乘区叠完，玩家只看到 999999，不知道哪一层重要。本作的 Build 反馈必须**可归因**。

**why it's different**：把"伤害可解释"作为硬约束，意味着伤害源数量要克制（见 §2），且每个伤害源必须有独立的视觉/听觉签名（与 #19 visual-polish 强耦合）。

---

## §2 伤害源谱系 —— 到底有哪些造成伤害的方式？

本作只允许 **9 种**伤害源。少于 9 种不够 build 多样性，多于 9 种玩家归因不过来（违反 Pillar C）。

下表中"接口方式"那一列就是落地 #20 时 client-unity 要接的具体事件。

| # | 伤害源 | 来源类 | 触发条件 | 数值口径 | 接口（事件 / 模块） | MDA - Aesthetics |
|---|---|---|---|---|---|---|
| **D1** | **武器普攻直伤** | 玩家主动 | 左键按下 → WeaponModule.FireWeapon | `WeaponConfig.BaseDamage`（远程不锁定，朝鼠标方向 raycast） | WeaponModule 发 `WeaponAttackHitEvent` | **Submission（顺从感）**：你按一下，世界就响应一下。手感的最低保障。 |
| **D2** | **武器蓄力直伤** | 玩家主动 | 左键长按 ≥ 0.4s 释放 → `isCharged=true` | `BaseDamage × ChargedMul × chargeRatio` | WeaponModule 发 `WeaponChargedAttackEvent` | **Challenge（挑战感）**：用读条窗口换爆发，逼玩家做"现在该不该停手"决策。 |
| **D3** | **刺青·形状直伤**（核心） | 刺青被动触发 | D1/D2 命中 → 触发右臂槽 → Shape.Apply 对 PrimaryTarget 扣血 | `WeaponDamage × ColorMul × PatternMul × SynergyMul × AffixSum`（已实现公式） | TattooModule.Fire 内 `target.Health -= dmg` | **Fellowship（构筑陪伴感）**：你的刺青和你一起打。这是 Pillar A 的核心载体。 |
| **D4** | **刺青·元素 DoT**（持续伤） | 状态附加 | D3 命中后 Element.ApplyElementEffect 给目标加状态 tag（Burn/Poison/...） | DoT DPS × Duration（来自 ElementConfig.Param1/Param2） | 当前 **占位**：仅 `Statuses.Add("Burn(...)")`，无人 tick → **本期必须接入 StatusTickerModule** | **Sensation（感官刺激）**：屏幕上有持续燃烧/中毒粒子，目标血条持续掉。 |
| **D5** | **刺青·Shape 多段/链式/范围** | 刺青形状变体 | D3 命中时按 Pattern 类型展开 | MultiHit segments / ChainJump decay / AOEBurst areaFactor（已实现） | TattooModule.Fire → 各 IShapeBehavior | **Discovery（发现感）**：换一个 pattern → 同一个右臂打出完全不同的形状。 |
| **D6** | **左腿·延迟打包**（已实现） | 玩家主动闪避后下一次普攻 | 空格闪避 → LeftLeg.InterceptApply 打包 PendingTrigger → 下次 D1 命中时 ConsumePendingTriggers 释放 | 与 D3 同公式 | TattooModule.ConsumePendingTriggers | **Expression（表达感）**：闪避→反打成为玩家可主动构造的连招。 |
| **D7** | **技能直伤** | 玩家主动 | Q/E 键 → SkillModule.SkillActivatedEvent（已实现冷却/充能/蓄力 3 种模型） | SkillConfig.BaseDamage（**本期需新增字段**）+ 左臂刺青联动加成 | SkillActivatedEvent → 新增 `SkillHitResolver` 接收并发伤害事件 | **Challenge + Submission**：CD/充能资源管理。 |
| **D8** | **敌人攻击玩家**（受击） | 敌人主动 | EnemyAIController.Tick → EnemyAttackEvent | `EnemyConfig.BaseDamage × EnrageMult` | 已有 EnemyAttackEvent，**本期需新增 PlayerDamageReceiver 把它转成 DamagedEvent + 扣玩家 HP** | **Tension（紧张感）**：让玩家有"我会死"的危机感，否则 Pillar B 的"短决策"没有压力。 |
| **D9** | **远程弹药耗尽降级**（已实现） | 系统自动 | 远程武器 CurrentAmmo=0 时下次开火走 0.4 × 近战挥砸 | `BaseDamage × 0.4` | WeaponModule.ConsumeAmmo 已实现 | **Submission + 微 Tension**：弹药管理压力，但保证不会"没武器可用"。 |

### §2 设计自检（反例）

- 反例 A：**给伤害再加"暴击 × 易伤 × 元素穿透 × 攻速"四个乘区** → 违反 Pillar C "伤害可归因"。**本期拒绝**。当前 CombatModule 那 25% 硬编码暴击直接砍掉，"暴击"改成 **Head 部位的刺青触发**（CritHitEvent 由 Head 槽的特定 Pattern 自己造，而不是普攻随机抽）。
- 反例 B：**加 9 种以上伤害源**（如召唤物自动伤害 / 反伤 / 易伤吸血 / 环境陷阱伤）→ 玩家归因不过来。**本期拒绝**，召唤物（SummonForm shape 已实现 stub）和环境陷阱**留给后续 change**。
- 反例 C：**让普攻直伤（D1）和刺青伤害（D3）走两个独立伤害公式** → 玩家无法在脑内估算"加这个刺青多少收益"。**本期拒绝**，D3 必须以 D1 的 WeaponDamage 为 base 缩放（已经是这么实现的，不要改）。

### §2 总结：本期实现矩阵

| 状态 | 伤害源 |
|---|---|
| ✅ 代码已就绪，只需接线 | D1, D2, D3, D5, D6, D9 |
| ⚠️ 代码有 stub 但缺消费者 | D4（Statuses 加了但没人 tick）、D7（SkillActivated 发了但没人转伤害） |
| ❗ 代码完全缺失 | D8 的 EnemyAttack → 玩家 HP 通路（PlayerDamageReceiver） |
| ❌ 本期不做 | 召唤物伤、环境陷阱伤、反伤、吸血、寒冰减速以外的硬控伤 |

---

## §3 核心战斗循环

### 微循环（一次按键，~ 0.5 秒）

```
鼠标方向选定（持续）
        │
        ▼
[左键按下] ──► WeaponModule.FireWeapon ──► 5 帧 Hitbox (60fps)
                    │                              │
                    │                              ▼
                    │                       命中 → WeaponAttackHitEvent
                    │                              │
                    │   ┌──────────────────────────┘
                    ▼   ▼
            CombatModule 桥接 → AttackHitEvent (Tattoo)
                              │
                              ▼
            TattooModule.Fire 遍历 6 槽：
                ┌─ Head    （监听 CritHitEvent 暂未触发）
                ├─ Torso   （监听 DamagedEvent，本次不触发）
                ├─ LeftArm （监听 SkillCastEvent，本次不触发）
                ├─ RightArm（监听 AttackHitEvent）★ 触发 D3+D4+D5
                ├─ LeftLeg （监听 DodgePressedEvent，可能消耗 PendingTrigger D6）
                └─ RightLeg（监听 MoveTickEvent，本次不触发）
                              │
                              ▼
                  发 VFXTriggerEvent / EffectAppliedEvent
                              │
                              ▼
                  HUD 浮动数字 + 粒子 + 音效（#19 polish 期）
```

**情感曲线**：按下→（10ms 武器音）→（150ms hitbox 命中）→（即刻刺青触发链开始飞）→（300ms 内目标血条扣减+粒子覆盖）。玩家在 0.3 秒内拿到 4 层反馈：手感震动、命中音、刺青线条飞出、伤害数字。

### 小循环（一波遭遇，~ 30 秒）

清杂兵 → Elite 出现 → 决策"先普攻清场 vs 蓄力打 Elite vs 闪避反打" → 清完 → 短暂的 Loot 拾取（武器 / 颜料） → 进入下一波。

### 大循环（一局 Run，~ 10 分钟）

Spawn → 拾取首件武器 + 首张刺青 → 战斗中逐步集齐 6 部位 → Boss 在第 10min spawn → Boss 三阶段 → Run 结算。

**Build 成型节奏**：玩家应在第 5 分钟前感受到"我的 Build 有 identity 了"（至少 2-3 个槽点亮）。如果到第 8 分钟还在裸装，节奏失败。

---

## §4 关键决策点 A/B 表（让主对话挑）

### Q1：暴击（CritHitEvent）该由谁产生？

| 选项 | 后果 | 推荐 |
|---|---|---|
| **A. 武器自带暴击率属性**（每次 D1 按 WeaponConfig.CritRate 抽） | 增加一层乘区，违反 Pillar C 可归因；当前代码无此字段需新增 | ❌ |
| **B. 当前硬编码 25%** | 简单但"暴击"完全是黑箱随机，无策略，与构筑无关 | ❌（应砍） |
| **C. 暴击 = Head 部位刺青触发** ⭐ | Head 槽监听 CritHitEvent → 玩家不装 Head 就没暴击；装了 Head + Fire + ProbBurst → 玩家自己 build 出"概率暴伤" | ✅ 推荐：构筑驱动暴击 |
| **D. 取消暴击概念** | 减少 1 个伤害源，但 Head 部位失去存在意义（6 部位中没有任何事件对应它） | ❌ |

**推荐 C 的理由**：保留 6 部位 trigger 对称性（每个部位都有专属事件），同时让"暴击"从随机噪音变成 Build 选择。CombatModule 那 25% 硬编码立刻砍掉，CritHitEvent 改由"Head 槽里某些特殊 Pattern (例如 ProbBurst) 自己以一定概率发出"。

---

### Q2：蓄力机制 D2 本期是否开放？

| 选项 | 后果 | 推荐 |
|---|---|---|
| **A. 本期开放** | WeaponConfig 已有 ChargedMul + RequiresCharge，但需在 InputModule 加蓄力 API + HumanPlayerController.ShouldChargedAttack | ✅ 推荐 |
| **B. 本期不开放，留 #19** | 武器系统功能闲置，弓类武器（RequiresCharge=true）开火即跳过，看上去 broken | ❌ |
| **C. 本期开放但仅近战可蓄力** | 折衷，但增加例外规则，违反 Pillar 简洁性 | ❌ |

**推荐 A**：蓄力是 9 种伤害源里直接给玩家"做小决策"的关键（Pillar B 短决策）。InputModule 加 `IsAttackHolding()` + `GetAttackHoldDuration()` 两个 API，HumanPlayerController 据此判断 chargeRatio。

---

### Q3：伤害公式骨架（D3 刺青伤害是否保持现有乘区）

当前公式：`magnitude = WeaponDamage × ScaleFactor × ColorMul × PatternMul × SynergyMul × ElementMod × (1 + AffixSum)`

| 选项 | 后果 | 推荐 |
|---|---|---|
| **A. 保持现有 6 层乘区** | 已实现且通过 336 个测试；玩家可在 UI 上看到 6 层数字 → 可归因 | ✅ 推荐 |
| **B. 简化为 base × 单一加成** | 一致性高但牺牲构筑深度，颜色/图案/词缀失去差异 | ❌ |
| **C. 改成 base + 加成（加法乘区）** | 数值膨胀更可控，但与现有 TattooModule.Fire 代码冲突，需重写 | ❌ |

**推荐 A**：现有乘区结构虽多，但每层都对应一个玩家可见的 Build 选择（颜色=元素、图案=形状、词缀=附魔），可归因性 OK。**不要碰**，本期只做接线。

---

### Q4：远程武器朝鼠标射击的"命中谁"判定

用户已敲定"鼠标方向射出"，但还要确定**判定方式**：

| 选项 | 后果 | 推荐 |
|---|---|---|
| **A. Raycast 一条射线，命中最近碰撞体** | 严格 FPS 手感，玩家必须瞄准；硬核但反馈直接 | ⭐ 推荐 |
| **B. 朝向锥形 SphereCast（半角 15°）** | 容错，对新手友好，但削弱"瞄准"感 | 折衷 |
| **C. 仍 GetAimTarget 自动锁定最近敌人，但发射方向用鼠标方向** | 视觉上朝鼠标飞，伤害仍判定锁定目标 → 假射击，玩家会发现 | ❌ |

**推荐 A**：与"动作 Roguelike"基调一致。HumanPlayerController.GetAimTarget 改成"从相机经鼠标位置做 Raycast，命中第一个 Enemy"。如果纯近战，依然走 WeaponModule 的 SphereOverlap 不变。

---

### Q5：D4 元素 DoT 由谁 tick？

当前实现只在 `target.Statuses` 加一个字符串 tag，**没有任何模块 tick 它扣血** —— 这是当前最大的 silent bug，让 D4 形同虚设。

| 选项 | 后果 | 推荐 |
|---|---|---|
| **A. 新建 StatusEffectModule，每 0.5s 遍历所有 actor 的 Statuses 扣血** | 干净，可扩展（未来加易伤/护盾等）；需新模块 | ⭐ 推荐 |
| **B. 在 TattooModule.Tick 里顺便处理** | 不需新模块但职责膨胀；TattooModule 当前不是 ITickable | ❌ |
| **C. 把 Statuses 改成结构化对象（DamagePerSecond/Duration/RemainingTime），由 CombatModule.OnUpdate 顺手 tick** | 中间方案，不引入新模块 | 折衷 |

**推荐 A**：StatusEffectModule 后续会承载冰冻/麻痹/护盾叠层/中毒/燃烧/标记/印记/虚弱 等所有 buff/debuff，是必经之路。本期最小实现：只 tick "Burn" 和 "Poison" 两种 DPS 状态。

---

## §5 与现有 3 个 change 的耦合

### #20 player-attack-system（本期）— 最小落地集

**本期必须实现**：

1. CombatModule 改造：砍掉 25% 硬编码暴击；把 ShouldAttack 路径从"直发 AttackHitEvent"改成"调 WeaponModule.FireWeapon"
2. HumanPlayerController.GetAimTarget 改为鼠标方向 Raycast
3. InputModule 增加 `IsAttackHolding()` + `GetAttackHoldDuration()`；HumanPlayerController.ShouldChargedAttack 接入
4. WeaponAttackHitEvent / WeaponChargedAttackEvent → CombatModule 监听并桥接为 Tattoo 的 AttackHitEvent / CritHitEvent
5. **新建 StatusEffectModule**（D4 落地，tick "Burn"/"Poison"）
6. **新建 PlayerDamageReceiver**（D8 落地，把 EnemyAttackEvent 转为 DamagedEvent 并扣玩家 HP）
7. **新建 SkillHitResolver**（D7 落地，SkillActivatedEvent → 发 AttackHitEvent + 走刺青链）
8. Head 部位刺青改为可发 CritHitEvent 的源（修改 HeadPartBehavior 或新增 Head Pattern）

**本期激活的伤害源**：D1, D2, D3, D4（最小化）, D5, D6, D7（基础）, D8, D9

---

### #18 weapon-select-flow（下一期）— 新激活

- 让玩家在 Run 中能切换/拾取不同 WeaponConfig 行 → D1 的 BaseDamage 开始变化
- 给 SkillConfig 加 BaseDamage 字段并补 4-6 个具体技能 → D7 真正有多样性
- 弹药/补给点机制 → D9 的降级体验真正成为策略压力

---

### #19 visual-polish（再下一期）— VFX 接入

- 每个伤害源给独立的 VFX/SFX 签名（对应 Pillar C "可归因"的视觉层面）：
  - D1 普攻：白色 hitspark
  - D2 蓄力：屏幕震动 + 红色 hitspark
  - D3/D5 刺青：按 element 颜色分（火橙/雷蓝/冰青/毒绿）
  - D4 DoT：目标身上持续元素粒子
  - D6 延迟打包：闪避后下次普攻的"双重特效"叠加
  - D7 技能：技能专属粒子（按 SkillId）
  - D8 受击：屏幕红边 + 血条扣减动画
- 优化 CritHitEvent 的视觉权重（暴击数字 1.5×, 颜色变金）

---

### 留给后续 change（**本期明确不做**）

- 召唤物伤害（SummonForm shape 已有 stub，但召唤物 AI 控制器没写）
- 环境陷阱伤害（无地图生成）
- 反伤 / 吸血 / 护盾值（StatusEffectModule 框架建好后再加）
- 易伤 / 元素抗性矩阵（避免暴击外再加乘区）
- PvP 伤害（单机项目，永远不做）
- 弓的多段持续射击 / 充能镭射 等特殊武器形态

---

## §6 风险与可玩性陷阱（gd-lead 判断）

### 风险 R1：**Build 成型时间过晚 → 前 5 分钟没玩头**
- **症状**：玩家裸装打 5 分钟才拿到第 1 个刺青，前期就是"按左键、看小数字"。
- **规避**：开局给玩家**预设 1 个固定槽**（例如右臂·火·SingleHit 的 starter build），第 1 个 kill 立刻掉 Head 槽颜料。第一波遭遇结束前必须有 ≥ 2 槽点亮。
- **本期落地**：在 SpawnerModule.PlayerActor 初始化时 `TattooModule.Equip(2, 1, 1)` 给个起手 build。

### 风险 R2：**伤害源 9 种，但玩家只感知 2-3 种 → 其余形同虚设**
- **症状**：D4 元素 DoT 没接入 tick → 玩家根本不知道自己中毒了；D6 延迟打包没视觉签名 → 玩家以为是普通普攻。
- **规避**：每个伤害源**必须**有独立浮动数字颜色 / 字号 / 前缀（"DoT 12"/"Crit 99"/"Delay 50"），见 #19 polish。本期至少让浮动数字带 source 标签。
- **本期落地**：EffectAppliedEvent 里的 EffectResult 已有 Part/Element/Shape/Status 字段，HUD 浮字必须读这几个字段渲染，不能只显示 Damage。

### 风险 R3：**数值膨胀失控**
- **症状**：Build 成型后单次普攻打 50000，Boss 也是 10 万血 → 数字大但没意义。
- **规避**：建立**伤害基线表**（gd-system 后续做）：裸装单次普攻=10，满 Build 后单次普攻含触发链 ≤ 200（20×）。Boss 血量按 200 × 60秒 / 2 = 6000 设计。任何 Build 不得突破"普攻 ≤ 300"上限。
- **本期落地**：建立"裸装基线 = 10 DPS"作为锚点，所有 ColorMul/PatternMul/ScaleFactor 在 DataTable 中校准。**gd-system 出公式表**。

### 风险 R4：**鼠标方向射击的视野/瞄准歧义**
- **症状**：俯视/斜视相机下，"鼠标在屏幕上"和"敌人在 3D 世界里"对不上，玩家瞄不准。
- **规避**：必须有**地面投影准星**（屏幕鼠标位置 → 地面 raycast → 投影点 → 玩家朝该点开火）。否则玩家会一直打偏。
- **本期落地**：HumanPlayerController.GetAimTarget 内部做 Plane(Vector3.up, 0).Raycast 取交点；客户端在该交点画一个准星 decal（visual 留 #19）。

### 风险 R5：**刺青触发链触发太多 → 一次按键 6 个事件同时发，玩家看不清是谁做了什么**
- **症状**：玩家按一下左键，6 槽全亮，屏幕粒子糊脸，根本不知道哪个槽贡献了多少。
- **规避**：(a) 同一个 AttackHit 同时只会触发"监听 AttackHitEvent 的槽"（右臂），不会同时触发所有 6 槽 —— 当前架构已经是这样，OK。 (b) 浮动数字要按 Part 分行错开显示，不要叠在一起。 (c) **本期暂不允许"同一部位装多个槽"**（TattooModule._equipped 当前是 List，要在 Equip 里加同 Part 互斥校验）。
- **本期落地**：Equip 时校验"同 PartId 已存在 → 替换而非追加"。

### 风险 R6（**需新模块**）：**StatusEffectModule 未实现 → D4 失效**
- 已在 §4 Q5 标记。**本期必做新模块**，不可推迟。
- 注：若决策推迟，则本策划案中 D4 必须明确写入"本期不做"，玩家不会感受到任何元素 DoT —— 这会让 7 种元素（Fire/Lightning/Nature/Frost/...) 中至少 3 种（Fire/Nature 的核心卖点是 DoT）的 identity 消失。**强烈不建议推迟**。

---

## 附录：本期 9 种伤害源 → 现有事件映射速查

| 伤害源 | 触发事件 | 消费者 | 本期需新增 |
|---|---|---|---|
| D1 普攻 | WeaponAttackHitEvent | CombatModule → AttackHitEvent | CombatModule 桥接逻辑 |
| D2 蓄力 | WeaponChargedAttackEvent | CombatModule → AttackHitEvent (高伤) | InputModule 蓄力 API |
| D3 刺青形状 | AttackHitEvent (内部) | TattooModule.Fire | 无（已实现） |
| D4 元素 DoT | Target.Statuses.Add | **StatusEffectModule** | **新模块** |
| D5 多段/链式 | AttackHitEvent (内部) | 各 IShapeBehavior | 无（已实现） |
| D6 延迟打包 | DodgePressedEvent + 下次 AttackHitEvent | LeftLegPartBehavior + ConsumePendingTriggers | 无（已实现） |
| D7 技能 | SkillActivatedEvent | **SkillHitResolver** → AttackHitEvent | **新桥接** |
| D8 受击 | EnemyAttackEvent | **PlayerDamageReceiver** → DamagedEvent | **新接收器** |
| D9 弹尽降级 | (内部 ConsumeAmmo) | WeaponModule.FireWeapon | 需在 ConsumeAmmo 返回 false 时做 0.4× 处理（当前代码有 TODO） |

---

## 结尾：给 grill 的"挖透清单"

主对话 grill 时建议聚焦以下 5 题（与 §4 对应），任何一题没确定就不能开 openspec：

1. **Q1 暴击归属**：A/B/C/D 选哪个？（推荐 C）
2. **Q2 蓄力本期开**：A/B/C 选哪个？（推荐 A）
3. **Q3 伤害公式骨架**：保持现有 6 层乘区？（推荐 A 保持）
4. **Q4 鼠标射击判定**：raycast 严格瞄准 vs SphereCast 容错？（推荐 A）
5. **Q5 D4 元素 DoT tick 方案**：新建 StatusEffectModule？（推荐 A，强烈不建议推迟）

另外还要主对话决定：**风险 R1 起手 build 给不给 + 给什么** —— 这会影响首 5 分钟体验。

---

## §7 grill 收敛最终决策（2026-07-01）

> 主对话与用户 6 轮 grill 收敛结果。以下为本 change 进入 openspec 阶段的**冻结决策**。

### 7.1 战斗设计决策

| 决策点 | 选项 | 说明 |
|---|---|---|
| **Q1 暴击归属** | C Head 刺青触发 | 删 CombatModule 25% 硬编码；HeadPartBehavior 改造为 CritHitEvent 源（按 Pattern 概率重发） |
| **Q2 蓄力机制** | A 本期开放 | InputModule +`IsAttackHolding()`+`GetAttackHoldDuration()`；HumanPlayerController.ShouldChargedAttack 接入 |
| **Q3 武器属性边界** | B 精化版 | 武器=基础数值 + **普攻 trait 1 个 + 蓄力 trait 1 个**（共 5 武器 × 2 = 10 traits） |
| **Q3.2 元素归属** | A 武器中性 | 元素完全由 ColorParam（右臂刺青）决定；WeaponConfig 不加 Element 字段 |
| **Q4 射击判定** | B 半角可配 | SphereCast，半角由 WeaponConfig.AimSpreadHalfDeg 字段控制（0°=Raycast 严格 / 180°=自动锁定） |
| **Q5 D4 DoT tick** | A 新建 StatusEffectModule | 独立模块，0.5s tick 所有 Statuses；本期最小集 tick "Burn"/"Poison" |
| **Q公式骨架** | A 保持 6 层乘区 | TattooModule.Fire 不改；336 TC 必须继续 PASS |
| **R1 起手 build** | C 扩展版 | StartupSelectForm（颜料三选一 + 武器五选一 + 图案二选一）|

### 7.2 新增决策（grill 中浮现）

- **攻击动画范式 — 元气骑士模式** ⭐：
  - 玩家本身只有**一套通用挥砍动画**（不分武器）
  - **武器自带挥砍/射击动画**（每个武器 prefab 含自己的 Animator）
  - → WeaponConfig 加 `WeaponPrefabPath` 字段，武器在玩家手上实例化为子物体
  - → 不再为每把武器扩玩家 Animator 状态机

- **图案解锁系统（Meta Progression）**：
  - 存档：`GlobalProgress.UnlockedPatternIds: List<int>`，默认 `[1, 2]`（单击 + 多段）
  - **本期仅读不写**：解锁触发（首杀 50 人/首杀 Boss/...）推后到独立 change（#21 meta-progression）
  - StartupSelectForm 的图案候选 = UnlockedPatternIds

- **#18 重定义**：
  - 原 weapon-select-flow（5 选 1 武器 UI）**拆入 #20**
  - **#18 新定义**：战斗中拾取武器替换（敌人死亡 10% 掉 WeaponPickup → 按 E 替换）
  - 18-weapon-select-flow proposal 待重写

### 7.3 本期 #20 最终范围（冻结）

**新建模块**（3 个）：
1. `StatusEffectModule`（D4 落地，tick Burn/Poison，~150 行）
2. `PlayerDamageReceiver`（D8 落地，EnemyAttackEvent → DamagedEvent + 扣玩家 HP，~80 行）
3. `SkillHitResolver`（D7 落地，SkillActivatedEvent → AttackHitEvent，~60 行）

**新建 UI**（1 个）：
- `StartupSelectForm`（颜料三选 + 武器五选 + 图案二选 + 进入战斗按钮）

**新建/扩展 DataTable**（2 张）：
- `WeaponConfig` +4 字段（`AimSpreadHalfDeg` / `NormalTraitId` / `ChargedTraitId` / `WeaponPrefabPath`）
- `WeaponTraitConfig`（新表，10 行：5 武器 × 2 trait）

**改造文件**（6 处）：
- `CombatModule.cs`：删 25% 硬编码暴击 + 改为调 `WeaponModule.FireWeapon`
- `HumanPlayerController.cs`：GetAimTarget 改为鼠标地面投影 + SphereCast；ShouldChargedAttack 接入
- `InputModule.cs`：+`IsAttackHolding()`/`GetAttackHoldDuration()`
- `TattooModule.cs`：+`[EventHandler] OnWeaponAttackHit(WeaponAttackHitEvent e) → Fire(AttackHitEvent)`
- `HeadPartBehavior.cs`：扩展 CritHitEvent 发射逻辑（按 Pattern 概率）
- `SpawnerModule.cs`：移除起手硬编码 init，改为接 StartupSelectedEvent

**美术任务**（Codex 出图，~5 批次）：
- 5 武器 prefab 帧序列（挥砍/射击动画 ~4 帧 each）
- StartupSelectForm 颜料卡片 × 3
- StartupSelectForm 武器卡片 × 5
- 准星 decal sprite
- 起手图案选择卡片 × 2

### 7.4 验收门槛

1. **可玩闭环**：MainMenu → CharacterSelect → **StartupSelectForm**（3 色 + 5 武 + 2 图案）→ Combat → 玩家可死 → 可杀敌 → RunResult
2. **9 伤害源全部可触发**：TC-Damage-D1～D9（每条 log 在 Console 可见 + 0 errors）
3. **手感指标**：左键按下 → 0.3s 内首位反馈；蓄力释放 → 0.5s 内多段火/冰粒子
4. **Pillar A 可视化**：一次普攻 ≥ 3 个 source 反馈（如：白 hitspark + 火字跳出 + 火点燃粒子）

### 7.5 关键约束

- ✅ **不引新依赖**：仅用现有 UniTask / DOTween / Resources / FrameworkLogger
- ✅ **现有 336 TC 不变 PASS**：TattooModule.Fire 公式不重写
- ✅ **Codex 可用**：本期允许出图（5 武器 prefab + UI 卡片 + 准星）
- ⚠️ Hub 场景 / 解锁触发 / 易伤抗性矩阵 / 召唤物 / 环境陷阱 / PvP → **明确本期不做**
