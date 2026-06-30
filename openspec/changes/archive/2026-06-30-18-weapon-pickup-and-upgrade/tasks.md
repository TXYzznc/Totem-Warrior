# tasks — 18-weapon-pickup-and-upgrade

> **并行策略**：B2（骨架）完成后，18-A / 18-B / 18-C / 18-D 四个子任务并行 fan-out。
> CONTRACT.md 是所有 agent 的唯一接口约定，不经 client-lead 裁定不得修改。

---

## B2 — 公共骨架（前置，必须先完成）

| 属性 | 值 |
|---|---|
| **Agent** | client-lead |
| **输入** | CONTRACT.md / design.md |
| **输出** | `Assets/Scripts/Events/WeaponPickupEvents.cs`（4 新事件）; 空壳 `WeaponSpawnerModule.cs` + `WeaponUpgradeModule.cs`; `Assets/Scripts/DataTable/WeaponDropConfig.cs` + `ChestConfig.cs` + `MerchantConfig.cs`（DataTableGenerator 跑后自动生成）; `Assets/Scripts/Modules/Economy/Actor.cs` 追加 WeaponLevels 字段; `GameApp.cs` 追加 2 行 AddModule |
| **估算** | 3 SP（4-6h） |
| **Deadline** | 2026-07-02 |
| **阻塞** | 18-A / 18-B / 18-C 全部依赖本任务完成后才能并行开工 |
| **验收** | 编译通过（0 error），4 事件可引用，2 空壳模块 InitializeAsync 返回 UniTask.CompletedTask |

---

## 18-A — WeaponSpawnerModule 实现

| 属性 | 值 |
|---|---|
| **Agent** | client-unity |
| **输入** | CONTRACT.md §A / design.md §3.1 / B2 产出的空壳文件 |
| **输出** | `Assets/Scripts/Modules/Weapon/WeaponSpawnerModule.cs`（完整实现）; 5 个武器拾取 prefab fallback（Cube+SpriteRenderer，路径 `Assets/Resources/Prefab/Weapon/Pickup/`）; 1 个宝箱 prefab fallback; 1 个商人 prefab fallback |
| **估算** | 5 SP（1-2 天） |
| **Deadline** | 2026-07-03 |
| **阻塞** | 依赖 B2；美术 prefab 可 fallback 不阻塞 |
| **验收** | EnemyDeadEvent(isElite=true) → 场上出现拾取 GO；ChestOpenedEvent 正确结算奖励；MerchantPurchaseEvent 扣金；Pickup GO 在 OnWeaponPickedUp 后 Destroy |

---

## 18-B — WeaponUpgradeModule 实现

| 属性 | 值 |
|---|---|
| **Agent** | client-unity |
| **输入** | CONTRACT.md §B / design.md §3.2 / B2 产出的空壳文件 |
| **输出** | `Assets/Scripts/Modules/Weapon/WeaponUpgradeModule.cs`（完整实现）; `Assets/Tests/WeaponUpgradeTests.cs`（TC-Pickup-01~04 UTF 单测） |
| **估算** | 5 SP（1-2 天） |
| **Deadline** | 2026-07-03 |
| **阻塞** | 依赖 B2；与 18-A 并行 |
| **验收** | TC-Pickup-01~04 全部 UTF 通过；GetMultipliers(L2) 精确到 0.001；满级转化事件正确触发 |

---

## 18-C — 拾取圈 UI + InputModule Pickup Action

| 属性 | 值 |
|---|---|
| **Agent** | client-unity |
| **输入** | CONTRACT.md §C / design.md §3.3 §9 / B2 产出文件 |
| **输出** | `Assets/Scripts/Modules/Weapon/WeaponPickupTrigger.cs`（MonoBehaviour）; `Assets/Scripts/Modules/Weapon/ChestInteractTrigger.cs`; `Assets/Scripts/Modules/Weapon/MerchantTrigger.cs`; InputModule 新增 Pickup Action（KeyCode.F）; 世界 UI 提示（TextMeshPro WorldSpace，"[F] 拾取"） |
| **估算** | 3 SP（4-6h） |
| **Deadline** | 2026-07-03 |
| **阻塞** | 依赖 B2；与 18-A / 18-B 并行 |
| **验收** | 玩家走入 trigger 范围显示提示；按 F 发布正确事件；离开 trigger 隐藏提示；宝箱开后 isOpened=true 不重复交互 |

---

## 18-D — gd-system 数值平衡

| 属性 | 值 |
|---|---|
| **Agent** | gd-system |
| **输入** | design.md §2.3 §2.4 §2.5 / ROADMAP §0 升级公式 / 现有 WeaponConfig.json |
| **输出** | `Assets/Resources/DataTable/WeaponDropConfig.json`（初始数值）; `Assets/Resources/DataTable/ChestConfig.json`; `Assets/Resources/DataTable/MerchantConfig.json`; `openspec/changes/18-weapon-pickup-and-upgrade/balance18.md`（数值设计说明 + 平衡理由） |
| **估算** | 2 SP（2-4h） |
| **Deadline** | 2026-07-03 |
| **阻塞** | 不依赖 B2，可最早开工；数值不影响编译 |
| **验收** | ChestConfig 同 ChestId 概率总和=100；WeaponDropConfig Weight 全为正整数；MerchantConfig GoldCost 在 [50, 200] 范围；balance18.md 解释每个关键值的设计理由 |
