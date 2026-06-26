# 08-codex-art-gen-mcp — 设计

## 一、架构总览

```
┌─────────────────────────────────────────────────────────┐
│ Claude (大脑)                                            │
│   ├ 调 codex_art_gen.parse_prompts(change_name)          │
│   ├ 调 codex_art_gen.bucket(items)                       │
│   ├ 调 codex_art_gen.dispatch_l1(batch_json)  ← 并发 2 路 │
│   ├ 调 codex_art_gen.dispatch_l2(sheet_json)  ← 并发 2 路 │
│   └ 调 codex_art_gen.write_record(results)               │
└─────────────────────────────────────────────────────────┘
              │ MCP stdio (JSON-RPC)
              ▼
┌─────────────────────────────────────────────────────────┐
│ tools/codex-art-gen-mcp/server.py (Python, 常驻)         │
│                                                          │
│  ┌────────────────┐  ┌────────────────┐                  │
│  │ parse_prompts  │  │ bucket         │                  │
│  │ (纯本地解析)    │  │ (L1/L2 分桶)   │                  │
│  └────────────────┘  └────────────────┘                  │
│                                                          │
│  ┌────────────────────────┐  ┌────────────────────────┐  │
│  │ dispatch_l1            │  │ dispatch_l2            │  │
│  │  ├ semaphore(2)        │  │  ├ semaphore(2)        │  │
│  │  ├ subprocess codex    │  │  ├ subprocess codex    │  │
│  │  │   - cwd=runner_dir  │  │  │   - cwd=runner_dir  │  │
│  │  │   - -m mini         │  │  │   - -m mini         │  │
│  │  │   - --ephemeral     │  │  │   - --ephemeral     │  │
│  │  │   - --skip-git-..   │  │  │   - --skip-git-..   │  │
│  │  │   - -o result.json  │  │  │   - -o result.json  │  │
│  │  │   - writable_roots  │  │  │   - writable_roots  │  │
│  │  ├ 解析 result.json    │  │  ├ image_cut.py        │  │
│  │  └ 验证 size>1KB       │  │  └ 按 layout 命名      │  │
│  └────────────────────────┘  └────────────────────────┘  │
│                                                          │
│  ┌────────────────┐                                      │
│  │ write_record   │                                      │
│  │ (追加 md)      │                                      │
│  └────────────────┘                                      │
└─────────────────────────────────────────────────────────┘
              │ subprocess
              ▼
┌─────────────────────────────────────────────────────────┐
│ codex exec (cwd=/tmp/codex-art-gen-runner)              │
│   只做：读 prompt → image_gen → 落盘 → 返 JSON          │
└─────────────────────────────────────────────────────────┘
```

## 二、目录结构

```
tools/codex-art-gen-mcp/
├── server.py              # MCP stdio 入口，5 tool handler
├── codex_runner.py        # 封装 codex exec 调用（cwd 隔离 + 参数）
├── batch_parser.py        # parse_prompts 实现
├── bucketizer.py          # L1/L2 分桶逻辑
├── sheet_cutter.py        # 调 image_cut.py 切图 + layout_order 回填
├── requirements.txt       # mcp / asyncio （内置即可）
└── README.md

# 运行时隔离目录（首次启动自动建）
/tmp/codex-art-gen-runner/
└── AGENTS.md              # 极短文本：你是图片生成执行器...
```

## 三、5 个 MCP 工具的契约

### Tool 1: `parse_prompts`

```json
input:  {"change_name": "06-v21-implementation"}
output: {
  "success": true,
  "items_json_path": "openspec/changes/06-v21-implementation/art/.batch-items.json",
  "total": 82,
  "by_category": {"weapon": 5, "skill": 8, ...}
}
```

