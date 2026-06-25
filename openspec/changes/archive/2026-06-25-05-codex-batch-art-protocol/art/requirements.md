# 美术需求 — 05 demo（验证 L2 合并画布协议）

美术素材状态: 已处理
处理日期: 2026-06-25
执行 SKILL: codex-image-gen（L2 模式）
输出目录: art/raw/
生成记录: art/raw/生成记录.md
批量分档: L1 ×0，L2 ×9（1 批），L0 ×0
节省额度: ~88.9%（9 次调用 → 1 次）

---

## 总览

本 change 是 codex-image-gen 批量协议改造的 **demo 验证**，不是真实业务美术需求。9 张 256×256 透明背景 icon → 通过 L2 合并画布协议一次生成。

## 资源清单

| # | 资源名 | 尺寸 | 透明 | 类型 | 用途 |
|---|---|---|---|---|---|
| 1 | sword_icon | 256×256 | 是 | ICON | 武器槽 demo |
| 2 | shield_icon | 256×256 | 是 | ICON | 防具槽 demo |
| 3 | staff_icon | 256×256 | 是 | ICON | 法器槽 demo |
| 4 | bow_icon | 256×256 | 是 | ICON | 远程武器槽 demo |
| 5 | potion_icon | 256×256 | 是 | ICON | 消耗品槽 demo |
| 6 | scroll_icon | 256×256 | 是 | ICON | 卷轴槽 demo |
| 7 | gem_icon | 256×256 | 是 | ICON | 宝石槽 demo |
| 8 | key_icon | 256×256 | 是 | ICON | 钥匙槽 demo |
| 9 | coin_icon | 256×256 | 是 | ICON | 货币槽 demo |

## 风格约束

- 统一卡通幻想风（cartoon fantasy）
- 干净轮廓，强可读性（icon 用途）
- 透明背景 PNG，alpha 通道有效
- 每个 icon 独立，**不要**共享背景或装饰边框
- **不要**画文字 / 数字标签

## 验收

- 9 张独立 PNG，size > 1KB
- 文件名按上表 `<资源名>.png`
- 切割后资源名与清单完全对应（顺序匹配或视觉校正后）
