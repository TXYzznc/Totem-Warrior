# 09-mcp-decouple — 任务

## T1. 建 helper 包骨架

- [ ] `mkdir tools/codex-art-gen-helper/`
- [ ] 新增 `__init__.py` `README.md`

## T2. 物理迁移 batch_parser.py / bucketizer.py

- [ ] `git mv tools/codex-art-gen-mcp/batch_parser.py tools/codex-art-gen-helper/`
- [ ] `git mv tools/codex-art-gen-mcp/bucketizer.py tools/codex-art-gen-helper/`

## T3. helper 参数化（去掉 v21 业务硬编码）

- [ ] `batch_parser.parse_prompts_md(prompts_md, style_base, neg_base, non_transparent_prefixes=())`：业务从模块常量降级为参数
- [ ] `bucketizer.split_l1_l2(items, l1_cats, l2_cats)`：分类集合改入参
- [ ] `bucketizer.group_l2_by_category` `chunk_l1` 已是纯函数,不动

## T4. 项目级 expand_v21.py

- [ ] 新增 `tools/codex-art-gen-helper/expand_v21.py`
- [ ] 内含 v21 专属 `STYLE_BASE` / `NEG_BASE` / `L1_CATS` / `L2_CATS` / `NON_TRANSPARENT_PREFIXES` 常量
- [ ] 暴露 `build_envelopes(change_name="06-v21-implementation") -> (l1_batches, l2_sheets)`
- [ ] envelope 直接符合 MCP `dispatch_l1` / `dispatch_l2` 入参格式（**绝对路径** + 已展开 prompt）

## T5. 删 MCP 的 parse_prompts / bucket

- [ ] `server.py` 删 import `from batch_parser ...` / `from bucketizer ...`
- [ ] 删 `tool_parse_prompts` / `tool_bucket` 函数
- [ ] 删 TOOLS 数组里两条 Tool
- [ ] 删 call_tool dispatch 表里两条
- [ ] 删 `resolve_art_dir`(没人用了)
- [ ] 删 `REPO_ROOT = Path(__file__).resolve().parents[2]`

## T6. dispatch_l1 / dispatch_l2 接口改直传 envelope

- [ ] `tool_dispatch_l1(args)`:`args = {"batches": [...], "concurrency"?: 2}`
- [ ] `run_one_l1_batch(batch_dict)`：从 dict 拿 batch_id / items / writable_roots，不再读文件
- [ ] `tool_dispatch_l2(args)`:`args = {"sheets": [...], "concurrency"?: 2}`
- [ ] `run_one_l2_sheet(sheet_dict)`：同上
- [ ] inputSchema 更新

## T7. L2_PROMPT_TPL 去风格

- [ ] 删 "所有素材统一使用 Hades-style 2.5D..." 那行
- [ ] 调用方在每个 item.prompt 里写风格

## T8. write_record 参数化

- [ ] `tool_write_record(args)`:`args = {"record_path": str, "results": [...]}`
- [ ] 不再要 change_name / art_dir
- [ ] inputSchema 更新

## T9. 烟测

- [ ] `tools/codex-art-gen-helper/smoke_test.py`:1 L1(2 张) + 1 L2(4 张),纯英文 prompt 无业务依赖
- [ ] 跑通即视为 MCP 解耦验证 OK
- [ ] **本会话先不跑**(会烧 codex token,等用户启动);只写脚本可立即跑

## T10. v21 剩余美术清单

- [ ] 扫 `openspec/changes/06-v21-implementation/art/raw/**.png` 已落盘集合
- [ ] 对比 expand_v21 全集
- [ ] 输出 `openspec/changes/06-v21-implementation/art/remaining-prompts.md`(参考用,不直接驱动跑)

## T11. 验证 + AST + grep

- [ ] `python -c "import ast; ast.parse(open('tools/codex-art-gen-mcp/server.py').read())"`
- [ ] `python -m py_compile tools/codex-art-gen-helper/*.py`
- [ ] `grep -rE "Hades|v21|STYLE_BASE|L1_CATEGORIES|parents\\[2\\]" tools/codex-art-gen-mcp/` → 应为空
- [ ] `grep -rE "Hades|v21" tools/codex-art-gen-helper/expand_v21.py` → 允许（这里是项目级）

## T12. 更新 HANDOFF + INDEX

- [ ] `openspec/changes/08-codex-art-gen-mcp/HANDOFF.md`:T4 标完成,T1/T2 更新为新接口签名
- [ ] `项目知识库（AI自行维护）/INDEX.md`:加 09 入口（archive 前可先标 "实施中"）
