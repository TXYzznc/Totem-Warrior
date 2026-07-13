# actor_common_m02 walk：生成与验证记录

## 范围

- 动作：`walk`
- 方向：`down`、`up`、`left`、`right`
- 帧数：每方向 6 帧，共 24 帧。
- 身份与方向唯一参考：`turnaround/actor_common_m02_front|back|left|right.png`。
- 未修改 Unity `Assets`、配置或其它动作资源。

## 生成与处理

四张源 sheet 均为单角色、单动作、单方向的横向六格序列，使用平坦绿幕生成。透明处理固定使用：

```text
--auto-key border --soft-matte --transparent-threshold 14 --opaque-threshold 230 --edge-contract 2 --despill
```

每张透明 sheet 按 `round(i * width / 6)` 分段；每个单元只保留最大的 Alpha 连通主体，以排除跨单元的孤立脚部残片。随后按方向内统一比例缩放、置入 `512×512` RGBA 画布，并将前景最低像素对齐至 `y=511`。

## 输出

| 方向 | 绿幕源 sheet | Alpha sheet | 帧输出 |
|---|---|---|---|
| down | `actor_common_m02_walk_down_sheet_chromakey.png` | `actor_common_m02_walk_down_sheet.png` | `_down_01.png` ～ `_down_06.png` |
| up | `actor_common_m02_walk_up_sheet_chromakey.png` | `actor_common_m02_walk_up_sheet.png` | `_up_01.png` ～ `_up_06.png` |
| left | `actor_common_m02_walk_left_sheet_chromakey.png` | `actor_common_m02_walk_left_sheet.png` | `_left_01.png` ～ `_left_06.png` |
| right | `actor_common_m02_walk_right_sheet_chromakey.png` | `actor_common_m02_walk_right_sheet.png` | `_right_01.png` ～ `_right_06.png` |

## 自动验证结果

对全部 24 张单帧逐张验证：

- 模式与尺寸：全部为 `RGBA 512×512`。
- 四角 Alpha：全部为 `0`。
- Alpha 前景最低行：全部为 `y=511`。
- 绿边检测：全部为 `0`。判定为 `alpha > 15 && G > R + 25 && G > B + 25 && G > 100`，计算前将通道转为有符号整数，避免 `uint8` 加法溢出。
- 目视复核：已复核 `left/02`；此前的相邻格孤立脚部残片已由最大连通主体规则移除。

## Alpha sheet 校验与哈希

四张 Alpha sheet 均为 `RGBA 2172×724`，四角 Alpha 均为 `0`。

| 文件 | SHA-256 |
|---|---|
| `actor_common_m02_walk_down_sheet.png` | `D81F1ABFCC0A6AF0C8019BE9703D7F502985A9BE7D2FD5059B26671664000B28` |
| `actor_common_m02_walk_up_sheet.png` | `3A32B5685957E0AD2279F2D7A51AE2454FE549F92845417CC208C3307507FA0B` |
| `actor_common_m02_walk_left_sheet.png` | `1419B655CD0CC1E853C38572DC5AAAE89497AF04AF5FBED515ED22CE76B010AE` |
| `actor_common_m02_walk_right_sheet.png` | `1D12D6E7495D3A89415E5A0CD14BCA93EC1E45A1BFE01D6AF4E4BDAB48A9B646` |
