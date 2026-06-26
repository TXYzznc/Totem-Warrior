美术素材状态: 已处理（阶段 3 完成，效果图已落盘，等用户确认进阶段 4）
处理日期: 2026-06-26 17:44
执行 SKILL: codex-image-gen
输出: art/mockups/SettingsForm.png（1920×1080，2.83 MB，1 张）
生成记录: art/mockups/生成记录.md
批量分档: L0 ×1（UI 子流程主面板单图，重试 1/3）

# 美术需求 — 10-settings-form 设置面板

> **状态**：阶段 1 骨架（draft），**禁止**在用户确认前进入阶段 2。
> **承担方**：`art-director` 把关风格 + `art-ui` 起草三表与提示词 + `codex-image-gen` 阶段 3 落盘
> **目标用户**：设置面板（SettingsForm）— 单页 UI，覆盖音量 / 画质 / 按键三组 + 保存/取消底栏

---

## 0. Art Bible 锚定（沿用项目既有风格）

> 设置面板的视觉风格必须与项目主菜单 / 暂停菜单 / HUD 一致。本次不引入新风格分支。

**继承自现有 art-bible**（如有），关键约束：
- 风格定位：与主菜单一致（待 art-ui 阶段 2 引用主菜单已确定的色板 / 字体 / 圆角半径 / 描边粗细）
- 字体：项目主用字体（CJK + 拉丁混排）
- 配色：暗色系主菜单基调（半透明背景 + 高对比文字 + 醒目主题色 accent）
- 圆角：与主菜单按钮一致
- 阴影/描边：与主菜单一致

> ⚠️ 阶段 2 art-ui 必须先**读主菜单效果图 / 风格指南**，再写本次提示词，避免风格分裂。

---

## 1. 页面清单（三表 1/3）

| # | 页面名 | 文件名（mockup） | 描述 | 状态 | 屏占比 |
|---|---|---|---|---|---|
| 1 | 设置面板 | `SettingsForm.png` | 单页面板，垂直堆叠音量 / 画质 / 按键三组 + 底栏 | 待出图 | 居中浮窗 ~ 60% 屏宽 × 75% 屏高 |
| 2 | 重绑定提示弹窗 | `SettingsForm-rebind-waiting.png`（可选） | 玩家点重绑定按钮后，按钮文字变「按任意键...」的局部状态 | 待出图 | 局部，覆盖在 1 上 |
| 3 | 冲突 Toast | `SettingsForm-conflict-toast.png`（可选） | 按键冲突时弹出的小 toast | 待出图 | 局部，屏底/居中浮窗 |

> **MVP 决策**：阶段 3 出图先只出 1（主面板）。2/3 是状态变体，阶段 5 联调时若运行时表现明显偏离设计意图再补出。

---

## 2. 复用组件清单（三表 2/3）

| 组件名 | 描述 | 是否复用现有 | 复用来源（如有） |
|---|---|---|---|
| `PanelFrame` | 半透明带圆角 + 描边的主面板容器 | 复用 | MainMenuForm / PauseMenuForm 同款 |
| `SectionHeader` | 组标题（音量 / 画质 / 按键） | 复用 | 主菜单分区标题同款 |
| `LabeledSlider` | 标签 + 滑动条 + 数值显示（0.00 ~ 1.00） | **新增** | — |
| `RadioGroup` | 单选三档（低/中/高） | **新增** | — |
| `KeyBindButton` | 重绑定按钮（显示当前键名，可点击进入等待状态） | **新增** | — |
| `PrimaryButton` | 保存按钮（主题色填充） | 复用 | 主菜单「开始游戏」按钮同款 |
| `SecondaryButton` | 取消按钮（描边版） | 复用 | 主菜单「退出」按钮同款 |
| `CloseButton` | 右上 X 关闭按钮 | 复用 | 项目通用关闭按钮 |
| `Toast` | 冲突提示 toast | **新增** | — |

> 新增 4 个组件：`LabeledSlider` / `RadioGroup` / `KeyBindButton` / `Toast`，需要 art-ui 阶段 2 给出独立视觉规格。

---

## 3. 组件状态表（三表 3/3）

| 组件 | 状态 | 视觉差异 |
|---|---|---|
| `LabeledSlider` | normal | 滑轨灰 + 滑块主题色 + 数值右侧显示 |
| | hover | 滑块放大 1.1x + 高光 |
| | dragging | 滑块拉伸 / 强高光 + 数值闪烁更新 |
| | disabled | 整体降饱和度 70% |
| `RadioGroup` | unselected | 描边 + 透明填充 |
| | selected | 主题色填充 + 白色对勾或圆点 |
| | hover | 描边变粗 |
| `KeyBindButton` | idle | 显示当前键名（如「Esc」「LMB」），描边版 |
| | hover | 描边变亮 |
| | waiting | 文字变「按任意键...」+ 主题色脉冲呼吸 |
| | conflict-just-now | 短暂红色描边闪一下（200ms） |
| `PrimaryButton`（保存） | idle / hover / pressed / disabled | 沿用主菜单按钮状态 |
| `SecondaryButton`（取消） | idle / hover / pressed | 沿用主菜单按钮状态 |
| `CloseButton` | idle / hover | 沿用项目通用 |
| `Toast` | enter（300ms 上滑淡入） / show（2s 停留） / exit（200ms 淡出） | 半透明黑底 + 白字 + 警告图标 |

---

## 4. 信息层级

```
PanelFrame
├─ TitleBar [设置]                                              [X]
├─ SectionHeader [音量]
│  ├─ LabeledSlider [BGM]      ───●─────  0.70
│  └─ LabeledSlider [SFX]      ─────●───  0.85
├─ SectionHeader [画质]
│  └─ RadioGroup  ( ) 低   (●) 中   ( ) 高
├─ SectionHeader [按键]
│  ├─ Label [移动]   KeyBindButton [ WASD ]
│  ├─ Label [攻击]   KeyBindButton [ 鼠标左键 ]
│  └─ Label [暂停]   KeyBindButton [ Esc ]
└─ Footer
   ├─ SecondaryButton [取消]
   └─ PrimaryButton   [保存]
```

---

## 5. 待用户确认 / 风险标记

- [ ] **风格继承基线**：art-ui 阶段 2 之前需先确认「主菜单已定的视觉风格基线在哪儿」。如果项目尚无统一风格指南，需先做一次风格快速锚定（color tokens / 字体 / 圆角 / 描边），否则设置面板会与主菜单视觉分裂。
- [ ] **重绑定按钮显示什么**：键盘 path 字符串（`<Keyboard>/escape`）不可读，需要 art-ui + client-unity 商定一个 PathToDisplayName 映射（如 `<Mouse>/leftButton` → `鼠标左键`、`<Keyboard>/wasd` → `WASD`）。阶段 4 client-unity 落实，阶段 2 art-ui 在提示词里用最终的玩家可读名。
- [ ] **3 个状态变体是否阶段 3 一起出**：MVP 倾向先只出主面板，阶段 5 不够再补。等用户确认。
- [ ] **是否需要画质档位的"分辨率/帧率"显示**：MVP 决定不显示，玩家只看「低 / 中 / 高」三档名。等用户确认。

---

## 6. 三表完整性自检（UI 出图前置门禁）

- [x] 1. 页面清单 ✅
- [x] 2. 复用组件清单 ✅
- [x] 3. 组件状态表 ✅

> 三表齐全 + 用户审阅确认后才能进入阶段 2（art-ui 写效果图提示词）。
