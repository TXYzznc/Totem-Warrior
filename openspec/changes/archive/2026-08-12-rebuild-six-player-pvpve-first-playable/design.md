## Context

项目已经迁移到 Unity 2022.3.62f3 + GF_X runtime，并具有 Launch 启动链、运行时服务、Bot、敌人、武器、纹身、缩圈、Boss、UI 和 DataTable 基础，但这些实现仍携带旧产品假设：50 名参赛者、角色选择、五类武器、主动技能、战斗中纹身、旧元素、早期 PvP 保护和未经确认的 Enemy/PVE 方案。最近一次全量诊断为 28 项通过、9 项失败、36 项警告，PlayMode smoke 未形成稳定退出证据，因此必须先恢复验证基线再重构。

本 change 的利益相关者是单一开发者、负责程序与设计协作的 AI，以及独立窗口中的美术生产工作。主目标是尽快看到结构完整的第一阶段效果，而不是在第一次实现中交付完整五轮产品。

约束：所有输入必须经过 `TotemInputService` / `ITotemInputProvider`；不修改 GF_X 框架核心；热路径零 GC 分配；配置继续使用业务 DataTable/runtime catalog；最终美术资源不可成为可玩闭环的前置阻塞。

## Goals / Non-Goals

**Goals:**

- 从主菜单开始，完成“本地对局确认 → 60 秒开局构筑 → 五轮战斗/四次缩圈 → 结果结算 → 返回菜单”的完整可重复流程。
- 6 名参赛者、3 支双人队、Bot 补足、无友伤，从第一轮起允许 PvP。
- 用分轮构筑、全员信息公开和精确本局成果形成可读的反制博弈。
- 用单枪械、P01/P02、三元素、事件队列和三队纯 PVP 验证核心战斗语言。
- 通过合法地图资源锚点刷新颜料，让前三轮保留探索、发育和争夺目标。
- 建立确定性、可诊断、可自动测试的运行时边界，支持固定 seed 和快速模式。
- 为完整五轮、Boss、撤离与线上双人匹配保留清晰扩展点，但不提前实现。
- 用可替换的测试输入验证“权威事件解锁撤离点 → 合法锚点生成 → 整队撤离 → Result”结构；未来 Boss 只替换事件来源。

**Non-Goals:**

- 不实现线上房间、好友、匹配、账号、后端或真正网络同步。
- 实现第 4/5 轮的纯 PVP 状态机；本阶段不实现 Boss 战、Boss 核心、高阶资源和 Boss 失败结算。撤离仅实现本地测试闭环，不包含联机服务器持续结算或战利品经济。
- 不实现多武器、角色选择、玩家主动技能、局外熟练度、永久成长或商业化。
- 不实现任何真正 Enemy、遭遇、EnemyLoot、敌人弱点、敌人 AI 或敌人美术；PVE 设计等待后续独立 change。
- 不生产最终 UI、角色、枪械或 VFX 美术；这些属于 `rebaseline-pvpve-art-resources`。
- 不重写 GF_X 生命周期、资源系统或 `Assets/Game/ScriptsBuiltin/`。

## Decisions

### Decision 1: 采用“可运行切片替换”，不做全量重写

候选方案：A 全量重写 31 个 runtime service；B 在旧流程外再建一套平行游戏；C 保留 GF_X 服务边界并逐条替换业务契约。选择 C。它能复用现有启动、资源、输入和诊断基础，并允许每个里程碑保持可运行。旧行为退出主流程后立即按零引用证据清理，不维护长期双轨；具体 Enemy/PVE 模块不属于保留边界。

### Decision 2: 显式 MatchPhase 协调器拥有时序

候选方案：A 由多个 UI/Service 各自计时；B 用场景 Coroutine 串联；C 由单一阶段协调器发布不可逆阶段转换。选择 C。专用 match-flow service 持有 `FrontEnd / OpeningBuild / Round1Combat / Build2 / Shrink1 / Round2Combat / Build3 / Shrink2 / Round3Combat / Build4 / Shrink3 / Round4Combat / Build5 / Shrink4 / Round5Combat / Result` 权威序列。构筑阶段冻结战斗模拟，UI 使用 unscaled time 展示倒计时；任何服务不得自行跨阶段。

### Decision 3: 暂停由“阶段门控 + MatchClock”实现

不单独依赖 `Time.timeScale = 0`，避免 UI、异步加载、诊断和 DOTween 时序被全局暂停污染。`TotemMatchClockService` 提供阶段时间和 gameplay-suspended 状态；移动、Bot AI、枪械攻击、伤害、元素 tick、地图资源交互和缩圈仅在允许阶段推进。必要时可同时设置物理暂停，但服务门控才是权威。

