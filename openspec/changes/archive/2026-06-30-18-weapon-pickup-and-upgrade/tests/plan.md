# QA 测试计划 — change#18 weapon-pickup-and-upgrade

**版本**: v1.0  
**日期**: 2026-07-01  
**测试范围**: WeaponSpawnerModule / WeaponUpgradeModule / Trigger MB / CombatModule 倍率注入

---

## 1. QA 策略（测试金字塔）

| 层级 | 数量 | 工具 | 状态 |
|---|---|---|---|
| 单元（EditMode UTF） | 4 | NUnit + 反射注入 | 已实装（`Assets/Tests/WeaponUpgradeTests.cs`） |
| 集成（PlayMode 手动） | 5 路径 | PlaytestDriverEditor 菜单 | 手动（REST QUEUE_FULL，不自动跑） |
| E2E（完整 Run 流程） | 1 | PlaytestDriverEditor | 手动 |

> 单元覆盖公式精确性；PlayMode 覆盖 Trigger 触发链 + 事件传播；E2E 验证满级转金逻辑闭环。

---

## 2. UTF 单元测试验收点（TC-Pickup-01 ~ 04）

文件：`Assets/Tests/WeaponUpgradeTests.cs`

| TC 编号 | 方法名 | 验收点 | 公式参考 |
|---|---|---|---|
| TC-Pickup-01 | `GetWeaponLevel_LazyDefault_Returns1` | 新 Actor 无记录 → GetWeaponLevel 返回 1 | — |
| TC-Pickup-02 | `TryUpgrade_FirstPickup_Level2_CorrectMultipliers` | 首次同类拾取 → L1→L2，DamageMul≈1.2，RangeAdd=0.5，CooldownMul≈0.9，误差≤0.001 | 1.2^1 / 0.5*1 / 0.9^1 |
| TC-Pickup-03 | `TryUpgrade_SecondPickup_Level3_CorrectMultipliers` | 第二次同类拾取 → L2→L3，DamageMul≈1.44，RangeAdd=1.0，CooldownMul≈0.81，误差≤0.001 | 1.2^2 / 0.5*2 / 0.9^2 |
| TC-Pickup-04 | `TryUpgrade_AtMaxLevel_ReturnsFalse_NoEvent` | 已 L3 再拾取 → TryUpgrade 返回 false，不发 WeaponUpgradedEvent | — |

**通过标准**：4 例全绿，0 Error，无 float 误差超 0.001。

---

## 3. 手动 PlayMode 测试路径

### 前置条件
- 退出 Play Mode，等编译完成，再 editor_play
- 调 `Tools/Playtest/01 Enable Simulator` 装配 InputSimulator

### PT-01：精英掉落 → 拾取 → 升级 → 满级转金

1. 进入含精英怪房间（或 Debug 菜单直接 Spawn 精英）
2. 精英死亡 → 检查 Console 出现 `[WeaponSpawnerModule` `Action=SpawnEliteWeapon`（当前为 no-op 占位，预期 Warn 日志）
3. 玩家走近武器 GO → `WeaponPickupTrigger` 进入触发范围
4. 调 `Tools/Playtest/Press/F (Pickup)` → 检查 Console: `WeaponPickedUpEvent` `WeaponId=<id>`
5. 再次拾取同 WeaponId → 检查 `WeaponUpgradedEvent` `NewLevel=2`
6. 第三次拾取 → `WeaponUpgradedEvent` `NewLevel=3`
7. 第四次拾取 → 检查 TryUpgrade 无 WeaponUpgradedEvent；检查转金逻辑（EconomyModule 反射兜底日志）

**期望日志关键字**：`WeaponPickedUpEvent` / `WeaponUpgradedEvent NewLevel=2` / `WeaponUpgradedEvent NewLevel=3`  
**通过标准**：命中上述关键字，0 Error（已知限制日志除外，见 bugs.md）

---

### PT-02：宝箱开启 → 武器 Spawn

1. 玩家走近宝箱 GO → `ChestInteractTrigger` 进入触发范围
2. 调 `Tools/Playtest/Press/F (Pickup)` → 检查 Console: `ChestOpenedEvent` `RewardType=Weapon`
3. 检查场景中 Spawn 出武器 GO（WeaponSpawnerModule 订阅 `OnChestOpened`）
4. 重复点击 F → 检查 `isOpened` flag 防止重复，无第二次 `ChestOpenedEvent`

