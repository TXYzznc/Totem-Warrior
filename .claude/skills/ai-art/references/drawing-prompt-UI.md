
# UI 元素提示词生成器

## UI 出图前置：先定表（强制）

> **核心方法论**：独立游戏做 UI，第一步不是出图，而是先定表。AI 可以帮你提高视觉效率，但前面的结构判断、页面取舍、状态规划，还是要人先定。先定表，再出图，会稳很多。

### 为什么必须先定表

实际进项目时，最容易卡住的不是第一张图不好看，而是后面发现：**页面不全、按钮没状态、风格不能延续、开发也不知道该怎么拆**。提前列三张表能省下大量返工：

| 痛点 | 缺失的表 | 后果 |
|---|---|---|
| 页面规划缺位 | 表 A 页面清单 | 后面每加一个功能 AI 都可能画成另一套游戏 |
| 复用结构缺位 | 表 B 复用组件清单 | 按钮、标题条、资源栏、弹窗底板各画一套，风格不延续 |
| 组件状态缺位 | 表 C 组件状态表 | 图再好看，开发也很难直接用——按钮无 pressed/disabled、页签无 selected/unselected、弹窗无确认/取消/关闭 |

### AI 自动行为约束

当 ai-art 检测到当前 change 需要 UI 类型素材（requirements 或 prompts 描述包含按钮 / 面板 / HUD / 弹窗 / 标签页 / 输入框 / 进度条 / 标题条 等），**必须**：

1. **主动起草三表骨架**，写入 `art/requirements.md`（不要等用户自己列）
2. **明示用户**：「以下三表为 AI 起草的骨架，请审阅修订后再进入出图阶段」
3. **数量字段**：表 B「目标数量」列不要预设默认数字（如"4-6 个"）——根据表 A 的页面清单**自行推算**当前项目实际需要的数量
4. **门槛**：三表缺一或用户未确认，**不得**进入 `prompts.md` 提示词生成

### 三表骨架模板

#### 表 A：页面清单（最小可用 UI）

```markdown
| 页面 | 优先级 | 备注 |
|---|---|---|
| <页面名> | 必做 / 可复用 / 后补 | <为什么这个优先级> |
```

**三类分法**：
- **必做**：核心玩法离不开的界面（如主菜单、战斗 HUD、结算）
- **可复用**：按钮、弹窗、卡片、页签等组件层（不是完整页面，是复用元素的集合）
- **后补**：商店、成就、活动等非首版功能

#### 表 B：复用组件清单（根据表 A 推算）

```markdown
| 组件 | 目标数量 | 用途 / 下一步 |
|---|---|---|
| <组件名> | <根据表 A 推算的实际数量> | <用在哪些页面 / 下一步要补什么> |
```

**常见复用组件类别**（不强制全列，按当前 change 实际需要选）：
主按钮 / 次级按钮 / 小按钮 / 关闭按钮 / 输入框 / 选中框 / 横向分割线 / 竖向分割线 / 装饰分割线 / 面板角花 / 边框装饰 / 标签页（active）/ 标签页（inactive）/ 滑动条 / 进度条 / 大面板底板 / 中面板底板 / 小面板底板 / 标题条 / 资源栏

#### 表 C：组件状态表

```markdown
| 组件 | 必备状态 | 备注 |
|---|---|---|
| 按钮 | normal / pressed / disabled | 引擎可做蒙版变色，后期美术优化时一般会二次制作 |
| 页签 | selected / unselected | 必须成套，否则切换没视觉反馈 |
| 弹窗 | 确认 / 取消 / 关闭 | 三按钮配套 |
| <其他组件按需追加> | | |
```

### 流程顺位

```
用户提 UI 美术需求
       ↓
ai-art 检测到 UI 类型
       ↓
✱ 自动起草三表骨架 → art/requirements.md
       ↓
明示用户「请审阅修订」
       ↓
用户确认（或修订完成）
       ↓
进入 prompts.md 提示词生成（按下方模板）
       ↓
codex-image-gen 实际出图
```

> 三表是 UI 类型独有的前置门槛。CHARACTER / ICON / SCENE / COMMON 四种类型不走该流程，直接进提示词生成。

---

## 核心设计理念

### 项目美术风格 —「黑暗史诗手绘肉鸽」

- **质感**：厚涂手绘 + 史诗感（**不是扁平 SVG**），有油彩笔触、有金属/木质质感
- **核心组件**：金属边框（外）+ 木质 / 兽皮 / 羊皮纸内衬 + 厚涂主体
- **装饰繁简**：中等装饰，边框四角点缀末日纹样
- **边框按主题切换**：进 AI 区边框带机械齿轮、外星区带生体触手、病毒区带真菌肿块、默认中性带金属铆钉

### UI 元素分类与色彩体系

