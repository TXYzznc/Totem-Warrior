---
name: godot-tilemap-mastery
description: 高效2D关卡设计的TileMapLayer与TileSet系统专业指南。涵盖地形自动铺砖、物理层、自定义数据、导航集成及运行时操作。适用于构建网格型关卡或实现可破坏瓦片场景。关键词：TileMapLayer、TileSet、terrain、autotiling、atlas、physics
  layer、custom data。
tags: tilemap-design, tileset-integration, 2d-level-design, godot-development, terrain-autotiling
tags_cn: TileMap设计, TileSet集成, 2D关卡设计, Godot开发, 地形自动铺砖
---

# TileMap精通指南

TileMapLayer网格、TileSet图集、地形自动铺砖及自定义数据构成了高效的2D关卡系统。

## 可用脚本

### [tilemap_data_manager.gd](scripts/tilemap_data_manager.gd)
适用于大型世界的专业TileMap序列化与分块管理器。

## TileMap使用禁忌

- **绝对不要在循环中无批处理调用set_cell()** — 1000个瓦片 × `set_cell()` = 1000次独立函数调用 = 性能低下。批量操作请使用`set_cells_terrain_connect()`，或先缓存修改再一次性应用。
- **绝对不要遗漏source_id参数** — 调用`set_cell(pos, atlas_coords)`却不带source_id？错误的重载会导致崩溃或静默失败。请使用`set_cell(pos, source_id, atlas_coords)`。
- **绝对不要混淆瓦片坐标与世界坐标** — 直接用`set_cell(mouse_position)`却不调用`local_to_map()`？会得到错误的网格位置。务必转换坐标：`local_to_map(global_pos)`。
- **绝对不要跳过地形集配置** — 手动为有机形状分配瓦片？一片草地要选100+个瓦片。请结合地形集使用`set_cells_terrain_connect()`实现自动铺砖。
- **绝对不要用TileMap承载动态实体** — 把敌人/道具设为瓦片？这样无法使用信号、物理系统和脚本。请使用Node2D/CharacterBody2D，TileMap仅用于静态或可破坏几何体。
- **绝对不要在_physics_process中调用get_cell_tile_data()** — 每帧查询瓦片数据？会导致性能暴跌。请把瓦片数据缓存到字典中：`tile_cache[pos] = get_cell_tile_data(pos)`。

---

### 步骤1：创建TileSet资源

1. 创建`TileMapLayer`节点
2. 在检查器中：**TileSet → 新建TileSet**
3. 点击TileSet打开底部的TileSet编辑器

### 步骤2：添加瓦片图集

1. 在TileSet编辑器中：**+ → 图集**
2. 选择你的瓦片集纹理
3. 配置网格大小（例如：16x16像素/瓦片）

### 步骤3：添加物理、碰撞与导航

```gdscript
# 每个瓦片可包含：
# - 物理层：为每个瓦片添加CollisionShape2D
# - 地形：自动铺砖规则
# - 自定义数据：任意属性
```

**为瓦片添加碰撞：**
1. 在TileSet编辑器中选择瓦片
2. 切换到「物理」标签页
3. 绘制碰撞多边形

## 使用TileMapLayer

### 基础TileMap设置

```gdscript
extends TileMapLayer

func _ready() -> void:
    # 在网格坐标(x, y)处设置瓦片
    set_cell(Vector2i(0, 0), 0, Vector2i(0, 0))  # source_id, atlas_coords
    
    # 获取指定坐标的瓦片
    var atlas_coords := get_cell_atlas_coords(Vector2i(0, 0))
    
    # 清除瓦片
    erase_cell(Vector2i(0, 0))
```

### 运行时瓦片放置

```gdscript
extends TileMapLayer

func _input(event: InputEvent) -> void:
    if event is InputEventMouseButton and event.pressed:
        var global_pos := get_global_mouse_position()
        var tile_pos := local_to_map(global_pos)
        
        # 放置草地瓦片（假设source_id=0，atlas坐标为(0,0)）
        set_cell(tile_pos, 0, Vector2i(0, 0))
```

### 洪水填充模式

```gdscript
func flood_fill(start_pos: Vector2i, tile_source: int, atlas_coords: Vector2i) -> void:
    var cells_to_fill: Array[Vector2i] = [start_pos]
    var original_tile := get_cell_atlas_coords(start_pos)
    
    while cells_to_fill.size() > 0:
        var current := cells_to_fill.pop_back()
        
        if get_cell_atlas_coords(current) != original_tile:
            continue
        
        set_cell(current, tile_source, atlas_coords)
        
        # 添加相邻瓦片
        for dir in [Vector2i.UP, Vector2i.DOWN, Vector2i.LEFT, Vector2i.RIGHT]:
            cells_to_fill.append(current + dir)
```

## 地形自动铺砖

### 设置地形集

1. 在TileSet编辑器中：**地形**标签页
2. 添加地形集（例如：「地面」）
3. 添加地形（例如：「草地」、「泥土」）
4. 通过绘制为瓦片分配地形

### 代码中使用地形

