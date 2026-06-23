---
name: godot-genre-platformer
description: 平台游戏专家蓝图，涵盖精准移动（coyote time、跳跃缓冲、可变跳跃高度）、游戏手感打磨（挤压/拉伸效果、粒子轨迹、镜头震动）、关卡设计原则（难度曲线、Checkpoint
  放置）、收集品系统（进度奖励）以及无障碍选项（辅助模式、可重映射控制）。基于《蔚蓝》（Celeste）/《空洞骑士》（Hollow Knight）的设计研究。触发关键词：platformer、coyote_time、jump_buffer、game_feel、level_design、precision_movement。
tags: platformer-design, game-feel, godot-scripting, level-design, accessibility-options
tags_cn: 平台游戏设计, Game Feel打磨, Godot脚本开发, 关卡设计, 无障碍选项
---

# 游戏类型：Platformer

平台游戏专家蓝图，重点关注移动手感、关卡设计与玩家体验。

## 绝对禁忌

- **绝对不要跳过coyote time** — 离开平台后如果没有6帧的缓冲时间，跳跃会显得反应迟缓。玩家会归咎于自己操作失误。
- **绝对不要忽略跳跃缓冲（jump buffering）** — 落地前6帧按下跳跃键应触发跳跃指令。缺少这一机制会让操控感变得拖沓。
- **绝对不要使用固定跳跃高度** — 可变跳跃（按住时间越长跳得越高）能赋予玩家主动权。轻按实现小跳，长按实现满高度跳跃。
- **绝对不要忘记镜头平滑** — 镜头瞬间切换会引发晕动症。使用position_smoothing或lerp实现平滑跟随。
- **绝对不要省略落地时的挤压/拉伸效果** — 落地时没有视觉反馈会显得毫无重量感。添加0.1秒的落地挤压效果来增强表现力。
---

## 可用脚本

> **强制要求**：在实现对应模式前，请先阅读相应的脚本。

### [advanced_platformer_controller.gd](scripts/advanced_platformer_controller.gd)
完整的平台游戏控制器，包含coyote time、跳跃缓冲、顶点漂浮和可变重力。基于move_toward的摩擦力设计，打造精致的游戏手感。

---

## 核心循环

`跳跃 → 穿越障碍 → 抵达目标 → 进入下一关`

## 技能链

`godot-project-foundations`, `godot-characterbody-2d`, `godot-input-handling`, `animation`, `sound-manager`, `tilemap-setup`, `camera-2d`

---

## 移动手感（"Game Feel"）

这是平台游戏最关键的部分。玩家应感受到**精准、响应迅速且操控自如**。

### 输入响应

```gdscript
# 即时方向切换 - 地面移动无加速度
func _physics_process(delta: float) -> void:
    var input_dir := Input.get_axis("move_left", "move_right")
    
    # 地面移动：即时响应
    if is_on_floor():
        velocity.x = input_dir * MOVE_SPEED
    else:
        # 空中移动：操控性略有降低
        velocity.x = move_toward(velocity.x, input_dir * MOVE_SPEED, AIR_ACCEL * delta)
```

### Coyote Time（缓冲时间）

允许玩家在离开平台后短暂时间内仍可跳跃：

```gdscript
var coyote_timer: float = 0.0
const COYOTE_TIME := 0.1  # 100毫秒缓冲时间

func _physics_process(delta: float) -> void:
    if is_on_floor():
        coyote_timer = COYOTE_TIME
    else:
        coyote_timer = max(0, coyote_timer - delta)
    
    # 当处于地面或缓冲时间内时可跳跃
    if Input.is_action_just_pressed("jump") and coyote_timer > 0:
        velocity.y = JUMP_VELOCITY
        coyote_timer = 0
```

### 跳跃缓冲（Jump Buffering）

记录玩家在落地前短暂时间内按下的跳跃指令：

```gdscript
var jump_buffer: float = 0.0
const JUMP_BUFFER_TIME := 0.15

func _physics_process(delta: float) -> void:
    if Input.is_action_just_pressed("jump"):
        jump_buffer = JUMP_BUFFER_TIME
    else:
        jump_buffer = max(0, jump_buffer - delta)
    
    if is_on_floor() and jump_buffer > 0:
        velocity.y = JUMP_VELOCITY
        jump_buffer = 0
```

### 可变跳跃高度

```gdscript
const JUMP_VELOCITY := -400.0
const JUMP_RELEASE_MULTIPLIER := 0.5

func _physics_process(delta: float) -> void:
    # 松开跳跃键时提前结束跳跃
    if Input.is_action_just_released("jump") and velocity.y < 0:
        velocity.y *= JUMP_RELEASE_MULTIPLIER
```

