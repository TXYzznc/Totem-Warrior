美术素材状态: 已处理且已确认（批次1/2/3/4 共10张，用户已确认可进入素材拆分阶段）
处理日期: 2026-06-28
执行 SKILL: codex-image-gen
输出路径: art/mockups/CombatHUDForm.png + MainMenuForm.png
生成记录: art/mockups/生成记录.md
重试轮次: 1/3（首轮通过，未触发重试）

---

# 效果图提示词 — 12-core-ui-screens

> **阶段 2 产出物**：本文件由 `art-ui` 起草，分批确认。当前批次：**批次 1（CombatHUDForm / MainMenuForm）**。
> 批次 2（CharacterSelectForm / PauseMenuForm / RunResultForm）与批次 3（TattooStudioForm / TattooEnchantForm / ShopForm / ThreeChoiceForm）待批次 1 确认后补充。

---

## Art Bible 锚定（强制继承，沿用 06-v21-implementation + 10-settings-form 已锁定风格）

### §0.1 风格定位

- **整体定位**：「似 Hades 精致 2.5D」——手绘描边 + 高对比光影 + 厚涂 + 鲜艳饱和色彩
- **画风层**：手绘描边轮廓（1-2px 等效暗色描边），厚涂，缩略尺寸仍可辨识
- **光影层**：主光源高位偏侧（30°-60°），背光 + rim light；UI 面板带轻微浮雕感
- **氛围层**：暗色基调，半透明深色背景 + 亮色文字，金色 accent 点亮焦点

### §0.2 色彩硬约束（与 SettingsForm 一致）

| 用途 | HEX |
|---|---|
| 面板/底板背景 | `#1A1C2E`（90% 不透明度） |
| 主文字 | `#F8F9FA` |
| 次级文字/标签 | `#A8A9C0` |
| Accent 金色 | `#FFB400` |
| 描边 | `#22243A` |
| 分割线 | `#2E3050` |

### §0.3 元素绑定色（HUD 专用，引用 06-v21-implementation §0.3）

| 元素 | HEX |
|---|---|
| 火焰/攻击 | `#E63946` |
| 雷电/速度 | `#F4C430` |
| 自然/治疗 | `#3DDC84` |
| 冰霜/冷却 | `#1FB6FF` |
| 异变/暴击 | `#9D4EDD` |
| 神圣/暴伤 | `#FFB400` |
| 纯能/距离 | `#F8F9FA` |

HP 条专用：normal `#3DDC84`（绿）→ warning `#F4C430`（黄,<50%）→ critical `#E63946`（红,<25%）。

---

## 页面 1：CombatHUDForm — 战斗常驻 HUD

**输出文件**：`art/mockups/CombatHUDForm.png`
**建议尺寸**：1920×1080

### 正向提示词（英文）

```
A single game combat HUD overlay mockup, full 1920x1080 resolution, dark fantasy action roguelike style inspired by Hades by Supergiant Games. The background shows a dim, blurred top-down battle arena (dark stone floor, faint ambient glow) so the HUD elements are clearly readable against it — this is an IN-GAME HUD OVERLAY, not a menu panel.

TOP-LEFT — HP BAR:
A horizontal health bar, 240x20px, anchored top-left with 16px safe-area padding. Dark recessed track (#22243A) with thick hand-painted gold-bordered frame. Fill is green (#3DDC84) at 78% indicating healthy HP, filled with subtle gradient and a thin emboss highlight on top edge. Numeric HP text "78/100" in white bold overlaid on the right end of the bar.

BELOW HP BAR — BUFF ROW:
A horizontal row of 3 small 20x20px buff icons (rounded square frames, dark background #22243A, 1px gold accent border), each showing a distinct colored glowing glyph (one fire-red #E63946 flame icon, one ice-blue #1FB6FF snowflake icon, one purple #9D4EDD crit-spark icon). Small countdown number "5" "3" "8" in tiny white text at bottom-right corner of each icon.

BOTTOM-CENTER — SKILL SLOTS Q/E:
Two large circular skill icon buttons, 56x56px each, side by side with 16px gap, anchored bottom-center. Left circle labeled "Q" shows a glowing red fireball icon, fully bright (ready state, no cooldown overlay). Right circle labeled "E" shows a blue ice-shard icon with a dark radial cooldown mask covering 40% of the circle (clockwise wipe), dimmed appearance, small countdown "2.4s" centered in white text. Both circles have thick gold-tinted hand-painted borders consistent with Hades UI style.

RIGHT OF SKILL SLOTS — WEAPON + AMMO:
A 40x40px weapon icon (a stylized hand-painted dagger, dark blade with red glow edge) directly right of the skill slots, with ammo text "∞" in bold white 24px next to it (melee weapon, infinite).

TOP-RIGHT — MINIMAP:
A circular minimap, 140x140px, anchored top-right, with thick gold ring border (#FFB400, 3px) and dark vignette mask. Inside shows simplified top-down dots representing player (white triangle, center) and a red shrinking-zone boundary circle near the edge.

BELOW MINIMAP — ZONE TIMER:
Text "缩圈 02:45" in bold white/orange (#F4C430) 18px, centered below the minimap, with a small clock icon to its left.

LEFT SIDEBAR (collapsed/compact state):
A narrow vertical translucent dark panel (#1A1C2E at 70% opacity), 200px wide, anchored left-center, spanning about 360px height, with a thin gold left-edge accent line. Contains three stacked compact sections separated by thin divider lines (#2E3050):
  - Top: "已装备纹身" header (small gold #FFB400 text) with 3 small icon rows below (small circular icons representing tattoo slots: head/arm/leg, each a small colored gem icon).
  - Middle: "被动" header with 1-2 short text lines listing an active passive name.
  - Bottom: "战斗日志" header with 3-4 short scrolling text lines in small gray/colored text (e.g. "命中 -24" in white, "击杀！" in gold, "受到伤害 -12" in red), simulating a combat log feed.

TOP-CENTER (conditional, shown for this mockup) — BOSS HP BAR:
A wide horizontal boss health bar, 400x12px, anchored top-center below the very top edge, dark frame with red (#E63946) fill at 65%, boss name "腐化守卫" in small white bold text above the bar.

OVERALL STYLE:
Hand-painted thick outline UI elements (Hades-inspired), beveled highlights top-left / shadows bottom-right on every icon and panel edge. Elements are sparse and only occupy the screen edges/corners — the CENTER of the screen must remain almost completely empty/clear to preserve battle visibility (this is critical: HUD total screen coverage should look like roughly 20-25% at most, concentrated at edges). Crisp, game-ready, highly readable at a glance.
```

