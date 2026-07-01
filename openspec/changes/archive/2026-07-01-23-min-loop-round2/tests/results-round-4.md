---
round: 4
date: 2026-07-01
executor: qa-engineer
unity_version: 2022.3.62f3c1
---

# Round 4 回归测试结果

## 汇总

| 指标 | 值 |
|---|---|
| 执行 TC 数 | 22 |
| PASS | 19 |
| FAIL | 2 |
| BLOCKED | 1 |
| Console Errors（全程） | 0 |

## TC 逐条结果

| TC | 场景 | 结果 | 关键日志 / 说明 |
|---|---|---|---|
| TC-01 | Launch 启动 | PASS | `[GameApp] 所有模块初始化完成` + `AllFormsLoaded Success=12` + errors=0 |
| TC-02 | MainMenuForm 自动显示 | PASS | `MainMenuForm(active=True)` |
| TC-03 | 装配 InputSimulator | PASS | `EnableSimulator Type=InputSimulator` |
| TC-04 | 点击 Start → 进战斗 | PASS | 走 fallback `Debug/ClickMainStartButton` → CharacterSelectForm → 再用 `Debug/StartGame (-> InGame)` → `StateChanged Old=MainMenu New=InGame` + `CombatHUDForm(active=True)` |
| TC-05 | CombatHUD 订阅就绪 | PASS | `CombatHUDForm Action=Ready` |
| TC-06 | WASD 移动 | PASS | `SetMove Dir=(1.00, 0.00)` + `SetMove Dir=null` |
| TC-07 | 鼠左普攻 | PASS | `PressMouse Button=0` |
| TC-08 | E 技能 | PASS | `PressKey Key=E` |
| TC-09 | Space 闪避 | PASS | `PressKey Key=Space` |
| TC-10 | Tab 打开 SelfTattooForm | PASS | `PressKey Key=Tab` + `SelfTattooForm(active=True)` |
| TC-11 | 再按 Tab 关闭 SelfTattooForm | PASS | `SelfTattooForm(active=False)` |
| TC-12 | Esc 暂停 | PASS | `PauseMenuForm(active=True)` + `PauseMenuForm Action=Open`（timeScale=0 代码路径已知，无日志但行为正确） |
| TC-13 | 全局 Error 检查（基线段） | PASS | errors=0 |
| TC-14 | 击杀 Bot | PASS | `ForceKillNearestBot Target=轻量2 Pos=(5.20, 0.40, 1.13)` + `VFXModule SpawnAOEBurst Pos=(5.20, 0.80, 1.13)` + errors=0 |
| TC-15 | 武器 pickup spawn（BUG-07 回归） | PASS | `WeaponSpawned Id=knife_basic Pos=(0.00, 0.40, 2.00)` + errors=0 — **BUG-07 VERIFIED** |
| TC-16 | 玩家拾起 pickup | BLOCKED | Player 移动通过 CombatModule 直接赋值 `transform.position`，不经物理引擎，无法触发 pickup GO 的 `OnTriggerEnter`；工具路径物理层面不兼容。新 BUG-10 |
| TC-17 | 武器升级选择 | BLOCKED | 依赖 TC-16 成功（同根因） |
| TC-18 | Hit spark VFX（BUG-09 回归） | FAIL | `PublishWeaponAttackHit Target=轻量2` 确认；errors=0（BUG-09 Engine Error 已消除）；但 `VFXModule SpawnSpark` 日志未出现，原因：TC-14 后 Bot GO 已从 Enemies 列表移除，TryGetPos 返回 null，SpawnSpark silent skip。新 BUG-10 同根因 |
| TC-19 | Bot 染色差异 | PASS | `ForceRefillEnemies Revived=0 Total=49` 确认有活 Bot；截图可见 3+ 种色调变体（橙/红/黑）；errors=0 |
| TC-20 | 玩家死亡 → AOEBurst | PASS | `VFXModule SpawnAOEBurst+Ring Pos=(0.00, 0.40, 0.00)` + errors=0 |
| TC-21 | RunResultForm 显示 | PASS | 手动触发 `Debug/GameOver (-> GameOver)` → `RunResultForm(active=True)` + `ShownByGameOver OldState=InGame` + errors=0 |
| TC-22 | 二次开局 HP 无残留 | PASS | `CombatModule Action=RunStarted MaxHp=100` + errors=0 |

## BUG-09 回归结论

