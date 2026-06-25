# 提示词 — 05 demo

美术素材状态: 已处理
处理日期: 2026-06-25

---

## L2 批次 batch_1（9 张 256×256 透明 icon → 1024×1024 透明画布）

### 画布提示词（codex exec 用）

```
A transparent 1024x1024 canvas containing 9 separate game item icons,
arranged in a 3x3 grid layout with at least 32 pixels of fully transparent
padding between each icon. Cartoon fantasy art style, clean outlines,
high readability. Each icon occupies roughly 200x200 pixels of visible
area centered in its grid cell. No connecting lines, no shared background,
no text or numbers anywhere on the canvas.

The 9 icons in row-major order (left-to-right, top-to-bottom):
1. (row 1, col 1) sword_icon — A glowing steel longsword icon, top-down view, ornate hilt
2. (row 1, col 2) shield_icon — A round wooden shield icon, top-down view, iron rim
3. (row 1, col 3) staff_icon — A magical wooden staff icon with glowing crystal tip, top-down
4. (row 2, col 1) bow_icon — A wooden longbow icon with bowstring, top-down view
5. (row 2, col 2) potion_icon — A small red health potion bottle icon, cork stopper
6. (row 2, col 3) scroll_icon — A rolled parchment scroll icon with ribbon tie
7. (row 3, col 1) gem_icon — A faceted blue gemstone icon, sparkling
8. (row 3, col 2) key_icon — An antique brass key icon, ornate bow
9. (row 3, col 3) coin_icon — A gold coin icon with star emblem

Negative: blurry, watermark, signature, text, labels, numbers, connecting lines,
shared background, dark background, white background.
```

### codex exec 完整命令（一次性）

```bash
codex exec -s workspace-write "请使用 imagegen 系统 skill 生成 1 张透明背景画布。

# 画布规格
- 尺寸：1024x1024
- 背景：完全透明（PNG alpha 通道有效）
- 落盘：openspec/changes/05-codex-batch-art-protocol/art/raw/_merged/batch_1.png

# 画面描述（英文）
A transparent 1024x1024 canvas containing 9 separate game item icons, arranged in a 3x3 grid layout with at least 32 pixels of fully transparent padding between each icon. Cartoon fantasy art style, clean outlines, high readability. Each icon occupies roughly 200x200 pixels of visible area centered in its grid cell. No connecting lines, no shared background, no text or numbers anywhere on the canvas.

The 9 icons in row-major order (left-to-right, top-to-bottom):
1. (row 1, col 1) sword_icon — A glowing steel longsword icon, top-down view, ornate hilt
2. (row 1, col 2) shield_icon — A round wooden shield icon, top-down view, iron rim
3. (row 1, col 3) staff_icon — A magical wooden staff icon with glowing crystal tip, top-down
4. (row 2, col 1) bow_icon — A wooden longbow icon with bowstring, top-down view
5. (row 2, col 2) potion_icon — A small red health potion bottle icon, cork stopper
6. (row 2, col 3) scroll_icon — A rolled parchment scroll icon with ribbon tie
7. (row 3, col 1) gem_icon — A faceted blue gemstone icon, sparkling
8. (row 3, col 2) key_icon — An antique brass key icon, ornate bow
9. (row 3, col 3) coin_icon — A gold coin icon with star emblem

# 关键约束（必须严格遵守）
- 每个 icon 占 ~200x200 像素可见区域
- icon 之间至少 32 像素全透明 padding (alpha=0)
- 不要画连接线 / 装饰边框 / 共享背景
- 不要画文字 / 数字 / 标签
- 按 row-major 顺序排列（这是后续切割回填资源名的依据）

# Negative
blurry, watermark, signature, text, labels, numbers, connecting lines, shared background, dark background, white background.

# 完成标准 — 必须返回严格 JSON（不要 markdown / 解释）
{
  \"canvas\": \"openspec/changes/05-codex-batch-art-protocol/art/raw/_merged/batch_1.png\",
  \"size_bytes\": <整数>,
  \"status\": \"ok\" | \"failed\",
  \"layout_order\": [\"sword_icon\", \"shield_icon\", \"staff_icon\", \"bow_icon\", \"potion_icon\", \"scroll_icon\", \"gem_icon\", \"key_icon\", \"coin_icon\"]
}

失败请说明原因，不要重试。"
```
