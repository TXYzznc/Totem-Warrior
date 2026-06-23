---
name: godot-genre-stealth
description: 潜行游戏（《细胞分裂》《杀手》《耻辱》《神偷》）专家级设计蓝图，涵盖AI检测系统、视野锥、声音传播、警戒状态、光影机制以及系统性设计。适用于构建需要敌方感知系统的潜行动作、战术潜入或沉浸式模拟游戏。关键词：视野锥、检测、警戒状态、声音传播、光照等级、系统性AI、渐进式检测。
tags: stealth-ai-design, ai-detection-system, sound-propagation-ai, vision-cone-system,
  godot-development
tags_cn: 潜行游戏AI设计, AI检测系统, 声音传播AI, 视野锥系统, Godot开发
---

# 游戏类型：潜行

玩家选择、系统性AI以及清晰的信息传递是潜行游戏的核心要素。

## 可用脚本

### [stealth_ai_controller.gd](scripts/stealth_ai_controller.gd)
具备渐进式检测、声音响应和警戒状态管理功能的专业AI控制器。

## 核心循环

`观察 → 规划 → 执行 → 适应 → 完成`

## 潜行游戏中的绝对禁忌

- **绝对不要使用即时二元检测** —— 应采用带视觉反馈（填充进度条）的0-100%渐进式检测。二元化的"看见/没看见"会剥夺玩家的主动权，且让玩家感觉不公。
- **绝对不要让守卫穿墙视物** —— 采用基于射线检测（Raycast）的视野系统，并配合碰撞掩码。`has_line_of_sight()`必须检查几何遮挡。穿墙透视会彻底破坏潜行游戏的完整性。
- **绝对不要使用简单的距离检测来判断声音** —— 声音应沿`NavigationServer3D`路径传播，而非直线距离。穿墙听音会破坏沉浸感。
- **绝对不要让战斗和潜行同样可行** —— 如果枪战比潜行更容易，玩家就会忽略潜行玩法。战斗应具备高风险（敌人数量占优、弹药有限、触发大范围警报）。
- **绝对不要向玩家隐藏被检测的原因** —— 要明确显示被检测的原因（光照等级过高、发出噪音、进入视野锥）。"突然死亡"的设计只会让玩家受挫，无法起到教学作用。
- **绝对不要仅用单个采样点判断玩家可见性** —— 应对玩家身体的多个部位（头部、躯干、脚部）进行采样。躲在低矮掩体后应能遮挡躯干，但暴露头部。
- **绝对不要忘记周边视野** —— 人类的周边视野约为180°（辨识度较低）+ 60°的聚焦视野。单一视野锥不符合现实。应采用复合形状（《细胞分裂》的实现方式）。

---

## 设计原则

来自行业专家（《细胞分裂》《耻辱》《杀手》开发者）的经验：

1. **玩家选择权**：每个场景都有多种可行的解决方式
2. **系统性设计**：基于规则的AI，让玩家可以学习并利用其规律
3. **清晰的信息传递**：玩家始终了解游戏状态和威胁
4. **公平的检测机制**：没有"突然死亡"的陷阱——危险出现前玩家就能察觉威胁

---

## AI检测系统

### 视野锥实现

基于《细胞分裂：黑名单》GDC演讲内容——真实的视野系统采用**复合形状**：

