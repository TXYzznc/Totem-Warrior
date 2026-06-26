美术素材状态: 已处理（阶段 3 第 1 轮一次通过，等用户确认）
处理日期: 2026-06-26 17:44
执行 SKILL: codex-image-gen
输出路径: art/mockups/SettingsForm.png（1920×1080，2.83 MB）
生成记录: art/mockups/生成记录.md
重试轮次: 1/3（首轮通过，未触发重试）
关联文件: art/requirements.md

---

# 效果图提示词 — 10-settings-form 设置面板

> **阶段 2 产出物**：本文件由 `art-ui` 起草，主对话拿去给用户确认后，阶段 3 由 `codex-image-gen` 逐条调 Codex 出图。
> **出图范围**：只出主面板 1 张 `SettingsForm.png`；状态变体（重绑定 waiting / 冲突 Toast）延后到阶段 5 联调时按需补。

---

## Art Bible 锚定（强制继承，codex-image-gen 单读本文件也能出对风格）

### §0.1 风格定位

继承项目已锁定风格基线（来源：`06-v21-implementation/art/requirements.md §0`）：

- **整体定位**：「似 Hades 精致 2.5D」——手绘描边 + 高对比光影 + 厚涂 + 鲜艳饱和色彩
- **画风层**：手绘描边轮廓（1-2px 等效暗色描边），厚涂，单体形象在缩略尺寸仍能清晰辨识
- **光影层**：主光源高位偏侧（30°-60°），背光 + rim light 强化边缘；UI 面板带轻微浮雕感
- **氛围层**：暗色系基调，UI 面板半透明深色背景 + 亮色调文字，金色 accent 点亮视觉焦点

### §0.2 色彩硬约束

| 用途 | HEX | 说明 |
|---|---|---|
| 面板背景 | `#1A1C2E`（含 90% 不透明度） | 深蓝黑，与游戏世界形成景深分离 |
| 主文字 | `#F8F9FA` | 高对比白 |
| 次级文字 / 标签 | `#A8A9C0` | 偏灰蓝，降调不抢视线 |
| Accent（主题色） | `#FFB400` | 金色，Hades 风 — 用于选中态 / 滑块 / 主要按钮 |
| 描边（通用） | `#22243A` | 深蓝黑描边，所有可交互元素边缘 |
| 分割线 | `#2E3050` | 比面板背景略亮，细线区分组块 |
| 滑动条轨道 | `#3A3C58` | 深灰蓝，未填充区段 |
| 滑动条已填充 | `#FFB400` | 同 Accent，已拖动区段 |
| 滑块（thumb） | `#FFD060` | 比 Accent 略亮，圆形小拖块 |
| RadioGroup 未选中 | 描边 `#6C6E90`，填充透明 | 空心圆圈 |
| RadioGroup 选中 | 描边 + 填充 `#FFB400`，内有实心白圆点 | 高亮单选点 |
| KeyBindButton 背景 | `#252740`，描边 `#6C6E90` | 键名显示区，空心矩形按钮 |
| PrimaryButton（保存） | 填充 `#FFB400`，文字 `#1A1C2E` | 金色实心，黑字 |
| SecondaryButton（取消） | 填充透明，描边 `#A8A9C0`，文字 `#F8F9FA` | 描边版 |
| 危险红（预留冲突用） | `#E63946` | 冲突状态描边闪烁色，本张 mockup 不出现 |

### §0.3 字体与排版硬约束

- **字体风格**：CJK 粗体（黑体/厚体族）+ 拉丁大写英文混排，厚重有力，与 Hades 风格一致
- **字号层级建议**：
  - 面板标题「设置」：32-40px，`#F8F9FA`，居左或居中
  - 组标题（音量 / 画质 / 按键）：22-26px，`#FFB400` accent 色，带「▸」前缀
  - 行标签（BGM / SFX / 移动 / 攻击 / 暂停）：18-20px，`#A8A9C0`
  - 数值显示（0.70 / 0.85）：16-18px，`#F8F9FA` 等宽字体感
  - 按钮文字（取消 / 保存）：18-20px
- **字间距**：组标题 letter-spacing 略松（+5%），正文 normal

---

## 效果图提示词（阶段 3 出图用）

### 页面 1：SettingsForm — 设置主面板

**输出文件**：`art/mockups/SettingsForm.png`
**建议尺寸**：1920×1080（16:9，标准游戏全屏参考尺寸）

---

#### 正向提示词（英文，imagegen 优先英文）

