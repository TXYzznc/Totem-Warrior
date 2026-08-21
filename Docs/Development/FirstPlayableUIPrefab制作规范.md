# First Playable UI Prefab 制作规范

> 状态：实施基准。适用范围：First Playable 的 UGUI / GF UIForm；设计参考画布为 1920×1080，桌面分辨率自适应为正式交付要求。
>
> 策划字段与页面范围以 `Docs/GameDesign/03-玩家体验/01-界面流程与信息架构.md` 为准；布局尺寸以 OpenSpec 的 `art/prefab-layout.md` 为准。

## 1. GF 表单约定

- 正式页面必须由 `GF.UI` 打开，根节点同时保留 GF 的 `UIForm` 与一个 `Totem*Form` 逻辑组件。
- 现有五个注册页面保留既有 prefab 名、资源路径与脚本名，不用“UIForm Prefab And Script”重新生成同名脚本：

| Form | Prefab | 逻辑脚本 | `UIFormConfig` 路径 |
|---|---|---|---|
| 主菜单 | `MainMenu.prefab` | `TotemMainMenuForm` | `UI/MainMenu` |
| 战斗 HUD | `CombatHUD.prefab` | `TotemCombatHUDForm` | `UI/CombatHUD` |
| 暂停 | `PauseMenu.prefab` | `TotemPauseMenuForm` | `UI/PauseMenu` |
| 设置 | `Settings.prefab` | `TotemSettingsForm` | `UI/Settings` |
| 结果 | `RunResult.prefab` | `TotemRunResultForm` | `UI/RunResult` |

- `Assets/GF Tools/Create/UIForm Prefab And Script` 只用于**新增且尚无逻辑类**的独立 GF Form。它会以 Prefab 文件名生成同名 `UIFormBase` 脚本并在下一次域重载后挂到根节点；随后必须改为项目的 `TotemUIFormBase`（或明确的派生基类）、编译通过，才能接入配置表。
- 本轮的构筑、情报、颜料请求、倒地、观战、开局确认、档案、帮助、制作人员和退出确认均为上述 Form 的子面板或 overlay，不新增独立 UIForm / UIViews 枚举项。
- 不在 `OnOpen` 或 `Update` 动态创建正式页面层级；动态内容仅能实例化已存在的列表 Item 模板。临时运行时生成器在对应 Prefab 完成后必须迁移或删除。
- 所有按键均通过 `TotemInputService` / InputModule；按钮只接收 UI 事件，不直接轮询 `Input`。

## 2. 目录与命名

```text
Assets/Game/Prefabs/UI/
  MainMenu.prefab
  CombatHUD.prefab
  PauseMenu.prefab
  Settings.prefab
  RunResult.prefab

Assets/Game/Sprites/UI/FirstPlayable/
  backgrounds/ buttons/ hud/ icons/ panels/
```

### 当前已交付页面

| 页面 | 资源路径 | 逻辑脚本 | 职责 |
|---|---|---|---|
| `MainMenu` | `MainMenu.prefab` | `TotemMainMenuForm` | 主菜单及本地确认、档案、帮助、制作人员、退出确认 overlay |
| `CombatHUD` | `CombatHUD.prefab` | `TotemCombatHUDForm` | 战斗 HUD、构筑、六人情报、颜料请求、观战状态 |
| `PauseMenu` | `PauseMenu.prefab` | `TotemPauseMenuForm` | 暂停与返回入口 |
| `Settings` | `Settings.prefab` | `TotemSettingsForm` | 设置草稿、预览、保存与取消 |
| `RunResult` | `RunResult.prefab` | `TotemRunResultForm` | 对局结算、成果摘要与返回入口 |

- 每个页面只使用一个 GF Form Prefab；该 Form 根节点直接拥有本页面的全部视觉子节点。不得为“每页一个专属 View”额外拆分子 Prefab。
- 仅在同一独立组件被两个或更多页面复用、且拥有稳定独立边界时，才可创建子 Prefab；创建前必须在 OpenSpec 中写清复用者与生命周期。当前 First Playable 没有这类组件。
- 根节点只承载 GF 生命周期、`Totem*Form`、Canvas 与输入组件；视觉节点直接位于根节点之下。不得保留旧视觉节点、停用的旧节点或第二套 UI 组件作为兜底。

- Prefab、脚本、公开业务类型：PascalCase；逻辑类必须以 `Totem` 开头、以 `Form` / `Item` 结尾。
- 节点为 `前缀_语义`，不得含 `/`、序号或本地化文字；同一 Form 内必须唯一。

| 节点前缀 | 用途 | 示例 |
|---|---|---|
| `Bg_` | 背景 | `Bg_Oasis` |
| `Overlay_` | 遮罩 / 模态根 | `Overlay_Build` |
| `Panel_` | 可视面板 | `Panel_Selection` |
| `Grp_` | 无视觉分组 | `Grp_MainActions` |
| `Txt_` | TMP 文本 | `Txt_Phase` |
| `Img_` | Image / RawImage | `Img_Reticle` |
| `Icon_` | 小图标 | `Icon_Fire` |
| `Btn_` | Button 根 | `Btn_StartLocal` |
| `Tgl_` | Toggle 根 | `Tgl_P01` |
| `Sld_` | Slider 根 | `Sld_MasterVolume` |
| `List_` | ScrollRect / 列表根 | `List_PlayerStats` |
| `Item_` | 默认 inactive 的模板 | `Item_LogRowTemplate` |