```gdscript
class_name EnemyVision
extends Node3D

@export var forward_vision_range := 20.0    # Main vision cone
@export var peripheral_range := 10.0        # Side vision
@export var forward_fov := 60.0             # Degrees
@export var peripheral_fov := 120.0          # Degrees
@export var detection_speed := 1.0          # How fast detection builds

var detection_level := 0.0  # 0-100
var target: Node3D = null

func _physics_process(delta: float) -> void:
    var player := get_player_if_visible()
    if player:
        # Detection rate varies by:
        # - Distance (closer = faster)
        # - Lighting on player
        # - Player movement (moving = more visible)
        # - In peripheral vs direct vision
        var rate := calculate_detection_rate(player)
        detection_level = min(100, detection_level + rate * delta)
    else:
        detection_level = max(0, detection_level - detection_speed * 0.5 * delta)

func get_player_if_visible() -> Player:
    var player := get_tree().get_first_node_in_group("player")
    if not player:
        return null
    
    var to_player := player.global_position - global_position
    var distance := to_player.length()
    var angle := rad_to_deg(global_basis.z.angle_to(-to_player.normalized()))
    
    # Check forward cone
    if angle < forward_fov / 2.0 and distance < forward_vision_range:
        if has_line_of_sight(player):
            return player
    
    # Check peripheral (less effective)
    elif angle < peripheral_fov / 2.0 and distance < peripheral_range:
        if has_line_of_sight(player):
            return player
    
    return null

func calculate_detection_rate(player: Player) -> float:
    var distance := global_position.distance_to(player.global_position)
    var distance_factor := 1.0 - (distance / forward_vision_range)
    
    var light_factor := player.get_light_level()  # 0.0 = dark, 1.0 = lit
    var movement_factor := 1.0 if player.velocity.length() > 0.5 else 0.3
    
    return detection_speed * distance_factor * light_factor * movement_factor * 50.0
```

### 声音检测系统

基于《神偷》/《杀手》的实现方式——声音沿导航路径传播：

```gdscript
class_name SoundPropagation
extends Node

# Sound travels through connected navigation points, not through walls
func propagate_sound(origin: Vector3, loudness: float, sound_type: String) -> void:
    for enemy in get_tree().get_nodes_in_group("enemies"):
        var path := NavigationServer3D.map_get_path(
            get_world_3d().navigation_map,
            origin,
            enemy.global_position,
            true
        )
        
        if path.is_empty():
            continue  # No path = sound blocked
        
        var path_distance := calculate_path_length(path)
        var heard_loudness := loudness - (path_distance * 0.5)  # Falloff
        
        if heard_loudness > enemy.hearing_threshold:
            enemy.hear_sound(origin, sound_type, heard_loudness)

func calculate_path_length(path: PackedVector3Array) -> float:
    var length := 0.0
    for i in range(1, path.size()):
        length += path[i].distance_to(path[i - 1])
    return length
```

### 玩家光照等级

```gdscript
class_name LightDetector
extends Node3D

@export var sample_points: Array[Marker3D]  # Multiple points on player body

func get_light_level() -> float:
    var total := 0.0
    var space := get_world_3d().direct_space_state
    
    for point in sample_points:
        for light in get_tree().get_nodes_in_group("lights"):
            var dir := light.global_position - point.global_position
            var query := PhysicsRayQueryParameters3D.create(
                point.global_position,
                light.global_position
            )
            var result := space.intersect_ray(query)
            
            if result.is_empty():  # Not blocked
                total += light.light_energy / dir.length_squared()
    
    return clamp(total / sample_points.size(), 0.0, 1.0)
```

---

## AI警戒状态

行业标准的三阶段系统：

```gdscript
enum AlertState { IDLE, SUSPICIOUS, ALERTED, COMBAT }

class_name EnemyAI
extends CharacterBody3D

var alert_state := AlertState.IDLE
var suspicion_point: Vector3
var search_timer := 0.0

signal alert_state_changed(new_state: AlertState)

func transition_to(new_state: AlertState) -> void:
    alert_state = new_state
    alert_state_changed.emit(new_state)
    
    match new_state:
        AlertState.SUSPICIOUS:
            play_animation("suspicious")
            speak_dialogue("what_was_that")
        AlertState.ALERTED:
            speak_dialogue("who_goes_there")
            # Other guards in range hear and become suspicious
            alert_nearby_guards()
        AlertState.COMBAT:
            speak_dialogue("intruder")
            trigger_alarm()
```

### 视觉反馈（至关重要！）

