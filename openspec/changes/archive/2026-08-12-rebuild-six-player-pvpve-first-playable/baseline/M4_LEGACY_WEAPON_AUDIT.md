# M4 旧武器与主动技能残留审计

## 第一版权威边界

- 第一版只允许一款 active 基础枪械，玩家与 Bot 共用命中、伤害和效果提交入口。
- 当前第一版表现契约已经固定占位资源键 `weapon.rifle.patrol.v1`；它是可替换的占位枪械资源键，不代表最终枪型或最终数值已经确定。
- 旧武器记录可以作为迁移证据保留，但必须显式 inactive，不能再被主菜单、构筑、掉落、商店、地图资源点、Bot 或调试入口选中。
- 玩家第一版没有主动技能伤害；优先级 100 的主动技能臂槽只保留队列协议，不连接旧技能运行时。

## 当前残留

| 残留面 | 当前证据 | M4 处理 |
|---|---|---|
| 主流程默认值 | `TotemGameFlowService`、`TotemStartupSelectForm`、`TotemCombatHUDForm` 仍默认 `knife_basic`，启动页暴露 Knife/Bow 等选择 | 删除武器选择分支，统一默认占位枪械 |
| 武器池 | `WeaponConfig` 仍包含 knife/hammer/pistol/bow/fist 五类 | 增加 first-playable active 状态，仅一款基础枪械 active；旧五类 inactive |
| 掉落与商店 | `WeaponDropConfig`、`MerchantConfig`、`EnemyLootConfig`、Choice/NPC 升级逻辑仍分发旧武器 | 第一版禁用旧武器掉落/购买/升级入口 |
| 地图资源 | `TotemMapService` 仍把 pistol/hammer/bow 写进三个资源锚点 | 改为基础枪械弹药/通用资源，不生成旧武器 |
| 投射物/VFX | `arrow_bow` 仍在 ProjectileConfig、VFX 映射和 runtime asset catalog 中 active | 基础枪械使用统一射线/占位弹道；旧箭矢 inactive |
| 硬编码 fallback | `TotemGameplayCatalog` 和 `TotemWeaponService` 含五武器 fallback、蓄力、弹药耗尽分支、近战/远程特判 | 新入口不再回退旧池；保留代码只到 M12 删除前的迁移隔离区 |
| 主动技能 | Combat/Input/HUD 仍保留 E/Q 伤害与冷却显示 | 第一版 Combat 不提交玩家主动技能伤害；UI 旧槽位在新 UI 交付前仅作占位并标记 inactive |
| 测试与诊断 | 多个诊断仍用 `knife_basic` 启动，旧 PlayMode smoke 验证 E/Q | 迁移为统一枪械 ID；旧主动技能断言移出第一版 Gate |

## 清理顺序

1. 先建立单枪械配置与统一有效直接伤害入口，保证代码引用有新落点。
2. 再切换主流程、Bot、地图资源、掉落和调试场景，消灭运行时旧武器可达路径。
3. 将旧配置和资源目录标为 inactive，并用诊断证明 runtime catalog 没有 active 引用。
4. M12 根据 Git 可恢复证据删除已经零引用的旧武器 prefab/sprite、旧技能 UI 和对应生成物。

## 禁止的兼容方式

- 不把 `pistol_basic` 改名后当作已经完成的单枪械架构。
- 不继续允许 `knife_basic` 作为缺省 fallback。
- 不通过 UI 隐藏掩盖掉落、Bot、商店或调试 API 仍可选择旧武器。
- 不为了旧测试通过而保留弹药耗尽近战降级、蓄力弓或玩家主动技能伤害。
