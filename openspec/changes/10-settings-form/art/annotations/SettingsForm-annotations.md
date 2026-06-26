# 标注稿 — SettingsForm（设置面板）

> 对应效果图：`art/mockups/SettingsForm.png`（1920×1080）
> 供 client-unity 阶段 4 直接 Read 使用，看完本文件即可搭 Prefab。
> **不含代码，不含 Unity 实现细节，专注于控件规格与布局。**

---

## 1. 整体规格

| 属性 | 值 |
|---|---|
| Canvas Reference Resolution | 1920 × 1080 |
| Canvas Scale Mode | Scale With Screen Size |
| Match（Width vs Height） | 0.5（均衡缩放，横竖屏通用） |
| Render Mode | Screen Space - Overlay |
| Sort Order | 100（浮窗层，高于游戏世界 HUD） |
| 主 Panel 尺寸 | 1152 × 810（约 60% 屏宽 × 75% 屏高） |
| 主 Panel 位置 | 居中（Anchor Center, Pivot 0.5 / 0.5, AnchoredPosition 0 / 0） |
| 安全区裕量 | 四边各留 ≥ 48px（参考 720p 最小屏适配） |

---

## 2. GameObject 层级

```
SettingsForm (Canvas)
├── Overlay (Image)                    ← 半透明黑色全屏遮罩
└── PanelFrame (Image)                 ← 主面板容器
    ├── TitleBar (RectTransform)
    │   ├── TitleText (TextMeshProUGUI)
    │   └── CloseButton (Button)
    │       └── CloseIcon (Image)
    ├── Sections (RectTransform + VerticalLayoutGroup)
    │   ├── VolumeSection (RectTransform)
    │   │   ├── SectionHeader_Volume (TextMeshProUGUI)
    │   │   ├── BgmSliderRow (RectTransform + HorizontalLayoutGroup)
    │   │   │   ├── Label_BGM (TextMeshProUGUI)
    │   │   │   ├── Slider_BGM (Slider)
    │   │   │   │   ├── Background (Image)
    │   │   │   │   ├── FillArea (RectTransform)
    │   │   │   │   │   └── Fill (Image)
    │   │   │   │   └── Handle Slide Area (RectTransform)
    │   │   │   │       └── Handle (Image)
    │   │   │   └── ValueText_BGM (TextMeshProUGUI)
    │   │   ├── SfxSliderRow (RectTransform + HorizontalLayoutGroup)
    │   │   │   ├── Label_SFX (TextMeshProUGUI)
    │   │   │   ├── Slider_SFX (Slider)
    │   │   │   │   ├── Background (Image)
    │   │   │   │   ├── FillArea (RectTransform)
    │   │   │   │   │   └── Fill (Image)
    │   │   │   │   └── Handle Slide Area (RectTransform)
    │   │   │   │       └── Handle (Image)
    │   │   │   └── ValueText_SFX (TextMeshProUGUI)
    │   │   └── Divider_Volume (Image)
    │   ├── QualitySection (RectTransform)
    │   │   ├── SectionHeader_Quality (TextMeshProUGUI)
    │   │   ├── QualityRadioRow (RectTransform + HorizontalLayoutGroup)
    │   │   │   ├── Radio_Low (RectTransform)
    │   │   │   │   ├── RadioCircle_Low (Image)
    │   │   │   │   └── RadioLabel_Low (TextMeshProUGUI)
    │   │   │   ├── Radio_Med (RectTransform)
    │   │   │   │   ├── RadioCircle_Med (Image)
    │   │   │   │   │   └── RadioDot_Med (Image)       ← 选中状态白点（仅选中态显示）
    │   │   │   │   └── RadioLabel_Med (TextMeshProUGUI)
    │   │   │   └── Radio_High (RectTransform)
    │   │   │       ├── RadioCircle_High (Image)
    │   │   │       └── RadioLabel_High (TextMeshProUGUI)
    │   │   └── Divider_Quality (Image)
    │   └── KeyBindSection (RectTransform)
    │       ├── SectionHeader_KeyBind (TextMeshProUGUI)
    │       ├── MoveBindRow (RectTransform + HorizontalLayoutGroup)
    │       │   ├── Label_Move (TextMeshProUGUI)
    │       │   └── KeyBindButton_Move (Button)
    │       │       └── KeyBindText_Move (TextMeshProUGUI)
    │       ├── AttackBindRow (RectTransform + HorizontalLayoutGroup)
    │       │   ├── Label_Attack (TextMeshProUGUI)
    │       │   └── KeyBindButton_Attack (Button)
    │       │       └── KeyBindText_Attack (TextMeshProUGUI)
    │       └── PauseBindRow (RectTransform + HorizontalLayoutGroup)
    │           ├── Label_Pause (TextMeshProUGUI)
    │           └── KeyBindButton_Pause (Button)
    │               └── KeyBindText_Pause (TextMeshProUGUI)
    └── FooterBar (RectTransform + HorizontalLayoutGroup)
        ├── CancelButton (Button)
        │   └── CancelText (TextMeshProUGUI)
        └── SaveButton (Button)
            └── SaveText (TextMeshProUGUI)
```

