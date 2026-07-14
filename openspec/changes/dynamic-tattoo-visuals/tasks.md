## 1. 资源与映射管线

- [ ] 1.1 为 M02 的 168 张角色帧定义六部位安全皮肤区域，并实现同尺寸 TattooMap 的生成与可视审核预览。
- [ ] 1.2 将 TattooMap、图案源和 M02 Sprite→映射索引导入 `Assets/Game`，并修正 M02 导入工具的标准控制器路径。
- [ ] 1.3 扩展 M02 导入验证，确保每张 Sprite 都有匹配尺寸的 TattooMap，且不再创建 `ActorCommonM02Rework.controller`。

## 2. 运行时纹身呈现

- [ ] 2.1 创建 URP 兼容的角色纹身合成 Shader 和材质，按部位裁切并从 PatternId/ColorId 合成图案。
- [ ] 2.2 创建无分配的 `TotemTattooVisualPresenter`，缓存 Sprite→TattooMap 映射，并仅在动画帧或装备摘要变化时更新 PropertyBlock。
- [ ] 2.3 为纹身视觉描述实现默认 `offset=(0.5,0.5)`、`scale=1.0` 接口，保持现有玩法状态与战斗效果不变。
- [ ] 2.4 将呈现组件接入玩家运行时视觉对象；SmartAI 与 LightAI 保持无可见纹身。

## 3. 验证与文档

- [ ] 3.1 添加 EditMode 测试，覆盖六部位的裁切、图案/颜色解析、默认变换和无装备降级。
- [ ] 3.2 添加 PlayMode 或 Editor 验证，覆盖 M02 的帧同步、四向关键帧、滚动帧和缺失映射的安全降级。
- [ ] 3.3 执行 Unity 编译、M02 导入验证和 GF_X 诊断；记录结果并更新本变更文档。
