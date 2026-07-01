---
created: 2026-06-30
change: 17-gameplay-character-art
status: 待出图
target_tool: codex-image-gen (gpt-image-2)
total_items: 40
---

# 提示词 — 17-gameplay-character-art

## 0. 共享风格基线（所有 40 条提示词必须前置注入）

### STYLE_BASE（英文，复制到每条 prompt 头）

```
hand-painted dark fantasy 2D game character sprite, gothic occult tone, painterly textured brushwork (no pixel art, no cel-shading, no line-art outline), warm gold rim light edge highlight from upper-left, cool blue-purple ambient shadow underbelly, palette dominated by dark slate blue / deep crimson / aged gold / muted browns, top-down 3/4 perspective viewed from a camera tilted 55 degrees downward (slightly looking-down angle), character body forward-leaning to fit overhead readability, transparent PNG background, isolated subject with NO ground shadow plate.
```

### FRAME_BASE（4 帧 sprite sheet 通用）

```
4-frame horizontal sprite sheet, frames arranged left-to-right with EQUAL width, identical bounding box per frame (character bounding box must NOT shift between frames), pure transparent gutter between subjects (alpha=0), no frame number labels, no border lines between frames.
```

### NEG_BASE（负面词，复制到每条 prompt 尾）

```
no text, no signature, no watermark, no logo, no UI elements, no health bar, no ground shadow, no checkered background, no frame number, no left-right flipped duplicate (each frame independently drawn), no pixel-art mosaic, no cel-shading hard cell lines, no anime line-art, no realistic photo skin, no nudity, no blood splatter overflow, no extra characters in the scene, no weapon swap between frames.
```

### 角色一致性锚点（每个角色固化，跨方向/动作不可漂移）

| 角色 | 不可变特征（CHAR_LOCK） |
|---|---|
| Player1 | medium-build male tribal ranger, dark-tan leather chest armor, **brown leather belt with golden buckle**, crimson hooded cape draped over LEFT shoulder, **left arm fully tattooed in spiral flame motif (amber-gold + blood-red linework, clearly visible from any angle)**, dual curved daggers, leather boots with golden lacing, short black hair, weather-tanned skin |
| Player2 | slim female coastal shaman, deep purple long robe with silver star embroidery, holding a wooden staff topped with a glowing cyan crystal, **star-constellation dotted tattoo across back of neck and shoulders (cyan + silver, half-hidden under robe collar)**, head-band with woven star-map cloth, silver bracelets, long silver-grey hair tied back, pale skin |
| Player3 | stocky male snow hunter, cold-grey chainmail over fur tunic, **huge two-handed greatsword strapped diagonally across the back (visible hilt over right shoulder)**, blue wolf-pelt cape with silver clasps, wolf-pelt hood, **wolf-totem tattoo on chest (white + silver linework, partially exposed at collar)**, snow boots, weathered braided beard, frost-blue eyes |
| Boss1 | colossal armored ancient tomb-guardian, **1.6x the size of Player1**, full-plate rust-black armor with cracked aged-gold engraved runes covering every plate surface, twin-handed colossal greatsword resting on shoulder, red-eye-slit helmet with glowing crimson eye-sockets, broken iron chains hanging from waist and gauntlets, hunched menacing silhouette, NO visible skin (fully armored) |

---

## 1. Player1（16 条 — 全套）

### 1.1 Idle

**size**: 384×96 px sheet（96×96 单帧 × 4）

