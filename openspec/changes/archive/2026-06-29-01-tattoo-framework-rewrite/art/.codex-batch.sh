#!/usr/bin/env bash
# 顺序批量生成 20 张图（color_red 已在前置步骤完成）
# 完成后写入 art/raw/生成记录.md
set -uo pipefail

CHANGE="01-tattoo-framework-rewrite"
RAW="openspec/changes/${CHANGE}/art/raw"
LOG="${RAW}/.codex-batch.log"
RECORD="${RAW}/生成记录.md"
mkdir -p "${RAW}"

# 公共风格描述
BASE="A 256x256 game UI icon, flat vector graphic, dark sci-fi tattoo style, transparent background, centered composition, no text, no signature, clean geometric silhouette, high contrast, suitable for a Unity sprite atlas."
NEG="blurry, watermark, text, signature, realistic skin, photographic"

gen() {
  local file="$1"
  local desc="$2"
  local out="${RAW}/${file}"
  if [[ -f "${out}" && $(stat -c%s "${out}") -gt 1024 ]]; then
    echo "[skip] ${file} already exists"
    return 0
  fi
  echo "=== generating ${file} ===" | tee -a "${LOG}"
  codex exec -s workspace-write "请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
${BASE}
${desc}

# 输出要求
- 尺寸：1024x1024
- 保存到：${out}
- 负面：${NEG}

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。" >> "${LOG}" 2>&1
  if [[ -f "${out}" && $(stat -c%s "${out}") -gt 1024 ]]; then
    echo "[ok] ${file} size=$(stat -c%s "${out}")"
  else
    echo "[fail] ${file}"
  fi
}

# ===== 6 颜色徽章（剩余 6 个） =====
gen "color_yellow.png" "A circular game element badge, 4px outer ring outline #7c8aa8, inner radial gradient from #fff58a center to #d8a020 edge, centered glyph in pure white showing a sharp Z-shaped lightning bolt with one small branching fork, expressing lightning element."
gen "color_green.png"  "A circular game element badge, 4px outer ring outline #7c8aa8, inner radial gradient from #b6f0a4 center to #2c7a3c edge, centered glyph in pure white showing a single downward-falling droplet with leaf-vein detail inside, expressing nature poison element."
gen "color_blue.png"   "A circular game element badge, 4px outer ring outline #7c8aa8, inner radial gradient from #cfeaff center to #1f5f9a edge, centered glyph in pure white showing a six-pointed snowflake with delicate triangular tips, expressing frost element."
gen "color_purple.png" "A circular game element badge, 4px outer ring outline #7c8aa8, inner radial gradient from #d8b6ff center to #6a2f9a edge, centered glyph in pure white showing a swirling spiral of two intertwined ribbons forming a vortex, expressing mutation element."
gen "color_gold.png"   "A circular game element badge, 4px outer ring outline #7c8aa8, inner radial gradient from #ffe9a8 center to #b07a16 edge, centered glyph in pure white showing a slim cross surrounded by an 8-ray sun-burst halo, expressing holy element."
gen "color_white.png"  "A circular game element badge, 4px outer ring outline #7c8aa8, inner radial gradient from #ffffff center to #c6d6ff edge, centered glyph in pure white showing a multi-faceted crystalline diamond with internal prism lines and cyan rim, expressing pure light element."

# ===== 8 图案符号 =====
gen "pattern_line.png"   "Pure white #ffffff strokes 6px wide on transparent background, a single bold vertical bar with a small arrowhead tip at top and a notch at bottom, clean geometric, centered."
gen "pattern_ring.png"   "Pure white #ffffff strokes 6px wide on transparent background, two concentric circles with four short radial spokes at 0 90 180 270 degrees, clean geometric, centered."
gen "pattern_spiral.png" "Pure white #ffffff strokes 6px wide on transparent background, an archimedean spiral of 5 loops growing outward from a central tiny dot marker, clean geometric, centered."
gen "pattern_zigzag.png" "Pure white #ffffff strokes 6px wide on transparent background, a 4-segment polyline forming a sharp zigzag with tapered ends, clean geometric, centered."
gen "pattern_bolt.png"   "Pure white #ffffff strokes 6px wide on transparent background, a jagged chained lightning bolt with two visible side forks flowing from top-left to bottom-right, clean geometric, centered."
gen "pattern_star.png"   "Pure white #ffffff strokes 6px wide on transparent background, a clean five-pointed star outline with a dashed inner star slightly offset, clean geometric, centered."
gen "pattern_stream.png" "Pure white #ffffff strokes 6px wide on transparent background, three parallel sweeping curved lines flowing from left to right with tapered tails, clean geometric, centered."
gen "pattern_beast.png"  "Pure white #ffffff strokes 6px wide on transparent background, three large diagonal claw-mark slashes top-left to bottom-right with a small simplified beast eye in the upper-right, clean geometric, centered."

# ===== 6 部位剪影 =====
gen "part_head.png"      "A pure white silhouette on transparent background, profile of a stylized warrior skull facing left, sharp triangular eye visor cut-out, minimal bone seam line on top, geometric, centered."
gen "part_torso.png"     "A pure white silhouette on transparent background, frontal view of a chest plate armor, central cross-shaped engraving, three pairs of rib lines on each side, no head no arms no legs, symmetric, centered."
gen "part_left_arm.png"  "A pure white silhouette on transparent background, flexed left arm with closed fist, elbow pointing down-left, biceps visible, knuckles wrapped, no shoulder, vertical orientation, centered."
gen "part_right_arm.png" "A pure white silhouette on transparent background, right arm gripping a vertical sword hilt, forearm horizontal, fist clenched on guard, blade extending up cut at frame top, symmetric, centered."
gen "part_left_leg.png"  "A pure white silhouette on transparent background, left leg mid-step forward, knee slightly bent, toe pointing down, two soft dashed motion-trail lines behind calf, geometric, centered."
gen "part_right_leg.png" "A pure white silhouette on transparent background, right leg in mid-run forward, knee high, foot forward, long curved sprint streak trailing behind heel, geometric, centered."

echo "=== batch complete ==="
