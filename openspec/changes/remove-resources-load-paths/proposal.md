## Why

资源已从 `Assets/Resources` 迁入 `Assets/Game`，但运行时目录、PCG 和框架内置代码仍有活动路径或 `Resources.Load` 调用指向旧目录。当前 Editor 与 Player 的资源行为不一致，PCG 和热更新启动链也可能因找不到资源而失败。

## What Changes

- 将运行时资源目录及默认资源目录中的旧 Sprite 路径更新为 `Assets/Game/Sprites`。
- 让运行时目录、PCG 配置与 PCG 贴图通过项目现有 GF 资源加载管线读取，移除业务运行时的 `Resources.Load` 依赖。
- 更新纹身资源配置、生成目录和编辑器诊断/迁移工具中的活动旧路径。
- 将 AppSettings、Obfuz 密钥和 AOT 元数据的加载与构建路径迁出已删除的 `Assets/Resources`。

## Capabilities

### New Capabilities

- `game-resource-loading`: 已迁移到 `Assets/Game` 的运行时资源在 Editor 和 Player 均可通过统一加载管线取得。

### Modified Capabilities

- `workflow`: 资源迁移后的活动路径检查不再接受 `Assets/Resources` 依赖。

## Impact

涉及 `TotemAssetService`、PCG 地图服务与目录、GF 内置启动/构建代码、业务 JSON/生成目录、编辑器迁移器及诊断；不移动或替换现有美术资源文件。
