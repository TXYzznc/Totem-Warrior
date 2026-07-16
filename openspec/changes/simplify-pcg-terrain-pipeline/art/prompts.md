# 草地—河流 PCG 美术生成提示词

## 状态

- 美术素材状态：已按单地貌画布重新生成，待用户视觉确认
- 模式：built-in imagegen，三个源画布；同一张画布只绘制一种地貌
- 风格修订：中高精度手绘像素风，降低首张试画的写实微细节
- 地貌图块：草地与河流各用一张 4×2 源画布，各自等分为 8 张 256×256
- 交界装饰：3×2 洋红底源画布等分、抠图后缩放为 6 张可变尺寸 PNG

## 源画布 A：8 个草地图块

Use case: stylized-concept.

Create one wide **4 column by 2 row grass terrain atlas** for a top-down 2.5D roguelike game. This canvas must contain grass terrain only. The eight cells must be equal squares that touch directly with **zero gutter, zero margin, zero padding, no grid lines and no separator strokes**. The artwork must fill the entire outer canvas in an exact 2:1 composition.

Style: original medium-high-detail hand-painted pixel art, approximately 64–96 pixel effective texture density per cell, deliberate chunky pixel clusters, simplified painterly shapes, readable material blocks, low-saturation dark natural palette, subtle upper-left ambient light. Clearly stylized and handcrafted, not photorealistic, not high-frequency blade-by-blade rendering, not smooth 3D, not vector art. Keep one consistent palette, pixel scale and lighting across all eight cells.

Layout, left to right:
- Row 1, cells 1–4: balanced muted green meadow; darker moss-rich grass; warm olive grass with sparse dry tips; fine short wind-combed grass.
- Row 2, cells 1–4: broad-leaf meadow texture; thick compressed tufts; cool dew-touched green grass; rugged dark flattened grass.

Every cell must contain only grass texture from edge to edge, including all four cell corners. No dirt patch, path, rock, flower, tree, prop or focal object.

Hard constraints: no river, no water, no labels, no numbers, no text, no watermark, no outer border, no rounded cells, no framed tile objects, no transparent pixels, no empty area. Individual cells do not need seamless or tileable edge matching. Do not imitate or copy any specific existing game.

## 源画布 B：8 个河流图块

Use case: stylized-concept.

Create one wide **4 column by 2 row river-water terrain atlas** for a top-down 2.5D roguelike game. This canvas must contain river water only. The eight cells must be equal squares that touch directly with **zero gutter, zero margin, zero padding, no grid lines and no separator strokes**. The artwork must fill the entire outer canvas in an exact 2:1 composition.

Style: original medium-high-detail hand-painted pixel art matching the grass atlas, approximately 64–96 pixel effective texture density per cell, deliberate chunky pixel clusters, simplified painterly shapes, readable water shapes, low-saturation dark teal and blue-green palette, subtle upper-left ambient light. Clearly stylized and handcrafted, not photorealistic, not high-frequency detail, not smooth 3D, not vector art. Keep one consistent palette, pixel scale and lighting across all eight cells.

Layout, left to right:
- Row 1, cells 1–4: calm deep teal ripples; broken blue-green current strokes; darker indigo-teal depth; clear cool overlapping wavelets.
- Row 2, cells 1–4: broad incomplete eddy arcs; subdued overcast green-blue ripples; sparse silver-cyan glints; slow olive-teal water with faint sediment undertone.

Every cell must contain only river water texture from edge to edge, including all four cell corners. No bank, land, beach, rocks, plants, foam border or floating object.

Hard constraints: no grass, no land, no labels, no numbers, no text, no watermark, no outer border, no rounded cells, no framed tile objects, no transparent pixels, no empty area. Individual cells do not need seamless or tileable edge matching. Do not imitate or copy any specific existing game.

## 源画布 C：6 交界装饰

Use case: stylized-concept.

Create one rectangular **3 column by 2 row sprite atlas** containing six isolated grass-river boundary decorations. The six cells must be equal rectangles with no visible grid line. Every cell background must be the exact same perfectly flat solid `#ff00ff`, with no gradient, texture, shadow, reflection or lighting variation. Keep every subject fully inside its own cell with generous separation and do not let any subject cross a cell boundary.

Style: the same original medium-high-detail hand-painted pixel art as the terrain atlas, approximately 64–96 pixel effective texture density, deliberate chunky pixel clusters, simplified painterly shapes, low-saturation wetland greens, muted brown stems and restrained teal highlights, readable top-down 2.5D silhouette, no photorealistic micro-detail, no smooth 3D rendering. Do not use `#ff00ff` inside any subject.

Layout, left to right:
- Row 1 cell 1: medium irregular cluster of reeds and wet grass at several heights.
- Row 1 cell 2: long sparse horizontal fringe of thin water grass and low reeds with clear gaps, not a continuous bank.
- Row 1 cell 3: small compact group of three muted floating leaves with a few fine water-grass shoots.
- Row 2 cell 1: short low horizontal tuft of wet-bank grass sprouts.
- Row 2 cell 2: loose cluster of three rounded mossy river stones interwoven with short aquatic grass, no ground slab.
- Row 2 cell 3: one weathered forked driftwood branch entwined with a few reeds and floating leaves, broad horizontal composition.

Each subject is a local environment decoration, not a shoreline strip, not a terrain transition mask, and must not include a rectangular patch of grass or water. No cast shadow, no border, no frame, no text, no watermark. Do not imitate or copy any specific existing game.

## 切图映射

- 草地源图与河流源图分别按 4×2 行优先顺序映射为 `grass_01`–`grass_08` 和 `river_01`–`river_08`。
- 装饰源图按 3×2 行优先顺序映射为 `grass_river_deco_01`–`grass_river_deco_06`。
- 若 4×2 源图不是 2:1，只允许对长边做对称中心裁切到 2:1；若像素尺寸不能被网格整除，只允许再做最多 3 px 的对称裁切。不得用生成算法补边。
