## Why

旧运行时基于 20/50 人、多武器、角色选择、主动技能、战斗中随时修改纹身、Enemy/PVE/Boss 等假设，既不适合“单开发者 + AI”的生产力，也无法快速验证新的构筑博弈。第一版必须先形成结构完整、可重复运行的六人五轮纯 PVP 闭环，再逐步补回经过独立设计的 Enemy/PVE、Boss 和撤离内容。

## What Changes

- **BREAKING**：每局 6 人、3 支双人队；空位由 Bot 补齐，同队无友伤。
- **BREAKING**：第一版只保留一款基础步枪；移除角色选择、多武器切换和玩家主动技能。
- 实现五轮流程：开局构筑 60 秒；第 1 轮战斗；第 2～5 轮各有 45 秒构筑；第 2～5 轮战斗开始时依次执行四次缩圈。
- 构筑阶段暂停世界模拟；只有构筑阶段可修改纹身。第一版只开放 P01/P02、六个部位、火/冰/雷颜料，每次装备消耗 10，拆除返还 6。
- 构筑开始时公开其他玩家的纹身效果文本、基础/局内属性和本局累计成果；队友间通过请求/同意转移颜料。
- 引入弱/标准/强三层元素、持续时间、衰减、反应、来源归因和按优先级排序的确定性事件队列。
- 第一版暂为纯 PVP：删除 Enemy、Encounter、EnemyLoot、Boss 的运行实现、配置、测试、诊断和资源入口。Boss、撤离与高阶资源等待后续设计，不得以旧实现占位。
- 地图资源只从合法锚点生成；拾取种类、数量区间、权重和生效轮次均由配置表控制。
- 增加测试版撤离切片：Round4Combat 起允许通过 InputModule 的 `Shift + Space` 一次性触发撤离解锁，从专用合法锚点按 seed 生成默认 3 个撤离点；本地玩家完成交互后整支本地双人队撤离并立即结束本局。未来 Boss 击杀复用同一解锁入口。
- 重做主菜单、CombatHUD、构筑、情报、资源请求、倒地/救援/淘汰、观战和五轮结果界面；旧 Shop、ThreeChoice、SelfTattoo 等入口退出主流程并物理清理。
- 建立 EditMode、PlayMode smoke、GF_X 全量诊断、固定种子回放和五轮结果证据。

## Capabilities

- `six-player-duo-match`
- `round-build-combat-loop`
- `construction-intelligence-and-trade`
- `elemental-reaction-and-effect-queue`
- `downed-revive-elimination`
- `first-playable-map-resources`
- `first-playable-acceptance`
- `main-menu-flow`
- `core-ui-screens`
- `tattoo`
- `player-attack-system`
- `test-extraction-flow`
- `authored-oasis-city-runtime`

## Impact

主要影响 `Assets/Game/Scripts/Runtime`、`Assets/Game/Scripts/UI`、业务 JSON/XLSX、runtime catalog、Launch 流程、测试、诊断和旧业务资源。场景、角色 3D 资源与已确认纹身贴花继续使用；UI 资源重新设计。GF_X 框架生命周期、输入服务和资源加载约定保持不变。
