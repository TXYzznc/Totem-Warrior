# Codex CLI 图片生成工作流重构建议

> 面向 Claude 工作流重构使用。
> 目标：让 Claude 负责需求解析、分批、后处理；Codex CLI 只作为图片生成执行器，减少 token 消耗与无意义代理行为。

## 一、背景

当前流程是：Claude 工作流准备美术需求与提示词，然后通过 Codex CLI 调用 imagegen 生成图片文件。Codex 的职责应该非常窄：

1. 接收已经整理好的直接绘图指令。
2. 调用 imagegen / image_gen 生成图片。
3. 按指定路径落盘。
4. 返回机器可解析 JSON。

不希望 Codex 在这个阶段继续承担：

- 阅读 `requirements.md` / `prompts.md`
- 理解项目结构
- 选择技能或代理
- 解释执行过程
- 多轮验证、重试、追问
- 后处理切图
- 写长篇总结

一句话：Claude 做脑子，Codex 做手。

## 二、现有记录暴露的问题

相关文件：

- `openspec/changes/06-v21-implementation/art/.codex-batch.sh`
- `openspec/changes/06-v21-implementation/art/.batch-items.json`
- `openspec/changes/06-v21-implementation/art/.batch-items.tsv`
- `openspec/changes/06-v21-implementation/art/codex_dispatched_prompts.md`
- `openspec/changes/06-v21-implementation/art/codex_usage_analysis.md`

### 1. 把批量任务拆成了大量独立 Codex session

旧脚本在 `.codex-batch.sh` 中使用：

```bash
while IFS=$'\t' read -r FILE SIZE PROMPT NEG; do
  codex exec -s workspace-write "生成一张图片..."
done < "${ITEMS_TSV}"
```

这意味着每一张图都是一次新的 `codex exec`。从记录看，82 个资源产生了 185 次 Codex 调用记录，每次都是独立 session，无法复用上下文。

正确方式不是：

```bash
for item in items; do
  codex exec "生成这一张"
done
```

而是：

```bash
codex exec "生成这一批 items"
```

### 2. 真正的大头是 image_gen 被重复调用

`codex_dispatched_prompts.md` 中的后续分析推翻了 `codex_usage_analysis.md` 的部分早期猜测：

- imagegen `SKILL.md` 并没有每次都被完整重读，实测只读了 1 次。
- 真正大头是每个 Codex session 内部反复调用 `image_gen`。
- 185 次 `codex exec` 中出现 1403 次 `image_gen` 调用，平均约 7.6 次/exec。

这说明旧 prompt 没有明确限制：

- 每个任务最多调用 1 次 imagegen
- 不要重试
- 不要验证 alpha
- 不要走复杂 chroma-key 纠结流程

### 3. 小素材没有使用合图策略

`.batch-items.json` 中大量资源属于小型素材：

- `weapon`
- `skill`
- `affix`
- `paint`
- `recipe`
- `consumable`
- `hud`

但它们全部被标成 `1024x1024` 单图生成，导致本应可以合并到一张画布中的小素材被逐张生成。

对很多小素材，正确做法是：在 prompt 中声明它们需要绘制在同一张大画布中，生成 spritesheet / atlas / contact sheet，然后由 Claude 或本地脚本切图。

### 4. env 贴图被错误标为透明背景

`env_floor_*` / `env_wall_*` 这类 tileable texture 通常应该是不透明贴图，不应默认 `transparent=true`。

透明背景会诱发额外处理流程，也可能破坏贴图用途。

### 5. Codex 在项目根目录启动，吃到了无关上下文

旧命令在项目根目录附近直接运行：

```bash
codex exec -s workspace-write "..."
```

这会让 Codex 加载项目级 `AGENTS.md`、`.codex/agents`、`.agents/skills` 等内容。记录中存在坏 frontmatter、malformed agent role 等噪声。这些不是图片生成所需上下文。

