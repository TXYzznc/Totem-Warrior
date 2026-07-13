## Context

GF_X 当前运行时以 `TotemActorModel` 和 `TotemActorKind { Player, SmartAi, LightAi, Boss }` 同时表达控制方式、参赛身份、怪物层级和敌我关系。`TotemActorService.IsEnemy` 将所有非真人对象判为敌人，而其它代码又把 SmartAI、LightAI 视为参赛者。结果包括：49 名人机复用 EnemyConfig、Boss 混入参赛者列表、索敌和武器二次伤害规则不一致、所有死亡对象复用死亡箱、Boss 奖励延迟到整局胜利、清空非真人对象即获胜。

当前地图使用 400m PCG 运行时和固定类型锚点；`EnemySpawn` 锚点仅携带 `inner/mid/outer`，没有主题池、遭遇预算或怪物配置外键。现有 EnemyConfig 只有 3 行，并被错误用于 SmartAI/LightAI/Boss。普通和精英怪没有活动 Prefab；3 张 Boss 立绘与旧 Boss1 动画可作为占位素材。实现必须基于 Unity 2022.3.62f3、GF_X runtime、现有 DataTable/JSON 工作流和 `TotemAssetService`，不得引入新的第三方运行时依赖。

已确认业务规则：

- 50 名参赛者由 1 名真人、20 名智能人机、29 名轻量人机构成，身份和权限一致。
- NPC 敌人独立于 50 人，对所有已激活参赛者按相同仇恨规则索敌。
- 最后一名参赛者获胜；怪物是否存活不影响胜负。
- 普通怪分批补充；精英从第 4 分钟开始有限生成且不重生；Boss 第 10 分钟生成一只且不重生。
- 战利品立即公开竞争，不设击杀者独占期。
- 首轮实现 8 种 Light、4 种 Elite、3 个 Boss；其中 common 池为 2 Light + 1 Elite，每个主题池为 2 Light + 1 Elite + 1 Boss。
- 服务端世界不等待全部客户端；每名真人独立 Ready，Ready 后最多保护 5 秒，90 秒未 Ready 按掉线淘汰。

> **当前用户确认覆盖旧决策（2026-07-10）**：50 名参赛者在对局开始后的前 60 秒内不能互相造成伤害；NPC 敌人仍可按正常关系规则攻击参赛者。60 秒后参赛者之间恢复正常伤害。该规则是全局对局时间规则，和单名真人的 Loading/Protected 就绪保护并存；Loading 或 Protected 目标仍然不能被攻击。

## Goals / Non-Goals

**Goals:**

- 让参赛者、怪物和服务型 NPC 在模型、配置、生命周期、AI、掉落和统计上彻底分离。
- 提供一套无每帧 GC 的 Enemy 基础状态机、仇恨模型、能力组件和少量专属子类。
- 用 PCG 遭遇锚点、主题池和配置预算生成可重复、可诊断的怪物 SpawnPlan。
- 完成 15 种首轮敌人的逻辑、数值入口、占位表现、掉落和 Boss 三阶段。
- 统一所有直接、状态、纹身、武器特性和环境伤害的关系判断。
- 让每个需求有 EditMode 诊断证据，并以 PlayMode smoke 验证实际场景闭环。

**Non-Goals:**

- 本变更不实现真实网络传输、房间匹配、断线重连或服务端部署；只提供网络可接入的 Ready 状态契约和本地实现。
- 不完成 15 种敌人的最终美术、全套帧动画、终版 VFX/BGM 或 UI 视觉精修。
- 不在本变更进行最终战斗手感和平衡调优；所有初始数值均配置化并接受后续调参。
- 不重写 PCG 地形算法、纹身公式、武器核心公式或商人/纹身师业务。

## Decisions

### 1. 使用独立领域模型，不扩展 TotemActorKind

候选方案：

| 方案 | 优点 | 缺点 |
|---|---|---|
| A. 继续扩展 `TotemActorKind` | 改动少 | 控制方式、身份和怪物层级继续耦合，无法正确表达关系 |
| B. 单一 Actor 加多个 bool/枚举 | 迁移较平滑 | 无效字段多，服务仍容易误用，编译期无法阻止污染 |
| C. `TotemCombatantModel` 基类 + Participant/Enemy 子模型 | 语义清晰，可共享生命/位置/表现，类型约束强 | 跨服务迁移范围最大 |

选择 C。模型结构如下：