### 负面提示词

```
flat design, flat vector, minimalist, material design, web UI, mobile app dashboard, light background, white panel, pastel colors, low saturation, desaturated, full-screen menu, centered modal dialog, HUD elements covering screen center, cluttered center composition, realistic photograph, watermark, signature, text artifacts, emoji, lorem ipsum, English-only labels, blurry, jpeg artifacts, misaligned elements, inconsistent style, neon cyberpunk, overlapping text, pixel art style, chibi cartoon
```

### 构图要点备注

- **核心约束**：HUD 元素必须只占屏幕边缘/角落，**屏幕中心大面积留空**（对应 GDD HUD ≤25% 屏占比红线）
- 必须同时可见：HP 条（绿色/健康态）、3 个 Buff 图标（带倒计时数字）、Q 技能（ready 态）+ E 技能（冷却中，radial 遮罩 + 倒计时数字）、武器+弹药"∞"、小地图（圆形+缩圈边界）、缩圈倒计时文字、左侧 Sidebar 折叠态（3 个分区：Build/被动/日志）、顶部 Boss HP 条
- 不要出现：暂停菜单、任何全屏面板、任何居中弹窗

### 重试策略

| 轮次 | 调整方向 |
|---|---|
| 第 1 轮 | 按上方提示词原样出图 |
| 第 2 轮 | 强调"HUD elements ONLY at screen edges, center 60% must be empty dark battlefield"；补充 reference "Hades combat HUD, Dead Cells HUD" |
| 第 3 轮 | 简化负面词，提示词中重复屏占比约束三次；增加 "extremely detailed game HUD concept art" |
| 超过 3 轮 | 停止，交回用户决定 |

---

## 页面 2：MainMenuForm — 主菜单

**输出文件**：`art/mockups/MainMenuForm.png`
**建议尺寸**：1920×1080

### 正向提示词（英文）

```
A single full-screen game main menu mockup, 1920x1080 resolution, dark fantasy action roguelike style inspired by Hades by Supergiant Games.

BACKGROUND:
A full-screen atmospheric dark illustration: a lone tattooed warrior character silhouette standing on cracked post-apocalyptic ground, dramatic high-contrast lighting from upper-side angle (30-60 degrees), deep shadow tones, with a faint glowing magic rune/tattoo pattern emanating from the character's body in mixed accent colors (gold #FFB400, red #E63946, blue #1FB6FF glow). Heavy vignette darkening the screen edges, painterly hand-painted style with visible thick brush strokes, NOT photographic.

TITLE LOGO:
Game title text positioned upper-center-left, large bold stylized CJK title text "纹身战士" (or stylized logo treatment), approximately 64-80px, white (#F8F9FA) with subtle gold (#FFB400) outer glow, hand-painted fantasy game logo typography style (thick, slightly rough edges, NOT clean corporate font).

MENU BUTTONS:
Vertically stacked list of 3 menu buttons, left-aligned, lower-left area of screen, each button ~280px wide x 56px tall, spaced 16px apart:
  - "开始游戏" (Start) — TOP button, in HIGHLIGHTED/SELECTED state: filled dark background (#252740) with a thick gold left-edge accent bar (#FFB400, 6px) and gold glow border, text in bold white #F8F9FA 24px.
  - "设置" (Settings) — middle button, IDLE state: transparent/dark background, thin gray border (#6C6E90), text in gray #A8A9C0 22px, no glow.
  - "退出游戏" (Quit) — bottom button, IDLE state: same idle style as Settings button, text #A8A9C0.

BOTTOM-RIGHT CORNER:
Small version number text "v2.1.0" in tiny gray text, bottom-right corner, 14px, low visual priority.

OVERALL STYLE:
Hand-painted thick outline + dramatic lighting, dark atmospheric mood, rich saturated accent colors against deep dark background, painterly brushwork. This must feel like a premium indie action roguelike main menu (Hades / Dead Cells tier), NOT a generic mobile game menu.
```