不建议为了图片流程直接删除项目的 `.codex/agents` 或 `.agents/skills`，因为它们是其他工作流的一部分。应改用 runner 目录隔离。

## 三、重构目标

### 总目标

将图片生成流程改为：

```text
Claude 解析 prompts.md / requirements.md
  -> Claude 生成批量任务 JSON
  -> Claude 按 L1/L2/L0 分批
  -> 单次 codex exec 接收一批任务
  -> Codex 只调用 imagegen 生成图片并落盘
  -> Codex 只返回 JSON
  -> Claude / 本地脚本验证文件、切图、写生成记录
```

### Codex 只允许做

- 读取本次 prompt 中给出的 JSON
- 调用 imagegen / image_gen
- 保存图片到指定路径
- 返回 JSON 状态

### Codex 不允许做

- 读取项目文档
- 读取 `SKILL.md`
- 分析需求
- 设计风格
- 修改 `requirements.md` / `prompts.md`
- 本地切图
- 多轮验证
- 自行重试
- 解释过程

## 四、推荐目录结构

在 art 目录下创建运行隔离目录：

```text
openspec/changes/06-v21-implementation/art/
  .codex-runner/
    AGENTS.md
  .batch-items.json
  .batch-l1-001.json
  .sheet-l2-001.json
  .prompt-l1-001.txt
  .prompt-l2-001.txt
  .codex-result-l1-001.json
  .codex-result-l2-001.json
  raw/
    _merged/
```

`.codex-runner/AGENTS.md` 内容保持极短：

```md
你是图片生成执行器。只生成图片并返回 JSON，不解释、不规划、不读取项目。
```

## 五、Codex CLI 调用方式

使用 `-C` 进入极小 runner 目录，并用 `--add-dir` 授权写入项目目录。

示例：

```bash
codex exec \
  -C openspec/changes/06-v21-implementation/art/.codex-runner \
  --add-dir D:/unity/UnityProject/GameDesinger \
  -s workspace-write \
  --ephemeral \
  -o openspec/changes/06-v21-implementation/art/.codex-result-l1-001.json \
  - < openspec/changes/06-v21-implementation/art/.prompt-l1-001.txt
```

说明：

- `-C`：避免加载完整项目上下文。
- `--add-dir`：允许写入真实项目路径。
- `--ephemeral`：一次性执行，不污染会话历史。
- `-o`：把最终 JSON 写入文件，外部工作流直接读取。
- `- < prompt.txt`：避免复杂 shell 引号问题。

## 六、批量策略

### L1：批量独立图片

适用：

- 角色
- boss
- npc
- 大型道具
- 场景图
- 需要单张完整构图的资源

策略：

- 每个 `codex exec` 放 6-8 张。
- 上限 12 张。
- 每个 item 最多 1 次 imagegen。
- 失败只记录 failed，不在 Codex 内重试。

L1 prompt 模板：

```text
你是图片生成执行器。

任务：
读取下面的 batch JSON，在同一个 Codex session 内批量生成所有 items。

硬性规则：
- 不要读取项目文档。
- 不要解释、不要计划、不要询问。
- 每个 item 最多调用 1 次 image_gen。
- 单个 item 失败时记录 failed，并继续下一个。
- 不要自行重试。
- 每张图片必须保存到 file 指定路径。
- 最终只输出合法 JSON 数组，不要 markdown。

返回格式：
[
  {"index":1,"file":"...","size_bytes":123456,"status":"ok"},
  {"index":2,"file":"...","size_bytes":0,"status":"failed","error":"..."}
]

batch JSON:
{
  "mode": "L1_BATCH",
  "items": [
    {
      "index": 1,
      "file": "openspec/changes/06-v21-implementation/art/raw/weapon/weapon_short_blade.png",
      "size": "1024x1024",
      "transparent": true,
      "prompt": "A short blade dagger as a game icon...",
      "negative": "no text, no watermark..."
    }
  ]
}
```

