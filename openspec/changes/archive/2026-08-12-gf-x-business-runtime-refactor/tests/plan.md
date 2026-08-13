# Test Plan: GF_X business runtime refactor

## 1. 自动化验证

| ID | 范围 | 操作 | 通过标准 |
|---|---|---|---|
| VT-01 | UnitySkills | `unity_skills.py health --port=8092` | `ok=true`，项目指向当前 `Totem-Warrior` |
| VT-02 | Unity 编译 | Unity batchmode 或 UnitySkills refresh 后读取 Console Error | 0 个脚本编译错误 |
| VT-03 | GF_X 诊断 | `unity_skills.py totem_diagnostics_run_all --port=8092` | 0 failure；迁移路径、BuildSettings、AppConfigs、AI JSON 检查通过 |
| VT-04 | AI DataTable | `AIGameDataTableGenerator.ValidateAllDataTablesAIJson` | GF_X Core 表 JSON 校验 0 failure |
| VT-05 | 旧业务表清单 | 扫描 `LegacyProjectArchive/Assets/Resources/DataTable/*.json` 与旧 `DataTableRegistry`，并确认 `Assets/Resources/DataTable` 不存在 | 28 张旧业务表全部被登记到迁移 manifest；无丢表且不在 active Resources 路径 |
| VT-06 | 资源路径 | 扫描 `Resources.Load` / prefab / sprite / audio 引用 | 缺失资源可报告；首屏链资源必须存在或有明确 fallback |
| VT-07 | UTF | EditMode / PlayMode 测试 | TestRunner 可用时执行；不可用时记录阻塞并使用诊断场景兜底 |

## 2. 不自动执行的界面验证

运行界面的 playtest 不要求自动执行，但需要保留用例：

| TC | 玩家路径 | 预期 |
|---|---|---|
| UI-01 | GF_X Launch -> MainMenu | 默认可见主菜单，无空 Workspace 停留 |
| UI-02 | MainMenu -> CharacterSelect -> StartupSelect | 角色、初始颜料/武器/图案选择可进入战斗 |
| UI-03 | InGame HUD | HUD 显示生命/资源/技能/纹身状态 |
| UI-04 | Tab 自助纹身 | 自助纹身面板可打开、选择、开始/取消读条 |
| UI-05 | Esc 暂停 | 暂停菜单可打开、Resume/Settings 可用 |

## 3. 迁移验收

- GF_X `Launch.unity` 是唯一默认启动入口。
- 启动流程不再停留在空 `WorkspaceProcedure`。
- 旧业务效果在迁移过程中有清单、有诊断、有测试证据。
- 新增业务代码不直接依赖或挂载旧 `GameApp` / `ModuleRunner` / `EventBus` / `UIModule` / 旧 `DataTableModule`。
- 所有按键输入继续通过 `InputModule` 或其 GF_X 后继服务。
- `Assets/Game/Examples` 与 `GameData/Examples` 不进入默认运行链。

## 4. GF_X Launch Play Mode smoke

| TC | 场景描述 | 前置条件 | 操作步骤 | 预期日志 | 预期 UI / 状态 | 通过标准 |
|---|---|---|---|---|---|---|
| PM-01 | 验证 GF_X `Launch.unity` 可以进入 Play Mode 并保持 Totem runtime 干净 | Unity 已编译完成，BuildSettings 启用 `Assets/Game/Scene/Launch.unity` | `scene_load Assets/Game/Scene/Launch.unity` -> `console_clear` -> `editor_play` -> 轮询 `editor_get_state` -> 执行 `totem_diagnostics_run_all` -> 读取 console/report -> 退出 Play Mode | Console 无 Error/Exception；诊断报告 0 failure / 0 warning | 当前场景路径为 `Assets/Game/Scene/Launch.unity`；Totem/GF_X runtime 诊断通过 | 命中预期状态，新增 Error=0，诊断报告 PASS |
## 5. PM-02 GF_X First UI Flow

| TC | Scene | Preconditions | Steps | Expected logs | Expected UI / State | Pass criteria |
|---|---|---|---|---|---|---|
| PM-02 | `Assets/Game/Scene/Launch.unity` | Unity compiled; active scene is `Launch`; GF_X startup path is clean | `Edit/Play` -> wait `MainMenu(Clone)` -> click `StartButton` -> click `CharacterCard_1` -> click `NextButton` -> click `Color_1` / `Weapon_knife_basic` / `Pattern_1` -> click `ConfirmButton` -> wait `CombatHUD(Clone)` -> run diagnostics | In-Play Console `Error/Exception=0`; diagnostics report `0 failure / 0 warning` | `CharacterCard_1/2/3`, `StartupSelect(Clone)`, `Color_1`, `Weapon_knife_basic`, `Pattern_1`, `CombatHUD(Clone)`, `HpBar`, `WeaponIcon`, `SkillSlotE` exist | PASS report: `tools/playtest/reports/2026-07-08-0241-PM-02-ui-flow-main-to-combat.md`; known Unity UIElements Material error after Play Mode exit is classified as Editor transient noise only when stack has no project frame |
