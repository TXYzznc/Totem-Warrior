# SelfTattooForm 美术资源说明

## 基本信息
对应 Form：SelfTattooForm（Assets/Scripts/Modules/Tattoo/UI/SelfTattooForm.cs）
对应 Prefab：Assets/Resources/Prefab/UI/SelfTattoo.prefab
原始需求出处：openspec/changes/12-core-ui-screens/art/requirements.md（表 A 第 10 页面）

## 功能说明
任意地点按 Tab 唤出的居中覆盖层纹身选择面板。流程：
1. 选择纹身部位（人体轮廓 6 个热点按钮）
2. 选择颜色（7 个颜色按钮，颜色用 Image.color 表达，无独立素材）
3. 选择图案（8 个图案按钮，复用 Sprite/Tattoo/Pattern/ 业务素材）
4. 预览组合
5. 触发纹身进程（开始/取消按钮）

## 文件清单（7 张图）

| 文件名 | 用途 | Prefab 引用 | 状态 |
|---|---|---|---|
| SelfTattooForm_bg.png | 面板背景 | ✓ | OK |
| SelfTattooForm_body_idle.png | 人体轮廓示意（普通态） | ✓ | OK |
| SelfTattooForm_body_part_selected.png | 人体轮廓示意（部位高亮） | ✓ | OK |
| SelfTattooForm_color_selected_glow.png | 颜色按钮选中态高亮光效（脚本 _colorSelectedGlow 字段引用） | ✓ | OK |
| SelfTattooForm_color_locked_gray.png | 颜色按钮锁定状态指示 | ✓ | 待联调确认 |
| SelfTattooForm_hourglass_icon.png | 沙漏图标（读条时长） | ✓ | OK |
| SelfTattooForm_divider_icon.png | 区段分隔装饰 | 待确认 | - |

## 颜色按钮（无独立素材）

按用户设计：颜色不需要美术资源，颜色按钮直接用 Image.color tint 表达。
7 个 ColorButton_1~7 的 Image.color 按 TattooColorConfig Id 1-7 顺序设：

| colorId | Name | Element | RGB (hex) |
|---|---|---|---|
| 1 | Red    | Fire      | #E74C3C |
| 2 | Yellow | Lightning | #F1C40F |
| 3 | Green  | Nature    | #2ECC71 |
| 4 | Blue   | Frost     | #3498DB |
| 5 | Purple | Mutation  | #9B59B6 |
| 6 | Gold   | Holy      | #F39C12 |
| 7 | White  | Pure      | #ECF0F1 |

## 图案按钮（复用业务素材）

8 个 PatternButton_1~8 的 PatternIcon 子节点 Image.sprite 指向 Sprite/Tattoo/Pattern/<Name>.png，
按 TattooPatternConfig Id 1-8 严格对齐：

| patternId | Name | 素材路径 |
|---|---|---|
| 1 | Line   | Sprite/Tattoo/Pattern/Line.png |
| 2 | Ring   | Sprite/Tattoo/Pattern/Ring.png |
| 3 | Spiral | Sprite/Tattoo/Pattern/Spiral.png |
| 4 | Zigzag | Sprite/Tattoo/Pattern/Zigzag.png |
| 5 | Bolt   | Sprite/Tattoo/Pattern/Bolt.png |
| 6 | Star   | Sprite/Tattoo/Pattern/Star.png |
| 7 | Stream | Sprite/Tattoo/Pattern/Stream.png |
| 8 | Beast  | Sprite/Tattoo/Pattern/Beast.png |

设计意图：UI 上点击的图案 = 未来角色身上贴的图案 = 同一套，WYSIWYG。

## 历史变更（2026-06-29）

之前由 codex-image-gen 出图阶段生成了 7 张错颜色（red/blue/purple/gold/white/black/...）
和 8 张错图案（lightning/wave/dragon 等命名与 DataTable 权威不一致）共 15 张图，
本次按"颜色用 tint、图案复用业务素材"的设计澄清，已全部清理；
ResourceConfig.json 同步删除 7 条 Tattoo.Color.* 条目（Id 1101-1107）。

## 保留素材的依据

- color_selected_glow.png：脚本 _colorSelectedGlow 字段引用，必留
- color_locked_gray.png：prefab 有 1 个独立装饰节点引用，未接到代码；阶段 6 联调时确认是否保留
- divider_icon.png：暂未在 prefab 中找到引用，留作阶段 6 联调时按 mockup 排版可能需要
