# Proposal — 05-codex-batch-art-protocol

> **范围**：把 [codex-image-gen](../../../.claude/skills/codex-image-gen/SKILL.md) 从「每张图一次 codex exec」改造为「批量协议（L1 进程内并行 + L2 合并画布）」，节省 Codex 额度。
> **决策日期**：2026-06-25
> **决策方式**：grill-me 阶段 A 出口（用户答 5 条 checklist + 工具链 PIL/ImageCut 验证）
> **预计阶段**：1 阶段（SKILL 文档改造 + demo 验证 + 归档）

---

## Why

（原章节名「为什么做」，openspec 期望英文 header）



Codex 额度有限，用户主要把 Codex 当**绘图后端**（Claude 主对话无图像生成能力）。旧 codex-image-gen SKILL §3.4 明确规定「每张图一次调用」，N 张图启动 N 次 codex 进程 + 加载 N 次 system prompt → 固定开销重复消耗。

升级方案两层并行：

| 层 | 机制 | 节省点 |
|---|---|---|
| **L1 · 进程内并行** | 单次 codex exec 塞 ≤12 张独立画作清单 → imagegen 并行处理 | 省 N-1 次 system prompt 与进程启动 |
| **L2 · 合并画布** | 单次 imagegen 在 1024×1024 透明画布画 N 个小资源 → ImageCut 按 alpha 切割 | **省 N-1 次 imagegen 调用本身**（最大节省点） |

## 目标（DoD）

- ✅ codex-image-gen SKILL §三 重写为 L0/L1/L2 三档自动归类
- ✅ 触发阈值：L2 单张 ≤256×256 且透明背景；L1 默认 8 张/批、上限 12
- ✅ 失败重试上限 1 次（整批），仍失败标 failed；L2 整批失败自动降级 L1
- ✅ 切割工具：`.venv/Scripts/python tools/ImageCut_Tool/image_cut.py` + `--alpha 16 --min-area 80 --padding 2 --debug --json`
- ✅ 9 张 256×256 透明 icon demo（本 change `art/`）实测通过：1 次 codex exec → 1 次 imagegen → 9 张独立 PNG

## 非目标

- ❌ 不改 [ai-art SKILL](../../../.claude/skills/ai-art/SKILL.md) 前置流程
- ❌ 不改 [settings.json hook](../../../.claude/settings.json)（关键词触发逻辑不变）
- ❌ 不引入新依赖（Pillow 已在 .venv，ImageCut_Tool 已在 tools/）
- ❌ 不改 SKILL_MATRIX 白名单（codex-image-gen 仍属 art-director/art-2d/art-ui）

## 用户决策摘要（grill-me 阶段 A 出口）

| 决策点 | 选择 | 理由 |
|---|---|---|
| L1 单批大小 | 默认 8、上限 12 | 留 Codex JSON 输出 buffer |
| L2 触发阈值 | ≤256×256 + 透明 | 典型 icon 场景；大图不强行合并 |
| 切割工具 | A. ImageCut_Tool（已有） | alpha 连通域切割 > 固定网格，更灵活 |
| 失败策略 | a. 失败收集 → 整批跑完后重试 1 次 | 平衡"不死循环"与"偶发故障容忍" |
| 验收方式 | b. 新建 demo change 实测 | 干净不污染主任务 |

## What Changes

（原章节名「文件变更清单」，openspec 期望英文 header）


| 文件 | 改动 |
|---|---|
| `.claude/skills/codex-image-gen/SKILL.md` | §三重写为三档 / §五陷阱表更新 |
| `openspec/changes/05-codex-batch-art-protocol/` | **新建**（本目录 — 含 demo art/） |
| `项目知识库（AI自行维护）/INDEX.md` | 追加 05 条目 |

## 风险与回滚

| 风险 | 缓解 |
|---|---|
| imagegen 不遵守 ≥32px padding → ImageCut 切误 | 用 `--alpha 16 --open 1` 激进分离；切出数 ≠ 清单数时整批降级 L1 |
| imagegen 不按 layout_order 排布 → 资源名映射错位 | Claude Read debug.png 视觉校正；混乱严重时降级 L1 |
| L1 JSON 输出被 Codex 截断 | 单批 ≤12；超过自动分批 |

回滚：`git revert <本次 commit>` 即可恢复旧 SKILL §三。

## 引用

- [.claude/skills/codex-image-gen/SKILL.md](../../../.claude/skills/codex-image-gen/SKILL.md) — 改造主体
- [tools/ImageCut_Tool/README.md](../../../tools/ImageCut_Tool/README.md) — alpha 连通域切割
- 视频：「ClaudeCode + Codex 6 种组合机制」（用户分享文字摘要，机制①+⑥落地）
