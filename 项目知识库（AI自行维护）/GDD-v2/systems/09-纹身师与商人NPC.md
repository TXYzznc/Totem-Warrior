# 09-纹身师与商人 NPC

> **v2.1 修订日期**: 2026-06-25
> **主导 Agent**：gd-system
> **协作 Agent**：gd-lead（体验目标审定）/ level-designer（工作室节点布局）/ client-unity（NPCModule 实现）
> **依赖系统**：01-纹身构筑系统 / 03-武器系统 / 04-主动技能（技能槽 2 个）/ 11-经济系统 / 12-数值平衡 / 13-UI/HUD / 16-BotControllerModule
> **事件契约**：CONTRACT.md §1.5（NPCInteractStartEvent / TattooSessionEndEvent / ShopPurchaseEvent / ShopRefreshEvent）+ 本次新增见 §七

---

## 一、玩家体验目标

本节涉及三条独立流程，体验目标各异：

| 流程 | 发生地点 | 核心体验 |
|---|---|---|
| **玩家自刻纹身** | 任意地点（Tab 键） | "读条 = 短暂脆弱"——找时机刻，感觉像战场上的赌注 |
| **纹身师附魔** | 工作室内 | "花稀有颜料赌词缀"——高收益随机感，类 PoE 混沌石 |
| **商人交易** | 商人摊位 | "应急购物 5 秒内决策"——武器 / 技能 / 颜料 / 消耗品一站补给 |

**体验目标**：
- 任意地点都能刻纹身（不锁工作室），但专程去工作室是为了附魔这个增益层。
- 第一次附魔失败（随机词缀不理想）应让玩家说"再来一次"而非"这系统烂"——靠刮除剂保底。
- 商人货架 5 秒扫完，不做深度比较；购买决策是压力下的直觉反应。

---

## 二、核心机制

### 2.1 玩家自刻纹身（任意地点，Tab 触发）

本流程与纹身师无关，玩家随时可用。

**前置条件**：
```
player.inkInventory[colorId] >= INK_CONSUME_COUNT(colorId)   // 颜料足够
player.recipeUnlocked(colorId, patternId) == true             // 已解锁图案配方
player.coinTotal >= 0                                         // 金币无要求（中断才扣）
player.tattooSlot[partId].isEquipped == false                 // 目标部位空槽
```

**执行流程**：
```
1. 按 Tab → 弹出纹身界面（见 §五 5.1）
2. 玩家选择 (partId, colorId, patternId) 并确认
3. 发布 TattooInProgressEvent { Owner=player, PartId, ColorId, PatternId, DurationSec }
4. 进入读条（部位决定时长，见下表）；期间：
   a. 玩家失去移动和攻击控制权
   b. 若被攻击 / 主动移动 / 死亡 → 读条中断，扣金币 50，颜料不扣
      → 发布 TattooCancelledEvent { Owner=player, Reason=Attacked|Moved|Died }
5. 读条完成 → 扣除颜料（INK_CONSUME_COUNT 瓶）
6. 调用 TattooModule.Equip(partId, colorId, patternId)
7. 发布 TattooFinishedEvent { Owner=player, NewSlot }
8. 关闭进度条 UI，玩家恢复控制
```

**读条时长与颜料消耗（部位决定）**：

| 部位 | 读条时长 (s) | 颜料消耗 (瓶) | 设计意图 |
|---|---|---|---|
| 脑袋 | 8 | 4 | 最强部位，代价最高 |
| 躯干 | 8 | 4 | 最强部位，代价最高 |
| 左臂 | 5 | 3 | 中等 |
| 右臂 | 5 | 3 | 中等 |
| 左腿 | 3 | 2 | 低代价快速上身 |
| 右腿 | 3 | 2 | 低代价快速上身 |

**中断惩罚**：仅扣 50 coin，颜料全额返还。设计意图：不让玩家因一次被打断损失大量颜料资源，但提供轻微金币摩擦感。

**边界与异常**：
- 金币 < 50 也可发起刻纹身（中断才需要扣），金币为 0 时中断发生则变为负数；实现时钳制为 0（不允许负金币）。
- 同一部位已刻 → 新刻前必须先清除（清除走商人"刮除剂"流程，见 §2.3）。
- 配方未解锁 → UI 内图案显示锁图标，无法选择。

---

### 2.2 纹身师附魔流程（工作室触发）

纹身师不刻纹身，只做词缀附魔。词缀是在已刻纹身基础上叠加的随机加成层。

**词缀系统基本规则**：
- 每个已刻纹身最多持有 **2 个词缀槽**，满了不能再附魔。
- 每次附魔随机从对应部位的词缀池（≥ 8 条）中抽取 **1 个词缀**；如词缀已存在则跳过重抽（最多重试 3 次后仍重复则返回错误提示）。
- 要清空词缀槽须使用"刮除剂"消耗品（从商人购买），清空后可重新附魔。

