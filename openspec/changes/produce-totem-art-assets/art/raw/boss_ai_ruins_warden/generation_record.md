# boss_ai_ruins_warden — 生成记录

- 生成日期：2026-07-13
- 产物类型：Boss 全身概念图（仅概念参考；未制作四视图或动画）
- 生成方式：内置 image generation，后续本地 chroma-key 抠图
- 概念方向：AI 遗迹执政官；直立双足、黑曜石甲片、锈铜骨架、破碎图腾石板、冷白至青蓝胸腔核心、厚重双足、肩背召唤构件。

## 文件

- 原始绿幕图：`boss_ai_ruins_warden_concept_chromakey.png`
- 已抠 Alpha 图：`boss_ai_ruins_warden_concept_alpha.png`

## 生成提示词

```text
Full-body concept art for an AI ruins warden, a towering upright bipedal mechanical guardian twice the perceived mass of a human player. Obsidian armor plates, oxidized copper skeleton, fractured totem stone slabs, broad shoulders, narrow waist, massive heavy feet, simple sensor-mask head, exposed chest energy core glowing cold white to cyan, and shoulder-back floating fragments or folded summon mechanisms. Its silhouette must clearly support stomp, beam, and summon attacks. Semi-realistic dark painterly game concept art, heavy brushwork, weathered metal and stone surfaces, clear readable silhouette. Exactly one complete standing body, front three-quarter pose, feet entirely visible, generous empty padding. Perfectly flat solid #00ff00 chroma-key background only; no floor plane, cast/contact shadow, text, watermark, or #00ff00 in the subject.
```

## 抠图验证

- 输出格式：RGBA PNG，1024 × 1536。
- 四个角像素均为 `(0, 0, 0, 0)`，背景已透明。
- Alpha 覆盖范围：`(97, 38, 929, 1500)`，角色主体完整且有足够边距。
- 透明像素：850,626 / 1,572,864（54.08%）。
- 半透明边缘像素：24,627；不透明主体像素：697,611。
- `alpha > 32` 的绿色泄漏像素：0。
- 目视检查：轮廓、漂浮肩背构件与脚部完整，无明显绿幕边缘。

## 限制说明

此图仅作为后续四视图与逐方向动画的一致性参考，尚未导入 Unity，也未修改任何项目资产或索引。
