# 09-mcp-decouple — 设计

## 一、新接口（MCP 公开 3 个 tool）

### `dispatch_l1(batches, concurrency?)`

```python
batches: list[{
    "batch_id": str,           # 用户自定义，用于日志和返回标识
    "writable_roots": [str],   # codex sandbox 可写目录列表（必须含所有 item.file 的父目录）
    "items": [{
        "index": int,
        "name": str,
        "file": str,           # 绝对路径
        "size": "1024x1024",
        "transparent": bool,
        "prompt": str,         # **调用方已展开** STYLE_BASE / 风格指南
        "negative": str,       # **调用方已展开** NEG_BASE
    }]
}]
concurrency: int = 2
```

**返回**：
```python
{
  "success": True,
  "results": [{
    "batch_id": ..., "ok": int, "failed": int,
    "items": [{"index":..., "file":..., "size_bytes":..., "status":"ok"|"failed", "error"?:...}]
  }]
}
```

### `dispatch_l2(sheets, concurrency?)`

```python
sheets: list[{
    "sheet_id": str,
    "canvas": str,             # 绝对路径
    "writable_roots": [str],
    "grid_rows": int,
    "grid_cols": int,
    "chroma_key": "#00ff00",   # 可选，默认 #00ff00
    "items": [{                # 同 L1 但 file 改名 target_file
        "index": int,
        "name": str,
        "target_file": str,    # 绝对路径，切图后最终位置
        "prompt": str,
        "negative": str,
    }]
}]
```

**返回**：
```python
{
  "success": True,
  "results": [{
    "sheet_id": ..., "canvas_path":..., "alpha_canvas":...,
    "cuts_ok": int, "cuts_failed": int,
    "cut_items": [{"name":..., "file":..., "size_bytes":..., "status":...}]
  }]
}
```

### `write_record(record_path, results)`

```python
record_path: str  # 绝对路径
results: list    # dispatch_l1 / dispatch_l2 的 results 合集
```

简化版：写 markdown 表格。MCP 不再假设 `art/raw/生成记录.md` 这种项目级路径。

---

## 二、删掉的 MCP tool

- `parse_prompts` — 业务,挪到 `tools/codex-art-gen-helper/`
- `bucket` — 业务,挪到 `tools/codex-art-gen-helper/`

---

## 三、prompt 模板的处理

### L1_PROMPT_TPL（不动）

已对齐官方,纯流程文案,无业务。保留。

### L2_PROMPT_TPL（删风格那行）

```diff
- - 所有素材统一使用 Hades-style 2.5D 厚涂笔触风格, 颜色饱和, 深色描边阴影。
```

风格指南由调用方在每个 item.prompt 里写明。MCP 模板只剩 chroma-key 和网格规则（这些是 chroma_key + image_cut 后处理的物理约束,不算业务）。

---

## 四、helper 包结构

```
tools/codex-art-gen-helper/
├── __init__.py
├── README.md
├── batch_parser.py        # 原 batch_parser.py 不变,但 STYLE_BASE/NEG_BASE 改为参数
├── bucketizer.py          # 原 bucketizer.py,L1_CATEGORIES/L2_CATEGORIES 改为参数
└── expand_v21.py          # v21 项目专用入口：硬编码 v21 的 STYLE_BASE/分类,调上面两个 lib
```

### `expand_v21.py` 接口

```python
def build_envelopes(change_name="06-v21-implementation") -> tuple[list[dict], list[dict]]:
    """读 v21 prompts.md → 展开 → 分桶 → 返回 (l1_batches, l2_sheets) 可直接传给 MCP dispatch_*"""
```

调用方（Claude 主对话）只需:
```python
from codex_art_gen_helper.expand_v21 import build_envelopes
l1, l2 = build_envelopes()
mcp.dispatch_l1(batches=l1)
mcp.dispatch_l2(sheets=l2)
```

---

## 五、v21 现状处理

1. 跑 `expand_v21.build_envelopes()` 拿全集
2. 扫 `openspec/changes/06-v21-implementation/art/raw/**.png`,> 1KB 的算已完成
3. 剔除已完成的 item,剩余写到 `openspec/changes/06-v21-implementation/art/remaining-prompts.md`(只供参考,真要跑就再调一次 build_envelopes 后过滤)
4. **本会话不跑这批**,等用户决定

---

## 六、烟测

`tools/codex-art-gen-helper/smoke_test.py`:
```python
# 1. 构造 2 张极简 L1 item（不依赖 v21 业务,纯英文 prompt）
# 2. 构造 4 张极简 L2 item
# 3. 调 dispatch_l1 + dispatch_l2
# 4. 校验落盘 + 大小
```

烟测要求**完全不依赖 v21 prompts.md**,这样能验证 MCP 真的解耦了。

---

## 七、不动的部分

- `chroma_key.py` / `image_cut.py` / `codex_runner.py` / `sheet_cutter.py`
- L1_PROMPT_TPL 模板（已对齐官方）
- L2_PROMPT_TPL 除"Hades-style"那行以外

---

## 八、迁移路径

- 旧 `tools/codex-art-gen-mcp/batch_parser.py` `bucketizer.py` 物理移到 `tools/codex-art-gen-helper/`
- `server.py` 删 import + 删 `tool_parse_prompts` `tool_bucket` 两个 handler
- TOOLS 数组只剩 3 个 Tool
- `.mcp.json` 不动（路径不变）

---

## 九、不可逆变更确认

- 删 `parse_prompts` / `bucket` 工具 —— 这是公开 API 移除,但 v21 调用方已经没用到这两个 tool 的真 stdio 路径（之前一直 Python import 旁路）,影响为零
- 物理挪 `batch_parser.py` `bucketizer.py` —— 仅本仓库内部,无外部依赖
