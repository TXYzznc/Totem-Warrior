# Unity 导入计划

## 原则

编辑器空闲前不把文件复制到 `Assets/`。后续导入应一次性完成，并立即在 `Assets/Game/Scene/ArtResourceTest.unity` 中检查，确认后才允许正式流程引用。

## 建议目标

| 离线资源 | Unity 目标目录 |
|---|---|
| `ui/png/` | `Assets/Game/Sprites/UI/FirstPlayable/` |
| `ui/shaders/` | `Assets/Game/Shaders/FirstPlayable/UI/` |
| `vfx/textures/` | `Assets/Game/Textures/FirstPlayable/VFX/` |
| `vfx/shaders/` | `Assets/Game/Shaders/FirstPlayable/VFX/` |
| 后续创建的材质 | `Assets/Game/Materials/FirstPlayable/VFX/` |
| 后续创建的特效 Prefab | `Assets/Game/Prefabs/FirstPlayable/VFX/` |

以上路径沿用项目现有 `Sprite`、`Shader`、`Texture`、`Material`、`Prefabs` 分类，不新增顶层 `UI` 或 `VFX` 目录。

## 导入设置

- 每张图的精确 TextureImporter 参数以 `offline-art-import.json` 为准。
- 面板、按钮和危险框使用 Single Sprite，并设置清单中的 Border。
- 单枚图标可作为 Single Sprite；图集按 64×64 网格切为 18 个 Sprite。正式流程只能选用“单图”或“图集”其中一种加载策略，避免重复常驻。
- VFX 灰度纹理关闭 sRGB；噪声与抖动纹理 Repeat，色带和形状图集 Clamp。
- `T_UI_FP_MainMenu_Background_Oasis_v02.png` 是当前唯一主菜单背景候选；正式导入前需补足目标分辨率并复核裁切。

## 编辑器空闲后的顺序

1. 将文件复制到上述目标目录并让 Unity 生成 `.meta`。
2. 按导入清单设置纹理，检查 Alpha、九宫格边缘与图集切片。
3. 编译三个 Shader，消除全部 Error/Warning，再创建材质球。
4. 按 `material-presets.json` 建立火、冰、雷及三种反应材质。
5. 在独立美术资源测试场景中创建 UI 展板和 VFX 展台，逐项调节材质、粒子曲线、Bloom 与相机距离。
6. 通过视觉、性能和无缺失引用检查后，才允许正式场景或 Prefab 引用。