**词缀参考池（按部位，各举 3–4 例）**：

| 部位 | 示例词缀 |
|---|---|
| 脑袋 | 冷却缩短 -10% / 技能充能 +1 / 施法后无敌 0.3s |
| 躯干 | 最大 HP +80 / 受击伤害 -12% / 格挡触发回血 +30 |
| 左臂 | 技能伤害 +18% / 闪电沾附 +20% / 冷却缩短 -8% |
| 右臂 | 普攻伤害 +15% / 火伤 +15% / 距离>8m 攻击 +30% |
| 左腿 | 闪避距离 +20% / 闪避后 1s 无敌 / 冲刺消耗 -1 |
| 右腿 | 移速 +10% / 落地震荡半径 +1m / 跑步时伤害减免 +8% |

> 完整词缀表由 `TattooEnchantAffixConfig.json` 定义（见 §四）。

**附魔花费公式**：

```
enchantCost(colorTier) = BaseCoinCost(colorTier) + RarePigmentCost
```

| 颜料档 | 颜色 | 金币花费 | 稀有颜料 | 设计意图 |
|---|---|---|---|---|
| 常见 | 红 / 黄 / 绿 / 蓝 | 200 | 稀有颜料 ×1 | 中局（t=6-8 min）可及 |
| 稀有 | 紫 / 金 | 350 | 稀有颜料 ×1 | 后期（t=12+ min）合理 |
| 传说 | 白 | 500 | 稀有颜料 ×1 | 后期高价值赌注 |

稀有颜料固定消耗 1 瓶（无论颜料档），金币随颜料档升阶。稀有颜料本身通过商人购买（见 §2.3）或箱子掉落获得。

**平衡校验**：
- 常见档附魔 200 coin + 1 瓶稀有颜料。单局"不死亡不击杀"基线金币约 600-800 coin（引用 12-数值平衡），200 coin 约占 25-33%，属中等消耗；稀有颜料稀缺度由掉落率控制，是真正的限制因子。
- 一个纹身 2 词缀上限防止附魔层叠无上限。满槽后玩家必须花费刮除剂（商人购买 180 coin）才能重刷，形成"花钱赌——不满意——再花钱"的决策循环，不允许无限低成本刷新。

**执行流程**：
```
前置条件：
  player.coinTotal >= enchantCost(colorTier)
  player.rarePigmentCount >= 1
  tattooSlot[partId].affixCount < 2       // 还有词缀槽
  session.isEnchanting == false

1. 走近纹身师按交互键 → 发布 NPCInteractStartEvent { Interactor=player, Npc=tattooist }
2. 弹出附魔界面（见 §五 5.2）
3. 玩家选择已刻部位（只展示当前局内已刻的部位）
4. 确认 → 扣金币 + 稀有颜料 1 瓶
5. 随机抽取词缀（从 TattooEnchantAffixConfig 对应 PartId + ColorId 池）
6. 写入 TattooSlot.affixes[]
7. 发布 TattooEnchantedEvent { Owner=player, Slot, NewAffixes }
8. 关闭 UI，玩家恢复控制
```

**工作室并发限制**：同一工作室每次只服务 1 个客户（占用 + 排队）。后来者收到"正忙"提示，等待上限 8s，超时后 UI 关闭玩家恢复控制。AI 不等待，直接寻找次近工作室。

---

### 2.3 商人交易

商人不卖配方、不卖附魔服务。商人只卖：

| 类目 | 品类 | 备注 |
|---|---|---|
| **颜料** | 常见档（红/黄/绿/蓝）| 普通颜料，用于自刻纹身 |
| **武器** | 本局可用武器 | 对接 03-武器系统 GDD |
| **技能** | 本局可用技能（最多配置 2 槽） | 对接 04-主动技能 GDD（**技能槽 v2.1 改为 2 个**）|
| **消耗品** | 解药 / 修复包 / 刮除剂 | 刮除剂清空某纹身的全部词缀供重刷 |
| **主题专属** | 外星区商人有稀有颜料（紫金）×3 库存 | 稀有颜料也是附魔消耗品 |

**购买流程**：
```
前置条件：
  player.coinTotal >= item.price
  item.stockCount > 0

1. 发布 NPCInteractStartEvent { Interactor=player, Npc=merchant }
2. 玩家选择商品 → 确认
3. 扣金币，物品加入背包，item.stockCount--
4. 发布 ShopPurchaseEvent { Buyer=player, ItemId, CostCoin }
```

**出售流程**：
```
玩家出售物品（背包物品，不含已刻纹身）：
  sellPrice = item.basePrice × 0.4（取整）
  金币 += sellPrice
  发布 CoinChangedEvent { Owner=player, Delta=+sellPrice, Reason=SellItem }
```

