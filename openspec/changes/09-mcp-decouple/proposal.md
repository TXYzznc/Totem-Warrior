# 09-mcp-decouple — codex-art-gen MCP 业务解耦（C 方案）

> 状态：阶段 B 实施中
> 创建：2026-06-26
> 触发：08 落地后，MCP 内部 hardcode 了 Hades 风格 / v21 分类 / `parents[2]` 等项目级业务,导致跨项目复用不可能。grill A 已确认走 **方案 C：调用方负责展开,MCP 只剩跑 codex + 后处理**。

## 一、为什么做

08-codex-art-gen-mcp 上线后,以下业务硬编码点遗留在 MCP 内（grill A 已识别）:

| 文件 | 位置 | 业务 |
|---|---|---|
| `tools/codex-art-gen-mcp/batch_parser.py` | `STYLE_BASE` `NEG_BASE` | "Hades-style 2.5D 厚涂..." 风格指南 |
| 同上 | `NON_TRANSPARENT_PREFIXES = ("env_floor","env_wall")` | v21 项目专属命名约定 |
| `tools/codex-art-gen-mcp/bucketizer.py` | `L1_CATEGORIES` `L2_CATEGORIES` | v21 项目的 8 个分类 + 哪些走 L1/L2 |
| `tools/codex-art-gen-mcp/server.py` | `REPO_ROOT = Path(...).parents[2]` | 假设 MCP 在 `tools/codex-art-gen-mcp/` 之下 |
| 同上 | `L2_PROMPT_TPL` 内 "Hades-style 2.5D 厚涂笔触风格" | 模板里又写一遍风格 |

用户原话："这是 26 个文件后引入的债,越早还越好"。再不还,T1 / T2 / 后续美术批次都会强化这套业务耦合。

## 二、目标

1. **MCP 完全无业务**:跨项目复用,装到任何 openspec 项目都能直接跑
2. **MCP 只剩跑 codex + 后处理**:`dispatch_l1` / `dispatch_l2` / `write_record` 三件套
3. **业务回到调用方**:Claude 主对话用项目级 helper 脚本读 prompts.md / 展开 STYLE_BASE / 分桶,然后把展开好的 envelope 传给 MCP
4. **不破坏 v21 已有产物**:`raw/ui/*.png` 16 张 UI 测试图 + 已经跑过的素材保留

## 三、关键决策（grill A 共识)

| 项 | 选择 | 备选理由 |
|---|---|---|
| G1 动机 | **A. 跨项目复用** | MCP 当成可装到任何 openspec 项目的通用工具 |
| G2 方案 | **C. 调用方负责展开** | 最彻底解耦; B 只是把硬编码换成参数,业务仍在 MCP 里跑; A 加 profile.yaml 仍要 MCP 知道 profile 字段语义 |
| G3 边界 | **重构当前内容,旧产物不丢** | 对比 v21 已完成美术 → 剩余未完成的用新工作流跑 |
| G4 验收 | **先烟测再正式跑** | 烟测 = L1 2 张 + L2 4 张; 通过后再跑剩余美术 |
| G5 时限 | **本会话内完成** | 不破坏 chroma_key.py / image_cut.py / 16 张 UI 测试图 |

## 四、不做

- 不动 `chroma_key.py` / `image_cut.py`(通用工具,本身不耦合业务)
- 不动 Codex CLI 调用层 `codex_runner.py`(已稳定,Windows .cmd 兼容已修)
- 不写新 MCP server（继续在 08 的 server.py 上重构）
- 不删 `batch_parser.py` / `bucketizer.py` —— **挪到** `tools/codex-art-gen-helper/` 作为项目级 helper, MCP 不再 import 它们
- 不跑剩余美术 (T5);烟测通过后由用户决定何时启动

## 五、验收标准

- [ ] MCP 内 grep 不到 `Hades` / `v21` / `env_floor` / `parents[2]` / `L1_CATEGORIES` / `STYLE_BASE`
- [ ] MCP 公开接口降为 3 个:`dispatch_l1` / `dispatch_l2` / `write_record`
- [ ] `tools/codex-art-gen-helper/` 目录承载 `batch_parser.py` / `bucketizer.py` (项目级 helper)
- [ ] `batch_parser.py` / `bucketizer.py` 仍然能跑通 v21 的 prompts.md
- [ ] 烟测脚本:1 L1 batch(2 张独立) + 1 L2 sheet(4 张合图) 全跑通,落盘且 chroma_key + image_cut 都生效
- [ ] v21 的 `prompts.md` 经对比剔除已完成,留下未完成清单(成一个新文件,不直接改老的)

## 六、关键约束

- 本会话内完成
- 不破坏 v21 已生成 16 张 UI 测试图(`openspec/changes/06-v21-implementation/art/raw/ui/*.png`)
- 不动框架核心 (`Assets/Scripts/Core/*`)
- 不动 chroma_key.py / image_cut.py / codex_runner.py
- 保留 v21 老 `prompts.md` 不直接改,新工作流走新文件

## 七、影响范围

| 类型 | 文件 |
|---|---|
| 修改 | `tools/codex-art-gen-mcp/server.py`(删 parse_prompts / bucket,精简 L2_PROMPT_TPL) |
| 修改 | `.mcp.json`（路径可能调整) |
| 新增 | `tools/codex-art-gen-helper/__init__.py` + `batch_parser.py` + `bucketizer.py` + `README.md` |
| 新增 | `tools/codex-art-gen-helper/expand_v21.py`(项目级展开脚本,把 prompts.md → MCP 直传 envelope) |
| 移除 | `tools/codex-art-gen-mcp/batch_parser.py` / `bucketizer.py`(挪走) |
| 新增 | `openspec/changes/06-v21-implementation/art/remaining-prompts.md`(剔除已完成的剩余美术清单) |
