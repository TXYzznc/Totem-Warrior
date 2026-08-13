# IDEA-003：重新设计真正 Enemy 与 PVE 系统

- 状态：`parked`
- 记录日期：2026-08-11
- 来源：六人双排 first-playable 重构讨论

## 想法

真正的 Enemy/PVE 系统需要由用户后续重新设计，可能涉及敌人职责、行为、遭遇、资源产出、弱点、Boss 与多人协作压力，但当前尚未形成可实施共识。

## 当前版本处理

- first-playable 按 6 人、3 支双人队的纯 PVP 实现。
- 删除当前未经确认的 Enemy、Encounter、EnemyLoot、Boss 业务实现、活动配置、测试、诊断和零引用资源。
- 保留玩家/Bot 共用的伤害、元素、反应、归因和队伍关系等通用战斗抽象。
- 地图发育资源改由独立 MapResourcePickup 系统提供，不复用 EnemyLoot 或武器拾取模型。

## 重新进入开发的门槛

只有在敌人定位、核心交互、资源产出、与 PVP 的关系、阶段节奏、验收标准和生产力边界全部确认，并创建独立 OpenSpec change 后，才重新实现 PVE。
