
# UI 元素提示词生成器

## 核心设计理念

### 项目美术风格
- **整体风格**：像素化UI，与角色和敌人风格统一
- **质感**：金属科幻感，厚重感，棱边清晰
- **色彩体系**：蓝紫渐变为主，金色边框点缀，高对比
- **装饰**：极简，避免复杂纹理，强调几何边框

### UI元素分类与色彩体系

| 元素类型 | 用途 | 尺寸 | 主色 | 边框 | 高光 |
|---------|------|------|------|------|------|
| **按钮-正常** | 菜单/选关 | 200x60px | 蓝紫渐变 | 金色2px | 顶部白线 |
| **按钮-悬停** | 高亮状态 | 200x60px | 更亮蓝紫 | 金色加粗 | 更明显高光 |
| **血量条背景** | HUD | 200x20px | 灰色 | 白色1px | 无 |
| **血量条填充** | HUD绿→红 | 200x20px | 绿→黄→红渐变 | 无 | 无 |

### 设计细节
1. **按钮**：圆角矩形，内角半径8px，支持9-slice拉伸
2. **边框**：清晰金色2像素边框，表现科幻金属感
3. **高光**：顶部白色细线（1px），强化金属质感
4. **文本背景**：深蓝(#0F1932)，文本白色或黄色

### 推理示例
**需求**："生成菜单按钮-正常态"  
**推理**：按钮 → 200x60 → 蓝紫渐变 → 金边 → HD像素  
**提示词关键字**：`HD pixel art, UI button 200x60 pixels, blue-purple gradient background, gold border 2px, white highlight top edge, transparent background, sci-fi style`

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
5. 如果生成结果带有棋盘格，请不要直接交付，请先后处理移除背景并导出真实透明 PNG。

这个提示词能保证AI绘制的图片是真正的PNG格式图片。
```

---

## 关键规范

- **透明背景**：UI 素材通常需要 `transparent background (PNG)`，方便 Unity 中直接使用
- **九宫格适配**：面板/按钮描述中加 `designed for 9-slice sprite (corners and edges tileable)`
- **尺寸清晰**：注明具体像素尺寸，如 `200x60 pixel button`
- **状态完整**：重要按钮最好说明需要 Normal/Hover/Pressed 三种状态
