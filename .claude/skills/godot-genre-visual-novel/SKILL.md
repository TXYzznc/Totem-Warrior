---
name: godot-genre-visual-novel
description: 面向视觉小说（如《心跳文学部》《逆转裁判》《命运石之门》）的专业开发蓝图，聚焦分支叙事、对话系统、选择后果、回滚机制与持久化标记。适用于构建剧情驱动、选择导向或恋爱模拟类游戏。关键词：视觉小说、对话系统、分支叙事、打字机效果、回滚、BBCode、RichTextLabel。
tags: visual-novel-development, godot-engine, branching-narrative, dialogue-system,
  rollback-mechanic
tags_cn: 视觉小说开发, Godot引擎, 分支叙事系统, 对话系统, 回滚机制
---

# 类型：视觉小说

分支叙事、有意义的选择以及便捷功能是视觉小说的核心特征。

## 核心循环
1.  **阅读**：浏览剧情文本与角色对话
2.  **抉择**：在关键节点做出选择
3.  **分支**：剧情根据选择走向不同分支
4.  **后果**：产生即时反应或长期的标记变化
5.  **结局**：达成多个结局中的一个

## 视觉开发绝对禁忌

- **绝对不要制造选择幻觉**——如果所有选项最终立刻导向同一结果，玩家会有被欺骗感。即便后续剧情会收敛，也务必要提供不同的对话内容或标记变化。
- **绝对不要省略自动播放、跳过与存档/读档功能**——这些是视觉小说类型的必备功能。玩家会反复游玩以解锁所有路线。缺少这些便捷功能会让视觉小说的品质大打折扣。
- **绝对不要展示大段无分割的文本**——超过6行的文本会让玩家望而生畏。每个对话框的文本限制在3-4行以内，可插入角色反应或停顿来分割文本。
- **绝对不要在脚本中硬编码对话**——编剧并非程序员。请使用JSON/CSV或自定义格式。硬编码会让剧情修改陷入迭代地狱。
- **绝对不要忘记回滚/历史功能**——玩家可能误点选择或想要重读内容。缺少回滚功能会让玩家感到挫败。在每一行文本前保存状态，使用历史栈来实现。
- **绝对不要忽视BBCode/富文本效果**——纯文本会显得乏味。在RichTextLabel中使用`[wave]`、`[shake]`、`[color]`等标签来增强情感表达。比如"我 [shake]讨厌[/shake] 你" 远胜于 "我讨厌你"。

---

| 阶段 | 技能 | 用途 |
|-------|--------|---------|
| 1. 文本与UI | `ui-system`, `rich-text-label` | 对话框、BBCode效果、打字机效果 |
| 2. 逻辑 | `json-parsing`, `resource-management` | 加载脚本、管理角色数据 |
| 3. 状态 | `godot-save-load-systems`, `dictionaries` | 标记、历史记录、持久化数据 |
| 4. 音频 | `audio-system` | 配音、背景音乐切换 |
| 5. 打磨 | `godot-tweening`, `shaders` | 角色切换、背景特效 |

## 架构概述

### 1. 剧情管理器（核心驱动）
解析脚本并调度其他系统。

```gdscript
# story_manager.gd
extends Node

var current_script: Dictionary
var current_line_index: int = 0
var flags: Dictionary = {}

func load_script(script_path: String) -> void:
    var file = FileAccess.open(script_path, FileAccess.READ)
    current_script = JSON.parse_string(file.get_as_text())
    current_line_index = 0
    display_next_line()

func display_next_line() -> void:
    if current_line_index >= current_script["lines"].size():
        return
        
    var line_data = current_script["lines"][current_line_index]
    
    if line_data.has("choice"):
        present_choices(line_data["choice"])
    else:
        CharacterManager.show_character(line_data.get("character"), line_data.get("expression"))
        DialogueUI.show_text(line_data["text"])
        current_line_index += 1
```

### 2. 对话UI（打字机效果）
逐字符显示文本。

```gdscript
# dialogue_ui.gd
func show_text(text: String) -> void:
    rich_text_label.text = text
    rich_text_label.visible_ratio = 0.0
    
    var tween = create_tween()
    tween.tween_property(rich_text_label, "visible_ratio", 1.0, text.length() * 0.05)
```

### 3. 历史记录与回滚
视觉小说的必备功能。在每一行文本前保存状态。

```gdscript
var history: Array[Dictionary] = []

func save_state_to_history() -> void:
    history.append({
        "line_index": current_line_index,
        "flags": flags.duplicate(),
        "background": current_background,
        "music": current_music
    })

func rollback() -> void:
    if history.is_empty(): return
    var trusted_state = history.pop_back()
    restore_state(trusted_state)
```

## 核心机制实现

### 分支路线（标记）
追踪玩家的决定以影响后续剧情。

```gdscript
func make_choice(choice_id: String) -> void:
    match choice_id:
        "be_nice":
            flags["relationship_alice"] += 1
            jump_to_label("alice_happy")
        "be_mean":
            flags["relationship_alice"] -= 1
            jump_to_label("alice_sad")
```

### 脚本格式（JSON vs 资源）
*   **JSON**: 便于外部编写，是标准格式。
*   **自定义资源**: 避免打字错误，可在Inspector中编辑。
*   **文本解析器**:（如类Markdown语法）对编剧更友好。

## 常见陷阱

1.  **文本过长**: 大段文本会让玩家望而生畏。**解决方法**: 限制每行最多3-4行文本。
2.  **选择幻觉**: 选择后立刻导向同一结果会显得廉价。**解决方法**: 即便主剧情收敛，也要提供不同的对话变体。
3.  **缺少便捷功能**: 没有跳过、自动播放、存档功能。**解决方法**: 这些是该类型的必备功能。

## Godot专属技巧

*   **RichTextLabel**: 使用BBCode的`[wave]`、`[shake]`、`[color]`等效果为文本增添情感。
*   **资源预加载器**: 视觉小说包含大量高分辨率资源（如4K背景）。可在章节间异步加载场景或使用加载界面。
*   **Dialogic**: 必须提及这个插件——它是Godot视觉小说开发的行业标准。如果需要全套工具可以使用它，若需求轻量化则可自行搭建。


## 参考
- 核心技能: [godot-master](../godot-master/SKILL.md)
