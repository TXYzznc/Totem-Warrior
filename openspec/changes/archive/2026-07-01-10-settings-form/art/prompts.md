# SettingsForm 效果图提示词

美术素材状态: 已处理（阶段 3 第 1 轮一次通过，等用户确认）
日期: 2026-07-01
关联 layout: openspec/changes/10-settings-form/art/prefab-layout.md
关联 mockup: openspec/changes/10-settings-form/art/mockups/SettingsForm.png（1920×1080, 1.13 MB）
生成记录: openspec/changes/10-settings-form/art/mockups/生成记录.md
执行 SKILL: codex-image-gen (dispatch_l1 MCP)
重试轮次: 1 / 3

---

## §0 Art Bible 锚定

**风格定位**：似 Hades 精致 2.5D——暗色调为主、金色 Accent 点睛、面板带轻微厚度感与内发光；整体氛围神秘肃穆，细节精致但不过度繁复。

**配色 Hex 表**（直接从 layout §配色系统复制，阶段出图严格使用）：

| 语义 | Hex | 用途 |
|---|---|---|
| 面板背景 | `#1A1C2E` @ 90% Alpha | PanelBg |
| 主文字 | `#F8F9FA` | 标题 / Row Label / 数值 |
| 次级文字 | `#A8A9C0` | SectionHeader / 副标题 |
| Accent 金色 | `#FFB400` | SaveButton / Slider 填充 / Radio 选中点 |
| 描边 | `#22243A` | 面板边框 / TitleBar / Footer 背景 |
| 分割线 | `#2E3050` | SectionDivider / CancelButton 背景 |
| Slider 轨道 | `#3A3C58` | Slider 背景轨道 / KeyBindButton disabled |
| Slider 填充 | `#FFB400` | Slider 已填充区（与 Accent 金色一致） |
| 按钮次级（取消） | `#2E3050` | CancelButton 背景 |
| 按钮禁用 | `#3A3C58` @ 60% Alpha | KeyBindButton disabled 态 |

**字体**：无衬线粗体用于标题和 SectionHeader，常规体用于 Row 标签，斜体细体用于副标题（「即将推出」）。字色严格按上表，不引入额外颜色。

---

## §1 结构约束

**画布总尺寸**：1920 × 1080 px（Canvas 基准分辨率）

**面板位置与尺寸**：居中，1056 × 810 px（宽占画布 55%，高占画布 75%）；面板外周围区域（画布边缘到面板边缘的四周留白）填充**纯绿色 `#00FF00`**，用于阶段 4 ui-asset-splitting chroma_key 抠底。

**面板内各区域占比**（基于面板高度 810px）：

| 区域 | 宽占面板宽 | 高占面板高 | 像素高 | 位置 |
|---|---|---|---|---|
| TitleBar | 100% | 8.9% | 72 | 顶部 |
| ContentArea（内边距后有效内容宽 93.9%） | 100% | 81.2% | 658 | TitleBar 下方 |
| — Section_Volume | 93.9% | 24.2% | 196 | ContentArea 顶部，含 2 个 Slider Row |
| — Section_Quality | 93.9% | 14.8% | 120 | Section_Volume 下方，含 3 个 Radio |
| — Section_KeyBinding | 93.9% | 31.6% | 256 | Section_Quality 下方，含 3 个 KeyBindButton |
| Footer | 100% | 9.9% | 80 | 底部 |

**关键坐标（相对面板左上角原点，Y 向下）**：
- TitleBar：y=0, h=72；标题文字「设置」距左 32px，字号 32，粗体，白色；关闭 X 按钮 52×52 距右 16px
- Section_Volume 顶：y=96（72 TitleBar + 24 内边距）
- Section_Quality 顶：y=301（96+196+9 分割线区域）
- Section_KeyBinding 顶：y=430（301+120+9）
- Footer 顶：y=730（810-80）；CancelButton 160×52 距右 232px；SaveButton 200×52 距右 16px

---

## §2 效果图提示词

**目标文件**：`openspec/changes/10-settings-form/art/mockups/SettingsForm.png`
**输出规格**：PNG，1920 × 1080，面板外纯绿色 `#00FF00` 背景

---

