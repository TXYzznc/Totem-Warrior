---
name: anvil-level-design-blender
description: Anvil Level Design Blender插件专家，这款插件借鉴Trenchbroom的设计理念，用于游戏关卡设计，具备自动化材质/UV管理和几何工具
triggers:
- how do I use Anvil Level Design in Blender
- set up material application with Anvil addon
- apply textures automatically in Blender level design
- create hotspot mapping for texture atlas
- use Anvil geometry tools for level design
- configure UV management in Anvil Blender
- export levels with Anvil Level Design
- troubleshoot Anvil texture application
tags: blender-addon, level-design, uv-management, material-tools, hotspot-mapping
tags_cn: Blender插件, 关卡设计, UV管理, 材质工具, 热点映射
---

# Anvil Level Design Blender插件

> 由[ara.so](https://ara.so)开发的技能——设计技能合集。

Anvil Level Design（Anvil LD）是一款Blender插件，结合了Trenchbroom风格的游戏关卡设计工具。它具备自动化材质应用与UV管理、纹理图集热点映射、相机与网格工具、带背面剔除选择的几何算子，以及优化后的GLB导出工作流。

## 安装

**要求：**
- Blender 5.1或更高版本
- Python（Blender自带）

**安装步骤：**

1. 从GitHub下载仓库压缩包（ZIP格式）
2. 在Blender中：编辑 → 偏好设置 → 插件
3. 点击下拉箭头 → 从磁盘安装
4. 选择下载的ZIP文件
5. 在偏好设置中启用该插件

**初始设置：**

```python
# 访问插件偏好设置以创建工作区
# 编辑 → 偏好设置 → 插件 → Anvil Level Design
# 点击“创建关卡设计工作区”和“创建热点映射工作区”
```

该插件会添加两个自定义工作区：
- **关卡设计** - 用于关卡几何建模与纹理制作的主工作区
- **热点映射** - 用于定义纹理图集热点的工作区

**自定义快捷键重映射：**

所有插件快捷键都集中在插件偏好设置中，方便重映射。常见可自定义的快捷键：
- 相机工具
- 纹理应用工具
- 几何算子

## 核心概念

### 材质与UV管理理念

Anvil会自动管理材质以避免重复。材质应用时会自动进行UV展开，确保纹理在面之间无缝平铺。**重要提示**：不要在对象模式下使用缩放算子调整大小——应通过移动和挤出面来调整，以保持正确的UV坐标。

### 工作区要求

必须处于“关卡设计”或“热点映射”工作区才能使用Anvil的功能。功能是基于工作区上下文的。

## 核心命令 - 材质应用

### 基础纹理应用

**从文件浏览器应用纹理：**
```python
# 1. 在编辑模式下选择一个面
# 2. 在文件浏览器中选择图像文件
# 系统会自动创建材质并应用UV
```

**在面之间复制材质：**
- `Alt + 左键`点击目标面：应用相同材质并保持无缝平铺
- `Alt + 右键`点击源面：从面中拾取纹理（跨对象生效）

**拉伸材质应用：**
- `Shift + Alt + 左键`：应用纹理并拉伸以适配目标面尺寸
- `Shift + Alt + 右键`：拾取要拉伸的纹理

**仅UV操作（不改变材质）：**
- `Ctrl + Alt + 左键`：应用UV但不改变材质
- `Ctrl + Alt + 右键`：拾取UV但不改变材质

### 交互式UV模式

**面吸附UV模式（`T`键）：**
```python
# 1. 选择一个面
# 2. 按T键进入模式
# 3. 将鼠标移至边缘附近 - 纹理底部会吸附到最近的边缘
# 4. WASD键：选择不同的纹理边缘
# 5. Q/E键：设置适配模式
# 6. 点击应用
```

**网格吸附UV模式：**
```python
# 1. 选择多个四边形面（仅单个岛）
# 2. 按T键
# 3. 吸附会应用于整个四边形网格
# 4. 控制方式与面吸附模式相同
```

**UV变换模式（`Shift + T`）：**
```python
# 1. 选择面
# 2. 按Shift+T
# 3. 将鼠标悬停在面上以设置原点（适用于多面选择）
# 4. 拖动手柄移动/调整UV大小，实时预览效果
# 5. 拖动调整手柄穿过对面以镜像纹理（负向缩放U/V）
```

### 材质面板设置

按`N`键打开侧边栏 → Anvil面板：

```python
# 手动UV调整
Scale U/V: 1.0  # 设为1.0时，pixels_per_meter控制纹理大小
Rotation: 0.0
Offset U/V: 0.0, 0.0

# 随机化偏移：点击Offset字段旁的刷新图标

# UV锁定开关
UV_Lock: True   # 材质随面调整而变形
UV_Lock: False  # 材质保持世界空间（挤出时不会拉伸）
```

**实用操作：**
- 重置缩放、旋转、偏移（面对齐投影）
- 将材质居中到面
- 适配材质到面尺寸

**材质属性：**
```python
# 将透明通道链接到着色器
# 调整粗糙度
# 启用顶点颜色
# 预乘alpha设置

# 修复Alpha渗色工具：编辑源图像，将透明像素设置为对应颜色
# 避免GLB导出时透明裁剪材质出现可见边缘
```

**默认材质设置：**
- 面板：Anvil（设置）- 按文件设置默认值
- 插件偏好设置 - 新.blend文件的全局默认值

### 材质清理

```python
# 在Anvil面板中
"清理未使用材质"按钮  # 删除孤立材质
```

## 热点映射系统

热点映射通过将面形状与纹理图集上的预定义区域匹配来分配UV坐标。热点UV永远不会镜像——始终是非翻转映射。

### 设置热点图

**在热点映射工作区中：**

```python
# 1. 在图像编辑器中打开纹理图集
# 2. 选择热点编辑工具（左侧边栏）
# 3. 按N键 → Anvil面板
# 4. 点击"标记为可热点化"将纹理标记为热点源
# 5. 在图像中添加线条以分割为热点区域
#    - 普通线条：点击并拖动
#    - 非网格线条：按住Ctrl + 拖动
# 6. 拖动线条调整热点大小
# 7. 使用[和]键进行像素吸附
```

**数据存储：**
- 默认存储在.blend文件中
- 可选导出为外部JSON以跨项目共享

### 热点方向类型

点击热点旁的方向按钮或热点本身的图标来切换：

```python
Any      # 适用于任何面，随机旋转
Upwards  # 仅适用于墙面（垂直），纹理顶部朝上 - 适用于砖块、壁板
Floor    # 仅适用于地面（朝上），随机旋转
Ceiling  # 仅适用于天花板（朝下），随机旋转
```

### 应用热点

**在3D视图的Anvil面板中：**

```python
"随机化热点"按钮  # 手动触发应用到选中面（若未选中或处于对象模式则应用到所有面）

"自动应用热点"开关  # 在几何编辑时自动应用（仅对移动的几何生效）

"固定"属性  # 选中面后开启可防止随机化

"选择热点"按钮  # 手动选择固定热点
```

### 组合面与接缝模式

**允许组合面：**
```python
# 启用（默认）：算法将相连面视为单个面
# - 适用于弯曲/弯折的面序列
# - 尝试找到可转换为矩形的面组
# - 根据法线和用户接缝分割面岛

# 禁用：每个面单独处理
```

**接缝模式：**
```python
"保留用户接缝"  # 清除自动接缝（对用户而言无变化）
"显示所有接缝"    # 显示算法识别的面岛，用于调试
"清除所有接缝"    # 删除所有接缝
```

**尺寸权重滑块：**
```python
# 默认（0.0）：选择最接近的宽高比匹配（保持像素比例）
# 增大值：优先选择匹配纹理像素密度的热点
# 避免小热点匹配宽高比时出现模糊纹理
```

**最佳实践：**
- 在图集中创建多种宽高比和尺寸的热点
- 确保对任何几何都能良好匹配
- 算法不会分割非矩形面岛（例如L形面保持独立）

## 几何工具

### 选择工具

**背面剔除覆盖：**
```python
# 当未处于X射线模式时，Anvil会覆盖选择逻辑，忽略被剔除的背面
# 支持：
# - 框选（单击、Shift+单击、Alt+单击）
# - 套索选择（单击变体）
# 
# 限制（Blender API）：
# - 不支持框选/套索拖动
# - 不支持圆形选择或微调工具
```

**顶点绘制模式：**
```python
# 若未开启面方向，会自动启用
# 正面方向颜色在当前主题中设为透明
# 显示无法穿透绘制的面
```

**绘制选择（`Ctrl + 左键`）：**
```python
# 类似圆形选择，但遵循背面剔除规则
# 按住并拖动可将交叉元素添加到选择中
```

**选择相连元素：**
```python
L                 # 悬停在元素上：选择所有相连元素
Ctrl+L            # 悬停在元素上：选择法线匹配的相连元素
                  # 重复按会逐步选择法线相似度更高的面
Ctrl+Shift+L      # 逐步回退基于法线的选择
Shift+L           # 将相连面岛添加到选择中
```

### 上下文感知焊接（`W`键）

```python
# 按W键执行上下文感知焊接
# 操作根据选择上下文而变化
# 下一次焊接操作会在Anvil面板中标记
# 具体行为请参考相关README章节
```

### 立方体切割模式（编辑模式下按`C`键）

```python
# 进入立方体切割模式
1. 在编辑模式下按C键
2. 点击面开始绘制
3. 移动鼠标并点击定义矩形面
4. 第三次移动鼠标定义深度
5. 第三次点击完成切割

# 行为：
# - 仅影响选中面（若未选中则影响所有面）
# - 在正交视图中绘制矩形会创建无限切割
```

### 基础导航

```python
右键鼠标按钮  # 相机导航
Tab                # 切换对象/编辑模式
G                  # 移动选择
E                  # 挤出选择
Alt+Click          # 选择循环边
B                  # 添加立方体（在对象和编辑模式下均生效）
L                  # 选择相连面（在多立方体编辑模式选择中很有用）
```

## 配置

### 插件偏好设置

**访问路径：** 编辑 → 偏好设置 → 插件 → Anvil Level Design

```python
# 工作区创建
"创建关卡设计工作区"
"创建热点映射工作区"

# 快捷键重映射
# 所有Anvil快捷键集中在此处，方便操作

# 默认材质设置（新.blend文件的全局设置）
Default_Pixels_Per_Meter: 100.0
Default_Roughness: 0.5
Default_Transparency_Mode: "OPAQUE"
```

### 按文件设置

**Anvil（设置）面板：**
```python
# 当前文件中新纹理的默认材质属性
Pixels_Per_Meter: 100.0
Roughness: 0.5
Enable_Vertex_Colors: False
Transparency_Settings: {...}
```

## 代码示例

### Python API - 材质应用

```python
import bpy

# 通过bpy.ops访问Anvil算子
def apply_texture_to_selected_face(image_path):
    """将文件中的纹理应用到选中面"""
    # 加载图像
    img = bpy.data.images.load(image_path)
    
    # Anvil会处理材质创建和UV应用
    # 通常通过UI操作，但也可通过脚本实现：
    obj = bpy.context.active_object
    if obj and obj.mode == 'EDIT':
        # 选择面并通过Anvil算子应用
        # bpy.ops.anvil.apply_material_from_image()
        pass

def get_anvil_material_settings():
    """访问Anvil材质设置"""
    prefs = bpy.context.preferences.addons['anvil-level-design'].preferences
    
    settings = {
        'pixels_per_meter': prefs.default_pixels_per_meter,
        'roughness': prefs.default_roughness,
    }
    return settings

# UV操作
def set_uv_scale_rotation(scale_u=1.0, scale_v=1.0, rotation=0.0):
    """通过Anvil属性设置UV变换"""
    obj = bpy.context.active_object
    if obj and obj.mode == 'EDIT':
        # 访问Anvil UV属性
        # 属性存储在对象/网格数据中
        # bpy.ops.anvil.set_uv_transform(scale_u=scale_u, scale_v=scale_v, rotation=rotation)
        pass
```

### Python API - 热点管理

```python
import bpy
import json

def create_hotspot_data(image_name, hotspots):
    """定义热点数据结构"""
    hotspot_data = {
        'image': image_name,
        'hotspots': []
    }
    
    for hs in hotspots:
        hotspot_data['hotspots'].append({
            'bounds': {
                'x_min': hs['x_min'],
                'y_min': hs['y_min'],
                'x_max': hs['x_max'],
                'y_max': hs['y_max']
            },
            'orientation': hs.get('orientation', 'Any'),  # Any, Upwards, Floor, Ceiling
            'name': hs.get('name', '')
        })
    
    return hotspot_data

def export_hotspot_json(data, filepath):
    """将热点数据导出到外部JSON"""
    with open(filepath, 'w') as f:
        json.dump(data, f, indent=2)

def apply_random_hotspots():
    """触发选中面的热点随机化"""
    # bpy.ops.anvil.randomise_hotspots()
    pass

# 示例热点定义
example_hotspots = [
    {
        'x_min': 0, 'y_min': 0, 'x_max': 512, 'y_max': 512,
        'orientation': 'Upwards',
        'name': 'brick_wall_01'
    },
    {
        'x_min': 512, 'y_min': 0, 'x_max': 1024, 'y_max': 256,
        'orientation': 'Floor',
        'name': 'floor_tile_01'
    }
]
```

### Python API - 几何操作

```python
import bpy

def context_aware_weld():
    """执行上下文感知焊接"""
    # bpy.ops.anvil.context_weld()
    pass

def select_connected_by_normal():
    """选择法线匹配的相连面"""
    # bpy.ops.anvil.select_connected_normal()
    pass

def paint_select_setup():
    """设置绘制选择模式"""
    obj = bpy.context.active_object
    if obj and obj.type == 'MESH':
        # 确保正确的选择模式
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.context.tool_settings.mesh_select_mode = (False, False, True)  # 面模式
```

## 常见模式

### 关卡设计工作流

```python
# 1. 初始几何块搭建
# - 切换到关卡设计工作区
# - 进入编辑模式（Tab）
# - 按B键添加立方体
# - 使用G（移动）和E（挤出）塑造关卡
# - Alt+Click选择循环边
# - L选择相连几何

# 2. 纹理应用
# - 选择面
# - 在文件浏览器中选择图像（自动应用材质）
# - Alt+左键点击其他面复制纹理
# - 按T键进入面吸附UV模式对齐纹理边缘
# - 使用Shift+T进入UV变换模式进行手动微调

# 3. 热点细节处理
# - 创建纹理图集
# - 切换到热点映射工作区
# - 使用热点编辑工具定义区域
# - 设置方向类型（墙面用Upwards，地面/天花板用Floor/Ceiling）
# - 返回关卡设计工作区，启用自动应用热点
# - 创建细节几何 - 热点会自动应用

# 4. 优化调整
# - 使用Ctrl+L按法线选择
# - 为面组应用特定材质
# - 将重要热点标记为固定
# - 必要时手动选择热点

# 5. 导出
# - 使用Anvil（导出）面板进行GLB导出
# - 材质已配置为适配游戏引擎
```

### 纹理图集设置

```python
# 热点图集最佳实践：
# 1. 包含多种宽高比
#    - 正方形（1:1）
#    - 宽矩形（4:1, 8:1）
#    - 高矩形（1:4, 1:8）
#    - 常见比例（2:1, 3:2）

# 2. 包含多种尺寸变体
#    - 大尺寸（1024x1024）用于大表面
#    - 中等尺寸（512x512, 512x256）用于通用场景
#    - 小尺寸（256x256, 128x256）用于细节
#    - 避免小热点导致纹理模糊

# 3. 按方向分组
#    - 墙面纹理组（Upwards方向）
#    - 地面纹理组（Floor方向）
#    - 天花板纹理组（Ceiling方向）
#    - 通用细节（Any方向）

# 4. 为热点命名清晰
#    - 手动选择固定热点时更方便
```

### 材质优化

```python
# 材质管理策略：

# 1. 每种材质类型使用单个图集
#    - 一个图集用于石材/砖块
#    - 一个图集用于金属
#    - 一个图集用于木材
#    - 减少材质数量

# 2. 利用Anvil的重复材质预防功能
#    - 插件会自动合并重复材质
#    - 定期运行“清理未使用材质”

# 3. 尽早配置默认值
#    - 为项目比例设置Pixels_Per_Meter
#    - 为每种材质类型设置默认粗糙度
#    - 纹理制作前配置透明设置

# 4. 导出前修复Alpha渗色
#    - 对透明纹理使用修复Alpha渗色工具
#    - 或在材质设置中启用预乘alpha
#    - 在目标引擎中测试
```

## 故障排除

### 纹理无法正确应用

**问题：** Alt+左键无法应用纹理
```python
# 检查：
1. 是否处于关卡设计工作区？
2. 面是否真的被选中（橙色高亮）？
3. 对象是否处于编辑模式？
4. 尝试先按Alt+右键拾取纹理

# 如果纹理已应用但显示异常：
5. 检查Anvil面板中的UV锁定设置
6. 验证Scale U/V值（通常应为1.0或接近1.0）
7. 检查纹理是否被镜像（负向Scale U/V）- 使用面板重置
```

**问题：** 挤出时纹理拉伸
```python
# UV锁定处于开启状态
# 解决方案：在Anvil面板中关闭UV锁定，实现世界空间材质行为
# UV锁定开启：材质随几何变形
# UV锁定关闭：材质保持世界空间（关卡设计首选）
```

### 热点问题

**问题：** 热点无法应用
```python
# 检查：
1. 图像是否在热点映射工作区中被标记为“可热点化”？
2. 热点区域是否正确定义（图像编辑器中可见线条）？
3. 面方向是否与热点方向类型匹配？
   - 使用Shift+Z切换线框模式查看面法线
4. “允许组合面”是否导致意外的面岛分组？
   - 尝试禁用该选项，单独处理每个面
```

**问题：** 选中错误的热点
```python
# 问题：选择了小热点，纹理模糊
# 解决方案：调整尺寸权重滑块
#   - 增大值优先选择匹配像素密度的热点
#   - 减小值仅优先匹配宽高比

# 问题：每次编辑时热点都会随机化
# 解决方案：将合适的热点标记为固定
#   1. 选择带有合适热点的面
#   2. 在Anvil面板中启用“固定”属性
```

**问题：** 热点产生接缝
```python
# 检查接缝模式设置：
"保留用户接缝"  # 默认，清除自动接缝
"显示所有接缝"    # 显示算法识别的面岛
"清除所有接缝"    # 终极解决方案

# 如果面岛未按预期形成：
1. 切换到“显示所有接缝”模式
2. 查看算法如何分割几何
3. 在需要的地方手动添加接缝
4. 或禁用“允许组合面”以实现逐面控制
```

### 选择问题

**问题：** 选择穿透几何
```python
# 背面剔除限制：
1. 关闭X射线模式（Alt+Z），背面剔除才会生效
2. 使用单击选择，而非拖动选择
3. 避免圆形选择 - 使用绘制选择（Ctrl+左键）替代
4. 框选和套索仅对单击操作生效，拖动无效

# 顶点绘制：
# 会自动启用面方向以显示可绘制的面
```

**问题：** 无法选择特定面
```python
# 使用选择模式：
Ctrl+L  # 按法线选择 - 逐步选择法线相似的面
L       # 选择所有相连元素
Shift+L # 添加面岛到选择
Alt+Click  # 选择循环边

# 绘制选择实现精准选择：
Ctrl+左键  # 按住并拖动，遵循背面剔除规则
```

### 性能问题

**问题：** 材质应用缓慢
```python
# 材质过多：
1. 定期在Anvil面板中运行“清理未使用材质”
2. 将相似材质合并到图集中
3. 在大纲视图中检查材质数量

# 面数过多：
1. 对细节几何使用简化修改器
2. 合并不必要的面
3. 对重复元素使用实例化
```

**问题：** 热点随机化缓慢
```python
# 纹理图集过大：
1. 尽可能降低图集分辨率
2. 减少热点区域数量
3. 大量建模时禁用“自动应用热点”
4. 准备好后手动触发“随机化热点”
```

### 导出问题

**问题：** GLB中透明材质显示异常
```python
# 出现Alpha渗色：
1. 对源纹理使用“修复Alpha渗色”工具
2. 或在材质设置中启用预乘alpha
3. 检查材质混合模式（应适配目标引擎）

# 材质缺失：
1. 验证所有材质在Blender中已分配
2. 检查Anvil（导出）面板设置
3. 先使用简单材质设置测试
```

### 工作区丢失

**问题：** 关卡设计或热点映射工作区丢失
```python
# 解决方案：
1. 编辑 → 偏好设置 → 插件 → Anvil Level Design
2. 点击“创建关卡设计工作区”
3. 点击“创建热点映射工作区”
4. 工作区会出现在Blender窗口顶部
```

### 快捷键无效

**问题：** 快捷键无法触发Anvil功能
```python
# 检查：
1. 是否处于正确的工作区？（关卡设计或热点映射）
2. 是否处于正确的模式？（对象模式 vs 编辑模式）
3. 在插件偏好设置中检查快捷键冲突
4. 在偏好设置中重新映射冲突的快捷键
5. 验证插件是否在偏好设置中启用
```

## 高级配置

### 自定义快捷键设置

```python
# 访问路径：编辑 → 偏好设置 → 插件 → Anvil Level Design
# 所有Anvil快捷键集中在此处

# 常见重映射：
T         # 面吸附UV模式（可能与工具冲突）
Shift+T   # UV变换模式
C         # 立方体切割（与圆形选择冲突）
W         # 上下文感知焊接
B         # 添加立方体（在某些场景下与框选冲突）

# 建议重映射到：
# - 功能键（F1-F12）
# - Alt+键组合
# - 数字小键盘（若未用于视图控制）
```

### 性能调优

```python
# 针对大型关卡：

# 1. 材质管理
Max_Materials_Before_Warning: 50  # 降低值以提前提醒清理

# 2. 热点设置
Size_Weight: 0.3  # 平衡质量与速度
Allow_Combined_Faces: False  # 处理更快，但智能分组减少

# 3. 选择设置
禁用绘制选择（若不需要）  # 小幅提升性能

# 4. UV设置
UV_Lock: False  # 默认关闭，挤出时性能更好
```

### 与版本控制集成

```python
# 热点数据存储在外部JSON中：

# 在热点映射工作区中：
1. 在纹理图集上定义热点
2. 将热点数据导出为JSON
3. 将JSON提交到版本控制
4. 团队成员加载相同的JSON
5. 团队间热点行为保持一致

# .blend文件注意事项：
# - 若使用版本控制，存储在Git LFS中
# - 热点数据默认嵌入文件
# - 可选导出为外部JSON用于共享
```

## 资源

- **Discord：** https://discord.gg/hHFZbDzR57
- **GitHub：** https://github.com/alexjhetherington/anvil-level-design
- **示例文件：** 查看仓库中的`examples/hotspot_tutorial.blend`

## 快速参考卡

```python
# 材质应用
Alt+左键         # 将材质复制到面（无缝平铺）
Alt+右键        # 从面拾取材质
Shift+Alt+左键   # 拉伸适配应用
Ctrl+Alt+左键    # 仅应用UV（不改变材质）

# UV模式
T                      # 面吸附UV模式
Shift+T                # UV变换模式
WASD（UV模式下）      # 选择纹理边缘
Q/E（UV模式下）       # 适配模式

# 选择
L                      # 选择相连元素
Ctrl+L                 # 按法线选择
Shift+L                # 添加面岛
Ctrl+左键        # 绘制选择

# 几何
B                      # 添加立方体
C（编辑模式）          # 立方体切割
E                      # 挤出
G                      # 移动
W                      # 上下文感知焊接

# 导航
右键鼠标            # 相机控制
Tab                    # 对象/编辑模式切换
Alt+Click              # 选择循环边
[ ]                    # 像素吸附（热点工作区中）
```
