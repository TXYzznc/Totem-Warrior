## Why

共享玩家角色 M02 当前依赖逐帧 Sprite 动画。它已经可用且必须保留，但每个姿势都需要单独绘制与维护，无法稳定地让纹身等局部视觉元素随角色部位运动。当前动作需求不复杂，适合先建立一条纯骨骼、可复用且不破坏旧资源的角色动画管线。

## What Changes

- 新增 M02 的独立纯骨骼预览管线：由分层部件 Sprite、Transform 骨骼层级和骨骼动画片段组成，不播放或覆盖现有逐帧 Sprite。
- 以四视图为基准制作四个方向的分层角色部件；每个部件归属稳定的骨骼，关节处保留必要的遮挡与补画。
- 建立独立的骨骼 Animator Controller、预览 Prefab、导入/验证工具和资源目录；现有 `ActorCommonM02.controller`、168 张逐帧资源、28 个 Clip 及现有参与者 Prefab 保持不变。
- 为 Head、Torso、LeftArm、RightArm、LeftLeg、RightLeg 定义骨骼挂点和遮罩边界契约，供后续纹身视觉层使用；本轮不接入玩家纹身编辑 UI。
- 修正旧逐帧导入工具使用的控制器路径，使其仍生成标准 `ActorCommonM02.controller`，不再重新创建 `ActorCommonM02Rework.controller`。

## Capabilities

### New Capabilities

- `skeletal-player-animation`: M02 共享玩家角色的纯 Transform 骨骼动画、四方向分层角色资源、独立预览与验证流程。

### Modified Capabilities

- `gameplay-character-art`: 角色美术资源契约增加“逐帧资源保留”和“骨骼资源与旧动画并行”的要求。
- `tattoo`: 纹身视觉层可以按固定部位骨骼挂点绑定，但不改变纹身的游戏效果规则。

## Impact

- 新增 `Assets/Game/Sprite/Actors/ActorCommonM02Skeletal/`、`Assets/Game/Animation/Actors/ActorCommonM02Skeletal/`、骨骼预览 Prefab 与编辑器验证工具。
- 新增分层原画源文件，置于本 change 的 `art/raw/characters/actor_common_m02_skeletal/`；原始 M02 四视图是美术一致性的参考，不会被覆盖。
- `dynamic-tattoo-visuals` 中尚未接入的逐帧映射原型不作为本 change 的运行时方案；后续将以本骨骼部位挂点为视觉接入点。
