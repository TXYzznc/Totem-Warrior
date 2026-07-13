# 美术生成提示词

## 状态

- 美术素材状态：待处理
- 处理日期：2026-07-13
- 输出目录：`art/raw/`
- 生成记录：`art/raw/生成记录.md`

## 全局视觉约束

角色类资源采用半写实厚涂游戏概念设计：手绘厚涂笔触、清晰大轮廓、低饱和炭黑/灰褐/旧帆布白/暗铜材质；不得复制特定作品。角色主体与 NPC 必须在纯 #00ff00 chroma-key 背景上生成，背景无阴影、无渐变、无地面、无文字；后续抠出透明 alpha。所有角色图不含固定纹身。

## actor_common 概念图

Use case: stylized-concept
Asset type: game character concept reference
Primary request: full-body concept art for a handsome young adult male protagonist with a subtle androgynous experimental-subject edge, lean athletic build, sharp jawline, confident cool expression, textured short hair with shaved sides, exposed temples and nape. Wear a sleeveless cropped tactical mantle, stylish dark functional straps, subtle cool-metal hardware, exposed collarbone to upper abdomen, both shoulders to forearms bare, tactical shorts, light knee guards, visible outer thighs and front-outer calves. No tattoos anywhere; these six skin areas must remain clearly readable for later runtime decals. Neutral charcoal, umber, aged canvas white and dark copper palette; no large blue, red or yellow color blocks.
Style/medium: semi-realistic painterly game concept art, heavy brushwork, readable silhouette, restrained 2.5D action-game styling.
Composition/framing: complete standing body, neutral relaxed pose, generous padding, feet visible.
Constraints: one character only; no weapon; no text; no watermark; no armor or hair covering the six decal zones; perfectly flat #00ff00 chroma-key background only.

## boss_ai_ruins_warden 概念图

Use case: stylized-concept
Asset type: game boss concept reference
Primary request: full-body concept art for an AI ruins warden, a towering upright bipedal mechanical guardian twice the perceived mass of the player. Obsidian armor plates, oxidized copper skeleton, fractured totem stone slabs, broad shoulders, narrow waist, massive feet, simple sensor-mask head, exposed chest energy core glowing cold white to cyan, shoulder-back floating fragments or folded summon mechanisms. The silhouette must support stomp, beam and summon attacks.
Style/medium: semi-realistic dark painterly game concept art, heavy brushwork, weathered metal and stone surfaces.
Composition/framing: complete standing body, front three-quarter pose, feet visible, generous padding.
Constraints: one Boss only; no background scene; no text; no watermark; perfectly flat #00ff00 chroma-key background.

## static NPCs and placeholder portraits

- Tattooist: retain a mature tattoo artist identity, visible purple glowing tattoo accents only on the reference character, layered workshop clothes, calm expert expression, full body three-quarter standing Sprite.
- Merchant: retain a charismatic merchant identity, warm copper, red-brown textiles, coin in hand, practical satchel, full body three-quarter standing Sprite.
- player_2: half-body portrait of an agile female wasteland signal hunter, partial shaved hair and short braids, sleeveless outfit and cropped mantle, alert expression.
- player_3: half-body portrait of a tall male unstable augmented subject, short hair, torn laboratory straps, exposed upper body, restrained but dangerous expression.

All use the global visual constraints. NPC bodies use #00ff00 chroma-key backgrounds; portraits use the existing CharacterSelect visual framing and do not need alpha.

## Animation canvas prompt suffix

Create exactly one character, one action, one cardinal direction per source canvas. Arrange exactly <FRAME_COUNT> sequential poses horizontally, each in a 512 by 512 cell, no grid lines, no labels and no overlap between cells. Every pose must match the approved turnaround exactly, preserve the same feet position and foot pivot, and use the same flat #00ff00 chroma-key background. Action: <ACTION>. Direction: <DIRECTION>. No tattoo, no VFX, no weapon trail, no text, no watermark.
