# 旧功能反查清单

该清单用于“冻结并逐项替换”，不是立即删除清单。代码结构索引未包含当前 `Assets/Game/Scripts` 新业务层，因此本次在 codebase-memory 无结果后使用最小范围 `rg` 取证。

| 旧能力 | 当前入口/证据 | First Playable 处置 | 预计收口阶段 |
|---|---|---|---|
| 50 人 roster | `TotemActorService.ParticipantCount = 50`，并分为 20 Smart + 29 Light Bot | 替换为 6 人、3 支双人队；旧常量与断言转 historical | M2 |
| 角色选择 | `TotemMainMenuForm -> OpenCharacterSelect`、`TotemCharacterSelectForm`、CharacterSelect UIForm 配置 | 从主流程移除；资源/代码暂保留 inactive | M10/M12 |
| 启动选择 | `TotemStartupSelectForm` 同时选择颜色、武器、图案 | 从主流程移除，替换为 LocalMatchConfirm -> OpeningBuild | M10 |
| 多武器 | `WeaponConfig`、`WeaponDropConfig`、商店/掉落及 HUD 支持 knife/hammer/pistol/bow/fist | 仅一个基础枪械 active；其余配置保留但 inactive | M4/M12 |
| 主动技能与 E/Q HUD | CombatHUD 显示 E/Q 冷却，旧武器 trait/ability 仍在 catalog | 玩家第一阶段无主动技能；保留效果队列优先级 100 槽 | M4/M10 |
| 随时纹身 | `TotemTattooService.Equip` 目前不校验 MatchPhase，自纹身/NPC/UI 可调用 | 所有业务入口统一限制为构筑阶段 | M5 |
| 60 秒 PvP 保护 | `TotemCombatRelationshipService.ParticipantCombatGraceSeconds = 60` | 删除前三轮保护；同队无友伤、敌队从第 1 轮可伤害 | M2 |
| Boss 入口 | EncounterSpawnConfig、BossPhaseConfig、EnemyConfig、Boss HUD/控制器 | 第一阶段运行配置禁用；保留后续五轮兼容契约 | M3/M12 |
| 撤离入口 | 当前主运行链未发现完整可用撤离闭环 | 第一阶段明确不实现；不得为了“完整”临时加半成品入口 | M3/M12 |

反查完成的判据：每一项必须在最终配置、代码入口、UI、测试和诊断中同时标记为 active、inactive 或 historical，不能只隐藏按钮而保留可达业务路径。

