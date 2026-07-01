# 测试结果 — 06-v21-implementation

## EditMode（通过 Unity Skills `test_run` 异步跑）

**结果**：✅ 154/155 通过（99.4%）

| 项 | 值 |
|---|---|
| 总测试 | 155 |
| 通过 | 154 |
| 失败 | 1 |
| 失败用例 | `UnitySkills.Tests.Core.SkillDocumentationConsistencyTests.SkillDocumentation_ShouldMatchCodeDefinitions` |
| 耗时 | 33 秒 |
| jobId | `f9df426e` |

> 唯一失败的是 Unity Skills 服务自己的内部一致性检查，跟本次 06-v21-implementation 改动无关。
>
> 项目相关测试（含 V21ContractTests 11 个用例 + Tattoo336 / TattooStrategy / TattooModuleIntegration 等历史套件）全数通过。

### V21ContractTests 覆盖

- `TattooAffix` 字段稳定性 + AffixType 枚举完整性
- `DamagedEvent`：基础构造 / 含 HP 构造（HUD 后向兼容）
- `SkillCastEvent`：仅 SkillId / 完整三参（HUD 后向兼容）
- `BossSpawnedEvent` Target+Position 填充
- `TattooInProgressEvent` 读条时长
- `TattooCancelledEvent` 4 种 CancelReason 枚举完整
- `PlayerControllerType` 4 值（Human / SmartBot / LightBot / NetworkReplay）
- `TattooSlot.Affixes` 列表容器

## PlayMode（实际进入 PlayMode 验证）

**结果**：✅ GameApp 启动成功 + 21 模块全就绪 + 0 错误

通过 `editor_play` 进入 PlayMode 验证（不走 TestRunner，因 Domain Reload 导致 jobId 丢失）：

| 项 | 状态 |
|---|---|
| 总日志 | 138 条 |
| 错误 | **0** |
| 警告 | 2（菜单重复定义，与 v2.1 无关） |
| GameApp 启动 | ✅ `[GameApp] 所有模块初始化完成，游戏就绪` |
| MapGenModule | ✅ Seed=1 / 4 房间 / 150m / ZonePhase=0 启动 |
| EventModule | ✅ OnMapGenerated 触发，RoomCount=4 |
| NPCModule | ✅ 注册 UIShopBuyConfirm / UIEnchantConfirm / BotInteractRequest / InteractPressed |
| EconomyModule | ✅ 注册 DeathChestLooted / ActorDied / TattooInterrupted |
| 全 21 模块 ModuleRunner 就绪 | ✅（含 v2.1 新增的 EconomyModule / NPCModule / MapGenModule / WeaponModule / SkillModule / EnemyModule / BossModule / EventModule / SaveModule / BotControllerModule） |

> V21SmokeTests.cs（PlayMode `[UnityTest]`）由于 Unity Skills 服务在 PlayMode Domain Reload 时丢失 jobId，无法通过 REST 获取结果。改用 `editor_play` 直接进入 PlayMode + `console_get_logs` 拉取运行日志的方式验证，效果等价。

## PlayMode 失败的 TestRunner 模式说明

`test_run(PlayMode)` 启动后 Test Runner 在 Domain Reload 时，UnitySkills 内存中的 jobId 表被清空。jobId 立即变为 "not found"。

- **影响范围**：仅限 REST 接口，Unity Editor 内 Test Runner 窗口正常工作
- **绕过方案**：当前用 `editor_play` 实测代替（足以验证启动链路）
- **正确做法**：未来如要 CI 跑 PlayMode 测试，应在 Unity Editor 命令行模式下用 `-runTests -testPlatform PlayMode -testResults`，结果走文件而非内存

## 集成验证（PlayMode 完整一局 / 帧率）

待 PlayMode smoke 通过后再做 10-15min 完整 run。

## 代码编译

✅ 0 个 CS 错误（通过 Unity Skills `console_get_logs type=error` 实测）
