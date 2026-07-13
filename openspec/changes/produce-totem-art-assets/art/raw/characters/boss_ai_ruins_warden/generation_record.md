# boss_ai_ruins_warden — 动画源画布与切分帧记录

- 生成日期：2026-07-13
- 主体／方向唯一参考：`../../boss_ai_ruins_warden/turnaround/` 下的 `front`、`back`、`left`、`right` 四张四视图。
- 生成方式：按单角色连续批处理顺序生成所有动作绿幕画布；本地 chroma-key 抠图；按动作／方向拆分、统一到透明 `512 × 512` 帧。
- 范围：仅 Boss 动画原始美术；未修改 `Assets/`、Unity 资源索引或 Animator。

## 交付范围

| 动作 | 每方向帧数 | 方向 | 源画布数 | 切分帧数 |
| --- | ---: | --- | ---: | ---: |
| `idle` | 4 | down / up / left / right | 4 | 16 |
| `walk` | 6 | down / up / left / right | 4 | 24 |
| `attack` | 6 | down / up / left / right | 4 | 24 |
| `death` | 8 | down / up / left / right | 4 | 32 |
| 合计 |  |  | 16 | 96 |

## 命名与文件保留

每个动作、方向均保留三种 sheet：

- 绿幕生成源：`boss_ai_ruins_warden_{action}_{direction}_sheet_chromakey.png`
- 抠图后、切分前的原始 Alpha sheet：`boss_ai_ruins_warden_{action}_{direction}_sheet_alpha_original.png`
- 规范化透明源画布：`boss_ai_ruins_warden_{action}_{direction}_sheet.png`

规范化源画布为横向连续序列：idle 为 `2048 × 512`、walk／attack 为 `3072 × 512`、death 为 `4096 × 512`。各切分帧命名为 `boss_ai_ruins_warden_{action}_{direction}_{frame:02}.png`，索引从 `00` 开始。

## 动作约束

- `idle`：机械呼吸与核心脉冲的轻微待机变化。
- `walk`：接触、承重、经过、反向接触的重型机械步态。
- `attack`：可复用于 stomp / beam / summon 的通用预备与收势；未绘制光束、冲击、召唤物、粒子、投射物或其它 VFX 位图。
- `death`：关机、失衡、下跪／倾倒、落地、静止的八帧连续过程。

## 验证结果

- 源画布：预期 16，实际 16；绿幕源：预期 16，实际 16。
- 切分帧：预期 96，实际 96；无缺失文件。
- 所有切分帧均为 RGBA `512 × 512`；所有画布和帧的四角为透明。
- 每帧 Alpha 内容非空；检测到的绿色泄漏像素为 0。
- 使用同一归一化锚点：96 帧 Alpha 边界底部均为 `y = 480`，脚底／地面接触锚点一致。
- 目视检查通过：Boss 的黑曜石／蓝色氧化甲片、锈铜缝隙、青色胸腔核心与肩背浮动碎片在四方向和动作内保持可识别；未混入其它角色或背景。
