# Balance Notes — Change #20 Player Attack System
> 作者：gd-system | 日期：2026-07-01 | 任务 H

---

## 一、设计锚点与约束

brainstorm §7.1 Q3 精化版决策：武器 = 基础数值 + 普攻 trait 1 个 + 蓄力 trait 1 个。
brainstorm §6 风险 R3 约束：裸装单次普攻 = 10 DPS 基线；满 Build 后单次普攻含触发链 ≤ 200（20×上限）；Boss 血量 = 6000 HP。

本文件只讨论 D1（武器普攻直伤）+ trait 层面的数值。刺青乘区（D3 ColorMul / PatternMul / SynergyMul）不在本文件范围内（那一层已有 336 TC 保障）。

---

## 二、武器 DPS 锚点计算（D1 裸装，无 trait，无刺青）

攻击间隔 = (BaseStartup + BaseActive + BaseRecovery) / 60 fps

| WeaponId    | BaseDamage | 攻击间隔(s)        | 裸装 D1 DPS | 弹药限制       |
|-------------|------------|--------------------|-------------|----------------|
| knife_basic | 18         | (3+3+4)/60 = 0.167 | 108         | 无限           |
| hammer_heavy| 45         | (8+5+12)/60 = 0.417| 108         | 无限           |
| pistol_basic| 22         | (2+1+5)/60 = 0.133 | 165         | 18发/弹匣      |
| bow_charge  | 35         | (12+2+6)/60 = 0.333| 105（需蓄力）| 24发/弹匣      |
| energy_fist | 28         | (5+4+6)/60 = 0.250 | 112         | 无限           |

**说明**：
- knife / hammer 裸装 DPS 几乎相同（108），设计意图一致——差异完全由 trait 提供。
- pistol 裸装 DPS(165) 高于近战，但弹匣 18 发 × 0.133s = 约 2.4s 打完，随后 D9 降级（0.4×）补偿。实际持续 DPS 需计入换弹或降级段，有效降低至约 120-130 DPS 区间。
- bow RequiresCharge = true，非蓄力无有效命中，DPS 是"蓄力完成的峰值 DPS"，实际操作间隔更长（约 0.5-0.8s 含蓄力读条时间）。⚠️ **需 playtest 验证**：实际蓄力间隔。
- energy_fist 处于 knife 与 hammer 之间，是兼顾速度与范围的混合武器。

---

## 三、Trait 数值理由

### trait_quickslash（快斩）
- **分配**：knife_basic 普攻、energy_fist 普攻（共享）
- **EffectParam1 = 0.30（后摇缩短 30%）**
  - knife 后摇 4 帧 → 缩短到 2.8 帧，实际攻击间隔从 0.167s 降至约 0.133s，DPS 提升约 25%
  - 但 quickslash 不是纯 DPS trait，而是"速度位"——它改变了攻击节奏感，让玩家感受到"快"
- **EffectParam2 = 4.0（第3刀起灼烧 tick 伤害）**
  - 4 × (1/0.5) × 1.5 = 12 点 DoT 总伤，约为 knife BaseDamage 的 67%，叠加在第 3 刀及之后
  - 设计意图：奖励持续对同一目标输出（连击感），而不是乱打一通
  - tick 伤害 4 点 ≈ BaseDamage(18) × 22%，在 15-25% 阈值内合规

### trait_pierce（穿透）
- **分配**：knife_basic 蓄力、pistol_basic 普攻、bow_charge 普攻（共享）
- **EffectParam1 = 3.0（穿透目标数）**、**EffectParam2 = 0.20（每穿一个衰减 20%）**
  - 第1目标 100%、第2目标 80%、第3目标 64%，总伤约 2.44×
  - 三把武器各自决策意义不同：knife pierce 是蓄力爆发穿线；pistol pierce 是走廊清杂；bow pierce 是高伤穿透
- 弓蓄力满穿透上限提升至 5（在 Description 中说明但由代码实现逻辑控制，EffectParam1 = 3 是普攻基准）
- ⚠️ **需 playtest 验证**：多目标 boss 战时穿透是否过强