---

## 3. 控件规格表

| GameObject | 类型 | Anchor | Pivot | Width | Height | AnchoredPosition | 关键参数 | 备注 |
|---|---|---|---|---|---|---|---|---|
| **Overlay** | Image | Stretch All (0,0,1,1) | 0.5/0.5 | — | — | 0/0 (stretch) | Color #000000 A=180 | 全屏遮罩；点击不穿透（Raycast Target On） |
| **PanelFrame** | Image | Center (0.5/0.5) | 0.5/0.5 | 1152 | 810 | 0/0 | Color #1A1C2E A=230；9-slice sprite；圆角8-12px；描边 Outline 1px #22243A | MVP 用内置 Background sprite 染色 |
| **TitleBar** | RectTransform | Top-Stretch (0,1,1,1) | 0.5/1 | — | 72 | 0/0 | 无组件，仅布局容器 | |
| **TitleText** | TextMeshProUGUI | Left-Middle (0,0.5,0,0.5) | 0/0.5 | — | — | 32/0 | FontSize 36；Bold；Color #F8F9FA；Alignment Left-Middle | 文字内容「设置」 |
| **CloseButton** | Button | Top-Right (1,1,1,1) | 1/1 | 40 | 40 | -16/-16 | Normal Color #2E3050；Highlighted Color #3A3C58；Pressed Color #1A1C2E | 圆形背景 |
| **CloseIcon** | Image | Stretch All | 0.5/0.5 | 20 | 20 | 0/0 | 「×」符号 sprite 或用 TMP 文字「×」Color #F8F9FA | |
| **Sections** | RectTransform + VerticalLayoutGroup | Top-Stretch (0,1,1,1) | 0.5/1 | — | — | 0/-72 | Padding Left/Right=32；Top=16；Bottom=16；Spacing=24；ChildForceExpandWidth On | 顶部从 TitleBar 下方开始 |
| **VolumeSection** | RectTransform | Stretch (0,0,1,1) | 0.5/0.5 | — | auto | — | 无布局组件，高度由子元素决定 | |
| **SectionHeader_Volume** | TextMeshProUGUI | Top-Stretch | 0/1 | — | 36 | 0/0 | FontSize 24；Bold；Color #FFB400；文字「▸ 音量」；LetterSpacing +5% | |
| **BgmSliderRow** | RectTransform + HorizontalLayoutGroup | Top-Stretch | 0.5/1 | — | 40 | 0/-8（Section 内 Margin Top 8） | Spacing=16；ChildAlignment MiddleLeft | |
| **Label_BGM** | TextMeshProUGUI | Left-Middle | 0.5/0.5 | 60 | 40 | 0/0 | FontSize 20；Color #A8A9C0；Alignment Left-Middle；文字「BGM」 | 固定宽 60，不参与 Flex |
| **Slider_BGM** | Slider | — | 0.5/0.5 | 460 | 24 | 0/0 | MinValue=0；MaxValue=1；WholeNumbers=Off；Direction=Left-To-Right | Flexible Width 1（占满剩余） |
| **Slider_BGM / Background** | Image | Stretch | 0.5/0.5 | — | 6 | 0/0 | Color #3A3C58；高度 6px；AnchorMin/Max Y 居中 | 轨道底色 |
| **Slider_BGM / Fill** | Image | Stretch（仅 X）| 0/0.5 | — | 6 | 0/0 | Color #FFB400 | 已填充区段 |
| **Slider_BGM / Handle** | Image | Center | 0.5/0.5 | 20 | 20 | 0/0 | Color #FFD060；圆形 sprite（或 Aspect Ratio 1:1 纯色圆） | 拖块 |
| **ValueText_BGM** | TextMeshProUGUI | Right-Middle | 1/0.5 | 52 | 40 | 0/0 | FontSize 18；Color #F8F9FA；Alignment Right-Middle；等宽（TMP monospace feature）；初始文字「0.70」 | 固定宽 52 |
| **SfxSliderRow** | 同 BgmSliderRow | — | — | — | 40 | 0/-12（Row 间距 12） | 同上 | |
| **Label_SFX** | TextMeshProUGUI | — | — | 60 | 40 | — | 同 Label_BGM；文字「SFX」 | |
| **Slider_SFX** | Slider | — | — | 460 | 24 | — | 同 Slider_BGM；初始值 0.85 | |
| **ValueText_SFX** | TextMeshProUGUI | — | — | 52 | 40 | — | 同 ValueText_BGM；初始文字「0.85」 | |
| **Divider_Volume** | Image | Bottom-Stretch (0,0,1,0) | 0.5/0 | — | 1 | 0/0 | Color #2E3050；IgnoreLayout Off | Section 底部分割线 |
| **QualitySection** | RectTransform | — | — | — | auto | — | 同 VolumeSection | |
| **SectionHeader_Quality** | TextMeshProUGUI | — | — | — | 36 | — | 同 SectionHeader_Volume；文字「▸ 画质」 | |
| **QualityRadioRow** | RectTransform + HorizontalLayoutGroup | Top-Stretch | 0.5/1 | — | 40 | 0/-8 | Spacing=48；ChildAlignment MiddleLeft | |
| **Radio_Low** | RectTransform + HorizontalLayoutGroup | — | — | 96 | 40 | — | Spacing=8；ChildAlignment MiddleLeft | 圆圈 + 标签的容器 |
| **RadioCircle_Low** | Image | Left-Middle | 0.5/0.5 | 24 | 24 | 0/0 | Color 透明；描边 Outline 2px Color #6C6E90；圆形 sprite | MVP 用纯色圆 Image，通过 Outline 组件模拟 |
| **RadioLabel_Low** | TextMeshProUGUI | — | — | 36 | 40 | — | FontSize 20；Color #A8A9C0；文字「低」 | |
| **Radio_Med** | 同 Radio_Low | — | — | 96 | 40 | — | — | 默认选中 |
| **RadioCircle_Med** | Image | Left-Middle | 0.5/0.5 | 24 | 24 | 0/0 | Color #FFB400；圆形 sprite（选中态） | |
| **RadioDot_Med** | Image | Center | 0.5/0.5 | 10 | 10 | 0/0 | Color #FFFFFF；圆形 sprite；子级在 RadioCircle_Med 内 | 白色实心内圆点 |
| **RadioLabel_Med** | TextMeshProUGUI | — | — | 36 | 40 | — | FontSize 20；Color #F8F9FA Bold；文字「中」 | 选中态白色粗体 |
| **Radio_High** | 同 Radio_Low | — | — | 96 | 40 | — | — | 未选中 |
| **RadioCircle_High** | Image | — | — | 24 | 24 | — | 同 RadioCircle_Low | |
| **RadioLabel_High** | TextMeshProUGUI | — | — | 36 | 40 | — | 同 RadioLabel_Low；文字「高」 | |
| **Divider_Quality** | Image | — | — | — | 1 | — | 同 Divider_Volume | |
| **KeyBindSection** | RectTransform | — | — | — | auto | — | 同 VolumeSection | |
| **SectionHeader_KeyBind** | TextMeshProUGUI | — | — | — | 36 | — | 同 SectionHeader_Volume；文字「▸ 按键」 | |
| **MoveBindRow** | RectTransform + HorizontalLayoutGroup | Top-Stretch | 0.5/1 | — | 48 | 0/-8 | Spacing=16；ChildAlignment MiddleLeft | |
| **Label_Move** | TextMeshProUGUI | — | — | 72 | 48 | — | FontSize 20；Color #A8A9C0；文字「移动」 | |
| **KeyBindButton_Move** | Button | — | — | 140 | 40 | — | Normal Color #252740；Highlighted Color #2E3050；Pressed Color #1A1C2E；Outline 1px #6C6E90；圆角 6px | |
| **KeyBindText_Move** | TextMeshProUGUI | Stretch | 0.5/0.5 | — | — | — | FontSize 18；Bold；Color #F8F9FA；Alignment Center；文字「WASD」 | |
| **AttackBindRow** | 同 MoveBindRow | — | — | — | 48 | 0/-12 | — | Row 间距 12 |
| **Label_Attack** | TextMeshProUGUI | — | — | 72 | 48 | — | 同 Label_Move；文字「攻击」 | |
| **KeyBindButton_Attack** | Button | — | — | 140 | 40 | — | 同 KeyBindButton_Move；文字「鼠标左键」 | |
| **KeyBindText_Attack** | TextMeshProUGUI | — | — | — | — | — | 同 KeyBindText_Move；文字「鼠标左键」 | |
| **PauseBindRow** | 同 MoveBindRow | — | — | — | 48 | 0/-12 | — | |
| **Label_Pause** | TextMeshProUGUI | — | — | 72 | 48 | — | 同 Label_Move；文字「暂停」 | |
| **KeyBindButton_Pause** | Button | — | — | 140 | 40 | — | 同 KeyBindButton_Move；文字「Esc」 | |
| **KeyBindText_Pause** | TextMeshProUGUI | — | — | — | — | — | 同 KeyBindText_Move；文字「Esc」 | |
| **FooterBar** | RectTransform + HorizontalLayoutGroup | Bottom-Stretch (0,0,1,0) | 0.5/0 | — | 72 | 0/0 | Padding Left/Right=32；Top/Bottom=16；Spacing=12；ChildAlignment MiddleRight | 子项靠右排 |
| **CancelButton** | Button | Right-Middle | 1/0.5 | 120 | 40 | — | Normal Color 透明；Highlighted Color #2E3050 A=100；Outline 1px #A8A9C0 | |
| **CancelText** | TextMeshProUGUI | Stretch | 0.5/0.5 | — | — | — | FontSize 18；Color #F8F9FA；Alignment Center；文字「取消」 | |
| **SaveButton** | Button | Right-Middle | 1/0.5 | 120 | 40 | — | Normal Color #FFB400；Highlighted Color #FFD060；Pressed Color #E6A200 | |
| **SaveText** | TextMeshProUGUI | Stretch | 0.5/0.5 | — | — | — | FontSize 18；Bold；Color #1A1C2E；Alignment Center；文字「保存」 | |

