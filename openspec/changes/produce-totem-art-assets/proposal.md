## Why

当前运行时资源路径已清理，但通用玩家、Boss、NPC 的可见 Sprite 与 AnimatorController 仍为空。PCG 目录配置的路径问题已修复并确认映射到项目现有切片，不属于本次美术缺口。现有资源同时混有过时目录、占位复用和不一致的画风，需要以已确认的美术方向一次建立可追溯的生产与接入闭环。

## What Changes

- 新增首批半写实厚涂角色美术生产：一套供 Player、SmartAI、LightAI 复用的无纹身角色；AI 遗迹执政官 Boss；纹身师、商人静态世界 Sprite；以及两张未来角色占位立绘。
- 为通用玩家和 Boss 生产概念立绘、四视图、四方向 `idle` / `walk` / `attack` / `death` 帧动画，并建立稳定的文件命名、切帧、脚底锚点和导入验收规则。
- 更新角色美术契约，以当前 GF_X runtime asset catalog 和 `Assets/Game/Sprite/Actors/` 目录替代历史 `Assets/Resources/Sprite/Character/` 路径及 Player2/3 动画要求。
- 记录阵营环与纹身贴花的后续接入约束；本变更不实现 VFX、Shader、粒子系统或运行时纹身贴花。

## Capabilities

### New Capabilities

- `totem-art-production`: 首批角色、NPC、概念图、四视图与帧动画的生产、命名和验收契约。
- `actor-faction-indicator`: 不改变半写实角色原色的玩家 / SmartAI / LightAI 阵营识别表现。

### Modified Capabilities

- `gameplay-character-art`: 用当前 GF_X 运行时目录、共享角色策略、六帧标准和 Animator 约束替换过时的 Resources 路径及角色范围。

## Impact

- 美术源文件和处理结果存放在 `openspec/changes/produce-totem-art-assets/art/`；最终导入物位于新的 `Assets/Game/Sprite/Actors/` 与 `NPC/` 目录，禁止恢复旧目录。
- 后续会更新 Actor/NPC prefab、AnimatorController、`totem_runtime_assets.json`、`TotemRuntimeAssetCatalog.cs` 和美术索引。
- 阵营识别目前由 runtime catalog 的整图 tint 实现；在接入本轮半写实角色前，需改为脚下环或头顶标识，避免染色破坏角色与纹身可读性。
