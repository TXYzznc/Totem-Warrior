---
name: godot-combat-system
description: 战斗系统的专业实现模式，涵盖碰撞箱（Hitbox）/受击箱（Hurtbox）架构、伤害计算（DamageData类）、生命值组件、战斗状态机、连招系统、技能冷却以及伤害飘字。适用于动作游戏、角色扮演游戏（RPG）或格斗游戏。触发关键词：Hitbox、Hurtbox、DamageData、HealthComponent、combat_state、combo_system、ability_cooldown、invincibility_frames、damage_popup。
tags: combat-system-design, godot-scripting, hitbox-hurtbox, damage-calculation, combo-system
tags_cn: 战斗系统设计, Godot脚本开发, Hitbox/Hurtbox实现, 伤害计算, 连招系统
---

# 战斗系统

构建灵活的、基于组件的战斗系统的专业指南。

## 绝对禁止的操作

- **绝对不要直接修改生命值（`target.health -= 10`）** — 这种方式会绕过护甲、抗性和事件机制，请使用DamageData + HealthComponent模式。
- **绝对不要忽略无敌帧** — 没有无敌帧的话，多段攻击会每帧都造成伤害，受击后应添加0.5-1秒的无敌时间。
- **绝对不要让碰撞箱始终处于激活状态** — 应通过动画轨道来启用/禁用碰撞箱，永久激活的碰撞箱会导致意外伤害。
- **绝对不要用分组来过滤碰撞箱** — 请使用碰撞层，分组不会遵循物理层规则，会导致友军误伤。
- **绝对不要在没有DamageData的情况下触发damage_received信号** — 直接传递整数/浮点数伤害会丢失上下文信息（来源、类型、击退效果），务必使用DamageData类。
---

## 可用脚本

> **强制要求**：在实现对应模式前，请先阅读相应的脚本。

### [hitbox_hurtbox.gd](scripts/hitbox_hurtbox.gd)
基于组件的碰撞箱，包含击中停顿和击退效果。使用Engine.time_scale结合ignore_time_scale计时器来实现正确的击中停顿帧效果。

---

## 伤害系统

```gdscript
# damage_data.gd
class_name DamageData
extends RefCounted

var amount: float
var source: Node
var damage_type: String = "physical"
var knockback: Vector2 = Vector2.ZERO
var is_critical: bool = false

func _init(dmg: float, src: Node = null) -> void:
    amount = dmg
    source = src
```

## 受击箱/碰撞箱模式

```gdscript
# hurtbox.gd
extends Area2D
class_name Hurtbox

signal damage_received(data: DamageData)

@export var health_component: Node

func _ready() -> void:
    area_entered.connect(_on_area_entered)

func _on_area_entered(area: Area2D) -> void:
    if area is Hitbox:
        var damage := area.get_damage()
        damage_received.emit(damage)
        
        if health_component:
            health_component.take_damage(damage)
```

```gdscript
# hitbox.gd
extends Area2D
class_name Hitbox

@export var damage: float = 10.0
@export var damage_type: String = "physical"
@export var knockback_force: float = 100.0
@export var owner_node: Node

func get_damage() -> DamageData:
    var data := DamageData.new(damage, owner_node)
    data.damage_type = damage_type
    
    # Calculate knockback direction
    if owner_node:
        var direction := (global_position - owner_node.global_position).normalized()
        data.knockback = direction * knockback_force
    
    return data
```

## 生命值组件

```gdscript
# health_component.gd
extends Node
class_name HealthComponent

signal health_changed(old_health: float, new_health: float)
signal died
signal healed(amount: float)

@export var max_health: float = 100.0
@export var current_health: float = 100.0
@export var invincible: bool = false

func take_damage(data: DamageData) -> void:
    if invincible:
        return
    
    var old_health := current_health
    current_health -= data.amount
    current_health = clampf(current_health, 0, max_health)
    
    health_changed.emit(old_health, current_health)
    
    if current_health <= 0:
        died.emit()

func heal(amount: float) -> void:
    var old_health := current_health
    current_health += amount
    current_health = minf(current_health, max_health)
    
    healed.emit(amount)
    health_changed.emit(old_health, current_health)

func is_dead() -> bool:
    return current_health <= 0
```