---

## 4. Spacing / Padding 规则

### 主 Panel 内边距

| 位置 | 值 |
|---|---|
| 左右内边距（Sections 的 Padding Left/Right） | 32px |
| TitleBar 左侧文字 Padding | 32px |
| CloseButton 右上角偏移 | -16px / -16px |
| FooterBar 左右 Padding | 32px |
| FooterBar 上下 Padding | 16px |

### Section 间距

| 位置 | 值 |
|---|---|
| Sections VerticalLayoutGroup Spacing（Section 间） | 24px |
| SectionHeader 到第一行 Row 的 Margin Top | 8px |
| Row 之间间距（同一 Section 内） | 12px |

### Row 内间距

| 位置 | 值 |
|---|---|
| HorizontalLayoutGroup Spacing（Label ↔ Control） | 16px |
| QualityRadioRow 内 Radio 间距 | 48px |
| FooterBar 按钮间距 | 12px |

### 按钮内边距

| 位置 | 值 |
|---|---|
| KeyBindButton 水平 Padding（依靠 ContentSizeFitter 或固定宽 140） | 固定宽 140 × 高 40 |
| CancelButton / SaveButton | 固定宽 120 × 高 40 |
| TitleBar 高度 | 72px |
| FooterBar 高度 | 72px |

---