### 负面提示词

```
flat design, minimalist, material design, mobile app UI, light background, white panel, pastel colors, low saturation, corporate clean font, sans-serif generic title, photographic realism, photo background, watermark, signature, text artifacts, emoji, lorem ipsum, English-only title (must show Chinese), blurry, jpeg artifacts, centered button layout, buttons in screen center, cluttered UI elements, multiple panels, settings sliders visible on this screen, character select cards visible on this screen
```

### 构图要点备注

- 必须同时可见：标题 logo、3 个菜单按钮（"开始游戏"高亮选中态 + "设置"/"退出游戏" idle 态各一个），三态同屏
- 背景需要有游戏世界感染力（角色剪影+纹身发光），不能是纯色/渐变背景
- 不要出现：角色选择卡片、设置面板内容、加载进度条

### 重试策略

| 轮次 | 调整方向 |
|---|---|
| 第 1 轮 | 按上方提示词原样出图 |
| 第 2 轮 | 加强"Hades main menu reference, painterly background illustration, NOT flat color background"；减弱按钮区域描述精度避免 AI 过度居中布局 |
| 第 3 轮 | 重复关键 hex 色值；增加"professional game key art quality" |
| 超过 3 轮 | 停止，交回用户决定 |

---

## 页面 3：CharacterSelectForm — 角色选择

**输出文件**：`art/mockups/CharacterSelectForm.png`
**建议尺寸**：1920×1080

> 现状备注：脚本目前是空壳（仅 UIModule 注册，无卡片逻辑），本批效果图同时作为 client-unity 后续补实现的视觉规格依据。本期仅 1 个可选角色，骨架须预留多角色横向扩展空间。

### 正向提示词（英文）

```
A single full-screen game character select mockup, 1920x1080 resolution, dark fantasy action roguelike style inspired by Hades by Supergiant Games. Same background treatment as the main menu: dark atmospheric blurred backdrop, deep vignette, NOT flat color.

TOP OF SCREEN:
Page title "选择角色" in bold white Chinese text, 36px, top-center, with a thin gold underline accent (#FFB400, 2px) beneath it.

CENTER — CHARACTER CARD GRID:
A horizontal row of 3 character card slots using a grid layout, centered, each card ~280px wide x 420px tall, spaced 24px apart. Only the LEFTMOST card is populated (the only playable character this period); the other two card slots are shown as LOCKED placeholder cards (dark empty silhouette with a large gold lock icon centered, semi-transparent, label "敬请期待" in small gray text below).
The populated card (leftmost, SELECTED state): dark card background (#1A1C2E, 90% opacity) with a thick gold glowing border (#FFB400, 3px) indicating selection, rounded corners. Inside: a hand-painted bust portrait of the same tattooed warrior character (upper body, dramatic side lighting, glowing red/blue tattoo patterns visible on arms), character name "无名战士" in bold white 22px below the portrait, and a short one-line flavor text "唯一的幸存者" in gray #A8A9C0 14px beneath the name.

BOTTOM OF SCREEN:
Two buttons, bottom-center, horizontally arranged with 16px gap: left button "返回" (Back) in idle outlined style (transparent fill, gray #A8A9C0 border, gray text); right button "确认" (Confirm) in primary filled style (gold #FFB400 fill, dark #1A1C2E bold text), both ~160px wide x 48px tall.

OVERALL STYLE:
Hand-painted thick outline, dramatic lighting, dark atmospheric mood consistent with the main menu. Card grid feels premium and game-ready, like a roguelike character select screen (Hades / Dead Cells tier).
```

### 负面提示词

```
flat design, minimalist, material design, mobile app UI, light background, white panel, pastel colors, low saturation, photographic realism, watermark, signature, text artifacts, emoji, lorem ipsum, English-only labels, blurry, jpeg artifacts, all three cards unlocked, all three cards populated with different characters, vertical card list, single column layout, settings content visible
```

### 构图要点备注

- 必须同时可见：1 张已选中（高亮金边）角色卡 + 2 张锁定占位卡（灰态+锁图标），三态布局体现"本期 1 角色，预留扩展"
- 角色卡内容需与 MainMenuForm 同一角色形象保持一致（同一纹身战士）
- 不要出现：多角色已解锁画面、设置/暂停内容

### 重试策略

