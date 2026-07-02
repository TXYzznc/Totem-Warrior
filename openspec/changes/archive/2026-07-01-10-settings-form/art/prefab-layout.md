# SettingsForm UI 结构文档

> art-ui 阶段 1 产出 · 版本 v1.0 · 2026-07-01
>
> **source of truth**：阶段 2 提示词反哺画布/占比、阶段 4 拆分清单、阶段 5 Prefab 层级，全部以本文为准。联调阶段若调整 RectTransform 值，必须同步回写本文。

---

## 全局约定

| 项目 | 值 |
|---|---|
| 画布基准分辨率 | 1920 × 1080 |
| Canvas Scaler 模式 | Scale With Screen Size |
| Canvas Scaler ReferenceResolution | 1920 × 1080 |
| Canvas Scaler Match | 0.5（宽高各占 50% 权重，720p / 1080p / 4K / 掌机均不破版） |
| Canvas 渲染模式 | Screen Space - Overlay |
| 本 change 涉及页面 | SettingsForm（1 页） |

### 配色系统（Hex 表）

| 语义 | Hex | 用途 |
|---|---|---|
| 面板背景 | `#1A1C2E` @ 90% Alpha | PanelBg Image |
| 主文字 | `#F8F9FA` | 标题 / Row Label / 数值 |
| 次级文字 | `#A8A9C0` | SectionHeader / 副标题 / 占位 |
| Accent 金色 | `#FFB400` | SaveButton 背景 / Slider 填充 / Radio 选中点 |
| 描边 | `#22243A` | 面板边框 / 按钮描边 |
| 分割线 | `#2E3050` | SectionDivider |
| Slider 轨道 | `#3A3C58` | Slider 背景轨道 |
| Slider 填充 | `#FFB400` | Slider 已填充区 |
| 按钮次级（取消） | `#2E3050` | CancelButton 背景 |
| 按钮禁用 | `#3A3C58` @ 60% Alpha | KeyBindButton disabled 态 |

### Anchor Preset 命名约定

本文使用语义名，与 Unity Inspector Anchor Preset 对应关系如下：

| 语义名 | anchorMin | anchorMax |
|---|---|---|
| stretch-all | (0, 0) | (1, 1) |
| top-stretch | (0, 1) | (1, 1) |
| bottom-stretch | (0, 0) | (1, 0) |
| middle-center | (0.5, 0.5) | (0.5, 0.5) |
| top-center | (0.5, 1) | (0.5, 1) |
| top-left | (0, 1) | (0, 1) |
| top-right | (1, 1) | (1, 1) |
| middle-left | (0, 0.5) | (0, 0.5) |
| middle-right | (1, 0.5) | (1, 0.5) |
| bottom-left | (0, 0) | (0, 0) |
| bottom-right | (1, 0) | (1, 0) |
| bottom-center | (0.5, 0) | (0.5, 0) |
| left-stretch | (0, 0) | (0, 1) |

### 单位规则

- 所有数值单位为 Canvas Scaler 虚拟像素（对应 1920×1080 基准分辨率）
- sizeDelta 在 stretch anchor 下语义为"相对父容器的偏移"（正值=内缩，负值=外扩），在 fixed anchor 下语义为实际尺寸
- anchoredPosition 坐标原点 = 父节点 anchor 点，Y 轴向上为正

### 通用尺寸速查

| 组件 | 尺寸 |
|---|---|
| 面板（SettingsPanel） | 1056 × 810（55% × 75% of 1920×1080） |
| TitleBar 高度 | 72 |
| SectionHeader 高度 | 40 |
| Row（音量/按键）高度 | 64 |
| Radio Row 高度 | 60 |
| Footer 高度 | 80 |
| Slider 轨道高度 | 12 |
| Slider Handle 尺寸 | 32 × 32 |
| Radio 圆圈外径 | 28 × 28 |
| KeyBindButton 尺寸 | 200 × 44 |
| SaveButton 尺寸 | 200 × 52 |
| CancelButton 尺寸 | 160 × 52 |

---

## 页面 1：SettingsForm

### HUD 信息层级

