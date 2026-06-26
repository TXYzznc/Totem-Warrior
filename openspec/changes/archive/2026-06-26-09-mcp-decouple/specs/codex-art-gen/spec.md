# spec: codex-art-gen MCP（解耦后接口契约）

## 公开工具（3 个）

### dispatch_l1

输入：
```json
{
  "batches": [
    {
      "batch_id": "string",
      "writable_roots": ["abs/path/dir", "..."],
      "items": [
        {
          "index": 1,
          "name": "asset_name",
          "file": "abs/path/to/output.png",
          "size": "1024x1024",
          "transparent": true,
          "prompt": "full English prompt with all style baked in",
          "negative": "negative prompt"
        }
      ]
    }
  ],
  "concurrency": 2
}
```

输出：
```json
{
  "success": true,
  "results": [
    {
      "batch_id": "...",
      "ok": 2,
      "failed": 0,
      "items": [{"index": 1, "file": "...", "size_bytes": 123, "status": "ok"}]
    }
  ]
}
```

### dispatch_l2

输入：
```json
{
  "sheets": [
    {
      "sheet_id": "string",
      "canvas": "abs/path/_merged/sheet.png",
      "writable_roots": ["abs/path/dir"],
      "grid_rows": 4,
      "grid_cols": 4,
      "chroma_key": "#00ff00",
      "items": [
        {
          "index": 1,
          "name": "asset_name",
          "target_file": "abs/path/cat/asset_name.png",
          "prompt": "full English prompt",
          "negative": "negative"
        }
      ]
    }
  ],
  "concurrency": 2
}
```

输出：
```json
{
  "success": true,
  "results": [
    {
      "sheet_id": "...",
      "canvas_path": "...",
      "alpha_canvas": "...",
      "cuts_ok": 4,
      "cuts_failed": 0,
      "cut_items": [{"name": "...", "file": "...", "size_bytes": 123, "status": "ok"}]
    }
  ]
}
```

### write_record

输入：
```json
{
  "record_path": "abs/path/to/记录.md",
  "results": [/* dispatch_l1 / dispatch_l2 results */]
}
```

输出：
```json
{"success": true, "record_path": "...", "appended_rows": 12}
```

## 不变契约

- L1 = 1 batch → 1 codex exec → 1 session → N 次 image_gen → N 张 PNG
- L2 = 1 sheet → 1 codex exec → 1 session → 1 次 image_gen → 1 张合图 → 本地 chroma_key → 本地 image_cut → N 张 PNG
- 并发上限默认 2（用户自定义）
- 失败 item 标 `failed` 不中断整批
- MCP **不读** prompts.md / project files,调用方负责展开
- MCP **不假设** REPO_ROOT 位置;所有路径必须是绝对路径

## 项目级 helper（不属 MCP 契约,仅参考）

`tools/codex-art-gen-helper/expand_v21.py` 是 v21 项目的展开入口,产出符合上面 dispatch_l1 / dispatch_l2 入参格式的 envelope。其他项目可以照葫芦画瓢写自己的 `expand_<project>.py`。
