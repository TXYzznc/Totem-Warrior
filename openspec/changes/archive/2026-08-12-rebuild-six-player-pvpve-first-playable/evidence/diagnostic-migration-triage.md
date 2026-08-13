# GF_X 全量诊断迁移分流（2026-08-11）

## 本轮结果

- 报告：`GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260811_143316.json`
- 汇总：30 success / 16 failure / 34 warning。
- Unity Console 仅保留 3 条既有 Unity Inspector/URP 编辑器异常；未发现本 change 新增编译错误。
- 本文只分流旧业务断言，不修改 `Assets/Game/ScriptsBuiltin/` GF_X 核心。

## 仍需关闭的当前问题

| 报告项 | 当前问题 | 关闭条件 |
|---|---|---|
| AI DataTable json | `UIFormConfig.xlsx` 与 first-playable JSON 不同步（56 cells / 13 rows） | 使用项目批准的 spreadsheet 写入链同步 xlsx，再复跑表校验 |
| GF_X Rewrite Inventory Contract | 美术索引仍有 46 个未分类系统、45 个 `classification-needed` 状态 | 由 `rebaseline-pvpve-art-resources` 更新资源索引与状态并重新生成 manifest |
| Clean Workspace Contract | `Assets/Screenshots` 位于 Unity 资产目录；`Assets/Resources` 含白名单外残留 | Bypass 下经 Unity AssetDatabase 审计并删除/迁移零引用内容 |

## 被新 BREAKING 规格取代的旧诊断

以下失败不是应该恢复的功能。恢复这些断言会重新引入本 change 明确取消的 50 人、多武器、主动技能、旧 UI、旧纹身和旧缩圈流程。它们在 GF_X 源中保留作历史证据，由 first-playable 新测试/诊断替代。

| 旧报告项 | 冲突的旧断言 | first-playable 替代证据 |
|---|---|---|
| GF_X Business Runtime Contract | `ThreeChoice` 拒绝后保持旧选择 UI | 主菜单直接进入本地确认；旧 ThreeChoice 不在 active UI catalog |
| Totem AI Runtime | 50 人、20 Smart + 29 Light、旧个性构筑 | 6 人三队、1 真人 + 5 Bot；`TotemBotTwentyRunSmokeTests` |
| Totem Audio Runtime | 近战命中必须使用 melee cue | 第一阶段唯一枪械使用 ranged 命中反馈 |
| Totem Balance Envelope | 旧 3 段缩圈和旧 TTK | 五轮流程包含四次缩圈；新 match-flow/zone 测试 |
| Totem Extended Gameplay | 旧 336 纹身组合、旧 shape/skill 入口 | 六部位、P01/P02、三元素、构筑阶段限定测试 |
| Totem First Round Contract | 50 人、旧 AI 配比、旧 3 段缩圈 | 六人 roster、五轮 match-flow、无友伤与固定 seed 测试 |
| Totem First Slice UI | CharacterSelect/StartupSelect/Shop/ThreeChoice/TattooStudio/SelfTattoo/TattooEnchant 和五武器 | MainMenu/CombatHUD/Pause/RunResult/Settings first-playable catalog |
| Totem Gameplay Catalog | 旧 catalog hash、3 个旧 ZoneShrink row | first-playable runtime filters、两次缩圈与确定性遭遇模板 |
| Totem Gameplay Runtime | 50 人、旧地图主题地形、旧武器/技能输入、战斗中 SelfTattoo | 六人共享 command、唯一枪械、构筑锁、五轮纯 PVP smoke |
| Totem Runtime Assets | 旧武器、玩家主动技能和旧 VFX sprite 必须 active | first-playable runtime asset catalog 与 fallback 合同 |
| Totem Runtime Catalog Binding | 5 武器、主动 Skill/Tattoo/Choice/Npc service 必须注册 | first-playable 默认 runtime 仅注册 active 服务；旧合同保持未注册 |
| Totem Runtime Causality Smoke | 旧技能、NPC、50 人、死亡箱和 Boss 因果链 | Launch→五轮→Result→MainMenu 的新端到端 smoke |
| Totem VFX Runtime | 旧技能 burst、手枪/弓投射物 key | 唯一枪械命中、元素/反应和占位弱点表现合同 |

## 判定规则

1. 新代码不得为了让旧报告变绿而恢复已取消的 active runtime 路径。
2. 与当前规格一致的失败必须修复；与 BREAKING 规格冲突的失败必须在知识库和诊断反查中标为 `historical/superseded`。
3. M12 最终验收以新 first-playable EditMode、PlayMode、20 局 smoke、性能采样和当前问题关闭为准，同时附上本报告说明不可修改的旧 GF_X 诊断债务。
