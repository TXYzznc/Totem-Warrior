
# 场景图提示词生成器

## 核心设计理念

### 项目美术风格
- **整体风格**：现代高精像素美术（HD Pixel Art）
- **世界观**：深空太空背景，科幻未来感
- **色调**：深蓝黑(#0F1932)为主，星光白(#FFFFFF)点缀
- **氛围**：宁静、辽阔、冷酷

### 场景类型与设计

| 场景 | 用途 | 特征 | 滚动方式 | 配色 |
|-----|------|------|--------|------|
| **菜单背景** | 游戏启动/选关界面 | 静止深空星野，星点密集 | 无滚动 | 深蓝黑+白星 |
| **游戏背景** | 游戏进行中 | 滚动星空，支持多层视差 | 向下滚动（表示向前） | 深蓝黑+白星 |

### 设计细节
1. **星点分布**：20-30颗白色星点，随机大小2-6像素，分布均匀但不规则
2. **深度感**：菜单背景单层；游戏背景可分为远星(慢速)和近星(快速)两层增加深度
3. **滚动速度**：游戏背景滚动0.5-1.0单位/秒，营造向前飞行感
4. **色彩约束**：避免过多颜色，仅用深蓝黑 + 白，简洁至极

### 推理示例
**需求**："生成菜单背景"  
**推理**：菜单 → 深空 → 静止 → 星点 → HD像素  
**提示词关键字**：`HD pixel art, deep space starfield, dark blue-black background, white stars scattered, 1920x1080, sci-fi atmosphere, isolated stars`

---

## 提示词结构

```
[分辨率] [场景类型] [地域/世界特征]
Key elements: [主要场景元素列表]
Color palette: [色彩代码]
Lighting: [光效/时间描述]
Atmosphere: [氛围关键词]
Background layers: [远景/中景/近景描述]
No border, no frame, no rounded corners, full bleed image filling the entire canvas
Professional game art style, [主题描述]
```

> **注意**：SCENE 生成的图片是铺满整个画布的背景画面，**不附加透明背景 PNG 要求**（不存在"主体外透明区域"的概念，强加该要求会与 `Background layers` 结构冲突）。其余类型（ICON/CHARACTER/UI）需附加，详见 `drawing-prompt-generator.md`。