### trait_stun（震晕）
- **分配**：hammer_heavy 普攻
- **EffectParam1 = 0.8s**（眩晕时长）、**EffectParam2 = 0.25**（眩晕期间受伤加成 25%）
- hammer 攻击间隔 0.417s，如果眩晕可叠刷（每次普攻刷新），实际控制率接近 100%
  - 这是预期的：hammer 是"控位"武器，牺牲速度换取控制
  - 眩晕期间 +25% 伤害 = 实际 DPS 从 108 提升到 108 × 1.25 = 135（近似锤的"高爆发窗口"）
- 蓄力眩晕 1.6s：hammer 蓄力本身 ChargedMul = 2.2×，配合 1.6s 眩晕可以给刺青触发链留出时间
- ⚠️ **需 playtest 验证**：对 Boss 的眩晕是否需要抗性（连续眩晕 Boss 会让战斗失去挑战性）

### trait_chain（连锁）
- **分配**：bow_charge 蓄力
- **EffectParam1 = 3.0**（跳传目标数）、**EffectParam2 = 0.30**（每跳衰减 30%）
  - 主目标 100%（bow 蓄力满 35 × 3.0 = 105）→ 跳1 74.5 → 跳2 52.1 → 跳3 36.4，总伤约 2.68×
  - 连锁跳传触发刺青：设计意图让弓蓄力打出"炸人链条"，和 bow 的 Rarity = 2 匹配
- ⚠️ **需 playtest 验证**：跳传范围 4m 是否合适（太大会让弓近距离也轻松命中）

### trait_explosive（爆震）
- **分配**：hammer_heavy 蓄力
- **EffectParam1 = 2.5m**（爆炸半径）、**EffectParam2 = 0.60**（溅射伤害率）
  - hammer 蓄力 = 45 × 2.2 = 99 点直伤，范围内其余目标各受 99 × 0.6 = 59.4 点
  - 爆炸引爆现有 DoT：锤蓄力 + 目标身上 Burn → 立即触发 1 次 tick，额外 4 点，增加 DoT 与控制协同感
- hammer 是"群控群伤"武器，explosive 是其高风险高回报的核心卖点

### trait_multishot（多重射击）
- **分配**：pistol_basic 蓄力
- **EffectParam1 = 3.0**（发射数量）、**EffectParam2 = 18.0**（扇形半角度数）
  - 单发 0.7×，3发合计 2.1× 等效伤害
  - pistol BaseDamage 22 × 0.7 = 15.4 × 3 = 46.2，约等于单次锤普攻
  - 扇形 18° 在近距离几乎全中（等效 2.1×），远距离分散（等效约 1.0-1.5×）——自动平衡距离
- 多重射击消耗 3 发弹药（弹匣 18 发 = 约 6 次 multishot），稀缺性合理

### trait_pull（拉扯）
- **分配**：energy_fist 蓄力
- **EffectParam1 = 1.5m**（拖拽距离，拉至玩家 1.5m 内）、**EffectParam2 = 5.0**（碰撞伤害/次，最多3次）
  - 拖拽过程最大额外伤害 = 5 × 3 = 15 点
  - energy_fist 蓄力 = 28 × 1.8 = 50.4 + 15 = 65.4 点（含拖拽）
  - 设计意图：拉扯是位移控制，配合闪避反打（D6）构成 energy_fist 的"近战流"combo
- ⚠️ **需 playtest 验证**：拖拽动画/物理实现是否会穿墙或卡地形（这是代码层风险，非数值风险）

### trait_dot_burn（灼烧附加）
- **分配**：当前无武器直接分配（由刺青元素系统触发，或未来 #18 补充）
- **EffectParam1 = 4.0**（每 tick 伤害）、**EffectParam2 = 2.5**（持续秒数）
  - 5 tick × 4 = 20 点总伤；tick 间隔 0.5s，持续 2.5s
  - 4 点 tick 相对于中等武器 BaseDamage(≈25) = 16%，低于上限 25%
  - 不叠层（刷新）：防止速攻武器堆叠 DoT 突破设计上限

