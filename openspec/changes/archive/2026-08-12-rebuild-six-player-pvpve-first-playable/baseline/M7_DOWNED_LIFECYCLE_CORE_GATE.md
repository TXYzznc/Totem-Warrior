# M7 倒地生命周期核心证据

## 已落地范围

- 单个参赛者使用纯 C# 生命周期状态：Alive、Downed、Eliminated；Unity 场景、输入、距离与控制判定留在运行时薄层。
- 有可救援存活队友时，致命伤进入 40% 最大生命的独立倒地血池；无可救援队友时直接按整队淘汰处理。
- 倒地流血为 20 秒，移动倍率为 35%；倒地者不能攻击、闪避或构筑。
- 处决与流血淘汰保存完整 `TotemCombatantReference`（domain + combatant ID），PVP 与 PVE 来源都不会退化为无效 Participant ID。
- 救援必须在现有 3 米交互边界内连续进行 3 秒；离开范围、救援者受控、救援者倒地或交互松开均立即取消并清零进度。
- 人类按键与 Bot 决策均编码为同一种 `BeginRevive` `TotemGameplayCommand`，只区分 command source；目标、队伍、状态、距离与控制校验不分叉。
- 人类输入只通过 `TotemInputService`/`ITotemInputProvider` 读取；Bot 不伪造设备输入，由生命周期服务持续验证其救援意图。
- Bot 将倒地队友置于战斗/资源目标之前，先接近到合法范围，再发出共用救援命令并持续救援。
- 救起后恢复 30% 最大生命、0 护盾，并获得 1 秒伤害保护；保护与流血计时受 gameplay suspension 门控。
- 救援交互会抑制同一帧的死亡箱/普通箱交互，避免一次 F 输入触发两个行为。
- 进入任意构筑阶段时，仍倒地者立即按 `BuildBoundary` 淘汰，救援进度不会跨入暂停构筑。
- 淘汰后摄像机自动改为跟随仍活跃的唯一队友；若全队淘汰则保留最后视点并进入等待结果状态，不提供重生路径。
- 最后一名活跃队友淘汰时，同队仍倒地成员立即按 `TeamEliminated` 结算，不再无意义地等待流血结束。

## 自动化证据

- Unity 编译：无本次新增 C# 编译错误。
- EditMode 测试源覆盖倒地、流血、四种救援取消、3 秒完成、30% 生命、1 秒保护、处决、构筑边界、观战、关系层、真人/Bot command 对称及 3 米边界。
- Test Runner 执行：UnitySkills 当前仍为 `Auto`，`test_run` 属于 `NeverInSemi`，未绕过权限；需在 Bypass Gate 补跑。
- GF_X 报告：`GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260811_134024.json`
  - 总计：32 success / 14 failure / 36 warning。
  - `Scenario/BusinessRuntime/Totem First Playable Downed Lifecycle`：success。
  - 记录：40 倒地生命、20 秒流血、0.35 移速、越界取消归零、30% 救起生命、1 秒保护、3 米边界、PVE 来源 `Enemy:1002`、构筑边界淘汰和两种观战路由。
  - 14 个 failure 与接入前基线相同，仍来自旧 50 人、多武器/技能、旧缩圈、旧 UI/资源合同及工作区清理断言；本次场景没有增加失败。

## 尚未闭合

- 观战摄像机和整队等待状态已接线；相应 UI 反馈归入 M10，端到端 smoke 仍需 Bypass Gate。
- 真人与 Bot 的真实场景互救仍需 PlayMode smoke；当前 Auto 模式不能执行该 Gate。
- PlayMode 与 Test Runner Gate 等待 UnitySkills 切换到 Bypass，或由用户显式加入白名单。
