美术素材状态: 已处理（21/21 Codex 高质量）
处理日期: 2026-06-24 → 2026-06-25
执行 SKILL: codex-image-gen
输出目录: art/raw/
生成记录: art/raw/生成记录.md

# 美术需求 — 01-tattoo-framework-rewrite Phase D

> **状态**：已交付（4 张降级图等 Codex 配额恢复后可重跑）
> **创建日期**：2026-06-24
> **承担方**：主对话 + `art-director` + `codex-image-gen` SKILL
> **目标用户**：玩家在 CombatHUD（UI Toolkit）下拉中辨认部位 / 颜色 / 图案，并在战斗中看到 VFX 反馈

---

## 1. 范围与原则

**核心原则**：「单色 + 几何符号」极简风，避免任何写实角色立绘。
**理由**：
- 当前是 grey-box 演示阶段，玩家只需快速分辨 21 个原子的语义
- AI 出图越简单越容易达成「一致性」+ 量产
- 后期可不破坏接口替换为高规格资源

**主色板**：暗色科技风（背景 `#1a1f2e` 系列，已在 [CombatHUD.uss](../../../../Assets/UI/CombatHUD.uss) 落地）

**目标分辨率**：所有图标统一 **256×256 PNG，透明背景**

---

## 2. 三类素材清单（共 21 张图）

### 2.1 部位图标（6 张）

| ID | 文件名 | 视觉概念 | 关键符号 |
|---|---|---|---|
| Head | `part_head.png` | 侧面颅骨剪影，眉眼线条 | 三角眼罩 + 头骨轮廓 |
| Torso | `part_torso.png` | 躯干胸甲剪影 | 中央十字纹 + 肋骨 |
| LeftArm | `part_left_arm.png` | 左臂剪影向上 | 长方形臂膀 + 拳套 |
| RightArm | `part_right_arm.png` | 右臂剪影持武器 | 镜像 LeftArm + 剑柄 |
| LeftLeg | `part_left_leg.png` | 左腿剪影前跨 | 闪避虚影线 |
| RightLeg | `part_right_leg.png` | 右腿剪影跑步姿 | 镜像 + 流线尾迹 |

**统一风格**：纯白前景剪影 + 透明背景 + 边缘 4px 描边为 `#7c8aa8`。

### 2.2 颜色图标（7 张）

| ID | 文件名 | 元素 | 视觉 |
|---|---|---|---|
| Red | `color_red.png` | 火焰 | 圆形 + 跳跃火焰符号 + 桔红渐变 |
| Yellow | `color_yellow.png` | 雷电 | 圆形 + Z 字闪电 + 金黄渐变 |
| Green | `color_green.png` | 自然/毒 | 圆形 + 滴液 + 翠绿 |
| Blue | `color_blue.png` | 冰霜 | 圆形 + 雪花 + 冰蓝 |
| Purple | `color_purple.png` | 异变 | 圆形 + 漩涡 + 紫罗兰 |
| Gold | `color_gold.png` | 神圣 | 圆形 + 十字光环 + 金黄高光 |
| White | `color_white.png` | 纯能/光 | 圆形 + 多边形光晶 + 纯白蓝边 |

**统一格式**：256×256 圆形徽章，外圈描边 4px，内部渐变 + 中央符号。

### 2.3 图案图标（8 张）

| ID | 文件名 | 形状 | 视觉概念 |
|---|---|---|---|
| Line | `pattern_line.png` | 直线 | 一根粗直线，单点重击 |
| Ring | `pattern_ring.png` | 圆环 | 双层同心圆 + 四方放射 |
| Spiral | `pattern_spiral.png` | 螺旋 | 阿基米德螺旋 5 圈 |
| Zigzag | `pattern_zigzag.png` | 锯齿 | 4 段折线 |
| Bolt | `pattern_bolt.png` | 闪电 | 链式 Z 字 + 分叉 |
| Star | `pattern_star.png` | 星形 | 五角星 + 概率虚线 |
| Stream | `pattern_stream.png` | 流线 | 水流图样 + 3 道平行流线 |
| Beast | `pattern_beast.png` | 兽形 | 兽爪剪影 + 召唤符 |

**统一风格**：白色线条 + 透明背景 + 1px 内描边为冷灰。

---

## 3. 落盘路径

| 阶段 | 路径 |
|---|---|
| 原始生成 | `openspec/changes/01-tattoo-framework-rewrite/art/raw/*.png` |
| 工程使用 | `Assets/Resources/Sprites/Tattoo/{parts,colors,patterns}/*.png` |
| 引用入口 | `Assets/Resources/DataTable/ResourceConfig.json` 注册 21 条 ResourceItem |

---

## 4. 验收 DoD

- [ ] 21 张 PNG 全部生成至 `art/raw/` 且分辨率 256×256
- [ ] `art/raw/生成记录.md` 记录每张图的状态（生成成功 / 失败 / 重试）
- [ ] 21 张 PNG 拷贝至 `Assets/Resources/Sprites/Tattoo/{parts,colors,patterns}/`
- [ ] `ResourceConfig.json` 新增 21 条 ResourceItem（Key 形如 `Tattoo.Part.Head`）
- [ ] `CombatHUDForm` 通过 `ResourceModule.Load<Sprite>` 在下拉中显示对应图标

---

## 5. 风险与回退

| 风险 | 缓解 |
|---|---|
| Codex 不可用 / 凭据缺失 | 自动降级：使用 `Assets/Resources/Sprites/Tattoo/_placeholder/` 下的纯色占位图 + 文字 label |
| 生成图过大（>1MB） | 经 `image_resize` MCP 压缩至 ≤256×256 后入工程 |
| 风格不统一 | 先生成 1 张 Red 圆形作 reference，其余 6 色按相同 prompt 模板替换名词 |

---

## 6. 与代码的关联

- 关联代码：[CombatHUDForm.cs](../../../../Assets/Scripts/Modules/Tattoo/UI/CombatHUDForm.cs) `BuildOptionList` 接到 ResourceModule.Load
- 关联配置：[ResourceConfig.json](../../../../Assets/Resources/DataTable/ResourceConfig.json) 新增 ResourceItem 行
- 关联文档：[05B-三层原子系统设计v2.md](../../../../开发文档/05B-三层原子系统设计v2.md)