## 5. 颜色表

| 用途 | HEX | Alpha |
|---|---|---|
| 面板背景（PanelFrame） | `#1A1C2E` | 230 / 255（约 90%） |
| 全屏遮罩（Overlay） | `#000000` | 180 / 255（约 70%） |
| 主文字 | `#F8F9FA` | 255 |
| 次级文字 / 标签 | `#A8A9C0` | 255 |
| Accent 主题色（滑块、选中态、保存按钮） | `#FFB400` | 255 |
| 描边通用（可交互元素边缘） | `#22243A` | 255 |
| 分割线（Divider） | `#2E3050` | 255 |
| 滑动条轨道（未填充区段） | `#3A3C58` | 255 |
| 滑动条已填充区段 | `#FFB400` | 255 |
| 滑块 thumb | `#FFD060` | 255 |
| Radio 未选中描边 | `#6C6E90` | 255 |
| Radio 选中填充 | `#FFB400` | 255 |
| Radio 选中内点（白点） | `#FFFFFF` | 255 |
| KeyBindButton 背景 | `#252740` | 255 |
| KeyBindButton 描边 | `#6C6E90` | 255 |
| PrimaryButton（保存）填充 | `#FFB400` | 255 |
| PrimaryButton（保存）文字 | `#1A1C2E` | 255 |
| PrimaryButton Hover | `#FFD060` | 255 |
| PrimaryButton Pressed | `#E6A200` | 255 |
| SecondaryButton（取消）描边 | `#A8A9C0` | 255 |
| SecondaryButton（取消）文字 | `#F8F9FA` | 255 |
| SecondaryButton Hover 背景 | `#2E3050` | 100 |
| 危险红（冲突用，本页未出现） | `#E63946` | 255 |

