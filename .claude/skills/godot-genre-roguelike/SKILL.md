---
name: godot-genre-roguelike
description: Roguelike游戏专家级开发蓝图，涵盖程序化生成（Walker算法、BSP房间）、带元进度的永久死亡机制（解锁内容持久化）、运行状态与元状态分离、带种子的RNG（可分享的游戏局）、战利品/遗物系统（基于钩子的
  modifier），以及难度缩放（基于楼层的进度）。适用于地牢爬行类、动作类Roguelike或Roguelite游戏。触发关键词：roguelike, procedural_generation,
  permadeath, meta_progression, seeded_RNG, relic_system, run_state.
tags: roguelike-development, procedural-generation, meta-progression, godot-engine,
  dungeon-crawler
tags_cn: Roguelike开发, 程序化生成, 元进度系统, Godot引擎, 地牢爬行游戏
---

# 游戏类型：Roguelike

兼顾挑战性、进度系统与重玩价值的Roguelike游戏专家级开发蓝图。

## 绝对不要做的事

- **绝对不要让游戏局完全依赖纯RNG** — 玩家的操作技巧应能抵消坏运气。需提供有保障的道具商店、重roll机制，或初始装备选择。
- **绝对不要设计过于强力的元升级** — 如果元进度升级过强，游戏会变成“刷到赢”而非“学到赢”。升级幅度需保持克制（最多+10%伤害）。
- **绝对不要缺乏内容多样性** — 程序化生成只是对现有内容进行重组。至少需要50+种房间、20+种敌人、100+种道具才能保持新鲜感。
- **绝对不要使用无种子的RNG** — 始终用种子初始化RandomNumberGenerator。这样才能支持可分享/可复现的游戏局。
- **绝对不要允许存档刷分** — 仅在楼层切换时保存状态。加载时删除存档（严格类Roguelike的标准做法）。
---

## 可用脚本

> **强制要求**：在实现对应模式前，请先阅读相应脚本。

### [meta_progression_manager.gd](scripts/meta_progression_manager.gd)
跨游戏局的货币与升级持久化系统。支持JSON保存/加载，跟踪升级购买与等级信息。生产构建时需加密。

---

## 核心循环
1.  **准备阶段**：选择角色，装备元升级内容。
2.  **游戏局进行**：完成程序化生成的关卡，获取临时增益。
3.  **挑战阶段**：在难度逐渐提升的遭遇战/Boss战中存活。
4.  **死亡/胜利**：游戏局结束，计算获得的资源。
5.  **元进度更新**：消耗资源解锁永久内容/升级。
6.  **重复挑战**：带着新能力开启新的游戏局。

## 技能链

| 阶段 | 技能 | 用途 |
|-------|--------|---------|
| 1.架构设计 | `state-machines`, `autoloads` | 管理运行状态与元状态 |
| 2.世界生成 | `godot-procedural-generation`, `tilemap`, `noise` | 每次游戏局生成独特关卡 |
| 3.战斗系统 | `godot-combat-system`, `enemy-ai` | 快节奏、高风险的战斗遭遇 |
| 4.进度系统 | `loot-tables`, `godot-inventory-system` | 管理游戏局专属道具/遗物 |
| 5.持久化 | `save-system`, `resources` | 在游戏局之间保存元进度 |

## 架构概述

Roguelike游戏要求严格区分**运行状态**（临时）与**元状态**（持久）。

### 1. 游戏局管理器（AutoLoad）
处理单局游戏的生命周期。死亡时完全重置状态。

```gdscript
# run_manager.gd
extends Node

signal run_started
signal run_ended(victory: bool)
signal floor_changed(new_floor: int)

var current_seed: int
var current_floor: int = 1
var player_stats: Dictionary = {}
var inventory: Array[Resource] = []
var rng: RandomNumberGenerator

func start_run(seed_val: int = -1) -> void:
    rng = RandomNumberGenerator.new()
    if seed_val == -1:
        rng.randomize()
        current_seed = rng.seed
    else:
        current_seed = seed_val
        rng.seed = current_seed
        
    current_floor = 1
    _reset_run_state()
    run_started.emit()

func _reset_run_state() -> void:
    player_stats = { "hp": 100, "gold": 0 }
    inventory.clear()

func next_floor() -> void:
    current_floor += 1
    floor_changed.emit(current_floor)
    
func end_run(victory: bool) -> void:
    run_ended.emit(victory)
    # Trigger meta-progression save here
```

