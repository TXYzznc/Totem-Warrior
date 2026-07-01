# UI Annotations — 21-startup-select-form-landing

> Prefab 层级结构 + 元素位置说明。unity-skills MCP 按此建 Prefab。

## CharacterSelect.prefab

```
CharacterSelect (Canvas ScreenSpaceOverlay + CanvasScaler + GraphicRaycaster)
└─ Panel (Image=CharacterSelectForm_bg, anchor 全屏)
   ├─ Title (Text "选择角色", 顶部居中, fontSize 36)
   ├─ CharacterRoot (HorizontalLayoutGroup, 中部, 卡片间距 40)
   │  └─ (code 动态生成 3 张卡片)
   └─ NextButton (Button, sprite=button_primary, 底部居中, 宽 200 高 60, Text "下一步")
```

**代码动态生成卡片结构**（每张卡片）：
```
Card (RectTransform 240×280)
├─ CardFrame (Image=card_frame_unlocked, 全铺)
├─ Portrait (Image=Player{N}/Idle/Down, 中部 180×180)
└─ Name (Text "角色 N", 底部居中, fontSize 20)
```

## StartupSelect.prefab

```
StartupSelect (Canvas ScreenSpaceOverlay + CanvasScaler + GraphicRaycaster)
└─ Panel (Image=CharacterSelectForm_bg, anchor 全屏)
   ├─ Title (Text "起手 Build", 顶部居中, fontSize 36)
   ├─ ColorRoot (HorizontalLayoutGroup, y=200, 卡片间距 20)
   ├─ WeaponRoot (HorizontalLayoutGroup, y=0, 卡片间距 20)
   ├─ PatternRoot (HorizontalLayoutGroup, y=-200, 卡片间距 20)
   ├─ ConfirmButton (Button, sprite=button_primary, 右下, 宽 160 高 50, Text "确定")
   └─ CancelButton (Button, sprite=button_idle, 左下, 宽 160 高 50, Text "取消")
```

**代码动态生成卡片结构**（每张卡片）：
```
Card (RectTransform 100×120)
├─ IconBg (Image=card_frame_unlocked 或 单色, 全铺)
├─ Icon (Image=对应 sprite, 中部 80×80)
└─ Name (Text, 底部居中, fontSize 14)
```

## 交互态

- **未选中**：Image tint = 白（`Color.white`）
- **已选中**：Image tint = 淡绿（`new Color(0.5f, 1f, 0.5f)`）—— StartupSelectForm.SelectedTint 已定义
- **Confirm 按钮**：3 项未齐全时 `interactable = false`（灰）；齐全后亮起

## 场景挂载

- `MainMenu.prefab` 已在 GameApp 引用
- `CharacterSelect.prefab` / `StartupSelect.prefab` 由 UIModule 在 `LoadAllForms` 时 Resources.Load + Instantiate
- 三个 Form 都 Awake 时 SetActive(false)，通过 UIModule.Register 后按需 Open

## 尺寸 & 分辨率

- Canvas 用 `Scaler → Scale With Screen Size 1920×1080`，`Match=0.5`
- 所有卡片用 pixel size，不用 anchor 拉伸
