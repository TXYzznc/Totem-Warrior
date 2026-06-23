---
name: godot-3d-world-building
description: 使用GridMap结合MeshLibrary、CSG构造实体几何、WorldEnvironment设置、ProceduralSkyMaterial和体积雾进行3D关卡设计的专家模式。适用于构建3D关卡、模块化tileset、BSP风格几何体或环境效果。触发关键词：GridMap、MeshLibrary、set_cell_item、get_cell_item、map_to_local、local_to_map、CSGCombiner3D、CSGBox3D、CSGSphere3D、CSGPolygon3D、WorldEnvironment、Environment、Sky、ProceduralSkyMaterial、PanoramaSkyMaterial、fog_enabled、volumetric_fog_enabled。
tags: 3d-level-design, gridmap-workflow, csg-geometry, godot-engine, environmental-effects
tags_cn: 3D关卡设计, GridMap工作流, CSG几何构建, Godot引擎, 环境效果设置
---

# 3D世界构建

使用GridMap、CSG和环境设置进行关卡设计的专家指南。

## 绝对不要做的事

- **绝对不要忘记烘焙GridMap导航网格** — GridMap不会自动生成导航网格。请使用EditorPlugin或手动创建NavigationRegion3D。
- **绝对不要将CSG用于最终游戏几何体** — CSG仅用于原型设计。为了性能，请转换为静态网格（使用编辑器中的“Bake CSG Mesh”功能）。
- **绝对不要在放置tiles后缩放GridMap的单元格大小** — 修改`cell_size`不会更新已放置的tiles，会导致对齐错误。请在开始时就设置好。
- **绝对不要使用没有碰撞形状的MeshLibrary** — 没有碰撞的物品会生成仅可见的几何体，玩家会直接穿过去。
- **绝对不要在没有DirectionalLight3D的情况下启用体积雾** — 体积雾至少需要一个光源来散射。没有光源的话，雾是不可见的。

---

## 可用脚本

> **强制要求**：在实现相应模式之前，请阅读对应的脚本。

### [collision_gen.gd](scripts/collision_gen.gd)
从网格自动生成碰撞形状。适用于导入无碰撞的模型或程序化几何体。

### [gridmap_runtime_builder.gd](scripts/gridmap_runtime_builder.gd)
运行时GridMap tile放置，支持批量操作和自动导航烘焙。

### [csg_bake_tool.gd](scripts/csg_bake_tool.gd)
将CSG几何体烘焙为带正确材质和碰撞的静态网格的EditorScript。适用于完成关卡原型时使用。

### [lod_manager.gd](scripts/lod_manager.gd)
基于相机距离的细节级别切换。管理大型户外场景中的网格切换和可见性。

### [occlusion_setup.gd](scripts/occlusion_setup.gd)
用于手动遮挡剔除的OccluderInstance3D配置。适用于有多个房间的室内关卡。

---

## GridMap基础

### 设置工作流

```gdscript
# 1. Create MeshLibrary resource (editor)
# Scene → New Inherits Scene → Create Grid-aligned meshes
# Scene → Convert To → MeshLibrary...

# 2. Assign to GridMap
extends GridMap

func _ready() -> void:
    mesh_library = load("res://tilesets/dungeon_library.tres")
    cell_size = Vector3(2, 2, 2)  # Must match library cell size
```

### 单元格操作

```gdscript
# gridmap_builder.gd
extends GridMap

# Place cell
func place_tile(grid_pos: Vector3i, tile_index: int) -> void:
    set_cell_item(grid_pos, tile_index)

# Get cell
func get_tile(grid_pos: Vector3i) -> int:
    return get_cell_item(grid_pos)  # Returns index or INVALID_CELL_ITEM (-1)

# Remove cell
func remove_tile(grid_pos: Vector3i) -> void:
    set_cell_item(grid_pos, INVALID_CELL_ITEM)

# Rotate cell (0-23, see GridMap.ROTATION_* constants)
func place_rotated(grid_pos: Vector3i, tile_index: int, orientation: int) -> void:
    set_cell_item(grid_pos, tile_index, orientation)
```

### 坐标转换

