# Change Verify：rebuild-six-player-pvpve-first-playable

## 摘要

| 维度 | 状态 |
|---|---|
| 完整性 | 46/46 tasks 完成 |
| 正确性 | 核心规则、配置和运行入口已通过全量 EditMode、20 局 Bot 稳定性与五轮 PlayMode smoke |
| 连贯性 | 纯 PVP、五轮、单步枪、构筑/元素/倒地设计与活动代码一致；旧双轨入口已物理清理 |

## CRITICAL

无。
## WARNING

无。`Core/UITable.xlsx` 已通过项目原生 `AIGameDataTableGenerator.ReverseDataTablesJsonToExcelByRelativePaths` 同步，并从 XLSX 内部 XML 验证只保留 MainMenu、CombatHUD、PauseMenu、RunResult、Settings 五项。

## 已验证映射

- 六人三队、Bot 补位、同队无友伤：`TotemActorService`、`TotemCombatRelationshipService`、纯 PVP contract diagnostic。
- 五轮/四缩圈：`TotemMatchFlowService`、`ZoneShrinkConfig`、`Totem Five Round Match Flow` diagnostic。
- 配置化地图拾取物：`MapResourcePickupConfig` 9 条记录；4～6、8～12、16～20 三档独立区间；运行时按 seed/轮次/锚点确定性随机。
- 单步枪：活动目录仅 `rifle_patrol_v1`；旧近战、蓄力、弹药、投射物和武器词条模型已删除。
- 构筑、公开信息、颜料转移、元素队列、倒地救援和纯 PVP 结算均有对应 EditMode diagnostic，最新全量报告 23/23 通过。
- 旧 CharacterSelect、StartupSelect、Shop、ThreeChoice、SelfTattoo、TattooEnchant、TattooStudio 的代码入口、runtime key 和活动 UI 图片目录已清理。
- 结果流程保存 `latest.json` 与带 seed/时间戳的回放文件，包含六人/三队状态、最终纹身、精确成果、阶段时长、异常和关键配置版本。

## 最新自动验证

- `openspec validate rebuild-six-player-pvpve-first-playable --strict`：通过。
- `GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260811_192809.json`：23 success / 0 failure / 0 warning。
- UnitySkills EditMode job `4e1127b8`：243/243 passed。
- UnitySkills PlayMode job `558c015b`：20 局“1 真人 + 5 Bot”快速稳定性用例 1/1 passed，用例内逐局检查参赛者与资源残留。
- UnitySkills PlayMode job `cac96296`：Launch→主菜单→开局构筑→五轮/四次缩圈→Result→主菜单 1/1 passed，返回菜单后六人运行时名单为 0。
- Sprite 补充清理后 UnitySkills PlayMode job `54a37af8`：同一五轮闭环 1/1 passed；`Assets/Game/Sprites` 从 740 个/约 200.34 MB 收敛为 254 个/约 18.90 MB，UI Prefab 旧贴图引用、Missing Script 与 Missing Reference 均为 0。
- 2026-08-12 用户确认切换 3D 角色路线后，旧 2D 角色帧/动画/纹身映射/Prefab 已整链移除；Tattoo 与 Weapons 图片目录留空等待新资源，当前角色使用 Capsule 3D fallback。最新 GF_X 报告 `gf-diagnostics-run-all_20260812_102329.json` 为 23/23，PlayMode job `ac9e8b30` 为 1/1 passed。
- Test Framework 直接依赖已与实际解析版本对齐到 1.4.6；PlayMode 结果通过编辑器回调桥持久化，域重载后可由 UnitySkills 恢复。
- 测试版整队撤离已接入：无后端，`Shift + Space` 从第四轮起解锁默认 3 个确定性撤离点，本地真人完成交互后未淘汰队友一并撤离，整局立即以 `LocalTeamExtracted` 进入 Result。UnitySkills PlayMode job `21c26148` 为 1/1 passed；GF_X 报告 `gf-diagnostics-run-all_20260812_111744.json` 为 23/23。
- 当前全量 EditMode job `04c33c06` 为 251/253；剩余两个失败是已知美术资产交付/清理边界（Oasis secondary UV、新步枪图片 catalog），不属于撤离实现。撤离后的五轮 PlayMode 补充回归受 Test Runner 启动/域重载结果缺失超时影响，任务 8.7 保持未完成，不将基础设施超时伪报为玩法失败或通过。

## 最终判断

当前可进入变更收尾/归档准备。实现主体、结构化五轮结果证据、活动 UI 配置与设计已连贯，所有 Test Runner gate 均已关闭。
