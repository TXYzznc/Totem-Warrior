# M6 三层元素状态核心证据

日期：2026-08-11

## 已落地范围

- 单个战斗目标使用固定容量三层状态：弱、标准、强。
- 同元素施加逐层提升；强层再次施加只刷新 3 秒计时，不覆盖已经记录的三层来源。
- 每 3 秒按 FIFO 消耗一层；构筑阶段通过显式暂停参数保持层级和剩余时间。
- 每层保存来源 participant 与 application sequence，热路径不创建临时集合。
- 异元素施加立即成为终止反应节点：新元素被消费，旧元素按 FIFO 降一层，弱层直接消失，不生成反应链。
- 火冰、火雷、冰雷组合与施加顺序无关，分别解析为 HeatShock、Overload、Stasis。
- 冻结常量已进入统一规则：火 0.5 秒 tick 与 1/1.25/1.5，冰 12%/20%/28%，雷 0.5 秒间隔，热冲击 0.6D，过载 0.35D/0.25D/3m，停滞 0.8 倍直伤/2 秒。
- 元素服务已经注册到唯一 `TotemGameRuntime`：构筑阶段由 `ITotemGameplaySimulationService` 统一暂停，返回前台时清空全部目标状态。
- 冰减速已经进入真人和 Bot 的实际移动倍率；停滞已经进入唯一 first-playable 枪械直伤入口。
- 火 tick 以批次事件从元素服务输出；单次大跨度推进会按 tick/衰减边界分段，结果与逐帧推进一致，同一时刻固定先 tick 后衰减。
- 雷放电使用每目标 0.5 秒间隔，并提供最近同阵营次要目标与无目标 50% 自身回流的确定性解析器。
- 反应结果保存触发者、FIFO 辅助来源与同一间接伤害值，击杀所有者固定为触发者；实际扣血仍留给后续战斗服务单次执行。

## 自动化证据

- Unity 编译：无本次新增 C# 编译错误。
- EditMode 发现：203 个测试；元素状态排列、FIFO、暂停、衰减、运行时服务以及大跨度/逐帧等价用例均为 `Runnable`。
- Test Runner 执行：UnitySkills 仍为 `Auto`，未绕过 `NeverInSemi` 权限。
- GF_X 报告：`GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260811_125312.json`
  - 总计：29 success / 14 failure / 36 warning。
  - `Scenario/BusinessRuntime/Totem First Playable Element State`：success。
  - 记录：Strong、暂停衰减 0、正常衰减 1、HeatShock、0.6D=6、killOwner=P6、反应后 Weak、火 tick、雷间隔、停滞 80% 直伤，以及 6.1 秒 hitch 恢复时 12 tick/16.5 系数/2 层衰减。

## 尚未闭合

- 火焰 tick 和雷放电尚缺少已冻结的基础伤害值，因此当前仅完成调度/选择，不能臆造实际扣血数值。
- 热冲击/过载实际单次扣血、过载 3 米邻近伤害与轻击退尚未接入战斗服务。
- 成果统计的双方同值间接伤害及单次生命扣减尚未接入。
- 完整 M6 Gate 仍需执行 EditMode/PlayMode 和固定 seed 追踪。