#### Player1_Idle_Down.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player1 CHAR_LOCK}, standing IDLE breathing pose, FACING THE CAMERA (front view, full face and chest plate visible, daggers sheathed at both hips, crimson cape behind shoulders).
Frame 1: neutral standing, arms relaxed at sides, weight even on both feet.
Frame 2: shoulders rise slightly (inhale), cape lifts ~3px.
Frame 3: identical to Frame 1 (back to neutral).
Frame 4: shoulders drop slightly (exhale), cape settles ~3px below neutral.
Loop-friendly cycle. All 4 frames share IDENTICAL feet position and IDENTICAL silhouette outline.
{NEG_BASE}
```

#### Player1_Idle_Up.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player1 CHAR_LOCK}, standing IDLE breathing pose, BACK TO THE CAMERA (rear view, hood/back of head, crimson cape covering most of back, daggers visible at both hips from behind, no face visible).
Frame 1: neutral standing, cape drapes straight down.
Frame 2: shoulders rise (inhale), cape lifts.
Frame 3: identical to Frame 1.
Frame 4: shoulders drop (exhale), cape settles.
Loop-friendly. All 4 frames share IDENTICAL feet position.
{NEG_BASE}
```

#### Player1_Idle_Left.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player1 CHAR_LOCK}, standing IDLE breathing pose, FACING SCREEN-LEFT (left-side profile view, character body turned to the left, LEFT-ARM TATTOO clearly visible facing the viewer because the tattooed arm is on the side facing the camera, dagger on left hip prominent).
Frame 1: neutral standing.
Frame 2: shoulders rise (inhale).
Frame 3: identical to Frame 1.
Frame 4: shoulders drop (exhale).
Loop-friendly. Side-profile silhouette consistent across frames.
{NEG_BASE}
```

#### Player1_Idle_Right.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player1 CHAR_LOCK}, standing IDLE breathing pose, FACING SCREEN-RIGHT (right-side profile view, character body turned to the right, tattooed left arm is now on the FAR side from the camera so only a hint of tattoo curls behind shoulder, dagger on right hip prominent).
Frame 1: neutral standing.
Frame 2: shoulders rise (inhale).
Frame 3: identical to Frame 1.
Frame 4: shoulders drop (exhale).
Loop-friendly. THIS IS NOT a mirrored Left — dagger on right hip, cape clasp on right shoulder, tattoo placement physically correct (still on left arm but mostly hidden).
{NEG_BASE}
```

### 1.2 Walk

#### Player1_Walk_Down.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player1 CHAR_LOCK}, WALKING toward the camera (front view, full face visible, cape sways).
Frame 1: LEFT foot lifted forward, right foot planted, arms swing opposite (right arm forward).
Frame 2: both feet near ground in passing pose, arms vertical, cape mid-sway.
Frame 3: RIGHT foot lifted forward, left foot planted, arms swing opposite (left arm forward).
Frame 4: passing pose, mirrored cape sway.
Loop-friendly 4-step walk cycle, vertical bob ~2px between frames.
{NEG_BASE}
```

#### Player1_Walk_Up.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player1 CHAR_LOCK}, WALKING away from the camera (rear view, back of head + cape billowing behind).
Frame 1: LEFT foot lifted forward (away into screen), cape lifts on left.
Frame 2: passing pose, cape mid-sway.
Frame 3: RIGHT foot lifted forward, cape lifts on right.
Frame 4: passing pose.
Loop-friendly 4-step walk cycle.
{NEG_BASE}
```

#### Player1_Walk_Left.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player1 CHAR_LOCK}, WALKING toward screen-left (left-side profile, tattooed left arm faces camera, cape trails to the right behind motion direction).
Frame 1: LEFT foot extended forward (leading the walk into screen-left direction), right foot trailing.
Frame 2: passing pose, both feet near vertical center.
Frame 3: RIGHT foot extended forward.
Frame 4: passing pose.
Loop-friendly 4-step side-walk cycle. Cape trails BEHIND the direction of motion (i.e., to the screen-right side).
{NEG_BASE}
```

#### Player1_Walk_Right.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player1 CHAR_LOCK}, WALKING toward screen-right (right-side profile, tattoo mostly hidden on far arm, cape trails to the left behind motion direction).
Frame 1: RIGHT foot extended forward (leading the walk into screen-right direction), left foot trailing.
Frame 2: passing pose.
Frame 3: LEFT foot extended forward.
Frame 4: passing pose.
Loop-friendly 4-step side-walk cycle. Cape trails BEHIND motion (to the screen-left side). NOT a mirror of Walk_Left — dagger/buckle positions remain physically anchored.
{NEG_BASE}
```

