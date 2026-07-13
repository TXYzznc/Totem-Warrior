# actor_common 四视图生成与验证记录

- 日期：2026-07-13
- 用途：`Player` / `SmartAI` / `LightAI` 共用主体的四视图基准，用于后续逐方向帧动画制作；不是最终入库 Sprite。
- 唯一角色参考：`../actor_common_concept_v02.png`。
- 生成方式：内置 `image_gen`，每个方向均为独立的 `identity-preserve` 生成，未使用任何其它人物参考。
- 透明底流程：每张先生成平坦绿色抠像源图，再用本地 chroma-key 工具输出 Alpha PNG。

## 交付文件

| 方向 | 绿色源图 | 透明成品 | Alpha 结果 |
| --- | --- | --- | --- |
| 前 | `actor_common_front_chromakey.png` | `actor_common_front.png` | 通过 |
| 后 | `actor_common_back_chromakey.png` | `actor_common_back.png` | 通过 |
| 左 | `actor_common_left_chromakey.png` | `actor_common_left.png` | 通过 |
| 右 | `actor_common_right_chromakey.png` | `actor_common_right.png` | 通过 |

## 内容验收

- 四图均为同一位偏男性、冷峻帅气的实验体逃亡者：短碎发侧剃、锐利男性脸部、精壮敏捷比例、黑色开放式机能战术服、暗铜/冷钢小件。
- 方向定义：`front` 直接面向镜头；`back` 直接背向镜头；`left` 面向画面左侧的严格左侧 profile；`right` 面向画面右侧的严格右侧 profile。
- 四图均为单角色、完整全身、双脚可见的中立 Idle 姿势；无武器、无文字、水印、额外人物或额外肢体。
- 未烘焙固定纹身、符文、体绘或类似纹身的疤痕。头部（侧剃与后颈）、躯干（正面胸腹 / 背面腰背）、左右臂、左右腿均保有可用于运行时贴花的干净皮肤区域。
- 角色本体未使用抠像绿；背景没有地面、投影、渐变、道具或纹理。

## Alpha 抠图验证

- 工具：`C:\Users\WIN10\.codex\skills\.system\imagegen\scripts\remove_chroma_key.py`
- 通用参数：`--auto-key border --soft-matte --transparent-threshold 12 --opaque-threshold 220 --despill`

| 方向 | 成品尺寸 | 透明 / 半透明 / 不透明像素 | Alpha 外接框 | 四角 Alpha |
| --- | --- | --- | --- | --- |
| front | `930 x 1691` | `1,181,611 / 10,061 / 380,958` | `(190, 68, 725, 1582)` | `0, 0, 0, 0` |
| back | `922 x 1706` | `1,149,045 / 10,637 / 413,250` | `(182, 41, 748, 1589)` | `0, 0, 0, 0` |
| left | `924 x 1703` | `1,305,907 / 5,723 / 261,942` | `(305, 53, 607, 1633)` | `0, 0, 0, 0` |
| right | `928 x 1695` | `1,285,847 / 5,197 / 281,916` | `(291, 43, 608, 1584)` | `0, 0, 0, 0` |

目视检查确认：头发、手指、服装边缘和足部均完整，无明显绿色漏边。四张成品均为 RGBA，透明角点、角色外接框和前景覆盖率均通过。

## 提示词约束摘要

每个方向均使用同一核心约束：

```text
Preserve the supplied actor_common_concept_v02.png as the ONLY character reference.
Preserve the same handsome young adult male protagonist, masculine facial structure,
short textured black fringe with shaved sides, lean athletic male anatomy, black open
cropped tactical mantle, crossed dark harness, tactical shorts, light knee guards and
strapped sandals. Generate exactly one full-body strict <DIRECTION> turnaround view,
with no tattoos and clean readable skin zones at head, torso, arms and legs for runtime decals.
Use a perfectly flat #00ff00 chroma-key background, no ground, shadow, gradient, text or watermark.
```

## SHA-256

- `actor_common_front.png`：`DB638ED4CFF09FEEB2D68F8B9CF41419E0916C0184D488BD1B4B7C719602A2C0`
- `actor_common_front_chromakey.png`：`05739691DBCB1C438C6E9B16B99AE21A6FA3311079A68DC05FBEAB0C6469328A`
- `actor_common_back.png`：`D86C8BA970F98647C9545DC9E874056CD4D404F84A77DC931F8ABA1D2BD90571`
- `actor_common_back_chromakey.png`：`18D7868D82CEFEF13DFE7BDBCBED18F49553FFDB1DDF4E70A04A8E0C1EBEED4F`
- `actor_common_left.png`：`FFB6A4AF59E14D3FF317A309AD778CDCD17F88C07F648B9155DFED64D9563700`
- `actor_common_left_chromakey.png`：`2F421E1219FC4411F4A83719AAA503D46609059FA0F9AB48196061121563F7DB`
- `actor_common_right.png`：`7A842FF836EAB90CD378A431EF771A99CCCBA68572A987719E188669639E7E8D`
- `actor_common_right_chromakey.png`：`2314E0CEDFD8807123DBD7176FD0FBC421C245C5C2CD484599016289D3F3E0F6`
