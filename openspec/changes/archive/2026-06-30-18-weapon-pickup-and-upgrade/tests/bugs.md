# Bug 追踪 — change#18 weapon-pickup-and-upgrade

**版本**: v1.0  
**日期**: 2026-07-01  
**状态说明**: 🟡 已知限制 / 🔴 阻塞 / 🟠 高 / 🟡 中 / 🟢 低 / ✅ 已修

---

## Bug 表

| 修复状态 | BugId | Severity | Title | Repro | Expected | Actual | Fix |
|---|---|---|---|---|---|---|---|
| 🟡 已知限制 | KL-18-01 | P3 | SpawnElites() 占位 no-op，精英不实际掉落武器 | 精英死亡后检查场景，无武器 GO 生成 | 精英死亡按 EliteDropConfig 概率 Spawn 武器 GO | `SpawnElites()` 方法体为空（占位注释） | 补全精英标记逻辑（ROADMAP §0 polish 范围） |
| 🟡 已知限制 | KL-18-02 | P3 | EconomyModule.DeductGold(Target,int) 不存在，商人扣金失效 | 商人购买 → Console 含反射兜底 Warn，金币实际未扣减 | 购买后金币正确扣除 | WeaponSpawnerModule 用反射尝试调用 DeductGold，方法不存在时跳过 | EconomyModule 补全 DeductGold 接口后删除反射兜底 |
| 🟡 已知限制 | KL-18-03 | P3 | MapGenModule.GetCurrentRoomIndex() 不存在，房间进度门控未生效 | 任意房间均可触发精英/宝箱 Spawn，不受房间进度限制 | 仅进度满足条件的房间才触发 Spawn | WeaponSpawnerModule 反射调用失败，fallback 返回 1（永远满足条件） | MapGenModule 补全 GetCurrentRoomIndex 后删除 fallback |
| 🟡 已知限制 | KL-18-04 | P3 | WeaponPickupTrigger 销毁 GO + WeaponSpawnerModule.OnWeaponPickedUp 可能双路径竞争 | 玩家拾取时 Trigger 销毁 GO，同帧 SpawnerModule 订阅事件也尝试处理，距离匹配 0.5m 与 Destroy 顺序未明确 | GO 被销毁一次，无 MissingReferenceException | 偶发 MissingReferenceException（概率较低，取决于帧序） | 在 SpawnerModule.OnWeaponPickedUp 内加 null 检查；或统一由 SpawnerModule 负责销毁，Trigger 仅发事件 |

> 以上 4 条均属于用户确认的「ROADMAP §0 polish 范围」，**不阻塞 change#18 归档**。  
> 状态标记：🟡 已知限制，不阻塞归档

---

## 新增 Bug（测试过程中发现后填入）

| 修复状态 | BugId | Severity | Title | Repro | Expected | Actual | Fix |
|---|---|---|---|---|---|---|---|
| — | — | — | — | — | — | — | — |

---

## Bug 严重度定义

| 级别 | 含义 |
|---|---|
| P1 Critical | 游戏崩溃 / 数据丢失 / 完全阻塞主流程 |
| P2 High | 核心功能不可用，无合理绕过方式 |
| P3 Medium | 功能有缺陷但可绕过，或仅影响非核心路径 |
| P4 Low | 视觉 / 文案 / 轻微 UX 问题 |
| 已知限制 | 有意识地推迟实现，已记录在 ROADMAP |
