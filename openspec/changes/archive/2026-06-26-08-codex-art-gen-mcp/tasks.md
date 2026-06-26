# Tasks — 08-codex-art-gen-mcp

> ✅ = 完成；🟡 = 进行中；🔲 = 未开始

## Phase A — openspec change 骨架（已完成）

- [x] grill 阶段 A 5 条挖透（弹窗 2 轮）✅
- [x] proposal.md ✅
- [x] design.md ✅
- [x] tasks.md（本文件）✅

## Phase B — MCP 核心实现

- [x] `tools/codex-art-gen-mcp/server.py` MCP stdio 入口 + 5 tool handler ✅
- [x] `tools/codex-art-gen-mcp/codex_runner.py` 封装 codex exec（cwd 隔离 + writable_roots + ephemeral + mini 模型）✅
- [x] `tools/codex-art-gen-mcp/batch_parser.py` 解析 prompts.md 的 json 块 → items 数组 ✅
- [x] `tools/codex-art-gen-mcp/bucketizer.py` 按类别分 L1/L2 + 跳过已 ok 文件 ✅
- [x] `tools/codex-art-gen-mcp/sheet_cutter.py` 调 `tools/ImageCut_Tool/image_cut.py` 切图 + layout_order 回填 ✅
- [x] `tools/codex-art-gen-mcp/requirements.txt` ✅
- [x] `tools/codex-art-gen-mcp/README.md` ✅

## Phase C — 注册与启用

- [x] `.mcp.json` 加 `codex-art-gen` 条目 ✅
- [x] `.claude/settings.local.json` enabledMcpjsonServers 加 `codex-art-gen` ✅
- [x] `/tmp/codex-art-gen-runner/AGENTS.md` 首次调用 codex_runner.ensure_runner_dir() 自动创建 ✅

## Phase D — 验证

- [x] 模块 import 全通过 + TOOLS 注册 5 个 ✅
- [x] `parse_prompts(06-v21-implementation)` 实测返回 82 items，分类正确 ✅
- [x] `bucket` 实测正确分桶：3 个 L1 batch + 1 个 L2 sheet（自动跳过已 ok 的 60 张）✅
- [ ] `dispatch_l1` 单 batch 实测 token < 10K（需等额度恢复）
- [ ] `dispatch_l2` 单 sheet 切图后 PNG 命名正确（需等额度恢复）
- [ ] 失败 item 不中断整批（需等额度恢复实测）
- [ ] 2 路并发墙钟时间约为串行一半（需等额度恢复）
- [ ] 用剩 22 张美术（recipe/item/env/character）跑完整流程（**需等 codex 额度恢复**）

## Phase E — 清理与归档

- [x] 删除 `openspec/changes/06-v21-implementation/art/.codex-batch.sh` ✅（grill A 同意，git 可恢复）
- [x] 更新 `项目知识库（AI自行维护）/INDEX.md` 加 08 入口 ✅
- [ ] `openspec archive-change 08-codex-art-gen-mcp`（等 dispatch 实测通过后）
