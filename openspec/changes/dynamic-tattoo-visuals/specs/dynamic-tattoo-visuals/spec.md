## ADDED Requirements

### Requirement: 玩家纹身必须随 M02 动画帧贴合
系统 SHALL 为 `ActorCommonM02` 的每个可播放角色 Sprite 提供同尺寸的 TattooMap，并在 `SpriteRenderer` 切换 Sprite 时同步切换映射。TattooMap MUST 编码六个纹身部位的局部采样坐标与可见遮罩。

#### Scenario: 行走帧切换保持贴合
- **WHEN** 玩家从 `idle_down` 切换到任一 `walk_down` 帧
- **THEN** 已装备纹身 SHALL 使用该行走帧对应的 TattooMap，而不得保持 Idle 帧的映射

#### Scenario: 映射缺失安全降级
- **WHEN** 当前角色 Sprite 没有对应 TattooMap
- **THEN** 系统 SHALL 仅渲染基础角色 Sprite，且验证 SHALL 报告缺失映射

### Requirement: 纹身必须按部位区域裁切并显示颜色与图案
系统 SHALL 仅在 TattooMap 指定的部位区域内绘制该部位已装备的纹身。图案 MUST 以 `PatternId` 选择，颜色 MUST 以 `ColorId` 选择；未装备部位、衣物、透明像素和区域外像素不得显示纹身。

#### Scenario: 右臂纹身不会泄漏到背心
- **WHEN** 玩家装备 RightArm 纹身并显示任意正面 M02 动画帧
- **THEN** 纹身像素 SHALL 只出现在该帧 RightArm 的 TattooMap 遮罩内，背心和躯干遮罩外像素保持基础颜色

#### Scenario: 多部位纹身独立合成
- **WHEN** 玩家同时装备 Torso、LeftLeg 和 RightLeg 纹身
- **THEN** 每个部位 SHALL 显示其自身的图案与颜色，且不得覆盖另一个部位的映射区域

### Requirement: 纹身视觉必须预留区域内位移与缩放接口
每个可见纹身描述 SHALL 包含归一化 `offset` 和正数 `scale`。本轮运行时 MUST 使用固定默认值 `offset=(0.5,0.5)` 与 `scale=1.0`，且 MUST NOT 提供玩家编辑 UI。

#### Scenario: 默认变换可重复
- **WHEN** 玩家装备任意有效纹身并重新进入运行时
- **THEN** 视觉层 SHALL 使用固定默认 offset 与 scale，且不得因为缺少编辑数据而改变位置或尺寸

### Requirement: 纹身呈现不得在逐帧更新中产生托管分配
纹身呈现 SHALL 复用材质属性块和缓存数据；在角色未换帧且装备摘要未改变时，逐帧检查 MUST NOT 创建材质、纹理、数组或字符串。

#### Scenario: 静止玩家的呈现更新
- **WHEN** 玩家保持同一 Sprite 且装备状态未改变超过一帧
- **THEN** 呈现组件 SHALL 复用上帧的渲染属性，不得重新创建运行时资源
