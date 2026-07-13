# actor_common 概念图生成记录

- 日期：2026-07-13
- 产物用途：通用玩家主体（`Player` / `SmartAI` / `LightAI`）后续四视图与帧动画的主体基准；不是可直接入库的最终 Sprite。
- 生成方式：内置 `image_gen`，`stylized-concept`。
- 原始生成图：`actor_common_concept_chromakey.png`
- 抠图成品：`actor_common_concept.png`

## 已确认的画面约束

- 仅一名中性、精瘦敏捷的刻印逃亡者；全身站立、脚部完整可见。
- 半写实厚涂的游戏概念图表现；色彩为炭黑、棕褐、旧帆布白、暗铜，无大面积蓝、红、黄。
- 角色本体无固定纹身、无纹身式疤痕、无武器、无文字或水印。
- 头侧、躯干、双臂、双腿均保留裸露皮肤区；短发与剃侧结构为后续四视图中的太阳穴和颈后贴花区提供可见空间。
- 原图使用平坦绿色抠像背景，无遮挡、无地面或投影。

## Alpha 抠图验证

- 抠图工具：`C:\Users\WIN10\.codex\skills\.system\imagegen\scripts\remove_chroma_key.py`
- 参数：`--auto-key border --soft-matte --transparent-threshold 12 --opaque-threshold 220 --despill`
- 自动检测键色：`#07f80d`。
- 成品尺寸：`1024 x 1536`，`RGBA`。
- 透明像素：`1,258,099 / 1,572,864`；半透明像素：`7,113`；不透明像素：`307,652`。
- Alpha 外接框：`(288, 50, 759, 1458)`；四个角的 Alpha 均为 `0`。
- 目视检查：边缘没有明显绿色漏边；轮廓、裸露皮肤区与足部完整。
- 结论：通过，`actor_common_concept.png` 可作为透明背景概念参考。

## 完整提示词

```text
Use case: stylized-concept
Asset type: game character concept reference, master visual reference for later four-view turnaround and animation
Primary request: Full-body concept art of exactly one androgynous, lean, agile escaped experimental subject in a post-apocalyptic dark totem world. Close-cropped hair with shaved sides; both temples and the nape visibly exposed. A short sleeveless mantle ending above the waist, crossed narrow chest straps, exposed collarbone through upper abdomen; both shoulders, upper arms and forearms clearly bare. Tactical shorts, minimal light knee guards, exposed outer thighs and front-outer calves. The body must be completely tattoo-free: preserve clean, readable bare-skin decal zones at head/temples+nape, torso chest/upper abdomen, left arm, right arm, left leg, and right leg for later runtime tattoo decals. Charcoal, umber, aged canvas white and dark copper materials; no large blue, red, or yellow color blocks.
Style/medium: semi-realistic painterly game concept art, deliberate heavy brushwork, physically plausible anatomy, readable large silhouette, restrained 2.5D action-game design. No fixed tattoos, no body paint, no scars resembling tattoos.
Composition/framing: complete standing body, neutral relaxed idle pose, facing slightly three-quarter front, arms slightly away from torso so both arms and torso remain visible, feet fully visible, generous padding around the entire figure.
Scene/backdrop: perfectly flat solid #00ff00 chroma-key background only. No floor plane, no cast or contact shadow, no gradient, no texture, no glow, no reflection, no background props.
Constraints: one character only; no weapon; no text; no watermark; crisp silhouette edges; do not let hair, armor, belts, cloth, sleeves, long pants or boots cover the six decal zones; do not use #00ff00 in the character.
```

## 完整性校验

- `actor_common_concept_chromakey.png` SHA-256：`D0DA8808D85CEA8E39BFF771809BBF5D3C16414C724EC9CD169B746D5BE10FEE`
- `actor_common_concept.png` SHA-256：`3DC5DEFFC989CC659E6C0FAE1705FDD671CF20D16365682DBB6F05D5FCBCD98E`