### trait_dot_poison（中毒附加）
- **分配**：当前无武器直接分配（同上）
- **EffectParam1 = 3.0**（每 tick 伤害）、**EffectParam2 = 3.0**（持续秒数）
  - 6 tick × 3 = 18 点总伤，略低于 burn（20 点）但可叠 2 层
  - 2 层叠加：总计 18 × 2 = 36 点，DoT DPS = 6/s（中毒 6s 等效），比 burn 更长线
  - burn vs poison 差异：burn 快速爆发（2.5s 结束），poison 慢渗透（3s × 2层）

### trait_lifesteal（微吸血）
- **分配**：当前未分配任何武器，预留给 #18 掉落系统
- **EffectParam1 = 0.08**（回血比例 8%）、**EffectParam2 = 12.0**（单次回血上限 HP）
  - 12 HP 上限：防止满 Build 高伤时吸血量过大（knife 触发链 200 伤 × 8% = 16 → 被上限卡到 12）
  - 此 trait 数值为预留值，**完全需要 playtest 验证**：吸血过多会让玩家无压力感（破坏 Pillar B 短决策）

---

## 四、武器 AimSpreadHalfDeg 决策表

| WeaponId    | AimSpreadHalfDeg | 原值  | 决策理由（30字内）                           |
|-------------|------------------|-------|----------------------------------------------|
| knife_basic | 120°             | 180°  | 近战容忍宽但不应全锁定，留少量方向感         |
| hammer_heavy| 90°              | 180°  | 慢速近战，120°太宽松，90°强制玩家朝向敌人    |
| pistol_basic| 12°              | 10°   | 射击需瞄准感，+2°减少俯视角误差率            |
| bow_charge  | 4°               | 5°    | 蓄力弓是精准位，比手枪更严格                 |
| energy_fist | 35°              | 30°   | 混合近战，35°兼顾拳击弧形攻击的容错          |

**说明**：
- knife/hammer 原值 180°（自动锁定）被改为 120°/90°，是为了让"方向朝向"保留操作意义。完全锁定（180°）违背 Pillar B "短决策"，让普攻无需任何操作判断。
- pistol 12° 在俯视角相机下等效屏幕宽度约 ±3-4px（20m距离处），感知上仍属精准瞄准。
- bow 4° 是最严格的瞄准要求，与 RequiresCharge 和高 BaseDamage 配合，形成"蓄力→精准射击"的高技巧高回报路线。
- energy_fist 35° 在 30-45° 区间，选 35° 而非 45° 是因为 pull trait 需要玩家大致对准目标才能拉扯有效。

---

## 五、DPS / TTK 估算（目标：50 HP 敌人，裸装 D1，无刺青）

TTK = 目标 HP / DPS

| 武器          | 裸装 D1 DPS | TTK (50HP 敌人) | 蓄力 TTK (首次蓄力完成后单刀) |
|---------------|-------------|-----------------|-------------------------------|
| knife_basic   | 108         | 0.46s (约 3 刀) | 1刀: 18×1.5=27, 还需再打 23   |
| hammer_heavy  | 108         | 0.83s (约 2 刀) | 1刀: 45×2.2=99 → 秒杀普通兵  |
| pistol_basic  | 165         | 0.30s (约 3 发) | 蓄力不要求，multishot 1次≈46  |
| bow_charge    | 105*        | 约 0.8s(含蓄力) | 1矢: 35×3.0=105 → 秒杀普通兵 |
| energy_fist   | 112         | 0.45s (约 2 拳) | 1拳: 28×1.8=50.4 → 约 1 击   |

> *bow 实际蓄力时间估算 0.4-0.8s，TTK 含蓄力读条。