### Decision 4: 真人与 Bot 共享 Participant/Command 契约

候选方案：A Bot 直接调用内部实现；B Bot 模拟输入；C 人类和 Bot 产生同一层 gameplay command，由服务统一校验。选择 C。输入侧仍只由 InputModule 读取设备；Bot 不伪造设备输入，而是通过受控 AI command provider 驱动同一业务入口。队伍、伤害、统计、资源和倒地规则只实现一次。

### Decision 5: 每次命中建立独立、确定性的效果队列

候选方案：A 事件立即嵌套调用；B 给元素反应设置全局冷却；C 收集本次触发的合法事件后排序并逐个结算。选择 C。排序键为优先级降序；相同优先级用 `matchSeed + resolutionSequence` 派生的随机序稳定洗牌。结算间延迟仅产生表现指令，不改变模拟时间、伤害或后续合法性。第一阶段反应不会生成新事件，避免无限链；无全局反应冷却。

### Decision 6: 元素层持有来源，而不是只在反应时猜归因

目标身上的每一层元素记录元素类型、来源 Participant、施加序号和剩余时间。同元素叠加或强层刷新保持 FIFO 来源队列。异元素触发时，触发者获得击杀归因；触发者和被消耗层来源都记录相同数值的“间接造成 X 点元素伤害”，但实际生命只扣一次。

### Decision 7: 构筑阶段使用边界快照

每次进入构筑阶段先结算未救起倒地玩家，再捕获“进入该阶段前”的 6 人构筑与累计成果快照。对手看到的纹身效果文本来自配置的无数值字段，不暴露精确参数；属性区分基础值与局内强化值；成果数据精确显示。快照在本次构筑阶段内不随他人编辑实时变化，防止无限镜像反制。

### Decision 8: 主玩法与美术通过稳定资源槽解耦

代码只依赖稳定的 UI form/slot ID、sprite/material/VFX key、Prefab 合同和 fallback。新美术未交付时使用程序图形、文字和现有资源占位；美术 change 验收后只替换资源映射和 layout 实现，不改变玩法事件或 DataTable schema。

### Decision 9: 配置最小增量，不把 ScriptableObject 当数据库

复用现有 Business DataTable/runtime catalog，增加或收敛 MatchPhase、Element、Reaction、TattooPattern、MapResource、Weapon 和 UI 文本所需字段。真正 Enemy、Encounter、EnemyLoot 的业务记录和资产从 first-playable 范围删除；Git 历史即为回滚证据，不在活动工作区维持双轨。所有比例使用明确单位和 clamp；事件队列运行时使用 struct 与预分配容器。

### Decision 10: 地图资源使用独立拾取模型

候选方案：A 继续借用 EnemyLoot；B 继续把资源伪装成武器拾取；C 建立轻量 MapResourcePickup 合同并由合法锚点生成。选择 C。权威 `MapResourcePickupConfig` 至少包含稳定 ID、资源类别/资源 ID、元素、最小/最大数量、权重、可出现轮次、资源键和启用状态；不同记录可使用不同数量区间，同一记录在其小范围内确定性随机。同一 match seed、轮次和锚点集合必须产生相同结果。拾取归属个人，不自动与队友共享，转移继续走已定义的请求/批准原子事务。

### Decision 11: 纯 PVP 结算由权威 MatchFlow 同步收口

任一时刻仅剩一支存活队伍时立即结束比赛，并同时停止 MatchFlow，防止结果界面出现后阶段仍推进。第五轮结束仍有多队时，按队伍淘汰数、玩家伤害、存活人数、剩余生命依次比较；全部相同则平局。淘汰跨轮保持，构筑阶段不复活玩家。

### Decision 12: 撤离解锁入口与事件来源解耦

撤离服务提供一次性的权威 `TryUnlock` 入口。测试版本只允许从 `Round4Combat` 起由 InputModule 产生的 `Shift + Space` 命令调用；更早输入、重复输入或直接读取设备均被拒绝。未来 Boss 被击败时调用同一入口，不修改撤离点生成、交互或结果结算。解锁后从专用 `Extraction` 合法锚点按 match seed 确定性抽取配置数量，第一版默认 3 个，生成后不移动。

### Decision 13: 本地整队撤离立即结束对局

