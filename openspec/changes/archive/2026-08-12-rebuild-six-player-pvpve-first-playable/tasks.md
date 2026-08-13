## 1. 六人双排与纯 PVP 边界

- [x] 1.1 收敛为 6 名参赛者、3 支双人队、1 真人 + 5 Bot 补位
- [x] 1.2 同队无友伤，敌队从第一轮起可互相伤害
- [x] 1.3 从运行时、配置、测试、诊断和资源入口删除 Enemy、Encounter、EnemyLoot、Boss
- [x] 1.4 合法出生锚点随机抽取，同队相邻生成
- [x] 1.5 完成 20 局快速模式 Bot 稳定性验证

## 2. 五轮构筑/战斗状态机

- [x] 2.1 实现 OpeningBuild + Round1～5 + Build2～5 + Result 权威状态机
- [x] 2.2 实现 60 秒开局构筑、其余 45 秒构筑、正常/快速战斗计时
- [x] 2.3 实现 Round2～5 开始时的四次动态缩圈
- [x] 2.4 将四次目标半径、圈外伤害和持续时间写入 `ZoneShrinkConfig` JSON/XLSX/runtime catalog
- [x] 2.5 构筑阶段暂停世界模拟和元素计时
- [x] 2.6 倒地玩家在进入构筑边界时淘汰，淘汰跨轮保持
- [x] 2.7 Gate：固定 seed 自动完成五轮、四次缩圈并进入 Result

## 3. 单步枪与确定性效果队列

- [x] 3.1 真人与 Bot 共用瞄准、开火、命中和伤害入口
- [x] 3.2 只启用 `rifle_patrol_v1`，玩家无主动技能
- [x] 3.3 身体/头部弱点、有效直接伤害触发枪械臂
- [x] 3.4 事件按优先级入队，同优先级按 seed 洗牌并逐项结算
- [x] 3.5 移除旧多武器掉落、升级、弹体和武器词条运行链路
- [x] 3.6 将 `WeaponConfig` 权威表物理收敛为单步枪，并删除 `ProjectileConfig`/`WeaponTraitConfig`

## 4. 构筑、颜料与信息公开

- [x] 4.1 六部位、P01/P02、火/冰/雷、装备消耗 10、拆除返还 6
- [x] 4.2 只有 OpeningBuild、Build2～5 可修改，战斗阶段只读
- [x] 4.3 地图拾取物按种类配置 MinAmount/MaxAmount/Weight/轮次
- [x] 4.4 小型 4～6、中型 8～12、大型 16～20；同类数量确定性随机
- [x] 4.5 构筑开始公开六人纹身效果文本、基础/局内属性和本局累计成果
- [x] 4.6 队友颜料请求、同意、拒绝、过期和原子转移
- [x] 4.7 删除旧 SelfTattoo、TattooEnchant、ReadingTime 运行链路、UI 与配置表

## 5. 元素、倒地与结算

- [x] 5.1 弱/标准/强三层元素、持续时间刷新和逐层衰减
- [x] 5.2 火焰 0.5 秒 tick、冰减速、雷放电与三种反应
- [x] 5.3 反应伤害多方成果记录、触发者击杀归因和间接伤害文本
- [x] 5.4 倒地、救援、流血、处决、保护、观战和整队淘汰
- [x] 5.5 第五轮超时按淘汰、玩家伤害、存活人数、剩余生命结算
- [x] 5.6 想法库保留“反应产生新事件/反应链”，第一版不实现

## 6. 主菜单、HUD 与旧内容清理

- [x] 6.1 主菜单直接进入本地对局确认，不进入角色/武器选择
- [x] 6.2 删除 Shop、ThreeChoice、TattooStudio 服务、脚本和预制体
- [x] 6.3 MainMenu、CombatHUD、PauseMenu、RunResult、Settings 保留稳定入口
- [x] 6.4 删除 CharacterSelect、StartupSelect、SelfTattoo、TattooEnchant 的剩余脚本、预制体、枚举和资源
- [x] 6.5 删除 NPC 商人/纹身师二进制素材和空目录
- [x] 6.6 清理 gameplay catalog 中旧 Shop/NPC/Event/Skill/Choice 类型和默认数据
- [x] 6.7 清理旧 UI/技能/武器/NPC runtime asset key 与未使用素材

## 7. 验收与交付

- [x] 7.1 Hotfix、EditMode、PlayMode 程序集编译为 0 错误
- [x] 7.2 GF_X 全量诊断已收敛至重构范围全部通过；剩余工作区通用项单独记录
- [x] 7.3 在 UnitySkills Bypass 下执行完整 EditMode
- [x] 7.4 在 UnitySkills Bypass 下执行 Launch→菜单→五轮→结果→菜单 PlayMode smoke
- [x] 7.5 更新五轮结果证据、诊断报告和固定 seed 回放
- [x] 7.6 清理 `Assets/Screenshots` 与 `Assets/Resources/Font` 或建立明确保留契约
- [x] 7.7 运行 `openspec validate` 和 change verify
- [x] 7.8 对照需求进行最终逐项完成审计

## 8. 测试版整队撤离切片

- [x] 8.1 定义 `Extraction` 专用合法锚点、配置化生成数量和确定性抽取合同
- [x] 8.2 通过 InputModule 增加 `Shift + Space` 测试解锁命令，保留空格闪避且限制 Round4Combat 起整局一次
- [x] 8.3 实现撤离解锁事件、默认 3 个撤离点生成、运行时可视占位和退出清理
- [x] 8.4 实现本地玩家按住 `F` 3 秒交互、中断规则、倒地队友拒绝和整队撤离状态
- [x] 8.5 本地整队撤离后立即停止 MatchFlow、生成撤离成功结果并返回既有 Result 流程
- [x] 8.6 增加确定性 EditMode、GF_X 诊断和 Launch PlayMode 撤离 smoke
- [x] 8.7 运行全量诊断、PlayMode 回归、OpenSpec strict validate 并更新证据

## 9. OasisCity 场景权威地图与 PCG 退役

- [x] 9.1 固化当前 PCG 动态引用、OasisCity 场景层级、构建场景与测试基线
- [x] 9.2 为 OasisCity 建立显式 PlayerSpawn / MapResource / Extraction 场景锚点合同与校验
- [x] 9.3 将 gameplay loader 改为先加载 OasisCity、后进入 CombatHUD，并在返回菜单时安全卸载
- [x] 9.4 将 TotemMapService 改为从已加载场景构建地图快照，移除程序化 PCG 地形与可视化生成
- [x] 9.5 零引用后删除 Assets/Resources/PCG、Runtime/PCGMap、空 TotemGame 及专用测试
- [x] 9.6 更新地图、参与者、撤离、诊断和 minimap 合同以适配场景世界边界
- [x] 9.7 运行编译、EditMode、PlayMode smoke、GF_X 全量诊断、严格 OpenSpec 校验并更新证据
