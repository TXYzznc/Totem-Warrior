# M12 纯 PVP 首个可玩版清理与诊断证据

## 当前权威边界

- 对局固定为 6 名参赛者、3 支双人队；缺位由 Bot 补齐。
- 第一版只启用 `rifle_patrol_v1`，玩家没有主动技能。
- Enemy、Encounter、EnemyLoot、Boss、NPC、Shop、Choice、旧道具经济与死亡箱链路已从活动运行时和权威业务表移除。
- 地图只保留 `OASIS_CITY` 活动配置。
- 活动业务表共 7 张：`BotBuildPreset`、`BotConfig`、`MapTemplateConfig`、`MapResourcePickupConfig`、`ResourceConfig`、`UIFormConfig`、`ZoneShrinkConfig`。

## 地图拾取物配置

- `MapResourcePickupConfig` 共 9 条：火、冰、雷分别包含小、中、大三种拾取规格。
- 数量区间为小型 4～6、中型 8～12、大型 16～20，各记录可独立配置 `MinAmount`、`MaxAmount`、`Weight` 和出现轮次。
- 同类数量不是固定值；运行时由 match seed、轮次和合法锚点派生确定性随机结果，同一输入可重放。

## 自动诊断

- 报告：`GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260811_180744.json`
- 结果：23 success / 0 failure / 0 warning。
- `Totem Five Round Match Flow` 已在 EditMode 固定输入下完成五轮、四次缩圈并进入 `Result`。
- `Totem First Playable Pure PVP Contract`、单步枪、元素、倒地、构筑、信息公开和颜料转移诊断全部通过。
- 旧 CharacterSelect、SelfTattoo、Shop、ThreeChoice、TattooEnchant、TattooStudio 的活动图片目录及 runtime asset key 已删除；Boss 动画/帧图/预制体、NPC 二进制素材、玩家技能图标、CombatHUD Boss 血条与 E/Q 技能槽也已物理删除。

## 已关闭的执行门

- UnitySkills 已切换为 Bypass；完整 EditMode、20 局 Bot 稳定性用例与五轮 PlayMode smoke 均已执行并通过。
- Sprite 补充清理后再次运行五轮 PlayMode smoke，结果见下方补充证据。

## 2026-08-11 Sprite 补充清理

- 对 `Assets/Game/Sprites` 做了逐 GUID 外部引用归属审计，而不是仅依据文件名判断。
- 资源由 740 个、约 200.34 MB 收敛为 254 个、约 18.90 MB；物理删除 486 个资源，约 181.44 MB。
- 已删除旧 Affix、Consumable、Enemy/Boss Effect、Item、旧颜料稀有度、三套 2D PCG 地块、全部旧 UI 图片、DeerWoman 2D 帧以及非步枪武器图片。
- MainMenu、CombatHUD、PauseMenu、RunResult、Settings 五个功能 Prefab 保留；其中 39 个 Image、1 个 RawImage 和 3 个 SpriteSwap 状态的旧图片引用已置空，后续等待新 UI 美术接入。
- 当前 Sprite 仅保留 252 个仍被 `Player.prefab`、Animator 和纹身帧映射直接依赖的 `ActorCommonM02` 临时运行占位资源、1 个纹身图集和 1 个步枪占位图。`ActorCommonM02` 应在 3D 玩家 Prefab 接入后成组移除，不能在当前阶段先删坏可玩闭环。
- `validate_find_missing_scripts(searchInPrefabs=true)`：0；`validate_missing_references`：0。
- GF_X 全量诊断：`GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260811_192809.json`，23 success / 0 failure / 0 warning。
- PlayMode 五轮 smoke：UnitySkills job `54a37af8`，1/1 passed；持久化结果更新时间 2026-08-11 19:36:11。

## 2026-08-12 3D 角色与待交付图片边界修正

- 用户确认角色改为 3D，旧 `ActorCommonM02` Sprite、Animator、纹身帧映射和 Player/SmartAI/LightAI 2D Prefab 不再使用，已整链物理删除；`Assets/Game/Sprites/Actors` 同步删除。
- `Assets/Game/Sprites/Tattoo` 与 `Assets/Game/Sprites/Weapons` 保留为空目录，分别等待新纹身图片和新武器图片。
- 角色 3D Prefab 尚未实际导入。运行时目录继续保留稳定目标路径，但在资产缺失时明确生成 Capsule 3D fallback；不再实例化带断裂 Sprite GUID 的不可见旧 Prefab。
- 单步枪诊断改为验证“稳定 key + 目标路径 + fallback 合同”，不再把尚未交付的图片文件错误地作为玩法硬门槛。
- 删除零引用且路线过期的 `SpriteTintOutline`、`TotemTattooSprite`、`TextMeshFont`、`ASE/FastLit` Shader；独立且未纳入 Git 的 3D VisualDestruction 实验未擅自删除。
- GF_X 全量诊断：`GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260812_102329.json`，23 success / 0 failure / 0 warning。
- 五轮 PlayMode smoke：UnitySkills job `ac9e8b30`，1/1 passed。

## 2026-08-12 测试版整队撤离切片

- 第一版继续保持无后端、无真实联机；本地真人所在队伍撤离成功后立即结束整局，停止其他 Bot 队伍模拟并进入既有 `Result`。
- `Shift + Space` 通过 `TotemInputService` 产生一次性测试解锁命令，普通空格仍为闪避；仅 `Round4Combat` 起可用。
- 地图提供 4 个专用 `Extraction` 合法锚点，按 match seed 确定性抽取默认 3 个固定撤离点。
- 本地真人在范围内按住 `F` 3 秒完成整队撤离；松开、离开、受伤、倒地会中断，倒地队友会阻止开始，已淘汰队友不会被复活。
- 撤离 PlayMode smoke：UnitySkills job `21c26148`，1/1 passed，覆盖输入解锁、3 点生成、双人队撤离、`LocalTeamExtracted` 结果与立即进入 `Result`。
- 最新 GF_X 全量诊断：`GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260812_111744.json`，23 success / 0 failure / 0 warning。
- 全量 EditMode job `04c33c06`：251/253 passed；两个既有失败分别是 OasisCity 模型 secondary UV 资源门和尚未交付的新步枪图片 catalog 门，与撤离切片无关。撤离新增的 10 个 EditMode 用例全部通过。
- 五轮 PlayMode 回归本轮两次未进入测试体并由 UnitySkills 以 Test Runner 启动/域重载结果缺失超时结束；没有产生玩法断言失败。变更前最近有效五轮回归仍为 job `ac9e8b30` 1/1 passed，因此任务 8.7 暂不关闭，等待测试基础设施稳定后补跑。