```text
TotemCombatantModel
  - CombatantId / Position / Health / MaxHealth / GameObject / IsAlive
  - Domain: Participant | Enemy

TotemParticipantModel : TotemCombatantModel
  - ParticipantId
  - ControllerKind: Human | SmartBot | LightBot
  - Lifecycle: Loading | Protected | Active | Eliminated | Disconnected
  - Loadout / inventory / personality / run stats

TotemEnemyModel : TotemCombatantModel
  - EnemyId / ThemeId / Tier: Light | Elite | Boss
  - BehaviorProfileId / AbilityIds / LootTableId / SpawnContext
  - Threat / leash / phase / encounter ownership

TotemServiceNpcModel
  - Merchant | Tattooist
  - 非 Combatant，不进入伤害与胜负系统
```

`TotemActorService` 收敛为参赛者生命周期与查询服务；新增 `TotemEnemyService` 负责怪物注册、生成、死亡和快照。共享伤害入口迁移到 `TotemCombatService`，避免两个领域各自扣血。

### 2. 用中心化关系策略替代 IsEnemy

候选方案：

| 方案 | 优点 | 缺点 |
|---|---|---|
| A. 各服务自行判断 | 局部简单 | 当前矛盾的根源，无法审计 |
| B. 仅依赖 Physics Layer | 命中筛选快 | 不能表达 Loading、Protected、技能例外 |
| C. `TotemCombatRelationshipService` 统一决策 | 规则唯一、可纯函数测试 | 所有伤害调用必须迁移 |

选择 C。所有伤害、索敌、连锁、范围和状态传播必须先调用统一策略。默认矩阵：

| Source | Target | 规则 |
|---|---|---|
| Participant | Participant | 同一实体禁止；对局时间小于 60 秒时禁止互伤；达到 60 秒且双方 Active/Alive 后允许 |
| Participant | Enemy | 双方 Active/Alive 时允许 |
| Enemy | Participant | Target 为 Active 且非 Protected 时允许 |
| Enemy | Enemy | 默认禁止；能力配置显式 `CanHitEnemies=true` 时允许 |
| World/ShrinkZone | Participant | 按环境规则允许；Loading/Disconnected 禁止 |
| World terrain hazard | Enemy | 仅显式配置为影响怪物时允许，默认禁止 |

关系决策返回结构化原因码，如 `Blocked.ParticipantLoading`、`Blocked.ParticipantProtected`、`Blocked.ParticipantCombatGracePeriod`、`Allowed.ParticipantToParticipant`、`Allowed.EnemyAttack`，写入 GFTrace 和伤害快照。

### 3. Enemy AI 采用 FSM + 能力组合 + 薄子类

候选方案：

| 方案 | 优点 | 缺点 |
|---|---|---|
| A. 每种敌人一个完整子类 | 直观 | 15 套重复状态机，维护和测试成本高 |
| B. 引入行为树库 | 复杂行为表达强 | 新依赖、编辑器资产和学习成本超出当前需要 |
| C. 基础 FSM + 数据驱动能力 + 少量生命周期子类 | 复用充分、可测试、扩展成本稳定 | 需要先定义稳定能力契约 |

选择 C。`TotemEnemyControllerBase` 持有固定状态机：

```text
Dormant -> Spawn -> Idle/Patrol -> Alert -> Chase
Chase -> AttackWindup/Cast -> AttackActive -> Recover -> Chase
Chase -> Return (lost target or leash exceeded) -> Patrol
AnyLiving -> Stagger -> previous valid state
AnyLiving -> Dead -> Despawn
```

基础控制器负责感知、目标选择、移动、脱战、状态转换、能力评分和诊断。Tier 薄子类只改变策略：

- `TotemLightEnemyController`: 单目标、低频决策、最多 1 个主动能力。
- `TotemEliteEnemyController`: 多能力评分、低血策略、群体警戒。
- `TotemBossEnemyController`: 阶段表、全局标记、阶段事件和不可降为 Dormant。
- 分裂、永久召唤等改变生命周期的 Boss 可再使用专属薄子类；普通数值差异不得创建子类。

`ITotemEnemyAbility` 使用 `CanStart/Score/Begin/Tick/Cancel` 契约。首轮通用能力组件包括：Melee、Projectile、Charge、Leap、Beam、ConeSweep、AreaPulse、HazardZone、Shield、Summon、Regenerate、DeathBurst 和 PhaseTransition。技能执行复用现有伤害、状态、VFX、音频服务，但不复用参赛者 `SkillConfig`。

