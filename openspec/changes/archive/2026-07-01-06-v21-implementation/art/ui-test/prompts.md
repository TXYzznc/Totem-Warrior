# UI 美术资源测试样本 — Prompts

> 风格统一前缀：`STYLE_BASE` = "A Hades-style 2.5D game UI asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the edges, designed for transparent overlay use, centered square framing, transparent background."
>
> 通用负面前缀：`NEG_BASE` = "no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, photorealistic, gradient mesh"

---

## L2 合图模式：16 张 UI 组件 → 1 张 1024×1024 4×4 sheet

```json
[
  {
    "file": "raw/ui/button_normal.png",
    "size": "256x256",
    "transparent": true,
    "prompt": "STYLE_BASE A wooden game UI button, horizontal rectangle 220x72 pixels area centered, dark oak wood texture with brass corner rivets, subtle warm brown highlights, slightly raised 3D look, idle resting state, no glow, no text.",
    "negative": "NEG_BASE, text, label, glow, hover effect"
  },
  {
    "file": "raw/ui/button_hover.png",
    "size": "256x256",
    "transparent": true,
    "prompt": "STYLE_BASE Same wooden game UI button as button_normal but in hover state, **add bright golden glow halo around the entire edge** (2-3 pixel rim light), brighter brass rivets, slightly lifted feel, no text.",
    "negative": "NEG_BASE, text, label, pressed-down look"
  },
  {
    "file": "raw/ui/button_pressed.png",
    "size": "256x256",
    "transparent": true,
    "prompt": "STYLE_BASE Same wooden game UI button as button_normal but in pressed state, **darkened by 15%**, **inner shadow at top edge** (cast from rim), slightly sunken 3D look, dim brass rivets, no glow, no text.",
    "negative": "NEG_BASE, text, label, glow, raised look"
  },
  {
    "file": "raw/ui/button_disabled.png",
    "size": "256x256",
    "transparent": true,
    "prompt": "STYLE_BASE Same wooden game UI button as button_normal but in disabled state, **fully desaturated to grayscale**, **60% opacity translucent**, flat lighting, no rivets shine, no text, looks dim and inactive.",
    "negative": "NEG_BASE, text, label, color, glow, vibrant"
  },
  {
    "file": "raw/ui/iconframe_common.png",
    "size": "256x256",
    "transparent": true,
    "prompt": "STYLE_BASE A common-rarity icon frame, square 192x192 pixels area centered, iron-grey weathered metal border 4 pixels thick, rounded inner corners radius 4 pixels, plain dark hollow center (icon slot), no gem decoration, common tier visual weight.",
    "negative": "NEG_BASE, text, content inside slot, gem, jewel, glow"
  },
  {
    "file": "raw/ui/iconframe_rare.png",
    "size": "256x256",
    "transparent": true,
    "prompt": "STYLE_BASE A rare-rarity icon frame, square 192x192 pixels area centered, **polished blue steel border** 4 pixels thick, rounded inner corners, plain dark hollow center, **subtle blue inner glow halo** around inner edge, a tiny blue gem at top center of frame, rare tier visual weight.",
    "negative": "NEG_BASE, text, content inside slot, gold, purple"
  },
  {
    "file": "raw/ui/iconframe_epic.png",
    "size": "256x256",
    "transparent": true,
    "prompt": "STYLE_BASE An epic-rarity icon frame, square 192x192 pixels area centered, **deep purple polished metal border** 4 pixels thick with **engraved arcane filigree pattern**, rounded inner corners, plain dark hollow center, **purple inner glow halo + outer aura** around frame, two purple gems at top corners.",
    "negative": "NEG_BASE, text, content inside slot, blue, gold"
  },
  {
    "file": "raw/ui/iconframe_legendary.png",
    "size": "256x256",
    "transparent": true,
    "prompt": "STYLE_BASE A legendary-rarity icon frame, square 192x192 pixels area centered, **ornate gold metal border** with intricate scrollwork, **strong golden glow halo + radiating sparks/embers**, rounded inner corners, plain dark hollow center, **large central gem at top edge of frame**, maximum visual weight, divine luxury feel.",
    "negative": "NEG_BASE, text, content inside slot, simple, plain"
  },
  {
    "file": "raw/ui/progressbar_bg.png",
    "size": "256x256",
    "transparent": true,
    "prompt": "STYLE_BASE A progress bar background trough, horizontal rectangle 220x32 pixels area centered, dark recessed groove with subtle inner shadow (carved into surface look), dark grey-brown color, brass end caps, designed for 9-slice horizontal stretching, no fill content, empty state.",
    "negative": "NEG_BASE, text, fill content, vertical, square"
  },
  {
    "file": "raw/ui/progressbar_fill_hp_green.png",
    "size": "256x256",
    "transparent": true,
    "prompt": "STYLE_BASE A progress bar fill segment for HP healthy state, horizontal rectangle 220x24 pixels area centered, **vibrant saturated green** liquid energy, smooth top highlight rim, slight upward shimmer, **uniform color along horizontal axis** (designed for 9-slice stretching), no fade at ends.",
    "negative": "NEG_BASE, text, background trough, gradient ends, red, yellow"
  },
  {
    "file": "raw/ui/progressbar_fill_hp_red.png",
    "size": "256x256",
    "transparent": true,
    "prompt": "STYLE_BASE A progress bar fill segment for HP critical state, horizontal rectangle 220x24 pixels area centered, **deep blood red** with **dark crack/fissure texture overlay**, glowing red rim, threatening urgent feel, uniform color along horizontal axis (designed for 9-slice stretching).",
    "negative": "NEG_BASE, text, background trough, green, healthy look"
  },
  {
    "file": "raw/ui/progressbar_fill_cd_radial.png",
    "size": "256x256",
    "transparent": true,
    "prompt": "STYLE_BASE A skill cooldown radial overlay, circular 192x192 pixels area centered, **semi-transparent white sweeping clock-hand shape** covering one quarter (90 degrees from top, like a pie slice), soft edge anti-aliased, designed to mask a skill icon underneath, no border, no text.",
    "negative": "NEG_BASE, text, icon underneath, full circle, square"
  },
  {
    "file": "raw/ui/modaloverlay_panel_bg.png",
    "size": "256x256",
    "transparent": true,
    "prompt": "STYLE_BASE A modal overlay panel background, square 240x240 pixels area centered, **dark warm brown leather texture** with **brass rivets at four corners**, slight inner darkening (vignette feel), rounded outer corners radius 8 pixels, designed for 9-slice stretching to larger panels, semi-translucent enough to suggest dark UI background.",
    "negative": "NEG_BASE, text, content inside, light color, sharp corners"
  },
  {
    "file": "raw/ui/cardpanel_idle.png",
    "size": "256x256",
    "transparent": true,
    "prompt": "STYLE_BASE A three-choice card panel in idle state, vertical rectangle 200x240 pixels area centered, dark parchment/scroll backing with subtle wood frame border, idle resting state without highlights, dim warm tone, plain empty content area inside, no glow.",
    "negative": "NEG_BASE, text, content inside card, glow, golden border"
  },
  {
    "file": "raw/ui/cardpanel_hover.png",
    "size": "256x256",
    "transparent": true,
    "prompt": "STYLE_BASE Same three-choice card panel as cardpanel_idle but in hover state, **add bright golden glowing border lines** around the wood frame (2-3 pixel rim light), brightened overall, parchment backing slightly brighter, content area still empty, no fill glow inside.",
    "negative": "NEG_BASE, text, content inside, inner fill glow, selected"
  },
  {
    "file": "raw/ui/cardpanel_selected.png",
    "size": "256x256",
    "transparent": true,
    "prompt": "STYLE_BASE Same three-choice card panel as cardpanel_idle but in selected/confirmed state, **strong inner golden glow filling the entire card backing**, bright warm golden tone throughout, glowing border at maximum, content area still empty, decisive confirmed feel.",
    "negative": "NEG_BASE, text, content inside, dim, dark"
  }
]
```

---

## 合图布局说明（Codex 收到时按此 4×4 网格生成）

| 行 | 列 1 | 列 2 | 列 3 | 列 4 |
|---|---|---|---|---|
| 1 | button_normal | button_hover | button_pressed | button_disabled |
| 2 | iconframe_common | iconframe_rare | iconframe_epic | iconframe_legendary |
| 3 | progressbar_bg | progressbar_fill_hp_green | progressbar_fill_hp_red | progressbar_fill_cd_radial |
| 4 | modaloverlay_panel_bg | cardpanel_idle | cardpanel_hover | cardpanel_selected |

> 每格 256×256，padding 32px，合计画布 1024×1024 透明背景。
> Codex 调用 1 次 image_gen 生成上图；MCP 本地切图后按 `layout_order` 命名落到 `ui-test/raw/<name>.png`。
