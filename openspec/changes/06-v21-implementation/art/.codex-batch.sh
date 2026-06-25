#!/usr/bin/env bash
# 91 张 v2.1 美术资源批处理。从 prompts.md 提取 JSON → 顺序调 codex exec
set -u  # 注意：去掉 -e 和 pipefail 避免 codex exec 非0 退出导致脚本中断

CHANGE="06-v21-implementation"
ART="openspec/changes/${CHANGE}/art"
RAW="${ART}/raw"
PROMPTS="${ART}/prompts.md"
LOG="${RAW}/.codex-batch.log"
RECORD="${RAW}/生成记录.md"
ITEMS_TSV="${ART}/.batch-items.tsv"

mkdir -p "${RAW}"
if [[ ! -s "${RECORD}" ]] || ! grep -q "生成开始" "${RECORD}"; then
  echo "# v2.1 美术资源生成记录" > "${RECORD}"
  echo "生成开始：$(date)" >> "${RECORD}"
  echo "" >> "${RECORD}"
  echo "| # | file | size | status | 时间 |" >> "${RECORD}"
  echo "|---|---|---|---|---|" >> "${RECORD}"
fi

# 从 prompts.md 提取所有 prompt 条目 → TSV (file<tab>size<tab>prompt<tab>negative)
python3 -c "
import re, json
with open('${PROMPTS}', 'r', encoding='utf-8') as f:
    txt = f.read()
blocks = re.findall(r'\`\`\`json\s*(\[.*?\])\s*\`\`\`', txt, re.S)
all_items = []
for b in blocks:
    try:
        items = json.loads(b)
        all_items.extend(items if isinstance(items, list) else [items])
    except: pass
STYLE = 'A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable.'
NEG = 'no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration'
with open('${ITEMS_TSV}','w', encoding='utf-8') as f:
    for it in all_items:
        prompt = it.get('prompt','').replace('STYLE_BASE', STYLE).replace('\n',' ').replace('\t',' ')
        neg = it.get('negative','').replace('NEG_BASE', NEG).replace('\n',' ').replace('\t',' ')
        f.write(f\"{it['file']}\t{it.get('size','1024x1024')}\t{prompt}\t{neg}\n\")
print(f'Extracted {len(all_items)} items to TSV')
"

TOTAL=$(wc -l < "${ITEMS_TSV}")
echo "Total prompts: ${TOTAL}" | tee -a "${LOG}"

INDEX=0
# 关键：用 < file 喂 while，且 codex exec 加 < /dev/null 隔离 stdin
while IFS=$'\t' read -r FILE SIZE PROMPT NEG; do
  INDEX=$((INDEX+1))
  OUT="openspec/changes/${CHANGE}/art/${FILE}"
  mkdir -p "$(dirname "${OUT}")"
  if [[ -f "${OUT}" && $(stat -c%s "${OUT}" 2>/dev/null || echo 0) -gt 1024 ]]; then
    echo "[skip ${INDEX}/${TOTAL}] ${FILE}" | tee -a "${LOG}"
    echo "| ${INDEX} | ${FILE} | ${SIZE} | skip | $(date +%H:%M) |" >> "${RECORD}"
    continue
  fi
  echo "=== [${INDEX}/${TOTAL}] generating ${FILE} ===" | tee -a "${LOG}"
  # 关键：codex exec stdin 从 /dev/null 读，stdout/stderr 全部到 LOG
  codex exec -s workspace-write "请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
${PROMPT}

# 输出要求
- 尺寸：${SIZE}
- 透明背景：是
- 保存到：${OUT}
- 负面：${NEG}

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。" < /dev/null >> "${LOG}" 2>&1
  if [[ -f "${OUT}" && $(stat -c%s "${OUT}" 2>/dev/null || echo 0) -gt 1024 ]]; then
    echo "[ok ${INDEX}/${TOTAL}] ${FILE}" | tee -a "${LOG}"
    echo "| ${INDEX} | ${FILE} | ${SIZE} | ok | $(date +%H:%M) |" >> "${RECORD}"
  else
    echo "[fail ${INDEX}/${TOTAL}] ${FILE}" | tee -a "${LOG}"
    echo "| ${INDEX} | ${FILE} | ${SIZE} | **fail** | $(date +%H:%M) |" >> "${RECORD}"
  fi
done < "${ITEMS_TSV}"

echo "=== batch complete ===" | tee -a "${LOG}"
echo "" >> "${RECORD}"
echo "生成结束：$(date)" >> "${RECORD}"