### 4. 仇恨对所有参赛者同权，不固定真人优先

每个 Enemy 维护固定容量、可复用的 Threat 条目，不在 Tick 中分配集合。候选目标必须是 Active、Alive、可达且在感知/技能允许范围内的 Participant。

```text
Threat = DamageThreat
       + ProximityThreat
       + RecentAttackerBonus
       + AbilityTargetModifier
```

- 受到伤害立即增加对应来源仇恨。
- 距离只提供有限基础仇恨，不覆盖大量伤害贡献。
- 新目标 Threat 必须达到当前目标的 1.25 倍才切换，避免抖动。
- Boss 范围能力可按最近 3 名或最高仇恨若干名参赛者选择，不改变长期主目标。
- Loading、Protected、Eliminated、Disconnected 参赛者不进入候选集。

### 5. PCG 只产出地形和遭遇锚点，Encounter 负责刷怪

候选方案：

| 方案 | 优点 | 缺点 |
|---|---|---|
| A. PCG 直接实例化敌人 | 一步完成 | 地图算法与战斗生命周期强耦合，无法独立测试 |
| B. EnemyService 使用固定坐标 | 简单 | 不适应主题、seed、可达性和区域变化 |
| C. PCG anchor -> SpawnPlan -> EncounterDirector -> EnemyService | 职责清晰、决定性强、可离线验证 | 增加一个计划层 |

选择 C。`TotemEncounterService.BuildSpawnPlan` 是纯数据接口，输入 MapSnapshot、主题、EncounterConfig、EnemyConfig 和 seed，输出包含 EnemyId、位置、波次、触发时间和锚点引用的计划。

默认可调参数：

- 普通怪开局 18 只；Active 上限 30；每 45 秒评估一次，以 4-6 只波次补充，禁止原地立即重生。
- 普通怪总生成上限 60，避免无限拖局和资源膨胀。
- 精英第 240 秒起生成，总数由地图配置在 5-8 之间；死亡不补位。
- Boss 第 600 秒在 Boss anchor 生成一只；死亡不重生。
- Spawn 必须可达、与任意 Active Participant 保持最短距离，并避开当前屏幕/相机可视核心区。
- 世界时钟和刷新不等待客户端 Ready。

### 6. 15 种敌人由 13 个通用能力组合

| 池 | EnemyId | Tier | 核心能力 |
|---|---|---|---|
| common | `enemy_common_hunter` | Light | 群体警戒、Melee |
| common | `enemy_common_shooter` | Light | Projectile、后撤 |
| common | `enemy_common_guardian` | Elite | Shield、AreaPulse、群体强化 |
| ai_ruins | `enemy_ai_servo` | Light | Melee、电击、呼叫同伴 |
| ai_ruins | `enemy_ai_arc_drone` | Light | Beam、侧移 |
| ai_ruins | `enemy_ai_manager` | Elite | Shield、EMP AreaPulse、Summon |
| ai_ruins | `boss_ai_core_zero` | Boss | AreaPulse、Beam、Summon、过载 HazardZone |
| alien_hive | `enemy_alien_crawler` | Light | Leap、Melee、侧绕 |
| alien_hive | `enemy_alien_spitter` | Light | Projectile、HazardZone、后撤 |
| alien_hive | `enemy_alien_guard` | Elite | ConeSweep、Summon、HazardZone |
| alien_hive | `boss_alien_hive_mother` | Boss | ConeSweep、Summon、HazardZone、狂暴 |
| virus_swamp | `enemy_virus_mutant` | Light | Charge、低血狂暴 |
| virus_swamp | `enemy_virus_spore_carrier` | Light | Projectile、DeathBurst、HazardZone |
| virus_swamp | `enemy_virus_spore_host` | Elite | AreaPulse、可打断 Regenerate、Summon |
| virus_swamp | `boss_virus_terminus` | Boss | Charge、HazardZone、分裂 Summon、Regenerate |

Boss 均使用 60%/30% HP 三阶段模板。阶段转换最多 1 秒，不锁定镜头，发布 BossPhaseChanged 供 UI、音频和 VFX 消费。

### 7. 怪物掉落与参赛者死亡箱完全分开

`TotemEnemyService` 在死亡时发布结构化 `TotemEnemyDiedEvent`；`TotemEnemyLootService` 根据 seed、LootTable 和 GuaranteedLoot 生成 `TotemLootPickupModel`。`TotemEconomyService` 只负责将拾取物写入 Participant inventory。

