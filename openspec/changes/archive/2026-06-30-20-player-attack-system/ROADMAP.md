---
created: 2026-07-01
status: 无人值守执行中
owner: 主对话
scope: change 18 + 19 + 20 全部走完 + 归档；codex 配合出图；失败自修复 3 轮上限
---

# 18/19/20 全链路 ROADMAP（无人值守）

> /compact 抗压：此文档是 post-compact 的唯一权威续跑入口。
> 任何 agent / 主对话续跑前必先读此文件。

## 0. 已冻结决策（全部 grill 通过）

| 范畴 | 决策 |
|---|---|
| 工作范围 | A(20收尾) + B(18) + C(19) + D(杂项) **全做** |
| change 18 升级公式 | 同武器拾取 → 升级，**3 级封顶 / 线性 +20%**：L1→L2→L3，每级 damage+20%、range+0.5m、cooldown-10% |
| change 18 拾取产生方式 | **精英/Boss 掉落 + 宝箱开出 + 商人随机刷新**（三路径） |
| change 19 polish 必做集 | **全 4 项**：暴击数字飘字+颜色 / Status 状态图标 (burn/poison/stun) / 命中 hitspark + screen shake / HP<30% 边缘血色闪烁 |
| 测试策略 | **UTF 单测 + playtest-driver E2E 双跑** |
| 失败处理 | 自行修复直到 0 错；3 轮过不去才中断用户 |
| 美术出图 | 3 status 图标 + 1 宝箱 + 1 商人 + 5 武器×2 攻击动画 (L2 模式)；玩家 sprite/武器 sprite 已存在，不重出 |
| 协作模式 | **loop + 多 agent fan-out 最大化** |

## 1. 当前实测资产盘点

- ✅ `Assets/Resources/Sprite/Weapons/` 5 武器 sprite（short_blade/heavy_hammer/pistol/bow/energy_fist）
- ✅ `Assets/Resources/Sprite/Character/Player1/{Idle,Attack,Walk,Death}/{Up,Down,Left,Right}.png` 16 张玩家方向贴图（FOLLOWUP-17 标"占位"但功能可用）
- ❌ `Assets/Resources/Prefab/Weapon/` 不存在 → PlayerWeaponMounter 走 Cube fallback；本期可建 5 个最小 prefab（空 GameObject + SpriteRenderer + Animator）

## 2. change 20 已完成的 fan-out（8/8 done）

| 任务 | Agent ID | 状态 |
|---|---|---|
| A StatusEffectModule (436 行, 单 accumulator) | client-unity | ✅ |
| B PlayerDamageReceiver (137 行, 死亡防抖) | client-unity | ✅ |
| C SkillHitResolver (订 SkillActivatedEvent + 8 EffectType 分支) | client-unity | ✅ |
| D HumanPlayerController.GetAimTarget (纯几何, score=(1-dot)*100+dist) | client-unity | ✅ |
| E InputModule+CombatModule+HeadPartBehavior+TattooModule (4 文件, 暴击走 CONTRACT §1.2 不走 crit_buff) | client-unity | ✅ |
| F StartupSelectForm (398 行) + SpawnerModule OnStartupSelected | client-unity | ✅ |
| G PlayerWeaponMounter (84 行) + SpawnerModule +3 行 | client-unity | ✅ |
| H WeaponTraitConfig.json + WeaponConfig.AimSpreadHalfDeg + balance.md (~230 行) | gd-system | ✅ |

**E 备注**：暴击路径与 CONTRACT 对齐（HeadPartBehavior 订阅 WeaponAttackHitEvent + 滚概率发 CritHitEvent），而非任务 prompt 提的 crit_buff status 路径。无 race。

**H 留待裁定**：balance.md §八 提到 brainstorm R3 "20× 上限"语义模糊，archive 前 gd-lead 确认。

## 3. 执行序列（loop 主对话按顺序推进）

每个里程碑结束后 commit + 更新此文档进度勾选。

### Phase A — change 20 收尾（约 30-60 min）
- [x] A1: 编译验证（asset_refresh + console_get_logs，0 error 才进 A2）
- [x] A2: 派 qa-engineer 写 TC-Damage-D1~D9 + 手感指标 + Pillar A 验证（UTF 单测 + playtest-driver 场景）→ 33 [Test] + 21 TC + 2 真 bug
- [x] A2.5: 修 BUG-20-02 P1（EffectAppliedEvent 加 Target）— in-flight `ae4502a7daa7cf041`
- [x] A3: gd-lead 裁定 balance.md §八 "20× 上限"语义；H 修最终值 → 20× = ≤ 200 点/链
- [ ] A4: 主对话执行 `openspec archive-change 20-player-attack-system`
- [ ] A5: 更新 `项目知识库（AI自行维护）/INDEX.md` 添加 #20 索引行
- [ ] A6: git commit