| 轮次 | 调整方向 |
|---|---|
| 第 1 轮 | 按上方提示词原样出图 |
| 第 2 轮 | 强调"only leftmost card unlocked, middle and right cards MUST show lock icon and be visibly dimmed/grayed" |
| 第 3 轮 | 简化卡片数量描述为"exactly 3 card slots in a row"，重复锁定态描述 |
| 超过 3 轮 | 停止，交回用户决定 |

---

## 页面 4：PauseMenuForm — 暂停菜单

**输出文件**：`art/mockups/PauseMenuForm.png`
**建议尺寸**：1920×1080

### 正向提示词（英文）

```
A single full-screen game pause menu overlay mockup, 1920x1080 resolution, dark fantasy action roguelike style inspired by Hades by Supergiant Games.

BACKGROUND:
A heavily blurred and darkened version of the combat HUD battle scene behind (dark stone arena, faint glow), with a semi-transparent black overlay (#1A1C2E at 70% opacity) covering the entire screen to dim the background and focus attention on the pause menu.

CENTER MENU:
A vertical stack of exactly 3 menu buttons, centered both horizontally and vertically on screen, each button ~320px wide x 64px tall, spaced 20px apart:
  - "继续" (Resume) — TOP button, HIGHLIGHTED state: filled dark background (#252740) with thick gold left-edge accent bar (#FFB400, 6px) and subtle gold glow border, text bold white #F8F9FA 26px.
  - "设置" (Settings) — middle button, IDLE state: transparent background, thin gray border (#6C6E90), text gray #A8A9C0 24px.
  - "退出游戏" (Quit) — bottom button, IDLE state: same idle style, text #A8A9C0.
Above the three buttons, a small page title "暂停" in bold white 32px with thin gold underline.

OVERALL STYLE:
Hand-painted thick outline UI consistent with main menu and settings panel. The pause menu should feel like a lightweight overlay (NOT a full new background illustration) — the dimmed battle scene must remain vaguely visible behind the dark overlay to communicate "paused mid-game", not "returned to main menu".
```

### 负面提示词

```
flat design, minimalist, material design, mobile app UI, light background, white panel, pastel colors, photographic realism, watermark, signature, text artifacts, emoji, lorem ipsum, English-only labels, blurry, jpeg artifacts, solid opaque black background with no battle scene visible, new illustrated background art, character portrait, more than 3 buttons, settings sliders visible on this screen
```

### 构图要点备注

- 必须同时可见：继续（高亮）/设置（idle）/退出游戏（idle）三态按钮
- 背景必须能看出是"模糊变暗的战斗场景"，而不是新背景插画或纯黑
- 不要出现：角色立绘、设置面板具体内容

### 重试策略

| 轮次 | 调整方向 |
|---|---|
| 第 1 轮 | 按上方提示词原样出图 |
| 第 2 轮 | 强调"this is a translucent overlay on TOP OF a blurred game scene, not a new painted background" |
| 第 3 轮 | 简化背景描述，重复"semi-transparent dark overlay over blurred battle arena" |
| 超过 3 轮 | 停止，交回用户决定 |

---

## 页面 5：RunResultForm — 单局结算

**输出文件**：`art/mockups/RunResultForm.png`
**建议尺寸**：1920×1080

> 现状备注：脚本目前是空壳，本批效果图同时作为 client-unity 后续补实现（杀敌数/存活时长/Build 快照展示逻辑）的视觉规格依据。

### 正向提示词（英文）

```
A single full-screen game run-result / end-of-run summary mockup, 1920x1080 resolution, dark fantasy action roguelike style inspired by Hades by Supergiant Games.

BACKGROUND:
Dark atmospheric background, heavy vignette, similar mood to main menu but more subdued/somber tone (muted red-orange glow suggesting end of a run, NOT victorious gold).

TOP OF SCREEN:
Large result title centered, bold text "本局结束" in white #F8F9FA 48px, with a smaller subtitle below "存活 08:32" (survived time) in gray #A8A9C0 20px.

LEFT HALF — STATS PANEL:
A dark semi-transparent card panel (#1A1C2E, 85% opacity, rounded corners, thin gold border) containing a vertical list of 4 stat rows, each with a small icon + label + value, separated by thin divider lines (#2E3050):
  - Sword icon + "击杀数" label + "23" value (white bold 22px)
  - Skull icon + "死因" label + "腐化守卫" value
  - Coin icon + "获得金币" label + "340" value
  - Map icon + "到达层数" label + "第 3 层" value

RIGHT HALF — BUILD SNAPSHOT PANEL:
A dark semi-transparent card panel matching left panel style, titled "纹身 Build" in gold #FFB400 22px at top, containing a grid of 6 small tattoo slot icons (3x2 grid, each a small circular gem-style icon in different element colors: red flame, blue snowflake, green leaf, purple spark, gold sun, white star), each icon ~48px with a thin gold ring border, arranged neatly with even spacing.

BOTTOM OF SCREEN:
A single primary button, bottom-center, "返回主菜单" (Return to Main Menu) text, gold filled background (#FFB400), dark bold text #1A1C2E, ~240px wide x 56px tall.

OVERALL STYLE:
Hand-painted thick outline, consistent dark fantasy aesthetic, somber but readable end-of-run summary screen, premium roguelike quality (Hades death/run-end screen tier).
```

