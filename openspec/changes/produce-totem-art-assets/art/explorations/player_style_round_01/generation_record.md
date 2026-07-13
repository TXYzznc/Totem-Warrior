# 通用玩家风格与外观探索 Round 01 — B01–B05

生成日期：2026-07-13  
生成方式：内置 `image_gen`（每个候选一张独立图）→ `remove_chroma_key.py` 抠图；未使用 CLI 降级或外部模型。  
工程影响：无。全部为 OpenSpec 探索候选，未写入 `Assets/`、catalog 或配置。

## 共同功能约束

- 主体为偏男性、可带少量中性气质的年轻成年角色；全身 Idle 构图。
- 角色本体无纹身、无身体标记；为未来运行时贴花保留头/颈、躯干、双臂、双腿六个区域。
- 避免照片级写实、3D 渲染、皮肤毛孔/微材质、复杂小配件；保持中低精细度、明显笔触与 3–5 级明暗。
- 使用纯 `#00ff00` 绿幕源图，后处理为透明 RGBA PNG；无地面、投影、文字、水印。

## 候选与验证

| ID | 画风与外观试验 | 成品 | 绿幕源 | 尺寸 / Alpha 验证 | 评审备注 |
|---|---|---|---|---|---|
| B01 | 复古漫画厚涂街头斗士；短发、开放无袖深蓝罩衣、短裤 | `B01_retro_comic_street_fighter.png` | `B01_retro_comic_street_fighter_chromakey.png` | 864×1821 RGBA；四角 alpha=0；0 个不透明绿幕残留 | 轮廓与男性帅气感最强；肌肉量偏高，是后续收敛时需降简的候选。 |
| B02 | 平面拼贴/油画笔触荒原猎手；短披风、露臂露腿 | `B02_collage_wasteland_hunter.png` | `B02_collage_wasteland_hunter_chromakey.png` | 1024×1536 RGBA；四角 alpha=0；0 个不透明绿幕残留 | 服装色块和披风带来较强身份感；披风覆盖局部肩部，后续需保证贴花落点规范。 |
| B03 | 民俗图腾感粗笔逃亡者；简洁腰布/短裤，未绘制实际纹身 | `B03_folk_totem_fugitive.png` | `B03_folk_totem_fugitive_chromakey.png` | 1024×1536 RGBA；四角 alpha=0；0 个不透明绿幕残留 | 用配色和粗笔而非皮肤符号表达图腾感；六区裸露度较高。 |
| B04 | 低多边形色块感的 2D 绘画实验体；极简护具 | `B04_lowpoly_paint_subject.png` | `B04_lowpoly_paint_subject_chromakey.png` | 1024×1536 RGBA；四角 alpha=0；0 个不透明绿幕残留 | 2D 平面感最强、便于动画简化；胸部护带占用局部躯干，但其余贴花区清晰。 |
| B05 | 水墨轮廓 + 有限彩色干刷敏捷战士；开放胸腹、短裤 | `B05_ink_drybrush_agile_warrior.png` | `B05_ink_drybrush_agile_warrior_chromakey.png` | 1024×1536 RGBA；四角 alpha=0；0 个不透明绿幕残留 | 视觉最简洁、适合 Sprite 降采样；其墨线语言与其余厚涂候选差异最大。 |

## 检查方法

- Pillow 读取每张成品，确认 `RGBA`、四角完全透明，并统计不透明像素中的高饱和绿色残留（5 张均为 0）。
- 人工检查全身未裁切、肢体完整、无纹身/文字/水印。`round_01_contact_sheet.jpg` 是仅供横向评审的深灰底预览，不是游戏资源。

## 后续门槛

仅在选择并定稿一个方向后，才用该方向建立四视图和四方向帧动画；本轮探索图不得直接导入运行时资源目录。
