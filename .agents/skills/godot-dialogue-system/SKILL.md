---
name: godot-dialogue-system
description: 分支对话系统的专家级实现模式，包括基于资源的对话图、角色立绘、玩家选择、条件对话（标记/任务）、打字机效果、本地化支持以及配音集成。适用于叙事游戏、RPG或视觉小说。触发关键词：DialogueLine、DialogueChoice、DialogueGraph、dialogue_manager、typewriter_effect、branching_dialogue、dialogue_flags、localization、voice_acting。
tags: branching-dialogue-system, godot-engine, gdscript, dialogue-ui, localization-support
tags_cn: 分支对话系统, Godot引擎, GDScript开发, 对话UI, 本地化支持
---

# 对话系统

构建灵活、数据驱动型对话系统的专家指南。

## 绝对不要做的事

- **绝对不要在脚本中硬编码对话**——使用基于资源的DialogueLine/DialogueGraph。硬编码对话不利于本地化维护。
- **绝对不要忘记检查选择条件**——显示不可用的选择会让玩家困惑。显示前通过`check_conditions()`过滤选项。
- **绝对不要使用未验证的字符串ID**——`next_line_id`中的拼写错误会导致静默失败。添加`assert(dialogues.has(line_id))`检查。
- **绝对不要在没有玩家选项的情况下跳过打字机效果**——有些玩家希望文本立即显示。添加“跳过打字机”按钮或设置。
- **绝对不要在UI中存储对话状态**——UI应仅负责显示。在DialogueManager（AutoLoad）中存储current_line/dialogue_id以支持场景切换。
---

## 可用脚本

> **强制要求**：在实现对应模式前，请阅读相应脚本。

### [dialogue_engine.gd](scripts/dialogue_engine.gd)
基于图的对话系统，支持BBCode信号标签。解析文本中的[trigger:event_id]标签，触发信号，并加载外部JSON对话图。

### [dialogue_manager.gd](scripts/dialogue_manager.gd)
支持分支、变量存储和条件选择的数据驱动型对话引擎。

---

## 对话数据

```gdscript
# dialogue_line.gd
class_name DialogueLine
extends Resource

@export var speaker: String
@export_multiline var text: String
@export var portrait: Texture2D
@export var choices: Array[DialogueChoice] = []
@export var conditions: Array[String] = []  # Quest flags, etc.
@export var next_line_id: String = ""
```

```gdscript
# dialogue_choice.gd
class_name DialogueChoice
extends Resource

@export var choice_text: String
@export var next_line_id: String
@export var conditions: Array[String] = []
@export var effects: Array[String] = []  # Set flags, give items
```

## 对话管理器

```gdscript
# dialogue_manager.gd (AutoLoad)
extends Node

signal dialogue_started
signal dialogue_ended
signal line_displayed(line: DialogueLine)
signal choice_selected(choice: DialogueChoice)

var dialogues: Dictionary = {}
var flags: Dictionary = {}

func load_dialogue(path: String) -> void:
    var data := load(path)
    dialogues[path] = data

func start_dialogue(dialogue_id: String, start_line: String = "start") -> void:
    dialogue_started.emit()
    display_line(dialogue_id, start_line)

func display_line(dialogue_id: String, line_id: String) -> void:
    var line: DialogueLine = dialogues[dialogue_id].lines[line_id]
    
    # Check conditions
    if not check_conditions(line.conditions):
        # Skip to next
        if line.next_line_id:
            display_line(dialogue_id, line.next_line_id)
        else:
            end_dialogue()
        return
    
    line_displayed.emit(line)
    
    # Auto-advance or wait for player
    if line.choices.is_empty() and line.next_line_id:
        # Wait for player to click
        await get_tree().create_timer(0.1).timeout
    elif line.choices.is_empty():
        end_dialogue()

func select_choice(dialogue_id: String, choice: DialogueChoice) -> void:
    choice_selected.emit(choice)
    
    # Apply effects
    for effect in choice.effects:
        apply_effect(effect)
    
    # Continue to next line
    if choice.next_line_id:
        display_line(dialogue_id, choice.next_line_id)
    else:
        end_dialogue()

func end_dialogue() -> void:
    dialogue_ended.emit()

func check_conditions(conditions: Array[String]) -> bool:
    for condition in conditions:
        if not flags.get(condition, false):
            return false
    return true

func apply_effect(effect: String) -> void:
    # Parse effect string, e.g., "set_flag:met_npc"
    var parts := effect.split(":")
    match parts[0]:
        "set_flag":
            flags[parts[1]] = true
        "give_item":
            # Integration with inventory
            pass
```