**库存刷新**：每局初始刷新一次。玩家可花 80 coin 手动刷新，每局限 1 次。刷新后发布 `ShopRefreshEvent { Shop=merchant, NewStock }`。

---

## 三、与其他系统的耦合

| 系统 | 耦合方式 | 依赖方向 |
|---|---|---|
| **01-纹身构筑系统** | 自刻最终调用 `TattooModule.Equip(partId, colorId, patternId)`；附魔写入 `TattooSlot.affixes[]` | 09 → 01 |
| **03-武器系统** | 商人武器库存类型、价格范围与 03 中武器 BaseDamage / ItemConfig 对齐 | 09 → 03 |
| **04-主动技能** | 商人出售技能槽数上限 = 2（v2.1 修订），技能 ID 对应 SkillConfig | 09 → 04 |
| **11-经济系统** | 所有金币变化通过 `CoinChangedEvent` 记录；`EconomyModule` 追踪颜料消耗与附魔花费流向 | 09 ↔ 11 |
| **12-数值平衡** | 附魔花费基准 / 读条中断扣 50 coin / 刮除剂 180 coin 均以 BalanceConstantsConfig 为参数来源 | 09 → 12 |
| **13-UI/HUD** | 自刻界面（Tab UI）/ 附魔界面 / 商人界面 = 3 个独立 UIForm；统一由 UIModule 管理 | 09 → 13 |
| **16-BotControllerModule** | 智能 AI 自刻纹身（任意地点）/ 去工作室附魔 / 访问商人，三条行为流程独立（见 §六）| 09 → 16 |
| **07-地图生成** | 工作室节点布局（3 个工作室 + 2 个商人），位置按区域分布（见 §七）| 09 → 07 |
| **08-宝箱与探财** | 颜料（含稀有颜料）、图案配方从箱子掉落，与商人购买形成双渠道 | 09 并列 08 |

---

## 四、数值与配置

### NPCConfig.json

**路径**：`Assets/Resources/DataTable/NPCConfig.json`

```json
{
  "table": "NPCConfig",
  "fields": [
    { "name": "NPCId",           "type": "string", "desc": "NPC 唯一 ID" },
    { "name": "Type",            "type": "string", "desc": "枚举：Tattooist | Merchant" },
    { "name": "MapTheme",        "type": "string", "desc": "所属地图主题（All / Slum / Lab / Alien）" },
    { "name": "ThemePriceMul",   "type": "float",  "desc": "主题价格倍率（×）；普通区 1.0，实验室 1.1，外星区 1.2" },
    { "name": "ShopStockTable",  "type": "string", "desc": "关联库存抽取表 Key（对应 ShopStockConfig.json TableId）；纹身师留空" },
    { "name": "ExclusiveItemIds","type": "int[]",  "desc": "专卖稀有物品 ID 列表（外星区商人填稀有颜料 ID）" },
    { "name": "GuardSpawnId",    "type": "string", "desc": "被攻击时生成的警卫怪 PrefabId" },
    { "name": "GuardCount1",     "type": "int",    "desc": "首次被攻击生成警卫数量（只）" },
    { "name": "GuardCount2",     "type": "int",    "desc": "30s 内再次被攻击生成警卫数量（只）" },
    { "name": "ServiceCooldown", "type": "float",  "desc": "被攻击后关闭服务时长（s）" },
    { "name": "InteractRadius",  "type": "float",  "desc": "触发交互提示的距离（m）" },
    { "name": "GuardRadius",     "type": "float",  "desc": "警卫怪巡逻半径（m）" }
  ],
  "rows": [
    {
      "NPCId": "tattooist_default", "Type": "Tattooist", "MapTheme": "All",
      "ThemePriceMul": 1.0, "ShopStockTable": "", "ExclusiveItemIds": [],
      "GuardSpawnId": "guard_medium", "GuardCount1": 2, "GuardCount2": 4,
      "ServiceCooldown": 60.0, "InteractRadius": 3.0, "GuardRadius": 8.0
    },
    {
      "NPCId": "tattooist_lab", "Type": "Tattooist", "MapTheme": "Lab",
      "ThemePriceMul": 1.1, "ShopStockTable": "", "ExclusiveItemIds": [],
      "GuardSpawnId": "guard_medium", "GuardCount1": 2, "GuardCount2": 4,
      "ServiceCooldown": 60.0, "InteractRadius": 3.0, "GuardRadius": 8.0
    },
    {
      "NPCId": "tattooist_alien", "Type": "Tattooist", "MapTheme": "Alien",
      "ThemePriceMul": 1.2, "ShopStockTable": "", "ExclusiveItemIds": [],
      "GuardSpawnId": "guard_heavy", "GuardCount1": 2, "GuardCount2": 3,
      "ServiceCooldown": 60.0, "InteractRadius": 3.0, "GuardRadius": 8.0
    },
    {
      "NPCId": "merchant_default", "Type": "Merchant", "MapTheme": "All",
      "ThemePriceMul": 1.0, "ShopStockTable": "stock_common", "ExclusiveItemIds": [],
      "GuardSpawnId": "guard_light", "GuardCount1": 1, "GuardCount2": 2,
      "ServiceCooldown": 30.0, "InteractRadius": 3.0, "GuardRadius": 6.0
    },
    {
      "NPCId": "merchant_alien", "Type": "Merchant", "MapTheme": "Alien",
      "ThemePriceMul": 1.2, "ShopStockTable": "stock_alien", "ExclusiveItemIds": [2007, 2008],
      "GuardSpawnId": "guard_medium", "GuardCount1": 1, "GuardCount2": 3,
      "ServiceCooldown": 30.0, "InteractRadius": 3.0, "GuardRadius": 6.0
    }
  ]
}
```

