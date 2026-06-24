---
name: codex-image-gen
description: "将 ai-art 已经沉淀的美术需求和提示词文件交给 Codex CLI 执行实际生图。Claude 主对话本身没有图像生成能力，需要通过 codex exec 把出图任务外包给 Codex（它内置 imagegen 系统 skill 与图像生成模型）。触发场景：用户提到'实现美术素材'、'按提示词出图'、'把美术需求出图'、'生图'、'绘制美术'、'生成美术素材'、'处理美术资源'等，且前置 art/requirements.md + art/prompts.md 已存在。前置工作（提示词与需求生成、风格初始化）走 ai-art SKILL，本 SKILL 只负责最后的执行落盘环节。"
---

# Codex Image Gen — 调用 Codex CLI 执行实际生图

## 一、定位与边界

**做的**：读取已有的 `art/requirements.md` + `art/prompts.md`，逐项调 `codex exec` 调用 Codex 的 imagegen 系统 skill 生成 PNG/JPG 文件，落到 `art/raw/` 并写 `生成记录.md`、更新需求文件头部状态。

**不做的**（→ 走 [ai-art](../ai-art/SKILL.md)）：
- 美术风格初始化、art bible 撰写
- 美术需求与提示词的从零撰写
- 图片反推提示词（image-to-prompt）

**调用关系**：
```
ai-art（沉淀 requirements.md + prompts.md）
       ↓
codex-image-gen（本 SKILL — 调 Codex 实际出图）
       ↓
art/raw/*.png + 生成记录.md
```

## 二、前置条件（不满足直接停手）

1. ✅ `codex --version` 可用（CLI 已安装、已登录）
2. ✅ `openspec/changes/<change-name>/art/requirements.md` 与 `art/prompts.md` 都存在
3. ✅ 这两个文件头部**未**出现 `美术素材状态: 已处理` 或 `美术素材状态: 已归档`（若已存在先询问用户是否重做）

任一不满足：停手并告诉用户具体阻塞点，**禁止**伪造文件、禁止假装已生成。

## 三、执行流程

### 3.1 定位 change

按优先级：
1. 执行 `openspec status` 找 active change（同一时刻应只有 1 个合法）
2. 从用户上下文（最近提到的 change-name）推测
3. 询问用户「目标 change 是哪个？」

### 3.2 解析 prompts.md

对每张图提取：
- `资源名`（也作为文件名基础）
- `提示词`（英文优先，imagegen 对英文响应更好）
- `尺寸`（默认 1024×1024；横构图 1536×1024；竖构图 1024×1536）
- `透明背景`（是 / 否）
- `负面提示词`（如有）
- `候选数量`（默认 1；多候选用 `资源名_v01.png` / `_v02.png`）

多模块需求按 prompts.md / requirements.md 里 `<子模块名>` 字段分组。

### 3.3 构造输出路径

- 单模块：`openspec/changes/<change>/art/raw/<filename>.png`
- 多模块：`openspec/changes/<change>/art/raw/<子模块名>/<filename>.png`

文件名优先用 prompts.md 里指定的；否则用 `序号-资源名.png`。**严禁** `ChatGPT Image ...png` 之类的默认名。

### 3.4 调 Codex 出图

**核心命令模板**（每张图一次调用，更精准、好调试）：

```bash
codex exec -s workspace-write "请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
<这里贴提示词内容>

# 输出要求
- 尺寸：1024x1024
- 透明背景：否
- 保存到：openspec/changes/01-foo/art/raw/sword.png（相对项目根，沙箱会透传到真实工作目录）
- 负面：blurry, watermark, text, signature

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误（账号/配额/参数），不要重试无限次。"
```

**关键参数**：
- `-s workspace-write`：允许写文件
- ❌ **不要**加 `-c sandbox_workspace_write.network_access=true` —— imagegen 走 Codex 内部通道，不需要外开网络
- ❌ **不要**加 `--model` —— 用 Codex 默认模型即可
- 单张耗时约 1-2 分钟，多张时**顺序**调用即可（不要写并行——Codex 单进程更稳）