```
A single dark fantasy game settings panel UI mockup, full 1920x1080 resolution, game HUD design inspired by Hades by Supergiant Games.

OVERALL COMPOSITION:
A centered floating panel occupying approximately 60% screen width and 75% screen height. Panel sits on a blurred dark background (scene behind). Panel has rounded corners (8-12px radius equivalent), a subtle golden inner glow border (#FFB400 at 40% opacity), and a deep dark-blue semi-transparent background (#1A1C2E at 90% opacity). Light outer shadow to separate from background.

TITLE BAR:
Top of panel: title text "设置" (Chinese: Settings) in bold CJK font, approximately 36px, white (#F8F9FA), left-aligned with 24px padding. Top-right corner has a circular close button [X] with dark fill and light border.

SECTION — 音量 (Volume):
Gold accent section header "▸ 音量" at ~24px bold, color #FFB400. Two rows below:
Row 1: label "BGM" (gray #A8A9C0, 18px), then a horizontal slider track (dark gray #3A3C58, height 6px, width ~300px), the left portion filled gold (#FFB400) indicating value 0.70 — slider thumb is a small gold circle (#FFD060, ~14px diameter) positioned at 70% mark, right side shows numeric value "0.70" in white 16px monospace.
Row 2: label "SFX" (gray #A8A9C0, 18px), similar slider with thumb at 85% mark, numeric value "0.85".
A thin horizontal divider line (#2E3050) separates this section from the next.

SECTION — 画质 (Quality):
Gold accent section header "▸ 画质" at ~24px bold, color #FFB400. One row with three radio buttons horizontally:
Option "低" (Low): empty circle border (#6C6E90), transparent fill, label text "低" gray.
Option "中" (Medium) — SELECTED STATE: filled gold circle (#FFB400) with white center dot, label text "中" in white bold. This is the active selection.
Option "高" (High): empty circle border (#6C6E90), transparent fill, label text "高" gray.
Spacing between radio options: ~60px. No resolution or FPS annotations.
A thin horizontal divider line (#2E3050) separates this section from the next.

SECTION — 按键 (Key Bindings):
Gold accent section header "▸ 按键" at ~24px bold, color #FFB400. Three rows:
Row 1: label "移动" (gray, 18px), then a rectangular key-bind button [ WASD ] — dark fill (#252740), border (#6C6E90, 1px), rounded corners, text "WASD" in white 16px bold, width ~120px.
Row 2: label "攻击" (gray, 18px), key-bind button [ 鼠标左键 ] — same style, text "鼠标左键" in white 16px bold, width ~120px.
Row 3: label "暂停" (gray, 18px), key-bind button [ Esc ] — same style, text "Esc" in white 16px bold, width ~120px.
All three key-bind buttons in idle state (no glow, standard border).

FOOTER BAR:
Thin divider at bottom of content area. Footer row right-aligned:
Left button "取消" (Cancel): transparent fill, border #A8A9C0 1px, text "取消" white 18px. Width ~100px.
Right button "保存" (Save): filled #FFB400 gold, text "保存" dark #1A1C2E 18px bold. Width ~100px. Spacing between buttons: 12px.

TYPOGRAPHY STYLE:
All Chinese text in heavy bold sans-serif CJK style (similar to Source Han Sans Bold / Noto Sans CJK Bold). High contrast. No decorative calligraphy. Slightly loose letter-spacing on section headers.

RENDERING STYLE:
Hand-painted thick outline style (Hades-inspired). UI elements have subtle beveled highlights on top-left edges and darker shadows on bottom-right (classic game UI emboss). The overall aesthetic is dark fantasy game UI, NOT flat design, NOT material design, NOT minimalist web UI. Rich, atmospheric, and clearly readable. The panel feels like it belongs in an action roguelike game.

LIGHTING:
Subtle golden glow emanates from gold accent elements (slider fill, radio selection, primary button). Panel background has very faint vignette at edges. Crisp, game-ready appearance.
```

---

#### 负面提示词

```
flat design, flat vector, minimalist, material design, web UI, mobile app UI, light background, white panel, pastel colors, low saturation, desaturated, gray dull palette, cartoon chibi, pixel art, realistic photograph, excessive decorations, heavy ornamental borders, watermark, signature, text artifacts outside UI elements, emoji, multiple separate windows, lorem ipsum, English-only labels for Chinese settings, blurry, jpeg compression artifacts, UI glitches, misaligned elements, inconsistent style, neon cyberpunk (unless gold accent), cluttered layout, overlapping text
```

---

#### 构图要点备注（给 codex-image-gen 的补充说明）

- 面板居中，四周留有游戏背景（可以是深色模糊背景或简单暗色渐变，模拟游戏内浮窗场景）
- 三大区块（音量 / 画质 / 按键）垂直堆叠，每组之间用细分割线区隔，整体有呼吸感，不拥挤
- 每个组件必须至少显示 1 个 normal/idle 状态（要求在同一张 mockup 上同时可见）：
  - BGM 滑块：正常拖动态，值显示 0.70
  - SFX 滑块：正常拖动态，值显示 0.85
  - RadioGroup：「低」未选中 + 「中」已选中 + 「高」未选中，三态同时可见
  - KeyBindButton：三个全部 idle 态，分别显示 WASD / 鼠标左键 / Esc
  - 取消按钮：idle 态（描边版）
  - 保存按钮：idle 态（金色实心）
  - 右上 X 关闭按钮：idle 态
- **不要**出现重绑定 waiting 状态（文字变「按任意键...」）
- **不要**出现冲突 Toast
- **不要**出现分辨率 / 帧率 / 灵敏度 / 手柄提示等内容

---

#### 重试策略（阶段 3 使用）

| 轮次 | 调整方向 |
|---|---|
| 第 1 轮 | 按上方提示词原样出图 |
| 第 2 轮（如不满意） | 加强提示词中「Hades-inspired thick outline hand-painted」权重；补充「reference style: Supergiant Games Hades UI, dark fantasy game menu」 |
| 第 3 轮（如仍不满意） | 把配色层 hex 值在提示词里重复一遍；增加「extremely detailed, professional game UI concept art, high quality」；减少负面词长度 |
| 超过 3 轮 | 停止，交回主对话 + 用户决定是否人工介入或跳过本页 |
```