- **瞬间扫到**：面板标题"设置"、关闭 X、底栏两个按钮（取消 / 保存）
- **慢看时扫到**：三大组标题（音量 / 画质 / 按键）、当前 Slider 值、当前 Radio 选项
- **长时间观察**：具体 Row 标签（BGM / SFX / 性能优先…）、按键内容（WASD / 鼠标左键 / Esc）、「即将推出」副标题

### 节点树 + RectTransform 数据

```
SettingsForm (RectTransform:
  anchor: stretch-all            # anchorMin=(0,0), anchorMax=(1,1)
  pivot: (0.5, 0.5)
  sizeDelta: (0, 0)              # 铺满 Canvas
  anchoredPosition: (0, 0)
)
│  components: [
│    CanvasGroup（用于淡入淡出动画，alpha 从 0→1）
│  ]
│
├─ Overlay (RectTransform:
│    anchor: stretch-all
│    pivot: (0.5, 0.5)
│    sizeDelta: (0, 0)
│    anchoredPosition: (0, 0)
│  )
│  components: [
│    Image:
│      color: #000000 @ 50% Alpha
│      raycastTarget: true       # 拦截面板外的点击，防止穿透
│  ]
│
└─ SettingsPanel (RectTransform:
     anchor: middle-center       # anchorMin=(0.5,0.5), anchorMax=(0.5,0.5)
     pivot: (0.5, 0.5)
     sizeDelta: (1056, 810)      # 55%×75% of 1920×1080
     anchoredPosition: (0, 0)   # 屏幕正中
   )
   components: [
     Image:
       source: Panel_BG.png（阶段 4 产出，1056×810）
       color: #1A1C2E @ 90% Alpha
       preserveAspect: false
   ]
   │
   ├─ TitleBar (RectTransform:
   │    anchor: top-stretch      # anchorMin=(0,1), anchorMax=(1,1)
   │    pivot: (0.5, 1)
   │    sizeDelta: (0, 72)       # stretch 下宽=父宽，高=72
   │    anchoredPosition: (0, 0)
   │  )
   │  components: [
   │    Image:
   │      color: #22243A @ 100% Alpha（深色标题条）
   │      preserveAspect: false
   │  ]
   │  ├─ TitleText (RectTransform:
   │  │    anchor: middle-left   # anchorMin=(0,0.5), anchorMax=(0,0.5)
   │  │    pivot: (0, 0.5)
   │  │    sizeDelta: (300, 48)
   │  │    anchoredPosition: (32, 0)  # 距左边 32px
   │  │  )
   │  │  components: [
   │  │    TMP_Text:
   │  │      text: "设置"
   │  │      fontSize: 32
   │  │      fontStyle: Bold
   │  │      color: #F8F9FA
   │  │      alignment: MiddleLeft
   │  │  ]
   │  │
   │  └─ CloseButton (RectTransform:
   │       anchor: middle-right  # anchorMin=(1,0.5), anchorMax=(1,0.5)
   │       pivot: (1, 0.5)
   │       sizeDelta: (52, 52)
   │       anchoredPosition: (-16, 0)  # 距右边 16px
   │     )
   │     states: [normal, hover, pressed]
   │     components: [
   │       Image:
   │         source: CloseButton_normal.png（复用跨页组件，见下方「跨页复用组件」）
   │         preserveAspect: false
   │       Button:
   │         onClick → OnCloseClicked（= Rollback + Close）
   │     ]
   │     └─ CloseIcon (RectTransform:
   │          anchor: stretch-all
   │          sizeDelta: (-12, -12)   # 内缩 6px 四边，图标 40×40
   │          anchoredPosition: (0, 0)
   │        )
   │        components: [
   │          Image:
   │            source: Icon_X.png（32×32，白色 X 图标）
   │            preserveAspect: true
   │        ]
   │
   ├─ ContentArea (RectTransform:
   │    anchor: stretch-all
   │    pivot: (0.5, 0.5)
   │    sizeDelta: (0, -152)     # 上避让 TitleBar 72，下避让 Footer 80；总缩 152
   │    anchoredPosition: (0, -4) # 微调：上偏 4px 使视觉居中（72 上 - 80 下 ÷ 2 = -4）
   │  )
   │  components: [
   │    ScrollRect（可选：若内容超出 ContentArea 高度则开启，MVP 内容高度约 556px < 658px，暂不开）
   │  ]
   │  │
   │  └─ VLayout (RectTransform:
   │       anchor: top-stretch   # anchorMin=(0,1), anchorMax=(1,1)
   │       pivot: (0.5, 1)
   │       sizeDelta: (-64, 0)   # 左右各内缩 32px，宽=1056-64=992
   │       anchoredPosition: (0, -24)  # 距 ContentArea 顶 24px
   │     )
   │     components: [
   │       VerticalLayoutGroup:
   │         spacing: 0
   │         padding: top=0, bottom=0, left=0, right=0
   │         childAlignment: UpperCenter
   │         childControlWidth: true
   │         childControlHeight: false
   │         childForceExpandWidth: true
   │         childForceExpandHeight: false
   │     ]
   │     │
   │     │  ── SECTION 1: 音量 ──────────────────────────────
   │     │
   │     ├─ Section_Volume (RectTransform:
   │     │    anchor: top-stretch
   │     │    pivot: (0.5, 1)
   │     │    sizeDelta: (0, 196)   # 40 header + 16 gap + 64 Row×2 + 12 bottom padding
   │     │    anchoredPosition: (0, 0)  # Layout Group 接管
   │     │  )
   │     │  ├─ SectionHeader_Volume (RectTransform:
   │     │  │    anchor: top-stretch
   │     │  │    pivot: (0.5, 1)
   │     │  │    sizeDelta: (0, 40)
   │     │  │    anchoredPosition: (0, 0)
   │     │  │  )
   │     │  │  components: [
   │     │  │    TMP_Text:
   │     │  │      text: "音量"
   │     │  │      fontSize: 20
   │     │  │      fontStyle: Bold
   │     │  │      color: #A8A9C0
   │     │  │      alignment: MiddleLeft
   │     │  │  ]
   │     │  │
   │     │  ├─ Row_BGM (RectTransform:
   │     │  │    anchor: top-stretch
   │     │  │    pivot: (0.5, 1)
   │     │  │    sizeDelta: (0, 64)
   │     │  │    anchoredPosition: (0, -40)   # 紧接 SectionHeader 下方
   │     │  │  )
   │     │  │  ├─ Label_BGM (RectTransform:
   │     │  │  │    anchor: middle-left
   │     │  │  │    pivot: (0, 0.5)
   │     │  │  │    sizeDelta: (80, 40)
   │     │  │  │    anchoredPosition: (0, 0)
   │     │  │  │  )
   │     │  │  │  components: [
   │     │  │  │    TMP_Text: text="BGM", fontSize=18, color=#F8F9FA, alignment=MiddleLeft
   │     │  │  │  ]
   │     │  │  │
   │     │  │  ├─ Slider_BGM (RectTransform:
   │     │  │  │    anchor: middle-left
   │     │  │  │    pivot: (0, 0.5)
   │     │  │  │    sizeDelta: (640, 32)
   │     │  │  │    anchoredPosition: (96, 0)  # Label 80 + 间距 16
   │     │  │  │  )
   │     │  │  │  states: [normal, hover, pressed]
   │     │  │  │  components: [
   │     │  │  │    Slider:
   │     │  │  │      minValue: 0, maxValue: 1, wholeNumbers: false
   │     │  │  │      onValueChanged → OnBgmSliderChanged
   │     │  │  │  ]
   │     │  │  │  ├─ Background (stretch-all, sizeDelta=(0,0))
   │     │  │  │  │  Image: color=#3A3C58, height 覆盖区域 12px（通过 Layout / Padding 实现）
   │     │  │  │  ├─ Fill Area (stretch-all, sizeDelta=(-16,0), anchoredPosition=(-8,0))
   │     │  │  │  │  └─ Fill (stretch-all, sizeDelta=(0,0))
   │     │  │  │  │     Image: color=#FFB400
   │     │  │  │  └─ Handle Slide Area (stretch-all, sizeDelta=(-16,0), anchoredPosition=(0,0))
   │     │  │  │     └─ Handle (middle-center, sizeDelta=(32,32))
   │     │  │  │        Image: source=Slider_Handle.png（32×32，圆形金色 Handle）
   │     │  │  │        preserveAspect: true
   │     │  │  │
   │     │  │  └─ ValueText_BGM (RectTransform:
   │     │  │       anchor: middle-right
   │     │  │       pivot: (1, 0.5)
   │     │  │       sizeDelta: (80, 40)
   │     │  │       anchoredPosition: (0, 0)
   │     │  │     )
   │     │  │     components: [
   │     │  │       TMP_Text: text="1.00", fontSize=16, color=#A8A9C0, alignment=MiddleRight
   │     │  │     ]
   │     │  │
   │     │  ├─ Row_SFX (RectTransform:
   │     │  │    anchor: top-stretch
   │     │  │    pivot: (0.5, 1)
   │     │  │    sizeDelta: (0, 64)
   │     │  │    anchoredPosition: (0, -104)  # 40 header + 64 Row_BGM
   │     │  │  )
   │     │  │  （结构与 Row_BGM 完全对称，仅 Label text="SFX"，绑定 OnSfxSliderChanged）
   │     │  │
   │     │  └─ Divider_Volume (RectTransform:
   │     │       anchor: bottom-stretch
   │     │       pivot: (0.5, 0)
   │     │       sizeDelta: (0, 1)
   │     │       anchoredPosition: (0, -8)
   │     │     )
   │     │     components: [Image: color=#2E3050]
   │     │
   │     │  ── SECTION 2: 画质 ──────────────────────────────
   │     │
   │     ├─ Section_Quality (RectTransform:
   │     │    anchor: top-stretch
   │     │    pivot: (0.5, 1)
   │     │    sizeDelta: (0, 120)   # 40 header + 60 radio row + 20 padding
   │     │    anchoredPosition: (0, -205)  # 196 Section_Volume + 9 分割线区域
   │     │  )
   │     │  ├─ SectionHeader_Quality (RectTransform:
   │     │  │    anchor: top-stretch
   │     │  │    pivot: (0.5, 1)
   │     │  │    sizeDelta: (0, 40)
   │     │  │    anchoredPosition: (0, 0)
   │     │  │  )
   │     │  │  components: [
   │     │  │    TMP_Text: text="画质", fontSize=20, fontStyle=Bold, color=#A8A9C0, alignment=MiddleLeft
   │     │  │  ]
   │     │  │
   │     │  ├─ RadioGroup (RectTransform:
   │     │  │    anchor: top-stretch
   │     │  │    pivot: (0.5, 1)
   │     │  │    sizeDelta: (0, 60)
   │     │  │    anchoredPosition: (0, -40)
   │     │  │  )
   │     │  │  components: [
   │     │  │    HorizontalLayoutGroup:
   │     │  │      spacing: 48
   │     │  │      padding: left=0, right=0, top=0, bottom=0
   │     │  │      childAlignment: MiddleLeft
   │     │  │      childControlWidth: false
   │     │  │      childControlHeight: false
   │     │  │      childForceExpandWidth: false
   │     │  │      childForceExpandHeight: false
   │     │  │    ToggleGroup（互斥 Radio 用）
   │     │  │  ]
   │     │  │  ├─ RadioBtn_Performant (RectTransform:
   │     │  │  │    anchor: middle-left  （Layout Group 接管位置）
   │     │  │  │    pivot: (0, 0.5)
   │     │  │  │    sizeDelta: (240, 52)
   │     │  │  │    anchoredPosition: (0, 0)
   │     │  │  │  )
   │     │  │  │  states: [normal, selected]
   │     │  │  │  components: [
   │     │  │  │    Toggle: group=ToggleGroup, onValueChanged → OnQualityChanged(0)
   │     │  │  │    Image: source=RadioBtn_normal.png（背景）
   │     │  │  │  ]
   │     │  │  │  ├─ RadioCircle (middle-left, sizeDelta=(28,28), anchoredPosition=(12,0))
   │     │  │  │  │  Image: source=RadioCircle_normal.png（外圈）
   │     │  │  │  │  └─ RadioDot（仅 selected 态显示，middle-center, sizeDelta=(14,14)）
   │     │  │  │  │     Image: color=#FFB400
   │     │  │  │  └─ RadioLabel (middle-left, sizeDelta=(190,40), anchoredPosition=(48,0))
   │     │  │  │     TMP_Text: text="性能优先", fontSize=17, color=#F8F9FA, alignment=MiddleLeft
   │     │  │  │
   │     │  │  ├─ RadioBtn_Balanced (同结构，text="均衡", onValueChanged → OnQualityChanged(1), 默认 isOn=true)
   │     │  │  └─ RadioBtn_HighFidelity (同结构，text="高画质", onValueChanged → OnQualityChanged(2))
   │     │  │
   │     │  └─ Divider_Quality (RectTransform:
   │     │       anchor: bottom-stretch
   │     │       pivot: (0.5, 0)
   │     │       sizeDelta: (0, 1)
   │     │       anchoredPosition: (0, -8)
   │     │     )
   │     │     components: [Image: color=#2E3050]
   │     │
   │     │  ── SECTION 3: 按键 ──────────────────────────────
   │     │
   │     └─ Section_KeyBinding (RectTransform:
   │          anchor: top-stretch
   │          pivot: (0.5, 1)
   │          sizeDelta: (0, 256)  # 40 header + 32 SectionNotice + 8 gap + 64×3 rows + 8 bottom
   │          anchoredPosition: (0, -334)  # 205 + 120 + 9
   │        )
   │        ├─ SectionHeader_KeyBinding (RectTransform:
   │        │    anchor: top-stretch
   │        │    pivot: (0.5, 1)
   │        │    sizeDelta: (0, 40)
   │        │    anchoredPosition: (0, 0)
   │        │  )
   │        │  components: [
   │        │    TMP_Text: text="按键", fontSize=20, fontStyle=Bold, color=#A8A9C0, alignment=MiddleLeft
   │        │  ]
   │        │
   │        ├─ SectionNotice (RectTransform:
   │        │    anchor: top-stretch
   │        │    pivot: (0.5, 1)
   │        │    sizeDelta: (0, 28)
   │        │    anchoredPosition: (0, -40)
   │        │  )
   │        │  components: [
   │        │    TMP_Text:
   │        │      text: "即将推出"
   │        │      fontSize: 14
   │        │      fontStyle: Italic
   │        │      color: #A8A9C0 @ 70% Alpha
   │        │      alignment: MiddleLeft
   │        │  ]
   │        │
   │        ├─ Row_Move (RectTransform:
   │        │    anchor: top-stretch
   │        │    pivot: (0.5, 1)
   │        │    sizeDelta: (0, 64)
   │        │    anchoredPosition: (0, -76)   # 40 header + 28 notice + 8 gap
   │        │  )
   │        │  ├─ Label_Move (middle-left, sizeDelta=(80,40), anchoredPosition=(0,0))
   │        │  │  TMP_Text: text="移动", fontSize=18, color=#A8A9C0, alignment=MiddleLeft
   │        │  └─ KeyBindButton_Move (RectTransform:
   │        │       anchor: middle-right
   │        │       pivot: (1, 0.5)
   │        │       sizeDelta: (200, 44)
   │        │       anchoredPosition: (0, 0)
   │        │     )
   │        │     states: [disabled]   # v1.0 唯一态，interactable=false
   │        │     components: [
   │        │       Image: source=KeyBindButton_disabled.png, color=#3A3C58 @ 60% Alpha, preserveAspect=false
   │        │       Button: interactable=false
   │        │     ]
   │        │     └─ KeyBindText_Move (stretch-all, sizeDelta=(-8,0), anchoredPosition=(0,0))
   │        │        TMP_Text: text="WASD", fontSize=16, color=#A8A9C0, alignment=MiddleCenter
   │        │
   │        ├─ Row_Attack (同结构，text="攻击", KeyBindText="鼠标左键", anchoredPosition=(0,-140))
   │        │
   │        └─ Row_Pause  (同结构，text="暂停", KeyBindText="Esc", anchoredPosition=(0,-204))
   │
   └─ Footer (RectTransform:
        anchor: bottom-stretch   # anchorMin=(0,0), anchorMax=(1,0)
        pivot: (0.5, 0)
        sizeDelta: (0, 80)
        anchoredPosition: (0, 0)
      )
      components: [
        Image:
          color: #22243A @ 100% Alpha（与 TitleBar 配对，底部深色条）
      ]
      ├─ CancelButton (RectTransform:
      │    anchor: middle-right  # anchorMin=(1,0.5), anchorMax=(1,0.5)
      │    pivot: (1, 0.5)
      │    sizeDelta: (160, 52)
      │    anchoredPosition: (-232, 0)  # 保存按钮 200 + 间距 16 + 16 边距 = 232
      │  )
      │  states: [normal, hover, pressed]
      │  components: [
      │    Image: source=CancelButton_normal.png, color=#2E3050（次级按钮色）, preserveAspect=false
      │    Button: onClick → OnCancelClicked（= Rollback + Close）
      │  ]
      │  └─ CancelText (stretch-all, sizeDelta=(0,0))
      │     TMP_Text: text="取消", fontSize=18, color=#F8F9FA, alignment=MiddleCenter
      │
      └─ SaveButton (RectTransform:
           anchor: middle-right  # anchorMin=(1,0.5), anchorMax=(1,0.5)
           pivot: (1, 0.5)
           sizeDelta: (200, 52)
           anchoredPosition: (-16, 0)   # 距面板右边 16px
         )
         states: [normal, hover, pressed]
         components: [
           Image: source=SaveButton_normal.png, color=#FFB400（主题金色）, preserveAspect=false
           Button: onClick → OnSaveClicked（= Commit + Close）
         ]
         └─ SaveText (stretch-all, sizeDelta=(0,0))
            TMP_Text: text="保存", fontSize=18, fontStyle=Bold, color=#1A1C2E（深色字/金底对比）, alignment=MiddleCenter
```

