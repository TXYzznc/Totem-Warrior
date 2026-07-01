"""
直接 import codex-art-gen server.py 的 tool_dispatch_l1 函数运行 batches。
绕过 MCP stdio 协议，但用同样的核心逻辑（codex_runner + chroma_key 一致）。

注意：MCP stdio 模式下 server.py 有 stdin 死锁陷阱（SKILL §7.9），
本脚本作为独立 Python 进程跑，stdin 是 tty，没有该问题。
"""
import asyncio
import json
import sys
import time
from pathlib import Path

# 注入 server.py 路径
sys.path.insert(0, str(Path("d:/unity/UnityProject/GameDesinger/tools/codex-art-gen-mcp").resolve()))

import server  # noqa: E402


async def run(batches_json_path: str):
    with open(batches_json_path, encoding="utf-8") as f:
        args = json.load(f)
    start = time.time()
    result = await server.tool_dispatch_l1(args)
    elapsed = time.time() - start

    # 输出汇总
    print(f"\n=== {batches_json_path} ({elapsed:.1f}s) ===")
    total_ok, total_fail = 0, 0
    for r in result["results"]:
        bid = r["batch_id"]
        ok = r.get("ok", 0)
        failed = r.get("failed", 0)
        total_ok += ok
        total_fail += failed
        print(f"  {bid}: ok={ok} failed={failed}")
        for it in r.get("items", []):
            if it.get("status") == "failed":
                print(f"    FAIL  idx={it['index']} {Path(it['file']).name}: {it.get('error','')}")
    print(f"\nTOTAL ok={total_ok} failed={total_fail} elapsed={elapsed:.1f}s")

    # 把汇总结果回写到磁盘，方便后续生成记录
    out = batches_json_path.replace(".json", "_result.json")
    with open(out, "w", encoding="utf-8") as f:
        json.dump(result, f, ensure_ascii=False, indent=2)
    print(f"result written: {out}")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("usage: _run_batches.py <batches_json_path>")
        sys.exit(1)
    asyncio.run(run(sys.argv[1]))
