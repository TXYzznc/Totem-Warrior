# 首轮 VFX Shader 参数契约

## 程序化形状

Shader：`Totem/FirstPlayable/VFX/ProceduralShape`

- `_Shape`：0 方框、1 菱形框、2 楔形、3 分叉电弧。
- `_Progress`：0–1 的显现进度；1 表示完整显示，可由粒子生命周期或脚本驱动。
- `_EdgeWidth`：结构线宽；远景效果应适当增大，防止细线闪烁。
- `_RevealWidth`：显现前沿宽度，只强化移动边界，不截掉已经显现的主体。
- `_NoiseScale/_NoiseSpeed/_NoiseAmount`：程序化扰动，雷系高、冰系低、火系中等。
- `_PulseSpeed/_PulseAmount`：亮度呼吸；命中瞬间可以短时增大，常驻效果应克制。
- `_Intensity`：HDR 亮度；最终值必须在测试场景结合 Bloom 调整。

## 程序化拖尾

Shader：`Totem/FirstPlayable/VFX/ProceduralTrail`

- 使用矩形分段而非圆润带状轮廓。
- `_Segments` 控制断续块数量，`_Gap` 控制块间空隙，`_Taper` 控制尾端收束。
- `_NoiseTex` 默认使用共享 `T_FP_VFX_Noise_256`；不要为单个元素另做专属噪声贴图。
- 火系使用较快流速与中等间隙，冰系使用低流速与清晰分段，雷系使用高流速与高对比。

## UI 焦点

Shader：`Totem/FirstPlayable/UI/Focus`

- 用于按钮或选项的切角矩形焦点框，不替代原按钮底图。
- 必须保留 UGUI Stencil、RectMask2D 裁切和 Color Tint 兼容。
- 扫描高光仅作为聚焦辅助，不能成为持续大面积闪烁。

## 组合策略

ParticleSystem 只负责生命周期、方向、尺寸和数量；Shader 负责轮廓、显现、扰动和颜色。单个效果优先采用 1–3 个粒子系统、1 个共享 Shader、0–1 张共享纹理，禁止依赖复杂序列帧作为基础方案。