- SerializeField 使用 `camelCase`，与节点语义一致，例如 `Btn_StartLocal → startLocalButton`、`Txt_Phase → phaseText`。优先显式绑定；仅可用 `FindChildComponent` 为已有旧 Prefab 过渡兜底。

## 3. 组件与布局

- Form 根：`RectTransform`、`UIForm`、`Totem*Form`、`Canvas`、`CanvasGroup`、`GraphicRaycaster`。`UIFormBase` 会在打开时标准化 Canvas 和 UI Layer，预制体不重复挂多套 Canvas。
- Form 根的 `RectTransform` 必须 `Stretch/Stretch`（anchors 为 `(0,0)` 至 `(1,1)`，offsets 为 0）；根 `CanvasScaler` 必须为 `Scale With Screen Size`，参考分辨率 `1920×1080`，`Match Width Or Height = 0.5`。禁止每个页面自行使用不同参考分辨率。
- 全屏背景与 overlay：`Stretch/Stretch`。模态框：`Middle/Center`，固定设计尺寸并由根 `CanvasScaler` 缩放。HUD：按语义锚定在对应屏幕边缘/中心（例如血条左下、操作区右上、阶段条上中、准星正中），不得用“1920 内的绝对坐标”模拟另一边缘的对齐。
- 面板内部的控件可相对面板左上定位；同一面板的按钮列、数据行优先使用 Layout Group。每个 `SetTopLeft` 节点必须同时使用左上 anchor 与左上 pivot，不能只改 anchor。
- 已有页面的锚点回归入口为 `Game Framework/GameTools/First Playable UI/Validate Semantic Screen Anchors`。它校验主菜单与战斗 HUD 的关键屏幕级节点；校验失败必须先修正 Prefab，不得仅修改生成脚本或文档。
- 可视背景/面板均使用 `Image`；已有 `UI_FP_Panel_*` 时启用 Sliced，按钮四态使用已导入的 `UI_FP_Button_{Normal,Focused,Pressed,Disabled}_512x96`。
- 所有正式文本使用 `TextMeshProUGUI`，唯一字体来源为 `Assets/Game/Font/Common/SIMHEI.TTF`，唯一 TMP 字体资产为 `Assets/Game/Font/Common/SIMHEI SDF.asset`；不使用默认 TMP 字体或 Legacy `Text`。
- `Button` 的 `targetGraphic` 必须是自身 Image；状态统一为 Sprite Swap。缺失专属状态资源时可采用 tint，不能虚构 Sprite。
- 可滚动内容使用 `ScrollRect` + `Viewport` + `Content`；动态行以 `Item_*Template` 作为默认 inactive 的模板，归属对应 Form 的对象池。
- 遮罩根须 `raycastTarget=true`，所有纯装饰图片/文本为 `false`；关闭按钮位于 modal 的最后一个可选中节点。

## 4. 制作和验收顺序

1. 以 `prefab-layout.md` 建立节点树、RectTransform 与组件；先完成根 Form 和一个页面样板。
2. 绑定已入库 UI 资源；没有资源的节点保留**待制作占位**，不使用错误旧资源填充。
3. 在 `Totem*Form` 中声明并绑定 SerializeField；只把业务状态映射到现有控件，不能重新生成正式结构。
4. 将旧的运行时构造 UI 迁移到对应 Form Prefab 后删除该构造路径，避免同页重复节点；同时确认根节点不存在专属 `View_*` 子 Prefab 包装层。
5. 编译、打开/关闭、Esc 层级、输入拦截与返回主菜单回归；最后运行 GF_X 诊断和 PlayMode smoke。
6. 在 16:9、16:10 与 21:9 三种桌面宽高比下确认：全屏遮罩铺满、HUD 仍贴合语义边缘、模态框居中、无重复视觉节点或射线拦截。

## 5. 资源命名

- 已有资源保持现名；新增资源用 `T_UI_FP_<Scope>_<Semantic>_<Variant>_<Size>`（纹理/九宫格）或 `ICO_FP_<Category>_<Semantic>`（图标）。
- `Scope` 仅允许 `MainMenu`、`HUD`、`Build`、`Intel`、`Request`、`Pause`、`Result`、`Common`。
- `Variant` 仅允许 `Normal`、`Focused`、`Pressed`、`Disabled`、`Mask`、`Frame`、`Background`；尺寸以像素表达，如 `512x96`。
- 所有新文件先导入对应资源目录并核验 Sprite import / Slice；运行时引用须经 Asset Catalog 的正式键，不能靠硬编码磁盘路径。