**含 trait 后 TTK 变化（knife + quickslash，第 3 刀起）：**
- 后摇缩短 30%，间隔从 0.167s → 0.133s，DPS 108 → 135
- TTK(50HP) = 50/135 ≈ 0.37s（约 3 刀）

**含 trait 后 TTK 变化（hammer + stun，首刀眩晕）：**
- 首刀 45 伤，目标眩晕 0.8s（受伤 +25%）
- 眩晕期间第二刀：45 × 1.25 = 56.25
- 两刀总伤：45 + 56.25 = 101.25，已超过 50HP
- TTK = 0.417 + 0.417 = 0.83s（但实际上第 2 刀就能秒杀 50HP 目标）

**Boss 级别（6000 HP）TTK 估算（裸装，不含刺青触发链）：**
- hammer 蓄力普攻链：约 60s（接近 Boss 设计的 10 分钟 Build 成型节奏合理）
- 满 Build（20× 刺青倍率上限）：6000 / (108 × 20) = 2.8s 理论最大（过快）
  - 实际上刺青乘区不会全部同时生效，实际满 Build TTK Boss 预期 15-30s
  - ⚠️ **需 playtest 验证**

---

## 六、平衡检查：优势策略与退化路径

**健康状况：存在隐患（中等风险）**

### 检测到的潜在问题

| 问题 | 武器/Trait | 风险等级 | 建议处理 |
|------|-----------|----------|---------|
| pistol 裸装 DPS 165，明显高于其他武器 | pistol_basic | 中 | 弹匣限制（18发≈2.4s）是自然平衡器；playtest 验证是否需降 BaseDamage |
| hammer stun + 刺青可能对 Boss 永久眩晕 | hammer+stun | 高 | **需要 Boss 抗眩晕机制**，建议 Boss 有 3s 眩晕免疫冷却（需代码配合）|
| pierce 穿透 3 目标可能在走廊中变成最优解 | pierce | 低 | 弓有弹药限制；pistol pierce 是普攻 trait，不加蓄力穿透不升（可接受）|
| quickslash 被 knife 和 energy_fist 共享，两把武器手感趋同 | 两把武器 | 低 | 设计上允许：identity 差异由 BaseDamage/Range/蓄力 trait 区分 |
| trait_lifesteal 未分配给武器（孤立数值） | lifesteal | 无（预留） | #18 阶段再激活，当前无影响 |

### 退化路径（玩家可能选择的最优解）

1. **弓 + 蓄力 pierce 链条**：蓄力满 105 伤 × pierce 2.44× = 256 单次最高伤害，配合 trait_chain 弓蓄力 = 105 × chain 跳传 2.68× ≈ 281，这是全游戏单次最高伤害输出。
   - 风险：仅限蓄力路线（普通攻击弓无效），弹药 24 发 = 约 8 次蓄力，操作门槛高
   - 评估：**可接受**，这是弓的设计定位（高技巧高回报）

2. **pistol 持续射击刷新 DoT**：pistol 自带 pierce，可以同时命中多目标触发刺青 DoT，弹匣打完前的 DPS 最高
   - 风险：弹匣 18 发消耗后降级至 D9 (0.4×BaseDamage = 8.8)，有自然惩罚
   - 评估：**可接受**

---

## 七、已知风险（需 playtest 验证）

以下数值纯靠理论推导，实际手感可能需调整：

1. **bow 蓄力实际时间**：代码中 0.4s 为 InputModule.GetAttackHoldDuration() 门槛，但玩家实际释放时机难预测，bow 有效 DPS 需实测。

2. **hammer stun 对 Boss 的影响**：Boss 是否需要眩晕抗性（Boss 血量 6000，每次 stun 0.8s 基本可以无限眩晕循环）——建议 Boss 加 `stun_immune_cd = 3.0s`，但这属于 Boss 设计而非 weapon trait。

3. **pierce + chain 叠加逻辑**：bow 普攻 trait 是 pierce，蓄力 trait 是 chain，两者不同时触发（普通攻击 bow 无效因 RequiresCharge），但代码实现需确认 pierce 和 chain 不会同时生效（如果后续 #18 武器掉落中玩家能给弓同时装 pierce+chain 则需限制）。

