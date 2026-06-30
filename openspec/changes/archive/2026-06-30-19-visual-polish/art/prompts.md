# art/prompts.md — 19-visual-polish

**创建**: 2026-07-01
**用途**: 供主对话直接喂给 codex-image-gen，每条即一次调用
**格式约定**: 每条提示词已包含 style / color / size / negative 字段

---

## PROMPT-01：burn 灼烧状态图标

```
pixel art game status icon, burn / fire effect icon,
16x16 pixel art style scaled to 64x64,
bright orange-red flame shape, glowing core,
main colors: #FF6622 orange-red, #FFAA44 bright yellow highlight, black outline 1px,
transparent background, no padding, no shadow, no glow blur,
centered, symmetrical silhouette, clean readable shape,
game UI icon style, flat pixel art,
negative: 3d render, photorealistic, blur, gradient, text, watermark, noise
```

**目标尺寸**: 64x64 PNG
**目标路径**: `Assets/Resources/Sprite/UI/StatusIcon/burn.png`

---

## PROMPT-02：poison 中毒状态图标

```
pixel art game status icon, poison / toxic effect icon,
16x16 pixel art style scaled to 64x64,
poison drop or skull with dripping toxic liquid,
main colors: #44BB22 poison green, #226611 dark green shadow, #AA44FF purple accent,
transparent background, no padding, no shadow, no glow blur,
1px black pixel outline, centered, compact readable shape,
game UI icon style, flat pixel art,
negative: 3d render, photorealistic, blur, gradient, text, watermark, noise
```

**目标尺寸**: 64x64 PNG
**目标路径**: `Assets/Resources/Sprite/UI/StatusIcon/poison.png`

---

## PROMPT-03：stun 眩晕状态图标

```
pixel art game status icon, stun / daze effect icon,
16x16 pixel art style scaled to 64x64,
spinning stars or spiral dizzy symbol,
main colors: #FFDD22 bright yellow, #2244FF blue accent, #FFFFFF white outline,
transparent background, no padding, no shadow, no glow blur,
1px black pixel outline, centered, compact readable shape,
dynamic rotational feel in static pixel form,
game UI icon style, flat pixel art,
negative: 3d render, photorealistic, blur, gradient, text, watermark, noise
```

**目标尺寸**: 64x64 PNG
**目标路径**: `Assets/Resources/Sprite/UI/StatusIcon/stun.png`

---

## PROMPT-04：暴击飘字视觉参考（概念图）

```
game UI concept art, damage number popup floating text reference,
dark game background, two damage numbers floating upward,
LEFT: small white bold number "83" rising gently,
RIGHT: large bright red bold number "CRIT 247" with slight outline, larger font, rising faster,
red number has 1px dark outline for readability, slight upward motion blur effect,
flat stylized game HUD art style, no fancy 3d,
color palette: white #FFFFFF for normal, bright red #FF2222 for crit,
negative: photorealistic, 3d render, watermark, logo, frame, border
```

**目标尺寸**: 512x256 PNG（横版概念图）
**目标路径**: `openspec/changes/19-visual-polish/art/raw/crit_text_ref.png`

---

## PROMPT-05：hitspark 粒子参考（概念图）

```
game VFX concept art, hit spark particle effect reference,
dark background, radial burst of small white and orange-red sparks,
center bright white flash, outer sparks color #FF6622 orange-red,
8-14 small spark lines radiating outward from center, short trail,
crisp flat 2D style, suitable for top-down action game,
clean simple shape, high contrast against dark background,
negative: photorealistic, 3d render, fire, explosion, smoke, lens flare, watermark
```

**目标尺寸**: 512x512 PNG
**目标路径**: `openspec/changes/19-visual-polish/art/raw/hitspark_ref.png`

---

## 调用顺序

主对话调 codex-image-gen 时按以下顺序执行（优先级高的先出）：

1. PROMPT-01（burn）→ 存 `Assets/Resources/Sprite/UI/StatusIcon/burn.png`
2. PROMPT-02（poison）→ 存 `Assets/Resources/Sprite/UI/StatusIcon/poison.png`
3. PROMPT-03（stun）→ 存 `Assets/Resources/Sprite/UI/StatusIcon/stun.png`
4. PROMPT-04（暴击飘字参考）→ 存 `openspec/changes/19-visual-polish/art/raw/crit_text_ref.png`
5. PROMPT-05（hitspark 参考）→ 存 `openspec/changes/19-visual-polish/art/raw/hitspark_ref.png`

每张出完后更新 `art/requirements.md` 对应行的状态字段。
若额度不足：PROMPT-04 和 PROMPT-05 为参考图可跳过，不影响功能实现。
