---
name: godot-economy-system
description: 游戏经济系统的专家级设计模式，涵盖货币管理（多货币、钱包系统）、商店系统（买入/卖出价格、库存限制）、动态定价（供需关系）、战利品表（加权掉落、稀有度层级）以及经济平衡（通胀控制、货币消耗机制）。适用于角色扮演游戏（RPG）、交易类游戏或资源管理类系统。触发关键词：EconomyManager,
  currency, shop_item, loot_table, dynamic_pricing, buy_sell_spread, currency_sink,
  inflation, item_rarity.
tags: game-economy-design, currency-management, shop-system, loot-table, godot-development
tags_cn: 游戏经济系统设计, 货币管理, 商店系统, 战利品表, Godot开发
---

# 游戏经济系统

关于设计包含货币、商店和战利品的平衡游戏经济系统的专家指南。

## 绝对不要做的事

- **绝对不要用`int`类型存储货币** — 小额货币可以用`int`，但大型经济系统应使用`float`或自定义BigInt类型。整数溢出会彻底破坏经济系统（`int`最大值为21亿）。
- **绝对不要忽略买入/卖出价差** — 买入和卖出价格相同会造成无限刷钱的漏洞。卖出价格应设为买入价格的30-50%。
- **绝对不要缺少货币消耗机制** — 没有消耗渠道（如修理费用、税费、消耗品）的话，经济会出现通胀，玩家会囤积无限财富。
- **绝对不要在客户端验证货币交易** — 客户端计算的“我有1000金币”不可信。所有交易必须由服务器验证，否则会出现漏洞利用。
- **绝对不要硬编码战利品掉落概率** — 战利品表应使用资源文件或JSON存储。设计师需要无需修改代码就能迭代调整的能力。
---

## 可用脚本

> **强制要求**：在实现对应模式前，请先阅读相应的脚本。

### [loot_table_weighted.gd](scripts/loot_table_weighted.gd)
基于累积概率的加权战利品表。基于资源的设计允许设计师通过检视面板进行迭代，无需修改代码。

---

## 货币管理器

```gdscript
# economy_manager.gd (AutoLoad)
extends Node

signal currency_changed(old_amount: int, new_amount: int)

var gold: int = 0

func add_currency(amount: int) -> void:
    var old := gold
    gold += amount
    currency_changed.emit(old, gold)

func spend_currency(amount: int) -> bool:
    if gold < amount:
        return false
    
    var old := gold
    gold -= amount
    currency_changed.emit(old, gold)
    return true

func has_currency(amount: int) -> bool:
    return gold >= amount
```

## 商店系统

```gdscript
# shop_item.gd
class_name ShopItem
extends Resource

@export var item: Item
@export var buy_price: int
@export var sell_price: int
@export var stock: int = -1  # -1 = 无限库存

func can_buy() -> bool:
    return stock != 0
```

```gdscript
# shop.gd
class_name Shop
extends Resource

@export var shop_name: String
@export var items: Array[ShopItem] = []

func buy_item(shop_item: ShopItem, inventory: Inventory) -> bool:
    if not shop_item.can_buy():
        return false
    
    if not EconomyManager.has_currency(shop_item.buy_price):
        return false
    
    if not EconomyManager.spend_currency(shop_item.buy_price):
        return false
    
    inventory.add_item(shop_item.item, 1)
    
    if shop_item.stock > 0:
        shop_item.stock -= 1
    
    return true

func sell_item(item: Item, inventory: Inventory) -> bool:
    # 查找对应物品的商店条目以获取卖出价格
    var shop_item := get_shop_item_for(item)
    if not shop_item:
        return false
    
    if not inventory.has_item(item, 1):
        return false
    
    inventory.remove_item(item, 1)
    EconomyManager.add_currency(shop_item.sell_price)
    return true

func get_shop_item_for(item: Item) -> ShopItem:
    for shop_item in items:
        if shop_item.item == item:
            return shop_item
    return null
```

## 定价公式

```gdscript
func calculate_sell_price(buy_price: int, markup: float = 0.5) -> int:
    # 卖出价格为买入价格的50%
    return int(buy_price * markup)

func calculate_dynamic_price(base_price: int, demand: float) -> int:
    # 价格随需求增长而提高
    return int(base_price * (1.0 + demand))
```

## 战利品表

```gdscript
# loot_table.gd
class_name LootTable
extends Resource

@export var drops: Array[LootDrop] = []

func roll_loot() -> Array[Item]:
    var items: Array[Item] = []
    
    for drop in drops:
        if randf() < drop.chance:
            items.append(drop.item)
    
    return items
```

```gdscript
# loot_drop.gd
class_name LootDrop
extends Resource

@export var item: Item
@export var chance: float = 0.5
@export var min_amount: int = 1
@export var max_amount: int = 1
```

## 最佳实践

1. **平衡性** - 仔细测试经济系统
2. **消耗机制** - 提供货币消耗渠道（如修理费用等）
3. **通胀控制** - 管控货币产出量

## 参考资料
- 相关资源：`godot-inventory-system`, `godot-save-load-systems`


### 相关内容
- 核心技能：[godot-master](../godot-master/SKILL.md)