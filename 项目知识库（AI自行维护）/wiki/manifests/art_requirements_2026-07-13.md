# 实际美术资源缺口（2026-07-13）

本清单以运行时资源索引、Prefab 引用和 PCG catalog 的当前路径为准。

## 已确认完成或已接入

- 通用玩家采用 M02 成熟战士方向；Player、SmartAI、LightAI 共享同一套角色资源。
- Boss、纹身师、商人以及 `player_2`、`player_3` 的肖像资源均已导入并接入；NPC 的世界资源为全身 Idle 姿势。
- `player_2`、`player_3` 当前只承担角色选择肖像占位，不制作独立战斗 prefab 或动画。
- 玩家、Boss 的四方向 `idle`、`walk`、`attack`、`death` 动画已接入各自的 AnimatorController；所有角色本体均保持无纹身。
- Player、SmartAI、LightAI 的阵营环已由运行时代码创建（蓝 / 红 / 黄），角色主体与 runtime catalog tint 均保持中性白色。

## PCG：无本轮美术缺口

PCG 当前不在项目资源范围内，相关配置待后续清理。不得据此文件补绘、导入或维护 PCG 图片，也不得恢复旧 `Assets/Resources/PCG/Terrain/` 资源。

本轮误创建的 50 张 PCG 图片及其 raw、预览、检查文件必须删除，不应再作为美术需求或运行时资源索引项。

## 当前美术缺口

本轮已确认范围内没有待生成的位图资源。以下是产品已明确延后的工作，不应伪报为本轮美术缺口：

1. 后续新增可玩角色时，再为 `player_2`、`player_3` 制作独立的全身四视图、战斗 prefab 与四方向动画；当前两张肖像仅为角色选择占位。
2. VFX 走 Shader / 粒子系统管线，不绘制位图特效。
3. 纹身贴花由后续程序在头、躯干、左右臂、左右腿运行时叠加；角色本体不烘焙纹身。

## 资源约束

- 不恢复 `Assets/Game/Sprites/Character`、`Characters`、`Environments`、`Recipes`、`Tattoo`。
- 新增角色与 NPC 资源使用半写实厚涂风格；PCG 保持既有资源，不纳入本轮重绘。
- 修改或新增资源后，重建 `wiki/manifests/art_assets.json` 并复跑 Unity 验证。
