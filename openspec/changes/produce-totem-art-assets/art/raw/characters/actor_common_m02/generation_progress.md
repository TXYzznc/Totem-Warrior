# actor_common_m02 最终角色生产记录（已完成）

唯一身份参考为 `../../../../explorations/player_style_round_01/mature_candidates/M02_graphite_open_jacket_warrior.png`。本目录未触碰 Unity `Assets`、配置或 VFX。

## 已完成

- 四视图：`turnaround/actor_common_m02_front|back|left|right.png`，均有对应 `*_chromakey.png` 源图。
- 四视图 Alpha 检查：四个成品均为 RGBA，四角 Alpha 均为 0，前景外接框有效。
- `idle/down`：绿色源 sheet 与透明 sheet 已输出；已切为 `actor_common_m02_idle_down_01.png` 至 `_04.png`。

## 绿色边缘修复标准

初版 idle/down 抽检发现边缘 chroma spill，因此所有后续 sheet 固定使用：

```text
--auto-key border --soft-matte --transparent-threshold 14 --opaque-threshold 230 --edge-contract 2 --despill --force
```

idle/down 修复后：

- 按比例边界 `0 / 444 / 887 / 1330 / 1774` 切分 1774×887 源 sheet；未拉伸。
- 每帧等比缩放、置入 512×512 RGBA 透明画布，Alpha 前景底部统一为 `y=511`。
- 四帧的高 G、低 R/B 绿色边缘像素检测值均为 `0`（条件：`alpha > 15`、`G > R + 25`、`G > B + 25`、`G > 100`）。
- 黑底复核通过，发梢、手指和足部没有可见绿边。

## 最终全量验收

已扫描 16 个动作方向、96 张最终单帧，结果无错误：

| 动作 | 每方向帧数 | 方向 | 小计 | 结果 |
| --- | ---: | --- | ---: | --- |
| idle | 4 | down / up / left / right | 16 | 通过 |
| walk | 6 | down / up / left / right | 24 | 通过 |
| attack | 6 | down / up / left / right | 24 | 通过 |
| death | 8 | down / up / left / right | 32 | 通过 |
| 总计 |  | 16 个动作方向 | **96** | **通过** |

所有 96 帧均满足：

- `RGBA`、`512 x 512`；
- 四个画布角 Alpha 均为 `0`；
- Alpha 前景外接框底部统一为 `y=511`；
- 高 G、低 R/B 的 chroma spill 检测为 `0`（`alpha > 15`、`G > R + 25`、`G > B + 25`、`G > 100`）；
- 每张源 sheet 保持单角色、单动作、单方向；保留绿幕源、透明 sheet 和切分帧。

动作批次的细节记录：`walk_generation_validation.md`、`attack_generation_validation.md`、`death_generation_validation.md`；`death_validation.json` 记录死亡动作的逐帧明细。黑底预览用于复核透明边缘。
