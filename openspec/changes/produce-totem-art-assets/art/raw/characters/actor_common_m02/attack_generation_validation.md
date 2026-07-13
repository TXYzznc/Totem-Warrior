# actor_common_m02 attack：生成与验证记录

## 范围

- 动作：`attack`，徒手拳击连段；不含武器、纹身、VFX 或发光效果。
- 方向：`down`、`up`、`left`、`right`；每方向 6 帧，共 24 帧。
- 身份参考：每个方向仅使用对应的 `turnaround/actor_common_m02_<direction>.png` 四视图作为生成参考。
- 未修改 Unity `Assets`、配置、catalog 或其他动作资源。

## 源 sheet 与透明处理

每张源 sheet 均为单一角色、单一动作、单一方向的横向六格序列，使用纯 `#00ff00` 绿幕生成。透明处理命令固定为：

```text
--auto-key border --soft-matte --transparent-threshold 14 --opaque-threshold 230 --edge-contract 2 --despill --force
```

| 方向 | 绿幕源 sheet | Alpha sheet | Alpha sheet SHA-256 |
| --- | --- | --- | --- |
| down | `actor_common_m02_attack_down_sheet_chromakey.png` | `actor_common_m02_attack_down_sheet.png` | `A876C0519DC1B7B6EB577EED94577D1C2C2BD57A2C3F0C6020D18CAA724A0230` |
| up | `actor_common_m02_attack_up_sheet_chromakey.png` | `actor_common_m02_attack_up_sheet.png` | `BAEA61A3CD18EE8FD57C892F987040CAE7605869689B781B3C8F4BE5190FFF64` |
| left | `actor_common_m02_attack_left_sheet_chromakey.png` | `actor_common_m02_attack_left_sheet.png` | `554ACBA3B1B752DF2699D95DB4D5B2C65B06E1CD77C32F3DE0EDBAC3DE626C63` |
| right | `actor_common_m02_attack_right_sheet_chromakey.png` | `actor_common_m02_attack_right_sheet.png` | `3958366B006F5D97FC0F4CCEFDCB6971817ABBE6C6676D68D63AB2A6CDAF4F96` |

四张 Alpha sheet 均为 `RGBA 2172×724`，四角 Alpha 均为 `0`。

## 切帧与锚点规则

- 最终单帧命名：`actor_common_m02_attack_<direction>_01.png` 至 `_06.png`。
- 每帧为 `RGBA 512×512`，只保留该帧最大的连续 Alpha 主体；攻击伸拳跨越原网格边界时，改从完整 sheet 的 6 个最大连通主体按水平质心排序提取，避免把相邻帧的拳头或脚部残片带入成品。
- 每方向统一缩放，前景最低像素统一对齐至 `y=511`。
- 按 `alpha > 15 && G > R + 25 && G > B + 25 && G > 100` 检查绿边，所有 24 帧残留为 `0`。

## 自动验证

对 24 张最终帧逐张检查：

- 尺寸与模式：全部为 `RGBA 512×512`。
- 四角 Alpha：全部为 `0`。
- 脚底基线：全部为 `y=511`。
- 高 G、低 R/B 的绿边残留：全部为 `0`。
- 透明输出详情、每帧源连通主体边界与 SHA-256：见 `attack_validation.json`。

目视复核预览：`actor_common_m02_attack_contact_black_preview.png`。四方向均保持 M02 的短发、胡须、无袖高领短背心、双胸带、深色短裤、赤脚与无纹身主体一致性。