---

## 6. 字号表

| 层级 | 用途 | FontSize（px） | FontStyle | Color |
|---|---|---|---|---|
| L1 | 面板标题「设置」（TitleText） | 36 | Bold | `#F8F9FA` |
| L2 | 组标题（SectionHeader_*） | 24 | Bold | `#FFB400` |
| L3 | 行标签（Label_BGM / SFX / Move / Attack / Pause） | 20 | Normal | `#A8A9C0` |
| L3 | Radio 标签（RadioLabel_*） | 20 | Normal（未选中）/ Bold（选中） | `#A8A9C0` / `#F8F9FA` |
| L4 | 数值显示（ValueText_BGM / SFX） | 18 | Normal（等宽） | `#F8F9FA` |
| L4 | KeyBindButton 键名文字（KeyBindText_*） | 18 | Bold | `#F8F9FA` |
| L4 | 按钮文字（CancelText / SaveText） | 18 | Normal / Bold | `#F8F9FA` / `#1A1C2E` |

> 字体族：CJK 粗体黑体族 + 拉丁大写混排（项目主用字体，与主菜单一致）。
> ValueText 建议启用 TMP 等宽字距（Monospace），避免数值更新时宽度跳动。
> SectionHeader 建议 Character Spacing 略松（+5）。

---

## 7. 临时占位资源建议（MVP 快速搭建用）

