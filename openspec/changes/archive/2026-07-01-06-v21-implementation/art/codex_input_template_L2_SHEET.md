# Codex 输入模板：L2_SHEET 多素材合并画布

> 用途：让 Claude 工作流把多个小素材合并成一张 spritesheet / atlas / contact sheet，由 Codex 只调用一次 imagegen 生成，再由本地脚本切图。
> 适用：技能图标、affix、药水、消耗品、HUD 小图标、recipe scroll、paint bottle、小型道具图标。

## CLI 调用建议

```bash
codex exec \
  -C openspec/changes/06-v21-implementation/art/.codex-runner \
  --add-dir D:/unity/UnityProject/GameDesinger \
  -s workspace-write \
  --ephemeral \
  -o openspec/changes/06-v21-implementation/art/.codex-result-l2-001.json \
  - < openspec/changes/06-v21-implementation/art/.prompt-l2-001.txt
```

## 输入模板

```text
你是图片生成执行器。

任务：
读取下面的 sheet JSON，只调用 1 次 image_gen，生成 1 张包含多个小素材的透明 PNG 画布。

硬性规则：
- 只调用 1 次 image_gen。
- 不要读取项目文档。
- 不要解释、不要计划、不要询问。
- 不要自行重试。
- 不要切图。
- 不要生成多个文件。
- 只保存 1 张画布到 canvas 指定路径。
- 最终只输出合法 JSON 对象，不要 markdown。

画布规则：
- 画布尺寸使用 size。
- 背景必须完全透明。
- 按 grid_rows x grid_cols 网格排列。
- 每个素材必须独立，不要相互接触。
- 每个素材位于自己的格子中央。
- 素材之间至少 32px 完全透明 padding。
- 不要文字、数字、水印、签名、边框、标签。
- 按从左到右、从上到下排列，顺序必须与 items 一致。

返回格式：
{
  "canvas": "...",
  "size_bytes": 123456,
  "status": "ok",
  "grid_rows": 4,
  "grid_cols": 4,
  "layout_order": ["asset_1","asset_2"]
}

sheet JSON:
{
  "mode": "L2_SHEET",
  "sheet_id": "{{SHEET_ID}}",
  "canvas": "openspec/changes/{{CHANGE_NAME}}/art/raw/_merged/{{SHEET_ID}}.png",
  "size": "1024x1024",
  "transparent": true,
  "grid_rows": 4,
  "grid_cols": 4,
  "items": [
    {
      "index": 1,
      "name": "{{ASSET_NAME_1}}",
      "target_file": "openspec/changes/{{CHANGE_NAME}}/art/raw/{{CATEGORY}}/{{ASSET_NAME_1}}.png",
      "prompt": "{{ENGLISH_PROMPT_1}}",
      "negative": "{{NEGATIVE_PROMPT_1}}"
    },
    {
      "index": 2,
      "name": "{{ASSET_NAME_2}}",
      "target_file": "openspec/changes/{{CHANGE_NAME}}/art/raw/{{CATEGORY}}/{{ASSET_NAME_2}}.png",
      "prompt": "{{ENGLISH_PROMPT_2}}",
      "negative": "{{NEGATIVE_PROMPT_2}}"
    }
  ]
}
```

## Claude 填充规则

- 每张 sheet 建议 4x4，即 16 个小素材。
- 可以使用 3x3 做高稳定性批次。
- 不建议一开始使用 5x5 或更多，容易粘连、错位、细节不足。
- `items` 顺序就是后续切图命名顺序。
- `target_file` 不给 Codex 写入，只供 Claude 后处理切图命名。
- 所有 item 应该是“小素材”，不要混入角色、Boss、复杂场景图。
- prompt 中不要要求文字渲染；文字层应后期处理。

## 适用示例

```json
{
  "mode": "L2_SHEET",
  "sheet_id": "paint-sheet-001",
  "canvas": "openspec/changes/06-v21-implementation/art/raw/_merged/paint-sheet-001.png",
  "size": "1024x1024",
  "transparent": true,
  "grid_rows": 4,
  "grid_cols": 4,
  "items": [
    {
      "index": 1,
      "name": "paint_red_common",
      "target_file": "openspec/changes/06-v21-implementation/art/raw/paint/paint_red_common.png",
      "prompt": "A small common red paint bottle game icon, simple glass vial with cork, vibrant red liquid, painterly 2.5D style",
      "negative": "text, label, watermark, signature, frame, background"
    },
    {
      "index": 2,
      "name": "paint_red_rare",
      "target_file": "openspec/changes/06-v21-implementation/art/raw/paint/paint_red_rare.png",
      "prompt": "A small rare red paint bottle game icon, ornate glass vial, subtle ember glow, painterly 2.5D style",
      "negative": "text, label, watermark, signature, frame, background"
    }
  ]
}
```

## 后处理切图建议

Codex 完成后，由 Claude 工作流或本地脚本执行切图：

```bash
.venv/Scripts/python tools/ImageCut_Tool/image_cut.py \
  openspec/changes/06-v21-implementation/art/raw/_merged/{{SHEET_ID}}.png \
  -o openspec/changes/06-v21-implementation/art/raw/_merged/{{SHEET_ID}}_cut \
  --alpha 16 --min-area 80 --padding 2 --debug --json
```

命名映射时，使用 `layout_order` 与切图 manifest 的 row bucket 排序对应，不要简单按 `(y, x)` 排序：

```python
CANVAS_H = manifest["size"]["height"]
ROWS = grid_rows
ROW_H = CANVAS_H / ROWS

def row_then_x(sprite):
    bb = sprite["bbox"]
    cx = bb["x"] + bb["width"] // 2
    cy = bb["y"] + bb["height"] // 2
    return (int(cy / ROW_H), cx)

sprites_sorted = sorted(manifest["sprites"], key=row_then_x)
# sprites_sorted[i] 对应 layout_order[i]
```

## 验收

Claude 工作流在 Codex 返回后必须本地验证：

- 返回内容是合法 JSON 对象。
- `canvas` 文件存在。
- `canvas` 文件大小大于 1KB。
- `grid_rows` / `grid_cols` 与请求一致。
- `layout_order` 数量等于 items 数量。
- 切图数量等于 items 数量。
- 切出的文件按 `target_file` 命名并写入目标目录。
- 切图失败时不要让 Codex 重新切图；由 Claude 决定降级 L1 或人工处理。