第一版不做后端和真实联机。仅本地真人可发起撤离交互：存活且未倒地的本地玩家在撤离范围内持续按住 `F` 3 秒，松开、离开范围、倒地或受到有效伤害时中断；完成后本地玩家与其仍未淘汰的 Bot 队友一并标记为撤离，并立即进入 `Result`。已淘汰队友不复活；倒地队友存在时不允许开始整队撤离。未来联机版本将“结束本地对局”替换为“该队离场，服务器继续模拟”。

### Decision 14: OasisCity 成为唯一权威地图，退役运行时 PCG 双轨

候选方案：A 直接删除 `Assets/Resources/PCG` 并保留当前回退地图；B 在已搭建的 OasisCity 上继续叠加 PCG 逻辑与可视化；C 先加载 OasisCity，再从场景中的显式玩家出生、资源和撤离锚点构建运行时地图快照。选择 C。A 会让当前动态 `Resources.Load` 链断裂并继续运行空 `TotemGame`；B 会维护两套空间真相，且旧 PCG 图集已经引用清理掉的 2D AiRuins 素材；C 让已确认的 3D 场景直接成为玩法空间，同时保留 seed 只负责“从合法锚点中确定性抽取”的既有规则。

加载顺序固定为 `Launch -> additive OasisCity -> 设置 OasisCity 为活动场景 -> 构建场景地图快照 -> EnterCombatHud -> 生成参与者`。返回主菜单时恢复 Launch 为活动场景、卸载 OasisCity 并清空地图运行态。场景合同使用显式 authoring 组件区分 `PlayerSpawn`、`MapResource` 与 `Extraction`，三类锚点不得相互冒充；资源/撤离运行时实例仍由既有服务按配置和 seed 生成。迁移完成且零引用扫描通过后，删除空 `TotemGame`、`Assets/Resources/PCG`、`Runtime/PCGMap` 及其专用测试/诊断。

## Risks / Trade-offs

- [现有服务与旧 50 人假设耦合] → 先做依赖/配置清单和失败诊断修复，再切 roster；每个里程碑保留 smoke。
- [构筑暂停遗漏某个系统] → 建立阶段门控契约测试，覆盖 Bot AI、移动、投射物、元素、地图资源交互、缩圈与统计。
- [效果队列在同帧重入或产生 GC] → 使用单次 resolution context、预分配队列和深度保护；第一阶段反应为终止节点。
- [Bot 无法正确使用构筑/救援/资源请求] → 第一阶段只要求确定性基础策略，不追求拟人；同一 command 校验保证规则一致。
- [公开信息过载] → 只在构筑阶段显示完整 6 人面板，战斗 HUD 保留即时必要信息；无数值效果文本由配置维护。
- [旧 OpenSpec 与新规格冲突] → 本 change 明确移除/修改旧 requirement；归档时同步主 spec，旧 change 保留历史证据。
- [美术窗口与程序窗口产生接口漂移] → 两个 change 都引用相同资源 ID/路径契约；玩法 tasks 不修改美术源文件，美术 tasks 不修改 C#。

## Migration Plan

1. 保存当前全量诊断、PlayMode 卡住位置、配置和场景锚点基线；先修复阻断 smoke 的问题。
2. 建立新 roster/team/phase 数据合同，并让 Launch 直接进入新流程；回滚依赖 Git 历史，不在活动运行时保留旧流程开关。
3. 将主菜单改为本地确认并接入 6 人/3 队生成；用占位 UI 完成五轮状态机。
4. 迁移单枪械、纹身构筑、事件队列、元素、倒地救援、地图资源和统计，每步增加纯逻辑/EditMode 测试。
5. 删除 Enemy、Encounter、EnemyLoot 运行时模块及其配置、测试、诊断、资源和所有跨服务分支，以编译和零引用扫描为门槛。
6. 接入构筑情报、颜料请求、HUD、结果和开发控制；运行固定 seed PlayMode smoke。
7. 接入测试撤离事件、专用锚点、整队撤离结算与自动化 Gate。
8. 完成全量诊断、性能/GC 采样和人工 playtest；关闭旧主流程 feature flag。

回滚：依靠 Git 历史恢复被删除的旧实现；活动工作区不保留旧配置与入口映射双轨。若某里程碑失败，只回退该服务的注册/配置，不回滚 GF_X 框架或已验证的数据迁移。

## Open Questions

- 无阻塞问题。PVE/Enemy 的玩法与内容、Boss 具体能力、高阶资源、正式撤离经济、线上网络模型、单枪械最终数值和最终 UI 视觉均为后续 change 或美术 change 决策，不影响本地 first-playable。