```gdscript
# World position ↔ Grid coordinates
func _input(event: InputEvent) -> void:
    if event is InputEventMouseButton and event.pressed:
        var camera := get_viewport().get_camera_3d()
        var from := camera.project_ray_origin(event.position)
        var to := from + camera.project_ray_normal(event.position) * 1000
        
        var space := get_world_3d().direct_space_state
        var query := PhysicsRayQueryParameters3D.create(from, to)
        var result := space.intersect_ray(query)
        
        if result:
            var world_pos: Vector3 = result.position
            var grid_pos := local_to_map(to_local(world_pos))
            place_tile(grid_pos, 0)  # Place tile at clicked position

# Grid → World
func get_cell_center(grid_pos: Vector3i) -> Vector3:
    return to_global(map_to_local(grid_pos))
```

---

## MeshLibrary创建

### 碰撞设置

```gdscript
# tile_scene.tscn (before converting to MeshLibrary)
# Root: Node3D
#   ├─ MeshInstance3D (visual)
#   └─ StaticBody3D (collision)
#       └─ CollisionShape3D

# CRITICAL: StaticBody3D must be sibling/child for GridMap to detect collision
```

### 物品元数据

```gdscript
# Access MeshLibrary item data
func get_tile_name(tile_index: int) -> String:
    return mesh_library.get_item_name(tile_index)

# Custom metadata (stored in MeshLibrary resource)
# Use item_set_name() in editor script to organize
```

---

## CSG（构造实体几何）

### 布尔运算

```
CSG Combiner3D
  ├─ CSGBox3D (Operation: Union)        # Base room
  ├─ CSGBox3D (Operation: Subtraction)  # Door cutout
  └─ CSGSphere3D (Operation: Intersection)  # Rounded corner
```

### CSG画笔类型

```gdscript
# CSGBox3D - Room primitives
var room := CSGBox3D.new()
room.size = Vector3(10, 5, 10)

# CSGCylinder3D - Pillars
var pillar := CSGCylinder3D.new()
pillar.radius = 0.5
pillar.height = 5.0

# CSGSphere3D - Domes
var dome := CSGSphere3D.new()
dome.radius = 3.0
dome.radial_segments = 16
dome.rings = 8

# CSGPolygon3D - Extruded 2D shapes
var arch := CSGPolygon3D.new()
arch.polygon = PackedVector2Array([
    Vector2(-1, 0), Vector2(-1, 2), Vector2(1, 2), Vector2(1, 0)
])
arch.depth = 0.5
```

### CSG性能

```gdscript
# ❌ BAD: Use CSG at runtime (slow)
func _ready() -> void:
    var csg := CSGBox3D.new()
    add_child(csg)  # Recalculates mesh every frame

# ✅ GOOD: Bake to MeshInstance3D (editor only)
# Select CSG node → Mesh → Bake Mesh Instance
# Then delete CSG node

# ✅ ALSO GOOD: Use CSG for level editor, bake on export
```

---

## WorldEnvironment设置

### 天空配置

```gdscript
# world_env.gd
extends WorldEnvironment

func _ready() -> void:
    var env := Environment.new()
    environment = env
    
    # Procedural sky
    env.background_mode = Environment.BG_SKY
    var sky := Sky.new()
    var sky_mat := ProceduralSkyMaterial.new()
    
    sky_mat.sky_top_color = Color(0.4, 0.6, 1.0)  # Blue
    sky_mat.sky_horizon_color = Color(0.8, 0.9, 1.0)  # Lighter
    sky_mat.ground_bottom_color = Color(0.2, 0.2, 0.1)
    sky_mat.sun_angle_max = 30.0
    
    sky.sky_material = sky_mat
    env.sky = sky
```

### HDRI天空盒

```gdscript
# For realistic lighting
var env := environment
env.background_mode = Environment.BG_SKY

var sky := Sky.new()
var panorama := PanoramaSkyMaterial.new()
panorama.panorama = load("res://hdri/sunset.hdr")  # Equirectangular HDR image

sky.sky_material = panorama
env.sky = sky

# Sky contribution to ambient light
env.ambient_light_source = Environment.AMBIENT_SOURCE_SKY
env.ambient_light_sky_contribution = 1.0
```

---

## 雾与大气

### 指数雾

```gdscript
extends WorldEnvironment

func _ready() -> void:
    var env := environment
    
    env.fog_enabled = true
    env.fog_mode = Environment.FOG_MODE_EXPONENTIAL
    env.fog_density = 0.01  # 0.0-1.0
    env.fog_light_color = Color(0.9, 0.95, 1.0)  # Blueish
    env.fog_light_energy = 1.0
```