### 1.3 Attack（OneShot, 双匕首横扫）

#### Player1_Attack_Down.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player1 CHAR_LOCK}, performing dual-dagger SLASH attack, body facing the camera (front view).
Frame 1: WIND-UP — both daggers drawn, pulled back behind hips, body coiled, slight forward lean.
Frame 2: SLASH START — daggers swung outward in horizontal crescent, faint motion blur trail behind blades, body uncoiling.
Frame 3: IMPACT — daggers reach maximum extension forward toward camera, both arms fully extended, tiny crimson energy flash at blade tips.
Frame 4: RECOVERY — daggers retracting back toward chest, body returning to neutral stance.
One-shot 4-frame slash, NOT looping. Each frame keeps the same feet position (no walking during attack).
{NEG_BASE}
```

#### Player1_Attack_Up.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player1 CHAR_LOCK}, performing dual-dagger SLASH attack, body BACK to the camera (rear view, attacking away into screen).
Frame 1: WIND-UP — daggers pulled back to ribs, visible from behind.
Frame 2: SLASH START — arms swing outward into screen, motion trail.
Frame 3: IMPACT — daggers fully extended away from camera, crimson flash at extremity.
Frame 4: RECOVERY — arms retract.
One-shot. Same feet position across all frames.
{NEG_BASE}
```

#### Player1_Attack_Left.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player1 CHAR_LOCK}, performing dual-dagger SLASH attack, body facing screen-LEFT.
Frame 1: WIND-UP — torso twisted right (away from target), daggers cocked behind back.
Frame 2: SLASH START — torso uncoils left, daggers sweep horizontally toward screen-left.
Frame 3: IMPACT — daggers fully extended to the LEFT edge of frame, crimson flash, body fully twisted left.
Frame 4: RECOVERY — daggers pull back toward chest.
One-shot. Feet anchored. Tattooed left arm clearly visible during the slash.
{NEG_BASE}
```

#### Player1_Attack_Right.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player1 CHAR_LOCK}, performing dual-dagger SLASH attack, body facing screen-RIGHT.
Frame 1: WIND-UP — torso twisted left, daggers cocked behind back.
Frame 2: SLASH START — uncoils right, daggers sweep toward screen-right.
Frame 3: IMPACT — daggers fully extended to the RIGHT edge of frame, crimson flash, body fully twisted right.
Frame 4: RECOVERY — daggers pull back.
One-shot. Feet anchored. NOT a mirror of Attack_Left — physical anchor of belt buckle / cape clasp stays on the correct side.
{NEG_BASE}
```

### 1.4 Death（OneShot, 倒地）

#### Player1_Death_Down.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player1 CHAR_LOCK}, DYING, originally facing the camera.
Frame 1: HIT — body jerks backward, arms fly outward, daggers dropped, pained grimace, slight crimson burst at chest.
Frame 2: COLLAPSE — knees buckle, body hunches forward, daggers fall to the ground.
Frame 3: FALLING — body tilts forward and to one side, arms sprawling.
Frame 4: GROUNDED — body lies prone face-down toward camera, arms spread, cape draped over back, completely still. THIS IS THE FINAL FRAME and should look clearly LIFELESS.
One-shot, NOT looping.
{NEG_BASE}
```

#### Player1_Death_Up.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player1 CHAR_LOCK}, DYING, originally facing away from camera (rear view).
Frame 1: HIT — body arches backward (toward camera).
Frame 2: COLLAPSE — knees buckle backward.
Frame 3: FALLING — body tilts backward toward viewer.
Frame 4: GROUNDED — body lies on its back, face turned upward toward camera, cape spread out beneath, arms loose. Final lifeless pose.
One-shot.
{NEG_BASE}
```

