# boss_ai_ruins_warden — 四视图生成记录

- 生成日期：2026-07-13
- 参考图：`../boss_ai_ruins_warden_concept_alpha.png`（唯一角色参考）
- 生成方式：内置 image generation；每个方向独立生成绿幕源图，再使用本地 chroma-key 工具输出透明 PNG。
- 范围：仅四视图；未生成动画，未修改 `Assets/`。

## 成品与绿幕源文件

| 方向 | 透明成品 | 绿幕源图 |
| --- | --- | --- |
| front | `boss_ai_ruins_warden_front.png` | `boss_ai_ruins_warden_front_chromakey.png` |
| back | `boss_ai_ruins_warden_back.png` | `boss_ai_ruins_warden_back_chromakey.png` |
| left | `boss_ai_ruins_warden_left.png` | `boss_ai_ruins_warden_left_chromakey.png` |
| right | `boss_ai_ruins_warden_right.png` | `boss_ai_ruins_warden_right_chromakey.png` |

## 生成约束

所有方向均锁定相同主体：直立双足的 AI 遗迹执政官、黑曜石甲片及蓝色氧化磨损、锈铜骨架缝隙、断裂图腾石板、窄腰宽肩和巨型厚足；胸前为冷白至青蓝圆形核心，肩背维持漂浮碎片／折叠召唤构件。姿势为中性 Idle 站立，完整脚部、纯 `#00ff00` 绿幕、无投影、无文字或水印。

## Alpha 验证

| 方向 | 尺寸 | Alpha 主体边界 | 覆盖率 | 四角透明 | 绿边泄漏（alpha > 32） |
| --- | --- | --- | ---: | --- | ---: |
| front | 1024 × 1536 | `(122, 34, 899, 1489)` | 41.92% | 是 | 0 |
| back | 1024 × 1536 | `(113, 51, 910, 1484)` | 42.47% | 是 | 0 |
| left | 1024 × 1536 | `(226, 42, 791, 1499)` | 27.66% | 是 | 0 |
| right | 1024 × 1536 | `(215, 27, 852, 1485)` | 28.86% | 是 | 0 |

目视检查完成：四张 Alpha 成品的轮廓、脚部和肩背构件完整，材质与核心识别一致，未见明显绿色边缘。左右图是相对侧向的独立站立视图，胸腔核心按角度仅部分可见。