实现：读 `art/prompts.md` 提取 \`\`\`json 块，规范化为 items 数组。env_floor/wall 自动加 `transparent=false`。

### Tool 2: `bucket`

```json
input:  {"items_json_path": "...", "l1_per_batch": 6, "l2_per_sheet": 16}
output: {
  "success": true,
  "l1_batches": [".batch-l1-001.json", ".batch-l1-002.json", ...],
  "l2_sheets":  [".sheet-l2-001.json", ".sheet-l2-002.json", ...],
  "summary": {"l1_items": 18, "l2_items": 64}
}
```

按类别决定 L1/L2：character/boss/npc/env → L1；weapon/skill/affix/paint/recipe/consumable/hud → L2。已 ok 的图自动 skip。

### Tool 3: `dispatch_l1`

```json
input:  {"batch_paths": [".batch-l1-001.json", ".batch-l1-002.json"]}
output: {
  "success": true,
  "results": [
    {"batch": "...001", "ok": 6, "failed": 0, "items": [{"file": "...", "status": "ok", "size_bytes": 524288}, ...]},
    {"batch": "...002", "ok": 5, "failed": 1, "items": [...]}
  ]
}
```

实现：`asyncio.Semaphore(2)`，每个 batch 一次 codex exec。codex 在 runner_dir 启动，写到 batch 指定的 art/raw 路径。

### Tool 4: `dispatch_l2`

```json
input:  {"sheet_paths": [".sheet-l2-001.json", ".sheet-l2-002.json"]}
output: {
  "success": true,
  "results": [
    {
      "sheet": "...001",
      "canvas_path": "art/raw/_merged/paint_sheet_01.png",
      "cuts_ok": 16,
      "cuts_failed": 0,
      "cut_items": [{"name": "paint_red_common", "file": "art/raw/paint/paint_red_common.png", "status": "ok"}, ...]
    }
  ]
}
```

实现：单次 codex exec 生 1024×1024 透明合图 → image_cut.py 按 alpha 切 16 张 → 按 grid row buckets + cx 排序回填 `layout_order` 命名 → 移动到对应类别目录。

### Tool 5: `write_record`

```json
input:  {"change_name": "06-v21-implementation", "results": [...]}
output: {"success": true, "record_path": "art/raw/生成记录.md", "appended_rows": 22}
```

追加到 `生成记录.md`，含批次 / 模式 / 资源名 / 路径 / 状态 / 错误。

## 四、codex exec 实际调用参数（codex_runner.py）

```python
cmd = [
    "codex", "exec",
    "--skip-git-repo-check",
    "--ephemeral",
    "-s", "workspace-write",
    "-c", f"sandbox_workspace_write.writable_roots=[\"{art_raw_abs}\"]",
    "-m", "gpt-5.4-mini",                  # 默认 mini，可参数覆盖
    "-o", result_json_path,
    "-",                                    # prompt 从 stdin 读
]
# cwd = /tmp/codex-art-gen-runner
# stdin = prompt 文件内容
# stderr 重定向到 batch 独立日志文件，不交叠
```

## 五、并发实现要点

```python
import asyncio
sem = asyncio.Semaphore(2)

async def run_one_batch(batch):
    async with sem:
        # subprocess.run codex exec
        ...

results = await asyncio.gather(*[run_one_batch(b) for b in batches], return_exceptions=True)
```

每个 batch 独立日志：`art/raw/.mcp-log-l1-001.log` 等。

## 六、错误处理边界

- 单 item 失败 → 标 `failed` 继续（不重试，不抛异常）
- 整 batch codex exec 失败 → 该 batch 全部 items 标 `failed`，其他 batch 不受影响
- MCP 工具调用本身失败（如 prompts.md 不存在） → 返 `{"success": false, "error": "..."}`，不抛异常

## 七、关于美术资源 ID 与 ResourceConfig

不在本 change 范围。生图结果落地 `art/raw/<category>/<name>.png` 即可，Claude 后续手动接入 ResourceConfig.json。

## 八、回滚方案

如果新 MCP 出问题：
1. `.claude/settings.local.json` 把 `codex-art-gen` 从 `enabledMcpjsonServers` 移除
2. `.mcp.json` 留着不影响
3. 仍可手抄一份 `.codex-batch.sh` 作临时方案（grill A 同意删但 git 可恢复）

不留 `.codex-batch-legacy.sh` 是因为 grill A 用户明确选了"直接删"。