## 战斗状态机

```gdscript
# combat_state.gd
extends Node
class_name CombatState

enum State { IDLE, ATTACKING, BLOCKING, DODGING, STUNNED }

var current_state: State = State.IDLE
var can_act: bool = true

func enter_attack_state() -> bool:
    if not can_act:
        return false
    
    current_state = State.ATTACKING
    can_act = false
    return true

func enter_block_state() -> void:
    current_state = State.BLOCKING

func enter_dodge_state() -> bool:
    if not can_act:
        return false
    
    current_state = State.DODGING
    can_act = false
    return true

func exit_state() -> void:
    current_state = State.IDLE
    can_act = true
```

## 连招系统

```gdscript
# combo_system.gd
extends Node
class_name ComboSystem

signal combo_executed(combo_name: String)

@export var combo_window: float = 0.5
var combo_buffer: Array[String] = []
var last_input_time: float = 0.0

func register_input(action: String) -> void:
    var current_time := Time.get_ticks_msec() / 1000.0
    
    if current_time - last_input_time > combo_window:
        combo_buffer.clear()
    
    combo_buffer.append(action)
    last_input_time = current_time
    
    check_combos()

func check_combos() -> void:
    # Light → Light → Heavy = Special Attack
    if combo_buffer.size() >= 3:
        var last_three := combo_buffer.slice(-3)
        if last_three == ["light", "light", "heavy"]:
            execute_combo("special_attack")
            combo_buffer.clear()

func execute_combo(combo_name: String) -> void:
    combo_executed.emit(combo_name)
```

## 技能系统

```gdscript
# ability.gd
class_name Ability
extends Resource

@export var ability_name: String
@export var cooldown: float = 1.0
@export var damage: float = 25.0
@export var range: float = 100.0
@export var animation: String

var is_on_cooldown: bool = false

func can_use() -> bool:
    return not is_on_cooldown

func use(caster: Node) -> void:
    if not can_use():
        return
    
    is_on_cooldown = true
    
    # Execute ability logic
    _execute(caster)
    
    # Start cooldown
    await caster.get_tree().create_timer(cooldown).timeout
    is_on_cooldown = false

func _execute(caster: Node) -> void:
    # Override in derived abilities
    pass
```

## 伤害飘字

```gdscript
# damage_popup.gd
extends Label

func show_damage(amount: float, is_crit: bool = false) -> void:
    text = str(int(amount))
    
    if is_crit:
        modulate = Color.RED
        scale = Vector2(1.5, 1.5)
    
    var tween := create_tween()
    tween.set_parallel(true)
    tween.tween_property(self, "position:y", position.y - 50, 1.0)
    tween.tween_property(self, "modulate:a", 0.0, 1.0)
    tween.finished.connect(queue_free)
```

## 暴击机制

```gdscript
func calculate_damage(base_damage: float, crit_chance: float = 0.1) -> DamageData:
    var data := DamageData.new(base_damage)
    
    if randf() < crit_chance:
        data.is_critical = true
        data.amount *= 2.0
    
    return data
```

## 最佳实践

1. **关注点分离** - 生命值系统 ≠ 战斗系统 ≠ 移动系统
2. **使用信号** - 实现系统解耦
3. **用Area2D实现碰撞箱** - 利用内置的碰撞检测功能
4. **添加无敌帧** - 防止重复 spam 伤害

## 参考资料
- 相关内容：`godot-2d-physics`, `godot-animation-player`, `godot-characterbody-2d`


### 相关内容
- 进阶技能：[godot-master](../godot-master/SKILL.md)