---

### 关键决策

| # | 决策 | 选择 | 理由 |
|---|---|---|---|
| 1 | SettingsPanel anchor | middle-center fixed（1056×810） | 浮窗居中，不随分辨率拉伸，Canvas Scaler 统一缩放 |
| 2 | Overlay 拦截点击 | raycastTarget=true | 阻止面板外点击穿透到场景 UI |
| 3 | ContentArea sizeDelta=(0,-152) | TitleBar 72 + Footer 80 = 152 | 精确排除上下固定区域，剩余内容区 = 810-152=658px |
| 4 | VLayout sizeDelta=(-64,0) | 左右各留 32px 内边距 | 内容不贴面板边缘，视觉呼吸感 |
| 5 | Slider_BGM 宽 640 | Label 80 + 间距 16 + Slider 640 + ValueText 80 + 弹性 176 = 992（VLayout 宽） | 滑条占主要宽度，值文字右对齐 |
| 6 | RadioGroup HorizontalLayoutGroup spacing=48 | 三个 Radio 等间距横向，无需手动算位置 | 未来改档位数目时自动适配 |
| 7 | KeyBindButton anchor=middle-right | 与 Label 左对齐、按键右对齐，视觉上形成两栏布局 | 左标签 / 右按键，扫读清晰 |
| 8 | SaveButton color=#FFB400 | 主题金色做主 CTA，CancelButton 用次级灰蓝 | 单一主 CTA 原则，用户视线自然落到保存 |
| 9 | TitleBar CloseButton = Rollback + Close | 用户按 X 等同取消，不丢当前编辑 | 符合 design.md Q2 决策：取消完全回滚 |
| 10 | Section_KeyBinding sizeDelta=(0,256) | MVP 三行按键占位，故意设 disabled 整体灰化 | v1.0 不实现重绑定，但 UI 占位展示「即将推出」提升预期 |