> 修改后运行 Unity 菜单 `Tools/DataTable/生成全部配置表代码` 生成 `NPCConfig.cs`。

---

### ShopStockConfig.json

**路径**：`Assets/Resources/DataTable/ShopStockConfig.json`

```json
{
  "table": "ShopStockConfig",
  "fields": [
    { "name": "TableId",    "type": "string", "desc": "库存抽取表 ID，被 NPCConfig.ShopStockTable 引用" },
    { "name": "ItemId",     "type": "int",    "desc": "物品 ID（对应 ItemConfig.json）" },
    { "name": "Category",   "type": "string", "desc": "物品类目：Weapon | Skill | Ink | Antidote | Remover（刮除剂）| RareInk" },
    { "name": "Weight",     "type": "float",  "desc": "抽取权重，同 TableId 内归一化" },
    { "name": "MinCount",   "type": "int",    "desc": "最小库存数量（件/瓶）" },
    { "name": "MaxCount",   "type": "int",    "desc": "最大库存数量（件/瓶）" },
    { "name": "BasePrice",  "type": "int",    "desc": "基础售价（coin），×ThemePriceMul 得实际价格" },
    { "name": "SellRatio",  "type": "float",  "desc": "玩家出售回收比例（×BasePrice）" }
  ],
  "rows": [
    { "TableId": "stock_common", "ItemId": 1001, "Category": "Weapon",   "Weight": 15, "MinCount": 1, "MaxCount": 2, "BasePrice": 120, "SellRatio": 0.4 },
    { "TableId": "stock_common", "ItemId": 2001, "Category": "Ink",      "Weight": 30, "MinCount": 2, "MaxCount": 5, "BasePrice": 50,  "SellRatio": 0.4 },
    { "TableId": "stock_common", "ItemId": 3001, "Category": "Antidote", "Weight": 20, "MinCount": 1, "MaxCount": 3, "BasePrice": 70,  "SellRatio": 0.3 },
    { "TableId": "stock_common", "ItemId": 4001, "Category": "Skill",    "Weight": 10, "MinCount": 0, "MaxCount": 1, "BasePrice": 200, "SellRatio": 0.4 },
    { "TableId": "stock_common", "ItemId": 6001, "Category": "Remover",  "Weight": 15, "MinCount": 0, "MaxCount": 1, "BasePrice": 180, "SellRatio": 0.2 },
    { "TableId": "stock_alien",  "ItemId": 1005, "Category": "Weapon",   "Weight": 15, "MinCount": 1, "MaxCount": 1, "BasePrice": 250, "SellRatio": 0.4 },
    { "TableId": "stock_alien",  "ItemId": 2007, "Category": "RareInk",  "Weight": 20, "MinCount": 1, "MaxCount": 3, "BasePrice": 120, "SellRatio": 0.3 },
    { "TableId": "stock_alien",  "ItemId": 2008, "Category": "RareInk",  "Weight": 20, "MinCount": 1, "MaxCount": 3, "BasePrice": 120, "SellRatio": 0.3 },
    { "TableId": "stock_alien",  "ItemId": 3001, "Category": "Antidote", "Weight": 25, "MinCount": 1, "MaxCount": 2, "BasePrice": 70,  "SellRatio": 0.3 },
    { "TableId": "stock_alien",  "ItemId": 6001, "Category": "Remover",  "Weight": 20, "MinCount": 0, "MaxCount": 2, "BasePrice": 180, "SellRatio": 0.2 }
  ]
}
```

---

### TattooEnchantAffixConfig.json（新增）

**路径**：`Assets/Resources/DataTable/TattooEnchantAffixConfig.json`

定义每个部位 × 颜色档的词缀池。每条记录代表一个可抽取词缀。

