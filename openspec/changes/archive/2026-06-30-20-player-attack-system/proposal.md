# Proposal — 20-player-attack-system

## Why

最小游戏闭环（#16）+ 角色美术（#17）已就绪，**但战斗系统骨架仍存在 3 处 silent bug + 1 处玩法缺失**：

1. **D4 元素 DoT 形同虚设**：`TattooModule.Fire` 仅给目标 `Statuses.Add("Burn(...)")`，**无任何模块 tick** → 玩家从未感受到燃烧/中毒 DoT。
2. **D8 受击通路缺失**：`EnemyAttackEvent` 已发布，**无消费者** → 玩家不会被怪扣血 → 不会死亡 → 无 risk → 无策略压力（违反 Pillar B）。
3. **D7 技能伤害无落地**：`SkillActivatedEvent` 已发布，**无桥接到 AttackHitEvent** → 玩家释放技能无伤害结算 → 无刺青联动。
4. **暴击是黑盒 25% 随机** → CombatModule.cs L138 硬编码，违反 Pillar C「伤害可归因」原则。
5. **起手 Build 硬编码** → SpawnerModule 给玩家固定起手 Build，**玩家无选择** → 前 5 分钟没有玩法 identity（违反 Pillar A）。

本 change **冻结 9 种伤害源** 并最小化打通全部消费路径，让 vertical slice 第一次出现"完整战斗循环"。

## What

按 `brainstorm.md §7` 冻结决策落地以下变更：

1. **新建 3 个模块**：
   - `StatusEffectModule`（D4 落地，0.5s tick 所有 Status，本期最小集 Burn/Poison）
   - `PlayerDamageReceiver`（D8 落地，`EnemyAttackEvent` → 扣玩家 HP + 发 `DamagedEvent` + 死亡时 `PlayerDiedEvent`）
   - `SkillHitResolver`（D7 落地，`SkillActivatedEvent` → 按 `SkillConfig.BaseDamage × DamageMul` 发 `AttackHitEvent` 走刺青链）

2. **新建 1 个 UI**：`StartupSelectForm`（颜料三选 + 武器五选 + 图案二选 → 发 `StartupSelectedEvent`）

3. **扩展/新建 DataTable**：
   - `WeaponConfig` +4 字段：`AimSpreadHalfDeg` / `NormalTraitId` / `ChargedTraitId` / `WeaponPrefabPath`
   - **新表** `WeaponTraitConfig`（10 行 trait：5 武器 × 2 trait 占位，具体效果交 gd-system 平衡）

4. **改造 6 处现有文件**：
   - `CombatModule.cs`：删 25% 硬编码暴击 + 改为调 `WeaponModule.FireWeapon`
   - `HumanPlayerController.cs`：`GetAimTarget` 改为鼠标地面投影 + SphereCast（半角读 WeaponConfig）；`ShouldChargedAttack` 接入
   - `InputModule.cs`：+`IsAttackHolding()` / `GetAttackHoldDuration()`
   - `TattooModule.cs`：+`[EventHandler] OnWeaponAttackHit / OnWeaponChargedAttack` 桥接到现有 `Fire(AttackHitEvent)`
   - `HeadPartBehavior.cs`：扩展为「按 Pattern 概率发 `CritHitEvent`」的暴击源（取代 25% 硬编码）
   - `SpawnerModule.cs`：移除起手硬编码 init，改为等待 `StartupSelectedEvent` 后才装备玩家

5. **元气骑士动画范式**：武器自带 prefab 动画（每个 `WeaponConfig` 行通过 `WeaponPrefabPath` 引用一个含 Animator 的 prefab，挂载到玩家手上作子物体），玩家自身只有一套通用挥砍动画。

## Scope

**做（本轮）**：

- 9 种伤害源全部可触发（D1～D9 各一条 acceptance test）
- 3 个新模块（StatusEffectModule / PlayerDamageReceiver / SkillHitResolver）骨架 + 实现 + 单测
- StartupSelectForm UGUI 实装（基础布局，美术由 ai-art 出图替换）
- WeaponConfig 扩展 + WeaponTraitConfig 新建（占位数值）
- 6 处现有文件改造
- HeadPartBehavior 改为 CritHitEvent 源（按 ColorParam × PatternParam 概率公式）
- 鼠标地面投影 + SphereCast 半角射击（半角配在 WeaponConfig）
- 元气骑士范式：5 武器 prefab 占位 + 玩家挂载