4. **energy_fist pull 拖拽的物理实现**：拖拽距离 1.5m 需要物理引擎支持平滑移动，俯视角下路径可能穿越地形——这是纯代码风险，不在数值调整范围内，但影响 trait 的实际效果。

5. **quickslash 后摇缩短的帧数精度**：30% 后摇缩短在 60fps 下 knife 后摇从 4 帧→2.8 帧（舍入到 3 帧），能否精确到 sub-frame 由 WeaponModule.TickHitboxJobs() 实现决定，需代码侧确认。

6. **trait_lifesteal 吸血上限 12 HP 是否合适**：预留值，待 #18 掉落系统确定玩家 MaxHp 范围后再校准（当前 SpawnerModule.PlayerMaxHp 不确定）。

---

## 八、与 brainstorm §6 风险 R3 对齐

| brainstorm 约束 | 本文数值设计 | 状态 |
|----------------|-------------|------|
| 裸装单次普攻 = 10 DPS 基线 | knife 裸装单刀 18 点（不是 10 点） | ⚠️ 偏差：brainstorm 原文是"TattooModule scaleStat 基准"，10 DPS 是刺青系统的 scaleStat 起点，武器 BaseDamage 18 不冲突（两个不同层） |
| 满 Build 单次普攻含触发链 ≤ 200（20×） | knife 18 × 20 = 360 > 200 | ⚠️ 需关注：brainstorm 约束针对的是触发链后总伤，knife 18 作为 base 配合 20× 刺青倍率确实超限。**建议把 20× 上限理解为"每次按键的总事件链伤害不超过 200"，而不是"单刀 × 20"**，这需要 gd-lead 确认定义 |
| Boss HP = 6000 | 已用于 TTK 估算 | OK |
| 任何 Build 不得突破"普攻 ≤ 300"上限 | 弓蓄力 pierce chain 理论峰值 ≈ 281，在上限内 | OK |

---

### 裁定（gd-lead, 2026-07-01）

**选定语义**：R3 "20× 上限" = **"单次按键事件的总伤害链 ≤ 基线 10 DPS × 20 = 200 点"**，即针对"一次玩家输入触发的完整事件链总伤"（直伤 + 刺青触发 + DoT 首 tick + 连锁/穿透 + 溅射），与"BaseDamage × 20"无关。武器 BaseDamage（如 knife 18）是 D1 直伤的实现细节，与 R3 锚点（10 DPS 抽象基线）分属两层，不冲突。

**数值是否落地**：**否（本期不落数值守门）**。WeaponConfig/WeaponTraitConfig 当前无"事件链总伤上限"字段，且本 change #20 范围限定在 D1+trait 层（≈ 200 点理论峰值已在弓蓄力 pierce/chain 链路达到 281，已接近但未超 300 硬上限）。刺青乘区（D3 ColorMul × PatternMul × SynergyMul）由 #18 落地时再做总伤 clamp（建议在 DamageResolver 加 `final_damage = min(chain_total, 200)` 软上限 + 300 硬上限熔断），届时 R3 约束才真正生效。

**对当前 10 trait 数值的影响**：**无需调整**。当前任何单 trait 含 D1 直伤峰值（hammer 蓄力 99 / bow 蓄力 pierce 256 / bow 蓄力 chain 281）均 ≤ 300 硬上限，符合 R3 兜底约束。本期通过 playtest 验证实际手感即可。

**后续 TODO（移交 #18 + balance 迭代）**：
1. #18 DamageResolver 实现总伤链 clamp 字段（200 软上限 / 300 硬上限）
2. balance.md §五 Boss TTK "满 Build 20× 刺青倍率" 演算需在 #18 完成后用实际乘区数值复算
3. trait_lifesteal 吸血上限 12 HP 校准延后至 #18 玩家 MaxHp 确定