#### Player1_Death_Left.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player1 CHAR_LOCK}, DYING, originally facing screen-left.
Frame 1: HIT — body recoils toward screen-right (away from incoming attack direction implied from left).
Frame 2: COLLAPSE — knees buckle, character drops to one knee facing left.
Frame 3: FALLING — body tilts forward onto left side.
Frame 4: GROUNDED — body lies on left side facing screen-left, daggers scattered, cape draped. Lifeless.
One-shot.
{NEG_BASE}
```

#### Player1_Death_Right.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player1 CHAR_LOCK}, DYING, originally facing screen-right.
Frame 1: HIT — body recoils toward screen-left.
Frame 2: COLLAPSE — drops to one knee facing right.
Frame 3: FALLING — body tilts onto right side.
Frame 4: GROUNDED — body lies on right side facing screen-right. Lifeless. NOT mirror of Death_Left — belt buckle anchored.
One-shot.
{NEG_BASE}
```

---

## 2. Player2（4 条 — 仅 Idle，CharacterSelect 预览用）

**size**: 384×96 px sheet

#### Player2_Idle_Down.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player2 CHAR_LOCK}, standing IDLE breathing pose, FACING CAMERA (front view, full robe pattern visible, holding staff vertically in right hand with cyan crystal glow at top).
Frame 1: neutral standing, robe hem still.
Frame 2: shoulders rise (inhale), robe hem lifts ~2px, cyan crystal glow slightly brighter.
Frame 3: identical to Frame 1.
Frame 4: shoulders drop (exhale), crystal glow dims slightly.
Loop-friendly. Identical feet position. Staff vertical at all times.
{NEG_BASE}
```

#### Player2_Idle_Up.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player2 CHAR_LOCK}, standing IDLE, BACK TO CAMERA (rear view, star-embroidery on robe back fully visible, star-constellation neck/shoulder tattoo clearly exposed above robe collar, staff in right hand).
Frame 1: neutral.
Frame 2: inhale (shoulders rise).
Frame 3: neutral.
Frame 4: exhale (shoulders drop).
Loop. Tattoo and embroidery static across frames.
{NEG_BASE}
```

#### Player2_Idle_Left.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player2 CHAR_LOCK}, standing IDLE, FACING SCREEN-LEFT (left side-profile, staff in right hand on the far side from camera so partially obscured by body, neck tattoo partially visible behind ear).
Frame 1: neutral.
Frame 2: inhale.
Frame 3: neutral.
Frame 4: exhale.
Loop.
{NEG_BASE}
```

#### Player2_Idle_Right.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player2 CHAR_LOCK}, standing IDLE, FACING SCREEN-RIGHT (right side-profile, staff in right hand prominently visible toward camera with cyan crystal in clear view, neck tattoo on far side hidden).
Frame 1: neutral.
Frame 2: inhale.
Frame 3: neutral.
Frame 4: exhale.
Loop. NOT mirror of Idle_Left — staff position physically anchored to right hand.
{NEG_BASE}
```

---

## 3. Player3（4 条 — 仅 Idle）

**size**: 384×96 px sheet

#### Player3_Idle_Down.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player3 CHAR_LOCK}, standing IDLE, FACING CAMERA (front view, full chainmail and chest wolf-totem tattoo partially visible at collar opening, greatsword hilt visible above right shoulder).
Frame 1: neutral, arms folded across chest.
Frame 2: chest rises (inhale), chainmail catches faint highlight.
Frame 3: neutral.
Frame 4: chest drops (exhale).
Loop. Greatsword hilt anchored above right shoulder across all frames.
{NEG_BASE}
```

#### Player3_Idle_Up.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player3 CHAR_LOCK}, standing IDLE, BACK TO CAMERA (rear view, FULL GREATSWORD strapped diagonally across the back is the dominant visual, wolf-pelt cape draped, hood up).
Frame 1: neutral.
Frame 2: inhale.
Frame 3: neutral.
Frame 4: exhale.
Loop. Greatsword position static.
{NEG_BASE}
```

