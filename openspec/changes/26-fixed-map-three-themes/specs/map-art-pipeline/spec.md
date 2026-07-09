# Spec — map-art-pipeline（大图美术生产管线）

> 术语/路径以 [CONTRACT.md](../../CONTRACT.md) 为准。完整 SOP 见 [art/图一美术生产管线.md](../../art/图一美术生产管线.md)。

## ADDED Requirements

### Requirement: 六步可复现管线

400×400m 底图 SHALL 通过六步管线产出：① 设计地貌 mask（纯色块）→ ② 矢量化/超分放大得高清基准图 → ③ 均匀切成 5×5=25 块 → ④ 逐块按美术风格重绘 → ⑤ 拼回无缝底图 → ⑥ 叠加物件层。每一步 MUST 有明确产物与通过条件，MUST 可复现（同 mask + 同风格提示词 → 结构一致的底图）。

#### Scenario: 管线跑通图一
- **WHEN** 按六步执行图一（AiRuins）
- **THEN** 产出 `Assets/Resources/Sprite/Map/AiRuins/BaseMap/tile_r{0-4}_c{0-4}.png` 共 25 块 + Props + Mask，可在游戏内拼成无缝 400m 地面

### Requirement: mask 作为单一结构源

地貌 mask MUST 同时作为美术重绘的结构约束（阶段④）与数据层派生源（TerrainGridBaker）。阶段④每块重绘 MUST 以该块 mask 子图为结构约束，保证重绘结果与 mask 结构完全一致（河流走向/区域边界不偏移），否则无法无缝拼接。

#### Scenario: 重绘结构对齐
- **WHEN** 重绘块 (r,c)
- **THEN** 其地貌区域边界与 mask 子图逐像素级对齐（允许纹理差异，不允许结构偏移）

### Requirement: 无缝拼接

25 块拼接后 MUST 无可见结构断缝。纹理接缝 SHALL 通过以下手段之一或组合缓解：重绘带 overlap 边 + 羽化融合 / 顺序重绘续接 / 底图低频柔和 + 高频交给物件层。

#### Scenario: 拼接无结构断缝
- **WHEN** 25 块按 `tile_r{行}_c{列}` 拼接
- **THEN** 相邻块边界的地貌结构连续，无错位

### Requirement: 5×5 切块工具

系统 SHALL 提供 `MapMaskSlicer` Editor 工具，把高清基准图/mask 均匀切成 5×5=25 块并按 `tile_r{行}_c{列}` 命名（行列 0-4），切块 MUST 均匀且可逆（拼回等于原图）。

#### Scenario: 切块可逆
- **WHEN** 对一张图切块再拼回
- **THEN** 拼回结果与原图逐像素一致

### Requirement: 物件层直立呈现

物件层（建筑/自然景物）MUST 用 `BillboardSprite`（change 25）立在地面、面向相机产生景深。物件 sprite MUST 透明背景，落盘于 `Assets/Resources/Sprite/Map/<Theme>/Props/`。

#### Scenario: 物件直立有景深
- **WHEN** 物件层加载到 2.5D 场景
- **THEN** 物件直立面向相机，相机俯角下呈现纵深，非平贴地面

### Requirement: 图一完整、图二三占位

本 change MUST 完整产出图一（AiRuins）全部美术资源；图二（Alien）/图三（Virus）MUST 仅交策划大纲，美术资源占位（纯色/复用），不阻塞代码接入。

#### Scenario: 交付范围
- **WHEN** 验收本 change
- **THEN** 图一 25 块底图+物件齐备，图二三仅有策划大纲 + 占位资源