```json
{
  "table": "TattooEnchantAffixConfig",
  "fields": [
    { "name": "AffixId",     "type": "int",    "desc": "词缀唯一 ID" },
    { "name": "PartId",      "type": "int",    "desc": "适用部位（0=全部位，1=脑袋，2=躯干，3=左臂，4=右臂，5=左腿，6=右腿）" },
    { "name": "ColorTier",   "type": "string", "desc": "适用颜料档（Common=红黄绿蓝 / Rare=紫金 / Legendary=白 / Any=全档）" },
    { "name": "AffixType",   "type": "string", "desc": "效果类型（StatBonus | CooldownReduce | DmgBonus | ConditionalDmg | StatusApply | DefenseBonus）" },
    { "name": "StatKey",     "type": "string", "desc": "影响的数值 Key（如 MaxHP / CritRate / CooldownPct / FireDmgPct）" },
    { "name": "Value",       "type": "float",  "desc": "词缀数值（单位由 StatKey 决定，百分比类已含 ÷100 换算，存储为小数）" },
    { "name": "ConditionKey","type": "string", "desc": "条件 Key（无条件留空；如 DistanceGt8m / AfterDodge / OnHit）" },
    { "name": "ConditionVal","type": "float",  "desc": "条件阈值（ConditionKey 为空时 = 0）" },
    { "name": "DisplayText", "type": "string", "desc": "UI 展示文案（如 "距离>8m 攻击 +30%"），不含数值计算逻辑" },
    { "name": "Weight",      "type": "float",  "desc": "同 PartId+ColorTier 池内的抽取权重，归一化" }
  ],
  "rows": [
    { "AffixId": 1001, "PartId": 4, "ColorTier": "Any",       "AffixType": "DmgBonus",      "StatKey": "NormalDmgPct", "Value": 0.15, "ConditionKey": "",          "ConditionVal": 0,   "DisplayText": "普攻伤害 +15%",          "Weight": 10 },
    { "AffixId": 1002, "PartId": 4, "ColorTier": "Any",       "AffixType": "DmgBonus",      "StatKey": "FireDmgPct",   "Value": 0.15, "ConditionKey": "",          "ConditionVal": 0,   "DisplayText": "火伤 +15%",              "Weight": 10 },
    { "AffixId": 1003, "PartId": 4, "ColorTier": "Any",       "AffixType": "StatusApply",   "StatKey": "LightningAffixPct","Value": 0.20,"ConditionKey": "",       "ConditionVal": 0,   "DisplayText": "闪电沾附 +20%",          "Weight": 8  },
    { "AffixId": 1004, "PartId": 4, "ColorTier": "Rare",      "AffixType": "ConditionalDmg","StatKey": "RangedDmgPct", "Value": 0.30, "ConditionKey": "DistanceGt8m","ConditionVal": 8, "DisplayText": "距离>8m 攻击 +30%",      "Weight": 6  },
    { "AffixId": 2001, "PartId": 3, "ColorTier": "Any",       "AffixType": "DmgBonus",      "StatKey": "SkillDmgPct",  "Value": 0.18, "ConditionKey": "",          "ConditionVal": 0,   "DisplayText": "技能伤害 +18%",          "Weight": 10 },
    { "AffixId": 2002, "PartId": 3, "ColorTier": "Any",       "AffixType": "CooldownReduce","StatKey": "CooldownPct",  "Value": 0.08, "ConditionKey": "",          "ConditionVal": 0,   "DisplayText": "冷却缩短 -8%",           "Weight": 10 },
    { "AffixId": 2003, "PartId": 3, "ColorTier": "Any",       "AffixType": "StatusApply",   "StatKey": "LightningAffixPct","Value": 0.20,"ConditionKey": "",       "ConditionVal": 0,   "DisplayText": "闪电沾附 +20%",          "Weight": 8  },
    { "AffixId": 3001, "PartId": 1, "ColorTier": "Any",       "AffixType": "CooldownReduce","StatKey": "CooldownPct",  "Value": 0.10, "ConditionKey": "",          "ConditionVal": 0,   "DisplayText": "冷却缩短 -10%",          "Weight": 10 },
    { "AffixId": 3002, "PartId": 1, "ColorTier": "Rare",      "AffixType": "StatBonus",     "StatKey": "SkillChargeBonus","Value": 1, "ConditionKey": "",          "ConditionVal": 0,   "DisplayText": "技能充能 +1",            "Weight": 6  },
    { "AffixId": 3003, "PartId": 1, "ColorTier": "Legendary", "AffixType": "DefenseBonus",  "StatKey": "PostCastInvincSec","Value": 0.3,"ConditionKey": "",        "ConditionVal": 0,   "DisplayText": "施法后无敌 0.3s",        "Weight": 4  },
    { "AffixId": 4001, "PartId": 2, "ColorTier": "Any",       "AffixType": "StatBonus",     "StatKey": "MaxHP",        "Value": 80,   "ConditionKey": "",          "ConditionVal": 0,   "DisplayText": "最大 HP +80",            "Weight": 10 },
    { "AffixId": 4002, "PartId": 2, "ColorTier": "Any",       "AffixType": "DefenseBonus",  "StatKey": "DmgTakenPct",  "Value": -0.12,"ConditionKey": "",         "ConditionVal": 0,   "DisplayText": "受击伤害 -12%",          "Weight": 10 },
    { "AffixId": 5001, "PartId": 5, "ColorTier": "Any",       "AffixType": "StatBonus",     "StatKey": "DodgeDistPct", "Value": 0.20, "ConditionKey": "",          "ConditionVal": 0,   "DisplayText": "闪避距离 +20%",          "Weight": 10 },
    { "AffixId": 5002, "PartId": 5, "ColorTier": "Rare",      "AffixType": "DefenseBonus",  "StatKey": "PostDodgeInvincSec","Value": 1.0,"ConditionKey": "AfterDodge","ConditionVal": 0,"DisplayText": "闪避后 1s 无敌",         "Weight": 6  },
    { "AffixId": 6001, "PartId": 6, "ColorTier": "Any",       "AffixType": "StatBonus",     "StatKey": "MoveSpeedPct", "Value": 0.10, "ConditionKey": "",          "ConditionVal": 0,   "DisplayText": "移速 +10%",              "Weight": 10 },
    { "AffixId": 6002, "PartId": 6, "ColorTier": "Rare",      "AffixType": "DefenseBonus",  "StatKey": "SprintDmgReducePct","Value": 0.08,"ConditionKey": "OnSprint","ConditionVal": 0, "DisplayText": "跑步时伤害减免 +8%",     "Weight": 8  }
  ]
}
```