- 普通怪：金币必掉，小概率普通物资。
- 精英：1 份稀有颜料必掉，附加金币和加权武器/装备。
- Boss：主题配方 1 张、颜料 2-3 份和金币。
- 所有掉落无 OwnerId，生成后立即允许任意 Active Participant 拾取。
- 配方拾取时立即写入该 Participant 的 profile；真人 profile 持久化，人机 profile 只在本局存在，权限和流程一致。
- 重复主题配方转换为 2 份高阶颜料。
- 参赛者死亡箱仍继承其库存，不使用 Enemy LootTable。

### 8. 服务端世界与每名真人 Ready 分离

候选方案：

| 方案 | 优点 | 缺点 |
|---|---|---|
| A. 等所有客户端加载 | 公平同步 | 慢客户端拖住全部玩家，已被用户否决 |
| B. 世界立即开始且客户端无保护 | 最简单 | 慢客户端可能在可操作前死亡 |
| C. 世界独立启动 + 每 Participant 独立 Ready/保护 | 快玩家无需等待，慢玩家不会无操作死亡 | 需要状态和超时防滥用 |

选择 C。世界 PCG、Encounter、AI 和缩圈使用服务端/权威世界时钟。每名真人状态为：

```text
Reserved -> Loading -> Protected -> Active -> Eliminated/Disconnected
```

- Loading 时保留名额和出生点，不生成可碰撞实体，不能索敌、受伤、输出、移动、交互或拾取。
- 客户端在场景、HUD、相机和 InputModule 均就绪并完成至少一个渲染帧后提交 Ready。
- Protected 最长 5 秒；任何经 InputModule 观察到的移动、攻击、技能或交互意图立即结束保护。
- Protected 不能造成伤害，NPC 敌人和参赛者不能索敌或伤害该对象。
- 90 秒仍未 Ready 时转为 Disconnected，不再计入存活参赛者。
- 当前单机实现使用本地 Readiness provider；未来网络层只替换 Ready 命令来源，不改业务状态机。

### 9. 胜负只统计 Participant

`aliveParticipantCount` 取代 `aliveEnemyCount` 作为 Run 结束依据。规则：

- 真人被淘汰时对本地玩家立即判负，但世界模拟可按测试/观战需要继续。
- 任意时刻只剩 1 名非 Loading、非 Disconnected、Alive Participant 时，该 Participant 获胜。
- Loading 在 90 秒内视为存活名额，超时后移除；Protected 视为存活。
- Enemy 数量只进入 HUD 怪物压力和诊断快照，不参与胜负。

### 10. 性能采用分层决策和按需寻路

- 移动插值可每帧执行，但 AI 决策不得全部每帧运行。
- Hot（距任意 Active Participant <=20m）：Light 5Hz、Elite 10Hz、Boss 20Hz。
- Warm（20-60m）：Light 2Hz、Elite 4Hz、Boss 10Hz。
- Cold（>60m）：Light 0.5Hz、Elite 1Hz、Boss 5Hz；无目标时冻结寻路。
- LOD 以最近的任意 Active Participant 为基准，不固定真人。
- 默认使用现有 Walkable grid 和轴向滑移。连续 0.75 秒无进展时才请求有节点上限的 A*；路径缓存到目标 cell 变化或 1 秒过期。
- Enemy update CPU 预算目标为桌面端平均 <=2.3ms/帧，单帧 GC alloc 为 0 B；诊断记录峰值决策数、A* 请求数和各 LOD 数量。

### 11. 数据表与资源入口

保留并扩展 `EnemyConfig`，新增 `EnemyAbilityConfig`、`EncounterSpawnConfig` 和 `EnemyLootConfig`。`BossPhaseConfig` 扩展到三个 Boss。核心字段：

```text
EnemyConfig:
EnemyId, DisplayName, ThemeId, Tier, RuntimeAssetKey,
BehaviorProfileId, AbilityIds, BaseHP, HPCurveK, BaseDamage,
DamageCurveK, MoveSpeed, AttackRange, DetectRange, LeashRange,
LootTableId, GuaranteedLootIds, SpawnCost, PoolIds

EnemyAbilityConfig:
AbilityId, AbilityType, Range, Radius, Cooldown, Windup,
Active, Recovery, DamageMultiplier, StatusId, StatusChance,
SummonEnemyId, SummonCount, VfxId, AudioCueId, ParametersJson

EncounterSpawnConfig:
EncounterId, ThemeId, ZoneRoles, EnemyPoolIds, StartTime,
EndTime, InitialCount, ActiveCap, TotalCap, WaveMin, WaveMax,
WaveInterval, MinParticipantDistance, MinSpacing, Weight, Unique

EnemyLootConfig:
LootTableId, ItemId, RewardType, MinCount, MaxCount,
Weight, Guaranteed, TierFilter, ThemeId
```