#### Player3_Idle_Left.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player3 CHAR_LOCK}, standing IDLE, FACING SCREEN-LEFT (left side-profile, greatsword hilt visible projecting up-right behind shoulder, braided beard silhouette).
Frame 1: neutral.
Frame 2: inhale.
Frame 3: neutral.
Frame 4: exhale.
Loop.
{NEG_BASE}
```

#### Player3_Idle_Right.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Player3 CHAR_LOCK}, standing IDLE, FACING SCREEN-RIGHT (right side-profile, greatsword hilt visible projecting up-left behind shoulder).
Frame 1: neutral.
Frame 2: inhale.
Frame 3: neutral.
Frame 4: exhale.
Loop. NOT mirror of Idle_Left — sword strap physically anchored.
{NEG_BASE}
```

---

## 4. Boss1（16 条 — 全套，体型 1.6× Player）

**size**: **512×128 px sheet**（128×128 单帧 × 4）— 比 Player 大一档

### 4.1 Idle

#### Boss1_Idle_Down.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Boss1 CHAR_LOCK}, standing IDLE menacingly, FACING CAMERA (front view, towering full-plate armor, crimson eye-slits glowing, twin-handed greatsword resting point-down in front of body with both gauntlets on the pommel).
Frame 1: neutral hulking stance, armor plates static.
Frame 2: chest plate rises slightly (slow heavy breathing), crimson eyes pulse brighter.
Frame 3: identical to Frame 1.
Frame 4: chest plate drops, eyes dim slightly.
Loop. Slow heavy breathing rhythm. Frame size 128x128 (boss is ~1.6x Player1).
{NEG_BASE}
```

#### Boss1_Idle_Up.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Boss1 CHAR_LOCK}, standing IDLE, BACK TO CAMERA (rear view, massive back-plate with cracked golden engraved runes dominates the frame, broken iron chains hang from waist, helmet rear visible).
Frame 1: neutral hulking stance.
Frame 2: shoulders rise (slow breath).
Frame 3: neutral.
Frame 4: shoulders drop.
Loop. Chains sway ~1-2px between breaths.
{NEG_BASE}
```

#### Boss1_Idle_Left.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Boss1 CHAR_LOCK}, standing IDLE, FACING SCREEN-LEFT (left side-profile, helmet eye-slit glowing crimson visible on the left side, greatsword held point-down on far side of body so half-obscured, chains hang from belt).
Frame 1: neutral.
Frame 2: slow breath rise.
Frame 3: neutral.
Frame 4: slow breath drop.
Loop.
{NEG_BASE}
```

#### Boss1_Idle_Right.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Boss1 CHAR_LOCK}, standing IDLE, FACING SCREEN-RIGHT (right side-profile, eye-slit on right side, greatsword visible toward camera on near side).
Frame 1: neutral.
Frame 2: slow breath rise.
Frame 3: neutral.
Frame 4: slow breath drop.
Loop. NOT mirror of Idle_Left — chain placement physically anchored.
{NEG_BASE}
```

### 4.2 Walk（沉重大步）

#### Boss1_Walk_Down.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Boss1 CHAR_LOCK}, WALKING heavily toward camera (front view, dragging greatsword point along the ground with sparks at the tip in frames 2/4).
Frame 1: LEFT armored boot raised mid-stomp.
Frame 2: left boot impact (slight dust puff, sword tip drags with sparks).
Frame 3: RIGHT boot raised mid-stomp.
Frame 4: right boot impact (dust + sparks).
Loop-friendly slow 4-step heavy walk. Body bobs ~4px per step (heavier than Player1).
{NEG_BASE}
```

#### Boss1_Walk_Up.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Boss1 CHAR_LOCK}, WALKING heavily AWAY from camera (rear view, back-plate runes prominent, greatsword dragged behind on the ground with spark trail).
Frame 1: LEFT boot raised.
Frame 2: left boot impact.
Frame 3: RIGHT boot raised.
Frame 4: right boot impact.
Loop. Heavy bob.
{NEG_BASE}
```

