## Why

当前 GF_X 业务重构把真人、人机控制档次、参赛身份和怪物层级混在 `TotemActorKind` 中，导致 49 名人机被错误当作 Enemy、Boss 被计入参赛者 Actor、索敌与二次伤害规则互相矛盾，并以“清空所有非真人对象”错误判定胜利。项目需要恢复独立于 50 名参赛者的地图怪物领域，并建立可配置、可扩展、可自动诊断的 NPC 敌人系统。

## What Changes

- **BREAKING** 将战斗实体拆分为参赛者、怪物和服务型 NPC；控制方式、参赛身份、战斗关系、怪物层级与掉落配置不再共用一个枚举。
- **BREAKING** 删除“非真人就是敌人”的判断和 SmartAI=Elite、LightAI=Light 的配置映射；Boss 不再属于参赛者 Actor。
- **BREAKING** 胜负改为 50 名参赛者中最后存活者获胜，普通怪、精英怪和 Boss 的存活数量不参与胜负判定。
- 新增独立 Enemy runtime、基础 AI 状态机、仇恨表、能力组件、LOD 调度、生命周期事件与因果诊断。
- 新增 15 种首轮敌人：通用池 3 种，AI 遗迹、异星巢穴、病毒沼泽各 4 种，包含 8 种 Light、4 种 Elite 和 3 个三阶段 Boss。
- 新增 Encounter 刷新闭环：PCG 产出遭遇锚点，Encounter 根据主题、区域、时间和预算选择敌人池；普通怪分批补充，精英第 4 分钟起有限生成，Boss 第 10 分钟生成一只。
- 新增独立怪物战利品：普通怪、精英和 Boss 使用各自掉落表，战利品立即公开竞争；Boss 配方拾取后立即永久解锁，重复配方转换为高阶颜料。
- 新增每名真人独立的 Loading/Ready/Protected/Active 生命周期；服务器世界不等待所有客户端，Ready 后最多保护 5 秒，90 秒加载超时按掉线淘汰。
- 保留并明确全局参赛者互伤保护：对局前 60 秒 Participant 之间不能互相造成伤害，NPC 敌人仍可攻击 Active Participant；达到 60 秒后 Participant 之间恢复正常伤害。单名真人的 Loading/Protected 保护仍独立生效。
- 更新 HUD、音频、VFX、状态、纹身、武器、经济和诊断对参赛者/怪物语义的消费方式。

## Capabilities

### New Capabilities

- `combat-entity-domain`: 参赛者、怪物、服务型 NPC、控制方式和战斗关系的独立领域契约。
- `enemy-ai-runtime`: 怪物基础状态机、仇恨、目标选择、能力组件、Boss 阶段和性能 LOD。
- `enemy-encounter-spawning`: PCG 遭遇锚点、主题池、刷新预算、普通/精英/Boss 生命周期。
- `enemy-loot-progression`: 公开战利品、怪物掉落表、精英奖励、Boss 配方与永久进度。
- `participant-readiness`: 服务端世界独立开局、每名真人独立 Ready、5 秒保护和 90 秒超时。

### Modified Capabilities

- `player-attack-system`: 玩家攻击目标和敌人攻击目标改用统一战斗关系，不再以非真人或单一玩家硬编码。
- `weapon-pickup`: 精英武器掉落由真实 EnemyTier.Elite 触发，不再以 SmartAI 控制档次代替精英身份。
- `tattoo`: 战斗结束条件改为最后一名参赛者，怪物清空不再触发胜利。
- `main-menu-flow`: 战斗 HUD 打开与本地 ClientReady 分离，并在相机、HUD 和 InputModule 就绪后激活本地参赛者。

## Impact

- 主要影响 `Assets/Game/Scripts/Runtime` 中 Actor、AI、Combat、Boss、Economy、Weapon、Status、Tattoo、Skill、Map、UI、Audio 和 VFX 服务。
- 新增 Enemy/Encounter 运行时模块、模型、事件、快照、配置定义和诊断场景。
- 更新 `GameData/AIData/DataTables/Business` 的 Enemy、Boss、Map 配置，并新增能力、遭遇与掉落配置；同步 xlsx、生成 C# DataTable 和 gameplay catalog。
- 更新至少 12 个依赖 `TotemActorKind`、`IsEnemy`、`aliveEnemyCount`、Boss 或掉落语义的诊断场景。
- 不引入新的第三方运行时依赖；继续使用 Unity 2022.3.62f3、GF_X、UniTask、DOTween 和现有 `TotemAssetService`。
- 最终美术、动画和 UI 视觉精修不作为本变更完成门槛；缺失敌人资源使用可区分且被诊断记录的占位表现。
