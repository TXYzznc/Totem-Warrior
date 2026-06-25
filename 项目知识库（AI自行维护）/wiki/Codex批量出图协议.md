---
title: Codex 批量出图协议（L1 进程内并行 + L2 合并画布）
owner: tools-engineer / art-director
created: 2026-06-25
last_updated: 2026-06-25
status: active
related_specs:
  - openspec/changes/05-codex-batch-art-protocol
related_skills:
  - codex-image-gen
  - ai-art
---

## 背景

Claude 主对话**无图像生成能力**，所有美术资源出图都外包给 Codex CLI（`codex exec` + 内置 imagegen system skill）。Codex 额度有限，旧 codex-image-gen SKILL 规定「每张图一次 codex exec」，N 张图 → N 次进程启动 + N 次 system prompt → 固定开销线性增长。

## 决策

把 codex-image-gen SKILL §三 改造为**三档自动归类**：

| 档 | 触发 | 单次 codex exec 产出 | 节省点 |
|---|---|---|---|
| **L0 · 单图模式** | 大图 / debug / fallback | 1 张 PNG | 无节省 |
| **L1 · 进程内并行** | 任意尺寸 → 自动合批 | ≤12 张独立 PNG | 省 N-1 次 system prompt + 进程启动 |
| **L2 · 合并画布** | ≤256×256 + 透明 | 1 张 1024×1024 → ImageCut 切割成 N 张小 PNG | **省 N-1 次 imagegen 调用本身** |

L2 是节省额度的最大头。本 demo 实测：9 张 256×256 透明 icon，1 次 codex exec（85,391 tokens），切出 9 张独立 PNG，相比旧协议预估省 88.9% imagegen 调用。

### 关键技术细节

- **L2 切割工具**：`tools/ImageCut_Tool/image_cut.py`（按 alpha 连通域而非固定网格，灵活）
- **资源名映射**：必须按 `(row_bucket, x_center)` 排序，**不能直接 (y, x)** —— 同一行 icon 的 y 中心可能差 2-5 像素，会跨行错位
- **Codex JSON 必返字段**：`canvas`, `size_bytes`, `status`, `grid_rows`, `grid_cols`, `layout_order`
- **失败策略**：批次内失败收集 → 整批跑完后重试 1 次 → 仍失败标 failed
- **L2 整批失败**自动降级 L1 单图模式重跑

## 被否定的备选

1. **「MCP 包装 Codex」**（视频里的机制②）—— 否定理由：MCP 让 Claude 更频繁调 Codex，反而烧额度，与用户"省额度"需求冲突
2. **「固定网格切割（2×2/4×4/8×8）」** —— 否定理由：ImageCut 按 alpha 连通域更灵活，不强求等距分布，imagegen 可以自由布局
3. **「单次 codex exec 内串行画 N 张」** —— 否定理由：用户实测 Codex 内置 imagegen 支持并行（≥10 张），无需串行
4. **「让 Codex 自己思考提示词」** —— 否定理由：用户明确要求"提示词全部 Claude 一次性准备好，Codex 不思考"，节省 Codex 推理 tokens

## 影响范围

| 类别 | 影响 |
|---|---|
| **SKILL** | [.claude/skills/codex-image-gen/SKILL.md](../../.claude/skills/codex-image-gen/SKILL.md) §三 整体重写 + §五陷阱表更新 |
| **agents** | art-director / art-2d / art-ui（白名单含 codex-image-gen 的 3 个 art-*） |
| **工具链** | `.venv/Scripts/python` + `tools/ImageCut_Tool/image_cut.py`（PIL 12.2.0） |
| **CLAUDE.md** | §六 美术素材生成意图无需改（流程不变，只是 SKILL 内部协议改） |
| **hook** | settings.json 第 4 个 hook 无需改 |

## 过时检查

- **何时 review**：Codex CLI 大版本升级时（imagegen 行为变化）/ ImageCut_Tool 升级 / 用户报告 L2 切割误判率高
- **何时归档**：Claude 主对话本身获得绘图能力时，整条 codex-image-gen 链路废弃
- **风险**：imagegen 不按 layout_order 排布 → SKILL 已加 fallback（Read debug.png 视觉校正 + 降级 L1）

## 实测数据

| 指标 | 数值 |
|---|---|
| Demo 输入 | 9 张 256×256 透明 icon（sword/shield/staff/bow/potion/scroll/gem/key/coin） |
| Codex tokens | 85,391 |
| 旧协议预估 tokens | ~450,000（9 × ~50k） |
| 节省 token | ~81% |
| imagegen 调用数 | 9 → 1（节省 88.9%） |
| ImageCut 切割正确率 | 9/9（100%） |
| 资源名映射正确率 | 9/9（首次按 (y,x) 错 3 张 → 修复为 row-bucketize 后 100%） |

详见 [openspec/changes/05-codex-batch-art-protocol/](../../openspec/changes/05-codex-batch-art-protocol/)
