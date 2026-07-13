## Context

当前 GF_X runtime catalog 有 Player、SmartAI、LightAI、Boss、纹身师和商人 prefab，但六个 prefab 的 `SpriteRenderer.m_Sprite` 与 `Animator.m_Controller` 均为空。现有 catalog 通过全量 `SpriteRenderer.color` 对角色上蓝、红、黄 tint；这会破坏本轮确认的半写实肤色、布料与未来纹身贴花颜色。

本次生产涉及角色、动画、NPC 和 runtime 接入，因此用一个 OpenSpec change 记录源文件、处理结果、导入物和验证结果。`art_production_brief_2026-07-13.md` 是已经确认的视觉基准。

## Goals / Non-Goals

**Goals:**

- 生产可被 Unity 直接导入的首批角色与 NPC 位图资源，并可追溯到概念图、四视图、源画布与处理记录。
- 为通用玩家和 Boss 建立四方向帧动画资产，恢复 Actor Animator 与 runtime asset diagnostics 的美术前提。
- 用脚下阵营环替代对整张角色 Sprite 的 tint，保留角色与运行时纹身贴花的原始颜色。
- 将最终资源接入新的非 Resources 旧目录、runtime catalog 和美术索引。

**Non-Goals:**

- 不制作 VFX 位图、Shader、粒子系统、技能图标替换或运行时纹身贴花系统。
- 不恢复旧 `Character`、`Characters`、`Environments`、`Recipes`、`Tattoo` 目录。
- 不为 `player_2` / `player_3` 制作战斗 prefab、四视图或动画。
- 不重绘 PCG 已存在的 1280 个主地形资源。

## Decisions

### 单一通用玩家主体

Player、SmartAI、LightAI 共用 `actor_common` 的同一份无纹身角色资源与 AnimatorController。选择复用而非为 AI 生成变体，原因是当前产品只需要一个正式可玩角色，AI 的角色差异尚无玩法和产品定义。阵营差异由 runtime indicator 承担。

### 先定主体，后定动作

每一个动画角色严格遵循“概念立绘 → 正/背/左/右四视图 → 单方向单动作源画布 → 切分帧 → Unity 导入”。所有动画帧以已确认四视图为唯一主体参考。源画布中每个 cell 最大为 512×512，且同一方向所有帧共用脚底锚点。

### 运行时贴花优先的服装结构

角色裸露区不绘制纹身。头、躯干、左右臂、左右腿的主贴花区必须在四视图和动画中保留；服装、护具和长发不能覆盖这些区域。选择这一路线而不是把纹身绘入角色，是为了让 6 部位 × 7 颜色 × 8 图案的运行时构筑保持可见和可组合。

### 阵营环替代全图 tint

`TotemAssetService.ApplyTint` 不再对 actor SpriteRenderer 直接上色。Actor root 下增加不遮挡身体的脚下环 renderer：玩家蓝、SmartAI 红、LightAI 黄；Boss 与 NPC 不使用该环。与全图 tint 相比，阵营环保留厚涂材质和未来贴花颜色，代价是需要接入一个小型 runtime renderer。

### NPC 的独立规格

纹身师和商人保留现有半身草图的身份语言（紫色纹身 / 暖铜金币），再生成透明背景的三分之四正面全身静态 Sprite；不为 NPC 生产动画。

## Risks / Trade-offs

- [生成图中角色主体漂移] → 每位角色先验收概念立绘与四视图；所有动作生成都把四视图作为参考；单张资产最多三轮针对性重试。
- [chroma key 残留绿边] → 使用内置 image generation 的纯色背景，经过 `remove_chroma_key.py` 后检查 alpha、透明角和边缘去色。
- [AI 生成多帧网格不均或帧间不一致] → 每张源画布只含一个角色、动作、方向；切图后逐帧检查尺寸、脚底锚点、帧数和主体一致性，不合格时只重做该方向。
- [脚下环遮挡地形或混入 VFX] → 环置于角色脚点下方、低排序层，采用纯色扁平小圆环，不使用粒子、拖尾或位图特效。

## Migration Plan

1. 在本 change 的 `art/` 中生成概念、四视图、源画布、切图和处理记录。
2. 验收后将最终 PNG 复制到 `Assets/Game/Sprite/Actors/` 与 `NPC/`，保留源文件在 change 中。
3. 创建 AnimatorController 与 clips，绑定 Actor/NPC prefab；更新 runtime catalog、required keys 与美术索引。
4. 修改 actor tint 为脚下阵营环，复跑 Unity 编译、actor runtime、runtime assets 和 gameplay runtime 检查。
5. 任一资源或导入验证失败时，仅删除本 change 新增的最终文件并回退该 key/prefab 引用；不恢复已删除旧目录。

## Open Questions

- NPC 静态 Sprite 的最终脚底锚点和世界缩放，以首次导入后的 Game View 截图确认。
