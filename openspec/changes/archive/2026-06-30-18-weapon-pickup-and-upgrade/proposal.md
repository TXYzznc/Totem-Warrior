# proposal — 18-weapon-pickup-and-upgrade

**版本**: v1.0
**创建日期**: 2026-07-01
**作者**: producer
**状态**: proposed
**决策来源**: `openspec/changes/20-player-attack-system/ROADMAP.md §0`（已冻结，不再讨论）

---

## Why

### 现状痛点

change 20 完成了完整的攻击系统（WeaponModule / CombatModule / TattooModule 全链路），
但武器始终是开局固定分配，场上不存在"掉落武器"、"宝箱"、"商人"概念：

- 玩家无法在 run 中途获得/切换武器，roguelite 核心反馈回路缺失
- 武器没有等级概念，重复捡到同种武器毫无意义
- 场上实体种类（精英/宝箱/商人）都未实装

### 解决的玩家问题

> "我终于打死了精英，应该得到奖励——但什么都没掉。"

拾取 + 升级系统直接解决：击败精英→掉落武器→拾取→当前武器升级这一正反馈回路。

### 砍掉会损失什么

砍掉武器拾取/升级 = roguelite 的 run-to-run 多样性归零，保留这一系统是 change 19（视觉 polish）
和后续 change 的前置依赖。

---

## What Changes

### 新增模块（两个）

| 模块 | 职责 |
|---|---|
| `WeaponSpawnerModule` | 场上武器实体的统一出口：精英掉落、宝箱开出、商人刷新 |
| `WeaponUpgradeModule` | 拾取判定 + 升级公式（同武器 → 升级；3 级封顶；线性 +20%） |

### 新增事件（四个）

| 事件 | 发布时机 |
|---|---|
| `WeaponPickedUpEvent` | 玩家与武器拾取圈重叠并确认拾取 |
| `WeaponUpgradedEvent` | 同类武器触发升级（1→2 或 2→3） |
| `ChestOpenedEvent` | 玩家交互宝箱，宝箱开出武器/金币 |
| `MerchantPurchaseEvent` | 玩家在商人处花费金币购买武器 |

### 数据扩展

- `PlayerActor` 新加 `Dictionary<string, int> WeaponLevels`（默认值 1，上限 3）
- 新配置表：`WeaponDropConfig.json` / `MerchantConfig.json` / `ChestConfig.json`
- 旧 `WeaponConfig.json` 不动 schema（fields[] 顺序冻结于 change 20 CONTRACT §4.1）

### Prefab

- 5 个武器拾取 prefab（空 GameObject + SpriteRenderer + BoxCollider trigger）
- 1 个宝箱 prefab（SpriteRenderer + BoxCollider trigger + Animator）
- 1 个商人 prefab（SpriteRenderer + BoxCollider trigger + NPC 交互脚本）
- 美术未出图时全部 fallback Cube + SpriteRenderer（不阻塞编程）

### 范围（Scope）

**本期做**：
- 三路径拾取（精英掉落 / 宝箱 / 商人）
- 升级公式：damage×1.2 / range+0.5m / cooldown×0.9，3 级封顶
- 拾取圈 MonoBehaviour + 世界 UI 提示
- gd-system 数值平衡（掉落概率 / 商人价格 / 宝箱概率）

**本期不做**（非范围）：
- 武器攻击动画（→ ROADMAP Phase E）
- 武器合成 / 分解系统
- 多武器槽（当前单槽 MVP）
- 商人刷新 CD / 贴图精度提升（留 change 19 polish）

---

## Definition of Done

- [ ] WeaponSpawnerModule 编译通过，精英死亡后场上出现拾取圈
- [ ] WeaponUpgradeModule 升级公式 UTF 单测全过（TC-Pickup-01~04）
- [ ] 拾取圈与玩家重叠后显示"[F] 拾取"提示，按 F 触发 WeaponPickedUpEvent
- [ ] 宝箱可交互，开出武器或金币（按 ChestConfig 概率）
- [ ] 商人显示当前刷新武器，玩家有足够金币可购买（MerchantConfig 价格）
- [ ] playtest-driver E2E：玩家打死精英 → 拾取武器 → UI 显示武器名 + 等级标记
- [ ] WeaponLevels 最大值不超过 3（边界条件）

---

## 里程碑

| 里程碑 | 日期 | 内容 |
|---|---|---|
| B1 骨架 | 2026-07-01 | proposal/design/tasks/CONTRACT 完成（本文件） |
| B2 公共骨架 | 2026-07-02 | client-lead 建空壳模块 + 事件文件 + Prefab fallback |
| B3 fan-out | 2026-07-03 | 4 个子任务并行实现 |
| B4 验证 | 2026-07-04 | 编译 + UTF + playtest E2E |
| B5 归档 | 2026-07-04 | archive + INDEX 更新 |
