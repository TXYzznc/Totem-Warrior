# 08-codex-art-gen-mcp 交接清单（给下一窗口的 Claude）

> 上一窗口已完成 T4(MCP 业务解耦,见 09-mcp-decouple change),进入 T1+T2 烟测。
> 本文档列出**已完成的状态**、**剩余的待办**、**每项待办开干前要做什么**,让下一个窗口可以无缝接力。

---

## 0. 上一窗口的最后状态(2026-06-26)

### 已完成

| 项 | 文件 / 入口 | 备注 |
|---|---|---|
| 读官方两份模板 | [codex_input_template_L1_BATCH.md](../06-v21-implementation/art/codex_input_template_L1_BATCH.md) [codex_input_template_L2_SHEET.md](../06-v21-implementation/art/codex_input_template_L2_SHEET.md) | 用户钦定 source of truth |
| L1/L2 模板对齐官方 | [server.py](../../../tools/codex-art-gen-mcp/server.py) `L1_PROMPT_TPL` `L2_PROMPT_TPL` | 单 session 多 image_gen / 单 image_gen 合图 |
| **T4 完成:MCP 业务解耦** | 见 [09-mcp-decouple](../09-mcp-decouple/) | 详见下表 |
| L1/L2 envelope 切到官方字段 | server.py `_run_one_l1_batch` / `_run_one_l2_sheet` | 字段次序与官方一致 |
| 项目级 helper 落地 | [tools/codex-art-gen-helper/](../../../tools/codex-art-gen-helper/) | v21 业务从 MCP 抽出 |
| v21 剩余美术清单 | [art/remaining-prompts.md](../06-v21-implementation/art/remaining-prompts.md) | 14 张 L1 + 8 张 L2 = 22 张待跑 |

#### T4 子项明细

| 项 | 改动 |
|---|---|
| MCP 公开 tool | 从 5 个降为 3 个: `dispatch_l1` / `dispatch_l2` / `write_record` |
| `parse_prompts` / `bucket` | **删除**, 业务挪到 helper |
| `dispatch_l1` / `dispatch_l2` | 接口改为**直传 envelope**(不再读磁盘 batch json) |
| `write_record` | `record_path` 由调用方传**绝对路径** |
| L2_PROMPT_TPL | 删除 "Hades-style" 风格指南行;风格由 item.prompt 自带 |
| `batch_parser.py` / `bucketizer.py` | 物理迁移到 `tools/codex-art-gen-helper/`,业务常量参数化 |
| `expand_v21.py` | 新增,v21 专属入口:读 prompts.md → 展开 → 分桶 → 直传 envelope |
| `sheet_cutter.py` | 外部工具路径支持 `CODEX_ART_GEN_*` env 覆盖 |
| AST / grep 验证 | MCP 目录无 `Hades` `STYLE_BASE` `L1_CATEGORIES` `v21` 等业务字符串 |

### 未完成(**没有真烧 codex token 跑过**)

- 端到端实跑:1 个 L1 batch + 1 个 L2 sheet 都没真跑过新接口
- MCP stdio 协议没真测过(一直 Python import 旁路)

---

## 1. 待办清单(执行顺序建议)

### T1. 真测 MCP stdio 协议(最低风险,先做这个找出 stdio 层 bug)

**目标**:通过 `mcp__codex-art-gen__*` 工具真走一遍 3 个 tool,证明 MCP server 在 Claude Code 重启后能被正确识别 + 调用。

**开干前**:
- 不需要 grill-me(只是测试现有路径)
- 重启 Claude Code 后**先** `/mcp` 列服务,确认 `codex-art-gen` 在线
- **不要**直接拿 v21 22 张正式美术开跑,先用烟测脚本

**执行方式**:

方案 A(推荐,无烧 token):跑烟测脚本 `tools/codex-art-gen-helper/smoke_test.py`
```bash
.venv/Scripts/python tools/codex-art-gen-helper/smoke_test.py
```
- 它会调 server 的 handler 函数(走 codex exec → 真烧 token!)
- 输出在 `.smoke-test-out/`:`smoke_apple.png` `smoke_banana.png` `smoke_potion_*.png` 4 张 + `smoke_sheet.png` + `smoke_record.md`

