# First Playable feature-slice 验收矩阵

| Slice | 核心规格 | 权威运行时/服务 | 配置与资源契约 | 最低 UI 证据 | 自动化与诊断证据 |
|---|---|---|---|---|---|
| 启动与闭环 | MainMenu -> LocalMatchConfirm -> OpeningBuild -> CombatHUD -> Result -> MainMenu | GameFlow、UIService、Runtime cleanup | UI form/slot key；占位资源允许 fallback | 开始、确认、三轮结果、返回 | Launch 端到端 PlayMode smoke；退出后零残留 |
| 6 人双排 | 6 participants、3 teams、1 human + 5 Bot、同队相邻合法锚点 | roster/team/spawn coordinator | 参与者/队伍/锚点不可变合同 | 队伍和存活信息 | 固定 seed EditMode；6 人三队 PlayMode/诊断 |
| 三轮节奏 | 60 秒开局构筑；45 秒第 2/3 构筑；三轮战斗；两次缩圈；随后结果 | MatchPhase coordinator、MatchClock、zone | normal 180/30；fast 60/10；禁用 R4/R5/Boss/撤离 | 阶段、倒计时、缩圈反馈 | 状态转换/暂停 EditMode；三轮自动 smoke |
| 单枪械战斗 | 单一 active 枪械、身体/弱点命中、有效直接伤害后触发枪械臂 | weapon/combat/damage/effect queue | 单枪械、hitbox/weak-point、priority | 准星、弹药/状态、命中反馈 | 命中/友伤/弱点/队列顺序测试；输入静态审计 |
| 纹身构筑 | 六部位、P01/P02、火冰雷；战斗只读；10 消耗、6 返还 | Tattoo、inventory、phase gate | 图案无精确数值公开文本；纹身视觉 key | 六部位、两图案、三颜料、成本/返还/Ready | 收支、替换、非法阶段、清理 EditMode/诊断 |
| 元素与反应 | 弱/标准/强；3 秒逐层；火 0.5 秒 tick；冰 12/20/28%；雷 0.5 秒；三反应 | element state、reaction resolver、effect queue、attribution | 元素/反应/表现延迟配置；无全局冷却 | 层级、反应反馈、间接伤害成果 | 排列顺序、FIFO、归因、暂停、零延迟等价测试 |
| 倒地救援 | 40% 倒地生命、20 秒、35% 移速、3 秒救援、构筑边界淘汰、观战 | lifecycle/revive/elimination/spectate | 状态与中断原因 | 倒地、救援、淘汰、观战反馈 | 边界/中断/保护/整队淘汰测试与诊断 |
| 第一阶段 PVE | 三种敌人职责、三轮递增压力、弱点提示、基础资源掉落 | encounter/enemy/loot | 近战/远程/护盾；合法 encounter anchors；弱点 visual key | 敌人/弱点/掉落可读 | 固定 seed 遭遇测试；EnemyReadiness smoke；对象池清理 |
| 情报与颜料请求 | 构筑边界冻结 6 人快照、精确本局成果、队友单向颜料请求与原子转移 | snapshot/stats/trade | 成果字段、公开文本、请求 DTO | 6 人情报、成果、请求/同意/拒绝 | 冻结、隐私、并发库存、断言与诊断 |
| 性能与证据 | 结果保存 seed/模式/阶段耗时/队伍/构筑/成果/异常/配置版本；常规帧零 GC | evidence recorder、pooled buffers | 可比较 evidence schema | 开发结果摘要 | EditMode 全量、端到端 smoke、GF_X 全诊断、120 秒 Profiler |

每个 slice 只有在“规格、运行时、配置、UI、测试/诊断”五列均有可追踪证据时才算完成；最终美术不作为功能闭环阻塞，缺失资源必须通过稳定 key 和 fallback 明示。