---

### TattooEnchantRecipeConfig.json（新增）

**路径**：`Assets/Resources/DataTable/TattooEnchantRecipeConfig.json`

定义附魔花费参数（由 NPC 代码读取，不硬编码）。

```json
{
  "table": "TattooEnchantRecipeConfig",
  "fields": [
    { "name": "ColorTier",         "type": "string", "desc": "颜料档枚举：Common | Rare | Legendary" },
    { "name": "CoinCost",          "type": "int",    "desc": "附魔金币花费（coin）" },
    { "name": "RarePigmentCost",   "type": "int",    "desc": "附魔稀有颜料花费（瓶），固定为 1" },
    { "name": "MaxAffixPerSlot",   "type": "int",    "desc": "每个纹身槽最大词缀数，固定为 2" }
  ],
  "rows": [
    { "ColorTier": "Common",    "CoinCost": 200, "RarePigmentCost": 1, "MaxAffixPerSlot": 2 },
    { "ColorTier": "Rare",      "CoinCost": 350, "RarePigmentCost": 1, "MaxAffixPerSlot": 2 },
    { "ColorTier": "Legendary", "CoinCost": 500, "RarePigmentCost": 1, "MaxAffixPerSlot": 2 }
  ]
}
```

---

## 五、UX / UI 触点

本系统共 3 个独立 UIForm，均由 UIModule 管理。

### 5.1 自刻纹身界面（TattooSelfInkUIForm，Tab 键触发）

```
+──────────────────────────────────────────────────────────+
│  刻纹身                                       [×关闭]    │
│  ┌─────────────┐  选中部位：右臂               [已满]    │
│  │  角色剪影   │                                          │
│  │  6 部位     │  颜色（4 常见 + 2 稀有 + 1 传说）        │
│  │  可点击高亮 │  ● 红 ● 黄 ● 绿 ● 蓝 ● 紫 ● 金 ● 白    │
│  │  已刻显图标 │  （无颜料 = 灰色禁用，数量悬停显示）     │
│  └─────────────┘                                          │
│                   图案（已解锁显示，未解锁显示🔒）         │
│                   — ○  ⚡  ✦  ……                         │
│                                                           │
│  [预览效果]  右臂 × 黄 × Bolt = 普攻链跳 3 目标麻痹 0.5s │
│                                                           │
│  消耗：黄颜料 ×3   金币（中断才扣 50）                    │
│  读条：5s（右臂）                                        │
│  [开始刻纹身]（读条期间失去控制；被打/移动/死亡中断）     │
+──────────────────────────────────────────────────────────+
```

UX 要点：
1. "金币只有中断才扣"需在 UI 内显著提示，避免玩家误以为现在就扣。
2. 被攻击中断时屏幕边缘红闪 + 文字"刻纹身被中断，损失 50 coin"。
3. 颜料库存不足时，颜色按钮直接灰化并悬停显示缺口数量。
4. 部位已刻时显示"已占用"标签，提示需用刮除剂清除后才能重刻（刮除剂从商人购买）。

### 5.2 附魔界面（TattooEnchantUIForm，纹身师触发）

