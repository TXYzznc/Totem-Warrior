# actor_common 概念图生成记录 v02

- 日期：2026-07-13
- 生成方式：内置 `image_gen`，`stylized-concept`。
- 生成原因：v01 的性别气质偏女性；本版按已更新 brief 重做为明确偏男性、略带中性实验体感的主角。
- 用途：作为 `Player` / `SmartAI` / `LightAI` 共用主体的后续四视图与动画基准；不是最终可入库 Sprite。
- 原始绿幕图：`actor_common_concept_v02_chromakey.png`
- 抠图成品：`actor_common_concept_v02.png`

## 内容验收

- 仅一位成年男性主角，锐利下颌、短碎发侧剃、冷峻自信神情、精壮敏捷比例，读感明确偏男性。
- 深色机能短斗篷、机能束带与少量冷色金属件；主色为炭黑、棕褐、旧帆布白、暗铜，未使用大面积蓝/红/黄。
- 无固定纹身、无纹身式疤痕、无武器、无文字或水印。
- 胸腹、双肩至前臂、双侧大腿与小腿均有稳定裸露皮肤区域；短发侧剃为头部贴花区保留空间。颈后区域将在背面四视图中明确锁定。
- 单人全身三分之四正面 Idle 姿势；双脚完整、留白充分；绿色背景无地面、阴影、渐变或道具。

## Alpha 抠图验证

- 抠图工具：`C:\Users\WIN10\.codex\skills\.system\imagegen\scripts\remove_chroma_key.py`
- 参数：`--auto-key border --soft-matte --transparent-threshold 12 --opaque-threshold 220 --despill`
- 自动检测键色：`#05f908`。
- 成品尺寸：`928 x 1695`，`RGBA`。
- 透明像素：`1,119,975 / 1,572,960`；半透明像素：`8,636`；不透明像素：`444,349`。
- Alpha 外接框：`(179, 34, 755, 1682)`；四个角的 Alpha 均为 `0`。
- 目视检查：角色边缘没有明显绿色漏边，短发、手指、服装边缘和足部均完整。
- 结论：通过。v02 为当前建议进入四视图阶段的 `actor_common` 概念基准；v01 仅保留为可追溯的否决版本。

## 完整提示词

```text
Use case: stylized-concept
Asset type: game character concept reference, master visual reference for later four-view turnaround and animation
Primary request: Create exactly one handsome young adult MALE protagonist, a cool male lead with a subtle experimental-subject edge, unmistakably masculine facial structure and masculine proportions. Lean athletic and agile rather than bulky: defined shoulders, flat masculine chest, narrow waist, long toned legs, sharp jawline, straight brows, confident cold expression. Short textured masculine fringe haircut with clearly shaved sides, exposed temples and nape. Wear a sleeveless cropped dark tactical mantle, stylish dark functional crossed straps, subtle cold-steel metal hardware, exposed collarbone through upper abdomen, both shoulders to forearms bare, fitted tactical shorts, minimal light knee guards, exposed outer thighs and front-outer calves. The entire body must be completely tattoo-free: preserve clean readable bare-skin decal zones at head/temples+nape, torso chest/upper abdomen, left arm, right arm, left leg, and right leg for later runtime tattoo decals. Palette: charcoal, umber, aged canvas white, dark copper with tiny restrained cold-steel highlights; no large blue, red, or yellow color blocks. The subject must read immediately as a stylish, charismatic male action-game protagonist, not female and not feminine.
Style/medium: semi-realistic painterly game concept art, controlled heavy brushwork, high-quality character key art, physically plausible male anatomy, readable large silhouette, restrained 2.5D action-game design.
Composition/framing: complete standing body, neutral relaxed idle pose, facing slightly three-quarter front, arms held slightly away from torso so arms and torso remain visible, both feet fully visible, generous padding.
Scene/backdrop: perfectly flat solid #00ff00 chroma-key background only. No floor plane, no cast or contact shadow, no gradient, no texture, no glow, no reflection, no background props.
Constraints: one male character only; no weapon; no text; no watermark; no fixed tattoos, no body paint, no scars resembling tattoos; do not let hair, armor, belts, cloth, sleeves, long pants or boots cover the six decal zones; do not use #00ff00 in the character.
```

## 完整性校验

- `actor_common_concept_v02_chromakey.png` SHA-256：`83B595B80928160DC8C1876AC77994EDFC9F4DB68F3B9FFF9E3276444EFF6EBD`
- `actor_common_concept_v02.png` SHA-256：`612FEDDDF9F89DFF80B6EAFAD0353E92C89AEA220079D59A75077D55B50EC671`
