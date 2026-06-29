#!/usr/bin/env bash
# 重跑 4 张失败的部位图（用 _v2 后缀，避免覆盖 PIL 降级图，校验通过后再 mv）
set -uo pipefail

RAW="openspec/changes/01-tattoo-framework-rewrite/art/raw"
LOG="${RAW}/.codex-retry.log"
BASE="A 256x256 game UI icon, flat vector graphic, transparent background, centered, no text, 4px outer outline #7c8aa8, pure white silhouette."

gen() {
  local file="$1"; local desc="$2"
  local out="${RAW}/${file}"
  if [[ -f "${out}" && $(stat -c%s "${out}") -gt 1024 ]]; then
    echo "[skip] ${file} already exists"; return 0
  fi
  echo "=== generating ${file} ===" | tee -a "${LOG}"
  codex exec -s workspace-write "请使用 imagegen 系统 skill 生成一张图片。

# 画面描述
${BASE}
${desc}

# 输出要求
- 尺寸：1024x1024
- 保存到：${out}
- 负面：blurry, watermark, text, signature, photographic

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。" >> "${LOG}" 2>&1
  if [[ -f "${out}" && $(stat -c%s "${out}") -gt 1024 ]]; then
    echo "[ok] ${file} size=$(stat -c%s "${out}")"
  else
    echo "[fail] ${file}"
  fi
}

# part_left_arm_v2 已生成验证；只跑剩 3 张
gen "part_right_arm_v2.png" "Pure white silhouette of a right arm gripping a vertical sword hilt, forearm horizontal, fist clenched on guard, blade extending up but cut at frame top, symmetric and clean."
gen "part_left_leg_v2.png"  "Pure white silhouette of a left leg mid-step forward, knee slightly bent, toe pointing down, two soft dashed motion-trail lines behind calf, geometric."
gen "part_right_leg_v2.png" "Pure white silhouette of a right leg in mid-run forward, knee high, foot forward, long curved sprint streak trailing behind heel, geometric."

echo "=== retry batch complete ==="