方案 B(走真 stdio 链路):
1. `mcp__codex-art-gen__dispatch_l1` → 传 `batches=[<smoke L1 batch>]`(直接抄 smoke_test.py 里的 L1_BATCH 常量)
2. `mcp__codex-art-gen__dispatch_l2` → 传 `sheets=[<smoke L2 sheet>]`
3. `mcp__codex-art-gen__write_record` → `record_path=".smoke-test-out/smoke_record.md"`,results=前两步合并

**验收**:
- [ ] 3 个 tool 都能从 stdio 协议层正确返回 JSON
- [ ] 没有 `subprocess_exec` / Windows .cmd 报错
- [ ] L1 输出 2 张独立 PNG(apple / banana),都 > 1KB
- [ ] L2 合图存在 + chroma_key + image_cut 跑通,切出 4 张独立 potion PNG

**已知坑**:见 [.claude/skills/codex-image-gen/SKILL.md](../../../.claude/skills/codex-image-gen/SKILL.md) §七(8 条)

---

### T2. 端到端验证 A + B 两条路同时跑通

T1 烟测通过实际上已经验证 A + B 两条路(L1 + L2)。若 T1 通过,T2 直接合并视为完成,标 ✓ 进 T5。

**追加验收**(看 codex log 行为):
- [ ] L1 codex log 显示**同一 session 内**多次 image_gen 调用(关键!)
- [ ] L2 codex log 显示**1 次** image_gen 调用
- [ ] 整个过程**没有**自动重试、没有读项目文档

**如果 A 失败**(codex 在 session 内还是只调 1 次 image_gen 或装死),跳到 T3。

---

### T3.(兜底)改 L1 模式为「每图独立 exec + 外部并发」

**触发条件**:T2 验收里 A 失败。

**目标**:放弃「codex 内多 image_gen」路径,改成**每张 L1 图 = 1 个独立 codex exec**,由 MCP 用 `asyncio.gather + Semaphore` 控并发。

**⚠️ 开干前需走 grill-me**(与官方模板方向反转):
- 用户原话曾反对「session 数量爆炸」,改回每图独立 exec 是否还能接受?
- token 成本:每图独立 exec → 每图独立 Codex 初始化开销(虽 cwd 隔离已省 93%)
- 失败重试:每图独立 exec 后单图重跑更简单

**改动范围**:
- `tools/codex-art-gen-mcp/server.py` 的 `_run_one_l1_batch`
- 把当前「1 个 batch → 1 个 exec → N 张」改成「1 个 batch → N 个 exec(并发)→ N 张」
- `L1_PROMPT_TPL` 改成单图版本

**不要**直接动;先 grill-me。

---

### T5. v21 剩余美术正式跑(T1 + T2 通过后启动)

**前提**:T1 + T2 都通过,smoke_test.py 4 张图 + 1 合图都成功落盘。

**清单**:见 [openspec/changes/06-v21-implementation/art/remaining-prompts.md](../06-v21-implementation/art/remaining-prompts.md)
- L1:14 张(item / character / env)
- L2:8 张(recipe sheet)
- 合计 22 张

**执行**:
```python
from codex_art_gen_helper.expand_v21 import build_envelopes
l1, l2 = build_envelopes()  # skip_done 会自动过滤已落盘
# 通过 MCP 调用:
mcp.dispatch_l1(batches=l1)
mcp.dispatch_l2(sheets=l2)
mcp.write_record(record_path="<abs>/art/raw/生成记录.md", results=[...])
```

**注意**:
- v21 美术清单可能不完整(用户曾说"不一定是 22 张,看到时候对比需求"),开跑前可对照 GDD-v2 §11 + §13 再核
- `env_floor_*` / `env_wall_*` helper 自动 `transparent=false`
- 每 batch ≤ 6 张(`V21_L1_PER_BATCH`),每 sheet ≤ 16 张(`V21_L2_PER_SHEET`),已在 expand_v21.py 配好

---

### T6. 收尾

