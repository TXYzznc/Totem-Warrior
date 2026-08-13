# M5 纹身构筑核心阶段证据

日期：2026-08-11

## 已落地范围

- `TotemFirstPlayableTattooBuildState` 固定六个部位槽位。
- 第一阶段仅接受 `P01`、`P02`，公开效果文本不包含精确数值，也不为图案添加正式名称。
- 颜料钱包仅包含火、冰、雷三种独立整数资源。
- 装备固定消耗 10；拆除或替换旧纹身固定返还 6。
- 替换事务先在临时账本中校验：同元素可使用旧纹身返还的 6，跨元素不足时不修改槽位、钱包或版本号。
- 只有 `OpeningBuild`、`Build2`、`Build3` 可修改；前台、三轮战斗和结果阶段均只读。
- `TotemFirstPlayableTattooBuildService` 按 participant 隔离状态并以 `TotemMatchFlowService.CurrentPhase` 作为权威权限来源。
- first-playable 合同新增可序列化 `tattooBuild.patterns[].publicEffectText` 字段，并严格校验只有 P01/P02、火/冰/雷、六槽位和 10/6 账本。
- 真人与 Bot 的 `EquipTattoo` / `RemoveTattoo` 使用同一 gameplay command 编解码及事务入口。
- 旧 `TotemTattooService` 已退出默认运行时注册；旧 CombatHUD 自助纹身快捷入口被阻断，因此旧 UI、AI 与 PCG 调试面板在正式运行时无法取得旧服务并绕过阶段权限。
- 对局返回 `FrontEnd` 时清空所有参与者构筑状态并广播 `BuildReset`，不会错误返还跨局颜料。

## 自动化证据

- Unity 编译：无本次新增 C# 编译错误。
- EditMode 发现：187 个测试；新增纹身测试覆盖配置拒绝、真人/Bot 共用命令、非法命令、清理和 10/6 事务，均为 `Runnable`。
- Test Runner 执行：当前 UnitySkills 处于 `Auto`，`test_run` 被 `NeverInSemi` 策略直接拒绝；未绕过权限。
- GF_X 全量诊断：`GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260811_124131.json`
  - 总计：29 success / 14 failure / 36 warning。
  - `Scenario/BusinessRuntime/Totem First Playable Tattoo Build`：success。
  - 记录：configPatternCount=2、slotCount=6、equipCost=10、removeRefund=6、combatMutationCode=NotBuildPhase、cleanup 后颜料=0、patterns=P01/P02、pigments=Fire/Ice/Lightning。
  - 14 个失败仍来自旧 50 人、多武器、旧技能、旧 7 色/8 图案与旧 UI 等未迁移断言；新增场景未产生失败。

## 尚未闭合

- 旧类型和 Prefab 暂时保留作 `ScriptsBuiltin/Editor` 编译兼容证据，但已从正式运行时断开；待新构筑 UI 完成后再物理迁移/删除。
- P01 单目标聚焦与 P02 邻近扩散的战斗行为尚未接入。
- 三次构筑阶段的实际 UI 操作和 PlayMode Gate 尚未执行。