### L2：小素材合并画布

适用：

- icon
- affix
- skill
- paint
- recipe
- consumable
- hud 小素材
- 其他可切图的小型透明资源

策略：

- 一组小素材生成一张 `1024x1024` 透明画布。
- 建议 `4x4`，每张 16 个素材。
- 稳定性优先，先不要一张塞 25 或 64 个。
- 生成后由本地切图脚本处理。
- Codex 不负责切图。

L2 prompt 模板：

```text
你是图片生成执行器。

只调用 1 次 image_gen，生成 1 张 1024x1024 PNG。
不要重试，不要解释，不要读取项目文件。

保存到：
openspec/changes/06-v21-implementation/art/raw/_merged/paint_sheet_01.png

画布要求：
- 1024x1024
- 透明背景
- 4x4 网格
- 每格约 256x256
- 每个素材都必须独立，不要相互接触
- 素材之间至少 32px 完全透明 padding
- 不要文字、数字、水印、边框、标签
- 按从左到右、从上到下排列

素材列表：
1. paint_red_common: simple glass vial filled with vibrant red liquid
2. paint_red_rare: ornate vial with red liquid and subtle ember glow
3. paint_red_legendary: exquisite vial with intense red magical aura
...
16. recipe_scroll_ring: ancient parchment scroll with concentric ring symbol

最终只输出 JSON：
{
  "canvas": "openspec/changes/06-v21-implementation/art/raw/_merged/paint_sheet_01.png",
  "status": "ok",
  "grid_rows": 4,
  "grid_cols": 4,
  "layout_order": [
    "paint_red_common",
    "paint_red_rare"
  ]
}
```

L2 后处理：

```bash
.venv/Scripts/python tools/ImageCut_Tool/image_cut.py \
  openspec/changes/06-v21-implementation/art/raw/_merged/paint_sheet_01.png \
  -o openspec/changes/06-v21-implementation/art/raw/_merged/paint_sheet_01_cut \
  --alpha 16 --min-area 80 --padding 2 --debug --json
```

切图命名时不要简单按 `(y, x)` 排序，应该按 grid row bucketize：

```python
CANVAS_H = manifest["size"]["height"]
ROWS = grid_rows
ROW_H = CANVAS_H / ROWS

def row_then_x(sprite):
    bb = sprite["bbox"]
    cx = bb["x"] + bb["width"] // 2
    cy = bb["y"] + bb["height"] // 2
    return (int(cy / ROW_H), cx)

sprites_sorted = sorted(manifest["sprites"], key=row_then_x)
# sprites_sorted[i] 对应 layout_order[i]
```

### L0：单图 debug

只用于：

- 人类临时检查风格
- 关键图反复试验
- L1/L2 失败后的少量人工修复

不要把 L0 当默认路径。

## 七、资源分类建议

基于当前 `.batch-items.json` 的类型，建议默认分配：

| 类型 | 建议 |
|---|---|
| `character` | L1，大图，透明 |
| `boss` | L1，大图，透明 |
| `npc` | L1，大图，透明 |
| `env_floor_*` | L1，不透明，不要 transparent |
| `env_wall_*` | L1，不透明，不要 transparent |
| `env_light_*` | L1 或 L2，取决于是否需要独立大图 |
| `weapon` | L2 优先；若需要高质量单体可 L1 |
| `skill` | L2 |
| `affix` | L2 |
| `paint` | L2 |
| `recipe` | L2 |
| `consumable` | L2 |
| `hud` | L2 |
| `item` | L2 或 L1，按复杂度决定 |

## 八、脚本重构建议

旧脚本 `.codex-batch.sh` 应拆分为几个阶段。

### 阶段 1：解析

从 `prompts.md` 解析 JSON block，生成规范化 `.batch-items.json`。

每个 item 应包含：