#### Boss1_Walk_Left.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Boss1 CHAR_LOCK}, WALKING heavily toward screen-left (left side-profile, greatsword dragged behind on the ground to the screen-right).
Frame 1: LEFT boot extended forward.
Frame 2: passing pose, sword sparks.
Frame 3: RIGHT boot extended forward.
Frame 4: passing pose, sword sparks.
Loop. Heavy bob.
{NEG_BASE}
```

#### Boss1_Walk_Right.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Boss1 CHAR_LOCK}, WALKING heavily toward screen-right (right side-profile, greatsword dragged behind to the screen-left).
Frame 1: RIGHT boot extended forward.
Frame 2: passing pose, sparks.
Frame 3: LEFT boot extended forward.
Frame 4: passing pose, sparks.
Loop. NOT mirror of Walk_Left — chain placement anchored.
{NEG_BASE}
```

### 4.3 Attack（双手大剑下劈）

#### Boss1_Attack_Down.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Boss1 CHAR_LOCK}, performing two-handed greatsword OVERHEAD SLAM attack, facing camera (front view).
Frame 1: WIND-UP — greatsword raised high above helmet with both hands, body coiled, crimson eyes blaze brighter.
Frame 2: SWING DOWN — sword arcs downward in front of body, motion trail behind blade.
Frame 3: IMPACT — sword hits the ground in front of boss, shockwave crackle, crimson energy burst at blade tip, dust puff at impact point.
Frame 4: RECOVERY — sword embedded briefly, boss leaning forward over it.
One-shot. NOT looping. Feet anchored throughout.
{NEG_BASE}
```

#### Boss1_Attack_Up.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Boss1 CHAR_LOCK}, performing two-handed greatsword OVERHEAD SLAM attack, BACK to camera (rear view, slamming away into screen).
Frame 1: WIND-UP — sword raised high overhead, viewed from behind.
Frame 2: SWING DOWN — sword arcs forward (away from viewer).
Frame 3: IMPACT — sword strikes ground ahead (into screen), shockwave + dust.
Frame 4: RECOVERY — boss hunched forward over embedded sword.
One-shot.
{NEG_BASE}
```

#### Boss1_Attack_Left.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Boss1 CHAR_LOCK}, performing two-handed greatsword HORIZONTAL CLEAVE attack, facing screen-LEFT.
Frame 1: WIND-UP — torso twisted to the right, sword cocked behind right shoulder.
Frame 2: SWING — torso uncoils, sword sweeps horizontally toward screen-left, motion trail.
Frame 3: IMPACT — sword fully extended to the LEFT edge of frame, crimson burst at blade.
Frame 4: RECOVERY — sword arc completes, body settles into left-facing stance.
One-shot. Feet anchored.
{NEG_BASE}
```

#### Boss1_Attack_Right.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Boss1 CHAR_LOCK}, performing two-handed greatsword HORIZONTAL CLEAVE attack, facing screen-RIGHT.
Frame 1: WIND-UP — torso twisted to the left, sword cocked behind left shoulder.
Frame 2: SWING — torso uncoils, sword sweeps horizontally toward screen-right.
Frame 3: IMPACT — sword fully extended to the RIGHT edge of frame, crimson burst.
Frame 4: RECOVERY — body settles right-facing.
One-shot. NOT mirror of Attack_Left.
{NEG_BASE}
```

### 4.4 Death（壮烈倒塌）