A high-quality game UI mockup screenshot, resolution 1920x1080. The entire canvas background outside the central panel is filled with flat solid chroma-key green (#00FF00), no gradients, no shadows bleeding onto the green area.

Centered on canvas: a dark fantasy settings menu panel, 1056x810 pixels, styled like Hades or Pyre — polished 2.5D dark RPG UI aesthetic with subtle inner glow on edges, refined metallic accents.

**STRUCTURAL CONSTRAINTS (strictly follow these proportions):**
- Panel background color: deep navy-dark #1A1C2E at 90% opacity with a thin 1px border in #22243A
- Panel has a very subtle inner top edge highlight suggesting depth (1-2px lighter rim, no strong 3D bevel)

**TITLE BAR** (top 8.9% of panel, height 72px, full panel width):
- Background: #22243A solid dark strip
- Left side: bold white text "设置" (Settings), font size ~32px, color #F8F9FA, left-aligned with 32px left margin
- Right side: a close button [X] icon, 52x52px circle/square button, 16px from right edge, icon color #F8F9FA

**CONTENT AREA** (fills 81.2% of panel height between title and footer, left-right inner margin 32px each side):

SECTION 1 — Volume (音量), occupies top 24.2% of content area (196px tall):
- Section header text "音量" in #A8A9C0, font size 20px, bold, left-aligned
- Row 1 BGM: left label "BGM" in white #F8F9FA 18px; center-right: a horizontal slider track 640px wide x 12px tall in #3A3C58 with gold fill #FFB400 covering ~70% (value 0.70), a circular gold handle 32x32px at the 70% mark; far right: value text "0.70" in #A8A9C0 16px
- Row 2 SFX: same layout as BGM, slider fill ~85% (value 0.85), handle at 85%, value text "0.85"
- Below SFX row: a thin 1px horizontal divider line in #2E3050

SECTION 2 — Quality (画质), occupies next 14.8% of content area (120px tall):
- Section header text "画质" in #A8A9C0, font size 20px, bold, left-aligned
- Three radio buttons in a horizontal row with 48px spacing between them:
  - RadioBtn 1: "性能优先" — outer circle 28x28px unfilled ring in #A8A9C0, no inner dot (unselected state)
  - RadioBtn 2: "均衡" — outer circle 28x28px ring in #FFB400, inner filled dot 14x14px solid #FFB400 (SELECTED, this one is active)
  - RadioBtn 3: "高画质" — outer circle 28x28px unfilled ring in #A8A9C0, no inner dot (unselected)
  - Each radio button label: font size 17px, color #F8F9FA, right of its circle
- Below radio row: a thin 1px horizontal divider line in #2E3050

SECTION 3 — Key Bindings (按键), occupies next 31.6% of content area (256px tall):
- Section header text "按键" in #A8A9C0, font size 20px, bold, left-aligned
- Sub-notice text "即将推出" in italic, font size 14px, color #A8A9C0 at 70% opacity, just below header
- Three key binding rows, each 64px tall:
  - Row 1: left label "移动" in #A8A9C0 18px; right side: a rounded rectangle button 200x44px in #3A3C58 at 60% alpha (greyed/disabled appearance), button text "WASD" centered in #A8A9C0 16px
  - Row 2: left label "攻击" in #A8A9C0 18px; right button "鼠标左键" same disabled style
  - Row 3: left label "暂停" in #A8A9C0 18px; right button "Esc" same disabled style
  - All three buttons appear visibly dimmed/disabled (no hover glow, muted colors)

**FOOTER** (bottom 9.9% of panel, height 80px, full panel width):
- Background: #22243A solid dark strip (matching TitleBar, creating a frame effect)
- Right side: two buttons horizontally aligned
  - CancelButton: 160x52px, background #2E3050 (secondary grey-blue), text "取消" in #F8F9FA 18px, centered; 232px from right edge
  - SaveButton: 200x52px, background solid gold #FFB400 (the main CTA, visually dominant), text "保存" in dark #1A1C2E 18px bold, centered; 16px from right edge
  - SaveButton should have a subtle inner glow / shine effect consistent with the gold Accent style

**OVERALL AESTHETIC:**
- Dark, moody, fantasy RPG atmosphere
- The gold (#FFB400) appears only on: Slider fill + Slider handle, selected Radio dot, SaveButton background
- Subtle ambient glow around the panel edges (very faint, not harsh)
- Clean typographic hierarchy: large bold title > medium bold section headers > regular row labels > small value text
- No decorative patterns, no controller glyphs, no multi-language buttons, no Steam/Xbox logos
- The panel should feel premium and polished, like a shipped AAA indie game UI

---

## §3 负面词 Negative Prompt

blurry, watermark, signature, text artifacts, extra panels, duplicate elements, lens flare, photorealistic render, 3D scene perspective, isometric view, cartoon flat design, neon glow overload, color bleeding onto green background, any color other than #00FF00 in the chroma-key border area, missing radio button labels, visible grid lines, Lorem ipsum placeholder text, English section headers (headers must be Chinese: 音量/画质/按键), crowded layout, elements outside panel boundaries, non-centered panel, panel touching canvas edge, gradient on chroma-key background, semi-transparent green border, controller button icons (A/B/X/Y), steam logo, xbox logo, language switch button, decorative floral pattern, border ornaments, multiple quality presets shown as tabs (must be radio buttons), horizontal scrollbar, any slider showing value above 1.0, extra UI screens, multiple panels, split screen

---

## §4 输出规格

| 参数 | 值 |
|---|---|
| 格式 | PNG（无损） |
| 画布尺寸 | 1920 × 1080 px |
| 面板外背景 | 纯绿色 `#00FF00`（flat solid，无渐变，无阴影溢出） |
| 面板配色 | 严格按 §0 Hex 表，不引入额外颜色 |
| 面板尺寸 | 1056 × 810 px，居中于画布 |
| 面板左边距 | (1920-1056)/2 = 432 px |
| 面板顶边距 | (1080-810)/2 = 135 px |
| 输出路径 | `openspec/changes/10-settings-form/art/mockups/SettingsForm.png` |
| 状态 | 主面板 default 态（所有可交互组件使用 default/normal 外观，Radio「均衡」为 selected） |
| 状态变体 | 不在本阶段出图（延后至阶段 4 由 ui-asset-splitting 逐态处理） |
