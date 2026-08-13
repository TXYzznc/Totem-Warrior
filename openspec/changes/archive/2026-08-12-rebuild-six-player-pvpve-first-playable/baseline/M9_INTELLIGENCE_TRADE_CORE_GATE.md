# M9 全员情报、成果与颜料请求核心证据

## 已落地范围

- `TotemFirstPlayableSocialService` 在每个构筑阶段开始后、六名参赛者均就绪时捕获固定六人快照；生命周期边界先结算，快照能反映淘汰状态。
- 快照对纹身与属性数组执行深拷贝，同一构筑阶段内的换装不会修改已公开版本。
- 只公开已装备部位、P01/P02、元素与配置中的无数值效果文本；不公开内部倍率、概率或冷却。
- 属性条目同时携带 `baseValue` 和 `inMatchBonus`；第一阶段暂无 Boss 强化，因此三个首版属性的局内加成为真实的 0，而非伪造数值。
- 每名参赛者独立累计玩家伤害/击倒/淘汰、PVE 伤害/击杀、治疗队友、护盾或减伤、成功救援、净化/解除控制、控制时长/次数、队友增伤收益、资源获取/分享、自身倒地和间接元素伤害。
- PVP 伤害、PVE 伤害/击杀、倒地/淘汰/救援、掉落拾取、自身倒地、反应间接伤害/停滞控制与颜料分享已接入现有运行时事件；未在第一阶段出现的辅助行为保留显式记录入口。
- 热冲击与过载只对目标生命应用一次实际间接伤害，再把同一个实际值同时记给触发者和旧元素辅助来源；击杀来源仍是触发异元素者。
- 颜料请求只能在构筑阶段向唯一队友发起，创建上限等于对方当时库存；批准时重新校验，并通过一个事务同时扣除赠予者、增加请求者、递增双方库存版本。
- 拒绝、阶段过期、响应者不匹配或批准时库存减少均不会部分修改库存；Bot 使用同一请求与事务入口，对仍有足量库存的合法请求立即批准。
- 人类与 Bot 的请求/审批均可编码为 `RequestPigment` / `ResolvePigmentRequest` gameplay command。

## 自动化证据

- Unity 编译：无本次新增 C# 编译错误。
- EditMode 测试源：`TotemFirstPlayableSocialStateTests` 覆盖全部成果字段、冻结/隐私、成功原子转移、并发库存变化、拒绝、过期及 command round-trip。
- GF_X 报告：`GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260811_134024.json`
  - 总计：32 success / 14 failure / 36 warning。
  - `Scenario/BusinessRuntime/Totem First Playable Intelligence And Pigment Trade`：success。
  - 记录：6 人目标快照、P01 冻结、3 个属性条目、成果精确值、请求 Approved、赠予者 8→3、请求者 1→6。
  - 14 个 failure 仍是迁移前旧合同断言，没有新增失败。

## 尚未闭合

- M6 的反应实际伤害与统计已接通，但火焰 tick 和雷元素放电的基础伤害尚未冻结，因此这两种持续/放电伤害不能凭空填值。
- 六人情报与颜料请求 UI 属于 M10；M9 Gate 需要 UI 接入后完成一次真实可审计转移。
- Test Runner/PlayMode 仍等待 UnitySkills Bypass；当前证据由编译、纯逻辑测试源与 GF_X EditMode 诊断组成。