**不做（推迟到他 change）**：

- ❌ **图案解锁触发器**（首杀 50 人/首杀 Boss 等）→ **#21 meta-progression**；本期仅读 `SaveData.PatternUnlocks`，写入留 #21
- ❌ **战斗中拾取替换武器**（敌死掉 WeaponPickup → 按 E 替换）→ **#18 weapon-pickup**（已重定义）
- ❌ **VFX 签名分化**（每个伤害源独立粒子/颜色/字号）→ **#19 visual-polish**
- ❌ **Trait 效果具体公式**（如 trait_quickslash 后摇减 X%）→ 占位接口，gd-system 在 tasks.md 阶段平衡
- ❌ **召唤物 / 环境陷阱 / 反伤 / 吸血**（明确 brainstorm §5 不做）
- ❌ **暴击外的乘区扩展**（易伤抗性矩阵 / 元素穿透）→ 违反 Pillar C

## DoD

1. **9 条 TC-Damage 全部 PASS**（TC-D1 ~ TC-D9，详见 `tests/min-plan.md`）
2. **新增 5 条 TC**：TC-Crit（仅 Head 装载时出现暴击） / TC-Charge（弓蓄力 0.4s 才有伤） / TC-Mouse（半角 0°/45°/180° 三种行为）/ TC-Startup（颜料/武器/图案三选 UI 进入战斗） / TC-Pickup（占位记录，本期不验，留 #18）
3. **现有 336 TC 全部继续 PASS**（TattooModule.Fire 公式不重写）
4. **可玩闭环**：MainMenu → CharacterSelect → **StartupSelectForm** → Combat → 玩家可死 → 可杀敌 → RunResult
5. **手感指标**：左键按下 → 0.3s 内首位反馈；蓄力 0.4s 后释放可见多段粒子（VFX 留 #19，本期只看是否有事件 + log）
6. **0 Console Error** 退出条件（同 #16/#17 标准）
7. **CONTRACT.md 不被违反**：fan-out 各 agent 不得动事件总表 / 模块公共签名 / DataTable schema / GameApp 注册

## Risk

| 风险 | 影响 | 缓解 |
|---|---|---|
| 6 处改造同时落地 + 3 个新模块，集成风险高 | 高 | 骨架先行（本 change 第一阶段），6+ agent fan-out 各自只填空，编译验证作为门禁 |
| 元气骑士范式：武器 prefab 动画与玩家朝向同步可能有 1-2 帧偏差 | 中 | 本期 prefab 仅占位（无真实动画），动画接入留 #19 polish；玩家朝鼠标转向走 transform.LookAt(aimPoint) |
| HeadPartBehavior 改造影响 336 TC | 高 | TC 中暴击触发已不依赖具体来源（看 magnitude × CritMul），改造保持 `CritHitEvent` 签名不变，公式参数加在 PatternParam 维度 |
| StatusEffectModule tick 性能（50 actor × N status） | 中 | tick 间隔 0.5s 而非 60Hz；本期仅 Burn/Poison 两种；List<ActiveStatus> 用 struct + 预分配 |
| Codex 出图 5 武器 prefab + UI 卡片 ~5 批次，token 预算大 | 中 | 武器 prefab 本期允许"无图灰盒"，仅 UI 卡片必须出图；3 轮重试上限沿用 #17 |
| 蓄力 API 与现有 IsAttackPressed 互斥 | 低 | 新增 `IsAttackHolding()` 不动 `IsAttackPressed()`；CombatModule 同帧只发一种事件（按下时长 < 0.4s 算普攻，≥0.4s 算蓄力） |

## Out-of-scope（明确不在本 change）

- Bot AI 主动开火（BotControllerModule 不动；本期只有人玩可触发武器）
- WeaponPrefabPath 真实美术资源（本期 prefab 可为空 GameObject 占位）
- 中性元素武器 buff（武器中性已定，元素纯由 ColorParam 决定）
- 多人 PvP 互打伤害
- 弓充能 UI 进度条（留 #19）