## 对话UI

```gdscript
# dialogue_ui.gd
extends Control

@onready var speaker_label := $Panel/Speaker
@onready var text_label := $Panel/Text
@onready var portrait := $Panel/Portrait
@onready var choices_container := $Panel/Choices

var current_dialogue: String
var current_line: DialogueLine

func _ready() -> void:
    DialogueManager.line_displayed.connect(_on_line_displayed)
    DialogueManager.dialogue_ended.connect(_on_dialogue_ended)
    visible = false

func _on_line_displayed(line: DialogueLine) -> void:
    visible = true
    current_line = line
    
    speaker_label.text = line.speaker
    portrait.texture = line.portrait
    
    # Typewriter effect
    text_label.text = ""
    for char in line.text:
        text_label.text += char
        await get_tree().create_timer(0.03).timeout
    
    # Show choices
    if line.choices.is_empty():
        # Wait for input to continue
        pass
    else:
        show_choices(line.choices)

func show_choices(choices: Array[DialogueChoice]) -> void:
    # Clear existing
    for child in choices_container.get_children():
        child.queue_free()
    
    # Add choice buttons
    for choice in choices:
        if not DialogueManager.check_conditions(choice.conditions):
            continue
        
        var button := Button.new()
        button.text = choice.choice_text
        button.pressed.connect(func(): _on_choice_selected(choice))
        choices_container.add_child(button)

func _on_choice_selected(choice: DialogueChoice) -> void:
    DialogueManager.select_choice(current_dialogue, choice)

func _on_dialogue_ended() -> void:
    visible = false
```

## NPC交互

```gdscript
# npc.gd
extends CharacterBody2D

@export var dialogue_path: String = "res://dialogues/npc_1.tres"
@export var start_line: String = "start"

func interact() -> void:
    DialogueManager.start_dialogue(dialogue_path, start_line)
```

## 对话图（资源）

```gdscript
# dialogue_graph.gd
class_name DialogueGraph
extends Resource

@export var lines: Dictionary = {}  # line_id → DialogueLine

func _init() -> void:
    # Example structure
    lines["start"] = create_line("Hero", "Hello!")
    lines["response"] = create_line("NPC", "Greetings, traveler!")

func create_line(speaker: String, text: String) -> DialogueLine:
    var line := DialogueLine.new()
    line.speaker = speaker
    line.text = text
    return line
```

## 本地化

```gdscript
# Use Godot's built-in CSV import
# dialogue_en.csv:
# dialogue_id,speaker,text
# npc_1_start,Hero,"Hello!"
# npc_1_response,NPC,"Greetings!"

func get_localized_line(line_id: String) -> String:
    return tr(line_id)
```

## 进阶：配音

```gdscript
@onready var voice_player := $AudioStreamPlayer

func play_voice_line(line_id: String) -> void:
    var audio := load("res://voice/" + line_id + ".mp3")
    if audio:
        voice_player.stream = audio
        voice_player.play()
```

## 最佳实践

1. **基于资源** - 将对话存储为资源
2. **标记系统** - 跟踪玩家选择
3. **打字机效果** - 提升质感
4. **跳过按钮** - 允许玩家跳过

## 参考
- 相关：`godot-signal-architecture`, `godot-save-load-systems`, `godot-ui-rich-text`


### 相关内容
- 核心技能：[godot-master](../godot-master/SKILL.md)
