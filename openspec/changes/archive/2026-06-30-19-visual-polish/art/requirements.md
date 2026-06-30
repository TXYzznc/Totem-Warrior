# art/requirements.md — 19-visual-polish

**状态**: 待处理
**创建**: 2026-07-01
**codex-image-gen 入口**: 主对话读此文件后直接调 codex，无需再问用户

---

## 美术需求总览

| 编号 | 素材名称 | 类型 | 目标尺寸 | 目标路径 | 状态 |
|---|---|---|---|---|---|
| ART-01 | burn（灼烧状态图标） | 游戏图标 sprite | 64x64 PNG | `Assets/Resources/Sprite/UI/StatusIcon/burn.png` | 待出图 |
| ART-02 | poison（中毒状态图标） | 游戏图标 sprite | 64x64 PNG | `Assets/Resources/Sprite/UI/StatusIcon/poison.png` | 待出图 |
| ART-03 | stun（眩晕状态图标） | 游戏图标 sprite | 64x64 PNG | `Assets/Resources/Sprite/UI/StatusIcon/stun.png` | 待出图 |
| ART-04 | hitspark 粒子参考图 | 参考 / 概念图 | 512x512 PNG | `openspec/changes/19-visual-polish/art/raw/hitspark_ref.png` | 待出图 |
| ART-05 | 暴击飘字字体效果参考 | 参考 / 概念图 | 512x256 PNG | `openspec/changes/19-visual-polish/art/raw/crit_text_ref.png` | 待出图 |

---

## ART-01：burn 灼烧状态图标

**用途**: 敌人/玩家头顶，表示当前处于灼烧状态
**风格**: 像素艺术风格（pixel art），16x16 原始像素放大到 64x64
**核心元素**: 火焰图标，橙红色调，轮廓清晰
**颜色参考**: 主色 #FF6622（橙红）、高光 #FFAA44（亮黄橙）、底色透明
**格式要求**: PNG，透明背景，无 padding，正方形

---

## ART-02：poison 中毒状态图标

**用途**: 敌人/玩家头顶，表示当前处于中毒状态
**风格**: 像素艺术风格（pixel art），16x16 原始像素放大到 64x64
**核心元素**: 毒液滴/骷髅图标，绿紫色调
**颜色参考**: 主色 #44BB22（毒绿）、暗色 #226611（深绿）、点缀 #AA44FF（紫色）
**格式要求**: PNG，透明背景，无 padding，正方形

---

## ART-03：stun 眩晕状态图标

**用途**: 敌人/玩家头顶，表示当前处于眩晕状态
**风格**: 像素艺术风格（pixel art），16x16 原始像素放大到 64x64
**核心元素**: 星星/螺旋图标，黄蓝色调
**颜色参考**: 主色 #FFDD22（亮黄）、点缀 #2244FF（蓝色）、轮廓 #FFFFFF（白色）
**格式要求**: PNG，透明背景，无 padding，正方形

---

## ART-04：hitspark 粒子参考图（概念图，非实际游戏资源）

**用途**: 给 client-ta 参考，指导粒子颜色和形态
**描述**: 白色与橙红色混合的爆炸粒子效果概念图，暗色背景，放射状光点，有动感拖尾感
**尺寸**: 512x512（宽松，概念图）

---

## ART-05：暴击飘字效果参考图（概念图，非实际游戏资源）

**用途**: 给 client-unity 参考，确认红色大字飘字视觉风格
**描述**: 游戏截图风格，暗色背景，右上方有一个红色加粗数字（如 "CRIT 247"）正在上浮，左侧有一个白色小数字（如 "83"）对比；字体有轻微描边
**尺寸**: 512x256（横版）

---

## 出图优先级

1. ART-01 burn（19-B 必需，最高优先）
2. ART-02 poison（19-B 必需）
3. ART-03 stun（19-B 必需）
4. ART-05 暴击飘字参考（19-A 辅助参考）
5. ART-04 hitspark 参考（19-C 辅助参考，可跳过）

---

## 素材规范（来自美术资源规范.md）

- 格式：PNG，透明背景
- 游戏图标（ART-01~03）：64x64，< 20KB 每张
- 概念参考图（ART-04~05）：< 200KB

---

## 出图后行动

1. ART-01~03：移动到 `Assets/Resources/Sprite/UI/StatusIcon/`
2. ART-04~05：留在 `art/raw/` 作为参考
3. 更新本文件头部「状态」字段为「已处理」
4. 写 `art/raw/生成记录.md`
