---
name: godot-genre-puzzle
description: 解谜游戏专家蓝图，包含撤销系统（用于状态回退的命令模式）、基于网格的逻辑（推箱子式机制）、非文字教程（通过关卡设计教学）、胜利条件检测、状态管理以及视觉反馈（有效操作的即时确认）。适用于逻辑解谜、物理解谜或三消类游戏。触发关键词：puzzle_game,
  undo_system, command_pattern, grid_logic, non_verbal_tutorial, state_management.
tags: puzzle-game-development, command-pattern, undo-system, godot-engine, grid-based-mechanics
tags_cn: 解谜游戏开发, Command模式, 撤销系统, Godot引擎, 网格机制
---

# 游戏类型：解谜

解谜游戏专家蓝图，强调清晰性、实验性和“顿悟”时刻。

## 绝对不要做的事

- **绝对不要惩罚实验行为**——解谜游戏的核心是测试想法。始终提供撤销/重置功能，不要为尝试操作施加惩罚。
- **绝对不要要求像素级精准的输入**——逻辑解谜游戏不应需要精准瞄准。使用网格吸附或宽松的碰撞盒。
- **绝对不要允许未被察觉的无解状态**——自动检测软锁状态，或提供醒目的“重置关卡”按钮。
- **绝对不要隐藏规则**——视觉反馈必须即时且清晰。例如线路连接时亮起即可直观传达规则。
- **绝对不要跳过非文字教程**——第1关=单独引入新机制。第2关=简单运用该机制。第3关=与现有机制结合使用。
---

## 可用脚本

> **强制要求**：在实现对应模式前，请阅读相应脚本。

### [command_undo_redo.gd](scripts/command_undo_redo.gd)
用于撤销/重做的命令模式。在双栈中存储可撤销操作，执行新操作时清空重做栈。包含MoveCommand示例。

---

## 核心循环
1.  **观察**：玩家评估关卡布局和机制。
2.  **实验**：玩家与元素交互（推动、拉动、切换）。
3.  **反馈**：游戏做出响应（门打开、激光被阻挡）。
4.  **顿悟**：玩家理解逻辑（“顿悟！”时刻）。
5.  **执行**：玩家执行解决方案以推进关卡。

## 技能链

| 阶段 | 技能 | 目的 |
|-------|--------|---------|
| 1. 交互 | `godot-input-handling`, `raycasting` | 点击、拖拽、网格移动 |
| 2. 逻辑 | `command-pattern`, `state-management` | 撤销/重做、追踪关卡状态 |
| 3. 反馈 | `godot-tweening`, `juice` | 有效操作的视觉确认 |
| 4. 进度 | `godot-save-load-systems`, `level-design` | 解锁关卡、追踪星级/分数 |
| 5. 打磨 | `ui-minimalism` | 非侵入式HUD |

## 架构概述

### 1. 命令模式（撤销系统）
解谜游戏的必备功能。绝对不要惩罚玩家的尝试行为。

```gdscript
# command.gd
class_name Command extends RefCounted

func execute() -> void: pass
func undo() -> void: pass

# level_manager.gd
var history: Array[Command] = []
var history_index: int = -1

func commit_command(cmd: Command) -> void:
    # 若操作分支则清空重做历史
    if history_index < history.size() - 1:
        history = history.slice(0, history_index + 1)
        
    cmd.execute()
    history.append(cmd)
    history_index += 1

func undo() -> void:
    if history_index >= 0:
        history[history_index].undo()
        history_index -= 1
```

### 2. 网格系统（TileMap vs 自定义）
对于基于网格的解谜游戏（如推箱子），自定义数据结构通常比仅读取物理引擎更好。

```gdscript
# grid_manager.gd
var grid_size: Vector2i = Vector2i(16, 16)
var objects: Dictionary = {} # Vector2i -> Node

func move_object(obj: Node, direction: Vector2i) -> bool:
    var start_pos = grid_pos(obj.position)
    var target_pos = start_pos + direction
    
    if is_wall(target_pos):
        return false
        
    if objects.has(target_pos):
        # 在此处处理推动逻辑
        return false
        
    # 执行移动
    objects.erase(start_pos)
    objects[target_pos] = obj
    tween_movement(obj, target_pos)
    return true
```

## 关键机制实现

### 胜利条件检测
每次操作后检查胜利状态。

```gdscript
func check_win_condition() -> void:
    for target in targets:
        if not is_satisfied(target):
            return
    
    level_complete.emit()
    save_progress()
```

### 非文字教程
通过关卡设计教授机制，而非文字说明。
1.  **隔离**：第1关仅在安全场景中引入新机制。
2.  **强化**：第2关要求玩家使用该机制解决一个简单问题。
3.  **结合**：第3关将该机制与之前的机制结合使用。

## 常见陷阱

1.  **严苛性**：逻辑解谜游戏要求像素级精准输入。**修复方案**：使用网格吸附或宽松的碰撞盒。
2.  **死胡同**：允许玩家进入无解状态却未察觉。**修复方案**：自动检测失败状态或提供醒目的“重置”按钮。
3.  **模糊性**：隐藏规则。**修复方案**：视觉反馈必须即时且清晰（例如，线路连接时亮起）。

## Godot专属技巧

*   **Tweens**：所有网格移动都使用`create_tween()`，比即时吸附的体验好得多。
*   **自定义资源**：将关卡数据（布局、起始位置）存储在`.tres`文件中，以便在Inspector中轻松编辑。
*   **信号**：使用`state_changed`等信号来更新UI/视觉效果，与逻辑解耦。


## 参考资料
- 核心技能：[godot-master](../godot-master/SKILL.md)
