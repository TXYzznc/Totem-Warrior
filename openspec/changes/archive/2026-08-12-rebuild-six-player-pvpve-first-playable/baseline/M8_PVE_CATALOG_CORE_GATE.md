# M8 第一阶段 PVE 目录核心证据

## 已落地范围

- 正式 first-playable 装配只允许三个 active 敌人 ID：近战追猎者、远程射手、护盾精英。
- `TotemEnemyService` 原先在构造时注册的 15 个内建敌人不再残留在正式运行时：`TotemEnemyWorldService` 使用替换式目录装配，将定义表清空后只注册三个角色。
- 三个角色均来自 Business `EnemyConfig`，具有正数生命/伤害/移速/射程、非空能力与掉落表；角色顺序固定，不依赖原表行顺序。
- Boss、异星/病毒主题和其他旧敌人仍作为历史/独立诊断定义保留，但正式 first-playable `TryGetDefinition` 无法查到，也不能进入 encounter 随机池。
- 正式敌人掉落目录已压缩为 5 条：轻型敌人的金币、基础补给、元素颜料，以及精英敌人的金币、元素颜料。旧武器、旧装备、Boss 配方和非火/冰/雷颜料不会进入运行时生成器。
- 元素颜料保持原掉率与数量合同，在生成时确定性解析为红色/火、蓝色/冰、黄色/雷；拾取后直接写入玩家独立的纹身构筑库存，不再写入旧通用颜料背包。
- 三轮遭遇从 Business 表的 `WaveMin=4 / WaveMax=6` 推导为 4/5/6 压力预算：第 1 轮仅追猎者，第 2 轮追猎者+射手，第 3 轮追猎者+射手+1 名守卫。每个职责使用独立白名单，旧主题敌人不能借共享 pool 混入。
- 遭遇不再按旧全局 0/240/600 秒时间表持续生成，而是在各轮真正进入 Combat 活动时重建。构筑和第 2/3 轮缩圈期间不提前生成新模板；所有位置继续通过合法 Encounter/EliteSpawn 锚点、可达性、玩家距离和同波间距校验。
- 玩家头部隐藏弱点与非人型敌人可见发光弱点占位合同已在 M4 运行时和诊断中接入，M8 直接复用，不建立第二套弱点系统。

## 自动化证据

- Unity 编译：无本次新增 C# 编译错误。
- EditMode：原有 220 个测试已发现；本轮新增 4 个掉落过滤/三元素映射用例并通过 Unity 编译。受 UnitySkills `test_list` 单页 100 条限制，新增页的独立发现与执行留在 Bypass 门禁完成。
- Test Runner 执行：UnitySkills 当前仍为 `Auto`，未绕过 `NeverInSemi` 权限。
- GF_X：`GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260811_132223.json`，31 success / 14 historical failure / 36 warning。`Totem First Playable PVE Catalog` 验证 1/2/3 个职责配置和 4/5/6 个敌人预算；`Totem Enemy Domain Runtime` 实际生成第 1 轮 4 个、第 2 轮累计 8 个轻型、第 3 轮累计 13 个轻型+1 个守卫，Boss 为 0；迁移后的对象池诊断也成功。

## 尚未闭合

- 4/5/6 是从现有表的波次最小/最大值推导出的 first-playable 默认值，不视为最终平衡结论；后续 playtest 可直接通过 Business 表调整边界。
- 三角色的前摇、受击、死亡与掉落运行时已有底层能力和固定流程诊断，但仍需 PlayMode 表现证据与职责可辨识度人工验收。
- Boss、高阶资源和图案体验卡需要在对应阶段单独建立第一版目录；当前普通敌人掉落不会提前暴露这些内容。