```
+──────────────────────────────────────────────────────────+
│  词缀附魔                                     [×关闭]    │
│                                                           │
│  选择纹身部位：（仅展示当前已刻部位）                    │
│  ┌ 右臂 × 黄 × Bolt ┐  词缀槽：[火伤+15%] [空槽]        │
│  └────────────────────┘                                   │
│  ┌ 左臂 × 蓝 × Line ┐  词缀槽：[空槽]     [空槽]         │
│  └────────────────────┘                                   │
│                                                           │
│  附魔花费（右臂 × 黄 = Common 档）：                      │
│    金币 ×200   稀有颜料 ×1                               │
│  持有：金币 560 / 稀有颜料 2 瓶                           │
│                                                           │
│  ⚠ 词缀随机，结果不可预知。满 2 槽须用刮除剂清空后重刷。 │
│  [确认附魔]                                               │
+──────────────────────────────────────────────────────────+
```

UX 要点：
1. 附魔完成后立即弹出词缀结果卡（高亮展示新词缀 1.5s），再关闭 UI。
2. 词缀满 2 槽的部位显示"满员"标签 + 灰化确认按钮，不允许继续附魔。
3. 稀有颜料不足时确认按钮灰化，并提示"稀有颜料不足，前往外星区商人购买"。

### 5.3 商人界面（MerchantShopUIForm，靠近商人触发）

```
+──────────────────────────────────────────+
│  商人                         [×关闭]    │
│  持有金币：560 c                          │
│                                           │
│  [购买]  [出售]                           │
│  ─────────────────────────────────────   │
│  ○ 黄颜料 ×3          50c    [购买]      │
│  ○ 稀有颜料（紫）×3  120c    [购买]      │
│  ○ 短剑（火）×1      180c    [购买]      │
│  ○ 技能: 火球 ×1     200c    [购买]      │
│  ○ 刮除剂 ×1         180c    [购买]      │
│  ○ 解毒剂 ×2          70c    [购买]      │
│                                           │
│  [刷新库存（80c）]（本局已用 0/1 次）     │
+──────────────────────────────────────────+
```

UX 要点：
1. 技能购买入口提示当前槽位占用情况（2 槽位 v2.1），已满时购买按钮置灰。
2. 刮除剂购买后背包显示图标，使用入口在自刻纹身界面的"已占用部位"旁。
3. 其他玩家或 AI 买走后库存立即更新（服务端同步）。

---

## 六、AI 行为侧需求

### 6.1 智能 AI 自刻纹身

AI 在**任意地点**按与玩家相同的 Tab 触发逻辑自刻，不需要去工作室。

```
触发条件（每 30s RethinkInterval）：
  - 有空白纹身槽
  - 持有对应颜料 >= 该部位 INK_CONSUME_COUNT
  - 未处于战斗状态（自我评估 HP > 60% 且 3s 内无受击）
  - 安全区内（不在缩圈 30s 倒计时内）

读条期间：AI 停止移动和攻击（与玩家相同的控制权剥夺）
被攻击中断：AI 接受 50 coin 惩罚并重新排入下轮 Rethink 计划
```

### 6.2 智能 AI 前往工作室附魔

```
触发条件：
  - 至少 1 个已刻纹身有空词缀槽
  - 持有稀有颜料 >= 1 瓶
  - 金币 >= 附魔花费（按最低档 200 coin）
  - 最近工作室距离 < 60m
  - 非缩圈紧迫阶段

行为输出：
  - 寻路至最近工作室
  - 到达后触发 NPCInteractStartEvent → 调用附魔流程
  - 选择未满词缀槽的部位，发起附魔
```

### 6.3 智能 AI 访问商人

```
购买优先级：
  1. 颜料（持有 < 2 瓶且金币充足）
  2. 稀有颜料（计划附魔且无稀有颜料）
  3. 解药（HP < 50% 且无解药库存）
  4. 武器（当前 BaseDamage < 商人武器 × 0.7）
  5. 技能（技能槽未满 2 个且金币充足）
  6. 刮除剂（词缀已满但对当前词缀不满意——AI 简化评分：词缀 Value < 阈值）
```

---

## 七、风险与开放问题

### 7.1 工作室节点推荐配置

**推荐**：3 个纹身师工作室 + 2 个商人，对应 12-数值平衡 §2.7 槽位时间节奏：

| 工作室 | 出现时间 | 区域 | 主题 |
|---|---|---|---|
| 工作室 1 | t=0–5 min | 起始区 | 普通/贫民区 |
| 工作室 2 | t=8–12 min | 中环 | 实验室 |
| 工作室 3 | t=15–20 min | 外星区 | 外星区（稀有颜料来源附近）|

工作室 < 3 时，玩家无法在 t=15 前完成 4-5 词缀的附魔目标。工作室 > 4 时重刻排队机制失效。

### 7.2 词缀退化路径风险（balance-check 结论）

**风险**：右臂"距离>8m 攻击 +30%"（ConditionalDmg）+ 左臂"技能伤害 +18%"+ 脑袋"冷却缩短 -10%"三词缀联动可达到明显优势策略（远程持续高伤）。

