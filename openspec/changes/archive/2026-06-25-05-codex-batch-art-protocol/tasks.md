# Tasks — 05-codex-batch-art-protocol

## Phase 1：SKILL 改造（已完成）

- [x] T1.1 codex-image-gen SKILL.md §三整体重写为 L0/L1/L2 三档
- [x] T1.2 codex-image-gen SKILL.md §五陷阱表更新（删旧、加 L1/L2 新陷阱）
- [x] T1.3 工具链可用性验证：`.venv/Scripts/python` + `tools/ImageCut_Tool/image_cut.py`

## Phase 2：Demo 实测（已完成）

- [x] T2.1 准备 demo art/requirements.md + art/prompts.md（9 个 256×256 透明 icon）
- [x] T2.2 调 codex exec 生成 1024×1024 透明画布（按 L2 协议）— 85,391 tokens
- [x] T2.3 ImageCut 切割 → 9 sprites + debug.png + manifest.json ✓
- [x] T2.4 Claude Read debug.png 校验资源名映射 — 视觉确认 row-major 排布正确
- [x] T2.5 写 art/raw/生成记录.md，更新 requirements.md 头部状态
- [x] T2.6 迭代：发现 (y,x) 排序在同行 y 抖动时跨行错位 → 改 SKILL §3.5/§3.6 引入 grid_rows + row-bucketize → 重跑映射 ✓

## Phase 3：归档（进行中）

- [x] T3.1 更新 `项目知识库（AI自行维护）/INDEX.md` 添加 05 条目 + wiki 条目
- [x] T3.2 `openspec archive 05-codex-batch-art-protocol`
- [x] T3.3 节省额度报告：实测 85k tokens vs 旧协议预估 ~450k tokens（~9 × imagegen），节省 ~81%；imagegen 调用数 9→1，节省 88.9%