- `openspec verify-change 09-mcp-decouple`
- `openspec archive-change 09-mcp-decouple`
- `openspec archive-change 08-codex-art-gen-mcp`(也可以并发)
- 同步 [项目知识库（AI自行维护）/INDEX.md](../../../项目知识库（AI自行维护）/INDEX.md):加 09 入口

---

## 2. 关键上下文(下一窗口必读)

### 2.1 必读文件

- [.claude/skills/codex-image-gen/SKILL.md](../../../.claude/skills/codex-image-gen/SKILL.md) **§七 踩坑实录**(8 条,2026-06-26)
- [openspec/changes/06-v21-implementation/art/codex_input_template_L1_BATCH.md](../06-v21-implementation/art/codex_input_template_L1_BATCH.md)
- [openspec/changes/06-v21-implementation/art/codex_input_template_L2_SHEET.md](../06-v21-implementation/art/codex_input_template_L2_SHEET.md)
- [openspec/changes/09-mcp-decouple/proposal.md](../09-mcp-decouple/proposal.md) + [design.md](../09-mcp-decouple/design.md)
- [tools/codex-art-gen-mcp/README.md](../../../tools/codex-art-gen-mcp/README.md)
- [tools/codex-art-gen-helper/__init__.py](../../../tools/codex-art-gen-helper/__init__.py)

### 2.2 新接口签名(T4 完成后)

```python
mcp.dispatch_l1(batches=[
  {
    "batch_id": "v21-l1-001",
    "writable_roots": ["D:/.../art/raw"],
    "items": [
      {"index":1, "name":"...", "file":"D:/.../abs.png",
       "size":"1024x1024", "transparent":True,
       "prompt":"...", "negative":"..."}
    ]
  }
], concurrency=2)

mcp.dispatch_l2(sheets=[
  {
    "sheet_id": "v21-l2-001",
    "canvas": "D:/.../_merged/abs.png",
    "writable_roots": ["D:/.../art/raw"],
    "grid_rows": 2, "grid_cols": 4,
    "chroma_key": "#00ff00",
    "items": [
      {"index":1, "name":"...", "target_file":"D:/.../abs.png",
       "prompt":"...", "negative":"..."}
    ]
  }
], concurrency=2)

mcp.write_record(record_path="D:/.../art/raw/生成记录.md", results=[...])
```

### 2.3 当前可跑的现状

- `.venv/Scripts/python` 是 Windows Python(PIL/Pillow/mcp 都装好了)
- codex CLI 通过 `tools/codex-art-gen-mcp/codex_runner.py` 调;Windows 下用 `cmd.exe /c codex.cmd ...`
- `chroma_key.py` / `image_cut.py` 已验证可用;env 覆盖见 README

### 2.4 已知**不能改**

- `Assets/Scripts/Core/*`(框架核心)
- `.claude/agents/*.md`(agent 定义)
- 已对齐官方模板的 L1/L2_PROMPT_TPL,**除非走 T3** 否则不要改

### 2.5 别再踩的坑(SKILL §七 速查)

1. gpt-image-2 不支持 transparent → 必走 chroma-key
2. 不要给 codex 写完整 JSON 模板(mini 会偷懒原样回抄)
3. Windows codex.cmd 要 `cmd.exe /c` 包一层
4. 外部工具先 `--help` / 干跑一次看输出,再写消费代码
5. `prompts.md` 的 `file` 字段必须是 3 段 `raw/<cat>/<name>.png`
6. MCP 不要硬编码业务 ← 已完成 T4 解决
7. 路径变量命名要清晰 + 单元测试
8. 别用「测试」名义绕过 MCP stdio 直 import

---

## 3. 收尾动作(任务做完后做)

- T1 + T2 通过:补一条 `生成记录.md`,把这次跑过的烟测 batch + sheet 记录
- T3 触发:建独立 change `<NN-l1-per-exec>`
- T5 启动:不用建新 change,在 06-v21-implementation 上加 task
- 全部完成:`openspec archive-change 08-codex-art-gen-mcp` + `09-mcp-decouple` + INDEX

---

*生成于 2026-06-26,上一窗口最后操作:T4 完成(MCP 业务解耦,9 个文件变更,AST/grep 全过),v21 剩余美术清单已落盘。*