**缓解**：
1. 传说词缀（白颜料）池保持 Weight 偏低（4–6），常见词缀 Weight = 10，稀释强力词缀出现概率。
2. 刮除剂 180 coin 的成本约束玩家无限刷词缀次数（单局刷 3 次 = 540 coin，占基线金币约 67%）。
3. v1.1 期监控附魔词缀分布数据，若"距离>8m"出现率 > 30% 则降低 Weight 至 3。

### 7.3 CONTRACT 追加事件建议

本次修订新增以下 4 个事件，建议同步更新 CONTRACT.md §1.5：

```
TattooInProgressEvent  { Actor Owner; int PartId; int ColorId; int PatternId; float DurationSec; }
  -- 玩家/AI 开始读条刻纹身时发布

TattooFinishedEvent    { Actor Owner; TattooSlot NewSlot; }
  -- 读条成功完成、纹身写入时发布

TattooCancelledEvent   { Actor Owner; CancelReason Reason; }
  -- 读条中断时发布（Reason: Attacked | Moved | Died）

TattooEnchantedEvent   { Actor Owner; TattooSlot Slot; List<TattooAffix> NewAffixes; }
  -- 附魔完成、词缀写入时发布
```

原有事件（NPCInteractStartEvent / TattooSessionEndEvent / ShopPurchaseEvent / ShopRefreshEvent）保持兼容，不删除。

### 7.4 刮除剂作为稀有颜料间接来源

**开放问题**：刮除剂清除词缀后，消耗的稀有颜料是否应有部分返还？

当前设计：不返还（一次性消耗），保持附魔决策权重。反向逻辑：若稀有颜料掉落率过低，后期玩家附魔机会极少，刮除剂形同虚设。

**建议**：MVP 阶段不返还，在 v1.1 根据稀有颜料的平均每局获取量（目标：基线玩家每局可附魔 2–3 次）调整掉落率；若不达标则考虑"刮除返还 1 瓶"。

### 7.5 技能槽 2→2 变更影响

04-主动技能 GDD 原定 3 槽位，v2.1 修订为 2 槽位。商人技能类目最大库存 MaxCount 已改为 1，但 04-主动技能.md 本身尚未同步修订。建议同步修订 04-主动技能.md §2.1 和 SkillConfig.json 的 MaxSlot 字段，避免实现时不一致。

---

## 八、引用

### 设计源
- v1 草案：[08-待讨论事项与下一步.md §一 4](../../raw/初版GDD-2026-06/08-待讨论事项与下一步.md)（纹身师 NPC 战略角色）
- v2.0 原始版本：本文件 git 历史

### 契约
- [CONTRACT.md §1.5](../../../openspec/changes/05-gdd-v2-full-design-docs/CONTRACT.md)（原有事件 + 本次追加 4 个事件，见 §七 7.3）
- [CONTRACT.md §1.4](../../../openspec/changes/05-gdd-v2-full-design-docs/CONTRACT.md)（CoinChangedEvent）

### 数值基线引用
- [12-数值平衡与曲线 §四 BalanceConstantsConfig](./12-数值平衡与曲线.md)（附魔成本基准 / 中断扣币 / 刮除剂定价）
- [12-数值平衡 §2.7 tattooSlotsByTime](./12-数值平衡与曲线.md)（NPC 数量与时间节奏）

### DataTable 配置（本 GDD 定义）
- `Assets/Resources/DataTable/NPCConfig.json`
- `Assets/Resources/DataTable/ShopStockConfig.json`
- `Assets/Resources/DataTable/TattooEnchantAffixConfig.json`（新增）
- `Assets/Resources/DataTable/TattooEnchantRecipeConfig.json`（新增）

### 同级 GDD 引用
- [01-纹身构筑系统](./01-纹身构筑系统.md) — TattooModule.Equip / TattooSlot.affixes API
- [03-武器系统](./03-武器系统.md) — 武器 ItemConfig 对接
- [04-主动技能](./04-主动技能.md) — 技能槽 2 个（待同步修订）
- [07-地图生成](./07-地图生成.md) — 工作室节点布局
- [11-经济系统](./11-经济系统.md) — 金币流向统计
- [12-数值平衡与曲线](./12-数值平衡与曲线.md) — 数值基线
- [13-UI与HUD](./13-UI与HUD.md) — 3 个 UIForm 实现
- [16-BotControllerModule](../modules/16-BotControllerModule.md) — AI 三条行为流程

---

> **本 GDD 状态**：v2.1 / 8 节完整 / 三流程独立（自刻 / 附魔 / 商人）/ 4 张 DataTable 草案已给出（NPCConfig + ShopStockConfig + TattooEnchantAffixConfig + TattooEnchantRecipeConfig）/ CONTRACT 追加 4 事件已列入 §七 7.3 / 开放问题：刮除返还（MVP 不做）/ 技能槽同步修订（待处理 04-主动技能.md）。