### Phase B — change 18: weapon-pickup-and-upgrade（约 90-120 min）
- [x] B1: producer 起 proposal + design（拾取圈+宝箱+商人+3级升级；引用本 ROADMAP §0 决策）→ 7 文件齐全；裁定 R1 方案 A
- [ ] B2: client-lead 公共骨架：
  - 新事件：`WeaponPickedUpEvent` / `WeaponUpgradedEvent` / `ChestOpenedEvent` / `MerchantPurchaseEvent`
  - 新模块：`WeaponSpawnerModule`（场上掉落 + 宝箱 + 商人统一出口）/ `WeaponUpgradeModule`（同武器拾取走升级）
  - 新数据：PlayerActor 加 `Dictionary<string, int> WeaponLevels`；可不动 WeaponConfig schema
  - 新 prefab：5 武器 prefab（GameObject+SpriteRenderer+BoxCollider trigger）+ 宝箱 prefab + 商人 prefab；如 codex 未出图则 fallback Cube+SpriteRenderer
  - 入口：GameApp 注册 2 新模块；SpawnerModule 加 `SpawnElites()` 标记精英
- [ ] B3: fan-out 实现（4 agent 并行）：
  - 18-A: WeaponSpawnerModule（精英掉落 + 宝箱 + 商人）
  - 18-B: WeaponUpgradeModule（拾取判定 + 升级公式 +20%/+0.5m/-10%）
  - 18-C: 拾取圈 MonoBehaviour + UI 提示
  - 18-D: gd-system 平衡（升级数值 + 商人价格 + 宝箱概率），写 balance18.md
- [ ] B4: 编译验证 + qa（TC-Pickup-01~04 + UTF + playtest E2E）
- [ ] B5: archive 18 + INDEX
- [ ] B6: git commit

### Phase C — change 19: visual-polish（约 90-120 min）
- [x] C1: producer 起 proposal（含全 4 polish 项）→ 7 文件齐全
- [x] C2: art-ui 起 prompts.md：3 status icon 64x64 + 暴击飘字字体规范（C1 合并完成）
- [x] C3: 主对话调 codex-image-gen 出 3 status icon + 2 参考图（5/5 ok；in-flight 压缩 + resize）
- [ ] C4: fan-out 实现（4 agent 并行）：
  - 19-A: client-unity 暴击数字飘字（DamageFloatTextBehaviour，订 CritHitEvent + AttackHitEvent，颜色区分）
  - 19-B: client-unity Status 头顶图标（订 StatusEffectAppliedEvent + Expired，自动加 sprite + Tween）
  - 19-C: client-ta hitspark particle + camera shake（订 AttackHitEvent）
  - 19-D: client-ta HP<30% post-process vignette shader（订 PlayerHealthChangedEvent）
- [ ] C5: 编译 + qa（TC-Polish-01~04 视觉走查 + screenshot diff）
- [ ] C6: archive 19 + INDEX
- [ ] C7: git commit

### Phase D — 杂项（约 30-45 min）
- [ ] D1: codex 出 5 张玩家 walk/death sprite（按 FOLLOWUP-17 的 _fixup_player1_walk_and_death_down.json 批次）— **延后**：codex 额度优先 phase B 宝箱+商人；Walk/Death 现 sprite 已可用
- [x] D2: BUG-17-01：`Assets/Editor/Character/AnimatorGenerator.cs` 末尾追加 `AssetDatabase.Refresh()` + `AssetDatabase.SaveAssets()`
- [ ] D3: 跑 TC-Art-02 + TC-Art-04 复验（需 Play Mode 自动化，延后到 phase B 后期统一跑）
- [ ] D4: git commit

### Phase E — 工作转圈（武器攻击动画，L2 模式，时间允许时跑）
- [ ] E1: art-director 起 5 武器 × 2 动画（普攻+蓄力）= 10 序列的 codex L2 batch
- [ ] E2: 调 codex-art-gen MCP dispatch_l2
- [ ] E3: 拆 sprite sheet（ui-asset-splitting 或 frame-ronin）
- [ ] E4: 武器 prefab Animator 接入

## 4. 失败处理 SOP（loop 内部）

```
agent 报告失败 → 主对话读日志 → 派修复 agent（最多 3 轮）
  ├─ 3 轮内修好 → 继续推进
  └─ 3 轮失败 → 写 FOLLOWUP-<NN>.md 记录 → 跳到下一个 milestone 继续
```

**仅以下情况中断用户**：
- 不可逆变更（删 schema / 改 .claude/openspec/Assets/Scripts/Core 框架核心）
- codex 配额耗尽（写 FOLLOWUP，不阻塞其他 phase）
- 与 ROADMAP §0 冻结决策**直接冲突**

## 5. 已知风险

- **R1**：change 18 武器 prefab 不存在 → PlayerWeaponMounter Cube fallback。本期 B2 阶段补 5 个最小 prefab，可缓解
- **R2**：codex 额度配合多个 phase 并发出图会耗费快 → 优先 status icon（C 必需）> 宝箱/商人（B 可 fallback）> 武器动画（E 锦上添花）
- **R3**：HumanPlayerController 评分公式 `(1-dot)*100+dist` 中 100 倍是否合适需 playtest（qa 阶段标）
- **R4**：StatusEffectModule 单 accumulator 全量遍历，敌人数 49 + 多 status 时性能预算未实测（qa 性能测试）

## 6. 续跑命令

post-compact 主对话第一句话查询：
```
读 openspec/changes/20-player-attack-system/ROADMAP.md
当前 Phase = ? 进行到哪个勾选项？继续。
```
