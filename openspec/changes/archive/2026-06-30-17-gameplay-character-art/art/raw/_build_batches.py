"""
构造 codex-art-gen MCP 的 L1 batches 输入 JSON。
40 张 sprite sheet 分 4 个批次：Player1 ×16 / Player2 ×4 / Player3 ×4 / Boss1 ×16
每批内分 sub-batch（≤8 张 per codex session），生成最终 batches 列表写到 _batches.json
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]  # GameDesinger
ART_RAW = Path(__file__).resolve().parent
ASSET_BASE = ROOT / "Assets" / "Resources" / "Sprite" / "Character"

# ─────────────────── 通用拼接基线 ───────────────────

STYLE_BASE = (
    "hand-painted dark fantasy 2D game character sprite, gothic occult tone, "
    "painterly textured brushwork (no pixel art, no cel-shading, no line-art outline), "
    "warm gold rim light edge highlight from upper-left, cool blue-purple ambient shadow "
    "underbelly, palette dominated by dark slate blue / deep crimson / aged gold / muted "
    "browns, top-down 3/4 perspective viewed from a camera tilted 55 degrees downward "
    "(slightly looking-down angle), character body forward-leaning to fit overhead readability."
)

FRAME_BASE = (
    "4-frame horizontal sprite sheet, 4 frames arranged left-to-right with EQUAL width, "
    "identical character bounding box per frame (character must NOT shift between frames), "
    "pure solid chroma-key green (#00ff00) gutter between subjects, no frame number labels, "
    "no border lines between frames, frames occupy the FULL width of the canvas equally."
)

NEG_BASE = (
    "Avoid: text, signature, watermark, logo, UI elements, health bar, ground shadow, "
    "checkered background, frame number, left-right flipped duplicate (each frame "
    "independently drawn), pixel-art mosaic, cel-shading hard cell lines, anime line-art, "
    "realistic photo skin, nudity, blood splatter overflow, extra characters in the scene, "
    "weapon swap between frames."
)

CHAR_LOCK = {
    "Player1": (
        "medium-build male tribal ranger, dark-tan leather chest armor, brown leather belt "
        "with golden buckle, crimson hooded cape draped over LEFT shoulder, LEFT ARM FULLY "
        "TATTOOED in spiral flame motif (amber-gold + blood-red linework, clearly visible "
        "from any angle), dual curved daggers sheathed at both hips, leather boots with "
        "golden lacing, short black hair, weather-tanned skin"
    ),
    "Player2": (
        "slim female coastal shaman, deep purple long robe with silver star embroidery, "
        "holding a wooden staff topped with a glowing cyan crystal in her RIGHT hand, "
        "star-constellation dotted tattoo across back of neck and shoulders (cyan + silver, "
        "half-hidden under robe collar), head-band with woven star-map cloth, silver "
        "bracelets, long silver-grey hair tied back, pale skin"
    ),
    "Player3": (
        "stocky male snow hunter, cold-grey chainmail over fur tunic, huge two-handed "
        "greatsword strapped DIAGONALLY across the back (visible hilt over RIGHT shoulder), "
        "blue wolf-pelt cape with silver clasps, wolf-pelt hood, wolf-totem tattoo on chest "
        "(white + silver linework, partially exposed at collar), snow boots, weathered "
        "braided beard, frost-blue eyes"
    ),
    "Boss1": (
        "colossal armored ancient tomb-guardian, 1.6x the size of a player character, full-"
        "plate rust-black armor with cracked aged-gold engraved runes covering every plate "
        "surface, twin-handed colossal greatsword, red-eye-slit helmet with glowing crimson "
        "eye-sockets, broken iron chains hanging from waist and gauntlets, hunched menacing "
        "silhouette, NO visible skin (fully armored)"
    ),
}

# ─────────────────── 40 条专属提示词（按 prompts.md 第 1-4 节） ───────────────────

# 每条 = (character, action, direction, body_prompt)
# body_prompt 来自 prompts.md 对应小节
ITEMS = [
    # ---------- Player1 / Idle ----------
    ("Player1", "Idle", "Down",
     "Subject: {char}, standing IDLE breathing pose, FACING THE CAMERA (front view, full face "
     "and chest plate visible, daggers sheathed at both hips, crimson cape behind shoulders). "
     "Frame 1: neutral standing, arms relaxed at sides, weight even on both feet. "
     "Frame 2: shoulders rise slightly (inhale), cape lifts. "
     "Frame 3: identical to Frame 1 (back to neutral). "
     "Frame 4: shoulders drop slightly (exhale), cape settles below neutral. "
     "Loop-friendly cycle. All 4 frames share IDENTICAL feet position and silhouette outline."),
    ("Player1", "Idle", "Up",
     "Subject: {char}, standing IDLE breathing pose, BACK TO THE CAMERA (rear view, hood / "
     "back of head, crimson cape covering most of back, daggers visible at both hips from "
     "behind, no face visible). "
     "Frame 1: neutral standing, cape drapes straight down. "
     "Frame 2: shoulders rise (inhale), cape lifts. Frame 3: identical to Frame 1. "
     "Frame 4: shoulders drop (exhale), cape settles. Loop-friendly. IDENTICAL feet position."),
    ("Player1", "Idle", "Left",
     "Subject: {char}, standing IDLE breathing pose, FACING SCREEN-LEFT (left-side profile "
     "view, LEFT-ARM TATTOO clearly visible facing the viewer because the tattooed arm is on "
     "the side facing the camera, dagger on left hip prominent). "
     "Frame 1: neutral standing. Frame 2: shoulders rise (inhale). Frame 3: identical to "
     "Frame 1. Frame 4: shoulders drop (exhale). Loop-friendly. Side-profile silhouette "
     "consistent across frames."),
    ("Player1", "Idle", "Right",
     "Subject: {char}, standing IDLE breathing pose, FACING SCREEN-RIGHT (right-side profile "
     "view, tattooed left arm is now on the FAR side from the camera so only a hint of tattoo "
     "curls behind shoulder, dagger on right hip prominent). "
     "Frame 1: neutral standing. Frame 2: shoulders rise (inhale). Frame 3: identical to "
     "Frame 1. Frame 4: shoulders drop (exhale). Loop-friendly. THIS IS NOT a mirrored Left -- "
     "dagger on right hip, cape clasp on right shoulder, tattoo placement physically correct."),

    # ---------- Player1 / Walk ----------
    ("Player1", "Walk", "Down",
     "Subject: {char}, WALKING toward the camera (front view, full face visible, cape sways). "
     "Frame 1: LEFT foot lifted forward, right foot planted, arms swing opposite (right arm "
     "forward). Frame 2: both feet near ground in passing pose, arms vertical, cape mid-sway. "
     "Frame 3: RIGHT foot lifted forward, left foot planted, arms swing opposite (left arm "
     "forward). Frame 4: passing pose, mirrored cape sway. Loop-friendly 4-step walk cycle, "
     "small vertical bob between frames."),
    ("Player1", "Walk", "Up",
     "Subject: {char}, WALKING away from the camera (rear view, back of head + cape billowing "
     "behind). Frame 1: LEFT foot lifted forward (away into screen), cape lifts on left. "
     "Frame 2: passing pose, cape mid-sway. Frame 3: RIGHT foot lifted forward, cape lifts on "
     "right. Frame 4: passing pose. Loop-friendly 4-step walk cycle."),
    ("Player1", "Walk", "Left",
     "Subject: {char}, WALKING toward screen-left (left-side profile, tattooed left arm faces "
     "camera, cape trails to the right behind motion direction). "
     "Frame 1: LEFT foot extended forward (leading the walk into screen-left direction), right "
     "foot trailing. Frame 2: passing pose, both feet near vertical center. Frame 3: RIGHT "
     "foot extended forward. Frame 4: passing pose. Loop-friendly 4-step side-walk cycle. "
     "Cape trails BEHIND the direction of motion (to the screen-right side)."),
    ("Player1", "Walk", "Right",
     "Subject: {char}, WALKING toward screen-right (right-side profile, tattoo mostly hidden "
     "on far arm, cape trails to the left behind motion direction). "
     "Frame 1: RIGHT foot extended forward (leading the walk into screen-right direction), "
     "left foot trailing. Frame 2: passing pose. Frame 3: LEFT foot extended forward. "
     "Frame 4: passing pose. Loop-friendly 4-step side-walk cycle. Cape trails BEHIND motion "
     "(to the screen-left side). NOT a mirror of Walk_Left -- dagger / buckle positions "
     "remain physically anchored."),

    # ---------- Player1 / Attack ----------
    ("Player1", "Attack", "Down",
     "Subject: {char}, performing dual-dagger SLASH attack, body facing the camera (front "
     "view). Frame 1: WIND-UP -- both daggers drawn, pulled back behind hips, body coiled, "
     "slight forward lean. Frame 2: SLASH START -- daggers swung outward in horizontal "
     "crescent, faint motion blur trail behind blades, body uncoiling. Frame 3: IMPACT -- "
     "daggers reach maximum extension forward toward camera, both arms fully extended, tiny "
     "crimson energy flash at blade tips. Frame 4: RECOVERY -- daggers retracting back toward "
     "chest, body returning to neutral stance. One-shot 4-frame slash, NOT looping. Each "
     "frame keeps the same feet position (no walking during attack)."),
    ("Player1", "Attack", "Up",
     "Subject: {char}, performing dual-dagger SLASH attack, body BACK to the camera (rear "
     "view, attacking away into screen). Frame 1: WIND-UP -- daggers pulled back to ribs, "
     "visible from behind. Frame 2: SLASH START -- arms swing outward into screen, motion "
     "trail. Frame 3: IMPACT -- daggers fully extended away from camera, crimson flash at "
     "extremity. Frame 4: RECOVERY -- arms retract. One-shot. Same feet position across all "
     "frames."),
    ("Player1", "Attack", "Left",
     "Subject: {char}, performing dual-dagger SLASH attack, body facing screen-LEFT. "
     "Frame 1: WIND-UP -- torso twisted right (away from target), daggers cocked behind back. "
     "Frame 2: SLASH START -- torso uncoils left, daggers sweep horizontally toward "
     "screen-left. Frame 3: IMPACT -- daggers fully extended to the LEFT edge of frame, "
     "crimson flash, body fully twisted left. Frame 4: RECOVERY -- daggers pull back toward "
     "chest. One-shot. Feet anchored. Tattooed left arm clearly visible during the slash."),
    ("Player1", "Attack", "Right",
     "Subject: {char}, performing dual-dagger SLASH attack, body facing screen-RIGHT. "
     "Frame 1: WIND-UP -- torso twisted left, daggers cocked behind back. Frame 2: SLASH "
     "START -- uncoils right, daggers sweep toward screen-right. Frame 3: IMPACT -- daggers "
     "fully extended to the RIGHT edge of frame, crimson flash, body fully twisted right. "
     "Frame 4: RECOVERY -- daggers pull back. One-shot. Feet anchored. NOT a mirror of "
     "Attack_Left -- physical anchor of belt buckle / cape clasp stays on the correct side."),

    # ---------- Player1 / Death ----------
    ("Player1", "Death", "Down",
     "Subject: {char}, DYING, originally facing the camera. "
     "Frame 1: HIT -- body jerks backward, arms fly outward, daggers dropped, pained grimace, "
     "slight crimson burst at chest. Frame 2: COLLAPSE -- knees buckle, body hunches forward, "
     "daggers fall to the ground. Frame 3: FALLING -- body tilts forward and to one side, "
     "arms sprawling. Frame 4: GROUNDED -- body lies prone face-down toward camera, arms "
     "spread, cape draped over back, completely still. THIS IS THE FINAL FRAME and should "
     "look clearly LIFELESS. One-shot, NOT looping."),
    ("Player1", "Death", "Up",
     "Subject: {char}, DYING, originally facing away from camera (rear view). "
     "Frame 1: HIT -- body arches backward (toward camera). Frame 2: COLLAPSE -- knees buckle "
     "backward. Frame 3: FALLING -- body tilts backward toward viewer. Frame 4: GROUNDED -- "
     "body lies on its back, face turned upward toward camera, cape spread out beneath, arms "
     "loose. Final lifeless pose. One-shot."),
    ("Player1", "Death", "Left",
     "Subject: {char}, DYING, originally facing screen-left. "
     "Frame 1: HIT -- body recoils toward screen-right (away from incoming attack direction "
     "implied from left). Frame 2: COLLAPSE -- knees buckle, character drops to one knee "
     "facing left. Frame 3: FALLING -- body tilts forward onto left side. Frame 4: GROUNDED -- "
     "body lies on left side facing screen-left, daggers scattered, cape draped. Lifeless. "
     "One-shot."),
    ("Player1", "Death", "Right",
     "Subject: {char}, DYING, originally facing screen-right. "
     "Frame 1: HIT -- body recoils toward screen-left. Frame 2: COLLAPSE -- drops to one knee "
     "facing right. Frame 3: FALLING -- body tilts onto right side. Frame 4: GROUNDED -- body "
     "lies on right side facing screen-right. Lifeless. NOT mirror of Death_Left -- belt "
     "buckle anchored. One-shot."),

    # ---------- Player2 / Idle ----------
    ("Player2", "Idle", "Down",
     "Subject: {char}, standing IDLE breathing pose, FACING CAMERA (front view, full robe "
     "pattern visible, holding staff vertically in right hand with cyan crystal glow at top). "
     "Frame 1: neutral standing, robe hem still. Frame 2: shoulders rise (inhale), robe hem "
     "lifts slightly, cyan crystal glow slightly brighter. Frame 3: identical to Frame 1. "
     "Frame 4: shoulders drop (exhale), crystal glow dims slightly. Loop-friendly. Identical "
     "feet position. Staff vertical at all times."),
    ("Player2", "Idle", "Up",
     "Subject: {char}, standing IDLE, BACK TO CAMERA (rear view, star-embroidery on robe back "
     "fully visible, star-constellation neck/shoulder tattoo clearly exposed above robe "
     "collar, staff in right hand). Frame 1: neutral. Frame 2: inhale (shoulders rise). "
     "Frame 3: neutral. Frame 4: exhale (shoulders drop). Loop. Tattoo and embroidery static "
     "across frames."),
    ("Player2", "Idle", "Left",
     "Subject: {char}, standing IDLE, FACING SCREEN-LEFT (left side-profile, staff in right "
     "hand on the far side from camera so partially obscured by body, neck tattoo partially "
     "visible behind ear). Frame 1: neutral. Frame 2: inhale. Frame 3: neutral. Frame 4: "
     "exhale. Loop."),
    ("Player2", "Idle", "Right",
     "Subject: {char}, standing IDLE, FACING SCREEN-RIGHT (right side-profile, staff in right "
     "hand prominently visible toward camera with cyan crystal in clear view, neck tattoo on "
     "far side hidden). Frame 1: neutral. Frame 2: inhale. Frame 3: neutral. Frame 4: exhale. "
     "Loop. NOT mirror of Idle_Left -- staff position physically anchored to right hand."),

    # ---------- Player3 / Idle ----------
    ("Player3", "Idle", "Down",
     "Subject: {char}, standing IDLE, FACING CAMERA (front view, full chainmail and chest "
     "wolf-totem tattoo partially visible at collar opening, greatsword hilt visible above "
     "right shoulder). Frame 1: neutral, arms folded across chest. Frame 2: chest rises "
     "(inhale), chainmail catches faint highlight. Frame 3: neutral. Frame 4: chest drops "
     "(exhale). Loop. Greatsword hilt anchored above right shoulder across all frames."),
    ("Player3", "Idle", "Up",
     "Subject: {char}, standing IDLE, BACK TO CAMERA (rear view, FULL GREATSWORD strapped "
     "diagonally across the back is the dominant visual, wolf-pelt cape draped, hood up). "
     "Frame 1: neutral. Frame 2: inhale. Frame 3: neutral. Frame 4: exhale. Loop. Greatsword "
     "position static."),
    ("Player3", "Idle", "Left",
     "Subject: {char}, standing IDLE, FACING SCREEN-LEFT (left side-profile, greatsword hilt "
     "visible projecting up-right behind shoulder, braided beard silhouette). Frame 1: "
     "neutral. Frame 2: inhale. Frame 3: neutral. Frame 4: exhale. Loop."),
    ("Player3", "Idle", "Right",
     "Subject: {char}, standing IDLE, FACING SCREEN-RIGHT (right side-profile, greatsword "
     "hilt visible projecting up-left behind shoulder). Frame 1: neutral. Frame 2: inhale. "
     "Frame 3: neutral. Frame 4: exhale. Loop. NOT mirror of Idle_Left -- sword strap "
     "physically anchored."),

    # ---------- Boss1 / Idle ----------
    ("Boss1", "Idle", "Down",
     "Subject: {char}, standing IDLE menacingly, FACING CAMERA (front view, towering full-"
     "plate armor, crimson eye-slits glowing, twin-handed greatsword resting point-down in "
     "front of body with both gauntlets on the pommel). Frame 1: neutral hulking stance, "
     "armor plates static. Frame 2: chest plate rises slightly (slow heavy breathing), "
     "crimson eyes pulse brighter. Frame 3: identical to Frame 1. Frame 4: chest plate drops, "
     "eyes dim slightly. Loop. Slow heavy breathing rhythm."),
    ("Boss1", "Idle", "Up",
     "Subject: {char}, standing IDLE, BACK TO CAMERA (rear view, massive back-plate with "
     "cracked golden engraved runes dominates the frame, broken iron chains hang from waist, "
     "helmet rear visible). Frame 1: neutral hulking stance. Frame 2: shoulders rise (slow "
     "breath). Frame 3: neutral. Frame 4: shoulders drop. Loop. Chains sway slightly between "
     "breaths."),
    ("Boss1", "Idle", "Left",
     "Subject: {char}, standing IDLE, FACING SCREEN-LEFT (left side-profile, helmet eye-slit "
     "glowing crimson visible on the left side, greatsword held point-down on far side of "
     "body so half-obscured, chains hang from belt). Frame 1: neutral. Frame 2: slow breath "
     "rise. Frame 3: neutral. Frame 4: slow breath drop. Loop."),
    ("Boss1", "Idle", "Right",
     "Subject: {char}, standing IDLE, FACING SCREEN-RIGHT (right side-profile, eye-slit on "
     "right side, greatsword visible toward camera on near side). Frame 1: neutral. Frame 2: "
     "slow breath rise. Frame 3: neutral. Frame 4: slow breath drop. Loop. NOT mirror of "
     "Idle_Left -- chain placement physically anchored."),

    # ---------- Boss1 / Walk ----------
    ("Boss1", "Walk", "Down",
     "Subject: {char}, WALKING heavily toward camera (front view, dragging greatsword point "
     "along the ground with sparks at the tip in frames 2/4). Frame 1: LEFT armored boot "
     "raised mid-stomp. Frame 2: left boot impact (slight dust puff, sword tip drags with "
     "sparks). Frame 3: RIGHT boot raised mid-stomp. Frame 4: right boot impact (dust + "
     "sparks). Loop-friendly slow 4-step heavy walk. Body bobs heavily per step (heavier "
     "than Player1)."),
    ("Boss1", "Walk", "Up",
     "Subject: {char}, WALKING heavily AWAY from camera (rear view, back-plate runes "
     "prominent, greatsword dragged behind on the ground with spark trail). Frame 1: LEFT "
     "boot raised. Frame 2: left boot impact. Frame 3: RIGHT boot raised. Frame 4: right "
     "boot impact. Loop. Heavy bob."),
    ("Boss1", "Walk", "Left",
     "Subject: {char}, WALKING heavily toward screen-left (left side-profile, greatsword "
     "dragged behind on the ground to the screen-right). Frame 1: LEFT boot extended "
     "forward. Frame 2: passing pose, sword sparks. Frame 3: RIGHT boot extended forward. "
     "Frame 4: passing pose, sword sparks. Loop. Heavy bob."),
    ("Boss1", "Walk", "Right",
     "Subject: {char}, WALKING heavily toward screen-right (right side-profile, greatsword "
     "dragged behind to the screen-left). Frame 1: RIGHT boot extended forward. Frame 2: "
     "passing pose, sparks. Frame 3: LEFT boot extended forward. Frame 4: passing pose, "
     "sparks. Loop. NOT mirror of Walk_Left -- chain placement anchored."),

    # ---------- Boss1 / Attack ----------
    ("Boss1", "Attack", "Down",
     "Subject: {char}, performing two-handed greatsword OVERHEAD SLAM attack, facing camera "
     "(front view). Frame 1: WIND-UP -- greatsword raised high above helmet with both hands, "
     "body coiled, crimson eyes blaze brighter. Frame 2: SWING DOWN -- sword arcs downward "
     "in front of body, motion trail behind blade. Frame 3: IMPACT -- sword hits the ground "
     "in front of boss, shockwave crackle, crimson energy burst at blade tip, dust puff at "
     "impact point. Frame 4: RECOVERY -- sword embedded briefly, boss leaning forward over "
     "it. One-shot. NOT looping. Feet anchored throughout."),
    ("Boss1", "Attack", "Up",
     "Subject: {char}, performing two-handed greatsword OVERHEAD SLAM attack, BACK to camera "
     "(rear view, slamming away into screen). Frame 1: WIND-UP -- sword raised high overhead, "
     "viewed from behind. Frame 2: SWING DOWN -- sword arcs forward (away from viewer). "
     "Frame 3: IMPACT -- sword strikes ground ahead (into screen), shockwave + dust. "
     "Frame 4: RECOVERY -- boss hunched forward over embedded sword. One-shot."),
    ("Boss1", "Attack", "Left",
     "Subject: {char}, performing two-handed greatsword HORIZONTAL CLEAVE attack, facing "
     "screen-LEFT. Frame 1: WIND-UP -- torso twisted to the right, sword cocked behind right "
     "shoulder. Frame 2: SWING -- torso uncoils, sword sweeps horizontally toward screen-"
     "left, motion trail. Frame 3: IMPACT -- sword fully extended to the LEFT edge of frame, "
     "crimson burst at blade. Frame 4: RECOVERY -- sword arc completes, body settles into "
     "left-facing stance. One-shot. Feet anchored."),
    ("Boss1", "Attack", "Right",
     "Subject: {char}, performing two-handed greatsword HORIZONTAL CLEAVE attack, facing "
     "screen-RIGHT. Frame 1: WIND-UP -- torso twisted to the left, sword cocked behind left "
     "shoulder. Frame 2: SWING -- torso uncoils, sword sweeps horizontally toward "
     "screen-right. Frame 3: IMPACT -- sword fully extended to the RIGHT edge of frame, "
     "crimson burst. Frame 4: RECOVERY -- body settles right-facing. One-shot. NOT mirror of "
     "Attack_Left."),

    # ---------- Boss1 / Death ----------
    ("Boss1", "Death", "Down",
     "Subject: {char}, DYING, originally facing camera. Frame 1: STAGGER -- helmet recoils "
     "backward, crimson eyes flicker, sword drops from one hand. Frame 2: KNEE-DOWN -- boss "
     "drops to one knee, greatsword stabs into the ground beside as support. Frame 3: "
     "COLLAPSE FORWARD -- boss falls forward, armor plates rattle. Frame 4: GROUNDED -- boss "
     "lies face-down toward camera, helmet eyes DIMMED (no glow), greatsword embedded in "
     "ground beside body, chains scattered. Final lifeless pose. One-shot. Final frame must "
     "look clearly DEAD (eyes dark)."),
    ("Boss1", "Death", "Up",
     "Subject: {char}, DYING, originally facing away from camera. Frame 1: STAGGER -- body "
     "arches backward toward viewer. Frame 2: KNEE-DOWN -- drops to one knee, viewed from "
     "behind. Frame 3: COLLAPSE BACKWARD -- body falls toward viewer. Frame 4: GROUNDED -- "
     "boss lies on back, face/helmet turned upward, eyes dimmed dark. Lifeless. One-shot."),
    ("Boss1", "Death", "Left",
     "Subject: {char}, DYING, originally facing screen-left. Frame 1: STAGGER -- recoils "
     "toward screen-right. Frame 2: KNEE-DOWN -- drops to one knee facing left. Frame 3: "
     "COLLAPSE -- falls onto left side. Frame 4: GROUNDED -- boss lies on left side facing "
     "screen-left, helmet eyes dimmed. Lifeless. One-shot."),
    ("Boss1", "Death", "Right",
     "Subject: {char}, DYING, originally facing screen-right. Frame 1: STAGGER -- recoils "
     "toward screen-left. Frame 2: KNEE-DOWN -- drops to one knee facing right. Frame 3: "
     "COLLAPSE -- falls onto right side. Frame 4: GROUNDED -- boss lies on right side facing "
     "screen-right, helmet eyes dimmed. Lifeless. One-shot. NOT mirror of Death_Left."),
]

assert len(ITEMS) == 40, f"expected 40 items, got {len(ITEMS)}"

# 共享底色 + 一些"宽幅"语义提示
# 我们让 codex 生成 1536x1024 横向 sheet (gpt-image-2 支持的尺寸)，4 格水平
# 后处理下采样到 384x96 / 512x128
WIDESHEET_HINT = (
    "Canvas dimensions 1536x1024. The 4 frames occupy the full canvas width, each frame is "
    "384px wide, arranged in a single horizontal row. Within each frame, the character body "
    "is centered horizontally and vertically, scaled large enough to fill ~75% of the frame "
    "height for player characters (~85% for boss). The 4 frames are visually separated by a "
    "thin (12-24px) solid #00ff00 vertical gutter, not a drawn line."
)

# ─────────────────── 拼装 items + 批次 ───────────────────

batches = []
sub_batch_size = 4  # 每个 codex session 4 张：减小单批 JSON 体积，提高成功率
sub_idx = 0

# 把 40 张按角色聚合，再每 4 张切一批
by_char = {}
for item in ITEMS:
    by_char.setdefault(item[0], []).append(item)

for char in ["Player1", "Player2", "Player3", "Boss1"]:
    char_items = by_char[char]
    # 按 sub_batch_size 切片
    for i in range(0, len(char_items), sub_batch_size):
        chunk = char_items[i:i + sub_batch_size]
        batch_items = []
        for j, (c, action, direction, body) in enumerate(chunk):
            file_abs = ASSET_BASE / c / action / f"{direction}.png"
            body_filled = body.replace("{char}", CHAR_LOCK[c])
            full_prompt = (
                f"{STYLE_BASE}\n\n"
                f"{FRAME_BASE}\n\n"
                f"{WIDESHEET_HINT}\n\n"
                f"Character (固定不变): {CHAR_LOCK[c]}.\n\n"
                f"{body_filled}\n\n"
                f"{NEG_BASE}"
            )
            batch_items.append({
                "index": j + 1,
                "name": f"{c}_{action}_{direction}",
                "file": str(file_abs).replace("\\", "/"),
                "size": "1536x1024",
                "transparent": True,
                "prompt": full_prompt,
                "negative": NEG_BASE,
            })
        sub_idx += 1
        batches.append({
            "batch_id": f"char17_{char.lower()}_{i // sub_batch_size + 1:02d}",
            "writable_roots": [
                str(ASSET_BASE).replace("\\", "/"),
                str(ART_RAW).replace("\\", "/"),
            ],
            "chroma_key": "#00ff00",
            "items": batch_items,
        })

# 输出 batches JSON
out_path = ART_RAW / "_batches.json"
with open(out_path, "w", encoding="utf-8") as f:
    json.dump({"batches": batches, "concurrency": 2}, f, ensure_ascii=False, indent=2)

# 统计
total_items = sum(len(b["items"]) for b in batches)
print(f"OK: {len(batches)} batches, total {total_items} items")
for b in batches:
    print(f"  {b['batch_id']}: {len(b['items'])} items -> {b['items'][0]['name']} ... {b['items'][-1]['name']}")
print(f"Wrote: {out_path}")