| 控件 | MVP 方案 | 后期升级方向 |
|---|---|---|
| **PanelFrame 圆角** | Unity 内置 `Sprites/UI/Background`（9-slice），染色 `#1A1C2E`，通过 Image Inspector 的 Pixels Per Unit Multiplier 调整圆角视觉感 | 接入 RoundedCornersShader 或自定义 9-slice sprite（圆角 8-12px） |
| **PanelFrame 描边** | Outline 组件，Effect Color `#22243A`，Distance 1/1 | 同上，或用外圈 Image（Mask 切掉内部）做描边 |
| **RadioCircle（未选中）** | 纯色圆形 Image Color 透明 + Outline 组件 2px Color `#6C6E90` | 替换为专用 sprite（描边圆） |
| **RadioCircle（选中）+ RadioDot** | 圆形 Image Color `#FFB400`，子级 12×12 圆形 Image Color `#FFFFFF` | 同上 |
| **Slider Handle** | 圆形 Image Color `#FFD060`，大小 20×20 | 替换为带外发光的 sprite |
| **Divider** | 1px 高 Image，Color `#2E3050`，宽度 Stretch | 保持不变，无需升级 |
| **KeyBindButton 描边** | Outline 组件 1px Color `#6C6E90` | 自定义 9-slice 按钮 sprite |
| **CloseButton 背景** | 圆形 Image Color `#2E3050`，40×40 | 替换为专用 sprite |
| **全屏遮罩** | 白色 Image + Color 设为 `#000000 A=180` | 保持不变 |

> 以上所有占位方案在 MVP 阶段均不需要额外美术贴图，全部通过 Unity 默认 sprite + 组件参数实现。

---

## 8. 与效果图差异说明

对照 `art/mockups/SettingsForm.png` 逐项核查，差异如下：

| 序号 | 差异点 | 效果图表现 | 标注稿要求 | 理由 |
|---|---|---|---|---|
| D1 | RadioDot（选中内白点） | 效果图「中」选中的金色圆内未明确绘出白色实心圆点，整体看起来是纯金色填充圆 | 标注稿要求 RadioDot_Med 为 `#FFFFFF` 10×10 白色实心圆 | 白心圆点在实际 UI 中可提升选中状态的可读性，避免纯金色圆在低亮度屏幕上难以辨认；效果图未画出白心是渲染精度限制，标注稿要求以功能清晰为准 |
| D2 | PanelFrame 描边光晕 | 效果图面板边缘有金色外发光（#FFB400 约 40% 透明度） | 标注稿仅要求 Outline 1px `#22243A` 描边，不要求外发光 | 外发光为美术润色层，MVP 阶段成本高（需 Shader 或额外 Image），列入后期升级；功能完整性不受影响 |
| D3 | 游戏背景 | 效果图背景可见昏暗的游戏场景（暗红色植被 + 火炬） | 标注稿 Overlay 为纯黑半透明遮罩 | 背景由游戏世界摄像机渲染，SettingsForm prefab 本身只需半透明遮罩；效果图中的背景是构图演示用，不属于 prefab 资产 |
| D4 | Section 内部间距观感 | 效果图三大 Section 之间呼吸感较大 | 标注稿 Sections VerticalLayoutGroup Spacing=24，Section 内 Header 到 Row Margin Top=8，Row 间距 12 | 已对齐效果图视觉比例，基于 1152px 宽面板折算；若联调发现偏紧，阶段 5 调整 Spacing 为 32 即可 |
| D5 | KeyBindButton 宽度 | 效果图「鼠标左键」按钮与「WASD」「Esc」按钮宽度相近（等宽） | 标注稿固定 Width=140，用 TextOverflow Ellipsis 处理超长文字 | 固定宽度保证三个按钮视觉对齐；「鼠标左键」4 字在 FontSize 18 下约 80px，140px 宽绰绰有余 |

---

*标注稿版本：1.0 / 生成日期：2026-06-26 / 出品：art-ui*