### 重力调节

```gdscript
const GRAVITY := 980.0
const FALL_GRAVITY_MULTIPLIER := 1.5  # 更快的下落速度手感更好
const MAX_FALL_SPEED := 600.0

func apply_gravity(delta: float) -> void:
    var grav := GRAVITY
    if velocity.y > 0:  # 下落时
        grav *= FALL_GRAVITY_MULTIPLIER
    velocity.y = min(velocity.y + grav * delta, MAX_FALL_SPEED)
```

---

## 关卡设计原则

### “教学三部曲”

1. **入门引导**：在安全环境中学习机制
2. **挑战环节**：在中等风险下应用机制
3. **创新变体**：与其他机制结合或加入时间压力

### 视觉语言

- **安全平台**：独特的颜色/纹理
- **危险区域**：红/橙色调、尖刺、发光效果
- **收集品**：明亮、带动画效果、粒子特效
- **隐藏内容**：微妙的环境提示

### 流程与节奏

```
简单 → 简单 → 中等 → CHECKPOINT → 中等 → 困难 → CHECKPOINT →  Boss
```

### 镜头设计

```gdscript
# 平台游戏的前瞻镜头
extends Camera2D

@export var look_ahead_distance := 100.0
@export var look_ahead_speed := 3.0

var target_offset := Vector2.ZERO

func _process(delta: float) -> void:
    var player_velocity: Vector2 = target.velocity
    var desired_offset := player_velocity.normalized() * look_ahead_distance
    target_offset = target_offset.lerp(desired_offset, look_ahead_speed * delta)
    offset = target_offset
```

---

## 平台游戏子类型

### 精准平台游戏（《蔚蓝》Celeste、《超级食肉男孩》Super Meat Boy）

- 死亡后立即重生
- 极其精准的操控（无加速度）
- 每几秒游戏内容就设置一个Checkpoint
- 死亡是学习过程，而非惩罚

### 收集型平台游戏（《超级马里奥64》Mario 64、《班卓熊大冒险》Banjo-Kazooie）

- 大型枢纽世界，包含多个目标
- 随进度解锁多种能力
- 鼓励回溯探索
- 星星/收集品作为进度闸门

### 解谜平台游戏（《地狱边境》Limbo、《内部》Inside）

- 缓慢、审慎的节奏
- 环境解谜
- 基于物理的机制
- 氛围感叙事

### 银河恶魔城（Metroidvania）（《空洞骑士》Hollow Knight）

- 参考技能：`godot-genre-metroidvania`
- 以能力为 gated 的探索
- 相互连通的世界地图

---

## 常见陷阱

| 陷阱 | 解决方案 |
|---------|----------|
| 跳跃漂浮感 | 增加重力，尤其是下落时的重力 |
| 落地不精准 | 添加coyote time和视觉落地反馈 |
| 不公平死亡 | 确保危险区域在玩家遭遇前清晰可见 |
| 盲跳 | 下落时使用镜头前瞻或拉远视角 |
| 中期游戏乏味 | 每2-3关引入新机制 |

---

## 打磨清单

- [ ] 落地/奔跑时添加 dust godot-particles 效果
- [ ] 重落地时添加屏幕震动
- [ ] 挤压/拉伸动画
- [ ] 为每个动作添加音效（跳跃、落地、滑墙）
- [ ] 死亡与重生动画
- [ ] Checkpoint 的视觉/音频反馈
- [ ] 无障碍难度选项（辅助模式）

---

## Godot 专属技巧

1. **CharacterBody2D vs RigidBody2D**：平台游戏角色始终使用`CharacterBody2D`——精准操控至关重要
2. **物理帧率**：考虑使用120Hz物理帧率实现更流畅的移动
3. **单向平台**：使用`set_collision_mask_value()`或专用碰撞层
4. **墙面检测**：使用`is_on_wall()`和`get_wall_normal()`实现墙跳

---

## 参考游戏示例

- **Celeste（蔚蓝）** - 完美的游戏手感，辅助模式无障碍设计
- **Hollow Knight（空洞骑士）** - 战斗与平台玩法的融合
- **Super Mario Bros. Wonder（超级马里奥兄弟：惊奇）** - 视觉打磨与惊喜设计
- **Shovel Knight（铲子骑士）** - 复古机制结合现代手感


## 参考资料
- 核心技能：[godot-master](../godot-master/SKILL.md)