### 2. 元进度系统（Resource）
存储永久解锁内容。

```gdscript
# meta_progression.gd
class_name MetaProgression
extends Resource

@export var total_runs: int = 0
@export var unlocked_weapons: Array[String] = ["sword_basic"]
@export var currency: int = 0
@export var skill_tree_nodes: Dictionary = {} # node_id: level

func save() -> void:
    ResourceSaver.save(self, "user://meta_progression.tres")

static func load_or_create() -> MetaProgression:
    if ResourceLoader.exists("user://meta_progression.tres"):
        return ResourceLoader.load("user://meta_progression.tres")
    return MetaProgression.new()
```

## 核心机制实现

### 程序化地牢生成（Walker算法）
一种简单的“醉汉漫步”算法，用于生成自然的、洞穴风格或连通的房间布局。

```gdscript
# dungeon_generator.gd
extends Node

@export var map_width: int = 50
@export var map_height: int = 50
@export var max_walkers: int = 5
@export var max_steps: int = 500

func generate_dungeon(tilemap: TileMapLayer, rng: RandomNumberGenerator) -> void:
    tilemap.clear()
    var walkers: Array[Vector2i] = [Vector2i(map_width/2, map_height/2)]
    var floor_tiles: Array[Vector2i] = []
    
    for step in max_steps:
        var new_walkers: Array[Vector2i] = []
        for walker in walkers:
            floor_tiles.append(walker)
            # 25% chance to destroy walker, 25% to spawn new one
            if rng.randf() < 0.25 and walkers.size() > 1:
                continue # Destroy
            if rng.randf() < 0.25 and walkers.size() < max_walkers:
                new_walkers.append(walker) # Spawn
            
            # Move walker
            var direction = [Vector2i.UP, Vector2i.DOWN, Vector2i.LEFT, Vector2i.RIGHT].pick_random()
            new_walkers.append(walker + direction)
        
        walkers = new_walkers
    
    # Set tiles
    for pos in floor_tiles:
        tilemap.set_cell(pos, 0, Vector2i(0,0)) # Assuming source_id 0 is floor
    
    # Post-process: Add walls, spawn points, etc.
```

### 道具/遗物系统（基于Resource）
遗物可修改属性或添加新行为。

```gdscript
# relic.gd
class_name Relic
extends Resource

@export var id: String
@export var name: String
@export var icon: Texture2D
@export_multiline var description: String

# Hook system for complex interactions
func on_pickup(player: Node) -> void:
    pass

func on_damage_dealt(player: Node, target: Node, damage: int) -> int:
    return damage # Return modified damage

func on_kill(player: Node, target: Node) -> void:
    pass
```

```gdscript
# example_relic_vampirism.gd
extends Relic

func on_kill(player: Node, target: Node) -> void:
    player.heal(5)
    print("Vampirism triggered!")
```

## 常见陷阱

1.  **RNG过度依赖**：不要让游戏局完全依赖运气。优秀的Roguelike游戏应允许玩家用技巧抵消糟糕的RNG结果。
2.  **元进度失衡**：如果元升级过强，游戏会变成“刷到赢”而非“学到赢”。
3.  **内容缺乏多样性**：程序化生成的质量取决于它所重组的内容。你需要大量的内容（房间、敌人、道具）才能保持新鲜感。
4.  **存档刷分**：玩家会尝试通过退出游戏来避免死亡。仅在楼层切换或退出时保存状态，加载时删除存档（可选，但属于严格类Roguelike的标准做法）。

## Godot专属技巧

-   **带种子的游戏局**：始终用种子初始化`RandomNumberGenerator`。这样玩家可以分享特定的游戏局布局。
-   **ResourceSaver**：使用`ResourceSaver`实现元进度保存，但要注意深度嵌套资源中的循环引用问题。
-   **场景作为房间**：将“房间”制作成独立场景（如`Room1.tscn`、`Room2.tscn`），并将它们实例化到生成的布局中，在程序化布局中融入手工设计的质量。
-   **导航系统**：如果使用2D导航，在地牢布局生成完成后，需重新烘焙`NavigationRegion2D`。

## 进阶技巧

-   **协同系统**：给道具添加标签（如`fire`、`projectile`、`companion`），通过检测标签组合来创造出 Emergent 的增益效果。
-   **导演AI**：一个隐形的“导演”系统，跟踪玩家的生命值/压力值，动态调整敌人刷新速率（类似《求生之路》）。


## 参考资料
- 核心技能：[godot-master](../godot-master/SKILL.md)