### 负面提示词

```
flat design, minimalist, material design, mobile app UI, light background, white panel, pastel colors, victorious golden celebration tone, confetti, photographic realism, watermark, signature, text artifacts, emoji, lorem ipsum, English-only labels, blurry, jpeg artifacts, more than one bottom button, settings content visible, main menu character art visible
```

### 构图要点备注

- 必须同时可见：标题+存活时长、左侧 4 项统计、右侧 6 格纹身 Build 快照、底部单一返回按钮
- 氛围应区别于主菜单：更低沉色调，而非主菜单的英雄感
- 不要出现：庆祝特效/彩带（本游戏死亡/撤离都走同一结算流程，不强调"胜利"）

### 重试策略

| 轮次 | 调整方向 |
|---|---|
| 第 1 轮 | 按上方提示词原样出图 |
| 第 2 轮 | 强调"somber muted tone, NOT celebratory, NOT golden victory screen" |
| 第 3 轮 | 简化为"two side-by-side card panels with stats and tattoo grid", 重复色板约束 |
| 超过 3 轮 | 停止，交回用户决定 |

---

## 页面 6：TattooStudioForm — 纹身师工作台

**输出文件**：`art/mockups/TattooStudioForm.png`
**建议尺寸**：1920×1080（800×600 居中覆盖层，置于全屏战斗背景上）

> 现状备注：脚本已有完整开关/事件逻辑，但 Build 预览区是占位（`RefreshBuildPreview` 留 TODO），本图同时作为后续接入 TattooModule 槽位数据的视觉规格。

### 正向提示词（英文）

```
A single game UI mockup showing a centered modal overlay panel on top of a blurred dark battle background, 1920x1080 resolution, dark fantasy action roguelike style inspired by Hades by Supergiant Games.

BACKGROUND:
Heavily blurred dark battle arena (same mood as combat HUD), with a semi-transparent black scrim (#1A1C2E, 60% opacity) covering the full screen behind the panel.

CENTER PANEL — TATTOO STUDIO:
A centered panel approximately 800x600px (rendered proportionally larger here for clarity, roughly 42% width x 56% height of the 1920x1080 canvas), dark background (#1A1C2E, 90% opacity), rounded corners, thick gold border glow (#FFB400). Panel title "纹身师工作台" Chinese text in bold white 28px top-left with close button [X] top-right.

LEFT SIDE OF PANEL — BODY SLOT DIAGRAM:
A simplified humanoid silhouette diagram (front-facing, stylized icon-like body outline) showing 6 tattoo slot markers (head, left arm, right arm, torso, left leg, right leg), each slot a small circular gold-ringed socket. 3 of the 6 sockets are filled with small glowing colored gem icons (one red flame on right arm, one blue snowflake on left leg, one green leaf on torso); the other 3 sockets are empty dark circles awaiting equip.

RIGHT SIDE OF PANEL — PROGRESS / READ BAR:
A horizontal progress bar (ProgressBar component), 280px wide, 16px tall, dark track (#3A3C58) with gold fill (#FFB400) at 55%, labeled "附魔读条" Chinese text above it in gray #A8A9C0 16px. Below the bar, a row of 3 small option icon buttons representing enchant choices (each a small square card with a colored rune icon).

BOTTOM-RIGHT CORNER OF PANEL — DEATH CHEST MAP HINT:
A small circular minimap thumbnail (~80px diameter) in the bottom-right of the panel, with a single red glowing dot marker labeled small text "死亡宝箱" Chinese text beside it.

OVERALL STYLE:
Hand-painted thick outline, consistent dark fantasy UI aesthetic, premium roguelike crafting/customization screen feel (similar to a tattoo/rune customization interface in an action RPG).
```

### 负面提示词

```
flat design, minimalist, material design, mobile app UI, light background, white panel, pastel colors, photographic realism, watermark, signature, text artifacts, emoji, lorem ipsum, English-only labels, blurry, jpeg artifacts, full-screen panel covering entire screen, no background scene visible, shop inventory grid visible, three-choice cards visible
```

### 构图要点备注

- 必须同时可见：人形 6 槽示意图（3 已装备+3 空槽）、附魔读条（filling 态，约 55%）、死亡宝箱地图提示
- 面板为居中覆盖层，**不能**占满全屏，背景战斗场景需可见（模糊+暗化）
- 不要出现：商店内容、三选一卡片

### 重试策略