Business JSON 是 AI 编辑源，xlsx 是策划源，现有 JSON 逆向导表流程负责同步。运行时只读取生成后的 gameplay catalog；资源加载统一走 `TotemAssetService`。缺失资源使用按 Tier/Theme 可区分的占位 Sprite/颜色，并记录 fallback，不允许静默缺失。

### 12. 可观测性和验证

新增 GFTrace 因果事件：Enemy.Spawned、Enemy.StateChanged、Enemy.TargetChanged、Enemy.AbilityStarted、Enemy.DamageResolved、Enemy.Died、Enemy.LootSpawned、Encounter.WaveScheduled、Encounter.WaveSpawned、Participant.Ready、Participant.ProtectionReleased、Run.WinnerResolved。每条包含 worldTime、entityId、source/target、reason 和关键配置 ID。

诊断分层：

- 纯数据：15 种内容、外键、主题池、Boss 阶段、资源键和掉落概率。
- 纯逻辑：关系矩阵、Ready/Protected/超时、仇恨切换、FSM、能力时序、胜负。
- 集成：PCG SpawnPlan 决定性、可达性、波次、掉落拾取、配方持久化。
- PlayMode smoke：Launch -> CombatHUD -> Ready -> 普通怪交战 -> 精英掉落 -> Boss 三阶段 -> 参赛者胜负；快速 PCG 模式用于日常测试，完整 PCG 用于最终验收。

## Risks / Trade-offs

- [Risk] `TotemActorModel` 被大量服务引用，类型拆分可能产生长时间编译红区。 -> 先建立共享模型/事件/关系接口，再按 Actor/Enemy/Combat/消费者顺序迁移，每个阶段保持可编译。
- [Risk] 当前工作树含未提交的启动保护修改。 -> 将其视为用户现有工作，迁移时吸收到 ParticipantReadiness，不回退或覆盖无关改动。
- [Risk] 15 种敌人缺少最终 Prefab 和动画。 -> 逻辑与资产键解耦，使用可区分占位资源并由资源诊断明确列出待替换项。
- [Risk] 多名参赛者对应多目标会增加仇恨与寻路成本。 -> 固定容量 Threat、LOD 降频、停滞后才 A*、路径缓存和每帧请求预算。
- [Risk] 公开 Boss 掉落可能导致抢取挫败。 -> 这是已确认的 BR 竞争规则；通过明显掉落表现和 AI 拾取诊断保证规则透明。
- [Risk] Loading 名额在 90 秒内可能延迟胜负。 -> 使用权威超时；超时后确定性转 Disconnected 并重新评估胜负。
- [Risk] 旧 OpenSpec 中“清空敌人获胜”和旧模块接口与新规则冲突。 -> 本变更提供完整 delta spec，并在完成后同步主 specs 和项目总结。

## Migration Plan

1. 创建新领域模型、关系策略、事件和纯逻辑测试，不接入运行时。
2. 将 50 名参赛者从 EnemyConfig 解耦，迁移 Participant AI 和 Ready/Protected 规则。
3. 新增 Enemy/Encounter/Loot 服务和配置 catalog，先接通 3 种通用敌人。
4. 接入三主题 12 种敌人、Boss 阶段和 PCG SpawnPlan。
5. 迁移 Combat、Weapon、Tattoo、Status、Economy、Audio、VFX、HUD 消费者，删除旧 `IsEnemy` 和 Boss Actor 路径。
6. 更新 DataTable JSON/xlsx/C# 产物、资源 catalog、诊断和文档。
7. 运行编译、全量 EditMode 诊断、快速 PlayMode、完整 PCG PlayMode 和 completion audit。

回滚时以功能切片提交为单位反向撤销；不保留双运行时兼容开关，避免旧模型重新污染活动代码。

## Open Questions

无阻塞问题。数值、掉落权重、具体动画和视觉表现按配置与后续 playtest 调整，不改变本设计的领域契约。
