# 正式资源入库状态

## 2026-08-12

已将 production 目录中的 38 个可运行时导入资源复制到项目正式资源根目录，原始 OpenSpec 产物保持不变。

| 类型 | 数量 | 正式位置 |
|---|---:|---|
| UI PNG | 30 | `Assets/Game/Sprites/UI/FirstPlayable/` |
| UI Shader | 1 | `Assets/Game/Shaders/FirstPlayable/UI/` |
| VFX 贴图 | 4 | `Assets/Game/Textures/FirstPlayable/VFX/` |
| VFX Shader / HLSL include | 3 | `Assets/Game/Shaders/FirstPlayable/VFX/` |

所有复制结果已与源文件完成 SHA-256 一致性校验。

## 待编辑器空闲后处理

- Unity 导入并生成 `.meta`。
- 按 `offline-art-import.json` 应用九宫格 Border、图集切片、纹理 sRGB、Wrap、Filter 与压缩设置。
- 编译 Shader，并依据 `vfx/material-presets.json` 创建 VFX 材质球。
- 在 `Assets/Game/Scene/ArtResourceTest.unity` 中完成 UI 与 VFX 视觉/性能验收后，再接入正式 Prefab 或场景。

## 未入库的非运行时产物

- `previews/UI_FP_OfflineAssetContactSheet_1920x1080.png`：审阅联系表。
- `*.md`、`*.json`、`*.py`：设计、参数、来源和离线生成记录。