```gdscript
class_name AlertIndicator
extends Node3D

@export var idle_icon: Texture2D
@export var suspicious_icon: Texture2D  # "?" 
@export var alerted_icon: Texture2D     # "!"
@export var detection_meter: ProgressBar  # Shows filling detection

func update_indicator(state: AlertState, detection: float) -> void:
    detection_meter.value = detection
    
    match state:
        AlertState.IDLE:
            icon.texture = idle_icon
            detection_meter.visible = false
        AlertState.SUSPICIOUS:
            icon.texture = suspicious_icon
            detection_meter.visible = true
        AlertState.ALERTED:
            icon.texture = alerted_icon
            detection_meter.visible = false
```

---

## 玩家能力

根据Mark Brown的分析，潜行工具分为五大类：

### 1. 移动方式改变

```gdscript
# Crouch, crawl, run (noisy vs quiet)
func calculate_noise_level() -> float:
    if is_crouching:
        return 0.2
    elif is_running:
        return 1.0
    else:
        return 0.5
```

### 2. 信息收集

```gdscript
# Peek, scout, mark enemies
func activate_detective_vision() -> void:
    for enemy in get_tree().get_nodes_in_group("enemies"):
        enemy.show_outline()
        enemy.show_vision_cone()
```

### 3. AI操控

```gdscript
# Throw distractions
func throw_distraction(target_position: Vector3) -> void:
    var rock := distraction_scene.instantiate()
    rock.global_position = target_position
    add_child(rock)
    SoundPropagation.propagate_sound(target_position, 30.0, "impact")
```

### 4. 空间控制

```gdscript
# Shoot out lights, create hiding spots
func shoot_light(light: Light3D) -> void:
    light.visible = false
    # Update light level for area
```

### 5. 敌人清除

```gdscript
func perform_takedown(enemy: EnemyAI, lethal: bool) -> void:
    if enemy.alert_state == AlertState.COMBAT:
        return  # Can't stealth kill alert enemy
    
    if lethal:
        enemy.die()
    else:
        enemy.knockout()
    
    # Body becomes interactable
    spawn_body(enemy)
```

---

## 关卡设计

### 前哨站设计（开放区域）

```
                      [用于观察的安全外围区域]
                               |
           [边缘区域守卫稀疏 - 可单独处理]
                               |
                [核心区域守卫密集，包含目标点]
                               |
              [多个入口与路线选择]
```

### 受限场景设计（走廊）

- 敌人在交战前8米以上即可被玩家看见
- 提供多条通行路径
- 设置掩体和隐藏点
- 规划紧急逃生路线

---

## UI信息传递

基于《神偷》"光宝石"的创新设计：

```gdscript
class_name StealthHUD
extends Control

@onready var visibility_meter: TextureProgressBar
@onready var sound_meter: TextureProgressBar
@onready var minimap: Control

func _process(_delta: float) -> void:
    visibility_meter.value = player.get_light_level() * 100
    sound_meter.value = player.current_noise_level * 100
```

---

## 常见陷阱

| 陷阱 | 解决方案 |
|---------|----------|
| 即时检测 | 使用带清晰反馈的渐进式检测 |
| 守卫穿墙视物 | 采用基于射线检测的视野系统，并正确设置碰撞 |
| 不合理的巡逻路线 | 让巡逻路线可被玩家学习，并设置明显的提示 |
| 两种玩法并行（潜行+战斗） | 要么专注于潜行，要么让战斗具备高风险 |
| 检测原因不明确 | 始终向玩家展示被检测的原因 |

---

## Godot专属技巧

1. **视野射线检测**：使用`PhysicsRayQueryParameters3D`并配合碰撞掩码
2. **NavigationAgent3D**：用于实现巡逻路线和寻路
3. **Area3D**：用于声音传播区域和触发区域
4. **AnimationTree**：实现警戒状态之间的动画过渡


## 参考资料
- 大师技能：[godot-master](../godot-master/SKILL.md)