| 轮次 | 调整方向 |
|---|---|
| 第 1 轮 | 按上方提示词原样出图 |
| 第 2 轮 | 强调"panel occupies only center ~45% of screen, background battle scene MUST remain visible around edges" |
| 第 3 轮 | 简化人形示意图描述为"simple humanoid icon with 6 circular slot markers"，重复槽位填充比例 |
| 超过 3 轮 | 停止，交回用户决定 |

---

## 页面 7：TattooEnchantForm — 纹身附魔

**输出文件**：`art/mockups/TattooEnchantForm.png`
**建议尺寸**：1920×1080（700×500 居中覆盖层，嵌套于 TattooStudio 流程内）

> 现状备注：脚本目前是空壳，本图作为后续实现的视觉规格依据。

### 正向提示词（英文）

```
A single game UI mockup showing a centered modal overlay panel on top of a blurred dark battle background, 1920x1080 resolution, dark fantasy action roguelike style inspired by Hades by Supergiant Games.

BACKGROUND:
Same heavily blurred dark scrim treatment as other modal overlays (#1A1C2E, 60% opacity over blurred battle scene).

CENTER PANEL — ENCHANT:
A centered panel approximately 700x500px (roughly 36% width x 46% height of canvas), dark background (#1A1C2E, 90% opacity), rounded corners, gold border glow. Panel title "纹身附魔" Chinese text bold white 26px top-left, close button [X] top-right.

CENTER OF PANEL — ENCHANT READ BAR (FILLING STATE):
A large horizontal progress bar, 400px wide x 24px tall, centered in the panel, dark track with gold fill at approximately 60%, animated-looking fill edge with a subtle bright highlight at the fill boundary (suggesting active filling motion). Below it, small text "附魔中..." Chinese text in gray #A8A9C0 16px.

BELOW READ BAR — DISABLED OPTION BUTTONS:
A row of 3 enchant option cards, each ~140x100px, grayed out / dimmed appearance (semi-transparent overlay, #1A1C2E at higher opacity, slightly desaturated icons) to indicate they are currently locked/disabled during the filling process. Each card shows a small colored rune icon (gold sun, purple spark, blue snowflake) with a short label below.

OVERALL STYLE:
Hand-painted thick outline, consistent dark fantasy UI aesthetic, conveys an active "enchanting in progress, please wait" state clearly through the read bar and disabled buttons.
```

### 负面提示词

```
flat design, minimalist, material design, mobile app UI, light background, white panel, pastel colors, photographic realism, watermark, signature, text artifacts, emoji, lorem ipsum, English-only labels, blurry, jpeg artifacts, full-screen panel, option buttons in bright enabled state, success or fail animation effects, body slot diagram visible
```

### 构图要点备注

- 必须同时可见：读条 filling 态（约 60%）+ 3 个禁用态选项卡（视觉灰化，呼应"读条期间按钮全部锁定"）
- 不要出现：成功/失败动效（success 绿色脉冲 / fail 抖动属于动态效果，静态 mockup 不表现）
- 面板为居中覆盖层，背景需可见

### 重试策略

| 轮次 | 调整方向 |
|---|---|
| 第 1 轮 | 按上方提示词原样出图 |
| 第 2 轮 | 强调"the 3 option cards MUST look visibly disabled/grayed, NOT vibrant/clickable" |
| 第 3 轮 | 简化描述，重复"reading bar filling + disabled buttons" 核心 |
| 超过 3 轮 | 停止，交回用户决定 |

---

## 页面 8：ShopForm — 商人交易

**输出文件**：`art/mockups/ShopForm.png`
**建议尺寸**：1920×1080（700×500 居中覆盖层）

> 现状备注：脚本已有完整开关/事件逻辑（含金币文本绑定），但库存格内容是占位（`RefreshInventory` 留 TODO），本图同时作为后续接入 ShopModule 库存数据的视觉规格。

### 正向提示词（英文）

```
A single game UI mockup showing a centered modal overlay panel on top of a blurred dark battle background, 1920x1080 resolution, dark fantasy action roguelike style inspired by Hades by Supergiant Games.

BACKGROUND:
Same heavily blurred dark scrim treatment (#1A1C2E, 60% opacity over blurred battle/NPC scene).

CENTER PANEL — SHOP:
A centered panel approximately 700x500px (roughly 36% width x 46% height of canvas), dark background (#1A1C2E, 90% opacity), rounded corners, gold border glow. Panel title "商人" Chinese text bold white 26px top-left, a gold coin icon plus numeric text "340" (current gold amount) top-right next to the close button [X].

CENTER OF PANEL — INVENTORY GRID:
A grid layout of 6 item slots arranged 3 columns x 2 rows, each slot ~96x96px, dark recessed background (#22243A) with thin gold border, spaced 16px apart. Each slot contains a distinct hand-painted item icon (a potion bottle, a dagger, a scroll, a tattoo ink vial glowing red, a shield, a ring) with a small price tag badge at the bottom-right corner of each slot showing a gold coin icon and a number (e.g. "50", "120", "80", "200", "150", "90").

BOTTOM OF PANEL — REFRESH BUTTON:
A small secondary button bottom-center inside the panel, "刷新商品" Chinese text, outlined idle style, ~140px wide x 40px tall.

OVERALL STYLE:
Hand-painted thick outline, consistent dark fantasy UI aesthetic, premium roguelike shop screen (Hades charon shop / Dead Cells merchant tier).
```