---

### 状态清单（需多态出图的节点汇总）

> 标注"阶段 3 各生一张"的节点，在效果图生成阶段每态独立出一张图；标注"仅 disabled"的节点只出一张。

| 节点 | 父路径 | 状态列表 | 阶段 3/4 出图要求 |
|---|---|---|---|
| CloseButton | SettingsPanel/TitleBar | normal / hover / pressed | 3 张独立图（52×52） |
| Slider_BGM | Section_Volume/Row_BGM | normal / hover / pressed | 3 张轨道+Handle 状态图（640×32 + Handle 32×32） |
| Slider_SFX | Section_Volume/Row_SFX | normal / hover / pressed | 同 Slider_BGM，3 张 |
| RadioBtn_Performant | Section_Quality/RadioGroup | normal / selected | 2 张（240×52） |
| RadioBtn_Balanced | Section_Quality/RadioGroup | normal / selected | 2 张（与 Performant 同尺寸） |
| RadioBtn_HighFidelity | Section_Quality/RadioGroup | normal / selected | 2 张（与 Performant 同尺寸） |
| KeyBindButton_Move | Section_KeyBinding/Row_Move | disabled（唯一态） | 1 张（200×44） |
| KeyBindButton_Attack | Section_KeyBinding/Row_Attack | disabled（唯一态） | 1 张 |
| KeyBindButton_Pause | Section_KeyBinding/Row_Pause | disabled（唯一态） | 1 张 |
| CancelButton | Footer | normal / hover / pressed | 3 张（160×52） |
| SaveButton | Footer | normal / hover / pressed | 3 张（200×52） |