**通过标准**：单次 ChestOpenedEvent，Spawn GO 可见，重复按键无响应，0 Error

---

### PT-03：商人购买 → 扣金 + 拾取

1. 走近商人 NPC → `MerchantTrigger` 显示商店 UI
2. 点击购买按钮（金币充足） → Console: `MerchantPurchaseEvent` `GoldCost=<n>`
3. 检查金币减少（EconomyModule.DeductGold 反射兜底，日志中含 fallback Warn）
4. 检查后续自动发 `WeaponPickedUpEvent`（WeaponSpawnerModule.OnMerchantPurchase 流程）

**通过标准**：MerchantPurchaseEvent + WeaponPickedUpEvent 均出现，Warn 为已知限制

---

### PT-04：Trigger 双路径冲突检测

1. 同时触发 `WeaponPickupTrigger`（走进范围）和 `WeaponSpawnerModule.OnWeaponPickedUp`（事件订阅）
2. 验证 GO 不被双重销毁（无 `MissingReferenceException`）
3. 距离匹配阈值 0.5m：玩家在 0.5m 内按 F → 拾取成功；超出 0.5m 按 F → 无响应

**通过标准**：0 MissingReferenceException，距离门控行为符合预期

---

### PT-05：CombatModule 倍率注入验证

1. 玩家拾取同一武器升至 L2 → Console: `WeaponUpgradedEvent DamageMul=1.2`
2. 触发攻击（`Tools/Playtest/Press/MouseLeft (Attack)`）
3. 检查 Console: `CombatModule` `_pendingMul` 应用，伤害数值约为基础值 × 1.2（误差 ≤ 5%）

**通过标准**：倍率日志与升级事件值一致，无 NullReferenceException

---

## 4. PlaytestDriverEditor 自动化路线

> 当前 REST QUEUE_FULL，以下为待跑路线，主对话稍后调度。

```bash
# STEP 1 — 清 Console 并进 Play
curl -s -X POST http://localhost:8091/skill/console_clear -H 'Content-Type: application/json' -d '{}'
curl -s -X POST http://localhost:8091/skill/editor_play -H 'Content-Type: application/json' -d '{}'

# STEP 2 — 装配 InputSimulator
curl -s -X POST http://localhost:8091/skill/editor_execute_menu \
  -H 'Content-Type: application/json' -d '{"menuPath": "Tools/Playtest/01 Enable Simulator"}'

# STEP 3 — PT-01 精英拾取链
# （需 Debug 菜单 Spawn 精英，或人工进精英房）
curl -s -X POST http://localhost:8091/skill/editor_execute_menu \
  -H 'Content-Type: application/json' -d '{"menuPath": "Tools/Playtest/Press/F (Pickup)"}'
sleep 0.5
curl -s -X POST http://localhost:8091/skill/console_get_logs \
  -H 'Content-Type: application/json' -d '{"filter": "WeaponPickedUpEvent", "limit": 10}'

# STEP 4 — 抓 Error 收尾
curl -s -X POST http://localhost:8091/skill/console_get_logs \
  -H 'Content-Type: application/json' -d '{"type": "Error", "limit": 30}'
curl -s -X POST http://localhost:8091/skill/editor_execute_menu \
  -H 'Content-Type: application/json' -d '{"menuPath": "Edit/Play"}'
```

> F (Pickup) 键已在 `InputModule.IsPickupPressed()` 注册，PlaytestDriverEditor 需确认 `Press/F (Pickup)` 菜单项存在；若未注册，退回人工按 F 键验证。

---

## 5. 验收门槛

| 门槛 | 指标 | 是否阻塞归档 |
|---|---|---|
| UTF 4 例全绿 | 0 Fail / 0 Error | 是 |
| PT-01 拾取升级链 PASS | 关键日志命中 + 0 NullRef | 是 |
| PT-02/03 宝箱 + 商人 PASS | 已知 Warn 可接受 | 否（已知限制） |
| PT-04 双路径无崩溃 | 0 MissingReferenceException | 是 |
| PT-05 倍率注入 PASS | 伤害倍率误差 ≤ 5% | 否（建议修复） |
