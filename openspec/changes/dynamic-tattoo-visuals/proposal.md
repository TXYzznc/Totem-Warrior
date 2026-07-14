## Why

当前纹身系统已经保存玩家的部位、颜色和图案选择，但 M02 角色使用逐帧 Sprite 动画，纹身没有任何可见呈现。简单叠加独立 Sprite 会在四向移动、翻滚和受击时脱离身体；需要一个能够随每一帧角色姿势贴合、并严格裁切在指定裸露区域内的运行时视觉层。

本轮先交付稳定的固定映射，避免阻塞已完成的角色动画。与此同时，在纹身视觉数据模型中保留区域内位移与缩放接口，后续开放编辑 UI 时不必替换渲染或动画绑定方案。

## What Changes

- 新增面向 `ActorCommonM02` 的逐帧纹身映射资源、Sprite Shader 与运行时呈现组件；它依据玩家当前已装备的纹身显示图案和颜色，并在角色换帧时同步更新映射。
- 为 Head、Torso、LeftArm、RightArm、LeftLeg、RightLeg 建立各自固定的可纹身皮肤区域；图案超出对应区域时必须被裁切。
- 扩展纹身视觉描述，保留 `offset` 与 `scale` 字段及其默认值，但本轮不提供玩家编辑入口，渲染使用固定默认位置与尺寸。
- 将 M02 导入工具、Prefab 接入和验证更新为标准 `ActorCommonM02.controller` 路径，避免后续美术重新导入重新创建旧的 `Rework` 控制器名称。
- 不实现纹身自由移动/缩放 UI、旋转、跨部位贴花、VFX 或为 SmartAI/LightAI 生成独立纹身外观。

## Capabilities

### New Capabilities

- `dynamic-tattoo-visuals`: 基于逐帧角色 Sprite 的玩家纹身视觉映射、区域裁切、颜色/图案合成和未来位置/缩放接口。

### Modified Capabilities

- `gameplay-character-art`: M02 角色动画资源契约增加逐帧纹身映射资源与标准控制器路径要求。
- `tattoo`: 已装备纹身的运行时状态增加仅供视觉层使用的默认位置与缩放信息，且不改变现有战斗效果规则。

## Impact

- 运行时代码：`Assets/Game/Scripts/Runtime/` 下新增或扩展纹身视觉组件，读取 `TotemTattooService` 和 Player 的当前状态。
- 渲染资源：新增 URP 兼容的 Sprite Shader、材质、纹身图案源和 M02 每帧映射资源；新资源通过 `TotemAssetService` 的既有 Prefab 加载流程到达运行时。
- 编辑器与资源：`Assets/Game/Editor/ActorCommonM02ArtImportTool.cs` 需要生成/验证映射绑定，并改用标准控制器路径。
- 验证：新增 EditMode/PlayMode 覆盖，确认六部位裁切、帧切换同步、默认 offset/scale 和无纹身降级行为。
