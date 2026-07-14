## Context

`ActorCommonM02` 是 Player、SmartAI 与 LightAI 共用的角色视觉。当前交付包含 168 张逐帧 Sprite、28 个 Clip 和标准 `ActorCommonM02.controller`。这些资产已经接入现有行为参数，且用户明确要求保留。与此同时，M02 的平面逐帧图无法为纹身等局部视觉提供稳定的部位跟随关系。

项目已通过 2D Feature 安装 `com.unity.2d.animation` 9.2.0，但现有 Sprite 没有绑定姿势和权重数据，不能直接套用 SpriteSkin。角色当前没有高复杂度的形变需求。

## Goals / Non-Goals

**Goals:**

- 建立与逐帧动画完全隔离的 M02 纯骨骼预览管线。
- 用四方向分层部件和稳定的 Transform 骨骼层级生成连续姿势动画。
- 在六个纹身部位提供可查找的骨骼挂点与局部裁切边界。
- 保持既有游戏行为参数和旧动画控制器的兼容边界，直到骨骼版验收后才允许运行时切换。

**Non-Goals:**

- 本轮不删除、移动、重导入或替换任一既有逐帧资源、Clip、控制器或参与者 Prefab。
- 本轮不实现网格蒙皮、权重刷制、IK、物理摆动、纹身编辑 UI、VFX 或运行时切换。
- 本轮不要求 SmartAI、LightAI 使用骨骼预览 Prefab；它们继续使用已验证的共享逐帧控制器。

## Decisions

### 1. V1 使用 Transform cutout 骨骼，而非立即使用 SpriteSkin 网格变形

骨骼层级以 `root/pelvis/torso/chest/neck/head` 为轴，四肢由上、下肢与手脚节点组成。部件 Sprite 作为对应骨骼的子对象，动作仅动画化 Transform 的平移、旋转、缩放与排序。

选择原因：现有图像是扁平逐帧图，不带 SpriteSkin 所需的 bind pose 与 bone weight。Transform cutout 是完整的纯骨骼动画，能满足当前动作复杂度，并且允许将来在同一骨骼命名契约上升级为网格蒙皮。

替代方案：直接用 SpriteSkin 会要求先重新制作权重化 Mesh；继续使用逐帧动画则不能解决部位附着问题。两者均不作为本轮的实现路径。

### 2. 新资产独立且只以预览 Prefab 进入场景

新资源使用 `ActorCommonM02Skeletal` 命名空间，分别位于 Sprite、Animation 和 Prefab 目录；控制器独立于 `ActorCommonM02.controller`。初始只创建 `PlayerSkeletalPreview`，不改 Player、SmartAI 或 LightAI 的生产 Prefab 引用。

### 3. 四方向使用分层部件集，不通过旋转同一张立绘伪造方向

前、后、左、右各自有一套透明部件原画。每套至少包含头部、躯干皮肤、服装覆盖、左右上/下臂、双手、骨盆/下装、左右上/下腿与双脚；关节交界必须保留可重叠的补画区。原始文件保存在 change 的 art/raw 目录，导入版本置于 Assets。

### 4. 纹身只依赖部位挂点和局部遮罩接口

骨骼预览根节点提供 `Head`、`Torso`、`LeftArm`、`RightArm`、`LeftLeg`、`RightLeg` 六个锚点。每个锚点包含一个局部矩形边界；后续纹身层可在该边界内使用 offset 与 scale，超出部分由局部遮罩裁切。本轮不将此前逐帧纹身原型接入角色。

### 5. 动画行为沿用当前参数名称，但新控制器从基础动作开始

骨骼控制器保留 `Direction`、`IsMoving`、`AttackTrigger`、`HitTrigger`、`DodgeTrigger`、`IsSprinting`、`Die` 和 `Dead` 参数。V1 先提供可验证的 Idle/Walk，随后按同一骨骼补充低幅度 Active、Hit、Roll、Sprint 和 Death；它们不使用逐帧 Sprite。

## Risks / Trade-offs

- [Cutout 关节露缝或穿插] → 分层原画预留关节补画区，以前后排序和局部遮罩控制重叠；复杂折叠姿势在未来升级为网格蒙皮。
- [新旧控制器混用] → 目录、名字、预览 Prefab 与验证工具全部使用 `Skeletal` 后缀；不改现有参与者 Prefab。
- [方向资源风格漂移] → 所有拆件以已批准的 M02 四视图为唯一外观参考，并在导入前逐方向核对比例与裸露皮肤区。
- [纹身原型与新管线重复] → 逐帧映射原型保持未接入状态；骨骼管线验收后再将其替换为挂点式实现。

## Migration Plan

1. 创建独立的骨骼原画、导入设置、骨骼定义和预览 Prefab。
2. 验证预览 Prefab 的四方向、骨骼层级、部件排序、基础 Idle/Walk 和六个纹身锚点。
3. 仅在用户确认视觉与动作满足要求后，新增可回退的运行时选择；旧逐帧 M02 继续保留为默认。
4. 若预览不通过，删除或停用仅 `Skeletal` 目录下的新引用即可，旧管线不受影响。

## Open Questions

- V1 骨骼预览的默认朝向是否使用 Down（正面）作为默认；本 change 采用 Down，待视觉验收后可调整。
- 未来网格蒙皮是否需要由外部 DCC 工具制作 PSD/骨骼权重；不阻塞本轮 Transform cutout 管线。