### 深度雾

```gdscript
# Distance-based fog
env.fog_enabled = true
env.fog_mode = Environment.FOG_MODE_DEPTH
env.fog_depth_begin = 50.0  # Start distance
env.fog_depth_end = 200.0   # End distance (fully opaque)
env.fog_depth_curve = 1.0   # Falloff curve
```

### 体积雾

```gdscript
# Requires DirectionalLight3D for scattering
env.volumetric_fog_enabled = true
env.volumetric_fog_density = 0.05
env.volumetric_fog_albedo = Color(0.9, 0.9, 1.0)
env.volumetric_fog_emission = Color.BLACK
env.volumetric_fog_gi_inject = 1.0  # How much GI affects fog

# Performance settings
env.volumetric_fog_temporal_reprojection_enabled = true
env.volumetric_fog_detail_spread = 2.0
```

---

## 关卡流加载/LOD

### GridMap分块

```gdscript
# level_streamer.gd - Load/unload GridMap chunks based on player position
extends Node3D

@export var chunk_size := 32  # Grid cells per chunk
@export var load_radius := 2  # Chunks to keep loaded

var loaded_chunks := {}  # Vector2i → GridMap

func _process(delta: float) -> void:
    var player_pos := get_player_position()
    var player_chunk := Vector2i(
        int(player_pos.x / (chunk_size * cell_size.x)),
        int(player_pos.z / (chunk_size * cell_size.z))
    )
    
    # Load nearby chunks
    for x in range(-load_radius, load_radius + 1):
        for z in range(-load_radius, load_radius + 1):
            var chunk_coord := player_chunk + Vector2i(x, z)
            if chunk_coord not in loaded_chunks:
                load_chunk(chunk_coord)
    
    # Unload distant chunks
    for chunk_coord in loaded_chunks.keys():
        var dist := chunk_coord.distance_to(player_chunk)
        if dist > load_radius:
            unload_chunk(chunk_coord)

func load_chunk(coord: Vector2i) -> void:
    var gridmap := GridMap.new()
    gridmap.mesh_library = preload("res://library.tres")
    add_child(gridmap)
    loaded_chunks[coord] = gridmap
    
    # TODO: Load chunk data from file/database
    # gridmap.set_cell_item(...)

func unload_chunk(coord: Vector2i) -> void:
    var gridmap: GridMap = loaded_chunks[coord]
    gridmap.queue_free()
    loaded_chunks.erase(coord)
```

---

## 程序化生成

### 使用GridMap生成随机地牢

```gdscript
# dungeon_generator.gd
extends GridMap

enum Tile { FLOOR, WALL, DOOR }

func generate_room(pos: Vector3i, size: Vector3i) -> void:
    # Fill with floor
    for x in range(size.x):
        for z in range(size.z):
            set_cell_item(pos + Vector3i(x, 0, z), Tile.FLOOR)
    
    # Add walls
    for x in range(size.x):
        set_cell_item(pos + Vector3i(x, 0, 0), Tile.WALL)  # North
        set_cell_item(pos + Vector3i(x, 0, size.z - 1), Tile.WALL)  # South
    
    for z in range(size.z):
        set_cell_item(pos + Vector3i(0, 0, z), Tile.WALL)  # West
        set_cell_item(pos + Vector3i(size.x - 1, 0, z), Tile.WALL)  # East

func _ready() -> void:
    generate_room(Vector3i(0, 0, 0), Vector3i(10, 1, 10))
```

---

## 边缘情况

### GridMap单元格无碰撞

```gdscript
# Problem: MeshLibrary items lack collision
# Solution: Ensure StaticBody3D + CollisionShape3D in source scene

# Verify in code:
var item_shapes := mesh_library.get_item_shapes(tile_index)
if item_shapes.is_empty():
    push_error("Tile %d has no collision!" % tile_index)
```

### CSG网格闪烁

```gdscript
# Problem: Z-fighting between overlapping CSG operations
# Solution: Add small offset (0.001) to prevent exact overlap

var box := CSGBox3D.new()
box.size = Vector3(10, 5, 10)

var cutout := CSGBox3D.new()
cutout.operation = CSGShape3D.OPERATION_SUBTRACTION
cutout.size = Vector3(2, 3, 2.002)  # Slightly larger depth
```


## 参考
- 主技能：[godot-master](../godot-master/SKILL.md)