#### Boss1_Death_Down.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Boss1 CHAR_LOCK}, DYING, originally facing camera.
Frame 1: STAGGER — helmet recoils backward, crimson eyes flicker, sword drops from one hand.
Frame 2: KNEE-DOWN — boss drops to one knee, greatsword stabs into the ground beside as support.
Frame 3: COLLAPSE FORWARD — boss falls forward, armor plates rattle.
Frame 4: GROUNDED — boss lies face-down toward camera, helmet eyes DIMMED (no glow), greatsword embedded in ground beside body, chains scattered. Final lifeless pose.
One-shot. Final frame must look clearly DEAD (eyes dark).
{NEG_BASE}
```

#### Boss1_Death_Up.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Boss1 CHAR_LOCK}, DYING, originally facing away from camera.
Frame 1: STAGGER — body arches backward toward viewer.
Frame 2: KNEE-DOWN — drops to one knee, viewed from behind.
Frame 3: COLLAPSE BACKWARD — body falls toward viewer.
Frame 4: GROUNDED — boss lies on back, face/helmet turned upward, eyes dimmed dark. Lifeless.
One-shot.
{NEG_BASE}
```

#### Boss1_Death_Left.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Boss1 CHAR_LOCK}, DYING, originally facing screen-left.
Frame 1: STAGGER — recoils toward screen-right.
Frame 2: KNEE-DOWN — drops to one knee facing left.
Frame 3: COLLAPSE — falls onto left side.
Frame 4: GROUNDED — boss lies on left side facing screen-left, helmet eyes dimmed. Lifeless.
One-shot.
{NEG_BASE}
```

#### Boss1_Death_Right.png
```
{STYLE_BASE}
{FRAME_BASE}
Subject: {Boss1 CHAR_LOCK}, DYING, originally facing screen-right.
Frame 1: STAGGER — recoils toward screen-left.
Frame 2: KNEE-DOWN — drops to one knee facing right.
Frame 3: COLLAPSE — falls onto right side.
Frame 4: GROUNDED — boss lies on right side facing screen-right, helmet eyes dimmed. Lifeless.
One-shot. NOT mirror of Death_Left.
{NEG_BASE}
```

---

## 5. codex-image-gen 调度建议

> 给主对话 / codex-image-gen orchestrator 的提示

- **批次切分**：40 张全部走 L1（单帧 96~128 不算"≤256 透明小图"边界，且每张本身就是 sprite sheet，不适合 L2 合并画布）
- **建议分 4 批，每批一个角色**（便于每批失败重试不影响其他角色）：
  - 批 1: Player1 × 16（4 sub-batch × 4 张，避免 L1 单批 >12）
  - 批 2: Player2 × 4
  - 批 3: Player3 × 4
  - 批 4: Boss1 × 16（同样 4 sub-batch × 4 张）
- **占位符替换**：发送给 codex 前，主对话需把 `{STYLE_BASE}` / `{FRAME_BASE}` / `{NEG_BASE}` / `{Player1 CHAR_LOCK}` 等占位符**展开成完整文本**（codex 不会自己 lookup）
- **重试上限**：每张 1 次整批重试（见 codex-image-gen SKILL §3.7）；个别图 3 轮仍失败 → 阻塞通知用户人工介入
- **落盘命名规则**：严格按上述 `<Name>_<Action>_<Dir>.png`，不要加版本号后缀（除非要多候选对比）
- **transparent**: 所有 40 张走 chroma-key 工作流（codex 内置 image_gen 不支持原生透明，见 codex-image-gen SKILL §7.1）。prompt 中已写 "transparent PNG background"，但实际产出可能带棋盘格 → 后处理用 `tools/chroma_key_tool/chroma_key.py` 去背（若该工具不存在，先用 rembg 兜底）

---

## 6. 验收 checklist（与 requirements.md §6 一致）

- [ ] 40 张 sheet 全部落到 `openspec/changes/17-gameplay-character-art/art/raw/` 对应子目录
- [ ] 每张 4 帧等宽水平排列、可被 Slice 工具自动 4 等分
- [ ] 同一角色 4 方向装备/纹身位置/体型完全一致
- [ ] Boss1 视觉明显大于 Player1（1.6× 体）
- [ ] 不同角色互不混淆（配色/装备/职业感）
- [ ] 真透明 alpha 通道（如有棋盘格背景 → 走 chroma-key 后处理）
