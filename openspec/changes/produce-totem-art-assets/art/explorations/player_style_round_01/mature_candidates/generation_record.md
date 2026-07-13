# 通用玩家成熟战士候选 M06–M10

生成日期：2026-07-13  
用途：通用玩家美术风格与外观探索的**最终候选组**。B01–B05 仅保留为画风比较材料，不再作为候选。  
生成链路：内置 `image_gen` 独立出图 → `remove_chroma_key.py` 绿幕抠图 → Pillow 透明/残留检查。  
工程影响：无；未写入 `Assets/`、运行时 catalog 或配置。

## 成熟战士共同基准

- 年龄视觉目标为 30 岁（允许 28–34 岁）；成年方正脸、成熟眼神或短胡茬。
- 常年战斗训练的结实体格：肩背略宽、四肢结实；禁止纤瘦青年、健美夸张和清晰六块腹肌。
- 无武器、无阵营色依赖、无纹身/身体标记；头/颈、躯干、双臂、双腿可作未来贴花区。
- 中低精细度的 2D 半写实厚涂：大笔触、3–5 档明暗、可读剪影；禁止照片写实、3D 渲染、微观材质。
- 每张全身 Idle、透明背景；同目录的 `*_chromakey.png` 是纯绿幕源，非运行时资源。

## 当前候选

| ID | 方向 | 当前成品 | 当前源图 | 尺寸 / 透明验证 | 评审说明 |
|---|---|---|---|---|---|
| M06 | 复古漫画厚涂街头斗士；深青无袖罩衣 | `M06_retro_comic_mature_street_fighter.png` | `M06_retro_comic_mature_street_fighter_chromakey.png` | 1024×1536 RGBA；四角透明；不透明绿幕残留 0 | 年龄和笔触成立；肌肉量已接近允许上限，后续若选用应进一步收简腹肌。 |
| M07 | 拼贴油画笔触荒原猎手；赭黄短肩披、短裤 | `M07_collage_mature_wasteland_hunter_v02.png` | `M07_collage_mature_wasteland_hunter_v02_chromakey.png` | 1024×1536 RGBA；四角透明；不透明绿幕残留 0 | v02 用平坦自然腹部重出；同名无 v02 的首图仅作溯源。 |
| M08 | 民俗图腾感粗笔逃亡者；开放背心、短裤 | `M08_folk_totem_mature_fugitive_v02.png` | `M08_folk_totem_mature_fugitive_v02_chromakey.png` | 1024×1536 RGBA；四角透明；不透明绿幕残留 0 | v02 清除了会被误读为纹身的皮肤色块；图腾感只来自服装配色与笔触。 |
| M09 | 几何色块/低多边形感的 2D 实验体；简短护具 | `M09_geometric_mature_subject_v02.png` | `M09_geometric_mature_subject_v02_chromakey.png` | 1024×1536 RGBA；四角透明；不透明绿幕残留 0 | v02 降低躯干肌肉切面；保持 2D 几何色块而非 3D 建模感。 |
| M10 | 水墨轮廓 + 有限彩色干刷敏捷战士；开放胸腹、短裤 | `M10_ink_mature_agile_warrior_v02.png` | `M10_ink_mature_agile_warrior_v02_chromakey.png` | 1024×1536 RGBA；四角透明；不透明绿幕残留 0 | v02 收敛腹部线条与写实材质，保留水墨轮廓方向。 |

`mature_candidates_v02_contact_sheet.jpg` 仅为横向评审预览，使用深灰底合成展示透明成品，不能作为游戏资源导入。

## 后续门槛

用户选定一个 M 候选后，才以该候选为唯一角色参考制作四视图；四视图通过后才进入四方向帧动画。不得直接把候选图导入运行时角色资源。