### 负面提示词

```
flat design, minimalist, material design, mobile app UI, light background, white panel, pastel colors, photographic realism, watermark, signature, text artifacts, emoji, lorem ipsum, English-only labels, blurry, jpeg artifacts, full-screen panel, body slot diagram visible, three-choice cards visible, empty inventory grid
```

### 构图要点备注

- 必须同时可见：6 格库存网格（每格不同图标+价格标签）、金币数显示、刷新商品按钮
- 不要出现：纹身工作台内容、三选一卡片
- 面板为居中覆盖层，背景需可见

### 重试策略

| 轮次 | 调整方向 |
|---|---|
| 第 1 轮 | 按上方提示词原样出图 |
| 第 2 轮 | 强调"each of the 6 slots MUST show a distinct item icon and a visible price tag" |
| 第 3 轮 | 简化为"3x2 grid of item icons with price badges"，重复网格规格 |
| 超过 3 轮 | 停止，交回用户决定 |

---

## 页面 9：ThreeChoiceForm — 宝箱三选一

**输出文件**：`art/mockups/ThreeChoiceForm.png`
**建议尺寸**：1920×1080（900×400 居中覆盖层，强制选择）

> 现状备注：脚本目前是空壳（仅 IExclusiveUIForm 接口+3s 防误触锁的访问器），本图作为后续实现的视觉规格依据。

### 正向提示词（英文）

```
A single game UI mockup showing a centered modal overlay panel on top of a blurred dark battle background, 1920x1080 resolution, dark fantasy action roguelike style inspired by Hades by Supergiant Games.

BACKGROUND:
Same heavily blurred dark scrim treatment (#1A1C2E, 60% opacity over blurred battle scene).

TOP OF SCREEN:
Small page title "选择一项奖励" Chinese text in bold white 28px, top-center, with a thin gold underline.

CENTER — THREE CARD CHOICES (HORIZONTAL ROW):
A horizontal row of exactly 3 card panels, centered, each card ~260px wide x 340px tall, spaced 32px apart:
  - LEFT card: idle state, dark background (#1A1C2E, 90% opacity), thin gray border, contains a large icon of a glowing red tattoo ink vial in upper portion, item name "烈焰纹身" Chinese text bold white below, short description "火焰伤害 +15%" Chinese text gray below that.
  - MIDDLE card: HOVER/highlighted state, slightly larger scale, gold glowing border (#FFB400, 3px), subtle gold inner glow, contains a glowing blue snowflake icon, item name "寒冰纹身" Chinese text, description "冷却缩减 +10%" Chinese text.
  - RIGHT card: LOCKED state (still within 3-second anti-mistouch lock window), dark background with a subtle gray semi-transparent overlay layer on top (indicating temporarily non-interactable), contains a glowing purple spark icon, item name "暴击纹身" Chinese text dimmed, description "暴击率 +8%" Chinese text dimmed, with a small countdown text "2s" Chinese-compatible numeral in the corner indicating remaining lock time.

BOTTOM:
A small hint text below the cards, centered, "锁定中，请仔细选择" Chinese text in gray #A8A9C0 14px.

OVERALL STYLE:
Hand-painted thick outline, consistent dark fantasy UI aesthetic, premium roguelike reward-choice screen (Hades boon choice / Slay the Spire card reward tier), three distinct visual states (idle / hover-selected / locked) clearly differentiable at a glance.
```

### 负面提示词

```
flat design, minimalist, material design, mobile app UI, light background, white panel, pastel colors, photographic realism, watermark, signature, text artifacts, emoji, lorem ipsum, English-only labels, blurry, jpeg artifacts, full-screen panel, more or fewer than 3 cards, all cards in identical idle state, vertical card list, close button visible (this choice cannot be skipped)
```

### 构图要点备注

- 必须同时可见：idle / hover-selected(高亮) / locked(灰化+倒计时) 三态卡片同屏，体现强制选择 + 3s 防误触锁
- **不要**出现关闭按钮（GDD 明确"必须做出选择，不可跳过"）
- 面板为居中覆盖层，背景需可见

### 重试策略

| 轮次 | 调整方向 |
|---|---|
| 第 1 轮 | 按上方提示词原样出图 |
| 第 2 轮 | 强调"NO close button anywhere on screen, this is a forced choice"；加强三态视觉差异描述 |
| 第 3 轮 | 简化为"3 reward cards: one idle, one highlighted/selected, one locked-dimmed with countdown" |
| 超过 3 轮 | 停止，交回用户决定 |

---

## 页面 10（复查新增）：SelfTattooForm — 玩家自纹身工作台

**输出文件**：`art/mockups/SelfTattooForm.png`
**建议尺寸**：1920×1080（居中覆盖层，任意地点 Tab 唤出）

