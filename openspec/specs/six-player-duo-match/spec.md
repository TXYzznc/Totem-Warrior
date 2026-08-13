# six-player-duo-match Specification

## Purpose
TBD - created by archiving change rebuild-six-player-pvpve-first-playable. Update Purpose after archive.
## Requirements
### Requirement: 对局固定为六名参赛者和三支双人队
第一阶段运行时 MUST 创建恰好 6 名参赛者，并将其分为 3 支互不重叠的双人队；本地模式中真人占 1 个位置，其余位置 MUST 由 Bot 补足。

#### Scenario: 单人启动本地对局
- **WHEN** 玩家从主菜单确认开始第一阶段本地对局
- **THEN** roster 包含 1 名真人和 5 名 Bot
- **AND** 每名参赛者恰好属于一支两人队

### Requirement: 同队无友伤且敌队从第一轮起可互相伤害
伤害服务 MUST 在应用伤害前检查队伍关系；同队直接伤害、元素伤害和反应伤害均为 0，敌队 PvP 在前三轮 MUST 始终合法，不得设置开局保护。

#### Scenario: 第一轮遇到敌队玩家
- **WHEN** 第一轮战斗中玩家的有效攻击命中敌队参赛者
- **THEN** 伤害正常结算
- **AND** 不因轮次或开局时间被禁止

#### Scenario: 攻击命中队友
- **WHEN** 任意直接或间接伤害以队友为目标
- **THEN** 队友生命和护盾不变
- **AND** 该伤害不计入有效伤害成果

### Requirement: 同队使用同一合法出生锚点
每支队伍 MUST 从地图合法出生锚点集合中随机抽取一个锚点，同队成员生成在该锚点定义的相邻位置；不同队不得使用同一独占锚点。

#### Scenario: 固定 seed 生成队伍
- **WHEN** 使用相同地图配置和 match seed 启动两次对局
- **THEN** 三支队伍选择相同的合法锚点
- **AND** 每支队伍的两名成员保持在同一出生组

### Requirement: Bot 遵守与真人相同的业务规则
Bot MUST 通过共享 gameplay command 入口执行移动、攻击、构筑、救援和资源交互，不得绕过队伍、资源、阶段或伤害校验。

#### Scenario: Bot 在非构筑阶段尝试修改纹身
- **WHEN** Bot command 请求修改纹身且当前为战斗阶段
- **THEN** 请求被拒绝
- **AND** 颜料、构筑和统计均不变化