```json
{
  "index": 1,
  "name": "weapon_short_blade",
  "category": "weapon",
  "file": "openspec/changes/06-v21-implementation/art/raw/weapon/weapon_short_blade.png",
  "size": "1024x1024",
  "transparent": true,
  "prompt": "...",
  "negative": "..."
}
```

### 阶段 2：分桶

生成：

- `.batch-l1-001.json`
- `.batch-l1-002.json`
- `.sheet-l2-001.json`
- `.sheet-l2-002.json`

规则：

- L1 chunks：6-8 items。
- L2 chunks：最多 16 items。
- env floor/wall 自动 `transparent=false`。
- icon 类资源可目标化为 256 或 512，但 sheet 画布仍是 1024。

### 阶段 3：生成 prompt 文件

不要在 shell 字符串里拼复杂 prompt。写到文件：

- `.prompt-l1-001.txt`
- `.prompt-l2-001.txt`

这样避免引号、换行、stdin 泄露问题。

### 阶段 4：调用 Codex

每个批次一次 `codex exec`。

不要在 item 循环中调用 `codex exec`。

### 阶段 5：本地验证

由 shell / Python 验证：

- JSON 可解析。
- 目标文件存在。
- 文件 size > 1KB。
- L2 切图数量和 `layout_order` 数量一致。

失败项写入记录，不在 Codex 内重试。

### 阶段 6：生成记录

写入 `raw/生成记录.md`：

- 批次
- 模式 L1/L2/L0
- 资源名
- 文件路径
- 状态 ok/failed/skipped
- 错误原因
- L2 sheet 来源

## 九、必须保留的安全约束

1. 处理前检查 `requirements.md` / `prompts.md` 头部状态，避免重复生成。
2. 不要伪造成功。必须检查文件存在且 size > 1KB。
3. 不要让 Codex 无限重试。
4. 不要让 Codex 切图。
5. 不要把生成结果散落到 `art/raw/` 之外。
6. 不要直接删除项目级 `.codex/agents` 或 `.agents/skills` 来解决图片流程问题；优先用 runner 隔离。
7. 中文文字不要交给 imagegen 直接渲染；需要文字层时后期处理。

## 十、验收标准

重构完成后应满足：

- 外部脚本不再逐图调用 `codex exec`。
- L1 每批 6-8 张，最多 12 张。
- L2 每批最多 16 个小素材合成 1 张 sheet。
- 每个 L1 item 最多 1 次 imagegen。
- 每个 L2 sheet 最多 1 次 imagegen。
- Codex 最终输出只有 JSON。
- `codex exec` 使用 `-C .codex-runner`、`--ephemeral`、`-o result.json`。
- env floor/wall 默认不透明。
- 生成记录覆盖 ok / failed / skipped。
- L2 切图由本地脚本完成，并按 `layout_order` 回填文件名。

## 十一、优先级

按性价比排序：

1. 把逐图 `codex exec` 改成批量 `codex exec`。
2. L2 合图：小素材一张画布生成，再本地切图。
3. Prompt 中强制“只调用 1 次 image_gen，不重试”。
4. 使用 `.codex-runner` 隔离项目上下文。
5. 修正资源尺寸与透明背景标记，尤其 env 贴图。
6. 最终可选：绕过 Codex CLI，改为直接 OpenAI Images API 脚本。

## 十二、最终建议

如果仍然使用 Codex CLI，最佳形态是：

```text
Claude:
  读 prompts.md
  解析为 batch-items.json
  分类 L1/L2
  生成 prompt-l1/l2 文件
  调用少量 codex exec
  本地验证与切图

Codex:
  收到一个批次
  直接 imagegen
  落盘
  返回 JSON
```

这与人工对话中“给 Codex 一个包含很多提示词的文档，让它批量绘制”的效果等价，但 CLI 版本必须显式保证：同一批素材在同一次 `codex exec` 中执行，而不是由外部 shell 循环拆成大量独立 session。