> **注**：Row_SFX 结构与 Row_BGM 完全对称，Slider 素材可复用同一套状态图（阶段 4 共享切片），UIForm 脚本绑定不同的 onValueChanged 回调即可。

---

### 画布占比速查（供阶段 2 提示词反哺）

> 面板画布 1056 × 810，以下比例用于效果图提示词的"结构约束"段。

| 区域 | 宽占比 | 高占比 | 像素高 | 备注 |
|---|---|---|---|---|
| TitleBar | 100% | 8.9% | 72 | 含标题文字 + X 按钮 |
| ContentArea（含内边距） | 100% | 81.2% | 658 | 三大组垂直堆叠 |
| — Section_Volume | 93.9% | 24.2% | 196 | 含 2 个 Slider Row |
| — Section_Quality | 93.9% | 14.8% | 120 | 含 3 个 Radio |
| — Section_KeyBinding | 93.9% | 31.6% | 256 | 含 3 个 KeyBindButton |
| Footer | 100% | 9.9% | 80 | 含 2 个按钮 |

---

### 控制器适配备注

- 焦点导航顺序（Tab Order）：CloseButton → Slider_BGM → Slider_SFX → RadioBtn_Balanced（默认选中） → CancelButton → SaveButton
- 所有 Button / Slider / Toggle 必须在 EventSystem 的 Navigation 中配置 Explicit 导航（不用 Automatic，避免焦点跳到 disabled KeyBindButton）
- KeyBindButton 全部设 interactable=false，Navigation.None（跳过）
- 控制器提示：底栏 CancelButton 左侧预留空间，可叠加 Controller Glyph（B 键），SaveButton 对应 A 键（阶段 5 由 client-unity 实现 ControllerPromptOverlay）