| 元素类型 | 用途 | 构成 | 主色 | 备注 |
|---------|------|------|------|------|
| **HUD 血条** | 屏顶血量 | 厚涂深红填充 + 金属边框 | 红 #c8302e + 金 #d4af37 | 可 9-slice |
| **HUD 技能槽** | 屏底 4 个技能 | 金属圆角槽 + 厚涂图标 | 暗褐 #3a2418 + 金边 | 槽内嵌 ICON |
| **缩圈倒计时** | 屏顶 | 卷轴底 + 厚描数字 | 羊皮纸 #d4b896 + 暗红字 | 史诗感 |
| **暂停菜单面板** | 全屏 | 大幅羊皮纸 + 四角金属装饰 | 羊皮纸 + 暗红血字 | 史诗 |
| **纹身师工作室面板** | 多面板布局 | 木质底 + 铜钉 + 人体图鉴 | 暗木 #4a2818 + 铜 #b87333 | 重头戏 |
| **死亡宝箱面板** | 拾取交互 | 金属边框 + 物品格子 | 暗灰 + 物品稀有度光 | 紧凑 |
| **构筑预览（远观）** | 看其他玩家纹身 | 描边轮廓 + 发光纹身位置 | 透明底 + 纹身元素色 | 不挡视野 |

### 主题边框三套（同一形状不同元素）

| 主题 | 边框装饰 | 颜色调整 |
|------|--------|---------|
| **AI 区** | 四角机械齿轮、铜管贯穿 | 金边偏冷青 #4a8aae |
| **外星区** | 四角生体触手、紫色裂纹 | 金边偏紫 #8a4a8e |
| **病毒区** | 四角真菌肿块、孢子飞散 | 金边偏黄绿 #8aae4a |
| **默认/中性** | 四角金属铆钉、磨损划痕 | 标准金 #d4af37 |

### 设计原则

1. **信息密度优先 + 装饰只在四角**：BR 节奏下玩家要快速读 HUD，不能被装饰干扰
2. **9-slice 友好**：所有面板/按钮的边框设计为四角 + 四边可拉伸
3. **移动端可读**：最小可视尺寸下，文字 + 图标 + 边框都还能看清
4. **史诗感来自材质而非繁复**：厚涂笔触、金属反光、木纹质感 → 史诗感；不需要堆装饰
5. **状态完整**：按钮三态 Normal / Hover / Pressed 必须明显区分

### 项目美术风格关键词集合（提示词模板填充用）

> 下方 `[项目美术风格关键词]` / `[项目 UI 美术风格关键词]` 占位符使用：
```
dark epic painterly hand-drawn UI element, heavy brushwork, ornate metal frame
border with parchment or wood inset, four-corner decoration only,
9-slice tileable edges, reference style of Darkest Dungeon UI and Diablo IV UI
```

> 下方 `[主题描述]` 占位符使用：
```
UI for post-apocalyptic roguelike battle royale, epic dark aesthetic,
mobile-friendly minimum readable size, optional theme variant
(AI / alien / virus / neutral)
```

### 推理示例

**需求**：「暂停菜单面板，默认（中性）主题」
**推理**：暂停面板 → 全屏羊皮纸 → 四角金属装饰 → 厚涂史诗感
**提示词关键字**：
```
dark epic painterly hand-drawn UI panel, large parchment background with worn
leather texture, ornate metal frame border with four-corner decoration of rivets
and weathered scratches, standard gold trim #d4af37, 9-slice tileable edges,
heavy brushwork, dark red blood-letter title bar at top, designed for pause menu,
reference Darkest Dungeon UI style, 1920x1080
```

---

## 提示词结构

```
[分辨率] [UI 元素类型] [功能描述]
Style: [项目 UI 美术风格关键词]
Material: [材质描述：木质/石质/金属/水晶/扁平/玻璃拟态等]
Color palette: [色彩代码]
Decorative elements: [装饰元素描述]
State: [正常/高亮/按下/禁用 — 需要哪种状态]
Transparent background / [或具体背景色]
No border on the outside, internal decorative borders only
Clean edges suitable for Unity UI sprite slicing
Professional game UI art style, [主题描述]

请生成真正透明背景的 PNG 图片。

要求：
1. 背景必须是真实透明通道，也就是 PNG RGBA 的 alpha=0。
2. 不要白底、不要灰底、不要棋盘格、不要"模拟透明背景"。
3. 画面中只能保留主体图形本身，主体外所有区域必须完全透明。
4. 生成后请用程序检查图片是否为 RGBA，并验证四个角落像素 alpha=0。

这个提示词能保证AI绘制的图片是真正的PNG格式图片。
```

---

## 关键规范

- **透明背景**：UI 素材通常需要 `transparent background (PNG)`，方便 Unity 中直接使用
- **九宫格适配**：面板/按钮描述中加 `designed for 9-slice sprite (corners and edges tileable)`
- **尺寸清晰**：注明具体像素尺寸，如 `200x60 pixel button`
- **状态完整**：重要按钮最好说明需要 Normal/Hover/Pressed 三种状态