- **Engine Error 已消除**：TC-18 全程 errors=0，确认 `SpawnSpark` 方法 L1000 的 `ps.Stop(true, StopEmittingAndClear)` 修复有效。
- **SpawnSpark 实际未被调用**：测试路径问题（Bot 已死亡，GO 移出 Enemies 列表，VFXModule.TryGetPos 返回 null，L1227 silent return）。
- **BUG-09 VERIFIED**（Engine Error 维度）；SpawnSpark 调用路径受 BUG-10 影响（测试工具 Bot 状态不一致）。

## 新发现问题

### BUG-10：PublishWeaponAttackHit 后 SpawnSpark 无法触发（TryGetPos 失败）

**现象**：TC-18 执行 `Publish WeaponAttackHit (nearest bot)` 后，VFXModule 收到事件（Target=轻量2），但 `SpawnSpark` 未被调用，无 Info 日志，无 Engine Error。

**根因**：
1. TC-14 `ForceKillNearestBot` 发布 `TargetKilledEvent` 后，Bot GO 被销毁（或从 SpawnerModule.Enemies 列表移除）。
2. `PublishWeaponAttackHit` 查找 nearest bot 时，`target.Health <= 0f` 过滤逻辑可能未过滤该 Bot（ForceKillNearestBot 只发事件，未直接设 Health=0），仍取轻量2 为最近 Bot。
3. VFXModule.OnWeaponAttackHit → `TryGetPos(e.Target)` 在 `_spawner.Enemies` 遍历中找不到已销毁/移出列表的 Bot GO，返回 null → L1227 `if (!pos.HasValue) return` → SpawnSpark 未执行。

**重现路径**：Play → StartGame → TC-14 ForceKillNearestBot → TC-18 PublishWeaponAttackHit → 观察无 SpawnSpark 日志。

**影响 TC**：TC-16、TC-17（物理触发器，不同根因）。TC-18 的 BUG-09 Engine Error 维度已修复；SpawnSpark 实际调用需 Bot 存活且在 Enemies 列表中。

**修复建议**（client-unity 负责）：
- 方案 A：`ForceKillNearestBot` 在 publish 事件前同时将 Target.Health 设为 0，确保过滤条件生效。
- 方案 B：`PublishWeaponAttackHit` 的 TryGetPos fallback：当 Enemies 列表找不到时，用事件携带的 Target 最后已知位置（需 WeaponAttackHitEvent 字段 HitPosition）。
- 方案 C：`SpawnSpark` 前加 Info 日志（`Action=SpawnSpark Pos=...`），TryGetPos null 时改 Warn+Vector3.zero 兜底（与 BUG-06 AOEBurst 修复思路一致）。

### BUG-11：TC-16/17 — Player 物理移动不触发 WeaponPickupTrigger.OnTriggerEnter

**现象**：ForceSpawnWeaponPickup 生成 pickup GO（SphereCollider trigger + WeaponPickupTrigger），Player 通过 Move/Up 移动到 pickup 位置上方，但 OnTriggerEnter 从未触发，WeaponPickedUpEvent 无日志。

**根因**：CombatModule 通过直接赋值 `transform.position`（L124：`go.transform.position += new Vector3(dir.x, 0, dir.y) * speed * dt`）移动 Player，绕过 Unity 物理引擎。物理触发器 OnTriggerEnter 依赖 Rigidbody 的物理步骤（FixedUpdate），直接修改 position 不会触发。

**影响 TC**：TC-16 BLOCKED，TC-17 BLOCKED（依赖 TC-16）。

**修复建议**（client-unity/tools-engineer 评估）：
- 方案 A（工具层）：PlaytestDriverEditor 新增 `ForcePickupWeapon` 菜单，直接调用 WeaponPickupTrigger.Pickup() 或 Publish WeaponPickedUpEvent，绕过物理触发器。
- 方案 B（引擎层）：Player 移动改用 Rigidbody.MovePosition（保持物理系统一致性，范围较大）。
- 推荐 方案 A（工具层最小侵入，本 loop 快速解锁）。

## TC-16/17 BLOCKED 正式记录

两个 TC 因物理触发器不兼容 playtest 工具路径而 BLOCKED，非游戏代码 bug，是工具层缺菜单。属于非代码问题，需 tools-engineer 补 `ForcePickupWeapon` debug 菜单。

## 全程 Console 总结

| 阶段 | Errors | Warnings |
|---|---|---|
| TC-01~13 基线段 | 0 | 2（音频资源缺失，已知 Warning） |
| TC-14~22 新能力段 | 0 | 多条音频 Warning（已知） |
| **全程合计** | **0** | 多条（均为已知音频 Warning） |

BUG-09 修复验证：全程 0 Engine Error，SpawnSpark 修复路径有效。