---

## 跨页复用组件

### CloseButton（复用标准）

- **来源 change**：10-settings-form 新建，后续 change 复用
- **状态**：新增（非复用，MainMenu 尚无独立 CloseButton 组件）
- **尺寸**：52 × 52
- **素材**：`CloseButton_normal.png` / `CloseButton_hover.png` / `CloseButton_pressed.png`
- **内嵌 Icon**：`Icon_X.png`（32×32，preserveAspect=true）
- **落库路径**：`Assets/Resources/Sprite/UI/Common/CloseButton_*.png`
- **用法**：任何需要关闭浮窗的 Form，直接引用 Common 目录下的 CloseButton 素材，无需重新拆分

### PrimaryButton（SaveButton 规格）

- **状态**：与 MainMenuForm 的 StartBtn 风格一致，但底色改 #FFB400（Accent 金色），属于**同风格不同皮肤**，记为「新增」以便阶段 4 单独拆片
- **尺寸**：200 × 52（与主菜单 320×80 不同，为浮窗内偏小规格）

### SecondaryButton（CancelButton 规格）

- **状态**：新增，底色 #2E3050
- **尺寸**：160 × 52

---

## 变更日志

- 2026-07-01 v1.0：art-ui 初版起草，覆盖 SettingsForm 完整节点树 + RectTransform + 状态清单 + 画布占比