```gdscript
extends TileMapLayer

func paint_terrain(start: Vector2i, end: Vector2i, terrain_set: int, terrain: int) -> void:
    for x in range(start.x, end.x + 1):
        for y in range(start.y, end.y + 1):
            set_cells_terrain_connect(
                [Vector2i(x, y)],
                terrain_set,
                terrain,
                false  # ignore_empty_terrains
            )
```

## 多层级模式

```gdscript
# 场景结构:
# Node2D (关卡)
#   ├─ TileMapLayer (地面)
#   ├─ TileMapLayer (装饰)
#   └─ TileMapLayer (碰撞)

# 每个层级可配置不同的：
# - 渲染顺序(z_index)
# - 碰撞层/掩码
# - 调制颜色（色调）
```

## 物理集成

### 启用物理层

1. TileSet编辑器 → **物理层**
2. 添加物理层
3. 为瓦片分配碰撞形状

**代码中检测碰撞：**
```gdscript
func _physics_process(delta: float) -> void:
    # TileMapLayer可作为StaticBody2D使用
    # CharacterBody2D.move_and_slide()会自动检测TileMap碰撞
    pass
```

### 单向碰撞瓦片

```gdscript
# 在TileSet物理层设置中：
# - 启用「单向碰撞」
# - 设置「单向碰撞边距」

# 角色可从下方跳跃穿过
```

## 自定义瓦片数据

### 定义自定义数据层

1. TileSet编辑器 → **自定义数据层**
2. 添加属性（例如："damage_per_second: int"）
3. 为特定瓦片设置值

### 读取自定义数据

```gdscript
func get_tile_damage(tile_pos: Vector2i) -> int:
    var tile_data := get_cell_tile_data(tile_pos)
    if tile_data:
        return tile_data.get_custom_data("damage_per_second")
    return 0
```

## 性能优化

### 使用TileMapLayer分组

```gdscript
# 静态几何体：单个大型TileMapLayer
# 动态瓦片：单独层级用于运行时修改
```

### 大型世界分块

```gdscript
# 将世界拆分为多个TileMapLayer节点
# 根据玩家位置加载/卸载区块

const CHUNK_SIZE := 32

func load_chunk(chunk_coords: Vector2i) -> void:
    var chunk_name := "Chunk_%d_%d" % [chunk_coords.x, chunk_coords.y]
    var chunk := TileMapLayer.new()
    chunk.name = chunk_name
    chunk.tile_set = base_tileset
    add_child(chunk)
    # 加载该区块的瓦片...
```

## 导航集成

### 设置导航层

1. TileSet编辑器 → **导航层**
2. 添加导航层
3. 为瓦片绘制导航多边形

**结合NavigationAgent2D使用：**
```gdscript
# 导航系统会自动从TileMap生成导航数据
# NavigationAgent2D.get_next_path_position()可直接使用
```

## 最佳实践

### 1. 按用途组织TileSet

```
TileSet层级：
- 地面（地形：草地、泥土、石头）
- 墙体（碰撞 + 渲染）
- 装饰（无碰撞，叠加层）
```

## 可用脚本

> **必须阅读**：在实现地形系统或运行时放置功能前请先阅读。

### [terrain_autotile.gd](scripts/terrain_autotile.gd)
基于`set_cells_terrain_connect`批处理与验证的运行时地形自动铺砖脚本。

### [tilemap_chunking.gd](scripts/tilemap_chunking.gd)
基于分块的TileMap管理脚本，支持批处理更新——是大型程序化世界的必备工具。

### 2. 用地形实现有机形状

```gdscript
# ✅ 推荐 - 平滑地形过渡
set_cells_terrain_connect(tile_positions, 0, 0)

# ❌ 不推荐 - 手动为有机形状分配瓦片
for pos in positions:
    set_cell(pos, 0, Vector2i(0, 0))
```

### 3. 层级Z轴顺序管理

```gdscript
# 背景层
$Background.z_index = -10

# 地面层
$Ground.z_index = 0

# 前景装饰层
$Foreground.z_index = 10
```

## 常见模式

### 可破坏瓦片

```gdscript
func destroy_tile(world_pos: Vector2) -> void:
    var tile_pos := local_to_map(world_pos)
    var tile_data := get_cell_tile_data(tile_pos)
    
    if tile_data and tile_data.get_custom_data("destructible"):
        erase_cell(tile_pos)
        # 生成粒子效果、掉落物品等
```

### 瓦片高亮

```gdscript
@onready var highlight_layer: TileMapLayer = $HighlightLayer

func highlight_tile(tile_pos: Vector2i) -> void:
    highlight_layer.clear()
    highlight_layer.set_cell(tile_pos, 0, Vector2i(0, 0))
```

## 参考资料
- [Godot文档：TileMaps](https://docs.godotengine.org/en/stable/tutorials/2d/using_tilemaps.html)
- [Godot文档：TileSets](https://docs.godotengine.org/en/stable/tutorials/2d/using_tilesets.html)


### 相关技能
- 大师技能：[godot-master](../godot-master/SKILL.md)
