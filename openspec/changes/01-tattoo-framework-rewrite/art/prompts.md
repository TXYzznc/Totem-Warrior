美术素材状态: 已处理（21/21 Codex 高质量）
处理日期: 2026-06-24 → 2026-06-25
执行 SKILL: codex-image-gen
输出目录: art/raw/
生成记录: art/raw/生成记录.md

# 美术提示词 — 01-tattoo-framework-rewrite Phase D

> **配套需求**：[requirements.md](./requirements.md)
> **执行 SKILL**：[codex-image-gen](../../../../.claude/skills/codex-image-gen/SKILL.md)
> **统一参数**：`size=1024x1024`（生成后由 image_resize 压到 256×256） / `quality=high` / `transparent_background=true`

> 注：所有 prompt 用英文写以提升 Codex/imagegen 模型理解度，最终图片以中文文件名归档。

---

## 公共风格基底（每条 prompt 前面都加这段）

```
A 256x256 game UI icon, flat vector graphic, dark sci-fi tattoo style,
transparent background, centered composition, no text, no signature,
clean geometric silhouette, 4px outer outline in color #7c8aa8,
high contrast, suitable for a Unity sprite atlas.
```

---

## 一、部位图标（6 张，纯白剪影 + 几何符号）

### 1.1 `part_head.png`

```
{base} A pure white profile silhouette of a stylized warrior skull facing left,
sharp triangular eye visor cut-out, minimal bone seam line on top,
no neck, no shoulders, fits inside a 200x200 inner safe-zone.
```

### 1.2 `part_torso.png`

```
{base} A pure white frontal silhouette of a chest plate / torso armor,
central cross-shaped engraving, three pairs of rib lines on each side,
no head, no arms, no legs, geometric and symmetric.
```

### 1.3 `part_left_arm.png`

```
{base} A pure white silhouette of a flexed left arm with closed fist,
elbow pointing down-left, biceps visible, knuckles wrapped in a hand-wrap,
no shoulder, no torso, oriented like an icon (vertical).
```

### 1.4 `part_right_arm.png`

```
{base} A pure white silhouette of a right arm gripping a vertical sword hilt,
forearm horizontal, fist clenched on guard, blade extending up but cut at frame top,
mirror of left arm, geometric and symmetric.
```

### 1.5 `part_left_leg.png`

```
{base} A pure white silhouette of a left leg mid-step forward,
knee slightly bent, foot toe-down, two soft dashed motion-trail lines
behind the calf hinting at a dodge. No torso, no other limbs.
```

### 1.6 `part_right_leg.png`

```
{base} A pure white silhouette of a right leg in mid-run forward,
knee high, foot forward, a long curved sprint streak trailing behind heel.
Mirror of left leg, geometric and symmetric.
```

---

## 二、颜色徽章（7 张，圆形 + 渐变 + 中央元素符号）

### 公共圆形模板

```
A circular 256x256 game element badge, 4px outer ring outline #7c8aa8,
inner radial gradient from {INNER} center to {OUTER} edge,
centered glyph in pure white with subtle inner glow,
transparent background outside the circle, no text, no signature.
```

### 2.1 `color_red.png` — Fire

```
{circle base, INNER=#ffd07a, OUTER=#c93f1c} centered glyph is a stylized flame
with three rising tongues, slight upward motion.
```

### 2.2 `color_yellow.png` — Lightning

```
{circle base, INNER=#fff58a, OUTER=#d8a020} centered glyph is a sharp
Z-shaped lightning bolt with one small branching fork.
```

### 2.3 `color_green.png` — Nature/Poison

```
{circle base, INNER=#b6f0a4, OUTER=#2c7a3c} centered glyph is a single
downward-falling droplet with a leaf-vein detail inside.
```

### 2.4 `color_blue.png` — Frost

```
{circle base, INNER=#cfeaff, OUTER=#1f5f9a} centered glyph is a six-pointed
snowflake with delicate triangular tips.
```

### 2.5 `color_purple.png` — Mutation

```
{circle base, INNER=#d8b6ff, OUTER=#6a2f9a} centered glyph is a swirling
spiral made of two intertwined ribbons forming a yin-yang style vortex.
```

### 2.6 `color_gold.png` — Holy

```
{circle base, INNER=#ffe9a8, OUTER=#b07a16} centered glyph is a slim cross
surrounded by a radiant 8-ray halo, soft sun-burst rim.
```

### 2.7 `color_white.png` — Pure / Light

```
{circle base, INNER=#ffffff, OUTER=#c6d6ff} centered glyph is a multi-faceted
crystalline diamond with internal prism lines, pure white core, hint of cyan rim.
```

---

## 三、图案符号（8 张，白色线条几何）

### 公共线条模板

```
A 256x256 game pattern glyph, pure white #ffffff strokes of 6px width,
on transparent background, 4px outer hint outline #7c8aa8,
clean geometric, no fill, no shading, centered.
```

### 3.1 `pattern_line.png`

```
{line base} A single bold vertical bar with a small arrowhead tip at top
and a notch at bottom, expressing focused single-target strike.
```

### 3.2 `pattern_ring.png`

```
{line base} Two concentric circles with four short radial spokes pointing
outward at 0°/90°/180°/270°, expressing an AOE burst.
```

### 3.3 `pattern_spiral.png`

```
{line base} An archimedean spiral of 5 loops growing outward from the center,
with a tiny dot marker at the center.
```

### 3.4 `pattern_zigzag.png`

```
{line base} A 4-segment polyline going down-right, up-right, down-right, up-right,
forming a sharp lightning-like zigzag, both ends slightly tapered.
```

### 3.5 `pattern_bolt.png`

```
{line base} A jagged chained lightning bolt with two visible side forks,
overall flowing from top-left to bottom-right, expressing a chain-jump effect.
```

### 3.6 `pattern_star.png`

```
{line base} A clean five-pointed star outline with one dashed inner star
slightly offset, expressing probability and burst.
```

### 3.7 `pattern_stream.png`

```
{line base} Three parallel sweeping curved lines flowing from left to right
like a swift current, evenly spaced, slightly tapered tails.
```

### 3.8 `pattern_beast.png`

```
{line base} Three large claw-mark slashes diagonally crossing the canvas,
top-left to bottom-right, with a small simplified beast eye marker
in the upper-right corner.
```

---

## 四、生成调用顺序（codex-image-gen 调度）

| 顺序 | 文件 | 备注 |
|---|---|---|
| 1-7 | `color_*.png` | 先做颜色徽章，确定调色板与圆形模板（最容易统一） |
| 8-15 | `pattern_*.png` | 几何最简单，确认 stroke 宽度 |
| 16-21 | `part_*.png` | 剪影最复杂，最后做以便参考前两组色调 |

每生成一张写入 `art/raw/生成记录.md` 一行：`| 文件 | 状态 | 时间 | 备注 |`

---

## 五、失败降级

如 Codex 模型不可用或生成超时：
1. 在 `art/raw/` 写入对应的纯色 256x256 PNG（用 Python PIL 现场生成）
2. 文件名规则不变，标记 `[降级]` 状态在生成记录中
3. 继续推进后续工程接入，可在 Codex 可用后回填