> GDD 依据：`01-纹身构筑系统.md` §5.1 线框图。核心是 v2.1 最重要的新机制——玩家任意地点按 Tab 自助刻纹身，区别于 NPC 触发的 TattooStudioForm（附魔）。后端 API 已实现，本图填补一直缺失的 UI。

### 正向提示词（英文）

```
A single game UI mockup showing a centered modal overlay panel on top of a blurred dark battle background, 1920x1080 resolution, dark fantasy action roguelike style inspired by Hades by Supergiant Games.

BACKGROUND:
Heavily blurred dark battle arena scrim (#1A1C2E, 60% opacity), same treatment as other modal overlays — this panel can appear ANYWHERE in the world, not just at an NPC location.

CENTER PANEL — SELF-TATTOO WORKBENCH:
A centered panel roughly 50% width x 70% height of canvas, dark background (#1A1C2E, 92% opacity), rounded corners, thick gold border glow. Panel title "纹身工作台" Chinese text bold white 28px top-left, small hint text "(再按 Tab 关闭)" Chinese text gray small font next to title, close button [X] top-right.

LEFT SIDE — BODY PART SELECTOR:
An ABSTRACT minimalist humanoid icon diagram (simple geometric outline, NOT a detailed painted character — think simple equipment-slot diagram style, thin line strokes), front-facing, with 6 circular slot markers (head, left arm, right arm, torso, left leg, right leg). The right-arm slot is highlighted/selected with a bright gold glowing ring, the other 5 slots are dim gray rings. Below the silhouette, text "选中部位：右臂　预估读条：5.0s" Chinese text in white 16px.

RIGHT SIDE TOP — COLOR PALETTE ROW:
A horizontal row of 7 color swatch buttons (circular, ~36px each): red filled bright with small count badge "x3", yellow filled bright "x1", green DIMMED GRAY (count x0, unavailable), blue filled bright "x2", purple filled bright "x1", gold DIMMED GRAY (x0), white DIMMED GRAY (x0). Bright ones have a thin gold selection ring on the yellow swatch (currently selected).

RIGHT SIDE MIDDLE — PATTERN GRID:
A grid of 8 small pattern icon buttons (2 rows x 4 columns, ~56x56px each): a straight line icon (unlocked, bright), a ring/circle icon (unlocked, bright, currently selected with gold border), and 6 other icons (spiral, lightning, zigzag, star, wave, dragon-scale) all shown with a small gold LOCK icon overlay and dimmed/grayed appearance indicating recipe not yet unlocked.

BELOW — PREVIEW TEXT:
One line of small text: "预览：右臂 × 黄 × 直线 = 黄电单点拳" Chinese text in gold #FFB400 14px. Do NOT show any risk/penalty warning text on this screen.

BOTTOM — ACTION BUTTONS:
Two buttons bottom-center: primary filled gold button "开始绘制（5.0s 读条）" Chinese text, dark bold text, ~280px wide; secondary outlined button "取消" Chinese text, gray border, ~120px wide, 16px gap between them.

OVERALL STYLE:
Hand-painted thick outline, consistent dark fantasy crafting UI aesthetic, information-dense but readable, conveys "anywhere-accessible self-service tattoo crafting station" feel.
```

### 负面提示词

```
flat design, minimalist, material design, mobile app UI, light background, white panel, pastel colors, photographic realism, watermark, signature, text artifacts, emoji, lorem ipsum, English-only labels, blurry, jpeg artifacts, full-screen panel covering entire screen with no background visible, all pattern icons unlocked, all color swatches available, shop inventory grid visible, three-choice cards visible, npc character portrait visible, detailed painted realistic body silhouette, risk warning text, penalty text
```

### 构图要点备注

- 必须同时可见：人形 6 槽选择(右臂高亮选中，**抽象简笔风格**)、7 色颜料库存(3 个为 0 灰显)、8 图案网格(仅直线+圆环解锁，其余 6 个锁图标)、预览文字、开始绘制+取消按钮、右上角关闭按钮 [×]
- **关键区分点**：与已出图的 `TattooStudioForm.png`（NPC 附魔，左侧已装备槽展示）不同，本图强调"选择+消耗+解锁状态"的工作台交互，且人形示意图为**抽象简笔/功能性图标**而非写实厚涂角色
- **不要出现**：NPC 角色立绘、风险/惩罚提示文字（确认稿已移除该行）

### 重试策略

| 轮次 | 调整方向 |
|---|---|
| 第 1 轮 | 按上方提示词原样出图 |
| 第 2 轮 | 强调"3 of 7 color swatches MUST be visibly grayed/dimmed (zero stock)，6 of 8 pattern icons MUST show lock overlay" |
| 第 3 轮 | 简化为"body silhouette selector + color row + pattern grid + preview text + two buttons"，重复锁定/灰显比例 |
| 超过 3 轮 | 停止，交回用户决定 |
