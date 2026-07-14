## Context

`ActorCommonM02` 由 168 张 512×512、透明背景的独帧 Sprite 与一个 `SpriteRenderer` 动画控制器组成；没有骨骼、网格或可供贴花投射的角色 UV。`TotemTattooService` 已有六个逻辑部位、七种颜色和八种图案，但尚无视觉消费者。角色在正面、侧面、背面和翻滚帧中裸露皮肤的可用区域不同，独立挂点 Sprite 会在换帧时漂移。

本变更只为玩家的当前装备状态提供视觉呈现。由于 Player、SmartAI、LightAI 共享 M02 Prefab 资源，呈现组件可以存在于通用 Prefab 上，但只有解析到 Player 的纹身数据时才启用。

## Goals / Non-Goals

**Goals:**

- 将每个 M02 动画帧的像素映射到 Head、Torso、LeftArm、RightArm、LeftLeg、RightLeg 六个局部纹身区域。
- 依据 `TotemTattooService` 当前装备的 `PartId`、`ColorId`、`PatternId` 合成视觉，并裁切掉区域外、衣物和透明像素。
- 保留以归一化局部坐标表示的 `offset` 与 `scale` 接口，默认 `(0.5, 0.5)` 与 `1.0`；本轮不暴露编辑 UI。
- 帧切换仅更新 `MaterialPropertyBlock`，不在 `Update` 中创建数组、纹理或材质实例。

**Non-Goals:**

- 不提供拖拽、缩放、旋转、镜像、跨部位或多人可见纹身编辑。
- 不生成 VFX、法线、3D Decal、网络同步协议或 SmartAI/LightAI 的独立外观。
- 不把纹身烘焙进角色原始 Sprite，也不为颜色×图案组合复制角色动画帧。

## Decisions

### Decision: 使用逐帧 TattooMap，而不是子 Sprite 挂点

每张角色帧对应一张同尺寸 `TattooMap`。其像素通道编码 `R/G=所属部位的局部 UV`、`B=部位编号(1–6)`、`A=可贴花遮罩`。Shader 在基础角色纹理与当前 `TattooMap` 的相同 Sprite UV 上采样，再以部位编号选择已装备纹身的图案和颜色。角色帧改变时，呈现组件以 Sprite 名称查表并设置新映射纹理。

这使贴花自然随不同方向、行走、受击、翻滚、冲刺和死亡帧的肢体姿势移动；遮罩为零的位置绝不会出现纹身。替代方案“六个子 SpriteRenderer + 锚点”不能处理滚动中的旋转和遮挡；“为每种组合预绘角色帧”会产生 6×7×8×168 的资源爆炸，均不采用。

### Decision: 区域固定；偏移和缩放只作为渲染描述字段

每个部位的 TattooMap UV 覆盖其已批准的裸露皮肤区域。图案采样采用 `((localUv - offset) / scale) + 0.5`；采样区外、映射遮罩外和未装备部位均输出基础角色颜色。当前数据写入默认值且不显示控制 UI，从而保证以后只需改变参数来源而无需替换 Shader 或重做映射。

旋转不在本轮预留为公开功能：四肢的非刚性移动已由映射解决，而旋转会带来图案边缘与 UI 交互额外约束。未来若需要，可向同一变换加角度字段。

### Decision: 图案使用中性 Alpha 源，颜色在 Shader 中着色

图案资产为可透明采样的中性线稿/遮罩，颜色来自现有 `ColorId`。这避免为七种颜色重复存储图案。首版可使用一个图案 Atlas 或独立纹理槽；实现中优先采用固定大小的纹理数组/Atlas，避免每像素循环所有图案或创建 shader variants。

### Decision: 映射资源由编辑器工具生成并进行人工可视审核

编辑器导入工具为所有角色帧生成或校验映射资源、基础 Sprite→TattooMap 索引和接触表预览。自动初稿只在角色 Alpha 与安全的、按动作/方向定义的皮肤区相交处写入映射；每个映射仍需要通过可视化叠图审核。未找到映射的帧必须安全降级为“不显示纹身”，并在验证中报错，禁止出现漂移的错误贴花。

### Decision: 玩家视觉由运行时服务更新，Prefab 不保存运行时材质实例

`TotemTattooVisualPresenter` 读取 `TotemTattooService.CaptureSnapshot(Player)` 或等价的装备访问接口，检测装备摘要或当前 Sprite 的变化。它复用一个 `MaterialPropertyBlock`，按需更新图案、颜色、映射纹理与默认 transform；不修改共享材质资产，也不分配新对象。缺少 Shader、材质、图案或映射时恢复标准 Sprite 材质显示。

## Risks / Trade-offs

- [自动区域初稿误把衣物算入皮肤] → 使用显式每帧遮罩、叠图审核和“无映射即不显示”的安全降级；不依赖运行时肤色猜测。
- [168 张映射图带来资源量] → 映射使用单通道/压缩友好编码、与基础帧同名索引；它替代组合帧爆炸，且不会因颜色/图案数量增长。
- [SpriteRenderer 的材质属性与帧动画不同步] → 在 `LateUpdate` 仅比较缓存的 Sprite 引用，并在换帧后写入同一个 PropertyBlock；增加方向、滚动帧的 PlayMode 验证。
- [未装备纹身或导入资源缺失] → 退回基础 Sprite，无异常、无新材质实例，并输出可定位的验证信息。
- [M02 导入工具复建 Rework 控制器] → 将常量改为标准 `ActorCommonM02.controller`，并把路径作为导入验证的一部分。

## Migration Plan

1. 先增加 Shader、材质、映射索引和 Presenter，但保持没有纹身时的画面与现有 Prefab 一致。
2. 为 M02 的全部 168 帧生成、审核和导入 TattooMap；缺图时验证失败而不是静默错位。
3. 将 Presenter 附加到 Player 的运行时视觉对象，并用现有纹身服务驱动；SmartAI/LightAI 保持无可见纹身。
4. 通过 EditMode/PlayMode 与 Unity 导入验证后启用。回滚时移除 Presenter/材质引用即可，角色原始 Sprite、Animator 和玩法纹身数据不受影响。

## Open Questions

- 无。玩家范围、固定默认位置、区域裁切、仅预留位移与缩放接口均已由用户确认。
