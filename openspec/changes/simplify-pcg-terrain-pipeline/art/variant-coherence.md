# 地貌图块变体一致性合同

## 目标

八张变体的用途是打散重复感，不是制造八种子地貌。玩家把任意四张同地貌图块相邻摆放时，应先读到“同一种连续地表”，之后才注意到笔触与纹理位置不同。

草地的有效基准是用户标记为合格的前三张：短而密的暗绿草簇、相同笔触密度、相近明度。水域的有效基准是用户标记为合格的前四张：深蓝青底色、细而低对比的软水纹、无强特征波形。

## 强制约束

- 每种地貌先定义一张“锚点图块”；同一地貌的另外七张只能改变锚点中的**局部纹理位置**、少量笔触断续和极轻微的局部密度。
- 八张图块必须使用同一主色相、同一明暗范围、同一像素笔触尺度、同一材质词汇与同一环境光方向。
- 不允许通过更换草叶/植物种类、植被高度、叶片形状、水纹方向、波浪类型、亮点密度、地表覆盖物或全局色温来制造差异。
- 不允许出现一张明显偏黄、一张偏青、一张特别亮/暗、一张极密/极疏，或一张具有大尺度条纹、涡流、网格、露珠等独占图案的情况。
- 图块仍须从边到边铺满 256×256 画布，四边无透明像素；这不是无缝要求，也不能借机引入边框或岸线。

## 允许变化预算

| 维度 | 允许 | 禁止 |
|---|---|---|
| 色彩 | 同一色板内极轻微的局部明暗起伏 | 改变整体色相、整体色温或整体亮度 |
| 密度 | 少量草簇/纹理笔触的局部疏密互换 | 改变整张图的植被、水纹或高光密度 |
| 构图 | 小尺度笔触的位置、方向、断续 | 明显涡流、条纹、大片空地、焦点物件 |
| 材质 | 同一种材料的细微笔触变化 | 另一种植物、另一种水面、石块、泥地、道路等新材料 |

## 验收方式

1. 将八张图块缩小到游戏中实际大小并以 4×2 拼接查看；不能一眼把它们分成不同草地/不同水域子类型。
2. 任意两张互换位置后，整体颜色和材质观感不能突然跳变。
3. 对草地，拒绝宽叶、芦苇、明显高草、露珠高光、方向性条纹和过黄/过青版本。
4. 对水域，拒绝大涡流、鱼鳞网纹、亮点密集版本、不同方向的大波浪和偏绿的浑水版本。
5. 不满足本合同的图块不进入八张随机池；宁可重绘，不以“变体丰富”为理由保留。

## 生产提示词附加段

每一张 4×2 地貌源画布的提示词都必须追加：

> All eight cells are near-identical micro-variants of one anchor terrain tile, not eight distinct designs. Keep the same palette, value range, density, material vocabulary, pixel scale and lighting in every cell. Only rearrange small existing texture clusters. At game scale, adjacent cells must read as one harmonious terrain type with no noticeable color, density or motif jump. Do not introduce a new plant type, water pattern, brightness level, hue shift, focal feature or large directional structure in any cell.

## Decoration transparency contract

- All model-drawn overlay atlases use one perfectly flat `#00FF00` chroma-key background. The key color must cover every non-subject pixel, including the spaces between cells.
- The pipeline must remove green from the **whole atlas** with `tools/chroma_key_tool/chroma_key.py` (`threshold 80`, `soft-edge 30`, `despill 0.85`) before splitting its six cells. This preserves anti-aliased edges and removes green spill.
- Final decorations may have varied canvas sizes, but must have transparent corners and genuine alpha around the subject. Native transparency, checkerboards, white/black backgrounds, and a second key color are not accepted.