### 3.5 验证落地

每次 `codex exec` 返回后：
1. 用 `Bash` 检查 `openspec/changes/<change>/art/raw/<filename>.png` 是否存在
2. 检查文件大小 > 1KB（避免空文件 / 错误占位）
3. 若失败：在 `生成记录.md` 标记 `status: failed`，记录 Codex 返回的错误片段，**继续处理下一张**而不是中断

### 3.6 写 `生成记录.md`

文件位置：`openspec/changes/<change>/art/raw/生成记录.md`（多模块时每个子模块目录各有一份）

模板：

```markdown
# 生成记录

生成时间：YYYY-MM-DD HH:MM
执行模型：Codex CLI vX.X.X（imagegen system skill）
总数：N，成功：N，失败：N

| # | 资源名 | 文件 | 尺寸 | 状态 | 提示词摘要 | 备注 |
|---|---|---|---|---|---|---|
| 1 | sword | sword.png | 1024x1024 | ok | A glowing steel longsword... | - |
| 2 | shield | shield.png | 1024x1024 | failed | A round wooden shield... | quota exceeded |
```

### 3.7 更新需求文件头部

在 `art/requirements.md` 与 `art/prompts.md` 文件**顶部**追加/更新（保留正文不动）：

```markdown
美术素材状态: 已处理
处理日期: YYYY-MM-DD
执行 SKILL: codex-image-gen
输出目录: art/raw/
生成记录: art/raw/生成记录.md
```

若部分失败：状态写 `部分已处理`，并在记录里点名失败项，等用户决定是否重试。

## 四、复用 codex exec 的最小调用脚本

可以直接用 Bash 工具调，不需要额外封装。但若 change 内图片很多（>10），可临时写到 `openspec/changes/<change>/art/.codex-batch.sh` 顺序跑（用完即删，不入库）。

## 五、常见陷阱

| 陷阱 | 现象 | 处理 |
|---|---|---|
| Codex 未登录 | `codex doctor` 报 `no Codex credentials` | 停手，提示用户跑 `codex login` |
| 输出路径写绝对路径 | 文件落到 Codex 沙箱镜像里，真实工作树没有 | 一律用相对项目根的路径 |
| 提示词包含中文且图里出现乱码 | imagegen 对中文文字渲染不稳 | 提示词改英文；如果非要中文文字，在生成记录里标注"文字层需后期" |
| 假装成功 | Codex 返回错但流程继续标 ok | **禁止**——逐张验证文件存在 + size > 1KB |
| 一次给 Codex 太多张 | 单次任务超时 / 输出截断 | 一张一次调用，循环处理 |
| 重复生成 | 没看头部 `美术素材状态` 字段 | 处理前先 grep 该字段；已是「已处理」就问用户 |
| 在 `art/raw/` 之外乱写文件 | 测试图、debug 图散落 | 严格只写 `art/raw/` 与对应 `生成记录.md` |

## 六、与 ai-art 的接力关系

如果用户说"实现和处理美术素材"且未先经过 ai-art 的需求/提示词撰写阶段：
- **先**让 ai-art 完成 `requirements.md` + `prompts.md` 撰写
- **再**调本 SKILL 实际出图

如果 ai-art 流程描述里写"调绘图模型逐项生成图片"那一步：
- 当前对话是 Claude → 用本 SKILL 替代该步
- 当前对话是带绘图工具的模型（GPT-Image 系列） → 按 ai-art 原流程直接调

## 七、Definition of Done

- [ ] 每张需求条目都在 `art/raw/`（或子模块目录）有对应 PNG，size > 1KB
- [ ] `生成记录.md` 完整覆盖所有条目（ok / failed 都要写）
- [ ] `requirements.md` 与 `prompts.md` 头部已更新状态字段
- [ ] 失败项已明确告知用户，未隐瞒
