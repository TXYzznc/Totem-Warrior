# actor_common_m02 death：生成与验收记录

## 范围

- 动作：`death`，徒手自然倒地；不包含武器、固定纹身、血液、VFX 或场景元素。
- 方向：`down`、`up`、`left`、`right`。
- 帧数：每方向 8 帧，共 32 帧。
- 唯一身份参考：`turnaround/actor_common_m02_front|back|left|right.png`。
- 未修改 Unity `Assets`、运行时索引、配置或 Prefab。

## 源图与处理

每方向使用“一动作、一方向、一张横向序列画布”的绿幕源图。输出采用：

```text
--auto-key border --soft-matte --transparent-threshold 14 --opaque-threshold 230 --edge-contract 2 --despill
```

生成模型在部分序列中没有严格遵守等宽格，处理脚本会在每个名义分格附近寻找最低 Alpha 的竖向缝隙，再提取每格最大的连续主体；这样可避免把倒地肢体截断在相邻格中。所有输出再按各方向统一比例置于 `512×512` RGBA 画布，并将最低前景像素对齐到 `y=511`。

| 方向 | 绿幕源图 | Alpha sheet | 最终帧 |
| --- | --- | --- | --- |
| down | `actor_common_m02_death_down_sheet_chromakey.png` | `actor_common_m02_death_down_sheet.png` | `_down_01.png` ～ `_down_08.png` |
| up | `actor_common_m02_death_up_sheet_chromakey.png` | `actor_common_m02_death_up_sheet.png` | `_up_01.png` ～ `_up_08.png` |
| left | `actor_common_m02_death_left_sheet_chromakey.png` | `actor_common_m02_death_left_sheet.png` | `_left_01.png` ～ `_left_08.png` |
| right | `actor_common_m02_death_right_sheet_chromakey.png` | `actor_common_m02_death_right_sheet.png` | `_right_01.png` ～ `_right_08.png` |

`left` 与 `right` 的首批源图存在末段跨格粘连，已保留为
`*_chromakey_v01_crosscell.png`，并重新生成最终采用的源图；未覆盖或删除可追溯版本。

## 自动验收

`death_validation.json` 对 32 张最终帧逐张记录了源格、连通主体边界、哈希与检查结果。结果如下：

- 模式与尺寸：全部为 `RGBA 512×512`。
- 四角 Alpha：全部为 `[0, 0, 0, 0]`。
- 基线：全部为 `y=511`。
- 绿边残留：全部为 `0`；判定为 `alpha > 15 && G > R + 25 && G > B + 25 && G > 100`。
- 目视复核：`actor_common_m02_death_contact_black_preview.png` 以及四方向第 8 帧已在黑底下检查，主体为 M02 的短发、胡须、无袖高领背心、胸前双带、深色短裤、赤脚且无固定纹身。

## 可复现处理

```text
python tools/process_actor_common_m02_death.py
```

该脚本只处理本目录的 death 源 sheet 与输出帧，并重建 `death_validation.json` 和黑底接触表，不操作 Unity 工程资产。
